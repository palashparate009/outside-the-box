using System.Collections.Generic;
using UnityEngine;

namespace OutsideTheBox
{
    // Presentation-only copy of the current board. All renderers and cameras are
    // authored in the transition prefab; capture stays on the GPU.
    public sealed class LevelBoardDiorama : MonoBehaviour
    {
        public Transform boardRoot, pipeRoot;
        public Camera captureCamera;
        public MeshFilter face, sides;
        public MeshRenderer faceRenderer;
        public MeshRenderer[] pipeRenderers;
        public Vector2 tiltDegrees = new(55f, -12f);
        [Range(0f, 0.4f)] public float liftFraction = 0.18f;
        [Range(0.01f, 0.12f)] public float thicknessFraction = 0.045f;
        private Transform source;
        private Vector2 sourceSize, size;
        private Vector3 exitWorld, center;
        private float exitRadius, revealMin, revealMax;
        private RenderTexture snapshot;
        private Mesh faceMesh, sideMesh;
        private MaterialPropertyBlock faceProperties, pipeProperties;
        private readonly List<Renderer> hiddenRenderers = new();
        private readonly List<bool> originalRenderingOff = new();
        private bool sourceHidden;
        private Quaternion tiltedPose;
        private Vector2 routeSide = Vector2.down;
        private float lastTilt = -1f, lastEntry = -1f, lastPipeReveal = -1f;
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int HoleId = Shader.PropertyToID("_BoardHole");
        private static readonly int CutHoleId = Shader.PropertyToID("_CutBoardHole");
        private static readonly int ClipPipeId = Shader.PropertyToID("_ClipPipe");
        private static readonly int RevealYId = Shader.PropertyToID("_RevealY");
        private static readonly int RevealAxisId = Shader.PropertyToID("_PipeRevealAxis");
        public bool Ready { get; private set; }
        public bool HasSource => source != null;
        public Vector3 Center => center;
        public float Thickness => Mathf.Min(size.x, size.y) * thicknessFraction;
        public Vector2 RouteSide => routeSide;

        public void SetRouteSide(Vector2 side, bool arriving)
        {
            routeSide = side;
            // Expose the underside on exit and the above-board feed on arrival.
            var facing = arriving ? -side : side;
            tiltedPose = Quaternion.Euler(-facing.y * tiltDegrees.x - facing.x * tiltDegrees.y,
                facing.x * tiltDegrees.x - facing.y * tiltDegrees.y, 0f);
            lastTilt = lastEntry = lastPipeReveal = -1f;
        }

        public void SetSource(Transform root, Vector2 localSize, Vector3 hole, float radius)
        { source = root; sourceSize = localSize; exitWorld = hole; exitRadius = radius; }

        public void HideSourceUntilCapture()
        {
            if (source == null || sourceHidden) return;
            RememberSourceVisibility();
            HideSource();
        }

        private void RememberSourceVisibility()
        {
            source.GetComponentsInChildren(true, hiddenRenderers);
            originalRenderingOff.Clear();
            foreach (var renderer in hiddenRenderers)
                originalRenderingOff.Add(renderer.forceRenderingOff);
        }

        public bool Prepare(Camera original, RectTransform overlay, Transform stage, Camera projection, int resolution, bool hideImmediately)
        {
            Release();
            if (source == null) return false;
            if (boardRoot == null || pipeRoot == null || captureCamera == null || face == null ||
                sides == null || faceRenderer == null || pipeRenderers == null || pipeRenderers.Length == 0)
                throw new System.InvalidOperationException("Board cinematic prefab references are incomplete.");
            var half = sourceSize * 0.5f;
            var bottom = source.TransformPoint(new Vector3(-half.x, -half.y, 0f));
            var top = source.TransformPoint(new Vector3(half.x, half.y, 0f));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, original.WorldToScreenPoint(bottom), null, out var screenMin);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, original.WorldToScreenPoint(top), null, out var screenMax);
            var bounds = overlay.rect;
            var depth = Vector3.Dot(stage.position - projection.transform.position, projection.transform.forward);
            Vector3 Project(Vector2 p) => stage.InverseTransformPoint(projection.ViewportToWorldPoint(new Vector3(
                (p.x - bounds.xMin) / bounds.width, (p.y - bounds.yMin) / bounds.height, depth)));
            var a = Project(screenMin); var b = Project(screenMax);
            center = (a + b) * 0.5f; center.z = 0f;
            size = new Vector2(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
            tiltedPose = Quaternion.Euler(tiltDegrees.x, tiltDegrees.y, 0f);
            transform.localPosition = center;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            boardRoot.localPosition = Vector3.zero;
            boardRoot.localRotation = Quaternion.identity;
            boardRoot.localScale = Vector3.one;
            faceMesh ??= new Mesh { name = "Cinematic Board Face" };
            sideMesh ??= new Mesh { name = "Cinematic Board Sides" };
            PipePreviewMeshes.BoardFace(faceMesh, size);
            PipePreviewMeshes.BoardSides(sideMesh, size, Thickness);
            face.sharedMesh = faceMesh; sides.sharedMesh = sideMesh;

            var worldWidth = Mathf.Abs(top.x - bottom.x);
            var worldHeight = Mathf.Max(0.01f, Mathf.Abs(top.y - bottom.y));
            var aspect = Mathf.Max(0.1f, worldWidth / worldHeight);
            var maximum = Mathf.Clamp(resolution, 256, 1024);
            var width = aspect >= 1f ? maximum : Mathf.Max(128, Mathf.RoundToInt(maximum * aspect));
            var height = aspect >= 1f ? Mathf.Max(128, Mathf.RoundToInt(maximum / aspect)) : maximum;
            snapshot = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            snapshot.name = "Cinematic Board Snapshot";
            snapshot.filterMode = FilterMode.Bilinear;
            captureCamera.CopyFrom(original);
            captureCamera.enabled = false;
            captureCamera.transform.SetPositionAndRotation(new Vector3((bottom.x + top.x) * 0.5f,
                (bottom.y + top.y) * 0.5f, original.transform.position.z), original.transform.rotation);
            captureCamera.orthographic = true;
            captureCamera.orthographicSize = worldHeight * 0.5f;
            captureCamera.aspect = aspect;
            captureCamera.rect = new Rect(0f, 0f, 1f, 1f);
            captureCamera.cullingMask = original.cullingMask & ~(1 << 31);
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = Color.clear;
            captureCamera.allowHDR = false;
            captureCamera.allowMSAA = false;
            captureCamera.allowDynamicResolution = false;
            var previous = RenderTexture.active;
            try { captureCamera.targetTexture = snapshot; captureCamera.Render(); }
            finally { captureCamera.targetTexture = null; RenderTexture.active = previous; }
            faceProperties ??= new MaterialPropertyBlock();
            faceProperties.Clear();
            faceProperties.SetTexture(MainTexId, snapshot);
            faceProperties.SetVector(HoleId, new Vector4((exitWorld.x - bottom.x) / Mathf.Max(0.01f, worldWidth),
                (exitWorld.y - bottom.y) / worldHeight, exitRadius * 0.72f / Mathf.Max(0.01f, worldWidth), exitRadius * 0.72f / worldHeight));
            faceProperties.SetFloat(CutHoleId, 1f);
            faceRenderer.SetPropertyBlock(faceProperties);
            RememberSourceVisibility();
            if (hideImmediately) HideSource();
            boardRoot.gameObject.SetActive(true);
            pipeRoot.gameObject.SetActive(false);
            Ready = true;
            return true;
        }

        public Vector3 LocalPoint(Vector3 stagePoint) => Ready ? stagePoint - center : stagePoint;

        public void SetPose(float tilt)
        {
            if (!Ready) return;
            tilt = Mathf.Clamp01(tilt);
            if (Unchanged(lastTilt, tilt)) return;
            lastTilt = tilt;
            transform.localPosition = center - new Vector3(routeSide.x * size.x, routeSide.y * size.y, 0f) *
                (liftFraction * tilt);
            transform.localRotation = Quaternion.Slerp(Quaternion.identity, tiltedPose, tilt);
        }

        public void SetBoardEntry(float progress)
        {
            if (!Ready) return;
            progress = Mathf.Clamp01(progress);
            // The offset is expressed in the board's current rotated coordinates.
            // Reapply while the pose changes, even if entry progress is held.
            if (progress == 1f && lastEntry == 1f) return;
            lastEntry = progress;
            // Move in camera space, so the tilted board never crosses the near plane.
            boardRoot.localPosition = Quaternion.Inverse(transform.localRotation) *
                new Vector3(-routeSide.x, -routeSide.y, 0f) *
                (Mathf.Max(size.x, size.y) * 2.2f * (1f - progress));
        }

        public void SetPipeBounds(Vector3 start, Vector3 end, float radius)
        {
            var inward = -routeSide;
            var a = start.x * inward.x + start.y * inward.y;
            var b = end.x * inward.x + end.y * inward.y;
            revealMin = Mathf.Min(a, b) - radius * 2f;
            revealMax = Mathf.Max(a, b) + radius * 2f;
            lastPipeReveal = -1f;
        }

        public void RevealPipe(float amount)
        {
            if (!Ready) return;
            amount = Mathf.Clamp01(amount);
            if (Unchanged(lastPipeReveal, amount)) return;
            lastPipeReveal = amount;
            var visible = amount > 0f;
            if (pipeRoot.gameObject.activeSelf != visible) pipeRoot.gameObject.SetActive(visible);
            pipeProperties ??= new MaterialPropertyBlock();
            pipeProperties.SetFloat(ClipPipeId, amount < 1f ? 1f : 0f);
            pipeProperties.SetFloat(RevealYId, Mathf.Lerp(revealMin, revealMax, amount));
            pipeProperties.SetVector(RevealAxisId, new Vector4(-routeSide.x, -routeSide.y, 0f, 0f));
            foreach (var renderer in pipeRenderers) if (renderer != null) renderer.SetPropertyBlock(pipeProperties);
        }

        public void Release()
        {
            RestoreSource();
            Ready = false;
            if (captureCamera != null) { captureCamera.enabled = false; captureCamera.targetTexture = null; }
            if (faceRenderer != null) faceRenderer.SetPropertyBlock(null);
            faceProperties?.Clear();
            if (snapshot != null) { RenderTexture.ReleaseTemporary(snapshot); snapshot = null; }
            if (boardRoot != null) { boardRoot.gameObject.SetActive(false); boardRoot.localPosition = Vector3.zero; }
            if (pipeRoot != null) pipeRoot.gameObject.SetActive(true);
            if (pipeRenderers != null)
                foreach (var renderer in pipeRenderers) if (renderer != null) renderer.SetPropertyBlock(null);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            lastTilt = lastEntry = lastPipeReveal = -1f;
        }

        public void RestoreSource()
        {
            for (var i = 0; i < Mathf.Min(hiddenRenderers.Count, originalRenderingOff.Count); i++)
                if (hiddenRenderers[i] != null) hiddenRenderers[i].forceRenderingOff = originalRenderingOff[i];
            hiddenRenderers.Clear();
            originalRenderingOff.Clear();
            sourceHidden = false;
        }

        public void HideSource()
        {
            if (sourceHidden) return;
            foreach (var renderer in hiddenRenderers) if (renderer != null) renderer.forceRenderingOff = true;
            sourceHidden = true;
        }

        private static bool Unchanged(float previous, float current) => previous == current ||
            current > 0f && current < 1f && Mathf.Approximately(previous, current);

        private void OnDisable() => Release();
        private void OnDestroy()
        {
            Release();
            if (faceMesh != null) Destroy(faceMesh);
            if (sideMesh != null) Destroy(sideMesh);
        }
    }
}
