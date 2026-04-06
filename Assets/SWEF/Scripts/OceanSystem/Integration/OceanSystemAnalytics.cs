// OceanSystemAnalytics.cs — Phase 117: Advanced Ocean & Maritime System
// Telemetry: water landings, carrier traps, SAR completions, maritime missions.
// Namespace: SWEF.OceanSystem

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Collects and aggregates telemetry for the Ocean &amp; Maritime
    /// System. Tracks water landings, carrier traps/bolters, SAR completions,
    /// and maritime mission statistics.
    /// </summary>
    public class OceanSystemAnalytics : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────

        /// <summary>Shared singleton instance.</summary>
        public static OceanSystemAnalytics Instance { get; private set; }

        // ── Private counters ──────────────────────────────────────────────────────

        private int   _totalWaterLandings;
        private int   _successfulWaterLandings;
        private int   _emergencyDitchings;
        private int   _carrierTraps;
        private int   _carrierBolters;
        private int   _sarCompletions;
        private int   _patrolCompletions;
        private int   _cargoDeliveries;
        private float _totalSeaTimeSeconds;

        private readonly List<WaterLandingRecord> _landingHistory = new List<WaterLandingRecord>();
        private readonly List<CarrierTrapRecord>  _trapHistory    = new List<CarrierTrapRecord>();

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            var mgr = OceanSystemManager.Instance;
            if (mgr == null) return;
            mgr.OnWaterLandingCompleted += RecordWaterLanding;
            mgr.OnCarrierTrapRecorded   += RecordCarrierTrap;
        }

        private void OnDisable()
        {
            var mgr = OceanSystemManager.Instance;
            if (mgr == null) return;
            mgr.OnWaterLandingCompleted -= RecordWaterLanding;
            mgr.OnCarrierTrapRecorded   -= RecordCarrierTrap;
        }

        private void Update()
        {
            var mgr = OceanSystemManager.Instance;
            if (mgr != null && mgr.CurrentRegion == OceanRegion.OpenOcean)
                _totalSeaTimeSeconds += Time.deltaTime;
        }

        // ── Record Methods ────────────────────────────────────────────────────────

        private void RecordWaterLanding(WaterLandingRecord record)
        {
            _totalWaterLandings++;
            if (record.success) _successfulWaterLandings++;
            if (record.landingType == WaterLandingType.Emergency) _emergencyDitchings++;
            _landingHistory.Add(record);
        }

        private void RecordCarrierTrap(CarrierTrapRecord record)
        {
            if (record.wasBolter) _carrierBolters++;
            else                  _carrierTraps++;
            _trapHistory.Add(record);
        }

        /// <summary>Records a SAR mission completion.</summary>
        public void RecordSARCompletion() => _sarCompletions++;

        /// <summary>Records a patrol mission completion.</summary>
        public void RecordPatrolCompletion() => _patrolCompletions++;

        /// <summary>Records a cargo delivery completion.</summary>
        public void RecordCargoDelivery() => _cargoDeliveries++;

        // ── Public Read Properties ────────────────────────────────────────────────

        /// <summary>Total water landing attempts.</summary>
        public int TotalWaterLandings => _totalWaterLandings;

        /// <summary>Successful water landings.</summary>
        public int SuccessfulWaterLandings => _successfulWaterLandings;

        /// <summary>Emergency ditching events.</summary>
        public int EmergencyDitchings => _emergencyDitchings;

        /// <summary>Total carrier traps (successful wire engagements).</summary>
        public int CarrierTraps => _carrierTraps;

        /// <summary>Total carrier bolters.</summary>
        public int CarrierBolters => _carrierBolters;

        /// <summary>SAR missions completed.</summary>
        public int SARCompletions => _sarCompletions;

        /// <summary>Patrol missions completed.</summary>
        public int PatrolCompletions => _patrolCompletions;

        /// <summary>Cargo deliveries completed.</summary>
        public int CargoDeliveries => _cargoDeliveries;

        /// <summary>Total time spent over open ocean in seconds.</summary>
        public float TotalSeaTimeSeconds => _totalSeaTimeSeconds;

        /// <summary>Water landing success rate (0–1).</summary>
        public float WaterLandingSuccessRate =>
            _totalWaterLandings > 0 ? (float)_successfulWaterLandings / _totalWaterLandings : 0f;

        /// <summary>Carrier trap rate (traps / (traps + bolters)).</summary>
        public float CarrierTrapRate =>
            (_carrierTraps + _carrierBolters) > 0
                ? (float)_carrierTraps / (_carrierTraps + _carrierBolters)
                : 0f;

        /// <summary>Read-only history of water landings.</summary>
        public IReadOnlyList<WaterLandingRecord> LandingHistory => _landingHistory;

        /// <summary>Read-only history of carrier trap events.</summary>
        public IReadOnlyList<CarrierTrapRecord> TrapHistory => _trapHistory;
    }
}
