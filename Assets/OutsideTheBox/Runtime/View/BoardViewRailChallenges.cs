using System;
using UnityEngine;

namespace OutsideTheBox
{
    public partial class BoardViewImplementation
    {
        private GameState railState;
        private RailChallengeSnapshot railRun;
        private RailChallengeMarker railMarker, switchMarker, chargeMarker;
        private SpriteRenderer railGoalGlow;
        private float nextRailNotice;
        public event Action<string> RailChallengeNotice;
        public RailSlot CurrentRailSlot => railState.magnet;
        public RailChallengeKind ActiveRailChallenge => level?.railChallenge?.kind ?? RailChallengeKind.None;
        public bool IsRailMagnetPowered =>
            (ActiveRailChallenge != RailChallengeKind.Cooldown || railRun.rechargeRemaining <= 0f) &&
            (ActiveRailChallenge != RailChallengeKind.TimedLock || CurrentRailSlot.side != level.railChallenge.side ||
                RailChallengeRules.IsOpen(level.railChallenge, railRun.clock));

        public RailChallengeSnapshot CaptureRailChallenge() => railRun;
        public void RestoreRailChallenge(RailChallengeSnapshot snapshot)
        {
            if (!snapshot.valid || ActiveRailChallenge == RailChallengeKind.None) return;
            railRun = snapshot;
            railRun.clock = Mathf.Max(0f, railRun.clock);
            railRun.sideChanges = Mathf.Clamp(railRun.sideChanges, 0, level.railChallenge.switchBudget);
            railRun.heat = Mathf.Clamp(railRun.heat, 0f, level.railChallenge.burstSeconds);
            railRun.rechargeRemaining = Mathf.Clamp(railRun.rechargeRemaining, 0f, level.railChallenge.rechargeSeconds);
            railState.railSwitchUnlocked = railRun.unlocked;
            railState.railSideChanges = railRun.sideChanges;
            RefreshRailVisuals();
        }

        public GameState RailSolverState(GridCell ball, ForceMode mode)
        { var result = railState; result.ball = ball; result.mode = mode; return result; }

        private void InitializeRailChallenge(GameState initial)
        {
            railMarker = switchMarker = chargeMarker = null;
            railGoalGlow = goalGlowTransform != null ? goalGlowTransform.GetComponent<SpriteRenderer>() : null;
            railState = initial;
            railRun = new RailChallengeSnapshot { valid = true };
            nextRailNotice = 0f;
            if (ActiveRailChallenge == RailChallengeKind.None) return;
            var rule = level.railChallenge;
            if (rule.kind is RailChallengeKind.LockedSide or RailChallengeKind.TimedLock or RailChallengeKind.UnlockSwitch)
                railMarker = MakeRailSegment(rule.side, 0, SideSlots(rule.side) - 1);
            if (rule.kind == RailChallengeKind.LimitedRail)
            {
                if (rule.minimumIndex > 0) MakeRailSegment(rule.side, 0, rule.minimumIndex - 1);
                if (rule.maximumIndex < SideSlots(rule.side) - 1)
                    MakeRailSegment(rule.side, rule.maximumIndex + 1, SideSlots(rule.side) - 1);
            }
            if (rule.kind == RailChallengeKind.UnlockSwitch)
            {
                switchMarker = SpawnRailMarker("Rail Unlock Switch");
                switchMarker.transform.localPosition = CellLocal(rule.switchCell) + Vector3.back * .14f;
                switchMarker.Configure(Vector2.one * (CellSize * .55f), true);
                switchMarker.SetState(false);
                switchMarker.SetMechanicIcon(AddMechanicIcon(switchMarker.transform, MechanicIconId.Switch, .4f));
            }
            if (rule.kind == RailChallengeKind.Cooldown)
            {
                chargeMarker = SpawnRailMarker("Magnet Charge");
                chargeMarker.Configure(new Vector2(CellSize * .8f, CellSize * .14f), false, false);
            }
            RefreshRailVisuals();
        }

        private RailChallengeMarker SpawnRailMarker(string markerName)
        {
            var instance = prefabs.Spawn(prefabs.railChallengeMarkerPrefab, markerName, staticContainer);
            spawned.Add(instance);
            return instance.GetComponent<RailChallengeMarker>();
        }

        private int SideSlots(MagnetSide side) => side is MagnetSide.Left or MagnetSide.Right ? level.height : level.width;
        private RailChallengeMarker MakeRailSegment(MagnetSide side, int first, int last)
        {
            var marker = SpawnRailMarker("Restricted " + side + " Rail");
            marker.transform.localPosition = (RailLocal(new RailSlot(side, first)) + RailLocal(new RailSlot(side, last))) * .5f
                + Vector3.back * .1f;
            marker.Configure(new Vector2((last - first + .7f) * CellSize, .14f * CellSize), false);
            if (side is MagnetSide.Left or MagnetSide.Right) marker.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            if ((side is MagnetSide.Left or MagnetSide.Right) && marker.lockIcon != null)
                marker.lockIcon.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            marker.SetState(false);
            marker.SetMechanicIcon(AddMechanicIcon(marker.transform, MechanicIconLibrary.ForRail(level.railChallenge.kind), .32f));
            return marker;
        }

        private bool AcceptRailTarget(Vector3 position, MagnetSide side, bool restore = false)
        {
            var slot = ProjectToRail(transform.TransformPoint(position));
            slot.side = side;
            var current = railState;
            if (restore) current.magnet = slot; // restoring is not a paid side change
            if (!RailChallengeRules.TrySelect(level, current, slot, out var next, railRun.clock, !restore))
            {
                if (restore)
                {
                    slot = RailChallengeRules.StartingSlot(level);
                    next = current; next.magnet = slot;
                    position = RailLocal(slot) + Vector3.back * .3f;
                    side = slot.side;
                }
                else
                {
                    if (Time.unscaledTime >= nextRailNotice)
                    {
                        nextRailNotice = Time.unscaledTime + 1.5f;
                        RailChallengeNotice?.Invoke(ActiveRailChallenge == RailChallengeKind.SwitchLimit
                            ? "NO SIDE CHANGES LEFT - STAY ON THIS SIDE OR RESTART" : "THAT RAIL IS BLOCKED - USE AN OPEN SIDE");
                    }
                    return false;
                }
            }
            railState = next;
            railRun.sideChanges = next.railSideChanges;
            SetMagnetTarget(position, side);
            return true;
        }

        private void UpdateRailChallenge(float deltaTime)
        {
            if (ActiveRailChallenge == RailChallengeKind.None) return;
            railRun.clock += deltaTime;
            var rule = level.railChallenge;
            if (rule.kind == RailChallengeKind.Cooldown)
            {
                if (railRun.rechargeRemaining > 0f)
                {
                    railRun.rechargeRemaining = Mathf.Max(0f, railRun.rechargeRemaining - deltaTime);
                    if (railRun.rechargeRemaining <= 0f) railRun.heat = 0f;
                }
                else if (magnetForceActive)
                {
                    railRun.heat = Mathf.Min(rule.burstSeconds, railRun.heat + deltaTime);
                    if (railRun.heat >= rule.burstSeconds) railRun.rechargeRemaining = rule.rechargeSeconds;
                }
                else railRun.heat = Mathf.Max(0f, railRun.heat - deltaTime);
            }
            RefreshRailVisuals();
        }

        private void PostMoveRailChallenge(Vector2 previous)
        {
            if (ActiveRailChallenge != RailChallengeKind.UnlockSwitch || railRun.unlocked) return;
            var center = (Vector2)CellLocal(level.railChallenge.switchCell);
            var movement = ballPosition - previous;
            var t = movement.sqrMagnitude < .000001f ? 0f : Mathf.Clamp01(Vector2.Dot(center - previous, movement) / movement.sqrMagnitude);
            var radius = config.ballRadius + CellSize * .22f;
            if ((previous + movement * t - center).sqrMagnitude > radius * radius) return;
            railRun.unlocked = railState.railSwitchUnlocked = true;
            RefreshRailVisuals();
            RailChallengeNotice?.Invoke("SWITCH ACTIVATED - RAIL AND GOAL UNLOCKED");
        }

        private void RefreshRailVisuals()
        {
            if (railMarker != null) railMarker.SetState(ActiveRailChallenge == RailChallengeKind.UnlockSwitch
                ? railRun.unlocked : ActiveRailChallenge == RailChallengeKind.TimedLock && RailChallengeRules.IsOpen(level.railChallenge, railRun.clock));
            if (switchMarker != null) switchMarker.SetState(railRun.unlocked);
            if (railGoalGlow != null && ActiveRailChallenge == RailChallengeKind.UnlockSwitch)
            {
                var color = railRun.unlocked ? config.goal : new Color(.95f, .2f, .18f, .8f);
                if (railGoalGlow.color != color) railGoalGlow.color = color;
            }
            if (chargeMarker != null)
            {
                chargeMarker.transform.localPosition = magnetTransform.localPosition + Vector3.down * CellSize * .48f;
                chargeMarker.SetProgress(railRun.rechargeRemaining > 0f ? 1f - railRun.rechargeRemaining / level.railChallenge.rechargeSeconds
                    : 1f - railRun.heat / level.railChallenge.burstSeconds);
            }
        }

        public string RailChallengeStatus
        {
            get
            {
                if (ActiveRailChallenge == RailChallengeKind.None) return null;
                var rule = level.railChallenge;
                var side = rule.side.ToString().ToUpperInvariant();
                switch (rule.kind)
                {
                    case RailChallengeKind.LockedSide: return side + " LOCKED | USE THE OTHER RAILS";
                    case RailChallengeKind.LimitedRail: return side + ": USE THE UNSTRIPED SECTION";
                    case RailChallengeKind.TimedLock:
                        var phase = Mathf.Repeat(railRun.clock, rule.openSeconds + rule.closedSeconds);
                        var open = phase < rule.openSeconds;
                        return side + (open ? " OPEN | CLOSES IN " : " LOCKED | OPENS IN ") +
                            Mathf.CeilToInt((open ? rule.openSeconds : rule.openSeconds + rule.closedSeconds) - phase) + "s";
                    case RailChallengeKind.UnlockSwitch: return railRun.unlocked ? side + " RAIL UNLOCKED" : "HIT SWITCH TO OPEN " + side + " RAIL + GOAL";
                    case RailChallengeKind.SwitchLimit: return "SIDE CHANGES: " + Mathf.Max(0, rule.switchBudget - railRun.sideChanges) + " | SAME SIDE IS FREE";
                    default: return railRun.rechargeRemaining > 0f ? "RECHARGE " + railRun.rechargeRemaining.ToString("0.0") + "s | LET THE BALL COAST"
                        : "CHARGE " + Mathf.RoundToInt((1f - railRun.heat / rule.burstSeconds) * 100f) + "% | SHORT PULLS";
                }
            }
        }
    }
}
