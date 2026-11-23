using System.Collections.Generic;
using UnityEngine;

namespace OutsideTheBox
{
    // Mesh data only. Every camera, renderer and actor is authored in the 3D prefab.
    public static class PipePreviewMeshes
    {
        private const int LengthSegments = 64;
        // Unity mesh uploads run on the main thread. Reuse this bounded workspace
        // instead of allocating lists and a tiny index array for every triangle.
        private static readonly Data Workspace = new();

        private static Data BeginMesh()
        {
            Workspace.vertices.Clear(); Workspace.normals.Clear();
            Workspace.uv.Clear(); Workspace.triangles.Clear();
            return Workspace;
        }
        private sealed class Data
        {
            public readonly List<Vector3> vertices = new();
            public readonly List<Vector3> normals = new();
            public readonly List<Vector2> uv = new();
            public readonly List<int> triangles = new();
            public void Vertex(Vector3 p, Vector3 normal, Vector2 tex)
            { vertices.Add(p); normals.Add(normal); uv.Add(tex); }
            public void Triangles(int a, int b, int c, int d, int e, int f)
            {
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(d); triangles.Add(e); triangles.Add(f);
            }
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
            {
                var first = vertices.Count;
                Vertex(a, normal, Vector2.zero); Vertex(b, normal, Vector2.right);
                Vertex(c, normal, Vector2.one); Vertex(d, normal, Vector2.up);
                Triangles(first, first + 1, first + 2, first, first + 2, first + 3);
            }
            public void Apply(Mesh mesh)
            {
                mesh.Clear();
                mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv);
                mesh.SetTriangles(triangles, 0, false); mesh.RecalculateBounds();
            }
        }

        public static Vector3 Point(Vector3[] curve, float t)
        {
            t = Mathf.Clamp01(t);
            var u = 1f - t;
            return u * u * u * curve[0] + 3f * u * u * t * curve[1] +
                3f * u * t * t * curve[2] + t * t * t * curve[3];
        }

        private static void Frame(Vector3[] curve, float t, out Vector3 tangent, out Vector3 side, out Vector3 front)
        {
            var u = 1f - t;
            tangent = 3f * u * u * (curve[1] - curve[0]) + 6f * u * t * (curve[2] - curve[1]) +
                3f * t * t * (curve[3] - curve[2]);
            if (tangent.sqrMagnitude < 0.000001f) tangent = Vector3.down;
            tangent.Normalize();
            side = Vector3.Cross(Vector3.forward, tangent);
            if (side.sqrMagnitude < 0.000001f) side = Vector3.right;
            side.Normalize();
            front = Vector3.Cross(side, tangent).normalized;
        }

        public static void Tube(Mesh mesh, Vector3[] curve, float radius, bool lining = false, bool glass = false)
        {
            var data = BeginMesh();
            var gap = 100f * Mathf.Deg2Rad;
            var start = glass ? Mathf.PI * 0.5f - gap * 0.5f : Mathf.PI * 0.5f + gap * 0.5f;
            var arc = glass ? gap : Mathf.PI * 2f - gap;
            var steps = glass ? 10 : 24;
            for (var i = 0; i <= LengthSegments; i++)
            {
                var t = i / (float)LengthSegments;
                Frame(curve, t, out _, out var side, out var front);
                var center = Point(curve, t);
                for (var j = 0; j <= steps; j++)
                {
                    var theta = start + arc * j / steps;
                    var radial = side * Mathf.Cos(theta) + front * Mathf.Sin(theta);
                    data.Vertex(center + radial * radius, lining ? -radial : radial, new Vector2(j / (float)steps, t));
                    if (i == 0 || j == 0) continue;
                    var current = i * (steps + 1) + j;
                    var previous = current - steps - 1;
                    data.Triangles(previous - 1, previous, current - 1, current - 1, previous, current);
                }
            }
            if (!lining && !glass)
            {
                // Close the cutaway lips and both ends so the metal has thickness.
                var inner = radius * 0.88f;
                for (var edge = 0; edge < 2; edge++)
                {
                    var theta = start + arc * edge;
                    for (var i = 0; i < LengthSegments; i++)
                    {
                        var t = i / (float)LengthSegments;
                        var next = (i + 1f) / LengthSegments;
                        Frame(curve, t, out _, out var side, out var front);
                        Frame(curve, next, out _, out var sideNext, out var frontNext);
                        var radial = side * Mathf.Cos(theta) + front * Mathf.Sin(theta);
                        var radialNext = sideNext * Mathf.Cos(theta) + frontNext * Mathf.Sin(theta);
                        var normal = (side * -Mathf.Sin(theta) + front * Mathf.Cos(theta)) * (edge == 0 ? -1f : 1f);
                        data.Quad(Point(curve, t) + radial * radius, Point(curve, t) + radial * inner,
                            Point(curve, next) + radialNext * inner, Point(curve, next) + radialNext * radius, normal);
                    }
                }
                for (var end = 0; end < 2; end++)
                {
                    Frame(curve, end, out var tangent, out var side, out var front);
                    var center = Point(curve, end);
                    for (var j = 0; j < steps; j++)
                    {
                        var angleA = start + arc * j / steps;
                        var angleB = start + arc * (j + 1f) / steps;
                        var a = side * Mathf.Cos(angleA) + front * Mathf.Sin(angleA);
                        var b = side * Mathf.Cos(angleB) + front * Mathf.Sin(angleB);
                        data.Quad(center + a * radius, center + a * inner, center + b * inner,
                            center + b * radius, tangent * (end == 0 ? -1f : 1f));
                    }
                }
            }
            data.Apply(mesh);
        }

        public static void Collars(Mesh mesh, Vector3[] curve, float radius)
        {
            var data = BeginMesh();
            const int sides = 28;
            for (var ring = 0; ring < 5; ring++)
            {
                var t = ring / 4f;
                Frame(curve, t, out var tangent, out var side, out var front);
                var center = Point(curve, t);
                var thickness = radius * 0.16f;
                var outer = radius * 1.14f;
                var inner = radius * 0.94f;
                for (var i = 0; i < sides; i++)
                {
                    var angle = i * Mathf.PI * 2f / sides;
                    var next = (i + 1f) * Mathf.PI * 2f / sides;
                    var a = side * Mathf.Cos(angle) + front * Mathf.Sin(angle);
                    var b = side * Mathf.Cos(next) + front * Mathf.Sin(next);
                    var half = tangent * thickness * 0.5f;
                    data.Quad(center + a * outer - half, center + b * outer - half,
                        center + b * outer + half, center + a * outer + half, (a + b).normalized);
                    data.Quad(center + a * inner - half, center + a * inner + half,
                        center + b * inner + half, center + b * inner - half, -(a + b).normalized);
                    for (var end = -1; end <= 1; end += 2)
                        data.Quad(center + a * outer + half * end, center + a * inner + half * end,
                            center + b * inner + half * end, center + b * outer + half * end, tangent * end);
                }
            }
            data.Apply(mesh);
        }

        public static Mesh Sphere()
        {
            var data = BeginMesh();
            const int rings = 20;
            const int sides = 32;
            for (var y = 0; y <= rings; y++)
            {
                var latitude = y * Mathf.PI / rings;
                for (var x = 0; x <= sides; x++)
                {
                    var longitude = x * Mathf.PI * 2f / sides;
                    var normal = new Vector3(Mathf.Sin(latitude) * Mathf.Cos(longitude),
                        Mathf.Cos(latitude), Mathf.Sin(latitude) * Mathf.Sin(longitude));
                    data.Vertex(normal * 0.5f, normal, new Vector2(x / (float)sides, y / (float)rings));
                    if (x == 0 || y == 0) continue;
                    var current = y * (sides + 1) + x;
                    var previous = current - sides - 1;
                    data.Triangles(previous - 1, previous, current - 1, current - 1, previous, current);
                }
            }
            var mesh = new Mesh { name = "Pipe Preview Sphere" };
            data.Apply(mesh);
            return mesh;
        }

        public static void BoardFace(Mesh mesh, Vector2 size)
        {
            var data = BeginMesh();
            var x = size.x * 0.5f; var y = size.y * 0.5f;
            data.Quad(new Vector3(-x, -y, 0f), new Vector3(x, -y, 0f),
                new Vector3(x, y, 0f), new Vector3(-x, y, 0f), Vector3.back);
            data.Apply(mesh);
        }

        public static void BoardSides(Mesh mesh, Vector2 size, float depth)
        {
            var data = BeginMesh();
            var x = size.x * 0.5f; var y = size.y * 0.5f;
            data.Quad(new Vector3(-x,-y,0f),new Vector3(x,-y,0f),new Vector3(x,-y,depth),new Vector3(-x,-y,depth),Vector3.down);
            data.Quad(new Vector3(x,y,0f),new Vector3(-x,y,0f),new Vector3(-x,y,depth),new Vector3(x,y,depth),Vector3.up);
            data.Quad(new Vector3(-x,y,0f),new Vector3(-x,-y,0f),new Vector3(-x,-y,depth),new Vector3(-x,y,depth),Vector3.left);
            data.Quad(new Vector3(x,-y,0f),new Vector3(x,y,0f),new Vector3(x,y,depth),new Vector3(x,-y,depth),Vector3.right);
            data.Apply(mesh);
        }
    }
}
