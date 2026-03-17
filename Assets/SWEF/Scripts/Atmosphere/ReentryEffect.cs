using UnityEngine;
using SWEF.Flight;
using SWEF.Util;
using SWEF.Audio;

namespace SWEF.Atmosphere
{
    /// <summary>
    /// Triggers atmospheric reentry visual effects when the player descends fast
    /// below <see cref="activationAltitude"/>.
    /// Scales particle emission rate and an optional screen glow proportionally
    /// to descent speed, using <see cref="ExpSmoothing.ExpLerp"/> for smooth transitions.
    /// </summary>
    public class ReentryEffect : MonoBehaviour
    {
        [SerializeField] private AltitudeController altitudeSource;
        [SerializeField] private FlightController   flightSource;

        [Header("Particles")]
        [SerializeField] private ParticleSystem reentryParticles;

        [Header("Thresholds")]
        [Tooltip("Effect can only activate below this altitude in meters.")]
        [SerializeField] private float activationAltitude = 120000f;

        [Tooltip("Minimum downward speed (m/s) required to start the effect.")]
        [SerializeField] private float minDescentSpeed = 100f;

        [Tooltip("Descent speed (m/s) that corresponds to maximum particle emission rate.")]
        [SerializeField] private float maxParticleRate = 500f;

        [Header("Screen Glow")]
        [Tooltip("Optional full-screen orange/red CanvasGroup overlay.")]
        [SerializeField] private CanvasGroup screenGlow;

        [Tooltip("Maximum alpha of the screen glow at peak reentry.")]
        [SerializeField] private float maxGlowAlpha = 0.3f;

        // ── State ─────────────────────────────────────────────────────────────────

        private float _previousAltitude;
        private float _smoothedEmissionRate;
        private float _smoothedGlowAlpha;
        private bool  _sfxPlaying;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            if (altitudeSource == null)
                altitudeSource = FindFirstObjectByType<AltitudeController>();
            if (flightSource == null)
                flightSource = FindFirstObjectByType<FlightController>();

            if (altitudeSource != null)
                _previousAltitude = altitudeSource.CurrentAltitudeMeters;

            // Ensure particles start stopped
            if (reentryParticles != null && reentryParticles.isPlaying)
                reentryParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (screenGlow != null)
                screenGlow.alpha = 0f;
        }

        private void Update()
        {
            if (altitudeSource == null) return;

            float alt = altitudeSource.CurrentAltitudeMeters;
            float dt  = Time.deltaTime;

            // Descent rate: positive value means descending
            float descentRate = (_previousAltitude - alt) / Mathf.Max(dt, 0.0001f);
            _previousAltitude = alt;

            bool inReentry = alt < activationAltitude && descentRate > minDescentSpeed;

            float targetRate = 0f;
            float targetGlow = 0f;

            if (inReentry)
            {
                float t = Mathf.Clamp01((descentRate - minDescentSpeed) /
                                        Mathf.Max(maxParticleRate - minDescentSpeed, 1f));
                targetRate = maxParticleRate * t;
                targetGlow = maxGlowAlpha * t;

                if (!_sfxPlaying)
                {
                    var audio = AudioManager.Instance;
                    if (audio != null)
                        audio.PlaySFX(4); // AltitudeWarning index
                    _sfxPlaying = true;
                }
            }
            else
            {
                _sfxPlaying = false;
            }

            _smoothedEmissionRate = ExpSmoothing.ExpLerp(_smoothedEmissionRate, targetRate, 3f, dt);
            _smoothedGlowAlpha    = ExpSmoothing.ExpLerp(_smoothedGlowAlpha,    targetGlow, 3f, dt);

            ApplyParticles(_smoothedEmissionRate);
            ApplyGlow(_smoothedGlowAlpha);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void ApplyParticles(float rate)
        {
            if (reentryParticles == null) return;

            if (rate < 0.5f)
            {
                if (reentryParticles.isEmitting)
                    reentryParticles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                return;
            }

            if (!reentryParticles.isPlaying)
                reentryParticles.Play();

            var emission = reentryParticles.emission;
            emission.rateOverTime = rate;
        }

        private void ApplyGlow(float alpha)
        {
            if (screenGlow == null) return;
            screenGlow.alpha = alpha;
        }
    }
}
