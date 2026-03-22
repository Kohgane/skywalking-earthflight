using System.Collections.Generic;
using UnityEngine;

namespace SWEF.WeatherChallenge
{
    /// <summary>
    /// Phase 53 — Bridges weather challenge lifecycle events to the SWEF analytics pipeline
    /// (<c>SWEF.Analytics.UserBehaviorTracker</c>).
    /// Subscribes to <see cref="WeatherChallengeManager"/> events in <see cref="OnEnable"/>
    /// and unsubscribes in <see cref="OnDisable"/> to avoid memory leaks.
    /// All events follow the <c>TrackFeatureDiscovery / TrackButtonClick</c> pattern used
    /// across the rest of the project.
    /// </summary>
    public class WeatherChallengeAnalyticsBridge : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static WeatherChallengeAnalyticsBridge Instance { get; private set; }

        #endregion

        #region Event Name Constants

        private const string EventChallengeGenerated  = "challenge_generated";
        private const string EventChallengeStarted    = "challenge_started";
        private const string EventChallengeCompleted  = "challenge_completed";
        private const string EventChallengeFailed     = "challenge_failed";
        private const string EventWaypointReached     = "waypoint_reached";

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            if (WeatherChallengeManager.Instance == null) return;
            WeatherChallengeManager.Instance.OnChallengeGenerated  += HandleChallengeGenerated;
            WeatherChallengeManager.Instance.OnChallengeStarted    += HandleChallengeStarted;
            WeatherChallengeManager.Instance.OnChallengeCompleted  += HandleChallengeCompleted;
            WeatherChallengeManager.Instance.OnChallengeFailed     += HandleChallengeFailed;
            WeatherChallengeManager.Instance.OnWaypointReached     += HandleWaypointReached;
        }

        private void OnDisable()
        {
            if (WeatherChallengeManager.Instance == null) return;
            WeatherChallengeManager.Instance.OnChallengeGenerated  -= HandleChallengeGenerated;
            WeatherChallengeManager.Instance.OnChallengeStarted    -= HandleChallengeStarted;
            WeatherChallengeManager.Instance.OnChallengeCompleted  -= HandleChallengeCompleted;
            WeatherChallengeManager.Instance.OnChallengeFailed     -= HandleChallengeFailed;
            WeatherChallengeManager.Instance.OnWaypointReached     -= HandleWaypointReached;
        }

        #endregion

        #region Public Tracking Methods

        /// <summary>
        /// Records the start of a weather challenge with contextual metadata.
        /// </summary>
        /// <param name="c">The challenge that was started.</param>
        public void RecordChallengeStart(WeatherChallenge c)
        {
            if (c == null) return;
            LogEvent(EventChallengeStarted, new Dictionary<string, object>
            {
                { "challenge_id",    c.challengeId },
                { "weather_type",   c.weatherType.ToString() },
                { "difficulty",     c.difficulty.ToString() },
                { "waypoint_count", c.waypoints.Count },
                { "time_limit",     c.timeLimit }
            });
        }

        /// <summary>
        /// Records the end of a weather challenge (completion or failure).
        /// </summary>
        /// <param name="c">The challenge that ended.</param>
        /// <param name="success"><c>true</c> if the challenge was completed; <c>false</c> if failed.</param>
        public void RecordChallengeEnd(WeatherChallenge c, bool success)
        {
            if (c == null) return;
            string eventName = success ? EventChallengeCompleted : EventChallengeFailed;
            LogEvent(eventName, new Dictionary<string, object>
            {
                { "challenge_id",     c.challengeId },
                { "weather_type",    c.weatherType.ToString() },
                { "difficulty",      c.difficulty.ToString() },
                { "score",           c.currentScore },
                { "max_score",       c.maxScore },
                { "elapsed_time",    c.elapsedTime },
                { "completion_pct",  c.CompletionPercentage() },
                { "bonus_completed", c.bonusCompleted }
            });
        }

        /// <summary>
        /// Records an individual waypoint being reached within the active challenge.
        /// </summary>
        /// <param name="c">The active challenge.</param>
        /// <param name="wp">The waypoint that was reached.</param>
        /// <param name="elapsed">Elapsed time at the moment the waypoint was reached, in seconds.</param>
        public void RecordWaypointReached(WeatherChallenge c, RouteWaypoint wp, float elapsed)
        {
            if (c == null || wp == null) return;
            LogEvent(EventWaypointReached, new Dictionary<string, object>
            {
                { "challenge_id",  c.challengeId },
                { "waypoint_id",   wp.waypointId },
                { "waypoint_name", wp.waypointName },
                { "is_optional",   wp.isOptional },
                { "elapsed_time",  elapsed }
            });
        }

        #endregion

        #region Private Event Handlers

        private void HandleChallengeGenerated(WeatherChallenge c)
        {
            if (c == null) return;
            LogEvent(EventChallengeGenerated, new Dictionary<string, object>
            {
                { "challenge_id",    c.challengeId },
                { "weather_type",   c.weatherType.ToString() },
                { "difficulty",     c.difficulty.ToString() },
                { "waypoint_count", c.waypoints.Count }
            });
        }

        private void HandleChallengeStarted(WeatherChallenge c)   => RecordChallengeStart(c);
        private void HandleChallengeCompleted(WeatherChallenge c) => RecordChallengeEnd(c, true);
        private void HandleChallengeFailed(WeatherChallenge c)    => RecordChallengeEnd(c, false);

        private void HandleWaypointReached(RouteWaypoint wp)
        {
            WeatherChallenge active = WeatherChallengeManager.Instance?.activeChallenge;
            // activeChallenge may already be null if challenge just completed — use cached elapsed
            float elapsed = active?.elapsedTime ?? 0f;
            if (active != null) RecordWaypointReached(active, wp, elapsed);
        }

        #endregion

        #region Logging

        private static void LogEvent(string eventName, Dictionary<string, object> parameters)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.UserBehaviorTracker.Instance?.TrackFeatureDiscovery(eventName, parameters);
#else
            if (Debug.isDebugBuild)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append($"[WeatherChallengeAnalytics] {eventName}: ");
                foreach (var kv in parameters)
                    sb.Append($"{kv.Key}={kv.Value} ");
                Debug.Log(sb.ToString());
            }
#endif
        }

        #endregion
    }
}
