using UnityEngine;
using UnityEngine.UI;

namespace OutsideTheBox
{
    // Small UI symbols are geometry, so display fonts need no icon glyphs or fallbacks.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UIIconGraphic : MaskableGraphic
    {
        public enum Symbol { Pause, ChevronDown }
        public Symbol symbol;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = GetPixelAdjustedRect();
            var size = Mathf.Min(rect.width, rect.height);
            var center = rect.center;
            if (symbol == Symbol.Pause)
            {
                Bar(mesh, center + Vector2.left * size * 0.22f, size * 0.18f, size * 0.76f);
                Bar(mesh, center + Vector2.right * size * 0.22f, size * 0.18f, size * 0.76f);
            }
            else
            {
                var bottom = center + Vector2.down * size * 0.2f;
                Stroke(mesh, center + new Vector2(-0.34f, 0.14f) * size, bottom, size * 0.12f);
                Stroke(mesh, bottom, center + new Vector2(0.34f, 0.14f) * size, size * 0.12f);
            }
        }

        private void Bar(VertexHelper mesh, Vector2 center, float width, float height)
        {
            var half = new Vector2(width, height) * 0.5f;
            Quad(mesh, center + new Vector2(-half.x, -half.y), center + new Vector2(-half.x, half.y),
                center + half, center + new Vector2(half.x, -half.y));
        }

        private void Stroke(VertexHelper mesh, Vector2 from, Vector2 to, float width)
        {
            var direction = (to - from).normalized;
            var normal = new Vector2(-direction.y, direction.x) * width * 0.5f;
            Quad(mesh, from - normal, from + normal, to + normal, to - normal);
        }

        private void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            var first = mesh.currentVertCount;
            mesh.AddVert(a, color, Vector2.zero);
            mesh.AddVert(b, color, Vector2.zero);
            mesh.AddVert(c, color, Vector2.zero);
            mesh.AddVert(d, color, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }
    }
}
