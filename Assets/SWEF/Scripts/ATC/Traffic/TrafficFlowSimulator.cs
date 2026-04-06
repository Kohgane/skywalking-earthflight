// TrafficFlowSimulator.cs — Phase 119: Advanced AI Traffic Control
// AI traffic simulation: departures, arrivals, overflights, holding patterns,
// realistic traffic density.
// Namespace: SWEF.ATC

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Simulates realistic AI traffic flow including scheduled departures,
    /// arrivals and en-route overflights.
    /// </summary>
    public class TrafficFlowSimulator : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────

        /// <summary>Shared instance of <see cref="TrafficFlowSimulator"/>.</summary>
        public static TrafficFlowSimulator Instance { get; private set; }

        [Header("Simulation")]
        [SerializeField] [Range(0f, 1f)] private float trafficDensity = 0.5f;
        [SerializeField] private float spawnIntervalSeconds = 60f;

        private float _nextSpawnTime;
        private int _spawnedCount;
        private readonly List<string> _activeFlights = new List<string>();

        private static readonly string[] AircraftTypes = { "B738", "A320", "B77W", "A388", "E190", "CRJ9" };
        private static readonly string[] ICAOs = { "KLAX", "KJFK", "EGLL", "EDDF", "RJTT", "YSSY" };

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when a simulated flight is spawned.</summary>
        public event Action<FlightStrip> OnFlightSpawned;

        /// <summary>Raised when a simulated flight completes and is removed.</summary>
        public event Action<string> OnFlightCompleted;

        // ── Simulation ────────────────────────────────────────────────────────────

        private void Update()
        {
            if (Time.time >= _nextSpawnTime)
            {
                float adjustedInterval = spawnIntervalSeconds / Mathf.Max(0.01f, trafficDensity);
                _nextSpawnTime = Time.time + adjustedInterval;
                SpawnFlight();
            }
        }

        private void SpawnFlight()
        {
            string callsign = GenerateCallsign();
            string type = AircraftTypes[UnityEngine.Random.Range(0, AircraftTypes.Length)];
            string origin = ICAOs[UnityEngine.Random.Range(0, ICAOs.Length)];
            string dest   = ICAOs[UnityEngine.Random.Range(0, ICAOs.Length)];
            if (origin == dest) return;

            int alt = (UnityEngine.Random.Range(0, 3)) switch { 0 => 35000, 1 => 39000, _ => 28000 };

            var strip = ATCSystemManager.Instance?.CreateFlightStrip(callsign, type, origin, dest, alt);
            if (strip == null) return;

            _activeFlights.Add(callsign);
            _spawnedCount++;
            OnFlightSpawned?.Invoke(strip);
        }

        private string GenerateCallsign()
        {
            return $"SIM{_spawnedCount + 1:000}";
        }

        /// <summary>Marks a simulated flight as complete and removes it.</summary>
        public void CompleteFlight(string callsign)
        {
            if (_activeFlights.Remove(callsign))
            {
                ATCSystemManager.Instance?.RemoveFlightStrip(callsign);
                OnFlightCompleted?.Invoke(callsign);
            }
        }

        /// <summary>Number of currently active simulated flights.</summary>
        public int ActiveFlightCount => _activeFlights.Count;

        /// <summary>Total flights spawned since simulation start.</summary>
        public int TotalSpawned => _spawnedCount;

        /// <summary>Traffic density setting (0–1).</summary>
        public float TrafficDensity
        {
            get => trafficDensity;
            set => trafficDensity = Mathf.Clamp01(value);
        }
    }
}
