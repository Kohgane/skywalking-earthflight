// SpaceDebrisManager.cs — Phase 114: Satellite & Space Debris Tracking
// Space debris field simulation: debris density by orbit altitude, collision probability.
// Namespace: SWEF.SatelliteTracking

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Manages the space debris simulation: procedural debris population by altitude
    /// shell, density model, and collision probability estimation.
    /// </summary>
    public class SpaceDebrisManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Shared singleton instance.</summary>
        public static SpaceDebrisManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Debris Density Model")]
        [Tooltip("Altitude shells (km) at which debris density peaks.")]
        [SerializeField] private float[] densityPeakAltitudesKm = { 800f, 1400f, 900f };

        [Tooltip("Relative density weight at each peak altitude.")]
        [SerializeField] private float[] densityWeights = { 1.0f, 0.6f, 0.8f };

        [Tooltip("Gaussian half-width (km) of each density peak.")]
        [SerializeField] private float[] densityWidthsKm = { 100f, 150f, 80f };

        [Header("Population")]
        [Tooltip("Total debris objects simulated across all shells.")]
        [Range(0, 50000)]
        [SerializeField] private int totalDebrisObjects = 5000;

        [SerializeField] private SatelliteTrackingConfig config;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when the debris population is (re)generated.</summary>
        public event Action<IReadOnlyList<DebrisObject>> OnDebrisGenerated;

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly List<DebrisObject> _debris = new List<DebrisObject>();
        private int _nextId = 1;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            GenerateDebrisField();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns all currently simulated debris objects.</summary>
        public IReadOnlyList<DebrisObject> GetAllDebris() => _debris.AsReadOnly();

        /// <summary>
        /// Returns all debris within the given altitude range (km).
        /// </summary>
        public List<DebrisObject> GetDebrisInAltitudeRange(float minKm, float maxKm)
        {
            var result = new List<DebrisObject>();
            foreach (var d in _debris)
                if (d.altitudeKm >= minKm && d.altitudeKm <= maxKm)
                    result.Add(d);
            return result;
        }

        /// <summary>
        /// Estimates the debris number density (objects/km³) at the given altitude.
        /// </summary>
        public float GetDensityAtAltitude(float altitudeKm)
        {
            float total = 0f;
            for (int i = 0; i < densityPeakAltitudesKm.Length; i++)
            {
                float diff = altitudeKm - densityPeakAltitudesKm[i];
                total += densityWeights[i]
                       * Mathf.Exp(-0.5f * (diff / densityWidthsKm[i]) * (diff / densityWidthsKm[i]));
            }
            return total;
        }

        /// <summary>
        /// Estimates the collision probability for an object at the given altitude
        /// with cross-section area (m²) over a 24-hour exposure period.
        /// </summary>
        public float EstimateCollisionProbability(float altitudeKm, float crossSectionM2,
                                                   float exposureHours = 24f)
        {
            float density   = GetDensityAtAltitude(altitudeKm);            // objects/km³
            float fluxKm2   = density * 7.8f;                               // average relative speed km/s × density
            float csKm2     = crossSectionM2 * 1e-6f;                       // m² → km²
            float exposureSec = exposureHours * 3600f;
            return 1f - Mathf.Exp(-fluxKm2 * csKm2 * exposureSec);
        }

        /// <summary>Regenerates the debris field using the current density model.</summary>
        public void GenerateDebrisField()
        {
            _debris.Clear();
            _nextId = 1;

            float multiplier = config != null ? config.debrisDensityMultiplier : 1f;
            int count = Mathf.RoundToInt(totalDebrisObjects * multiplier);

            var gen = FindObjectOfType<DebrisGenerator>() ?? gameObject.AddComponent<DebrisGenerator>();

            for (int i = 0; i < count; i++)
            {
                var d = gen.GenerateOne(_nextId++, this);
                if (d != null) _debris.Add(d);
            }

            OnDebrisGenerated?.Invoke(_debris.AsReadOnly());
            Debug.Log($"[SpaceDebrisManager] Generated {_debris.Count} debris objects.");
        }
    }
}
