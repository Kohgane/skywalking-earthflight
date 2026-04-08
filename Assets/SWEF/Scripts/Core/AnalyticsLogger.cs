using System.Collections.Generic;
using UnityEngine;
using SWEF.Flight;

namespace SWEF.Core
{
    /// <summary>
    /// Tracks local session analytics using PlayerPrefs.
    /// Persists: session count, total flight time (seconds), max altitude (meters),
    /// total teleport count, and total screenshot count.
    /// No external service is used — all data stays on device.
    /// </summary>
    public class AnalyticsLogger : MonoBehaviour
    {
        private const string KEY_SESSION_COUNT    = "SWEF_SessionCount";
        private const string KEY_FLIGHT_TIME      = "SWEF_TotalFlightTime";
        private const string KEY_MAX_ALTITUDE     = "SWEF_MaxAltitude";
        private const string KEY_TELEPORT_COUNT   = "SWEF_TeleportCount";
        private const string KEY_SCREENSHOT_COUNT = "SWEF_ScreenshotCount";
        // Phase 19 — Weather
        private const string KEY_WEATHER_EVENTS   = "SWEF_WeatherEventCount";

        /// <summary>Singleton instance; set during Awake.</summary>
        public static AnalyticsLogger Instance { get; private set; }

        [SerializeField] private AltitudeController altitudeSource;

        /// <summary>Total number of app sessions started on this device.</summary>
        public int SessionCount { get; private set; }

        /// <summary>Cumulative flight time in seconds across all sessions.</summary>
        public float TotalFlightTimeSec { get; private set; }

        /// <summary>Maximum altitude reached in meters across all sessions.</summary>
        public float MaxAltitudeMeters { get; private set; }

        /// <summary>Total number of teleports performed across all sessions.</summary>
        public int TeleportCount { get; private set; }

        /// <summary>Total number of screenshots taken across all sessions.</summary>
        public int ScreenshotCount { get; private set; }

        /// <summary>Total number of weather condition change events recorded across all sessions.</summary>
        public int WeatherEventCount { get; private set; }

        private float _sessionFlightTime;
        private float _sessionMaxAltitude;

        // Phase 21 — TelemetryDispatcher reference
        private SWEF.Analytics.TelemetryDispatcher _dispatcher;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Load persisted stats
            SessionCount      = PlayerPrefs.GetInt(KEY_SESSION_COUNT, 0) + 1;
            TotalFlightTimeSec = PlayerPrefs.GetFloat(KEY_FLIGHT_TIME, 0f);
            MaxAltitudeMeters  = PlayerPrefs.GetFloat(KEY_MAX_ALTITUDE, 0f);
            TeleportCount      = PlayerPrefs.GetInt(KEY_TELEPORT_COUNT, 0);
            ScreenshotCount    = PlayerPrefs.GetInt(KEY_SCREENSHOT_COUNT, 0);
            WeatherEventCount  = PlayerPrefs.GetInt(KEY_WEATHER_EVENTS, 0);

            // Persist incremented session count immediately
            PlayerPrefs.SetInt(KEY_SESSION_COUNT, SessionCount);
            PlayerPrefs.Save();
        }

        private void Start()
        {
            // Run after all Awake() calls so FindFirstObjectByType is reliable
            if (altitudeSource == null)
                altitudeSource = FindFirstObjectByType<AltitudeController>();

            InitializeTelemetryPipeline();

            // Phase 19 — subscribe to weather transitions
            if (SWEF.Weather.WeatherDataService.Instance != null)
                SWEF.Weather.WeatherDataService.Instance.OnWeatherTransitionStart += (_, to) =>
                    RecordWeatherCondition(to.condition);

            // Phase 20 — subscribe to multiplayer events
            var roomManager = SWEF.Multiplayer.RoomManager.Instance != null
                ? SWEF.Multiplayer.RoomManager.Instance
                : FindFirstObjectByType<SWEF.Multiplayer.RoomManager>();
            if (roomManager != null)
            {
                roomManager.OnRoomJoined  += info =>
                    RecordMultiplayerEvent("room_joined",  new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "roomId",  info.roomId  },
                        { "region",  info.region  },
                        { "players", info.playerCount.ToString() }
                    });
                roomManager.OnRoomCreated += info =>
                    RecordMultiplayerEvent("room_created", new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "roomId", info.roomId },
                        { "region", info.region }
                    });
            }

            var race = SWEF.Multiplayer.MultiplayerRace.Instance != null
                ? SWEF.Multiplayer.MultiplayerRace.Instance
                : FindFirstObjectByType<SWEF.Multiplayer.MultiplayerRace>();
            if (race != null)
            {
                race.OnRaceStateChanged += state =>
                {
                    if (state == SWEF.Multiplayer.RaceState.Racing)
                        RecordMultiplayerEvent("race_started", null);
                };
                race.OnRaceFinished += results =>
                    RecordMultiplayerEvent("race_finished",
                        new System.Collections.Generic.Dictionary<string, string>
                        {
                            { "players", results?.Count.ToString() ?? "0" },
                            { "winner",  results?.Count > 0 ? results[0].playerName : "none" }
                        });
            }
        }

        private void Update()
        {
            _sessionFlightTime += Time.deltaTime;

            if (altitudeSource != null)
            {
                float alt = altitudeSource.CurrentAltitudeMeters;
                if (alt > _sessionMaxAltitude)
                    _sessionMaxAltitude = alt;
            }
        }

        // ── Phase 21 — Telemetry Pipeline ────────────────────────────────────────

        /// <summary>Finds or creates the TelemetryDispatcher and wires it up.</summary>
        private void InitializeTelemetryPipeline()
        {
            _dispatcher = SWEF.Analytics.TelemetryDispatcher.Instance != null
                ? SWEF.Analytics.TelemetryDispatcher.Instance
                : FindFirstObjectByType<SWEF.Analytics.TelemetryDispatcher>();
        }

        /// <summary>
        /// Log a flight-specific telemetry event with additional properties.
        /// </summary>
        public void LogFlightEvent(string eventName, Dictionary<string, object> props)
        {
            LogEvent(eventName);
            if (_dispatcher == null) return;
            var evt = SWEF.Analytics.TelemetryEventBuilder.Create(eventName)
                .WithCategory("flight")
                .WithProperties(props)
                .Build();
            _dispatcher.EnqueueEvent(evt);
        }

        /// <summary>
        /// Log a purchase telemetry event. Critical path — flushed immediately.
        /// </summary>
        public void LogPurchaseEvent(string productId, bool success, float price)
        {
            string eventName = success
                ? SWEF.Analytics.AnalyticsEvents.IapCompleted
                : SWEF.Analytics.AnalyticsEvents.IapFailed;
            LogEvent(eventName, productId);
            if (_dispatcher == null) return;
            var evt = SWEF.Analytics.TelemetryEventBuilder.Create(eventName)
                .WithCategory("purchase")
                .WithProperty("productId", productId)
                .WithProperty("price",     price)
                .WithProperty("success",   success)
                .Build();
            _dispatcher.EnqueueCriticalEvent(evt);
        }

        /// <summary>Records a single teleport event and persists the updated count.</summary>
        public void RecordTeleport()
        {
            TeleportCount++;
            PlayerPrefs.SetInt(KEY_TELEPORT_COUNT, TeleportCount);
            PlayerPrefs.Save();
        }

        /// <summary>Records a single screenshot event and persists the updated count.</summary>
        public void RecordScreenshot()
        {
            ScreenshotCount++;
            PlayerPrefs.SetInt(KEY_SCREENSHOT_COUNT, ScreenshotCount);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Records a weather condition change event during a flight session.
        /// Also emits a <see cref="LogEvent"/> entry for external analytics hooks.
        /// </summary>
        /// <param name="condition">The new weather condition that was encountered.</param>
        public void RecordWeatherCondition(SWEF.Weather.WeatherCondition condition)
        {
            WeatherEventCount++;
            PlayerPrefs.SetInt(KEY_WEATHER_EVENTS, WeatherEventCount);
            PlayerPrefs.Save();
            LogEvent("weather_condition", condition.ToString());
        }

        /// <summary>
        /// Records a multiplayer event with optional key-value parameters.
        /// Logs to the console using the standard SWEF format.
        /// </summary>
        /// <param name="eventType">Event type identifier (e.g. "room_joined").</param>
        /// <param name="parameters">Optional string-to-string parameter map.</param>
        public void RecordMultiplayerEvent(string eventType,
            System.Collections.Generic.Dictionary<string, string> parameters)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"[SWEF] MP Event: {eventType}");

            if (parameters != null)
            {
                foreach (var kvp in parameters)
                    sb.Append($"  {kvp.Key}={kvp.Value}");
            }

            Debug.Log(sb.ToString());
            LogEvent($"mp_{eventType}");
        }


        /// <summary>
        /// Resets all analytics keys in PlayerPrefs and clears in-memory values.
        /// </summary>
        public void ResetAll()
        {
            PlayerPrefs.DeleteKey(KEY_SESSION_COUNT);
            PlayerPrefs.DeleteKey(KEY_FLIGHT_TIME);
            PlayerPrefs.DeleteKey(KEY_MAX_ALTITUDE);
            PlayerPrefs.DeleteKey(KEY_TELEPORT_COUNT);
            PlayerPrefs.DeleteKey(KEY_SCREENSHOT_COUNT);
            PlayerPrefs.DeleteKey(KEY_WEATHER_EVENTS);
            PlayerPrefs.Save();

            SessionCount        = 0;
            TotalFlightTimeSec  = 0f;
            MaxAltitudeMeters   = 0f;
            TeleportCount       = 0;
            ScreenshotCount     = 0;
            WeatherEventCount   = 0;
            _sessionFlightTime  = 0f;
            _sessionMaxAltitude = 0f;
        }

        private void SaveFlightStats()
        {
            float totalTime = TotalFlightTimeSec + _sessionFlightTime;
            float maxAlt    = Mathf.Max(MaxAltitudeMeters, _sessionMaxAltitude);

            TotalFlightTimeSec = totalTime;
            MaxAltitudeMeters  = maxAlt;

            PlayerPrefs.SetFloat(KEY_FLIGHT_TIME,  totalTime);
            PlayerPrefs.SetFloat(KEY_MAX_ALTITUDE, maxAlt);
            PlayerPrefs.Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveFlightStats();
        }

        private void OnApplicationQuit()
        {
            SaveFlightStats();
        }

        /// <summary>
        /// Records a named analytics event with an optional string value.
        /// Uses <see cref="Instance"/> when available; falls back to
        /// <see cref="Debug.Log"/> so callers never need a null-check.
        /// </summary>
        /// <param name="eventName">The event name (e.g. "iap_purchase").</param>
        /// <param name="value">Optional value associated with the event (e.g. product ID).</param>
        public static void LogEvent(string eventName, string value = "")
        {
            Debug.Log($"[SWEF] Analytics: {eventName}" + (string.IsNullOrEmpty(value) ? "" : $" — {value}"));
        }
    }
}
