using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Leaderboards;
using UnityEngine;

namespace OutsideTheBox
{
    [Serializable]
    public sealed class PerformanceLeaderboardMetadata
    {
        public int schemaVersion = 1;
        public int chapter;
        public int completedLevels;
        public float performancePercent;
        public float timeSeconds;
        public float energy;
    }

    /// <summary>
    /// Owns the Unity Gaming Services identity used by Cloud Save and Leaderboards.
    /// Google Play Games supplies an authorization code; Unity Authentication remains
    /// the single player identity for both Google and guest players.
    /// </summary>
    public sealed class UnityPlayerService
    {
        private LiveOpsConfigData config;
        private bool initialized;
        private CloudProgressWriteQueue progressWrites;
        public bool CloudProgressReady => IsSignedIn && progressWrites?.IsReady == true && progressWrites.Owner == PlayerId;
        private LiveOpsConfigData Settings => LiveOpsConfigStore.Current ?? config;

        public bool IsInitialized => initialized;
        public bool IsSignedIn => initialized && AuthenticationService.Instance.IsSignedIn;
        public string PlayerId => IsSignedIn ? AuthenticationService.Instance.PlayerId : string.Empty;
        public string Username => IsSignedIn ? AuthenticationService.Instance.PlayerInfo?.Username : null;
        public bool HasUsernamePassword => !string.IsNullOrWhiteSpace(Username);

        public async Task InitializeAsync(LiveOpsConfigData settings)
        {
            config = settings ?? new LiveOpsConfigData();
            if (initialized || !config.unityServicesEnabled) return;
            // Pin the released game's identity, progress and scores to the live environment,
            // regardless of the environment currently selected in the Editor dashboard.
            var options = new InitializationOptions().SetEnvironmentName("production");
            await UnityServices.InitializeAsync(options);
            initialized = true;
            progressWrites = new CloudProgressWriteQueue(WriteProgressAsync);
        }

        public async Task<bool> TryRestoreSessionAsync()
        {
            await InitializeAsync(config);
            if (!initialized || IsSignedIn || !AuthenticationService.Instance.SessionTokenExists)
                return IsSignedIn;
            try
            {
                // Anonymous sign-in reuses any cached Unity Authentication session token,
                // including a session originally created with a platform provider.
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                AnalyticsService.SetPlayer(PlayerId);
                AnalyticsService.Log("session_restored");
                return true;
            }
            catch (Exception exception)
            {
                AnalyticsService.Log("session_restore_failed");
                Debug.LogWarning($"Unity player session could not be restored: {exception.Message}");
                return false;
            }
        }

        public async Task SignInUsernamePasswordAsync(string username, string password)
        {
            await InitializeAsync(config);
            EnsureUsernamePasswordEnabled();
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
                throw new ArgumentException("Enter your username and password.");
            if (IsSignedIn)
                throw new InvalidOperationException("Sign out of the current player before signing into another account.");
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username.Trim(), password);
            AnalyticsService.SetPlayer(PlayerId);
            AnalyticsService.Log("login", "method", "username_password");
        }

        public async Task CreateOrLinkUsernamePasswordAsync(string username, string password)
        {
            await InitializeAsync(config);
            EnsureUsernamePasswordEnabled();
            ValidateUsernamePassword(username, password);
            username = username.Trim();
            if (IsSignedIn)
            {
                if (!Settings.guestAccountUpgradeEnabled)
                    throw new InvalidOperationException("Guest account upgrades are disabled by live config.");
                if (HasUsernamePassword)
                    throw new InvalidOperationException("This Unity player already has a username and password.");
                // Linking preserves the anonymous guest's Player ID, Cloud Save and scores.
                await AuthenticationService.Instance.AddUsernamePasswordAsync(username, password);
                AnalyticsService.Log("account_linked", "method", "username_password");
            }
            else
            {
                await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
                AnalyticsService.SetPlayer(PlayerId);
                AnalyticsService.Log("sign_up", "method", "username_password");
            }
        }

        private void EnsureUsernamePasswordEnabled()
        {
            if (!Settings.usernamePasswordEnabled)
                throw new InvalidOperationException("Username and password sign-in is disabled by live config.");
        }

        private void ValidateUsernamePassword(string username, string password)
        {
            var settings = Settings;
            username = username?.Trim() ?? string.Empty;
            if (username.Length < settings.usernameMinLength || username.Length > settings.usernameMaxLength ||
                !Regex.IsMatch(username, @"^[A-Za-z0-9.\-@_]+$"))
                throw new ArgumentException($"Username must be {settings.usernameMinLength}-{settings.usernameMaxLength} characters and use only letters, numbers, . - @ _");
            if (string.IsNullOrEmpty(password) || password.Length < settings.passwordMinLength ||
                password.Length > settings.passwordMaxLength ||
                !Regex.IsMatch(password, "[a-z]") || !Regex.IsMatch(password, "[A-Z]") ||
                !Regex.IsMatch(password, "[0-9]") || !Regex.IsMatch(password, "[^A-Za-z0-9]"))
                throw new ArgumentException($"Password must be {settings.passwordMinLength}-{settings.passwordMaxLength} characters with upper, lower, number and symbol.");
        }

        public async Task SignInGooglePlayAsync()
        {
            await InitializeAsync(config);
            if (!Settings.googlePlayGamesEnabled)
                throw new InvalidOperationException("Google Play Games sign-in is disabled by live config.");
#if UNITY_EDITOR
            if (!Settings.editorGooglePlayTestLoginEnabled)
                throw new PlatformNotSupportedException(
                    "Google Play Games is available on Android. Enable the Editor test login in live config for PC testing.");

            // The Play Games native authentication UI is unavailable on PC. Use one
            // cached Unity Authentication identity so Editor playtests exercise the same
            // Cloud Save/Leaderboard startup path without storing test credentials.
            if (!IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            AnalyticsService.SetPlayer(PlayerId);
            AnalyticsService.Log("login", "method", "editor_google_play_test");
            return;
#elif UNITY_ANDROID
            PlayGamesPlatform.Activate();
            var platform = PlayGamesPlatform.Instance;
            if (!platform.IsAuthenticated())
            {
                var signInResult = new TaskCompletionSource<SignInStatus>();
                platform.ManuallyAuthenticate(status => signInResult.TrySetResult(status));
                var status = await signInResult.Task;
                if (status != SignInStatus.Success)
                    throw new InvalidOperationException($"Google Play Games sign-in failed ({status}).");
            }

            var codeResult = new TaskCompletionSource<string>();
            platform.RequestServerSideAccess(false, code => codeResult.TrySetResult(code));
            var authCode = await codeResult.Task;
            if (string.IsNullOrWhiteSpace(authCode))
                throw new InvalidOperationException("Google Play Games did not return a server authorization code.");

            if (IsSignedIn && !string.IsNullOrWhiteSpace(AuthenticationService.Instance.PlayerInfo?.GetGooglePlayGamesId()))
            {
                AnalyticsService.SetPlayer(PlayerId);
                AnalyticsService.Log("login", "method", "google_play_games");
                return;
            }
            if (IsSignedIn)
                await AuthenticationService.Instance.LinkWithGooglePlayGamesAsync(authCode);
            else
                await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authCode);
            AnalyticsService.SetPlayer(PlayerId);
            AnalyticsService.Log("login", "method", "google_play_games");
#else
            throw new PlatformNotSupportedException("Google Play Games sign-in is available on Android builds.");
#endif
        }

        public async Task SignOutAsync()
        {
            if (CloudProgressReady) await progressWrites.Flush(PlayerId);
            progressWrites?.Begin(null);
            if (initialized && AuthenticationService.Instance.IsSignedIn)
                AuthenticationService.Instance.SignOut(true);
            AnalyticsService.Log("logout");
            AnalyticsService.SetPlayer(null);
            AnalyticsService.ClearLevel();
        }

        public async Task DeleteAccountAsync()
        {
            if (!IsSignedIn)
                throw new InvalidOperationException("No player account is currently signed in.");
            if (CloudProgressReady) await progressWrites.Flush(PlayerId);
            await AuthenticationService.Instance.DeleteAccountAsync();
            progressWrites?.Begin(null);
            AnalyticsService.Log("account_deleted");
            AnalyticsService.SetPlayer(null);
            AnalyticsService.ClearLevel();
        }

        public async Task<string> LoadProgressAsync()
        {
            var settings = Settings;
            if (!IsSignedIn || !settings.cloudProgressEnabled) return null;
            progressWrites.Begin(PlayerId);
            var keys = new HashSet<string> { settings.cloudProgressKey };
#pragma warning disable CS0618
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
#pragma warning restore CS0618
            return data.TryGetValue(settings.cloudProgressKey, out var item)
                ? item.Value.GetAsString()
                : null;
        }

        public void MarkProgressLoaded(string playerId) => progressWrites.MarkReady(playerId);

        public void QueueProgressSave(string json)
        {
            if (!CloudProgressReady || !Settings.cloudProgressEnabled) return;
            progressWrites.Enqueue(PlayerId, json);
        }

        public async Task SaveProgressNowAsync(string json)
        {
            if (!Settings.cloudProgressEnabled) return;
            if (!CloudProgressReady)
                throw new InvalidOperationException("Load cloud progress before uploading progress.");
            progressWrites.Enqueue(PlayerId, json);
            await progressWrites.Flush(PlayerId);
        }

        private async Task WriteProgressAsync(string playerId, string json)
        {
            if (!CloudProgressReady || PlayerId != playerId)
                throw new InvalidOperationException("The cloud save belongs to another player.");
            var data = new Dictionary<string, object> { [Settings.cloudProgressKey] = json };
            try
            {
#pragma warning disable CS0618
                await CloudSaveService.Instance.Data.Player.SaveAsync(data);
#pragma warning restore CS0618
            }
            catch
            {
                AnalyticsService.Log("cloud_progress_save_failed");
                throw;
            }
        }

        public async Task ReportCampaignScoreAsync(int totalStars)
        {
            var settings = Settings;
            if (!CanUseLeaderboard(settings.campaignLeaderboardId)) return;
            try
            {
                await LeaderboardsService.Instance.AddPlayerScoreAsync(
                    settings.campaignLeaderboardId, Math.Max(0, totalStars));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Campaign leaderboard score was not submitted: {exception.Message}");
            }
        }

        public async Task ReportEndlessScoreAsync(int level)
        {
            var settings = Settings;
            if (!CanUseLeaderboard(settings.endlessLeaderboardId)) return;
            try
            {
                await LeaderboardsService.Instance.AddPlayerScoreAsync(
                    settings.endlessLeaderboardId, Math.Max(0, level));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Endless leaderboard score was not submitted: {exception.Message}");
            }
        }

        public async Task ReportPerformanceScoresAsync(int chapterIndex, PerformanceAggregate chapter,
            PerformanceAggregate allTime)
        {
            if (!IsSignedIn || !Settings.unityLeaderboardsEnabled) return;
            if (allTime != null && allTime.score > 0)
                await SubmitPerformanceScoreAsync(Settings.allTimeLeaderboardId, -1, allTime);
            if (chapter == null || !chapter.IsComplete || chapterIndex < 0 ||
                chapterIndex >= Settings.chapterLeaderboardIds.Length) return;
            await SubmitPerformanceScoreAsync(Settings.chapterLeaderboardIds[chapterIndex], chapterIndex, chapter);
        }

        private async Task SubmitPerformanceScoreAsync(string leaderboardId, int chapterIndex,
            PerformanceAggregate performance)
        {
            if (!CanUseLeaderboard(leaderboardId)) return;
            try
            {
                var metadata = new PerformanceLeaderboardMetadata
                {
                    chapter = chapterIndex + 1,
                    completedLevels = performance.completedLevels,
                    performancePercent = performance.AverageRating * 100f,
                    timeSeconds = performance.elapsedSeconds,
                    energy = performance.energyUsed
                };
                await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, performance.score,
                    new AddPlayerScoreOptions { Metadata = metadata });
                AnalyticsService.Log("leaderboard_submitted", ("scope_chapter", chapterIndex + 1), ("score", performance.score));
            }
            catch (Exception exception)
            {
                AnalyticsService.Log("leaderboard_submit_failed", "scope_chapter", chapterIndex + 1);
                Debug.LogWarning($"Performance score was not submitted to {leaderboardId}: {exception.Message}");
            }
        }

        public async Task<string> GetPerformanceLeaderboardSummaryAsync(int chapterIndex, int limit = 10)
        {
            var leaderboardId = chapterIndex < 0
                ? Settings.allTimeLeaderboardId
                : chapterIndex < Settings.chapterLeaderboardIds.Length
                    ? Settings.chapterLeaderboardIds[chapterIndex]
                    : string.Empty;
            if (!CanUseLeaderboard(leaderboardId)) return null;
            var page = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId,
                new GetScoresOptions
                {
                    Limit = Mathf.Clamp(limit, 1, 20),
                    IncludeMetadata = true
                });
            var builder = new StringBuilder();
            foreach (var entry in page.Results)
            {
                var name = string.IsNullOrWhiteSpace(entry.PlayerName)
                    ? entry.PlayerId.Substring(0, Math.Min(8, entry.PlayerId.Length))
                    : entry.PlayerName;
                PerformanceLeaderboardMetadata metadata = null;
                if (!string.IsNullOrWhiteSpace(entry.Metadata))
                {
                    try { metadata = JsonUtility.FromJson<PerformanceLeaderboardMetadata>(entry.Metadata); }
                    catch { /* A legacy entry can still be displayed without metadata. */ }
                }
                if (builder.Length > 0) builder.Append('\n');
                builder.Append($"{entry.Rank + 1}. {name}");
                if (metadata != null)
                {
                    var seconds = Mathf.Max(0, Mathf.RoundToInt(metadata.timeSeconds));
                    builder.Append($"\n   {metadata.performancePercent:0.0}%  |  " +
                                   $"TIME {seconds / 60:00}:{seconds % 60:00}  |  " +
                                   $"ENERGY {metadata.energy:0.0}");
                }
                else builder.Append($"  |  SCORE {Math.Round(entry.Score)}");
            }
            return builder.ToString();
        }

        public async Task<string> GetCampaignLeaderboardSummaryAsync(int limit = 5)
        {
            var settings = Settings;
            if (!CanUseLeaderboard(settings.campaignLeaderboardId)) return null;
            var page = await LeaderboardsService.Instance.GetScoresAsync(settings.campaignLeaderboardId,
                new Unity.Services.Leaderboards.GetScoresOptions { Limit = Mathf.Clamp(limit, 1, 20) });
            var builder = new StringBuilder("TOP PLAYERS");
            foreach (var entry in page.Results)
            {
                var name = string.IsNullOrWhiteSpace(entry.PlayerName)
                    ? entry.PlayerId.Substring(0, Math.Min(8, entry.PlayerId.Length))
                    : entry.PlayerName;
                builder.Append($"\n{entry.Rank + 1}. {name}  {Math.Round(entry.Score)}");
            }
            return builder.ToString();
        }

        private bool CanUseLeaderboard(string id) => IsSignedIn && Settings.unityLeaderboardsEnabled &&
                                                      !string.IsNullOrWhiteSpace(id);
    }
}
