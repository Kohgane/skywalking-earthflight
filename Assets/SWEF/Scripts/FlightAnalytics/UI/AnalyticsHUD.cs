// AnalyticsHUD.cs — Phase 116: Flight Analytics Dashboard
// In-flight compact analytics HUD: current session stats, personal record indicators.
// Namespace: SWEF.FlightAnalytics

using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Compact in-flight HUD overlay showing live session statistics:
    /// elapsed time, distance flown, current altitude and speed, and a real-time
    /// efficiency meter compared against personal best.
    /// </summary>
    public class AnalyticsHUD : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────

        [Header("Display Settings")]
        [SerializeField] private bool showEfficiencyMeter = true;
        [SerializeField] private bool showPersonalRecordIndicator = true;

        // ── State ─────────────────────────────────────────────────────────────────

        private float _sessionStartTime;
        private bool  _isActive;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Activate the HUD when a session starts.</summary>
        public void Activate()
        {
            _sessionStartTime = Time.time;
            _isActive = true;
            gameObject.SetActive(true);
        }

        /// <summary>Deactivate the HUD when the session ends.</summary>
        public void Deactivate()
        {
            _isActive = false;
            gameObject.SetActive(false);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Update()
        {
            if (!_isActive) return;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            float elapsedSeconds = Time.time - _sessionStartTime;
            int   mins           = Mathf.FloorToInt(elapsedSeconds / 60f);
            int   secs           = Mathf.FloorToInt(elapsedSeconds % 60f);

            var session = FlightAnalyticsManager.Instance?.CurrentSession;
            float distNm = session != null ? session.distanceNm : 0f;

            // Concrete label wiring (TMP components) goes here.
            // Keeping as a stub to avoid hard TMP dependency at compile time.
            _ = $"{mins:D2}:{secs:D2}  |  {distNm:F1} nm";
        }
    }
}
