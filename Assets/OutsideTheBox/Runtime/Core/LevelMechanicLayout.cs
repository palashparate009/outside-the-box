using System.Collections.Generic;
using UnityEngine;

namespace OutsideTheBox
{
    // Places interactive pieces along the verified route, reserving their complete
    // footprint (including moving-wall travel), rather than just their centre cell.
    public static class LevelMechanicLayout
    {
        private static readonly GridCell[] WallConnections = { new(1, 0), new(0, 1) };
        private sealed class Placement
        {
            public Vector2 center;
            public Vector2 halfSize;
            public bool horizontal;
        }

        public static void Arrange(LevelDefinition level, GameConfig config)
        {
            if (level == null || config == null ||
                (level.wallSegments != null && level.wallSegments.Count > 0)) return;
            var route = VerifiedRoute(level);
            if (route.Count < 3) return;
            var occupied = new List<Rect>();
            var cellSize = config.cellSize;
            var wallHalf = Mathf.Min(config.wallThickness, config.wallCollisionThickness) * 0.5f;
            foreach (var gate in level.gates)
                occupied.Add(Bounds(Center(level, config, gate.cell), Vector2.one * cellSize * config.gateSize * 0.5f));
            if (level.railChallenge?.kind == RailChallengeKind.UnlockSwitch)
                occupied.Add(Bounds(Center(level, config, level.railChallenge.switchCell),
                    Vector2.one * cellSize * 0.4f));

            // Reserve solid mechanics first. A moving shutter runs across the route
            // and retracts along its length, leaving an opening at either end.
            for (var i = 0; i < level.movingWalls.Count;)
            {
                var target = (i + 1f) / (level.movingWalls.Count + 1f);
                var placement = Find(level, config, route, occupied, target,
                    new Vector2(cellSize * 0.62f, wallHalf), true, 0.15f);
                if (placement == null) { level.movingWalls.RemoveAt(i); continue; }
                var wall = level.movingWalls[i];
                wall.center = placement.center;
                wall.horizontal = placement.horizontal;
                wall.length = cellSize * 0.76f;
                wall.travel = (wall.horizontal ? Vector2.right : Vector2.up) * cellSize * 0.24f;
                level.movingWalls[i] = wall;
                occupied.Add(Bounds(placement.center, placement.halfSize));
                i++;
            }
            for (var i = 0; i < level.polarityLocks.Count;)
            {
                var placement = Find(level, config, route, occupied,
                    (i + 1f) / (level.polarityLocks.Count + 1f),
                    new Vector2(level.polarityLocks[i].length * 0.5f, wallHalf), true, 0.15f);
                if (placement == null) { level.polarityLocks.RemoveAt(i); continue; }
                var item = level.polarityLocks[i];
                item.center = placement.center;
                item.horizontal = placement.horizontal;
                level.polarityLocks[i] = item;
                occupied.Add(Bounds(placement.center, placement.halfSize));
                i++;
            }

            // Spread zones and lenses over the journey, rather than stacking them
            // at the first few open cells nearest one corner of the board.
            var lenses = new List<FieldLensDefinition>(level.fieldLenses);
            var zones = new List<FieldZoneDefinition>(level.fieldZones);
            level.fieldLenses.Clear();
            level.fieldZones.Clear();
            var effectCount = lenses.Count + zones.Count;
            var effectIndex = 0;
            for (var i = 0; i < Mathf.Max(lenses.Count, zones.Count); i++)
            {
                // Give each mechanic a turn before adding duplicates of either.
                if (i < zones.Count)
                {
                    var radius = cellSize * 0.56f;
                    var target = (++effectIndex) / (effectCount + 1f);
                    var placement = Find(level, config, route, occupied,
                        target, Vector2.one * radius, false, 0.25f);
                    if (placement == null)
                    {
                        radius = cellSize * 0.46f;
                        placement = Find(level, config, route, occupied, target,
                            Vector2.one * radius, false, 0.25f);
                    }
                    if (placement != null)
                    {
                        var item = zones[i];
                        item.center = placement.center;
                        item.radius = radius;
                        level.fieldZones.Add(item);
                        occupied.Add(Bounds(placement.center, placement.halfSize));
                    }
                }
                if (i < lenses.Count)
                {
                    var placement = Find(level, config, route, occupied,
                        (++effectIndex) / (effectCount + 1f), Vector2.one * cellSize * 0.48f, false, 0.25f);
                    if (placement != null)
                    {
                        var item = lenses[i];
                        item.center = placement.center;
                        item.radius = cellSize * 0.48f;
                        level.fieldLenses.Add(item);
                        occupied.Add(Bounds(placement.center, placement.halfSize));
                    }
                }
            }

            // Imprints belong to a wall beside the route. Keep their badges away
            // from gates and other markers, and spread their positions as well.
            var imprintCenters = new List<Vector2>();
            for (var i = 0; i < level.magneticImprints.Count;)
            {
                var target = (i + 1f) / (level.magneticImprints.Count + 1f);
                var bestScore = float.PositiveInfinity;
                var best = Vector2.zero;
                foreach (var cell in level.walls)
                {
                    var position = Center(level, config, cell);
                    var badgeHalf = Vector2.one * config.wallThickness * 0.65f;
                    if (!Inside(level, config, position, badgeHalf) ||
                        Overlaps(occupied, Bounds(position, badgeHalf), cellSize * 0.12f)) continue;
                    var separated = true;
                    foreach (var previous in imprintCenters)
                        if (Vector2.Distance(previous, position) < cellSize * 1.5f) separated = false;
                    if (!separated) continue;
                    for (var r = 1; r < route.Count - 1; r++)
                    {
                        var distance = Vector2.Distance(position, Center(level, config, route[r]));
                        if (distance > cellSize * 1.05f) continue;
                        var score = Mathf.Abs(r / (float)(route.Count - 1) - target) * 6f + distance / cellSize;
                        if (score >= bestScore) continue;
                        bestScore = score;
                        best = position;
                    }
                }
                if (float.IsPositiveInfinity(bestScore)) { level.magneticImprints.RemoveAt(i); continue; }
                var item = level.magneticImprints[i];
                item.center = best;
                level.magneticImprints[i] = item;
                imprintCenters.Add(best);
                i++;
            }
        }

        public static List<GridCell> VerifiedRoute(LevelDefinition level)
        {
            var route = new List<GridCell> { level.start };
            var state = new GameState(level.start, RailChallengeRules.StartingSlot(level), ForceMode.Pull);
            if (level.verifiedSolution != null)
                foreach (var step in level.verifiedSolution)
                {
                    if (!RailChallengeRules.TrySelect(level, state, step.magnet, out state))
                        return ProceduralLevelGenerator.FindOpenRoute(level);
                    state.mode = step.mode;
                    var result = GameRules.Advance(level, state);
                    if (!result.moved) return ProceduralLevelGenerator.FindOpenRoute(level);
                    state = result.state;
                    route.Add(state.ball);
                    if (result.won) return route;
                }
            return ProceduralLevelGenerator.FindOpenRoute(level);
        }

        private static Placement Find(LevelDefinition level, GameConfig config, List<GridCell> route,
            List<Rect> occupied, float target, Vector2 halfSize, bool orientAcrossRoute, float routeOffset)
        {
            Placement best = null;
            var bestScore = float.PositiveInfinity;
            for (var r = 1; r < route.Count - 1; r++)
            {
                var routePosition = Center(level, config, route[r]);
                var delta = route[r + 1] + new GridCell(-route[r - 1].x, -route[r - 1].y);
                var horizontal = Mathf.Abs(delta.y) >= Mathf.Abs(delta.x);
                var half = orientAcrossRoute && !horizontal ? new Vector2(halfSize.y, halfSize.x) : halfSize;
                var boardHalf = new Vector2(level.width, level.height) * config.cellSize * 0.5f;
                var inset = half + Vector2.one * config.cellSize * 0.1f;
                var position = new Vector2(Mathf.Clamp(routePosition.x, -boardHalf.x + inset.x, boardHalf.x - inset.x),
                    Mathf.Clamp(routePosition.y, -boardHalf.y + inset.y, boardHalf.y - inset.y));
                if (Vector2.Distance(position, routePosition) > config.cellSize * routeOffset ||
                    !Inside(level, config, position, half)) continue;
                var endpointClearance = Mathf.Max(config.exitRadius, config.ballRadius) + half.magnitude + config.cellSize * 0.25f;
                if (Vector2.Distance(position, Center(level, config, level.start)) < endpointClearance ||
                    Vector2.Distance(position, Center(level, config, level.exit)) < endpointClearance) continue;
                var footprint = Bounds(position, half);
                if (Overlaps(occupied, footprint, config.cellSize * 0.14f)) continue;
                var clear = true;
                foreach (var wall in level.walls)
                {
                    var wallCenter = Center(level, config, wall);
                    var wallHalf = Vector2.one * config.wallThickness * 0.5f;
                    if (!level.IsWall(wall + new GridCell(1, 0)) && !level.IsWall(wall + new GridCell(-1, 0)) &&
                        !level.IsWall(wall + new GridCell(0, 1)) && !level.IsWall(wall + new GridCell(0, -1)))
                    {
                        var standaloneHorizontal = ((wall.x + wall.y + level.levelNumber) & 1) == 0;
                        var available = standaloneHorizontal
                            ? level.width * config.cellSize * 0.5f - Mathf.Abs(wallCenter.x)
                            : level.height * config.cellSize * 0.5f - Mathf.Abs(wallCenter.y);
                        var lengthHalf = Mathf.Max(config.wallThickness * 0.5f,
                            Mathf.Min(config.cellSize * config.standaloneWallLength * 0.5f, available));
                        if (standaloneHorizontal) wallHalf.x = lengthHalf;
                        else wallHalf.y = lengthHalf;
                    }
                    var wallBounds = Bounds(wallCenter, wallHalf);
                    if (footprint.Overlaps(wallBounds)) { clear = false; break; }
                    foreach (var direction in WallConnections)
                    {
                        if (!level.IsWall(wall + direction)) continue;
                        var end = Center(level, config, wall + direction);
                        var min = Vector2.Min(wallCenter, end) - wallHalf;
                        var max = Vector2.Max(wallCenter, end) + wallHalf;
                        if (footprint.Overlaps(Rect.MinMaxRect(min.x, min.y, max.x, max.y))) clear = false;
                    }
                    if (!clear) break;
                }
                if (!clear) continue;
                var score = Mathf.Abs(r / (float)(route.Count - 1) - target);
                if (score >= bestScore) continue;
                bestScore = score;
                best = new Placement { center = position, halfSize = half, horizontal = horizontal };
            }
            return best;
        }

        private static bool Inside(LevelDefinition level, GameConfig config, Vector2 center, Vector2 half) =>
            Mathf.Abs(center.x) + half.x <= level.width * config.cellSize * 0.5f - config.cellSize * 0.08f &&
            Mathf.Abs(center.y) + half.y <= level.height * config.cellSize * 0.5f - config.cellSize * 0.08f;

        private static bool Overlaps(List<Rect> occupied, Rect bounds, float gap)
        {
            bounds = Rect.MinMaxRect(bounds.xMin - gap, bounds.yMin - gap, bounds.xMax + gap, bounds.yMax + gap);
            foreach (var other in occupied) if (bounds.Overlaps(other)) return true;
            return false;
        }

        private static Rect Bounds(Vector2 center, Vector2 half) => new(center - half, half * 2f);
        private static Vector2 Center(LevelDefinition level, GameConfig config, GridCell cell) =>
            new((cell.x - (level.width - 1) * 0.5f) * config.cellSize,
                (cell.y - (level.height - 1) * 0.5f) * config.cellSize);
    }
}
