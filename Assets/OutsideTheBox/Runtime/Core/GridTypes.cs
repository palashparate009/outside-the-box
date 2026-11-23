using System;
using UnityEngine;

namespace OutsideTheBox
{
    public enum MagnetSide { Left, Right, Bottom, Top }
    public enum ForceMode { Pull, Push }
    public enum CardinalDirection { Up, Right, Down, Left }
    public enum PerformanceMode { Auto, BatterySaver, Smooth }

    [Serializable]
    public struct GridCell : IEquatable<GridCell>
    {
        public int x;
        public int y;

        public GridCell(int x, int y) { this.x = x; this.y = y; }
        public bool Equals(GridCell other) => x == other.x && y == other.y;
        public override bool Equals(object obj) => obj is GridCell other && Equals(other);
        public override int GetHashCode() => (x * 397) ^ y;
        public static bool operator ==(GridCell a, GridCell b) => a.Equals(b);
        public static bool operator !=(GridCell a, GridCell b) => !a.Equals(b);
        public static GridCell operator +(GridCell a, GridCell b) => new(a.x + b.x, a.y + b.y);
        public override string ToString() => $"({x},{y})";
    }

    [Serializable]
    public struct RailSlot : IEquatable<RailSlot>
    {
        public MagnetSide side;
        public int index;

        public RailSlot(MagnetSide side, int index) { this.side = side; this.index = index; }
        public bool Equals(RailSlot other) => side == other.side && index == other.index;
        public override bool Equals(object obj) => obj is RailSlot other && Equals(other);
        public override int GetHashCode() => ((int)side * 397) ^ index;
        public static bool operator ==(RailSlot a, RailSlot b) => a.Equals(b);
        public static bool operator !=(RailSlot a, RailSlot b) => !a.Equals(b);
        public override string ToString() => $"{side}:{index}";
    }

    [Serializable]
    public struct GateDefinition
    {
        public GridCell cell;
        [Tooltip("The only direction in which the ball may enter this cell.")]
        public CardinalDirection allowedEntryDirection;
    }

    [Serializable]
    public struct GameState : IEquatable<GameState>
    {
        public GridCell ball;
        public RailSlot magnet;
        public ForceMode mode;

        public bool railSwitchUnlocked;
        public int railSideChanges;

        public GameState(GridCell ball, RailSlot magnet, ForceMode mode)
        {
            this.ball = ball;
            this.magnet = magnet;
            this.mode = mode;
            railSwitchUnlocked = false;
            railSideChanges = 0;
        }

        public bool Equals(GameState other) => ball == other.ball && magnet == other.magnet && mode == other.mode &&
            railSwitchUnlocked == other.railSwitchUnlocked && railSideChanges == other.railSideChanges;
        public override bool Equals(object obj) => obj is GameState other && Equals(other);
        public override int GetHashCode() => ((((ball.GetHashCode() * 397) ^ magnet.GetHashCode()) * 397 ^ (int)mode)
            * 397 ^ railSideChanges) * 397 ^ (railSwitchUnlocked ? 1 : 0);
    }

    public readonly struct MoveResult
    {
        public readonly GameState state;
        public readonly bool moved;
        public readonly bool blocked;
        public readonly bool won;

        public MoveResult(GameState state, bool moved, bool blocked, bool won)
        {
            this.state = state;
            this.moved = moved;
            this.blocked = blocked;
            this.won = won;
        }
    }
}
