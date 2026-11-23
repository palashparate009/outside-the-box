using UnityEngine;

namespace OutsideTheBox
{
    /// <summary>
    /// Privacy-safe balancing counters stored only in the player's normal save.
    /// No identifiers, network calls, location, device ID, or personal data are collected.
    /// </summary>
    public static class LocalTelemetryService
    {
        public static void RecordAttempt(int levelIndex, bool endless)
        {
            var data = SaveService.Data?.telemetry;
            if (data == null) return;
            if (endless) data.endlessAttempts++;
            else Increment(data.attempts, levelIndex);
        }

        public static void RecordCompletion(int levelIndex, bool endless)
        {
            var data = SaveService.Data?.telemetry;
            if (data == null) return;
            if (endless) data.endlessCompletions++;
            else Increment(data.completions, levelIndex);
        }

        public static void RecordRestart(int levelIndex, bool endless)
        {
            var data = SaveService.Data?.telemetry;
            if (data == null || endless) return;
            Increment(data.restarts, levelIndex);
        }

        public static void RecordHint(int levelIndex, bool endless)
        {
            var data = SaveService.Data?.telemetry;
            if (data == null || endless) return;
            Increment(data.hints, levelIndex);
        }

        public static void RecordCollision(int levelIndex, bool endless)
        {
            var data = SaveService.Data?.telemetry;
            if (data == null || endless) return;
            Increment(data.collisions, levelIndex);
        }

        public static void RecordPlayTime(float seconds)
        {
            var data = SaveService.Data?.telemetry;
            if (data == null || seconds <= 0f) return;
            data.playSeconds += seconds;
        }

        private static void Increment(System.Collections.Generic.List<int> values, int index)
        {
            if (values == null || index < 0 || index >= values.Count) return;
            values[index]++;
        }
    }
}
