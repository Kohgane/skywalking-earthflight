// AdvancedPhotographyAnalytics.cs — SWEF Advanced Photography & Drone Camera System (Phase 89)

#if SWEF_ANALYTICS_AVAILABLE
using SWEF.Analytics;
#endif

namespace SWEF.AdvancedPhotography
{
    /// <summary>
    /// Phase 89 — Static utility class that dispatches Advanced Photography telemetry
    /// events to <c>SWEF.Analytics.TelemetryDispatcher</c>.
    ///
    /// <para>All methods are no-ops when <c>SWEF_ANALYTICS_AVAILABLE</c> is not defined,
    /// so they are always safe to call.</para>
    /// </summary>
    public static class AdvancedPhotographyAnalytics
    {
        // ── Drone ─────────────────────────────────────────────────────────────────

        /// <summary>Records that a drone flight session was started in the given mode.</summary>
        public static void RecordDroneFlightStarted(DroneFlightMode mode)
        {
#if SWEF_ANALYTICS_AVAILABLE
            TelemetryDispatcher.Instance?.Track("drone_flight_started",
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "mode", mode.ToString() }
                });
#endif
        }

        /// <summary>Records that a drone flight session has ended.</summary>
        public static void RecordDroneFlightEnded(float durationSeconds)
        {
#if SWEF_ANALYTICS_AVAILABLE
            TelemetryDispatcher.Instance?.Track("drone_flight_ended",
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "duration", durationSeconds }
                });
#endif
        }

        /// <summary>Records that the drone battery was fully depleted mid-flight.</summary>
        public static void RecordDroneBatteryDepleted()
        {
#if SWEF_ANALYTICS_AVAILABLE
            TelemetryDispatcher.Instance?.Track("drone_battery_depleted",
                new System.Collections.Generic.Dictionary<string, object>());
#endif
        }

        // ── AI Composition ────────────────────────────────────────────────────────

        /// <summary>Records that the AI composition assistant was used.</summary>
        public static void RecordAICompositionUsed()
        {
#if SWEF_ANALYTICS_AVAILABLE
            TelemetryDispatcher.Instance?.Track("ai_composition_used",
                new System.Collections.Generic.Dictionary<string, object>());
#endif
        }

        /// <summary>Records that the auto-frame feature was invoked.</summary>
        public static void RecordAutoFrameUsed()
        {
#if SWEF_ANALYTICS_AVAILABLE
            TelemetryDispatcher.Instance?.Track("auto_frame_used",
                new System.Collections.Generic.Dictionary<string, object>());
#endif
        }

        // ── Panorama ──────────────────────────────────────────────────────────────

        /// <summary>Records a completed panorama capture of the given type.</summary>
        public static void RecordPanoramaCaptured(PanoramaType type)
        {
#if SWEF_ANALYTICS_AVAILABLE
            TelemetryDispatcher.Instance?.Track("panorama_captured",
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "type", type.ToString() }
                });
#endif
        }

        // ── Timelapse ─────────────────────────────────────────────────────────────

        /// <summary>Records a completed timelapse session.</summary>
        public static void RecordTimelapseCompleted(int frameCount)
        {
#if SWEF_ANALYTICS_AVAILABLE
            TelemetryDispatcher.Instance?.Track("timelapse_completed",
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "frame_count", frameCount }
                });
#endif
        }

        // ── Contest ───────────────────────────────────────────────────────────────

        /// <summary>Records that a photo was submitted to a contest.</summary>
        public static void RecordPhotoSubmittedToContest(string contestId)
        {
#if SWEF_ANALYTICS_AVAILABLE
            TelemetryDispatcher.Instance?.Track("photo_submitted_to_contest",
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "contest_id", contestId }
                });
#endif
        }

        // ── Photo Spot ────────────────────────────────────────────────────────────

        /// <summary>Records that a new photo spot was discovered.</summary>
        public static void RecordPhotoSpotDiscovered(string spotId)
        {
#if SWEF_ANALYTICS_AVAILABLE
            TelemetryDispatcher.Instance?.Track("photo_spot_discovered",
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "spot_id", spotId }
                });
#endif
        }

        // ── Drone Path ────────────────────────────────────────────────────────────

        /// <summary>Records that a drone waypoint path was created or modified.</summary>
        public static void RecordDronePathCreated()
        {
#if SWEF_ANALYTICS_AVAILABLE
            TelemetryDispatcher.Instance?.Track("drone_path_created",
                new System.Collections.Generic.Dictionary<string, object>());
#endif
        }

        // ── Challenge ─────────────────────────────────────────────────────────────

        /// <summary>Records that a photo challenge was completed.</summary>
        public static void RecordChallengeCompleted(string challengeId)
        {
#if SWEF_ANALYTICS_AVAILABLE
            TelemetryDispatcher.Instance?.Track("challenge_completed",
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "challenge_id", challengeId }
                });
#endif
        }
    }
}
