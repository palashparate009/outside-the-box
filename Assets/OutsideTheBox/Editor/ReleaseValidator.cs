using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OutsideTheBox.Editor
{
    public static class ReleaseValidator
    {
        private const string Root = "Assets/OutsideTheBox";
        private const string ConfigPath = Root + "/Resources/OutsideTheBox/Configs/GameConfig.asset";
        private const string LiveOpsConfigPath = Root + "/Resources/OutsideTheBox/Configs/LiveOpsConfig.asset";
        private const string CatalogPath = Root + "/Resources/OutsideTheBox/Configs/LevelCatalog.asset";
        private const string RegistryPath = Root + "/Resources/OutsideTheBox/Configs/RuntimePrefabRegistry.asset";

        [MenuItem("Tools/Outside The Box/Validate Android Release")]
        public static void ValidateAndroidRelease()
        {
            var failures = new List<string>();
            var warnings = new List<string>();
            var passes = new List<string>();
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            var liveOpsConfig = AssetDatabase.LoadAssetAtPath<LiveOpsConfigAsset>(LiveOpsConfigPath);
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
            var registry = AssetDatabase.LoadAssetAtPath<RuntimePrefabRegistry>(RegistryPath);
            Check(registry != null && registry.quitConfirmationPrefab != null && registry.offlinePanelPrefab != null &&
                  registry.cloudSyncPanelPrefab != null && registry.loadingOverlayPrefab != null && registry.screenTransitionPrefab != null,
                "Quit, offline, cloud-sync, loading and transition screen prefabs are assigned", failures, passes);

            Check(config != null, "GameConfig asset exists", failures, passes);
            if (config != null && config.previewPipeLevelTransitions)
            {
                warnings.Add("TEST PREVIEW: pipe level transitions are enabled. Turn off GameConfig > Testing > Preview Pipe Level Transitions to restore normal transitions before production if the experiment is not approved.");
                Check(registry != null && (config.pipeTransitionPreviewStyle == PipeTransitionPreviewStyle.ThreeDimensional
                        ? registry.levelPipeTransition3DPrefab != null && registry.levelPipeTransition3DPrefab.GetComponent<LevelPipeTransition3D>() != null
                        : registry.levelPipeTransitionPrefab != null && registry.levelPipeTransitionPrefab.GetComponent<LevelPipeTransition>() != null),
                    "Selected pipe transition preview prefab is imported and assigned", failures, passes);
                if (config.pipeTransitionPreviewStyle == PipeTransitionPreviewStyle.ThreeDimensional && registry != null)
                {
                    var cinematic = registry.levelPipeTransition3DPrefab != null
                        ? registry.levelPipeTransition3DPrefab.GetComponent<LevelPipeTransition3D>() : null;
                    var board = cinematic != null ? cinematic.board : null;
                    Check(board != null && board.boardRoot != null && board.pipeRoot != null &&
                          board.captureCamera != null && board.face != null && board.sides != null && board.faceRenderer != null,
                        "3D board model and snapshot camera are assigned", failures, passes);
                }
            }
            Check(liveOpsConfig != null && liveOpsConfig.values != null,
                "LiveOpsConfig ScriptableObject exists", failures, passes);
            Check(catalog != null && catalog.Count == 60, "Campaign contains 60 levels", failures, passes);
            if (config != null && config.focusedChapterDesignEnabled)
            {
                var icons = MechanicIconLibrary.Current;
                Check(icons != null && icons.icons != null && icons.icons.Length == Enum.GetValues(typeof(MechanicIconId)).Length &&
                      icons.icons.All(s => s != null), "All mechanic icons are assigned", failures, passes);
                Check(registry != null && registry.mechanicWorldIconPrefab != null && registry.mechanicTutorialIconPrefab != null &&
                      registry.mechanicButtonIconPrefab != null && registry.mechanicLegendPrefab != null &&
                      registry.mechanicLegendPrefab.GetComponent<MechanicLegendView>() != null,
                    "Mechanic icons have authored world, tutorial and HUD prefabs", failures, passes);
                var gameplay = registry != null && registry.gameplayHudPrefab != null
                    ? registry.gameplayHudPrefab.GetComponent<GameplayHudPresentation>() : null;
                Check(gameplay != null && gameplay.chapterCaption != null && gameplay.timeFill != null &&
                      gameplay.energyFill != null && gameplay.forceControl != null && gameplay.echoControl != null,
                    "Authored gameplay HUD has its stats, meters and controls assigned", failures, passes);
            }
            if (config != null && config.railChallengesEnabled)
            {
                var marker = registry != null && registry.railChallengeMarkerPrefab != null
                    ? registry.railChallengeMarkerPrefab.GetComponent<RailChallengeMarker>() : null;
                Check(marker != null && marker.surface != null && marker.surface.sprite != null && marker.fill != null && marker.lockIcon != null &&
                      marker.switchIcon != null && marker.stripes != null && marker.stripes.Length > 0,
                    "Rail challenge marker prefab and icons are assigned", failures, passes);
                Check(config.railChallengeTutorials != null && config.railChallengeTutorials.Length == 6 &&
                      config.railChallengeTutorials.All(t => t != null && t.id > 0 && !string.IsNullOrWhiteSpace(t.body)) &&
                      config.railChallengeTutorials.Select(t => t.id).Distinct().Count() == 6,
                    "All six rail mechanics have unique first-encounter tutorials", failures, passes);
            }
            Check(registry != null && registry.gameRootPrefab != null && registry.hudPrefab != null &&
                  registry.feedbackBurstPrefab != null && registry.chapterBackgroundPrefab != null &&
                  registry.signInPanelPrefab != null,
                "Runtime prefab registry is complete", failures, passes);
            var tutorialPopup = registry != null && registry.tutorialOverlayPrefab != null
                ? registry.tutorialOverlayPrefab.GetComponent<TutorialPopupView>() : null;
            Check(tutorialPopup != null && tutorialPopup.card != null &&
                  tutorialPopup.inputGroup != null && tutorialPopup.cardGroup != null &&
                  tutorialPopup.backdropGroup != null && tutorialPopup.backdrop != null &&
                  tutorialPopup.backdrop.transform == tutorialPopup.backdropGroup.transform &&
                  tutorialPopup.backdropOpacity > 0f && tutorialPopup.backdropOpacity < 1f &&
                  tutorialPopup.cardGroup.transform == tutorialPopup.card &&
                  tutorialPopup.backdropGroup.transform.parent == tutorialPopup.transform &&
                  tutorialPopup.card.parent == tutorialPopup.transform,
                "FTUE has an authored popup card and separate full-screen backdrop", failures, passes);
            Check(registry != null && registry.storePanelPrefab != null && registry.gemStorePanelPrefab != null &&
                  registry.gemBalanceButtonPrefab != null,
                "Cosmetic store, gem store and gem balance button prefabs are assigned", failures, passes);
            if (registry != null && registry.gameRootPrefab != null)
            {
                Check(registry.gameRootPrefab.GetComponentsInChildren<Component>(true).All(component => component != null),
                    "GameRoot prefab contains no missing scripts", failures, passes);
                Check(registry.gameRootPrefab.GetComponent<MobilePerformanceController>() != null,
                    "Adaptive mobile performance controller is included in GameRoot", failures, passes);
            }
            if (registry != null && registry.uiCanvasPrefab != null)
                Check(registry.uiCanvasPrefab.GetComponent<ResponsiveCanvasScaler>() != null,
                    "Responsive canvas scaling is configured", failures, passes);
            Check(EditorBuildSettings.scenes.Length > 0 && EditorBuildSettings.scenes[0].enabled,
                "Gameplay scene is enabled in Build Settings", failures, passes);

            var identifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            Check(!string.IsNullOrWhiteSpace(identifier) && !identifier.Contains("DefaultCompany"),
                $"Android application ID is configured ({identifier})", failures, passes);
            Check(EditorUserBuildSettings.buildAppBundle, "Android App Bundle output is enabled", failures, passes);
            Check(!EditorUserBuildSettings.development && !EditorUserBuildSettings.allowDebugging &&
                  !EditorUserBuildSettings.connectProfiler && !EditorUserBuildSettings.buildWithDeepProfilingSupport,
                "Development build, script debugging and profiler connections are disabled", failures, passes);
            Check(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP,
                "Android uses the IL2CPP scripting backend", failures, passes);
            var billingModePath = "Assets/Resources/BillingMode.json";
            Check(File.Exists(billingModePath) && File.ReadAllText(billingModePath).Contains("\"GooglePlay\""),
                "Purchases use the Google Play store", failures, passes);
            Check(PlayerSettings.Android.targetArchitectures.HasFlag(AndroidArchitecture.ARM64),
                "ARM64 architecture is enabled", failures, passes);
            Check((int)PlayerSettings.Android.targetSdkVersion >= 36,
                "Android target API is 36 or newer", failures, passes);
            try
            {
                AndroidLauncherBuildGuard.ValidateLauncherManifest("Assets/Plugins/Android/AndroidManifest.xml");
                passes.Add("Android manifest contains an exported Unity launcher activity");
            }
            catch (Exception exception) { failures.Add(exception.Message); }

            if (config != null)
            {
                Check(!config.unlockAllChaptersForTesting,
                    "Testing chapter unlock is disabled", failures, passes);
                Check(Uri.TryCreate(config.privacyPolicyUrl, UriKind.Absolute, out var privacyUri) &&
                      privacyUri.Scheme == Uri.UriSchemeHttps,
                    "HTTPS privacy policy URL is configured", failures, passes);
                Check(!string.IsNullOrWhiteSpace(config.supportEmail) && config.supportEmail.Contains("@"),
                    "Support email is configured", failures, passes);
            }

            if (!PlayerSettings.Android.useCustomKeystore)
                warnings.Add("Configure a private Android signing keystore before uploading the release AAB.");
            warnings.Add("Replace procedural fallback audio with licensed final WAV/OGG music and effects before store review.");
            warnings.Add("Capture final phone screenshots, feature graphic, icon, short description, and store listing copy.");
            warnings.Add("Complete Play Console Data Safety, content rating, target audience, ads, and privacy declarations.");
            warnings.Add("Run a closed device test and review Android Vitals / pre-launch reports before production rollout.");
            warnings.Add("Gem purchase receipts still require server-side validation and an authoritative wallet before public paid-currency rollout. Use license-test payments in closed testing.");

            var report = new List<string>
            {
                "# Outside the Box — Android Release Validation",
                string.Empty,
                $"Generated: {DateTime.UtcNow:O}",
                string.Empty,
                "## Passed"
            };
            foreach (var item in passes) report.Add($"- [x] {item}");
            report.Add(string.Empty);
            report.Add("## Blocking failures");
            if (failures.Count == 0) report.Add("- None");
            else foreach (var item in failures) report.Add($"- [ ] {item}");
            report.Add(string.Empty);
            report.Add("## Owner inputs / warnings");
            foreach (var item in warnings) report.Add($"- [ ] {item}");

            var reportPath = Path.GetFullPath(Root + "/Documentation/ReleaseValidation.md");
            File.WriteAllLines(reportPath, report);
            AssetDatabase.Refresh();
            var summary = $"Outside the Box release validation: {passes.Count} passed, {failures.Count} blocking, {warnings.Count} owner inputs.";
            if (failures.Count == 0) Debug.Log(summary);
            else Debug.LogWarning(summary + " See Assets/OutsideTheBox/Documentation/ReleaseValidation.md");
        }

        [MenuItem("Tools/Outside The Box/Configure Release Build Flags")]
        private static void ConfigureReleaseFlags()
        {
            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.buildWithDeepProfilingSupport = false;
            Debug.Log("Release flags configured: Android App Bundle, non-development, debugging disabled.");
        }

        private static void Check(bool condition, string message, ICollection<string> failures,
            ICollection<string> passes)
        {
            if (condition) passes.Add(message);
            else failures.Add(message);
        }
    }

    /// <summary>Stops a build before Gradle if its launcher entry has been lost.</summary>
    public sealed class AndroidLauncherBuildGuard : IPreprocessBuildWithReport, IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 10000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.Android)
                ValidateLauncherManifest("Assets/Plugins/Android/AndroidManifest.xml");
        }

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            ValidateLauncherManifest(Path.Combine(path, "src/main/AndroidManifest.xml"));
        }

        public static void ValidateLauncherManifest(string path)
        {
            if (!File.Exists(path)) throw new BuildFailedException($"Android manifest missing: {path}");
            var document = new XmlDocument { XmlResolver = null };
            document.Load(path);
            var ns = new XmlNamespaceManager(document.NameTable);
            const string android = "http://schemas.android.com/apk/res/android";
            ns.AddNamespace("android", android);
            var launchers = document.SelectNodes("/manifest/application/activity[intent-filter[" +
                "action[@android:name='android.intent.action.MAIN'] and " +
                "category[@android:name='android.intent.category.LAUNCHER']]]", ns);
            if (launchers == null || launchers.Count != 1)
                throw new BuildFailedException($"Android needs exactly one launcher activity with MAIN + LAUNCHER. " +
                    $"Found {launchers?.Count ?? 0} in {path}. Without it the installed game has no icon or Play button.");
            var activity = launchers[0] as XmlElement;
            var application = activity?.ParentNode as XmlElement;
            if (activity == null || activity.GetAttribute("exported", android) != "true" ||
                activity.GetAttribute("enabled", android) == "false" || application?.GetAttribute("enabled", android) == "false")
                throw new BuildFailedException($"Android launcher must be enabled and exported in {path}.");
            var unityEntry = activity.SelectSingleNode("meta-data[@android:name='unityplayer.UnityActivity'][@android:value='true']", ns);
            if (unityEntry == null)
                throw new BuildFailedException($"Android launcher is missing Unity activity metadata in {path}.");
            if (activity.GetAttribute("name", android) == "com.unity3d.player.UnityPlayerGameActivity" &&
                activity.SelectSingleNode("meta-data[@android:name='android.app.lib_name'][@android:value='game']", ns) == null)
                throw new BuildFailedException($"Unity GameActivity is missing its native game library metadata in {path}.");
        }
    }

    public sealed class BalanceReportWindow : EditorWindow
    {
        private const string BalanceRoot = "Assets/OutsideTheBox";
        private Vector2 scroll;
        private PlayerTelemetryData telemetry;
        private int levelCount;

        [MenuItem("Tools/Outside The Box/Player Balance Report")]
        private static void Open() => GetWindow<BalanceReportWindow>("Balance Report");

        private void OnEnable() => Reload();

        private void Reload()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(BalanceRoot + "/Resources/OutsideTheBox/Configs/GameConfig.asset");
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(BalanceRoot + "/Resources/OutsideTheBox/Configs/LevelCatalog.asset");
            levelCount = catalog?.Count ?? 0;
            if (config != null && levelCount > 0) telemetry = SaveService.Load(config, levelCount).telemetry;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Local Playtest Balance Data", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Stored locally only. No player identifiers or network analytics are used.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh")) Reload();
                if (GUILayout.Button("Export CSV")) ExportCsv();
            }

            if (telemetry == null)
            {
                EditorGUILayout.HelpBox("Play the game once to create telemetry data.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField($"Play time: {telemetry.playSeconds / 60f:0.0} minutes   " +
                                       $"Endless: {telemetry.endlessCompletions}/{telemetry.endlessAttempts}");
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Level     Attempts   Complete   Rate     Restarts   Hints   Hits", EditorStyles.miniBoldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (var i = 0; i < levelCount; i++)
            {
                var attempts = Value(telemetry.attempts, i);
                var completions = Value(telemetry.completions, i);
                var rate = attempts > 0 ? completions * 100f / attempts : 0f;
                EditorGUILayout.LabelField(
                    $"{i + 1,3}       {attempts,5}       {completions,5}    {rate,5:0}%       " +
                    $"{Value(telemetry.restarts, i),5}     {Value(telemetry.hints, i),4}   {Value(telemetry.collisions, i),4}");
            }
            EditorGUILayout.EndScrollView();
        }

        private void ExportCsv()
        {
            Reload();
            if (telemetry == null) return;
            var lines = new List<string> { "Level,Attempts,Completions,CompletionRate,Restarts,Hints,Collisions" };
            for (var i = 0; i < levelCount; i++)
            {
                var attempts = Value(telemetry.attempts, i);
                var completions = Value(telemetry.completions, i);
                var rate = attempts > 0 ? completions / (float)attempts : 0f;
                lines.Add($"{i + 1},{attempts},{completions},{rate:0.000}," +
                          $"{Value(telemetry.restarts, i)},{Value(telemetry.hints, i)},{Value(telemetry.collisions, i)}");
            }

            var path = Path.GetFullPath(BalanceRoot + "/Documentation/BalanceReport.csv");
            File.WriteAllLines(path, lines);
            AssetDatabase.Refresh();
            Debug.Log($"Outside the Box balance report exported to {path}");
        }

        private static int Value(IReadOnlyList<int> values, int index) =>
            values != null && index >= 0 && index < values.Count ? values[index] : 0;
    }

    public static class LiveOpsSetupValidator
    {
        private const string LiveOpsJsonPath = "Assets/OutsideTheBox/Resources/OutsideTheBox/default_live_config.json";
        private const string LiveOpsReportPath = "Assets/OutsideTheBox/Documentation/LiveOpsValidation.md";

        [MenuItem("Tools/Outside The Box/Export Firebase Remote Config JSON")]
        private static void ExportRemoteConfigJson()
        {
            var config = AssetDatabase.LoadAssetAtPath<LiveOpsConfigAsset>(
                "Assets/OutsideTheBox/Resources/OutsideTheBox/Configs/LiveOpsConfig.asset");
            if (config == null || config.values == null)
            {
                Debug.LogError("Missing LiveOpsConfig.asset. Run Tools > Outside The Box > Build Project Content.");
                return;
            }
            config.values.Normalize();
            var json = JsonUtility.ToJson(config.values, true) + Environment.NewLine;
            File.WriteAllText(Path.GetFullPath(LiveOpsJsonPath), json);
            var destination = Path.GetFullPath("Assets/OutsideTheBox/Documentation/FirebaseRemoteConfigValue.json");
            File.WriteAllText(destination, json);
            AssetDatabase.Refresh();
            EditorGUIUtility.systemCopyBuffer = json;
            Debug.Log("Remote Config JSON exported and copied. Paste it into Firebase parameter 'game_config_json'.");
        }

        [MenuItem("Tools/Outside The Box/Resolve Android Live Ops Dependencies")]
        private static void ResolveAndroidDependencies()
        {
            var resolverType = ReflectionUtility.FindType("GooglePlayServices.PlayServicesResolver");
            var method = resolverType?.GetMethods(System.Reflection.BindingFlags.Public |
                                                   System.Reflection.BindingFlags.Static)
                .FirstOrDefault(candidate => candidate.Name == "ResolveSync" &&
                                             candidate.GetParameters().Length <= 1);
            if (method == null)
            {
                Debug.LogError("External Dependency Manager Android resolver is not available.");
                return;
            }
            var parameters = method.GetParameters();
            var result = method.Invoke(null, parameters.Length == 0 ? null : new object[] { true });
            Debug.Log(result is bool success && !success
                ? "Android dependency resolution reported a failure. Check the Console for the package that failed."
                : "Android live-ops dependency resolution completed.");
        }

        [MenuItem("Tools/Outside The Box/Validate Live Ops Setup")]
        private static void Validate()
        {
            var failures = new List<string>();
            var passes = new List<string>();
            var warnings = new List<string>();
            var source = AssetDatabase.LoadAssetAtPath<TextAsset>(LiveOpsJsonPath);
            var configAsset = AssetDatabase.LoadAssetAtPath<LiveOpsConfigAsset>(
                "Assets/OutsideTheBox/Resources/OutsideTheBox/Configs/LiveOpsConfig.asset");
            LiveOpsConfigData settings = null;
            if (source != null)
            {
                try { settings = JsonUtility.FromJson<LiveOpsConfigData>(source.text); }
                catch { /* Reported below. */ }
            }

            Add(source != null && settings != null && settings.version > 0,
                "Default live configuration JSON is valid", failures, passes);
            Add(configAsset != null && configAsset.values != null && configAsset.values.version > 0,
                "LiveOpsConfig ScriptableObject is valid", failures, passes);
            var packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            Add(packages.Any(package => package.name == "com.unity.services.levelplay"),
                "Unity LevelPlay package is installed", failures, passes);
            Add(File.Exists(Path.GetFullPath("Assets/LevelPlay/Editor/IronSourceSDKDependencies.xml")) &&
                File.Exists(Path.GetFullPath("Assets/LevelPlay/Editor/ISAdMobAdapterDependencies.xml")) &&
                File.Exists(Path.GetFullPath("Assets/LevelPlay/Editor/ISUnityAdsAdapterDependencies.xml")),
                "LevelPlay, AdMob, and Unity Ads Android dependencies are declared", failures, passes);
            var firebaseManaged = ReflectionUtility.FindType("Firebase.FirebaseApp") != null &&
                                  ReflectionUtility.FindType("Firebase.Analytics.FirebaseAnalytics") != null;
            var nativeDependenciesPath = Path.GetFullPath(
                "Assets/OutsideTheBox/Editor/OutsideTheBoxLiveOpsDependencies.xml");
            var nativeFirebaseDependencies = File.Exists(nativeDependenciesPath);
            Add(firebaseManaged || nativeFirebaseDependencies,
                "Firebase Analytics and Remote Config integration is installed", failures, passes);
            Add(ReflectionUtility.FindType("GooglePlayGames.PlayGamesPlatform") != null,
                "Google Play Games plugin is installed", failures, passes);
            Add(ReflectionUtility.FindType("Unity.Services.Authentication.AuthenticationService") != null &&
                ReflectionUtility.FindType("Unity.Services.CloudSave.CloudSaveService") != null &&
                ReflectionUtility.FindType("Unity.Services.Leaderboards.LeaderboardsService") != null,
                "Unity Authentication, Cloud Save, and Leaderboards packages are installed", failures, passes);
            var gameInfoPath = Path.GetFullPath(
                "Assets/GooglePlayGames/com.google.play.games/Runtime/Scripts/GameInfo.cs");
            var gameInfo = File.Exists(gameInfoPath) ? File.ReadAllText(gameInfoPath) : string.Empty;
            Add(!string.IsNullOrEmpty(gameInfo) &&
                !gameInfo.Contains("ApplicationId = \"\"") &&
                !gameInfo.Contains("WebClientId = \"\""),
                "Google Play Games Android Setup has generated the application and Web OAuth client IDs",
                failures, passes);
            Add(Directory.GetFiles(Application.dataPath, "google-services.json", SearchOption.AllDirectories).Length > 0,
                "Firebase google-services.json exists", failures, passes);

            if (settings != null)
            {
                Add(!settings.adsEnabled || (!string.IsNullOrWhiteSpace(settings.levelPlayAppKey) &&
                                             !string.IsNullOrWhiteSpace(settings.rewardedAdUnitId) &&
                                             !string.IsNullOrWhiteSpace(settings.interstitialAdUnitId) &&
                                             !string.IsNullOrWhiteSpace(settings.bannerAdUnitId)),
                    "LevelPlay App Key and all ad-unit IDs are configured when ads are enabled", failures, passes);
                Add(!settings.unityServicesEnabled ||
                    (settings.guestSignInEnabled && settings.cloudProgressEnabled),
                    "Guest Authentication and Cloud Save are enabled", failures, passes);
                Add(!settings.unityLeaderboardsEnabled ||
                    (!string.IsNullOrWhiteSpace(settings.campaignLeaderboardId) &&
                     !string.IsNullOrWhiteSpace(settings.endlessLeaderboardId)),
                    "Unity campaign and endless leaderboard IDs are configured", failures, passes);
                if (settings.adsEnabled)
                {
                    Add(nativeFirebaseDependencies && File.ReadAllText(nativeDependenciesPath)
                            .Contains("user-messaging-platform"),
                        "Google UMP consent dependency is declared", failures, passes);
                    Add(settings.interstitialEveryCompletions > 0 &&
                        settings.interstitialMinimumSeconds >= 0f,
                        "Interstitial completion pacing and optional cooldown are configured", failures, passes);
                    Add(!settings.levelPlayAdapterDebug,
                        "LevelPlay adapter debug logging is disabled for release", failures, passes);
                    warnings.Add("Verify test mode, consent flow, child-directed treatment, and dashboard frequency caps before live ads.");
                }
            }

            warnings.Add("Add AdMob and Unity Ads as mediated networks in LevelPlay Network Manager; do not initialize them directly in game code.");
            warnings.Add("Create Android OAuth credentials using both upload and Play App Signing SHA-1 fingerprints, then publish Play Games Services.");
            warnings.Add("Register test devices/accounts and validate rewarded grants, interstitial pacing, banner safe area, login, and scores on a Play-delivered build.");

            var lines = new List<string>
            {
                "# Outside the Box - Live Ops Validation",
                string.Empty,
                $"Generated: {DateTime.UtcNow:O}",
                string.Empty,
                "## Passed"
            };
            foreach (var item in passes) lines.Add($"- [x] {item}");
            lines.Add(string.Empty);
            lines.Add("## Blocking setup");
            if (failures.Count == 0) lines.Add("- None");
            else foreach (var item in failures) lines.Add($"- [ ] {item}");
            lines.Add(string.Empty);
            lines.Add("## Dashboard/device checks");
            foreach (var item in warnings) lines.Add($"- [ ] {item}");
            File.WriteAllLines(Path.GetFullPath(LiveOpsReportPath), lines);
            AssetDatabase.Refresh();
            var summary = $"Live Ops validation: {passes.Count} passed, {failures.Count} blocking setup items.";
            if (failures.Count == 0) Debug.Log(summary);
            else Debug.LogWarning(summary + $" See {LiveOpsReportPath}");
        }

        private static void Add(bool condition, string label, ICollection<string> failures, ICollection<string> passes)
        {
            if (condition) passes.Add(label);
            else failures.Add(label);
        }
    }
}
