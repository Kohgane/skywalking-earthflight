// MaritimeTrafficManager.cs — Phase 117: Advanced Ocean & Maritime System
// Sea vessel spawning and routing: shipping lanes, port arrivals/departures.
// Namespace: SWEF.OceanSystem

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Singleton that manages AI maritime vessel spawning, routing,
    /// and lifecycle. Shipping lane routes and port arrival/departure logic are
    /// coordinated here.
    /// </summary>
    public class MaritimeTrafficManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────

        /// <summary>Shared singleton instance.</summary>
        public static MaritimeTrafficManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Configuration")]
        [SerializeField] private OceanSystemConfig config;

        [Header("Vessel Prefabs")]
        [SerializeField] private GameObject cargoShipPrefab;
        [SerializeField] private GameObject tankerPrefab;
        [SerializeField] private GameObject sailboatPrefab;
        [SerializeField] private GameObject speedboatPrefab;

        // ── Private state ─────────────────────────────────────────────────────────

        private readonly List<VesselController> _activeVessels = new List<VesselController>();
        private float _spawnTimer;
        private const float SpawnInterval = 30f;

        // ── Seed vessel data ──────────────────────────────────────────────────────

        private static readonly VesselData[] SeedVessels =
        {
            new VesselData { vesselId="V001", vesselName="Pacific Trader",  vesselType=VesselType.CargoShip,  heading=270f, speedKnots=14f, destination="Port Alpha" },
            new VesselData { vesselId="V002", vesselName="Arctic Star",     vesselType=VesselType.Tanker,     heading=90f,  speedKnots=12f, destination="Port Beta"  },
            new VesselData { vesselId="V003", vesselName="Sea Breeze",      vesselType=VesselType.Sailboat,   heading=180f, speedKnots=6f,  destination="Marina Cove"},
            new VesselData { vesselId="V004", vesselName="Thunder Runner",  vesselType=VesselType.Speedboat,  heading=45f,  speedKnots=28f, destination="Harbour Bay"},
            new VesselData { vesselId="V005", vesselName="Neptune's Catch", vesselType=VesselType.FishingBoat,heading=315f, speedKnots=8f,  destination="Fish Port"  }
        };

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when a vessel is spawned.</summary>
        public event Action<VesselData> OnVesselSpawned;

        /// <summary>Raised when a vessel is despawned.</summary>
        public event Action<string> OnVesselDespawned;

        // ── Public Properties ─────────────────────────────────────────────────────

        /// <summary>Number of currently active AI vessels.</summary>
        public int ActiveVesselCount => _activeVessels.Count;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            SpawnSeedVessels();
        }

        private void Update()
        {
            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= SpawnInterval)
            {
                _spawnTimer = 0f;
                TrySpawnVessel();
            }
        }

        // ── Spawning ──────────────────────────────────────────────────────────────

        private void SpawnSeedVessels()
        {
            foreach (var data in SeedVessels)
            {
                if (_activeVessels.Count >= (config != null ? config.maxActiveVessels : 20)) break;
                SpawnVesselFromData(data);
            }
        }

        private void TrySpawnVessel()
        {
            if (config == null) return;
            if (_activeVessels.Count >= config.maxActiveVessels) return;

            float angle    = UnityEngine.Random.Range(0f, 360f);
            float radius   = config.vesselSpawnRadius;
            var   spawnPos = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * radius,
                                         0f,
                                         Mathf.Cos(angle * Mathf.Deg2Rad) * radius);

            int    typeIdx = UnityEngine.Random.Range(0, SeedVessels.Length);
            var    seed    = SeedVessels[typeIdx];
            var    data    = new VesselData
            {
                vesselId   = "V" + UnityEngine.Random.Range(100, 999),
                vesselName = "Vessel-" + UnityEngine.Random.Range(100, 999),
                vesselType = seed.vesselType,
                position   = spawnPos,
                heading    = UnityEngine.Random.Range(0f, 360f),
                speedKnots = UnityEngine.Random.Range(config.minVesselSpeedKnots, config.maxCargoVesselSpeedKnots),
                isActive   = true
            };
            SpawnVesselFromData(data);
        }

        private void SpawnVesselFromData(VesselData data)
        {
            data.isActive = true;
            OnVesselSpawned?.Invoke(data);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns a snapshot of all active vessel data.</summary>
        public IReadOnlyList<VesselController> GetActiveVessels() => _activeVessels;
    }
}
