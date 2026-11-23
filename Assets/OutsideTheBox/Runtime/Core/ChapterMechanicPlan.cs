using UnityEngine;

namespace OutsideTheBox
{
    public enum LevelMechanicKind { None, Gates, MovingWalls, FieldZones, Echo, Memory, Imprints, Lenses, PolarityLocks, Orbit }

    public readonly struct ChapterMechanicPlan
    {
        public readonly LevelMechanicKind primary, supporting;
        public ChapterMechanicPlan(LevelMechanicKind main, LevelMechanicKind extra = LevelMechanicKind.None)
        { primary = main; supporting = extra; }
        public bool Has(LevelMechanicKind kind) => primary == kind || supporting == kind;
        public int Count => (primary == LevelMechanicKind.None ? 0 : 1) + (supporting == LevelMechanicKind.None ? 0 : 1);

        public static ChapterMechanicPlan For(LevelDefinition level, GameConfig config)
        {
            if (level.useNormalPhysics) return default;
            var local = (Mathf.Max(1, level.levelNumber) - 1) % Mathf.Max(1, config.levelsPerChapter) + 1;
            var chapter = (Mathf.Max(1, level.levelNumber) - 1) / Mathf.Max(1, config.levelsPerChapter);
            var rail = RailChallengeGenerator.ScheduledKind(level, config) != RailChallengeKind.None;
            if (level.isEndless)
            {
                // The vault/daily challenge rotates a small selection; difficulty
                // never enables all previously unlocked mechanics at once.
                var seed = (level.generatorSeed & int.MaxValue);
                var main = (LevelMechanicKind)(1 + seed % (int)LevelMechanicKind.Orbit);
                var extra = !rail && level.endlessIndex % 5 == 0
                    ? (main == LevelMechanicKind.Gates ? LevelMechanicKind.MovingWalls : LevelMechanicKind.Gates)
                    : LevelMechanicKind.None;
                return new ChapterMechanicPlan(main, extra);
            }
            switch (chapter)
            {
                // Basics, frozen floor, and polarity have their identity in the
                // core controls/physics, so they need no extra board objects.
                case 0: case 1: case 2: return default;
                case 3:
                    return new ChapterMechanicPlan(LevelMechanicKind.Gates, rail ? LevelMechanicKind.None : local switch
                    { 2 => LevelMechanicKind.Echo, 5 or 8 => LevelMechanicKind.Memory,
                      3 or 9 or 10 => LevelMechanicKind.MovingWalls, _ => LevelMechanicKind.None });
                case 4:
                    return new ChapterMechanicPlan(LevelMechanicKind.FieldZones, rail ? LevelMechanicKind.None : local switch
                    { 2 or 3 => LevelMechanicKind.Imprints, 5 or 7 or 9 or 10 => LevelMechanicKind.Lenses,
                      _ => LevelMechanicKind.None });
                default:
                    return new ChapterMechanicPlan(LevelMechanicKind.PolarityLocks, rail ? LevelMechanicKind.None : local switch
                    { 3 or 10 => LevelMechanicKind.Echo, 8 => LevelMechanicKind.Memory,
                      7 or 9 => LevelMechanicKind.Orbit, _ => LevelMechanicKind.None });
            }
        }
    }
}
