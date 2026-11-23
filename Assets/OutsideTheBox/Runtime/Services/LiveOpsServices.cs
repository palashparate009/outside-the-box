using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using Unity.Services.LevelPlay;

namespace OutsideTheBox
{
    [Serializable]
    public sealed class UIScreenTransitionConfig
    {
        public string style = "SlideFadeScale";
        public string enterDirection = "Right";
        public string exitDirection = "Left";
        public string enterEasing = "EaseOutCubic";
        public string exitEasing = "SmoothStep";
        public float duration = 0.34f;
        public float exitDuration = 0.187f;
        public float distance = 220f;
        public float startScale = 0.975f;
        public float exitScale = 0.97f;
        public bool animateOutgoing = true;

        public static UIScreenTransitionConfig Standard() => new();

        public static UIScreenTransitionConfig Popup() => new()
        {
            style = "ScaleFade",
            enterDirection = "None",
            exitDirection = "None",
            enterEasing = "EaseOutBack",
            exitEasing = "EaseInCubic",
            duration = 0.28f,
            exitDuration = 0.16f,
            distance = 0f,
            startScale = 0.88f,
            exitScale = 0.94f,
            animateOutgoing = true
        };

        public void Normalize()
        {
            style = string.IsNullOrWhiteSpace(style) ? "SlideFadeScale" : style.Trim();
            enterDirection = string.IsNullOrWhiteSpace(enterDirection) ? "Right" : enterDirection.Trim();
            exitDirection = string.IsNullOrWhiteSpace(exitDirection) ? "Left" : exitDirection.Trim();
            enterEasing = string.IsNullOrWhiteSpace(enterEasing) ? "EaseOutCubic" : enterEasing.Trim();
            exitEasing = string.IsNullOrWhiteSpace(exitEasing) ? "SmoothStep" : exitEasing.Trim();
            duration = Mathf.Clamp(duration, 0.05f, 2f);
            exitDuration = Mathf.Clamp(exitDuration, 0.05f, 2f);
            distance = Mathf.Clamp(distance, 0f, 720f);
            startScale = Mathf.Clamp(startScale, 0.5f, 1.5f);
            exitScale = Mathf.Clamp(exitScale, 0.5f, 1.5f);
        }
    }

    [Serializable]
    public sealed class GemPackConfig
    {
        public string productId;
        public string name;
        public int gems;
        public string fallbackPrice;

        public GemPackConfig(int amount, string label, string price)
        {
            productId = $"otb_gems_{amount}";
            name = label;
            gems = amount;
            fallbackPrice = price;
        }

        public static GemPackConfig[] Defaults() => new[]
        {
            new GemPackConfig(50, "POCKET GEMS", "INR 10"),
            new GemPackConfig(150, "GEM POUCH", "INR 29"),
            new GemPackConfig(500, "GEM CHEST", "INR 89"),
            new GemPackConfig(1000, "GEM VAULT", "INR 169"),
            new GemPackConfig(1500, "GEM TREASURY", "INR 229"),
            new GemPackConfig(3000, "GEM HOARD", "INR 399")
        };
    }

    [Serializable]
    public sealed class LiveOpsConfigData
    {
        public int version = 13;
        public bool timersEnabled = true;
        public float baseLevelSeconds = 32f;
        public float[] chapterLevelSeconds = { 40f, 45f, 50f, 55f, 60f, 65f, 70f };
        public float secondsPerSolutionBeat = 4.5f;
        public float minimumLevelSeconds = 45f;
        public float maximumLevelSeconds = 180f;
        public float reviveSeconds = 30f;
        public int maxRevivesPerLevel = 2;
        public float energyTargetMultiplier = 1f;
        // Chapters 1–6 followed by the endless/daily chapter.
        public float[] chapterEnergyTargetMultipliers = { 1f, 0.45f, 0.42f, 0.40f, 0.39f, 0.38f, 0.36f };
        public float pushEnergyMultiplier = 1.35f;
        public float minimumEnergyTarget = 20f;
        public float minimumLaterChapterEnergyTarget = 12f;
        public bool energyFailureEnabled = true;
        public float energyFailureMultiplier = 1.6f;
        public float energyRevivePercent = 0.4f;

        public int startingCoins = 150;
        public int coinReviveCost = 50;
        public int campaignCompletionCoins = 2;
        public int coinsPerNewStar = 1;
        public int dailyCompletionCoins = 8;
        public int endlessCompletionCoins = 3;
        public int rewardedCoinAmount = 10;
        public bool gemPurchasesEnabled = true;
        public GemPackConfig[] gemPacks = GemPackConfig.Defaults();
        public int[] cosmeticGemPrices = { 0, 100, 120, 130, 0, 0, 0, 0, 0, 0 };
        public bool chapterGiftsEnabled = true;
        public string[] chapterGiftColors = { "#A8C4D4", "#B3C7B0", "#D1BDA8", "#C2B3D1", "#A6C7C4", "#D1C7A3" };
        public bool localNotificationsEnabled = true;
        public bool firebasePushEnabled = true;
        public string firebasePushTopic = "otb_updates";
        public bool firebasePushLogTokenInDevelopment = true;
        public float giftReminderHours = 24f;
        public float gameReminderHours = 24f;
        public string giftReminderMessage = "Your chapter gift is waiting. Open the store to claim it!";
        public string gameReminderMessage = "Ready for another magnetic puzzle? Your next level is waiting.";
        public string[] gameReminderMessages =
        {
            "A fresh perspective can unlock a new path. Ready for your next puzzle?",
            "Small moves can lead to big discoveries. Try one more magnetic puzzle.",
            "Take a little puzzle break. Your next challenge is waiting.",
            "Every great solution starts with a little curiosity. Follow yours today.",
            "Stuck yesterday? A fresh look might reveal the way today.",
            "Think outside the box. Your next clever move is waiting.",
            "One puzzle, one bright idea. See what you can discover today.",
            "A new angle can change everything. Find your next path.",
            "Make a little time for curiosity. Come back for a magnetic challenge.",
            "The next breakthrough could be one move away. Give it a try.",
            "Pause the busy day and play with a new possibility.",
            "You bring the ideas. We bring the puzzles. Ready to play?",
            "A little patience. A clever move. A satisfying finish. Your next puzzle awaits.",
            "Keep exploring. There is always another way to think outside the box."
        };
        public int[] cosmeticRarities = { 0, 0, 1, 1, 1, 2, 2, 2, 3, 3 };
        public string[] cosmeticRarityColors = { "#B8CFDF", "#55CCFF", "#C68AFF", "#FFD166" };
        public string cosmeticArtworkResource = "OutsideTheBox/Art/Cosmetics/CosmeticAtlas";
        public float storeRevealSeconds = 0.28f;
        public float storeRevealStagger = 0.035f;
        public float storePreviewMotionAmount = 0.035f;
        public float storePreviewMotionSpeed = 1.2f;
        public int cosmeticRewardedViews = 1;
        public string[] cosmeticIapProductIds =
        {
            "", "", "", "", "otb_ball_skin_04", "otb_ball_skin_05", "otb_ball_skin_06", "otb_ball_skin_07", "otb_ball_skin_08", "otb_ball_skin_09",
            "", "", "", "", "otb_magnet_skin_04", "otb_magnet_skin_05", "otb_magnet_skin_06", "otb_magnet_skin_07", "otb_magnet_skin_08", "otb_magnet_skin_09",
            "", "", "", "", "otb_hole_skin_04", "otb_hole_skin_05", "otb_hole_skin_06", "otb_hole_skin_07", "otb_hole_skin_08", "otb_hole_skin_09",
            "", "", "", "", "otb_walls_skin_04", "otb_walls_skin_05", "otb_walls_skin_06", "otb_walls_skin_07", "otb_walls_skin_08", "otb_walls_skin_09",
            "", "", "", "", "otb_glass_skin_04", "otb_glass_skin_05", "otb_glass_skin_06", "otb_glass_skin_07", "otb_glass_skin_08", "otb_glass_skin_09",
            "", "", "", "", "otb_background_skin_04", "otb_background_skin_05", "otb_background_skin_06", "otb_background_skin_07", "otb_background_skin_08", "otb_background_skin_09"
        };
        public string[] cosmeticIapFallbackPrices =
        {
            "", "", "", "", "INR 29", "INR 39", "INR 49", "INR 59", "INR 79", "INR 99"
        };

        public bool signatureMechanicsEnabled = true;
        public bool echoMagnetEnabled = true;
        public float echoRecordSeconds = 3f;
        public int echoSampleRate = 20;
        public int echoMaximumSamples = 96;
        public float echoStrengthMultiplier = 0.62f;
        public float echoLoopDelay = 0.2f;
        public float echoEnergyMultiplier = 0.45f;
        public bool ferrofluidMemoryEnabled = true;
        public int ferrofluidMaximumPoints = 72;
        public float ferrofluidPointSpacing = 0.16f;
        public float ferrofluidGuidanceStrength = 0.8f;
        public bool magneticImprintsEnabled = true;
        public float magneticImprintDuration = 4f;
        public float magneticImprintStrength = 5f;
        public float magneticImprintActivationRadius = 0.7f;
        public bool fieldLensesEnabled = true;
        public float fieldLensDirectionBlend = 1f;
        public bool polarityLocksEnabled = true;
        public bool orbitGoalsEnabled = true;
        public float orbitGoalRadiusMultiplier = 2.5f;
        public float orbitGoalMinimumSpeed = 0.35f;
        public float imprintPulseSpeed = 9f;
        public float fieldLensRotationSpeed = 22f;
        public float orbitGoalPulseSpeed = 4f;
        public float orbitGoalPulseAmount = 0.035f;
        public float orbitGoalRotationSpeed = 35f;
        public bool ftueEnabled = true;
        public int ftueLevelCount = 2;
        public float ftueTooltipSeconds = 4.5f;
        public float uiAnimationSeconds = 0.28f;
        public float timerMilestoneSeconds = 30f;
        public float timerWarningSeconds = 10f;
        public float energyWarningFraction = 0.8f;
        public float hudPulseSeconds = 0.55f;
        public float hudPulseScale = 0.2f;
        public float uiPopupStartScale = 0.88f;
        public float uiButtonPressScale = 0.94f;
        public float uiButtonSpringFrequency = 18f;
        public float uiButtonSpringDamping = 0.72f;
        public float uiButtonHoverAmount = 0f;
        public float uiButtonHoverSpeed = 0.65f;
        public float uiAmbientPulseSpeed = 1.2f;
        public float uiStaggerSeconds = 0.045f;
        public float uiRevealOffset = 34f;
        public float uiBackdropMotionSpeed = 1f;
        public float uiScreenSlideDistance = 88f;
        public float uiScreenStartScale = 0.975f;
        public float uiScreenOvershoot = 0.06f;
        public float loadingMinimumSeconds = 0.35f;
        public float loadingFadeSeconds = 0.16f;
        public string loadingStartupMessage = "SYNCING PLAYER DATA";
        public string loadingLevelMessage = "BUILDING LEVEL";
        public string loadingMenuMessage = "RETURNING TO MENU";
        public bool onlineRequired = true;
        public float connectionCheckSeconds = 6f;
        public float connectionRetrySeconds = 3f;
        public int connectionTimeoutSeconds = 4;
        public int connectionFailureThreshold = 2;
        public string connectionCheckUrl = "https://www.gstatic.com/generate_204";
        public string connectionFallbackUrl = "https://connectivitycheck.gstatic.com/generate_204";
        public UIScreenTransitionConfig mainMenuTransition = UIScreenTransitionConfig.Standard();
        public UIScreenTransitionConfig gameplayTransition = UIScreenTransitionConfig.Standard();
        public UIScreenTransitionConfig chapterSelectTransition = UIScreenTransitionConfig.Standard();
        public UIScreenTransitionConfig levelSelectTransition = UIScreenTransitionConfig.Standard();
        public UIScreenTransitionConfig settingsTransition = UIScreenTransitionConfig.Standard();
        public UIScreenTransitionConfig winTransition = UIScreenTransitionConfig.Standard();
        public UIScreenTransitionConfig timeoutTransition = UIScreenTransitionConfig.Standard();
        public UIScreenTransitionConfig pauseTransition = UIScreenTransitionConfig.Popup();
        public UIScreenTransitionConfig tutorialTransition = UIScreenTransitionConfig.Popup();
        public UIScreenTransitionConfig signInTransition = UIScreenTransitionConfig.Standard();
        public UIScreenTransitionConfig accountTransition = UIScreenTransitionConfig.Standard();
        public UIScreenTransitionConfig leaderboardTransition = UIScreenTransitionConfig.Standard();

        public bool mobileAdaptivePerformanceEnabled = true;
        public int mobileBatteryFrameRate = 30;
        public int mobileBalancedFrameRate = 60;
        public int mobileSmoothFrameRate = 60;
        public int mobilePhysicsTickRate = 60;
        public int mobileLowMemoryThresholdMb = 3072;
        public int mobileHighMemoryThresholdMb = 6144;
        public int mobileLowGpuMemoryThresholdMb = 768;
        public int mobileHighGpuMemoryThresholdMb = 2048;
        public int mobileLowProcessorCount = 4;
        public int mobileHighProcessorCount = 8;
        public float mobileLowEndRenderScale = 0.72f;
        public float mobileBalancedRenderScale = 0.88f;
        public float mobileHighEndRenderScale = 1f;
        public float mobileMinimumRenderScale = 0.65f;
        public float mobileRenderScaleStep = 0.08f;
        public float mobilePerformanceSampleSeconds = 4f;
        public float mobileSlowFrameThreshold = 0.82f;
        public float mobileRecoveryFrameThreshold = 0.96f;
        public int mobileSlowSampleCount = 3;
        public int mobileRecoverySampleCount = 8;
        public int mobileLowQualityLevel = 0;
        public int mobileBalancedQualityLevel = 1;
        public int mobileHighQualityLevel = 2;

        public bool analyticsEnabled = true;
        public bool adsEnabled;
        public bool simulateAdsInEditor = true;
        public int interstitialEveryCompletions = 2;
        public float interstitialMinimumSeconds = 0f;
        public bool bannerEnabled = true;
        public bool bannerUseAdaptiveSize = false;
        public bool bannerRespectSafeArea = true;
        public float bannerLayoutPaddingDp = 4f;
        public string bannerPosition = "BottomCenter";
        public string bannerPlacement = "global_banner";
        public float adLoadRetrySeconds = 8f;
        public bool childDirected;
        public bool doNotSellPersonalInformation;
        public bool levelPlayAdapterDebug;
        public bool validateLevelPlayIntegrationInDevelopment = true;
        public string levelPlayAppKey = "";
        public string rewardedAdUnitId = "";
        public string interstitialAdUnitId = "";
        public string bannerAdUnitId = "";
        public string revivePlacement = "level_revive";
        public string energyRevivePlacement = "energy_revive";
        public string currencyPlacement = "free_gems";
        public string interstitialPlacement = "level_complete";

        public bool firebaseRemoteConfigEnabled = true;
        public string remoteConfigJsonKey = "game_config_json";
        public int remoteFetchMinutes = 60;
        public float remoteFetchTimeoutSeconds = 12f;

        public bool googlePlayGamesEnabled = true;
        // Google Play Games cannot authenticate inside the Unity Editor. When enabled,
        // its Editor button uses a persistent Unity Authentication test identity instead.
        // The branch is compile-time Editor-only and can never run in an Android player.
        public bool editorGooglePlayTestLoginEnabled = true;
        public bool unityServicesEnabled = true;
        public bool guestSignInEnabled = true;
        public bool usernamePasswordEnabled = true;
        public bool guestAccountUpgradeEnabled = true;
        public int usernameMinLength = 3;
        public int usernameMaxLength = 20;
        public int passwordMinLength = 8;
        public int passwordMaxLength = 30;
        public bool cloudProgressEnabled = true;
        public bool unityLeaderboardsEnabled = true;
        public string cloudProgressKey = "player_progress_v1";
        public float leaderboardTimeWeight = 0.7f;
        public float leaderboardEnergyWeight = 0.3f;
        public float threeStarPerformanceThreshold = 0.65f;
        public float twoStarPerformanceThreshold = 0.35f;
        public float leaderboardEnergyRangeMultiplier = 2f;
        public float leaderboardHintPenalty = 0.1f;
        public float leaderboardRevivePenalty = 0.15f;
        public int leaderboardCompletionPoints = 100000;
        public int leaderboardPerformanceBonusPoints = 1000;
        public string allTimeLeaderboardId = "all_time_performance";
        public string[] chapterLeaderboardIds =
        {
            "chapter_01_performance", "chapter_02_performance", "chapter_03_performance",
            "chapter_04_performance", "chapter_05_performance", "chapter_06_performance"
        };
        public string campaignLeaderboardId = "campaign_stars";
        public string endlessLeaderboardId = "endless_depth";

        public void Normalize()
        {
            version = Mathf.Max(1, version);
            baseLevelSeconds = Mathf.Max(5f, baseLevelSeconds);
            chapterLevelSeconds ??= Array.Empty<float>();
            for (var i = 0; i < chapterLevelSeconds.Length; i++)
                chapterLevelSeconds[i] = Mathf.Clamp(chapterLevelSeconds[i], 10f, 600f);
            secondsPerSolutionBeat = Mathf.Max(0f, secondsPerSolutionBeat);
            minimumLevelSeconds = Mathf.Max(10f, minimumLevelSeconds);
            maximumLevelSeconds = Mathf.Max(minimumLevelSeconds, maximumLevelSeconds);
            reviveSeconds = Mathf.Clamp(reviveSeconds, 5f, maximumLevelSeconds);
            maxRevivesPerLevel = Mathf.Clamp(maxRevivesPerLevel, 0, 10);
            energyTargetMultiplier = Mathf.Clamp(energyTargetMultiplier, 0.25f, 2f);
            chapterEnergyTargetMultipliers ??= Array.Empty<float>();
            for (var i = 0; i < chapterEnergyTargetMultipliers.Length; i++)
                chapterEnergyTargetMultipliers[i] = Mathf.Clamp(chapterEnergyTargetMultipliers[i], 0.25f, 2f);
            pushEnergyMultiplier = Mathf.Clamp(pushEnergyMultiplier, 1f, 3f);
            minimumEnergyTarget = Mathf.Clamp(minimumEnergyTarget, 5f, 100f);
            minimumLaterChapterEnergyTarget = Mathf.Clamp(minimumLaterChapterEnergyTarget, 5f, 100f);
            energyFailureMultiplier = Mathf.Clamp(energyFailureMultiplier, 1.05f, 5f);
            energyRevivePercent = Mathf.Clamp(energyRevivePercent, 0.1f, 0.9f);
            startingCoins = Mathf.Max(0, startingCoins);
            coinReviveCost = Mathf.Max(0, coinReviveCost);
            campaignCompletionCoins = Mathf.Max(0, campaignCompletionCoins);
            coinsPerNewStar = Mathf.Max(0, coinsPerNewStar);
            dailyCompletionCoins = Mathf.Max(0, dailyCompletionCoins);
            endlessCompletionCoins = Mathf.Max(0, endlessCompletionCoins);
            rewardedCoinAmount = Mathf.Max(0, rewardedCoinAmount);
            cosmeticGemPrices ??= Array.Empty<int>();
            if (chapterGiftColors == null || chapterGiftColors.Length == 0)
                chapterGiftColors = new[] { "#A8C4D4", "#B3C7B0", "#D1BDA8", "#C2B3D1", "#A6C7C4", "#D1C7A3" };
            giftReminderHours = Mathf.Clamp(giftReminderHours, 1f, 168f);
            gameReminderHours = Mathf.Clamp(gameReminderHours, 12f, 168f);
            firebasePushTopic = string.IsNullOrWhiteSpace(firebasePushTopic) ? "otb_updates" : firebasePushTopic.Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(firebasePushTopic, @"^[a-zA-Z0-9\-_.~%]{1,900}$"))
                firebasePushTopic = "otb_updates";
            if (string.IsNullOrWhiteSpace(giftReminderMessage)) giftReminderMessage = "Your chapter gift is waiting in the store!";
            if (string.IsNullOrWhiteSpace(gameReminderMessage)) gameReminderMessage = "Your next magnetic puzzle is waiting.";
            gameReminderMessages = (gameReminderMessages ?? Array.Empty<string>())
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Select(message => message.Trim()).Distinct().Take(60).ToArray();
            if (gameReminderMessages.Length == 0) gameReminderMessages = new[] { gameReminderMessage };
            cosmeticRarities ??= new[] { 0, 0, 1, 1, 1, 2, 2, 2, 3, 3 };
            cosmeticRarityColors ??= new[] { "#B8CFDF", "#55CCFF", "#C68AFF", "#FFD166" };
            cosmeticArtworkResource = string.IsNullOrWhiteSpace(cosmeticArtworkResource)
                ? "OutsideTheBox/Art/Cosmetics/CosmeticAtlas" : cosmeticArtworkResource.Trim();
            storeRevealSeconds = Mathf.Clamp(storeRevealSeconds, 0.1f, 1f);
            storeRevealStagger = Mathf.Clamp(storeRevealStagger, 0f, 0.1f);
            storePreviewMotionAmount = Mathf.Clamp(storePreviewMotionAmount, 0f, 0.08f);
            storePreviewMotionSpeed = Mathf.Clamp(storePreviewMotionSpeed, 0.1f, 3f);
            for (var i = 0; i < cosmeticGemPrices.Length; i++)
                cosmeticGemPrices[i] = Mathf.Max(0, cosmeticGemPrices[i]);
            cosmeticRewardedViews = Mathf.Clamp(cosmeticRewardedViews, 1, 10);
            cosmeticIapProductIds ??= Array.Empty<string>();
            for (var i = 0; i < cosmeticIapProductIds.Length; i++)
                cosmeticIapProductIds[i] = cosmeticIapProductIds[i]?.Trim() ?? string.Empty;
            cosmeticIapFallbackPrices ??= Array.Empty<string>();
            for (var i = 0; i < cosmeticIapFallbackPrices.Length; i++)
                cosmeticIapFallbackPrices[i] = cosmeticIapFallbackPrices[i]?.Trim() ?? string.Empty;
            gemPacks = (gemPacks ?? GemPackConfig.Defaults())
                .Where(pack => pack != null && GemPurchaseLedger.TryGetPackAmount(pack.productId?.Trim(), out _) &&
                               !cosmeticIapProductIds.Contains(pack.productId?.Trim()))
                .GroupBy(pack => pack.productId.Trim(), StringComparer.Ordinal).Select(group => group.First()).Take(6).ToArray();
            foreach (var pack in gemPacks)
            {
                pack.productId = pack.productId.Trim();
                GemPurchaseLedger.TryGetPackAmount(pack.productId, out pack.gems);
                pack.name = string.IsNullOrWhiteSpace(pack.name) ? "GEM PACK" : pack.name.Trim();
                pack.fallbackPrice = pack.fallbackPrice?.Trim() ?? string.Empty;
            }
            echoRecordSeconds = Mathf.Clamp(echoRecordSeconds, 0.5f, 12f);
            echoSampleRate = Mathf.Clamp(echoSampleRate, 5, 60);
            echoMaximumSamples = Mathf.Clamp(echoMaximumSamples, 8, 360);
            echoStrengthMultiplier = Mathf.Clamp(echoStrengthMultiplier, 0.05f, 2f);
            echoLoopDelay = Mathf.Clamp(echoLoopDelay, 0f, 3f);
            echoEnergyMultiplier = Mathf.Clamp(echoEnergyMultiplier, 0f, 2f);
            ferrofluidMaximumPoints = Mathf.Clamp(ferrofluidMaximumPoints, 8, 256);
            ferrofluidPointSpacing = Mathf.Clamp(ferrofluidPointSpacing, 0.03f, 1f);
            ferrofluidGuidanceStrength = Mathf.Clamp(ferrofluidGuidanceStrength, 0f, 5f);
            magneticImprintDuration = Mathf.Clamp(magneticImprintDuration, 0.25f, 30f);
            magneticImprintStrength = Mathf.Clamp(magneticImprintStrength, 0f, 30f);
            magneticImprintActivationRadius = Mathf.Clamp(magneticImprintActivationRadius, 0.15f, 3f);
            fieldLensDirectionBlend = Mathf.Clamp01(fieldLensDirectionBlend);
            orbitGoalRadiusMultiplier = Mathf.Clamp(orbitGoalRadiusMultiplier, 1.25f, 6f);
            orbitGoalMinimumSpeed = Mathf.Clamp(orbitGoalMinimumSpeed, 0f, 5f);
            imprintPulseSpeed = Mathf.Clamp(imprintPulseSpeed, 0f, 30f);
            fieldLensRotationSpeed = Mathf.Clamp(fieldLensRotationSpeed, 0f, 180f);
            orbitGoalPulseSpeed = Mathf.Clamp(orbitGoalPulseSpeed, 0f, 30f);
            orbitGoalPulseAmount = Mathf.Clamp(orbitGoalPulseAmount, 0f, 0.25f);
            orbitGoalRotationSpeed = Mathf.Clamp(orbitGoalRotationSpeed, 0f, 180f);
            ftueLevelCount = Mathf.Clamp(ftueLevelCount, 0, 10);
            ftueTooltipSeconds = Mathf.Clamp(ftueTooltipSeconds, 1f, 15f);
            uiAnimationSeconds = Mathf.Clamp(uiAnimationSeconds, 0.05f, 1.5f);
            timerMilestoneSeconds = Mathf.Clamp(timerMilestoneSeconds, 1f, 600f);
            timerWarningSeconds = Mathf.Clamp(timerWarningSeconds, 1f, 60f);
            energyWarningFraction = Mathf.Clamp(energyWarningFraction, 0.1f, 1f);
            hudPulseSeconds = Mathf.Clamp(hudPulseSeconds, 0.1f, 2f);
            hudPulseScale = Mathf.Clamp(hudPulseScale, 0f, 0.5f);
            uiPopupStartScale = Mathf.Clamp(uiPopupStartScale, 0.5f, 1f);
            uiButtonPressScale = Mathf.Clamp(uiButtonPressScale, 0.75f, 1f);
            uiButtonSpringFrequency = Mathf.Clamp(uiButtonSpringFrequency, 4f, 40f);
            uiButtonSpringDamping = Mathf.Clamp(uiButtonSpringDamping, 0.35f, 1.4f);
            uiButtonHoverAmount = Mathf.Clamp(uiButtonHoverAmount, 0f, 0.05f);
            uiButtonHoverSpeed = Mathf.Clamp(uiButtonHoverSpeed, 0f, 3f);
            uiAmbientPulseSpeed = Mathf.Clamp(uiAmbientPulseSpeed, 0f, 5f);
            uiStaggerSeconds = Mathf.Clamp(uiStaggerSeconds, 0f, 0.15f);
            uiRevealOffset = Mathf.Clamp(uiRevealOffset, 0f, 100f);
            uiBackdropMotionSpeed = Mathf.Clamp(uiBackdropMotionSpeed, 0f, 3f);
            uiScreenSlideDistance = Mathf.Clamp(uiScreenSlideDistance, 0f, 240f);
            uiScreenStartScale = Mathf.Clamp(uiScreenStartScale, 0.75f, 1f);
            uiScreenOvershoot = Mathf.Clamp(uiScreenOvershoot, 0f, 0.2f);
            loadingMinimumSeconds = Mathf.Clamp(loadingMinimumSeconds, 0f, 3f);
            loadingFadeSeconds = Mathf.Clamp(loadingFadeSeconds, 0.05f, 1f);
            connectionCheckSeconds = Mathf.Clamp(connectionCheckSeconds, 2f, 60f);
            connectionRetrySeconds = Mathf.Clamp(connectionRetrySeconds, 1f, 30f);
            connectionTimeoutSeconds = Mathf.Clamp(connectionTimeoutSeconds, 2, 10);
            connectionFailureThreshold = Mathf.Clamp(connectionFailureThreshold, 1, 3);
            if (!Uri.TryCreate(connectionCheckUrl, UriKind.Absolute, out var connectionUri) ||
                connectionUri.Scheme != Uri.UriSchemeHttps)
                connectionCheckUrl = "https://www.gstatic.com/generate_204";
            if (!Uri.TryCreate(connectionFallbackUrl, UriKind.Absolute, out var fallbackUri) ||
                fallbackUri.Scheme != Uri.UriSchemeHttps)
                connectionFallbackUrl = "https://connectivitycheck.gstatic.com/generate_204";
            loadingStartupMessage = string.IsNullOrWhiteSpace(loadingStartupMessage)
                ? "SYNCING PLAYER DATA"
                : loadingStartupMessage.Trim();
            loadingLevelMessage = string.IsNullOrWhiteSpace(loadingLevelMessage)
                ? "BUILDING LEVEL"
                : loadingLevelMessage.Trim();
            loadingMenuMessage = string.IsNullOrWhiteSpace(loadingMenuMessage)
                ? "RETURNING TO MENU"
                : loadingMenuMessage.Trim();
            mainMenuTransition ??= UIScreenTransitionConfig.Standard();
            gameplayTransition ??= UIScreenTransitionConfig.Standard();
            chapterSelectTransition ??= UIScreenTransitionConfig.Standard();
            levelSelectTransition ??= UIScreenTransitionConfig.Standard();
            settingsTransition ??= UIScreenTransitionConfig.Standard();
            winTransition ??= UIScreenTransitionConfig.Standard();
            timeoutTransition ??= UIScreenTransitionConfig.Standard();
            pauseTransition ??= UIScreenTransitionConfig.Popup();
            tutorialTransition ??= UIScreenTransitionConfig.Popup();
            signInTransition ??= UIScreenTransitionConfig.Standard();
            accountTransition ??= UIScreenTransitionConfig.Standard();
            leaderboardTransition ??= UIScreenTransitionConfig.Standard();
            mainMenuTransition.Normalize();
            gameplayTransition.Normalize();
            chapterSelectTransition.Normalize();
            levelSelectTransition.Normalize();
            settingsTransition.Normalize();
            winTransition.Normalize();
            timeoutTransition.Normalize();
            pauseTransition.Normalize();
            tutorialTransition.Normalize();
            signInTransition.Normalize();
            accountTransition.Normalize();
            leaderboardTransition.Normalize();
            usernameMinLength = Mathf.Clamp(usernameMinLength, 3, 20);
            usernameMaxLength = Mathf.Clamp(usernameMaxLength, usernameMinLength, 20);
            passwordMinLength = Mathf.Clamp(passwordMinLength, 8, 30);
            passwordMaxLength = Mathf.Clamp(passwordMaxLength, passwordMinLength, 30);
            cloudProgressKey = string.IsNullOrWhiteSpace(cloudProgressKey)
                ? "player_progress_v1"
                : cloudProgressKey.Trim();
            leaderboardTimeWeight = Mathf.Clamp01(leaderboardTimeWeight);
            leaderboardEnergyWeight = Mathf.Clamp01(leaderboardEnergyWeight);
            if (leaderboardTimeWeight + leaderboardEnergyWeight <= 0.001f)
            {
                leaderboardTimeWeight = 0.7f;
                leaderboardEnergyWeight = 0.3f;
            }
            threeStarPerformanceThreshold = Mathf.Clamp01(threeStarPerformanceThreshold);
            twoStarPerformanceThreshold = Mathf.Clamp(twoStarPerformanceThreshold, 0f, threeStarPerformanceThreshold);
            leaderboardEnergyRangeMultiplier = Mathf.Clamp(leaderboardEnergyRangeMultiplier, 1.05f, 5f);
            leaderboardHintPenalty = Mathf.Clamp01(leaderboardHintPenalty);
            leaderboardRevivePenalty = Mathf.Clamp(leaderboardRevivePenalty, 0f, 0.5f);
            leaderboardCompletionPoints = Mathf.Max(1, leaderboardCompletionPoints);
            leaderboardPerformanceBonusPoints = Mathf.Clamp(leaderboardPerformanceBonusPoints, 0, 10000);
            allTimeLeaderboardId = string.IsNullOrWhiteSpace(allTimeLeaderboardId)
                ? "all_time_performance"
                : allTimeLeaderboardId.Trim();
            chapterLeaderboardIds ??= Array.Empty<string>();
            for (var i = 0; i < chapterLeaderboardIds.Length; i++)
                chapterLeaderboardIds[i] = chapterLeaderboardIds[i]?.Trim() ?? string.Empty;
            mobileBatteryFrameRate = Mathf.Clamp(mobileBatteryFrameRate, 24, 60);
            mobileBalancedFrameRate = Mathf.Clamp(mobileBalancedFrameRate, 30, 120);
            mobileSmoothFrameRate = Mathf.Clamp(mobileSmoothFrameRate, 30, 120);
            mobilePhysicsTickRate = Mathf.Clamp(mobilePhysicsTickRate, 30, 120);
            mobileLowMemoryThresholdMb = Mathf.Clamp(mobileLowMemoryThresholdMb, 1024, 8192);
            mobileHighMemoryThresholdMb = Mathf.Max(mobileLowMemoryThresholdMb, mobileHighMemoryThresholdMb);
            mobileLowGpuMemoryThresholdMb = Mathf.Clamp(mobileLowGpuMemoryThresholdMb, 128, 4096);
            mobileHighGpuMemoryThresholdMb = Mathf.Max(mobileLowGpuMemoryThresholdMb, mobileHighGpuMemoryThresholdMb);
            mobileLowProcessorCount = Mathf.Clamp(mobileLowProcessorCount, 1, 16);
            mobileHighProcessorCount = Mathf.Max(mobileLowProcessorCount, mobileHighProcessorCount);
            mobileMinimumRenderScale = Mathf.Clamp(mobileMinimumRenderScale, 0.5f, 1f);
            mobileLowEndRenderScale = Mathf.Clamp(mobileLowEndRenderScale, mobileMinimumRenderScale, 1f);
            mobileBalancedRenderScale = Mathf.Clamp(mobileBalancedRenderScale, mobileLowEndRenderScale, 1f);
            mobileHighEndRenderScale = Mathf.Clamp(mobileHighEndRenderScale, mobileBalancedRenderScale, 1f);
            mobileRenderScaleStep = Mathf.Clamp(mobileRenderScaleStep, 0.02f, 0.2f);
            mobilePerformanceSampleSeconds = Mathf.Clamp(mobilePerformanceSampleSeconds, 2f, 15f);
            mobileSlowFrameThreshold = Mathf.Clamp(mobileSlowFrameThreshold, 0.5f, 0.98f);
            mobileRecoveryFrameThreshold = Mathf.Clamp(mobileRecoveryFrameThreshold,
                mobileSlowFrameThreshold, 1.05f);
            mobileSlowSampleCount = Mathf.Clamp(mobileSlowSampleCount, 1, 10);
            mobileRecoverySampleCount = Mathf.Clamp(mobileRecoverySampleCount, 1, 30);
            mobileLowQualityLevel = Mathf.Max(0, mobileLowQualityLevel);
            mobileBalancedQualityLevel = Mathf.Max(0, mobileBalancedQualityLevel);
            mobileHighQualityLevel = Mathf.Max(0, mobileHighQualityLevel);
            interstitialEveryCompletions = Mathf.Max(0, interstitialEveryCompletions);
            interstitialMinimumSeconds = Mathf.Clamp(interstitialMinimumSeconds, 0f, 1800f);
            bannerPosition = string.IsNullOrWhiteSpace(bannerPosition) ? "BottomCenter" : bannerPosition.Trim();
            bannerLayoutPaddingDp = Mathf.Clamp(bannerLayoutPaddingDp, 0f, 48f);
            bannerPlacement = string.IsNullOrWhiteSpace(bannerPlacement) ? "global_banner" : bannerPlacement.Trim();
            adLoadRetrySeconds = Mathf.Clamp(adLoadRetrySeconds, 1f, 120f);
            remoteFetchMinutes = Mathf.Clamp(remoteFetchMinutes, 0, 1440);
            remoteFetchTimeoutSeconds = Mathf.Clamp(remoteFetchTimeoutSeconds, 2f, 60f);
            remoteConfigJsonKey = string.IsNullOrWhiteSpace(remoteConfigJsonKey)
                ? "game_config_json"
                : remoteConfigJsonKey.Trim();
        }
    }

    public static class PerformanceBalance
    {
        public static float CalculateRating(float elapsed, float timeLimit, float energyUsed, float energyTarget,
            bool hintUsed, int revivesUsed, LiveOpsConfigData settings)
        {
            var timeEfficiency = 1f - Mathf.Clamp01(elapsed / Mathf.Max(1f, timeLimit));
            var energyRatio = energyUsed / Mathf.Max(0.01f, energyTarget);
            var energyEfficiency = 1f - Mathf.Clamp01(energyRatio / settings.leaderboardEnergyRangeMultiplier);
            if (!settings.timersEnabled || timeLimit <= 0f) timeEfficiency = energyEfficiency;
            var weightTotal = Mathf.Max(0.001f, settings.leaderboardTimeWeight + settings.leaderboardEnergyWeight);
            var rating = (timeEfficiency * settings.leaderboardTimeWeight + energyEfficiency * settings.leaderboardEnergyWeight) / weightTotal;
            var penalty = (hintUsed ? settings.leaderboardHintPenalty : 0f) + Mathf.Max(0, revivesUsed) * settings.leaderboardRevivePenalty;
            return Mathf.Clamp01(rating * Mathf.Clamp01(1f - penalty));
        }

        public static int CalculateStars(float rating, bool hintUsed, LiveOpsConfigData settings)
        {
            var stars = rating >= settings.threeStarPerformanceThreshold ? 3 : rating >= settings.twoStarPerformanceThreshold ? 2 : 1;
            return hintUsed ? Mathf.Min(2, stars) : stars;
        }
    }

    public static class EnergyBalance
    {
        public static float CalculateTarget(float baseTarget, int chapterIndex, LiveOpsConfigData settings)
        {
            if (settings == null) settings = new LiveOpsConfigData();
            var multipliers = settings.chapterEnergyTargetMultipliers;
            var chapterMultiplier = multipliers != null && chapterIndex >= 0 && chapterIndex < multipliers.Length
                ? Mathf.Clamp(multipliers[chapterIndex], 0.25f, 2f)
                : 1f;
            var minimum = chapterIndex > 0 ? settings.minimumLaterChapterEnergyTarget : settings.minimumEnergyTarget;
            return Mathf.Max(minimum,
                baseTarget * settings.energyTargetMultiplier * chapterMultiplier);
        }

        public static float CalculateUsage(float perSecond, float perDistance, float elapsedSeconds,
            float distance, bool inputHeld, ForceMode mode, bool echoActive, ForceMode echoMode,
            LiveOpsConfigData settings)
        {
            if (settings == null) settings = new LiveOpsConfigData();
            var pushMultiplier = Mathf.Clamp(settings.pushEnergyMultiplier, 1f, 3f);
            var result = inputHeld
                ? (Mathf.Max(0f, perSecond) * Mathf.Max(0f, elapsedSeconds) +
                   Mathf.Max(0f, perDistance) * Mathf.Max(0f, distance)) *
                  (mode == ForceMode.Push ? pushMultiplier : 1f)
                : 0f;
            if (echoActive)
                result += Mathf.Max(0f, perSecond) * Mathf.Max(0f, settings.echoEnergyMultiplier) *
                          Mathf.Max(0f, elapsedSeconds) * (echoMode == ForceMode.Push ? pushMultiplier : 1f);
            return result;
        }
    }

    public static class LiveOpsConfigStore
    {
        private const string CacheKey = "otb_remote_live_config_v1";
        private static LiveOpsConfigData defaults = new();
        private static readonly LiveOpsConfigData EmergencyFallback = new();

        /// <summary>
        /// Runtime-only ScriptableObject containing the currently active values. This is a clone,
        /// so Firebase Remote Config never dirties or overwrites the project asset.
        /// </summary>
        public static LiveOpsConfigAsset ActiveAsset { get; private set; }

        /// <summary>
        /// Compatibility accessor used by gameplay systems. Values always come from ActiveAsset.
        /// </summary>
        public static LiveOpsConfigData Current =>
            ActiveAsset != null && ActiveAsset.values != null ? ActiveAsset.values : EmergencyFallback;

        public static event Action Changed;

        public static void Initialize()
        {
            var configAsset = Resources.Load<LiveOpsConfigAsset>("OutsideTheBox/Configs/LiveOpsConfig");
            var defaultJson = Resources.Load<TextAsset>("OutsideTheBox/default_live_config");
            defaults = configAsset != null
                ? configAsset.CreateRuntimeCopy()
                : Parse(defaultJson != null ? defaultJson.text : null) ?? new LiveOpsConfigData();
            defaults.Normalize();

            var activeValues = Clone(defaults);
            var cached = PlayerPrefs.GetString(CacheKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(cached)) activeValues = Parse(cached, defaults) ?? activeValues;
            activeValues.Normalize();
            ApplyToRuntimeAsset(activeValues);
        }

        public static bool ApplyRemoteJson(string json)
        {
            var parsed = Parse(json, defaults);
            if (parsed == null) return false;
            parsed.Normalize();
            ApplyToRuntimeAsset(parsed);
            PlayerPrefs.SetString(CacheKey, json);
            PlayerPrefs.Save();
            return true;
        }

        public static float CalculateTimeLimit(int solutionBeats, int chapterIndex = -1)
        {
            var settings = Current ?? new LiveOpsConfigData();
            if (!settings.timersEnabled) return 0f;
            if (chapterIndex >= 0 && settings.chapterLevelSeconds != null && chapterIndex < settings.chapterLevelSeconds.Length)
                return settings.chapterLevelSeconds[chapterIndex];
            return Mathf.Clamp(settings.baseLevelSeconds + Mathf.Max(1, solutionBeats) * settings.secondsPerSolutionBeat,
                settings.minimumLevelSeconds, settings.maximumLevelSeconds);
        }

        private static LiveOpsConfigData Parse(string json, LiveOpsConfigData baseValues = null)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                // Start from code defaults so older Firebase payloads can omit newly added
                // fields without silently turning a mechanic off or zeroing its tuning.
                var parsed = baseValues != null ? Clone(baseValues) : new LiveOpsConfigData();
                JsonUtility.FromJsonOverwrite(json, parsed);
                if (parsed == null || parsed.version <= 0) return null;
                return parsed;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Live config JSON was rejected; using the last valid values. {exception.Message}");
                return null;
            }
        }

        private static LiveOpsConfigData Clone(LiveOpsConfigData source)
        {
            var clone = new LiveOpsConfigData();
            if (source != null) JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), clone);
            return clone;
        }

        private static void ApplyToRuntimeAsset(LiveOpsConfigData values)
        {
            if (ActiveAsset == null)
            {
                ActiveAsset = ScriptableObject.CreateInstance<LiveOpsConfigAsset>();
                ActiveAsset.name = "LiveOpsConfig (Runtime)";
                ActiveAsset.hideFlags = HideFlags.DontSave;
            }

            ActiveAsset.ReplaceRuntimeValues(values);
            Changed?.Invoke();
        }
    }

    public static class EconomyService
    {
        public static int Balance => Mathf.Max(0, SaveService.Data?.softCurrency ?? 0);

        public static void Initialize(int startingCoins)
        {
            if (SaveService.Data == null || SaveService.Data.currencyInitialized) return;
            SaveService.Data.currencyInitialized = true;
            SaveService.Data.softCurrency = Mathf.Max(0, startingCoins);
            SaveService.Save();
            AnalyticsService.Log("currency_granted", "amount", SaveService.Data.softCurrency);
        }

        public static void Add(int amount, string source)
        {
            if (SaveService.Data == null || amount <= 0) return;
            SaveService.Data.softCurrency = Mathf.Max(0, SaveService.Data.softCurrency + amount);
            SaveService.Data.lifetimeCurrencyEarned += amount;
            SaveService.SaveDeferred();
            AnalyticsService.Log("earn_virtual_currency", ("value", amount), ("virtual_currency_name", "gems"), ("source", source ?? "unknown"), ("balance", SaveService.Data.softCurrency));
        }

        public static bool TrySpend(int amount, string sink)
        {
            if (SaveService.Data == null || amount < 0 || SaveService.Data.softCurrency < amount) return false;
            SaveService.Data.softCurrency -= amount;
            SaveService.Data.lifetimeCurrencySpent += amount;
            SaveService.SaveDeferred();
            AnalyticsService.Log("spend_virtual_currency", ("value", amount), ("virtual_currency_name", "gems"), ("item_name", sink ?? "unknown"), ("balance", SaveService.Data.softCurrency));
            return true;
        }
    }

    /// <summary>One analytics gateway: bounded parameters, anonymous gameplay context and no UI input text.</summary>
    public static class AnalyticsService
    {
        private static Type analyticsType;
        private static Type parameterType;
        private static bool lookedUp;
        private static bool? collectionEnabled;
        private static bool warned;
        private static int level;
        private static int chapter;
        private static int attempt;
        private static string mode = "none";
        public static string CurrentScreen { get; private set; } = "startup";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            analyticsType = parameterType = null;
            lookedUp = warned = false;
            collectionEnabled = null;
            level = chapter = attempt = 0;
            mode = "none";
            CurrentScreen = "startup";
        }

        public static void Configure()
        {
            var enabled = LiveOpsConfigStore.Current.analyticsEnabled;
            if (collectionEnabled == enabled) return;
            EnsureType();
            try
            {
                if (analyticsType != null)
                    analyticsType.GetMethod("SetAnalyticsCollectionEnabled")?.Invoke(null, new object[] { enabled });
#if UNITY_ANDROID && !UNITY_EDITOR
                else
                    WithAndroid(analytics => analytics.Call("setAnalyticsCollectionEnabled", enabled));
#endif
                collectionEnabled = enabled;
            }
            catch { WarnOnce(); }
        }

        public static void BeginLevel(int number, int chapterNumber, string gameMode)
        {
            level = number;
            chapter = chapterNumber;
            mode = gameMode;
            attempt++;
        }

        // Opaque Unity Authentication ID only. Never pass a username, email, password or token.
        public static void SetPlayer(string playerId)
        {
            if (!Application.isPlaying) return;
            Configure();
            if (!LiveOpsConfigStore.Current.analyticsEnabled) playerId = null;
            try
            {
                EnsureType();
                if (analyticsType != null)
                    analyticsType.GetMethod("SetUserId")?.Invoke(null, new object[] { playerId });
#if UNITY_ANDROID && !UNITY_EDITOR
                else WithAndroid(analytics => analytics.Call("setUserId", playerId));
#endif
            }
            catch { WarnOnce(); }
        }

        public static void ClearLevel()
        {
            level = chapter = 0;
            mode = "none";
        }

        public static void Screen(string name)
        {
            var next = Sanitize(name);
            if (!Application.isPlaying || next == CurrentScreen) return;
            var previous = CurrentScreen;
            CurrentScreen = next;
            Log("screen_view", ("screen_name", next), ("screen_class", "GameUIController"),
                ("previous_screen", previous));
        }

        public static void Log(string name) => Log(name, Array.Empty<(string, object)>());
        public static void Log(string name, string key, long value) => Log(name, (key, (object)value));
        public static void Log(string name, string key, int value) => Log(name, key, (long)value);
        public static void Log(string name, string key, string value) => Log(name, (key, (object)value));

        public static void Log(string name, params (string key, object value)[] values)
        {
            // Content generation runs this same code in Edit Mode: never send editor authoring events.
            if (!Application.isPlaying) return;
            Configure();
            if (!LiveOpsConfigStore.Current.analyticsEnabled) return;
            var eventName = Sanitize(name);
            var data = BuildParameters(values);
            try
            {
#if UNITY_EDITOR
                // Preview only. Editor playtests never pollute the Android production stream.
                Debug.Log($"[Analytics Preview] {eventName} " +
                    string.Join(", ", data.Select(pair => $"{pair.Key}={pair.Value}")));
#else
                EnsureType();
                if (analyticsType != null && parameterType != null)
                {
                    var parameters = Array.CreateInstance(parameterType, data.Count);
                    var i = 0;
                    foreach (var pair in data)
                        parameters.SetValue(Activator.CreateInstance(parameterType, pair.Key, pair.Value), i++);
                    analyticsType.GetMethod("LogEvent", new[] { typeof(string), parameters.GetType() })
                        ?.Invoke(null, new object[] { eventName, parameters });
                }
#if UNITY_ANDROID
                else
                    WithAndroid(analytics =>
                    {
                        using (var bundle = new AndroidJavaObject("android.os.Bundle"))
                        {
                            foreach (var pair in data)
                            {
                                if (pair.Value is long integer) bundle.Call("putLong", pair.Key, integer);
                                else if (pair.Value is double number) bundle.Call("putDouble", pair.Key, number);
                                else bundle.Call("putString", pair.Key, (string)pair.Value);
                            }
                            analytics.Call("logEvent", eventName, bundle);
                        }
                    });
#endif
#endif
            }
            // Analytics must never interrupt navigation, rewards, authentication or gameplay.
            catch { WarnOnce(); }
        }

        private static Dictionary<string, object> BuildParameters((string key, object value)[] values)
        {
            var data = new Dictionary<string, object>
            {
                ["screen"] = CurrentScreen,
                ["level"] = (long)level,
                ["chapter"] = (long)chapter,
                ["mode"] = mode,
                ["attempt"] = (long)attempt,
                ["config_version"] = (long)LiveOpsConfigStore.Current.version,
                ["test_build"] = Debug.isDebugBuild ? 1L : 0L
            };
            return AppendParameters(data, values);
        }

        private static Dictionary<string, object> AppendParameters(Dictionary<string, object> data,
            (string key, object value)[] values)
        {
            if (values != null)
                foreach (var pair in values)
                {
                    var key = Sanitize(pair.key);
                    if (key.Contains("password") || key.Contains("token") || key.Contains("receipt") ||
                        key is "username" or "email" or "auth_code" or "advertising_id") continue;
                    if (!data.ContainsKey(key) && data.Count >= 25) continue;
                    if (pair.value is bool boolean) data[key] = boolean ? 1L : 0L;
                    else if (pair.value is byte or short or int or long)
                        data[key] = Convert.ToInt64(pair.value);
                    else if (pair.value is float or double or decimal)
                    {
                        var number = Convert.ToDouble(pair.value);
                        if (!double.IsNaN(number) && !double.IsInfinity(number)) data[key] = number;
                    }
                    else if (pair.value is string text)
                        data[key] = text.Length > 100 ? text.Substring(0, 100) : text;
                }
            return data;
        }

        private static void EnsureType()
        {
            if (lookedUp) return;
            lookedUp = true;
            analyticsType = ReflectionUtility.FindType("Firebase.Analytics.FirebaseAnalytics");
            parameterType = ReflectionUtility.FindType("Firebase.Analytics.Parameter");
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void WithAndroid(Action<AndroidJavaObject> action)
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var type = new AndroidJavaClass("com.google.firebase.analytics.FirebaseAnalytics"))
            using (var analytics = type.CallStatic<AndroidJavaObject>("getInstance", activity))
                action(analytics);
        }
#endif

        private static void WarnOnce()
        {
            if (warned) return;
            warned = true;
            Debug.LogWarning("Firebase Analytics is unavailable. Gameplay continues; check Firebase dependencies and Android resources.");
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "unknown";
            var result = new System.Text.StringBuilder();
            foreach (var character in value.Trim().ToLowerInvariant())
            {
                if (result.Length == 40) break;
                result.Append(character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_' ? character : '_');
            }
            if (result.Length == 0 || result[0] < 'a' || result[0] > 'z') result.Insert(0, "e");
            if (result.Length > 40) result.Length = 40;
            var name = result.ToString();
            if (name.StartsWith("firebase_") || name.StartsWith("google_") || name.StartsWith("ga_"))
                name = "otb_" + name;
            return name.Length > 40 ? name.Substring(0, 40) : name;
        }
    }
#pragma warning disable CS0618 // Google Play Games' Unity plugin currently installs through Unity's Social facade.
    public sealed class PlayGamesService
    {
        private LiveOpsConfigData config;
        public bool IsAuthenticated => Social.localUser != null && Social.localUser.authenticated;

        public void Initialize(LiveOpsConfigData settings)
        {
            config = settings;
            if (!config.googlePlayGamesEnabled) return;
            try
            {
                var platformType = ReflectionUtility.FindType("GooglePlayGames.PlayGamesPlatform");
                platformType?.GetMethod("Activate", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Google Play Games activation failed: {exception.GetBaseException().Message}");
            }
        }

        public void Authenticate(Action<bool> complete)
        {
            if (!config.googlePlayGamesEnabled)
            {
                complete?.Invoke(false);
                return;
            }
            if (IsAuthenticated)
            {
                complete?.Invoke(true);
                return;
            }
            Social.localUser.Authenticate(success =>
            {
                AnalyticsService.Log("play_games_login", "success", success ? 1 : 0);
                complete?.Invoke(success);
            });
        }

        public void ShowLeaderboard(Action<bool> complete)
        {
            Authenticate(success =>
            {
                if (!success || string.IsNullOrWhiteSpace(config.campaignLeaderboardId))
                {
                    complete?.Invoke(false);
                    return;
                }
                Social.ShowLeaderboardUI();
                complete?.Invoke(true);
            });
        }

        public void ReportCampaignScore(int totalStars)
        {
            if (!IsAuthenticated || string.IsNullOrWhiteSpace(config.campaignLeaderboardId)) return;
            Social.ReportScore(Mathf.Max(0, totalStars), config.campaignLeaderboardId,
                success => AnalyticsService.Log("leaderboard_score", "success", success ? 1 : 0));
        }

        public void ReportEndlessScore(int level)
        {
            if (!IsAuthenticated || string.IsNullOrWhiteSpace(config.endlessLeaderboardId)) return;
            Social.ReportScore(Mathf.Max(0, level), config.endlessLeaderboardId,
                success => AnalyticsService.Log("endless_leaderboard", "success", success ? 1 : 0));
        }
    }
#pragma warning restore CS0618

    public sealed class LevelPlayAdService
    {
        private readonly MonoBehaviour host;
        private LiveOpsConfigData config;
        private LevelPlayRewardedAd rewardedAd;
        private LevelPlayInterstitialAd interstitialAd;
        private LevelPlayBannerAd bannerAd;
        private bool initialized;
        private bool initializing;
        private bool consentPending;
        private bool bannerWanted;
        private bool bannerLoaded;
        private float bannerHeightDp = 50f;
        private float displayPixelsPerDp = 1f;
        private int nativeDisplayHeight;
        private Vector2Int metricsScreenSize;
        private bool interstitialShowing;
        private bool shuttingDown;
        private Action<bool> rewardCallback;
        private string rewardPlacement;
        private bool rewardGranted;
        private int completionsSinceInterstitial;
        private float lastInterstitialRealtime = float.NegativeInfinity;

        public bool RewardedReady => Application.isEditor && config != null && config.simulateAdsInEditor ||
                                     rewardedAd != null && rewardedAd.IsAdReady();
        public bool IsInitialized => initialized;

        // Native ads sit above the Unity surface. Keep their density-independent
        // height in Unity screen pixels, including devices using reduced render resolution.
        public float BannerReservedHeightPixels => bannerWanted && !shuttingDown && config != null &&
            config.adsEnabled && config.bannerEnabled && !string.IsNullOrWhiteSpace(config.bannerAdUnitId) &&
            !Application.isEditor
                ? (bannerHeightDp + config.bannerLayoutPaddingDp) * displayPixelsPerDp *
                  (nativeDisplayHeight > 0 ? Screen.height / (float)nativeDisplayHeight : 1f) : 0f;
        public bool BannerAtTop => config?.bannerPosition?.StartsWith("Top", StringComparison.Ordinal) == true;
        public bool BannerRespectsSafeArea => config?.bannerRespectSafeArea ?? true;
        public void RefreshBannerLayoutMetrics()
        {
            if (metricsScreenSize.x != Screen.width || metricsScreenSize.y != Screen.height)
                ReadDisplayMetrics();
        }

        public LevelPlayAdService(MonoBehaviour coroutineHost) => host = coroutineHost;

        public void Initialize(LiveOpsConfigData settings)
        {
            config = settings;
            ReadDisplayMetrics();
            // The LevelPlay Android runtime is not a valid in-Editor ad surface. Running it
            // while editor simulation is enabled produces a placeholder banner and, with
            // some SDK versions, repeated LevelPlayAdInfo.GetAdSize null exceptions. Rewarded
            // and interstitial editor flows are already simulated below, so keep the native
            // SDK for actual device builds only.
            if (Application.isEditor && config != null && config.simulateAdsInEditor)
            {
                initialized = true;
                bannerWanted = false;
                return;
            }
            if (initialized || initializing || consentPending || !config.adsEnabled ||
                string.IsNullOrWhiteSpace(config.levelPlayAppKey)) return;
            var consent = GoogleUmpConsent.Ensure(host.gameObject);
            if (consent == null) return;
            consentPending = true;
            consent.EnsureReady(true, allowed =>
            {
                consentPending = false;
                if (!allowed)
                {
                    Debug.LogWarning("Ads stay disabled because Google UMP did not authorize an ad request.");
                    AnalyticsService.Log("ads_blocked_by_consent");
                    return;
                }
                InitializeSdk();
            });
        }

        private void InitializeSdk()
        {
            if (initialized || initializing) return;
            ApplyPrivacySettings();
            LevelPlay.OnInitSuccess -= OnInitialized;
            LevelPlay.OnInitFailed -= OnInitializationFailed;
            LevelPlay.OnInitSuccess += OnInitialized;
            LevelPlay.OnInitFailed += OnInitializationFailed;
            initializing = true;
            try
            {
                LevelPlay.SetAdaptersDebug(config.levelPlayAdapterDebug);
                LevelPlay.Init(config.levelPlayAppKey);
            }
            catch (Exception exception)
            {
                initializing = false;
                Debug.LogWarning($"LevelPlay initialization failed: {exception.GetBaseException().Message}");
            }
        }

        public bool ShowRewarded(string placement, Action<bool> complete)
        {
            if (rewardCallback == null) rewardPlacement = placement;
            if (Application.isEditor && config != null && config.simulateAdsInEditor)
            {
                if (rewardCallback != null) return false;
                rewardCallback = complete;
                rewardGranted = false;
                host.StartCoroutine(CompleteSimulatedReward());
                AnalyticsService.Log("rewarded_requested", "placement", placement);
                return true;
            }
            if (!RewardedReady || rewardCallback != null)
            {
                AnalyticsService.Log("rewarded_unavailable", "placement", placement);
                return false;
            }
            rewardCallback = complete;
            rewardGranted = false;
            AnalyticsService.Log("rewarded_requested", "placement", placement);
            try
            {
                if (!string.IsNullOrWhiteSpace(placement) && LevelPlayRewardedAd.IsPlacementCapped(placement))
                {
                    rewardCallback = null;
                    AnalyticsService.Log("rewarded_capped", "placement", placement);
                    return false;
                }
                rewardedAd.ShowAd(string.IsNullOrWhiteSpace(placement) ? null : placement);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"LevelPlay rewarded ad could not be shown: {exception.Message}");
                rewardCallback = null;
                return false;
            }
        }

        public void RecordCompletionAndMaybeShowInterstitial()
        {
            if (Application.isEditor && config != null && config.simulateAdsInEditor)
            {
                completionsSinceInterstitial++;
                if (config.interstitialEveryCompletions > 0 &&
                    completionsSinceInterstitial >= config.interstitialEveryCompletions)
                {
                    completionsSinceInterstitial = 0;
                    Debug.Log("[Ad Simulation] Interstitial would be shown after this completion.");
                }
                return;
            }
            if (!initialized || config.interstitialEveryCompletions <= 0) return;
            completionsSinceInterstitial++;
            if (completionsSinceInterstitial < config.interstitialEveryCompletions) return;
            if (interstitialAd == null || interstitialShowing || !interstitialAd.IsAdReady()) return;
            if (Time.realtimeSinceStartup - lastInterstitialRealtime < config.interstitialMinimumSeconds) return;
            if (!string.IsNullOrWhiteSpace(config.interstitialPlacement) &&
                LevelPlayInterstitialAd.IsPlacementCapped(config.interstitialPlacement)) return;
            try
            {
                interstitialShowing = true;
                interstitialAd.ShowAd(string.IsNullOrWhiteSpace(config.interstitialPlacement)
                    ? null
                    : config.interstitialPlacement);
                AnalyticsService.Log("interstitial_requested", "placement", config.interstitialPlacement);
            }
            catch (Exception exception)
            {
                interstitialShowing = false;
                Debug.LogWarning($"LevelPlay interstitial could not be shown: {exception.Message}");
                interstitialAd.LoadAd();
            }
        }

        public void SetBannerVisible(bool visible)
        {
            bannerWanted = visible;
            if (bannerAd == null || !config.bannerEnabled) return;
            try
            {
                if (visible)
                {
                    bannerAd.ShowAd();
                    bannerAd.ResumeAutoRefresh();
                }
                else
                {
                    bannerAd.PauseAutoRefresh();
                    bannerAd.HideAd();
                }
            }
            catch { /* An unloaded banner is allowed to ignore visibility changes. */ }
        }

        private void ReadDisplayMetrics()
        {
            displayPixelsPerDp = Screen.dpi > 0 ? Screen.dpi / 160f : 1f;
            nativeDisplayHeight = Screen.height;
            metricsScreenSize = new Vector2Int(Screen.width, Screen.height);
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
                using var resources = activity.Call<AndroidJavaObject>("getResources");
                using var metrics = resources.Call<AndroidJavaObject>("getDisplayMetrics");
                displayPixelsPerDp = metrics.Get<float>("density");
                nativeDisplayHeight = metrics.Get<int>("heightPixels");
                using var window = activity.Call<AndroidJavaObject>("getWindow");
                using var decor = window.Call<AndroidJavaObject>("getDecorView");
                var windowHeight = decor.Call<int>("getHeight");
                if (windowHeight > 0) nativeDisplayHeight = windowHeight;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Banner display metrics unavailable: {exception.Message}");
            }
#endif
        }

        public void OnApplicationPause(bool paused)
        {
            if (bannerAd == null || !bannerLoaded) return;
            if (paused || !bannerWanted) bannerAd.PauseAutoRefresh();
            else bannerAd.ResumeAutoRefresh();
        }

        public bool LaunchTestSuite()
        {
            if (!initialized || (!Application.isEditor && !Debug.isDebugBuild)) return false;
            LevelPlay.LaunchTestSuite();
            return true;
        }

        public void Shutdown()
        {
            if (shuttingDown) return;
            shuttingDown = true;
            LevelPlay.OnInitSuccess -= OnInitialized;
            LevelPlay.OnInitFailed -= OnInitializationFailed;
            UnsubscribeAdCallbacks();
            rewardedAd?.DestroyAd();
            interstitialAd?.DestroyAd();
            bannerAd?.DestroyAd();
            rewardedAd = null;
            interstitialAd = null;
            bannerAd = null;
            bannerLoaded = false;
            initialized = false;
            initializing = false;
            FinishReward(false);
        }

        private void OnInitialized(LevelPlayConfiguration configuration)
        {
            if (shuttingDown) return;
            initializing = false;
            initialized = true;
            CreateAdObjects();
            if (config.validateLevelPlayIntegrationInDevelopment && (Application.isEditor || Debug.isDebugBuild))
                LevelPlay.ValidateIntegration();
            rewardedAd?.LoadAd();
            interstitialAd?.LoadAd();
            bannerAd?.LoadAd();
            AnalyticsService.Log("ads_initialized");
        }

        private void OnInitializationFailed(LevelPlayInitError error)
        {
            initializing = false;
            Debug.LogWarning($"LevelPlay initialization failed: {error}");
            AnalyticsService.Log("ads_init_failed");
        }

        private void CreateAdObjects()
        {
            if (!string.IsNullOrWhiteSpace(config.rewardedAdUnitId))
            {
                rewardedAd = new LevelPlayRewardedAd(config.rewardedAdUnitId);
                rewardedAd.OnAdRewarded += OnRewarded;
                rewardedAd.OnAdClicked += OnRewardedClicked;
                rewardedAd.OnAdDisplayed += OnRewardedDisplayed;
                rewardedAd.OnAdClosed += OnRewardedClosed;
                rewardedAd.OnAdDisplayFailed += OnRewardedDisplayFailed;
                rewardedAd.OnAdLoadFailed += OnRewardedLoadFailed;
            }
            else Debug.LogWarning("LevelPlay rewarded ad unit ID is missing.");

            if (!string.IsNullOrWhiteSpace(config.interstitialAdUnitId))
            {
                interstitialAd = new LevelPlayInterstitialAd(config.interstitialAdUnitId);
                interstitialAd.OnAdDisplayed += OnInterstitialDisplayed;
                interstitialAd.OnAdClicked += OnInterstitialClicked;
                interstitialAd.OnAdClosed += OnInterstitialClosed;
                interstitialAd.OnAdDisplayFailed += OnInterstitialDisplayFailed;
                interstitialAd.OnAdLoadFailed += OnInterstitialLoadFailed;
            }
            else Debug.LogWarning("LevelPlay interstitial ad unit ID is missing.");

            if (config.bannerEnabled && !string.IsNullOrWhiteSpace(config.bannerAdUnitId))
            {
                var size = config.bannerUseAdaptiveSize
                    ? LevelPlayAdSize.CreateAdaptiveAdSize() : LevelPlayAdSize.BANNER;
                if (size.Height > 0) bannerHeightDp = size.Height;
                var bannerConfig = new LevelPlayBannerAd.Config.Builder()
                    .SetSize(size)
                    .SetPosition(GetBannerPosition(config.bannerPosition))
                    .SetPlacementName(config.bannerPlacement)
                    .SetDisplayOnLoad(false)
                    .SetRespectSafeArea(config.bannerRespectSafeArea)
                    .Build();
                bannerAd = new LevelPlayBannerAd(config.bannerAdUnitId, bannerConfig);
                bannerAd.OnAdLoaded += OnBannerLoaded;
                bannerAd.OnAdClicked += OnBannerClicked;
                bannerAd.OnAdDisplayed += OnBannerDisplayed;
                bannerAd.OnAdLoadFailed += OnBannerLoadFailed;
                bannerAd.OnAdDisplayFailed += OnBannerDisplayFailed;
            }
            else if (config.bannerEnabled) Debug.LogWarning("LevelPlay banner ad unit ID is missing.");
        }

        private void ApplyPrivacySettings()
        {
            try
            {
                LevelPlayPrivacySettings.SetCOPPA(config.childDirected);
                LevelPlayPrivacySettings.SetCCPA(config.doNotSellPersonalInformation);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"LevelPlay privacy settings were not applied: {exception.GetBaseException().Message}");
            }
        }

        private static LevelPlayBannerPosition GetBannerPosition(string value)
        {
            return value?.Trim() switch
            {
                "TopLeft" => LevelPlayBannerPosition.TopLeft,
                "TopCenter" => LevelPlayBannerPosition.TopCenter,
                "TopRight" => LevelPlayBannerPosition.TopRight,
                "CenterLeft" => LevelPlayBannerPosition.CenterLeft,
                "Center" => LevelPlayBannerPosition.Center,
                "CenterRight" => LevelPlayBannerPosition.CenterRight,
                "BottomLeft" => LevelPlayBannerPosition.BottomLeft,
                "BottomRight" => LevelPlayBannerPosition.BottomRight,
                _ => LevelPlayBannerPosition.BottomCenter
            };
        }

        private void OnRewardedDisplayed(LevelPlayAdInfo info) =>
            AnalyticsService.Log("rewarded_shown", "placement", rewardPlacement);
        private void OnRewardedClicked(LevelPlayAdInfo info) =>
            AnalyticsService.Log("ad_clicked", ("ad_format", "rewarded"), ("placement", rewardPlacement));
        private void OnInterstitialClicked(LevelPlayAdInfo info) =>
            AnalyticsService.Log("ad_clicked", ("ad_format", "interstitial"), ("placement", config.interstitialPlacement));
        private void OnBannerDisplayed(LevelPlayAdInfo info) =>
            AnalyticsService.Log("banner_shown", "placement", config.bannerPlacement);
        private void OnBannerClicked(LevelPlayAdInfo info) =>
            AnalyticsService.Log("ad_clicked", ("ad_format", "banner"), ("placement", config.bannerPlacement));

        private void OnRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
        {
            rewardGranted = true;
            FinishReward(true);
        }

        private void OnRewardedClosed(LevelPlayAdInfo adInfo)
        {
            AnalyticsService.Log("rewarded_closed", ("placement", rewardPlacement), ("reward_granted", rewardGranted));
            rewardedAd?.LoadAd();
            if (!rewardGranted && rewardCallback != null) host.StartCoroutine(FinishUnrewardedAfterDelay());
        }

        private IEnumerator FinishUnrewardedAfterDelay()
        {
            yield return new WaitForSecondsRealtime(1f);
            if (!rewardGranted) FinishReward(false);
        }

        private IEnumerator CompleteSimulatedReward()
        {
            yield return new WaitForSecondsRealtime(0.35f);
            rewardGranted = true;
            Debug.Log("[Ad Simulation] Rewarded ad completed.");
            FinishReward(true);
        }

        private void OnRewardedDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
        {
            AnalyticsService.Log("rewarded_failed", "placement", rewardPlacement);
            rewardedAd?.LoadAd();
            FinishReward(false);
        }

        private void OnRewardedLoadFailed(LevelPlayAdError error)
        {
            AnalyticsService.Log("ad_load_failed", "ad_format", "rewarded");
            Debug.LogWarning($"LevelPlay rewarded ad failed to load: {error}");
            host.StartCoroutine(RetryLoadRewarded());
        }

        private void OnInterstitialDisplayed(LevelPlayAdInfo adInfo)
        {
            completionsSinceInterstitial = 0;
            lastInterstitialRealtime = Time.realtimeSinceStartup;
            AnalyticsService.Log("interstitial_shown", "placement", config.interstitialPlacement);
        }

        private void OnInterstitialClosed(LevelPlayAdInfo adInfo)
        {
            AnalyticsService.Log("interstitial_closed", "placement", config.interstitialPlacement);
            interstitialShowing = false;
            interstitialAd?.LoadAd();
        }

        private void OnInterstitialDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
        {
            interstitialShowing = false;
            AnalyticsService.Log("interstitial_failed");
            interstitialAd?.LoadAd();
        }

        private void OnInterstitialLoadFailed(LevelPlayAdError error)
        {
            AnalyticsService.Log("ad_load_failed", "ad_format", "interstitial");
            Debug.LogWarning($"LevelPlay interstitial failed to load: {error}");
            host.StartCoroutine(RetryLoadInterstitial());
        }

        private void OnBannerLoaded(LevelPlayAdInfo adInfo)
        {
            if (adInfo?.AdSize != null && adInfo.AdSize.Height > 0)
                bannerHeightDp = adInfo.AdSize.Height;
            ReadDisplayMetrics();
            bannerLoaded = true;
            AnalyticsService.Log("banner_loaded");
            SetBannerVisible(bannerWanted);
        }

        private void OnBannerLoadFailed(LevelPlayAdError error)
        {
            bannerLoaded = false;
            AnalyticsService.Log("ad_load_failed", "ad_format", "banner");
            Debug.LogWarning($"LevelPlay banner failed to load: {error}");
            host.StartCoroutine(RetryLoadBanner());
        }

        private void OnBannerDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
        {
            bannerLoaded = false;
            Debug.LogWarning($"LevelPlay banner failed to display: {error}");
            host.StartCoroutine(RetryLoadBanner());
        }

        private IEnumerator RetryLoadRewarded()
        {
            yield return new WaitForSecondsRealtime(config != null ? config.adLoadRetrySeconds : 8f);
            if (!shuttingDown && rewardedAd != null && !rewardedAd.IsAdReady()) rewardedAd.LoadAd();
        }

        private IEnumerator RetryLoadInterstitial()
        {
            yield return new WaitForSecondsRealtime(config != null ? config.adLoadRetrySeconds : 8f);
            if (!shuttingDown && interstitialAd != null && !interstitialAd.IsAdReady()) interstitialAd.LoadAd();
        }

        private IEnumerator RetryLoadBanner()
        {
            yield return new WaitForSecondsRealtime(config != null ? config.adLoadRetrySeconds : 8f);
            if (!shuttingDown && bannerAd != null && !bannerLoaded) bannerAd.LoadAd();
        }

        private void FinishReward(bool success)
        {
            var callback = rewardCallback;
            rewardCallback = null;
            if (callback != null)
                AnalyticsService.Log(success ? "rewarded_completed" : "rewarded_not_completed",
                    ("placement", rewardPlacement), ("simulated", Application.isEditor));
            callback?.Invoke(success);
        }

        private void UnsubscribeAdCallbacks()
        {
            if (rewardedAd != null)
            {
                rewardedAd.OnAdRewarded -= OnRewarded;
                rewardedAd.OnAdClicked -= OnRewardedClicked;
                rewardedAd.OnAdDisplayed -= OnRewardedDisplayed;
                rewardedAd.OnAdClosed -= OnRewardedClosed;
                rewardedAd.OnAdDisplayFailed -= OnRewardedDisplayFailed;
                rewardedAd.OnAdLoadFailed -= OnRewardedLoadFailed;
            }
            if (interstitialAd != null)
            {
                interstitialAd.OnAdDisplayed -= OnInterstitialDisplayed;
                interstitialAd.OnAdClicked -= OnInterstitialClicked;
                interstitialAd.OnAdClosed -= OnInterstitialClosed;
                interstitialAd.OnAdDisplayFailed -= OnInterstitialDisplayFailed;
                interstitialAd.OnAdLoadFailed -= OnInterstitialLoadFailed;
            }
            if (bannerAd != null)
            {
                bannerAd.OnAdLoaded -= OnBannerLoaded;
                bannerAd.OnAdClicked -= OnBannerClicked;
                bannerAd.OnAdDisplayed -= OnBannerDisplayed;
                bannerAd.OnAdLoadFailed -= OnBannerLoadFailed;
                bannerAd.OnAdDisplayFailed -= OnBannerDisplayFailed;
            }
        }
    }

    public static class ReflectionUtility
    {
        public static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, false);
                if (type != null) return type;
            }
            return null;
        }

        public static object InvokeBest(object source, string methodName, params object[] arguments)
        {
            if (source == null) return null;
            var type = source as Type ?? source.GetType();
            var target = source is Type ? null : source;
            var flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
            foreach (var method in type.GetMethods(flags))
            {
                if (method.Name != methodName) continue;
                var parameters = method.GetParameters();
                if (arguments.Length > parameters.Length) continue;
                if (parameters.Length > arguments.Length &&
                    parameters.Skip(arguments.Length).Any(parameter => !parameter.HasDefaultValue)) continue;
                var compatible = true;
                for (var i = 0; i < arguments.Length; i++)
                {
                    if (arguments[i] == null) continue;
                    if (!parameters[i].ParameterType.IsInstanceOfType(arguments[i]))
                    {
                        compatible = false;
                        break;
                    }
                }
                if (!compatible) continue;
                if (parameters.Length == arguments.Length) return method.Invoke(target, arguments);
                var invocationArguments = new object[parameters.Length];
                Array.Copy(arguments, invocationArguments, arguments.Length);
                for (var i = arguments.Length; i < parameters.Length; i++)
                    invocationArguments[i] = parameters[i].DefaultValue;
                return method.Invoke(target, invocationArguments);
            }
            return null;
        }
    }
}
