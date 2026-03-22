using System;
using System.Collections.Generic;
using UnityEngine;
using SWEF.Analytics;
using SWEF.Flight;
using SWEF.Screenshot;

namespace SWEF.TimeOfDay
{
    /// <summary>
    /// Tracks time-of-day related analytics events via the SWEF telemetry pipeline.
    /// <para>
    /// All events are gated behind <see cref="TelemetryDispatcher.telemetryEnabled"/> (internal)
    /// and only fired when the player was actually flying (IsFlying on <see cref="FlightController"/>).
    /// Aggregates per-session totals that are flushed on <c>OnDestroy</c>.
    /// </para>
    /// </summary>
    public class TimeOfDayAnalytics : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("References (auto-found if null)")]
        [SerializeField] private TimeOfDayManager  timeOfDayManager;
        [SerializeField] private TelemetryDispatcher telemetryDispatcher;
        [SerializeField] private FlightController  flightController;
        [SerializeField] private ScreenshotController screenshotController;

        // ── Session aggregates ────────────────────────────────────────────────────
        private float _nightFlightSeconds;
        private readonly Dictionary<Season, float> _seasonSeconds  = new Dictionary<Season, float>();
        private readonly Dictionary<int, float>    _hourSeconds    = new Dictionary<int, float>();
        private int   _timeScaleChanges;
        private bool  _sunriseWitnessed;
        private bool  _sunsetWitnessed;
        private bool  _auroraWitnessed;

        // ── State ─────────────────────────────────────────────────────────────────
        private DayPhase _lastPhase = DayPhase.Day;
        private bool     _goldenHourActive;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (timeOfDayManager   == null) timeOfDayManager   = FindFirstObjectByType<TimeOfDayManager>();
            if (telemetryDispatcher== null) telemetryDispatcher= FindFirstObjectByType<TelemetryDispatcher>();
            if (flightController   == null) flightController   = FindFirstObjectByType<FlightController>();
            if (screenshotController == null) screenshotController = FindFirstObjectByType<ScreenshotController>();
        }

        private void OnEnable()
        {
            if (timeOfDayManager == null) return;
            timeOfDayManager.OnDayPhaseChanged += OnPhaseChanged;
            timeOfDayManager.OnSunrise         += OnSunrise;
            timeOfDayManager.OnSunset          += OnSunset;
        }

        private void OnDisable()
        {
            if (timeOfDayManager == null) return;
            timeOfDayManager.OnDayPhaseChanged -= OnPhaseChanged;
            timeOfDayManager.OnSunrise         -= OnSunrise;
            timeOfDayManager.OnSunset          -= OnSunset;
        }

        private void Update()
        {
            if (timeOfDayManager == null || flightController == null) return;
            if (!IsFlying()) return;

            float dt = Time.deltaTime;

            // Night flight duration
            if (timeOfDayManager.CurrentDayPhase == DayPhase.Night ||
                timeOfDayManager.CurrentDayPhase == DayPhase.AstronomicalTwilight)
            {
                _nightFlightSeconds += dt;
            }

            // Season distribution
            Season s = timeOfDayManager.CurrentSeason;
            _seasonSeconds[s] = (_seasonSeconds.TryGetValue(s, out float v) ? v : 0f) + dt;

            // Hour distribution
            int hour = (int)timeOfDayManager.CurrentHour;
            _hourSeconds[hour] = (_hourSeconds.TryGetValue(hour, out float hv) ? hv : 0f) + dt;

            // Aurora check
            if (!_auroraWitnessed)
            {
                bool nearPole = Mathf.Abs(timeOfDayManager.Latitude) > 60f;
                bool isDark   = timeOfDayManager.GetSunMoonState()?.sunAltitudeDeg < -12f;
                if (nearPole && isDark)
                {
                    _auroraWitnessed = true;
                    Track("tod_aurora_witnessed", new Dictionary<string, object>
                    {
                        { "latitude", timeOfDayManager.Latitude }
                    });
                }
            }
        }

        private void OnDestroy()
        {
            FlushSessionData();
        }

        // ── Event handlers ────────────────────────────────────────────────────────

        private void OnPhaseChanged(DayPhase previous, DayPhase next)
        {
            // Golden hour screenshot opportunity
            bool wasGolden = previous == DayPhase.GoldenHour;
            bool isGolden  = next     == DayPhase.GoldenHour;

            if (isGolden && !wasGolden)
            {
                _goldenHourActive = true;
            }
            else if (wasGolden && !isGolden)
            {
                _goldenHourActive = false;
            }
        }

        private void OnSunrise()
        {
            if (!IsFlying() || _sunriseWitnessed) return;
            _sunriseWitnessed = true;
            Track("tod_sunrise_witnessed", new Dictionary<string, object>
            {
                { "hour",    timeOfDayManager?.CurrentHour },
                { "season",  timeOfDayManager?.CurrentSeason.ToString() }
            });
        }

        private void OnSunset()
        {
            if (!IsFlying() || _sunsetWitnessed) return;
            _sunsetWitnessed = true;
            Track("tod_sunset_witnessed", new Dictionary<string, object>
            {
                { "hour",    timeOfDayManager?.CurrentHour },
                { "season",  timeOfDayManager?.CurrentSeason.ToString() }
            });
        }

        /// <summary>
        /// Should be called externally (e.g. by <see cref="ScreenshotController"/>) when a
        /// screenshot is captured.
        /// </summary>
        public void OnScreenshotTaken()
        {
            if (_goldenHourActive)
            {
                Track("tod_golden_hour_screenshot", new Dictionary<string, object>
                {
                    { "hour",   timeOfDayManager?.CurrentHour },
                    { "season", timeOfDayManager?.CurrentSeason.ToString() }
                });
            }
        }

        /// <summary>Should be called when the user manually changes the time scale.</summary>
        public void OnTimeScaleChanged(float newScale)
        {
            _timeScaleChanges++;
            Track("tod_time_scale_usage", new Dictionary<string, object>
            {
                { "new_scale", newScale },
                { "change_count", _timeScaleChanges }
            });
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void FlushSessionData()
        {
            if (telemetryDispatcher == null) return;

            // Night flight duration
            if (_nightFlightSeconds > 1f)
            {
                Track("tod_night_flight_duration", new Dictionary<string, object>
                {
                    { "seconds", _nightFlightSeconds }
                });
            }

            // Season distribution
            foreach (var kv in _seasonSeconds)
            {
                Track("tod_season_distribution", new Dictionary<string, object>
                {
                    { "season",  kv.Key.ToString() },
                    { "seconds", kv.Value }
                });
            }

            // Favorite time of day (most-flown hour)
            int favoriteHour = -1;
            float maxTime    = 0f;
            foreach (var kv in _hourSeconds)
            {
                if (kv.Value > maxTime) { maxTime = kv.Value; favoriteHour = kv.Key; }
            }
            if (favoriteHour >= 0)
            {
                Track("tod_favorite_time", new Dictionary<string, object>
                {
                    { "hour", favoriteHour }
                });
            }
        }

        private void Track(string eventName, Dictionary<string, object> props = null)
        {
            if (telemetryDispatcher == null) return;
            var builder = new TelemetryEventBuilder(eventName).WithCategory("time_of_day");
            if (props != null)
            {
                foreach (var kv in props)
                    builder = builder.WithProperty(kv.Key, kv.Value);
            }
            telemetryDispatcher.EnqueueEvent(builder.Build());
        }

        private bool IsFlying() =>
            flightController != null && flightController.IsFlying;
    }
}
