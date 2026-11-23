using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OutsideTheBox
{
    // Runtime board rendering, continuous movement, collisions, and goal presentation.
    public partial class BoardViewImplementation : MonoBehaviour
    {
        [SerializeField] private Transform staticContainer;
        [SerializeField] private Transform ballTransform;
        [SerializeField] private Transform magnetTransform;
        [SerializeField] private LineRenderer fieldLine;
        [SerializeField] private Transform chapterBackgroundTransform;
        [SerializeField] private ParticleSystem feedbackParticles;

        private readonly List<GameObject> spawned = new();
        private readonly List<LineRenderer> fieldLines = new();
        private Vector3[] fieldLinePositions = Array.Empty<Vector3>();
        private readonly List<Rect> wallCollisionRects = new();
        private readonly List<Rect> movingWallCollisionRects = new();
        private readonly List<MovingWallRuntime> movingWallRuntimes = new();
        private readonly List<FieldZoneRuntime> fieldZoneRuntimes = new();
        private LevelDefinition level;
        private GameConfig config;
        private Vector2 ballPosition;
        private Vector2 ballVelocity;
        private Vector3 magnetTarget;
        private float magnetTargetAngle;
        private ForceMode forceMode;
        private bool magnetForceActive;
        private bool simulationPaused;
        private bool exitReached;
        private bool hinting;
        private bool fieldLineDirty = true;
        private bool wasColliding;
        private float nextCollisionFeedbackTime;
        private Transform goalTransform;
        private Transform goalGlowTransform;
        private Transform glassFrameTransform;
        private SpriteRenderer cosmeticBackgroundRenderer;
        private Transform glassGlowTransform;
        private Coroutine pocketRoutine;
        private Sprite glassFrameSprite;
        private Sprite ballArtSprite;
        private Sprite goalPocketSprite;
        private Sprite magnetArtSprite;
        private ChapterSettings chapterSettings;
        private RuntimePrefabRegistry prefabs;
        private SpriteRenderer chapterBackgroundRenderer;
        private MaterialPropertyBlock chapterBackgroundProperties;
        private Rect lastContentRect;
        private Vector2Int lastDisplaySize;
        private float lastHudBottom;

        private const float PositionSnapSqrDistance = 0.000001f;
        private static readonly Dictionary<string, Sprite> ArtCache = new();
        private static Sprite whiteSprite;
        private static Sprite circleSprite;
        private static Sprite roundedWallSprite;
        private static Sprite radialGlowSprite;
        private static readonly int BackgroundColorAId = Shader.PropertyToID("_ColorA");
        private static readonly int BackgroundColorBId = Shader.PropertyToID("_ColorB");
        private static readonly int BackgroundAccentId = Shader.PropertyToID("_Accent");
        private static readonly int BackgroundSpeedId = Shader.PropertyToID("_Speed");
        private static readonly int BackgroundPatternId = Shader.PropertyToID("_Pattern");
        private static readonly int BackgroundIntensityId = Shader.PropertyToID("_Intensity");

        private sealed class MovingWallRuntime
        {
            public MovingWallDefinition definition;
            public Transform visual;
        }

        private sealed class FieldZoneRuntime
        {
            public float strength;
            public SpriteRenderer renderer;
            public GameObject marker;
        }

        public event Action ExitReached;
        public event Action ObstacleHit;

        public LevelDefinition Level => level;
        public Vector2 BallPosition => ballPosition;
        public Vector2 BallVelocity => ballVelocity;
        public Vector2 MagnetLocalPosition => magnetTransform != null ? (Vector2)magnetTransform.localPosition : Vector2.zero;
        public bool IsBallAnimating => ballVelocity.sqrMagnitude > 0.0001f || pocketRoutine != null;

        private float CellSize => config != null ? config.cellSize : 1f;
        private float BoardWidth => level != null ? level.width * CellSize : 0f;
        private float BoardHeight => level != null ? level.height * CellSize : 0f;

        protected virtual void Awake() => EnsureRuntimeObjects();

        public void Build(LevelDefinition definition, GameConfig gameConfig, GameState initial)
        {
            level = definition;
            config = gameConfig;
            chapterSettings = config.GetChapter(level.useNormalPhysics ? 1 : level.levelNumber);
            EnsureRuntimeObjects();
            LoadArt();
            ConfigureChapterBackground();
            StopPocketAnimation();
            ClearBoard();

            var boardBack = MakeSprite("Glass Board Surface", transform, config.board, -2, prefabs.boardSurfacePrefab);
            boardBack.transform.localPosition = new Vector3(0f, 0f, 0.2f);
            boardBack.transform.localScale = new Vector3(BoardWidth, BoardHeight, 1f);

            BuildGlassFrame();
            BuildWallVisuals();
            BuildMovingWalls();
            BuildFieldZones();
            BuildGoal();
            BuildGates();
            ApplyBallVisual(initial.ball);
            InitializeSignatureMechanics();
            InitializeRailChallenge(initial);
            ApplyCosmeticTints();

            forceMode = initial.mode;
            magnetForceActive = false;
            simulationPaused = false;
            exitReached = false;
            wasColliding = false;
            SetMagnet(initial.magnet, true);
            hinting = false;
            EnsureFieldLines();
            SetFieldLinesEnabled(true);
            UpdateFieldLines();
            fieldLineDirty = false;
            FitCamera();
        }

        public void SetForceMode(ForceMode mode) => forceMode = mode;

        public void SetMagnetForceActive(bool active)
        {
            magnetForceActive = active && !simulationPaused && !exitReached;
        }

        public void SetSimulationPaused(bool paused)
        {
            simulationPaused = paused;
            if (paused) magnetForceActive = false;
        }

        public void ApplyPlayerSettings()
        {
            ConfigureChapterBackground();
            RefreshFieldZoneAccessibility();
            ApplyCosmeticTints();
        }

        public void RefreshDisplayLayout()
        {
            if (config != null) FitCamera();
        }

        private void ApplyCosmeticTints()
        {
            if (config == null) return;
            var save = SaveService.Data;
            var ballRenderer = ballTransform != null ? ballTransform.GetComponent<SpriteRenderer>() : null;
            var magnetRenderer = magnetTransform != null ? magnetTransform.GetComponent<SpriteRenderer>() : null;
            if (ballRenderer != null)
            {
                var artwork = CosmeticArtwork.Get(0, save?.selectedBallSkin ?? 0);
                if (artwork != null) ballRenderer.sprite = artwork;
                var baseColor = ballArtSprite != null ? Color.white : config.ball;
                var tint = GetSkinTint(config.ballSkinTints, save?.selectedBallSkin ?? 0);
                ballRenderer.color = Color.Lerp(baseColor, tint, ballArtSprite != null ? 0.48f : 0.72f);
                if (artwork != null) ballRenderer.color = (save?.selectedBallSkin ?? 0) >= 10 ? tint : Color.white;
            }
            if (magnetRenderer != null)
            {
                var artwork = CosmeticArtwork.Get(1, save?.selectedMagnetSkin ?? 0);
                if (artwork != null) magnetRenderer.sprite = artwork;
                var baseColor = magnetArtSprite != null ? Color.white : config.magnet;
                var tint = GetSkinTint(config.magnetSkinTints, save?.selectedMagnetSkin ?? 0);
                magnetRenderer.color = Color.Lerp(baseColor, tint, magnetArtSprite != null ? 0.32f : 0.6f);
                if (artwork != null) magnetRenderer.color = (save?.selectedMagnetSkin ?? 0) >= 10 ? tint : Color.white;
            }

            var goalTint = GetSkinTint(config.goalSkinTints, save?.selectedGoalSkin ?? 0);
            var goalRenderer = goalTransform != null ? goalTransform.GetComponent<SpriteRenderer>() : null;
            if (goalRenderer != null)
            {
                var artwork = CosmeticArtwork.Get(2, save?.selectedGoalSkin ?? 0);
                if (artwork != null) goalRenderer.sprite = artwork;
                var baseColor = goalPocketSprite != null ? Color.white : config.goal;
                goalRenderer.color = Color.Lerp(baseColor, goalTint, goalPocketSprite != null ? 0.46f : 0.7f);
                if (artwork != null) goalRenderer.color = (save?.selectedGoalSkin ?? 0) >= 10 ? goalTint : Color.white;
            }
            var goalGlowRenderer = goalGlowTransform != null ? goalGlowTransform.GetComponent<SpriteRenderer>() : null;
            if (goalGlowRenderer != null)
                goalGlowRenderer.color = WithAlpha(Color.Lerp(config.goal, goalTint, 0.68f), config.goalGlowAlpha);

            var wallTint = GetSkinTint(config.wallSkinTints, save?.selectedWallSkin ?? 0);
            var glassTint = GetSkinTint(config.glassSkinTints, save?.selectedGlassSkin ?? 0);
            var glassArtwork = GameplayGlassArtwork(save?.selectedGlassSkin ?? 0);
            if (glassArtwork != null && glassFrameTransform != null)
            {
                var frameRenderer = glassFrameTransform.GetComponent<SpriteRenderer>();
                if (frameRenderer != null) { frameRenderer.sprite = glassArtwork; frameRenderer.color = (save?.selectedGlassSkin ?? 0) >= 10 ? glassTint : Color.white; }
            }
            for (var i = 0; i < spawned.Count; i++)
            {
                var item = spawned[i];
                if (item == null) continue;
                var itemName = item.name;
                var renderer = item.GetComponent<SpriteRenderer>();
                if (renderer == null) continue;

                if (itemName.IndexOf("Glass", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    renderer.color = glassArtwork != null && item.transform == glassFrameTransform
                        ? ((save?.selectedGlassSkin ?? 0) >= 10 ? glassTint : Color.white) : TintPreservingAlpha(renderer.color, glassTint, 0.58f);
                    continue;
                }

                if (itemName.IndexOf("Wall", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    itemName.IndexOf("Block", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    renderer.color = TintPreservingAlpha(renderer.color, wallTint, 0.56f);
                    var artwork = CosmeticArtwork.Get(3, save?.selectedWallSkin ?? 0);
                    if (artwork != null && !itemName.Contains("Shadow") && !itemName.Contains("Bevel"))
                    {
                        var footprint = (Vector2)item.transform.localScale;
                        if (renderer.drawMode == SpriteDrawMode.Sliced)
                            footprint = Vector2.Scale(footprint, renderer.size);
                        var vertical = footprint.y > footprint.x;
                        var rotation = Mathf.Repeat(item.transform.localEulerAngles.z, 180f);
                        if (rotation > 45f && rotation < 135f) vertical = !vertical;
                        renderer.sprite = artwork;
                        renderer.drawMode = SpriteDrawMode.Sliced;
                        renderer.size = new Vector2(Mathf.Max(footprint.x, footprint.y),
                            Mathf.Min(footprint.x, footprint.y));
                        item.transform.localScale = Vector3.one;
                        item.transform.localRotation = Quaternion.Euler(0f, 0f, vertical ? 90f : 0f);
                        renderer.color = (save?.selectedWallSkin ?? 0) >= 10 ? wallTint : Color.white;
                    }
                }
            }
        }

        private static Color TintPreservingAlpha(Color source, Color tint, float amount)
        {
            var alpha = source.a;
            var result = Color.Lerp(source, tint, Mathf.Clamp01(amount));
            result.a = alpha;
            return result;
        }

        private static Color GetSkinTint(Color[] colors, int requestedIndex)
        {
            if (requestedIndex >= 10) return ChapterGiftService.GiftTint(requestedIndex);
            if (colors == null || colors.Length == 0) return Color.white;
            return colors[Mathf.Clamp(requestedIndex, 0, colors.Length - 1)];
        }

        public void PlayCollisionEffect() => EmitFeedback(config.boardEdge, config.collisionParticleCount, 1.15f);

        public void PlayPolarityEffect(ForceMode mode)
        {
            var color = mode == ForceMode.Pull ? new Color(0.2f, 0.65f, 1f) : config.uiOrange;
            EmitFeedback(color, config.polarityParticleCount, 1.5f);
        }

        public void PlayWinEffect() => EmitFeedback(config.goal, config.winParticleCount, 2.1f);

        private void EmitFeedback(Color color, int requestedCount, float speed)
        {
            if (feedbackParticles == null || requestedCount <= 0) return;
            var count = SaveService.Data?.reduceMotion ?? false
                ? Mathf.Min(4, requestedCount)
                : requestedCount;
            feedbackParticles.transform.localPosition = BallLocalPosition + Vector3.back * 0.35f;
            var main = feedbackParticles.main;
            main.startLifetime = config.feedbackParticleLifetime;
            main.startSize = config.feedbackParticleSize;
            main.startColor = color;
            for (var i = 0; i < count; i++)
            {
                var angle = i * 2.399963f;
                var velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) *
                               speed * (0.72f + i % 4 * 0.09f);
                var emit = new ParticleSystem.EmitParams
                {
                    position = Vector3.zero,
                    velocity = velocity,
                    startColor = color,
                    startLifetime = config.feedbackParticleLifetime,
                    startSize = config.feedbackParticleSize
                };
                feedbackParticles.Emit(emit, 1);
            }
        }

        public void RestoreBall(Vector2 position, Vector2 velocity)
        {
            StopPocketAnimation();
            ballPosition = ClampToArena(position);
            ballVelocity = velocity;
            exitReached = false;
            wasColliding = false;
            ResetBallRenderer();
            ballTransform.localPosition = BallLocalPosition;
            SetFieldLinesEnabled(true);
            fieldLineDirty = true;
        }

        public void SnapBall(GridCell cell) => RestoreBall(CellLocal(cell), Vector2.zero);

        public GridCell NearestCell()
        {
            var x = Mathf.Clamp(Mathf.RoundToInt(ballPosition.x / CellSize + (level.width - 1) * 0.5f), 0, level.width - 1);
            var y = Mathf.Clamp(Mathf.RoundToInt(ballPosition.y / CellSize + (level.height - 1) * 0.5f), 0, level.height - 1);
            return new GridCell(x, y);
        }

        public void SetMagnet(RailSlot slot, bool immediate = false)
        {
            if (!AcceptRailTarget(RailLocal(slot) + Vector3.back * 0.3f, slot.side, immediate)) return;
            if (!immediate) return;
            magnetTransform.localPosition = magnetTarget;
            magnetTransform.localRotation = Quaternion.Euler(0f, 0f, magnetTargetAngle);
            fieldLineDirty = true;
        }

        public void SetHint(RailSlot? slot)
        {
            hinting = slot.HasValue;
            if (slot.HasValue && ActiveRailChallenge == RailChallengeKind.None) SetMagnet(slot.Value);
            var renderer = magnetTransform.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = hinting ? Color.Lerp(Color.white, config.hint, 0.55f) : Color.white;
            fieldLineDirty = true;
        }

        public RailSlot ProjectToRail(Vector3 worldPosition)
        {
            var local = transform.InverseTransformPoint(worldPosition);
            var bestSlot = new RailSlot(MagnetSide.Left, 0);
            var bestDistance = float.PositiveInfinity;
            for (var y = 0; y < level.height; y++)
            {
                ConsiderSlot(local, new RailSlot(MagnetSide.Left, y), ref bestSlot, ref bestDistance);
                ConsiderSlot(local, new RailSlot(MagnetSide.Right, y), ref bestSlot, ref bestDistance);
            }
            for (var x = 0; x < level.width; x++)
            {
                ConsiderSlot(local, new RailSlot(MagnetSide.Bottom, x), ref bestSlot, ref bestDistance);
                ConsiderSlot(local, new RailSlot(MagnetSide.Top, x), ref bestSlot, ref bestDistance);
            }
            return bestSlot;
        }

        public void SetMagnetFromPointer(Vector3 worldPosition)
        {
            var local = transform.InverseTransformPoint(worldPosition);
            var horizontalExtent = (level.width - 1) * CellSize * 0.5f;
            var verticalExtent = (level.height - 1) * CellSize * 0.5f;
            var railX = BoardWidth * 0.5f + config.railGap;
            var railY = BoardHeight * 0.5f + config.railGap;
            var pointer = new Vector2(local.x, local.y);
            var best = new Vector3(-railX, Mathf.Clamp(local.y, -verticalExtent, verticalExtent), -0.3f);
            var bestSide = MagnetSide.Left;
            var bestDistance = float.PositiveInfinity;
            ConsiderPoint(pointer, best, MagnetSide.Left, ref best, ref bestSide, ref bestDistance);
            ConsiderPoint(pointer, new Vector3(railX, Mathf.Clamp(local.y, -verticalExtent, verticalExtent), -0.3f), MagnetSide.Right, ref best, ref bestSide, ref bestDistance);
            ConsiderPoint(pointer, new Vector3(Mathf.Clamp(local.x, -horizontalExtent, horizontalExtent), -railY, -0.3f), MagnetSide.Bottom, ref best, ref bestSide, ref bestDistance);
            ConsiderPoint(pointer, new Vector3(Mathf.Clamp(local.x, -horizontalExtent, horizontalExtent), railY, -0.3f), MagnetSide.Top, ref best, ref bestSide, ref bestDistance);
            AcceptRailTarget(best, bestSide);
        }

        public Vector3 CellWorld(GridCell cell) => transform.TransformPoint(CellLocal(cell));

        public bool CanBeginMagnetDrag(Vector3 worldPosition)
        {
            if (config == null || magnetTransform == null) return false;
            var local = transform.InverseTransformPoint(worldPosition);
            return Vector2.Distance(local, magnetTransform.localPosition) <= config.magnetDragPickupRadius;
        }

        protected virtual void FixedUpdate()
        {
            if (config == null || level == null || simulationPaused || exitReached) return;
            UpdateRailChallenge(Time.fixedDeltaTime);
            UpdateMovingWalls();
            // A stopped ball already inside a pocket still completes the level.
            // Do this before the idle optimization skips the movement/goal checks.
            if (TryCaptureGoal()) return;
            var stopSpeedSqr = config.ballStopSpeed * config.ballStopSpeed;
            if (!magnetForceActive && !HasActiveSignatureForces() &&
                ballVelocity.sqrMagnitude <= stopSpeedSqr && movingWallRuntimes.Count == 0)
            {
                if (ballVelocity != Vector2.zero) ballVelocity = Vector2.zero;
                return;
            }

            var deltaTime = Time.fixedDeltaTime;
            if (magnetForceActive && IsRailMagnetPowered)
            {
                var toMagnet = (Vector2)magnetTransform.localPosition - ballPosition;
                var distance = Mathf.Max(0.35f, toMagnet.magnitude);
                var direction = toMagnet / distance;
                if (forceMode == ForceMode.Push) direction = -direction;
                direction = TransformDirectionThroughLenses(direction);
                var boardSpan = Mathf.Max(BoardWidth, BoardHeight);
                var distanceScale = Mathf.Lerp(1.35f, 0.72f, Mathf.Clamp01(distance / boardSpan));
                var strength = chapterSettings != null ? chapterSettings.SafeMagneticStrength : 1f;
                if (chapterSettings != null && chapterSettings.interferenceStrength > 0f)
                {
                    var wave = Mathf.Sin(Time.fixedTime * 2.17f + ballPosition.x * 1.31f + ballPosition.y * 0.83f);
                    strength *= Mathf.Max(0.2f, 1f + wave * chapterSettings.interferenceStrength);
                }
                if (level.fieldZones != null)
                {
                    for (var i = 0; i < level.fieldZones.Count; i++)
                    {
                        var zone = level.fieldZones[i];
                        if ((ballPosition - zone.center).sqrMagnitude <= zone.radius * zone.radius)
                            strength *= Mathf.Max(0.15f, zone.strengthMultiplier);
                    }
                }
                ballVelocity += direction * (config.magneticAcceleration * strength * distanceScale * deltaTime);
            }

            ApplySignatureForces(deltaTime);

            var dragMultiplier = chapterSettings != null ? chapterSettings.SafeDragMultiplier : 1f;
            ballVelocity *= Mathf.Exp(-config.ballDrag * dragMultiplier * deltaTime);
            if (ballVelocity.sqrMagnitude > config.maximumBallSpeed * config.maximumBallSpeed)
                ballVelocity = ballVelocity.normalized * config.maximumBallSpeed;

            var previous = ballPosition;
            var collided = MoveWithCollisions(deltaTime);
            PostMoveSignatureUpdate(previous);
            PostMoveRailChallenge(previous);
            if ((ballPosition - previous).sqrMagnitude > 0f)
            {
                ballTransform.localPosition = BallLocalPosition;
                fieldLineDirty = true;
            }

            if (collided && !wasColliding && Time.unscaledTime >= nextCollisionFeedbackTime)
            {
                nextCollisionFeedbackTime = Time.unscaledTime + config.collisionFeedbackCooldown;
                ObstacleHit?.Invoke();
            }
            wasColliding = collided;

            TryCaptureGoal();
        }

        private bool TryCaptureGoal()
        {
            var center = (Vector2)CellLocal(level.exit);
            if ((ballPosition - center).sqrMagnitude > config.exitRadius * config.exitRadius ||
                !SignatureGoalAllowsEntry() || !RailChallengeRules.GoalAllowsEntry(level, railState)) return false;
            BeginPocketDrop(center);
            return true;
        }

        protected virtual void Update()
        {
            if (config == null) return;
            if (lastContentRect != SafeAreaFitter.AvailableScreenRect() ||
                lastDisplaySize.x != Screen.width || lastDisplaySize.y != Screen.height ||
                !Mathf.Approximately(lastHudBottom, GameplayHudPresentation.BoardAreaBottom)) FitCamera();
            UpdateSignatureRecording();
            var visualsMoved = false;
            if ((magnetTransform.localPosition - magnetTarget).sqrMagnitude > PositionSnapSqrDistance)
            {
                var follow = 1f - Mathf.Exp(-config.magnetFollowSpeed * Time.unscaledDeltaTime);
                var nextPosition = Vector3.Lerp(magnetTransform.localPosition, magnetTarget, follow);
                if ((nextPosition - magnetTarget).sqrMagnitude <= PositionSnapSqrDistance) nextPosition = magnetTarget;
                magnetTransform.localPosition = nextPosition;
                visualsMoved = true;
            }

            var angle = Mathf.LerpAngle(magnetTransform.localEulerAngles.z, magnetTargetAngle,
                1f - Mathf.Exp(-config.magnetFollowSpeed * Time.unscaledDeltaTime));
            if (Mathf.Abs(Mathf.DeltaAngle(angle, magnetTargetAngle)) > 0.05f)
            {
                magnetTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                visualsMoved = true;
            }

            AnimateAmbientVisuals();
            AnimateSignatureVisuals();
            if (!visualsMoved && !fieldLineDirty) return;
            if (visualsMoved) FitCamera();
            UpdateFieldLines();
            fieldLineDirty = false;
        }

        private bool MoveWithCollisions(float deltaTime)
        {
            var collided = false;
            ballPosition += ballVelocity * deltaTime;

            var xLimit = BoardWidth * 0.5f - config.ballCollisionRadius;
            var yLimit = BoardHeight * 0.5f - config.ballCollisionRadius;
            if (ballPosition.x < -xLimit || ballPosition.x > xLimit)
            {
                ballPosition.x = Mathf.Clamp(ballPosition.x, -xLimit, xLimit);
                // Keep the component parallel to the glass and discard only the
                // component trying to move through it. Wall contact should slide,
                // never bounce or reduce the along-wall speed.
                ballVelocity.x = 0f;
                collided = true;
            }
            if (ballPosition.y < -yLimit || ballPosition.y > yLimit)
            {
                ballPosition.y = Mathf.Clamp(ballPosition.y, -yLimit, yLimit);
                ballVelocity.y = 0f;
                collided = true;
            }

            for (var pass = 0; pass < config.collisionSolverPasses; pass++)
            {
                for (var i = 0; i < wallCollisionRects.Count; i++) collided |= ResolveCircleRect(wallCollisionRects[i]);
                for (var i = 0; i < movingWallCollisionRects.Count; i++) collided |= ResolveCircleRect(movingWallCollisionRects[i]);
                for (var i = 0; i < level.gates.Count; i++)
                {
                    var gate = level.gates[i];
                    if (GateAllowsVelocity(gate, ballVelocity)) continue;
                    var center = (Vector2)CellLocal(gate.cell);
                    var size = CellSize * config.gateSize;
                    collided |= ResolveCircleRect(new Rect(center - Vector2.one * size * 0.5f, Vector2.one * size));
                }
                ResolveSignatureCollisions(ref collided);
            }
            return collided;
        }

        private bool ResolveCircleRect(Rect rect)
        {
            var closest = new Vector2(Mathf.Clamp(ballPosition.x, rect.xMin, rect.xMax), Mathf.Clamp(ballPosition.y, rect.yMin, rect.yMax));
            var delta = ballPosition - closest;
            var distanceSqr = delta.sqrMagnitude;
            var collisionRadius = config.ballCollisionRadius;
            if (distanceSqr >= collisionRadius * collisionRadius) return false;

            Vector2 normal;
            float pushDistance;
            if (distanceSqr > 0.000001f)
            {
                var distance = Mathf.Sqrt(distanceSqr);
                normal = delta / distance;
                pushDistance = collisionRadius - distance;
            }
            else
            {
                var left = ballPosition.x - rect.xMin;
                var right = rect.xMax - ballPosition.x;
                var bottom = ballPosition.y - rect.yMin;
                var top = rect.yMax - ballPosition.y;
                var minimum = Mathf.Min(left, right, bottom, top);
                if (minimum == left) { normal = Vector2.left; pushDistance = left + collisionRadius; }
                else if (minimum == right) { normal = Vector2.right; pushDistance = right + collisionRadius; }
                else if (minimum == bottom) { normal = Vector2.down; pushDistance = bottom + collisionRadius; }
                else { normal = Vector2.up; pushDistance = top + collisionRadius; }
            }

            ballPosition += normal * pushDistance;
            var velocityIntoWall = Vector2.Dot(ballVelocity, normal);
            if (velocityIntoWall < 0f)
                ballVelocity -= normal * velocityIntoWall;
            return true;
        }

        private static bool GateAllowsVelocity(GateDefinition gate, Vector2 velocity)
        {
            if (velocity.sqrMagnitude < 0.0001f) return false;
            var allowed = GameRules.DirectionVector(gate.allowedEntryDirection);
            return Vector2.Dot(velocity.normalized, new Vector2(allowed.x, allowed.y)) > 0.55f;
        }

        private Vector2 ClampToArena(Vector2 position)
        {
            var xLimit = BoardWidth * 0.5f - config.ballCollisionRadius;
            var yLimit = BoardHeight * 0.5f - config.ballCollisionRadius;
            position.x = Mathf.Clamp(position.x, -xLimit, xLimit);
            position.y = Mathf.Clamp(position.y, -yLimit, yLimit);
            return position;
        }

        private void BeginPocketDrop(Vector2 exitCenter)
        {
            if (exitReached) return;
            exitReached = true;
            magnetForceActive = false;
            ballVelocity = Vector2.zero;
            SetFieldLinesEnabled(false);
            pocketRoutine = StartCoroutine(AnimateBallIntoPocket(exitCenter));
        }

        private IEnumerator AnimateBallIntoPocket(Vector2 exitCenter)
        {
            var startPosition = ballPosition;
            var startScale = ballTransform.localScale;
            var startRotation = ballTransform.localEulerAngles.z;
            var renderer = ballTransform.GetComponent<SpriteRenderer>();
            var duration = Mathf.Max(0.01f, config.pocketDropDuration);
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = t * t * (3f - 2f * t);
                var suction = Mathf.Min(1.08f, eased + Mathf.Sin(t * Mathf.PI) * config.pocketSuctionOvershoot);
                ballPosition = Vector2.LerpUnclamped(startPosition, exitCenter, suction);
                ballTransform.localPosition = BallLocalPosition;
                ballTransform.localScale = startScale * Mathf.Lerp(1f, config.pocketDropEndScale, eased * eased);
                ballTransform.localRotation = Quaternion.Euler(0f, 0f, startRotation + config.pocketDropSpinDegrees * eased);
                if (renderer != null)
                {
                    if (t > config.pocketUnderlayTime) renderer.sortingOrder = 2;
                    var color = renderer.color;
                    color.a = 1f - Mathf.SmoothStep(config.pocketFadeStart, 1f, t);
                    renderer.color = color;
                }
                yield return null;
            }

            ballPosition = exitCenter;
            ballTransform.localPosition = BallLocalPosition;
            pocketRoutine = null;
            ExitReached?.Invoke();
        }

        private void StopPocketAnimation()
        {
            if (pocketRoutine == null) return;
            StopCoroutine(pocketRoutine);
            pocketRoutine = null;
        }

        private void BuildGlassFrame()
        {
            var size = new Vector2(BoardWidth + config.glassFramePadding * 2f, BoardHeight + config.glassFramePadding * 2f);
            if (glassFrameSprite == null)
            {
                BuildFallbackGlassFrame(size);
                BuildSolidGlassRim();
                return;
            }

            if (config.glassGlowAlpha > 0.001f)
            {
                var glow = MakeSprite("Glass Rim Glow", transform, WithAlpha(config.boardEdge, config.glassGlowAlpha), 0,
                    prefabs.glassPiecePrefab);
                var glowRenderer = glow.GetComponent<SpriteRenderer>();
                glowRenderer.sprite = glassFrameSprite;
                glowRenderer.drawMode = SpriteDrawMode.Sliced;
                glowRenderer.size = size * config.glassGlowScale / GlassFrameUnitScale;
                glow.transform.localScale = Vector3.one * GlassFrameUnitScale;
                glassGlowTransform = glow.transform;
            }

            var frame = MakeSprite("Illuminated Glass Border", transform, Color.white, 2, prefabs.glassPiecePrefab);
            var frameRenderer = frame.GetComponent<SpriteRenderer>();
            frameRenderer.sprite = glassFrameSprite;
            frameRenderer.drawMode = SpriteDrawMode.Sliced;
            frameRenderer.size = size / GlassFrameUnitScale;
            frame.transform.localScale = Vector3.one * GlassFrameUnitScale;
            glassFrameTransform = frame.transform;
            // The textured frame already supplies all four edges. An additional
            // square rim protrudes through its translucent left/bottom bevels.
        }

        private void BuildSolidGlassRim()
        {
            var thickness = config.outerGlassThickness;
            if (thickness <= 0.001f || config.outerGlassAlpha <= 0.001f) return;
            var color = WithAlpha(config.boardEdge, config.outerGlassAlpha);
            var horizontalSize = new Vector3(BoardWidth + thickness, thickness, 1f);
            var verticalSize = new Vector3(thickness, BoardHeight + thickness, 1f);
            var top = MakeSprite("Outer Glass Top", transform, color, 3, prefabs.glassPiecePrefab);
            top.transform.localPosition = new Vector3(0f, BoardHeight * 0.5f, -0.01f);
            top.transform.localScale = horizontalSize;
            var bottom = MakeSprite("Outer Glass Bottom", transform, color, 3, prefabs.glassPiecePrefab);
            bottom.transform.localPosition = new Vector3(0f, -BoardHeight * 0.5f, -0.01f);
            bottom.transform.localScale = horizontalSize;
            var left = MakeSprite("Outer Glass Left", transform, color, 3, prefabs.glassPiecePrefab);
            left.transform.localPosition = new Vector3(-BoardWidth * 0.5f, 0f, -0.01f);
            left.transform.localScale = verticalSize;
            var right = MakeSprite("Outer Glass Right", transform, color, 3, prefabs.glassPiecePrefab);
            right.transform.localPosition = new Vector3(BoardWidth * 0.5f, 0f, -0.01f);
            right.transform.localScale = verticalSize;
        }

        private void BuildFallbackGlassFrame(Vector2 size)
        {
            var thickness = Mathf.Max(0.08f, config.glassFrameBorderFraction);
            var horizontalScale = new Vector3(size.x, thickness, 1f);
            var verticalScale = new Vector3(thickness, size.y, 1f);
            var top = MakeSprite("Glass Border Top", transform, config.boardEdge, 2, prefabs.glassPiecePrefab);
            top.transform.localPosition = Vector3.up * (size.y * 0.5f);
            top.transform.localScale = horizontalScale;
            var bottom = MakeSprite("Glass Border Bottom", transform, config.boardEdge, 2, prefabs.glassPiecePrefab);
            bottom.transform.localPosition = Vector3.down * (size.y * 0.5f);
            bottom.transform.localScale = horizontalScale;
            var left = MakeSprite("Glass Border Left", transform, config.boardEdge, 2, prefabs.glassPiecePrefab);
            left.transform.localPosition = Vector3.left * (size.x * 0.5f);
            left.transform.localScale = verticalScale;
            var right = MakeSprite("Glass Border Right", transform, config.boardEdge, 2, prefabs.glassPiecePrefab);
            right.transform.localPosition = Vector3.right * (size.x * 0.5f);
            right.transform.localScale = verticalScale;
        }

        private void BuildGoal()
        {
            if (config.goalGlowAlpha > 0.001f)
            {
                var glow = MakeSprite("Goal Green Glow", staticContainer,
                    WithAlpha(config.goal, config.goalGlowAlpha), 3, prefabs.goalGlowPrefab);
                glow.GetComponent<SpriteRenderer>().sprite = RadialGlowSprite;
                glow.transform.localPosition = CellLocal(level.exit) + Vector3.back * 0.02f;
                glow.transform.localScale = Vector3.one * (config.exitRadius * 2f * config.goalArtScale * config.goalGlowScale);
                goalGlowTransform = glow.transform;
            }
            var goal = MakeSprite("Carrom Goal Pocket", staticContainer, Color.white, 4, prefabs.exitPrefab);
            var renderer = goal.GetComponent<SpriteRenderer>();
            renderer.sprite = goalPocketSprite != null ? goalPocketSprite : CircleSprite;
            if (goalPocketSprite == null) renderer.color = config.goal;
            goal.transform.localPosition = CellLocal(level.exit) + Vector3.back * 0.03f;
            goal.transform.localScale = Vector3.one * (config.exitRadius * 2f * config.goalArtScale);
            goalTransform = goal.transform;
        }

        private void BuildWallVisuals()
        {
            wallCollisionRects.Clear();
            if (level.wallSegments != null && level.wallSegments.Count > 0)
            {
                for (var i = 0; i < level.wallSegments.Count; i++)
                {
                    var segment = level.wallSegments[i];
                    var length = Mathf.Max(config.wallThickness, segment.length);
                    wallCollisionRects.Add(CreateWallCollisionRect(segment.center, length, segment.horizontal));
                    MakePill($"Wall Segment {i + 1}", segment.center, length, segment.horizontal);
                }
                return;
            }

            var occupied = new bool[level.width, level.height];
            for (var i = 0; i < level.walls.Count; i++)
            {
                var wall = level.walls[i];
                if (level.IsInside(wall)) occupied[wall.x, wall.y] = true;
            }

            var included = new bool[level.width, level.height];
            for (var y = 0; y < level.height; y++)
            {
                for (var x = 0; x < level.width;)
                {
                    if (!occupied[x, y]) { x++; continue; }
                    var start = x;
                    while (x + 1 < level.width && occupied[x + 1, y]) x++;
                    if (x - start + 1 >= 2)
                    {
                        AddWallRun(new GridCell(start, y), new GridCell(x, y), true);
                        for (var mark = start; mark <= x; mark++) included[mark, y] = true;
                    }
                    x++;
                }
            }

            for (var x = 0; x < level.width; x++)
            {
                for (var y = 0; y < level.height;)
                {
                    if (!occupied[x, y]) { y++; continue; }
                    var start = y;
                    while (y + 1 < level.height && occupied[x, y + 1]) y++;
                    if (y - start + 1 >= 2)
                    {
                        AddWallRun(new GridCell(x, start), new GridCell(x, y), false);
                        for (var mark = start; mark <= y; mark++) included[x, mark] = true;
                    }
                    y++;
                }
            }

            for (var y = 0; y < level.height; y++)
            for (var x = 0; x < level.width; x++)
            {
                if (!occupied[x, y] || included[x, y]) continue;
                AddStandaloneWall(new GridCell(x, y));
            }
        }

        private void AddStandaloneWall(GridCell cell)
        {
            var center = (Vector2)CellLocal(cell);
            var horizontal = ((cell.x + cell.y + level.levelNumber) & 1) == 0;
            var requestedLength = CellSize * config.standaloneWallLength;
            var availableLength = horizontal
                ? (BoardWidth * 0.5f - Mathf.Abs(center.x)) * 2f
                : (BoardHeight * 0.5f - Mathf.Abs(center.y)) * 2f;
            var length = Mathf.Max(config.wallThickness, Mathf.Min(requestedLength, availableLength));
            wallCollisionRects.Add(CreateWallCollisionRect(center, length, horizontal));
            MakePill($"Wall {cell}", center, length, horizontal);
        }

        private void AddWallRun(GridCell first, GridCell last, bool horizontal)
        {
            var a = (Vector2)CellLocal(first);
            var b = (Vector2)CellLocal(last);
            var halfThickness = config.wallThickness * 0.5f;
            var rimInset = config.outerGlassThickness * 0.5f;
            float minimum;
            float maximum;
            if (horizontal)
            {
                minimum = a.x - halfThickness;
                maximum = b.x + halfThickness;
                if (first.x == 0) minimum = -BoardWidth * 0.5f + rimInset;
                if (last.x == level.width - 1) maximum = BoardWidth * 0.5f - rimInset;
            }
            else
            {
                minimum = a.y - halfThickness;
                maximum = b.y + halfThickness;
                if (first.y == 0) minimum = -BoardHeight * 0.5f + rimInset;
                if (last.y == level.height - 1) maximum = BoardHeight * 0.5f - rimInset;
            }
            var center = horizontal
                ? new Vector2((minimum + maximum) * 0.5f, a.y)
                : new Vector2(a.x, (minimum + maximum) * 0.5f);
            var length = maximum - minimum;
            wallCollisionRects.Add(CreateWallCollisionRect(center, length, horizontal));
            MakePill($"Wall {first}-{last}", center, length, horizontal);
        }

        private Rect CreateWallCollisionRect(Vector2 center, float visualLength, bool horizontal)
        {
            var thickness = Mathf.Min(config.wallThickness, config.wallCollisionThickness);
            var length = Mathf.Max(thickness, visualLength - config.wallCollisionEndInset * 2f);
            var size = horizontal ? new Vector2(length, thickness) : new Vector2(thickness, length);
            return new Rect(center - size * 0.5f, size);
        }

        private void MakePill(string objectName, Vector2 center, float length, bool horizontal)
        {
            var thickness = config.wallThickness;
            var shadowCenter = center + new Vector2(config.wallShadowOffset, -config.wallShadowOffset);
            MakePillLayer(objectName + " Shadow", shadowCenter, length, thickness, horizontal,
                new Color(0.01f, 0.025f, 0.06f, config.wallShadowAlpha), 1);
            MakePillLayer(objectName, center, length, thickness, horizontal, config.wall, 8);

            if (config.wallBevelAlpha <= 0f) return;
            var highlightOffset = horizontal ? new Vector2(0f, thickness * 0.28f) : new Vector2(-thickness * 0.28f, 0f);
            var highlightSize = horizontal
                ? new Vector3(Mathf.Max(thickness * 0.35f, length - thickness * 0.35f), thickness * 0.09f, 1f)
                : new Vector3(thickness * 0.09f, Mathf.Max(thickness * 0.35f, length - thickness * 0.35f), 1f);
            var highlight = MakeSprite(objectName + " Bevel", staticContainer,
                new Color(config.boardEdge.r, config.boardEdge.g, config.boardEdge.b, config.wallBevelAlpha), 9,
                prefabs.wallPrefab);
            highlight.transform.localPosition = (Vector3)(center + highlightOffset) + Vector3.back * 0.02f;
            highlight.transform.localScale = highlightSize;
        }

        private void MakePillLayer(string objectName, Vector2 center, float length, float thickness, bool horizontal, Color color, int order)
        {
            var wall = MakeSprite(objectName, staticContainer, color, order, prefabs.wallPrefab);
            wall.transform.localPosition = center;
            wall.transform.localScale = horizontal
                ? new Vector3(length, thickness, 1f)
                : new Vector3(thickness, length, 1f);
        }

        private void BuildGates()
        {
            for (var i = 0; i < level.gates.Count; i++)
            {
                var gate = level.gates[i];
                var item = MakeSprite($"Gate {gate.cell}", staticContainer, config.gate, 3, prefabs.oneWayGatePrefab);
                item.transform.localPosition = CellLocal(gate.cell);
                item.transform.localScale = Vector3.one * (CellSize * config.gateSize);
                var arrow = AddMechanicIcon(item.transform, MechanicIconId.Gates, .56f);
                if (arrow != null)
                {
                    arrow.color = config.uiNavy;
                    arrow.transform.localRotation = Quaternion.Euler(0f, 0f,
                    gate.allowedEntryDirection switch { CardinalDirection.Up => 90f, CardinalDirection.Down => -90f,
                        CardinalDirection.Left => 180f, _ => 0f });
                }
            }
        }

        private void BuildMovingWalls()
        {
            movingWallCollisionRects.Clear();
            movingWallRuntimes.Clear();
            if (level.movingWalls == null) return;
            for (var i = 0; i < level.movingWalls.Count; i++)
            {
                var definition = level.movingWalls[i];
                var visual = MakeSprite($"Moving Wall {i + 1}", staticContainer,
                    Color.Lerp(config.wall, config.gate, 0.22f), 8, prefabs.wallPrefab);
                visual.transform.localScale = definition.horizontal
                    ? new Vector3(definition.length, config.wallThickness, 1f)
                    : new Vector3(config.wallThickness, definition.length, 1f);
                var runtime = new MovingWallRuntime { definition = definition, visual = visual.transform };
                AddMechanicIcon(visual.transform, MechanicIconId.MovingWall, .32f);
                movingWallRuntimes.Add(runtime);
                movingWallCollisionRects.Add(default);
            }
            UpdateMovingWalls();
        }

        private void UpdateMovingWalls()
        {
            for (var i = 0; i < movingWallRuntimes.Count; i++)
            {
                var runtime = movingWallRuntimes[i];
                var definition = runtime.definition;
                var wave = Mathf.Sin(Time.fixedTime * definition.speed + definition.phase);
                var center = definition.center + definition.travel * wave;
                runtime.visual.localPosition = (Vector3)center + Vector3.back * 0.02f;
                movingWallCollisionRects[i] = CreateWallCollisionRect(center, definition.length, definition.horizontal);
            }
        }

        private void BuildFieldZones()
        {
            if (level.fieldZones == null) return;
            for (var i = 0; i < level.fieldZones.Count; i++)
            {
                var zone = level.fieldZones[i];
                var baseColor = GetFieldZoneColor(zone.strengthMultiplier);
                var item = MakeSprite($"Field Zone {i + 1}", staticContainer,
                    WithAlpha(baseColor, config.fieldZoneAlpha), 0, prefabs.goalGlowPrefab);
                var renderer = item.GetComponent<SpriteRenderer>();
                renderer.sprite = RadialGlowSprite;
                item.transform.localPosition = (Vector3)zone.center + Vector3.forward * 0.1f;
                item.transform.localScale = Vector3.one * (zone.radius * 2f);
                BuildMechanicRing(item.transform, baseColor, 0.46f);

                var icon = AddMechanicIcon(item.transform, MechanicIconId.FieldZone, .46f);
                var marker = icon != null ? icon.gameObject : null;
                fieldZoneRuntimes.Add(new FieldZoneRuntime
                {
                    strength = zone.strengthMultiplier,
                    renderer = renderer,
                    marker = marker
                });
            }
        }

        private Color GetFieldZoneColor(float strength)
        {
            var accessible = SaveService.Data?.colorBlindMode ?? false;
            if (strength < 1f) return accessible ? config.accessibleWeakFieldZone : config.weakFieldZone;
            return accessible ? config.accessibleStrongFieldZone : config.strongFieldZone;
        }

        private void RefreshFieldZoneAccessibility()
        {
            foreach (var zone in fieldZoneRuntimes)
            {
                if (zone.renderer != null)
                    zone.renderer.color = WithAlpha(GetFieldZoneColor(zone.strength), config.fieldZoneAlpha);
                if (zone.marker != null) zone.marker.SetActive(true);
            }
        }

        private void ApplyBallVisual(GridCell initialCell)
        {
            ballPosition = CellLocal(initialCell);
            ballVelocity = Vector2.zero;
            ballTransform.localPosition = BallLocalPosition;
            ballTransform.localRotation = Quaternion.identity;
            ResetBallRenderer();
        }

        private void ResetBallRenderer()
        {
            ballTransform.localScale = Vector3.one * (config.ballRadius * 2f * (ballArtSprite != null ? config.ballArtScale : 1f));
            var renderer = ballTransform.GetComponent<SpriteRenderer>();
            if (renderer == null)
                throw new InvalidOperationException("Ball prefab requires a SpriteRenderer.");
            renderer.sprite = ballArtSprite != null ? ballArtSprite : CircleSprite;
            renderer.color = ballArtSprite != null ? Color.white : config.ball;
            renderer.sortingOrder = 10;
            var fallbackHighlight = ballTransform.Find("Highlight");
            if (fallbackHighlight != null) fallbackHighlight.gameObject.SetActive(ballArtSprite == null);
            if (ballArtSprite == null) EnsureBallHighlight();
        }

        private void AnimateAmbientVisuals()
        {
            var time = Time.unscaledTime;
            var motionMultiplier = SaveService.Data?.reduceMotion ?? false ? 0f : 1f;
            if (goalTransform != null)
            {
                var pulse = 1f + Mathf.Sin(time * config.goalPulseSpeed * Mathf.PI * 2f) * config.goalPulseAmount * motionMultiplier;
                goalTransform.localScale = Vector3.one * (config.exitRadius * 2f * config.goalArtScale * pulse);
                if (goalGlowTransform != null)
                {
                    var goalGlowPulse = 1f + Mathf.Sin(time * config.goalPulseSpeed * Mathf.PI * 2f) * config.goalGlowPulseAmount * motionMultiplier;
                    goalGlowTransform.localScale = Vector3.one *
                        (config.exitRadius * 2f * config.goalArtScale * config.goalGlowScale * goalGlowPulse);
                }
            }
            if (glassGlowTransform == null) return;
            var glowPulse = 1f + Mathf.Sin(time * config.glassGlowPulseSpeed * Mathf.PI * 2f) * config.glassGlowPulseAmount;
            glassGlowTransform.localScale = Vector3.one * (glowPulse * GlassFrameUnitScale);
        }

        private void EnsureFieldLines()
        {
            if (fieldLine != null && fieldLines.Count == 0) fieldLines.Add(fieldLine);
            while (fieldLines.Count < config.fieldLineCount)
            {
                var lineObject = prefabs.Spawn(prefabs.magneticFieldPrefab,
                    $"Magnetic Field {fieldLines.Count + 1}", transform);
                var line = lineObject.GetComponent<LineRenderer>();
                if (line == null)
                    throw new InvalidOperationException("MagneticField prefab requires a LineRenderer.");
                line.useWorldSpace = true;
                line.sortingOrder = 6;
                fieldLines.Add(line);
            }
            for (var i = 0; i < fieldLines.Count; i++)
            {
                var line = fieldLines[i];
                line.enabled = i < config.fieldLineCount;
                line.widthMultiplier = config.fieldLineWidth;
                line.numCapVertices = 3;
                line.numCornerVertices = 2;
            }
            fieldLine = fieldLines.Count > 0 ? fieldLines[0] : null;
        }

        private void SetFieldLinesEnabled(bool value)
        {
            for (var i = 0; i < fieldLines.Count; i++) fieldLines[i].enabled = value && i < config.fieldLineCount;
        }

        private void UpdateFieldLines()
        {
            if (ballTransform == null || magnetTransform == null) return;
            var count = Mathf.Min(config.fieldLineCount, fieldLines.Count);
            var segments = Mathf.Max(4, config.fieldLineSegments);
            if (fieldLinePositions.Length != segments) fieldLinePositions = new Vector3[segments];
            var start = (Vector2)magnetTransform.position;
            var end = (Vector2)ballTransform.position;
            var depth = transform.position.z - 0.1f;
            var direction = end - start;
            var perpendicular = direction.sqrMagnitude > 0.0001f ? new Vector2(-direction.y, direction.x).normalized : Vector2.up;

            for (var lineIndex = 0; lineIndex < fieldLines.Count; lineIndex++)
            {
                var line = fieldLines[lineIndex];
                line.enabled = lineIndex < count && !exitReached;
                if (!line.enabled) continue;
                if (line.positionCount != segments) line.positionCount = segments;
                var normalizedIndex = count <= 1 ? 0f : lineIndex / (float)(count - 1) * 2f - 1f;
                var control = (start + end) * 0.5f + perpendicular * (normalizedIndex * config.fieldCurveSpread);
                for (var segment = 0; segment < segments; segment++)
                {
                    var t = segment / (float)(segments - 1);
                    var oneMinusT = 1f - t;
                    var point = oneMinusT * oneMinusT * start + 2f * oneMinusT * t * control + t * t * end;
                    fieldLinePositions[segment] = new Vector3(point.x, point.y, depth);
                }
                line.SetPositions(fieldLinePositions);
                var startColor = hinting ? config.hint : new Color(config.boardEdge.r, config.boardEdge.g, config.boardEdge.b, config.fieldLineAlpha);
                line.startColor = startColor;
                line.endColor = new Color(config.ball.r, config.ball.g, config.ball.b, config.fieldLineAlpha * 0.28f);
            }
        }

        private void SetMagnetTarget(Vector3 position, MagnetSide side)
        {
            magnetTarget = position;
            magnetTargetAngle = side switch
            {
                MagnetSide.Left => 180f,
                MagnetSide.Right => 0f,
                MagnetSide.Bottom => -90f,
                _ => 90f
            };
        }

        private void ConsiderSlot(Vector3 local, RailSlot slot, ref RailSlot bestSlot, ref float bestDistance)
        {
            var candidate = RailLocal(slot);
            var delta = local - candidate;
            var distance = delta.x * delta.x + delta.y * delta.y;
            if (distance >= bestDistance) return;
            bestDistance = distance;
            bestSlot = slot;
        }

        private static void ConsiderPoint(Vector2 pointer, Vector3 candidate, MagnetSide side,
            ref Vector3 best, ref MagnetSide bestSide, ref float bestDistance)
        {
            var dx = pointer.x - candidate.x;
            var dy = pointer.y - candidate.y;
            var distance = dx * dx + dy * dy;
            if (distance >= bestDistance) return;
            bestDistance = distance;
            best = candidate;
            bestSide = side;
        }

        private Vector3 BallLocalPosition => new(ballPosition.x, ballPosition.y, -0.2f);

        private Vector3 CellLocal(GridCell cell) => new(
            (cell.x - (level.width - 1) * 0.5f) * CellSize,
            (cell.y - (level.height - 1) * 0.5f) * CellSize,
            0f);

        private Vector3 RailLocal(RailSlot slot)
        {
            var railX = BoardWidth * 0.5f + config.railGap;
            var railY = BoardHeight * 0.5f + config.railGap;
            return slot.side switch
            {
                MagnetSide.Left => new Vector3(-railX, (slot.index - (level.height - 1) * 0.5f) * CellSize, 0f),
                MagnetSide.Right => new Vector3(railX, (slot.index - (level.height - 1) * 0.5f) * CellSize, 0f),
                MagnetSide.Bottom => new Vector3((slot.index - (level.width - 1) * 0.5f) * CellSize, -railY, 0f),
                _ => new Vector3((slot.index - (level.width - 1) * 0.5f) * CellSize, railY, 0f)
            };
        }

        private void EnsureRuntimeObjects()
        {
            prefabs ??= RuntimePrefabRegistry.Current;
            if (staticContainer == null)
                staticContainer = prefabs.Spawn(prefabs.containerPrefab, "Static Pieces", transform).transform;
            if (ballTransform == null)
                ballTransform = prefabs.Spawn(prefabs.ballPrefab, "Ball", transform).transform;
            if (magnetTransform == null)
                magnetTransform = prefabs.Spawn(prefabs.magnetPrefab, "Magnet", transform).transform;
            if (chapterBackgroundTransform == null)
                chapterBackgroundTransform = prefabs.Spawn(prefabs.chapterBackgroundPrefab, "Chapter Live Background", transform).transform;
            if (feedbackParticles == null)
                feedbackParticles = prefabs.Spawn(prefabs.feedbackBurstPrefab, "Feedback Burst", transform)
                    .GetComponent<ParticleSystem>();
            if (feedbackParticles == null)
                throw new InvalidOperationException("FeedbackBurst prefab requires a ParticleSystem.");
            chapterBackgroundRenderer = chapterBackgroundTransform.GetComponent<SpriteRenderer>();
            if (chapterBackgroundRenderer == null)
                throw new InvalidOperationException("ChapterBackground prefab requires a SpriteRenderer.");
            chapterBackgroundRenderer.sprite = WhiteSprite;
            chapterBackgroundRenderer.drawMode = SpriteDrawMode.Simple;
            chapterBackgroundRenderer.color = Color.white;
            chapterBackgroundRenderer.sortingOrder = -100;
            chapterBackgroundTransform.localPosition = new Vector3(0f, 0f, 2f);
            var magnetRenderer = magnetTransform.GetComponent<SpriteRenderer>();
            if (magnetRenderer == null)
                throw new InvalidOperationException("Magnet prefab requires a SpriteRenderer.");
            magnetRenderer.sortingOrder = 12;
            if (fieldLine != null && !fieldLines.Contains(fieldLine)) fieldLines.Add(fieldLine);
        }

        private void ConfigureChapterBackground()
        {
            if (chapterBackgroundRenderer == null || chapterSettings == null) return;
            // Test levels can opt into simple physics without losing their chapter's visual identity.
            var visualSettings = config.GetChapter(level.levelNumber) ?? chapterSettings;
            var backgroundTint = GetSkinTint(config.backgroundSkinTints,
                SaveService.Data?.selectedBackgroundSkin ?? 0);
            chapterBackgroundProperties ??= new MaterialPropertyBlock();
            chapterBackgroundRenderer.GetPropertyBlock(chapterBackgroundProperties);
            chapterBackgroundProperties.SetColor(BackgroundColorAId,
                Color.Lerp(visualSettings.backgroundColorA, backgroundTint, 0.24f));
            chapterBackgroundProperties.SetColor(BackgroundColorBId,
                Color.Lerp(visualSettings.backgroundColorB, backgroundTint, 0.2f));
            chapterBackgroundProperties.SetColor(BackgroundAccentId,
                Color.Lerp(visualSettings.backgroundAccent, backgroundTint, 0.32f));
            var reduceMotion = SaveService.Data?.reduceMotion ?? false;
            chapterBackgroundProperties.SetFloat(BackgroundSpeedId, reduceMotion ? 0f : visualSettings.backgroundSpeed);
            chapterBackgroundProperties.SetFloat(BackgroundPatternId, visualSettings.backgroundPattern);
            chapterBackgroundProperties.SetFloat(BackgroundIntensityId, visualSettings.backgroundIntensity);
            chapterBackgroundRenderer.SetPropertyBlock(chapterBackgroundProperties);
            var backgroundArtwork = CosmeticArtwork.Get(5, SaveService.Data?.selectedBackgroundSkin ?? 0);
            if (backgroundArtwork != null)
            {
                if (cosmeticBackgroundRenderer == null)
                    cosmeticBackgroundRenderer = prefabs.Spawn(prefabs.spritePiecePrefab, "Cosmetic Background Artwork", chapterBackgroundTransform)
                        .GetComponent<SpriteRenderer>();
                if (cosmeticBackgroundRenderer != null)
                {
                    cosmeticBackgroundRenderer.sprite = backgroundArtwork;
                    // Background atlas cells have no slice borders and may not
                    // be square. Cover the unit background instead of relying on
                    // a sliced size that doesn't match the sprite's actual bounds.
                    cosmeticBackgroundRenderer.drawMode = SpriteDrawMode.Simple;
                    var bounds = backgroundArtwork.bounds;
                    var cover = Mathf.Max(1f / Mathf.Max(0.001f, bounds.size.x),
                        1f / Mathf.Max(0.001f, bounds.size.y));
                    var artworkTransform = cosmeticBackgroundRenderer.transform;
                    artworkTransform.localRotation = Quaternion.identity;
                    artworkTransform.localScale = Vector3.one * cover;
                    artworkTransform.localPosition = new Vector3(-bounds.center.x * cover,
                        -bounds.center.y * cover, -0.02f);
                    cosmeticBackgroundRenderer.sortingOrder = -99;
                    cosmeticBackgroundRenderer.color = WithAlpha((SaveService.Data?.selectedBackgroundSkin ?? 0) >= 10
                        ? backgroundTint : Color.white, 0.75f);
                }
            }
        }

        private void LoadArt()
        {
            glassFrameSprite = LoadNormalizedSprite(config.glassFrameResource, config.glassFrameBorderFraction);
            ballArtSprite = LoadNormalizedSprite(config.ballResource);
            goalPocketSprite = LoadNormalizedSprite(config.goalPocketResource);
            magnetArtSprite = LoadNormalizedSprite(config.magnetResource);
            var skins = SaveService.Data;
            glassFrameSprite = GameplayGlassArtwork(skins?.selectedGlassSkin ?? 0) ?? glassFrameSprite;
            ballArtSprite = CosmeticArtwork.Get(0, skins?.selectedBallSkin ?? 0) ?? ballArtSprite;
            goalPocketSprite = CosmeticArtwork.Get(2, skins?.selectedGoalSkin ?? 0) ?? goalPocketSprite;
            magnetArtSprite = CosmeticArtwork.Get(1, skins?.selectedMagnetSkin ?? 0) ?? magnetArtSprite;

            var magnetRenderer = magnetTransform.GetComponent<SpriteRenderer>();
            magnetRenderer.sprite = magnetArtSprite != null ? magnetArtSprite : WhiteSprite;
            magnetRenderer.color = magnetArtSprite != null ? Color.white : config.magnet;
            magnetTransform.localScale = Vector3.one * config.magnetVisualSize;
            var red = magnetTransform.Find("Red Pole");
            var blue = magnetTransform.Find("Blue Pole");
            if (red != null) red.gameObject.SetActive(magnetArtSprite == null);
            if (blue != null) blue.gameObject.SetActive(magnetArtSprite == null);
            if (magnetArtSprite == null) ConfigureMagnetPoles(red, blue);
        }

        private float GlassFrameUnitScale => Mathf.Max(0.01f, CellSize * 2f);

        private Sprite GameplayGlassArtwork(int skin) => CosmeticArtwork.GetBoardGlass(skin) ??
            LoadNormalizedSprite(config.glassFrameResource, config.glassFrameBorderFraction);

        private static Sprite LoadNormalizedSprite(string resourcePath, float borderFraction = 0f)
        {
            if (string.IsNullOrWhiteSpace(resourcePath)) return null;
            var cacheKey = resourcePath + ":" + borderFraction.ToString("0.000");
            if (ArtCache.TryGetValue(cacheKey, out var cached) && cached != null) return cached;
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) return null;
            var borderPixels = Mathf.Min(texture.width, texture.height) * Mathf.Clamp01(borderFraction);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.one * 0.5f,
                Mathf.Max(texture.width, texture.height), 0u, SpriteMeshType.FullRect,
                Vector4.one * borderPixels);
            sprite.name = texture.name + " Runtime Sprite";
            ArtCache[cacheKey] = sprite;
            return sprite;
        }

        private void ConfigureMagnetPoles(Transform red, Transform blue)
        {
            if (red == null || blue == null)
                throw new InvalidOperationException("Magnet prefab requires Red Pole and Blue Pole children.");
            var redRenderer = red.GetComponent<SpriteRenderer>();
            var blueRenderer = blue.GetComponent<SpriteRenderer>();
            if (redRenderer == null || blueRenderer == null)
                throw new InvalidOperationException("Magnet pole prefab children require SpriteRenderers.");
            redRenderer.sprite = WhiteSprite;
            redRenderer.color = new Color(0.95f, 0.14f, 0.2f);
            redRenderer.sortingOrder = 12;
            blueRenderer.sprite = WhiteSprite;
            blueRenderer.color = new Color(0.15f, 0.45f, 0.95f);
            blueRenderer.sortingOrder = 12;
        }

        private void EnsureBallHighlight()
        {
            var highlight = ballTransform.Find("Highlight");
            if (highlight == null)
                throw new InvalidOperationException("Ball prefab requires a Highlight child.");
            var renderer = highlight.GetComponent<SpriteRenderer>();
            if (renderer == null)
                throw new InvalidOperationException("Ball Highlight prefab child requires a SpriteRenderer.");
            renderer.sprite = CircleSprite;
            renderer.color = new Color(1f, 1f, 1f, 0.75f);
            renderer.sortingOrder = 11;
        }

        private void ClearBoard()
        {
            ResetSignatureMechanics();
            goalTransform = null;
            goalGlowTransform = null;
            glassFrameTransform = null;
            glassGlowTransform = null;
            wallCollisionRects.Clear();
            movingWallCollisionRects.Clear();
            movingWallRuntimes.Clear();
            fieldZoneRuntimes.Clear();
            for (var i = spawned.Count - 1; i >= 0; i--)
            {
                var item = spawned[i];
                if (item == null) continue;
                Destroy(item);
            }
            spawned.Clear();
        }

        private GameObject MakeSprite(string objectName, Transform parent, Color color, int order,
            GameObject prefab = null, bool trackForCleanup = true)
        {
            var item = prefabs.Spawn(prefab != null ? prefab : prefabs.spritePiecePrefab, objectName, parent);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            item.transform.localScale = Vector3.one;
            var renderer = item.GetComponent<SpriteRenderer>();
            if (renderer == null)
                throw new InvalidOperationException($"Prefab used for '{objectName}' requires a SpriteRenderer.");
            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.size = Vector2.one;
            renderer.flipX = false;
            renderer.flipY = false;
            renderer.sprite = WhiteSprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            if (trackForCleanup) spawned.Add(item);
            return item;
        }

        private GameObject MakeTextArrow(Transform parent, CardinalDirection direction)
        {
            var go = prefabs.Spawn(prefabs.directionArrowPrefab, "Direction Arrow", parent);
            go.transform.localPosition = Vector3.back * 0.1f;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, -90f * (int)direction);
            var mesh = go.GetComponent<TextMesh>();
            if (mesh == null)
                throw new InvalidOperationException("DirectionArrow prefab requires a TextMesh.");
            mesh.text = "▲";
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = 0.14f;
            mesh.fontSize = 56;
            mesh.color = config.wall;
            mesh.GetComponent<MeshRenderer>().sortingOrder = 4;
            return go;
        }

        private void FitCamera()
        {
            var camera = Camera.main;
            if (camera == null) return;
            var backgroundTint = GetSkinTint(config.backgroundSkinTints,
                SaveService.Data?.selectedBackgroundSkin ?? 0);
            camera.backgroundColor = Color.Lerp(config.background, backgroundTint, 0.22f);
            camera.orthographic = true;
            var content = SafeAreaFitter.AvailableScreenRect();
            var screenWidth = Mathf.Max(1f, Screen.width);
            var screenHeight = Mathf.Max(1f, Screen.height);
            // Keep the board centered in the banner-safe play area. Symmetric rail
            // clearance leaves room for the visible magnet without shifting the board
            // towards the opposite screen edge whenever the magnet moves.
            var bottom = GameplayHudPresentation.BoardAreaBottom;
            var boardArea = new Rect(content.x + content.width * 0.01f,
                content.y + content.height * bottom, content.width * 0.98f, content.height * (.735f - bottom));
            var magnetRenderer = magnetTransform.GetComponent<SpriteRenderer>();
            var visibleBounds = magnetRenderer.bounds;
            var min = transform.InverseTransformPoint(visibleBounds.min);
            var max = transform.InverseTransformPoint(visibleBounds.max);
            var framing = MobileBoardFraming.Frame(new Vector2(BoardWidth, BoardHeight),
                Mathf.Max(config.glassFramePadding, config.outerGlassThickness * 0.5f),
                config.railGap + config.magnetVisualSize * 0.5f,
                Rect.MinMaxRect(min.x, min.y, max.x, max.y));
            var worldCenter = transform.TransformPoint(framing.center);
            var worldSize = Vector2.Scale(framing.size, new Vector2(Mathf.Abs(transform.lossyScale.x),
                Mathf.Abs(transform.lossyScale.y)));
            var vertical = worldSize.y * 0.5f / Mathf.Max(0.1f, boardArea.height / screenHeight);
            var horizontal = worldSize.x * 0.5f /
                (Mathf.Max(0.1f, boardArea.width / screenWidth) * Mathf.Max(0.4f, camera.aspect));
            camera.orthographicSize = Mathf.Max(vertical, horizontal);
            var position = camera.transform.position;
            position.x = worldCenter.x - (boardArea.center.x / screenWidth - 0.5f) *
                camera.orthographicSize * 2f * camera.aspect;
            position.y = worldCenter.y - (boardArea.center.y / screenHeight - 0.5f) *
                camera.orthographicSize * 2f;
            camera.transform.position = position;
            lastContentRect = content;
            lastDisplaySize = new Vector2Int(Screen.width, Screen.height);
            lastHudBottom = bottom;
            if (chapterBackgroundTransform != null)
            {
                // Fit in the board parent's coordinates. Assigning world viewport
                // dimensions to localScale leaves uncovered edges on scaled boards.
                var backgroundPlane = transform.TransformPoint(new Vector3(0f, 0f, 2f));
                var depth = Vector3.Dot(backgroundPlane - camera.transform.position, camera.transform.forward);
                var minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                var maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                const float overscan = 0.005f;
                for (var y = 0; y < 2; y++)
                for (var x = 0; x < 2; x++)
                {
                    var worldCorner = camera.ViewportToWorldPoint(new Vector3(
                        x == 0 ? -overscan : 1f + overscan, y == 0 ? -overscan : 1f + overscan, depth));
                    var localCorner = (Vector2)transform.InverseTransformPoint(worldCorner);
                    minimum = Vector2.Min(minimum, localCorner);
                    maximum = Vector2.Max(maximum, localCorner);
                }
                var backgroundCenter = (minimum + maximum) * 0.5f;
                var backgroundSize = maximum - minimum;
                chapterBackgroundTransform.localPosition = new Vector3(backgroundCenter.x, backgroundCenter.y, 2f);
                chapterBackgroundTransform.localRotation = Quaternion.identity;
                chapterBackgroundTransform.localScale = new Vector3(backgroundSize.x, backgroundSize.y, 1f);
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite != null) return whiteSprite;
                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "Runtime White Pixel" };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply(false, true);
                whiteSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
                whiteSprite.name = "Runtime White Sprite";
                return whiteSprite;
            }
        }

        private static Sprite CircleSprite
        {
            get
            {
                if (circleSprite != null) return circleSprite;
                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    name = "Runtime Circle",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                var pixels = new Color[size * size];
                var center = (size - 1) * 0.5f;
                var radius = size * 0.5f;
                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / radius;
                    var alpha = 1f - Mathf.SmoothStep(0.88f, 1f, distance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                texture.SetPixels(pixels);
                texture.Apply(false, true);
                circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
                circleSprite.name = "Runtime Circle Sprite";
                return circleSprite;
            }
        }

        private static Sprite RoundedWallSprite
        {
            get
            {
                if (roundedWallSprite != null) return roundedWallSprite;
                const int size = 64;
                const int radius = 10;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    name = "Runtime Rounded Wall",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                var pixels = new Color[size * size];
                var innerMin = radius - 0.5f;
                var innerMax = size - radius - 0.5f;
                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var nearestX = Mathf.Clamp(x, innerMin, innerMax);
                    var nearestY = Mathf.Clamp(y, innerMin, innerMax);
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(nearestX, nearestY));
                    var alpha = 1f - Mathf.SmoothStep(radius - 1.5f, radius + 0.5f, distance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                texture.SetPixels(pixels);
                texture.Apply(false, true);
                roundedWallSprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f,
                    size, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
                roundedWallSprite.name = "Runtime Rounded Wall Sprite";
                return roundedWallSprite;
            }
        }

        private static Sprite RadialGlowSprite
        {
            get
            {
                if (radialGlowSprite != null) return radialGlowSprite;
                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    name = "Runtime Goal Glow",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                var pixels = new Color[size * size];
                var center = Vector2.one * ((size - 1) * 0.5f);
                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var radius = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                    var fade = 1f - Mathf.SmoothStep(0.05f, 1f, radius);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, fade * fade);
                }
                texture.SetPixels(pixels);
                texture.Apply(false, true);
                radialGlowSprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
                radialGlowSprite.name = "Runtime Goal Glow Sprite";
                return radialGlowSprite;
            }
        }
    }
}
