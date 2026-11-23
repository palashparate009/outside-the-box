using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace OutsideTheBox
{
    public sealed class LiveOpsManager : MonoBehaviour
    {
        public static LiveOpsManager Instance { get; private set; }
        public LevelPlayAdService Ads { get; private set; }
        public PlayGamesService PlayGames { get; private set; }
        public UnityPlayerService Players { get; private set; }

        public static LiveOpsManager Ensure(GameObject owner)
        {
            if (Instance != null) return Instance;
            Instance = FindFirstObjectByType<LiveOpsManager>();
            if (Instance != null) return Instance;
            if (owner == null) return null;
            Instance = owner.GetComponent<LiveOpsManager>();
            if (Instance == null) Instance = owner.AddComponent<LiveOpsManager>();
            return Instance;
        }

        public void Initialize()
        {
            if (Ads != null) return;
            Instance = this;
            LiveOpsConfigStore.Initialize();
            AnalyticsService.Configure();
            AnalyticsService.Log("app_start");
            Ads = new LevelPlayAdService(this);
            PlayGames = new PlayGamesService();
            Players = new UnityPlayerService();
            Ads.Initialize(LiveOpsConfigStore.Current);
            PlayGames.Initialize(LiveOpsConfigStore.Current);
            StartCoroutine(FetchRemoteConfig());
            AnalyticsService.Log("app_liveops_ready");
        }

        private IEnumerator FetchRemoteConfig()
        {
            var settings = LiveOpsConfigStore.Current;
            if (!settings.firebaseRemoteConfigEnabled) yield break;
            var appType = ReflectionUtility.FindType("Firebase.FirebaseApp");
            var remoteType = ReflectionUtility.FindType("Firebase.RemoteConfig.FirebaseRemoteConfig");
            if (appType == null || remoteType == null)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                yield return FetchRemoteConfigFromAndroid(settings);
#endif
                yield break;
            }

            object dependencyTask;
            try
            {
                dependencyTask = appType.GetMethod("CheckAndFixDependenciesAsync",
                    BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Firebase initialization failed: {exception.GetBaseException().Message}");
                yield break;
            }
            yield return WaitForTask(dependencyTask);
            if (!TaskSucceeded(dependencyTask)) yield break;

            object remote;
            try { remote = remoteType.GetProperty("DefaultInstance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null); }
            catch { yield break; }
            if (remote == null) yield break;

            var defaults = new Dictionary<string, object>
            {
                [settings.remoteConfigJsonKey] = JsonUtility.ToJson(settings)
            };
            yield return WaitForTask(ReflectionUtility.InvokeBest(remote, "SetDefaultsAsync", defaults));
            var fetch = ReflectionUtility.InvokeBest(remote, "FetchAsync", TimeSpan.FromMinutes(settings.remoteFetchMinutes));
            yield return WaitForTask(fetch);
            if (!TaskSucceeded(fetch)) yield break;
            yield return WaitForTask(ReflectionUtility.InvokeBest(remote, "ActivateAsync"));

            try
            {
                var configValue = ReflectionUtility.InvokeBest(remote, "GetValue", settings.remoteConfigJsonKey);
                var json = configValue?.GetType().GetProperty("StringValue")?.GetValue(configValue) as string;
                ApplyRemoteConfig(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Firebase Remote Config value could not be applied: {exception.GetBaseException().Message}");
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private IEnumerator FetchRemoteConfigFromAndroid(LiveOpsConfigData settings)
        {
            AndroidJavaObject remote = null;
            AndroidJavaObject task = null;
            try
            {
                using (var type = new AndroidJavaClass("com.google.firebase.remoteconfig.FirebaseRemoteConfig"))
                    remote = type.CallStatic<AndroidJavaObject>("getInstance");
                using (var builder = new AndroidJavaObject(
                           "com.google.firebase.remoteconfig.FirebaseRemoteConfigSettings$Builder"))
                {
                    builder.Call<AndroidJavaObject>("setMinimumFetchIntervalInSeconds",
                        Math.Max(0, settings.remoteFetchMinutes) * 60L);
                    using (var configSettings = builder.Call<AndroidJavaObject>("build"))
                        remote.Call<AndroidJavaObject>("setConfigSettingsAsync", configSettings)?.Dispose();
                }
                task = remote.Call<AndroidJavaObject>("fetchAndActivate");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Firebase Remote Config could not start: {exception.Message}");
                remote?.Dispose();
                yield break;
            }

            var timeoutAt = Time.realtimeSinceStartup + settings.remoteFetchTimeoutSeconds;
            while (Time.realtimeSinceStartup < timeoutAt)
            {
                bool complete;
                try { complete = task.Call<bool>("isComplete"); }
                catch { complete = true; }
                if (complete) break;
                yield return null;
            }

            var json = string.Empty;
            try
            {
                if (task.Call<bool>("isComplete") && task.Call<bool>("isSuccessful"))
                    json = remote.Call<string>("getString", settings.remoteConfigJsonKey);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Firebase Remote Config fetch failed: {exception.Message}");
            }
            task?.Dispose();
            remote?.Dispose();
            ApplyRemoteConfig(json);
        }
#endif

        private void ApplyRemoteConfig(string json)
        {
            if (!LiveOpsConfigStore.ApplyRemoteJson(json)) return;
            AnalyticsService.Configure();
            EconomyService.Initialize(LiveOpsConfigStore.Current.startingCoins);
            Ads.Initialize(LiveOpsConfigStore.Current);
            PlayGames.Initialize(LiveOpsConfigStore.Current);
            FindFirstObjectByType<GameSession>()?.ApplyPlayerSettings();
            LocalGameNotifications.RefreshPushRegistration();
            AnalyticsService.Log("remote_config_applied", "version", LiveOpsConfigStore.Current.version);
        }

        private static IEnumerator WaitForTask(object taskObject)
        {
            if (taskObject is not Task task) yield break;
            while (!task.IsCompleted) yield return null;
        }

        private static bool TaskSucceeded(object taskObject) =>
            taskObject is not Task task || task.Status == TaskStatus.RanToCompletion;

        private void OnApplicationPause(bool paused) => Ads?.OnApplicationPause(paused);
        private void OnDestroy()
        {
            Ads?.Shutdown();
            if (Instance == this) Instance = null;
        }
        private void OnApplicationQuit() => SaveService.Flush();
    }
}
