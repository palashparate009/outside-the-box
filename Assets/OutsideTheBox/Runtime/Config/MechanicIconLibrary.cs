using UnityEngine;

namespace OutsideTheBox
{
    public enum MechanicIconId
    {
        Magnet, Goal, Energy, Walls, Ice, Polarity, Gates, MovingWall, FieldZone,
        Echo, Memory, Imprint, Lens, PolarityLock, Orbit, LockedRail, LimitedRail,
        TimedRail, Switch, Budget, Cooldown, Master, Endless, Pull, Push
    }

    [CreateAssetMenu(menuName = "Outside The Box/Mechanic Icons")]
    public sealed class MechanicIconLibrary : ScriptableObject
    {
        public Sprite[] icons;
        private static MechanicIconLibrary current;
        public static MechanicIconLibrary Current => current != null ? current :
            current = Resources.Load<MechanicIconLibrary>("OutsideTheBox/Configs/MechanicIcons");
        public Sprite Get(MechanicIconId id) => icons != null && (int)id >= 0 && (int)id < icons.Length ? icons[(int)id] : null;

        public static MechanicIconId ForRail(RailChallengeKind kind) => kind switch
        {
            RailChallengeKind.LockedSide => MechanicIconId.LockedRail,
            RailChallengeKind.LimitedRail => MechanicIconId.LimitedRail,
            RailChallengeKind.TimedLock => MechanicIconId.TimedRail,
            RailChallengeKind.UnlockSwitch => MechanicIconId.Switch,
            RailChallengeKind.SwitchLimit => MechanicIconId.Budget,
            _ => MechanicIconId.Cooldown
        };
        public static MechanicIconId ForMechanic(LevelMechanicKind kind) => kind switch
        {
            LevelMechanicKind.Gates => MechanicIconId.Gates,
            LevelMechanicKind.MovingWalls => MechanicIconId.MovingWall,
            LevelMechanicKind.FieldZones => MechanicIconId.FieldZone,
            LevelMechanicKind.Echo => MechanicIconId.Echo,
            LevelMechanicKind.Memory => MechanicIconId.Memory,
            LevelMechanicKind.Imprints => MechanicIconId.Imprint,
            LevelMechanicKind.Lenses => MechanicIconId.Lens,
            LevelMechanicKind.PolarityLocks => MechanicIconId.PolarityLock,
            LevelMechanicKind.Orbit => MechanicIconId.Orbit,
            _ => MechanicIconId.Magnet
        };
        public static MechanicIconId ForTutorial(int id) => id switch
        {
            102 => MechanicIconId.Goal, 103 => MechanicIconId.Energy, 111 => MechanicIconId.Walls,
            201 => MechanicIconId.Ice, 301 => MechanicIconId.Polarity, 401 => MechanicIconId.Gates,
            402 => MechanicIconId.MovingWall, 501 => MechanicIconId.FieldZone, 601 => MechanicIconId.Master,
            701 => MechanicIconId.Endless, 801 => MechanicIconId.Echo, 802 => MechanicIconId.Memory,
            803 => MechanicIconId.Imprint, 804 => MechanicIconId.Lens, 805 => MechanicIconId.PolarityLock,
            806 => MechanicIconId.Orbit, 811 => MechanicIconId.LockedRail, 812 => MechanicIconId.LimitedRail,
            813 => MechanicIconId.TimedRail, 814 => MechanicIconId.Switch, 815 => MechanicIconId.Budget,
            816 => MechanicIconId.Cooldown, _ => MechanicIconId.Magnet
        };
    }
}
