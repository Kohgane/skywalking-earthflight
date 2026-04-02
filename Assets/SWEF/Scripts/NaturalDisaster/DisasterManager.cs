// DisasterManager.cs — SWEF Natural Disaster & Dynamic World Events (Phase 86)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if SWEF_WEATHER_AVAILABLE
using SWEF.Weather;
#endif

namespace SWEF.NaturalDisaster
{
    /// <summary>
    /// Phase 86 — Singleton MonoBehaviour that acts as the central controller for all
    /// natural disasters.  Manages the spawn pool, active disaster list, biome/weather
    /// compatibility checks, and exposes aggregate hazard query APIs used by
    /// <see cref="DisasterFlightModifier"/> and UI components.
    ///
    /// <para>Attach to a persistent scene object — uses DontDestroyOnLoad.</para>
    /// </summary>
    public sealed class DisasterManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static DisasterManager Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Disaster Pool")]
        [Tooltip("All DisasterData assets available for spawning.")]
        public List<DisasterData> disasterPool = new List<DisasterData>();

        [Header("Spawn Settings")]
        [Tooltip("Maximum number of disasters active at the same time.")]
        [Min(1)]
        public int maxConcurrentDisasters = DisasterConfig.MaxConcurrentDisasters;

        [Tooltip("Interval in seconds between automatic spawn-chance checks.")]
        [Min(5f)]
        public float disasterCheckInterval = DisasterConfig.DisasterCheckInterval;

        [Tooltip("Base probability (0–1) of spawning a disaster on each check.")]
        [Range(0f, 1f)]
        public float baseSpawnChance = DisasterConfig.BaseSpawnChance;

        [Header("Player Reference")]
        [Tooltip("Player transform used for distance-based spawn placement. Auto-found if null.")]
        [SerializeField] private Transform _playerTransform;

        #endregion

        #region Public State

        /// <summary>Read-only view of currently active disasters.</summary>
        public IReadOnlyList<ActiveDisaster> activeDisasters => _activeDisasters;

        #endregion

        #region Events

        /// <summary>Raised when a new disaster is spawned.</summary>
        public event Action<ActiveDisaster> OnDisasterSpawned;

        /// <summary>Raised when an active disaster changes phase.</summary>
        public event Action<ActiveDisaster> OnDisasterPhaseChanged;

        /// <summary>Raised when an active disaster fully ends.</summary>
        public event Action<ActiveDisaster> OnDisasterEnded;

        #endregion

        #region Private State

        private readonly List<ActiveDisaster> _activeDisasters = new List<ActiveDisaster>();
        private Coroutine _spawnCheckCoroutine;

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
            Debug.Log("[SWEF] DisasterManager: initialised.");
        }

        private void Start()
        {
            if (_playerTransform == null)
            {
                var fc = FindFirstObjectByType<Flight.FlightController>();
                if (fc != null)
                {
                    _playerTransform = fc.transform;
                    Debug.Log("[SWEF] DisasterManager: auto-found FlightController as player transform.");
                }
            }

            _spawnCheckCoroutine = StartCoroutine(SpawnCheckLoop());
        }

        private void OnDestroy()
        {
            if (_spawnCheckCoroutine != null)
                StopCoroutine(_spawnCheckCoroutine);
        }

        #endregion

        #region Spawn Loop

        private IEnumerator SpawnCheckLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(disasterCheckInterval);

                if (_activeDisasters.Count >= maxConcurrentDisasters) continue;
                if (disasterPool.Count == 0) continue;

                if (UnityEngine.Random.value > baseSpawnChance) continue;

                DisasterData candidate = PickCandidate();
                if (candidate == null) continue;

                Vector3 spawnPos = PickSpawnPosition(candidate);
                SpawnDisaster(candidate, spawnPos);
            }
        }

        private DisasterData PickCandidate()
        {
            // Shuffle-pick a compatible disaster
            List<DisasterData> shuffled = new List<DisasterData>(disasterPool);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                DisasterData tmp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = tmp;
            }

            foreach (DisasterData d in shuffled)
            {
                if (IsBiomeCompatible(d) && IsWeatherCompatible(d))
                    return d;
            }
            return null;
        }

        private bool IsBiomeCompatible(DisasterData d)
        {
            if (d.validBiomes == null || d.validBiomes.Length == 0) return true;
            if (_playerTransform == null) return true;

#if SWEF_BIOME_AVAILABLE
            Vector3 pos = _playerTransform.position;
            // In SWEF's coordinate system: Z maps to latitude (north), X to longitude (east), Y to altitude.
            SWEF.Biome.BiomeType biome = SWEF.Biome.BiomeClassifier.ClassifyBiome(pos.z, pos.x, pos.y);
            string biomeName = biome.ToString();
            foreach (string valid in d.validBiomes)
                if (string.Equals(valid, biomeName, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
#else
            return true;
#endif
        }

        private bool IsWeatherCompatible(DisasterData d)
        {
            // All disasters are weather-compatible by default;
            // sub-classes or data can gate this further via validBiomes.
            return true;
        }

        private Vector3 PickSpawnPosition(DisasterData d)
        {
            if (_playerTransform == null) return Vector3.zero;

            // Spawn 10–30 km away from the player in a random direction
            float dist  = UnityEngine.Random.Range(10000f, 30000f);
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            return _playerTransform.position + offset;
        }

        #endregion

        #region Public Spawn API

        /// <summary>
        /// Spawns a disaster of the given type at <paramref name="position"/> with
        /// severity determined by <paramref name="data"/>'s maxSeverity.
        /// </summary>
        public void SpawnDisaster(DisasterData data, Vector3 position)
        {
            SpawnDisasterInternal(data, position, data.maxSeverity);
        }

        /// <summary>
        /// Force-spawns a disaster of the given type at <paramref name="position"/>
        /// with the specified <paramref name="severity"/>.  Useful for testing.
        /// </summary>
        public void ForceSpawnDisaster(DisasterData data, Vector3 position, DisasterSeverity severity)
        {
            SpawnDisasterInternal(data, position, severity);
        }

        private void SpawnDisasterInternal(DisasterData data, Vector3 position, DisasterSeverity severity)
        {
            if (data == null)
            {
                Debug.LogWarning("[SWEF] DisasterManager: SpawnDisaster called with null data.");
                return;
            }

            GameObject go = new GameObject($"Disaster_{data.disasterName}_{data.disasterId}");
            ActiveDisaster active = go.AddComponent<ActiveDisaster>();
            active.Initialise(data, position, severity);
            active.OnDisasterEnded += HandleDisasterEnded;

            _activeDisasters.Add(active);
            OnDisasterSpawned?.Invoke(active);
            Debug.Log($"[SWEF] DisasterManager: spawned {data.disasterName} at {position} (severity={severity}).");
        }

        #endregion

        #region End Disaster

        /// <summary>
        /// Terminates <paramref name="disaster"/> and removes it from the active list.
        /// Called automatically by <see cref="ActiveDisaster"/> when Aftermath ends; can also
        /// be called externally to force-end a disaster.
        /// </summary>
        public void EndDisaster(ActiveDisaster disaster)
        {
            if (disaster == null) return;
            _activeDisasters.Remove(disaster);
            OnDisasterEnded?.Invoke(disaster);
            Destroy(disaster.gameObject);
            Debug.Log($"[SWEF] DisasterManager: ended disaster {disaster.data?.disasterName}.");
        }

        private void HandleDisasterEnded(ActiveDisaster disaster)
        {
            // ActiveDisaster already calls EndDisaster; guard against double removal.
            _activeDisasters.Remove(disaster);
        }

        #endregion

        #region Phase Change Notification

        /// <summary>
        /// Called by <see cref="ActiveDisaster"/> when its phase changes.
        /// </summary>
        internal void NotifyPhaseChanged(ActiveDisaster disaster)
        {
            OnDisasterPhaseChanged?.Invoke(disaster);

            // Push turbulence data to WeatherFlightModifier when available
#if SWEF_WEATHER_AVAILABLE
            if (WeatherFlightModifier.Instance != null)
                WeatherFlightModifier.Instance.SetDisasterTurbulence(
                    GetTurbulenceAt(
                        _playerTransform != null ? _playerTransform.position : Vector3.zero,
                        _playerTransform != null ? _playerTransform.position.y : 0f));
#endif
        }

        #endregion

        #region Hazard Query API

        /// <summary>
        /// Returns all hazard zones from every active disaster that contain the given
        /// world position and altitude.
        /// </summary>
        public List<HazardZone> GetHazardsAtPosition(Vector3 pos, float alt)
        {
            var result = new List<HazardZone>();
            foreach (ActiveDisaster d in _activeDisasters)
                foreach (HazardZone z in d.hazardZones)
                    if (z.IsPlayerInside(pos, alt))
                        result.Add(z);
            return result;
        }

        /// <summary>
        /// Returns <c>true</c> if any active disaster has a NoFlyZone hazard zone
        /// that contains the given world position and altitude.
        /// </summary>
        public bool IsInNoFlyZone(Vector3 pos, float alt)
        {
            foreach (ActiveDisaster d in _activeDisasters)
                foreach (HazardZone z in d.hazardZones)
                    if (z.type == HazardZoneType.NoFlyZone && z.IsPlayerInside(pos, alt))
                        return true;
            return false;
        }

        /// <summary>
        /// Returns the aggregate turbulence multiplier at the given world position and altitude.
        /// </summary>
        public float GetTurbulenceAt(Vector3 pos, float alt)
        {
            float total = 0f;
            foreach (ActiveDisaster d in _activeDisasters)
            {
                foreach (HazardZone z in d.hazardZones)
                {
                    if (z.type != HazardZoneType.Turbulence) continue;
                    float localIntensity = z.GetIntensityAtPosition(pos);
                    if (localIntensity > 0f)
                        total += localIntensity * d.data.turbulenceMultiplier;
                }
            }
            return Mathf.Min(total, DisasterConfig.MaxTurbulenceMultiplier);
        }

        /// <summary>
        /// Returns the aggregate visibility reduction factor (0–1) at the given
        /// world position and altitude.  0 = full visibility, 1 = zero visibility.
        /// </summary>
        public float GetVisibilityAt(Vector3 pos, float alt)
        {
            float total = 0f;
            foreach (ActiveDisaster d in _activeDisasters)
            {
                foreach (HazardZone z in d.hazardZones)
                {
                    if (z.type != HazardZoneType.ReducedVisibility && z.type != HazardZoneType.AshCloud)
                        continue;
                    float localIntensity = z.GetIntensityAtPosition(pos);
                    if (localIntensity > 0f)
                        total += localIntensity * d.data.visibilityReduction;
                }
            }
            return Mathf.Min(total, DisasterConfig.MaxVisibilityReduction);
        }

        #endregion
    }
}
