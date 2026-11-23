using System;
using UnityEngine;

namespace OutsideTheBox
{
    public enum RailChallengeKind { None, LockedSide, LimitedRail, TimedLock, UnlockSwitch, SwitchLimit, Cooldown }

    [Serializable]
    public sealed class RailChallengeDefinition
    {
        public RailChallengeKind kind;
        public MagnetSide side;
        public int minimumIndex;
        public int maximumIndex;
        public GridCell switchCell;
        [Min(1)] public int switchBudget = 6;
        [Min(0.5f)] public float openSeconds = 4f;
        [Min(0.5f)] public float closedSeconds = 2f;
        [Min(0.5f)] public float burstSeconds = 3f;
        [Min(0.5f)] public float rechargeSeconds = 1.2f;
    }

    [Serializable]
    public struct RailChallengeSnapshot
    {
        public bool valid;
        public bool unlocked;
        public int sideChanges;
        public float clock;
        public float heat;
        public float rechargeRemaining;
    }

    // Pure rules shared by dragging, hints and generation. Timing does not remove
    // routes from the grid solver: the player may wait for an opening/recharge.
    public static class RailChallengeRules
    {
        public static bool TrySelect(LevelDefinition level, GameState current, RailSlot slot, out GameState next,
            float clock = 0f, bool includeTiming = false)
        {
            next = current;
            var count = slot.side is MagnetSide.Left or MagnetSide.Right ? level.height : level.width;
            if ((int)slot.side < 0 || (int)slot.side > 3 || slot.index < 0 || slot.index >= count) return false;
            var rule = level.railChallenge;
            if (rule != null)
            {
                if (slot.side == rule.side)
                {
                    if (rule.kind == RailChallengeKind.LockedSide) return false;
                    if (rule.kind == RailChallengeKind.UnlockSwitch && !current.railSwitchUnlocked) return false;
                    if (rule.kind == RailChallengeKind.LimitedRail &&
                        (slot.index < rule.minimumIndex || slot.index > rule.maximumIndex)) return false;
                    if (rule.kind == RailChallengeKind.TimedLock && includeTiming && !IsOpen(rule, clock)) return false;
                }
                if (rule.kind == RailChallengeKind.SwitchLimit && slot.side != current.magnet.side)
                {
                    if (current.railSideChanges >= rule.switchBudget) return false;
                    next.railSideChanges++;
                }
            }
            next.magnet = slot;
            return true;
        }

        public static bool IsOpen(RailChallengeDefinition rule, float clock) =>
            Mathf.Repeat(Mathf.Max(0f, clock), Mathf.Max(0.5f, rule.openSeconds) + Mathf.Max(0.5f, rule.closedSeconds))
            < Mathf.Max(0.5f, rule.openSeconds);

        public static bool GoalAllowsEntry(LevelDefinition level, GameState state) =>
            level.railChallenge?.kind != RailChallengeKind.UnlockSwitch || state.railSwitchUnlocked;

        public static RailSlot StartingSlot(LevelDefinition level)
        {
            var slot = level.useCustomStartingMagnet ? level.startingMagnet : new RailSlot(MagnetSide.Left, level.start.y);
            var state = new GameState(level.start, slot, ForceMode.Pull);
            if (TrySelect(level, state, slot, out _)) return slot;
            foreach (var candidate in GameRules.AllSlots(level))
                if (TrySelect(level, state, candidate, out _)) return candidate;
            return slot;
        }
    }
}
