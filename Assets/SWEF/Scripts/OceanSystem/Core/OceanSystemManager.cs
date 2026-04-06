// OceanSystemManager.cs — Phase 117: Advanced Ocean & Maritime System
// Central singleton manager. DontDestroyOnLoad.
// Namespace: SWEF.OceanSystem

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Central singleton for the Advanced Ocean &amp; Maritime System.
    /// Orchestrates ocean rendering, wave simulation, maritime traffic, water landings,
    /// carrier operations, and maritime missions.
    /// Survives scene loads via <c>DontDestroyOnLoad</c>.
    /// </summary>
    public class OceanSystemManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────

        /// <summary>Shared singleton instance.</summary>
        public static OceanSystemManager Instance { get; private set; }

        // ── Inspector fields ──────────────────────────────────────────────────────

        [Header("Configuration")]
        [SerializeField] private OceanSystemConfig config;

        [Header("Sub-Systems")]
        [SerializeField] private OceanWaveSimulator waveSimulator;
        [SerializeField] private TideController tideController;
        [SerializeField] private OceanCurrentSimulator currentSimulator;
        [SerializeField] private MaritimeTrafficManager trafficManager;

        // ── Private state ─────────────────────────────────────────────────────────

        private SeaState _currentSeaState = SeaState.Calm;
        private OceanRegion _currentRegion = OceanRegion.OpenOcean;
        private WaveConditions _currentConditions;
        private bool _isInitialised;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when the sea state changes.</summary>
        public event Action<SeaState> OnSeaStateChanged;

        /// <summary>Raised when the ocean region changes.</summary>
        public event Action<OceanRegion> OnRegionChanged;

        /// <summary>Raised when a water landing is completed.</summary>
        public event Action<WaterLandingRecord> OnWaterLandingCompleted;

        /// <summary>Raised when a carrier trap or bolter occurs.</summary>
        public event Action<CarrierTrapRecord> OnCarrierTrapRecorded;

        // ── Public Properties ─────────────────────────────────────────────────────

        /// <summary>Runtime configuration reference.</summary>
        public OceanSystemConfig Config => config;

        /// <summary>Current sea state classification.</summary>
        public SeaState CurrentSeaState => _currentSeaState;

        /// <summary>Current ocean region.</summary>
        public OceanRegion CurrentRegion => _currentRegion;

        /// <summary>Current wave conditions snapshot.</summary>
        public WaveConditions CurrentConditions => _currentConditions;

        /// <summary>Whether the ocean system has been initialised.</summary>
        public bool IsInitialised => _isInitialised;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialise();
        }

        private void Start()
        {
            ResolveSubSystems();
        }

        private void Update()
        {
            if (!_isInitialised) return;
            UpdateWaveConditions();
        }

        // ── Initialisation ────────────────────────────────────────────────────────

        private void Initialise()
        {
            _currentConditions = new WaveConditions
            {
                seaState      = SeaState.Calm,
                windSpeed     = 5f,
                windDirection = 270f,
                waveDirection = 270f,
                dominantPeriod         = 6f,
                significantWaveHeight  = 0.5f
            };
            _isInitialised = true;
        }

        private void ResolveSubSystems()
        {
            if (waveSimulator    == null) waveSimulator    = FindFirstObjectByType<OceanWaveSimulator>();
            if (tideController   == null) tideController   = FindFirstObjectByType<TideController>();
            if (currentSimulator == null) currentSimulator = FindFirstObjectByType<OceanCurrentSimulator>();
            if (trafficManager   == null) trafficManager   = FindFirstObjectByType<MaritimeTrafficManager>();
        }

        // ── Wave Condition Updates ────────────────────────────────────────────────

        private void UpdateWaveConditions()
        {
            if (waveSimulator == null) return;
            _currentConditions = waveSimulator.GetCurrentConditions();
            var newState = ClassifySeaState(_currentConditions.significantWaveHeight);
            if (newState != _currentSeaState)
            {
                _currentSeaState = newState;
                _currentConditions.seaState = newState;
                OnSeaStateChanged?.Invoke(_currentSeaState);
            }
        }

        private static SeaState ClassifySeaState(float waveHeightMetres)
        {
            if (waveHeightMetres < 0.5f)  return SeaState.Calm;
            if (waveHeightMetres < 1.25f) return SeaState.Slight;
            if (waveHeightMetres < 2.5f)  return SeaState.Moderate;
            if (waveHeightMetres < 4f)    return SeaState.Rough;
            if (waveHeightMetres < 6f)    return SeaState.VeryRough;
            return SeaState.HighSeas;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Forcefully sets the sea state, e.g. from a weather system integration.
        /// </summary>
        public void SetSeaState(SeaState state)
        {
            if (_currentSeaState == state) return;
            _currentSeaState = state;
            _currentConditions.seaState = state;
            waveSimulator?.ApplySeaState(state);
            OnSeaStateChanged?.Invoke(state);
        }

        /// <summary>Sets the current ocean region for the player's position.</summary>
        public void SetOceanRegion(OceanRegion region)
        {
            if (_currentRegion == region) return;
            _currentRegion = region;
            OnRegionChanged?.Invoke(region);
        }

        /// <summary>Updates global wind parameters to drive wave simulation.</summary>
        public void SetWindParameters(float speedMs, float directionDeg)
        {
            if (_currentConditions == null) return;
            _currentConditions.windSpeed     = speedMs;
            _currentConditions.windDirection = directionDeg;
            waveSimulator?.SetWind(speedMs, directionDeg);
        }

        /// <summary>
        /// Returns the estimated ocean surface height at <paramref name="worldXZ"/>.
        /// </summary>
        public float GetSurfaceHeight(Vector2 worldXZ)
        {
            return waveSimulator != null
                ? waveSimulator.GetSurfaceHeight(worldXZ)
                : 0f;
        }

        /// <summary>Records and broadcasts a completed water landing event.</summary>
        public void RecordWaterLanding(WaterLandingRecord record)
        {
            if (record == null) return;
            OnWaterLandingCompleted?.Invoke(record);
        }

        /// <summary>Records and broadcasts a carrier trap/bolter event.</summary>
        public void RecordCarrierTrap(CarrierTrapRecord record)
        {
            if (record == null) return;
            OnCarrierTrapRecorded?.Invoke(record);
        }
    }
}
