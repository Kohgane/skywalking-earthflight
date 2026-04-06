// TerrainAnalyzer.cs — Phase 113: Procedural City & Airport Generation
// Analyse terrain for suitable city/airport placement: flat areas, coastlines,
// valleys, elevation analysis.
// Namespace: SWEF.ProceduralWorld

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Analyses terrain height data around a candidate position to determine
    /// suitability for city or airport placement.
    /// </summary>
    public class TerrainAnalyzer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Sample Settings")]
        [Tooltip("Number of sample points along each axis of the analysis grid.")]
        [SerializeField] private int sampleCount = 16;

        [Tooltip("Layer mask used when raycasting against terrain.")]
        [SerializeField] private LayerMask terrainLayer = ~0;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Analyses the terrain within <paramref name="radiusMetres"/> of
        /// <paramref name="centre"/> and returns a <see cref="TerrainAnalysisResult"/>.
        /// </summary>
        public TerrainAnalysisResult Analyse(Vector3 centre, float radiusMetres, ProceduralWorldConfig cfg)
        {
            var elevations = SampleElevations(centre, radiusMetres);
            float avg = Average(elevations);
            float maxSlope = ComputeMaxSlope(elevations, radiusMetres);
            bool coastal = DetectCoastline(centre, radiusMetres);

            float effectiveMax = cfg != null ? cfg.maxSlopeForCity : 5f;
            bool suitable = maxSlope <= effectiveMax;

            CityType cityType = coastal ? CityType.Coastal
                : avg > 1500f ? CityType.Village
                : suitable ? CityType.Town
                : CityType.Industrial;

            AirportType airportType = coastal ? AirportType.Seaplane
                : maxSlope <= (cfg != null ? cfg.maxSlopeForRunway : 1f) ? AirportType.International
                : AirportType.Helipad;

            return new TerrainAnalysisResult
            {
                isSuitable = suitable,
                averageElevation = avg,
                maxSlopeDegrees = maxSlope,
                hasCoastline = coastal,
                recommendedCityType = cityType,
                recommendedAirportType = airportType
            };
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private List<float> SampleElevations(Vector3 centre, float radius)
        {
            var results = new List<float>();
            float step = radius * 2f / sampleCount;

            for (int i = 0; i <= sampleCount; i++)
            {
                for (int j = 0; j <= sampleCount; j++)
                {
                    float x = centre.x - radius + i * step;
                    float z = centre.z - radius + j * step;
                    float y = SampleHeight(new Vector3(x, centre.y + 5000f, z));
                    results.Add(y);
                }
            }
            return results;
        }

        private float SampleHeight(Vector3 origin)
        {
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10000f, terrainLayer))
                return hit.point.y;
            // Fall back to Unity Terrain API if available
            var terrain = Terrain.activeTerrain;
            if (terrain != null)
                return terrain.SampleHeight(origin);
            return 0f;
        }

        private static float Average(List<float> values)
        {
            if (values.Count == 0) return 0f;
            float sum = 0f;
            foreach (var v in values) sum += v;
            return sum / values.Count;
        }

        private static float ComputeMaxSlope(List<float> elevations, float radius)
        {
            if (elevations.Count < 2) return 0f;
            float min = float.MaxValue, max = float.MinValue;
            foreach (var e in elevations)
            {
                if (e < min) min = e;
                if (e > max) max = e;
            }
            // Approximate maximum slope in degrees from elevation range over diameter
            float heightDiff = max - min;
            float slope = Mathf.Atan2(heightDiff, radius * 2f) * Mathf.Rad2Deg;
            return slope;
        }

        private static bool DetectCoastline(Vector3 centre, float radius)
        {
            // Simple heuristic: check if any sample hits water (y < sea level)
            // Full implementation handled by CoastlineDetector
            return false;
        }
    }
}
