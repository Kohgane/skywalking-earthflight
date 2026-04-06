// MarineFogController.cs — Phase 117: Advanced Ocean & Maritime System
// Marine fog/mist: advection fog, sea smoke, visibility gradients near surface.
// Namespace: SWEF.OceanSystem

using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Controls marine fog and mist rendering near the ocean surface.
    /// Simulates advection fog, sea smoke (steam fog), and altitude-based visibility
    /// gradients for low-flying aircraft.
    /// </summary>
    public class MarineFogController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Fog Settings")]
        [SerializeField] private float baseFogDensity  = 0.002f;
        [SerializeField] private float maxFogDensity   = 0.08f;
        [SerializeField] private Color fogColour       = new Color(0.75f, 0.8f, 0.85f);

        [Header("Altitude Gradient")]
        [Tooltip("Altitude below which fog starts to thicken (metres).")]
        [SerializeField] private float fogAltitudeStart = 200f;
        [Tooltip("Altitude at full fog density (metres).")]
        [SerializeField] private float fogAltitudeMax   = 20f;

        [Header("Sea Smoke")]
        [SerializeField] private ParticleSystem seaSmokeEffect;
        [SerializeField] private float seaSmokeWindThreshold = 3f;

        // ── Private state ─────────────────────────────────────────────────────────

        private float _targetDensity;
        private float _currentDensity;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Start()
        {
            RenderSettings.fog      = true;
            RenderSettings.fogColor = fogColour;
            RenderSettings.fogMode  = FogMode.Exponential;
        }

        private void Update()
        {
            ApplyAltitudeFog();
            AnimateFogDensity();
        }

        // ── Fog Logic ─────────────────────────────────────────────────────────────

        private void ApplyAltitudeFog()
        {
            var cam = Camera.main;
            if (cam == null) return;

            float altitude = cam.transform.position.y;
            float t = 1f - Mathf.InverseLerp(fogAltitudeMax, fogAltitudeStart, altitude);
            _targetDensity = Mathf.Lerp(0f, maxFogDensity, t);
        }

        private void AnimateFogDensity()
        {
            _currentDensity = Mathf.Lerp(_currentDensity, _targetDensity, Time.deltaTime * 0.5f);
            RenderSettings.fogDensity = Mathf.Max(baseFogDensity, _currentDensity);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Called by <see cref="OceanWeatherIntegration"/> to adjust fog from wind speed.
        /// </summary>
        public void SetFogIntensityFromWind(float windSpeedMs)
        {
            // Light winds → advection fog; strong winds → clear conditions
            float fogFactor = Mathf.Clamp01(1f - windSpeedMs / 15f);
            _targetDensity = fogFactor * maxFogDensity;

            // Sea smoke on light winds
            if (seaSmokeEffect != null)
            {
                bool shouldSmoke = windSpeedMs < seaSmokeWindThreshold;
                if (shouldSmoke && !seaSmokeEffect.isPlaying) seaSmokeEffect.Play();
                else if (!shouldSmoke && seaSmokeEffect.isPlaying) seaSmokeEffect.Stop();
            }
        }

        /// <summary>Sets the fog colour directly.</summary>
        public void SetFogColour(Color colour)
        {
            fogColour              = colour;
            RenderSettings.fogColor = colour;
        }
    }
}
