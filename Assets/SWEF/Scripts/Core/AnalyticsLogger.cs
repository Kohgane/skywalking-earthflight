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
        private const string KEY_SESSION_COUNT   = "SWEF_SessionCount";
        private const string KEY_FLIGHT_TIME     = "SWEF_TotalFlightTime";
        private const string KEY_MAX_ALTITUDE    = "SWEF_MaxAltitude";
        private const string KEY_TELEPORT_COUNT  = "SWEF_TeleportCount";
        private const string KEY_SCREENSHOT_COUNT = "SWEF_ScreenshotCount";

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

        private float _sessionFlightTime;
        private float _sessionMaxAltitude;

        private void Awake()
        {
            // Load persisted stats
            SessionCount      = PlayerPrefs.GetInt(KEY_SESSION_COUNT, 0) + 1;
            TotalFlightTimeSec = PlayerPrefs.GetFloat(KEY_FLIGHT_TIME, 0f);
            MaxAltitudeMeters  = PlayerPrefs.GetFloat(KEY_MAX_ALTITUDE, 0f);
            TeleportCount      = PlayerPrefs.GetInt(KEY_TELEPORT_COUNT, 0);
            ScreenshotCount    = PlayerPrefs.GetInt(KEY_SCREENSHOT_COUNT, 0);

            // Persist incremented session count immediately
            PlayerPrefs.SetInt(KEY_SESSION_COUNT, SessionCount);
            PlayerPrefs.Save();
        }

        private void Start()
        {
            // Run after all Awake() calls so FindFirstObjectByType is reliable
            if (altitudeSource == null)
                altitudeSource = FindFirstObjectByType<AltitudeController>();
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
        /// Resets all analytics keys in PlayerPrefs and clears in-memory values.
        /// </summary>
        public void ResetAll()
        {
            PlayerPrefs.DeleteKey(KEY_SESSION_COUNT);
            PlayerPrefs.DeleteKey(KEY_FLIGHT_TIME);
            PlayerPrefs.DeleteKey(KEY_MAX_ALTITUDE);
            PlayerPrefs.DeleteKey(KEY_TELEPORT_COUNT);
            PlayerPrefs.DeleteKey(KEY_SCREENSHOT_COUNT);
            PlayerPrefs.Save();

            SessionCount        = 0;
            TotalFlightTimeSec  = 0f;
            MaxAltitudeMeters   = 0f;
            TeleportCount       = 0;
            ScreenshotCount     = 0;
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
    }
}
