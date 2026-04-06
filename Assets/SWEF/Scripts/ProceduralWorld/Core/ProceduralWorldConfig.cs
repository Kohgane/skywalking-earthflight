// ProceduralWorldConfig.cs — Phase 113: Procedural City & Airport Generation
// ScriptableObject holding all runtime-configurable procedural world parameters.
// Namespace: SWEF.ProceduralWorld

using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// ScriptableObject configuration asset for the procedural world generation system.
    /// Create via <em>Assets → Create → SWEF → ProceduralWorld → World Config</em>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/ProceduralWorld/World Config", fileName = "ProceduralWorldConfig")]
    public class ProceduralWorldConfig : ScriptableObject
    {
        // ── Generation Density ────────────────────────────────────────────────────

        [Header("Generation Density")]
        /// <summary>Global density multiplier for building placement [0.1 .. 2.0].</summary>
        [Range(0.1f, 2f)]
        [Tooltip("Multiplier applied to all building density values.")]
        public float generationDensity = 1f;

        /// <summary>Frequency of airport spawning per world tile.</summary>
        [Range(0f, 1f)]
        [Tooltip("Probability that a tile contains an airport [0..1].")]
        public float airportFrequency = 0.05f;

        /// <summary>Density of road grid lines per km².</summary>
        [Range(1f, 20f)]
        [Tooltip("Number of major road grid lines per kilometre.")]
        public float roadGridDensity = 4f;

        // ── Building Height ───────────────────────────────────────────────────────

        [Header("Building Height Ranges")]
        /// <summary>Minimum number of floors on any building.</summary>
        [Range(1, 5)]
        public int minFloors = 1;

        /// <summary>Maximum number of floors on tall commercial buildings.</summary>
        [Range(5, 200)]
        public int maxFloors = 60;

        /// <summary>Height in metres per floor.</summary>
        [Range(2f, 5f)]
        public float metresPerFloor = 3f;

        // ── LOD Distances ─────────────────────────────────────────────────────────

        [Header("LOD Distances (metres)")]
        /// <summary>Distance at which buildings switch from LOD0 to LOD1.</summary>
        [Range(100f, 2000f)]
        public float lod1Distance = 500f;

        /// <summary>Distance at which buildings switch from LOD1 to LOD2 (billboard).</summary>
        [Range(500f, 10000f)]
        public float lod2Distance = 2000f;

        /// <summary>Distance at which buildings switch from LOD2 to LOD3 (batch/sprite).</summary>
        [Range(2000f, 30000f)]
        public float lod3Distance = 8000f;

        /// <summary>Distance beyond which city chunks are unloaded entirely.</summary>
        [Range(5000f, 50000f)]
        public float chunkUnloadDistance = 20000f;

        // ── Chunk Settings ────────────────────────────────────────────────────────

        [Header("Chunk Settings")]
        /// <summary>Edge length of a single world chunk in metres.</summary>
        [Range(500f, 5000f)]
        public float chunkSizeMetres = 2000f;

        /// <summary>Number of chunks preloaded around the player.</summary>
        [Range(1, 5)]
        public int preloadRadius = 2;

        // ── Terrain Analysis ──────────────────────────────────────────────────────

        [Header("Terrain Analysis")]
        /// <summary>Maximum slope angle (degrees) considered flat for city placement.</summary>
        [Range(1f, 15f)]
        public float maxSlopeForCity = 5f;

        /// <summary>Maximum slope angle (degrees) considered flat for runway placement.</summary>
        [Range(0.1f, 3f)]
        public float maxSlopeForRunway = 1f;

        /// <summary>Sample radius used when analysing terrain flatness (metres).</summary>
        [Range(100f, 2000f)]
        public float terrainSampleRadius = 500f;

        // ── Noise Parameters ──────────────────────────────────────────────────────

        [Header("Noise / Zoning")]
        /// <summary>Scale of the Perlin noise used for zoning maps.</summary>
        [Range(0.00001f, 0.001f)]
        public float noiseScale = 0.0001f;

        /// <summary>Number of octaves in the zoning noise.</summary>
        [Range(1, 8)]
        public int noiseOctaves = 4;

        /// <summary>Persistence for noise octave layering.</summary>
        [Range(0.1f, 1f)]
        public float noisePersistence = 0.5f;
    }
}
