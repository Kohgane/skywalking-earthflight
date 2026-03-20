using UnityEngine;
using SWEF.Flight;

namespace SWEF.Achievement
{
    /// <summary>
    /// MonoBehaviour that automatically tracks flight metrics each frame and
    /// reports progress to <see cref="AchievementManager"/>.
    /// Attach to a persistent GameObject in the World scene.
    /// </summary>
    public class AchievementTracker : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────────
        /// <summary>Minimum speed (m/s) required to count the player as actively flying.</summary>
        private const float MinimumFlightSpeedThreshold = 0.5f;

        // ── Inspector refs ────────────────────────────────────────────────────────
        [Header("References (auto-wired if null)")]
        [SerializeField] private FlightController flightController;
        [SerializeField] private AltitudeController altitudeController;
        [SerializeField] private AchievementManager achievementManager;

        // ── State ─────────────────────────────────────────────────────────────────
        private float _totalFlightSeconds;
        private float _totalDistanceMeters;
        private float _maxAltitudeMeters;
        private float _maxSpeedMps;
        private int   _uniqueLocationsVisited;
        private int   _totalScreenshots;
        private int   _totalFavorites;
        private int   _sessionCount;

        private Vector3 _lastPosition;
        private bool    _firstFrameDone;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (flightController   == null) flightController   = FindFirstObjectByType<FlightController>();
            if (altitudeController == null) altitudeController = FindFirstObjectByType<AltitudeController>();
            if (achievementManager == null) achievementManager = AchievementManager.Instance
                                                                  ?? FindFirstObjectByType<AchievementManager>();

            // Count this launch as a session.
            _sessionCount = PlayerPrefs.GetInt("SWEF_ACH_SessionCount", 0) + 1;
            PlayerPrefs.SetInt("SWEF_ACH_SessionCount", _sessionCount);
            PlayerPrefs.Save();

            _totalFlightSeconds  = PlayerPrefs.GetFloat("SWEF_ACH_FlightSeconds", 0f);
            _totalDistanceMeters = PlayerPrefs.GetFloat("SWEF_ACH_DistanceMeters", 0f);
            _maxAltitudeMeters   = PlayerPrefs.GetFloat("SWEF_ACH_MaxAltitude", 0f);
            _maxSpeedMps         = PlayerPrefs.GetFloat("SWEF_ACH_MaxSpeed", 0f);
            _uniqueLocationsVisited = PlayerPrefs.GetInt("SWEF_ACH_UniqueLocations", 0);
            _totalScreenshots    = PlayerPrefs.GetInt("SWEF_ACH_Screenshots", 0);
            _totalFavorites      = PlayerPrefs.GetInt("SWEF_ACH_Favorites", 0);
        }

        private void Update()
        {
            if (achievementManager == null) return;

            float dt    = Time.deltaTime;
            float alt   = altitudeController?.CurrentAltitudeMeters ?? 0f;
            float speed = flightController?.CurrentSpeedMps          ?? 0f;

            // ── Flight time ───────────────────────────────────────────────────────
            if (speed > MinimumFlightSpeedThreshold)
            {
                _totalFlightSeconds += dt;
                achievementManager.SetProgress("flight_time_1h",   _totalFlightSeconds);
                achievementManager.SetProgress("flight_time_10h",  _totalFlightSeconds);
                achievementManager.SetProgress("flight_time_100h", _totalFlightSeconds);
            }

            // ── Altitude records ──────────────────────────────────────────────────
            if (alt > _maxAltitudeMeters)
            {
                _maxAltitudeMeters = alt;
                achievementManager.SetProgress("altitude_10km",    _maxAltitudeMeters);
                achievementManager.SetProgress("altitude_50km",    _maxAltitudeMeters);
                achievementManager.SetProgress("altitude_100km",   _maxAltitudeMeters);
                achievementManager.SetProgress("altitude_karman",  _maxAltitudeMeters);
            }

            // ── Speed records ─────────────────────────────────────────────────────
            if (speed > _maxSpeedMps)
            {
                _maxSpeedMps = speed;
                achievementManager.SetProgress("speed_100",   _maxSpeedMps);
                achievementManager.SetProgress("speed_500",   _maxSpeedMps);
                achievementManager.SetProgress("speed_mach1", _maxSpeedMps);
                achievementManager.SetProgress("speed_mach5", _maxSpeedMps);
            }

            // ── Distance ──────────────────────────────────────────────────────────
            if (!_firstFrameDone)
            {
                _firstFrameDone = true;
                _lastPosition   = transform.position;
            }
            else if (speed > MinimumFlightSpeedThreshold)
            {
                float delta = Vector3.Distance(transform.position, _lastPosition);
                _totalDistanceMeters += delta;
                _lastPosition         = transform.position;

                achievementManager.SetProgress("distance_100km",               _totalDistanceMeters);
                achievementManager.SetProgress("distance_1000km",              _totalDistanceMeters);
                achievementManager.SetProgress("distance_earth_circumference", _totalDistanceMeters);
            }

            // ── Session count ─────────────────────────────────────────────────────
            achievementManager.SetProgress("sessions_10",  _sessionCount);
            achievementManager.SetProgress("sessions_50",  _sessionCount);
            achievementManager.SetProgress("sessions_100", _sessionCount);
        }

        private void OnApplicationQuit()
        {
            PersistCounters();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) PersistCounters();
        }

        private void PersistCounters()
        {
            PlayerPrefs.SetFloat("SWEF_ACH_FlightSeconds",  _totalFlightSeconds);
            PlayerPrefs.SetFloat("SWEF_ACH_DistanceMeters", _totalDistanceMeters);
            PlayerPrefs.SetFloat("SWEF_ACH_MaxAltitude",    _maxAltitudeMeters);
            PlayerPrefs.SetFloat("SWEF_ACH_MaxSpeed",       _maxSpeedMps);
            PlayerPrefs.Save();
        }

        // ── Public notifiers ──────────────────────────────────────────────────────

        /// <summary>Call when the player teleports to a new location.</summary>
        public void NotifyTeleport()
        {
            _uniqueLocationsVisited++;
            PlayerPrefs.SetInt("SWEF_ACH_UniqueLocations", _uniqueLocationsVisited);
            PlayerPrefs.Save();

            achievementManager?.SetProgress("explore_5",   _uniqueLocationsVisited);
            achievementManager?.SetProgress("explore_25",  _uniqueLocationsVisited);
            achievementManager?.SetProgress("explore_100", _uniqueLocationsVisited);
        }

        /// <summary>Call when the player takes a screenshot.</summary>
        public void NotifyScreenshot()
        {
            _totalScreenshots++;
            PlayerPrefs.SetInt("SWEF_ACH_Screenshots", _totalScreenshots);
            PlayerPrefs.Save();

            achievementManager?.SetProgress("screenshots_10", _totalScreenshots);
            achievementManager?.SetProgress("screenshots_50", _totalScreenshots);
        }

        /// <summary>Call when the player saves a favorite location.</summary>
        public void NotifyFavoriteSaved()
        {
            _totalFavorites++;
            PlayerPrefs.SetInt("SWEF_ACH_Favorites", _totalFavorites);
            PlayerPrefs.Save();

            achievementManager?.SetProgress("favorites_5",  _totalFavorites);
            achievementManager?.SetProgress("favorites_25", _totalFavorites);
        }
    }
}
