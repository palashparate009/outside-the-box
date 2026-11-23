using System;
using System.Collections.Generic;
using UnityEngine;

namespace OutsideTheBox
{
    /// <summary>
    /// Google UMP consent gate used before any mediated ad SDK is initialized.
    /// This component lives on the GameRoot prefab; it never creates a hidden runtime object.
    /// </summary>
    [DefaultExecutionOrder(-1050)]
    public sealed class GoogleUmpConsent : MonoBehaviour
    {
        private static GoogleUmpConsent instance;
        private readonly Queue<Action> mainThreadActions = new();
        private readonly List<Action<bool>> readyCallbacks = new();
        private bool started;
        private bool completed;
        private bool canRequestAds;
        private bool privacyOptionsRequired = false;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject activity;
        private AndroidJavaObject consentInformation;
        private ConsentInfoSuccessProxy consentInfoSuccessProxy;
        private ConsentInfoFailureProxy consentInfoFailureProxy;
        private ConsentFormDismissedProxy formDismissedProxy;
        private PrivacyFormDismissedProxy privacyFormDismissedProxy;
#endif

        public static bool PrivacyOptionsRequired => instance != null && instance.privacyOptionsRequired;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                enabled = false;
                return;
            }
            instance = this;
        }

        private void Update()
        {
            lock (mainThreadActions)
            {
                while (mainThreadActions.Count > 0) mainThreadActions.Dequeue()?.Invoke();
            }
        }

        public static GoogleUmpConsent Ensure(GameObject owner)
        {
            if (instance != null) return instance;
            if (owner == null) return null;
            instance = owner.GetComponent<GoogleUmpConsent>();
            if (instance == null) instance = owner.AddComponent<GoogleUmpConsent>();
            return instance;
        }

        public void EnsureReady(bool adsEnabled, Action<bool> callback)
        {
            if (completed)
            {
                callback?.Invoke(canRequestAds);
                return;
            }
            if (callback != null) readyCallbacks.Add(callback);
            if (started) return;
            started = true;

            if (!adsEnabled)
            {
                Complete(false);
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            BeginAndroidConsentFlow();
#else
            Complete(true);
#endif
        }

        public static void ShowPrivacyOptions(Action<bool> callback)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (instance != null) instance.ShowAndroidPrivacyOptions(callback);
            else callback?.Invoke(false);
#else
            callback?.Invoke(false);
#endif
        }

        private void Complete(bool adsAllowed)
        {
            completed = true;
            canRequestAds = adsAllowed;
            var callbacks = readyCallbacks.ToArray();
            readyCallbacks.Clear();
            foreach (var callback in callbacks) callback?.Invoke(adsAllowed);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private void BeginAndroidConsentFlow()
        {
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using (var ump = new AndroidJavaClass("com.google.android.ump.UserMessagingPlatform"))
                    consentInformation = ump.CallStatic<AndroidJavaObject>("getConsentInformation", activity);

                var builder = new AndroidJavaObject("com.google.android.ump.ConsentRequestParameters$Builder");
                var parameters = builder.Call<AndroidJavaObject>("build");
                builder.Dispose();
                consentInfoSuccessProxy = new ConsentInfoSuccessProxy(this);
                consentInfoFailureProxy = new ConsentInfoFailureProxy(this);
                RunOnAndroidUiThread(() =>
                {
                    consentInformation.Call("requestConsentInfoUpdate", activity, parameters,
                        consentInfoSuccessProxy, consentInfoFailureProxy);
                    parameters.Dispose();
                });
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Google UMP consent could not start; ads remain disabled: {exception.Message}");
                Complete(false);
            }
        }

        private void LoadAndShowRequiredForm()
        {
            try
            {
                formDismissedProxy = new ConsentFormDismissedProxy(this);
                RunOnAndroidUiThread(() =>
                {
                    using (var ump = new AndroidJavaClass("com.google.android.ump.UserMessagingPlatform"))
                        ump.CallStatic("loadAndShowConsentFormIfRequired", activity, formDismissedProxy);
                });
            }
            catch (Exception exception)
            {
                RefreshConsentState(exception.Message);
            }
        }

        private void RefreshConsentState(string errorMessage = "")
        {
            try { canRequestAds = consentInformation != null && consentInformation.Call<bool>("canRequestAds"); }
            catch (Exception exception)
            {
                canRequestAds = false;
                if (string.IsNullOrWhiteSpace(errorMessage)) errorMessage = exception.Message;
            }
            privacyOptionsRequired = ReadPrivacyOptionsRequired();
            if (!string.IsNullOrWhiteSpace(errorMessage))
                Debug.LogWarning($"Google UMP consent status: {errorMessage}");
            AnalyticsService.Log("consent_ready", "ads_allowed", canRequestAds ? 1 : 0);
            Complete(canRequestAds);
        }

        private bool ReadPrivacyOptionsRequired()
        {
            if (consentInformation == null) return false;
            try
            {
                using (var status = consentInformation.Call<AndroidJavaObject>("getPrivacyOptionsRequirementStatus"))
                {
                    if (status == null) return false;
                    string name;
                    try { name = status.Call<string>("name"); }
                    catch { name = status.Call<string>("toString"); }
                    return string.Equals(name, "REQUIRED", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { return false; }
        }

        private void ShowAndroidPrivacyOptions(Action<bool> callback)
        {
            if (!completed || !privacyOptionsRequired || activity == null)
            {
                callback?.Invoke(false);
                return;
            }
            privacyFormDismissedProxy = new PrivacyFormDismissedProxy(this, callback);
            RunOnAndroidUiThread(() =>
            {
                using (var ump = new AndroidJavaClass("com.google.android.ump.UserMessagingPlatform"))
                    ump.CallStatic("showPrivacyOptionsForm", activity, privacyFormDismissedProxy);
            });
        }

        private void RunOnAndroidUiThread(Action action) =>
            activity.Call("runOnUiThread", new AndroidJavaRunnable(action));

        private void Post(Action action)
        {
            if (action == null) return;
            lock (mainThreadActions) mainThreadActions.Enqueue(action);
        }

        private static string ReadError(AndroidJavaObject error)
        {
            if (error == null) return string.Empty;
            try { return error.Call<string>("getMessage") ?? "Unknown UMP error"; }
            catch { return "Unknown UMP error"; }
        }

        private sealed class ConsentInfoSuccessProxy : AndroidJavaProxy
        {
            private readonly GoogleUmpConsent owner;
            public ConsentInfoSuccessProxy(GoogleUmpConsent owner)
                : base("com.google.android.ump.ConsentInformation$OnConsentInfoUpdateSuccessListener") => this.owner = owner;
            public void onConsentInfoUpdateSuccess() => owner.Post(owner.LoadAndShowRequiredForm);
        }

        private sealed class ConsentInfoFailureProxy : AndroidJavaProxy
        {
            private readonly GoogleUmpConsent owner;
            public ConsentInfoFailureProxy(GoogleUmpConsent owner)
                : base("com.google.android.ump.ConsentInformation$OnConsentInfoUpdateFailureListener") => this.owner = owner;
            public void onConsentInfoUpdateFailure(AndroidJavaObject error)
            {
                var message = ReadError(error);
                owner.Post(() => owner.RefreshConsentState(message));
            }
        }

        private sealed class ConsentFormDismissedProxy : AndroidJavaProxy
        {
            private readonly GoogleUmpConsent owner;
            public ConsentFormDismissedProxy(GoogleUmpConsent owner)
                : base("com.google.android.ump.ConsentForm$OnConsentFormDismissedListener") => this.owner = owner;
            public void onConsentFormDismissed(AndroidJavaObject error)
            {
                var message = ReadError(error);
                owner.Post(() => owner.RefreshConsentState(message));
            }
        }

        private sealed class PrivacyFormDismissedProxy : AndroidJavaProxy
        {
            private readonly GoogleUmpConsent owner;
            private readonly Action<bool> callback;
            public PrivacyFormDismissedProxy(GoogleUmpConsent owner, Action<bool> callback)
                : base("com.google.android.ump.ConsentForm$OnConsentFormDismissedListener")
            {
                this.owner = owner;
                this.callback = callback;
            }
            public void onConsentFormDismissed(AndroidJavaObject error)
            {
                var message = ReadError(error);
                owner.Post(() =>
                {
                    owner.RefreshConsentState(message);
                    callback?.Invoke(string.IsNullOrEmpty(message));
                });
            }
        }
#endif

        private void OnDestroy()
        {
            if (instance != this) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            consentInformation?.Dispose();
            activity?.Dispose();
#endif
            instance = null;
        }
    }
}
