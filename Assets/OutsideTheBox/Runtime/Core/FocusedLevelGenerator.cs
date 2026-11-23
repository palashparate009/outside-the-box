using System.Collections.Generic;
using UnityEngine;

namespace OutsideTheBox
{
    public static partial class ProceduralLevelGenerator
    {
        public static int FocusedRouteTarget(LevelDefinition level, GameConfig config)
        {
            var number = level.isEndless ? config.CuratedLevelCount + Mathf.Min(60, level.endlessIndex) / 4
                : Mathf.Max(1, level.levelNumber);
            return 6 + Mathf.RoundToInt((number - 1) * .46f);
        }

        public static int RouteTurns(IReadOnlyList<GridCell> route)
        {
            var turns = 0;
            for (var i = 2; i < route.Count; i++)
            {
                var a = route[i - 1] + new GridCell(-route[i - 2].x, -route[i - 2].y);
                var b = route[i] + new GridCell(-route[i - 1].x, -route[i - 1].y);
                if (a != b) turns++;
            }
            return turns;
        }

        private static SolverResult BuildProgressiveLayout(LevelDefinition level, GameConfig config, int seed)
        {
            var size = level.width;
            var number = level.isEndless ? config.CuratedLevelCount + level.endlessIndex : level.levelNumber;
            var baffles = number < 7 ? 1 : number < 21 ? 2 : number < 51 ? 3 : 4;
            baffles = Mathf.Min(baffles, (size - 1) / 2);
            var plan = ChapterMechanicPlan.For(level, config);
            // Orbit chambers retain a larger final room for a full, fair circle.
            if (plan.Has(LevelMechanicKind.Orbit)) baffles = Mathf.Min(baffles, 3);
            var target = FocusedRouteTarget(level, config);
            var bestScore = float.NegativeInfinity;
            List<GridCell> best = null;
            var start = level.start; var goal = level.exit;
            // Score inexpensive geometric paths first; run the magnetic solver
            // only for the chosen candidate, rather than for every wall attempt.
            for (var attempt = 0; attempt < 32; attempt++)
            {
                var random = new System.Random(unchecked(seed + attempt * 7919));
                var rotation = random.Next(4);
                var mirror = random.Next(2) == 0;
                var rooms = number >= 21 && (attempt & 1) == 0;
                var hooks = number >= 21 && attempt % 3 == 0 && baffles > 1;
                var bars = hooks ? baffles - 1 : baffles;
                var room = size - 2 * bars;
                var offset = room > 1 ? random.Next(1, Mathf.Min(3, room + 1)) : 1;
                if (plan.Has(LevelMechanicKind.Orbit)) offset = 1;
                var cells = new HashSet<GridCell>();
                if (rooms)
                    BuildRoomMaze(level, random, cells);
                for (var bar = 0; !rooms && bar < bars; bar++)
                {
                    var gap = random.Next(2, Mathf.Min(4, size - 1));
                    var y = offset + bar * 2;
                    if (hooks) y = Mathf.RoundToInt((bar + 1f) * (size - 1) / (bars + 1));
                    var left = (bar & 1) == 0;
                    var first = left ? 0 : gap;
                    var last = left ? size - gap - 1 : size - 1;
                    for (var x = first; x <= last; x++)
                        cells.Add(FocusedCell(new GridCell(x, y), size, rotation, mirror));
                    if (hooks && bar < bars - 1)
                    {
                        // A deliberate L corner, leaving a full cell-width lane
                        // before the next barrier; no disconnected wall fragments.
                        var end = left ? last : first;
                        cells.Add(FocusedCell(new GridCell(end, y + 1), size, rotation, mirror));
                    }
                }
                if (!AllOpenCellsConnected(size, cells)) continue;
                if (!rooms)
                {
                    level.start = FocusedCell(new GridCell(0, 0), size, rotation, mirror);
                    level.exit = FocusedCell(new GridCell((bars & 1) == 1 ? 0 : size - 1, size - 1), size, rotation, mirror);
                }
                level.walls = new List<GridCell>(cells);
                var route = FindOpenRoute(level);
                if (route.Count < 2) continue;
                var steps = route.Count - 1;
                var turns = RouteTurns(route);
                var detour = steps - ManhattanDistance(level.start, level.exit);
                // Meeting a rising route target matters more than decorating a
                // board with additional bars. Penalize under-target candidates.
                var score = -Mathf.Abs(steps - target) * 2f - Mathf.Max(0, target - steps) * 6f
                    + Mathf.Min(turns, 2 + number / 10) * 3f + Mathf.Min(detour, 12) + (hooks ? 2f : 0f)
                    + (rooms == (number % 3 != 0) ? 10f : 0f);
                if (score <= bestScore) continue;
                bestScore = score; best = new List<GridCell>(level.walls); start = level.start; goal = level.exit;
            }
            level.start = start; level.exit = goal;
            level.walls = best ?? new List<GridCell>();
            return LevelSolver.Solve(level);
        }

        private static GridCell FocusedCell(GridCell cell, int size, int rotation, bool mirror)
        {
            if (mirror) cell = new GridCell(size - 1 - cell.x, cell.y);
            for (var i = 0; i < rotation; i++) cell = new GridCell(size - 1 - cell.y, cell.x);
            return cell;
        }

        private static void BuildRoomMaze(LevelDefinition level, System.Random random, HashSet<GridCell> walls)
        {
            var size = level.width;
            var dividers = size >= 8 ? new[] { size / 3, size * 2 / 3 }
                : new[] { size / 2 };
            var count = dividers.Length + 1;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                if (System.Array.IndexOf(dividers, x) >= 0 || System.Array.IndexOf(dividers, y) >= 0)
                    walls.Add(new GridCell(x, y));
            var visited = new HashSet<GridCell>();
            var stack = new Stack<GridCell>();
            var startRoom = new GridCell(random.Next(count), random.Next(count));
            visited.Add(startRoom); stack.Push(startRoom);
            var directions = new[] { new GridCell(1, 0), new GridCell(-1, 0), new GridCell(0, 1), new GridCell(0, -1) };
            while (stack.Count > 0)
            {
                var room = stack.Peek();
                var nextRooms = new List<GridCell>();
                foreach (var d in directions)
                {
                    var next = room + d;
                    if (next.x >= 0 && next.y >= 0 && next.x < count && next.y < count && !visited.Contains(next))
                        nextRooms.Add(next);
                }
                if (nextRooms.Count == 0) { stack.Pop(); continue; }
                var chosen = nextRooms[random.Next(nextRooms.Count)];
                var horizontal = chosen.x != room.x;
                var coordinate = horizontal ? room.y : room.x;
                var low = coordinate == 0 ? 0 : dividers[coordinate - 1] + 1;
                var high = coordinate == dividers.Length ? size - 1 : dividers[coordinate] - 1;
                var opening = random.Next(low, high + 1);
                var divider = dividers[horizontal ? Mathf.Min(room.x, chosen.x) : Mathf.Min(room.y, chosen.y)];
                walls.Remove(horizontal ? new GridCell(divider, opening) : new GridCell(opening, divider));
                visited.Add(chosen); stack.Push(chosen);
            }
            // Do not create isolated posts: their physical sprite extends beyond
            // a grid cell and previously made tiny fragments look accidental.
            var singles = new List<GridCell>();
            foreach (var wall in walls)
            {
                var connected = false;
                foreach (var d in directions) if (walls.Contains(wall + d)) connected = true;
                if (!connected) singles.Add(wall);
            }
            foreach (var single in singles) walls.Remove(single);
            level.walls = new List<GridCell>(walls);
            // Pick well-separated boundary endpoints across the actual maze,
            // rather than placing the goal beside the entrance of a long branch.
            var boundary = new List<GridCell>();
            for (var i = 0; i < size; i++)
            {
                boundary.Add(new GridCell(i, 0)); boundary.Add(new GridCell(i, size - 1));
                if (i > 0 && i < size - 1) { boundary.Add(new GridCell(0, i)); boundary.Add(new GridCell(size - 1, i)); }
            }
            var longest = -1;
            var order = random.Next(boundary.Count);
            for (var i = 0; i < boundary.Count; i++)
            {
                var from = boundary[(i + order) % boundary.Count];
                if (walls.Contains(from)) continue;
                var distances = new Dictionary<GridCell, int> { [from] = 0 };
                var queue = new Queue<GridCell>(); queue.Enqueue(from);
                while (queue.Count > 0)
                {
                    var cell = queue.Dequeue();
                    foreach (var d in directions)
                    {
                        var next = cell + d;
                        if (!level.IsInside(next) || walls.Contains(next) || distances.ContainsKey(next)) continue;
                        distances[next] = distances[cell] + 1; queue.Enqueue(next);
                    }
                }
                foreach (var to in boundary)
                {
                    if (!distances.TryGetValue(to, out var distance) || distance <= longest) continue;
                    longest = distance; level.start = from; level.exit = to;
                }
            }
        }

        private static void PopulateFocusedMechanics(LevelDefinition level, GameConfig config,
            System.Random random, ref SolverResult solution)
        {
            var plan = ChapterMechanicPlan.For(level, config);
            level.primaryMechanic = plan.primary;
            level.supportingMechanic = plan.supporting;
            if (plan.Has(LevelMechanicKind.Gates)) solution = AddVerifiedGates(level, solution, 1);
            var route = LevelMechanicLayout.VerifiedRoute(level);
            if (route.Count < 3) route = FindOpenRoute(level);
            var centerCell = route.Count > 2 ? route[route.Count / 2] : level.start;
            var center = CellCenter(level, config, centerCell);
            if (plan.Has(LevelMechanicKind.MovingWalls))
                level.movingWalls.Add(new MovingWallDefinition { center = center, length = config.cellSize * .8f,
                    horizontal = true, travel = Vector2.right * config.cellSize * .24f, speed = .9f,
                    phase = (float)random.NextDouble() * Mathf.PI * 2f });
            if (plan.Has(LevelMechanicKind.FieldZones))
                level.fieldZones.Add(new FieldZoneDefinition { center = center, radius = config.cellSize * .55f,
                    strengthMultiplier = .6f });
            level.echoMagnetEnabled = plan.Has(LevelMechanicKind.Echo);
            level.ferrofluidMemoryEnabled = plan.Has(LevelMechanicKind.Memory);
            if (plan.Has(LevelMechanicKind.Imprints) && level.walls.Count > 0)
                level.magneticImprints.Add(new MagneticImprintDefinition { center = CellCenter(level, config, level.walls[0]), polarity = ForceMode.Pull });
            if (plan.Has(LevelMechanicKind.Lenses))
                level.fieldLenses.Add(new FieldLensDefinition { center = center, radius = config.cellSize * .48f, quarterTurns = 1 });
            if (plan.Has(LevelMechanicKind.PolarityLocks))
                level.polarityLocks.Add(new PolarityLockDefinition { center = center, length = config.cellSize * .8f,
                    horizontal = true, patternLength = 2, patternBits = 1 });
            if (plan.Has(LevelMechanicKind.Orbit))
            {
                // Normal pockets live on the boundary. An orbit needs a clear
                // interior goal, with the full ring and ball inside the arena.
                var best = -1;
                GridCell goal = level.exit;
                for (var y = 1; y < level.height - 1; y++)
                for (var x = 1; x < level.width - 1; x++)
                {
                    var cell = new GridCell(x, y);
                    if (level.IsWall(cell) || level.TryGetGate(cell, out _)) continue;
                    var clear = true;
                    foreach (var wall in level.walls)
                        if (Mathf.Max(Mathf.Abs(wall.x - x), Mathf.Abs(wall.y - y)) <= 1) clear = false;
                    if (!clear || !OrbitGoalRules.HasClearance(CellCenter(level, config, cell),
                        new Vector2(level.width, level.height) * config.cellSize, config.ballRadius,
                        config.exitRadius, LiveOpsConfigStore.Current.orbitGoalRadiusMultiplier)) continue;
                    var originalGoal = level.exit; level.exit = cell;
                    var score = FindOpenRoute(level).Count;
                    level.exit = originalGoal;
                    if (score <= best) continue;
                    best = score; goal = cell;
                }
                if (best >= 0)
                {
                    var previousStart = level.start;
                    var previous = level.exit; level.exit = goal;
                    // If only a nearby room has enough orbit clearance, approach
                    // it from the far end of the maze instead of gifting a shortcut.
                    if (best < FocusedRouteTarget(level, config) * .75f) level.start = previous;
                    var candidate = LevelSolver.Solve(level);
                    if (candidate.solved) { solution = candidate; level.orbitGoalEnabled = true; }
                    else { level.exit = previous; level.start = previousStart; }
                }
            }
        }
    }
}
