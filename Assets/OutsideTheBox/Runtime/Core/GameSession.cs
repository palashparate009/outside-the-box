using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace OutsideTheBox
{
    public sealed partial class GameSession : MonoBehaviour
    {
        private const int OnlineStartupTimeoutSeconds = 12;

        [SerializeField] private GameConfig config;
        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private BoardView boardView;
        [SerializeField] private MagnetInput magnetInput;
        [SerializeField] private GameUIController ui;
        [SerializeField] private FeedbackService feedback;
        [SerializeField] private MobilePerformanceController mobilePerformance;

        private readonly Stack<HistoryEntry> history = new();
        private LevelDefinition level;
        private LevelDefinition runtimeLevel;
        private Coroutine completionRoutine;
        private Coroutine navigationLoadingRoutine;
        private GameState state;
        private int levelIndex;
        private int movementBeats;
        private float energyUsed;
        private float energyTarget;
        private float nextEnergyUiUpdate;
        private float timeRemaining;
        private float timeLimit;
        private float runElapsedSeconds;
        private float nextTimerUiUpdate;
        private int revivesUsed;
        private bool timedOut;
        private bool energyDepleted;
        private Vector2 lastMagnetPosition;
        private bool endlessMode;
        private bool dailyMode;
        private int endlessIndex;
        private bool hintUsed;
        private bool inputHeld;
        private bool running;
        private bool paused;
        private bool tutorialPaused;
        private bool systemModalBlocked;
        private bool attemptRecorded;
        private bool analyticsRunEnded;
        private int analyticsCollisions;
        private bool allowAttemptRecording;
        private bool echoArmed;
        private bool startupPreloading;
        private bool startupCompleted;
        private bool signInBusy;
        public bool IsCloudSyncInProgress { get; private set; }
        private bool startupAuthenticationInProgress;
        public bool IsStartupBusy => IsCloudSyncInProgress || startupAuthenticationInProgress;
        private float activePlayStartedAt = -1f;
        private int leaderboardScope = -1;
        private int leaderboardLoadVersion;

        private struct HistoryEntry
        {
            public GameState state;
            public Vector2 ballPosition;
            public Vector2 ballVelocity;
            public int beats;
            public float energy;
            public RailChallengeSnapshot railChallenge;
        }

        public GameConfig Config => config;
        public LevelCatalog Catalog => catalog;
        public LevelDefinition CurrentLevel => level;
        public GameState State => state;
        public int LevelIndex => levelIndex;
        public int MovementBeats => movementBeats;
        public float EnergyUsed => energyUsed;
        public float EnergyTarget => energyTarget;
        public bool IsEndless => endlessMode;
        public bool IsDaily => dailyMode;
        public int EndlessIndex => endlessIndex;
        public bool IsRunning => running;
        public bool IsPaused => paused;
        public float TimeRemaining => timeRemaining;
        public float TimeLimit => timeLimit;
        public int RevivesUsed => revivesUsed;
        public bool IsTimedOut => timedOut;
        public bool IsEnergyDepleted => energyDepleted;
        public bool IsEchoArmed => echoArmed;
        public bool IsEchoRecording => boardView != null && boardView.IsEchoRecording;
        public bool HasEcho => boardView != null && boardView.HasEcho;
        public bool RequiresOrbit => boardView != null && boardView.RequiresOrbit;
        public string RailChallengeStatus => boardView != null ? boardView.RailChallengeStatus : null;

        private void Awake()
        {
            ResolveDependencies();
            // The board prefab is authored active for editing, but it must never render
            // while authentication/session restoration is still deciding the first screen.
            if (boardView != null) boardView.gameObject.SetActive(false);
            ApplyApplicationSettings();
        }

        private void ResolveDependencies()
        {
            if (config == null) config = Resources.Load<GameConfig>("OutsideTheBox/Configs/GameConfig");
            if (catalog == null) catalog = Resources.Load<LevelCatalog>("OutsideTheBox/Configs/LevelCatalog");
            if (boardView == null) boardView = FindFirstObjectByType<BoardView>();
            if (magnetInput == null) magnetInput = FindFirstObjectByType<MagnetInput>();
            if (ui == null) ui = FindFirstObjectByType<GameUIController>();
            if (feedback == null) feedback = FindFirstObjectByType<FeedbackService>();
            if (mobilePerformance == null) mobilePerformance = GetComponent<MobilePerformanceController>();
        }

        private void ApplyApplicationSettings()
        {
            Application.runInBackground = config == null ||
                                          (Application.isEditor ? config.runInBackgroundInEditor : config.runInBackgroundOnDevice);
            if (mobilePerformance == null)
                mobilePerformance = GetComponent<MobilePerformanceController>() ??
                                    gameObject.AddComponent<MobilePerformanceController>();
            var mode = (PerformanceMode)Mathf.Clamp(SaveService.Data?.performanceMode ?? 0, 0, 2);
            mobilePerformance.Apply(mode);
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        public void ApplyPlayerSettings()
        {
            ApplyApplicationSettings();
            feedback?.ApplySettings();
            boardView?.ApplyPlayerSettings();
        }

        /// <summary>
        /// Last-resort presentation recovery used by the UI startup watchdog. Online
        /// operations may continue in the background, but they may never leave a player
        /// looking at an opaque empty screen.
        /// </summary>
        public void ResolveStartupPresentationTimeout()
        {
            if (IsStartupBusy)
            {
                ui?.ShowLoading(LiveOpsConfigStore.Current.loadingStartupMessage);
                return;
            }
            if (startupCompleted)
            {
                ui?.ShowMainMenu();
                return;
            }

            var players = LiveOpsManager.Instance?.Players;
            if (players != null && players.IsSignedIn && !players.CloudProgressReady)
            {
                if (!IsCloudSyncInProgress) ui?.ShowCloudSyncError();
                return;
            }
            if (players != null && players.IsSignedIn)
            {
                EconomyService.Initialize(LiveOpsConfigStore.Current.startingCoins);
                ApplyPlayerSettings();
                try
                {
                    CompleteStartup();
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Startup level preload failed; opening the menu without preload: {exception}");
                    ui?.ShowMainMenu();
                }
                return;
            }

            ui?.ShowSignIn();
            ui?.SetSignInBusy(false, ui.Strings?.signInStatus ?? "SIGN IN TO SYNC PROGRESS ACROSS DEVICES");
        }

        private async void Start()
        {
            // Awake runs while the GameRoot prefab is being instantiated. Resolve once
            // more here so prefab children or compatibility objects created afterwards
            // are available before the session validates its dependencies.
            ResolveDependencies();
            ApplyApplicationSettings();

            var missing = new List<string>();
            if (config == null) missing.Add("GameConfig");
            if (catalog == null) missing.Add("LevelCatalog");
            else if (catalog.Count == 0) missing.Add("LevelCatalog levels");
            if (boardView == null) missing.Add("BoardView");
            if (magnetInput == null) missing.Add("MagnetInput");
            if (ui == null) missing.Add("GameUIController");
            if (missing.Count > 0)
            {
                Debug.LogError($"Outside the Box cannot start. Missing: {string.Join(", ", missing)}. " +
                               "Run Tools > Outside The Box > Build Project Content if an asset is absent.");
                enabled = false;
                return;
            }
            SaveService.Load(config, catalog.Count);
            var liveOps = LiveOpsManager.Ensure(gameObject);
            liveOps?.Initialize();
            // No ad belongs to the authentication/loading state. Individual UI screens
            // opt the banner in after the correct first screen has been revealed.
            LiveOpsManager.Instance?.Ads?.SetBannerVisible(false);
            ApplyPlayerSettings();
            ui?.Initialize(this);
            if (magnetInput != null)
            {
                magnetInput.Bind(boardView);
                magnetInput.SlotChanged += OnMagnetChanged;
                magnetInput.FirstDragged += OnFirstDragged;
                magnetInput.DragStateChanged += OnDragStateChanged;
            }
            boardView.ExitReached += OnExitReached;
            boardView.ObstacleHit += OnObstacleHit;
            boardView.RailChallengeNotice += OnRailChallengeNotice;

            var players = liveOps?.Players;
            if (players == null || !LiveOpsConfigStore.Current.unityServicesEnabled)
            {
                Debug.LogError("Unity player services are required for cloud progress. Enable Unity Services in LiveOpsConfig.");
                ui?.ShowSignInError("ONLINE SERVICES ARE NOT CONFIGURED");
                return;
            }
            try
            {
                startupAuthenticationInProgress = true;
                await ui.WaitForInternetAsync();
                if (this == null) return;
                await WithStartupTimeout(players.InitializeAsync(LiveOpsConfigStore.Current),
                    "Unity Services initialization");
                if (await WithStartupTimeout(players.TryRestoreSessionAsync(),
                        "player session restoration"))
                {
                    await LoadCloudProgressAndStartAsync();
                    return;
                }
            }
            catch (Exception exception)
            {
                AnalyticsService.Log("startup_services_failed");
                Debug.LogWarning($"Unity Authentication initialization failed: {exception.Message}");
            }
            finally { startupAuthenticationInProgress = false; }
            // Do not present the sign-in screen until session restoration has failed.
            // This prevents an authenticated player seeing the sign-in UI flash at boot.
            ui?.ShowSignIn();
            ui?.SetSignInBusy(false, ui.Strings?.signInStatus ?? "SIGN IN TO SYNC PROGRESS ACROSS DEVICES");
        }

        private void CompleteStartup()
        {
            if (startupCompleted) return;
            startupCompleted = true;
            var suspended = SaveService.Data.suspendedRun;
            startupPreloading = true;
            try
            {
                if (suspended != null && suspended.valid && suspended.daily && config.dailyChallengeEnabled)
                    StartDailyChallengeInternal(suspended.dailyKey, false, true);
                else if (suspended != null && suspended.valid && suspended.endless && config.endlessEnabled)
                    StartEndlessInternal(Mathf.Max(1, suspended.endlessIndex), false, true);
                else
                    LoadLevel(suspended != null && suspended.valid
                        ? suspended.levelIndex
                        : Mathf.Clamp(SaveService.Data.lastPlayedLevel - 1, 0, catalog.Count - 1), false,
                        suspended != null && suspended.valid);
            }
            finally
            {
                startupPreloading = false;
            }
            OpenMainMenuImmediate();
        }

        private async Task<bool> LoadCloudProgressAndStartAsync(bool resetWhenCloudSaveIsMissing = false)
        {
            var players = LiveOpsManager.Instance?.Players;
            if (players == null || !players.IsSignedIn)
            {
                if (!startupCompleted) ui?.ShowSignIn();
                return false;
            }
            var playerId = players.PlayerId;
            IsCloudSyncInProgress = true;
            try
            {
                var cloudJson = await WithStartupTimeout(players.LoadProgressAsync(), "Cloud Save download");
                if (!players.IsSignedIn || players.PlayerId != playerId) return false;
                AnalyticsService.Log("cloud_progress_loaded", "has_progress", !string.IsNullOrWhiteSpace(cloudJson) ? 1 : 0);
                if (!string.IsNullOrWhiteSpace(cloudJson))
                    SaveService.ApplyCloudJson(cloudJson, config, catalog.Count);
                else if (resetWhenCloudSaveIsMissing || !SaveService.HasLegacyLocalSave)
                    SaveService.ResetForCloudProfile(config, catalog.Count);
                players.MarkProgressLoaded(playerId);
            }
            catch (Exception exception)
            {
                AnalyticsService.Log("cloud_progress_load_failed");
                Debug.LogWarning($"Cloud progress could not be loaded: {exception.Message}");
                if (players.IsSignedIn && players.PlayerId == playerId) ui?.ShowCloudSyncError();
                return false;
            }
            finally { IsCloudSyncInProgress = false; }

            EconomyService.Initialize(LiveOpsConfigStore.Current.startingCoins);
            CosmeticIapService.Instance?.ReapplyEntitlements();
            ApplyPlayerSettings();
            CompleteStartup();
            ui?.HideLoading();
            LocalGameNotifications.RequestPermissionIfEnabled();
            try
            {
                await WithStartupTimeout(players.SaveProgressNowAsync(SaveService.ToJson()), "initial Cloud Save upload");
                SaveService.MarkCloudSynchronized();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Initial cloud progress upload was deferred: {exception.Message}");
            }
            return true;
        }

        public async void RetryCloudProgress()
        {
            if (signInBusy || IsCloudSyncInProgress) return;
            signInBusy = true;
            ui?.ShowLoading(LiveOpsConfigStore.Current.loadingStartupMessage);
            try { await LoadCloudProgressAndStartAsync(true); }
            finally { signInBusy = false; ui?.HideLoading(); }
        }

        private static async Task WithStartupTimeout(Task operation, string operationName)
        {
            var timeout = Task.Delay(TimeSpan.FromSeconds(OnlineStartupTimeoutSeconds));
            if (await Task.WhenAny(operation, timeout) != operation)
                throw new TimeoutException($"{operationName} exceeded {OnlineStartupTimeoutSeconds} seconds.");
            await operation;
        }

        private static async Task<T> WithStartupTimeout<T>(Task<T> operation, string operationName)
        {
            var timeout = Task.Delay(TimeSpan.FromSeconds(OnlineStartupTimeoutSeconds));
            if (await Task.WhenAny(operation, timeout) != operation)
                throw new TimeoutException($"{operationName} exceeded {OnlineStartupTimeoutSeconds} seconds.");
            return await operation;
        }

        private void RunLoadingOperation(string message, Action operation)
        {
            if (operation == null || navigationLoadingRoutine != null) return;
            navigationLoadingRoutine = StartCoroutine(LoadingOperation(message, operation));
        }

        private IEnumerator LoadingOperation(string message, Action operation)
        {
            ui?.ShowLoading(message);
            // Give the loading UI one complete rendered frame before performing synchronous
            // procedural generation or rebuilding the board hierarchy.
            yield return null;
            var shownAt = Time.realtimeSinceStartup;
            try
            {
                operation();
            }
            catch (Exception exception)
            {
                AnalyticsService.Log("navigation_failed");
                CancelPipePreview();
                Debug.LogError($"Navigation loading operation failed: {exception}");
                ui?.ShowToast("COULD NOT COMPLETE THAT ACTION");
            }

            // Screen-opening methods may request the banner immediately. Keep native ads
            // behind the loading curtain until the destination is ready for interaction.
            LiveOpsManager.Instance?.Ads?.SetBannerVisible(false);

            var minimum = Mathf.Max(0f, LiveOpsConfigStore.Current.loadingMinimumSeconds);
            while (Time.realtimeSinceStartup - shownAt < minimum) yield return null;
            ui?.HideLoading();
            yield return PreviewPendingPipeArrival();
            navigationLoadingRoutine = null;
        }

        private void OnDestroy()
        {
            CancelPipePreview();
            if (magnetInput != null)
            {
                magnetInput.SlotChanged -= OnMagnetChanged;
                magnetInput.FirstDragged -= OnFirstDragged;
                magnetInput.DragStateChanged -= OnDragStateChanged;
            }
            if (boardView != null)
            {
                boardView.ExitReached -= OnExitReached;
                boardView.ObstacleHit -= OnObstacleHit;
                boardView.RailChallengeNotice -= OnRailChallengeNotice;
            }
            if (runtimeLevel != null) Destroy(runtimeLevel);
        }

        private void Update()
        {
            if (!running || paused || systemModalBlocked || IsPipePreviewBusy || level == null || boardView == null) return;
            runElapsedSeconds += Time.unscaledDeltaTime;
            var live = LiveOpsConfigStore.Current;
            if (live.timersEnabled)
            {
                timeRemaining = Mathf.Max(0f, timeRemaining - Time.unscaledDeltaTime);
                if (Time.unscaledTime >= nextTimerUiUpdate)
                {
                    nextTimerUiUpdate = Time.unscaledTime + 0.1f;
                    ui?.RefreshTimer(timeRemaining, timeLimit);
                }
                if (timeRemaining <= 0f)
                {
                    HandleTimeExpired();
                    return;
                }
            }
            var echoActive = boardView.HasEcho && live.signatureMechanicsEnabled && live.echoMagnetEnabled;
            if (!inputHeld && !echoActive) return;
            var chapter = config.GetChapter(level.useNormalPhysics ? 1 : level.levelNumber);
            var currentMagnetPosition = boardView.MagnetLocalPosition;
            var distance = Vector2.Distance(currentMagnetPosition, lastMagnetPosition);
            var energyPerSecond = chapter != null ? chapter.SafeEnergyPerSecond : 8f;
            var energyPerDistance = chapter != null ? chapter.SafeEnergyPerWorldUnit : 2f;
            energyUsed += EnergyBalance.CalculateUsage(energyPerSecond, energyPerDistance, Time.unscaledDeltaTime,
                distance, inputHeld && boardView.IsRailMagnetPowered, state.mode, echoActive, boardView.EchoForceMode, live);
            lastMagnetPosition = currentMagnetPosition;
            if (HasEnergyExpired())
            {
                HandleEnergyExpired();
                return;
            }
            if (Time.unscaledTime < nextEnergyUiUpdate) return;
            nextEnergyUiUpdate = Time.unscaledTime + config.energyUiRefreshInterval;
            ui?.RefreshGameplay(level, state, energyUsed, energyTarget);
        }

        public void LoadLevel(int index) => RunLoadingOperation(
            LiveOpsConfigStore.Current.loadingLevelMessage, () => LoadLevel(index, true, false));

        private void LoadLevel(int index, bool showTutorials, bool restoreSuspended)
        {
            if (completionRoutine != null)
            {
                StopCoroutine(completionRoutine);
                completionRoutine = null;
            }
            endlessMode = false;
            dailyMode = false;
            endlessIndex = 0;
            levelIndex = Mathf.Clamp(index, 0, catalog.Count - 1);
            if (!restoreSuspended) SaveService.ClearSuspendedRun();
            if (runtimeLevel != null) Destroy(runtimeLevel);
            runtimeLevel = ProceduralLevelGenerator.Create(catalog.Get(levelIndex), config);
            level = runtimeLevel;
            SaveService.Data.lastPlayedLevel = levelIndex + 1;
            // Campaign resume progress is critical navigation state. Flush it when a
            // campaign level is opened so Play always survives an app termination.
            SaveService.Save();
            PrepareLoadedLevel(showTutorials, restoreSuspended);
        }

        public void StartEndless(int requestedIndex = 0)
        {
            RunLoadingOperation(LiveOpsConfigStore.Current.loadingLevelMessage,
                () => StartEndlessInternal(requestedIndex, true, false));
        }

        private void StartEndlessInternal(int requestedIndex, bool showTutorials, bool restoreSuspended)
        {
            if (!config.endlessEnabled || catalog.Count == 0) return;
            if (completionRoutine != null)
            {
                StopCoroutine(completionRoutine);
                completionRoutine = null;
            }
            endlessMode = true;
            dailyMode = false;
            endlessIndex = requestedIndex > 0
                ? requestedIndex
                : Mathf.Max(1, SaveService.Data?.endlessLevel ?? 1);
            if (!restoreSuspended) SaveService.ClearSuspendedRun();
            levelIndex = -1;
            if (runtimeLevel != null) Destroy(runtimeLevel);
            runtimeLevel = ProceduralLevelGenerator.CreateEndless(catalog.Get(catalog.Count - 1), config, endlessIndex);
            level = runtimeLevel;
            PrepareLoadedLevel(showTutorials, restoreSuspended);
        }

        public void StartDailyChallenge()
        {
            RunLoadingOperation(LiveOpsConfigStore.Current.loadingLevelMessage,
                () =>
                {
                    if (!config.dailyChallengeEnabled || catalog.Count == 0) return;
                    RequestMenuPipeArrival();
                    StartDailyChallengeInternal(System.DateTime.UtcNow.ToString("yyyyMMdd"), true, false);
                });
        }

        private void StartDailyChallengeInternal(string dailyKey, bool showTutorials, bool restoreSuspended)
        {
            if (!config.dailyChallengeEnabled || catalog.Count == 0) return;
            if (completionRoutine != null)
            {
                StopCoroutine(completionRoutine);
                completionRoutine = null;
            }
            endlessMode = false;
            dailyMode = true;
            endlessIndex = 0;
            levelIndex = -1;
            if (!restoreSuspended) SaveService.ClearSuspendedRun();
            if (runtimeLevel != null) Destroy(runtimeLevel);
            runtimeLevel = ProceduralLevelGenerator.CreateDaily(catalog.Get(catalog.Count - 1), config, dailyKey);
            level = runtimeLevel;
            PrepareLoadedLevel(showTutorials, restoreSuspended);
        }

        private void PrepareLoadedLevel(bool showTutorials, bool restoreSuspended)
        {
            if (level == null) return;
            CancelPipePreview(true);
            EndAnalyticsRun("level_changed");
            analyticsRunEnded = false;
            analyticsCollisions = 0;
            var startingMagnet = RailChallengeRules.StartingSlot(level);
            state = new GameState(level.start, startingMagnet, ForceMode.Pull);
            movementBeats = 0;
            energyUsed = 0f;
            var chapter = config.GetChapter(level.useNormalPhysics ? 1 : level.levelNumber);
            var live = LiveOpsConfigStore.Current;
            var configuredEnergyTarget = level.targetEnergy > 0f
                ? level.targetEnergy
                : Mathf.Max(20f, level.targetMoves * (chapter?.SafeTargetEnergyPerStep ?? 9f));
            var energyChapterIndex = level.isEndless
                ? config.ChapterCount
                : Mathf.Clamp((Mathf.Max(1, level.levelNumber) - 1) / Mathf.Max(1, config.levelsPerChapter),
                    0, Mathf.Max(0, config.ChapterCount - 1));
            energyTarget = EnergyBalance.CalculateTarget(configuredEnergyTarget, energyChapterIndex, live);
            hintUsed = false;
            var estimatedBeats = Mathf.Max(1, level.targetMoves);
            timeLimit = LiveOpsConfigStore.CalculateTimeLimit(estimatedBeats, energyChapterIndex);
            timeRemaining = timeLimit;
            runElapsedSeconds = 0f;
            revivesUsed = 0;
            timedOut = false;
            energyDepleted = false;
            echoArmed = false;
            nextTimerUiUpdate = 0f;
            attemptRecorded = false;
            allowAttemptRecording = showTutorials;
            inputHeld = false;
            running = true;
            paused = false;
            tutorialPaused = false;
            history.Clear();
            boardView.gameObject.SetActive(true);
            boardView.enabled = true;
            boardView.Build(level, config, state);
            HidePipeArrivalBall();
            var chapterIndex = level.isEndless
                ? config.ChapterCount
                : Mathf.Clamp((level.levelNumber - 1) / Mathf.Max(1, config.levelsPerChapter), 0, config.ChapterCount - 1);
            feedback?.SetChapter(chapterIndex);
            boardView.SetForceMode(state.mode);
            boardView.SetMagnetForceActive(false);
            boardView.SetSimulationPaused(systemModalBlocked || IsPipePreviewBusy);
            if (restoreSuspended) RestoreSuspendedRun();
            lastMagnetPosition = boardView.MagnetLocalPosition;
            nextEnergyUiUpdate = 0f;
            ApplyApplicationSettings();
            if (startupPreloading)
            {
                boardView.SetSimulationPaused(true);
                if (magnetInput != null) magnetInput.Enabled = false;
                return;
            }
            if (magnetInput != null)
            {
                magnetInput.ResetInteraction();
                magnetInput.Enabled = !systemModalBlocked && !IsPipePreviewBusy;
            }
            if (showTutorials && !IsPipePreviewBusy) RecordAttemptIfNeeded();
            ui?.ShowGameplay();
            ui?.RefreshGameplay(level, state, energyUsed, energyTarget);
            ui?.RefreshTimer(timeRemaining, timeLimit);
            ui?.RefreshCurrency();
            ui?.RefreshSignatureControls();
            if (showTutorials && !IsPipePreviewBusy) ui?.TryShowTutorials(level);
            StartCoroutine(EnsureGameplayInputReadyNextFrame());
        }

        private IEnumerator EnsureGameplayInputReadyNextFrame()
        {
            // Procedural/daily boards are built and their UI is transitioned in the same
            // frame. Re-assert input after EventSystem/layout state has settled so the
            // first drag works without requiring Restart.
            yield return null;
            if (!running || paused || tutorialPaused || timedOut || systemModalBlocked || IsPipePreviewBusy) yield break;
            boardView?.SetSimulationPaused(systemModalBlocked);
            if (magnetInput != null) magnetInput.Enabled = !systemModalBlocked;
        }

        private void RecordAttemptIfNeeded()
        {
            if (!allowAttemptRecording || attemptRecorded || IsPipePreviewBusy) return;
            LocalTelemetryService.RecordAttempt(levelIndex, endlessMode);
            AnalyticsService.BeginLevel(dailyMode ? 0 : endlessMode ? endlessIndex : level?.levelNumber ?? 0,
                dailyMode || endlessMode ? 0 : Mathf.Max(1, (level.levelNumber - 1) / Mathf.Max(1, config.levelsPerChapter) + 1),
                dailyMode ? "daily" : endlessMode ? "endless" : "campaign");
            AnalyticsService.Log("level_start", ("level_name", dailyMode ? "daily_" + level.dailyKey :
                endlessMode ? "endless_" + endlessIndex : "level_" + level.levelNumber),
                ("time_limit", timeLimit), ("energy_target", energyTarget),
                ("rail_challenge", level.railChallenge?.kind.ToString() ?? "None"));
            attemptRecorded = true;
            activePlayStartedAt = Time.realtimeSinceStartup;
        }

        private void TrackRun(string eventName, string reason)
        {
            AnalyticsService.Log(eventName, ("reason", reason), ("elapsed_seconds", runElapsedSeconds),
                ("time_remaining", timeRemaining), ("energy_used", energyUsed), ("energy_target", energyTarget),
                ("moves", movementBeats), ("revives", revivesUsed), ("hint_used", hintUsed),
                ("collisions", analyticsCollisions), ("success", 0));
        }

        private void EndAnalyticsRun(string reason)
        {
            if (!attemptRecorded || analyticsRunEnded) return;
            TrackRun("level_end", reason);
            analyticsRunEnded = true;
        }

        private void RecordActivePlayTime()
        {
            if (activePlayStartedAt < 0f) return;
            LocalTelemetryService.RecordPlayTime(Time.realtimeSinceStartup - activePlayStartedAt);
            activePlayStartedAt = -1f;
        }

        private void RestoreSuspendedRun()
        {
            var run = SaveService.Data?.suspendedRun;
            if (run == null || !run.valid || run.endless != endlessMode || run.daily != dailyMode) return;
            if (dailyMode && run.dailyKey != level.dailyKey) return;
            if (!endlessMode && run.levelIndex != levelIndex) return;
            if (endlessMode && run.endlessIndex != endlessIndex) return;
            if (run.designRevision != level.generatedDesignRevision)
            {
                // A revised wall layout must not restore an old position inside
                // a new obstacle. Campaign/account progress stays unchanged.
                SaveService.ClearSuspendedRun();
                return;
            }

            var side = (MagnetSide)Mathf.Clamp(run.magnetSide, 0, 3);
            var maximumIndex = side is MagnetSide.Left or MagnetSide.Right
                ? level.height - 1
                : level.width - 1;
            state.magnet = new RailSlot(side, Mathf.Clamp(run.magnetIndex, 0, maximumIndex));
            state.mode = run.forceMode == (int)ForceMode.Push && level.pushUnlocked
                ? ForceMode.Push
                : ForceMode.Pull;
            movementBeats = Mathf.Max(0, run.movementBeats);
            energyUsed = Mathf.Max(0f, run.energyUsed);
            hintUsed = run.hintUsed;
            if (run.timeRemaining > 0f) timeRemaining = Mathf.Min(timeLimit, run.timeRemaining);
            runElapsedSeconds = Mathf.Max(0f, run.elapsedSeconds);
            revivesUsed = Mathf.Clamp(run.revivesUsed, 0, LiveOpsConfigStore.Current.maxRevivesPerLevel);
            boardView.RestoreBall(new Vector2(run.ballX, run.ballY), new Vector2(run.velocityX, run.velocityY));
            state.ball = boardView.NearestCell();
            boardView.RestoreRailChallenge(run.railChallenge);
            boardView.SetMagnet(state.magnet, true);
            state = boardView.RailSolverState(state.ball, state.mode);
            boardView.SetForceMode(state.mode);
        }

        private void CaptureSuspendedRun()
        {
            if (!running || level == null || boardView == null) return;
            if (timedOut) return;
            SaveService.SaveSuspendedRun(endlessMode, dailyMode, level?.dailyKey, levelIndex, endlessIndex,
                boardView.BallPosition, boardView.BallVelocity, state.magnet, state.mode,
                movementBeats, energyUsed, hintUsed, timeRemaining, runElapsedSeconds, revivesUsed,
                boardView.CaptureRailChallenge(), level.generatedDesignRevision);
        }

        public void ToggleMode()
        {
            if (!running || systemModalBlocked || !level.pushUnlocked) return;
            state.mode = state.mode == ForceMode.Pull ? ForceMode.Push : ForceMode.Pull;
            AnalyticsService.Log("force_mode_changed", "force_mode", state.mode.ToString());
            boardView.SetForceMode(state.mode);
            boardView.NotifyPolarityChanged(state.mode);
            boardView.PlayPolarityEffect(state.mode);
            feedback?.Polarity();
            ui?.RefreshGameplay(level, state, energyUsed, energyTarget);
        }

        public void ToggleEcho()
        {
            if (!running || paused || systemModalBlocked || boardView == null || !boardView.EchoAvailable) return;
            if (boardView.IsEchoRecording)
            {
                var ready = boardView.EndEchoRecording();
                AnalyticsService.Log("echo_recording_finished", "ready", ready ? 1 : 0);
                echoArmed = false;
                ui?.ShowToast(ready ? "ECHO LOOP READY" : "MOVE THE MAGNET TO RECORD AN ECHO");
            }
            else
            {
                if (boardView.HasEcho) boardView.ClearEcho();
                echoArmed = !echoArmed;
                ui?.ShowToast(echoArmed ? "DRAG THE MAGNET TO RECORD" : "ECHO CANCELLED");
            }
            ui?.RefreshSignatureControls();
            AnalyticsService.Log("echo_toggled", "armed", echoArmed ? 1 : 0);
        }

        public void Undo()
        {
            if (!running || history.Count == 0) return;
            AnalyticsService.Log("undo_used");
            var previous = history.Pop();
            state = previous.state;
            movementBeats = previous.beats;
            energyUsed = previous.energy;
            boardView.RestoreRailChallenge(previous.railChallenge);
            inputHeld = false;
            boardView.SetHint(null);
            boardView.RestoreBall(previous.ballPosition, previous.ballVelocity);
            boardView.SetMagnet(state.magnet, true);
            boardView.SetForceMode(state.mode);
            boardView.SetMagnetForceActive(false);
            lastMagnetPosition = boardView.MagnetLocalPosition;
            ui?.RefreshGameplay(level, state, energyUsed, energyTarget);
        }

        public void Restart()
        {
            RunLoadingOperation(LiveOpsConfigStore.Current.loadingLevelMessage, RestartImmediate);
        }

        private void RestartImmediate()
        {
            if (level == null) return;
            EndAnalyticsRun("restart");
            LocalTelemetryService.RecordRestart(levelIndex, endlessMode);
            AnalyticsService.Log("level_restart");
            RecordActivePlayTime();
            if (dailyMode) StartDailyChallengeInternal(level.dailyKey, true, false);
            else if (endlessMode) StartEndlessInternal(endlessIndex, true, false);
            else LoadLevel(levelIndex, true, false);
        }

        public void RequestHint()
        {
            if (!running) return;
            state = boardView.RailSolverState(boardView.NearestCell(), state.mode);
            var result = LevelSolver.SolveFrom(level, state);
            if (!result.solved || result.steps.Count == 0) return;
            if (SaveService.Data.hintsRemaining <= 0)
            {
                ui?.ShowToast(ui.Strings.noHints);
                return;
            }
            SaveService.Data.hintsRemaining--;
            LocalTelemetryService.RecordHint(levelIndex, endlessMode);
            AnalyticsService.Log("hint_used");
            SaveService.SaveDeferred();
            hintUsed = true;
            boardView.SetHint(result.steps[0].magnet);
            ui?.ShowHint(result.steps[0], SaveService.Data.hintsRemaining);
        }

        public void SetPaused(bool value)
        {
            if (!running) return;
            if (!value && (tutorialPaused || timedOut)) return;
            if (value)
            {
                CaptureSuspendedRun();
                RecordActivePlayTime();
            }
            if (paused != value) TrackRun(value ? "level_paused" : "level_resumed", "pause_menu");
            paused = value;
            inputHeld = false;
            boardView.SetSimulationPaused(paused || systemModalBlocked || IsPipePreviewBusy);
            boardView.SetMagnetForceActive(false);
            if (magnetInput != null) magnetInput.Enabled = !paused && !systemModalBlocked && !IsPipePreviewBusy;
            ui?.SetPauseVisible(paused);
            if (!value && !systemModalBlocked && !IsPipePreviewBusy)
            {
                RecordAttemptIfNeeded();
                activePlayStartedAt = Time.realtimeSinceStartup;
            }
        }

        public void SetTutorialPaused(bool value)
        {
            if (!running) return;
            tutorialPaused = value;
            if (value) RecordActivePlayTime();
            if (paused != value) TrackRun(value ? "level_paused" : "level_resumed", "pause_menu");
            paused = value;
            inputHeld = false;
            boardView.SetSimulationPaused(value || systemModalBlocked || IsPipePreviewBusy);
            boardView.SetMagnetForceActive(false);
            if (magnetInput != null) magnetInput.Enabled = !value && !systemModalBlocked && !IsPipePreviewBusy;
            ui?.SetPauseVisible(false);
            if (!value && !systemModalBlocked && !IsPipePreviewBusy)
            {
                RecordAttemptIfNeeded();
                activePlayStartedAt = Time.realtimeSinceStartup;
            }
        }

        public void CompleteNext()
        {
            if (dailyMode)
            {
                LoadLevel(Mathf.Clamp((SaveService.Data?.lastPlayedLevel ?? 1) - 1, 0, catalog.Count - 1), false, false);
                OpenMainMenu();
                return;
            }
            if (navigationLoadingRoutine != null) return;
            RequestPipeArrival();
            if (endlessMode)
            {
                StartEndless(endlessIndex + 1);
                return;
            }
            if (levelIndex + 1 < catalog.Count) LoadLevel(levelIndex + 1);
            else StartEndless(1);
        }

        public void StartFromMainMenu()
        {
            RunLoadingOperation(LiveOpsConfigStore.Current.loadingLevelMessage, StartFromMainMenuImmediate);
        }

        private void StartFromMainMenuImmediate()
        {
            var savedCampaignIndex = Mathf.Clamp((SaveService.Data?.lastPlayedLevel ?? 1) - 1,
                0, Mathf.Max(0, catalog.Count - 1));
            if (dailyMode || endlessMode || levelIndex != savedCampaignIndex)
            {
                RequestMenuPipeArrival();
                LoadLevel(savedCampaignIndex, true, false);
                return;
            }
            if (!running)
            {
                RequestMenuPipeArrival();
                RestartImmediate();
                return;
            }
            boardView.gameObject.SetActive(true);
            if (timedOut)
            {
                ui?.ShowTimeout(timeRemaining, LiveOpsConfigStore.Current.coinReviveCost,
                    revivesUsed < LiveOpsConfigStore.Current.maxRevivesPerLevel, energyDepleted);
                return;
            }
            RequestMenuPipeArrival();
            HidePipeArrivalBall();
            allowAttemptRecording = true;
            tutorialPaused = false;
            paused = false;
            inputHeld = false;
            boardView.SetSimulationPaused(systemModalBlocked || IsPipePreviewBusy);
            boardView.SetMagnetForceActive(false);
            if (magnetInput != null) magnetInput.Enabled = !systemModalBlocked && !IsPipePreviewBusy;
            RecordAttemptIfNeeded();
            ui?.ShowGameplay();
            ui?.RefreshGameplay(level, state, energyUsed, energyTarget);
            if (!IsPipePreviewBusy)
            {
                activePlayStartedAt = Time.realtimeSinceStartup;
                ui?.TryShowTutorials(level);
            }
        }

        public void OpenMainMenu()
        {
            if (!startupCompleted || startupPreloading)
            {
                OpenMainMenuImmediate();
                return;
            }
            RunLoadingOperation(LiveOpsConfigStore.Current.loadingMenuMessage, OpenMainMenuImmediate);
        }

        private void OpenMainMenuImmediate()
        {
            CancelPipePreview();
            if (attemptRecorded && running) TrackRun("level_suspended", "navigation");
            CaptureSuspendedRun();
            RecordActivePlayTime();
            tutorialPaused = false;
            paused = true;
            inputHeld = false;
            boardView.SetSimulationPaused(true);
            boardView.SetMagnetForceActive(false);
            boardView.gameObject.SetActive(false);
            if (magnetInput != null) magnetInput.Enabled = false;
            ui?.ShowMainMenu();
        }

        public void OpenLevelSelect()
        {
            if (attemptRecorded && running) TrackRun("level_suspended", "navigation");
            CaptureSuspendedRun();
            RecordActivePlayTime();
            tutorialPaused = false;
            paused = true;
            inputHeld = false;
            boardView.SetSimulationPaused(true);
            boardView.SetMagnetForceActive(false);
            if (magnetInput != null) magnetInput.Enabled = false;
            ui?.ShowChapterSelect();
        }

        public void OpenSettings() => ui?.ShowSettings();

        public void ReviveWithCoins()
        {
            if (!CanRevive()) return;
            var cost = LiveOpsConfigStore.Current.coinReviveCost;
            if (!EconomyService.TrySpend(cost, "level_revive"))
            {
                ui?.ShowToast("NOT ENOUGH GEMS");
                return;
            }
            ApplyRevive(false);
        }

        public void ReviveWithRewardedAd()
        {
            if (!CanRevive()) return;
            var ads = LiveOpsManager.Instance?.Ads;
            var live = LiveOpsConfigStore.Current;
            var placement = energyDepleted ? live.energyRevivePlacement : live.revivePlacement;
            if (ads == null || !ads.ShowRewarded(placement, success =>
                {
                    if (success) ApplyRevive(true);
                    else ui?.ShowToast("REWARDED AD IS NOT READY");
                }))
                ui?.ShowToast("REWARDED AD IS NOT READY");
        }

        public void WatchAdForGems()
        {
            var ads = LiveOpsManager.Instance?.Ads;
            if (ads == null || !ads.ShowRewarded(LiveOpsConfigStore.Current.currencyPlacement, success =>
                {
                    if (!success)
                    {
                        ui?.ShowToast("REWARDED AD IS NOT READY");
                        return;
                    }
                    var reward = LiveOpsConfigStore.Current.rewardedCoinAmount;
                    EconomyService.Add(reward, "rewarded_ad");
                    ui?.RefreshCurrency();
                    ui?.ShowToast($"+{reward} GEMS");
                }))
                ui?.ShowToast("REWARDED AD IS NOT READY");
        }

        // Kept for existing generated prefabs and old serialized button events.
        public void WatchAdForCoins() => WatchAdForGems();

        public async void SignInGooglePlay()
        {
            if (signInBusy) return;
            var players = LiveOpsManager.Instance?.Players;
            if (players == null) return;
            signInBusy = true;
            AnalyticsService.Log("login_requested", "method", "google_play_games");
            ui?.SetSignInBusy(true, ui.Strings?.connectingGoogle ?? "CONNECTING GOOGLE PLAY GAMES...");
            try
            {
                await players.SignInGooglePlayAsync();
                if (!startupCompleted)
                {
                    if (!await LoadCloudProgressAndStartAsync(true)) return;
                }
                else await players.SaveProgressNowAsync(SaveService.ToJson());
#if UNITY_EDITOR
                ui?.ShowToast("EDITOR TEST ACCOUNT CONNECTED");
#else
                ui?.ShowToast("GOOGLE PLAY GAMES CONNECTED");
#endif
            }
            catch (Exception exception)
            {
                AnalyticsService.Log("login_failed", "method", "google_play_games");
                Debug.LogWarning($"Google Play sign-in failed: {exception.Message}");
                if (!startupCompleted) ui?.ShowSignInError("GOOGLE PLAY SIGN-IN FAILED");
                else ui?.ShowToast("GOOGLE PLAY SIGN-IN FAILED");
            }
            finally
            {
                signInBusy = false;
                ui?.SetSignInBusy(false);
            }
        }

        public async void SignOutPlayer()
        {
            if (signInBusy) return;
            signInBusy = true;
            CaptureSuspendedRun();
            var account = LiveOpsManager.Instance?.Players;
            ui?.ShowLoading("SAVING PROGRESS");
            try
            {
                if (account != null) await account.SignOutAsync();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Sign-out is waiting for unsynchronized progress: {exception.Message}");
                ui?.ShowToast("RECONNECT TO SAVE YOUR PROGRESS BEFORE SIGNING OUT");
                return;
            }
            finally
            {
                signInBusy = false;
                ui?.HideLoading();
            }
            LocalGameNotifications.RefreshPushRegistration(true);

            SaveService.ResetForCloudProfile(config, catalog.Count);
            startupCompleted = false;
            running = false;
            paused = true;
            tutorialPaused = false;
            timedOut = false;
            inputHeld = false;
            if (magnetInput != null) magnetInput.Enabled = false;
            boardView?.SetMagnetForceActive(false);
            boardView?.SetSimulationPaused(true);
            if (boardView != null) boardView.gameObject.SetActive(false);
            ui?.ShowSignIn();
            ui?.ShowToast("SIGNED OUT");
        }

        public async void DeletePlayerAccount()
        {
            if (signInBusy) return;
            var players = LiveOpsManager.Instance?.Players;
            if (players == null || !players.IsSignedIn)
            {
                ui?.ShowToast("NO SIGNED-IN ACCOUNT");
                return;
            }
            signInBusy = true;
            AnalyticsService.Log("account_delete_requested");
            ui?.SetDeleteAccountBusy(true);
            try
            {
                await players.DeleteAccountAsync();
                LocalGameNotifications.RefreshPushRegistration(true);
                SaveService.ResetForCloudProfile(config, catalog.Count);
                startupCompleted = false;
                running = false;
                paused = true;
                tutorialPaused = false;
                timedOut = false;
                inputHeld = false;
                if (magnetInput != null) magnetInput.Enabled = false;
                boardView?.SetMagnetForceActive(false);
                boardView?.SetSimulationPaused(true);
                if (boardView != null) boardView.gameObject.SetActive(false);
                ui?.ShowSignIn();
                ui?.ShowToast("ACCOUNT DELETED");
            }
            catch (Exception exception)
            {
                AnalyticsService.Log("account_delete_failed");
                Debug.LogWarning($"Unity player account deletion failed: {exception.Message}");
                ui?.ShowToast("ACCOUNT DELETION FAILED");
            }
            finally
            {
                signInBusy = false;
                ui?.SetDeleteAccountBusy(false);
            }
        }

        public async void SignInUsernamePassword(string username, string password)
        {
            if (signInBusy) return;
            var players = LiveOpsManager.Instance?.Players;
            if (players == null) return;
            signInBusy = true;
            AnalyticsService.Log("login_requested", "method", "username_password");
            ui?.SetAccountBusy(true, "SIGNING IN TO UNITY CLOUD...");
            try
            {
                await players.SignInUsernamePasswordAsync(username, password);
                if (!await LoadCloudProgressAndStartAsync(true)) return;
                ui?.ClearAccountSecrets();
                ui?.ShowToast("UNITY CLOUD ACCOUNT CONNECTED");
            }
            catch (Exception exception)
            {
                AnalyticsService.Log("login_failed", "method", "username_password");
                Debug.LogWarning($"Unity username sign-in failed: {exception.Message}");
                ui?.ShowAccountError(GetAccountError(exception, false));
            }
            finally
            {
                signInBusy = false;
                ui?.SetAccountBusy(false);
            }
        }

        public async void CreateOrLinkUsernamePassword(string username, string password, string confirmation)
        {
            if (signInBusy) return;
            if (!string.Equals(password, confirmation, StringComparison.Ordinal))
            {
                AnalyticsService.Log("sign_up_failed", "reason", "password_mismatch");
                ui?.ShowAccountError("PASSWORDS DO NOT MATCH");
                return;
            }
            var players = LiveOpsManager.Instance?.Players;
            if (players == null) return;
            signInBusy = true;
            AnalyticsService.Log("sign_up_requested", "method", "username_password");
            ui?.SetAccountBusy(true, players.IsSignedIn
                ? "SECURING THIS GUEST PROFILE..."
                : "CREATING UNITY CLOUD ACCOUNT...");
            try
            {
                await players.CreateOrLinkUsernamePasswordAsync(username, password);
                if (!startupCompleted)
                {
                    if (!await LoadCloudProgressAndStartAsync(true)) return;
                }
                else
                {
                    await players.SaveProgressNowAsync(SaveService.ToJson());
                    ui?.ShowMainMenu();
                }
                ui?.ClearAccountSecrets();
                ui?.ShowToast("ACCOUNT SECURED - PROGRESS PRESERVED");
            }
            catch (Exception exception)
            {
                AnalyticsService.Log("sign_up_failed", "method", "username_password");
                Debug.LogWarning($"Unity username account creation failed: {exception.Message}");
                ui?.ShowAccountError(GetAccountError(exception, true));
            }
            finally
            {
                signInBusy = false;
                ui?.SetAccountBusy(false);
            }
        }

        private static string GetAccountError(Exception exception, bool creating)
        {
            var message = exception?.Message ?? string.Empty;
            if (message.IndexOf("already", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("conflict", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("409", StringComparison.OrdinalIgnoreCase) >= 0)
                return "USERNAME IS ALREADY TAKEN";
            if (message.IndexOf("format", StringComparison.OrdinalIgnoreCase) >= 0 || exception is ArgumentException)
                return message.ToUpperInvariant();
            if (!creating && (message.IndexOf("invalid", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              message.IndexOf("unauthorized", StringComparison.OrdinalIgnoreCase) >= 0))
                return "USERNAME OR PASSWORD IS INCORRECT";
            return creating ? "ACCOUNT COULD NOT BE CREATED. CHECK YOUR CONNECTION."
                : "SIGN-IN FAILED. CHECK YOUR DETAILS AND CONNECTION.";
        }

        public async void ShowLeaderboard()
        {
            leaderboardScope = -1;
            await LoadLeaderboardScopeAsync();
        }

        public async void SelectLeaderboardScope(int chapterIndex)
        {
            leaderboardScope = Mathf.Clamp(chapterIndex, -1, Mathf.Max(-1, config.ChapterCount - 1));
            await LoadLeaderboardScopeAsync();
        }

        private async Task LoadLeaderboardScopeAsync()
        {
            var players = LiveOpsManager.Instance?.Players;
            if (players == null || !players.IsSignedIn)
            {
                ui?.ShowToast("SIGN IN TO VIEW THE LEADERBOARD");
                return;
            }
            AnalyticsService.Log("leaderboard_requested", "scope_chapter", leaderboardScope + 1);
            var requestedScope = leaderboardScope;
            var requestVersion = ++leaderboardLoadVersion;
            ui?.ShowLeaderboardLoading(requestedScope);
            try
            {
                var summary = await players.GetPerformanceLeaderboardSummaryAsync(requestedScope);
                AnalyticsService.Log("leaderboard_loaded", ("scope_chapter", requestedScope + 1), ("has_scores", !string.IsNullOrWhiteSpace(summary)));
                if (requestVersion != leaderboardLoadVersion || requestedScope != leaderboardScope) return;
                ui?.ShowLeaderboardResults(requestedScope,
                    string.IsNullOrWhiteSpace(summary) ? "NO SCORES YET" : summary);
            }
            catch (Exception exception)
            {
                if (requestVersion != leaderboardLoadVersion || requestedScope != leaderboardScope) return;
                AnalyticsService.Log("leaderboard_failed", "scope_chapter", requestedScope + 1);
                Debug.LogWarning($"Leaderboard load failed: {exception.Message}");
                ui?.ShowLeaderboardResults(requestedScope,
                    "LEADERBOARD IS NOT CONFIGURED OR IS CURRENTLY UNAVAILABLE");
            }
        }

        private bool CanRevive()
        {
            if (!timedOut || !running) return false;
            if (revivesUsed < LiveOpsConfigStore.Current.maxRevivesPerLevel) return true;
            ui?.ShowToast("NO REVIVES LEFT");
            return false;
        }

        private void ApplyRevive(bool rewardedAd)
        {
            if (!CanRevive()) return;
            var wasEnergyDepleted = energyDepleted;
            revivesUsed++;
            timedOut = false;
            energyDepleted = false;
            paused = false;
            var live = LiveOpsConfigStore.Current;
            if (wasEnergyDepleted)
            {
                var energyLimit = energyTarget * live.energyFailureMultiplier;
                energyUsed = Mathf.Max(0f, energyUsed - energyLimit * live.energyRevivePercent);
            }
            else
            {
                timeRemaining = Mathf.Min(timeLimit, Mathf.Max(1f, live.reviveSeconds));
            }
            inputHeld = false;
            boardView.SetSimulationPaused(systemModalBlocked);
            boardView.SetMagnetForceActive(false);
            if (magnetInput != null) magnetInput.Enabled = !systemModalBlocked;
            if (rewardedAd && SaveService.Data != null) SaveService.Data.rewardedRevives++;
            SaveService.SaveDeferred();
            AnalyticsService.Log("level_revived", ("method", rewardedAd ? "rewarded_ad" : "gems"),
                ("resource", wasEnergyDepleted ? "energy" : "time"), ("revives", revivesUsed),
                ("time_remaining", timeRemaining), ("energy_used", energyUsed));
            ui?.ShowGameplayAfterRevive();
            ui?.RefreshGameplay(level, state, energyUsed, energyTarget);
            ui?.RefreshTimer(timeRemaining, timeLimit);
            ui?.RefreshCurrency();
            activePlayStartedAt = Time.realtimeSinceStartup;
        }

        private void HandleTimeExpired()
        {
            if (timedOut || !running) return;
            timedOut = true;
            energyDepleted = false;
            paused = true;
            inputHeld = false;
            RecordActivePlayTime();
            boardView.SetSimulationPaused(true);
            boardView.SetMagnetForceActive(false);
            if (magnetInput != null) magnetInput.Enabled = false;
            if (SaveService.Data != null) SaveService.Data.timedOutRuns++;
            SaveService.ClearSuspendedRun();
            SaveService.SaveDeferred();
            TrackRun("level_timeout", "time");
            ui?.ShowTimeout(0f, LiveOpsConfigStore.Current.coinReviveCost,
                revivesUsed < LiveOpsConfigStore.Current.maxRevivesPerLevel, false);
        }

        private bool HasEnergyExpired()
        {
            var live = LiveOpsConfigStore.Current;
            return live.energyFailureEnabled && energyUsed >= energyTarget * live.energyFailureMultiplier;
        }

        private void HandleEnergyExpired()
        {
            if (timedOut || !running) return;
            timedOut = true;
            energyDepleted = true;
            paused = true;
            inputHeld = false;
            RecordActivePlayTime();
            boardView.SetSimulationPaused(true);
            boardView.SetMagnetForceActive(false);
            if (magnetInput != null) magnetInput.Enabled = false;
            SaveService.ClearSuspendedRun();
            SaveService.SaveDeferred();
            TrackRun("level_energy_depleted", "energy");
            ui?.ShowTimeout(timeRemaining, LiveOpsConfigStore.Current.coinReviveCost,
                revivesUsed < LiveOpsConfigStore.Current.maxRevivesPerLevel, true);
        }

        private void OnMagnetChanged(RailSlot slot)
        {
            if (!running || paused || systemModalBlocked) return;
            state = boardView.RailSolverState(state.ball, state.mode);
        }

        private void OnRailChallengeNotice(string message)
        {
            ui?.ShowToast(message);
            AnalyticsService.Log("rail_challenge_notice", ("kind", level.railChallenge.kind.ToString()),
                ("level_number", level.levelNumber), ("message", message));
        }

        private void OnFirstDragged()
        {
            if (SaveService.Data == null) return;
            if (!SaveService.Data.onboardingMovedMagnet)
            {
                AnalyticsService.Log("tutorial_first_drag");
                SaveService.Data.onboardingMovedMagnet = true;
                SaveService.SaveDeferred();
                ui?.HideDragGuide();
            }
            if (level?.levelNumber == 1 && LiveOpsConfigStore.Current.ftueEnabled &&
                !SaveService.Data.ftueLevelOneInteractionShown)
            {
                SaveService.Data.ftueLevelOneInteractionShown = true;
                SaveService.SaveDeferred();
                ui?.ShowToast("GREAT! HOLD TO PULL • RELEASE TO LET THE BALL COAST");
            }
        }

        private void OnDragStateChanged(bool held)
        {
            if (!running || paused || systemModalBlocked) return;
            if (held && echoArmed)
            {
                echoArmed = false;
                AnalyticsService.Log("echo_recording_started");
                boardView.BeginEchoRecording();
                ui?.RefreshSignatureControls();
            }
            else if (!held && boardView.IsEchoRecording)
            {
                var ready = boardView.EndEchoRecording();
                AnalyticsService.Log("echo_recording_finished", "ready", ready ? 1 : 0);
                ui?.ShowToast(ready ? "ECHO RECORDED • DRAG AGAIN TO COMBINE FIELDS" : "ECHO WAS TOO SHORT");
                ui?.RefreshSignatureControls();
            }
            inputHeld = held;
            boardView.SetMagnetForceActive(held);
            if (!held) return;
            state = boardView.RailSolverState(boardView.NearestCell(), state.mode);
            history.Push(new HistoryEntry
            {
                state = state,
                ballPosition = boardView.BallPosition,
                ballVelocity = boardView.BallVelocity,
                beats = movementBeats,
                energy = energyUsed,
                railChallenge = boardView.CaptureRailChallenge()
            });
            movementBeats++;
            lastMagnetPosition = boardView.MagnetLocalPosition;
            feedback?.Move();
            ui?.RefreshGameplay(level, state, energyUsed, energyTarget);
        }

        private void OnObstacleHit()
        {
            if (!running || paused || systemModalBlocked) return;
            energyUsed += config.collisionEnergyPenalty;
            if (HasEnergyExpired())
            {
                HandleEnergyExpired();
                return;
            }
            LocalTelemetryService.RecordCollision(levelIndex, endlessMode);
            analyticsCollisions++;
            // Wall contact may repeat every physics tick: send a bounded set of milestones.
            if (analyticsCollisions <= 3 || analyticsCollisions is 10 or 25 or 50 or 100)
                AnalyticsService.Log("obstacle_hit", "collision_count", analyticsCollisions);
            boardView.PlayCollisionEffect();
            feedback?.Blocked();
            if (level?.levelNumber == 2 && LiveOpsConfigStore.Current.ftueEnabled && SaveService.Data != null &&
                !SaveService.Data.ftueLevelTwoWallShown)
            {
                SaveService.Data.ftueLevelTwoWallShown = true;
                SaveService.SaveDeferred();
                ui?.ShowToast("BLOCKED • MOVE THE MAGNET TO ANOTHER SIDE AND PULL AROUND THE WALL");
            }
            ui?.RefreshGameplay(level, state, energyUsed, energyTarget);
        }

        private void OnExitReached()
        {
            if (!running) return;
            completionRoutine = StartCoroutine(CompleteAfterAnimation());
        }

        /// <summary>Independent of pause/FTUE so dismissing a system dialog cannot resume either.</summary>
        public void SetSystemModalBlocked(bool blocked)
        {
            if (systemModalBlocked == blocked) return;
            systemModalBlocked = blocked;
            if (blocked)
            {
                CaptureSuspendedRun();
                RecordActivePlayTime();
                inputHeld = false;
                boardView?.SetMagnetForceActive(false);
            }
            var canPlay = running && !paused && !tutorialPaused && !timedOut && !blocked && !IsPipePreviewBusy;
            boardView?.SetSimulationPaused(!canPlay);
            if (magnetInput != null) magnetInput.Enabled = canPlay;
            if (canPlay)
            {
                lastMagnetPosition = boardView.MagnetLocalPosition;
                if (attemptRecorded) activePlayStartedAt = Time.realtimeSinceStartup;
            }
        }

        public async Task SaveBeforeQuitAsync()
        {
            CaptureSuspendedRun();
            RecordActivePlayTime();
            SaveService.Save();
            var players = LiveOpsManager.Instance?.Players;
            if (players?.CloudProgressReady != true ||
                Application.internetReachability == NetworkReachability.NotReachable) return;
            // Bound exit latency; the existing write queue keeps the most recent pending snapshot.
            var save = players.SaveProgressNowAsync(SaveService.ToJson());
            if (await Task.WhenAny(save, Task.Delay(3000)) == save) await save;
        }

        private IEnumerator CompleteAfterAnimation()
        {
            // A pocket drop can finish while the offline/quit dialog is open. Delay its
            // result rather than losing the one-shot ExitReached event and trapping the level.
            while (systemModalBlocked) yield return null;
            if (!running) yield break;
            running = false;
            RecordActivePlayTime();
            inputHeld = false;
            boardView.SetMagnetForceActive(false);
            if (magnetInput != null) magnetInput.Enabled = false;
            feedback?.Win();
            boardView.PlayWinEffect();
            yield return new WaitForSecondsRealtime(config.winDelaySeconds);
            var performance = CalculatePerformanceScore();
            var newChapterGifts = 0;
            var stars = PerformanceBalance.CalculateStars(performance.rating, hintUsed, LiveOpsConfigStore.Current);
            var currencyReward = 0;
            if (dailyMode)
            {
                var yesterday = System.DateTime.UtcNow.AddDays(-1).ToString("yyyyMMdd");
                var firstToday = SaveService.Data?.lastDailyCompleted != level.dailyKey;
                SaveService.CompleteDaily(level.dailyKey, yesterday, energyUsed);
                if (firstToday) currencyReward = LiveOpsConfigStore.Current.dailyCompletionCoins;
            }
            else if (endlessMode)
            {
                SaveService.CompleteEndless(endlessIndex, energyUsed);
                currencyReward = LiveOpsConfigStore.Current.endlessCompletionCoins;
            }
            else
            {
                var previousStars = levelIndex >= 0 && levelIndex < (SaveService.Data?.bestStars?.Count ?? 0)
                    ? SaveService.Data.bestStars[levelIndex]
                    : 0;
                SaveService.CompleteLevel(levelIndex, stars, energyUsed, runElapsedSeconds,
                    performance.score, performance.rating, catalog.Count);
                newChapterGifts = ChapterGiftService.EnsureRewards(config);
                currencyReward = (previousStars == 0 ? LiveOpsConfigStore.Current.campaignCompletionCoins : 0) +
                                 Mathf.Max(0, stars - previousStars) * LiveOpsConfigStore.Current.coinsPerNewStar;
            }
            EconomyService.Add(currencyReward, dailyMode ? "daily_complete" : endlessMode ? "endless_complete" : "level_complete");
            LocalTelemetryService.RecordCompletion(levelIndex, endlessMode);
            SaveService.ClearSuspendedRun();
            analyticsRunEnded = true;
            AnalyticsService.Log("level_end", ("success", 1), ("reason", "completed"),
                ("stars", stars), ("performance", performance.rating), ("score", performance.score),
                ("elapsed_seconds", runElapsedSeconds), ("time_limit", timeLimit),
                ("energy_used", energyUsed), ("energy_target", energyTarget),
                ("revives", revivesUsed), ("hint_used", hintUsed), ("moves", movementBeats),
                ("collisions", analyticsCollisions), ("gems_awarded", currencyReward));
            yield return PreviewPipeDeparture();
            ui?.ShowWin(stars, energyUsed, energyTarget, runElapsedSeconds, performance.rating,
                endlessMode, endlessIndex, currencyReward);
            if (!dailyMode && !endlessMode)
            {
                var levelsPerChapter = Mathf.Max(1, config.levelsPerChapter);
                var chapterIndex = Mathf.Clamp(levelIndex / levelsPerChapter, 0, config.ChapterCount - 1);
                var chapterStart = chapterIndex * levelsPerChapter;
                var chapterLevelCount = Mathf.Min(levelsPerChapter, catalog.Count - chapterStart);
                var chapterPerformance = SaveService.GetPerformanceAggregate(chapterStart, chapterLevelCount);
                var allTimePerformance = SaveService.GetPerformanceAggregate(0, catalog.Count);
                _ = LiveOpsManager.Instance?.Players?.ReportPerformanceScoresAsync(chapterIndex,
                    chapterPerformance, allTimePerformance);
            }
            if (endlessMode)
                _ = LiveOpsManager.Instance?.Players?.ReportEndlessScoreAsync(SaveService.Data.bestEndlessLevel);
            LiveOpsManager.Instance?.Ads?.RecordCompletionAndMaybeShowInterstitial();
            var achievements = EvaluateAchievements(stars);
            if (achievements.Count > 0)
                ui?.ShowToast($"ACHIEVEMENT UNLOCKED  •  {string.Join("  •  ", achievements)}");
            ui?.ShowChapterGiftNotice(newChapterGifts > 0);
            boardView.gameObject.SetActive(false);
            completionRoutine = null;
        }

        private (int score, float rating) CalculatePerformanceScore()
        {
            var live = LiveOpsConfigStore.Current;
            var rating = PerformanceBalance.CalculateRating(runElapsedSeconds, timeLimit, energyUsed,
                energyTarget, hintUsed, revivesUsed, live);
            var score = live.leaderboardCompletionPoints +
                        Mathf.RoundToInt(live.leaderboardPerformanceBonusPoints * rating);
            return (score, rating);
        }

        private List<string> EvaluateAchievements(int stars)
        {
            var unlocked = new List<string>();
            TryUnlock("first_pocket", "FIRST POCKET", unlocked);
            if (stars == 3) TryUnlock("perfect_energy", "PERFORMANCE MASTER", unlocked);
            if (dailyMode) TryUnlock("daily_solver", "DAILY SOLVER", unlocked);
            if (endlessMode && endlessIndex >= 10) TryUnlock("endless_10", "VAULT EXPLORER", unlocked);
            if (!dailyMode && !endlessMode && (levelIndex + 1) % Mathf.Max(1, config.levelsPerChapter) == 0)
                TryUnlock($"chapter_{(levelIndex + 1) / config.levelsPerChapter}", "CHAPTER MASTER", unlocked);
            if (SaveService.TotalStars >= 90) TryUnlock("star_90", "STAR COLLECTOR", unlocked);
            return unlocked;
        }

        private static void TryUnlock(string id, string label, ICollection<string> unlocked)
        {
            if (SaveService.UnlockAchievement(id))
            {
                unlocked.Add(label);
                AnalyticsService.Log("unlock_achievement", "achievement_id", id);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            AnalyticsService.Log(pauseStatus ? "app_background" : "app_foreground");
            if (attemptRecorded && running) TrackRun(pauseStatus ? "run_background" : "run_foreground", "application");
            if (!pauseStatus)
            {
                LocalGameNotifications.Cancel();
                LocalGameNotifications.RefreshPushRegistration();
                if (running && !paused && !systemModalBlocked && !IsPipePreviewBusy && attemptRecorded)
                    activePlayStartedAt = Time.realtimeSinceStartup;
                return;
            }
            CaptureSuspendedRun();
            RecordActivePlayTime();
            SaveService.Save();
            LocalGameNotifications.Schedule();
        }

        private void OnApplicationQuit()
        {
            AnalyticsService.Log("app_exit");
            LocalGameNotifications.Schedule();
            CaptureSuspendedRun();
            RecordActivePlayTime();
            SaveService.Save();
        }
    }
}
