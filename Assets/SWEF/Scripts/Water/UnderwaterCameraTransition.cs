// UnderwaterCameraTransition.cs — SWEF Phase 74: Water Interaction & Buoyancy System
using System;
using UnityEngine;

namespace SWEF.Water
{
    /// <summary>
    /// Phase 74 — Handles visual, audio, and particle transitions when the camera
    /// crosses the water surface boundary.
    ///
    /// <para>Null-safe integration points:</para>
    /// <list type="bullet">
    ///   <item><see cref="SWEF.Audio.AudioManager"/> — low-pass filter + bubble sounds when submerged.</item>
    ///   <item><see cref="SWEF.Flight.CameraController"/> — camera world position source.</item>
    /// </list>
    /// </summary>
    [DisallowMultipleComponent]
    public class UnderwaterCameraTransition : MonoBehaviour
    {
        #region Inspector

        [Header("Zone Thresholds (metres below surface)")]
        [SerializeField] private float shallowDepth  = 10f;
        [SerializeField] private float midDepth       = 50f;
        [SerializeField] private float deepDepth      = 200f;

        [Header("Transition Zone")]
        [Tooltip("Half-height of the surface blend zone in metres.")]
        [SerializeField] private float surfaceBlendZone = 0.5f;

        [Header("Visuals")]
        [Tooltip("Particle system for floating debris and bubbles underwater.")]
        [SerializeField] private ParticleSystem bubbleParticles;
        [Tooltip("Animated caustic texture overlay GameObject.")]
        [SerializeField] private GameObject causticsOverlay;
        [Tooltip("Transition duration in seconds for fog/lighting changes.")]
        [SerializeField] private float transitionDuration = 0.5f;

        [Header("Fog Settings")]
        [SerializeField] private Color surfaceFogColor  = new Color(0.1f, 0.4f, 0.5f);
        [SerializeField] private Color shallowFogColor  = new Color(0.04f, 0.3f, 0.4f);
        [SerializeField] private Color midFogColor      = new Color(0.02f, 0.1f, 0.25f);
        [SerializeField] private Color deepFogColor     = new Color(0.01f, 0.03f, 0.1f);
        [SerializeField] private Color abyssFogColor    = new Color(0.0f, 0.0f, 0.02f);

        [SerializeField] private float surfaceFogDensity  = 0.05f;
        [SerializeField] private float shallowFogDensity  = 0.08f;
        [SerializeField] private float midFogDensity      = 0.12f;
        [SerializeField] private float deepFogDensity     = 0.18f;
        [SerializeField] private float abyssFogDensity    = 0.4f;

        #endregion

        #region Events

        /// <summary>Fired when the camera transitions below the water surface.</summary>
        public event Action<UnderwaterZone> OnSubmerged;

        /// <summary>Fired when the camera returns above the water surface.</summary>
        public event Action OnSurfaced;

        /// <summary>Fired when the underwater zone classification changes.</summary>
        public event Action<UnderwaterZone> OnZoneChanged;

        #endregion

        #region Public Properties

        /// <summary>Returns <c>true</c> when the camera is below the water surface.</summary>
        public bool IsUnderwater { get; private set; }

        /// <summary>Returns the current depth below the water surface in metres (0 when above).</summary>
        public float CurrentDepth { get; private set; }

        /// <summary>Returns the current underwater zone classification.</summary>
        public UnderwaterZone CurrentZone { get; private set; } = UnderwaterZone.Surface;

        #endregion

        #region Private State

        private Camera _camera;
        private WaterConfig _config;
        private Light _directionalLight;
        private UnderwaterZone _previousZone = UnderwaterZone.Surface;
        private bool _wasUnderwater;

        private Color _targetFogColor;
        private float _targetFogDensity;
        private Color _currentFogColor;
        private float _currentFogDensity;
        private bool _fogWasEnabled;
        private FogMode _originalFogMode;
        private Color _originalFogColor;
        private float _originalFogDensity;

        // Null-safe cross-system references
        private Component _audioManager;
        private bool _crossSystemCacheDone;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _camera = Camera.main;
            _config = WaterSurfaceManager.Instance != null
                ? WaterSurfaceManager.Instance.Config
                : new WaterConfig();

            // Cache directional light for per-frame light falloff (avoids FindObjectsOfType in Update)
            Light[] lights = FindObjectsOfType<Light>();
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    _directionalLight = light;
                    break;
                }
            }

            // Cache original fog state
            _fogWasEnabled   = RenderSettings.fog;
            _originalFogMode = RenderSettings.fogMode;
            _originalFogColor   = RenderSettings.fogColor;
            _originalFogDensity = RenderSettings.fogDensity;
            _currentFogColor    = _originalFogColor;
            _currentFogDensity  = _originalFogDensity;

            if (!_crossSystemCacheDone) CacheCrossSystemReferences();
        }

        private void Update()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            float waterHeight = WaterSurfaceManager.Instance != null
                ? WaterSurfaceManager.Instance.GetWaterHeight(_camera.transform.position)
                : (_config != null ? _config.waterLevel : 0f);

            float depth = waterHeight - _camera.transform.position.y;
            CurrentDepth = Mathf.Max(0f, depth);

            // Determine if underwater
            bool underwater = depth > -surfaceBlendZone;
            if (underwater != _wasUnderwater)
            {
                if (underwater)
                {
                    IsUnderwater = true;
                    SaveAndEnableUnderwaterFog();
                    PlayBubbles(true);
                    SetAudioLowPass(true);
                    OnSubmerged?.Invoke(ClassifyZone(CurrentDepth));
                }
                else
                {
                    IsUnderwater = false;
                    RestoreAboveWaterFog();
                    PlayBubbles(false);
                    SetAudioLowPass(false);
                    OnSurfaced?.Invoke();
                }
                _wasUnderwater = underwater;
            }

            UnderwaterZone zone = ClassifyZone(CurrentDepth);
            if (zone != _previousZone)
            {
                CurrentZone = zone;
                _previousZone = zone;
                OnZoneChanged?.Invoke(zone);
            }

            if (IsUnderwater)
            {
                UpdateUnderwaterVisuals(depth);
                UpdateCausticsOverlay();
            }

            // Smooth fog transitions
            _currentFogColor   = Color.Lerp(_currentFogColor, _targetFogColor, Time.deltaTime / transitionDuration);
            _currentFogDensity = Mathf.Lerp(_currentFogDensity, _targetFogDensity, Time.deltaTime / transitionDuration);
            if (RenderSettings.fog)
            {
                RenderSettings.fogColor   = _currentFogColor;
                RenderSettings.fogDensity = _currentFogDensity;
            }
        }

        private void OnDestroy()
        {
            // Restore fog on destroy
            RenderSettings.fog        = _fogWasEnabled;
            RenderSettings.fogMode    = _originalFogMode;
            RenderSettings.fogColor   = _originalFogColor;
            RenderSettings.fogDensity = _originalFogDensity;
        }

        #endregion

        #region Private Helpers

        private UnderwaterZone ClassifyZone(float depth)
        {
            if (depth <= 0f)              return UnderwaterZone.Surface;
            if (depth < shallowDepth)     return UnderwaterZone.Shallow;
            if (depth < midDepth)         return UnderwaterZone.Mid;
            if (depth < deepDepth)        return UnderwaterZone.Deep;
            return UnderwaterZone.Abyss;
        }

        private void UpdateUnderwaterVisuals(float depth)
        {
            float lightFalloff = _config != null ? _config.underwaterLightFalloff : 0.02f;
            float lightIntensity = Mathf.Exp(-CurrentDepth * lightFalloff);

            // Apply light falloff to cached directional light
            if (_directionalLight != null)
            {
                _directionalLight.intensity = Mathf.Lerp(
                    _directionalLight.intensity,
                    _directionalLight.intensity * lightIntensity,
                    Time.deltaTime * 2f);
            }

            // Set fog targets by zone
            switch (CurrentZone)
            {
                case UnderwaterZone.Surface:
                    _targetFogColor   = surfaceFogColor;
                    _targetFogDensity = surfaceFogDensity;
                    break;
                case UnderwaterZone.Shallow:
                    _targetFogColor   = shallowFogColor;
                    _targetFogDensity = shallowFogDensity;
                    break;
                case UnderwaterZone.Mid:
                    _targetFogColor   = midFogColor;
                    _targetFogDensity = midFogDensity;
                    break;
                case UnderwaterZone.Deep:
                    _targetFogColor   = deepFogColor;
                    _targetFogDensity = deepFogDensity;
                    break;
                case UnderwaterZone.Abyss:
                    _targetFogColor   = abyssFogColor;
                    _targetFogDensity = abyssFogDensity;
                    break;
            }
        }

        private void UpdateCausticsOverlay()
        {
            if (causticsOverlay == null) return;
            bool showCaustics = CurrentZone == UnderwaterZone.Shallow || CurrentZone == UnderwaterZone.Surface;
            if (causticsOverlay.activeSelf != showCaustics)
                causticsOverlay.SetActive(showCaustics);
        }

        private void SaveAndEnableUnderwaterFog()
        {
            RenderSettings.fog     = true;
            RenderSettings.fogMode = FogMode.Exponential;
            _targetFogColor        = surfaceFogColor;
            _targetFogDensity      = surfaceFogDensity;
        }

        private void RestoreAboveWaterFog()
        {
            RenderSettings.fog        = _fogWasEnabled;
            RenderSettings.fogMode    = _originalFogMode;
            _targetFogColor           = _originalFogColor;
            _targetFogDensity         = _originalFogDensity;
            if (causticsOverlay != null) causticsOverlay.SetActive(false);
        }

        private void PlayBubbles(bool active)
        {
            if (bubbleParticles == null) return;
            if (active)  bubbleParticles.Play();
            else         bubbleParticles.Stop();
        }

        private void SetAudioLowPass(bool active)
        {
            if (_audioManager == null) return;
            try
            {
                string methodName = active ? "SetLowPassFilter" : "ClearLowPassFilter";
                var method = _audioManager.GetType().GetMethod(methodName);
                method?.Invoke(_audioManager, active ? new object[] { 800f } : Array.Empty<object>());
            }
            catch { }
        }

        private void CacheCrossSystemReferences()
        {
            _crossSystemCacheDone = true;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var amType = assembly.GetType("SWEF.Audio.AudioManager");
                if (amType != null)
                {
                    _audioManager = FindObjectOfType(amType) as Component;
                    if (_audioManager != null) break;
                }
            }
        }

        #endregion
    }
}
