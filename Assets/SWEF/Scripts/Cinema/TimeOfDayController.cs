using System;
using UnityEngine;

namespace SWEF.Cinema
{
    /// <summary>
    /// Controls sun/moon position and scene lighting based on a 0–24 h time-of-day value.
    /// Supports real-world time sync, animated sky blending, star particles, and
    /// integration with <see cref="SWEF.Atmosphere.DayNightCycle"/>.
    /// Phase 18 — Time-of-Day System.
    /// </summary>
    public class TimeOfDayController : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Lights")]
        [SerializeField] private Light sunLight;
        [SerializeField] private Light moonLight;

        [Header("Time")]
        [SerializeField] private float timeOfDay = 12.0f;
        [SerializeField] private float timeSpeed  = 0.0f;
        [SerializeField] private bool  useRealWorldTime = false;

        [Header("Ambient / Sun Curves")]
        [SerializeField] private Gradient       ambientColorOverDay;
        [SerializeField] private AnimationCurve sunIntensityCurve    = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve ambientIntensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Sky Colors")]
        [SerializeField] private Color daySkyColor    = new Color(0.5f, 0.7f, 1.0f);
        [SerializeField] private Color sunsetSkyColor = new Color(1.0f, 0.5f, 0.2f);
        [SerializeField] private Color nightSkyColor  = new Color(0.02f, 0.02f, 0.08f);
        [SerializeField] private Material skyboxMaterial;

        [Header("Stars")]
        [SerializeField] private ParticleSystem starParticles;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired every frame the time changes, passing the new hour (0–24).</summary>
        public event Action<float> OnTimeChanged;

        /// <summary>Fired when the day/night state crosses a boundary. Argument is true when transitioning to day.</summary>
        public event Action<bool> OnDayNightTransition;

        // ── Computed properties ──────────────────────────────────────────────────
        /// <summary>Returns the current time of day in 0–24 h.</summary>
        public float GetTimeOfDay() => timeOfDay;

        /// <summary>Returns the time formatted as "HH:MM".</summary>
        public string GetTimeString()
        {
            int hours   = Mathf.FloorToInt(timeOfDay) % 24;
            int minutes = Mathf.FloorToInt((timeOfDay - Mathf.Floor(timeOfDay)) * 60f);
            return $"{hours:D2}:{minutes:D2}";
        }

        /// <summary>True between 06:00 and 17:59.</summary>
        public bool IsDaytime => timeOfDay >= 6f && timeOfDay < 18f;

        /// <summary>True during golden-hour windows (06:00–07:30 or 17:00–18:30).</summary>
        public bool IsGoldenHour =>
            (timeOfDay >= 6f && timeOfDay < 7.5f) ||
            (timeOfDay >= 17f && timeOfDay < 18.5f);

        /// <summary>True before 05:00 or at/after 20:00.</summary>
        public bool IsNight => timeOfDay < 5f || timeOfDay >= 20f;

        // ── State ─────────────────────────────────────────────────────────────────
        private bool _wasDaytime;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Start()
        {
            if (useRealWorldTime) SyncToRealTime();
            _wasDaytime = IsDaytime;
            ApplyAllLighting();
        }

        private void Update()
        {
            if (useRealWorldTime)
            {
                SyncToRealTime();
            }
            else if (timeSpeed > 0f)
            {
                // timeSpeed in in-game hours per real second (1.0 = 1 h every real second; 3600 = real-time)
                timeOfDay += timeSpeed * Time.deltaTime;
                if (timeOfDay >= 24f) timeOfDay -= 24f;
            }

            ApplyAllLighting();
            OnTimeChanged?.Invoke(timeOfDay);

            bool nowDaytime = IsDaytime;
            if (nowDaytime != _wasDaytime)
            {
                OnDayNightTransition?.Invoke(nowDaytime);
                _wasDaytime = nowDaytime;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────
        /// <summary>Sets the time of day clamped to [0, 24) and immediately updates all lighting.</summary>
        public void SetTimeOfDay(float hour)
        {
            timeOfDay = Mathf.Clamp(hour, 0f, 24f);
            if (timeOfDay >= 24f) timeOfDay = 0f;
            ApplyAllLighting();
            OnTimeChanged?.Invoke(timeOfDay);
        }

        /// <summary>Sets the time-advance speed (0 = paused, 1 = real-time, >1 = accelerated). Clamped to [0, 100].</summary>
        public void SetTimeSpeed(float speed)
        {
            timeSpeed = Mathf.Clamp(speed, 0f, 100f);
        }

        /// <summary>Enables or disables real-world time synchronisation.</summary>
        public void ToggleRealWorldTime(bool enabled)
        {
            useRealWorldTime = enabled;
            if (enabled) SyncToRealTime();
        }

        // ── Internals ────────────────────────────────────────────────────────────
        private void SyncToRealTime()
        {
            var now   = DateTime.Now;
            timeOfDay = now.Hour + now.Minute / 60f + now.Second / 3600f;
        }

        private void ApplyAllLighting()
        {
            UpdateSunPosition();
            UpdateSkyColor();
            UpdateStars();
        }

        /// <summary>
        /// Calculates and applies sun/moon rotation and intensity.
        /// Sun altitude (degrees above horizon) = 90 − |timeOfDay − 12| × 15.
        /// </summary>
        private void UpdateSunPosition()
        {
            float sunAltitude = 90f - Mathf.Abs(timeOfDay - 12f) * 15f;
            float sunAzimuth  = (timeOfDay / 24f) * 360f;

            float t01 = timeOfDay / 24f; // normalised 0–1

            if (sunLight != null)
            {
                sunLight.transform.rotation = Quaternion.Euler(sunAltitude, sunAzimuth, 0f);
                sunLight.intensity          = sunIntensityCurve.Evaluate(t01);
                sunLight.enabled            = sunAltitude > -10f;
            }

            if (moonLight != null)
            {
                // Moon is roughly opposite the sun
                moonLight.transform.rotation = Quaternion.Euler(-sunAltitude, sunAzimuth + 180f, 0f);
                moonLight.enabled            = sunAltitude < 5f;
                moonLight.intensity          = Mathf.Clamp01((-sunAltitude + 5f) / 15f) * 0.3f;
            }

            // Ambient
            if (ambientColorOverDay != null)
                RenderSettings.ambientLight = ambientColorOverDay.Evaluate(t01);

            RenderSettings.ambientIntensity = ambientIntensityCurve.Evaluate(t01);
        }

        private void UpdateSkyColor()
        {
            if (skyboxMaterial == null) return;

            Color skyColor;

            if (timeOfDay >= 10f && timeOfDay <= 15f)
            {
                // Midday — pure day color
                skyColor = daySkyColor;
            }
            else if (IsGoldenHour)
            {
                // Golden hour — lerp between day/sunset
                float blendSunset = (timeOfDay >= 17f)
                    ? Mathf.InverseLerp(17f, 18.5f, timeOfDay)
                    : Mathf.InverseLerp(7.5f, 6f, timeOfDay);
                skyColor = Color.Lerp(daySkyColor, sunsetSkyColor, blendSunset);
            }
            else if (timeOfDay >= 18.5f && timeOfDay < 20f)
            {
                // Dusk
                skyColor = Color.Lerp(sunsetSkyColor, nightSkyColor, Mathf.InverseLerp(18.5f, 20f, timeOfDay));
            }
            else if (timeOfDay >= 5f && timeOfDay < 6f)
            {
                // Dawn
                skyColor = Color.Lerp(nightSkyColor, sunsetSkyColor, Mathf.InverseLerp(5f, 6f, timeOfDay));
            }
            else
            {
                // Night
                skyColor = nightSkyColor;
            }

            if (skyboxMaterial.HasProperty("_Tint"))
                skyboxMaterial.SetColor("_Tint", skyColor);
            else if (skyboxMaterial.HasProperty("_Color"))
                skyboxMaterial.SetColor("_Color", skyColor);
        }

        private void UpdateStars()
        {
            if (starParticles == null) return;

            float sunAltitude = 90f - Mathf.Abs(timeOfDay - 12f) * 15f;

            if (sunAltitude < 10f)
            {
                if (!starParticles.isPlaying)
                    starParticles.Play();

                // Full intensity below -10°, fading between -10° and 10°
                float alpha          = Mathf.InverseLerp(10f, -10f, sunAltitude);
                var   main           = starParticles.main;
                Color c              = main.startColor.color;
                c.a                  = alpha;
                main.startColor      = c;
            }
            else
            {
                if (starParticles.isPlaying)
                    starParticles.Stop();
            }
        }
    }
}
