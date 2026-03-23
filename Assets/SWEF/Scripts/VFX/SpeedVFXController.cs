// SpeedVFXController.cs — SWEF Particle Effects & VFX System
using System.Collections;
using UnityEngine;

namespace SWEF.VFX
{
    /// <summary>
    /// Drives speed-reactive visual effects based on the aircraft's current velocity.
    ///
    /// <para>
    /// <list type="bullet">
    /// <item><description>Speed lines appear above 200 m/s and intensify with speed.</description></item>
    /// <item><description>Wing-tip vortices activate in humid conditions.</description></item>
    /// <item><description>A sonic boom shockwave ring fires once at the Mach 1 transition.</description></item>
    /// <item><description>Reentry flame/plasma effects activate above 2 000 m/s.</description></item>
    /// <item><description>Screen shake is forwarded to <c>CameraController</c> when available.</description></item>
    /// </list>
    /// </para>
    ///
    /// <para>Integrates with <c>FlightController</c> and <c>CameraController</c> when
    /// the <c>SWEF_FLIGHT_AVAILABLE</c> compile symbol is defined.</para>
    /// </summary>
    public sealed class SpeedVFXController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Particle Systems")]
        [Tooltip("Radial speed lines emitted around the aircraft at high speed.")]
        [SerializeField] private ParticleSystem speedLinesSystem;

        [Tooltip("Wing-tip vortex swirl particles.")]
        [SerializeField] private ParticleSystem wingTipVortexSystem;

        [Tooltip("Sonic boom shockwave ring prefab — played once at Mach transition.")]
        [SerializeField] private ParticleSystem sonicBoomSystem;

        [Tooltip("Reentry plasma / flame particle system active above reentry speed.")]
        [SerializeField] private ParticleSystem reentryFlameSystem;

        [Tooltip("Mach diamond pattern particle system visible in exhaust plume.")]
        [SerializeField] private ParticleSystem machDiamondSystem;

        [Header("Speed Thresholds (m/s)")]
        [Tooltip("Speed above which speed lines begin appearing.")]
        [SerializeField, Min(0f)] private float speedLinesThreshold = 200f;

        [Tooltip("Speed of sound reference for Mach calculation (m/s, sea level).")]
        [SerializeField, Min(1f)] private float speedOfSound = 343f;

        [Tooltip("Speed in m/s above which reentry plasma effects activate.")]
        [SerializeField, Min(0f)] private float reentrySpeedThreshold = 2_000f;

        [Header("Intensity Scaling")]
        [Tooltip("Speed lines emission rate at reference maximum speed.")]
        [SerializeField, Min(0f)] private float maxSpeedLinesEmission = 200f;

        [Tooltip("Reference speed (m/s) at which speed lines reach full emission.")]
        [SerializeField, Min(1f)] private float speedLinesMaxSpeed = 1_000f;

        [Header("Wing-tip Vortices")]
        [Tooltip("Humidity threshold 0–1 above which wing-tip vortices appear.")]
        [SerializeField, Range(0f, 1f)] private float vortexHumidityThreshold = 0.6f;

        [Tooltip("Current ambient humidity 0–1. Can be set externally or from WeatherManager.")]
        [SerializeField, Range(0f, 1f)] private float currentHumidity = 0.5f;

        [Header("Screen Shake")]
        [Tooltip("Sonic boom camera shake magnitude.")]
        [SerializeField, Min(0f)] private float sonicBoomShakeMagnitude = 0.4f;

        [Tooltip("Sonic boom camera shake duration in seconds.")]
        [SerializeField, Min(0f)] private float sonicBoomShakeDuration = 0.6f;

        [Header("External State")]
        [Tooltip("Current aircraft speed in m/s. Driven by FlightController when available.")]
        [SerializeField, Min(0f)] private float currentSpeedMs;

        // ── Private State ─────────────────────────────────────────────────────────

        private bool  _sonicBoomFired;
        private bool  _reentryActive;
        private float _previousSpeedMs;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Update()
        {
            PollIntegrations();
            UpdateSpeedLines();
            UpdateWingTipVortices();
            CheckSonicBoom();
            UpdateReentryFlame();
            _previousSpeedMs = currentSpeedMs;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Sets the current aircraft speed externally.</summary>
        /// <param name="speedMs">Speed in metres per second.</param>
        public void SetSpeed(float speedMs) => currentSpeedMs = Mathf.Max(0f, speedMs);

        /// <summary>Sets the ambient humidity for wing-tip vortex evaluation.</summary>
        /// <param name="humidity">Humidity 0–1.</param>
        public void SetHumidity(float humidity) => currentHumidity = Mathf.Clamp01(humidity);

        // ── Internal Updates ──────────────────────────────────────────────────────

        private void UpdateSpeedLines()
        {
            if (speedLinesSystem == null) return;

            if (currentSpeedMs >= speedLinesThreshold)
            {
                float t    = Mathf.Clamp01((currentSpeedMs - speedLinesThreshold) / (speedLinesMaxSpeed - speedLinesThreshold));
                var emission = speedLinesSystem.emission;
                emission.rateOverTime = maxSpeedLinesEmission * t;
                if (!speedLinesSystem.isPlaying) speedLinesSystem.Play();
            }
            else
            {
                if (speedLinesSystem.isPlaying)
                    speedLinesSystem.Stop(withChildren: true, stopBehavior: ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void UpdateWingTipVortices()
        {
            if (wingTipVortexSystem == null) return;

            bool shouldShow = currentHumidity >= vortexHumidityThreshold && currentSpeedMs > 50f;
            if (shouldShow && !wingTipVortexSystem.isPlaying)
                wingTipVortexSystem.Play();
            else if (!shouldShow && wingTipVortexSystem.isPlaying)
                wingTipVortexSystem.Stop(withChildren: true, stopBehavior: ParticleSystemStopBehavior.StopEmitting);
        }

        private void CheckSonicBoom()
        {
            float prevMach = _previousSpeedMs / speedOfSound;
            float currMach = currentSpeedMs    / speedOfSound;

            // Fire once on Mach 1 crossing (both directions for realism)
            if ((prevMach < 1f && currMach >= 1f) || (prevMach >= 1f && currMach < 1f))
            {
                if (!_sonicBoomFired)
                {
                    _sonicBoomFired = true;
                    StartCoroutine(ResetSonicBoomFlag());
                    TriggerSonicBoom();
                }
            }

            // Mach diamond effect in supersonic range
            if (machDiamondSystem != null)
            {
                if (currMach >= 1.05f && !machDiamondSystem.isPlaying) machDiamondSystem.Play();
                else if (currMach < 1.05f && machDiamondSystem.isPlaying) machDiamondSystem.Stop();
            }
        }

        private IEnumerator ResetSonicBoomFlag()
        {
            yield return new WaitForSeconds(2f);
            _sonicBoomFired = false;
        }

        private void TriggerSonicBoom()
        {
            if (sonicBoomSystem != null)
                sonicBoomSystem.Play(withChildren: true);

            TriggerScreenShake(sonicBoomShakeMagnitude, sonicBoomShakeDuration);

            if (VFXTriggerSystem.Instance != null)
                VFXTriggerSystem.Instance.FireImmediate(VFXType.SonicBoom, transform.position);
        }

        private void UpdateReentryFlame()
        {
            if (reentryFlameSystem == null) return;

            bool shouldShow = currentSpeedMs >= reentrySpeedThreshold;
            if (shouldShow != _reentryActive)
            {
                _reentryActive = shouldShow;
                if (shouldShow) reentryFlameSystem.Play(withChildren: true);
                else reentryFlameSystem.Stop(withChildren: true, stopBehavior: ParticleSystemStopBehavior.StopEmitting);
            }

            if (_reentryActive)
            {
                float t = Mathf.Clamp01((currentSpeedMs - reentrySpeedThreshold) / 5_000f);
                var main = reentryFlameSystem.main;
                main.startColor = Color.Lerp(new Color(1f, 0.4f, 0f), Color.white, t);
            }
        }

        private static void TriggerScreenShake(float magnitude, float duration)
        {
#if SWEF_FLIGHT_AVAILABLE
            if (SWEF.Flight.CameraController.Instance != null)
                SWEF.Flight.CameraController.Instance.TriggerShake(magnitude, duration);
#endif
        }

        private void PollIntegrations()
        {
#if SWEF_FLIGHT_AVAILABLE
            if (SWEF.Flight.FlightController.Instance != null)
                currentSpeedMs = SWEF.Flight.FlightController.Instance.SpeedMs;
#endif
#if SWEF_WEATHER_AVAILABLE
            if (SWEF.Weather.WeatherManager.Instance != null)
                currentHumidity = SWEF.Weather.WeatherManager.Instance.CurrentWeather.humidity;
#endif
        }

#if UNITY_EDITOR
        [ContextMenu("Test Mach 1 Crossing")]
        private void EditorTestSonicBoom()
        {
            _previousSpeedMs = 340f;
            currentSpeedMs   = 346f;
        }

        [ContextMenu("Test Reentry Speed")]
        private void EditorTestReentry() => currentSpeedMs = 2500f;

        [ContextMenu("Test Speed Lines")]
        private void EditorTestSpeedLines() => currentSpeedMs = 500f;
#endif
    }
}
