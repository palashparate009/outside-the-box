using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OutsideTheBox
{
    /// <summary>Persistent sprite-free rounded rectangle with a visible outer border.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UIRoundRectGraphic : MaskableGraphic
    {
        [SerializeField] private Color borderColor = new(0.35f, 0.88f, 0.92f, 0.85f);
        [SerializeField, Min(0f)] private float borderWidth = 5f;
        [SerializeField, Min(0f)] private float cornerRadius = 26f;
        [SerializeField, Range(3, 12)] private int cornerSegments = 6;
        [SerializeField] private bool useGradient;
        [SerializeField] private Color gradientBottom = new(0.12f, 0.26f, 0.4f, 1f);
        [SerializeField] private Color gradientTop = new(0.22f, 0.48f, 0.62f, 1f);
        [SerializeField] private Color gradientHighlight = new(0.5f, 0.95f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float gradientHighlightStrength = 0.12f;

        public bool GradientEnabled => useGradient;

        public void Configure(Color fill, Color border, float width, float radius)
        {
            color = fill;
            borderColor = border;
            borderWidth = Mathf.Max(0f, width);
            cornerRadius = Mathf.Max(0f, radius);
            useGradient = false;
            SetVerticesDirty();
        }

        public void ConfigureGradient(Color bottom, Color top, Color highlight, float highlightStrength)
        {
            gradientBottom = bottom;
            gradientTop = top;
            gradientHighlight = highlight;
            gradientHighlightStrength = Mathf.Clamp01(highlightStrength);
            useGradient = true;
            SetVerticesDirty();
        }

        protected override void OnEnable()
        {
            if (GetComponent<CanvasRenderer>() == null) gameObject.AddComponent<CanvasRenderer>();
            base.OnEnable();
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            var rect = GetPixelAdjustedRect();
            var radius = Mathf.Min(cornerRadius, Mathf.Min(rect.width, rect.height) * 0.5f);
            var width = Mathf.Min(borderWidth, radius);
            var outer = BuildPerimeter(rect, radius);
            var innerRect = new Rect(rect.xMin + width, rect.yMin + width,
                Mathf.Max(0f, rect.width - width * 2f), Mathf.Max(0f, rect.height - width * 2f));
            var inner = BuildPerimeter(innerRect, Mathf.Max(0f, radius - width));

            var outerColor = (Color32)borderColor;
            var fillColor = (Color32)color;
            for (var i = 0; i < outer.Count; i++) helper.AddVert(outer[i], outerColor, Vector2.zero);
            var innerStart = helper.currentVertCount;
            for (var i = 0; i < inner.Count; i++)
                helper.AddVert(inner[i], useGradient ? EvaluateGradient(inner[i].y, rect) : fillColor, Vector2.zero);
            var centerIndex = helper.currentVertCount;
            helper.AddVert(innerRect.center,
                useGradient ? EvaluateGradient(innerRect.center.y, rect) : fillColor, Vector2.zero);

            for (var i = 0; i < outer.Count; i++)
            {
                var next = (i + 1) % outer.Count;
                helper.AddTriangle(i, next, innerStart + next);
                helper.AddTriangle(i, innerStart + next, innerStart + i);
                helper.AddTriangle(centerIndex, innerStart + i, innerStart + next);
            }
        }

        private Color32 EvaluateGradient(float y, Rect rect)
        {
            var t = Mathf.InverseLerp(rect.yMin, rect.yMax, y);
            var result = Color.Lerp(gradientBottom, gradientTop, t);
            var highlight = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Abs(t - 0.72f) * 3.6f) *
                            gradientHighlightStrength;
            return Color.Lerp(result, gradientHighlight, highlight);
        }

        private List<Vector2> BuildPerimeter(Rect rect, float radius)
        {
            var points = new List<Vector2>((cornerSegments + 1) * 4);
            AddCorner(points, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f);
            AddCorner(points, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f);
            AddCorner(points, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f);
            AddCorner(points, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 270f, 360f);
            return points;
        }

        private void AddCorner(List<Vector2> points, Vector2 center, float radius, float from, float to)
        {
            for (var i = 0; i <= cornerSegments; i++)
            {
                var radians = Mathf.Lerp(from, to, i / (float)cornerSegments) * Mathf.Deg2Rad;
                points.Add(center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius);
            }
        }
    }
}
