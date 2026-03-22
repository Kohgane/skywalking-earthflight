using UnityEngine;

namespace SWEF.TimeOfDay
{
    /// <summary>
    /// Applies a <see cref="LightingSnapshot"/> to the active Unity scene every update interval.
    /// <para>
    /// Manages two directional lights (sun and moon), <see cref="RenderSettings"/> fog and ambient,
    /// a skybox material, and an optional star particle system.  All transitions are smoothly
    /// interpolated via configurable lerp speeds.
    /// </para>
    /// Quality-tier awareness: on low-end devices (detected via <see cref="PerformanceProfiler"/>)
    /// expensive RenderSettings updates are skipped or throttled.
    /// </summary>
    public class LightingController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Directional Lights")]
        [Tooltip("Main sun directional light.")]
        [SerializeField] private Light sunLight;

        [Tooltip("Secondary moon directional light.")]
        [SerializeField] private Light moonLight;

        [Header("Skybox")]
        [Tooltip("Skybox material to modify at runtime. Must expose _Tint and _Exposure properties.")]
        [SerializeField] private Material skyboxMaterial;

        [Header("Stars")]
        [Tooltip("Particle system used to render the star field.")]
        [SerializeField] private ParticleSystem starParticles;

        [Header("Transition")]
        [Tooltip("Lerp speed for light color and intensity changes (units per second).")]
        [SerializeField, Range(0.1f, 20f)] private float lightLerpSpeed = 3f;

        [Tooltip("Lerp speed for ambient and fog color changes.")]
        [SerializeField, Range(0.1f, 20f)] private float ambientLerpSpeed = 2f;

        [Header("References (auto-found if null)")]
        [Tooltip("TimeOfDayManager to subscribe to. Auto-found if null.")]
        [SerializeField] private TimeOfDayManager timeOfDayManager;

        [Tooltip("Minimum Unity quality level (0–5) below which expensive updates are skipped. -1 = never skip.")]
        [SerializeField, Range(-1, 5)] private int lowEndQualityLevelThreshold = 1;

        // ── Skybox property IDs ───────────────────────────────────────────────────
        private static readonly int ShaderTint     = Shader.PropertyToID("_Tint");
        private static readonly int ShaderExposure = Shader.PropertyToID("_Exposure");

        // ── Internal state ────────────────────────────────────────────────────────
        private LightingSnapshot _target   = new LightingSnapshot();
        private LightingSnapshot _current  = new LightingSnapshot();
        private bool _isLowEnd;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (timeOfDayManager == null)
                timeOfDayManager = FindFirstObjectByType<TimeOfDayManager>();

            _isLowEnd = lowEndQualityLevelThreshold >= 0 &&
                        QualitySettings.GetQualityLevel() <= lowEndQualityLevelThreshold;
        }

        private void Update()
        {
            if (timeOfDayManager == null) return;

            _target = timeOfDayManager.GetCurrentLighting();
            ApplyLighting();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Forces an immediate (non-lerped) application of the given snapshot.</summary>
        public void ApplyImmediate(LightingSnapshot snapshot)
        {
            _current = snapshot;
            _target  = snapshot;
            ApplyToScene(snapshot);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void ApplyLighting()
        {
            float dt = Time.deltaTime;

            // Lerp current toward target
            _current.sunColor     = Color.Lerp(_current.sunColor,     _target.sunColor,     dt * lightLerpSpeed);
            _current.sunIntensity = Mathf.Lerp(_current.sunIntensity, _target.sunIntensity, dt * lightLerpSpeed);
            _current.moonColor    = Color.Lerp(_current.moonColor,    _target.moonColor,    dt * lightLerpSpeed);
            _current.moonIntensity= Mathf.Lerp(_current.moonIntensity,_target.moonIntensity,dt * lightLerpSpeed);

            if (!_isLowEnd)
            {
                _current.ambientSkyColor    = Color.Lerp(_current.ambientSkyColor,    _target.ambientSkyColor,    dt * ambientLerpSpeed);
                _current.ambientEquatorColor= Color.Lerp(_current.ambientEquatorColor,_target.ambientEquatorColor,dt * ambientLerpSpeed);
                _current.ambientGroundColor = Color.Lerp(_current.ambientGroundColor, _target.ambientGroundColor, dt * ambientLerpSpeed);
                _current.fogColor           = Color.Lerp(_current.fogColor,           _target.fogColor,           dt * ambientLerpSpeed);
                _current.fogDensity         = Mathf.Lerp(_current.fogDensity,         _target.fogDensity,         dt * ambientLerpSpeed);
                _current.skyboxExposure     = Mathf.Lerp(_current.skyboxExposure,     _target.skyboxExposure,     dt * lightLerpSpeed);
                _current.skyboxTint         = Color.Lerp(_current.skyboxTint,         _target.skyboxTint,         dt * lightLerpSpeed);
                _current.shadowStrength     = Mathf.Lerp(_current.shadowStrength,     _target.shadowStrength,     dt * lightLerpSpeed);
            }

            _current.starVisibility = Mathf.Lerp(_current.starVisibility, _target.starVisibility, dt * ambientLerpSpeed);

            ApplyToScene(_current);
        }

        private void ApplyToScene(LightingSnapshot s)
        {
            SunMoonState state = timeOfDayManager?.GetSunMoonState();

            // ── Sun directional light ──────────────────────────────────────────────
            if (sunLight != null)
            {
                if (state != null)
                    sunLight.transform.forward = -state.sunDirection;
                sunLight.color     = s.sunColor;
                sunLight.intensity = s.sunIntensity;
                if (sunLight.shadows != LightShadows.None)
                    sunLight.shadowStrength = s.shadowStrength;
            }

            // ── Moon directional light ────────────────────────────────────────────
            if (moonLight != null)
            {
                if (state != null)
                    moonLight.transform.forward = -state.moonDirection;
                moonLight.color     = s.moonColor;
                moonLight.intensity = s.moonIntensity;
            }

            if (_isLowEnd) return; // skip expensive updates on low-end

            // ── Ambient ───────────────────────────────────────────────────────────
            RenderSettings.ambientMode         = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor     = s.ambientSkyColor;
            RenderSettings.ambientEquatorColor = s.ambientEquatorColor;
            RenderSettings.ambientGroundColor  = s.ambientGroundColor;

            // ── Fog ───────────────────────────────────────────────────────────────
            RenderSettings.fogColor   = s.fogColor;
            RenderSettings.fogDensity = s.fogDensity;

            // ── Skybox ────────────────────────────────────────────────────────────
            if (skyboxMaterial != null)
            {
                skyboxMaterial.SetColor(ShaderTint, s.skyboxTint);
                skyboxMaterial.SetFloat(ShaderExposure, s.skyboxExposure);
            }

            // ── Stars ─────────────────────────────────────────────────────────────
            if (starParticles != null)
            {
                var main = starParticles.main;
                Color c  = main.startColor.color;
                c.a      = s.starVisibility;
                main.startColor = c;

                if (s.starVisibility > 0.01f && !starParticles.isPlaying)
                    starParticles.Play();
                else if (s.starVisibility <= 0.01f && starParticles.isPlaying)
                    starParticles.Stop();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (sunLight != null)
            {
                UnityEditor.Handles.color = Color.yellow;
                UnityEditor.Handles.DrawLine(sunLight.transform.position,
                    sunLight.transform.position + sunLight.transform.forward * 5f);
            }
        }
#endif
    }
}
