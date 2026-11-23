using System;
using System.Collections.Generic;
using UnityEngine;

namespace OutsideTheBox
{
    [Serializable]
    public sealed class SuspendedRunData
    {
        public bool valid;
        public bool endless;
        public bool daily;
        public string dailyKey;
        public int levelIndex;
        public int endlessIndex;
        public float ballX;
        public float ballY;
        public float velocityX;
        public float velocityY;
        public int magnetSide;
        public int magnetIndex;
        public int forceMode;
        public int movementBeats;
        public float energyUsed;
        public bool hintUsed;
        public float timeRemaining;
        public float elapsedSeconds;
        public int revivesUsed;
        public RailChallengeSnapshot railChallenge;
        public int designRevision;
    }

    [Serializable]
    public sealed class PlayerTelemetryData
    {
        public List<int> attempts = new();
        public List<int> completions = new();
        public List<int> restarts = new();
        public List<int> hints = new();
        public List<int> collisions = new();
        public float playSeconds;
        public int endlessAttempts;
        public int endlessCompletions;
    }

    [Serializable]
    public sealed class ChapterGiftData
    {
        public int chapter;
        public int category;
        public bool claimed;
    }

    [Serializable]
    public sealed class PlayerSaveData
    {
        public int highestUnlockedLevel = 1;
        public int lastPlayedLevel = 1;
        public List<int> bestStars = new();
        public List<float> bestEnergy = new();
        public List<int> bestPerformanceScores = new();
        public List<float> bestPerformanceRatings = new();
        public List<float> bestPerformanceTimes = new();
        public List<float> bestPerformanceEnergy = new();
        public bool endlessUnlocked;
        public int endlessLevel = 1;
        public int bestEndlessLevel;
        public float bestEndlessEnergy;
        public bool musicEnabled = true;
        public bool soundEnabled = true;
        public bool vibrationEnabled = true;
        public bool reduceMotion;
        public bool colorBlindMode;
        public bool largeText;
        public int performanceMode;
        public int selectedBallSkin;
        public int selectedMagnetSkin;
        public int selectedGoalSkin;
        public int selectedWallSkin;
        public int selectedGlassSkin;
        public int selectedBackgroundSkin;
        public List<string> unlockedCosmetics = new();
        public List<string> processedGemTransactions = new();
        public List<ChapterGiftData> chapterGifts = new();
        public bool notificationsEnabled = true;
        public string languageCode = "en";
        public string lastDailyCompleted;
        public string lastDailyCompletionDay;
        public float bestDailyEnergy;
        public int dailyStreak;
        public List<string> achievements = new();
        public int hintsRemaining = 5;
        public bool currencyInitialized;
        public int softCurrency;
        public int lifetimeCurrencyEarned;
        public int lifetimeCurrencySpent;
        public int rewardedRevives;
        public int timedOutRuns;
        public bool onboardingMovedMagnet;
        public bool ftueLevelOneInteractionShown;
        public bool ftueLevelTwoWallShown;
        public List<int> learnedTutorials = new();
        public SuspendedRunData suspendedRun = new();
        public PlayerTelemetryData telemetry = new();
    }

    public static class ChapterGiftService
    {
        public static int PendingCount
        {
            get
            {
                var count = 0;
                if (SaveService.Data?.chapterGifts == null) return count;
                foreach (var gift in SaveService.Data.chapterGifts) if (gift != null && !gift.claimed) count++;
                return count;
            }
        }

        public static int EnsureRewards(GameConfig config)
        {
            var data = SaveService.Data;
            if (data == null || config == null || !LiveOpsConfigStore.Current.chapterGiftsEnabled) return 0;
            data.chapterGifts ??= new List<ChapterGiftData>();
            var added = 0;
            var count = Mathf.Max(1, config.levelsPerChapter);
            for (var chapter = 0; chapter < config.ChapterCount; chapter++)
            {
                if (data.chapterGifts.Exists(g => g != null && g.chapter == chapter + 1)) continue;
                var complete = true;
                for (var i = chapter * count; i < (chapter + 1) * count; i++)
                    if (i >= data.bestStars.Count || data.bestStars[i] <= 0) { complete = false; break; }
                if (!complete) continue;
                var gift = new ChapterGiftData { chapter = chapter + 1, category = UnityEngine.Random.Range(0, 6) };
                data.chapterGifts.Add(gift);
                AnalyticsService.Log("chapter_gift_available", ("gift_chapter", gift.chapter), ("category", gift.category));
                added++;
            }
            if (added > 0) SaveService.SaveDeferred();
            return added;
        }

        public static bool Claim(ChapterGiftData gift)
        {
            var data = SaveService.Data;
            if (data == null || gift == null || gift.claimed || !(data.chapterGifts?.Contains(gift) ?? false)) return false;
            gift.claimed = true;
            AnalyticsService.Log("chapter_gift_claimed", ("gift_chapter", gift.chapter), ("category", gift.category));
            SaveService.SaveDeferred();
            LocalGameNotifications.Cancel();
            return true;
        }

        public static bool OwnsGift(int category, int index)
        {
            if (index < 10) return false;
            return SaveService.Data?.chapterGifts?.Exists(g => g != null && g.claimed && g.category == category && g.chapter == index - 9) ?? false;
        }

        public static Color GiftTint(int index)
        {
            var colors = LiveOpsConfigStore.Current.chapterGiftColors;
            return colors != null && colors.Length > 0 &&
                   ColorUtility.TryParseHtmlString(colors[Mathf.Abs(index - 10) % colors.Length], out var tint)
                ? tint : new Color(0.66f, 0.77f, 0.83f);
        }
    }

    public static class LocalGameNotifications
    {
        private const string Permission = "android.permission.POST_NOTIFICATIONS";

        [Serializable]
        private sealed class ReminderMessages
        {
            public string[] messages;
        }

        public static void Enable(bool enabled)
        {
            if (SaveService.Data == null) return;
            SaveService.Data.notificationsEnabled = enabled;
            AnalyticsService.Log("notifications_preference", "enabled", enabled ? 1 : 0);
            SaveService.SaveDeferred();
            if (!enabled) { Cancel(); RefreshPushRegistration(); return; }
            RequestAndroidPermission(false);
            RefreshPushRegistration();
        }

        public static void RequestPermissionIfEnabled()
        {
            if (SaveService.Data?.notificationsEnabled != true ||
                LiveOpsManager.Instance?.Players?.CloudProgressReady != true) return;
            var settings = LiveOpsConfigStore.Current;
            if (!settings.firebasePushEnabled && !settings.localNotificationsEnabled) return;
            RequestAndroidPermission(true);
            RefreshPushRegistration();
        }

        private static void RequestAndroidPermission(bool automatic)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            const string promptKey = "otb_notification_permission_prompted";
            if (automatic && PlayerPrefs.GetInt(promptKey, 0) != 0) return;
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                if (version.GetStatic<int>("SDK_INT") >= 33 && !UnityEngine.Android.Permission.HasUserAuthorizedPermission(Permission))
                {
                    var callbacks = new UnityEngine.Android.PermissionCallbacks();
                    callbacks.PermissionGranted += _ => RefreshPushRegistration();
                    callbacks.PermissionDenied += _ => RefreshPushRegistration();
                    PlayerPrefs.SetInt(promptKey, 1);
                    PlayerPrefs.Save();
                    UnityEngine.Android.Permission.RequestUserPermission(Permission, callbacks);
                }
#endif
        }

        public static void RefreshPushRegistration(bool forceDisable = false)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                var settings = LiveOpsConfigStore.Current;
                // Startup presents sign-in UI before cloud session restoration completes.
                // Preserve the existing device token until an explicit logout or restored profile.
                if (!forceDisable && LiveOpsManager.Instance?.Players?.IsSignedIn != true) return;
                var enabled = !forceDisable && settings.firebasePushEnabled &&
                              (SaveService.Data?.notificationsEnabled ?? false) &&
                              LiveOpsManager.Instance?.Players?.IsSignedIn == true;
                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                if (version.GetStatic<int>("SDK_INT") >= 33 && !UnityEngine.Android.Permission.HasUserAuthorizedPermission(Permission))
                    enabled = false;
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var plugin = new AndroidJavaClass("com.outsidethebox.notifications.GamePushMessagingService");
                plugin.CallStatic("configure", activity, enabled, settings.firebasePushTopic,
                    Debug.isDebugBuild && settings.firebasePushLogTokenInDevelopment);
            }
            catch (Exception exception) { Debug.LogWarning($"Firebase push registration failed: {exception.Message}"); }
#endif
        }

        public static void Cancel()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var plugin = new AndroidJavaClass("com.outsidethebox.notifications.GameReminderReceiver");
                plugin.CallStatic("cancel", activity);
            }
            catch (Exception exception) { Debug.LogWarning($"Notification cleanup failed: {exception.Message}"); }
#endif
        }

        public static void Schedule()
        {
            Cancel();
            var settings = LiveOpsConfigStore.Current;
            if (!(SaveService.Data?.notificationsEnabled ?? false) || !settings.localNotificationsEnabled ||
                LiveOpsManager.Instance?.Players?.IsSignedIn != true) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                var hasGift = ChapterGiftService.PendingCount > 0;
                var hours = hasGift ? settings.giftReminderHours : settings.gameReminderHours;
                var fire = DateTimeOffset.Now.AddHours(hours);
                if (fire.Hour < 9) fire = new DateTimeOffset(fire.Date.AddHours(10), fire.Offset);
                else if (fire.Hour >= 21) fire = new DateTimeOffset(fire.Date.AddDays(1).AddHours(10), fire.Offset);
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var plugin = new AndroidJavaClass("com.outsidethebox.notifications.GameReminderReceiver");
                var messages = JsonUtility.ToJson(new ReminderMessages { messages = settings.gameReminderMessages });
                var intervalMilliseconds = (long)(Math.Max(24d, settings.gameReminderHours) * 3600000d);
                plugin.CallStatic("scheduleDaily", activity, fire.ToUnixTimeMilliseconds(), "Outside the Box",
                    hasGift ? settings.giftReminderMessage : string.Empty, settings.gameReminderMessage,
                    messages, intervalMilliseconds);
            }
            catch (Exception exception) { Debug.LogWarning($"Notification scheduling failed: {exception.Message}"); }
#endif
        }
    }

    public sealed class PerformanceAggregate
    {
        public int score;
        public int completedLevels;
        public int expectedLevels;
        public float ratingTotal;
        public float elapsedSeconds;
        public float energyUsed;
        public bool IsComplete => expectedLevels > 0 && completedLevels >= expectedLevels;
        public float AverageRating => completedLevels > 0 ? ratingTotal / completedLevels : 0f;
    }

    public static class SaveService
    {
        private const string SaveKey = "otb_save_v1";
        private static bool loadedFromLegacyLocalSave;
        public static PlayerSaveData Data { get; private set; }
        public static bool HasLegacyLocalSave => loadedFromLegacyLocalSave;

        public static PlayerSaveData Load(GameConfig config, int levelCount)
        {
            if (Data == null)
            {
                // Existing testers are migrated once. New progress is never persisted to
                // PlayerPrefs; Cloud Save becomes authoritative after authentication.
                var json = PlayerPrefs.GetString(SaveKey, string.Empty);
                loadedFromLegacyLocalSave = !string.IsNullOrEmpty(json);
                Data = string.IsNullOrEmpty(json) ? new PlayerSaveData() : JsonUtility.FromJson<PlayerSaveData>(json);
                if (Data == null) Data = new PlayerSaveData();
            }
            Normalize(config, levelCount, !loadedFromLegacyLocalSave);
            return Data;
        }

        public static PlayerSaveData ApplyCloudJson(string json, GameConfig config, int levelCount)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                try { Data = JsonUtility.FromJson<PlayerSaveData>(json) ?? throw new FormatException("Empty cloud profile."); }
                catch (Exception exception)
                {
                    throw new FormatException("Cloud progress could not be read. The existing save was not overwritten.", exception);
                }
            }
            Data ??= new PlayerSaveData();
            Normalize(config, levelCount, string.IsNullOrWhiteSpace(json) && !loadedFromLegacyLocalSave);
            return Data;
        }

        public static PlayerSaveData ResetForCloudProfile(GameConfig config, int levelCount)
        {
            // Used only when the player explicitly signs into a different cloud account
            // and that account has no save yet. This prevents progress from a previous
            // device profile being uploaded to the newly selected player.
            Data = new PlayerSaveData();
            loadedFromLegacyLocalSave = false;
            Normalize(config, levelCount, true);
            return Data;
        }

        public static string ToJson() => Data == null ? string.Empty : JsonUtility.ToJson(Data);

        public static void MarkCloudSynchronized()
        {
            if (!loadedFromLegacyLocalSave) return;
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            loadedFromLegacyLocalSave = false;
        }

        private static void Normalize(GameConfig config, int levelCount, bool applyNewPlayerDefaults)
        {
            Data.highestUnlockedLevel = Mathf.Clamp(Data.highestUnlockedLevel, 1, Mathf.Max(1, levelCount));
            Data.lastPlayedLevel = Mathf.Clamp(Data.lastPlayedLevel, 1, Mathf.Max(1, levelCount));
            Data.hintsRemaining = Mathf.Max(Data.hintsRemaining, 0);
            Data.softCurrency = Mathf.Max(Data.softCurrency, 0);
            Data.endlessLevel = Mathf.Max(1, Data.endlessLevel);
            if (applyNewPlayerDefaults)
            {
                Data.vibrationEnabled = config.defaultVibration;
                Data.reduceMotion = config.defaultReduceMotion;
                Data.hintsRemaining = config.freeHintAllowance;
            }
            Data.bestStars ??= new List<int>();
            Data.bestEnergy ??= new List<float>();
            Data.bestPerformanceScores ??= new List<int>();
            Data.bestPerformanceRatings ??= new List<float>();
            Data.bestPerformanceTimes ??= new List<float>();
            Data.bestPerformanceEnergy ??= new List<float>();
            Data.learnedTutorials ??= new List<int>();
            Data.suspendedRun ??= new SuspendedRunData();
            Data.telemetry ??= new PlayerTelemetryData();
            Data.achievements ??= new List<string>();
            Data.unlockedCosmetics ??= new List<string>();
            Data.processedGemTransactions ??= new List<string>();
            Data.chapterGifts ??= new List<ChapterGiftData>();
            EnsureTelemetryCapacity(levelCount);
            while (Data.bestStars.Count < levelCount) Data.bestStars.Add(0);
            while (Data.bestEnergy.Count < levelCount) Data.bestEnergy.Add(0f);
            while (Data.bestPerformanceScores.Count < levelCount) Data.bestPerformanceScores.Add(0);
            while (Data.bestPerformanceRatings.Count < levelCount) Data.bestPerformanceRatings.Add(0f);
            while (Data.bestPerformanceTimes.Count < levelCount) Data.bestPerformanceTimes.Add(0f);
            while (Data.bestPerformanceEnergy.Count < levelCount) Data.bestPerformanceEnergy.Add(0f);
            if (levelCount > 0 && Data.bestStars[levelCount - 1] > 0) Data.endlessUnlocked = true;
        }

        private static void EnsureTelemetryCapacity(int levelCount)
        {
            if (Data?.telemetry == null) return;
            Data.telemetry.attempts ??= new List<int>();
            Data.telemetry.completions ??= new List<int>();
            Data.telemetry.restarts ??= new List<int>();
            Data.telemetry.hints ??= new List<int>();
            Data.telemetry.collisions ??= new List<int>();
            while (Data.telemetry.attempts.Count < levelCount) Data.telemetry.attempts.Add(0);
            while (Data.telemetry.completions.Count < levelCount) Data.telemetry.completions.Add(0);
            while (Data.telemetry.restarts.Count < levelCount) Data.telemetry.restarts.Add(0);
            while (Data.telemetry.hints.Count < levelCount) Data.telemetry.hints.Add(0);
            while (Data.telemetry.collisions.Count < levelCount) Data.telemetry.collisions.Add(0);
        }

        public static bool HasLearnedTutorial(int tutorialId)
        {
            return tutorialId > 0 && Data?.learnedTutorials != null && Data.learnedTutorials.Contains(tutorialId);
        }

        public static void MarkTutorialLearned(int tutorialId)
        {
            if (Data == null || tutorialId <= 0) return;
            Data.learnedTutorials ??= new List<int>();
            if (Data.learnedTutorials.Contains(tutorialId)) return;
            Data.learnedTutorials.Add(tutorialId);
            SaveDeferred();
        }

        public static void ResetTutorialProgress()
        {
            if (Data == null) return;
            Data.learnedTutorials ??= new List<int>();
            Data.learnedTutorials.Clear();
            Data.onboardingMovedMagnet = false;
            Data.ftueLevelOneInteractionShown = false;
            Data.ftueLevelTwoWallShown = false;
            Save();
        }

        public static void SaveSuspendedRun(bool endless, bool daily, string dailyKey, int levelIndex, int endlessIndex,
            Vector2 ballPosition, Vector2 ballVelocity, RailSlot magnet, ForceMode forceMode,
            int movementBeats, float energyUsed, bool hintUsed, float timeRemaining, float elapsedSeconds,
            int revivesUsed, RailChallengeSnapshot railChallenge = default, int designRevision = 0)
        {
            if (Data == null) return;
            Data.suspendedRun ??= new SuspendedRunData();
            var run = Data.suspendedRun;
            run.valid = true;
            run.endless = endless;
            run.daily = daily;
            run.dailyKey = dailyKey;
            run.levelIndex = levelIndex;
            run.endlessIndex = endlessIndex;
            run.ballX = ballPosition.x;
            run.ballY = ballPosition.y;
            run.velocityX = ballVelocity.x;
            run.velocityY = ballVelocity.y;
            run.magnetSide = (int)magnet.side;
            run.magnetIndex = magnet.index;
            run.forceMode = (int)forceMode;
            run.movementBeats = Mathf.Max(0, movementBeats);
            run.energyUsed = Mathf.Max(0f, energyUsed);
            run.hintUsed = hintUsed;
            run.timeRemaining = Mathf.Max(0f, timeRemaining);
            run.elapsedSeconds = Mathf.Max(0f, elapsedSeconds);
            run.revivesUsed = Mathf.Max(0, revivesUsed);
            run.railChallenge = railChallenge;
            run.designRevision = designRevision;
            SaveDeferred();
        }

        public static void ClearSuspendedRun(bool flush = false)
        {
            if (Data?.suspendedRun == null || !Data.suspendedRun.valid) return;
            Data.suspendedRun.valid = false;
            if (flush) Save();
            else SaveDeferred();
        }

        public static void CompleteLevel(int levelIndex, int stars, float energyUsed, float elapsedSeconds,
            int performanceScore, float performanceRating, int levelCount)
        {
            if (Data == null) return;
            Data.bestStars ??= new List<int>();
            Data.bestEnergy ??= new List<float>();
            while (Data.bestStars.Count < levelCount) Data.bestStars.Add(0);
            while (Data.bestEnergy.Count < levelCount) Data.bestEnergy.Add(0f);
            Data.bestPerformanceScores ??= new List<int>();
            Data.bestPerformanceRatings ??= new List<float>();
            Data.bestPerformanceTimes ??= new List<float>();
            Data.bestPerformanceEnergy ??= new List<float>();
            while (Data.bestPerformanceScores.Count < levelCount) Data.bestPerformanceScores.Add(0);
            while (Data.bestPerformanceRatings.Count < levelCount) Data.bestPerformanceRatings.Add(0f);
            while (Data.bestPerformanceTimes.Count < levelCount) Data.bestPerformanceTimes.Add(0f);
            while (Data.bestPerformanceEnergy.Count < levelCount) Data.bestPerformanceEnergy.Add(0f);
            Data.bestStars[levelIndex] = Mathf.Max(Data.bestStars[levelIndex], stars);
            if (Data.bestEnergy[levelIndex] <= 0f || energyUsed < Data.bestEnergy[levelIndex])
                Data.bestEnergy[levelIndex] = energyUsed;
            var previousScore = Data.bestPerformanceScores[levelIndex];
            var previousTime = Data.bestPerformanceTimes[levelIndex];
            if (performanceScore > previousScore ||
                (performanceScore == previousScore && (previousTime <= 0f || elapsedSeconds < previousTime)))
            {
                Data.bestPerformanceScores[levelIndex] = Mathf.Max(0, performanceScore);
                Data.bestPerformanceRatings[levelIndex] = Mathf.Clamp01(performanceRating);
                Data.bestPerformanceTimes[levelIndex] = Mathf.Max(0f, elapsedSeconds);
                Data.bestPerformanceEnergy[levelIndex] = Mathf.Max(0f, energyUsed);
            }
            Data.highestUnlockedLevel = Mathf.Max(Data.highestUnlockedLevel, Mathf.Min(levelCount, levelIndex + 2));
            Data.lastPlayedLevel = Mathf.Min(levelCount, levelIndex + 2);
            if (levelIndex >= levelCount - 1) Data.endlessUnlocked = true;
            SaveDeferred();
        }

        public static PerformanceAggregate GetPerformanceAggregate(int firstLevelIndex, int levelCount)
        {
            var result = new PerformanceAggregate { expectedLevels = Mathf.Max(0, levelCount) };
            if (Data == null || levelCount <= 0) return result;
            for (var index = Mathf.Max(0, firstLevelIndex);
                 index < firstLevelIndex + levelCount && index < Data.bestPerformanceScores.Count;
                 index++)
            {
                if (Data.bestPerformanceScores[index] <= 0) continue;
                result.completedLevels++;
                result.score += Data.bestPerformanceScores[index];
                result.ratingTotal += index < Data.bestPerformanceRatings.Count
                    ? Data.bestPerformanceRatings[index]
                    : 0f;
                result.elapsedSeconds += index < Data.bestPerformanceTimes.Count
                    ? Data.bestPerformanceTimes[index]
                    : 0f;
                result.energyUsed += index < Data.bestPerformanceEnergy.Count
                    ? Data.bestPerformanceEnergy[index]
                    : 0f;
            }
            return result;
        }

        public static void CompleteEndless(int endlessLevel, float energyUsed)
        {
            if (Data == null) return;
            Data.endlessUnlocked = true;
            Data.bestEndlessLevel = Mathf.Max(Data.bestEndlessLevel, endlessLevel);
            if (Data.bestEndlessEnergy <= 0f || energyUsed < Data.bestEndlessEnergy)
                Data.bestEndlessEnergy = energyUsed;
            Data.endlessLevel = Mathf.Max(Data.endlessLevel, endlessLevel + 1);
            SaveDeferred();
        }

        public static void CompleteDaily(string dailyKey, string previousDayKey, float energyUsed)
        {
            if (Data == null || string.IsNullOrWhiteSpace(dailyKey)) return;
            if (Data.lastDailyCompleted != dailyKey)
            {
                Data.dailyStreak = Data.lastDailyCompletionDay == previousDayKey
                    ? Mathf.Max(1, Data.dailyStreak + 1)
                    : 1;
                Data.lastDailyCompleted = dailyKey;
                Data.lastDailyCompletionDay = dailyKey;
                Data.bestDailyEnergy = energyUsed;
            }
            else if (Data.bestDailyEnergy <= 0f || energyUsed < Data.bestDailyEnergy)
            {
                Data.bestDailyEnergy = energyUsed;
            }
            SaveDeferred();
        }

        public static bool UnlockAchievement(string achievementId)
        {
            if (Data == null || string.IsNullOrWhiteSpace(achievementId)) return false;
            Data.achievements ??= new List<string>();
            if (Data.achievements.Contains(achievementId)) return false;
            Data.achievements.Add(achievementId);
            SaveDeferred();
            return true;
        }

        public static int TotalStars
        {
            get
            {
                if (Data?.bestStars == null) return 0;
                var total = 0;
                foreach (var stars in Data.bestStars) total += Mathf.Max(0, stars);
                return total;
            }
        }

        public static int GetHighestUnlockedLevel(GameConfig config, int levelCount)
        {
            if (config != null && config.unlockAllChaptersForTesting)
                return Mathf.Max(1, levelCount);
            return Mathf.Clamp(Data?.highestUnlockedLevel ?? 1, 1, Mathf.Max(1, levelCount));
        }

        public static bool IsEndlessUnlocked(GameConfig config)
        {
            return config != null && config.endlessEnabled &&
                   (config.unlockAllChaptersForTesting || (Data?.endlessUnlocked ?? false));
        }

        public static void Save()
        {
            if (Data == null) return;
            LiveOpsManager.Instance?.Players?.QueueProgressSave(ToJson());
        }

        public static void SaveDeferred()
        {
            if (Data == null) return;
            LiveOpsManager.Instance?.Players?.QueueProgressSave(ToJson());
        }

        public static void Flush()
        {
            Save();
        }
    }

    public static class LocalizationService
    {
        public static GameStrings LoadSelected(GameConfig config)
        {
            var selected = SaveService.Data?.languageCode;
            if (string.IsNullOrWhiteSpace(selected)) selected = "en";
            if (!IsSupported(config, selected)) selected = "en";
            var table = Resources.Load<GameStrings>($"OutsideTheBox/Configs/Localization/Strings_{selected}");
            if (table == null) table = Resources.Load<GameStrings>("OutsideTheBox/Configs/Localization/Strings_en");
            return table;
        }

        public static bool IsSupported(GameConfig config, string languageCode)
        {
            if (config?.supportedLanguageCodes == null) return languageCode == "en";
            foreach (var supported in config.supportedLanguageCodes)
                if (string.Equals(supported, languageCode, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
