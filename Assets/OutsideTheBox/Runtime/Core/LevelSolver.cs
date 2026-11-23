using System.Collections.Generic;
using UnityEngine;

namespace OutsideTheBox
{
    public sealed class SolverResult
    {
        public bool solved;
        public int movementBeats;
        public int visitedStates;
        public List<SolutionStep> steps = new();
    }

    public static class LevelSolver
    {
        private readonly struct SearchNode
        {
            public readonly GameState state;
            public readonly int parent;
            public readonly SolutionStep action;
            public SearchNode(GameState state, int parent, SolutionStep action)
            { this.state = state; this.parent = parent; this.action = action; }
        }

        public static SolverResult Solve(LevelDefinition level, int maxVisited = 25000)
        {
            var initialMagnet = RailChallengeRules.StartingSlot(level);
            var initial = new GameState(level.start, initialMagnet, ForceMode.Pull);
            var nodes = new List<SearchNode> { new(initial, -1, default) };
            var queue = new Queue<int>();
            var visited = new HashSet<GameState> { initial };
            queue.Enqueue(0);

            while (queue.Count > 0 && visited.Count <= maxVisited)
            {
                var parentIndex = queue.Dequeue();
                var current = nodes[parentIndex].state;
                foreach (var slot in GameRules.AllSlots(level))
                {
                    var modeCount = level.pushUnlocked ? 2 : 1;
                    for (var m = 0; m < modeCount; m++)
                    {
                        var action = new SolutionStep { magnet = slot, mode = (ForceMode)m };
                        if (!RailChallengeRules.TrySelect(level, current, slot, out var prepared)) continue;
                        prepared.mode = action.mode;
                        var result = GameRules.Advance(level, prepared);
                        if (!result.moved || !visited.Add(result.state)) continue;

                        var nodeIndex = nodes.Count;
                        nodes.Add(new SearchNode(result.state, parentIndex, action));
                        if (result.won)
                            return BuildResult(nodes, nodeIndex, visited.Count);
                        queue.Enqueue(nodeIndex);
                    }
                }
            }
            return new SolverResult { solved = false, visitedStates = visited.Count };
        }

        public static SolverResult SolveFrom(LevelDefinition level, GameState initial, int maxVisited = 25000)
        {
            if (initial.ball == level.exit && RailChallengeRules.GoalAllowsEntry(level, initial))
                return new SolverResult { solved = true };
            var proxy = SolveInternal(level, initial, maxVisited);
            return proxy;
        }

        private static SolverResult SolveInternal(LevelDefinition level, GameState initial, int maxVisited)
        {
            var nodes = new List<SearchNode> { new(initial, -1, default) };
            var queue = new Queue<int>();
            var visited = new HashSet<GameState> { initial };
            queue.Enqueue(0);
            while (queue.Count > 0 && visited.Count <= maxVisited)
            {
                var parentIndex = queue.Dequeue();
                var current = nodes[parentIndex].state;
                foreach (var slot in GameRules.AllSlots(level))
                {
                    var modeCount = level.pushUnlocked ? 2 : 1;
                    for (var m = 0; m < modeCount; m++)
                    {
                        var action = new SolutionStep { magnet = slot, mode = (ForceMode)m };
                        if (!RailChallengeRules.TrySelect(level, current, slot, out var prepared)) continue;
                        prepared.mode = action.mode;
                        var result = GameRules.Advance(level, prepared);
                        if (!result.moved || !visited.Add(result.state)) continue;
                        var nodeIndex = nodes.Count;
                        nodes.Add(new SearchNode(result.state, parentIndex, action));
                        if (result.won) return BuildResult(nodes, nodeIndex, visited.Count);
                        queue.Enqueue(nodeIndex);
                    }
                }
            }
            return new SolverResult { solved = false, visitedStates = visited.Count };
        }

        private static SolverResult BuildResult(List<SearchNode> nodes, int endIndex, int visited)
        {
            var reversed = new List<SolutionStep>();
            for (var index = endIndex; nodes[index].parent >= 0; index = nodes[index].parent)
                reversed.Add(nodes[index].action);
            reversed.Reverse();
            return new SolverResult { solved = true, movementBeats = reversed.Count, visitedStates = visited, steps = reversed };
        }
    }

    public static partial class ProceduralLevelGenerator
    {
        public static LevelDefinition Create(LevelDefinition source, GameConfig config)
        {
            if (source == null) return null;
            var level = UnityEngine.Object.Instantiate(source);
            level.name = source.name + " (Runtime)";
            var chapter = config != null ? config.GetChapter(level.useNormalPhysics ? 1 : level.levelNumber) : null;
            if (chapter != null) level.pushUnlocked = chapter.pushUnlocked;
            var authoredLayout = source.wallSegments != null && source.wallSegments.Count > 0;
            var generatedLayoutIsCurrent = config != null && source.generatedContentVersion == config.contentVersion &&
                (!config.focusedChapterDesignEnabled ||
                    source.generatedDesignRevision == config.levelDesignRevision);
            if (config == null || !config.proceduralLevelsEnabled || authoredLayout || generatedLayoutIsCurrent)
            {
                if (config != null && !authoredLayout)
                {
                    RedistributeGates(level);
                    LevelMechanicLayout.Arrange(level, config);
                }
                ApplyEnergyTarget(level, chapter);
                RailChallengeGenerator.Apply(level, config);
                ApplyEnergyTarget(level, chapter);
                return level;
            }

            Generate(level, config);
            RailChallengeGenerator.Apply(level, config);
            ApplyEnergyTarget(level, chapter);
            return level;
        }

        public static LevelDefinition CreateEndless(LevelDefinition template, GameConfig config, int endlessIndex)
        {
            if (template == null || config == null) return null;
            var level = UnityEngine.Object.Instantiate(template);
            level.name = $"Endless {Mathf.Max(1, endlessIndex):000} (Runtime)";
            level.levelId = $"endless_{Mathf.Max(1, endlessIndex):000000}";
            level.levelNumber = config.CuratedLevelCount + Mathf.Max(1, endlessIndex);
            level.isEndless = true;
            level.endlessIndex = Mathf.Max(1, endlessIndex);
            level.generatorSeed = unchecked(config.endlessSeedBase + endlessIndex * 104729);
            level.wallSegments = new List<WallSegmentDefinition>();
            level.walls = new List<GridCell>();
            level.gates = new List<GateDefinition>();
            Generate(level, config);
            RailChallengeGenerator.Apply(level, config);
            ApplyEnergyTarget(level, config.GetChapter(level.levelNumber));
            return level;
        }

        public static LevelDefinition CreateDaily(LevelDefinition template, GameConfig config, string dailyKey)
        {
            if (template == null || config == null) return null;
            var numericDay = 0;
            if (!int.TryParse(dailyKey, out numericDay)) numericDay = dailyKey?.GetHashCode() ?? 0;
            var cycle = Mathf.Max(1, config.dailyDifficultyCycle);
            var difficulty = config.dailyMinimumDifficulty + Mathf.Abs(numericDay % cycle);
            var level = UnityEngine.Object.Instantiate(template);
            level.name = $"Daily {dailyKey} (Runtime)";
            level.levelId = $"daily_{dailyKey}";
            level.levelNumber = config.CuratedLevelCount + difficulty;
            level.isEndless = true;
            level.endlessIndex = difficulty;
            level.isDailyChallenge = true;
            level.dailyKey = dailyKey;
            level.generatorSeed = unchecked(config.dailySeedBase + numericDay * 104729);
            level.wallSegments = new List<WallSegmentDefinition>();
            level.walls = new List<GridCell>();
            level.gates = new List<GateDefinition>();
            Generate(level, config);
            RailChallengeGenerator.Apply(level, config);
            ApplyEnergyTarget(level, config.GetChapter(level.levelNumber));
            level.tutorialPrompt = "One shared challenge. One attempt at your best score.";
            return level;
        }

        private static void Generate(LevelDefinition level, GameConfig config)
        {
            var focused = config.focusedChapterDesignEnabled;
            level.generatedDesignRevision = focused ? config.levelDesignRevision : 0;
            level.railChallenge = new RailChallengeDefinition();
            var levelNumber = Mathf.Max(1, level.levelNumber);
            var chapter = config.GetChapter(levelNumber);
            var scoringChapter = config.GetChapter(level.useNormalPhysics ? 1 : levelNumber);
            var size = Mathf.Clamp(chapter?.boardSize ?? 5, 5, 10);
            level.width = size;
            level.height = size;
            level.pushUnlocked = chapter != null && chapter.pushUnlocked;
            level.wallSegments = new List<WallSegmentDefinition>();
            level.gates = new List<GateDefinition>();
            level.movingWalls = new List<MovingWallDefinition>();
            level.fieldZones = new List<FieldZoneDefinition>();
            level.magneticImprints = new List<MagneticImprintDefinition>();
            level.fieldLenses = new List<FieldLensDefinition>();
            level.polarityLocks = new List<PolarityLockDefinition>();
            level.echoMagnetEnabled = false;
            level.ferrofluidMemoryEnabled = false;
            level.orbitGoalEnabled = false;
            level.orbitGoalTurns = 1f;

            var seed = level.generatorSeed != 0 ? level.generatorSeed : 74000 + levelNumber * 977;
            var random = new System.Random(seed);
            level.start = PickBoundaryCell(random, size);
            level.exit = PickDistantBoundaryCell(random, size, level.start);
            var side = (MagnetSide)random.Next(0, 4);
            var slotCount = side == MagnetSide.Left || side == MagnetSide.Right ? level.height : level.width;
            level.useCustomStartingMagnet = true;
            level.startingMagnet = new RailSlot(side, random.Next(0, slotCount));

            var chapterLevel = (levelNumber - 1) % Mathf.Max(1, config.levelsPerChapter);
            var chapterProgress = chapterLevel / (float)Mathf.Max(1, config.levelsPerChapter - 1);
            var density = (chapter?.wallDensity ?? 0.1f) + (chapter?.wallDensityRamp ?? 0.04f) * chapterProgress;
            if (level.isEndless)
                density = Mathf.Min(0.36f, density + Mathf.Min(0.13f, (level.endlessIndex - 1) * 0.002f));
            var maximumWalls = level.isEndless ? config.endlessMaximumWallCells : config.generatorMaximumWallCells;
            var minimumWallGroups = Mathf.Max(1, chapter?.minimumWallGroups ?? 2);
            var targetWalls = Mathf.Clamp(
                Mathf.Max(Mathf.RoundToInt(size * size * density), minimumWallGroups * 2),
                2,
                maximumWalls);
            var maximumLength = Mathf.Clamp(chapter?.maxWallRunLength ?? 3, 2, 7);
            var minimumSolution = Mathf.Max(size - 1, (chapter?.minimumSolutionSteps ?? size - 1) + Mathf.RoundToInt(chapterProgress * 2f));
            if (level.isEndless) minimumSolution += Mathf.Min(5, (level.endlessIndex - 1) / 20);

            SolverResult solved = focused ? BuildProgressiveLayout(level, config, seed) : null;
            List<GridCell> bestWalls = focused ? new List<GridCell>(level.walls) : null;
            var bestComplexityScore = int.MinValue;
            var layoutAttemptLimit = level.isEndless
                ? Mathf.Min(10, config.generatorLayoutAttempts)
                : config.generatorLayoutAttempts;
            for (var layoutAttempt = 0; !focused && layoutAttempt < layoutAttemptLimit; layoutAttempt++)
            {
                var layoutRandom = new System.Random(unchecked(seed + layoutAttempt * 7919));
                // Reserve only spawn and goal. Connectivity validation below keeps
                // every open region reachable without gifting the solver a direct
                // Manhattan corridor through every generated board.
                var reservedPath = new HashSet<GridCell> { level.start, level.exit };
                var walls = new HashSet<GridCell>();
                var wallLimit = Mathf.Min(targetWalls, size * size - reservedPath.Count);
                for (var shapeAttempt = 0;
                     shapeAttempt < config.generatorShapeAttempts && walls.Count < wallLimit;
                     shapeAttempt++)
                {
                    var remaining = wallLimit - walls.Count;
                    if (remaining < 2) break;
                    var length = layoutRandom.Next(2, Mathf.Min(maximumLength, remaining) + 1);
                    TryAddBar(layoutRandom, size, length, minimumWallGroups, reservedPath, walls);
                }

                level.walls = new List<GridCell>(walls);
                var route = FindOpenRoute(level);
                if (route.Count == 0) continue;
                var candidate = LevelSolver.Solve(level);
                if (!candidate.solved) continue;
                var wallGroups = CountWallGroups(size, walls);
                var detour = Mathf.Max(0, route.Count - 1 - ManhattanDistance(level.start, level.exit));
                var blockedLine = IsDirectRouteBlocked(level, config);
                var complexityScore = candidate.movementBeats * 8 + wallGroups * 5 + walls.Count +
                                      detour * Mathf.Max(0, config.generatorDetourWeight) +
                                      (blockedLine ? Mathf.Max(0, config.generatorBlockedLineBonus) : 0);
                if (solved == null || complexityScore > bestComplexityScore)
                {
                    solved = candidate;
                    bestWalls = new List<GridCell>(level.walls);
                    bestComplexityScore = complexityScore;
                }
                if (candidate.movementBeats >= minimumSolution && wallGroups >= minimumWallGroups &&
                    walls.Count >= wallLimit - 1 &&
                    (!config.generatorPreferBlockedDirectPath || blockedLine) &&
                    detour >= Mathf.Max(0, config.generatorMinimumDetourSteps)) break;
            }

            if (solved == null)
            {
                level.walls.Clear();
                solved = LevelSolver.Solve(level);
            }
            else if (!focused)
            {
                level.walls = bestWalls;
                solved = LevelSolver.Solve(level);
            }
            if (solved.solved && !focused && chapter != null && chapter.oneWayGates)
            {
                var gateCount = level.isEndless
                    ? Mathf.Clamp(chapter.gateCount + (level.endlessIndex - 1) / Mathf.Max(1, config.endlessGateInterval), 1, 4)
                    : Mathf.Max(1, chapter.gateCount);
                solved = AddVerifiedGates(level, solved, gateCount);
            }
            level.verifiedSolution = solved.steps;
            if (focused) PopulateFocusedMechanics(level, config, random, ref solved);
            else
            {
                AddChapterMechanics(level, config, chapter, random);
                AddSignatureMechanics(level, config, random);
            }
            level.verifiedSolution = solved.steps;
            LevelMechanicLayout.Arrange(level, config);
            level.targetMoves = Mathf.Max(1, solved.movementBeats);
            ApplyEnergyTarget(level, scoringChapter);
            level.verifiedSolution = solved.steps;
            level.tutorialPrompt = level.useNormalPhysics ? "Standard floor test chamber"
                : level.isEndless ? $"Endless chamber {level.endlessIndex}"
                : levelNumber == 1 ? "Drag the magnet around the box"
                : levelNumber % Mathf.Max(1, config.levelsPerChapter) == 1 ? chapter?.challenge : string.Empty;
        }

        private static void ApplyEnergyTarget(LevelDefinition level, ChapterSettings chapter)
        {
            var energyPerStep = chapter != null ? chapter.SafeTargetEnergyPerStep : 9f;
            level.targetEnergy = Mathf.Max(20f, Mathf.Max(1, level.targetMoves) * energyPerStep);
        }

        private static void AddChapterMechanics(LevelDefinition level, GameConfig config,
            ChapterSettings chapter, System.Random random)
        {
            if (chapter == null || (!chapter.movingWalls && !chapter.fieldZones)) return;
            var candidates = new List<GridCell>();
            var route = FindOpenRoute(level);
            for (var y = 1; y < level.height - 1; y++)
            for (var x = 1; x < level.width - 1; x++)
            {
                var cell = new GridCell(x, y);
                if (cell == level.start || cell == level.exit || level.IsWall(cell) || level.TryGetGate(cell, out _)) continue;
                candidates.Add(cell);
            }
            for (var i = candidates.Count - 1; i > 0; i--)
            {
                var swap = random.Next(i + 1);
                (candidates[i], candidates[swap]) = (candidates[swap], candidates[i]);
            }
            // Stable route-first ordering makes mechanics interact with the path
            // rather than occupying arbitrary, unused corners of the board.
            candidates.Sort((a, b) => DistanceToRoute(a, route).CompareTo(DistanceToRoute(b, route)));

            var cursor = 0;
            if (chapter.movingWalls)
            {
                var count = level.isEndless
                    ? Mathf.Clamp(chapter.movingWallCount + (level.endlessIndex - 1) / 25, 1, 4)
                    : Mathf.Max(1, chapter.movingWallCount);
                for (var i = 0; i < count && cursor < candidates.Count; i++, cursor++)
                {
                    var cell = candidates[cursor];
                    var horizontal = random.Next(2) == 0;
                    level.movingWalls.Add(new MovingWallDefinition
                    {
                        center = CellCenter(level, config, cell),
                        length = config.cellSize * 0.82f,
                        horizontal = horizontal,
                        travel = (horizontal ? Vector2.up : Vector2.right) * config.cellSize * 0.28f,
                        speed = 0.75f + (float)random.NextDouble() * 0.65f + (level.isEndless ? Mathf.Min(0.6f, level.endlessIndex * 0.006f) : 0f),
                        phase = (float)random.NextDouble() * Mathf.PI * 2f
                    });
                }
            }

            if (!chapter.fieldZones) return;
            var zoneCount = level.isEndless
                ? Mathf.Clamp(chapter.fieldZoneCount + (level.endlessIndex - 1) / 30, 1, 5)
                : Mathf.Max(1, chapter.fieldZoneCount);
            for (var i = 0; i < zoneCount && cursor < candidates.Count; i++, cursor++)
            {
                var weak = (i & 1) == 0;
                level.fieldZones.Add(new FieldZoneDefinition
                {
                    center = CellCenter(level, config, candidates[cursor]),
                    radius = config.cellSize * 0.72f,
                    strengthMultiplier = weak ? 0.48f : 1.38f
                });
            }
        }

        private static void AddSignatureMechanics(LevelDefinition level, GameConfig config, System.Random random)
        {
            var number = Mathf.Max(1, level.levelNumber);
            var spacing = Mathf.Max(1, config.signatureMechanicSpacing);
            level.echoMagnetEnabled = number >= config.echoMagnetUnlockLevel;
            level.ferrofluidMemoryEnabled = number >= config.ferrofluidMemoryUnlockLevel &&
                                            (level.isEndless || number == config.ferrofluidMemoryUnlockLevel || number % spacing == 0);
            level.orbitGoalEnabled = number >= config.orbitGoalUnlockLevel &&
                                     (level.isEndless || number == config.orbitGoalUnlockLevel || number % spacing == 0);
            level.orbitGoalEnabled &= OrbitGoalRules.HasClearance(CellCenter(level, config, level.exit),
                new Vector2(level.width, level.height) * config.cellSize, config.ballRadius,
                config.exitRadius, LiveOpsConfigStore.Current.orbitGoalRadiusMultiplier);
            level.orbitGoalTurns = level.isEndless && level.endlessIndex > 30 ? 1.5f : 1f;

            if (number >= config.magneticImprintUnlockLevel && level.walls != null && level.walls.Count > 0)
            {
                var wallCandidates = new List<GridCell>(level.walls);
                Shuffle(wallCandidates, random);
                var wallRoute = FindOpenRoute(level);
                wallCandidates.Sort((a, b) => DistanceToRoute(a, wallRoute).CompareTo(DistanceToRoute(b, wallRoute)));
                var count = Mathf.Min(config.magneticImprintsPerLevel, wallCandidates.Count);
                for (var i = 0; i < count; i++)
                {
                    level.magneticImprints.Add(new MagneticImprintDefinition
                    {
                        center = CellCenter(level, config, wallCandidates[i]),
                        polarity = (i & 1) == 0 ? ForceMode.Pull : ForceMode.Push
                    });
                }
            }

            var open = new List<GridCell>();
            var route = FindOpenRoute(level);
            for (var y = 1; y < level.height - 1; y++)
            for (var x = 1; x < level.width - 1; x++)
            {
                var cell = new GridCell(x, y);
                if (cell == level.start || cell == level.exit || level.IsWall(cell) || level.TryGetGate(cell, out _) ||
                    IsMechanicCellOccupied(level, config, cell)) continue;
                open.Add(cell);
            }
            Shuffle(open, random);
            open.Sort((a, b) => DistanceToRoute(a, route).CompareTo(DistanceToRoute(b, route)));
            var cursor = 0;
            if (number >= config.fieldLensUnlockLevel)
            {
                var count = Mathf.Min(config.fieldLensesPerLevel, open.Count);
                for (var i = 0; i < count && cursor < open.Count; i++, cursor++)
                {
                    level.fieldLenses.Add(new FieldLensDefinition
                    {
                        center = CellCenter(level, config, open[cursor]),
                        radius = config.cellSize * 0.74f,
                        quarterTurns = (i & 1) == 0 ? 1 : -1
                    });
                }
            }

            if (number < config.polarityLockUnlockLevel) return;
            var lockCount = Mathf.Min(config.polarityLocksPerLevel, open.Count - cursor);
            for (var i = 0; i < lockCount && cursor < open.Count; i++, cursor++)
            {
                var length = Mathf.Clamp(config.polarityLockPatternLength + (level.isEndless ? level.endlessIndex / 35 : 0), 2, 5);
                var bits = 0;
                for (var step = 0; step < length; step++)
                    if ((step & 1) == 0) bits |= 1 << step; // PUSH, PULL, PUSH... starts with one visible toggle.
                level.polarityLocks.Add(new PolarityLockDefinition
                {
                    center = CellCenter(level, config, open[cursor]),
                    length = config.cellSize * 0.82f,
                    horizontal = random.Next(2) == 0,
                    patternLength = length,
                    patternBits = bits
                });
            }
        }

        private static void Shuffle<T>(IList<T> list, System.Random random)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var swap = random.Next(i + 1);
                (list[i], list[swap]) = (list[swap], list[i]);
            }
        }

        private static Vector2 CellCenter(LevelDefinition level, GameConfig config, GridCell cell)
        {
            return new Vector2(
                (cell.x - (level.width - 1) * 0.5f) * config.cellSize,
                (cell.y - (level.height - 1) * 0.5f) * config.cellSize);
        }

        private static int ManhattanDistance(GridCell a, GridCell b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        // A connected cell route is a conservative guide for placing mechanics.
        // The game uses continuous physics, so this is not a full physics solution.
        public static List<GridCell> FindOpenRoute(LevelDefinition level)
        {
            var result = new List<GridCell>();
            if (level == null || !level.IsInside(level.start) || !level.IsInside(level.exit) ||
                level.IsWall(level.start) || level.IsWall(level.exit)) return result;
            var queue = new Queue<GridCell>();
            var parents = new Dictionary<GridCell, GridCell> { [level.start] = level.start };
            var directions = new[] { new GridCell(1, 0), new GridCell(0, 1), new GridCell(-1, 0), new GridCell(0, -1) };
            queue.Enqueue(level.start);
            while (queue.Count > 0 && !parents.ContainsKey(level.exit))
            {
                var cell = queue.Dequeue();
                foreach (var direction in directions)
                {
                    var next = cell + direction;
                    if (GameRules.IsBlocked(level, cell, next, direction) || parents.ContainsKey(next)) continue;
                    parents.Add(next, cell);
                    queue.Enqueue(next);
                }
            }
            if (!parents.ContainsKey(level.exit)) return result;
            var cursor = level.exit;
            while (cursor != level.start)
            {
                result.Add(cursor);
                cursor = parents[cursor];
            }
            result.Add(level.start);
            result.Reverse();
            return result;
        }

        private static int DistanceToRoute(GridCell cell, List<GridCell> route)
        {
            var best = int.MaxValue;
            foreach (var routeCell in route) best = Mathf.Min(best, ManhattanDistance(cell, routeCell));
            return best;
        }

        private static bool IsMechanicCellOccupied(LevelDefinition level, GameConfig config, GridCell cell)
        {
            var position = CellCenter(level, config, cell);
            var minimumDistanceSqr = config.cellSize * config.cellSize * 0.64f;
            foreach (var wall in level.movingWalls)
                if ((wall.center - position).sqrMagnitude < minimumDistanceSqr) return true;
            foreach (var zone in level.fieldZones)
                if ((zone.center - position).sqrMagnitude < minimumDistanceSqr) return true;
            return false;
        }

        // Test the actual straight route against the rendered wall runs enlarged by
        // the ball radius. Adjacent wall cells form a continuous rectangular run.
        public static bool IsDirectRouteBlocked(LevelDefinition level, GameConfig config)
        {
            if (level == null || config == null || level.walls == null) return false;
            var from = CellCenter(level, config, level.start);
            var to = CellCenter(level, config, level.exit);
            var half = Mathf.Min(config.wallThickness, config.wallCollisionThickness) * 0.5f +
                       config.ballCollisionRadius;
            var walls = new HashSet<GridCell>(level.walls);
            foreach (var cell in walls)
            {
                var center = CellCenter(level, config, cell);
                if (SegmentIntersectsRect(from, to, new Rect(center - Vector2.one * half, Vector2.one * half * 2f)))
                    return true;
                foreach (var delta in new[] { new GridCell(1, 0), new GridCell(0, 1) })
                {
                    var next = cell + delta;
                    if (!walls.Contains(next)) continue;
                    var end = CellCenter(level, config, next);
                    var min = Vector2.Min(center, end) - Vector2.one * half;
                    var max = Vector2.Max(center, end) + Vector2.one * half;
                    if (SegmentIntersectsRect(from, to, Rect.MinMaxRect(min.x, min.y, max.x, max.y))) return true;
                }
            }
            return false;
        }

        private static bool SegmentIntersectsRect(Vector2 from, Vector2 to, Rect rect)
        {
            var direction = to - from;
            var near = 0f;
            var far = 1f;
            return ClipSegmentAxis(from.x, direction.x, rect.xMin, rect.xMax, ref near, ref far) &&
                   ClipSegmentAxis(from.y, direction.y, rect.yMin, rect.yMax, ref near, ref far);
        }

        private static bool ClipSegmentAxis(float origin, float direction, float min, float max,
            ref float near, ref float far)
        {
            if (Mathf.Abs(direction) < 0.00001f) return origin >= min && origin <= max;
            var a = (min - origin) / direction;
            var b = (max - origin) / direction;
            if (a > b) (a, b) = (b, a);
            near = Mathf.Max(near, a);
            far = Mathf.Min(far, b);
            return near <= far;
        }

        private static SolverResult AddVerifiedGates(LevelDefinition level, SolverResult solution, int gateCount)
        {
            var state = new GameState(level.start, level.startingMagnet, ForceMode.Pull);
            var candidates = new List<GateDefinition>();
            var entries = new Dictionary<GridCell, CardinalDirection>();
            var conflicting = new HashSet<GridCell>();
            for (var i = 0; i < solution.steps.Count; i++)
            {
                var step = solution.steps[i];
                state.magnet = step.magnet;
                state.mode = step.mode;
                var result = GameRules.Advance(level, state);
                if (!result.moved) continue;
                var destination = result.state.ball;
                var direction = GameRules.DirectionFromDelta(new GridCell(destination.x - state.ball.x, destination.y - state.ball.y));
                if (entries.TryGetValue(destination, out var previous) && previous != direction)
                    conflicting.Add(destination);
                entries[destination] = direction;
                if (ManhattanDistance(destination, level.start) >= 2 && ManhattanDistance(destination, level.exit) >= 2 &&
                    !level.IsWall(destination) && !level.TryGetGate(destination, out _))
                {
                    candidates.Add(new GateDefinition
                    {
                        cell = destination,
                        allowedEntryDirection = direction
                    });
                }
                state = result.state;
            }

            for (var n = 0; n < gateCount; n++)
            {
                var selected = -1;
                var score = float.PositiveInfinity;
                for (var i = 0; i < candidates.Count; i++)
                {
                    var candidate = candidates[i];
                    if (conflicting.Contains(candidate.cell)) continue;
                    var clear = true;
                    foreach (var gate in level.gates)
                        if (ManhattanDistance(candidate.cell, gate.cell) < 2) clear = false;
                    if (!clear) continue;
                    var distance = Mathf.Abs((i + 1f) / (candidates.Count + 1f) - (n + 1f) / (gateCount + 1f));
                    if (distance >= score) continue;
                    selected = i;
                    score = distance;
                }
                if (selected >= 0) level.gates.Add(candidates[selected]);
            }

            var verified = LevelSolver.Solve(level);
            if (verified.solved) return verified;
            level.gates.Clear();
            return solution;
        }

        private static void RedistributeGates(LevelDefinition level)
        {
            if (level.gates == null || level.gates.Count == 0) return;
            var original = new List<GateDefinition>(level.gates);
            level.gates.Clear();
            var solved = LevelSolver.Solve(level);
            if (!solved.solved) { level.gates = original; return; }
            solved = AddVerifiedGates(level, solved, original.Count);
            level.verifiedSolution = solved.steps;
            level.targetMoves = Mathf.Max(1, solved.movementBeats);
        }

        private static void TryAddBar(System.Random random, int size, int length, int minimumGroups,
            HashSet<GridCell> reservedPath, HashSet<GridCell> walls)
        {
            var previousGroups = CountWallGroups(size, walls);
            var horizontal = random.Next(0, 2) == 0;
            var startX = horizontal ? random.Next(0, size - length + 1) : random.Next(0, size);
            var startY = horizontal ? random.Next(0, size) : random.Next(0, size - length + 1);
            var cells = new List<GridCell>(length);
            for (var i = 0; i < length; i++)
            {
                var cell = new GridCell(startX + (horizontal ? i : 0), startY + (horizontal ? 0 : i));
                if (reservedPath.Contains(cell)) return;
                cells.Add(cell);
            }
            var added = new List<GridCell>(cells.Count);
            for (var i = 0; i < cells.Count; i++)
            {
                if (walls.Add(cells[i])) added.Add(cells[i]);
            }
            var newGroups = CountWallGroups(size, walls);
            var groupRequirementMet = previousGroups >= minimumGroups
                ? newGroups >= minimumGroups
                : newGroups > previousGroups;
            if (added.Count >= 2 && groupRequirementMet && !ContainsSolidBlock(size, walls) &&
                AllOpenCellsConnected(size, walls)) return;
            for (var i = 0; i < added.Count; i++) walls.Remove(added[i]);
        }

        public static int CountWallGroups(LevelDefinition level)
        {
            return level == null || level.walls == null
                ? 0
                : CountWallGroups(Mathf.Max(level.width, level.height), new HashSet<GridCell>(level.walls));
        }

        private static int CountWallGroups(int size, HashSet<GridCell> walls)
        {
            var included = new HashSet<GridCell>();
            var groups = 0;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size;)
                {
                    if (!walls.Contains(new GridCell(x, y))) { x++; continue; }
                    var start = x;
                    while (x + 1 < size && walls.Contains(new GridCell(x + 1, y))) x++;
                    if (x - start + 1 >= 2)
                    {
                        groups++;
                        for (var mark = start; mark <= x; mark++) included.Add(new GridCell(mark, y));
                    }
                    x++;
                }
            }
            for (var x = 0; x < size; x++)
            {
                for (var y = 0; y < size;)
                {
                    if (!walls.Contains(new GridCell(x, y))) { y++; continue; }
                    var start = y;
                    while (y + 1 < size && walls.Contains(new GridCell(x, y + 1))) y++;
                    if (y - start + 1 >= 2)
                    {
                        groups++;
                        for (var mark = start; mark <= y; mark++) included.Add(new GridCell(x, mark));
                    }
                    y++;
                }
            }
            foreach (var wall in walls)
                if (!included.Contains(wall)) groups++;
            return groups;
        }

        private static bool ContainsSolidBlock(int size, HashSet<GridCell> walls)
        {
            for (var y = 0; y < size - 1; y++)
            for (var x = 0; x < size - 1; x++)
            {
                if (walls.Contains(new GridCell(x, y)) &&
                    walls.Contains(new GridCell(x + 1, y)) &&
                    walls.Contains(new GridCell(x, y + 1)) &&
                    walls.Contains(new GridCell(x + 1, y + 1))) return true;
            }
            return false;
        }

        private static bool AllOpenCellsConnected(int size, HashSet<GridCell> walls)
        {
            GridCell first = default;
            var foundFirst = false;
            var openCount = 0;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var cell = new GridCell(x, y);
                if (walls.Contains(cell)) continue;
                openCount++;
                if (foundFirst) continue;
                first = cell;
                foundFirst = true;
            }
            if (!foundFirst) return false;

            var visited = new HashSet<GridCell> { first };
            var queue = new Queue<GridCell>();
            queue.Enqueue(first);
            var directions = new[]
            {
                new GridCell(1, 0), new GridCell(-1, 0),
                new GridCell(0, 1), new GridCell(0, -1)
            };
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                for (var i = 0; i < directions.Length; i++)
                {
                    var next = current + directions[i];
                    if (next.x < 0 || next.y < 0 || next.x >= size || next.y >= size) continue;
                    if (walls.Contains(next) || !visited.Add(next)) continue;
                    queue.Enqueue(next);
                }
            }
            return visited.Count == openCount;
        }

        private static HashSet<GridCell> BuildReservedPath(GridCell start, GridCell exit, bool horizontalFirst)
        {
            var path = new HashSet<GridCell> { start, exit };
            var cursor = start;
            if (horizontalFirst)
            {
                AddHorizontal(path, ref cursor, exit.x);
                AddVertical(path, ref cursor, exit.y);
            }
            else
            {
                AddVertical(path, ref cursor, exit.y);
                AddHorizontal(path, ref cursor, exit.x);
            }
            return path;
        }

        private static void AddHorizontal(HashSet<GridCell> path, ref GridCell cursor, int targetX)
        {
            var direction = targetX >= cursor.x ? 1 : -1;
            while (cursor.x != targetX)
            {
                cursor = new GridCell(cursor.x + direction, cursor.y);
                path.Add(cursor);
            }
        }

        private static void AddVertical(HashSet<GridCell> path, ref GridCell cursor, int targetY)
        {
            var direction = targetY >= cursor.y ? 1 : -1;
            while (cursor.y != targetY)
            {
                cursor = new GridCell(cursor.x, cursor.y + direction);
                path.Add(cursor);
            }
        }

        private static GridCell PickDistantBoundaryCell(System.Random random, int size, GridCell start)
        {
            var fallback = new GridCell(size - 1 - start.x, size - 1 - start.y);
            for (var attempt = 0; attempt < 32; attempt++)
            {
                var candidate = PickBoundaryCell(random, size);
                var distance = Mathf.Abs(candidate.x - start.x) + Mathf.Abs(candidate.y - start.y);
                if (candidate != start && distance >= size - 1) return candidate;
            }
            return fallback;
        }

        private static GridCell PickBoundaryCell(System.Random random, int size)
        {
            var side = random.Next(0, 4);
            var index = random.Next(0, size);
            return side switch
            {
                0 => new GridCell(0, index),
                1 => new GridCell(size - 1, index),
                2 => new GridCell(index, 0),
                _ => new GridCell(index, size - 1)
            };
        }
    }
}
