using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace OutsideTheBox
{
    public sealed class LevelPipeTransition3D : LevelPipeTransitionBase
    {
        public RawImage viewport;
        public Transform stage;
        public Camera renderCamera;
        public MeshFilter shell, lining, glassCover, collars, sphere;
        public MeshRenderer sphereRenderer;
        public LevelBoardDiorama board;
        [Header("Board Cinematic")]
        [Min(0.1f)] public float boardTiltSeconds = 0.5f;
        [Min(0.1f)] public float boardEnterSeconds = 0.42f;
        [Min(0.1f)] public float boardSettleSeconds = 0.55f;
        [Range(256, 1536)] public int maximumRenderSize = 1024;
        [Header("Under-board Departure")]
        [Min(0.05f)] public float pipeRevealSeconds = 0.28f;
        [Min(0.05f)] public float undergroundEntrySeconds = 0.22f;
        private RenderTexture target;
        private Mesh shellMesh, liningMesh, glassMesh, collarMesh, ballMesh;
        private MaterialPropertyBlock ballProperties;
        private readonly Vector3[] curve = new Vector3[4];
        private Vector2 viewportOffsetMin, viewportOffsetMax;
        private Rect viewportUv;
        private float viewportHeight, lastReveal = -1f;
        private bool viewportCaptured;
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int MainTexSTId = Shader.PropertyToID("_MainTex_ST");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public void SetBoardSource(Transform root, Vector2 localSize, Vector3 hole, float radius)
        {
            if (board == null)
            {
                Debug.LogWarning("The 3D transition is missing its board presentation prefab.");
                return;
            }
            board.SetSource(root, localSize, hole, radius);
        }

        public void HideBoardUntilArrival() => board?.HideSourceUntilCapture();

        public override IEnumerator Play(bool arriving, Camera camera, SpriteRenderer source,
            Vector3 worldPoint, float worldDiameter, Func<bool> blocked, Action landed = null)
        {
            Completed = false;
            if (camera == null || source == null || group == null || viewport == null || stage == null ||
                renderCamera == null || shell == null || lining == null || glassCover == null ||
                collars == null || sphere == null || sphereRenderer == null) yield break;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            group.alpha = 0f;
            group.blocksRaycasts = true;
            group.interactable = false;
            Canvas.ForceUpdateCanvases();
            var root = (RectTransform)transform;
            var bounds = root.rect;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, camera.WorldToScreenPoint(worldPoint), null, out var point);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root,
                camera.WorldToScreenPoint(worldPoint + Vector3.up * worldDiameter * 0.5f), null, out var top);
            var withBoard = board != null && board.HasSource;
            var diameter = Mathf.Clamp(Vector2.Distance(point, top) * 2f, withBoard ? 4f : 28f, withBoard ? 160f : 76f);
            var width = Mathf.Clamp(diameter * 1.75f, 66f, 120f);
            var startUI = arriving ? new Vector2(point.x * 0.25f, bounds.yMax + width) : point;
            var endUI = arriving ? point : new Vector2(bounds.center.x, bounds.yMin - width);
            var travel = Mathf.Abs(startUI.y - endUI.y) * 0.4f;
            PrepareCamera(bounds);
            var hasBoard = board != null && board.Prepare(camera, root, stage, renderCamera, maximumRenderSize, arriving);
            var side = arriving ? Vector2.up : Vector2.down;
            if (hasBoard)
            {
                // Gravity always carries the ball down: enter from above the
                // spawn and leave below the hole, regardless of their board edge.
                board.SetRouteSide(side, arriving);
                if (arriving) startUI = OutsidePoint(point, bounds, side, width);
                else endUI = OutsidePoint(point, bounds, side, width);
                travel = Vector2.Distance(startUI, endUI) * 0.4f;
            }
            CaptureViewport();
            SetPipeReveal(hasBoard ? 0f : arriving ? 1f : 0f);
            if (hasBoard) { board.SetPose(arriving ? 1f : 0f); board.SetBoardEntry(arriving ? 0f : 1f); }
            var start = ToStage(startUI, bounds);
            var end = ToStage(endUI, bounds);
            var b = ToStage(startUI + Vector2.down * travel, bounds);
            var c = ToStage(endUI + Vector2.up * travel, bounds);
            var bend = Mathf.Clamp(Vector3.Distance(start, end) * 0.24f, 0.55f, 2.5f);
            var radius = Vector3.Distance(ToStage(point + Vector2.up * diameter * 0.5f, bounds), ToStage(point, bounds));
            if (arriving)
            {
                if (hasBoard)
                {
                    end = ToStage(endUI, bounds, -radius);
                    start = ToStage(startUI, bounds, -radius - bend * 0.5f);
                    b = ToStage(startUI - side * travel, bounds, -radius - bend);
                    c = end + Vector3.back * Mathf.Max(bend, radius * 3f);
                }
                else { b.z = -bend; c.z = bend * 0.65f; }
            }
            else
            {
                // The board is viewed from above: entering its hole means moving
                // away from the camera, through the board, before turning down.
                // Keep the entire outlet below the board plane, with its mouth
                // projected exactly beneath the original 2D pocket.
                var inletDepth = hasBoard ? board.Thickness + radius * 0.2f : radius * 2f;
                var outletDepth = inletDepth + bend * 0.65f;
                start = hasBoard ? ToStage(point, bounds) + Vector3.forward * inletDepth
                    : ToStage(startUI, bounds, inletDepth);
                b = start + Vector3.forward * Mathf.Max(radius * 4f, bend);
                end = ToStage(endUI, bounds, outletDepth);
                c = ToStage(endUI - side * travel, bounds, outletDepth);
            }
            if (hasBoard)
            {
                // The board tilt changes the pipe's screen projection. Extend its
                // outer end in that final 3D space, so no open mouth floats inside
                // the screen on either departure or arrival.
                if (arriving)
                {
                    var extended = ExtendToScreenEdge(start, side);
                    b += extended - start;
                    start = extended;
                    // Perspective can turn the upper bend slightly uphill.
                    // Keep its control point between the inlet and landing bend
                    // in screen height, without changing the landing position.
                    board.SetPose(1f);
                    var inlet = renderCamera.WorldToViewportPoint(board.transform.TransformPoint(start));
                    var control = renderCamera.WorldToViewportPoint(board.transform.TransformPoint(b));
                    var landing = renderCamera.WorldToViewportPoint(board.transform.TransformPoint(c));
                    control.y = Mathf.Clamp(control.y, landing.y, inlet.y);
                    b = board.transform.InverseTransformPoint(renderCamera.ViewportToWorldPoint(control));
                }
                else
                {
                    var extended = ExtendToScreenEdge(end, side);
                    c += extended - end;
                    end = extended;
                }
                board.SetPose(arriving ? 1f : 0f);
            }
            curve[0] = start; curve[1] = b; curve[2] = c; curve[3] = end;
            BuildMeshes(curve, radius * 1.75f);
            if (hasBoard) board.SetPipeBounds(start, end, radius * 1.75f);
            ConfigureBall(source);
            sphere.gameObject.SetActive(arriving && !hasBoard);
            var duration = Mathf.Max(0.2f, arriving ? arrivalSeconds : departureSeconds);
            var fade = Mathf.Clamp(fadeSeconds, 0.01f, duration * 0.2f);
            var movement = duration - fade;
            var revealTime = arriving ? 0f : Mathf.Clamp(pipeRevealSeconds, 0.01f, movement * 0.3f);
            var entryTime = arriving ? 0f : Mathf.Clamp(undergroundEntrySeconds, 0.01f, movement * 0.25f);
            var tiltTime = hasBoard && !arriving ? Mathf.Clamp(boardTiltSeconds, 0.01f, movement * 0.24f) : 0f;
            var enterTime = hasBoard && arriving ? Mathf.Clamp(boardEnterSeconds, 0.01f, movement * 0.2f) : 0f;
            var settleTime = hasBoard && arriving ? Mathf.Clamp(boardSettleSeconds, 0.01f, movement * 0.26f) : 0f;
            if (hasBoard && arriving) revealTime = Mathf.Clamp(pipeRevealSeconds, 0.01f, movement * 0.16f);
            var elapsed = 0f;
            var didLand = false;
            while (elapsed < duration)
            {
                if (blocked?.Invoke() == true) { yield return null; continue; }
                elapsed += Mathf.Min(0.05f, Time.unscaledDeltaTime);
                ApplyCinematicFrame(arriving, hasBoard, elapsed, duration, radius, fade, movement,
                    revealTime, entryTime, tiltTime, enterTime, settleTime, ref didLand, landed);
                yield return null;
            }
            Completed = true;
            Hide();
        }

        private void ApplyCinematicFrame(bool arriving, bool hasBoard, float elapsed, float duration,
            float radius, float fade, float movement, float revealTime, float entryTime,
            float tiltTime, float enterTime, float settleTime, ref bool didLand, Action landed)
        {
            var t = Mathf.Clamp01(elapsed / movement);
            var progress = Smooth(t);
            if (hasBoard && arriving)
            {
                var settleStart = movement - settleTime;
                var settling = Smooth((elapsed - settleStart) / settleTime);
                board.SetPose(1f - settling);
                board.SetBoardEntry(Smooth((elapsed - revealTime) / enterTime));
                SetPipeReveal(Smooth(elapsed / revealTime) * (1f - settling));
                var travelStart = revealTime + enterTime;
                progress = Smooth((elapsed - travelStart) / Mathf.Max(0.01f, settleStart - travelStart));
                var visible = elapsed >= travelStart;
                if (sphere.gameObject.activeSelf != visible) sphere.gameObject.SetActive(visible);
            }
            else if (!arriving)
            {
                if (hasBoard) board.SetPose(Smooth(elapsed / tiltTime));
                var pipeElapsed = elapsed - tiltTime;
                SetPipeReveal(Smooth(pipeElapsed / revealTime));
                var travelTime = Mathf.Max(0.01f, movement - tiltTime - revealTime - entryTime);
                progress = TravelProgress(pipeElapsed - revealTime, entryTime, travelTime);
                // Continue below the board once the ball clears the inlet lip.
                var visible = progress >= 0.17f;
                if (sphere.gameObject.activeSelf != visible) sphere.gameObject.SetActive(visible);
            }

            sphere.transform.localPosition = PipePreviewMeshes.Point(curve, progress);
            var spin = Mathf.Sin(progress * Mathf.PI);
            sphere.transform.localRotation = Quaternion.Euler(spin * 190f, spin * 280f, spin * -120f);
            sphere.transform.localScale = Vector3.one * radius * 2f;
            group.alpha = Mathf.Min(Smooth(elapsed / fade), Smooth((duration - elapsed) / fade));
            if (hasBoard && !arriving && elapsed >= fade) board.HideSource();
            if (arriving && t >= 1f && !didLand)
            {
                didLand = true;
                if (hasBoard) board.RestoreSource();
                landed?.Invoke();
                // Keep the cinematic ball through the final board crossfade.
                if (!hasBoard) sphere.gameObject.SetActive(false);
            }
        }

        private static Vector2 OutsidePoint(Vector2 anchor, Rect bounds, Vector2 side, float padding)
        {
            if (side.x < 0f) return new Vector2(bounds.xMin - padding, anchor.y);
            if (side.x > 0f) return new Vector2(bounds.xMax + padding, anchor.y);
            return new Vector2(anchor.x, side.y < 0f ? bounds.yMin - padding : bounds.yMax + padding);
        }

        private Vector3 ExtendToScreenEdge(Vector3 endpoint, Vector2 side)
        {
            // Aim the extension toward the screen edge halfway through the tilt.
            // A purely board-local direction can converge to a vanishing point
            // inside a wide screen, however long the pipe becomes.
            board.SetPose(0.5f);
            var outward = board.transform.InverseTransformDirection(
                renderCamera.transform.TransformDirection(new Vector3(side.x, side.y, 0f)));
            var horizontal = side.x != 0f;
            // Overscan hides the whole end collar, including its thickness.
            var edge = (horizontal ? side.x : side.y) > 0f ? 1.15f : -0.15f;
            var extension = 0f;
            // Cover the tilted hold and the return to 2D. This is done once when
            // building the path, rather than rebuilding pipe meshes every frame.
            for (var pose = 0; pose <= 4; pose++)
            {
                board.SetPose(pose * 0.25f);
                var from = renderCamera.WorldToViewportPoint(board.transform.TransformPoint(endpoint));
                var to = renderCamera.WorldToViewportPoint(board.transform.TransformPoint(endpoint + outward));
                var a = horizontal ? from.x : from.y;
                var b = horizontal ? to.x : to.y;
                // Perspective coordinates divide by depth. Solve the projected
                // line/viewport-edge intersection before the board is animated.
                var denominator = renderCamera.orthographic ? b - a :
                    (b - edge) * to.z - (a - edge) * from.z;
                if (Mathf.Abs(denominator) < 0.00001f) continue;
                var distance = (edge - a) * (renderCamera.orthographic ? 1f : from.z) / denominator;
                extension = Mathf.Max(extension, distance);
            }
            return endpoint + outward * extension;
        }

        // Zero velocity and acceleration at either end of a board/reveal phase.
        private static float Smooth(float value)
        {
            var t = Mathf.Clamp01(value);
            return Mathf.Clamp01(t * t * t * (t * (t * 6f - 15f) + 10f));
        }

        private static float TravelProgress(float elapsed, float entry, float travel)
        {
            const float inletEnd = 0.22f;
            if (elapsed <= 0f) return 0f;
            if (elapsed >= entry + travel) return 1f;
            // Match velocity and acceleration across the inlet/travel join, so
            // the ball rolls through it without stopping and starting again.
            var speed = Mathf.Min(inletEnd / entry, (1f - inletEnd) / travel) * 1.25f;
            return elapsed < entry
                ? Hermite(0f, inletEnd, 0f, speed * entry, elapsed / entry)
                : Hermite(inletEnd, 1f, speed * travel, 0f, (elapsed - entry) / travel);
        }

        private static float Hermite(float start, float end, float startTangent, float endTangent, float t)
        {
            var delta = end - start;
            var a = 10f * delta - 6f * startTangent - 4f * endTangent;
            var b = -15f * delta + 8f * startTangent + 7f * endTangent;
            var c = 6f * delta - 3f * startTangent - 3f * endTangent;
            return Mathf.Clamp01(start + startTangent * t + t * t * t * (a + t * (b + t * c)));
        }

        private void PrepareCamera(Rect bounds)
        {
            ReleaseTarget();
            // The staging scene is never in the gameplay camera's view. Its camera
            // sees only prefab layer 31, and outputs to this overlay's RawImage.
            // Keep the stage outside the board's view, but close enough to the
            // origin for precise perspective projection through the hole.
            stage.position = new Vector3(100f, 100f, 100f);
            var aspect = Mathf.Max(0.1f, bounds.width / Mathf.Max(1f, bounds.height));
            var longest = Mathf.Clamp(maximumRenderSize, 256, 1536);
            var width = aspect >= 1f ? longest : Mathf.Max(128, Mathf.RoundToInt(longest * aspect));
            var height = aspect >= 1f ? Mathf.Max(128, Mathf.RoundToInt(longest / aspect)) : longest;
            // The inner and outer pipe surfaces are close together. A 16-bit
            // depth buffer causes visible bands where those surfaces overlap.
            target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default, 2);
            target.name = "Pipe 3D Preview";
            target.filterMode = FilterMode.Bilinear;
            if (!target.IsCreated() && !target.Create())
                throw new InvalidOperationException("3D preview render target unavailable.");
            renderCamera.targetTexture = target;
            renderCamera.aspect = aspect;
            var scale = Mathf.Max(0.001f, Mathf.Abs(stage.lossyScale.z));
            renderCamera.nearClipPlane = scale * 0.05f;
            renderCamera.farClipPlane = scale * 60f;
            renderCamera.enabled = true;
            viewport.texture = target;
            viewport.color = Color.white;
        }

        private Vector3 ToStage(Vector2 point, Rect bounds, float belowBoardDepth = 0f)
        {
            var depth = Vector3.Dot(stage.position - renderCamera.transform.position, renderCamera.transform.forward);
            var world = renderCamera.ViewportToWorldPoint(new Vector3((point.x - bounds.xMin) / bounds.width,
                (point.y - bounds.yMin) / bounds.height, depth + belowBoardDepth * Mathf.Abs(stage.lossyScale.z)));
            var local = stage.InverseTransformPoint(world);
            local.z = belowBoardDepth;
            return board != null ? board.LocalPoint(local) : local;
        }

        private void CaptureViewport()
        {
            viewportOffsetMin = viewport.rectTransform.offsetMin;
            viewportOffsetMax = viewport.rectTransform.offsetMax;
            viewportUv = viewport.uvRect;
            viewportHeight = viewport.rectTransform.rect.height;
            viewportCaptured = true;
            lastReveal = -1f;
        }

        private void SetPipeReveal(float amount)
        {
            if (board != null && board.Ready) { board.RevealPipe(amount); return; }
            amount = Mathf.Clamp01(amount);
            if (!viewportCaptured || lastReveal == amount ||
                amount > 0f && amount < 1f && Mathf.Approximately(lastReveal, amount)) return;
            // Crop matching geometry AND texture UVs: the pipe rises from below
            // without stretching it or moving its connection to the hole.
            viewport.rectTransform.offsetMax = viewportOffsetMax + Vector2.down * (viewportHeight * (1f - amount));
            viewport.uvRect = new Rect(viewportUv.x, viewportUv.y, viewportUv.width, viewportUv.height * amount);
            lastReveal = amount;
        }

        private void RestoreViewport()
        {
            if (!viewportCaptured || viewport == null) return;
            viewport.rectTransform.offsetMin = viewportOffsetMin;
            viewport.rectTransform.offsetMax = viewportOffsetMax;
            viewport.uvRect = viewportUv;
            viewportCaptured = false;
            lastReveal = -1f;
        }

        private void BuildMeshes(Vector3[] curve, float radius)
        {
            shellMesh ??= new Mesh { name = "Pipe 3D Shell" };
            liningMesh ??= new Mesh { name = "Pipe 3D Lining" };
            glassMesh ??= new Mesh { name = "Pipe 3D Glass" };
            collarMesh ??= new Mesh { name = "Pipe 3D Collars" };
            ballMesh ??= PipePreviewMeshes.Sphere();
            PipePreviewMeshes.Tube(shellMesh, curve, radius);
            PipePreviewMeshes.Tube(liningMesh, curve, radius * 0.88f, true);
            PipePreviewMeshes.Tube(glassMesh, curve, radius * 0.96f, false, true);
            PipePreviewMeshes.Collars(collarMesh, curve, radius);
            shell.sharedMesh = shellMesh; lining.sharedMesh = liningMesh;
            glassCover.sharedMesh = glassMesh; collars.sharedMesh = collarMesh; sphere.sharedMesh = ballMesh;
        }

        private void ConfigureBall(SpriteRenderer source)
        {
            ballProperties ??= new MaterialPropertyBlock();
            ballProperties.Clear();
            var tint = source.color;
            tint.a = 1f;
            ballProperties.SetColor(ColorId, tint);
            if (source.sprite != null)
            {
                var texture = source.sprite.texture;
                var rect = source.sprite.rect;
                ballProperties.SetTexture(MainTexId, texture);
                ballProperties.SetVector(MainTexSTId, new Vector4(rect.width / texture.width,
                    rect.height / texture.height, rect.x / texture.width, rect.y / texture.height));
            }
            sphereRenderer.SetPropertyBlock(ballProperties);
        }

        private void ReleaseTarget()
        {
            if (board != null) board.Release();
            RestoreViewport();
            if (renderCamera != null) { renderCamera.enabled = false; renderCamera.targetTexture = null; }
            if (viewport != null) viewport.texture = null;
            if (target == null) return;
            RenderTexture.ReleaseTemporary(target);
            target = null;
        }

        public override void Hide()
        {
            if (sphere != null) sphere.gameObject.SetActive(false);
            ReleaseTarget();
            base.Hide();
        }
        protected override void OnDisable() { ReleaseTarget(); base.OnDisable(); }
        private void OnDestroy()
        {
            ReleaseTarget();
            foreach (var mesh in new[] { shellMesh, liningMesh, glassMesh, collarMesh, ballMesh })
                if (mesh != null) Destroy(mesh);
        }
    }
}
