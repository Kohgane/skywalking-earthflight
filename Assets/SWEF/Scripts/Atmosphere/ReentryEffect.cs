using UnityEngine;
using SWEF.Flight;
using SWEF.Util;

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

        // ── Reflection cache (avoids repeated lookup for SWEF.Audio dependency) ──

        // SFX index used for the altitude-warning sound (matches AudioManager's clip array).
        private const int AltitudeWarningSfxIndex = 4;

        private static System.Type           _audioManagerType;
        private static System.Reflection.MethodInfo _playSfxMethod;

        private static void EnsureAudioReflection()
        {
            if (_audioManagerType != null) return;
            _audioManagerType = System.Type.GetType("SWEF.Audio.AudioManager, SWEF.Audio");
            if (_audioManagerType != null)
                _playSfxMethod = _audioManagerType.GetMethod("PlaySFX", new[] { typeof(int) });
        }

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
                    // Use reflection to avoid a hard compile-time dependency on SWEF.Audio
                    // (which would create a cyclic assembly reference Atmosphere → Audio → … → Atmosphere).
                    EnsureAudioReflection();
                    if (_audioManagerType != null)
                    {
                        var instance = _audioManagerType.GetProperty("Instance")?.GetValue(null);
                        if (instance != null)
                            _playSfxMethod?.Invoke(instance, new object[] { AltitudeWarningSfxIndex });
                    }
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
