using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Analytics
{
    /// <summary>
    /// Automatically captures flight-specific telemetry events and forwards them
    /// to <see cref="TelemetryDispatcher"/>.
    /// Attach to a persistent GameObject in the world scene.
    /// Does NOT modify any existing flight logic.
    /// </summary>
    public class FlightTelemetryCollector : MonoBehaviour
    {
        // ── Inspector refs (auto-resolved if null) ───────────────────────────────
        [Header("Flight Refs")]
        [SerializeField] private Flight.FlightController   flightController;
        [SerializeField] private Flight.AltitudeController altitudeController;

        [Header("Weather Ref")]
        [SerializeField] private Weather.WeatherStateManager weatherStateManager;

        [Header("Sampling Intervals")]
        [SerializeField] private float altitudeSampleInterval = 10f;  // seconds
        [SerializeField] private float heatmapSampleInterval  = 30f;  // seconds

        // ── Milestone altitudes (metres) ─────────────────────────────────────────
        private static readonly float[] MilestoneMeters = { 1000f, 5000f, 10000f, 50000f, 100000f, 120000f };

        // ── State ────────────────────────────────────────────────────────────────
        private bool  _flightActive;
        private float _flightStartTime;
        private float _maxAltitude;
        private float _maxSpeed;
        private float _distanceTraveled;
        private Vector3 _lastPosition;
        private bool    _lastPositionSet;

        private float _sampleTimer;
        private float _heatmapTimer;

        private readonly bool[] _milestonePassed = new bool[6];
        private float _personalBestSpeed; // persisted

        private const string PrefsBestSpeed = "SWEF_BestSpeed";

        // Heatmap buffer — pre-allocated, no per-frame allocation
        private readonly List<string> _heatmapPoints = new List<string>(128);

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            _personalBestSpeed = PlayerPrefs.GetFloat(PrefsBestSpeed, 0f);

            if (flightController == null)
                flightController = FindFirstObjectByType<Flight.FlightController>();
            if (altitudeController == null)
                altitudeController = FindFirstObjectByType<Flight.AltitudeController>();
            if (weatherStateManager == null)
                weatherStateManager = Weather.WeatherStateManager.Instance != null
                    ? Weather.WeatherStateManager.Instance
                    : FindFirstObjectByType<Weather.WeatherStateManager>();
        }

        private void Start()
        {
            StartFlight();
        }

        private void Update()
        {
            if (!_flightActive) return;

            float alt   = altitudeController != null ? altitudeController.CurrentAltitudeMeters : 0f;
            float speed = flightController    != null ? flightController.CurrentSpeedMps          : 0f;

            if (alt   > _maxAltitude) _maxAltitude = alt;
            if (speed > _maxSpeed)    _maxSpeed    = speed;

            // Distance accumulation
            if (flightController != null)
            {
                Vector3 pos = flightController.transform.position;
                if (_lastPositionSet)
                    _distanceTraveled += Vector3.Distance(pos, _lastPosition);
                _lastPosition     = pos;
                _lastPositionSet  = true;
            }

            // Altitude milestones
            for (int i = 0; i < MilestoneMeters.Length; i++)
            {
                if (!_milestonePassed[i] && alt >= MilestoneMeters[i])
                {
                    _milestonePassed[i] = true;
                    FireMilestoneEvent(MilestoneMeters[i], alt, speed);
                }
            }

            // Speed record
            if (speed > _personalBestSpeed)
            {
                _personalBestSpeed = speed;
                PlayerPrefs.SetFloat(PrefsBestSpeed, _personalBestSpeed);
                FireSpeedRecord(speed);
            }

            // Periodic altitude sample
            _sampleTimer += Time.unscaledDeltaTime;
            if (_sampleTimer >= altitudeSampleInterval)
            {
                _sampleTimer = 0f;
                FireAltitudeSample(alt, speed);
            }

            // Heatmap sample
            _heatmapTimer += Time.unscaledDeltaTime;
            if (_heatmapTimer >= heatmapSampleInterval)
            {
                _heatmapTimer = 0f;
                RecordHeatmapPoint(alt);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _flightActive) EndFlight();
        }

        private void OnApplicationQuit()
        {
            if (_flightActive) EndFlight();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Manually trigger the flight_start event.</summary>
        public void StartFlight()
        {
            if (_flightActive) return;
            _flightActive    = true;
            _flightStartTime = Time.realtimeSinceStartup;
            _maxAltitude     = 0f;
            _maxSpeed        = 0f;
            _distanceTraveled = 0f;
            _lastPositionSet = false;
            Array.Clear(_milestonePassed, 0, _milestonePassed.Length);
            _heatmapPoints.Clear();

            var dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            var props = BuildFlightStartProps();
            var evt = TelemetryEventBuilder.Create(AnalyticsEvents.FlightStart)
                .WithCategory("flight")
                .WithProperties(props)
                .Build();
            dispatcher.EnqueueEvent(evt);
        }

        /// <summary>Manually trigger the flight_end event.</summary>
        public void EndFlight()
        {
            if (!_flightActive) return;
            _flightActive = false;

            var dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            float duration = Time.realtimeSinceStartup - _flightStartTime;
            var props = new Dictionary<string, object>
            {
                { "durationSeconds",  duration },
                { "maxAltitudeM",     _maxAltitude },
                { "maxSpeedMps",      _maxSpeed },
                { "distanceKm",       _distanceTraveled / 1000f },
                { "heatmapPoints",    _heatmapPoints.Count },
                { "weather",          GetWeatherName() },
            };

            var evt = TelemetryEventBuilder.Create(AnalyticsEvents.FlightEnd)
                .WithCategory("flight")
                .WithProperties(props)
                .Build();
            dispatcher.EnqueueEvent(evt);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private Dictionary<string, object> BuildFlightStartProps()
        {
            var props = new Dictionary<string, object>
            {
                { "deviceModel",  SystemInfo.deviceModel },
                { "osVersion",    SystemInfo.operatingSystem },
                { "weather",      GetWeatherName() },
                { "timeOfDayHour", DateTime.UtcNow.Hour },
            };
            return props;
        }

        private void FireAltitudeSample(float alt, float speed)
        {
            var dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            var evt = TelemetryEventBuilder.Create(AnalyticsEvents.AltitudeSample)
                .WithCategory("flight")
                .WithProperty("altitudeM", alt)
                .WithProperty("speedMps",  speed)
                .WithProperty("weather",   GetWeatherName())
                .WithProperty("throttle",  flightController != null ? (object)flightController.Throttle01 : 0f)
                .Build();
            dispatcher.EnqueueEvent(evt);
        }

        private void FireMilestoneEvent(float milestoneM, float currentAlt, float currentSpeed)
        {
            var dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            var evt = TelemetryEventBuilder.Create(AnalyticsEvents.AltitudeMilestone)
                .WithCategory("flight")
                .WithProperty("milestoneM",  milestoneM)
                .WithProperty("altitudeM",   currentAlt)
                .WithProperty("speedMps",    currentSpeed)
                .Build();
            dispatcher.EnqueueEvent(evt);
        }

        private void FireSpeedRecord(float speed)
        {
            var dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            var evt = TelemetryEventBuilder.Create(AnalyticsEvents.SpeedRecord)
                .WithCategory("flight")
                .WithProperty("speedMps", speed)
                .Build();
            dispatcher.EnqueueEvent(evt);
        }

        private void RecordHeatmapPoint(float alt)
        {
            if (flightController == null) return;
            Vector3 pos = flightController.transform.position;
            // Store as compact string to avoid allocations in hot path
            _heatmapPoints.Add($"{pos.x:F1},{pos.z:F1},{alt:F0}");
        }

        private string GetWeatherName()
        {
            if (weatherStateManager == null) return "unknown";
            return weatherStateManager.ActiveWeather?.condition.ToString() ?? "unknown";
        }
    }
}
