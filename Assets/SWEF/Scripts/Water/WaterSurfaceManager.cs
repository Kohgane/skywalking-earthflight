// WaterSurfaceManager.cs — SWEF Phase 74: Water Interaction & Buoyancy System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Water
{
    /// <summary>
    /// Phase 74 — Singleton MonoBehaviour that owns the Gerstner wave simulation and
    /// water body detection for all water surfaces in the scene.
    ///
    /// <para>Integration points (all null-safe):</para>
    /// <list type="bullet">
    ///   <item><see cref="SWEF.Flight.FlightController"/> — player world position.</item>
    ///   <item><see cref="SWEF.Weather.WeatherManager"/> — wind speed → wave intensity.</item>
    /// </list>
    /// </summary>
    [DisallowMultipleComponent]
    public class WaterSurfaceManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static WaterSurfaceManager Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Water Configuration")]
        [Tooltip("Serializable water interaction configuration.")]
        [SerializeField] private WaterConfig config = new WaterConfig();

        [Header("Wave Octaves")]
        [Tooltip("Direction vectors for additional Gerstner wave octaves (up to 4 used).")]
        [SerializeField] private Vector2[] waveDirections = new Vector2[]
        {
            new Vector2(1f,  0.3f),
            new Vector2(0.6f, 1f),
            new Vector2(-0.4f, 1f),
            new Vector2(1f, -0.5f),
        };

        #endregion

        #region Events

        /// <summary>Fired when the player first moves over a water body. Passes the detected <see cref="WaterBodyType"/>.</summary>
        public event Action<WaterBodyType> OnWaterDetected;

        /// <summary>Fired when the player leaves a water area.</summary>
        public event Action OnWaterLost;

        /// <summary>Fired whenever the wave phase state changes significantly.</summary>
        public event Action<WaterSurfaceState> OnWaveStateChanged;

        #endregion

        #region Public Properties

        /// <summary>Configuration shared with all water sub-systems.</summary>
        public WaterConfig Config => config;

        /// <summary>Most recently sampled surface state at the player position.</summary>
        public WaterSurfaceState CurrentState { get; private set; } = new WaterSurfaceState();

        #endregion

        #region Private State

        private float _time;
        private bool _wasOverWater;

        // Cached reflection-based access to cross-system managers (null-safe)
        private Component _flightController;
        private Component _weatherManager;
        private bool _crossSystemCacheDone;

        #endregion

        #region Constants

        private const int MaxWaveOctaves = 4;
        private const float NormalSampleOffset = 0.1f;

        /// <summary>
        /// Extra vertical buffer (m) added to the water detection range so that low-altitude
        /// terrain features do not cause flickering on/off of the water detection state.
        /// </summary>
        private const float WaterDetectionBuffer = 50f;

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
        }

        private void Update()
        {
            _time += Time.deltaTime;

            if (!_crossSystemCacheDone)
                CacheCrossSystemReferences();

            Vector3 playerPos = GetPlayerPosition();
            bool overWater = IsOverWater(playerPos);

            if (overWater != _wasOverWater)
            {
                if (overWater)
                    OnWaterDetected?.Invoke(DetectWaterBodyType(playerPos));
                else
                    OnWaterLost?.Invoke();
                _wasOverWater = overWater;
            }

            if (overWater)
                UpdateSurfaceState(playerPos);
        }

        #endregion

        #region Public API — Water Detection

        /// <summary>
        /// Returns <c>true</c> if the given world position is above or at a water surface.
        /// Uses a simple altitude check against the wave-displaced water height.
        /// </summary>
        /// <param name="worldPosition">World-space position to test.</param>
        public bool IsOverWater(Vector3 worldPosition)
        {
            return worldPosition.y <= GetWaterHeight(worldPosition) + config.skimAltitudeThreshold + WaterDetectionBuffer;
        }

        /// <summary>
        /// Returns the wave-displaced water surface height (world-space Y) at the given XZ position.
        /// </summary>
        /// <param name="worldPosition">World-space position; only X and Z are used.</param>
        public float GetWaterHeight(Vector3 worldPosition)
        {
            float height = config.waterLevel;
            float windMultiplier = GetWindWaveMultiplier();

            int octaves = Mathf.Min(waveDirections != null ? waveDirections.Length : 0, MaxWaveOctaves);
            for (int i = 0; i < octaves; i++)
            {
                float amp = config.waveAmplitude * windMultiplier * Mathf.Pow(0.5f, i);
                float freq = config.waveFrequency * Mathf.Pow(2f, i);
                float speed = config.waveSpeed * Mathf.Pow(1.3f, i);
                Vector2 dir = waveDirections[i].sqrMagnitude < 0.001f ? Vector2.right : waveDirections[i].normalized;
                float phase = freq * (dir.x * worldPosition.x + dir.y * worldPosition.z) - speed * _time;
                height += amp * Mathf.Sin(phase);
            }

            return height;
        }

        /// <summary>
        /// Returns the surface normal of the water at the given world XZ position,
        /// computed via finite differences over the Gerstner height field.
        /// </summary>
        /// <param name="worldPosition">World-space position; only X and Z are used.</param>
        public Vector3 GetSurfaceNormal(Vector3 worldPosition)
        {
            float hC = GetWaterHeight(worldPosition);
            float hX = GetWaterHeight(worldPosition + new Vector3(NormalSampleOffset, 0f, 0f));
            float hZ = GetWaterHeight(worldPosition + new Vector3(0f, 0f, NormalSampleOffset));
            Vector3 tangentX = new Vector3(NormalSampleOffset, hX - hC, 0f);
            Vector3 tangentZ = new Vector3(0f, hZ - hC, NormalSampleOffset);
            return Vector3.Cross(tangentZ, tangentX).normalized;
        }

        /// <summary>
        /// Heuristic detection of the water body type at a world position.
        /// Classifies by wave amplitude and area context.
        /// </summary>
        /// <param name="worldPosition">World-space position to classify.</param>
        public WaterBodyType DetectWaterBodyType(Vector3 worldPosition)
        {
            // Heuristic based on wave amplitude (as a proxy for body size/exposure)
            float windMult = GetWindWaveMultiplier();
            float effectiveAmp = config.waveAmplitude * windMult;

            if (effectiveAmp > 1.5f) return WaterBodyType.Ocean;
            if (effectiveAmp > 0.8f) return WaterBodyType.Sea;
            if (effectiveAmp > 0.4f) return WaterBodyType.Lake;
            if (effectiveAmp > 0.2f) return WaterBodyType.River;
            if (effectiveAmp > 0.05f) return WaterBodyType.Pond;
            return WaterBodyType.Unknown;
        }

        #endregion

        #region Private Helpers

        private void UpdateSurfaceState(Vector3 position)
        {
            float height = GetWaterHeight(position);
            Vector3 normal = GetSurfaceNormal(position);
            float windMult = GetWindWaveMultiplier();
            float phase = config.waveFrequency * _time;

            bool changed = Mathf.Abs(height - CurrentState.heightAtPosition) > 0.01f;

            CurrentState.heightAtPosition = height;
            CurrentState.surfaceNormal = normal;
            CurrentState.wavePhase = phase;
            CurrentState.bodyType = DetectWaterBodyType(position);
            CurrentState.temperature = Mathf.Lerp(25f, 5f, windMult);
            CurrentState.clarity = Mathf.Clamp01(1f - windMult * 0.5f);

            if (changed)
                OnWaveStateChanged?.Invoke(CurrentState);
        }

        private Vector3 GetPlayerPosition()
        {
            // Null-safe access to SWEF.Flight.FlightController
            if (_flightController != null)
            {
                try
                {
                    var prop = _flightController.GetType().GetProperty("Position")
                               ?? _flightController.GetType().GetProperty("WorldPosition");
                    if (prop != null)
                        return (Vector3)prop.GetValue(_flightController);
                }
                catch { }
            }
            return Vector3.zero;
        }

        private float GetWindWaveMultiplier()
        {
            // Null-safe access to SWEF.Weather.WeatherManager
            if (_weatherManager != null)
            {
                try
                {
                    var prop = _weatherManager.GetType().GetProperty("WindSpeed")
                               ?? _weatherManager.GetType().GetProperty("CurrentWindSpeed");
                    if (prop != null)
                    {
                        float windSpeed = (float)prop.GetValue(_weatherManager);
                        return Mathf.Clamp(windSpeed / 20f, 0.5f, 3f);
                    }
                }
                catch { }
            }
            return 1f;
        }

        private void CacheCrossSystemReferences()
        {
            _crossSystemCacheDone = true;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var fcType = assembly.GetType("SWEF.Flight.FlightController");
                if (fcType != null)
                {
                    _flightController = FindObjectOfType(fcType) as Component;
                }
                var wmType = assembly.GetType("SWEF.Weather.WeatherManager");
                if (wmType != null)
                {
                    _weatherManager = FindObjectOfType(wmType) as Component;
                }
            }
        }

        #endregion
    }
}
