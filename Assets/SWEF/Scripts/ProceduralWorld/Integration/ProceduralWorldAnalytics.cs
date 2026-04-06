// ProceduralWorldAnalytics.cs — Phase 113: Procedural City & Airport Generation
// Generation telemetry: generation time, memory usage, LOD distribution,
// chunk load/unload stats.
// Namespace: SWEF.ProceduralWorld

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Collects and reports telemetry for the procedural world generation system.
    /// Tracks generation times, memory usage, LOD distribution, and chunk events.
    /// </summary>
    public static class ProceduralWorldAnalytics
    {
        // ── Counters ──────────────────────────────────────────────────────────────
        private static int _citiesGenerated;
        private static int _airportsGenerated;
        private static int _chunksLoaded;
        private static int _chunksUnloaded;
        private static float _totalCityGenTime;
        private static float _totalAirportGenTime;

        private static readonly Dictionary<LODLevel, int> LodDistribution =
            new Dictionary<LODLevel, int>
            {
                { LODLevel.LOD0, 0 },
                { LODLevel.LOD1, 0 },
                { LODLevel.LOD2, 0 },
                { LODLevel.LOD3, 0 }
            };

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Records a successfully generated city.</summary>
        public static void TrackCityGenerated(CityDescription city)
        {
            _citiesGenerated++;
#if SWEF_ANALYTICS_AVAILABLE
            Debug.Log($"[Analytics] City generated: {city.cityName} ({city.cityType}) pop={city.population}");
#endif
        }

        /// <summary>Records a successfully generated airport.</summary>
        public static void TrackAirportGenerated(AirportLayout airport)
        {
            _airportsGenerated++;
#if SWEF_ANALYTICS_AVAILABLE
            Debug.Log($"[Analytics] Airport generated: {airport.icaoCode} ({airport.airportType}) runways={airport.runways.Count}");
#endif
        }

        /// <summary>Records a chunk load event.</summary>
        public static void TrackChunkLoaded(ChunkCoord coord)
        {
            _chunksLoaded++;
        }

        /// <summary>Records a chunk unload event.</summary>
        public static void TrackChunkUnloaded(ChunkCoord coord)
        {
            _chunksUnloaded++;
        }

        /// <summary>Records a LOD level assignment for a building.</summary>
        public static void TrackLODAssignment(LODLevel level)
        {
            if (LodDistribution.ContainsKey(level))
                LodDistribution[level]++;
        }

        /// <summary>Records city generation duration in seconds.</summary>
        public static void TrackCityGenTime(float seconds) => _totalCityGenTime += seconds;

        /// <summary>Records airport generation duration in seconds.</summary>
        public static void TrackAirportGenTime(float seconds) => _totalAirportGenTime += seconds;

        /// <summary>Resets all analytics counters.</summary>
        public static void Reset()
        {
            _citiesGenerated = 0;
            _airportsGenerated = 0;
            _chunksLoaded = 0;
            _chunksUnloaded = 0;
            _totalCityGenTime = 0f;
            _totalAirportGenTime = 0f;
            foreach (var key in new List<LODLevel>(LodDistribution.Keys))
                LodDistribution[key] = 0;
        }

        // ── Accessors (for tests and debug panels) ────────────────────────────────
        /// <summary>Total cities generated since last reset.</summary>
        public static int CitiesGenerated => _citiesGenerated;

        /// <summary>Total airports generated since last reset.</summary>
        public static int AirportsGenerated => _airportsGenerated;

        /// <summary>Total chunks loaded since last reset.</summary>
        public static int ChunksLoaded => _chunksLoaded;

        /// <summary>Total chunks unloaded since last reset.</summary>
        public static int ChunksUnloaded => _chunksUnloaded;

        /// <summary>Returns the LOD distribution dictionary (read-only copy).</summary>
        public static IReadOnlyDictionary<LODLevel, int> GetLODDistribution() =>
            new Dictionary<LODLevel, int>(LodDistribution);
    }
}
