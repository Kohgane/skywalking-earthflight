// WaterSurfaceManager.cs — SWEF Ocean & Water Interaction System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Water
{
    #region Singleton Manager

    /// <summary>
    /// Phase 55 — Singleton MonoBehaviour that owns the Gerstner wave simulation for
    /// all water bodies in the scene.
    ///
    /// <para>Provides two primary sampling APIs used by buoyancy, camera, and VFX systems:
    /// <list type="bullet">
    ///   <item><see cref="GetWaterHeightAt"/> — world-space Y of the water surface at any XZ position.</item>
    ///   <item><see cref="GetWaterNormalAt"/> — surface normal at any XZ position for object tilt.</item>
    /// </list>
    /// </para>
    ///
    /// <para>Integration points:
    /// <list type="bullet">
    ///   <item><see cref="SWEF.Weather.WeatherManager"/> — drives <see cref="WaterState"/> transitions (optional).</item>
    ///   <item><see cref="BuoyancyController"/> — queries height and normal each physics frame.</item>
    ///   <item><see cref="SplashEffectController"/> — queries height to detect surface crossing.</item>
    ///   <item><see cref="UnderwaterCameraTransition"/> — queries height to detect camera submersion.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class WaterSurfaceManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static WaterSurfaceManager Instance { get; private set; }

        #endregion

        #region Constants

        private const int MaxWaveLayers = 4;

        /// <summary>Small XZ offset used when computing finite-difference normals (world units).</summary>
        private const float NormalSampleOffset = 0.1f;

        #endregion

        #region Inspector

        [Header("Interaction Profile")]
        [Tooltip("WaterInteractionProfile ScriptableObject containing all wave/surface config.")]
        [SerializeField] private WaterInteractionProfile profile;

        [Header("Water State")]
        [Tooltip("Initial water agitation state.")]
        [SerializeField] private WaterState initialState = WaterState.Calm;

        [Header("Base Water Height")]
        [Tooltip("World-space Y coordinate of the undisplaced water surface.")]
        [SerializeField] private float baseWaterHeight = 0f;

        #endregion

        #region Events

        /// <summary>
        /// Fired whenever the water agitation state changes.
        /// Subscribers (audio, VFX, HUD) should listen here rather than polling.
        /// </summary>
        public event Action<WaterState> OnWaterStateChanged;

        #endregion

        #region Private State

        private WaterState _currentState;
        private WaveLayer[] _activeLayers;
        private float _time;

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

            _currentState = initialState;
            RebuildActiveLayers();
        }

        private void Update()
        {
            _time += Time.deltaTime;
        }

        #endregion

        #region Public API

        /// <summary>Gets the current wave agitation state of the water surface.</summary>
        public WaterState CurrentState => _currentState;

        /// <summary>Gets the Y coordinate of the undisplaced base water plane.</summary>
        public float BaseWaterHeight => baseWaterHeight;

        /// <summary>
        /// Returns the world-space Y (height) of the water surface at the given XZ position,
        /// computed via a multi-layer Gerstner wave approximation.
        /// </summary>
        /// <param name="worldPos">Any world-space position; only the X and Z components are used.</param>
        /// <returns>World-space Y of the water surface directly below (or above) <paramref name="worldPos"/>.</returns>
        public float GetWaterHeightAt(Vector3 worldPos)
        {
            if (_activeLayers == null || _activeLayers.Length == 0)
                return baseWaterHeight;

            float height = baseWaterHeight;
            for (int i = 0; i < _activeLayers.Length; i++)
                height += EvaluateGerstnerY(_activeLayers[i], worldPos.x, worldPos.z, _time);

            return height;
        }

        /// <summary>
        /// Returns the surface normal of the water at the given XZ position using finite
        /// differences over the Gerstner height field.  Useful for tilting floating objects.
        /// </summary>
        /// <param name="worldPos">Any world-space position; only X and Z are used.</param>
        /// <returns>Normalised surface normal vector.</returns>
        public Vector3 GetWaterNormalAt(Vector3 worldPos)
        {
            float hC = GetWaterHeightAt(worldPos);
            float hX = GetWaterHeightAt(worldPos + new Vector3(NormalSampleOffset, 0f, 0f));
            float hZ = GetWaterHeightAt(worldPos + new Vector3(0f, 0f, NormalSampleOffset));

            Vector3 tangentX = new Vector3(NormalSampleOffset, hX - hC, 0f);
            Vector3 tangentZ = new Vector3(0f, hZ - hC, NormalSampleOffset);
            return Vector3.Cross(tangentZ, tangentX).normalized;
        }

        /// <summary>
        /// Transitions the water to a new agitation <paramref name="state"/> and fires
        /// <see cref="OnWaterStateChanged"/>.  No event is emitted if the state is unchanged.
        /// Wave layers are rebuilt to reflect any state-specific configuration.
        /// </summary>
        /// <param name="state">Target water agitation state.</param>
        public void SetWaterState(WaterState state)
        {
            if (_currentState == state) return;
            _currentState = state;
            RebuildActiveLayers();
            OnWaterStateChanged?.Invoke(_currentState);
        }

        /// <summary>
        /// Rebuilds the active wave layers from the current profile.
        /// Call this if the profile asset is swapped at runtime.
        /// </summary>
        public void RefreshProfile()
        {
            RebuildActiveLayers();
        }

        #endregion

        #region Private Helpers

        private void RebuildActiveLayers()
        {
            if (profile == null || profile.surface == null || profile.surface.waveLayers == null)
            {
                _activeLayers = Array.Empty<WaveLayer>();
                return;
            }

            int count = Mathf.Min(profile.surface.waveLayers.Length, MaxWaveLayers);
            _activeLayers = new WaveLayer[count];
            Array.Copy(profile.surface.waveLayers, _activeLayers, count);
        }

        /// <summary>
        /// Evaluates the Y displacement of a single Gerstner wave layer at (x, z, t).
        /// </summary>
        private static float EvaluateGerstnerY(WaveLayer layer, float x, float z, float t)
        {
            Vector2 dir = layer.direction.sqrMagnitude < 0.001f ? Vector2.right : layer.direction.normalized;
            float k = layer.frequency * Mathf.PI * 2f;
            float phase = k * (dir.x * x + dir.y * z) - layer.speed * t;
            return layer.amplitude * Mathf.Sin(phase);
        }

        #endregion
    }

    #endregion
}
