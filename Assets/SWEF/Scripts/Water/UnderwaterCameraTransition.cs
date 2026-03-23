// UnderwaterCameraTransition.cs — SWEF Ocean & Water Interaction System
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace SWEF.Water
{
    /// <summary>
    /// Phase 55 — MonoBehaviour that manages the visual transition when a camera
    /// crosses the water surface boundary.
    ///
    /// <para>Attach this component to the main camera or any camera that should
    /// respond to water submersion.  All visual parameters are driven by
    /// <see cref="UnderwaterSettings"/> from the assigned <see cref="profile"/>.</para>
    ///
    /// <para>Effects applied when underwater:
    /// <list type="bullet">
    ///   <item>Scene fog colour and density.</item>
    ///   <item>Ambient light tint shift.</item>
    ///   <item>Post-processing Volume weight interpolation (requires a <see cref="Volume"/> component).</item>
    ///   <item>Sinusoidal UV distortion via <c>Material.SetFloat</c> on an optional distortion material.</item>
    ///   <item>Bubble particle system activation.</item>
    ///   <item>Depth-based light attenuation via a directional light intensity reduction.</item>
    /// </list>
    /// </para>
    ///
    /// <para>Events:
    /// <list type="bullet">
    ///   <item><see cref="OnUnderwaterEnter"/> — fired when the camera first dips below the surface.</item>
    ///   <item><see cref="OnUnderwaterExit"/> — fired when the camera returns above the surface.</item>
    /// </list>
    /// </para>
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class UnderwaterCameraTransition : MonoBehaviour
    {
        #region Inspector

        [Header("Interaction Profile")]
        [Tooltip("Profile containing UnderwaterSettings values.")]
        [SerializeField] private WaterInteractionProfile profile;

        [Header("References")]
        [Tooltip("Post-processing Volume whose weight is interpolated during transitions. Optional.")]
        [SerializeField] private Volume underwaterVolume;

        [Tooltip("Bubble particle system activated while underwater. Optional.")]
        [SerializeField] private ParticleSystem bubbleSystem;

        [Tooltip("Directional light whose intensity is attenuated by depth. Optional.")]
        [SerializeField] private Light sunLight;

        [Tooltip("Full-screen distortion material whose '_DistortionAmount' property is driven at runtime. Optional.")]
        [SerializeField] private Material distortionMaterial;

        [Header("Debug")]
        [Tooltip("Log state transitions to the Unity console.")]
        [SerializeField] private bool debugLog;

        #endregion

        #region Events

        /// <summary>Fired the frame the camera crosses below the water surface.</summary>
        public event Action OnUnderwaterEnter;

        /// <summary>Fired the frame the camera crosses back above the water surface.</summary>
        public event Action OnUnderwaterExit;

        #endregion

        #region Private State

        private UnderwaterSettings _settings;
        private Camera _camera;
        private bool _isUnderwater;
        private float _transitionProgress; // 0 = air, 1 = fully underwater
        private float _baseSunIntensity;

        // Cached fog state to restore on surface
        private bool   _airFogEnabled;
        private Color  _airFogColor;
        private float  _airFogDensity;
        private Color  _airAmbientLight;

        private static readonly int ShaderDistortionAmount = Shader.PropertyToID("_DistortionAmount");
        private static readonly int ShaderDistortionTime   = Shader.PropertyToID("_DistortionTime");

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _settings = profile != null ? profile.underwater : new UnderwaterSettings();

            CacheAirState();

            if (sunLight != null)
                _baseSunIntensity = sunLight.intensity;
        }

        private void Update()
        {
            if (WaterSurfaceManager.Instance == null) return;

            float waterY = WaterSurfaceManager.Instance.GetWaterHeightAt(transform.position);
            bool shouldBeUnderwater = transform.position.y < waterY;

            if (shouldBeUnderwater != _isUnderwater)
            {
                _isUnderwater = shouldBeUnderwater;

                if (_isUnderwater)
                {
                    OnUnderwaterEnter?.Invoke();
                    WaterInteractionAnalytics.RecordUnderwaterEntry();
                    if (debugLog) Debug.Log("[UnderwaterCamera] Entered water.");
                }
                else
                {
                    OnUnderwaterExit?.Invoke();
                    WaterInteractionAnalytics.RecordUnderwaterExit();
                    if (debugLog) Debug.Log("[UnderwaterCamera] Exited water.");
                }

                if (bubbleSystem != null)
                {
                    if (_isUnderwater) bubbleSystem.Play();
                    else bubbleSystem.Stop();
                }
            }

            // Lerp transition progress
            float targetProgress = _isUnderwater ? 1f : 0f;
            float speed = _settings.transitionDuration > 0f ? 1f / _settings.transitionDuration : 1000f;
            _transitionProgress = Mathf.MoveTowards(_transitionProgress, targetProgress, Time.deltaTime * speed);

            ApplyVisualEffects(waterY);
        }

        private void OnDestroy()
        {
            // Restore fog state on destroy so the editor doesn't get stale values
            RenderSettings.fog          = _airFogEnabled;
            RenderSettings.fogColor     = _airFogColor;
            RenderSettings.fogDensity   = _airFogDensity;
            RenderSettings.ambientLight = _airAmbientLight;
        }

        #endregion

        #region Private Helpers

        private void CacheAirState()
        {
            _airFogEnabled  = RenderSettings.fog;
            _airFogColor    = RenderSettings.fogColor;
            _airFogDensity  = RenderSettings.fogDensity;
            _airAmbientLight = RenderSettings.ambientLight;
        }

        private void ApplyVisualEffects(float waterY)
        {
            float t = _transitionProgress;

            // --- Fog ---
            RenderSettings.fog        = t > 0f || _airFogEnabled;
            RenderSettings.fogColor   = Color.Lerp(_airFogColor, _settings.fogColor, t);
            RenderSettings.fogDensity = Mathf.Lerp(_airFogDensity, _settings.fogDensity, t);

            // --- Ambient light ---
            RenderSettings.ambientLight = _airAmbientLight + _settings.ambientLightShift * t;

            // --- Post-processing volume ---
            if (underwaterVolume != null)
                underwaterVolume.weight = Mathf.Lerp(underwaterVolume.weight,
                    _settings.postProcessingVolumeWeight * t, Time.deltaTime * 10f);

            // --- Distortion material ---
            if (distortionMaterial != null)
            {
                distortionMaterial.SetFloat(ShaderDistortionAmount, _settings.distortionAmplitude * t);
                distortionMaterial.SetFloat(ShaderDistortionTime,
                    Time.time * _settings.distortionSpeed);
            }

            // --- Depth-based light attenuation ---
            if (sunLight != null && _isUnderwater)
            {
                float depth = waterY - transform.position.y;
                float attenuationFactor = _settings.maxAttenuationDepth > 0f
                    ? 1f - Mathf.Clamp01(depth / _settings.maxAttenuationDepth)
                    : 1f;
                sunLight.intensity = Mathf.Lerp(_baseSunIntensity, 0f, (1f - attenuationFactor) * t);
            }
            else if (sunLight != null)
            {
                sunLight.intensity = Mathf.Lerp(sunLight.intensity, _baseSunIntensity, Time.deltaTime * 5f);
            }
        }

        #endregion
    }
}
