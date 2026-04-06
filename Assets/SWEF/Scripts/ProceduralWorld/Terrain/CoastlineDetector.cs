// CoastlineDetector.cs — Phase 113: Procedural City & Airport Generation
// Detect coastlines for coastal cities and seaplane bases.
// Namespace: SWEF.ProceduralWorld

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Detects the presence and direction of coastlines near a world position
    /// by comparing terrain elevation with sea level.
    /// </summary>
    public class CoastlineDetector : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Sea Level")]
        [Tooltip("World-space Y coordinate considered to be sea level.")]
        [SerializeField] private float seaLevelY = 0f;

        [Header("Detection")]
        [Tooltip("Number of radial samples used to detect coastline transitions.")]
        [SerializeField] private int radialSamples = 36;

        [Tooltip("Search radius in metres.")]
        [SerializeField] private float searchRadius = 2000f;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> if a land-to-sea transition is detected within
        /// <see cref="searchRadius"/> of <paramref name="worldPos"/>.
        /// </summary>
        public bool IsCoastal(Vector3 worldPos)
        {
            bool landFound = false;
            bool waterFound = false;

            for (int i = 0; i < radialSamples; i++)
            {
                float angle = (float)i / radialSamples * 360f * Mathf.Deg2Rad;
                Vector3 sample = worldPos + new Vector3(
                    Mathf.Cos(angle) * searchRadius, 0f,
                    Mathf.Sin(angle) * searchRadius);

                float elev = SampleElevation(sample);
                if (elev > seaLevelY) landFound = true;
                else waterFound = true;

                if (landFound && waterFound) return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the approximate direction (in world space) toward the nearest water body,
        /// or <c>Vector3.zero</c> if none is detected.
        /// </summary>
        public Vector3 CoastlineDirection(Vector3 worldPos)
        {
            Vector3 towardWater = Vector3.zero;
            int waterCount = 0;

            for (int i = 0; i < radialSamples; i++)
            {
                float angle = (float)i / radialSamples * 360f * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 sample = worldPos + dir * searchRadius;

                if (SampleElevation(sample) <= seaLevelY)
                {
                    towardWater += dir;
                    waterCount++;
                }
            }

            return waterCount > 0 ? towardWater.normalized : Vector3.zero;
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private float SampleElevation(Vector3 pos)
        {
            // Try terrain first
            var terrain = Terrain.activeTerrain;
            if (terrain != null) return terrain.SampleHeight(pos);

            // Fall back to raycast
            if (Physics.Raycast(new Vector3(pos.x, seaLevelY + 5000f, pos.z), Vector3.down,
                    out RaycastHit hit, 10000f))
                return hit.point.y;

            return seaLevelY;
        }
    }
}
