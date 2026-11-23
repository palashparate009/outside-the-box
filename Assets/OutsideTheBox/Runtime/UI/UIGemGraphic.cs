using UnityEngine;
using UnityEngine.UI;

namespace OutsideTheBox
{
    /// <summary>Small faceted gem artwork rendered directly as UI geometry.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UIGemGraphic : MaskableGraphic
    {
        [Range(1, 6)] public int gemCount = 1;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = GetPixelAdjustedRect();
            var count = Mathf.Clamp(gemCount, 1, 6);
            var scale = Mathf.Min(rect.width / (count == 1 ? 1.15f : 2.1f), rect.height / 1.4f);
            for (var i = 0; i < count; i++)
            {
                var center = rect.center + (count == 1 ? Vector2.zero : new Vector2(
                    (i % 3 - 1) * scale * 0.48f, (i / 3 - 0.5f) * scale * 0.44f));
                DrawGem(mesh, center, scale * (count == 1 ? 0.8f : 0.52f));
            }
        }

        private void DrawGem(VertexHelper mesh, Vector2 center, float size)
        {
            var top = center + new Vector2(0, size * 0.48f);
            var left = center + new Vector2(-size * 0.5f, size * 0.12f);
            var right = center + new Vector2(size * 0.5f, size * 0.12f);
            var bottom = center + new Vector2(0, -size * 0.55f);
            var mid = center + new Vector2(0, size * 0.06f);
            Triangle(mesh, top, left, mid, Color.Lerp(color, Color.white, 0.72f));
            Triangle(mesh, top, mid, right, Color.Lerp(color, Color.white, 0.35f));
            Triangle(mesh, left, bottom, mid, color);
            Triangle(mesh, mid, bottom, right, Color.Lerp(color, new Color(0.05f, 0.2f, 0.65f, color.a), 0.65f));
        }

        private static void Triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color tint)
        {
            var first = mesh.currentVertCount;
            mesh.AddVert(a, tint, Vector2.zero);
            mesh.AddVert(b, tint, Vector2.zero);
            mesh.AddVert(c, tint, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
        }
    }
}
