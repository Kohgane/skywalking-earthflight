// SatelliteTrackingManager.cs — Phase 114: Satellite & Space Debris Tracking
// Central singleton that orchestrates satellite tracking, debris monitoring,
// and orbital mechanics. DontDestroyOnLoad.
// Namespace: SWEF.SatelliteTracking

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Central singleton manager for the Satellite &amp; Space Debris Tracking system.
    /// Orchestrates TLE ingestion, orbital propagation, debris simulation, docking
    /// scenarios, and all sub-system coordination.
    /// Survives scene loads via <c>DontDestroyOnLoad</c>.
    /// </summary>
    public class SatelliteTrackingManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Shared singleton instance.</summary>
        public static SatelliteTrackingManager Instance { get; private set; }

        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Configuration")]
        [SerializeField] private SatelliteTrackingConfig config;

        [Header("Sub-Systems")]
        [SerializeField] private OrbitalMechanicsEngine orbitalEngine;
        [SerializeField] private SpaceDebrisManager debrisManager;
        [SerializeField] private ISSTracker issTracker;
        [SerializeField] private CollisionWarningSystem collisionWarning;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Current tracking mode.</summary>
        public TrackingMode CurrentMode { get; private set; } = TrackingMode.Off;

        /// <summary>Runtime configuration (read-only access).</summary>
        public SatelliteTrackingConfig Config => config;

        /// <summary>All satellite records currently loaded.</summary>
        public IReadOnlyList<SatelliteRecord> TrackedSatellites => _trackedSatellites.AsReadOnly();

        /// <summary>Total number of tracked objects (satellites + debris).</summary>
        public int TotalTrackedObjects => _trackedSatellites.Count + _trackedDebris.Count;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when a new satellite is added to the tracking catalogue.</summary>
        public event Action<SatelliteRecord> OnSatelliteAdded;

        /// <summary>Raised when a satellite's position is updated.</summary>
        public event Action<SatelliteRecord> OnSatelliteUpdated;

        /// <summary>Raised when a conjunction warning is issued.</summary>
        public event Action<ConjunctionData> OnConjunctionWarning;

        /// <summary>Raised when the tracking mode changes.</summary>
        public event Action<TrackingMode> OnTrackingModeChanged;

        /// <summary>Raised when TLE data has been refreshed.</summary>
        public event Action OnTLEDataRefreshed;

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly List<SatelliteRecord> _trackedSatellites = new List<SatelliteRecord>();
        private readonly List<DebrisObject> _trackedDebris = new List<DebrisObject>();
        private readonly Dictionary<int, SatelliteRecord> _satelliteById = new Dictionary<int, SatelliteRecord>();

        private Coroutine _positionUpdateCoroutine;
        private Coroutine _tleRefreshCoroutine;
        private Coroutine _conjunctionCoroutine;
        private float _lastPositionUpdate;
        private bool _isInitialised;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (config == null)
                config = ScriptableObject.CreateInstance<SatelliteTrackingConfig>();

            Initialise();
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            if (Instance == this) Instance = null;
        }

        // ── Initialisation ────────────────────────────────────────────────────────

        /// <summary>Bootstraps all tracking sub-systems.</summary>
        public void Initialise()
        {
            if (_isInitialised) return;

            _isInitialised = true;
            SetTrackingMode(TrackingMode.Passive);

            _positionUpdateCoroutine = StartCoroutine(PositionUpdateLoop());
            _tleRefreshCoroutine     = StartCoroutine(TLERefreshLoop());
            _conjunctionCoroutine    = StartCoroutine(ConjunctionCheckLoop());

            Debug.Log("[SatelliteTracking] Initialised.");
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Changes the active tracking mode.</summary>
        public void SetTrackingMode(TrackingMode mode)
        {
            if (CurrentMode == mode) return;
            CurrentMode = mode;
            OnTrackingModeChanged?.Invoke(mode);
        }

        /// <summary>Adds a satellite record to the live tracking catalogue.</summary>
        public void RegisterSatellite(SatelliteRecord record)
        {
            if (record == null || _satelliteById.ContainsKey(record.noradId)) return;
            _trackedSatellites.Add(record);
            _satelliteById[record.noradId] = record;
            OnSatelliteAdded?.Invoke(record);
        }

        /// <summary>Returns the satellite record for the given NORAD ID, or null.</summary>
        public SatelliteRecord GetSatellite(int noradId)
        {
            _satelliteById.TryGetValue(noradId, out var record);
            return record;
        }

        /// <summary>Removes all tracked satellites and debris, resetting the catalogue.</summary>
        public void ClearCatalogue()
        {
            _trackedSatellites.Clear();
            _trackedDebris.Clear();
            _satelliteById.Clear();
        }

        /// <summary>Registers a debris object for tracking.</summary>
        public void RegisterDebris(DebrisObject debris)
        {
            if (debris == null) return;
            _trackedDebris.Add(debris);
        }

        /// <summary>Forces an immediate conjunction check across all tracked objects.</summary>
        public void RunConjunctionCheck()
        {
            if (collisionWarning != null)
                collisionWarning.RunCheck(_trackedSatellites, _trackedDebris);
        }

        // ── Coroutines ────────────────────────────────────────────────────────────

        private IEnumerator PositionUpdateLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(config != null ? config.positionUpdateInterval : 1f);

                if (CurrentMode == TrackingMode.Off) continue;

                var now = DateTime.UtcNow;
                foreach (var sat in _trackedSatellites)
                {
                    if (sat.tle == null) continue;
                    if (orbitalEngine != null)
                        sat.currentState = orbitalEngine.Propagate(sat.tle, now);
                    OnSatelliteUpdated?.Invoke(sat);
                }
            }
        }

        private IEnumerator TLERefreshLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(config != null ? config.tleRefreshInterval : 3600f);
                OnTLEDataRefreshed?.Invoke();
                Debug.Log("[SatelliteTracking] TLE data refreshed.");
            }
        }

        private IEnumerator ConjunctionCheckLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(config != null ? config.conjunctionCheckInterval : 30f);
                RunConjunctionCheck();
            }
        }
    }
}
