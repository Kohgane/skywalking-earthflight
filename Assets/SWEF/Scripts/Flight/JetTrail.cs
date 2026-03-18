using UnityEngine;
using SWEF.Util;

namespace SWEF.Flight
{
    /// <summary>
    /// Speed-based jet trail renderer.
    /// Controls a <see cref="TrailRenderer"/>'s width, colour, and emission
    /// proportional to the current flight speed.
    /// </summary>
    public class JetTrail : MonoBehaviour
    {
        [SerializeField] private FlightController flight;
        [SerializeField] private TrailRenderer    trail;

        [Header("Speed Thresholds")]
        [Tooltip("Trail stays invisible below this speed (m/s).")]
        [SerializeField] private float minSpeedForTrail = 50f;

        [Tooltip("Speed (m/s) at which the trail reaches maximum intensity.")]
        [SerializeField] private float maxTrailSpeed = 250f;

        [Header("Visual")]
        [SerializeField] private float trailWidthMin = 0.5f;
        [SerializeField] private float trailWidthMax = 3f;

        [Tooltip("Trail colour evaluated from normalised speed (0 = subtle white, 1 = bright blue).")]
        [SerializeField] private Gradient trailColorGradient;

        // ── Cached values ─────────────────────────────────────────────────────────

        private float _initialTime;
        private float _smoothedT; // normalised speed 0..1, smoothed

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            if (flight == null)
                flight = FindFirstObjectByType<FlightController>();

            if (trail == null)
                trail = GetComponent<TrailRenderer>();

            if (trail != null)
            {
                _initialTime = trail.time;
                trail.emitting = false;
            }
        }

        private void Update()
        {
            if (flight == null || trail == null) return;

            float speed = flight.CurrentSpeedMps;
            float targetT = speed < minSpeedForTrail
                ? 0f
                : Mathf.Clamp01((speed - minSpeedForTrail) /
                                 Mathf.Max(maxTrailSpeed - minSpeedForTrail, 1f));

            _smoothedT = ExpSmoothing.ExpLerp(_smoothedT, targetT, 5f, Time.deltaTime);

            bool shouldEmit = _smoothedT > 0.01f;
            if (trail.emitting != shouldEmit)
                trail.emitting = shouldEmit;

            if (!shouldEmit) return;

            // Width
            trail.startWidth = Mathf.Lerp(trailWidthMin, trailWidthMax, _smoothedT);
            trail.endWidth   = 0f;

            // Colour
            if (trailColorGradient != null)
            {
                Color c = trailColorGradient.Evaluate(_smoothedT);
                // Keep the existing alpha from the gradient but respect smoothedT for fade-in
                c.a *= _smoothedT;
                trail.startColor = c;
                Color endColor = c;
                endColor.a = 0f;
                trail.endColor = endColor;
            }
        }
        // ── Phase 20 — Remote trail control ──────────────────────────────────────

        /// <summary>
        /// Sets the jet trail state from a remote sync packet.
        /// 0 = off, 1+ = on (higher values = higher intensity override).
        /// </summary>
        /// <param name="state">Trail state integer from <see cref="SWEF.Multiplayer.PlayerSyncData"/>.</param>
        public void SetTrailState(int state)
        {
            if (trail == null) return;

            if (state <= 0)
            {
                trail.emitting = false;
                return;
            }

            float intensity = Mathf.Clamp01(state / 255f);
            trail.emitting   = true;
            trail.startWidth = Mathf.Lerp(trailWidthMin, trailWidthMax, intensity);
            trail.endWidth   = 0f;
        }
    }
}
