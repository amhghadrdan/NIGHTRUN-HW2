using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NightRun
{
    /// <summary>Build city details directly into material batches instead of thousands of overlapping primitives.</summary>
    public sealed class EnvironmentMesh
    {
        sealed class Buffer
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector3> Normals = new List<Vector3>();
            public readonly List<Vector2> UV = new List<Vector2>();
            public readonly List<int> Triangles = new List<int>();
        }
        readonly Dictionary<Material, Buffer> buffers = new Dictionary<Material, Buffer>();
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Material material)
        {
            if (!buffers.TryGetValue(material, out var v)) { v = new Buffer(); buffers[material] = v; }
            int n = v.Vertices.Count; Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            v.Vertices.Add(a); v.Vertices.Add(b); v.Vertices.Add(c); v.Vertices.Add(d);
            for (int k = 0; k < 4; k++) v.Normals.Add(normal);
            v.UV.Add(Vector2.zero); v.UV.Add(Vector2.up); v.UV.Add(Vector2.one); v.UV.Add(Vector2.right);
            v.Triangles.Add(n); v.Triangles.Add(n + 1); v.Triangles.Add(n + 2);
            v.Triangles.Add(n); v.Triangles.Add(n + 2); v.Triangles.Add(n + 3);
        }
        public void Box(Vector3 center, Vector3 size, Material material)
        {
            Vector3 p = center - size / 2, q = center + size / 2;
            Quad(new Vector3(p.x, p.y, p.z), new Vector3(p.x, q.y, p.z), new Vector3(q.x, q.y, p.z), new Vector3(q.x, p.y, p.z), material);
            Quad(new Vector3(q.x, p.y, q.z), new Vector3(q.x, q.y, q.z), new Vector3(p.x, q.y, q.z), new Vector3(p.x, p.y, q.z), material);
            Quad(new Vector3(p.x, p.y, q.z), new Vector3(p.x, q.y, q.z), new Vector3(p.x, q.y, p.z), new Vector3(p.x, p.y, p.z), material);
            Quad(new Vector3(q.x, p.y, p.z), new Vector3(q.x, q.y, p.z), new Vector3(q.x, q.y, q.z), new Vector3(q.x, p.y, q.z), material);
            Quad(new Vector3(p.x, q.y, p.z), new Vector3(p.x, q.y, q.z), new Vector3(q.x, q.y, q.z), new Vector3(q.x, q.y, p.z), material);
            Quad(new Vector3(p.x, p.y, q.z), new Vector3(p.x, p.y, p.z), new Vector3(q.x, p.y, p.z), new Vector3(q.x, p.y, q.z), material);
        }
        public void Cylinder(Vector3 center, float radius, float height, Material material, int sides = 12)
        {
            for (int i = 0; i < sides; i++)
            {
                float a = Mathf.PI * 2 * i / sides, b = Mathf.PI * 2 * (i + 1) / sides;
                Vector3 p = center + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius;
                Vector3 q = center + new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)) * radius;
                Quad(p, p + Vector3.up * height, q + Vector3.up * height, q, material);
                Quad(center + Vector3.up * height, q + Vector3.up * height, p + Vector3.up * height, center + Vector3.up * height, material);
            }
        }
        public void Beam(Vector3 a, Vector3 b, float radius, Material material)
        {
            Vector3 dir = (b - a).normalized;
            Vector3 across = Vector3.Cross(dir, Vector3.up).normalized * radius;
            if (across.sqrMagnitude < .00001f) across = Vector3.right * radius;
            Vector3 up = Vector3.Cross(dir, across).normalized * radius;
            Quad(a - across, b - across, b + across, a + across, material);
            Quad(a - up, a + up, b + up, b - up, material);
        }
        public void Foliage(Vector3 center, Vector3 scale, Material material, int seed)
        {
            var random = new System.Random(seed);
            const int rings = 12, slices = 24;
            Vector3[,] points = new Vector3[rings + 1, slices + 1];
            for (int y = 0; y <= rings; y++) for (int x = 0; x <= slices; x++)
            {
                float a = Mathf.PI * y / rings, b = Mathf.PI * 2 * x / slices;
                float irregularity = .8f + (float)random.NextDouble() * .35f;
                points[y, x] = center + Vector3.Scale(new Vector3(Mathf.Sin(a) * Mathf.Cos(b), Mathf.Cos(a), Mathf.Sin(a) * Mathf.Sin(b)) * irregularity, scale);
            }
            for (int y = 0; y < rings; y++) for (int x = 0; x < slices; x++)
            {
                var corners = new[] { points[y, x + 1], points[y + 1, x + 1], points[y + 1, x], points[y, x] };
                Quad(corners[0], corners[1], corners[2], corners[3], material);
                var buffer = buffers[material]; int start = buffer.Normals.Count - 4;
                for (int i = 0; i < 4; i++) buffer.Normals[start + i] = (corners[i] - center).normalized;
            }
        }
        public void Finish(Transform parent, string name)
        {
            foreach (var pair in buffers)
            {
                var b = pair.Value;
                var mesh = new Mesh { name = name + " / " + pair.Key.name, indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(b.Vertices); mesh.SetNormals(b.Normals); mesh.SetUVs(0, b.UV); mesh.SetTriangles(b.Triangles, 0);
                mesh.RecalculateBounds(); mesh.RecalculateTangents(); mesh.UploadMeshData(true);
                var go = new GameObject(mesh.name); go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = pair.Key;
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = true;
            }
        }
    }
}
