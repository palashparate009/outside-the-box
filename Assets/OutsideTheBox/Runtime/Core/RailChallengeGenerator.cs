using System;
using System.Collections.Generic;
using UnityEngine;

namespace OutsideTheBox
{
    public static class RailChallengeGenerator
    {
        public static RailChallengeKind ScheduledKind(LevelDefinition level, GameConfig config)
        {
            if (config == null || !config.railChallengesEnabled || level.useNormalPhysics) return RailChallengeKind.None;
            var seed = Seed(level);
            if (level.isEndless)
                return level.endlessIndex >= 3 && (!config.focusedChapterDesignEnabled || level.endlessIndex % 10 != 6) && (seed & int.MaxValue) % 3 == 0
                    ? (RailChallengeKind)(1 + (seed & int.MaxValue) % 6) : RailChallengeKind.None;
            var count = Mathf.Max(1, config.levelsPerChapter);
            var chapter = (Mathf.Max(1, level.levelNumber) - 1) / count;
            var local = (Mathf.Max(1, level.levelNumber) - 1) % count + 1;
            if (chapter == 0) return local == Mathf.Min(8, count) && local > 2
                ? RailChallengeKind.LockedSide : RailChallengeKind.None;
            var introduction = Mathf.Min(4, count);
            var newest = Mathf.Clamp(chapter + 1, 1, 6);
            if (local == introduction) return (RailChallengeKind)newest;
            if (count <= introduction + 1 || local <= introduction || local == count) return RailChallengeKind.None;
            var random = new System.Random(63127 + chapter * 997);
            if (config.focusedChapterDesignEnabled && count >= 10)
            {
                var positions = new[] { 5, 7, 8, 9 };
                var firstIndex = random.Next(positions.Length);
                var secondIndex = (firstIndex + 1 + random.Next(positions.Length - 1)) % positions.Length;
                return local == positions[firstIndex] || local == positions[secondIndex]
                    ? (RailChallengeKind)(1 + new System.Random(seed).Next(newest)) : RailChallengeKind.None;
            }
            var first = random.Next(introduction + 1, count);
            var second = random.Next(introduction + 1, count);
            if (first == second) second = first + 1 < count ? first + 1 : introduction + 1;
            return local == first || local == second ? (RailChallengeKind)(1 + new System.Random(seed).Next(newest))
                : RailChallengeKind.None;
        }

        public static void Apply(LevelDefinition level, GameConfig config)
        {
            var kind = ScheduledKind(level, config);
            level.railChallenge = new RailChallengeDefinition();
            if (kind == RailChallengeKind.None) return;
            var baseline = LevelSolver.Solve(level);
            if (!baseline.solved || baseline.steps.Count == 0) return;
            var startSlot = RailChallengeRules.StartingSlot(level);
            var random = new System.Random(Seed(level));
            var route = new List<GridCell>();
            var current = new GameState(level.start, startSlot, ForceMode.Pull);
            var changes = 0;
            foreach (var step in baseline.steps)
            {
                if (step.magnet.side != current.magnet.side) changes++;
                current.magnet = step.magnet; current.mode = step.mode;
                current = GameRules.Advance(level, current).state;
                if (current.ball != level.start && current.ball != level.exit && !route.Contains(current.ball))
                    route.Add(current.ball);
            }
            for (var attempt = 0; attempt < 24; attempt++)
            {
                var side = (MagnetSide)((random.Next(4) + attempt) % 4);
                var slots = side is MagnetSide.Left or MagnetSide.Right ? level.height : level.width;
                // If a narrow segment cannot preserve a route, try a gentler
                // restriction before dropping this level's challenge entirely.
                var allowed = Mathf.Max(2, attempt < 12 ? slots / 2 : slots - 1);
                var minimum = random.Next(slots - allowed + 1);
                var rule = new RailChallengeDefinition { kind = kind, side = side, minimumIndex = minimum,
                    maximumIndex = minimum + allowed - 1, switchBudget = Mathf.Max(4, changes + 2) };
                if (kind == RailChallengeKind.UnlockSwitch)
                {
                    if (route.Count == 0 || side == startSlot.side) continue;
                    rule.switchCell = route[(attempt + random.Next(route.Count)) % route.Count];
                    if (!ClearSwitch(level, rule.switchCell, config)) continue;
                }
                level.railChallenge = rule;
                var result = kind is RailChallengeKind.SwitchLimit or RailChallengeKind.Cooldown or RailChallengeKind.TimedLock
                    ? baseline : LevelSolver.Solve(level);
                if (!result.solved) continue;
                level.useCustomStartingMagnet = true;
                level.startingMagnet = RailChallengeRules.StartingSlot(level);
                level.verifiedSolution = result.steps;
                level.targetMoves = Mathf.Max(level.targetMoves, result.movementBeats);
                // A restricted rail can change the verified path. Keep the other
                // mechanics on that final journey and reserve the switch footprint.
                if (config.focusedChapterDesignEnabled) LevelMechanicLayout.Arrange(level, config);
                return;
            }
            // Do not attach an unverified restriction to a playable board.
            level.railChallenge = new RailChallengeDefinition();
        }

        private static int Seed(LevelDefinition level) => unchecked((level.generatorSeed != 0
            ? level.generatorSeed : 74000 + level.levelNumber * 977) ^ 0x4a7139);

        private static bool ClearSwitch(LevelDefinition level, GridCell cell, GameConfig config)
        {
            if (level.IsWall(cell) || level.TryGetGate(cell, out _)) return false;
            var p = new Vector2((cell.x - (level.width - 1) * .5f) * config.cellSize,
                (cell.y - (level.height - 1) * .5f) * config.cellSize);
            var radius = config.ballRadius + .22f * config.cellSize;
            if (level.wallSegments != null)
                foreach (var wall in level.wallSegments)
                {
                    var half = new Vector2(wall.horizontal ? wall.length : config.wallThickness,
                        wall.horizontal ? config.wallThickness : wall.length) * .5f;
                    var nearest = new Vector2(Mathf.Clamp(p.x, wall.center.x - half.x, wall.center.x + half.x),
                        Mathf.Clamp(p.y, wall.center.y - half.y, wall.center.y + half.y));
                    if ((nearest - p).sqrMagnitude < radius * radius) return false;
                }
            if (level.movingWalls != null)
                foreach (var wall in level.movingWalls)
                {
                    var half = new Vector2(wall.horizontal ? wall.length * .5f : config.wallThickness * .5f,
                        wall.horizontal ? config.wallThickness * .5f : wall.length * .5f);
                    half += new Vector2(Mathf.Abs(wall.travel.x), Mathf.Abs(wall.travel.y)) + Vector2.one * radius;
                    var delta = p - wall.center;
                    if (Mathf.Abs(delta.x) < half.x && Mathf.Abs(delta.y) < half.y) return false;
                }
            if (level.fieldZones != null)
                foreach (var zone in level.fieldZones)
                    if ((zone.center - p).magnitude < zone.radius + radius) return false;
            if (level.fieldLenses != null)
                foreach (var lens in level.fieldLenses)
                    if ((lens.center - p).magnitude < lens.radius + radius) return false;
            if (level.polarityLocks != null)
                foreach (var item in level.polarityLocks)
                {
                    var half = new Vector2(item.horizontal ? item.length : config.wallThickness,
                        item.horizontal ? config.wallThickness : item.length) * .5f + Vector2.one * radius;
                    if (Mathf.Abs(p.x - item.center.x) < half.x && Mathf.Abs(p.y - item.center.y) < half.y) return false;
                }
            return true;
        }
    }
}
