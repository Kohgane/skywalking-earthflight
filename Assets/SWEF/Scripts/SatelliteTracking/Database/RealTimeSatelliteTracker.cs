// RealTimeSatelliteTracker.cs — Phase 114: Satellite & Space Debris Tracking
// Real-time satellite position updates from TLE data with periodic refresh.
// Namespace: SWEF.SatelliteTracking

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Maintains a live-updating position table for all registered satellites by
    /// periodically fetching TLE updates and propagating positions in real time.
    /// </summary>
    public class RealTimeSatelliteTracker : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Data Provider")]
        [Tooltip("Satellite data provider component (e.g. CelesTrakProvider or MockSatelliteDataProvider).")]
        [SerializeField] private SatelliteDataProvider dataProvider;

        [Header("Timing")]
        [Tooltip("How often (seconds) positions are recalculated.")]
        [Range(0.5f, 60f)]
        [SerializeField] private float positionUpdateRate = 2f;

        [Tooltip("Whether to request a TLE refresh on startup.")]
        [SerializeField] private bool fetchOnStart = true;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised each time all positions have been updated.</summary>
        public event Action<IReadOnlyList<SatelliteRecord>> OnPositionsUpdated;

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly List<SatelliteRecord> _records = new List<SatelliteRecord>();
        private OrbitalMechanicsEngine _engine;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _engine = FindObjectOfType<OrbitalMechanicsEngine>();
        }

        private void Start()
        {
            if (dataProvider != null)
            {
                dataProvider.OnDataReceived += HandleTLEData;
                dataProvider.OnFetchError   += err => Debug.LogWarning($"[Tracker] {err}");
                if (fetchOnStart) dataProvider.FetchLatestTLEs();
            }
            else
            {
                // Fall back to mock provider
                var mock = gameObject.AddComponent<MockSatelliteDataProvider>();
                mock.OnDataReceived += HandleTLEData;
                mock.FetchLatestTLEs();
            }

            StartCoroutine(PositionUpdateLoop());
        }

        private void OnDestroy()
        {
            if (dataProvider != null)
                dataProvider.OnDataReceived -= HandleTLEData;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns a snapshot of all tracked satellite records.</summary>
        public IReadOnlyList<SatelliteRecord> GetRecords() => _records.AsReadOnly();

        /// <summary>Forces an immediate TLE refresh from the configured provider.</summary>
        public void RequestTLERefresh()
        {
            if (dataProvider != null) dataProvider.FetchLatestTLEs();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void HandleTLEData(List<TLEData> tles)
        {
            _records.Clear();
            foreach (var tle in tles)
            {
                var record = new SatelliteRecord
                {
                    name          = tle.name,
                    noradId       = tle.noradId,
                    satelliteType = SatelliteType.Communication, // Default; filter can refine
                    status        = SatelliteStatus.Active,
                    tle           = tle
                };
                _records.Add(record);

                // Also register with the manager if present
                SatelliteTrackingManager.Instance?.RegisterSatellite(record);
            }
            Debug.Log($"[RealTimeSatelliteTracker] Loaded {_records.Count} TLE records.");
        }

        private IEnumerator PositionUpdateLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(positionUpdateRate);
                if (_engine == null) continue;

                var now = DateTime.UtcNow;
                foreach (var record in _records)
                {
                    if (record.tle == null) continue;
                    record.currentState = _engine.Propagate(record.tle, now);

                    // Classify orbit from altitude
                    if (record.currentState != null)
                        record.orbitType = OrbitalMechanicsEngine.ClassifyOrbit(record.currentState.altitudeKm);
                }
                OnPositionsUpdated?.Invoke(_records.AsReadOnly());
            }
        }
    }
}
