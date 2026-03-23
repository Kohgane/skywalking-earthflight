using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Ocean
{
    /// <summary>
    /// Phase 50 — Central singleton that manages all registered water bodies,
    /// drives global wave animation time, and exposes the public Ocean API to
    /// other SWEF systems.
    ///
    /// <para>Attach to a persistent GameObject in the bootstrap scene.
    /// Integrate with <c>SWEF.Weather.WeatherManager</c> via
    /// <see cref="SetWindParameters"/> or the <see cref="OceanWeatherIntegrator"/>.</para>
    /// </summary>
    public class OceanManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static OceanManager Instance { get; private set; }

        #endregion

        #region Constants

        private const float DefaultWaveTimeScale    = 1f;
        private const float UnderwaterEntryLerpRate = 4f;
        private const int   MaxWaterBodies          = 256;

        #endregion

        #region Inspector

        [Header("Quality")]
        [Tooltip("Global wave simulation quality applied to all water bodies.")]
        [SerializeField] private WaveQuality globalWaveQuality = WaveQuality.High;

        [Tooltip("Global reflection mode applied to all water bodies.")]
        [SerializeField] private ReflectionMode globalReflectionMode = ReflectionMode.PlanarSimple;

        [Header("Animation")]
        [Tooltip("Multiplier on Time.time that drives all wave animation.")]
        [SerializeField] private float waveTimeScale = DefaultWaveTimeScale;

        [Header("References")]
        [Tooltip("WaveSimulator component used for height queries. Resolved at runtime if null.")]
        [SerializeField] private WaveSimulator waveSimulator;

        [Tooltip("Camera whose Y-position is checked for underwater detection.")]
        [SerializeField] private Camera trackedCamera;

        #endregion

        #region Events

        /// <summary>Fired after a new <see cref="WaterBodyDefinition"/> is registered.</summary>
        public event Action<WaterBodyDefinition> OnWaterBodyRegistered;

        /// <summary>Fired after a water body is removed from the registry.</summary>
        public event Action<string> OnWaterBodyUnregistered;

        /// <summary>Fired when the global wave or reflection quality changes.</summary>
        public event Action<WaveQuality, ReflectionMode> OnQualityChanged;

        /// <summary>Fired once when the tracked camera enters water.</summary>
        public event Action OnUnderwaterEntered;

        /// <summary>Fired once when the tracked camera exits water.</summary>
        public event Action OnUnderwaterExited;

        #endregion

        #region Public Properties

        /// <summary>Global wave animation time (seconds, driven by <see cref="waveTimeScale"/>).</summary>
        public float WaveTime { get; private set; }

        /// <summary>Current global wave quality setting.</summary>
        public WaveQuality GlobalWaveQuality => globalWaveQuality;

        /// <summary>Current global reflection mode.</summary>
        public ReflectionMode GlobalReflectionMode => globalReflectionMode;

        /// <summary><c>true</c> when the tracked camera is below the water surface.</summary>
        public bool IsUnderwater { get; private set; }

        /// <summary>Read-only access to all registered water body definitions.</summary>
        public IReadOnlyList<WaterBodyDefinition> WaterBodies => _waterBodies;

        #endregion

        #region Private State

        private readonly List<WaterBodyDefinition> _waterBodies = new List<WaterBodyDefinition>(16);

        // Wind parameters forwarded from the weather system each frame.
        private float   _windSpeed     = 8f;
        private float   _windDirection = 270f;

        // Underwater state tracking.
        private bool    _wasUnderwater;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (waveSimulator == null)
                waveSimulator = FindFirstObjectByType<WaveSimulator>();

            if (trackedCamera == null)
                trackedCamera = Camera.main;
        }

        private void Update()
        {
            WaveTime += Time.deltaTime * waveTimeScale;
            UpdateUnderwaterState();
        }

        #endregion

        #region Public API — Registry

        /// <summary>
        /// Registers a <see cref="WaterBodyDefinition"/> with the manager.
        /// If a body with the same <see cref="WaterBodyDefinition.id"/> already exists it is replaced.
        /// </summary>
        /// <param name="def">Water body to register.</param>
        public void RegisterWaterBody(WaterBodyDefinition def)
        {
            if (def == null) { Debug.LogWarning("[SWEF.Ocean] RegisterWaterBody: null definition ignored."); return; }
            if (_waterBodies.Count >= MaxWaterBodies)
            {
                Debug.LogWarning("[SWEF.Ocean] RegisterWaterBody: maximum water-body count reached.");
                return;
            }

            // Replace if already present.
            for (int i = 0; i < _waterBodies.Count; i++)
            {
                if (_waterBodies[i].id == def.id)
                {
                    _waterBodies[i] = def;
                    OnWaterBodyRegistered?.Invoke(def);
                    return;
                }
            }

            _waterBodies.Add(def);
            OnWaterBodyRegistered?.Invoke(def);
        }

        /// <summary>
        /// Removes the water body identified by <paramref name="id"/> from the registry.
        /// </summary>
        public void UnregisterWaterBody(string id)
        {
            for (int i = _waterBodies.Count - 1; i >= 0; i--)
            {
                if (_waterBodies[i].id == id)
                {
                    _waterBodies.RemoveAt(i);
                    OnWaterBodyUnregistered?.Invoke(id);
                    return;
                }
            }
        }

        /// <summary>Returns the <see cref="WaterBodyDefinition"/> closest to <paramref name="position"/>,
        /// or <c>null</c> if no water bodies are registered.</summary>
        public WaterBodyDefinition GetNearestWaterBody(Vector3 position)
        {
            WaterBodyDefinition nearest  = null;
            float               bestDist = float.MaxValue;

            foreach (var body in _waterBodies)
            {
                if (!body.enabled) continue;
                float d = Vector3.Distance(position, body.worldPosition);
                if (d < bestDist)
                {
                    bestDist = d;
                    nearest  = body;
                }
            }

            return nearest;
        }

        #endregion

        #region Public API — Surface Queries

        /// <summary>
        /// Returns the Y world-space height of the water surface directly above or below
        /// <paramref name="worldPos"/>.  Falls back to the nearest water body's Y origin
        /// when no <see cref="WaveSimulator"/> is available.
        /// </summary>
        public float GetWaterHeightAtPosition(Vector3 worldPos)
        {
            var body = GetNearestWaterBody(worldPos);
            if (body == null) return 0f;

            float baseY = body.worldPosition.y;
            if (waveSimulator != null)
                baseY += waveSimulator.GetWaveHeightAt(worldPos.x, worldPos.z, WaveTime);

            return baseY;
        }

        /// <summary>
        /// Returns <c>true</c> when <paramref name="worldPos"/> is below the water surface
        /// at that XZ location.
        /// </summary>
        public bool IsPositionUnderwater(Vector3 worldPos)
        {
            return worldPos.y < GetWaterHeightAtPosition(worldPos);
        }

        #endregion

        #region Public API — Wind

        /// <summary>
        /// Updates the global wind parameters used to modulate wave amplitude and direction.
        /// Call this from <see cref="OceanWeatherIntegrator"/> or directly from the Weather system.
        /// </summary>
        /// <param name="speedMs">Wind speed in metres per second.</param>
        /// <param name="directionDeg">Meteorological direction in degrees (0 = North, 90 = East).</param>
        public void SetWindParameters(float speedMs, float directionDeg)
        {
            _windSpeed     = speedMs;
            _windDirection = directionDeg;

            if (waveSimulator != null)
                waveSimulator.ApplyWindParameters(_windSpeed, _windDirection);
        }

        #endregion

        #region Public API — Quality

        /// <summary>
        /// Changes the global wave and reflection quality, notifying all listeners.
        /// </summary>
        public void SetQuality(WaveQuality wave, ReflectionMode reflection)
        {
            bool changed = wave != globalWaveQuality || reflection != globalReflectionMode;
            globalWaveQuality    = wave;
            globalReflectionMode = reflection;

            if (changed)
                OnQualityChanged?.Invoke(globalWaveQuality, globalReflectionMode);
        }

        #endregion

        #region Private — Underwater Detection

        private void UpdateUnderwaterState()
        {
            if (trackedCamera == null) return;

            IsUnderwater = IsPositionUnderwater(trackedCamera.transform.position);

            if (IsUnderwater && !_wasUnderwater)
                OnUnderwaterEntered?.Invoke();
            else if (!IsUnderwater && _wasUnderwater)
                OnUnderwaterExited?.Invoke();

            _wasUnderwater = IsUnderwater;
        }

        #endregion
    }
}
