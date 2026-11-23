using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

namespace OutsideTheBox
{
    public sealed partial class GameUIController : MonoBehaviour
    {
        [SerializeField] private GameStrings strings;
        private GameSession session;
        private Canvas canvas;
        private RectTransform safeRoot;
        private GameObject signInPanel;
        private GameObject accountPanel;
        private GameObject leaderboardPanel;
        private GameObject mainMenuPanel;
        private GameObject gameplayPanel;
        private GameObject chapterSelectPanel;
        private GameObject levelSelectPanel;
        private GameObject settingsPanel;
        private GameObject winPanel;
        private GameObject pausePanel;
        private GameObject timeoutPanel;
        private TMP_Text levelText;
        private TMP_Text energyText;
        private TMP_Text timerText;
        private TMP_Text tutorialText;
        private TMP_Text modeText;
        private TMP_Text hintText;
        private TMP_Text toastText;
        private TMP_Text winTitle;
        private TMP_Text winStats;
        private TMP_Text winEyebrow;
        private TMP_Text winTimeValue;
        private TMP_Text winEnergyValue;
        private TMP_Text winRewardValue;
        private readonly Image[] winStars = new Image[3];
        private static Sprite victoryStarSprite;
        private Coroutine winStarsRoutine;
        private Coroutine timerPulseRoutine;
        private Coroutine energyPulseRoutine;
        private float lastTimerElapsed = -1f;
        private float lastTimerRemaining = -1f;
        private int displayedTimerSeconds = int.MinValue;
        private string displayedTimeLabel;
        private int displayedCurrencyBalance = int.MinValue;
        private string displayedCurrencyLabel;
        private string currencyDisplay;
        private bool energyWarningShown;
        private TMP_Text levelSelectTitle;
        private TMP_Text signInStatusText;
        private Button googleSignInButton;
        private Button guestSignInButton;
        private Button accountSignInButton;
        private Button accountCreateButton;
        private Button accountGoogleButton;
        private Button accountLogoutButton;
        private Button deleteAccountButton;
        private TMP_InputField accountUsernameInput;
        private TMP_InputField accountPasswordInput;
        private TMP_InputField accountConfirmInput;
        private TMP_Text accountStatusText;
        private TMP_Text leaderboardScopeText;
        private TMP_Text leaderboardBodyText;
        private Button leaderboardScopeDropdownButton;
        private GameObject leaderboardScopeDropdownMenu;
        private Button modeButton;
        private Button echoButton;
        private TMP_Text echoText;
        private TMP_Text signatureStatusText;
        private GameObject dragGuide;
        private GameObject tutorialOverlay;
        private GameObject tooltipPanel;
        private TMP_Text tutorialTitleText;
        private TMP_Text tutorialBodyText;
        private TMP_Text tutorialProgressText;
        private TMP_Text tooltipText;
        private Button tutorialContinueButton;
        private float toastUntil;
        private int tutorialStepIndex;
        private readonly List<TutorialStepDefinition> tutorialQueue = new();
        private int selectedChapterIndex;
        private readonly List<GameObject> screens = new();
        private static Sprite roundedPanelSprite;
        private static Sprite victoryIllustrationSprite;
        private RuntimePrefabRegistry prefabs;
        private bool subMenuReturnsToMainMenu;
        private readonly List<TMP_Text> currencyTexts = new();
        private GameObject screenTransitionLayer;
        private CanvasGroup screenTransitionGroup;
        private GameObject loadingOverlay;
        private GameObject cloudSyncPanel;
        private CanvasGroup loadingOverlayGroup;
        private TMP_Text loadingStatusText;
        private string loadingStatusBase;
        private Coroutine loadingPulseRoutine;
        private Coroutine loadingHideRoutine;
        private GameObject fullScreenBackdrop;
        private Coroutine screenTransitionRoutine;
        private Coroutine pauseTransitionRoutine;
        private Coroutine tutorialTransitionRoutine;
        private Coroutine deferredTutorialRoutine;
        private Coroutine startupPresentationWatchdog;
        private bool hasPresentedScreen;
        private bool startupPresentationReleased;
        private bool accountReturnsToSignIn;
        private bool deleteAccountArmed;
        private bool uiHierarchyBuilt;
        private bool incompletePrefabWarningLogged;
        private static readonly Color StartupCoverColor = new(0.015f, 0.055f, 0.09f, 1f);

        public GameStrings Strings => strings;

        private void Awake()
        {
            // The authored HUD prefab contains active panels so it remains editable in
            // Prefab Mode. Hide its Canvas before Unity can render a frame; authentication
            // restoration decides which screen is actually allowed to become visible.
            PrepareStartupPresentation();
        }

        public void Initialize(GameSession gameSession)
        {
            hasPresentedScreen = false;
            startupPresentationReleased = false;
            session = gameSession;
            prefabs = RuntimePrefabRegistry.Current;
            strings = LocalizationService.LoadSelected(session.Config);
            BuildUI();
            PrepareStartupPresentation();
            EnsureScreenTransitionLayer();
            EnsureBackNavigation();
            EnsureSignatureControls();
            ApplyPremiumTheme();
            ApplyUiMotion();
            ApplyUiScale();
            EnsureStoreUI();
            EnsureLoadingOverlay();
            // Authored screen prefabs may be active while the HUD prefab is instantiated.
            // Normalize them before the first rendered frame so timeout/revive UI cannot flash.
            HideScreens();
            if (fullScreenBackdrop != null) fullScreenBackdrop.SetActive(false);
            ShowLoading(LiveOpsConfigStore.Current.loadingStartupMessage);
            if (startupPresentationWatchdog != null) StopCoroutine(startupPresentationWatchdog);
            startupPresentationWatchdog = StartCoroutine(StartupPresentationWatchdog());
            InitializeSystemDialogs();
        }

        private IEnumerator StartupPresentationWatchdog()
        {
            yield return new WaitForSecondsRealtime(10f);
            startupPresentationWatchdog = null;
            if (startupPresentationReleased) yield break;

            AnalyticsService.Log("startup_presentation_timeout");
            Debug.LogWarning("Startup presentation timed out. Releasing the UI through the safe authentication fallback.");
            session?.ResolveStartupPresentationTimeout();
            if (startupPresentationReleased || session.IsStartupBusy) yield break;

            // Even a secondary startup exception must not strand the player behind the
            // cover. Sign-in is the safest screen because it does not expose gameplay
            // before an identity has been established.
            hasPresentedScreen = false;
            ShowSignIn();
        }

        private void PrepareStartupPresentation()
        {
            if (!Application.isPlaying || startupPresentationReleased) return;
            if (canvas == null) canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null) return;

            // Keep the Canvas enabled and cover the world instead of disabling all UI.
            // Disabling it exposed the camera/BoardView while cloud authentication waited.
            canvas.enabled = true;
            var existing = canvas.transform.Find("Screen Transition");
            if (existing == null) return; // EnsureScreenTransitionLayer creates it during Initialize.
            screenTransitionLayer = existing.gameObject;
            var image = screenTransitionLayer.GetComponent<Image>();
            if (image != null)
            {
                image.color = StartupCoverColor;
                image.raycastTarget = true;
            }
            screenTransitionGroup = screenTransitionLayer.GetComponent<CanvasGroup>();
            if (screenTransitionGroup == null)
                screenTransitionGroup = screenTransitionLayer.AddComponent<CanvasGroup>();
            screenTransitionGroup.alpha = 1f;
            screenTransitionGroup.interactable = true;
            screenTransitionGroup.blocksRaycasts = true;
            screenTransitionLayer.SetActive(true);
            screenTransitionLayer.transform.SetAsLastSibling();
        }

        private void ReleaseStartupPresentation()
        {
            if (startupPresentationReleased) return;
            startupPresentationReleased = true;
            if (startupPresentationWatchdog != null)
            {
                StopCoroutine(startupPresentationWatchdog);
                startupPresentationWatchdog = null;
            }
            if (canvas != null) canvas.enabled = true;
            if (screenTransitionGroup != null)
            {
                screenTransitionGroup.alpha = 0f;
                screenTransitionGroup.interactable = false;
                screenTransitionGroup.blocksRaycasts = false;
            }
            if (screenTransitionLayer != null)
            {
                var image = screenTransitionLayer.GetComponent<Image>();
                if (image != null) image.color = Color.clear;
                screenTransitionLayer.SetActive(false);
            }
            HideLoadingImmediately();
        }

        private void Update()
        {
            SyncBackdropVisibility();
            RefreshRailChallengeStatus();
            if (tooltipPanel != null && tooltipPanel.activeSelf && Time.unscaledTime >= toastUntil)
                tooltipPanel.SetActive(false);
            if (session == null || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (HandleSystemDialogBack()) return;
            if (session.IsPipePreviewBusy) return;
            if (tutorialOverlay != null && tutorialOverlay.activeSelf)
            {
                ContinueTutorial();
                return;
            }
            if (leaderboardScopeDropdownMenu != null && leaderboardScopeDropdownMenu.activeSelf)
            {
                leaderboardScopeDropdownMenu.SetActive(false);
                return;
            }
            if (levelSelectPanel != null && levelSelectPanel.activeSelf)
                ShowChapterSelect();
            else if (accountPanel != null && accountPanel.activeSelf)
                ReturnFromAccount();
            else if (leaderboardPanel != null && leaderboardPanel.activeSelf)
                ShowMainMenu();
            else if (storePanel != null && storePanel.activeSelf)
                ShowMainMenu();
            else if ((chapterSelectPanel != null && chapterSelectPanel.activeSelf) ||
                     (settingsPanel != null && settingsPanel.activeSelf))
                ReturnFromSubMenu();
            else if ((winPanel != null && winPanel.activeSelf) ||
                     (timeoutPanel != null && timeoutPanel.activeSelf))
                session.OpenMainMenu();
            else if (pausePanel != null && pausePanel.activeSelf)
                session.SetPaused(false);
            else if (mainMenuPanel != null && mainMenuPanel.activeSelf)
                ExitGame();
            else if (signInPanel != null && signInPanel.activeSelf)
                ExitGame();
            else
                session.SetPaused(true);
        }

        public void ShowMainMenu()
        {
            LocalGameNotifications.Cancel();
            LocalGameNotifications.RefreshPushRegistration();
            ChapterGiftService.EnsureRewards(session.Config);
            EnsureMainMenuStoreButton();
            subMenuReturnsToMainMenu = true;
            TransitionToScreen(mainMenuPanel);
            RefreshResumeButton();
            RefreshDailyButton();
            RefreshCurrency();
            RefreshAccountButton();
            LiveOpsManager.Instance?.Ads?.SetBannerVisible(true);
        }

        public void ShowSignIn()
        {
            LocalGameNotifications.Cancel();
            TransitionToScreen(signInPanel);
            var live = LiveOpsConfigStore.Current;
            if (googleSignInButton != null) googleSignInButton.gameObject.SetActive(live.googlePlayGamesEnabled);
            // Username/password belongs to the Unity Cloud route. Keep the entry screen to
            // exactly two account ecosystems: Google Play Games or Unity Cloud/guest.
            var legacyUsernameButton = FindObject(safeRoot, "Sign In Screen/Username Sign In");
            if (legacyUsernameButton != null) legacyUsernameButton.SetActive(false);
            if (guestSignInButton != null)
                guestSignInButton.gameObject.SetActive(live.guestSignInEnabled || live.usernamePasswordEnabled);
            SetSignInBusy(false, strings.signInStatus);
            LiveOpsManager.Instance?.Ads?.SetBannerVisible(false);
        }

        public void ShowCloudSyncError()
        {
            if (cloudSyncPanel == null)
            {
                var view = SpawnServiceScreen(prefabs.cloudSyncPanelPrefab, "Cloud Sync", safeRoot);
                cloudSyncPanel = view.gameObject;
                screens.Add(cloudSyncPanel);
                BindServiceButton(view.primaryButton, () => session.RetryCloudProgress());
                BindServiceButton(view.secondaryButton, () => session.SignOutPlayer());
                BindServiceButton(view.backButton, () => session.SignOutPlayer());
            }
            TransitionToScreen(cloudSyncPanel);
            HideLoading();
            LiveOpsManager.Instance?.Ads?.SetBannerVisible(false);
        }

        public void ShowSignInError(string message)
        {
            if (signInPanel != null && !signInPanel.activeSelf) TransitionToScreen(signInPanel);
            SetSignInBusy(false, message);
        }

        public void SetSignInBusy(bool busy, string message = null)
        {
            if (googleSignInButton != null) googleSignInButton.interactable = !busy;
            if (guestSignInButton != null) guestSignInButton.interactable = !busy;
            if (signInStatusText != null && !string.IsNullOrWhiteSpace(message)) signInStatusText.text = message;
        }

        public void OpenUsernameFromSignIn()
        {
            accountReturnsToSignIn = true;
            RefreshAccountScreen();
            TransitionToScreen(accountPanel);
            LiveOpsManager.Instance?.Ads?.SetBannerVisible(false);
        }

        private void OpenAccountFromMainMenu()
        {
            accountReturnsToSignIn = false;
            RefreshAccountScreen();
            TransitionToScreen(accountPanel);
            LiveOpsManager.Instance?.Ads?.SetBannerVisible(true);
        }

        private void ReturnFromAccount()
        {
            ClearAccountSecrets();
            if (accountReturnsToSignIn) ShowSignIn();
            else ShowMainMenu();
        }

        private void SubmitUsernameSignIn()
        {
            session?.SignInUsernamePassword(accountUsernameInput?.text, accountPasswordInput?.text);
        }

        private void SubmitUsernameCreate()
        {
            session?.CreateOrLinkUsernamePassword(accountUsernameInput?.text, accountPasswordInput?.text,
                accountConfirmInput?.text);
        }

        public void SetAccountBusy(bool busy, string message = null)
        {
            if (accountSignInButton != null) accountSignInButton.interactable = !busy;
            if (accountCreateButton != null) accountCreateButton.interactable = !busy;
            if (accountUsernameInput != null) accountUsernameInput.interactable = !busy;
            if (accountPasswordInput != null) accountPasswordInput.interactable = !busy;
            if (accountConfirmInput != null) accountConfirmInput.interactable = !busy;
            if (accountStatusText != null && !string.IsNullOrWhiteSpace(message)) accountStatusText.text = message;
        }

        public void ShowAccountError(string message)
        {
            if (accountPanel != null && !accountPanel.activeSelf) TransitionToScreen(accountPanel);
            SetAccountBusy(false, message);
        }

        public void ClearAccountSecrets()
        {
            if (accountPasswordInput != null) accountPasswordInput.text = string.Empty;
            if (accountConfirmInput != null) accountConfirmInput.text = string.Empty;
        }

        private void RefreshAccountScreen()
        {
            var players = LiveOpsManager.Instance?.Players;
            var alreadySecured = players?.HasUsernamePassword ?? false;
            if (accountUsernameInput != null)
            {
                accountUsernameInput.text = alreadySecured ? players.Username : string.Empty;
                accountUsernameInput.interactable = !alreadySecured;
            }
            ClearAccountSecrets();
            if (accountSignInButton != null) accountSignInButton.gameObject.SetActive(accountReturnsToSignIn);
            if (accountCreateButton != null)
            {
                accountCreateButton.gameObject.SetActive(!alreadySecured);
                var label = accountCreateButton.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = players != null && players.IsSignedIn
                    ? strings.secureAccount : strings.createAccount;
            }
            if (accountConfirmInput != null) accountConfirmInput.gameObject.SetActive(!alreadySecured);
            if (accountPasswordInput != null) accountPasswordInput.gameObject.SetActive(!alreadySecured || accountReturnsToSignIn);
            if (accountGoogleButton != null)
                accountGoogleButton.gameObject.SetActive(!accountReturnsToSignIn &&
                                                         LiveOpsConfigStore.Current.googlePlayGamesEnabled);
            if (accountLogoutButton != null)
                accountLogoutButton.gameObject.SetActive(!accountReturnsToSignIn && players?.IsSignedIn == true);
            if (accountStatusText != null) accountStatusText.text = alreadySecured
                ? $"SECURED AS {players.Username}"
                : strings.accountRules;
        }

        private void RefreshAccountButton()
        {
            var button = FindComponent<Button>(safeRoot, "Main Menu/Account");
            if (button == null) return;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            var secured = LiveOpsManager.Instance?.Players?.HasUsernamePassword ?? false;
            if (label != null) label.text = secured ? strings.account : strings.secureAccount;
            button.gameObject.SetActive(LiveOpsConfigStore.Current.usernamePasswordEnabled);
        }

        public void ShowGameplay()
        {
            subMenuReturnsToMainMenu = false;
            if (session != null && !session.IsRunning)
            {
                session.Restart();
                return;
            }
            TransitionToScreen(gameplayPanel);
            LiveOpsManager.Instance?.Ads?.SetBannerVisible(true);
            session?.SetPaused(false);
        }

        public void RefreshGameplay(LevelDefinition level, GameState state, float energyUsed, float targetEnergy)
        {
            if (levelText == null) return;
            levelText.text = level.isDailyChallenge
                ? strings.dailyChallenge
                : level.isEndless ? $"Endless {level.endlessIndex:000}" : $"Level {level.levelNumber:00}";
            var live = LiveOpsConfigStore.Current;
            var energyLimit = live.energyFailureEnabled
                ? targetEnergy * live.energyFailureMultiplier
                : targetEnergy;
            energyText.text = gameplayPresentation != null ? FormatEnergy(energyUsed, targetEnergy, live)
                : $"{strings.energy}  {FormatEnergy(energyUsed, targetEnergy, live)}";
            if (gameplayPresentation != null)
            {
                gameplayPresentation.SetChapter(level.isEndless ? 0 : (level.levelNumber - 1) / session.Config.levelsPerChapter + 1);
                gameplayPresentation.SetEnergy(energyLimit > 0f ? energyUsed / energyLimit : 0f);
            }
            if (energyUsed <= 0f) energyWarningShown = false;
            if (!energyWarningShown && energyLimit > 0f && energyUsed / energyLimit >= live.energyWarningFraction)
            {
                energyWarningShown = true;
                AnalyticsService.Log("energy_warning");
                PulseImportantText(energyText, ref energyPulseRoutine);
            }
            var chapter = session.Config.GetChapter(level.levelNumber);
            tutorialText.text = string.IsNullOrWhiteSpace(level.tutorialPrompt)
                ? chapter?.challenge ?? "Drag the magnet around the box"
                : level.tutorialPrompt;
            if (gameplayPresentation != null) tutorialText.text = FocusedGameplayPrompt(level);
            tutorialText.gameObject.SetActive(true);
            modeButton.gameObject.SetActive(level.pushUnlocked);
            modeText.text = state.mode == ForceMode.Pull ? strings.pull : strings.push;
            gameplayPresentation?.SetMode(state.mode);
            hintText.text = $"{strings.hint}  {SaveService.Data?.hintsRemaining ?? 0}";
            RefreshTimer(session.TimeRemaining, session.TimeLimit);
            RefreshCurrency();
            RefreshSignatureControls();
            if (dragGuide != null) dragGuide.SetActive(level.levelNumber == 1 && !(SaveService.Data?.onboardingMovedMagnet ?? false));
        }

        public void RefreshSignatureControls()
        {
            if (echoButton == null || session == null) return;
            var available = session.CurrentLevel != null && session.CurrentLevel.echoMagnetEnabled &&
                            LiveOpsConfigStore.Current.signatureMechanicsEnabled &&
                            LiveOpsConfigStore.Current.echoMagnetEnabled;
            echoButton.gameObject.SetActive(available);
            gameplayPresentation?.SetControls(modeButton != null && modeButton.gameObject.activeSelf, available);
            RefreshSignatureStatus();
            if (!available || echoText == null) return;
            echoText.text = session.IsEchoRecording ? "RECORDING…"
                : session.IsEchoArmed ? "DRAG TO RECORD"
                : session.HasEcho ? "RE-RECORD ECHO"
                : "ECHO";
            if (gameplayPresentation == null) SetButtonFill(echoButton, session.IsEchoRecording || session.IsEchoArmed
                ? session.Config.echoMagnetColor
                : session.Config.uiButton);
        }

        private void RefreshSignatureStatus()
        {
            if (RefreshMechanicLegend()) return;
            if (signatureStatusText == null || session?.CurrentLevel == null) return;
            if (!LiveOpsConfigStore.Current.signatureMechanicsEnabled)
            {
                signatureStatusText.gameObject.SetActive(false);
                return;
            }
            var active = session.CurrentLevel;
            var labels = new List<string>();
            if (active.echoMagnetEnabled && LiveOpsConfigStore.Current.echoMagnetEnabled) labels.Add("ECHO");
            if (active.ferrofluidMemoryEnabled && LiveOpsConfigStore.Current.ferrofluidMemoryEnabled) labels.Add("MEMORY");
            if (active.magneticImprints?.Count > 0 && LiveOpsConfigStore.Current.magneticImprintsEnabled) labels.Add("IMPRINTS");
            if (active.fieldLenses?.Count > 0 && LiveOpsConfigStore.Current.fieldLensesEnabled) labels.Add("LENSES");
            if (active.polarityLocks?.Count > 0 && LiveOpsConfigStore.Current.polarityLocksEnabled) labels.Add("LOCKS");
            if (session.RequiresOrbit) labels.Add("ORBIT");
            signatureStatusText.text = string.Join("  •  ", labels);
            signatureStatusText.gameObject.SetActive(labels.Count > 0);
        }

        public void HideDragGuide()
        {
            if (dragGuide != null) dragGuide.SetActive(false);
        }

        public void ShowHint(SolutionStep step, int remaining)
        {
            hintText.text = $"{strings.hint}  {remaining}";
            ShowToast($"{step.mode.ToString().ToUpperInvariant()} • {step.magnet.side.ToString().ToUpperInvariant()} {step.magnet.index + 1}");
        }

        public void ShowWin(int stars, float energyUsed, float targetEnergy, float elapsedSeconds,
            float performanceRating, bool endless, int endlessLevel, int currencyReward)
        {
            if (!EnsureWinBindings())
            {
                Debug.LogError("Cannot show the level result because the Win Screen is missing from the HUD.");
                ShowMainMenu();
                return;
            }
            TransitionToScreen(winPanel);
            var heading = session.IsDaily
                ? "DAILY CHALLENGE COMPLETE"
                : endless ? $"ENDLESS {endlessLevel:000} COMPLETE" : strings.completed;
            winTitle.text = heading;
            var seconds = Mathf.Max(0, Mathf.RoundToInt(elapsedSeconds));
            winStats.text = $"{Mathf.Clamp01(performanceRating) * 100f:0.0}%";
            if (winEyebrow != null) winEyebrow.text = session.IsDaily ? "DAILY CHALLENGE" :
                endless ? $"ENDLESS  •  {endlessLevel:000}" : $"LEVEL  {session.LevelIndex + 1:00}";
            if (winTimeValue != null) winTimeValue.text = $"{seconds / 60:00}:{seconds % 60:00}";
            if (winEnergyValue != null) winEnergyValue.text = FormatEnergy(energyUsed, targetEnergy, LiveOpsConfigStore.Current);
            if (winRewardValue != null) winRewardValue.text = $"+{Mathf.Max(0, currencyReward)} GEMS";
            stars = Mathf.Clamp(stars, 0, winStars.Length);
            for (var i = 0; i < winStars.Length; i++)
                if (winStars[i] != null)
                    winStars[i].color = i < stars ? session.Config.uiOrange : session.Config.uiTextMuted * 0.55f;
            if (winStarsRoutine != null) StopCoroutine(winStarsRoutine);
            if (isActiveAndEnabled) winStarsRoutine = StartCoroutine(AnimateWinStars(stars));
            var nextLabel = FindComponent<Button>(safeRoot, "Win Screen/Next")?.GetComponentInChildren<TMP_Text>(true);
            if (nextLabel != null) nextLabel.text = session.IsDaily ? strings.mainMenu : strings.next;
            RefreshCurrency();
            LiveOpsManager.Instance?.Ads?.SetBannerVisible(true);
        }

        private static string FormatEnergy(float used, float target, LiveOpsConfigData settings)
        {
            var limit = settings.energyFailureEnabled ? target * settings.energyFailureMultiplier : target;
            return $"{Mathf.CeilToInt(used)} / {Mathf.CeilToInt(limit)}";
        }

        public void ShowLeaderboardLoading(int chapterIndex)
        {
            var legend = FindComponent<TMP_Text>(safeRoot, "Leaderboard Screen/Legend");
            if (legend != null)
            {
                var settings = LiveOpsConfigStore.Current;
                var total = Mathf.Max(0.001f, settings.leaderboardTimeWeight + settings.leaderboardEnergyWeight);
                var timePercent = Mathf.RoundToInt(100f * settings.leaderboardTimeWeight / total);
                legend.text = $"RANKED {timePercent}% TIME + {100 - timePercent}% ENERGY  •  BEST RUNS";
            }
            SetLeaderboardDropdownValue(chapterIndex);
            if (leaderboardBodyText != null) leaderboardBodyText.text = "LOADING SCORES...";
            TransitionToScreen(leaderboardPanel);
            LiveOpsManager.Instance?.Ads?.SetBannerVisible(true);
        }

        public void ShowLeaderboardResults(int chapterIndex, string results)
        {
            SetLeaderboardDropdownValue(chapterIndex);
            if (leaderboardBodyText != null) leaderboardBodyText.text = results;
        }

        private void SetLeaderboardDropdownValue(int chapterIndex)
        {
            if (leaderboardScopeText != null)
                leaderboardScopeText.text = chapterIndex < 0
                    ? "ALL-TIME PERFORMANCE"
                    : $"CHAPTER {chapterIndex + 1:00} PERFORMANCE";
            if (leaderboardScopeDropdownMenu != null) leaderboardScopeDropdownMenu.SetActive(false);
        }

        private void ToggleLeaderboardDropdown()
        {
            if (leaderboardScopeDropdownMenu == null) return;
            var show = !leaderboardScopeDropdownMenu.activeSelf;
            leaderboardScopeDropdownMenu.SetActive(show);
            if (show) leaderboardScopeDropdownMenu.transform.SetAsLastSibling();
        }

        private void SelectLeaderboardDropdownScope(int chapterIndex)
        {
            if (leaderboardScopeDropdownMenu != null) leaderboardScopeDropdownMenu.SetActive(false);
            session?.SelectLeaderboardScope(chapterIndex);
        }

        public void RefreshTimer(float remaining, float limit)
        {
            if (timerText == null) return;
            if (!LiveOpsConfigStore.Current.timersEnabled)
            {
                if (timerText.gameObject.activeSelf) timerText.gameObject.SetActive(false);
                return;
            }
            if (!timerText.gameObject.activeSelf) timerText.gameObject.SetActive(true);
            var seconds = Mathf.Max(0, Mathf.CeilToInt(remaining));
            if (seconds != displayedTimerSeconds || displayedTimeLabel != strings.time)
            {
                timerText.text = gameplayPresentation != null ? $"{seconds / 60:00}:{seconds % 60:00}"
                    : $"{strings.time}  {seconds / 60:00}:{seconds % 60:00}";
                displayedTimerSeconds = seconds;
                displayedTimeLabel = strings.time;
            }
            timerText.color = limit > 0f && remaining <= Mathf.Min(15f, limit * 0.2f)
                ? new Color(0.95f, 0.2f, 0.18f)
                : gameplayPresentation != null ? gameplayPresentation.normalTimerColor : session.Config.uiNavy;
            gameplayPresentation?.SetTime(limit > 0f ? remaining / limit : 1f);
            var live = LiveOpsConfigStore.Current;
            var elapsed = Mathf.Max(0f, limit - remaining);
            if (lastTimerElapsed < 0f || elapsed + 0.1f < lastTimerElapsed || remaining > lastTimerRemaining + 1f)
            {
                lastTimerElapsed = elapsed;
                lastTimerRemaining = remaining;
                return;
            }
            var interval = live.timerMilestoneSeconds;
            var milestone = interval > 0f && Mathf.FloorToInt(elapsed / interval) > Mathf.FloorToInt(lastTimerElapsed / interval);
            var warning = lastTimerRemaining > live.timerWarningSeconds && remaining <= live.timerWarningSeconds;
            lastTimerElapsed = elapsed;
            lastTimerRemaining = remaining;
            if (milestone) AnalyticsService.Log("timer_milestone", "elapsed_seconds", Mathf.FloorToInt(elapsed));
            if (warning) AnalyticsService.Log("timer_warning", "remaining_seconds", seconds);
            if (milestone || warning) PulseImportantText(timerText, ref timerPulseRoutine);
        }

        private void PulseImportantText(TMP_Text text, ref Coroutine routine)
        {
            if (text == null || !isActiveAndEnabled || (SaveService.Data?.reduceMotion ?? false)) return;
            if (routine != null) StopCoroutine(routine);
            text.transform.localScale = Vector3.one;
            routine = StartCoroutine(AnimateImportantText(text));
        }

        private IEnumerator AnimateImportantText(TMP_Text text)
        {
            var elapsed = 0f;
            var settings = LiveOpsConfigStore.Current;
            var duration = settings.hudPulseSeconds;
            while (text != null && elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                text.transform.localScale = Vector3.one * (1f + Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI) * settings.hudPulseScale);
                yield return null;
            }
            if (text != null) text.transform.localScale = Vector3.one;
        }

        public void RefreshCurrency()
        {
            var balance = EconomyService.Balance;
            if (currencyDisplay == null || balance != displayedCurrencyBalance || displayedCurrencyLabel != strings.coins)
            {
                displayedCurrencyBalance = balance;
                displayedCurrencyLabel = strings.coins;
                currencyDisplay = $"{strings.coins}  {balance}";
            }
            for (var i = currencyTexts.Count - 1; i >= 0; i--)
            {
                if (currencyTexts[i] == null)
                {
                    currencyTexts.RemoveAt(i);
                    continue;
                }
                currencyTexts[i].text = currencyDisplay;
            }
        }

        public void ShowTimeout(float remaining, int reviveCost, bool canRevive, bool energyDepleted = false)
        {
            TransitionToScreen(timeoutPanel);
            var title = FindComponent<TMP_Text>(safeRoot, "Time Out/Title");
            if (title != null) title.text = energyDepleted ? "ENERGY EMPTY" : strings.timeUp;
            var details = FindComponent<TMP_Text>(safeRoot, "Time Out/Details");
            if (details != null)
                details.text = canRevive
                    ? energyDepleted
                        ? $"RESTORE {Mathf.RoundToInt(LiveOpsConfigStore.Current.energyRevivePercent * 100f)}% ENERGY AND CONTINUE"
                        : $"ADD {Mathf.CeilToInt(LiveOpsConfigStore.Current.reviveSeconds)} SECONDS AND CONTINUE"
                    : "NO REVIVES LEFT";
            var ad = FindComponent<Button>(safeRoot, "Time Out/Watch Ad");
            var coins = FindComponent<Button>(safeRoot, "Time Out/Use Coins");
            if (ad != null) ad.interactable = canRevive;
            if (coins != null)
            {
                coins.interactable = canRevive && EconomyService.Balance >= reviveCost;
                var label = coins.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = $"{strings.useCoins}  {reviveCost}";
            }
            RefreshCurrency();
            LiveOpsManager.Instance?.Ads?.SetBannerVisible(true);
        }

        public void ShowGameplayAfterRevive()
        {
            TransitionToScreen(gameplayPanel);
            LiveOpsManager.Instance?.Ads?.SetBannerVisible(true);
        }

        public void ShowChapterSelect()
        {
            RebuildChapterButtons();
            TransitionToScreen(chapterSelectPanel);
            LiveOpsManager.Instance?.Ads?.SetBannerVisible(true);
        }

        public void ShowLevelSelect(int chapterIndex)
        {
            selectedChapterIndex = chapterIndex;
            RebuildLevelButtons();
            TransitionToScreen(levelSelectPanel);
            LiveOpsManager.Instance?.Ads?.SetBannerVisible(true);
        }

        public void ShowSettings()
        {
            RebuildSettings();
            TransitionToScreen(settingsPanel);
            LiveOpsManager.Instance?.Ads?.SetBannerVisible(true);
        }

        public void SetPauseVisible(bool visible)
        {
            if (pausePanel == null) return;
            if (!visible && !pausePanel.activeSelf) return;
            AnalyticsService.Log(visible ? "popup_open" : "popup_closed", "popup", "pause");
            if (pauseTransitionRoutine != null)
            {
                StopCoroutine(pauseTransitionRoutine);
                pauseTransitionRoutine = null;
            }
            pauseTransitionRoutine = StartCoroutine(AnimateOverlayTransition(pausePanel, visible,
                LiveOpsConfigStore.Current.pauseTransition, () => pauseTransitionRoutine = null));
        }

        public void ShowToast(string message, float duration = -1f)
        {
            if (tooltipPanel == null || tooltipText == null || string.IsNullOrWhiteSpace(message)) return;
            tooltipText.text = message;
            ShowAnimated(tooltipPanel, 0.82f);
            tooltipPanel.transform.SetAsLastSibling();
            toastUntil = Time.unscaledTime + (duration > 0f
                ? duration
                : LiveOpsConfigStore.Current.ftueTooltipSeconds);
        }

        public void TryShowTutorials(LevelDefinition activeLevel)
        {
            if (activeLevel == null || session == null || tutorialOverlay == null) return;

            // When gameplay is opened from another screen, its transition first animates
            // the outgoing panel and then calls HideScreens(). Showing FTUE before that
            // point made HideScreens close the tutorial and clear its queue while the
            // session remained tutorial-paused. Wait until gameplay owns the screen so
            // the first level behaves exactly like a restarted level.
            if (screenTransitionRoutine != null)
            {
                if (deferredTutorialRoutine != null) StopCoroutine(deferredTutorialRoutine);
                deferredTutorialRoutine = StartCoroutine(ShowTutorialsAfterScreenTransition(activeLevel));
                return;
            }

            ShowTutorialsNow(activeLevel);
        }

        private IEnumerator ShowTutorialsAfterScreenTransition(LevelDefinition activeLevel)
        {
            while (screenTransitionRoutine != null) yield return null;
            deferredTutorialRoutine = null;
            if (activeLevel == null || gameplayPanel == null || !gameplayPanel.activeInHierarchy ||
                session == null || !session.IsRunning)
                yield break;
            ShowTutorialsNow(activeLevel);
        }

        private void ShowTutorialsNow(LevelDefinition activeLevel)
        {
            if (tutorialOverlay.GetComponent<TutorialPopupView>() == null)
            {
                var legacyOverlay = tutorialOverlay;
                legacyOverlay.SetActive(false);
                legacyOverlay.name = "Legacy Tutorial Overlay";
                BuildTutorialOverlay();
                Destroy(legacyOverlay);
            }
            tutorialQueue.Clear();
            tutorialStepIndex = 0;

            if (activeLevel.levelNumber == 1 && LiveOpsConfigStore.Current.ftueEnabled &&
                LiveOpsConfigStore.Current.ftueLevelCount >= 1)
                AddUnlearnedTutorials(session.Config.firstTimeTutorials);
            if (activeLevel.levelNumber == 2 && LiveOpsConfigStore.Current.ftueEnabled &&
                LiveOpsConfigStore.Current.ftueLevelCount >= 2)
                AddUnlearnedTutorials(session.Config.secondLevelTutorials);
            var chapter = session.Config.GetChapter(activeLevel.levelNumber);
            AddUnlearnedTutorials(chapter?.tutorialSteps);
            // Daily/endless chambers can introduce a board mechanic before its
            // campaign chapter. Teach it on the first actual encounter as well.
            for (var i = 0; i < session.Config.ChapterCount; i++)
            {
                var steps = session.Config.GetChapterByIndex(i)?.tutorialSteps;
                if (steps == null) continue;
                foreach (var step in steps)
                    if (step != null && step.id is 301 or 401 or 402 or 501)
                        AddUnlearnedTutorial(step);
            }
            if (activeLevel.echoMagnetEnabled) AddUnlearnedTutorial(session.Config.echoMagnetTutorial);
            if (activeLevel.ferrofluidMemoryEnabled) AddUnlearnedTutorial(session.Config.ferrofluidTutorial);
            if (activeLevel.magneticImprints != null && activeLevel.magneticImprints.Count > 0)
                AddUnlearnedTutorial(session.Config.magneticImprintTutorial);
            if (activeLevel.fieldLenses != null && activeLevel.fieldLenses.Count > 0)
                AddUnlearnedTutorial(session.Config.fieldLensTutorial);
            if (activeLevel.polarityLocks != null && activeLevel.polarityLocks.Count > 0)
                AddUnlearnedTutorial(session.Config.polarityLockTutorial);
            if (session.RequiresOrbit) AddUnlearnedTutorial(session.Config.orbitGoalTutorial);
            AddRailChallengeTutorial(activeLevel);

            if (tutorialQueue.Count == 0) return;
            AnalyticsService.Log("tutorial_begin", "steps", tutorialQueue.Count);
            tooltipPanel?.SetActive(false);
            if (tutorialTransitionRoutine != null) StopCoroutine(tutorialTransitionRoutine);
            RenderTutorialStep();
            tutorialTransitionRoutine = StartCoroutine(AnimateOverlayTransition(tutorialOverlay, true,
                LiveOpsConfigStore.Current.tutorialTransition, () => tutorialTransitionRoutine = null));
            tutorialOverlay.transform.SetAsLastSibling();
            session.SetTutorialPaused(true);
        }

        private void AddUnlearnedTutorials(TutorialStepDefinition[] tutorials)
        {
            if (tutorials == null) return;
            foreach (var tutorial in tutorials)
            {
                if (tutorial == null || tutorial.id <= 0 || SaveService.HasLearnedTutorial(tutorial.id) ||
                    !MechanicTutorialApplies(tutorial.id, session.CurrentLevel)) continue;
                tutorialQueue.Add(tutorial);
            }
        }

        private void AddUnlearnedTutorial(TutorialStepDefinition tutorial)
        {
            if (tutorial == null || tutorial.id <= 0 || SaveService.HasLearnedTutorial(tutorial.id) ||
                !MechanicTutorialApplies(tutorial.id, session.CurrentLevel)) return;
            if (tutorialQueue.Exists(queued => queued.id == tutorial.id)) return;
            tutorialQueue.Add(tutorial);
        }

        private void ContinueTutorial()
        {
            if (tutorialStepIndex < 0 || tutorialStepIndex >= tutorialQueue.Count) return;
            var finished = tutorialQueue[tutorialStepIndex];
            SaveService.MarkTutorialLearned(finished.id);
            AnalyticsService.Log("tutorial_step_complete", "tutorial_id", finished.id);
            tutorialStepIndex++;
            if (tutorialStepIndex < tutorialQueue.Count)
            {
                RenderTutorialStep();
                return;
            }

            if (tutorialTransitionRoutine != null)
            {
                StopCoroutine(tutorialTransitionRoutine);
                tutorialTransitionRoutine = null;
            }
            tutorialQueue.Clear();
            AnalyticsService.Log("tutorial_complete");
            void ResumeAfterTutorial()
            {
                tutorialTransitionRoutine = null;
                session.SetTutorialPaused(false);
                if (!string.IsNullOrWhiteSpace(finished.completionTooltip)) ShowToast(finished.completionTooltip);
            }
            if (tutorialOverlay.GetComponent<TutorialPopupView>() != null)
                tutorialTransitionRoutine = StartCoroutine(AnimateOverlayTransition(tutorialOverlay, false,
                    LiveOpsConfigStore.Current.tutorialTransition, ResumeAfterTutorial));
            else
            {
                tutorialOverlay.SetActive(false);
                ResumeAfterTutorial();
            }
        }

        private void RenderTutorialStep()
        {
            var step = tutorialQueue[tutorialStepIndex];
            RefreshTutorialMechanicIcon(step);
            AnalyticsService.Log("tutorial_step_view", "tutorial_id", step.id);
            tutorialTitleText.text = step.title?.ToUpperInvariant() ?? "NEW MECHANIC";
            tutorialBodyText.text = step.body ?? string.Empty;
            tutorialProgressText.text = $"{tutorialStepIndex + 1} / {tutorialQueue.Count}";
            var label = tutorialContinueButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = string.IsNullOrWhiteSpace(step.buttonLabel) ? "GOT IT" : step.buttonLabel;
        }

        private void BuildUI()
        {
            RemoveDuplicateMobileUiRoots();
            if (uiHierarchyBuilt && canvas != null && safeRoot != null) return;

            if (TryBindPrefabHierarchy())
            {
                EnsureEventSystem();
                WirePrefabButtons();
                ApplyPrefabStyling();
                uiHierarchyBuilt = true;
                return;
            }

            // In a player (including Editor Play Mode), compose missing whole screens
            // from their assets. The procedural builder below is only an authoring tool.
            if (Application.isPlaying)
            {
                EnsureAuthoredScreenHierarchy();
                if (!TryBindPrefabHierarchy())
                    throw new InvalidOperationException("The HUD screen prefab bindings are incomplete. Check the Runtime Prefab Registry and HUD prefab.");
                EnsureEventSystem();
                WirePrefabButtons();
                ApplyPrefabStyling();
                uiHierarchyBuilt = true;
                return;
            }

            // A stale/incomplete authored hierarchy must never be left underneath a
            // generated replacement. It was the source of two Mobile UI canvases and
            // an opaque background covering the real menu.
            DiscardAllMobileUiRoots();
            EnsureEventSystem();
            var canvasGo = prefabs.Spawn(prefabs.uiCanvasPrefab, "Mobile UI", transform);
            canvas = canvasGo.GetComponent<Canvas>();
            if (canvas == null)
                throw new InvalidOperationException("UI Canvas prefab requires a Canvas.");
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            if (scaler == null)
                throw new InvalidOperationException("UI Canvas prefab requires a CanvasScaler.");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            safeRoot = prefabs.Spawn(prefabs.uiSafeAreaPrefab, "Safe Area", canvasGo.transform)
                .GetComponent<RectTransform>();
            if (safeRoot == null)
                throw new InvalidOperationException("UI Safe Area prefab requires a RectTransform.");
            Stretch(safeRoot);
            BuildGameplay();
            BuildSignIn();
            BuildAccountPanel();
            BuildMainMenu();
            BuildLeaderboardPanel();
            BuildWin();
            BuildPause();
            BuildTimeout();
            BuildChapterSelect();
            BuildLevelSelect();
            BuildSettingsPanel();
            BuildTutorialOverlay();
            BuildTooltip();
            uiHierarchyBuilt = true;
        }

        private void EnsureAuthoredScreenHierarchy()
        {
            var canvasRoot = transform.Find("Mobile UI");
            if (canvasRoot == null)
                canvasRoot = prefabs.Spawn(prefabs.uiCanvasPrefab, "Mobile UI", transform).transform;
            canvas = canvasRoot.GetComponent<Canvas>();
            if (canvas == null) throw new InvalidOperationException("UICanvas prefab needs a Canvas.");
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            safeRoot = canvasRoot.Find("Safe Area") as RectTransform;
            if (safeRoot == null)
                safeRoot = prefabs.Spawn(prefabs.uiSafeAreaPrefab, "Safe Area", canvasRoot).GetComponent<RectTransform>();
            if (safeRoot == null) throw new InvalidOperationException("UISafeArea prefab needs a RectTransform.");
            Stretch(safeRoot);
            var panels = new (string name, GameObject prefab)[]
            {
                ("Sign In Screen", prefabs.signInPanelPrefab), ("Account Screen", prefabs.accountPanelPrefab),
                ("Leaderboard Screen", prefabs.leaderboardPanelPrefab), ("Main Menu", prefabs.mainMenuPrefab),
                ("Gameplay HUD", prefabs.gameplayHudPrefab), ("Win Screen", prefabs.winPanelPrefab),
                ("Pause Sheet", prefabs.pausePanelPrefab), ("Time Out", prefabs.timeOutPanelPrefab),
                ("Chapter Select", prefabs.chapterSelectPrefab), ("Level Select", prefabs.levelSelectPrefab),
                ("Settings Screen", prefabs.settingsPanelPrefab), ("Tutorial Overlay", prefabs.tutorialOverlayPrefab),
                ("Tooltip", prefabs.tooltipPrefab)
            };
            foreach (var panel in panels)
            {
                if (safeRoot.Find(panel.name) != null) continue;
                var instance = prefabs.Spawn(panel.prefab, panel.name, safeRoot);
                instance.SetActive(false);
            }
        }

        private void RemoveDuplicateMobileUiRoots()
        {
            var roots = new List<Transform>();
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name == "Mobile UI") roots.Add(child);
            }
            if (roots.Count <= 1) return;

            var keep = roots[0];
            var bestScore = ScoreMobileUiRoot(keep);
            for (var i = 1; i < roots.Count; i++)
            {
                var score = ScoreMobileUiRoot(roots[i]);
                if (score <= bestScore) continue;
                keep = roots[i];
                bestScore = score;
            }

            for (var i = 0; i < roots.Count; i++)
            {
                if (roots[i] == keep) continue;
                DiscardMobileUiRoot(roots[i]);
            }
            Debug.LogWarning($"Removed {roots.Count - 1} duplicate Mobile UI root(s). The HUD now owns one canvas.");
        }

        private static int ScoreMobileUiRoot(Transform root)
        {
            if (root == null) return -1;
            var score = root.GetComponent<Canvas>() != null ? 100 : 0;
            var safe = root.Find("Safe Area");
            if (safe == null) return score;
            score += 100;
            string[] expected =
            {
                "Main Menu", "Sign In Screen", "Gameplay HUD", "Chapter Select",
                "Level Select", "Settings Screen", "Win Screen", "Time Out"
            };
            for (var i = 0; i < expected.Length; i++)
                if (safe.Find(expected[i]) != null) score += 10;
            return score;
        }

        private void DiscardAllMobileUiRoots()
        {
            var roots = new List<Transform>();
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name == "Mobile UI") roots.Add(child);
            }
            for (var i = 0; i < roots.Count; i++) DiscardMobileUiRoot(roots[i]);
            canvas = null;
            safeRoot = null;
        }

        private static void DiscardMobileUiRoot(Transform root)
        {
            if (root == null) return;
            var objectToDiscard = root.gameObject;
            objectToDiscard.SetActive(false);
            if (Application.isPlaying)
            {
                // Detach immediately so the hierarchy and future Find calls see only
                // the authoritative canvas; Unity destroys the object at frame end.
                root.SetParent(null, false);
                objectToDiscard.name = "Discarded Mobile UI";
                Destroy(objectToDiscard);
            }
            else
            {
                DestroyImmediate(objectToDiscard);
            }
        }

        private bool TryBindPrefabHierarchy()
        {
            var canvasRoot = transform.Find("Mobile UI");
            if (canvasRoot == null) return false;
            canvas = canvasRoot.GetComponent<Canvas>();
            safeRoot = canvasRoot.Find("Safe Area") as RectTransform;
            if (canvas == null || safeRoot == null) return false;

            gameplayPanel = FindObject(safeRoot, "Gameplay HUD");
            gameplayPresentation = gameplayPanel != null ? gameplayPanel.GetComponent<GameplayHudPresentation>() : null;
            signInPanel = FindObject(safeRoot, "Sign In Screen");
            accountPanel = FindObject(safeRoot, "Account Screen");
            leaderboardPanel = FindObject(safeRoot, "Leaderboard Screen");
            mainMenuPanel = FindObject(safeRoot, "Main Menu");
            winPanel = FindObject(safeRoot, "Win Screen");
            pausePanel = FindObject(safeRoot, "Pause Sheet");
            chapterSelectPanel = FindObject(safeRoot, "Chapter Select");
            levelSelectPanel = FindObject(safeRoot, "Level Select");
            settingsPanel = FindObject(safeRoot, "Settings Screen");
            timeoutPanel = FindObject(safeRoot, "Time Out");
            if (signInPanel == null || accountPanel == null || leaderboardPanel == null || mainMenuPanel == null || gameplayPanel == null || winPanel == null || pausePanel == null ||
                chapterSelectPanel == null || levelSelectPanel == null || settingsPanel == null || timeoutPanel == null)
                return false;

            levelText = FindComponent<TMP_Text>(safeRoot, "Gameplay HUD/Level Card/Level");
            energyText = FindComponent<TMP_Text>(safeRoot, "Gameplay HUD/Energy") ??
                         FindComponent<TMP_Text>(safeRoot, "Gameplay HUD/Moves");
            timerText = FindComponent<TMP_Text>(safeRoot, "Gameplay HUD/Timer");
            tutorialText = FindComponent<TMP_Text>(safeRoot, "Gameplay HUD/Instruction Card/Tutorial Prompt");
            modeButton = FindComponent<Button>(safeRoot, "Gameplay HUD/Force Mode");
            modeText = modeButton != null ? modeButton.GetComponentInChildren<TMP_Text>(true) : null;
            echoButton = FindComponent<Button>(safeRoot, "Gameplay HUD/Echo");
            echoText = echoButton != null ? echoButton.GetComponentInChildren<TMP_Text>(true) : null;
            signatureStatusText = FindComponent<TMP_Text>(safeRoot, "Gameplay HUD/Signature Status");
            var hintButton = FindComponent<Button>(safeRoot, "Gameplay HUD/Hint");
            hintText = hintButton != null ? hintButton.GetComponentInChildren<TMP_Text>(true) : null;
            dragGuide = FindObject(safeRoot, "Gameplay HUD/Drag Guide");
            winTitle = FindComponent<TMP_Text>(safeRoot, "Win Screen/Result");
            winStats = FindComponent<TMP_Text>(safeRoot, "Win Screen/Stats");
            levelSelectTitle = FindComponent<TMP_Text>(safeRoot, "Level Select/Title");
            signInStatusText = FindComponent<TMP_Text>(safeRoot, "Sign In Screen/Status");
            googleSignInButton = FindComponent<Button>(safeRoot, "Sign In Screen/Google Play Sign In");
            guestSignInButton = FindComponent<Button>(safeRoot, "Sign In Screen/Guest Sign In");
            accountUsernameInput = FindComponent<TMP_InputField>(safeRoot, "Account Screen/Username");
            accountPasswordInput = FindComponent<TMP_InputField>(safeRoot, "Account Screen/Password");
            accountConfirmInput = FindComponent<TMP_InputField>(safeRoot, "Account Screen/Confirm Password");
            accountSignInButton = FindComponent<Button>(safeRoot, "Account Screen/Sign In");
            accountCreateButton = FindComponent<Button>(safeRoot, "Account Screen/Create Account");
            accountStatusText = FindComponent<TMP_Text>(safeRoot, "Account Screen/Status");
            leaderboardScopeDropdownButton = FindComponent<Button>(safeRoot, "Leaderboard Screen/Scope Dropdown");
            leaderboardScopeText = FindComponent<TMP_Text>(safeRoot, "Leaderboard Screen/Scope Dropdown/Label");
            leaderboardBodyText = FindComponent<TMP_Text>(safeRoot, "Leaderboard Screen/Results");
            leaderboardScopeDropdownMenu = FindObject(safeRoot, "Leaderboard Screen/Scope Dropdown Menu");
            ApplySignInStrings();
            ApplyEconomyStrings();
            tutorialOverlay = FindObject(safeRoot, "Tutorial Overlay");
            tutorialTitleText = FindComponent<TMP_Text>(safeRoot, "Tutorial Overlay/Card/Title");
            tutorialBodyText = FindComponent<TMP_Text>(safeRoot, "Tutorial Overlay/Card/Body");
            tutorialProgressText = FindComponent<TMP_Text>(safeRoot, "Tutorial Overlay/Card/Progress");
            tutorialContinueButton = FindComponent<Button>(safeRoot, "Tutorial Overlay/Card/Continue");
            tooltipPanel = FindObject(safeRoot, "Tooltip");
            tooltipText = FindComponent<TMP_Text>(safeRoot, "Tooltip/Text");
            toastText = tooltipText;
            currencyTexts.Clear();
            AddCurrencyText("Gameplay HUD/Currency");
            AddCurrencyText("Main Menu/Currency");
            AddCurrencyText("Time Out/Currency");

            screens.Clear();
            screens.Add(signInPanel);
            screens.Add(accountPanel);
            screens.Add(leaderboardPanel);
            screens.Add(mainMenuPanel);
            screens.Add(gameplayPanel);
            screens.Add(winPanel);
            screens.Add(chapterSelectPanel);
            screens.Add(levelSelectPanel);
            screens.Add(settingsPanel);
            screens.Add(timeoutPanel);

            // New optional widgets are repaired later by Ensure* methods. They must not
            // invalidate the whole authored prefab and cause a second Canvas to be built.
            // Report missing references for prefab maintenance while continuing to use
            // the single, valid hierarchy.
            var missing = new List<string>();
            AddMissing(missing, levelText, "Gameplay HUD/Level Card/Level");
            AddMissing(missing, energyText, "Gameplay HUD/Energy");
            AddMissing(missing, timerText, "Gameplay HUD/Timer");
            AddMissing(missing, tutorialText, "Gameplay HUD/Instruction Card/Tutorial Prompt");
            AddMissing(missing, modeButton, "Gameplay HUD/Force Mode");
            AddMissing(missing, hintText, "Gameplay HUD/Hint label");
            AddMissing(missing, winTitle, "Win Screen/Result");
            AddMissing(missing, winStats, "Win Screen/Stats");
            AddMissing(missing, levelSelectTitle, "Level Select/Title");
            AddMissing(missing, signInStatusText, "Sign In Screen/Status");
            AddMissing(missing, googleSignInButton, "Sign In Screen/Google Play Sign In");
            AddMissing(missing, guestSignInButton, "Sign In Screen/Guest Sign In");
            AddMissing(missing, accountUsernameInput, "Account Screen/Username");
            AddMissing(missing, accountPasswordInput, "Account Screen/Password");
            AddMissing(missing, accountConfirmInput, "Account Screen/Confirm Password");
            AddMissing(missing, accountSignInButton, "Account Screen/Sign In");
            AddMissing(missing, accountCreateButton, "Account Screen/Create Account");
            AddMissing(missing, accountStatusText, "Account Screen/Status");
            AddMissing(missing, leaderboardScopeText, "Leaderboard Screen/Scope Dropdown/Label");
            AddMissing(missing, leaderboardBodyText, "Leaderboard Screen/Results");
            AddMissing(missing, leaderboardScopeDropdownButton, "Leaderboard Screen/Scope Dropdown");
            AddMissing(missing, leaderboardScopeDropdownMenu, "Leaderboard Screen/Scope Dropdown Menu");
            AddMissing(missing, tutorialOverlay, "Tutorial Overlay");
            AddMissing(missing, tutorialContinueButton, "Tutorial Overlay/Card/Continue");
            AddMissing(missing, tooltipPanel, "Tooltip");
            if (missing.Count > 0 && !incompletePrefabWarningLogged)
            {
                incompletePrefabWarningLogged = true;
                Debug.LogWarning("HUD prefab is usable but has optional bindings to rebuild: " +
                                 string.Join(", ", missing));
            }
            return true;
        }

        private static void AddMissing<T>(ICollection<string> missing, T value, string path) where T : UnityEngine.Object
        {
            if (value == null) missing.Add(path);
        }

        private void WirePrefabButtons()
        {
            WireButton("Sign In Screen/Google Play Sign In", () => session.SignInGooglePlay());
            WireButton("Sign In Screen/Guest Sign In", OpenUsernameFromSignIn);
            WireButton("Account Screen/Back", ReturnFromAccount);
            WireButton("Account Screen/Sign In", SubmitUsernameSignIn);
            WireButton("Account Screen/Create Account", SubmitUsernameCreate);
            WireButton("Leaderboard Screen/Back", ShowMainMenu);
            WireButton("Leaderboard Screen/Scope Dropdown", ToggleLeaderboardDropdown);
            WireButton("Leaderboard Screen/Scope Dropdown Menu/All-Time", () => SelectLeaderboardDropdownScope(-1));
            for (var chapter = 0; chapter < session.Config.ChapterCount; chapter++)
            {
                var capturedChapter = chapter;
                WireButton($"Leaderboard Screen/Scope Dropdown Menu/Chapter {chapter + 1:00}",
                    () => SelectLeaderboardDropdownScope(capturedChapter));
            }
            WireButton("Main Menu/Play", () => session.StartFromMainMenu());
            WireButton("Main Menu/Daily Challenge", () => session.StartDailyChallenge());
            WireButton("Main Menu/Chapters", OpenChaptersFromMainMenu);
            WireButton("Main Menu/Settings", OpenSettingsFromMainMenu);
            WireButton("Gameplay HUD/Menu", () => session.SetPaused(true));
            WireButton("Gameplay HUD/Undo", () => session.Undo());
            WireButton("Gameplay HUD/Restart", () => session.Restart());
            WireButton("Gameplay HUD/Hint", () => session.RequestHint());
            WireButton("Gameplay HUD/Force Mode", () => session.ToggleMode());
            WireButton("Gameplay HUD/Echo", () => session.ToggleEcho());
            WireButton("Win Screen/Next", () => session.CompleteNext());
            WireButton("Win Screen/Replay", () => session.Restart());
            WireButton("Win Screen/Levels", OpenChaptersFromGameplay);
            WireButton("Pause Sheet/Resume", () => session.SetPaused(false));
            WireButton("Pause Sheet/Levels", OpenChaptersFromGameplay);
            WireButton("Pause Sheet/Settings", OpenSettingsFromGameplay);
            WireButton("Pause Sheet/Main Menu", () => session.OpenMainMenu());
            WireButton("Chapter Select/Back", ReturnFromSubMenu);
            WireButton("Level Select/Back", ShowChapterSelect);
            WireButton("Settings Screen/Back", ReturnFromSubMenu);
            WireButton("Settings Screen/Privacy", OpenPrivacyPolicy);
            WireButton("Main Menu/Free Coins", () => session.WatchAdForGems());
            WireButton("Main Menu/Account", OpenAccountFromMainMenu);
            WireButton("Main Menu/Leaderboard", () => session.ShowLeaderboard());
            WireButton("Time Out/Watch Ad", () => session.ReviveWithRewardedAd());
            WireButton("Time Out/Use Coins", () => session.ReviveWithCoins());
            WireButton("Time Out/Restart", () => session.Restart());
            WireButton("Time Out/Main Menu", () => session.OpenMainMenu());
            WireButton("Tutorial Overlay/Card/Continue", ContinueTutorial);
        }

        private void ApplySignInStrings()
        {
            if (strings == null || safeRoot == null) return;
            var body = FindComponent<TMP_Text>(safeRoot, "Sign In Screen/Body");
            var warning = FindComponent<TMP_Text>(safeRoot, "Sign In Screen/Guest Warning");
            var googleLabel = googleSignInButton != null ? googleSignInButton.GetComponentInChildren<TMP_Text>(true) : null;
            var guestLabel = guestSignInButton != null ? guestSignInButton.GetComponentInChildren<TMP_Text>(true) : null;
            if (body != null) body.text = strings.signInTitle;
            if (warning != null) warning.text = strings.guestWarning;
            if (googleLabel != null) googleLabel.text = strings.signInGooglePlay;
            if (guestLabel != null) guestLabel.text = strings.unityCloud;
            if (signInStatusText != null) signInStatusText.text = strings.signInStatus;
        }

        private void ApplyEconomyStrings()
        {
            if (strings == null || safeRoot == null) return;
            var freeCurrency = FindComponent<Button>(safeRoot, "Main Menu/Free Coins");
            var label = freeCurrency != null ? freeCurrency.GetComponentInChildren<TMP_Text>(true) : null;
            if (label != null) label.text = strings.freeCoins;
        }

        private void AddCurrencyText(string path)
        {
            var value = FindComponent<TMP_Text>(safeRoot, path);
            if (value != null && !currencyTexts.Contains(value)) currencyTexts.Add(value);
        }

        private void OpenChaptersFromMainMenu()
        {
            subMenuReturnsToMainMenu = true;
            session.OpenLevelSelect();
        }

        private void OpenSettingsFromMainMenu()
        {
            subMenuReturnsToMainMenu = true;
            session.OpenSettings();
        }

        private void OpenChaptersFromGameplay()
        {
            subMenuReturnsToMainMenu = false;
            session.OpenLevelSelect();
        }

        private void OpenSettingsFromGameplay()
        {
            subMenuReturnsToMainMenu = false;
            session.OpenSettings();
        }

        private void ReturnFromSubMenu()
        {
            if (subMenuReturnsToMainMenu) ShowMainMenu();
            else ShowGameplay();
        }

        private void WireButton(string relativePath, UnityEngine.Events.UnityAction action)
        {
            var button = FindComponent<Button>(safeRoot, relativePath);
            if (button == null)
            {
                Debug.LogWarning($"HUD prefab button '{relativePath}' is unavailable; the rest of the UI remains active.");
                return;
            }
            button.onClick.RemoveAllListeners();
            AnalyticsButtonTracker.Bind(button);
            button.onClick.AddListener(action);
        }

        private void ApplyPrefabStyling()
        {
            foreach (var button in safeRoot.GetComponentsInChildren<Button>(true))
            {
                var image = button.GetComponent<Image>();
                if (image == null) continue;
                image.sprite = RoundedPanelSprite;
                image.type = Image.Type.Sliced;
            }

            ApplyRoundedPanel("Gameplay HUD/Level Card");
            ApplyRoundedPanel("Gameplay HUD/Instruction Card");
            ApplyRoundedPanel("Gameplay HUD/Drag Guide");
            ApplyRoundedPanel("Tutorial Overlay/Card");
            ApplyRoundedPanel("Tooltip");
        }

        private void ApplyPremiumTheme()
        {
            if (safeRoot == null || session?.Config == null) return;
            var config = session.Config;
            EnsureFullScreenBackdrop();
            ThemeScreen(signInPanel, true);
            ThemeScreen(mainMenuPanel, true);
            ThemeScreen(accountPanel, true);
            ThemeScreen(leaderboardPanel, true);
            ThemeScreen(chapterSelectPanel, true);
            ThemeScreen(levelSelectPanel, true);
            ThemeScreen(settingsPanel, true);
            ThemeScreen(winPanel, false);
            ThemeScreen(timeoutPanel, false);
            ThemeScreen(pausePanel, false);

            EnsureBackdrop(signInPanel, false);
            EnsureBackdrop(mainMenuPanel, true);
            EnsureBackdrop(accountPanel, false);
            EnsureBackdrop(leaderboardPanel, false);
            EnsureBackdrop(chapterSelectPanel, false);
            EnsureBackdrop(levelSelectPanel, false);
            EnsureBackdrop(settingsPanel, false);
            EnsureSurface(mainMenuPanel, "Menu Glass", new Vector2(0.1f, 0.045f), new Vector2(0.9f, 0.585f));
            EnsureSurface(winPanel, "Result Glass", new Vector2(0.08f, 0.17f), new Vector2(0.92f, 0.82f), 0);
            EnsureSurface(timeoutPanel, "Timeout Glass", new Vector2(0.08f, 0.09f), new Vector2(0.92f, 0.84f), 0);
            EnsureSurface(pausePanel, "Pause Glass", new Vector2(0.1f, 0.14f), new Vector2(0.9f, 0.84f), 0);
            EnsureSurface(gameplayPanel, "Top Stats Glass", new Vector2(0.035f, 0.775f), new Vector2(0.965f, 0.865f), 0);
            EnsureMainMenuDetails();
            ApplyPremiumLayout();
            EnsureWinPresentation();

            foreach (var button in safeRoot.GetComponentsInChildren<Button>(true)) StyleButton(button);
            StyleVictoryButtons();
            StyleSurface("Main Menu/Menu Glass", config.uiSurface, true);
            StyleSurface("Main Menu/Currency Chip", config.uiSurfaceBright, false);
            StyleSurface("Leaderboard Screen/Results Surface", config.uiSurface, false);
            StyleSurface("Time Out/Timeout Glass", config.uiSurface, true);
            StyleSurface("Pause Sheet/Pause Glass", config.uiSurface, true);
            StyleSurface("Account Screen/Username", config.uiSurfaceBright, false);
            StyleSurface("Account Screen/Password", config.uiSurfaceBright, false);
            StyleSurface("Account Screen/Confirm Password", config.uiSurfaceBright, false);
            StyleSurface("Gameplay HUD/Level Card", config.uiSurfaceBright, false);
            StyleSurface("Gameplay HUD/Instruction Card", config.uiSurface, true);
            StyleSurface("Gameplay HUD/Drag Guide", new Color(config.uiPrimary.r, config.uiPrimary.g, config.uiPrimary.b, 0.2f), false);
            StyleSurface("Tutorial Overlay/Card", config.uiSurface, true);
            StyleSurface("Tooltip", config.uiSurfaceBright, true);

            SetTextColor("Gameplay HUD/Level Card/Level", config.uiTextPrimary);
            SetTextColor("Gameplay HUD/Energy", config.uiTextPrimary);
            SetTextColor("Gameplay HUD/Timer", config.uiTextPrimary);
            SetTextColor("Gameplay HUD/Currency", config.uiTextPrimary);
            SetTextColor("Gameplay HUD/Instruction Card/Tutorial Prompt", config.uiTextPrimary);
            SetTextColor("Gameplay HUD/Signature Status", config.uiTextPrimary);
            SetTextColor("Gameplay HUD/Drag Guide/Guide Text", config.uiTextPrimary);
            SetTextColor("Tutorial Overlay/Card/Title", config.uiTextPrimary);
            SetTextColor("Tutorial Overlay/Card/Body", config.uiTextPrimary);
            SetTextColor("Tutorial Overlay/Card/Progress", config.uiTextMuted);
            SetTextColor("Tooltip/Text", config.uiTextPrimary);

            var menuTitle = mainMenuPanel != null ? mainMenuPanel.transform.Find("Title")?.GetComponent<TMP_Text>() : null;
            if (menuTitle != null)
            {
                menuTitle.color = config.uiTextPrimary;
                menuTitle.text = $"<color=#{ColorUtility.ToHtmlStringRGB(config.uiTextPrimary)}>OUTSIDE</color>\n" +
                                 $"<color=#{ColorUtility.ToHtmlStringRGB(config.uiPrimary)}>THE BOX</color>";
            }
        }

        private void EnsureWinPresentation()
        {
            if (winPanel == null || winTitle == null || winStats == null) return;
            var root = winPanel.transform;
            var config = session.Config;
            var glass = root.Find("Result Glass") as RectTransform;
            if (glass != null)
            {
                SetRect(glass, new Vector2(0.055f, 0.095f), new Vector2(0.945f, 0.875f));
                ConfigureVictoryCard(glass.gameObject, config.uiSurface,
                    new Color(config.uiGlow.r, config.uiGlow.g, config.uiGlow.b, 0.5f), 3f, 52f);
            }

            var halo = root.Find("Victory Glow");
            if (halo == null)
            {
                var glow = CreateRoundedPanel("Victory Glow", root,
                    new Color(config.uiGlow.r, config.uiGlow.g, config.uiGlow.b, 0.24f));
                SetRect(glow.GetComponent<RectTransform>(), new Vector2(0.08f, 0.49f), new Vector2(0.92f, 0.87f));
                MakeSoftOrb(glow);
                halo = glow.transform;
            }
            SetBackdropGlow(halo, config.uiGlow, 0.24f);
            halo.SetSiblingIndex(Mathf.Min(1, root.childCount - 1));

            winEyebrow = EnsureWinLabel("Victory Eyebrow", root, 25, config.uiPrimary,
                new Vector2(0.21f, 0.825f), new Vector2(0.79f, 0.855f));
            winEyebrow.text = "LEVEL COMPLETE";
            winTitle.color = config.uiTextPrimary;
            winTitle.fontStyle = FontStyles.Bold;
            winTitle.fontSize = 64;
            winTitle.enableAutoSizing = true;
            winTitle.fontSizeMin = 25;
            winTitle.fontSizeMax = 64;
            SetRect(winTitle.rectTransform, new Vector2(0.10f, 0.765f), new Vector2(0.90f, 0.825f));

            var hero = root.Find("Victory Illustration");
            if (hero == null) hero = CreatePanel("Victory Illustration", root, Color.white).transform;
            SetRect((RectTransform)hero, new Vector2(0.14f, 0.525f), new Vector2(0.86f, 0.775f));
            var heroImage = hero.GetComponent<Image>();
            if (victoryIllustrationSprite == null)
            {
                var texture = Resources.Load<Texture2D>(config.victoryHeroResource);
                if (texture != null)
                    victoryIllustrationSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f));
            }
            if (heroImage != null)
            {
                heroImage.sprite = victoryIllustrationSprite;
                heroImage.preserveAspect = true;
                heroImage.raycastTarget = false;
                heroImage.enabled = victoryIllustrationSprite != null;
            }

            for (var i = 0; i < winStars.Length; i++)
            {
                var center = 0.35f + i * 0.15f;
                var starName = $"Victory Star {i + 1}";
                var star = root.Find(starName);
                if (star == null) star = CreatePanel(starName, root, config.uiOrange).transform;
                winStars[i] = star.GetComponent<Image>();
                if (winStars[i] != null)
                {
                    ConfigureStarImage(winStars[i], true);
                    winStars[i].preserveAspect = true;
                    winStars[i].raycastTarget = false;
                    winStars[i].color = config.uiOrange;
                }
                SetRect((RectTransform)star, new Vector2(center - 0.035f, 0.50f),
                    new Vector2(center + 0.035f, 0.54f));
            }

            var performance = EnsureWinCard("Performance Card", root,
                new Vector2(0.12f, 0.395f), new Vector2(0.88f, 0.475f), config.uiSurfaceBright);
            EnsureWinLabel("Caption", performance, 22, config.uiTextMuted,
                new Vector2(0.03f, 0.57f), new Vector2(0.97f, 0.94f)).text = "PERFORMANCE";
            winStats.color = config.uiTextPrimary;
            winStats.fontStyle = FontStyles.Bold;
            winStats.fontSize = 48;
            SetRect(winStats.rectTransform, new Vector2(0.2f, 0.399f), new Vector2(0.8f, 0.447f));
            winStats.transform.SetAsLastSibling();

            var time = EnsureWinCard("Time Card", root,
                new Vector2(0.12f, 0.305f), new Vector2(0.49f, 0.385f), config.uiSurfaceBright);
            var energy = EnsureWinCard("Energy Card", root,
                new Vector2(0.51f, 0.305f), new Vector2(0.88f, 0.385f), config.uiSurfaceBright);
            EnsureWinLabel("Caption", time, 21, config.uiTextMuted,
                new Vector2(0.05f, 0.54f), new Vector2(0.95f, 0.93f)).text = "TIME";
            EnsureWinLabel("Caption", energy, 21, config.uiTextMuted,
                new Vector2(0.05f, 0.54f), new Vector2(0.95f, 0.93f)).text = "ENERGY";
            winTimeValue = EnsureWinLabel("Value", time, 35, config.uiTextPrimary,
                new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.57f));
            winEnergyValue = EnsureWinLabel("Value", energy, 31, config.uiTextPrimary,
                new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.57f));

            var reward = EnsureWinCard("Reward Chip", root,
                new Vector2(0.28f, 0.255f), new Vector2(0.72f, 0.30f),
                Color.Lerp(config.uiSurfaceBright, config.uiPrimary, 0.27f));
            winRewardValue = EnsureWinLabel("Value", reward, 27, config.uiTextPrimary,
                new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f));

            SetRectAt("Win Screen/Next", new Vector2(0.12f, 0.185f), new Vector2(0.88f, 0.245f));
            SetRectAt("Win Screen/Replay", new Vector2(0.12f, 0.115f), new Vector2(0.49f, 0.17f));
            SetRectAt("Win Screen/Levels", new Vector2(0.51f, 0.115f), new Vector2(0.88f, 0.17f));
        }

        private bool EnsureWinBindings()
        {
            if (safeRoot == null && canvas != null)
                safeRoot = canvas.transform.Find("Safe Area") as RectTransform;
            if (winPanel == null && safeRoot != null) winPanel = FindObject(safeRoot, "Win Screen");
            if (winPanel == null) return false;
            if (winTitle == null) winTitle = winPanel.transform.Find("Result")?.GetComponent<TMP_Text>();
            if (winStats == null) winStats = winPanel.transform.Find("Stats")?.GetComponent<TMP_Text>();
            if (winTitle == null)
                winTitle = CreateText("Result", winPanel.transform, 54, TextAlignmentOptions.Center, session.Config.uiTextPrimary);
            if (winStats == null)
                winStats = CreateText("Stats", winPanel.transform, 43, TextAlignmentOptions.Center, session.Config.uiTextPrimary);
            if (!screens.Contains(winPanel)) screens.Add(winPanel);
            if (winEyebrow == null || winTimeValue == null || winEnergyValue == null || winRewardValue == null)
                EnsureWinPresentation();
            return true;
        }

        private Transform EnsureWinCard(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var card = parent.Find(name);
            if (card == null) card = CreateRoundedPanel(name, parent, color).transform;
            SetRect((RectTransform)card, min, max);
            ConfigureVictoryCard(card.gameObject, color,
                new Color(session.Config.uiGlow.r, session.Config.uiGlow.g, session.Config.uiGlow.b, 0.35f), 2f, 27f);
            return card;
        }

        private void ConfigureVictoryCard(GameObject target, Color fill, Color border, float width, float radius)
        {
            var image = target.GetComponent<Image>();
            if (image != null) { image.enabled = false; image.raycastTarget = false; }
            // Unity permits only one Graphic on a GameObject, even when its Image is
            // disabled. The decorative graphic needs its own stretched child.
            var frame = target.transform.Find("Victory Card Frame");
            if (frame == null)
            {
                frame = CreateRect("Victory Card Frame", target.transform);
                Stretch((RectTransform)frame);
                frame.SetAsFirstSibling();
            }
            if (frame.GetComponent<CanvasRenderer>() == null) frame.gameObject.AddComponent<CanvasRenderer>();
            var graphic = frame.GetComponent<UIRoundRectGraphic>() ?? frame.gameObject.AddComponent<UIRoundRectGraphic>();
            if (graphic == null)
            {
                if (image != null) { image.enabled = true; image.color = fill; }
                Debug.LogWarning($"Could not create the victory card frame for {target.name}.");
                return;
            }
            graphic.raycastTarget = false;
            graphic.Configure(fill, border, width, radius);
            graphic.ConfigureGradient(Color.Lerp(fill, session.Config.uiBackgroundBottom, 0.25f),
                Color.Lerp(fill, session.Config.uiPrimary, 0.08f), session.Config.uiTextPrimary, 0.035f);
        }

        private void StyleVictoryButtons()
        {
            foreach (var name in new[] { "Next", "Replay", "Levels" })
            {
                var button = winPanel != null ? winPanel.transform.Find(name)?.GetComponent<Button>() : null;
                var label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
                if (label == null) continue;
                label.fontSize = name == "Next" ? 34 : 28;
                label.enableAutoSizing = true;
                label.fontSizeMin = 18;
                label.fontSizeMax = label.fontSize;
            }
        }

        private TMP_Text EnsureWinLabel(string name, Transform parent, int fontSize, Color color, Vector2 min, Vector2 max)
        {
            var label = parent.Find(name)?.GetComponent<TMP_Text>() ??
                        CreateText(name, parent, fontSize, TextAlignmentOptions.Center, color);
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = color;
            label.raycastTarget = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(12, fontSize / 2);
            label.fontSizeMax = fontSize;
            SetRect(label.rectTransform, min, max);
            return label;
        }

        private IEnumerator AnimateWinStars(int earnedStars)
        {
            foreach (var star in winStars) if (star != null) star.transform.localScale = Vector3.one;
            if (SaveService.Data?.reduceMotion ?? false) { winStarsRoutine = null; yield break; }
            for (var i = 0; i < earnedStars; i++) if (winStars[i] != null) winStars[i].transform.localScale = Vector3.zero;
            while (screenTransitionRoutine != null) yield return null;
            if (winPanel == null || !winPanel.activeInHierarchy) { winStarsRoutine = null; yield break; }
            var duration = Mathf.Clamp(LiveOpsConfigStore.Current.uiAnimationSeconds, 0.1f, 0.6f);
            for (var i = 0; i < earnedStars; i++)
            {
                var star = winStars[i];
                if (star == null) continue;
                var elapsed = 0f;
                while (elapsed < duration && winPanel.activeInHierarchy)
                {
                    elapsed += Time.unscaledDeltaTime;
                    var t = Mathf.Clamp01(elapsed / duration);
                    star.transform.localScale = Vector3.one * (1f - Mathf.Pow(1f - t, 3f) + Mathf.Sin(t * Mathf.PI) * 0.22f);
                    yield return null;
                }
                star.transform.localScale = Vector3.one;
                if (!winPanel.activeInHierarchy) break;
            }
            winStarsRoutine = null;
        }

        private void ThemeScreen(GameObject panel, bool opaque)
        {
            if (panel == null) return;
            if (panel == tutorialOverlay)
            {
                var tutorialImage = panel.GetComponent<Image>();
                if (tutorialImage != null) tutorialImage.enabled = false;
                var tutorialGradient = panel.GetComponent<UIGradient>();
                if (tutorialGradient != null) tutorialGradient.enabled = false;
                return;
            }
            var config = session.Config;
            var image = panel.GetComponent<Image>();
            if (image == null) return;
            if (opaque)
            {
                image.color = Color.clear;
                var existingGradient = panel.GetComponent<UIGradient>();
                if (existingGradient != null) existingGradient.enabled = false;
                return;
            }
            image.color = Color.white;
            var gradient = panel.GetComponent<UIGradient>();
            if (gradient == null) gradient = panel.AddComponent<UIGradient>();
            var bottom = config.uiBackgroundBottom;
            var top = config.uiBackgroundTop;
            if (!opaque)
            {
                bottom.a = 0.97f;
                top.a = 0.94f;
            }
            gradient.Configure(bottom, top, config.uiGlow, 0.07f);
        }

        private void EnsureFullScreenBackdrop()
        {
            if (canvas == null) return;
            var existing = canvas.transform.Find("Full Screen Background");
            if (existing != null) fullScreenBackdrop = existing.gameObject;
            else
            {
                fullScreenBackdrop = CreatePanel("Full Screen Background", canvas.transform, Color.white);
                fullScreenBackdrop.transform.SetAsFirstSibling();
            }
            var image = fullScreenBackdrop.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
            var gradient = fullScreenBackdrop.GetComponent<UIGradient>();
            if (gradient == null) gradient = fullScreenBackdrop.AddComponent<UIGradient>();
            gradient.enabled = true;
            gradient.Configure(session.Config.uiBackgroundBottom, session.Config.uiBackgroundTop,
                session.Config.uiGlow, 0.14f);
        }

        private void EnsureMainMenuDetails()
        {
            if (mainMenuPanel == null) return;
            var root = mainMenuPanel.transform;
            if (root.Find("Currency Chip") == null)
            {
                var chip = CreateRoundedPanel("Currency Chip", root, session.Config.uiSurfaceBright);
                chip.GetComponent<Image>().raycastTarget = false;
                AddSoftShadow(chip, 0.25f, new Vector2(0f, -4f));
                var currency = root.Find("Currency");
                if (currency != null) chip.transform.SetSiblingIndex(currency.GetSiblingIndex());
            }
            if (root.Find("Eyebrow") == null)
            {
                var eyebrow = CreateText("Eyebrow", root, 22, TextAlignmentOptions.Center, session.Config.uiPrimary);
                eyebrow.fontStyle = FontStyles.Bold;
                eyebrow.text = "A MAGNETIC PUZZLE";
            }
        }

        private void ApplyPremiumLayout()
        {
            if (safeRoot == null) return;
            SetRectAt("Main Menu/Currency Chip", new Vector2(0.62f, 0.91f), new Vector2(0.94f, 0.965f));
            SetRectAt("Main Menu/Currency", new Vector2(0.63f, 0.91f), new Vector2(0.93f, 0.965f));
            SetRectAt("Main Menu/Eyebrow", new Vector2(0.22f, 0.865f), new Vector2(0.78f, 0.905f));
            SetRectAt("Main Menu/Title", new Vector2(0.08f, 0.68f), new Vector2(0.92f, 0.86f));
            SetRectAt("Main Menu/Subtitle", new Vector2(0.10f, 0.615f), new Vector2(0.90f, 0.665f));
            SetRectAt("Main Menu/Play", new Vector2(0.08f, 0.525f), new Vector2(0.92f, 0.61f));
            SetRectAt("Main Menu/Daily Challenge", new Vector2(0.08f, 0.429f), new Vector2(0.92f, 0.514f));
            SetRectAt("Main Menu/Chapters", new Vector2(0.08f, 0.333f), new Vector2(0.92f, 0.418f));
            SetRectAt("Main Menu/Leaderboard", new Vector2(0.08f, 0.237f), new Vector2(0.92f, 0.322f));
            SetRectAt("Main Menu/Settings", new Vector2(0.08f, 0.141f), new Vector2(0.49f, 0.226f));
            SetRectAt("Main Menu/Free Coins", new Vector2(0.51f, 0.141f), new Vector2(0.92f, 0.226f));
            SetRectAt("Main Menu/Account", new Vector2(0.08f, 0.045f), new Vector2(0.49f, 0.13f));
            SetRectAt("Main Menu/Store", new Vector2(0.51f, 0.045f), new Vector2(0.92f, 0.13f));
            var obsoleteGoogle = safeRoot.Find("Main Menu/Google Play");
            if (obsoleteGoogle != null) obsoleteGoogle.gameObject.SetActive(false);
            SetRectAt("Main Menu/Version", new Vector2(0.2f, 0.003f), new Vector2(0.8f, 0.035f));
            if (gameplayPresentation == null)
            {
            SetRectAt("Gameplay HUD/Undo", new Vector2(0.035f, 0.015f), new Vector2(0.28f, 0.09f));
            SetRectAt("Gameplay HUD/Restart", new Vector2(0.30f, 0.015f), new Vector2(0.565f, 0.09f));
            SetRectAt("Gameplay HUD/Hint", new Vector2(0.585f, 0.015f), new Vector2(0.965f, 0.09f));
            SetRectAt("Gameplay HUD/Instruction Card", new Vector2(0.035f, 0.101f), new Vector2(0.965f, 0.145f));
            SetRectAt("Gameplay HUD/Force Mode", new Vector2(0.35f, 0.157f), new Vector2(0.65f, 0.212f));
            SetRectAt("Gameplay HUD/Echo", new Vector2(0.67f, 0.157f), new Vector2(0.95f, 0.212f));
            SetRectAt("Gameplay HUD/Signature Status", new Vector2(0.08f, 0.745f), new Vector2(0.92f, 0.775f));
            SetRectAt("Gameplay HUD/Currency", new Vector2(0.155f, 0.90f), new Vector2(0.38f, 0.965f));
            }
            var title = FindComponent<TMP_Text>(safeRoot, "Main Menu/Title");
            if (title != null) title.fontSize = 98;
            foreach (var button in mainMenuPanel.GetComponentsInChildren<Button>(true))
            {
                if (button.name is "Back" or "Gem Balance") continue;
                var label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.fontSize = Mathf.Max(label.fontSize, 40f);
            }
            var subtitle = FindComponent<TMP_Text>(safeRoot, "Main Menu/Subtitle");
            if (subtitle != null)
            {
                subtitle.text = "BEND THE FIELD  •  FIND THE PATH";
                subtitle.color = session.Config.uiTextMuted;
            }
        }

        private void SetRectAt(string path, Vector2 min, Vector2 max)
        {
            var rect = safeRoot.Find(path) as RectTransform;
            if (rect != null) SetRect(rect, min, max);
        }

        private void EnsureBackdrop(GameObject panel, bool hero)
        {
            if (panel == null) return;
            var existingCyan = panel.transform.Find("Aurora Cyan");
            if (existingCyan != null)
            {
                SetBackdropGlow(existingCyan, session.Config.uiGlow, hero ? 0.15f : 0.09f);
                SetBackdropGlow(panel.transform.Find("Aurora Violet"), session.Config.uiSecondary,
                    hero ? 0.13f : 0.07f);
                return;
            }
            var root = panel.GetComponent<RectTransform>();
            var config = session.Config;
            var cyan = CreateRoundedPanel("Aurora Cyan", root,
                new Color(config.uiGlow.r, config.uiGlow.g, config.uiGlow.b, hero ? 0.15f : 0.09f));
            SetRect(cyan.GetComponent<RectTransform>(), new Vector2(0.48f, 0.66f), new Vector2(1.12f, 1.08f));
            cyan.transform.SetAsFirstSibling();
            cyan.GetComponent<Image>().raycastTarget = false;
            MakeSoftOrb(cyan);
            cyan.AddComponent<UIFloatMotion>().Configure(new Vector2(18f, 26f), 0.42f, 0.4f, 5f);

            var violet = CreateRoundedPanel("Aurora Violet", root,
                new Color(config.uiSecondary.r, config.uiSecondary.g, config.uiSecondary.b, hero ? 0.13f : 0.07f));
            SetRect(violet.GetComponent<RectTransform>(), new Vector2(-0.14f, -0.08f), new Vector2(0.48f, 0.34f));
            violet.transform.SetSiblingIndex(1);
            violet.GetComponent<Image>().raycastTarget = false;
            MakeSoftOrb(violet);
            violet.AddComponent<UIFloatMotion>().Configure(new Vector2(22f, 16f), 0.31f, 2.1f, -4f);
        }

        private static void SetBackdropGlow(Transform target, Color color, float alpha)
        {
            if (target == null) return;
            color.a = alpha;
            var glow = target.Find("Soft Glow")?.GetComponent<UIRadialGlow>();
            if (glow != null) glow.color = color;
        }

        private void MakeSoftOrb(GameObject target)
        {
            var image = target.GetComponent<Image>();
            if (image == null) return;
            var glowTransform = target.transform.Find("Soft Glow");
            if (glowTransform == null)
            {
                var glowObject = prefabs.Spawn(prefabs.uiRectPrefab, "Soft Glow", target.transform);
                glowTransform = glowObject.transform;
                Stretch((RectTransform)glowTransform);
            }
            if (glowTransform.GetComponent<CanvasRenderer>() == null)
                glowTransform.gameObject.AddComponent<CanvasRenderer>();
            var glow = glowTransform.GetComponent<UIRadialGlow>();
            if (glow == null) glow = glowTransform.gameObject.AddComponent<UIRadialGlow>();
            glow.color = image.color;
            glow.raycastTarget = false;
            image.enabled = false;
        }

        private void EnsureSurface(GameObject panel, string name, Vector2 min, Vector2 max, int sibling = 2)
        {
            if (panel == null || panel.transform.Find(name) != null) return;
            var surface = CreateRoundedPanel(name, panel.transform, session.Config.uiSurface);
            SetRect(surface.GetComponent<RectTransform>(), min, max);
            surface.transform.SetSiblingIndex(Mathf.Clamp(sibling, 0, panel.transform.childCount - 1));
            surface.GetComponent<Image>().raycastTarget = false;
            AddSoftShadow(surface, 0.34f, new Vector2(0f, -10f));
        }

        private void StyleSurface(string path, Color color, bool shadow)
        {
            if (gameplayPresentation != null && path.StartsWith("Gameplay HUD/", StringComparison.Ordinal)) return;
            if (path.StartsWith("Tutorial Overlay/", StringComparison.Ordinal) &&
                tutorialOverlay != null && tutorialOverlay.GetComponent<TutorialPopupView>() != null) return;
            var image = FindComponent<Image>(safeRoot, path);
            if (image == null) return;
            image.color = color;
            image.sprite = RoundedPanelSprite;
            image.type = Image.Type.Sliced;
            if (shadow) AddSoftShadow(image.gameObject, 0.32f, new Vector2(0f, -8f));
        }

        private void SetTextColor(string path, Color color)
        {
            if (gameplayPresentation != null && path.StartsWith("Gameplay HUD/", StringComparison.Ordinal)) return;
            if (path.StartsWith("Tutorial Overlay/", StringComparison.Ordinal) &&
                tutorialOverlay != null && tutorialOverlay.GetComponent<TutorialPopupView>() != null) return;
            var text = FindComponent<TMP_Text>(safeRoot, path);
            if (text != null) text.color = color;
        }

        private void StyleButton(Button button)
        {
            if (button == null) return;
            AnalyticsButtonTracker.Bind(button);
            if (button.GetComponentInParent<GameplayHudPresentation>(true) != null ||
                button.GetComponentInParent<TutorialPopupView>(true) != null) return;
            var config = session.Config;
            var image = button.GetComponent<Image>();
            if (image == null) return;
            var name = button.gameObject.name;
            var primary = name is "Play" or "Next" or "Resume" or "Continue" or "Watch Ad";
            var reward = name is "Daily Challenge" or "Free Coins";
            var danger = name == "Delete Account" ||
                         (name == "Restart" && button.transform.parent != gameplayPanel?.transform);
            var fill = primary ? config.uiPrimary : reward ? config.uiSecondary : danger ? config.uiDanger : config.uiButton;
            image.color = fill;
            image.sprite = RoundedPanelSprite;
            image.type = Image.Type.Sliced;
            image.enabled = false;
            var frameTransform = button.transform.Find("Button Frame");
            if (frameTransform == null)
            {
                var frameObject = prefabs.Spawn(prefabs.uiRectPrefab, "Button Frame", button.transform);
                frameTransform = frameObject.transform;
                Stretch((RectTransform)frameTransform);
                frameTransform.SetAsFirstSibling();
            }
            if (frameTransform.GetComponent<CanvasRenderer>() == null)
                frameTransform.gameObject.AddComponent<CanvasRenderer>();
            var frame = frameTransform.GetComponent<UIRoundRectGraphic>();
            if (frame == null) frame = frameTransform.gameObject.AddComponent<UIRoundRectGraphic>();
            var border = primary ? config.uiPrimaryBorder
                : reward ? config.uiRewardBorder
                : danger ? config.uiDangerBorder
                : config.uiButtonBorder;
            frame.Configure(fill, border, primary ? 6f : 4f, primary ? 30f : 24f);
            if (config.uiButtonGradientsEnabled)
            {
                var bottom = primary ? config.uiPrimaryGradientBottom
                    : reward ? config.uiRewardGradientBottom
                    : danger ? config.uiDangerGradientBottom
                    : config.uiButtonGradientBottom;
                var top = primary ? config.uiPrimaryGradientTop
                    : reward ? config.uiRewardGradientTop
                    : danger ? config.uiDangerGradientTop
                    : config.uiButtonGradientTop;
                frame.ConfigureGradient(bottom, top, config.uiButtonGradientHighlight,
                    config.uiButtonGradientHighlightStrength);
            }
            frame.raycastTarget = true;
            button.targetGraphic = frame;
            var text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.color = primary ? config.uiNavy : config.uiTextPrimary;
                text.fontStyle = FontStyles.Bold;
            }
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.84f, 0.9f, 0.94f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.42f, 0.48f, 0.55f, 0.48f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            AddSoftShadow(button.gameObject, primary ? 0.42f : 0.28f, new Vector2(0f, primary ? -8f : -5f));
        }

        private void SetButtonFill(Button button, Color fill)
        {
            if (button == null) return;
            var image = button.GetComponent<Image>();
            if (image != null) image.color = fill;
            var frame = button.transform.Find("Button Frame")?.GetComponent<UIRoundRectGraphic>();
            if (frame != null)
            {
                frame.Configure(fill, Color.Lerp(fill, Color.white, 0.5f), 5f, 26f);
                if (session.Config.uiButtonGradientsEnabled)
                    frame.ConfigureGradient(Color.Lerp(fill, Color.black, 0.2f),
                        Color.Lerp(fill, Color.white, 0.18f), session.Config.uiButtonGradientHighlight,
                        session.Config.uiButtonGradientHighlightStrength);
            }
            // Chapter, store and selected-state colors are dynamic. Keep their labels
            // readable when a light tint replaces the standard blue button.
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                var brightness = fill.r * 0.2126f + fill.g * 0.7152f + fill.b * 0.0722f;
                label.color = brightness > 0.58f ? session.Config.uiNavy : session.Config.uiTextPrimary;
            }
        }

        private static void AddSoftShadow(GameObject target, float alpha, Vector2 distance)
        {
            var shadow = target.GetComponent<Shadow>();
            if (shadow == null) shadow = target.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.02f, 0.06f, alpha);
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private void ApplyRoundedPanel(string path)
        {
            if (gameplayPresentation != null && path.StartsWith("Gameplay HUD/", StringComparison.Ordinal)) return;
            if (path.StartsWith("Tutorial Overlay/", StringComparison.Ordinal) &&
                tutorialOverlay != null && tutorialOverlay.GetComponent<TutorialPopupView>() != null) return;
            var image = FindComponent<Image>(safeRoot, path);
            if (image == null) return;
            image.sprite = RoundedPanelSprite;
            image.type = Image.Type.Sliced;
        }

        private static GameObject FindObject(Transform root, string path)
        {
            var found = root.Find(path);
            return found != null ? found.gameObject : null;
        }

        private static T FindComponent<T>(Transform root, string path) where T : Component
        {
            var found = root.Find(path);
            return found != null ? found.GetComponent<T>() : null;
        }

        private void BuildGameplay()
        {
            gameplayPanel = CreatePanel("Gameplay HUD", safeRoot, Color.clear);
            screens.Add(gameplayPanel);
            var root = gameplayPanel.GetComponent<RectTransform>();
            var config = session.Config;

            var title = CreateText("Game Title", root, 58, TextAlignmentOptions.Center, config.uiNavy);
            title.fontStyle = FontStyles.Bold;
            title.richText = true;
            title.text = $"<color=#{ColorUtility.ToHtmlStringRGB(config.uiNavy)}>OUTSIDE</color>\n" +
                         $"<color=#{ColorUtility.ToHtmlStringRGB(config.uiOrange)}>THE BOX</color>";
            title.lineSpacing = 0.72f;
            SetRect(title.rectTransform, new Vector2(0.14f, 0.865f), new Vector2(0.86f, 0.99f));

            var levelCard = CreateRoundedPanel("Level Card", root, config.uiCream);
            SetRect(levelCard.GetComponent<RectTransform>(), new Vector2(0.31f, 0.79f), new Vector2(0.69f, 0.855f));
            levelText = CreateText("Level", levelCard.transform, 42, TextAlignmentOptions.Center, config.uiNavy);
            levelText.fontStyle = FontStyles.Bold;
            Stretch(levelText.rectTransform);
            energyText = CreateText("Energy", root, 27, TextAlignmentOptions.Right, config.uiNavy);
            energyText.fontStyle = FontStyles.Bold;
            SetRect(energyText.rectTransform, new Vector2(0.63f, 0.79f), new Vector2(0.94f, 0.855f));
            timerText = CreateText("Timer", root, 27, TextAlignmentOptions.Left, config.uiNavy);
            timerText.fontStyle = FontStyles.Bold;
            SetRect(timerText.rectTransform, new Vector2(0.06f, 0.79f), new Vector2(0.36f, 0.855f));
            var gameplayCurrency = CreateText("Currency", root, 24, TextAlignmentOptions.Left, config.uiNavy);
            gameplayCurrency.fontStyle = FontStyles.Bold;
            SetRect(gameplayCurrency.rectTransform, new Vector2(0.05f, 0.90f), new Vector2(0.35f, 0.965f));
            currencyTexts.Add(gameplayCurrency);
            var menu = CreateButton("Menu", root, string.Empty, () => session.SetPaused(true));
            menu.GetComponent<Image>().color = config.uiButton;
            SetRect(menu.GetComponent<RectTransform>(), new Vector2(0.84f, 0.91f), new Vector2(0.96f, 0.975f));
            CreateAuthoredButtonIcon(menu, "Pause Icon", UIIconGraphic.Symbol.Pause);

            var instructionCard = CreateRoundedPanel("Instruction Card", root, config.uiCream);
            SetRect(instructionCard.GetComponent<RectTransform>(), new Vector2(0.06f, 0.145f), new Vector2(0.94f, 0.20f));
            tutorialText = CreateText("Tutorial Prompt", instructionCard.transform, 31, TextAlignmentOptions.Center, config.uiNavy);
            tutorialText.fontStyle = FontStyles.Bold;
            tutorialText.rectTransform.anchorMin = new Vector2(0.06f, 0f);
            tutorialText.rectTransform.anchorMax = new Vector2(0.94f, 1f);
            tutorialText.rectTransform.offsetMin = tutorialText.rectTransform.offsetMax = Vector2.zero;

            var undo = CreateButton("Undo", root, strings.undo, () => session.Undo());
            SetRect(undo.GetComponent<RectTransform>(), new Vector2(0.05f, 0.075f), new Vector2(0.28f, 0.14f));
            var restart = CreateButton("Restart", root, strings.restart, () => session.Restart());
            SetRect(restart.GetComponent<RectTransform>(), new Vector2(0.30f, 0.075f), new Vector2(0.56f, 0.14f));
            var hint = CreateButton("Hint", root, strings.hint, () => session.RequestHint());
            SetRect(hint.GetComponent<RectTransform>(), new Vector2(0.58f, 0.075f), new Vector2(0.95f, 0.14f));
            hintText = hint.GetComponentInChildren<TMP_Text>();

            modeButton = CreateButton("Force Mode", root, strings.pull, () => session.ToggleMode());
            SetRect(modeButton.GetComponent<RectTransform>(), new Vector2(0.35f, 0.205f), new Vector2(0.65f, 0.255f));
            modeText = modeButton.GetComponentInChildren<TMP_Text>();

            echoButton = CreateButton("Echo", root, "ECHO", () => session.ToggleEcho());
            SetRect(echoButton.GetComponent<RectTransform>(), new Vector2(0.67f, 0.205f), new Vector2(0.95f, 0.255f));
            echoText = echoButton.GetComponentInChildren<TMP_Text>();
            echoText.fontSize = 24;

            signatureStatusText = CreateText("Signature Status", root, 21, TextAlignmentOptions.Center, config.uiNavy);
            signatureStatusText.fontStyle = FontStyles.Bold;
            SetRect(signatureStatusText.rectTransform, new Vector2(0.08f, 0.735f), new Vector2(0.92f, 0.78f));
            signatureStatusText.gameObject.SetActive(false);

            dragGuide = CreateRoundedPanel("Drag Guide", root, new Color(config.uiOrange.r, config.uiOrange.g, config.uiOrange.b, 0.2f));
            SetRect(dragGuide.GetComponent<RectTransform>(), new Vector2(0.31f, 0.30f), new Vector2(0.69f, 0.355f));
            var dragText = CreateText("Guide Text", dragGuide.transform, 25, TextAlignmentOptions.Center, config.uiNavy);
            Stretch(dragText.rectTransform);
            dragText.text = "DRAG AROUND THE BOX";
        }

        private void BuildSignIn()
        {
            var config = session.Config;
            signInPanel = CreatePanel("Sign In Screen", safeRoot, config.uiBackgroundBottom);
            screens.Add(signInPanel);
            var root = signInPanel.GetComponent<RectTransform>();

            var eyebrow = CreateText("Eyebrow", root, 22, TextAlignmentOptions.Center, config.uiPrimary);
            eyebrow.fontStyle = FontStyles.Bold;
            eyebrow.text = "WELCOME TO";
            SetRect(eyebrow.rectTransform, new Vector2(0.18f, 0.79f), new Vector2(0.82f, 0.84f));

            var title = CreateText("Title", root, 78, TextAlignmentOptions.Center, config.uiTextPrimary);
            title.fontStyle = FontStyles.Bold;
            title.richText = true;
            title.text = $"<color=#{ColorUtility.ToHtmlStringRGB(config.uiTextPrimary)}>OUTSIDE</color>\n" +
                         $"<color=#{ColorUtility.ToHtmlStringRGB(config.uiPrimary)}>THE BOX</color>";
            title.lineSpacing = 0.7f;
            SetRect(title.rectTransform, new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.79f));

            var body = CreateText("Body", root, 27, TextAlignmentOptions.Center, config.uiTextMuted);
            body.text = strings.signInTitle;
            SetRect(body.rectTransform, new Vector2(0.10f, 0.54f), new Vector2(0.90f, 0.60f));

            googleSignInButton = CreateButton("Google Play Sign In", root, strings.signInGooglePlay,
                () => session.SignInGooglePlay());
            SetRect(googleSignInButton.GetComponent<RectTransform>(), new Vector2(0.13f, 0.44f), new Vector2(0.87f, 0.52f));

            guestSignInButton = CreateButton("Guest Sign In", root, strings.unityCloud,
                OpenUsernameFromSignIn);
            SetRect(guestSignInButton.GetComponent<RectTransform>(), new Vector2(0.13f, 0.33f), new Vector2(0.87f, 0.41f));

            signInStatusText = CreateText("Status", root, 22, TextAlignmentOptions.Center, config.uiTextMuted);
            signInStatusText.fontStyle = FontStyles.Bold;
            signInStatusText.text = strings.signInStatus;
            SetRect(signInStatusText.rectTransform, new Vector2(0.10f, 0.17f), new Vector2(0.90f, 0.22f));

            var warning = CreateText("Guest Warning", root, 20, TextAlignmentOptions.Center, config.uiTextMuted);
            warning.text = strings.guestWarning;
            SetRect(warning.rectTransform, new Vector2(0.10f, 0.075f), new Vector2(0.90f, 0.15f));
        }

        private void BuildAccountPanel()
        {
            var config = session.Config;
            accountPanel = CreatePanel("Account Screen", safeRoot, config.uiBackgroundBottom);
            screens.Add(accountPanel);
            var root = accountPanel.GetComponent<RectTransform>();

            var title = CreateText("Title", root, 54, TextAlignmentOptions.Center, config.uiTextPrimary);
            title.fontStyle = FontStyles.Bold;
            title.text = strings.accountTitle;
            SetRect(title.rectTransform, new Vector2(0.12f, 0.81f), new Vector2(0.88f, 0.89f));

            var body = CreateText("Body", root, 24, TextAlignmentOptions.Center, config.uiTextMuted);
            body.text = strings.accountBody;
            SetRect(body.rectTransform, new Vector2(0.10f, 0.72f), new Vector2(0.90f, 0.80f));

            accountUsernameInput = CreateInputField("Username", root, strings.usernamePlaceholder, false, 20);
            SetRect(accountUsernameInput.GetComponent<RectTransform>(), new Vector2(0.13f, 0.61f), new Vector2(0.87f, 0.68f));
            accountPasswordInput = CreateInputField("Password", root, strings.passwordPlaceholder, true, 30);
            SetRect(accountPasswordInput.GetComponent<RectTransform>(), new Vector2(0.13f, 0.51f), new Vector2(0.87f, 0.58f));
            accountConfirmInput = CreateInputField("Confirm Password", root, strings.confirmPasswordPlaceholder, true, 30);
            SetRect(accountConfirmInput.GetComponent<RectTransform>(), new Vector2(0.13f, 0.41f), new Vector2(0.87f, 0.48f));

            accountSignInButton = CreateButton("Sign In", root, strings.signInUsername, SubmitUsernameSignIn);
            SetRect(accountSignInButton.GetComponent<RectTransform>(), new Vector2(0.13f, 0.30f), new Vector2(0.87f, 0.38f));
            accountCreateButton = CreateButton("Create Account", root, strings.createAccount, SubmitUsernameCreate);
            SetRect(accountCreateButton.GetComponent<RectTransform>(), new Vector2(0.13f, 0.21f), new Vector2(0.87f, 0.29f));

            accountGoogleButton = CreateButton("Google Play", root, "CONNECT GOOGLE PLAY", () => session.SignInGooglePlay());
            SetRect(accountGoogleButton.GetComponent<RectTransform>(), new Vector2(0.13f, 0.21f), new Vector2(0.87f, 0.29f));
            accountLogoutButton = CreateButton("Log Out", root, "LOG OUT / SWITCH ACCOUNT", () => session.SignOutPlayer());
            SetRect(accountLogoutButton.GetComponent<RectTransform>(), new Vector2(0.13f, 0.115f), new Vector2(0.87f, 0.195f));

            accountStatusText = CreateText("Status", root, 20, TextAlignmentOptions.Center, config.uiTextMuted);
            accountStatusText.fontStyle = FontStyles.Bold;
            accountStatusText.text = strings.accountRules;
            SetRect(accountStatusText.rectTransform, new Vector2(0.10f, 0.025f), new Vector2(0.90f, 0.095f));
        }

        private void BuildMainMenu()
        {
            var config = session.Config;
            mainMenuPanel = CreatePanel("Main Menu", safeRoot, config.uiBackgroundBottom);
            screens.Add(mainMenuPanel);
            var root = mainMenuPanel.GetComponent<RectTransform>();
            var currencyChip = CreateRoundedPanel("Currency Chip", root, config.uiSurfaceBright);
            SetRect(currencyChip.GetComponent<RectTransform>(), new Vector2(0.62f, 0.91f), new Vector2(0.94f, 0.965f));
            currencyChip.GetComponent<Image>().raycastTarget = false;
            AddSoftShadow(currencyChip, 0.25f, new Vector2(0f, -4f));
            var mainCurrency = CreateText("Currency", root, 27, TextAlignmentOptions.Center, config.uiTextPrimary);
            mainCurrency.fontStyle = FontStyles.Bold;
            SetRect(mainCurrency.rectTransform, new Vector2(0.63f, 0.91f), new Vector2(0.93f, 0.965f));
            currencyTexts.Add(mainCurrency);

            var eyebrow = CreateText("Eyebrow", root, 22, TextAlignmentOptions.Center, config.uiPrimary);
            eyebrow.fontStyle = FontStyles.Bold;
            eyebrow.text = "A MAGNETIC PUZZLE";
            SetRect(eyebrow.rectTransform, new Vector2(0.22f, 0.865f), new Vector2(0.78f, 0.905f));

            var title = CreateText("Title", root, 86, TextAlignmentOptions.Center, config.uiTextPrimary);
            title.fontStyle = FontStyles.Bold;
            title.richText = true;
            title.text = $"<color=#{ColorUtility.ToHtmlStringRGB(config.uiTextPrimary)}>OUTSIDE</color>\n" +
                         $"<color=#{ColorUtility.ToHtmlStringRGB(config.uiPrimary)}>THE BOX</color>";
            title.lineSpacing = 0.68f;
            SetRect(title.rectTransform, new Vector2(0.08f, 0.68f), new Vector2(0.92f, 0.86f));

            var subtitle = CreateText("Subtitle", root, 27, TextAlignmentOptions.Center, config.uiTextMuted);
            subtitle.text = "BEND THE FIELD  •  FIND THE PATH";
            SetRect(subtitle.rectTransform, new Vector2(0.10f, 0.615f), new Vector2(0.90f, 0.665f));

            var play = CreateButton("Play", root, strings.play, () => session.StartFromMainMenu());
            play.GetComponentInChildren<TMP_Text>().fontSize = 34;
            SetRect(play.GetComponent<RectTransform>(), new Vector2(0.16f, 0.485f), new Vector2(0.84f, 0.555f));
            var daily = CreateButton("Daily Challenge", root, strings.dailyChallenge, () => session.StartDailyChallenge());
            SetRect(daily.GetComponent<RectTransform>(), new Vector2(0.16f, 0.40f), new Vector2(0.84f, 0.47f));
            var chapters = CreateButton("Chapters", root, strings.chapters, OpenChaptersFromMainMenu);
            SetRect(chapters.GetComponent<RectTransform>(), new Vector2(0.16f, 0.315f), new Vector2(0.84f, 0.385f));
            var leaderboard = CreateButton("Leaderboard", root, strings.leaderboard, () => session.ShowLeaderboard());
            SetRect(leaderboard.GetComponent<RectTransform>(), new Vector2(0.16f, 0.23f), new Vector2(0.84f, 0.30f));
            leaderboard.GetComponentInChildren<TMP_Text>().fontSize = 22;
            var settings = CreateButton("Settings", root, strings.settings, OpenSettingsFromMainMenu);
            SetRect(settings.GetComponent<RectTransform>(), new Vector2(0.16f, 0.145f), new Vector2(0.49f, 0.215f));
            var freeCoins = CreateButton("Free Coins", root, strings.freeCoins, () => session.WatchAdForGems());
            SetRect(freeCoins.GetComponent<RectTransform>(), new Vector2(0.51f, 0.145f), new Vector2(0.84f, 0.215f));
            freeCoins.GetComponentInChildren<TMP_Text>().fontSize = 24;
            var account = CreateButton("Account", root, strings.secureAccount, OpenAccountFromMainMenu);
            SetRect(account.GetComponent<RectTransform>(), new Vector2(0.16f, 0.07f), new Vector2(0.49f, 0.13f));
            account.GetComponentInChildren<TMP_Text>().fontSize = 22;
            var store = CreateButton("Store", root, "STORE", ShowStore);
            SetRect(store.GetComponent<RectTransform>(), new Vector2(0.51f, 0.07f), new Vector2(0.84f, 0.13f));
            store.GetComponentInChildren<TMP_Text>().fontSize = 22;

            var version = CreateText("Version", root, 21, TextAlignmentOptions.Center, config.uiTextMuted);
            version.text = $"v{Application.version}";
            SetRect(version.rectTransform, new Vector2(0.2f, 0.015f), new Vector2(0.8f, 0.05f));
        }

        private void BuildLeaderboardPanel()
        {
            var config = session.Config;
            leaderboardPanel = CreatePanel("Leaderboard Screen", safeRoot, config.uiBackgroundBottom);
            screens.Add(leaderboardPanel);
            var root = leaderboardPanel.GetComponent<RectTransform>();

            var title = CreateText("Title", root, 48, TextAlignmentOptions.Center, config.uiTextPrimary);
            title.fontStyle = FontStyles.Bold;
            title.text = strings.leaderboard;
            SetRect(title.rectTransform, new Vector2(0.16f, 0.88f), new Vector2(0.84f, 0.94f));

            leaderboardScopeDropdownButton = CreateButton("Scope Dropdown", root,
                "ALL-TIME PERFORMANCE", ToggleLeaderboardDropdown);
            SetRect(leaderboardScopeDropdownButton.GetComponent<RectTransform>(), new Vector2(0.14f, 0.79f), new Vector2(0.86f, 0.86f));
            leaderboardScopeText = leaderboardScopeDropdownButton.GetComponentInChildren<TMP_Text>(true);
            leaderboardScopeText.margin = new Vector4(16f, 0f, 64f, 0f);
            CreateAuthoredButtonIcon(leaderboardScopeDropdownButton, "Dropdown Icon", UIIconGraphic.Symbol.ChevronDown);
            leaderboardScopeText.gameObject.name = "Label";
            leaderboardScopeText.fontSize = 26;

            var legend = CreateText("Legend", root, 20, TextAlignmentOptions.Center, config.uiTextMuted);
            legend.text = "RANKED 70% TIME + 30% ENERGY  •  BEST RUNS";
            SetRect(legend.rectTransform, new Vector2(0.08f, 0.735f), new Vector2(0.92f, 0.78f));

            var resultsSurface = CreateRoundedPanel("Results Surface", root, config.uiSurface);
            SetRect(resultsSurface.GetComponent<RectTransform>(), new Vector2(0.07f, 0.10f), new Vector2(0.93f, 0.72f));
            resultsSurface.GetComponent<Image>().raycastTarget = false;
            leaderboardBodyText = CreateText("Results", resultsSurface.transform, 23, TextAlignmentOptions.TopLeft,
                config.uiTextPrimary);
            leaderboardBodyText.fontStyle = FontStyles.Bold;
            leaderboardBodyText.text = "LOADING SCORES...";
            leaderboardBodyText.lineSpacing = 1.12f;
            SetRect(leaderboardBodyText.rectTransform, new Vector2(0.055f, 0.04f), new Vector2(0.945f, 0.96f));

            leaderboardScopeDropdownMenu = CreateRoundedPanel("Scope Dropdown Menu", root, config.uiSurfaceBright);
            SetRect(leaderboardScopeDropdownMenu.GetComponent<RectTransform>(), new Vector2(0.14f, 0.335f), new Vector2(0.86f, 0.785f));
            AddSoftShadow(leaderboardScopeDropdownMenu, 0.45f, new Vector2(0f, -6f));
            var optionCount = Mathf.Max(1, session.Config.ChapterCount + 1);
            var rowHeight = 0.88f / optionCount;
            for (var option = 0; option < optionCount; option++)
            {
                var chapterIndex = option - 1;
                var optionName = option == 0 ? "All-Time" : $"Chapter {option:00}";
                var optionLabel = option == 0 ? "ALL-TIME PERFORMANCE" : $"CHAPTER {option:00} PERFORMANCE";
                var item = CreateButton(optionName, leaderboardScopeDropdownMenu.transform, optionLabel,
                    () => SelectLeaderboardDropdownScope(chapterIndex));
                var top = 0.94f - option * rowHeight;
                SetRect(item.GetComponent<RectTransform>(), new Vector2(0.035f, top - rowHeight + 0.008f),
                    new Vector2(0.965f, top));
                item.GetComponentInChildren<TMP_Text>(true).fontSize = 22;
            }
            leaderboardScopeDropdownMenu.SetActive(false);
        }

        private void BuildTimeout()
        {
            timeoutPanel = CreateOverlay("Time Out", safeRoot);
            screens.Add(timeoutPanel);
            var title = CreateText("Title", timeoutPanel.transform, 64, TextAlignmentOptions.Center, session.Config.uiDanger);
            title.fontStyle = FontStyles.Bold;
            title.text = strings.timeUp;
            SetRect(title.rectTransform, new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.82f));
            var details = CreateText("Details", timeoutPanel.transform, 30, TextAlignmentOptions.Center, session.Config.uiTextPrimary);
            SetRect(details.rectTransform, new Vector2(0.08f, 0.61f), new Vector2(0.92f, 0.70f));
            var timeoutCurrency = CreateText("Currency", timeoutPanel.transform, 28, TextAlignmentOptions.Center, session.Config.uiCream);
            SetRect(timeoutCurrency.rectTransform, new Vector2(0.2f, 0.55f), new Vector2(0.8f, 0.61f));
            currencyTexts.Add(timeoutCurrency);
            var ad = CreateButton("Watch Ad", timeoutPanel.transform, strings.watchAd, () => session.ReviveWithRewardedAd());
            SetRect(ad.GetComponent<RectTransform>(), new Vector2(0.16f, 0.43f), new Vector2(0.84f, 0.51f));
            var coins = CreateButton("Use Coins", timeoutPanel.transform, strings.useCoins, () => session.ReviveWithCoins());
            SetRect(coins.GetComponent<RectTransform>(), new Vector2(0.16f, 0.33f), new Vector2(0.84f, 0.41f));
            var restart = CreateButton("Restart", timeoutPanel.transform, strings.restart, () => session.Restart());
            SetRect(restart.GetComponent<RectTransform>(), new Vector2(0.16f, 0.23f), new Vector2(0.84f, 0.31f));
            var menu = CreateButton("Main Menu", timeoutPanel.transform, strings.mainMenu, () => session.OpenMainMenu());
            SetRect(menu.GetComponent<RectTransform>(), new Vector2(0.16f, 0.13f), new Vector2(0.84f, 0.21f));
        }

        private void RefreshDailyButton()
        {
            var button = FindComponent<Button>(safeRoot, "Main Menu/Daily Challenge");
            var label = button?.GetComponentInChildren<TMP_Text>(true);
            if (button == null || label == null) return;
            button.gameObject.SetActive(session.Config.dailyChallengeEnabled);
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var completed = SaveService.Data?.lastDailyCompleted == today;
            label.text = completed
                ? $"{strings.dailyChallenge}  ✓   STREAK {SaveService.Data.dailyStreak}"
                : strings.dailyChallenge;
        }

        private void RefreshResumeButton()
        {
            var button = FindComponent<Button>(safeRoot, "Main Menu/Play");
            if (button == null) return;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null) return;
            var savedLevel = Mathf.Clamp(SaveService.Data?.lastPlayedLevel ?? 1, 1,
                Mathf.Max(1, session?.Catalog?.Count ?? 1));
            label.text = $"{strings.play}  •  LEVEL {savedLevel:00}";
        }

        private void BuildWin()
        {
            winPanel = CreateOverlay("Win Screen", safeRoot);
            screens.Add(winPanel);
            winTitle = CreateText("Result", winPanel.transform, 54, TextAlignmentOptions.Center, session.Config.uiSuccess);
            SetRect(winTitle.rectTransform, new Vector2(0.08f, 0.57f), new Vector2(0.92f, 0.78f));
            winStats = CreateText("Stats", winPanel.transform, 25, TextAlignmentOptions.Center, session.Config.uiTextPrimary);
            SetRect(winStats.rectTransform, new Vector2(0.1f, 0.425f), new Vector2(0.9f, 0.565f));
            var next = CreateButton("Next", winPanel.transform, strings.next, () => session.CompleteNext());
            SetRect(next.GetComponent<RectTransform>(), new Vector2(0.2f, 0.32f), new Vector2(0.8f, 0.41f));
            var replay = CreateButton("Replay", winPanel.transform, strings.replay, () => session.Restart());
            SetRect(replay.GetComponent<RectTransform>(), new Vector2(0.2f, 0.21f), new Vector2(0.8f, 0.29f));
            var levels = CreateButton("Levels", winPanel.transform, strings.levels, OpenChaptersFromGameplay);
            SetRect(levels.GetComponent<RectTransform>(), new Vector2(0.2f, 0.1f), new Vector2(0.8f, 0.18f));
        }

        // Only used by the edit-mode prefab authoring path. Player builds use
        // the icon children already saved in GameplayHUD and LeaderboardPanel.
        private void CreateAuthoredButtonIcon(Button button, string name, UIIconGraphic.Symbol symbol)
        {
            var item = prefabs.Spawn(prefabs.uiRectPrefab, name, button.transform);
            var rect = item.GetComponent<RectTransform>();
            var icon = item.AddComponent<UIIconGraphic>();
            icon.symbol = symbol;
            icon.color = session.Config.uiTextPrimary;
            icon.raycastTarget = false;
            rect.anchorMin = rect.anchorMax = symbol == UIIconGraphic.Symbol.Pause
                ? new Vector2(0.5f, 0.5f) : new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = symbol == UIIconGraphic.Symbol.Pause ? Vector2.zero : new Vector2(-28f, 0f);
            rect.sizeDelta = symbol == UIIconGraphic.Symbol.Pause ? new Vector2(44f, 44f) : new Vector2(32f, 24f);
        }

        private void BuildPause()
        {
            pausePanel = CreateOverlay("Pause Sheet", safeRoot);
            var title = CreateText("Pause Title", pausePanel.transform, 54, TextAlignmentOptions.Center, session.Config.uiTextPrimary);
            title.text = strings.paused;
            SetRect(title.rectTransform, new Vector2(0.1f, 0.68f), new Vector2(0.9f, 0.82f));
            var resume = CreateButton("Resume", pausePanel.transform, strings.resume, () => session.SetPaused(false));
            SetRect(resume.GetComponent<RectTransform>(), new Vector2(0.2f, 0.52f), new Vector2(0.8f, 0.61f));
            var levels = CreateButton("Levels", pausePanel.transform, strings.levels, OpenChaptersFromGameplay);
            SetRect(levels.GetComponent<RectTransform>(), new Vector2(0.2f, 0.41f), new Vector2(0.8f, 0.50f));
            var settings = CreateButton("Settings", pausePanel.transform, strings.settings, OpenSettingsFromGameplay);
            SetRect(settings.GetComponent<RectTransform>(), new Vector2(0.2f, 0.30f), new Vector2(0.8f, 0.39f));
            var mainMenu = CreateButton("Main Menu", pausePanel.transform, strings.mainMenu, () => session.OpenMainMenu());
            SetRect(mainMenu.GetComponent<RectTransform>(), new Vector2(0.2f, 0.19f), new Vector2(0.8f, 0.28f));
            pausePanel.SetActive(false);
        }

        private void BuildChapterSelect()
        {
            chapterSelectPanel = CreateOverlay("Chapter Select", safeRoot);
            screens.Add(chapterSelectPanel);
            var title = CreateText("Chapter Title", chapterSelectPanel.transform, 52, TextAlignmentOptions.Center, Color.white);
            title.text = "CHAPTERS";
            SetRect(title.rectTransform, new Vector2(0.18f, 0.89f), new Vector2(0.82f, 0.97f));
            var back = CreateButton("Back", chapterSelectPanel.transform, "<", ReturnFromSubMenu);
            SetRect(back.GetComponent<RectTransform>(), new Vector2(0.04f, 0.9f), new Vector2(0.16f, 0.97f));
            var cards = CreateRect("Chapter Cards", chapterSelectPanel.transform);
            SetRect(cards, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.87f));
        }

        private void RebuildChapterButtons()
        {
            var cards = chapterSelectPanel.transform.Find("Chapter Cards");
            if (cards == null) return;
            for (var i = cards.childCount - 1; i >= 0; i--) Destroy(cards.GetChild(i).gameObject);
            var config = session.Config;
            var levelsPerChapter = Mathf.Max(1, config.levelsPerChapter);
            var curatedChapterCount = Mathf.CeilToInt(session.Catalog.Count / (float)levelsPerChapter);
            var chapterCount = curatedChapterCount + (config.endlessEnabled ? 1 : 0);
            var unlocked = SaveService.GetHighestUnlockedLevel(config, session.Catalog.Count);
            for (var i = 0; i < chapterCount; i++)
            {
                var chapterIndex = i;
                var isEndless = i >= curatedChapterCount;
                if (isEndless)
                {
                    var endlessSettings = config.endlessChapter;
                    var endlessUnlocked = SaveService.IsEndlessUnlocked(config);
                    var best = SaveService.Data.bestEndlessLevel;
                    var endlessLabel = $"CHAPTER {i + 1}  {endlessSettings.title}\n" +
                                       (endlessUnlocked ? $"{endlessSettings.challenge}  |  {strings.best} {best}" : strings.locked);
                    var endlessButton = CreateButton($"Chapter {i + 1}", cards, endlessLabel,
                        () => { if (endlessUnlocked) session.StartEndless(); });
                    SetButtonFill(endlessButton, Color.Lerp(config.uiButton,
                        endlessSettings.backgroundAccent, 0.42f));
                    endlessButton.interactable = endlessUnlocked;
                    endlessButton.GetComponentInChildren<TMP_Text>().fontSize = 29;
                    var endlessRowTop = 1f - i / (float)chapterCount;
                    var endlessRowBottom = 1f - (i + 1) / (float)chapterCount;
                    SetRect(endlessButton.GetComponent<RectTransform>(),
                        new Vector2(0f, endlessRowBottom + 0.008f), new Vector2(1f, endlessRowTop - 0.008f));
                    continue;
                }
                var firstLevel = i * levelsPerChapter + 1;
                var lastLevel = Mathf.Min(session.Catalog.Count, firstLevel + levelsPerChapter - 1);
                var settings = config.GetChapter(firstLevel);
                var title = settings?.title ?? $"Chapter {i + 1}";
                var challenge = settings?.challenge ?? "Guide the ball";
                var isUnlocked = unlocked >= firstLevel;
                var label = $"CHAPTER {i + 1}  {title}\n{firstLevel:00}-{lastLevel:00}  |  {(isUnlocked ? challenge : "LOCKED")}";
                var button = CreateButton($"Chapter {i + 1}", cards, label,
                    () => ShowLevelSelect(chapterIndex));
                if (settings != null)
                    SetButtonFill(button, Color.Lerp(config.uiButton, settings.backgroundColorB, 0.34f));
                button.interactable = isUnlocked;
                button.GetComponentInChildren<TMP_Text>().fontSize = 29;
                var rowTop = 1f - i / (float)chapterCount;
                var rowBottom = 1f - (i + 1) / (float)chapterCount;
                SetRect(button.GetComponent<RectTransform>(),
                    new Vector2(0f, rowBottom + 0.008f), new Vector2(1f, rowTop - 0.008f));
            }
        }

        private void BuildLevelSelect()
        {
            levelSelectPanel = CreateOverlay("Level Select", safeRoot);
            screens.Add(levelSelectPanel);
            levelSelectTitle = CreateText("Title", levelSelectPanel.transform, 48, TextAlignmentOptions.Center, Color.white);
            levelSelectTitle.text = strings.levels;
            SetRect(levelSelectTitle.rectTransform, new Vector2(0.1f, 0.9f), new Vector2(0.9f, 0.98f));
            var back = CreateButton("Back", levelSelectPanel.transform, "‹", () => ShowChapterSelect());
            SetRect(back.GetComponent<RectTransform>(), new Vector2(0.04f, 0.91f), new Vector2(0.16f, 0.975f));
            var scrollGo = prefabs.Spawn(prefabs.uiScrollPrefab, "Level Scroll", levelSelectPanel.transform);
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            if (scrollRect == null)
                throw new InvalidOperationException("UI Scroll prefab requires a RectTransform.");
            SetRect(scrollRect, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.88f));
            scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.16f);
            var content = scrollGo.transform.Find("Level Button Content") as RectTransform;
            if (content == null)
                throw new InvalidOperationException("UI Scroll prefab requires a Level Button Content child.");
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = new Vector2(0, -650); content.offsetMax = Vector2.zero;
            var grid = content.GetComponent<GridLayoutGroup>();
            if (grid == null)
                throw new InvalidOperationException("Level Button Content prefab child requires a GridLayoutGroup.");
            grid.cellSize = new Vector2(260, 118); grid.spacing = new Vector2(26, 24); grid.padding = new RectOffset(28, 28, 28, 28);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;
            scrollGo.GetComponent<ScrollRect>().content = content;
            scrollGo.GetComponent<ScrollRect>().horizontal = false;
            scrollGo.GetComponent<ScrollRect>().movementType = ScrollRect.MovementType.Clamped;
        }

        private void RebuildLevelButtons()
        {
            var content = levelSelectPanel.transform.Find("Level Scroll/Level Button Content");
            if (content == null) return;
            for (var i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
            var levelsPerChapter = Mathf.Max(1, session.Config.levelsPerChapter);
            var firstIndex = selectedChapterIndex * levelsPerChapter;
            var lastIndex = Mathf.Min(session.Catalog.Count, firstIndex + levelsPerChapter);
            var settings = session.Config.GetChapter(firstIndex + 1);
            levelSelectTitle.text = settings != null ? settings.title.ToUpperInvariant() : $"CHAPTER {selectedChapterIndex + 1}";
            var unlocked = SaveService.GetHighestUnlockedLevel(session.Config, session.Catalog.Count);
            for (var i = firstIndex; i < lastIndex; i++)
            {
                var index = i;
                var isUnlocked = i + 1 <= unlocked;
                var stars = i < SaveService.Data.bestStars.Count ? SaveService.Data.bestStars[i] : 0;
                var label = isUnlocked ? $"{i + 1}" : $"{i + 1}\nLOCKED";
                var button = CreateButton($"Level {i + 1}", content, label, () => { if (isUnlocked) session.LoadLevel(index); });
                button.interactable = isUnlocked;
                if (settings != null && isUnlocked)
                    SetButtonFill(button, Color.Lerp(session.Config.uiButton, settings.backgroundAccent, 0.22f));
                var number = button.GetComponentInChildren<TMP_Text>();
                number.fontSize = 32;
                if (isUnlocked)
                {
                    SetRect(number.rectTransform, new Vector2(0.05f, 0.43f), new Vector2(0.95f, 0.98f));
                    for (var starIndex = 0; starIndex < 3; starIndex++)
                    {
                        var star = CreatePanel($"Rating Star {starIndex + 1}", button.transform, Color.white);
                        ConfigureStarImage(star.GetComponent<Image>(), starIndex < Mathf.Clamp(stars, 0, 3));
                        var center = 0.32f + starIndex * 0.18f;
                        SetRect(star.GetComponent<RectTransform>(), new Vector2(center - 0.065f, 0.08f),
                            new Vector2(center + 0.065f, 0.39f));
                    }
                }
            }
        }

        private void ConfigureStarImage(Image image, bool earned)
        {
            if (victoryStarSprite == null)
            {
                victoryStarSprite = Resources.Load<Sprite>("OutsideTheBox/Art/UI/Star");
                // A stale texture import must never turn the rating into colored squares.
                if (victoryStarSprite == null)
                {
                    var texture = Resources.Load<Texture2D>("OutsideTheBox/Art/UI/Star");
                    if (texture != null)
                        victoryStarSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                            Vector2.one * 0.5f, texture.width);
                }
            }
            image.overrideSprite = null;
            image.sprite = victoryStarSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = victoryStarSprite != null;
            image.color = earned ? session.Config.uiOrange : session.Config.uiTextMuted * 0.55f;
            var rectangle = image.GetComponent<UIRoundRectGraphic>();
            if (rectangle != null) rectangle.enabled = false;
        }

        private void BuildSettingsPanel()
        {
            settingsPanel = CreateOverlay("Settings Screen", safeRoot);
            screens.Add(settingsPanel);
            var title = CreateText("Title", settingsPanel.transform, 48, TextAlignmentOptions.Center, Color.white);
            title.text = strings.settings;
            SetRect(title.rectTransform, new Vector2(0.1f, 0.86f), new Vector2(0.9f, 0.96f));
            var back = CreateButton("Back", settingsPanel.transform, "‹", ReturnFromSubMenu);
            SetRect(back.GetComponent<RectTransform>(), new Vector2(0.04f, 0.88f), new Vector2(0.16f, 0.95f));
            var privacy = CreateButton("Privacy", settingsPanel.transform, strings.privacy, OpenPrivacyPolicy);
            SetRect(privacy.GetComponent<RectTransform>(), new Vector2(0.20f, 0.045f), new Vector2(0.80f, 0.105f));
            deleteAccountButton = CreateButton("Delete Account", settingsPanel.transform, "DELETE ACCOUNT", ConfirmDeleteAccount);
            SetRect(deleteAccountButton.GetComponent<RectTransform>(), new Vector2(0.20f, 0.115f), new Vector2(0.80f, 0.175f));
            SetButtonFill(deleteAccountButton, session.Config.uiDanger);
        }

        private void ConfirmDeleteAccount()
        {
            if (!deleteAccountArmed)
            {
                deleteAccountArmed = true;
                var label = deleteAccountButton?.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = "TAP AGAIN TO DELETE PERMANENTLY";
                ShowToast("THIS DELETES CLOUD PROGRESS AND THE PLAYER ACCOUNT");
                return;
            }
            deleteAccountArmed = false;
            session?.DeletePlayerAccount();
        }

        public void SetDeleteAccountBusy(bool busy)
        {
            if (deleteAccountButton == null) return;
            deleteAccountButton.interactable = !busy;
            var label = deleteAccountButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = busy ? "DELETING ACCOUNT..." : "DELETE ACCOUNT";
            if (!busy) deleteAccountArmed = false;
        }

        private void OpenPrivacyPolicy()
        {
            if (GoogleUmpConsent.PrivacyOptionsRequired)
            {
                LiveOpsManager.Instance?.Ads?.SetBannerVisible(false);
                GoogleUmpConsent.ShowPrivacyOptions(success =>
                {
                    if (!success) ShowToast("Ad privacy choices are unavailable right now");
                    LiveOpsManager.Instance?.Ads?.SetBannerVisible(true);
                });
                return;
            }
            var url = session.Config.privacyPolicyUrl;
            if (string.IsNullOrWhiteSpace(url))
            {
                ShowToast("Privacy policy URL is not configured yet");
                return;
            }
            Application.OpenURL(url);
        }

        private void RebuildSettings()
        {
            var old = settingsPanel.transform.Find("Dynamic Settings");
            if (old != null)
            {
                old.gameObject.SetActive(false);
                Destroy(old.gameObject);
            }
            var root = CreateRect("Dynamic Settings", settingsPanel.transform);
            SetRect(root, new Vector2(0.12f, 0.19f), new Vector2(0.88f, 0.84f));
            AddToggle(root, strings.music, () => SaveService.Data.musicEnabled, v => SaveService.Data.musicEnabled = v, 0.91f);
            AddToggle(root, strings.sound, () => SaveService.Data.soundEnabled, v => SaveService.Data.soundEnabled = v, 0.82f);
            AddToggle(root, strings.vibration, () => SaveService.Data.vibrationEnabled, v => SaveService.Data.vibrationEnabled = v, 0.73f);
            AddToggle(root, strings.reduceMotion, () => SaveService.Data.reduceMotion, v => SaveService.Data.reduceMotion = v, 0.64f);
            AddToggle(root, strings.colorBlind, () => SaveService.Data.colorBlindMode, v => SaveService.Data.colorBlindMode = v, 0.55f);
            AddToggle(root, strings.largeText, () => SaveService.Data.largeText, v => SaveService.Data.largeText = v, 0.46f);
            AddSelector(root, strings.performance, GetPerformanceValue,
                () => AdjustPerformanceMode(-1), () => AdjustPerformanceMode(1), 0.37f,
                () => true, () => true);
            var ballStyles = GetOwnedSkinIndices(true).Count;
            AddSelector(root, strings.ballStyle, () => GetSkinValue(true),
                () => AdjustSkin(true, -1), () => AdjustSkin(true, 1), 0.28f,
                () => ballStyles > 1, () => ballStyles > 1);
            var magnetStyles = GetOwnedSkinIndices(false).Count;
            AddSelector(root, strings.magnetStyle, () => GetSkinValue(false),
                () => AdjustSkin(false, -1), () => AdjustSkin(false, 1), 0.19f,
                () => magnetStyles > 1, () => magnetStyles > 1);
            AddToggle(root, "NOTIFICATIONS", () => SaveService.Data.notificationsEnabled, LocalGameNotifications.Enable, 0.10f);
            var version = CreateText("Version", root, 24, TextAlignmentOptions.Center, new Color(1, 1, 1, 0.55f));
            SetRect(version.rectTransform, new Vector2(0, 0), new Vector2(1, 0.07f));
            version.text = $"v{Application.version}";
            ApplyUiScale();
        }

        private string GetPerformanceValue()
        {
            var mode = (PerformanceMode)Mathf.Clamp(SaveService.Data?.performanceMode ?? 0, 0, 2);
            return mode switch
            {
                PerformanceMode.BatterySaver => strings.batterySaver,
                PerformanceMode.Smooth => strings.smooth,
                _ => strings.automatic
            };
        }

        private void AdjustPerformanceMode(int direction)
        {
            SaveService.Data.performanceMode = (SaveService.Data.performanceMode + direction + 3) % 3;
            CommitSettings(false);
        }

        private string GetSkinValue(bool ball)
        {
            var config = session.Config;
            var colors = ball ? config.ballSkinTints : config.magnetSkinTints;
            var selected = ball ? SaveService.Data.selectedBallSkin : SaveService.Data.selectedMagnetSkin;
            if (selected >= 10) return $"CH {selected - 9:00} GIFT";
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, (colors?.Length ?? 1) - 1));
            return $"{selected + 1}/{Mathf.Max(1, colors?.Length ?? 1)}";
        }

        private int GetUnlockedSkinCount(int available)
        {
            if (available <= 0) return 1;
            var thresholds = session.Config.cosmeticStarThresholds;
            var unlocked = 1;
            for (var i = 1; i < available; i++)
            {
                var threshold = thresholds != null && i < thresholds.Length ? thresholds[i] : i * 30;
                if (SaveService.TotalStars >= threshold) unlocked = i + 1;
            }
            return Mathf.Clamp(unlocked, 1, available);
        }

        private void AdjustSkin(bool ball, int direction)
        {
            var owned = GetOwnedSkinIndices(ball);
            if (owned.Count == 0) return;
            var selected = ball ? SaveService.Data.selectedBallSkin : SaveService.Data.selectedMagnetSkin;
            var position = owned.IndexOf(selected);
            if (position < 0) position = 0;
            position = (position + direction + owned.Count) % owned.Count;
            if (ball) SaveService.Data.selectedBallSkin = owned[position];
            else SaveService.Data.selectedMagnetSkin = owned[position];
            CommitSettings(false);
        }

        private List<int> GetOwnedSkinIndices(bool ball)
        {
            var colors = ball ? session.Config.ballSkinTints : session.Config.magnetSkinTints;
            var category = ball ? CosmeticCategory.Ball : CosmeticCategory.Magnet;
            var owned = new List<int>();
            for (var i = 0; i < (colors?.Length ?? 0); i++)
                if (IsCosmeticOwned(category, i)) owned.Add(i);
            foreach (var gift in SaveService.Data.chapterGifts)
                if (gift.claimed && gift.category == (int)category) owned.Add(gift.chapter + 9);
            if (owned.Count == 0) owned.Add(0);
            return owned;
        }

        private void BuildTutorialOverlay()
        {
            // Every tutorial uses the same authored card and transparent backdrop.
            tutorialOverlay = prefabs.Spawn(prefabs.tutorialOverlayPrefab, "Tutorial Overlay", safeRoot);
            Stretch(tutorialOverlay.GetComponent<RectTransform>());
            tutorialTitleText = FindComponent<TMP_Text>(tutorialOverlay.transform, "Card/Title");
            tutorialBodyText = FindComponent<TMP_Text>(tutorialOverlay.transform, "Card/Body");
            tutorialProgressText = FindComponent<TMP_Text>(tutorialOverlay.transform, "Card/Progress");
            tutorialContinueButton = FindComponent<Button>(tutorialOverlay.transform, "Card/Continue");
            tutorialContinueButton.onClick.AddListener(ContinueTutorial);
            tutorialMechanicIcon = null;
            tutorialOverlay.SetActive(false);
        }

        private void BuildTooltip()
        {
            var config = session.Config;
            tooltipPanel = CreateRoundedPanel("Tooltip", safeRoot, config.uiCream);
            tooltipPanel.GetComponent<Image>().raycastTarget = false;
            SetRect(tooltipPanel.GetComponent<RectTransform>(), new Vector2(0.12f, 0.235f), new Vector2(0.88f, 0.315f));
            tooltipText = CreateText("Text", tooltipPanel.transform, 27, TextAlignmentOptions.Center, config.uiNavy);
            tooltipText.fontStyle = FontStyles.Bold;
            SetRect(tooltipText.rectTransform, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));
            toastText = tooltipText;
            tooltipPanel.SetActive(false);
        }

        private void AddToggle(Transform root, string label, Func<bool> getter, Action<bool> setter, float y)
        {
            AddSelector(root, label, () => getter() ? strings.on : strings.off,
                () =>
                {
                    if (!getter()) return;
                    setter(false);
                    CommitSettings(false);
                },
                () =>
                {
                    if (getter()) return;
                    setter(true);
                    CommitSettings(false);
                },
                // Keep both arrows visually stable. Pressing the arrow for the current
                // value is a harmless no-op instead of disabling/enabling its partner.
                y, () => true, () => true);
        }

        private void AddSelector(Transform root, string label, Func<string> valueGetter,
            UnityEngine.Events.UnityAction previousAction, UnityEngine.Events.UnityAction nextAction, float y,
            Func<bool> previousInteractable, Func<bool> nextInteractable)
        {
            var row = CreateSettingRow(root, label, valueGetter(), y);
            var valueText = row.transform.Find("Value")?.GetComponent<TMP_Text>();
            Button previous = null;
            Button next = null;

            void RefreshRow()
            {
                if (valueText != null) valueText.text = valueGetter();
                if (previous != null) previous.interactable = previousInteractable();
                if (next != null) next.interactable = nextInteractable();
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            }

            previous = CreateButton("Previous", row.transform, "\u2039", () =>
            {
                previousAction();
                AnalyticsService.Log("setting_changed", ("setting", label), ("value", valueGetter()));
                RefreshRow();
            });
            SetRect(previous.GetComponent<RectTransform>(), new Vector2(0.53f, 0.12f), new Vector2(0.64f, 0.88f));
            previous.GetComponentInChildren<TMP_Text>(true).fontSize = 38;
            previous.navigation = new Navigation { mode = Navigation.Mode.None };

            next = CreateButton("Next", row.transform, "\u203A", () =>
            {
                nextAction();
                AnalyticsService.Log("setting_changed", ("setting", label), ("value", valueGetter()));
                RefreshRow();
            });
            SetRect(next.GetComponent<RectTransform>(), new Vector2(0.87f, 0.12f), new Vector2(0.98f, 0.88f));
            next.GetComponentInChildren<TMP_Text>(true).fontSize = 38;
            next.navigation = new Navigation { mode = Navigation.Mode.None };
            RefreshRow();
        }

        private GameObject CreateSettingRow(Transform root, string label, string value, float y)
        {
            var config = session.Config;
            var row = CreateRoundedPanel($"{label} Setting", root, config.uiButton);
            SetRect(row.GetComponent<RectTransform>(), new Vector2(0, y), new Vector2(1, y + 0.085f));

            // Only the arrow children receive raycasts. The row, label, value and empty area are inert.
            row.GetComponent<Image>().enabled = false;
            var frameObject = prefabs.Spawn(prefabs.uiRectPrefab, "Setting Frame", row.transform);
            Stretch(frameObject.GetComponent<RectTransform>());
            if (frameObject.GetComponent<CanvasRenderer>() == null)
                frameObject.AddComponent<CanvasRenderer>();
            var frame = frameObject.GetComponent<UIRoundRectGraphic>();
            if (frame == null) frame = frameObject.AddComponent<UIRoundRectGraphic>();
            frame.Configure(config.uiButton,
                config.uiButtonBorder, 4f, 24f);
            if (config.uiButtonGradientsEnabled)
                frame.ConfigureGradient(config.uiButtonGradientBottom, config.uiButtonGradientTop,
                    config.uiButtonGradientHighlight, config.uiButtonGradientHighlightStrength);
            frame.raycastTarget = false;

            var nameText = CreateText("Name", row.transform, 27, TextAlignmentOptions.Left, config.uiTextPrimary);
            nameText.fontStyle = FontStyles.Bold;
            nameText.text = label;
            SetRect(nameText.rectTransform, new Vector2(0.055f, 0.08f), new Vector2(0.52f, 0.92f));

            var valueText = CreateText("Value", row.transform, 24, TextAlignmentOptions.Center, config.uiTextPrimary);
            valueText.fontStyle = FontStyles.Bold;
            valueText.text = value;
            SetRect(valueText.rectTransform, new Vector2(0.64f, 0.08f), new Vector2(0.87f, 0.92f));
            return row;
        }

        private void EnsureBackNavigation()
        {
            // Home exits through the phone's Back action and the quit confirmation.
            var homeBack = mainMenuPanel != null ? mainMenuPanel.transform.Find("Back") : null;
            if (homeBack != null) homeBack.gameObject.SetActive(false);
            EnsureBackButton(accountPanel, ReturnFromAccount);
            EnsureBackButton(leaderboardPanel, ShowMainMenu);
            EnsureBackButton(gameplayPanel, () => session.SetPaused(true));
            EnsureBackButton(winPanel, () => session.OpenMainMenu());
            EnsureBackButton(timeoutPanel, () => session.OpenMainMenu());
            EnsureBackButton(pausePanel, () => session.SetPaused(false));
            EnsureBackButton(chapterSelectPanel, ReturnFromSubMenu);
            EnsureBackButton(levelSelectPanel, ShowChapterSelect);
            EnsureBackButton(settingsPanel, ReturnFromSubMenu);
            if (tutorialOverlay != null && tutorialOverlay.GetComponent<TutorialPopupView>() != null)
            {
                var tutorialBack = tutorialOverlay.transform.Find("Back");
                if (tutorialBack != null) tutorialBack.gameObject.SetActive(false);
            }
            else EnsureBackButton(tutorialOverlay, ContinueTutorial);
        }

        private void EnsureBackButton(GameObject panel, UnityEngine.Events.UnityAction action)
        {
            if (panel == null) return;
            var existing = panel.transform.Find("Back");
            Button button;
            if (existing != null) button = existing.GetComponent<Button>();
            else button = CreateButton("Back", panel.transform, "\u2039", action);
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            AnalyticsButtonTracker.Bind(button);
            button.onClick.AddListener(action);
            var text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = "\u2039";
                text.fontSize = 44;
            }
            SetRect(button.GetComponent<RectTransform>(), new Vector2(0.035f, 0.905f), new Vector2(0.145f, 0.972f));
        }

        private void ExitGame()
        {
            ShowQuitConfirmation();
        }

        private void CommitSettings(bool rebuild = true)
        {
            SaveService.Save();
            session?.ApplyPlayerSettings();
            ApplyUiScale();
            if (rebuild) RebuildSettings();
        }

        private void ApplyUiScale()
        {
            if (canvas == null) return;
            if (safeRoot != null && safeRoot.GetComponent<SafeAreaFitter>() == null)
                safeRoot.gameObject.AddComponent<SafeAreaFitter>();
            safeRoot?.GetComponent<SafeAreaFitter>()?.Apply();
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) return;
            var resolution = SaveService.Data?.largeText ?? false
                ? new Vector2(960f, 1707f)
                : new Vector2(1080f, 1920f);
            var responsive = canvas.GetComponent<ResponsiveCanvasScaler>();
            if (responsive == null) responsive = canvas.gameObject.AddComponent<ResponsiveCanvasScaler>();
            responsive.SetReferenceResolution(resolution);
        }

        private void EnsureSignatureControls()
        {
            if (gameplayPanel == null) return;
            if (echoButton == null)
            {
                echoButton = CreateButton("Echo", gameplayPanel.transform, "ECHO", () => session.ToggleEcho());
                SetRect(echoButton.GetComponent<RectTransform>(), new Vector2(0.67f, 0.205f), new Vector2(0.95f, 0.255f));
                echoText = echoButton.GetComponentInChildren<TMP_Text>(true);
                if (echoText != null) echoText.fontSize = 24;
                echoButton.gameObject.SetActive(false);
            }
            if (signatureStatusText == null)
            {
                signatureStatusText = CreateText("Signature Status", gameplayPanel.transform, 21,
                    TextAlignmentOptions.Center, session.Config.uiNavy);
                signatureStatusText.fontStyle = FontStyles.Bold;
                SetRect(signatureStatusText.rectTransform, new Vector2(0.08f, 0.735f), new Vector2(0.92f, 0.78f));
                signatureStatusText.gameObject.SetActive(false);
            }
        }

        private void ApplyUiMotion()
        {
            if (safeRoot == null) return;
            var buttons = safeRoot.GetComponentsInChildren<Button>(true);
            for (var i = 0; i < buttons.Length; i++)
            {
                var juice = buttons[i].GetComponent<UIJuice>();
                if (juice == null) juice = buttons[i].gameObject.AddComponent<UIJuice>();
                juice.IdleHover = false;
            }
            var mainTitle = mainMenuPanel != null ? mainMenuPanel.transform.Find("Title") : null;
            if (mainTitle != null)
            {
                var pulse = mainTitle.GetComponent<UIJuice>();
                if (pulse == null) pulse = mainTitle.gameObject.AddComponent<UIJuice>();
                pulse.AmbientPulse = true;
                var floating = mainTitle.GetComponent<UIFloatMotion>();
                if (floating == null) floating = mainTitle.gameObject.AddComponent<UIFloatMotion>();
                floating.Configure(new Vector2(3f, 7f), 0.22f, 1.3f, 0.45f);
            }
        }

        private void ShowAnimated(GameObject panel, float startScale = -1f)
        {
            if (panel == null) return;
            StopCoroutineIfRunning(panel);
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            var canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = panel.AddComponent<CanvasGroup>();
            var panelRect = panel.transform as RectTransform;
            if (panelRect != null) panelRect.anchoredPosition = Vector2.zero;
            if (SaveService.Data?.reduceMotion ?? false)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                panel.transform.localScale = Vector3.one;
                var childGroups = panel.GetComponentsInChildren<CanvasGroup>(true);
                for (var i = 0; i < childGroups.Length; i++) childGroups[i].alpha = 1f;
                return;
            }
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            var routine = StartCoroutine(AnimatePanelIn(panel, canvasGroup,
                startScale > 0f ? startScale : LiveOpsConfigStore.Current.uiPopupStartScale));
            panelAnimations[panel.GetInstanceID()] = routine;
        }

        private readonly Dictionary<int, Coroutine> panelAnimations = new();

        private void EnsureLoadingOverlay()
        {
            if (canvas == null) return;
            if (loadingOverlay == null)
            {
                var existing = canvas.transform.Find("Loading Overlay");
                loadingOverlay = existing != null
                    ? existing.gameObject
                    : prefabs.Spawn(prefabs.loadingOverlayPrefab, "Loading Overlay", canvas.transform);
            }
            Stretch(loadingOverlay.transform as RectTransform);
            var background = loadingOverlay.GetComponent<Image>();
            if (background != null)
            {
                background.color = StartupCoverColor;
                background.raycastTarget = true;
            }
            EnsureLoadingCanvasGroup();

            var brand = loadingOverlay.transform.Find("Brand")?.GetComponent<TMP_Text>();
            loadingStatusText = loadingOverlay.transform.Find("Status")?.GetComponent<TMP_Text>();
            if (brand == null || loadingStatusText == null)
                throw new InvalidOperationException("LoadingOverlay prefab requires its authored Brand and Status TMP labels.");
            loadingOverlay.SetActive(false);
        }

        private bool EnsureLoadingCanvasGroup()
        {
            if (loadingOverlay == null) return false;
            loadingOverlayGroup = loadingOverlay.GetComponent<CanvasGroup>();
            return loadingOverlayGroup != null;
        }

        public void ShowLoading(string message)
        {
            EnsureLoadingOverlay();
            if (loadingOverlay == null || !EnsureLoadingCanvasGroup())
            {
                Debug.LogError("Loading Overlay could not create its required CanvasGroup.");
                return;
            }
            if (loadingHideRoutine != null)
            {
                StopCoroutine(loadingHideRoutine);
                loadingHideRoutine = null;
            }
            loadingStatusBase = string.IsNullOrWhiteSpace(message) ? "LOADING" : message.Trim().ToUpperInvariant();
            if (loadingStatusText != null) loadingStatusText.text = loadingStatusBase;
            loadingOverlay.SetActive(true);
            loadingOverlay.transform.SetAsLastSibling();
            loadingOverlayGroup.alpha = 1f;
            loadingOverlayGroup.interactable = true;
            loadingOverlayGroup.blocksRaycasts = true;
            if (loadingPulseRoutine == null) loadingPulseRoutine = StartCoroutine(AnimateLoadingStatus());
            LiveOpsManager.Instance?.Ads?.SetBannerVisible(false);
        }

        public void HideLoading()
        {
            if (loadingOverlay == null || !loadingOverlay.activeSelf) return;
            if (loadingHideRoutine != null) StopCoroutine(loadingHideRoutine);
            loadingHideRoutine = StartCoroutine(FadeLoadingOut());
        }

        private IEnumerator AnimateLoadingStatus()
        {
            var phase = 0;
            while (loadingOverlay != null && loadingOverlay.activeSelf)
            {
                if (loadingStatusText != null)
                    loadingStatusText.text = loadingStatusBase + new string('.', phase + 1);
                phase = (phase + 1) % 3;
                yield return new WaitForSecondsRealtime(0.28f);
            }
            loadingPulseRoutine = null;
        }

        private IEnumerator FadeLoadingOut()
        {
            if (!EnsureLoadingCanvasGroup())
            {
                HideLoadingImmediately();
                yield break;
            }
            var duration = Mathf.Max(0.05f, LiveOpsConfigStore.Current.loadingFadeSeconds);
            var elapsed = 0f;
            while (elapsed < duration && loadingOverlay != null && loadingOverlay.activeSelf)
            {
                elapsed += Time.unscaledDeltaTime;
                loadingOverlayGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            loadingHideRoutine = null;
            HideLoadingImmediately();
        }

        private void HideLoadingImmediately()
        {
            if (loadingHideRoutine != null)
            {
                StopCoroutine(loadingHideRoutine);
                loadingHideRoutine = null;
            }
            if (loadingPulseRoutine != null)
            {
                StopCoroutine(loadingPulseRoutine);
                loadingPulseRoutine = null;
            }
            if (loadingOverlayGroup != null)
            {
                loadingOverlayGroup.alpha = 0f;
                loadingOverlayGroup.interactable = false;
                loadingOverlayGroup.blocksRaycasts = false;
            }
            if (loadingOverlay != null) loadingOverlay.SetActive(false);
            RefreshBannerVisibilityAfterLoading();
        }

        private void RefreshBannerVisibilityAfterLoading()
        {
            if (screens == null || screens.Count == 0) return;
            var hasVisibleScreen = false;
            for (var i = 0; i < screens.Count; i++)
            {
                if (screens[i] == null || !screens[i].activeSelf) continue;
                hasVisibleScreen = true;
                break;
            }
            var signInVisible = signInPanel != null && signInPanel.activeSelf;
            var accountFromSignInVisible = accountReturnsToSignIn && accountPanel != null && accountPanel.activeSelf;
            LiveOpsManager.Instance?.Ads?.SetBannerVisible(
                hasVisibleScreen && !signInVisible && !accountFromSignInVisible);
        }

        private void EnsureScreenTransitionLayer()
        {
            if (canvas == null) return;
            var existing = canvas.transform.Find("Screen Transition");
            screenTransitionLayer = existing != null
                ? existing.gameObject
                : prefabs.Spawn(prefabs.screenTransitionPrefab, "Screen Transition", canvas.transform);
            var image = screenTransitionLayer.GetComponent<Image>();
            if (image != null)
            {
                image.color = startupPresentationReleased ? Color.clear : StartupCoverColor;
                image.raycastTarget = true;
            }
            screenTransitionGroup = screenTransitionLayer.GetComponent<CanvasGroup>();
            if (screenTransitionGroup == null)
                throw new InvalidOperationException("ScreenTransition prefab requires a CanvasGroup.");
            screenTransitionGroup.alpha = startupPresentationReleased ? 0f : 1f;
            screenTransitionGroup.interactable = !startupPresentationReleased;
            screenTransitionGroup.blocksRaycasts = !startupPresentationReleased;
            screenTransitionLayer.SetActive(!startupPresentationReleased);
            if (!startupPresentationReleased) screenTransitionLayer.transform.SetAsLastSibling();
        }

        private void TransitionToScreen(GameObject target)
        {
            if (target == null) return;
            CancelScreenTransition();
            // The first screen must be complete before the first rendered frame. Starting it
            // at alpha zero exposes a dim, half-animated menu for one frame on fast devices.
            // Later navigation still uses the configured per-screen transitions.
            if (!Application.isPlaying || !hasPresentedScreen || !startupPresentationReleased)
            {
                hasPresentedScreen = true;
                HideScreens();
                ShowScreenImmediately(target);
                return;
            }
            hasPresentedScreen = true;
            screenTransitionRoutine = StartCoroutine(AnimateScreenTransition(target));
        }

        private IEnumerator AnimateScreenTransition(GameObject target)
        {
            if (screenTransitionLayer == null || screenTransitionGroup == null)
            {
                EnsureScreenTransitionLayer();
                if (screenTransitionLayer == null || screenTransitionGroup == null)
                {
                    HideScreens();
                    ShowScreenImmediately(target);
                    screenTransitionRoutine = null;
                    yield break;
                }
            }

            GameObject outgoing = null;
            for (var i = 0; i < screens.Count; i++)
            {
                if (screens[i] != null && screens[i] != target && screens[i].activeSelf)
                {
                    outgoing = screens[i];
                    break;
                }
            }

            var targetProfile = GetTransitionProfile(target);
            var outgoingProfile = GetTransitionProfile(outgoing);
            screenTransitionLayer.SetActive(true);
            screenTransitionLayer.transform.SetAsLastSibling();
            screenTransitionGroup.alpha = 1f;
            screenTransitionGroup.interactable = true;
            screenTransitionGroup.blocksRaycasts = true;

            if (outgoing != null && outgoingProfile.animateOutgoing)
            {
                StopCoroutineIfRunning(outgoing);
                var outgoingGroup = EnsureCanvasGroup(outgoing);
                outgoingGroup.interactable = false;
                outgoingGroup.blocksRaycasts = false;
                var outgoingRect = outgoing.transform as RectTransform;
                var startAlpha = outgoingGroup.alpha;
                var elapsed = 0f;
                var exitDirection = DirectionVector(outgoingProfile.exitDirection);
                var exitSlides = StyleUses(outgoingProfile.style, "Slide");
                var exitFades = StyleUses(outgoingProfile.style, "Fade");
                var exitScales = StyleUses(outgoingProfile.style, "Scale");
                while (elapsed < outgoingProfile.exitDuration && outgoing.activeSelf)
                {
                    elapsed += Time.unscaledDeltaTime;
                    var t = Mathf.Clamp01(elapsed / outgoingProfile.exitDuration);
                    var eased = EvaluateEase(outgoingProfile.exitEasing, t);
                    outgoingGroup.alpha = exitFades ? Mathf.Lerp(startAlpha, 0f, eased) : startAlpha;
                    outgoing.transform.localScale = Vector3.one * (exitScales
                        ? Mathf.Lerp(1f, outgoingProfile.exitScale, eased)
                        : 1f);
                    if (outgoingRect != null && exitSlides)
                        outgoingRect.anchoredPosition = exitDirection * outgoingProfile.distance * eased;
                    yield return null;
                }
            }

            HideScreens();
            SetBackdropForScreen(target);
            target.SetActive(true);
            AnalyticsService.Screen(target.name);
            target.transform.SetAsLastSibling();
            var targetRect = target.transform as RectTransform;
            var targetGroup = EnsureCanvasGroup(target);
            var enterSlides = StyleUses(targetProfile.style, "Slide");
            var enterFades = StyleUses(targetProfile.style, "Fade");
            var enterScales = StyleUses(targetProfile.style, "Scale");
            var enterDirection = DirectionVector(targetProfile.enterDirection);
            targetGroup.alpha = enterFades ? 0f : 1f;
            targetGroup.interactable = false;
            targetGroup.blocksRaycasts = false;
            target.transform.localScale = Vector3.one * (enterScales ? targetProfile.startScale : 1f);
            if (targetRect != null)
                targetRect.anchoredPosition = enterSlides
                    ? enterDirection * targetProfile.distance
                    : Vector2.zero;
            ResetNestedCanvasGroups(target, targetGroup);
            // Reveal interactive content in sequence while the existing screen transition runs.
            // Backgrounds stay visible, and gameplay remains blocked until the transition ends.
            var revealGroups = new List<CanvasGroup>();
            if (!(SaveService.Data?.reduceMotion ?? false))
            {
                for (var i = 0; i < target.transform.childCount; i++)
                {
                    var child = target.transform.GetChild(i);
                    if (!child.gameObject.activeSelf || (child.GetComponent<Button>() == null && child.GetComponent<TMP_Text>() == null)) continue;
                    // Unity can return a destroyed/missing component wrapper that compares
                    // equal to null but is not C# null. Use Unity's null check here.
                    var group = EnsureCanvasGroup(child.gameObject);
                    if (group == null) continue;
                    group.alpha = 0f;
                    revealGroups.Add(group);
                }
            }
            screenTransitionLayer.transform.SetAsLastSibling();

            var enterElapsed = 0f;
            var revealDelay = Mathf.Min(LiveOpsConfigStore.Current.uiStaggerSeconds,
                targetProfile.duration * 0.5f / Mathf.Max(1, revealGroups.Count - 1));
            var revealDuration = Mathf.Max(0.01f, targetProfile.duration - revealDelay * Mathf.Max(0, revealGroups.Count - 1));
            while (enterElapsed < targetProfile.duration && target.activeSelf)
            {
                enterElapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(enterElapsed / targetProfile.duration);
                var eased = EvaluateEase(targetProfile.enterEasing, t);
                targetGroup.alpha = enterFades ? Mathf.Clamp01(eased) : 1f;
                target.transform.localScale = Vector3.one * (enterScales
                    ? Mathf.LerpUnclamped(targetProfile.startScale, 1f, eased)
                    : 1f);
                if (targetRect != null && enterSlides)
                    targetRect.anchoredPosition = Vector2.LerpUnclamped(
                        enterDirection * targetProfile.distance, Vector2.zero, eased);
                for (var i = 0; i < revealGroups.Count; i++)
                    if (revealGroups[i] != null)
                        revealGroups[i].alpha = 1f - Mathf.Pow(1f - Mathf.Clamp01((enterElapsed - i * revealDelay) / revealDuration), 3f);
                yield return null;
            }

            foreach (var group in revealGroups) if (group != null) group.alpha = 1f;
            if (targetRect != null) targetRect.anchoredPosition = Vector2.zero;
            target.transform.localScale = Vector3.one;
            targetGroup.alpha = 1f;
            targetGroup.interactable = true;
            targetGroup.blocksRaycasts = true;

            screenTransitionGroup.alpha = 1f;
            screenTransitionGroup.interactable = false;
            screenTransitionGroup.blocksRaycasts = false;
            screenTransitionLayer.SetActive(false);
            screenTransitionRoutine = null;
        }

        private IEnumerator AnimateOverlayTransition(GameObject panel, bool visible,
            UIScreenTransitionConfig profile, Action onComplete)
        {
            profile ??= UIScreenTransitionConfig.Popup();
            profile.Normalize();
            var tutorialPopup = panel == tutorialOverlay ? panel.GetComponent<TutorialPopupView>() : null;
            if (tutorialPopup != null)
            {
                yield return AnimateTutorialCard(tutorialPopup, visible, profile);
                onComplete?.Invoke();
                yield break;
            }
            var group = EnsureCanvasGroup(panel);
            var rect = panel.transform as RectTransform;
            var slides = StyleUses(profile.style, "Slide");
            var fades = StyleUses(profile.style, "Fade");
            var scales = StyleUses(profile.style, "Scale");
            var direction = DirectionVector(visible ? profile.enterDirection : profile.exitDirection);
            var duration = visible ? profile.duration : profile.exitDuration;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            group.interactable = false;
            group.blocksRaycasts = visible;
            ResetNestedCanvasGroups(panel, group);
            var elapsed = 0f;
            while (elapsed < duration && panel.activeSelf)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = EvaluateEase(visible ? profile.enterEasing : profile.exitEasing, t);
                var progress = visible ? eased : 1f - eased;
                group.alpha = fades ? Mathf.Clamp01(progress) : 1f;
                panel.transform.localScale = Vector3.one * (scales
                    ? Mathf.LerpUnclamped(visible ? profile.startScale : profile.exitScale,
                        visible ? 1f : 1f, progress)
                    : 1f);
                if (rect != null && slides)
                    rect.anchoredPosition = direction * profile.distance * (visible ? 1f - eased : eased);
                yield return null;
            }
            if (rect != null) rect.anchoredPosition = Vector2.zero;
            panel.transform.localScale = Vector3.one;
            group.alpha = 1f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
            if (!visible) panel.SetActive(false);
            onComplete?.Invoke();
        }

        private IEnumerator AnimateTutorialCard(TutorialPopupView popup, bool visible, UIScreenTransitionConfig profile)
        {
            popup.Prepare(visible);
            var reducedMotion = SaveService.Data?.reduceMotion ?? false;
            var duration = reducedMotion ? 0f : visible ? profile.duration : profile.exitDuration;
            var slides = StyleUses(profile.style, "Slide");
            var fades = StyleUses(profile.style, "Fade");
            var scales = StyleUses(profile.style, "Scale");
            var direction = DirectionVector(visible ? profile.enterDirection : profile.exitDirection);
            var start = visible ? profile.startScale : profile.exitScale;
            popup.SetFrame(visible && fades ? 0f : 1f, visible && scales ? start : 1f,
                visible && slides ? direction * profile.distance : Vector2.zero);
            var elapsed = 0f;
            while (elapsed < duration && popup.gameObject.activeSelf)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = EvaluateEase(visible ? profile.enterEasing : profile.exitEasing, t);
                var progress = visible ? eased : 1f - eased;
                popup.SetFrame(fades ? progress : 1f, scales ? Mathf.LerpUnclamped(start, 1f, progress) : 1f,
                    slides ? direction * profile.distance * (visible ? 1f - eased : eased) : Vector2.zero);
                yield return null;
            }
            popup.Complete(visible);
        }

        private UIScreenTransitionConfig GetTransitionProfile(GameObject panel)
        {
            var live = LiveOpsConfigStore.Current;
            var profile = panel == signInPanel ? live.signInTransition
                : panel == accountPanel ? live.accountTransition
                : panel == leaderboardPanel ? live.leaderboardTransition
                : panel == mainMenuPanel ? live.mainMenuTransition
                : panel == gameplayPanel ? live.gameplayTransition
                : panel == chapterSelectPanel ? live.chapterSelectTransition
                : panel == levelSelectPanel ? live.levelSelectTransition
                : panel == settingsPanel ? live.settingsTransition
                : panel == winPanel ? live.winTransition
                : panel == timeoutPanel ? live.timeoutTransition
                : UIScreenTransitionConfig.Standard();
            profile ??= UIScreenTransitionConfig.Standard();
            profile.Normalize();
            return profile;
        }

        private static bool StyleUses(string style, string feature) =>
            !string.IsNullOrWhiteSpace(style) &&
            style.IndexOf(feature, StringComparison.OrdinalIgnoreCase) >= 0;

        private static Vector2 DirectionVector(string direction)
        {
            if (string.Equals(direction, "Left", StringComparison.OrdinalIgnoreCase)) return Vector2.left;
            if (string.Equals(direction, "Up", StringComparison.OrdinalIgnoreCase)) return Vector2.up;
            if (string.Equals(direction, "Down", StringComparison.OrdinalIgnoreCase)) return Vector2.down;
            if (string.Equals(direction, "None", StringComparison.OrdinalIgnoreCase)) return Vector2.zero;
            return Vector2.right;
        }

        private static float EvaluateEase(string easing, float t)
        {
            t = Mathf.Clamp01(t);
            if (string.Equals(easing, "Linear", StringComparison.OrdinalIgnoreCase)) return t;
            if (string.Equals(easing, "EaseInCubic", StringComparison.OrdinalIgnoreCase)) return t * t * t;
            if (string.Equals(easing, "SmoothStep", StringComparison.OrdinalIgnoreCase))
                return t * t * (3f - 2f * t);
            if (string.Equals(easing, "EaseOutBack", StringComparison.OrdinalIgnoreCase))
            {
                const float c1 = 1.70158f;
                var p = t - 1f;
                return 1f + (c1 + 1f) * p * p * p + c1 * p * p;
            }
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        private void ShowScreenImmediately(GameObject target)
        {
            if (target == null) return;
            SetBackdropForScreen(target);
            target.SetActive(true);
            AnalyticsService.Screen(target.name);
            target.transform.SetAsLastSibling();
            var rect = target.transform as RectTransform;
            if (rect != null) rect.anchoredPosition = Vector2.zero;
            target.transform.localScale = Vector3.one;
            var group = EnsureCanvasGroup(target);
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            ResetNestedCanvasGroups(target, group);
            // Reveal only after the selected screen is fully normalized. This prevents
            // the sign-in, timeout, or menu prefab state flashing during app startup.
            ReleaseStartupPresentation();
        }

        private void SetBackdropForScreen(GameObject target)
        {
            if (fullScreenBackdrop == null && canvas != null)
            {
                var existing = canvas.transform.Find("Full Screen Background");
                if (existing != null) fullScreenBackdrop = existing.gameObject;
            }
            if (fullScreenBackdrop == null) return;
            var showBackdrop = target == signInPanel || target == accountPanel || target == leaderboardPanel || target == mainMenuPanel || target == storePanel || target == chapterSelectPanel ||
                               target == levelSelectPanel || target == settingsPanel || target == cloudSyncPanel;
            fullScreenBackdrop.SetActive(showBackdrop);
            if (showBackdrop) fullScreenBackdrop.transform.SetAsFirstSibling();
        }

        private void SyncBackdropVisibility()
        {
            if (canvas == null) return;
            if (fullScreenBackdrop == null)
            {
                var existing = canvas.transform.Find("Full Screen Background");
                if (existing == null) return;
                fullScreenBackdrop = existing.gameObject;
            }
            var worldScreenActive = (gameplayPanel != null && gameplayPanel.activeSelf) ||
                                    (winPanel != null && winPanel.activeSelf) ||
                                    (timeoutPanel != null && timeoutPanel.activeSelf);
            if (worldScreenActive && fullScreenBackdrop.activeSelf)
                fullScreenBackdrop.SetActive(false);
        }

        private static CanvasGroup EnsureCanvasGroup(GameObject target)
        {
            var group = target.GetComponent<CanvasGroup>();
            return group != null ? group : target.AddComponent<CanvasGroup>();
        }

        private static void ResetNestedCanvasGroups(GameObject target, CanvasGroup root)
        {
            var groups = target.GetComponentsInChildren<CanvasGroup>(true);
            for (var i = 0; i < groups.Length; i++)
                if (groups[i] != root) groups[i].alpha = 1f;
        }

        private void CancelScreenTransition()
        {
            if (screenTransitionRoutine != null)
            {
                StopCoroutine(screenTransitionRoutine);
                screenTransitionRoutine = null;
            }
            if (screenTransitionGroup != null)
            {
                screenTransitionGroup.alpha = 1f;
                screenTransitionGroup.interactable = false;
                screenTransitionGroup.blocksRaycasts = false;
            }
            if (screenTransitionLayer != null)
            {
                screenTransitionLayer.SetActive(false);
            }
            for (var i = 0; i < screens.Count; i++)
            {
                var screen = screens[i];
                if (screen == null || !screen.activeSelf) continue;
                var rect = screen.transform as RectTransform;
                if (rect != null) rect.anchoredPosition = Vector2.zero;
                screen.transform.localScale = Vector3.one;
                var group = EnsureCanvasGroup(screen);
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }
        }

        private void StopCoroutineIfRunning(GameObject panel)
        {
            var key = panel.GetInstanceID();
            if (!panelAnimations.TryGetValue(key, out var routine) || routine == null) return;
            StopCoroutine(routine);
            panelAnimations.Remove(key);
        }

        private IEnumerator AnimatePanelIn(GameObject panel, CanvasGroup canvasGroup, float startScale)
        {
            var key = panel.GetInstanceID();
            var settings = LiveOpsConfigStore.Current;
            var duration = settings.uiAnimationSeconds;
            var elapsed = 0f;
            var items = new List<RectTransform>();
            var itemGroups = new List<CanvasGroup>();
            for (var i = 0; i < panel.transform.childCount; i++)
            {
                var child = panel.transform.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeInHierarchy ||
                    child.name.StartsWith("Aurora", StringComparison.Ordinal) ||
                    child.name.Contains("Glass", StringComparison.Ordinal) || child.GetComponent<UIFloatMotion>() != null) continue;
                items.Add(child);
                var group = child.GetComponent<CanvasGroup>();
                if (group == null) group = child.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                itemGroups.Add(group);
            }
            canvasGroup.alpha = 0f;
            var panelStartScale = panel == tooltipPanel || panel == tutorialOverlay || panel == pausePanel
                ? startScale
                : Mathf.Max(settings.uiScreenStartScale, startScale);
            panel.transform.localScale = Vector3.one * panelStartScale;
            var panelRect = panel.transform as RectTransform;
            var panelOrigin = Vector2.zero;
            var slideDistance = panel == tooltipPanel || panel == tutorialOverlay || panel == pausePanel
                ? 0f
                : settings.uiScreenSlideDistance;
            if (panelRect != null) panelRect.anchoredPosition = panelOrigin + Vector2.right * slideDistance;
            var maximumStagger = Mathf.Min(0.22f, Mathf.Max(0, items.Count - 1) * settings.uiStaggerSeconds);
            var totalDuration = duration + maximumStagger;
            while (elapsed < totalDuration && panel != null && panel.activeSelf)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                var eased = 1f - Mathf.Pow(1f - t, 3f) +
                            Mathf.Sin(t * Mathf.PI) * (1f - t) * settings.uiScreenOvershoot;
                canvasGroup.alpha = eased;
                panel.transform.localScale = Vector3.one * Mathf.Lerp(panelStartScale, 1f, eased);
                if (panelRect != null)
                    panelRect.anchoredPosition = Vector2.LerpUnclamped(panelOrigin + Vector2.right * slideDistance,
                        panelOrigin, eased);
                for (var i = 0; i < items.Count; i++)
                {
                    if (items[i] == null) continue;
                    var delay = Mathf.Min(0.22f, i * settings.uiStaggerSeconds);
                    var itemT = Mathf.Clamp01((elapsed - delay) / Mathf.Max(0.01f, duration));
                    itemGroups[i].alpha = 1f - Mathf.Pow(1f - itemT, 3f);
                }
                yield return null;
            }
            if (panel != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                panel.transform.localScale = Vector3.one;
                if (panelRect != null) panelRect.anchoredPosition = panelOrigin;
                for (var i = 0; i < items.Count; i++)
                {
                    if (items[i] == null) continue;
                    itemGroups[i].alpha = 1f;
                }
            }
            panelAnimations.Remove(key);
        }

        private void HideScreens()
        {
            var tutorialWasActive = tutorialOverlay != null && tutorialOverlay.activeSelf;
            if (pauseTransitionRoutine != null)
            {
                StopCoroutine(pauseTransitionRoutine);
                pauseTransitionRoutine = null;
            }
            if (tutorialTransitionRoutine != null)
            {
                StopCoroutine(tutorialTransitionRoutine);
                tutorialTransitionRoutine = null;
            }
            foreach (var screen in screens)
            {
                if (screen == null) continue;
                StopCoroutineIfRunning(screen);
                screen.SetActive(false);
                var rect = screen.transform as RectTransform;
                if (rect != null) rect.anchoredPosition = Vector2.zero;
                screen.transform.localScale = Vector3.one;
                var group = screen.GetComponent<CanvasGroup>();
                ResetNestedCanvasGroups(screen, group);
                if (group != null)
                {
                    group.alpha = 1f;
                    group.interactable = true;
                    group.blocksRaycasts = true;
                }
            }
            if (pausePanel != null) pausePanel.SetActive(false);
            if (tutorialOverlay != null) tutorialOverlay.SetActive(false);
            if (tooltipPanel != null) tooltipPanel.SetActive(false);
            tutorialQueue.Clear();
            // Never leave gameplay frozen if navigation or another transition removes
            // an active tutorial before its Continue button can complete it.
            if (tutorialWasActive) session?.SetTutorialPaused(false);
        }

        private void EnsureEventSystem()
        {
            if (!Application.isPlaying) return;
            if (EventSystem.current != null) return;
            prefabs.Spawn(prefabs.eventSystemPrefab, "EventSystem");
        }

        private GameObject CreateOverlay(string name, Transform parent) =>
            CreatePanel(name, parent, new Color(session.Config.uiBackgroundBottom.r,
                session.Config.uiBackgroundBottom.g, session.Config.uiBackgroundBottom.b, 0.96f));

        private GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var go = prefabs.Spawn(prefabs.uiPanelPrefab, name, parent);
            Stretch(go.GetComponent<RectTransform>());
            var image = go.GetComponent<Image>();
            if (image == null)
                throw new InvalidOperationException("UI Panel prefab requires an Image.");
            image.color = color;
            image.raycastTarget = false;
            return go;
        }

        private GameObject CreateRoundedPanel(string name, Transform parent, Color color)
        {
            var panel = CreatePanel(name, parent, color);
            var image = panel.GetComponent<Image>();
            image.sprite = RoundedPanelSprite;
            image.type = Image.Type.Sliced;
            return panel;
        }

        private RectTransform CreateRect(string name, Transform parent)
        {
            var rect = prefabs.Spawn(prefabs.uiRectPrefab, name, parent).GetComponent<RectTransform>();
            if (rect == null)
                throw new InvalidOperationException("UI Rect prefab requires a RectTransform.");
            return rect;
        }

        private TMP_Text CreateText(string name, Transform parent, int size, TextAlignmentOptions anchor, Color color)
        {
            var go = prefabs.Spawn(prefabs.uiTextPrefab, name, parent);
            var text = go.GetComponent<TMP_Text>();
            if (text == null)
                throw new InvalidOperationException("UI Text prefab requires a TextMeshProUGUI component.");
            if (text.font == null) text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private TMP_InputField CreateInputField(string name, Transform parent, string placeholder, bool password,
            int characterLimit)
        {
            var panel = CreateRoundedPanel(name, parent, session.Config.uiSurfaceBright);
            var image = panel.GetComponent<Image>();
            image.raycastTarget = true;
            var input = panel.AddComponent<TMP_InputField>();
            input.targetGraphic = image;
            input.textViewport = panel.GetComponent<RectTransform>();
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = password ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            input.characterLimit = characterLimit;
            input.shouldHideMobileInput = false;

            var value = CreateText("Text", panel.transform, 27, TextAlignmentOptions.Left, session.Config.uiTextPrimary);
            value.richText = false;
            value.textWrappingMode = TextWrappingModes.Normal;
            SetRect(value.rectTransform, new Vector2(0.055f, 0.08f), new Vector2(0.945f, 0.92f));
            var hint = CreateText("Placeholder", panel.transform, 25, TextAlignmentOptions.Left, session.Config.uiTextMuted);
            hint.fontStyle = FontStyles.Italic;
            hint.text = placeholder;
            SetRect(hint.rectTransform, new Vector2(0.055f, 0.08f), new Vector2(0.945f, 0.92f));
            input.textComponent = value;
            input.placeholder = hint;
            return input;
        }

        private Button CreateButton(string name, Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var go = prefabs.Spawn(prefabs.uiButtonPrefab, name, parent);
            var image = go.GetComponent<Image>();
            var button = go.GetComponent<Button>();
            var text = go.GetComponentInChildren<TMP_Text>(true);
            if (image == null || button == null || text == null)
                throw new InvalidOperationException("UI Button prefab requires Image, Button and a child TextMeshProUGUI component.");
            image.color = session.Config.uiButton;
            image.sprite = RoundedPanelSprite;
            image.type = Image.Type.Sliced;
            button.targetGraphic = image;
            button.onClick.RemoveAllListeners();
            AnalyticsButtonTracker.Bind(button);
            button.onClick.AddListener(action);
            if (text.font == null) text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = 30;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.text = label;
            Stretch(text.rectTransform);
            text.raycastTarget = false;
            StyleButton(button);
            return button;
        }

        private static Sprite RoundedPanelSprite
        {
            get
            {
                if (roundedPanelSprite != null) return roundedPanelSprite;
                const int size = 64;
                const float radius = 14f;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    name = "Runtime Rounded UI",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                var pixels = new Color[size * size];
                var half = (size - 1) * 0.5f;
                var straight = half - radius;
                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Max(Mathf.Abs(x - half) - straight, 0f);
                    var dy = Mathf.Max(Mathf.Abs(y - half) - straight, 0f);
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = 1f - Mathf.SmoothStep(radius - 1.25f, radius + 0.25f, distance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                texture.SetPixels(pixels);
                texture.Apply(false, true);
                roundedPanelSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f,
                    size, 0u, SpriteMeshType.FullRect, new Vector4(16f, 16f, 16f, 16f));
                roundedPanelSprite.name = "Runtime Rounded UI Sprite";
                return roundedPanelSprite;
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }

}
