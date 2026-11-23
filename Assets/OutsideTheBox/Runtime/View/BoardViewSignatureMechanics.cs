using System;
using System.Collections.Generic;
using UnityEngine;

namespace OutsideTheBox
{
    /// <summary>
    /// Signature mechanics layered over the continuous board simulation. Every visual is
    /// instantiated from RuntimePrefabRegistry; no mechanic creates an untracked GameObject.
    /// </summary>
    public partial class BoardViewImplementation
    {
        private readonly struct EchoSample
        {
            public readonly Vector2 position;
            public readonly float time;
            public EchoSample(Vector2 position, float time) { this.position = position; this.time = time; }
        }

        private sealed class ImprintRuntime
        {
            public MagneticImprintDefinition definition;
            public SpriteRenderer renderer;
            public float activeUntil;
        }

        private sealed class LensRuntime
        {
            public FieldLensDefinition definition;
            public Transform visual;
        }

        private sealed class LockRuntime
        {
            public PolarityLockDefinition definition;
            public Transform visual;
            public SpriteRenderer renderer;
            public SpriteRenderer[] sequence;
            public SpriteRenderer badge;
            public int progress;
            public bool unlocked;
        }

        private readonly List<EchoSample> echoSamples = new();
        private readonly List<ImprintRuntime> imprintRuntimes = new();
        private readonly List<LensRuntime> lensRuntimes = new();
        private readonly List<LockRuntime> lockRuntimes = new();
        private readonly List<Vector3> ferrofluidPoints = new();
        private Vector3[] ferrofluidUploadBuffer = Array.Empty<Vector3>();
        private Transform echoMagnetTransform;
        private LineRenderer echoTrail;
        private LineRenderer ferrofluidTrail;
        private Transform orbitRingTransform;
        private SpriteRenderer orbitRingRenderer;
        private bool echoRecording;
        private bool echoReady;
        private float echoRecordStartedAt;
        private float nextEchoSampleAt;
        private float echoPlaybackStartedAt;
        private ForceMode echoMode;
        private float orbitSignedDegrees;
        private float orbitLastAngle;
        private bool orbitTracking;
        private bool orbitUnlocked;
        public bool RequiresOrbit { get; private set; }

        public bool EchoAvailable => SignatureEnabled && level != null && level.echoMagnetEnabled &&
                                     LiveOpsConfigStore.Current.echoMagnetEnabled;
        public bool IsEchoRecording => echoRecording;
        public bool HasEcho => echoReady;
        public ForceMode EchoForceMode => echoMode;
        public float OrbitProgress => !RequiresOrbit || orbitUnlocked
            ? 1f
            : Mathf.Clamp01(Mathf.Abs(orbitSignedDegrees) / (Mathf.Max(0.25f, level.orbitGoalTurns) * 360f));
        private bool SignatureEnabled => LiveOpsConfigStore.Current?.signatureMechanicsEnabled ?? true;

        public bool BeginEchoRecording()
        {
            if (!EchoAvailable || simulationPaused || exitReached) return false;
            echoSamples.Clear();
            echoRecording = true;
            echoReady = false;
            echoMode = forceMode;
            echoRecordStartedAt = Time.unscaledTime;
            nextEchoSampleAt = echoRecordStartedAt;
            SetEchoVisual(false);
            CaptureEchoSample(true);
            return true;
        }

        public bool EndEchoRecording()
        {
            if (!echoRecording) return false;
            CaptureEchoSample(true);
            echoRecording = false;
            echoReady = echoSamples.Count >= 2 && echoSamples[^1].time >= 0.12f;
            echoPlaybackStartedAt = Time.unscaledTime;
            RebuildEchoTrail();
            SetEchoVisual(echoReady);
            return echoReady;
        }

        public void ClearEcho()
        {
            echoSamples.Clear();
            echoRecording = false;
            echoReady = false;
            if (echoTrail != null) echoTrail.positionCount = 0;
            SetEchoVisual(false);
        }

        public void NotifyPolarityChanged(ForceMode mode)
        {
            if (!SignatureEnabled || !LiveOpsConfigStore.Current.polarityLocksEnabled) return;
            for (var i = 0; i < lockRuntimes.Count; i++)
            {
                var item = lockRuntimes[i];
                if (item.unlocked) continue;
                var expected = item.definition.ModeAt(item.progress);
                item.progress = mode == expected
                    ? item.progress + 1
                    : mode == item.definition.ModeAt(0) ? 1 : 0;
                if (item.progress >= Mathf.Max(1, item.definition.patternLength))
                {
                    item.unlocked = true;
                    RefreshLockLabel(item);
                    if (item.renderer != null) item.renderer.color = new Color(0.25f, 1f, 0.55f, 0.25f);
                }
                else
                {
                    RefreshLockLabel(item);
                }
            }
        }

        private void InitializeSignatureMechanics()
        {
            ResetSignatureMechanics();
            if (!SignatureEnabled || level == null) return;
            BuildEchoVisuals();
            BuildMagneticImprints();
            BuildFieldLenses();
            BuildFerrofluidTrail();
            BuildPolarityLocks();
            BuildOrbitGoal();
        }

        private void ResetSignatureMechanics()
        {
            echoSamples.Clear();
            imprintRuntimes.Clear();
            lensRuntimes.Clear();
            lockRuntimes.Clear();
            ferrofluidPoints.Clear();
            echoMagnetTransform = null;
            echoTrail = null;
            ferrofluidTrail = null;
            orbitRingTransform = null;
            orbitRingRenderer = null;
            echoRecording = false;
            echoReady = false;
            orbitSignedDegrees = 0f;
            orbitTracking = false;
            RequiresOrbit = false;
            orbitUnlocked = level == null || !level.orbitGoalEnabled;
        }

        private void BuildEchoVisuals()
        {
            if (!level.echoMagnetEnabled) return;
            var ghost = prefabs.Spawn(prefabs.magnetPrefab, "Echo Magnet", transform);
            spawned.Add(ghost);
            echoMagnetTransform = ghost.transform;
            echoMagnetTransform.localScale = Vector3.one * config.magnetVisualSize * 0.92f;
            var renderers = ghost.GetComponentsInChildren<SpriteRenderer>(true);
            for (var i = 0; i < renderers.Length; i++) renderers[i].color = config.echoMagnetColor;
            var lineObject = prefabs.Spawn(prefabs.magneticFieldPrefab, "Echo Recorded Path", transform);
            spawned.Add(lineObject);
            echoTrail = lineObject.GetComponent<LineRenderer>();
            echoTrail.useWorldSpace = false;
            echoTrail.widthMultiplier = config.fieldLineWidth * 1.65f;
            echoTrail.startColor = config.echoMagnetColor;
            echoTrail.endColor = new Color(config.echoMagnetColor.r, config.echoMagnetColor.g, config.echoMagnetColor.b, 0.08f);
            echoTrail.positionCount = 0;
            SetEchoVisual(false);
        }

        private void BuildMagneticImprints()
        {
            if (!LiveOpsConfigStore.Current.magneticImprintsEnabled || level.magneticImprints == null) return;
            for (var i = 0; i < level.magneticImprints.Count; i++)
            {
                var definition = level.magneticImprints[i];
                var color = definition.polarity == ForceMode.Pull
                    ? config.magneticImprintPullColor
                    : config.magneticImprintPushColor;
                var visual = MakeSprite($"Magnetic Imprint {i + 1}", staticContainer, color, 10, prefabs.goalGlowPrefab);
                var renderer = visual.GetComponent<SpriteRenderer>();
                renderer.sprite = RadialGlowSprite;
                visual.transform.localPosition = (Vector3)definition.center + Vector3.back * 0.14f;
                visual.transform.localScale = Vector3.one * (config.wallThickness * 2.3f);
                AddMechanicIcon(visual.transform, MechanicIconId.Imprint, .34f);
                imprintRuntimes.Add(new ImprintRuntime { definition = definition, renderer = renderer });
            }
        }

        private void BuildFieldLenses()
        {
            if (!LiveOpsConfigStore.Current.fieldLensesEnabled || level.fieldLenses == null) return;
            for (var i = 0; i < level.fieldLenses.Count; i++)
            {
                var definition = level.fieldLenses[i];
                var visual = MakeSprite($"Field Lens {i + 1}", staticContainer,
                    new Color(config.fieldLensColor.r, config.fieldLensColor.g, config.fieldLensColor.b, 0.3f), 2,
                    prefabs.goalGlowPrefab);
                visual.GetComponent<SpriteRenderer>().sprite = RadialGlowSprite;
                visual.transform.localPosition = (Vector3)definition.center + Vector3.forward * 0.08f;
                visual.transform.localScale = Vector3.one * definition.radius * 2f;
                BuildMechanicRing(visual.transform, config.fieldLensColor, 0.46f);
                AddMechanicIcon(visual.transform, MechanicIconId.Lens, .46f);
                lensRuntimes.Add(new LensRuntime { definition = definition, visual = visual.transform });
            }
        }

        private void BuildFerrofluidTrail()
        {
            if (!level.ferrofluidMemoryEnabled || !LiveOpsConfigStore.Current.ferrofluidMemoryEnabled) return;
            var lineObject = prefabs.Spawn(prefabs.magneticFieldPrefab, "Ferrofluid Memory Trail", transform);
            spawned.Add(lineObject);
            ferrofluidTrail = lineObject.GetComponent<LineRenderer>();
            ferrofluidTrail.useWorldSpace = false;
            ferrofluidTrail.widthMultiplier = config.fieldLineWidth * 2.2f;
            ferrofluidTrail.startColor = config.ferrofluidTrailColor;
            ferrofluidTrail.endColor = new Color(config.ferrofluidTrailColor.r, config.ferrofluidTrailColor.g,
                config.ferrofluidTrailColor.b, 0.04f);
            ferrofluidTrail.positionCount = 0;
        }

        private void BuildPolarityLocks()
        {
            if (!LiveOpsConfigStore.Current.polarityLocksEnabled || level.polarityLocks == null) return;
            for (var i = 0; i < level.polarityLocks.Count; i++)
            {
                var definition = level.polarityLocks[i];
                var visual = MakeSprite($"Polarity Lock {i + 1}", staticContainer, config.polarityLockColor, 9, prefabs.wallPrefab);
                visual.transform.localPosition = (Vector3)definition.center + Vector3.back * 0.04f;
                visual.transform.localScale = definition.horizontal
                    ? new Vector3(definition.length, config.wallThickness, 1f)
                    : new Vector3(config.wallThickness, definition.length, 1f);
                var runtime = new LockRuntime { definition = definition, visual = visual.transform,
                    renderer = visual.GetComponent<SpriteRenderer>(),
                    sequence = new SpriteRenderer[Mathf.Max(1, definition.patternLength)] };
                runtime.badge = AddMechanicIcon(visual.transform, MechanicIconId.PolarityLock, .3f);
                if (runtime.badge != null) runtime.badge.transform.position = visual.transform.position +
                    transform.TransformVector(new Vector3(0f, CellSize * .28f, -.2f));
                for (var step = 0; step < runtime.sequence.Length; step++)
                {
                    var icon = AddMechanicIcon(visual.transform,
                        definition.ModeAt(step) == ForceMode.Pull ? MechanicIconId.Pull : MechanicIconId.Push, .22f);
                    if (icon != null) icon.transform.position = visual.transform.position + transform.TransformVector(
                        new Vector3((step - (runtime.sequence.Length - 1) * .5f) * CellSize * .26f, 0f, -.22f));
                    runtime.sequence[step] = icon;
                }
                RefreshLockLabel(runtime);
                lockRuntimes.Add(runtime);
            }
        }

        private void BuildMechanicRing(Transform parent, Color color, float radius)
        {
            var item = prefabs.Spawn(prefabs.magneticFieldPrefab, "Mechanic Boundary", parent);
            item.transform.localPosition = Vector3.back * 0.05f;
            item.transform.localRotation = Quaternion.identity;
            item.transform.localScale = Vector3.one;
            var line = item.GetComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 32;
            line.widthMultiplier = Mathf.Max(0.012f, config.fieldLineWidth * 0.65f);
            color.a = 0.72f;
            line.startColor = color;
            line.endColor = color;
            line.sortingOrder = 3;
            for (var i = 0; i < line.positionCount; i++)
            {
                var angle = i * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }

        private void BuildOrbitGoal()
        {
            RequiresOrbit = level.orbitGoalEnabled && LiveOpsConfigStore.Current.orbitGoalsEnabled &&
                OrbitGoalRules.HasClearance(CellLocal(level.exit), new Vector2(BoardWidth, BoardHeight),
                    config.ballRadius, config.exitRadius,
                    LiveOpsConfigStore.Current.orbitGoalRadiusMultiplier, wallCollisionRects);
            orbitUnlocked = !RequiresOrbit;
            if (orbitUnlocked) return;
            var radius = config.exitRadius * LiveOpsConfigStore.Current.orbitGoalRadiusMultiplier;
            var ring = MakeSprite("Orbit Goal Ring", staticContainer,
                new Color(config.orbitGoalColor.r, config.orbitGoalColor.g, config.orbitGoalColor.b, 0.28f), 2,
                prefabs.goalGlowPrefab);
            var icon = AddMechanicIcon(staticContainer, MechanicIconId.Orbit, .38f);
            if (icon != null) icon.transform.localPosition = CellLocal(level.exit) + new Vector3(0f, radius * .85f, -.2f);
            orbitRingTransform = ring.transform;
            orbitRingRenderer = ring.GetComponent<SpriteRenderer>();
            orbitRingRenderer.sprite = RadialGlowSprite;
            orbitRingTransform.localPosition = CellLocal(level.exit) + Vector3.forward * 0.1f;
            orbitRingTransform.localScale = Vector3.one * radius * 2f;
        }

        private void UpdateSignatureRecording()
        {
            if (!echoRecording) return;
            CaptureEchoSample(false);
            if (Time.unscaledTime - echoRecordStartedAt >= LiveOpsConfigStore.Current.echoRecordSeconds)
                EndEchoRecording();
        }

        private void CaptureEchoSample(bool force)
        {
            if (!echoRecording || magnetTransform == null) return;
            var settings = LiveOpsConfigStore.Current;
            if (!force && Time.unscaledTime < nextEchoSampleAt) return;
            nextEchoSampleAt = Time.unscaledTime + 1f / Mathf.Max(5, settings.echoSampleRate);
            var elapsed = Mathf.Min(settings.echoRecordSeconds, Time.unscaledTime - echoRecordStartedAt);
            var position = (Vector2)magnetTransform.localPosition;
            if (!force && echoSamples.Count > 0 && (echoSamples[^1].position - position).sqrMagnitude < 0.0004f) return;
            if (echoSamples.Count >= settings.echoMaximumSamples) echoSamples.RemoveAt(0);
            echoSamples.Add(new EchoSample(position, elapsed));
        }

        private void RebuildEchoTrail()
        {
            if (echoTrail == null) return;
            echoTrail.positionCount = echoSamples.Count;
            for (var i = 0; i < echoSamples.Count; i++)
                echoTrail.SetPosition(i, new Vector3(echoSamples[i].position.x, echoSamples[i].position.y, -0.12f));
        }

        private void SetEchoVisual(bool visible)
        {
            if (echoMagnetTransform != null) echoMagnetTransform.gameObject.SetActive(visible);
            if (echoTrail != null) echoTrail.enabled = visible;
        }

        private void ApplySignatureForces(float deltaTime)
        {
            if (!SignatureEnabled) return;
            var settings = LiveOpsConfigStore.Current;
            if (echoReady && settings.echoMagnetEnabled && TryGetEchoPosition(out var echoPosition))
            {
                echoMagnetTransform.localPosition = new Vector3(echoPosition.x, echoPosition.y, -0.32f);
                ballVelocity += CalculateSignatureAcceleration(echoPosition, echoMode, settings.echoStrengthMultiplier) * deltaTime;
            }

            if (settings.magneticImprintsEnabled)
            {
                for (var i = 0; i < imprintRuntimes.Count; i++)
                {
                    var imprint = imprintRuntimes[i];
                    if (Time.unscaledTime < imprint.activeUntil)
                        ballVelocity += CalculateSignatureAcceleration(imprint.definition.center,
                            imprint.definition.polarity, settings.magneticImprintStrength / Mathf.Max(0.1f, config.magneticAcceleration)) * deltaTime;
                }
            }

            if (level.ferrofluidMemoryEnabled && settings.ferrofluidMemoryEnabled && magnetForceActive && ferrofluidPoints.Count >= 2)
            {
                var tangent = (Vector2)(ferrofluidPoints[^1] - ferrofluidPoints[^2]);
                if (tangent.sqrMagnitude > 0.0001f)
                    ballVelocity += tangent.normalized * settings.ferrofluidGuidanceStrength * deltaTime;
            }
        }

        private bool HasActiveSignatureForces()
        {
            if (!SignatureEnabled) return false;
            if (echoReady && LiveOpsConfigStore.Current.echoMagnetEnabled) return true;
            if (!LiveOpsConfigStore.Current.magneticImprintsEnabled) return false;
            for (var i = 0; i < imprintRuntimes.Count; i++)
                if (Time.unscaledTime < imprintRuntimes[i].activeUntil) return true;
            return false;
        }

        private Vector2 CalculateSignatureAcceleration(Vector2 source, ForceMode mode, float multiplier)
        {
            var toSource = source - ballPosition;
            var distance = Mathf.Max(0.35f, toSource.magnitude);
            var direction = toSource / distance;
            if (mode == ForceMode.Push) direction = -direction;
            direction = TransformDirectionThroughLenses(direction);
            var boardSpan = Mathf.Max(BoardWidth, BoardHeight);
            var distanceScale = Mathf.Lerp(1.35f, 0.72f, Mathf.Clamp01(distance / boardSpan));
            var strength = chapterSettings != null ? chapterSettings.SafeMagneticStrength : 1f;
            if (level.fieldZones != null)
            {
                for (var i = 0; i < level.fieldZones.Count; i++)
                {
                    var zone = level.fieldZones[i];
                    if ((ballPosition - zone.center).sqrMagnitude <= zone.radius * zone.radius)
                        strength *= Mathf.Max(0.15f, zone.strengthMultiplier);
                }
            }
            return direction * (config.magneticAcceleration * strength * distanceScale * multiplier);
        }

        private Vector2 TransformDirectionThroughLenses(Vector2 direction)
        {
            if (!LiveOpsConfigStore.Current.fieldLensesEnabled) return direction;
            for (var i = 0; i < lensRuntimes.Count; i++)
            {
                var lens = lensRuntimes[i].definition;
                if ((ballPosition - lens.center).sqrMagnitude > lens.radius * lens.radius) continue;
                var rotated = lens.quarterTurns >= 0
                    ? new Vector2(-direction.y, direction.x)
                    : new Vector2(direction.y, -direction.x);
                direction = Vector2.Lerp(direction, rotated, LiveOpsConfigStore.Current.fieldLensDirectionBlend).normalized;
            }
            return direction;
        }

        private bool TryGetEchoPosition(out Vector2 position)
        {
            position = default;
            if (!echoReady || echoSamples.Count < 2) return false;
            var duration = echoSamples[^1].time;
            var cycle = duration + LiveOpsConfigStore.Current.echoLoopDelay;
            if (duration <= 0f || cycle <= 0f) return false;
            var time = (Time.unscaledTime - echoPlaybackStartedAt) % cycle;
            if (time > duration)
            {
                if (echoMagnetTransform != null) echoMagnetTransform.gameObject.SetActive(false);
                return false;
            }
            if (echoMagnetTransform != null && !echoMagnetTransform.gameObject.activeSelf)
                echoMagnetTransform.gameObject.SetActive(true);
            for (var i = 1; i < echoSamples.Count; i++)
            {
                if (echoSamples[i].time < time) continue;
                var previous = echoSamples[i - 1];
                var next = echoSamples[i];
                var t = Mathf.InverseLerp(previous.time, next.time, time);
                position = Vector2.Lerp(previous.position, next.position, t);
                return true;
            }
            position = echoSamples[^1].position;
            return true;
        }

        private void PostMoveSignatureUpdate(Vector2 previousPosition)
        {
            if (!SignatureEnabled) return;
            var settings = LiveOpsConfigStore.Current;
            if (settings.magneticImprintsEnabled)
            {
                var activationRadius = settings.magneticImprintActivationRadius;
                for (var i = 0; i < imprintRuntimes.Count; i++)
                {
                    var imprint = imprintRuntimes[i];
                    if ((ballPosition - imprint.definition.center).sqrMagnitude <= activationRadius * activationRadius)
                        imprint.activeUntil = Mathf.Max(imprint.activeUntil, Time.unscaledTime + settings.magneticImprintDuration);
                }
            }

            if (ferrofluidTrail != null && (ballPosition - previousPosition).sqrMagnitude > 0f)
            {
                var spacing = settings.ferrofluidPointSpacing;
                if (ferrofluidPoints.Count == 0 || ((Vector2)ferrofluidPoints[^1] - ballPosition).sqrMagnitude >= spacing * spacing)
                {
                    if (ferrofluidPoints.Count >= settings.ferrofluidMaximumPoints) ferrofluidPoints.RemoveAt(0);
                    ferrofluidPoints.Add(new Vector3(ballPosition.x, ballPosition.y, -0.08f));
                    ferrofluidTrail.positionCount = ferrofluidPoints.Count;
                    if (ferrofluidUploadBuffer.Length < ferrofluidPoints.Count)
                        ferrofluidUploadBuffer = new Vector3[Mathf.Max(ferrofluidPoints.Count, settings.ferrofluidMaximumPoints)];
                    ferrofluidPoints.CopyTo(ferrofluidUploadBuffer);
                    // LineRenderer uploads only positionCount entries from this buffer.
                    ferrofluidTrail.SetPositions(ferrofluidUploadBuffer);
                }
            }
            UpdateOrbitProgress();
        }

        private void UpdateOrbitProgress()
        {
            if (orbitUnlocked || !level.orbitGoalEnabled || !LiveOpsConfigStore.Current.orbitGoalsEnabled) return;
            var center = (Vector2)CellLocal(level.exit);
            var delta = ballPosition - center;
            var ringRadius = config.exitRadius * LiveOpsConfigStore.Current.orbitGoalRadiusMultiplier;
            var insideRing = delta.magnitude <= ringRadius && delta.magnitude >= config.exitRadius * 0.85f &&
                             ballVelocity.magnitude >= LiveOpsConfigStore.Current.orbitGoalMinimumSpeed;
            if (!insideRing)
            {
                orbitTracking = false;
                return;
            }
            var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            if (orbitTracking)
            {
                var turn = Mathf.DeltaAngle(orbitLastAngle, angle);
                if (Mathf.Abs(turn) <= 60f) orbitSignedDegrees += turn;
            }
            orbitLastAngle = angle;
            orbitTracking = true;
            if (Mathf.Abs(orbitSignedDegrees) >= Mathf.Max(0.25f, level.orbitGoalTurns) * 360f)
            {
                orbitUnlocked = true;
                if (orbitRingRenderer != null) orbitRingRenderer.color = config.orbitGoalColor;
            }
        }

        private bool SignatureGoalAllowsEntry()
        {
            return !SignatureEnabled || !RequiresOrbit || !LiveOpsConfigStore.Current.orbitGoalsEnabled || orbitUnlocked;
        }

        private void ResolveSignatureCollisions(ref bool collided)
        {
            if (!SignatureEnabled || !LiveOpsConfigStore.Current.polarityLocksEnabled) return;
            for (var i = 0; i < lockRuntimes.Count; i++)
            {
                var item = lockRuntimes[i];
                if (item.unlocked) continue;
                var rect = CreateWallCollisionRect(item.definition.center, item.definition.length, item.definition.horizontal);
                collided |= ResolveCircleRect(rect);
            }
        }

        private void AnimateSignatureVisuals()
        {
            if (!SignatureEnabled) return;
            var time = Time.unscaledTime;
            var settings = LiveOpsConfigStore.Current;
            for (var i = 0; i < imprintRuntimes.Count; i++)
            {
                var imprint = imprintRuntimes[i];
                if (imprint.renderer == null) continue;
                var active = Time.unscaledTime < imprint.activeUntil;
                var baseColor = imprint.definition.polarity == ForceMode.Pull
                    ? config.magneticImprintPullColor
                    : config.magneticImprintPushColor;
                baseColor.a = active ? 0.72f + Mathf.Sin(time * settings.imprintPulseSpeed) * 0.2f : 0.22f;
                imprint.renderer.color = baseColor;
            }
            for (var i = 0; i < lensRuntimes.Count; i++)
                if (lensRuntimes[i].visual != null && !(SaveService.Data?.reduceMotion ?? false))
                    lensRuntimes[i].visual.localRotation = Quaternion.Euler(0f, 0f,
                        time * settings.fieldLensRotationSpeed * lensRuntimes[i].definition.quarterTurns);
            if (orbitRingTransform != null && !(SaveService.Data?.reduceMotion ?? false))
            {
                var pulse = 1f + Mathf.Sin(time * settings.orbitGoalPulseSpeed) * settings.orbitGoalPulseAmount;
                var radius = config.exitRadius * settings.orbitGoalRadiusMultiplier;
                orbitRingTransform.localScale = Vector3.one * radius * 2f * pulse;
                orbitRingTransform.localRotation = Quaternion.Euler(0f, 0f, time * settings.orbitGoalRotationSpeed);
            }
        }

        private static void RefreshLockLabel(LockRuntime item)
        {
            if (item.sequence == null) return;
            for (var i = 0; i < item.sequence.Length; i++)
                if (item.sequence[i] != null) item.sequence[i].color = item.unlocked || i < item.progress
                    ? new Color(.3f, 1f, .55f, .75f) : Color.white;
            if (item.badge != null) item.badge.color = item.unlocked ? new Color(1f, 1f, 1f, .3f) : Color.white;
        }
    }
}
