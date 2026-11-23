using System.Collections.Generic;
using UnityEngine;

namespace OutsideTheBox
{
    [CreateAssetMenu(menuName = "Outside The Box/Level", fileName = "Level_001")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string levelId = "level_001";
        [Min(1)] public int levelNumber = 1;
        [Min(0)] public int generatorSeed;
        [HideInInspector, Min(0)] public int generatedContentVersion;
        [HideInInspector] public int generatedDesignRevision;
        [HideInInspector] public LevelMechanicKind primaryMechanic;
        [HideInInspector] public LevelMechanicKind supportingMechanic;

        [Header("Board")]
        [Range(5, 10)] public int width = 5;
        [Range(5, 10)] public int height = 5;
        public GridCell start = new(0, 0);
        public GridCell exit = new(4, 4);
        public bool useCustomStartingMagnet;
        public RailSlot startingMagnet = new(MagnetSide.Left, 0);
        public List<GridCell> walls = new();
        public List<WallSegmentDefinition> wallSegments = new();
        public List<GateDefinition> gates = new();
        public List<MovingWallDefinition> movingWalls = new();
        public List<FieldZoneDefinition> fieldZones = new();
        public List<MagneticImprintDefinition> magneticImprints = new();
        public List<FieldLensDefinition> fieldLenses = new();
        public List<PolarityLockDefinition> polarityLocks = new();
        [Header("Magnet rail challenge")]
        public RailChallengeDefinition railChallenge = new();
        public bool echoMagnetEnabled;
        public bool ferrofluidMemoryEnabled;
        public bool orbitGoalEnabled;
        [Min(0.25f)] public float orbitGoalTurns = 1f;

        [Header("Rules and rating")]
        [Tooltip("Keeps designated QA levels on the Chapter 1 physics profile.")]
        public bool useNormalPhysics;
        public bool pushUnlocked;
        [Min(1)] public int targetMoves = 6;
        [Min(1f)] public float targetEnergy = 45f;
        [HideInInspector] public bool isEndless;
        [HideInInspector, Min(0)] public int endlessIndex;
        [HideInInspector] public bool isDailyChallenge;
        [HideInInspector] public string dailyKey;
        [TextArea] public string tutorialPrompt;
        public List<SolutionStep> verifiedSolution = new();

        public bool IsWall(GridCell cell) => walls != null && walls.Contains(cell);

        public bool TryGetGate(GridCell cell, out GateDefinition gate)
        {
            if (gates != null)
            {
                for (var i = 0; i < gates.Count; i++)
                {
                    if (gates[i].cell == cell) { gate = gates[i]; return true; }
                }
            }
            gate = default;
            return false;
        }

        public bool IsInside(GridCell cell) => cell.x >= 0 && cell.y >= 0 && cell.x < width && cell.y < height;
    }

    [System.Serializable]
    public struct WallSegmentDefinition
    {
        public Vector2 center;
        [Min(0.1f)] public float length;
        public bool horizontal;
    }

    [System.Serializable]
    public struct MovingWallDefinition
    {
        public Vector2 center;
        [Min(0.2f)] public float length;
        public bool horizontal;
        public Vector2 travel;
        [Min(0.1f)] public float speed;
        [Range(0f, 6.2832f)] public float phase;
    }

    [System.Serializable]
    public struct FieldZoneDefinition
    {
        public Vector2 center;
        [Min(0.2f)] public float radius;
        [Range(0.15f, 1.8f)] public float strengthMultiplier;
    }

    [System.Serializable]
    public struct MagneticImprintDefinition
    {
        public Vector2 center;
        public ForceMode polarity;
    }

    [System.Serializable]
    public struct FieldLensDefinition
    {
        public Vector2 center;
        [Min(0.2f)] public float radius;
        [Range(-1, 1)] public int quarterTurns;
    }

    [System.Serializable]
    public struct PolarityLockDefinition
    {
        public Vector2 center;
        [Min(0.2f)] public float length;
        public bool horizontal;
        [Range(1, 8)] public int patternLength;
        [Min(0)] public int patternBits;

        public ForceMode ModeAt(int index) => ((patternBits >> Mathf.Max(0, index)) & 1) == 0
            ? ForceMode.Pull
            : ForceMode.Push;
    }

    [System.Serializable]
    public struct SolutionStep
    {
        public RailSlot magnet;
        public ForceMode mode;
    }
}
