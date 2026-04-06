// UVProjectionController.cs — Phase 115: Advanced Aircraft Livery Editor
// Paint projection: 3D-to-UV mapping for direct painting on 3D model.
// Namespace: SWEF.LiveryEditor

using System;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Projects 3-D paint operations onto the UV canvas by converting
    /// world-space paint positions to normalised UV coordinates via raycasting.
    /// </summary>
    public class UVProjectionController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [SerializeField] private Camera previewCamera;
        [SerializeField] private LayerMask aircraftLayerMask = ~0;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when a screen position maps successfully to a UV coordinate.</summary>
        public event Action<Vector2> OnUVHit;

        // ── Public properties ─────────────────────────────────────────────────────
        /// <summary>Whether the projection controller is actively mapping paint.</summary>
        public bool IsActive { get; set; }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to project a screen-space position onto the aircraft mesh and
        /// returns the UV coordinate at the hit point.
        /// </summary>
        /// <param name="screenPos">Screen-space mouse or touch position.</param>
        /// <param name="uv">Output UV coordinate (valid only if the method returns <c>true</c>).</param>
        /// <returns><c>true</c> if the ray hit the aircraft mesh.</returns>
        public bool ScreenToUV(Vector2 screenPos, out Vector2 uv)
        {
            uv = Vector2.zero;
            if (previewCamera == null) return false;

            Ray ray = previewCamera.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, aircraftLayerMask)) return false;

            uv = hit.textureCoord;
            OnUVHit?.Invoke(uv);
            return true;
        }

        /// <summary>
        /// Converts a world-space position on the aircraft surface to a UV coordinate
        /// using the mesh's UV data.
        /// </summary>
        /// <param name="worldPos">World-space surface position.</param>
        /// <param name="meshFilter">Mesh filter of the aircraft surface.</param>
        /// <param name="uv">Output UV coordinate.</param>
        /// <returns><c>true</c> if the projection succeeds.</returns>
        public bool WorldToUV(Vector3 worldPos, MeshFilter meshFilter, out Vector2 uv)
        {
            uv = Vector2.zero;
            if (meshFilter == null || meshFilter.sharedMesh == null) return false;

            var mesh = meshFilter.sharedMesh;
            if (mesh.uv == null || mesh.uv.Length == 0) return false;

            // Find the closest UV by brute-forcing all triangles.
            Vector3 local = meshFilter.transform.InverseTransformPoint(worldPos);
            Vector3[] verts = mesh.vertices;
            int[]     tris  = mesh.triangles;
            Vector2[] uvs   = mesh.uv;

            float minDist = float.MaxValue;
            uv = Vector2.zero;

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 v0 = verts[tris[i]];
                Vector3 v1 = verts[tris[i + 1]];
                Vector3 v2 = verts[tris[i + 2]];

                Vector3 closest = ClosestPointOnTriangle(local, v0, v1, v2);
                float dist = Vector3.SqrMagnitude(closest - local);

                if (dist < minDist)
                {
                    minDist = dist;
                    // Barycentric interpolation of UVs.
                    Vector3 bary = Barycentric(closest, v0, v1, v2);
                    uv = uvs[tris[i]]     * bary.x
                       + uvs[tris[i + 1]] * bary.y
                       + uvs[tris[i + 2]] * bary.z;
                }
            }

            return true;
        }

        // ── Geometry helpers ──────────────────────────────────────────────────────

        private static Vector3 ClosestPointOnTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a, ac = c - a, ap = p - a;
            float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0f && d2 <= 0f) return a;

            Vector3 bp = p - b;
            float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0f && d4 <= d3) return b;

            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0f && d1 >= 0f && d3 <= 0f)
            {
                float v = d1 / (d1 - d3);
                return a + v * ab;
            }

            Vector3 cp = p - c;
            float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0f && d5 <= d6) return c;

            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0f && d2 >= 0f && d6 <= 0f)
            {
                float w = d2 / (d2 - d6);
                return a + w * ac;
            }

            float va = d3 * d6 - d5 * d4;
            if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
            {
                float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return b + w * (c - b);
            }

            float denom = 1f / (va + vb + vc);
            {
                float v = vb * denom, w = vc * denom;
                return a + ab * v + ac * w;
            }
        }

        private static Vector3 Barycentric(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 v0 = b - a, v1 = c - a, v2 = p - a;
            float d00 = Vector3.Dot(v0, v0);
            float d01 = Vector3.Dot(v0, v1);
            float d11 = Vector3.Dot(v1, v1);
            float d20 = Vector3.Dot(v2, v0);
            float d21 = Vector3.Dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;
            if (Mathf.Abs(denom) < 1e-8f) return new Vector3(1f / 3f, 1f / 3f, 1f / 3f);
            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            return new Vector3(1f - v - w, v, w);
        }
    }
}
