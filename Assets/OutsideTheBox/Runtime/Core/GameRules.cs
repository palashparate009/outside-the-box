using System.Collections.Generic;
using UnityEngine;

namespace OutsideTheBox
{
    public static class OrbitGoalRules
    {
        // A full orbit needs the whole ball inside the arena at every angle.
        // Boundary pockets generated for normal goals cannot satisfy that condition.
        public static bool HasClearance(Vector2 center, Vector2 boardSize, float ballRadius,
            float exitRadius, float radiusMultiplier, IEnumerable<Rect> obstacles = null)
        {
            var orbitRadius = exitRadius * (0.85f + radiusMultiplier) * 0.5f;
            var clearance = orbitRadius + ballRadius + 0.02f;
            if (Mathf.Abs(center.x) + clearance > boardSize.x * 0.5f ||
                Mathf.Abs(center.y) + clearance > boardSize.y * 0.5f) return false;
            if (obstacles != null)
                foreach (var obstacle in obstacles)
                {
                    var nearest = new Vector2(Mathf.Clamp(center.x, obstacle.xMin, obstacle.xMax),
                        Mathf.Clamp(center.y, obstacle.yMin, obstacle.yMax));
                    if ((nearest - center).sqrMagnitude <= clearance * clearance) return false;
                }
            return true;
        }
    }

    /// <summary>Pure deterministic rules shared by runtime, hints, validation and editor generation.</summary>
    public static class GameRules
    {
        private static readonly GridCell[] DirectionVectors =
        {
            new(0, 1), new(1, 0), new(0, -1), new(-1, 0)
        };

        public static MoveResult Advance(LevelDefinition level, GameState state)
        {
            if (!RailChallengeRules.TrySelect(level, state, state.magnet, out _))
                return new MoveResult(state, false, true, false);
            var target = MagnetCoordinate(level, state.magnet);
            var dx = target.x - state.ball.x;
            var dy = target.y - state.ball.y;

            // The documented tie rule is horizontal.
            GridCell delta;
            if (Mathf.Abs(dx) >= Mathf.Abs(dy))
                delta = new GridCell(dx >= 0 ? 1 : -1, 0);
            else
                delta = new GridCell(0, dy >= 0 ? 1 : -1);

            if (state.mode == ForceMode.Push)
                delta = new GridCell(-delta.x, -delta.y);

            var destination = state.ball + delta;
            var blocked = IsBlocked(level, state.ball, destination, delta);
            var next = state;
            if (!blocked) next.ball = destination;
            if (!blocked && level.railChallenge?.kind == RailChallengeKind.UnlockSwitch &&
                destination == level.railChallenge.switchCell) next.railSwitchUnlocked = true;
            return new MoveResult(next, !blocked, blocked, !blocked && destination == level.exit &&
                RailChallengeRules.GoalAllowsEntry(level, next));
        }

        public static bool IsBlocked(LevelDefinition level, GridCell from, GridCell to, GridCell movement)
        {
            if (!level.IsInside(to) || level.IsWall(to)) return true;
            if (!level.TryGetGate(to, out var gate)) return false;
            return DirectionVector(gate.allowedEntryDirection) != movement;
        }

        public static Vector2Int MagnetCoordinate(LevelDefinition level, RailSlot slot)
        {
            return slot.side switch
            {
                MagnetSide.Left => new Vector2Int(-1, Mathf.Clamp(slot.index, 0, level.height - 1)),
                MagnetSide.Right => new Vector2Int(level.width, Mathf.Clamp(slot.index, 0, level.height - 1)),
                MagnetSide.Bottom => new Vector2Int(Mathf.Clamp(slot.index, 0, level.width - 1), -1),
                _ => new Vector2Int(Mathf.Clamp(slot.index, 0, level.width - 1), level.height)
            };
        }

        public static int SlotCount(LevelDefinition level) => 2 * level.width + 2 * level.height;

        public static IEnumerable<RailSlot> AllSlots(LevelDefinition level)
        {
            for (var y = 0; y < level.height; y++) yield return new RailSlot(MagnetSide.Left, y);
            for (var y = 0; y < level.height; y++) yield return new RailSlot(MagnetSide.Right, y);
            for (var x = 0; x < level.width; x++) yield return new RailSlot(MagnetSide.Bottom, x);
            for (var x = 0; x < level.width; x++) yield return new RailSlot(MagnetSide.Top, x);
        }

        public static GridCell DirectionVector(CardinalDirection direction) => DirectionVectors[(int)direction];

        public static CardinalDirection DirectionFromDelta(GridCell delta)
        {
            if (delta.y > 0) return CardinalDirection.Up;
            if (delta.x > 0) return CardinalDirection.Right;
            if (delta.y < 0) return CardinalDirection.Down;
            return CardinalDirection.Left;
        }
    }
}
