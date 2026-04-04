// TerrainDeformationSystem.cs — SWEF Dynamic Terrain Events & Geological Phenomena (Phase 105)
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.TerrainEvents
{
    /// <summary>
    /// Phase 105 — Singleton MonoBehaviour that handles runtime terrain mesh modifications
    /// caused by geological events (volcanic caldera collapse, earthquake fault lines, etc.).
    ///
    /// <para>Modifications are stored as a list of <see cref="DeformationRecord"/> structs
    /// and can be replayed or undone independently. When no Unity Terrain component is
    /// present the system logs deformation data only (safe for EditMode tests).</para>
    /// </summary>
    public sealed class TerrainDeformationSystem : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static TerrainDeformationSystem Instance { get; private set; }

        #endregion

        #region Data

        /// <summary>Immutable record of a single deformation operation.</summary>
        public readonly struct DeformationRecord
        {
            /// <summary>World-space centre of the deformation.</summary>
            public readonly Vector3 centre;

            /// <summary>Radius affected in metres.</summary>
            public readonly float radius;

            /// <summary>Signed depth/height delta in metres (negative = crater, positive = uplift).</summary>
            public readonly float amount;

            /// <summary>Elapsed game time in seconds since Unity startup (from <c>Time.time</c>).</summary>
            public readonly float gameTime;

            public DeformationRecord(Vector3 centre, float radius, float amount, float gameTime)
            {
                this.centre   = centre;
                this.radius   = radius;
                this.amount   = amount;
                this.gameTime = gameTime;
            }
        }

        #endregion

        #region Inspector

        [Header("Deformation Settings")]
        [Tooltip("Maximum number of deformation records to retain.")]
        [Min(1)]
        public int maxRecords = 50;

        [Tooltip("Falloff curve applied radially from the deformation centre (x=normalised distance, y=multiplier).")]
        public AnimationCurve falloffCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        #endregion

        #region Public State

        /// <summary>Read-only list of all recorded deformations this session.</summary>
        public IReadOnlyList<DeformationRecord> records => _records;

        #endregion

        #region Private State

        private readonly List<DeformationRecord> _records = new List<DeformationRecord>();
        private Terrain _terrain;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _terrain = FindFirstObjectByType<Terrain>();
            if (_terrain == null)
                Debug.Log("[SWEF] TerrainDeformationSystem: no Unity Terrain found — deformations logged only.");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Applies a deformation centred on <paramref name="worldPos"/> with the given
        /// <paramref name="radius"/> and signed <paramref name="amount"/> (metres).
        /// </summary>
        public void ApplyDeformation(Vector3 worldPos, float radius, float amount)
        {
            var record = new DeformationRecord(worldPos, radius, amount, Time.time);

            if (_records.Count >= maxRecords)
                _records.RemoveAt(0);
            _records.Add(record);

            Debug.Log($"[SWEF] TerrainDeformationSystem: deformation at {worldPos} " +
                      $"radius={radius:F0} m, amount={amount:F1} m.");

            if (_terrain != null)
                ApplyToUnityTerrain(record);
        }

        /// <summary>Clears all deformation records (does not revert the mesh).</summary>
        public void ClearRecords() => _records.Clear();

        #endregion

        #region Unity Terrain Integration

        private void ApplyToUnityTerrain(DeformationRecord record)
        {
            TerrainData td        = _terrain.terrainData;
            int         resolution = td.heightmapResolution;
            Vector3     terrainPos = _terrain.transform.position;
            Vector3     terrainSize = td.size;

            // Convert world position to heightmap UV
            float normX = (record.centre.x - terrainPos.x) / terrainSize.x;
            float normZ = (record.centre.z - terrainPos.z) / terrainSize.z;

            int centreHX = Mathf.RoundToInt(normX * (resolution - 1));
            int centreHZ = Mathf.RoundToInt(normZ * (resolution - 1));

            // Half-width in heightmap texels
            int halfW = Mathf.CeilToInt((record.radius / terrainSize.x) * (resolution - 1));

            int startX = Mathf.Clamp(centreHX - halfW, 0, resolution - 1);
            int startZ = Mathf.Clamp(centreHZ - halfW, 0, resolution - 1);
            int endX   = Mathf.Clamp(centreHX + halfW, 0, resolution - 1);
            int endZ   = Mathf.Clamp(centreHZ + halfW, 0, resolution - 1);

            int width  = endX - startX + 1;
            int height = endZ - startZ + 1;

            float[,] heights = td.GetHeights(startX, startZ, width, height);

            float normalised = record.amount / terrainSize.y;

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (startX + x - centreHX) / (float)halfW;
                    float dz = (startZ + z - centreHZ) / (float)halfW;
                    float dist = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dz * dz));
                    float falloff = falloffCurve.Evaluate(dist);
                    heights[z, x] = Mathf.Clamp01(heights[z, x] + normalised * falloff);
                }
            }

            td.SetHeights(startX, startZ, heights);
        }

        #endregion
    }
}
