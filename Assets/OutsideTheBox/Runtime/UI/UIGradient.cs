using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OutsideTheBox
{
    /// <summary>Lightweight vertex gradient for mobile uGUI surfaces.</summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class UIGradient : BaseMeshEffect
    {
        [SerializeField] private Color bottom = new(0.025f, 0.055f, 0.11f, 1f);
        [SerializeField] private Color top = new(0.09f, 0.18f, 0.3f, 1f);
        [SerializeField] private Color glow = new(0.24f, 0.84f, 0.8f, 1f);
        [SerializeField, Range(0f, 1f)] private float glowStrength = 0.12f;

        public void Configure(Color bottomColor, Color topColor, Color glowColor, float strength)
        {
            bottom = bottomColor;
            top = topColor;
            glow = glowColor;
            glowStrength = Mathf.Clamp01(strength);
            if (graphic != null) graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper helper)
        {
            if (!IsActive() || helper.currentVertCount == 0) return;
            var vertices = new List<UIVertex>(helper.currentVertCount);
            helper.GetUIVertexStream(vertices);
            var minY = float.MaxValue;
            var maxY = float.MinValue;
            for (var i = 0; i < vertices.Count; i++)
            {
                minY = Mathf.Min(minY, vertices[i].position.y);
                maxY = Mathf.Max(maxY, vertices[i].position.y);
            }
            var height = Mathf.Max(0.001f, maxY - minY);
            for (var i = 0; i < vertices.Count; i++)
            {
                var vertex = vertices[i];
                var t = Mathf.Clamp01((vertex.position.y - minY) / height);
                var color = Color.Lerp(bottom, top, t);
                var light = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Abs(t - 0.72f) * 3.6f) * glowStrength;
                color = Color.Lerp(color, glow, light);
                color.a *= vertex.color.a / 255f;
                vertex.color = color;
                vertices[i] = vertex;
            }
            helper.Clear();
            helper.AddUIVertexTriangleStream(vertices);
        }
    }
}
