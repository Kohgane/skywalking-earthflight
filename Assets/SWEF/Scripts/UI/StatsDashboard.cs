using UnityEngine;
using UnityEngine.UI;
using SWEF.Flight;

namespace SWEF.UI
{
    /// <summary>
    /// Real-time statistics dashboard showing flight duration, max altitude reached,
    /// max speed reached, distance traveled, and achievements unlocked.
    /// </summary>
    public class StatsDashboard : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private FlightController flight;
        [SerializeField] private AltitudeController altitude;

        [Header("Texts")]
        [SerializeField] private Text flightTimeText;
        [SerializeField] private Text maxAltText;
        [SerializeField] private Text maxSpeedText;
        [SerializeField] private Text distanceTraveledText;
        [SerializeField] private Text achievementsText;

        [Header("Panel")]
        [SerializeField] private GameObject dashboardPanel;
        [SerializeField] private Button toggleButton;

        [Header("Phase 21 — Enriched Stats")]
        [SerializeField] private Text dailyStreakText;
        [SerializeField] private Text totalFlightsText;
        [SerializeField] private Text avgFlightDurationText;

        // ── Tracking state ────────────────────────────────────────────────────────
        private float _flightTime;
        private float _maxAltitude;
        private float _maxSpeed;
        private float _totalDistance;
        private Vector3 _lastPosition;
        private bool _lastPositionSet;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (flight == null)
                flight = FindFirstObjectByType<FlightController>();
            if (altitude == null)
                altitude = FindFirstObjectByType<AltitudeController>();

            if (toggleButton != null)
                toggleButton.onClick.AddListener(TogglePanel);

            if (dashboardPanel != null)
                dashboardPanel.SetActive(false);
        }

        private void Update()
        {
            // Only accumulate stats when the game is running (not paused).
            // Prefer PauseManager when present; fall back to inspecting Time.deltaTime.
            bool paused = Core.PauseManager.Instance != null
                ? Core.PauseManager.Instance.IsPaused
                : Time.deltaTime <= 0f;
            if (paused) return;

            _flightTime += Time.deltaTime;

            float currentAlt   = altitude != null ? altitude.CurrentAltitudeMeters : 0f;
            float currentSpeed = flight   != null ? flight.CurrentSpeedMps         : 0f;

            if (currentAlt   > _maxAltitude) _maxAltitude = currentAlt;
            if (currentSpeed > _maxSpeed)    _maxSpeed    = currentSpeed;

            // Phase 13 — feed live values into SessionTracker
            var tracker = Core.SessionTracker.Instance;
            if (tracker != null)
            {
                tracker.UpdateAltitude(currentAlt);
                tracker.UpdateSpeed(currentSpeed);
            }

            if (flight != null)
            {
                Vector3 pos = flight.transform.position;
                if (_lastPositionSet)
                {
                    float delta = Vector3.Distance(pos, _lastPosition);
                    _totalDistance += delta;
                    tracker?.AddDistance(delta);
                }
                _lastPosition    = pos;
                _lastPositionSet = true;
            }

            RefreshDisplay();
        }

        // ── Internal ─────────────────────────────────────────────────────────────
        private void TogglePanel()
        {
            if (dashboardPanel != null)
                dashboardPanel.SetActive(!dashboardPanel.activeSelf);
        }

        private void RefreshDisplay()
        {
            if (flightTimeText != null)
            {
                int totalSec = Mathf.FloorToInt(_flightTime);
                int minutes  = totalSec / 60;
                int seconds  = totalSec % 60;
                flightTimeText.text = $"{minutes:00}:{seconds:00}";
            }

            if (maxAltText != null)
                maxAltText.text = FormatAltitude(_maxAltitude);

            if (maxSpeedText != null)
                maxSpeedText.text = FormatSpeed(_maxSpeed);

            if (distanceTraveledText != null)
                distanceTraveledText.text = $"{_totalDistance / 1000f:0.0} km";

            if (achievementsText != null)
            {
                var achMgr = Achievement.AchievementManager.Instance;
                if (achMgr != null)
                    achievementsText.text = $"{achMgr.UnlockedCount}/{achMgr.TotalCount} 🏆";
                else
                    achievementsText.text = "—";
            }

            // Phase 21 — enriched lifetime stats from AnalyticsDashboardData
            var dash = Analytics.AnalyticsDashboardData.Instance;
            if (dash != null)
            {
                if (dailyStreakText != null)
                    dailyStreakText.text = $"{dash.DailyActiveStreak} day streak 🔥";
                if (totalFlightsText != null)
                    totalFlightsText.text = $"{dash.TotalFlights} total flights";
                if (avgFlightDurationText != null)
                    avgFlightDurationText.text = $"Avg: {Mathf.FloorToInt(dash.AverageFlightDuration / 60f)}m {(int)(dash.AverageFlightDuration % 60f)}s";
            }
        }

        private static string FormatAltitude(float meters)
        {
            if (meters >= 1000f)
                return $"{meters / 1000f:0.0} km";
            return $"{meters:0} m";
        }

        private static string FormatSpeed(float mps)
        {
            float kmh  = mps * 3.6f;
            float mach = mps / 343f;
            if (mps >= 343f)
                return $"{kmh:0} km/h (Mach {mach:0.0})";
            return $"{kmh:0} km/h";
        }
    }
}
