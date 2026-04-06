// FlightAnalyticsBridge.cs — Phase 116: Flight Analytics Dashboard
// Integration with existing SWEF systems: Flight, Journal, Achievement, CloudSave.
// Namespace: SWEF.FlightAnalytics

using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Bridges the Flight Analytics system to other SWEF modules.
    /// Uses compile-time guards to avoid hard dependencies on optional modules.
    /// </summary>
    public class FlightAnalyticsBridge : MonoBehaviour
    {
#if SWEF_ANALYTICS_AVAILABLE

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (FlightAnalyticsManager.Instance != null)
            {
                FlightAnalyticsManager.Instance.OnSessionEnded += OnSessionEnded;
            }
        }

        private void OnDisable()
        {
            if (FlightAnalyticsManager.Instance != null)
            {
                FlightAnalyticsManager.Instance.OnSessionEnded -= OnSessionEnded;
            }
        }

        // ── Handlers ──────────────────────────────────────────────────────────────

        private void OnSessionEnded(FlightSessionRecord session)
        {
            // Forward session summary to Journal system
#if SWEF_JOURNAL_AVAILABLE
            // SWEF.Journal.JournalManager.Instance?.RecordFlightSession(session);
#endif

            // Push performance score to Achievement system
#if SWEF_ACHIEVEMENT_AVAILABLE
            // SWEF.Achievement.AchievementManager.Instance?.NotifyFlightComplete(session);
#endif

            // Queue cloud save
#if SWEF_CLOUD_SAVE_AVAILABLE
            // SWEF.CloudSave.CloudSaveManager.Instance?.QueueSave("analytics", session);
#endif
        }

#endif // SWEF_ANALYTICS_AVAILABLE
    }
}
