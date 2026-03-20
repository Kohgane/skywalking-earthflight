using UnityEngine;
using SWEF.Flight;

namespace SWEF.Audio
{
    /// <summary>
    /// Computes Doppler pitch multipliers for spatial audio sources based on player velocity
    /// and altitude-corrected speed of sound.
    /// </summary>
    public class DopplerEffectController : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────────
        /// <summary>Speed of sound at sea level in m/s.</summary>
        public const float SeaLevelSpeedOfSound = 343f;

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Doppler")]
        [Range(0f, 2f)]
        [SerializeField] private float dopplerIntensity = 1f;

        [Header("Refs (auto-found if null)")]
        [SerializeField] private FlightController flightController;
        [SerializeField] private AltitudeController altitudeController;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Whether Doppler processing is currently active.</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>Intensity multiplier (0 = no effect, 2 = exaggerated).</summary>
        public float DopplerIntensity
        {
            get => dopplerIntensity;
            set => dopplerIntensity = Mathf.Clamp(value, 0f, 2f);
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (flightController == null)
                flightController = FindFirstObjectByType<FlightController>();
            if (altitudeController == null)
                altitudeController = FindFirstObjectByType<AltitudeController>();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the Doppler pitch multiplier for a sound source at <paramref name="sourcePos"/>
        /// with velocity <paramref name="sourceVelocity"/>.
        /// </summary>
        public float CalculateDoppler(Vector3 sourcePos, Vector3 sourceVelocity)
        {
            if (!IsEnabled || dopplerIntensity <= 0f) return 1f;

            float alt  = altitudeController != null ? altitudeController.CurrentAltitudeMeters : 0f;
            float sos  = GetSpeedOfSound(alt);
            if (sos <= 0f) return 1f;

            Vector3 listenerPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            Vector3 listenerVel = flightController != null ? flightController.Velocity : Vector3.zero;

            Vector3 toListener = (listenerPos - sourcePos).normalized;

            float vSource   = Vector3.Dot(sourceVelocity, -toListener);
            float vListener = Vector3.Dot(listenerVel,    toListener);

            float denom = sos - vSource * dopplerIntensity;
            if (Mathf.Abs(denom) < 1f) denom = Mathf.Sign(denom);

            float pitch = (sos + vListener * dopplerIntensity) / denom;
            return Mathf.Clamp(pitch, 0.5f, 2.0f);
        }

        /// <summary>
        /// Returns the speed of sound (m/s) at the given altitude.
        /// Drops from 343 m/s at sea level, falls off in stratosphere, and reaches ~0 in space.
        /// </summary>
        public static float GetSpeedOfSound(float altitude)
        {
            if (altitude >= 120000f) return 0f;
            if (altitude >= 80000f)  return Mathf.Lerp(0f, 50f, 1f - (altitude - 80000f) / 40000f);
            if (altitude >= 20000f)  return Mathf.Lerp(50f, 295f, 1f - (altitude - 20000f) / 60000f);
            if (altitude >= 11000f)  return 295f; // tropopause — constant ~-56°C
            // Troposphere: linear temperature lapse
            float t = 1f - altitude / 11000f;
            return Mathf.Lerp(295f, SeaLevelSpeedOfSound, t);
        }
    }
}
