// EngineExhaustController.cs — SWEF Particle Effects & VFX System
using System.Collections;
using UnityEngine;

namespace SWEF.VFX
{
    /// <summary>
    /// Controls aircraft engine VFX driven by throttle and altitude data.
    ///
    /// <para>Throttle input (0–1) modulates flame length, colour, and intensity.
    /// Altitude drives an exhaust colour shift from blue (sea level) through orange
    /// (stratosphere) to invisible (near-vacuum). Contrails are generated above a
    /// configurable altitude threshold. Boost/afterburner triggers an additional
    /// flare effect.</para>
    ///
    /// <para>Integrates with <c>FlightController</c> and <c>AltitudeController</c>
    /// when the <c>SWEF_FLIGHT_AVAILABLE</c> compile symbol is defined; otherwise
    /// uses the inspector-exposed <see cref="throttle"/> and <see cref="altitudeMetres"/>
    /// fields which can be driven externally.</para>
    /// </summary>
    public sealed class EngineExhaustController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Particle Systems")]
        [Tooltip("Primary engine exhaust flame particle system.")]
        [SerializeField] private ParticleSystem exhaustFlame;

        [Tooltip("Afterburner / boost flare particle system.")]
        [SerializeField] private ParticleSystem afterburnerFlare;

        [Tooltip("Contrail particle system activated at high altitude.")]
        [SerializeField] private ParticleSystem contrailSystem;

        [Tooltip("Heat distortion particle or renderer overlay.")]
        [SerializeField] private ParticleSystem heatDistortion;

        [Header("Throttle Settings")]
        [Tooltip("Current throttle value (0 = idle, 1 = full thrust). Driven by FlightController when available.")]
        [SerializeField, Range(0f, 1f)] private float throttle;

        [Tooltip("Minimum flame scale multiplier at zero throttle.")]
        [SerializeField, Min(0f)] private float idleFlameScale = 0.1f;

        [Tooltip("Maximum flame scale multiplier at full throttle.")]
        [SerializeField, Min(0f)] private float fullFlameScale = 1f;

        [Tooltip("Throttle threshold above which the afterburner flare activates.")]
        [SerializeField, Range(0f, 1f)] private float afterburnerThreshold = 0.9f;

        [Header("Altitude Settings")]
        [Tooltip("Current altitude above sea level in metres. Driven by AltitudeController when available.")]
        [SerializeField, Min(0f)] private float altitudeMetres;

        [Tooltip("Altitude in metres above which contrails are generated (default 8 000 m).")]
        [SerializeField, Min(0f)] private float contrailAltitude = 8_000f;

        [Tooltip("Altitude in metres above which the exhaust becomes fully transparent (vacuum).")]
        [SerializeField, Min(0f)] private float vacuumAltitude = 80_000f;

        [Header("Colour Gradients")]
        [Tooltip("Exhaust colour gradient mapped from sea-level (left) to vacuum (right).")]
        [SerializeField] private Gradient exhaustColorGradient;

        [Header("Smoothing")]
        [Tooltip("Smoothing speed for throttle → particle transition.")]
        [SerializeField, Min(0f)] private float throttleSmoothSpeed = 5f;

        // ── Private State ─────────────────────────────────────────────────────────

        private float _smoothedThrottle;
        private bool  _contrailActive;
        private bool  _afterburnerActive;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            _smoothedThrottle = throttle;
            InitGradient();
        }

        private void Update()
        {
            PollIntegrations();
            _smoothedThrottle = Mathf.Lerp(_smoothedThrottle, throttle, Time.deltaTime * throttleSmoothSpeed);

            UpdateExhaustFlame();
            UpdateContrail();
            UpdateAfterburner();
            UpdateHeatDistortion();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Sets the throttle value directly (0 = idle, 1 = full thrust).</summary>
        /// <param name="value">Normalised throttle (0–1).</param>
        public void SetThrottle(float value) => throttle = Mathf.Clamp01(value);

        /// <summary>Sets the current altitude in metres above sea level.</summary>
        /// <param name="metres">Altitude in metres.</param>
        public void SetAltitude(float metres) => altitudeMetres = Mathf.Max(0f, metres);

        /// <summary>Immediately triggers the afterburner flare ignition animation.</summary>
        public void TriggerAfterburner() => StartCoroutine(AfterburnerRoutine());

        // ── Internal Updates ──────────────────────────────────────────────────────

        private void UpdateExhaustFlame()
        {
            if (exhaustFlame == null) return;

            float scaleT   = Mathf.InverseLerp(0f, 1f, _smoothedThrottle);
            float scale    = Mathf.Lerp(idleFlameScale, fullFlameScale, scaleT);
            float altT     = Mathf.Clamp01(altitudeMetres / vacuumAltitude);
            float alpha    = 1f - altT;

            var main = exhaustFlame.main;
            main.startSize      = scale;
            main.startLifetime  = scale * 0.8f + 0.1f;

            Color col = exhaustColorGradient.Evaluate(altT);
            col.a = alpha;
            main.startColor = col;

            if (!exhaustFlame.isPlaying && _smoothedThrottle > 0.01f)
                exhaustFlame.Play();
            else if (exhaustFlame.isPlaying && _smoothedThrottle <= 0.01f)
                exhaustFlame.Stop();
        }

        private void UpdateContrail()
        {
            if (contrailSystem == null) return;

            bool shouldShow = altitudeMetres >= contrailAltitude;
            if (shouldShow != _contrailActive)
            {
                _contrailActive = shouldShow;
                if (shouldShow) contrailSystem.Play();
                else contrailSystem.Stop();
            }
        }

        private void UpdateAfterburner()
        {
            if (afterburnerFlare == null) return;

            bool shouldShow = _smoothedThrottle >= afterburnerThreshold;
            if (shouldShow != _afterburnerActive)
            {
                _afterburnerActive = shouldShow;
                if (shouldShow) afterburnerFlare.Play();
                else afterburnerFlare.Stop();
            }
        }

        private void UpdateHeatDistortion()
        {
            if (heatDistortion == null) return;

            var main        = heatDistortion.main;
            main.startSize  = _smoothedThrottle * 0.5f;
            if (_smoothedThrottle > 0.3f && !heatDistortion.isPlaying) heatDistortion.Play();
            else if (_smoothedThrottle <= 0.3f && heatDistortion.isPlaying) heatDistortion.Stop();
        }

        private IEnumerator AfterburnerRoutine()
        {
            if (afterburnerFlare != null) afterburnerFlare.Play();
            yield return new WaitForSeconds(0.5f);
        }

        private void InitGradient()
        {
            if (exhaustColorGradient != null && exhaustColorGradient.colorKeys.Length > 0) return;

            // Default blue → orange → transparent gradient
            exhaustColorGradient = new Gradient();
            exhaustColorGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.4f, 0.6f, 1f), 0f),
                    new GradientColorKey(new Color(1f, 0.55f, 0f), 0.4f),
                    new GradientColorKey(new Color(0.9f, 0.9f, 0.9f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                });
        }

        private void PollIntegrations()
        {
#if SWEF_FLIGHT_AVAILABLE
            if (SWEF.Flight.FlightController.Instance != null)
                throttle = SWEF.Flight.FlightController.Instance.Throttle;
            if (SWEF.Flight.AltitudeController.Instance != null)
                altitudeMetres = SWEF.Flight.AltitudeController.Instance.AltitudeMetres;
#endif
        }

#if UNITY_EDITOR
        [ContextMenu("Test Full Throttle")]
        private void EditorTestFullThrottle() => SetThrottle(1f);

        [ContextMenu("Test Idle")]
        private void EditorTestIdle() => SetThrottle(0f);

        [ContextMenu("Test High Altitude Contrail")]
        private void EditorTestContrail() => SetAltitude(10_000f);
#endif
    }
}
