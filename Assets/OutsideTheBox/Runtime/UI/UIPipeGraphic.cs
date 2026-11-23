using UnityEngine;
using UnityEngine.UI;

namespace OutsideTheBox
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UIPipeGraphic : MaskableGraphic
    {
        public bool frontGlass;
        [Min(16f)] public float pipeWidth = 84f;
        private Vector2 a, b, c, d;
        private bool hasPath;
        private const int Segments = 48;

        public void SetPath(Vector2 start, Vector2 controlA, Vector2 controlB, Vector2 end, float width)
        {
            a = start; b = controlA; c = controlB; d = end;
            pipeWidth = width;
            hasPath = true;
            SetVerticesDirty();
        }

        public static Vector2 Point(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
        {
            var u = 1f - Mathf.Clamp01(t);
            t = Mathf.Clamp01(t);
            return u * u * u * a + 3f * u * u * t * b + 3f * u * t * t * c + t * t * t * d;
        }

        private Vector2 Normal(float t)
        {
            var u = 1f - t;
            var tangent = 3f * u * u * (b - a) + 6f * u * t * (c - b) + 3f * t * t * (d - c);
            if (tangent.sqrMagnitude < 0.001f) tangent = Vector2.down;
            tangent.Normalize();
            return new Vector2(-tangent.y, tangent.x);
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (!hasPath) return;
            if (!frontGlass)
            {
                Stroke(mesh, pipeWidth + 18f, new Color(0.1f, 0.85f, 1f, 0.12f));
                Stroke(mesh, pipeWidth + 8f, new Color(0.025f, 0.09f, 0.18f, 1f));
                Stroke(mesh, pipeWidth, new Color(0.42f, 0.69f, 0.82f, 1f));
                Stroke(mesh, pipeWidth - 12f, new Color(0.045f, 0.17f, 0.27f, 1f));
                Stroke(mesh, 3f, new Color(0.65f, 0.95f, 1f, 0.9f), -pipeWidth * 0.43f);
            }
            else
            {
                Stroke(mesh, pipeWidth - 15f, new Color(0.45f, 0.88f, 1f, 0.09f));
                for (var i = 0; i <= 4; i++) Cuff(mesh, i / 4f);
            }
        }

        private void Stroke(VertexHelper mesh, float width, Color tint, float offset = 0f)
        {
            var first = mesh.currentVertCount;
            tint *= color;
            for (var i = 0; i <= Segments; i++)
            {
                var t = i / (float)Segments;
                var normal = Normal(t);
                var center = Point(a, b, c, d, t) + normal * offset;
                mesh.AddVert(center - normal * width * 0.5f, tint, Vector2.zero);
                mesh.AddVert(center + normal * width * 0.5f, tint, Vector2.zero);
                if (i == 0) continue;
                var current = first + i * 2;
                mesh.AddTriangle(current - 2, current - 1, current);
                mesh.AddTriangle(current, current - 1, current + 1);
            }
        }

        private void Cuff(VertexHelper mesh, float t)
        {
            var center = Point(a, b, c, d, t);
            var normal = Normal(t);
            var tangent = new Vector2(-normal.y, normal.x);
            var first = mesh.currentVertCount;
            var span = normal * (pipeWidth + 7f) * 0.5f;
            var half = tangent * 3.5f;
            var tint = new Color(0.7f, 0.94f, 1f, 0.68f) * color;
            mesh.AddVert(center - span - half, tint, Vector2.zero);
            mesh.AddVert(center + span - half, tint, Vector2.zero);
            mesh.AddVert(center + span + half, tint, Vector2.zero);
            mesh.AddVert(center - span + half, tint, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }
    }
}
