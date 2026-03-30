using System.Collections.Generic;
using UnityEngine;

namespace HexStrategy.Board
{
    public static class HexMeshUtility
    {
        private const int CornerCount = 6;
        private const float AngleOffsetDegrees = 30f;

        private static readonly Dictionary<string, Mesh> MeshCache = new();

        public static Mesh CreateHexPrismMesh(float outerRadius, float height)
        {
            string cacheKey = $"{outerRadius:0.###}_{height:0.###}";

            if (MeshCache.TryGetValue(cacheKey, out Mesh cachedMesh))
            {
                return cachedMesh;
            }

            Mesh mesh = new Mesh
            {
                name = $"HexPrism_{outerRadius:0.##}_{height:0.##}"
            };

            float topY = height * 0.5f;
            float bottomY = -topY;

            List<Vector3> vertices = new List<Vector3>(38);
            List<int> triangles = new List<int>(60);

            // Build the top cap first as a center vertex plus 6 corner vertices.
            int topCenterIndex = vertices.Count;
            vertices.Add(new Vector3(0f, topY, 0f));

            for (int i = 0; i < CornerCount; i++)
            {
                Vector3 corner = GetCorner(outerRadius, i);
                vertices.Add(new Vector3(corner.x, topY, corner.z));
            }

            for (int i = 0; i < CornerCount; i++)
            {
                int current = topCenterIndex + 1 + i;
                int next = topCenterIndex + 1 + ((i + 1) % CornerCount);
                triangles.Add(topCenterIndex);
                triangles.Add(next);
                triangles.Add(current);
            }

            // Bottom cap uses reversed winding so its normals face downward.
            int bottomCenterIndex = vertices.Count;
            vertices.Add(new Vector3(0f, bottomY, 0f));

            for (int i = 0; i < CornerCount; i++)
            {
                Vector3 corner = GetCorner(outerRadius, i);
                vertices.Add(new Vector3(corner.x, bottomY, corner.z));
            }

            for (int i = 0; i < CornerCount; i++)
            {
                int current = bottomCenterIndex + 1 + i;
                int next = bottomCenterIndex + 1 + ((i + 1) % CornerCount);
                triangles.Add(bottomCenterIndex);
                triangles.Add(current);
                triangles.Add(next);
            }

            // Each side gets its own quad so Unity can generate clean outward-facing normals.
            for (int i = 0; i < CornerCount; i++)
            {
                Vector3 currentCorner = GetCorner(outerRadius, i);
                Vector3 nextCorner = GetCorner(outerRadius, (i + 1) % CornerCount);

                int sideStart = vertices.Count;

                vertices.Add(new Vector3(currentCorner.x, topY, currentCorner.z));
                vertices.Add(new Vector3(nextCorner.x, topY, nextCorner.z));
                vertices.Add(new Vector3(currentCorner.x, bottomY, currentCorner.z));
                vertices.Add(new Vector3(nextCorner.x, bottomY, nextCorner.z));

                triangles.Add(sideStart);
                triangles.Add(sideStart + 1);
                triangles.Add(sideStart + 2);

                triangles.Add(sideStart + 1);
                triangles.Add(sideStart + 3);
                triangles.Add(sideStart + 2);
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MeshCache[cacheKey] = mesh;
            return mesh;
        }

        private static Vector3 GetCorner(float outerRadius, int cornerIndex)
        {
            // The 30 degree offset gives us a pointy-top hex layout, which matches the board placement math.
            float angle = Mathf.Deg2Rad * (AngleOffsetDegrees + 60f * cornerIndex);
            return new Vector3(Mathf.Cos(angle) * outerRadius, 0f, Mathf.Sin(angle) * outerRadius);
        }
    }
}
