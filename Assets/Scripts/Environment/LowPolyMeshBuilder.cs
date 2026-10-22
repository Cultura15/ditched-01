using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MyGame.Environment
{
    /// <summary>Accumulates flat-shaded triangles (one normal per face) and turns them into a Mesh.</summary>
    public class LowPolyMeshBuilder
    {
        private const float DegenerateEpsilon = 1e-10f;
        private const float Tau = Mathf.PI * 2f;

        private static readonly int[] IcosahedronFaces =
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
            1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
            4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
        };

        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<int> _triangles = new List<int>();

        public int TriangleCount => _triangles.Count / 3;

        /// <summary>Adds a triangle, flipping its winding so the face points along <paramref name="outward"/>.</summary>
        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a);
            if (normal.sqrMagnitude < DegenerateEpsilon) return;

            if (Vector3.Dot(normal, outward) < 0f)
            {
                (b, c) = (c, b);
                normal = -normal;
            }

            normal.Normalize();
            int start = _vertices.Count;
            AddVertex(a, normal);
            AddVertex(b, normal);
            AddVertex(c, normal);
            _triangles.Add(start);
            _triangles.Add(start + 1);
            _triangles.Add(start + 2);
        }

        /// <summary>Adds a quad given its corners in perimeter order.</summary>
        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
        {
            AddTriangle(a, b, c, outward);
            AddTriangle(a, c, d, outward);
        }

        /// <summary>Revolves a 2D profile (x = radius, y = height) around the Y axis.
        /// Trace the profile counter-clockwise: bottom center, outward, up, then back inward.</summary>
        public LowPolyMeshBuilder AddLathe(IReadOnlyList<Vector2> profile, int segments, Vector3 offset = default)
        {
            for (int s = 0; s < segments; s++)
            {
                float a0 = s / (float)segments * Tau;
                float a1 = (s + 1) / (float)segments * Tau;
                var d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                var d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                Vector3 mid = (d0 + d1).normalized;

                for (int i = 0; i < profile.Count - 1; i++)
                {
                    Vector2 p0 = profile[i];
                    Vector2 p1 = profile[i + 1];
                    Vector3 outward = mid * (p1.y - p0.y) + Vector3.up * (p0.x - p1.x);

                    Vector3 v00 = offset + d0 * p0.x + Vector3.up * p0.y;
                    Vector3 v01 = offset + d1 * p0.x + Vector3.up * p0.y;
                    Vector3 v10 = offset + d0 * p1.x + Vector3.up * p1.y;
                    Vector3 v11 = offset + d1 * p1.x + Vector3.up * p1.y;
                    AddQuad(v00, v01, v11, v10, outward);
                }
            }
            return this;
        }

        /// <summary>Adds a jittered icosphere. Subdivision 0 = 20 faces, 1 = 80 faces.</summary>
        public LowPolyMeshBuilder AddIcosphere(Vector3 center, Vector3 scale, int subdivisions, float jitter, System.Random rng)
        {
            var points = new List<Vector3>();
            var faces = new List<int>(IcosahedronFaces);
            AddIcosahedronPoints(points);
            for (int i = 0; i < subdivisions; i++) faces = Subdivide(points, faces);

            for (int i = 0; i < points.Count; i++)
            {
                float noise = rng == null ? 0f : (float)(rng.NextDouble() * 2.0 - 1.0) * jitter;
                points[i] = Vector3.Scale(points[i].normalized * (1f + noise), scale);
            }

            for (int i = 0; i < faces.Count; i += 3)
            {
                Vector3 a = points[faces[i]], b = points[faces[i + 1]], c = points[faces[i + 2]];
                AddTriangle(center + a, center + b, center + c, (a + b + c) / 3f);
            }
            return this;
        }

        /// <summary>Adds a gable (triangular prism) roof: ridge runs along X, base sits at y = 0.</summary>
        public LowPolyMeshBuilder AddGable(float width, float depth, float height, Vector3 offset = default)
        {
            float hw = width * 0.5f, hd = depth * 0.5f;
            Vector3 center = offset + new Vector3(0f, height / 3f, 0f);
            Vector3 lf = offset + new Vector3(-hw, 0f, -hd), rf = offset + new Vector3(hw, 0f, -hd);
            Vector3 lb = offset + new Vector3(-hw, 0f, hd), rb = offset + new Vector3(hw, 0f, hd);
            Vector3 lt = offset + new Vector3(-hw, height, 0f), rt = offset + new Vector3(hw, height, 0f);

            AddQuad(lf, rf, rt, lt, Facing(center, lf, rf, rt));
            AddQuad(lb, rb, rt, lt, Facing(center, lb, rb, rt));
            AddQuad(lf, rf, rb, lb, Vector3.down);
            AddTriangle(lf, lb, lt, Vector3.left);
            AddTriangle(rf, rb, rt, Vector3.right);
            return this;
        }

        /// <summary>Adds a double-sided grass blade triangle.</summary>
        public void AddBlade(Vector3 baseLeft, Vector3 baseRight, Vector3 tip)
        {
            Vector3 normal = Vector3.Cross(baseRight - baseLeft, tip - baseLeft);
            AddTriangle(baseLeft, baseRight, tip, normal);
            AddTriangle(baseLeft, baseRight, tip, -normal);
        }

        /// <summary>Adds a jittered grid of ground triangles inside a circle, with heights from <paramref name="height"/>.</summary>
        public LowPolyMeshBuilder AddTerrain(float radius, float cellSize, float jitter, Func<float, float, float> height, System.Random rng)
        {
            int cells = Mathf.CeilToInt(radius * 2f / cellSize);
            var grid = new Vector3[cells + 1, cells + 1];
            for (int x = 0; x <= cells; x++)
            for (int z = 0; z <= cells; z++)
            {
                float px = -radius + x * cellSize + (float)(rng.NextDouble() * 2.0 - 1.0) * jitter * cellSize;
                float pz = -radius + z * cellSize + (float)(rng.NextDouble() * 2.0 - 1.0) * jitter * cellSize;
                grid[x, z] = new Vector3(px, height(px, pz), pz);
            }

            for (int x = 0; x < cells; x++)
            for (int z = 0; z < cells; z++)
            {
                Vector3 a = grid[x, z], b = grid[x + 1, z], c = grid[x + 1, z + 1], d = grid[x, z + 1];
                Vector3 cellCenter = (a + c) * 0.5f;
                if (new Vector2(cellCenter.x, cellCenter.z).magnitude > radius) continue;

                if ((x + z) % 2 == 0) AddQuad(a, b, c, d, Vector3.up);
                else AddQuad(b, c, d, a, Vector3.up);
            }
            return this;
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (_vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uvs);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private void AddVertex(Vector3 position, Vector3 normal)
        {
            _vertices.Add(position);
            _normals.Add(normal);
            _uvs.Add(new Vector2(position.x + position.z, position.y));
        }

        private static Vector3 Facing(Vector3 shapeCenter, Vector3 a, Vector3 b, Vector3 c)
        {
            return (a + b + c) / 3f - shapeCenter;
        }

        private static void AddIcosahedronPoints(List<Vector3> points)
        {
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            points.Add(new Vector3(-1, t, 0)); points.Add(new Vector3(1, t, 0));
            points.Add(new Vector3(-1, -t, 0)); points.Add(new Vector3(1, -t, 0));
            points.Add(new Vector3(0, -1, t)); points.Add(new Vector3(0, 1, t));
            points.Add(new Vector3(0, -1, -t)); points.Add(new Vector3(0, 1, -t));
            points.Add(new Vector3(t, 0, -1)); points.Add(new Vector3(t, 0, 1));
            points.Add(new Vector3(-t, 0, -1)); points.Add(new Vector3(-t, 0, 1));
            for (int i = 0; i < points.Count; i++) points[i] = points[i].normalized;
        }

        private static List<int> Subdivide(List<Vector3> points, List<int> faces)
        {
            var cache = new Dictionary<long, int>();
            var result = new List<int>(faces.Count * 4);
            for (int i = 0; i < faces.Count; i += 3)
            {
                int a = faces[i], b = faces[i + 1], c = faces[i + 2];
                int ab = Midpoint(points, cache, a, b);
                int bc = Midpoint(points, cache, b, c);
                int ca = Midpoint(points, cache, c, a);
                result.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }
            return result;
        }

        private static int Midpoint(List<Vector3> points, Dictionary<long, int> cache, int a, int b)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (cache.TryGetValue(key, out int index)) return index;

            points.Add(((points[a] + points[b]) * 0.5f).normalized);
            index = points.Count - 1;
            cache[key] = index;
            return index;
        }
    }
}
