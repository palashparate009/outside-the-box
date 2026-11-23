using System;
using UnityEngine;

namespace OutsideTheBox
{
    [CreateAssetMenu(menuName = "Outside The Box/Game Config", fileName = "GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        [Min(1)] public int contentVersion = 7;

        [Header("Testing")]
        [Tooltip("Opens every campaign level and the Endless chapter without changing player save progress. Disable before a release build.")]
        public bool unlockAllChaptersForTesting;
        [Tooltip("TEST PREVIEW: pipe travel after a win and a drop-in on Next Level. Turn off to restore normal transitions. Skipped when Reduce Motion is enabled.")]
        public bool previewPipeLevelTransitions;
        [Tooltip("TEST PREVIEW ONLY: choose the existing 2D pipe or the isolated 3D cinematic. Gameplay stays 2D.")]
        public PipeTransitionPreviewStyle pipeTransitionPreviewStyle = PipeTransitionPreviewStyle.TwoDimensional;

        [Header("Movement")]
        [Min(1f)] public float magnetFollowSpeed = 22f;
        [Min(0.1f)] public float magneticAcceleration = 13f;
        [Min(0.1f)] public float maximumBallSpeed = 5.5f;
        [Min(0f)] public float ballDrag = 1.35f;
        [Range(0f, 1f)] public float wallBounce = 0.22f;
        [Range(0.15f, 0.48f)] public float ballRadius = 0.31f;
        [Range(0.12f, 0.4f)] public float ballCollisionRadius = 0.29f;
        [Range(0.2f, 0.75f)] public float exitRadius = 0.42f;
        [Range(0.001f, 0.2f)] public float ballStopSpeed = 0.035f;
        [Min(0f)] public float winDelaySeconds = 0.12f;

        [Header("Performance")]
        [Range(30, 120)] public int gameplayFrameRate = 60;
        [Range(24, 60)] public int batterySaverFrameRate = 30;
        [Range(30, 120)] public int smoothFrameRate = 60;
        [Range(1024, 8192)] public int automaticLowMemoryThresholdMb = 3072;
        public bool runInBackgroundInEditor = true;
        public bool runInBackgroundOnDevice;

        [Header("Layout")]
        [Range(0.5f, 2f)] public float cellSize = 1f;
        [Range(0.2f, 1f)] public float railGap = 0.55f;
        [Range(0.05f, 0.3f)] public float cellGap = 0.09f;
        [Range(0.25f, 0.75f)] public float wallThickness = 0.42f;
        [Range(0.12f, 0.65f)] public float wallCollisionThickness = 0.42f;
        [Range(0f, 0.2f)] public float wallCollisionEndInset = 0f;
        [Range(0.75f, 1.75f)] public float standaloneWallLength = 1f;
        [Range(0.35f, 0.95f)] public float gateSize = 0.68f;
        [Range(0f, 0.2f)] public float wallShadowOffset = 0.04f;
        [Range(0f, 1f)] public float wallShadowAlpha = 0.18f;
        [Range(0f, 1f)] public float wallBevelAlpha = 0f;
        [Range(0f, 0.75f)] public float glassFramePadding = 0.08f;
        [Range(0.05f, 0.3f)] public float glassFrameBorderFraction = 0.22f;
        [Range(0.08f, 0.35f)] public float outerGlassThickness = 0.2f;
        [Range(0f, 1f)] public float outerGlassAlpha = 0.72f;
        [Range(0f, 1f)] public float cameraSidePadding = 0.2f;
        [Range(1.5f, 4f)] public float cameraVerticalPadding = 2.8f;
        [Range(1f, 2.5f)] public float ballArtScale = 1f;
        [Range(1f, 2f)] public float goalArtScale = 1f;
        [Range(0.5f, 2.5f)] public float magnetVisualSize = 1.35f;
        [Range(0.3f, 2f)] public float magnetDragPickupRadius = 0.9f;

        [Header("Goal Animation")]
        [Range(0.15f, 1.5f)] public float pocketDropDuration = 0.42f;
        [Range(0f, 720f)] public float pocketDropSpinDegrees = 240f;
        [Range(0f, 0.3f)] public float pocketDropEndScale = 0.04f;
        [Range(0f, 0.5f)] public float pocketSuctionOvershoot = 0.12f;
        [Range(0f, 1f)] public float pocketUnderlayTime = 0.28f;
        [Range(0f, 0.95f)] public float pocketFadeStart = 0.55f;
        [Range(0f, 0.2f)] public float goalPulseAmount = 0.055f;
        [Range(0f, 8f)] public float goalPulseSpeed = 2.4f;
        [Range(0f, 1f)] public float goalGlowAlpha = 0.48f;
        [Range(1f, 3f)] public float goalGlowScale = 1.75f;
        [Range(0f, 0.15f)] public float goalGlowPulseAmount = 0.04f;

        [Header("Glass Illumination")]
        [Range(0f, 1f)] public float glassGlowAlpha = 0.38f;
        [Range(1f, 1.15f)] public float glassGlowScale = 1.04f;
        [Range(0f, 0.08f)] public float glassGlowPulseAmount = 0.012f;
        [Range(0f, 8f)] public float glassGlowPulseSpeed = 1.1f;

        [Header("Magnetic Field")]
        [Range(1, 7)] public int fieldLineCount = 4;
        [Range(4, 32)] public int fieldLineSegments = 16;
        [Range(0.005f, 0.12f)] public float fieldLineWidth = 0.026f;
        [Range(0f, 1.5f)] public float fieldCurveSpread = 0.48f;
        [Range(0f, 1f)] public float fieldLineAlpha = 0.62f;
        [Range(0f, 0.8f)] public float fieldZoneAlpha = 0.2f;
        public Color weakFieldZone = new(0.35f, 0.72f, 1f, 1f);
        public Color strongFieldZone = new(1f, 0.55f, 0.15f, 1f);
        public Color accessibleWeakFieldZone = new(0.1f, 0.75f, 1f, 1f);
        public Color accessibleStrongFieldZone = new(1f, 0.2f, 0.75f, 1f);

        [Header("Timing")]
        [Range(0.02f, 0.5f)] public float collisionFeedbackCooldown = 0.12f;
        [Range(2, 8)] public int collisionSolverPasses = 4;

        [Header("Energy Scoring")]
        [Range(0.03f, 0.5f)] public float energyUiRefreshInterval = 0.1f;
        [Range(0f, 20f)] public float collisionEnergyPenalty = 1.5f;
        [Range(1.05f, 3f)] public float twoStarEnergyMultiplier = 1.35f;

        [Header("Procedural Levels")]
        public bool proceduralLevelsEnabled = true;
        public bool focusedChapterDesignEnabled = true;
        [Min(1)] public int levelDesignRevision = 2;
        [Range(0, 12)] public int generatorBaseWallCells = 2;
        [Range(0.05f, 0.6f)] public float generatorWallCellsPerLevel = 0.3f;
        [Range(6, 60)] public int generatorMaximumWallCells = 36;
        [Range(40, 500)] public int generatorShapeAttempts = 180;
        [Range(3, 40)] public int generatorLayoutAttempts = 32;
        [Header("Procedural Route Challenge")]
        [Tooltip("Prefer layouts where walls block a direct ball-to-goal path.")]
        public bool generatorPreferBlockedDirectPath = true;
        [Tooltip("Extra grid steps to prefer above the unobstructed shortest route.")]
        [Range(0, 12)] public int generatorMinimumDetourSteps = 2;
        [Tooltip("Weight given to walls forcing a longer route when choosing a layout.")]
        [Range(0, 100)] public int generatorDetourWeight = 24;
        [Tooltip("Bonus for blocking the straight line between ball and pocket.")]
        [Range(0, 500)] public int generatorBlockedLineBonus = 100;

        [Header("Signature Mechanics - Content")]
        [Min(1)] public int echoMagnetUnlockLevel = 31;
        [Min(1)] public int ferrofluidMemoryUnlockLevel = 36;
        [Min(1)] public int magneticImprintUnlockLevel = 41;
        [Min(1)] public int fieldLensUnlockLevel = 46;
        [Min(1)] public int polarityLockUnlockLevel = 51;
        [Min(1)] public int orbitGoalUnlockLevel = 56;
        [Range(0, 4)] public int magneticImprintsPerLevel = 2;
        [Range(0, 4)] public int fieldLensesPerLevel = 2;
        [Range(0, 3)] public int polarityLocksPerLevel = 1;
        [Range(1, 5)] public int polarityLockPatternLength = 3;
        [Range(1, 5)] public int signatureMechanicSpacing = 2;
        public Color echoMagnetColor = new(0.15f, 0.95f, 1f, 0.72f);
        public Color magneticImprintPullColor = new(0.2f, 0.7f, 1f, 0.9f);
        public Color magneticImprintPushColor = new(1f, 0.3f, 0.25f, 0.9f);
        public Color fieldLensColor = new(0.75f, 0.35f, 1f, 0.82f);
        public Color ferrofluidTrailColor = new(0.12f, 0.12f, 0.2f, 0.58f);
        public Color polarityLockColor = new(1f, 0.72f, 0.15f, 0.95f);
        public Color orbitGoalColor = new(0.25f, 1f, 0.55f, 0.85f);

        [Header("Endless Vault")]
        public bool endlessEnabled = true;
        public int endlessSeedBase = 910247;
        [Range(0.02f, 1f)] public float endlessWallCellsPerLevel = 0.16f;
        [Range(8, 60)] public int endlessMaximumWallCells = 38;
        [Range(5, 25)] public int endlessGateInterval = 8;

        [Header("Daily Challenge")]
        public bool dailyChallengeEnabled = true;
        public int dailySeedBase = 481516;
        [Range(1, 100)] public int dailyMinimumDifficulty = 16;
        [Range(1, 100)] public int dailyDifficultyCycle = 35;

        [Header("Chapters")]
        public bool railChallengesEnabled = true;
        public TutorialStepDefinition[] railChallengeTutorials =
        {
            new() { id = 811, title = "Locked Side", body = "The {side} rail is closed. Red stripes and a lock mark the blocked side. Drag the magnet around the other three sides to guide the ball.", completionTooltip = "Plan your route using the open rails" },
            new() { id = 812, title = "Limited Rail", body = "Only the unstriped section of the {side} rail is available. The other three sides remain open. Use a different angle when a blocked section stops your drag.", completionTooltip = "Use the unstriped rail section" },
            new() { id = 813, title = "Timed Lock", body = "The {side} rail opens and closes. Green means open; red stripes mean locked. Watch the countdown. If it closes while your magnet is there, the field pauses until it opens, or you move to another side.", completionTooltip = "Watch the rail countdown and time your pulls" },
            new() { id = 814, title = "Unlock Switch", body = "Guide the ball over the colored switch inside the board. It turns green and permanently unlocks the {side} rail and goal for this attempt.", completionTooltip = "Hit the switch before heading for the goal" },
            new() { id = 815, title = "Side Change Limit", body = "This level gives you {budget} changes between magnet sides. Moving along the same side costs nothing. Plan your route before switching; Restart gives you a fresh budget.", completionTooltip = "The remaining side changes are shown below the board" },
            new() { id = 816, title = "Magnet Cooldown", body = "Long pulls drain the magnet charge. When empty, the magnet briefly recharges and the ball coasts. Release early to cool it down, and use short pulls to stay in control.", completionTooltip = "Watch the charge bar and let the ball coast while recharging" }
        };
        [Min(1)] public int levelsPerChapter = 10;
        public TutorialStepDefinition[] firstTimeTutorials =
        {
            new() { id = 101, title = "Move the magnet", body = "Touch and drag the magnet around the outside edge of the glass. The magnet stays outside; the ball stays inside.", buttonLabel = "NEXT" },
            new() { id = 102, title = "Guide the ball", body = "Hold the magnet to attract the steel ball. Release to let it coast, then guide it into the glowing green pocket.", buttonLabel = "NEXT" },
            new() { id = 103, title = "Use energy wisely", body = "Your magnetic field uses energy while you hold and move the magnet. Finish with less energy to earn more stars.", buttonLabel = "LET'S PLAY", completionTooltip = "Drag the magnet to begin" }
        };
        public TutorialStepDefinition[] secondLevelTutorials =
        {
            new() { id = 111, title = "Walls block force", body = "A wall removes only the ball's motion into it. Slide along the wall, then move the magnet to another side to route around the corner.", buttonLabel = "SHOW ME", completionTooltip = "Use another side of the glass when the direct pull is blocked" },
            new() { id = 112, title = "Release and redirect", body = "You do not need to hold the magnet forever. Release to let the ball coast, then grab the magnet again from a better side.", buttonLabel = "LET'S TRY", completionTooltip = "Short controlled pulls are easier than one long pull" }
        };
        public TutorialStepDefinition echoMagnetTutorial = new() { id = 801, title = "Echo Magnet", body = "Tap ECHO, then drag the magnet. Your recorded path repeats as a cyan ghost while you control the real magnet. Both fields combine.", buttonLabel = "RECORD AN ECHO", completionTooltip = "Record a path, then steer beside your ghost" };
        public TutorialStepDefinition ferrofluidTutorial = new() { id = 802, title = "Ferrofluid Memory", body = "The ball leaves a conductive trail. When it crosses its trail again, the remembered direction helps guide its momentum.", buttonLabel = "FOLLOW THE TRAIL" };
        public TutorialStepDefinition magneticImprintTutorial = new() { id = 803, title = "Magnetic Imprints", body = "Striped metal walls wake when the ball touches them. Blue imprints pull and red imprints push for a few seconds.", buttonLabel = "WAKE THE WALLS" };
        public TutorialStepDefinition fieldLensTutorial = new() { id = 804, title = "Field Lenses", body = "Purple lenses rotate magnetic force. Watch the arrow inside each lens before committing to a pull.", buttonLabel = "BEND THE FIELD" };
        public TutorialStepDefinition polarityLockTutorial = new() { id = 805, title = "Polarity Locks", body = "Follow the PULL and PUSH symbols on the lock using the force button. Completed symbols turn green. Finish the sequence to open the passage.", buttonLabel = "ENTER THE CODE" };
        public TutorialStepDefinition orbitGoalTutorial = new() { id = 806, title = "Orbit Goal", body = "The pocket opens only after the ball circles its green ring. Build sideways momentum, complete the orbit, then drop into the centre.", buttonLabel = "ENTER ORBIT" };
        public ChapterSettings[] chapters =
        {
            new() { title = "Magnetic Basics", challenge = "Learn smooth, efficient pulls", boardSize = 5, wallDensity = 0.12f, wallDensityRamp = 0.06f, maxWallRunLength = 2, minimumWallGroups = 2, minimumSolutionSteps = 5, dragMultiplier = 1f, magneticStrengthMultiplier = 1f, energyPerSecond = 8f, energyPerWorldUnit = 2f, targetEnergyPerSolutionStep = 9f, backgroundColorA = new Color(0.13f, 0.55f, 0.78f), backgroundColorB = new Color(0.48f, 0.82f, 0.92f), backgroundAccent = new Color(0.76f, 0.96f, 1f), backgroundSpeed = 0.55f, backgroundPattern = 0, backgroundIntensity = 0.55f },
            new() { title = "Frozen Glass", challenge = "The ball keeps sliding", boardSize = 6, wallDensity = 0.17f, wallDensityRamp = 0.06f, maxWallRunLength = 3, minimumWallGroups = 3, minimumSolutionSteps = 7, dragMultiplier = 0.32f, magneticStrengthMultiplier = 1f, energyPerSecond = 8.5f, energyPerWorldUnit = 2.2f, targetEnergyPerSolutionStep = 10f, backgroundColorA = new Color(0.09f, 0.32f, 0.66f), backgroundColorB = new Color(0.4f, 0.82f, 0.94f), backgroundAccent = new Color(0.8f, 0.98f, 1f), backgroundSpeed = 0.45f, backgroundPattern = 1, backgroundIntensity = 0.55f, tutorialSteps = new[] { new TutorialStepDefinition { id = 201, title = "Slippery floor", body = "The frozen glass has very little friction. Release the magnet early and use short pulls so the ball does not overshoot.", buttonLabel = "GOT IT", completionTooltip = "Short pulls give you more control" } } },
            new() { title = "Polarity Lab", challenge = "Master pull and push", boardSize = 7, wallDensity = 0.21f, wallDensityRamp = 0.07f, maxWallRunLength = 3, minimumWallGroups = 4, minimumSolutionSteps = 9, dragMultiplier = 1f, magneticStrengthMultiplier = 1f, energyPerSecond = 9f, energyPerWorldUnit = 2.5f, targetEnergyPerSolutionStep = 11f, pushUnlocked = true, backgroundColorA = new Color(0.22f, 0.12f, 0.55f), backgroundColorB = new Color(0.82f, 0.2f, 0.45f), backgroundAccent = new Color(0.32f, 0.82f, 1f), backgroundSpeed = 0.65f, backgroundPattern = 2, backgroundIntensity = 0.55f, tutorialSteps = new[] { new TutorialStepDefinition { id = 301, title = "Switch polarity", body = "Use the PULL / PUSH button to change the magnetic force. Pull brings the ball closer; push drives it away.", buttonLabel = "TRY POLARITY", completionTooltip = "Switch polarity whenever the route needs it" } } },
            new() { title = "Gate Factory", challenge = "Read gates and time moving walls", boardSize = 8, wallDensity = 0.25f, wallDensityRamp = 0.07f, maxWallRunLength = 3, minimumWallGroups = 6, minimumSolutionSteps = 11, gateCount = 2, movingWallCount = 2, dragMultiplier = 0.9f, magneticStrengthMultiplier = 1f, energyPerSecond = 9.5f, energyPerWorldUnit = 2.7f, targetEnergyPerSolutionStep = 12f, pushUnlocked = true, oneWayGates = true, movingWalls = true, backgroundColorA = new Color(0.08f, 0.2f, 0.36f), backgroundColorB = new Color(0.15f, 0.62f, 0.62f), backgroundAccent = new Color(1f, 0.64f, 0.12f), backgroundSpeed = 0.75f, backgroundPattern = 3, backgroundIntensity = 0.58f, tutorialSteps = new[] { new TutorialStepDefinition { id = 401, title = "One-way gates", body = "The arrow shows the only direction the ball can enter a gate. Approach from the arrow side.", buttonLabel = "NEXT" }, new TutorialStepDefinition { id = 402, title = "Moving walls", body = "Moving walls change the safe route. Watch their rhythm and move when a gap opens.", buttonLabel = "GOT IT", completionTooltip = "Read the arrows and time the moving walls" } } },
            new() { title = "Field Distortion", challenge = "Colored zones alter magnetic strength", boardSize = 9, wallDensity = 0.29f, wallDensityRamp = 0.07f, maxWallRunLength = 3, minimumWallGroups = 8, minimumSolutionSteps = 13, fieldZoneCount = 3, dragMultiplier = 0.8f, magneticStrengthMultiplier = 0.9f, interferenceStrength = 0.12f, energyPerSecond = 10f, energyPerWorldUnit = 3f, targetEnergyPerSolutionStep = 13f, pushUnlocked = true, fieldZones = true, backgroundColorA = new Color(0.08f, 0.36f, 0.3f), backgroundColorB = new Color(0.25f, 0.7f, 0.4f), backgroundAccent = new Color(0.95f, 0.82f, 0.22f), backgroundSpeed = 0.6f, backgroundPattern = 4, backgroundIntensity = 0.58f, tutorialSteps = new[] { new TutorialStepDefinition { id = 501, title = "Field zones", body = "Colored zones change magnetic strength. Blue weakens the field; orange strengthens it. Plan for the change before entering.", buttonLabel = "GOT IT", completionTooltip = "Blue is weaker, orange is stronger" } } },
            new() { title = "Master Chambers", challenge = "Polarity locks with one focused variation", boardSize = 10, wallDensity = 0.31f, wallDensityRamp = 0.07f, maxWallRunLength = 4, minimumWallGroups = 10, minimumSolutionSteps = 15, gateCount = 4, movingWallCount = 3, fieldZoneCount = 4, dragMultiplier = 0.58f, magneticStrengthMultiplier = 0.92f, interferenceStrength = 0.1f, energyPerSecond = 11f, energyPerWorldUnit = 3.2f, targetEnergyPerSolutionStep = 14f, pushUnlocked = true, oneWayGates = true, movingWalls = true, fieldZones = true, backgroundColorA = new Color(0.12f, 0.08f, 0.3f), backgroundColorB = new Color(0.05f, 0.5f, 0.6f), backgroundAccent = new Color(1f, 0.32f, 0.2f), backgroundSpeed = 0.8f, backgroundPattern = 5, backgroundIntensity = 0.62f, tutorialSteps = new[] { new TutorialStepDefinition { id = 601, title = "Master chamber", body = "Polarity locks are the chapter specialty. Each chamber adds at most one focused variation. Read its icons, enter the lock sequence, then guide the ball.", buttonLabel = "I'M READY", completionTooltip = "Read the active icons. Only the shown mechanics are active." } } }
        };

        public ChapterSettings endlessChapter = new()
        {
            title = "Endless Vault",
            challenge = "Survive the toughest generated chambers",
            boardSize = 10,
            wallDensity = 0.33f,
            wallDensityRamp = 0.08f,
            maxWallRunLength = 4,
            minimumWallGroups = 11,
            minimumSolutionSteps = 16,
            gateCount = 3,
            movingWallCount = 3,
            fieldZoneCount = 4,
            dragMultiplier = 0.52f,
            magneticStrengthMultiplier = 0.88f,
            interferenceStrength = 0.25f,
            energyPerSecond = 12f,
            energyPerWorldUnit = 3.5f,
            targetEnergyPerSolutionStep = 15f,
            pushUnlocked = true,
            oneWayGates = true,
            movingWalls = true,
            fieldZones = true
            ,backgroundColorA = new Color(0.015f, 0.025f, 0.09f)
            ,backgroundColorB = new Color(0.28f, 0.04f, 0.34f)
            ,backgroundAccent = new Color(0.2f, 0.9f, 0.9f)
            ,backgroundSpeed = 1f
            ,backgroundPattern = 6
            ,backgroundIntensity = 0.68f
            ,tutorialSteps = new[] { new TutorialStepDefinition { id = 701, title = "Endless vault", body = "Each chamber selects a small combination of mechanics. Check the active icons, plan the route and use the open space. New chambers keep the challenge going.", buttonLabel = "ENTER THE VAULT", completionTooltip = "Endless difficulty rises as you progress" } }
        };

        public int ChapterCount => chapters?.Length ?? 0;
        public int CuratedLevelCount => ChapterCount * Mathf.Max(1, levelsPerChapter);
        public int TotalChapterCount => ChapterCount + (endlessEnabled ? 1 : 0);

        public ChapterSettings GetChapter(int levelNumber)
        {
            if (chapters == null || chapters.Length == 0) return null;
            if (levelNumber > CuratedLevelCount && endlessEnabled) return endlessChapter;
            var chapterIndex = Mathf.Clamp((Mathf.Max(1, levelNumber) - 1) / Mathf.Max(1, levelsPerChapter), 0, chapters.Length - 1);
            return chapters[chapterIndex];
        }

        public ChapterSettings GetChapterByIndex(int chapterIndex)
        {
            if (chapterIndex >= ChapterCount) return endlessEnabled ? endlessChapter : null;
            return chapters != null && chapterIndex >= 0 && chapterIndex < chapters.Length ? chapters[chapterIndex] : null;
        }

        [Header("Art Resources")]
        public string glassFrameResource = "OutsideTheBox/Art/GlassFrame";
        public string ballResource = "OutsideTheBox/Art/SteelBall";
        public string goalPocketResource = "OutsideTheBox/Art/GoalPocket";
        public string magnetResource = "OutsideTheBox/Art/Magnet";
        public string victoryHeroResource = "OutsideTheBox/Art/VictoryHero";

        [Header("Feedback")]
        public bool defaultVibration = true;
        public bool defaultReduceMotion;
        [Range(0, 10)] public int freeHintAllowance = 5;

        [Header("Release Links")]
        public string privacyPolicyUrl;
        public string supportEmail;

        [Header("Localization")]
        public string[] supportedLanguageCodes = { "en" };

        [Header("Audio")]
        [Range(0f, 1f)] public float effectsVolume = 0.72f;
        [Range(0f, 1f)] public float musicVolume = 0.18f;
        public bool proceduralMusicFallback = true;
        [Range(4f, 20f)] public float ambientLoopSeconds = 8f;
        public AudioClip moveAudioClip;
        public AudioClip collisionAudioClip;
        public AudioClip polarityAudioClip;
        public AudioClip winAudioClip;
        public AudioClip[] chapterMusicClips = Array.Empty<AudioClip>();

        [Header("Visual Feedback")]
        [Range(0, 40)] public int collisionParticleCount = 7;
        [Range(0, 80)] public int polarityParticleCount = 14;
        [Range(0, 120)] public int winParticleCount = 36;
        [Range(0.04f, 0.4f)] public float feedbackParticleSize = 0.12f;
        [Range(0.1f, 1.5f)] public float feedbackParticleLifetime = 0.45f;

        [Header("Palette")]
        public Color background = new(0.025f, 0.06f, 0.12f, 1f);
        public Color board = new(0.55f, 0.83f, 0.92f, 0.78f);
        public Color boardEdge = new(0.68f, 0.94f, 1f, 1f);
        public Color wall = new(0.04f, 0.13f, 0.25f, 1f);
        public Color goal = new(0.2f, 0.95f, 0.48f, 1f);
        public Color gate = new(1f, 0.72f, 0.17f, 1f);
        public Color ball = new(0.9f, 0.95f, 1f, 1f);
        public Color magnet = new(0.96f, 0.22f, 0.28f, 1f);
        public Color hint = new(1f, 0.9f, 0.22f, 1f);
        public Color[] ballSkinTints =
        {
            Color.white,
            new(0.55f, 0.9f, 1f, 1f),
            new(1f, 0.78f, 0.28f, 1f),
            new(0.85f, 0.45f, 1f, 1f),
            new(1f, 0.42f, 0.48f, 1f),
            new(0.36f, 1f, 0.62f, 1f),
            new(0.18f, 0.22f, 0.38f, 1f),
            new(1f, 0.55f, 0.16f, 1f),
            new(0.46f, 0.66f, 1f, 1f),
            new(0.96f, 0.94f, 0.76f, 1f)
        };
        public Color[] magnetSkinTints =
        {
            Color.white,
            new(0.65f, 0.9f, 1f, 1f),
            new(1f, 0.78f, 0.35f, 1f),
            new(0.85f, 0.5f, 1f, 1f),
            new(1f, 0.36f, 0.45f, 1f),
            new(0.34f, 1f, 0.68f, 1f),
            new(0.18f, 0.3f, 0.42f, 1f),
            new(1f, 0.52f, 0.14f, 1f),
            new(0.42f, 0.62f, 1f, 1f),
            new(0.92f, 0.9f, 0.72f, 1f)
        };
        public Color[] goalSkinTints =
        {
            Color.white, new(0.25f, 1f, 0.55f, 1f), new(0.2f, 0.85f, 1f, 1f),
            new(1f, 0.72f, 0.18f, 1f), new(0.9f, 0.38f, 1f, 1f),
            new(1f, 0.3f, 0.42f, 1f), new(0.32f, 1f, 0.82f, 1f),
            new(0.62f, 0.72f, 1f, 1f), new(1f, 0.48f, 0.12f, 1f),
            new(0.9f, 1f, 0.6f, 1f)
        };
        public Color[] wallSkinTints =
        {
            Color.white, new(0.25f, 0.66f, 1f, 1f), new(0.2f, 0.9f, 0.72f, 1f),
            new(0.72f, 0.4f, 1f, 1f), new(1f, 0.46f, 0.4f, 1f),
            new(1f, 0.7f, 0.24f, 1f), new(0.5f, 0.58f, 0.72f, 1f),
            new(0.95f, 0.95f, 1f, 1f), new(0.16f, 0.82f, 0.92f, 1f),
            new(0.92f, 0.34f, 0.62f, 1f)
        };
        public Color[] glassSkinTints =
        {
            Color.white, new(0.34f, 0.94f, 1f, 1f), new(0.5f, 0.68f, 1f, 1f),
            new(0.72f, 0.5f, 1f, 1f), new(0.3f, 1f, 0.68f, 1f),
            new(1f, 0.56f, 0.42f, 1f), new(1f, 0.84f, 0.4f, 1f),
            new(0.95f, 0.38f, 0.72f, 1f), new(0.56f, 0.96f, 0.82f, 1f),
            new(0.82f, 0.9f, 1f, 1f)
        };
        public Color[] backgroundSkinTints =
        {
            Color.white, new(0.32f, 0.82f, 1f, 1f), new(0.34f, 1f, 0.66f, 1f),
            new(0.72f, 0.48f, 1f, 1f), new(1f, 0.38f, 0.52f, 1f),
            new(1f, 0.62f, 0.2f, 1f), new(0.34f, 0.4f, 0.62f, 1f),
            new(0.2f, 0.9f, 0.9f, 1f), new(0.92f, 0.38f, 0.88f, 1f),
            new(0.92f, 0.94f, 0.72f, 1f)
        };
        public int[] cosmeticStarThresholds = { 0, 15, 30, 50, 75, 105, 140, 180, 225, 275 };

        [Header("UI Palette")]
        public Color uiCream = new(0.965f, 0.985f, 1f, 1f);
        public Color uiNavy = new(0.06f, 0.13f, 0.28f, 1f);
        public Color uiOrange = new(1f, 0.76f, 0.22f, 1f);
        public Color uiButton = new(0.12f, 0.38f, 0.76f, 1f);
        public Color uiBackgroundTop = new(0.18f, 0.48f, 0.82f, 1f);
        public Color uiBackgroundBottom = new(0.06f, 0.22f, 0.49f, 1f);
        public Color uiSurface = new(0.075f, 0.20f, 0.40f, 0.96f);
        public Color uiSurfaceBright = new(0.13f, 0.32f, 0.56f, 1f);
        public Color uiPrimary = new(1f, 0.72f, 0.22f, 1f);
        public Color uiSecondary = new(0.59f, 0.37f, 0.96f, 1f);
        public Color uiTextPrimary = new(0.965f, 0.985f, 1f, 1f);
        public Color uiTextMuted = new(0.78f, 0.88f, 1f, 1f);
        public Color uiSuccess = new(0.4f, 1f, 0.74f, 1f);
        public Color uiDanger = new(1f, 0.35f, 0.40f, 1f);
        public Color uiGlow = new(0.36f, 0.88f, 1f, 1f);

        [Header("UI Button Gradients")]
        public bool uiButtonGradientsEnabled = true;
        public Color uiButtonGradientBottom = new(0.07f, 0.28f, 0.65f, 1f);
        public Color uiButtonGradientTop = new(0.19f, 0.52f, 0.90f, 1f);
        public Color uiButtonBorder = new(0.65f, 0.86f, 1f, 0.9f);
        public Color uiPrimaryGradientBottom = new(1f, 0.52f, 0.08f, 1f);
        public Color uiPrimaryGradientTop = new(1f, 0.84f, 0.34f, 1f);
        public Color uiPrimaryBorder = new(1f, 0.94f, 0.68f, 1f);
        public Color uiRewardGradientBottom = new(0.35f, 0.22f, 0.76f, 1f);
        public Color uiRewardGradientTop = new(0.66f, 0.47f, 1f, 1f);
        public Color uiRewardBorder = new(0.88f, 0.8f, 1f, 1f);
        public Color uiDangerGradientBottom = new(0.72f, 0.16f, 0.25f, 1f);
        public Color uiDangerGradientTop = new(1f, 0.42f, 0.45f, 1f);
        public Color uiDangerBorder = new(1f, 0.8f, 0.78f, 1f);
        public Color uiButtonGradientHighlight = new(1f, 1f, 1f, 1f);
        [Range(0f, 0.5f)] public float uiButtonGradientHighlightStrength = 0.08f;
    }

    [Serializable]
    public sealed class TutorialStepDefinition
    {
        [Min(1)] public int id;
        public string title;
        [TextArea(2, 5)] public string body;
        public string buttonLabel = "GOT IT";
        public string completionTooltip;
    }

    [Serializable]
    public sealed class ChapterSettings
    {
        public string title;
        public string challenge;
        [Range(5, 10)] public int boardSize = 5;
        [Range(0.04f, 0.4f)] public float wallDensity = 0.1f;
        [Range(0f, 0.15f)] public float wallDensityRamp = 0.04f;
        [Range(2, 7)] public int maxWallRunLength = 3;
        [Range(1, 20)] public int minimumWallGroups = 2;
        [Range(3, 30)] public int minimumSolutionSteps = 5;
        [Range(0, 4)] public int gateCount;
        [Range(0, 4)] public int movingWallCount;
        [Range(0, 5)] public int fieldZoneCount;
        [Range(0.1f, 2f)] public float dragMultiplier = 1f;
        [Range(0.25f, 2f)] public float magneticStrengthMultiplier = 1f;
        [Range(0f, 0.75f)] public float interferenceStrength;
        [Min(0.1f)] public float energyPerSecond = 8f;
        [Min(0f)] public float energyPerWorldUnit = 2f;
        [Min(1f)] public float targetEnergyPerSolutionStep = 9f;
        [Range(0f, 4f)] public float bounceMultiplier = 1f;
        public bool pushUnlocked;
        public bool oneWayGates;
        public bool movingWalls;
        public bool fieldZones;

        [Header("FTUE")]
        public TutorialStepDefinition[] tutorialSteps = Array.Empty<TutorialStepDefinition>();

        [Header("Live Background")]
        public Color backgroundColorA = new(0.13f, 0.55f, 0.78f, 1f);
        public Color backgroundColorB = new(0.48f, 0.82f, 0.92f, 1f);
        public Color backgroundAccent = new(0.76f, 0.96f, 1f, 1f);
        [Range(0f, 3f)] public float backgroundSpeed = 0.55f;
        [Range(0, 6)] public int backgroundPattern;
        [Range(0f, 1f)] public float backgroundIntensity = 0.55f;

        public float SafeDragMultiplier => dragMultiplier > 0f ? dragMultiplier : 1f;
        public float SafeMagneticStrength => magneticStrengthMultiplier > 0f ? magneticStrengthMultiplier : 1f;
        public float SafeEnergyPerSecond => energyPerSecond > 0f ? energyPerSecond : 8f;
        public float SafeEnergyPerWorldUnit => energyPerWorldUnit > 0f ? energyPerWorldUnit : 2f;
        public float SafeTargetEnergyPerStep => targetEnergyPerSolutionStep > 0f ? targetEnergyPerSolutionStep : 9f;
    }
}
