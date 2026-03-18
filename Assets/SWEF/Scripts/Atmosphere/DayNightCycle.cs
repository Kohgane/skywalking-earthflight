using UnityEngine;

namespace SWEF.Atmosphere
{
    /// <summary>
    /// Real-time day/night cycle controller.
    /// Rotates a directional light, applies a colour gradient and intensity curve,
    /// and blends ambient intensity to simulate the passage of time.
    /// </summary>
    public class DayNightCycle : MonoBehaviour
    {
        [SerializeField] private Light sunLight;

        [Tooltip("Length of a full day/night cycle in real-world minutes.")]
        [SerializeField] private float dayDurationMinutes = 24f;

        [Tooltip("When true the controller uses the device's current UTC time instead of a simulated clock.")]
        [SerializeField] private bool useRealTime = false;

        [Tooltip("Sun colour keyed from midnight (0) through noon (0.5) to midnight (1).")]
        [SerializeField] private Gradient sunColorGradient;

        [Tooltip("Sun intensity keyed from midnight (0) through noon (0.5) to midnight (1). 0 = night, 1+ = noon.")]
        [SerializeField] private AnimationCurve sunIntensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField] private float nightAmbientIntensity = 0.05f;
        [SerializeField] private float dayAmbientIntensity   = 1.0f;

        // 0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset, 1 = midnight
        private float _timeOfDay;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Current time of day normalised to 0–1 (0 = midnight, 0.5 = noon).</summary>
        public float TimeOfDay01 => _timeOfDay;

        /// <summary>Returns true when the current time of day is considered night (before 06:00 or after 18:00).</summary>
        public bool IsNight => _timeOfDay < 0.25f || _timeOfDay > 0.75f;

        /// <summary>Manually override the time of day (0–1).</summary>
        public void SetTimeOfDay(float t01)
        {
            _timeOfDay = Mathf.Repeat(t01, 1f);
            ApplySunTransform();
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            if (useRealTime)
            {
                var now = System.DateTime.UtcNow;
                _timeOfDay = (float)(now.Hour * 3600 + now.Minute * 60 + now.Second) / 86400f;
            }
        }

        private void Update()
        {
            if (useRealTime)
            {
                var now = System.DateTime.UtcNow;
                _timeOfDay = (float)(now.Hour * 3600 + now.Minute * 60 + now.Second) / 86400f;
            }
            else
            {
                float cycleLengthSeconds = dayDurationMinutes * 60f;
                if (cycleLengthSeconds > 0f)
                    _timeOfDay = Mathf.Repeat(_timeOfDay + Time.deltaTime / cycleLengthSeconds, 1f);
            }

            ApplySunTransform();
        }

        // ── Phase 18 — Time of Day ────────────────────────────────────────────────
        [Header("Phase 18 — Time of Day")]
        [SerializeField] private SWEF.Cinema.TimeOfDayController timeOfDayController;

        private void OnEnable()
        {
            if (timeOfDayController != null)
                timeOfDayController.OnTimeChanged += HandleTimeChanged;
        }

        private void OnDisable()
        {
            if (timeOfDayController != null)
                timeOfDayController.OnTimeChanged -= HandleTimeChanged;
        }

        private void HandleTimeChanged(float hour)
        {
            // Sync internal state with TimeOfDayController (hour 0–24 → normalised 0–1)
            _timeOfDay = hour / 24f;
            ApplySunTransform();
            Debug.Log($"[SWEF] DayNightCycle synced to TimeOfDay: {hour:F1}h");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void ApplySunTransform()
        {
            if (sunLight == null) return;

            // Rotate sun: 0 (midnight) → 0°, 0.25 (sunrise) → 90°, 0.5 (noon) → 180°, 0.75 (sunset) → 270°
            float sunAngle = _timeOfDay * 360f;
            sunLight.transform.localRotation = Quaternion.Euler(sunAngle - 90f, 170f, 0f);

            // Colour and intensity from designer curves/gradient
            if (sunColorGradient != null)
                sunLight.color = sunColorGradient.Evaluate(_timeOfDay);

            sunLight.intensity = sunIntensityCurve.Evaluate(_timeOfDay);

            // Ambient intensity
            float ambientTarget = IsNight ? nightAmbientIntensity : dayAmbientIntensity;
            RenderSettings.ambientIntensity =
                Mathf.Lerp(RenderSettings.ambientIntensity, ambientTarget,
                           1f - Mathf.Exp(-3f * Time.deltaTime));
        }
    }
}
