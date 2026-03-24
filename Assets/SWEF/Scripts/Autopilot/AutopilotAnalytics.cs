// Phase 72 — Autopilot & Cruise Control System
// Assets/SWEF/Scripts/Autopilot/AutopilotAnalytics.cs
using UnityEngine;
using SWEF.Analytics;

namespace SWEF.Autopilot
{
    /// <summary>
    /// Analytics bridge for the Autopilot system.
    /// Subscribes to <see cref="AutopilotController"/> and <see cref="CruiseControlManager"/>
    /// events and forwards them to <see cref="TelemetryDispatcher"/> and
    /// <see cref="UserBehaviorTracker"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class AutopilotAnalytics : MonoBehaviour
    {
        #region Singleton
        /// <summary>Shared analytics instance.</summary>
        public static AutopilotAnalytics Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _modeUsageCounts = new int[ModeCount];
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Session Counters
        private static readonly int ModeCount = System.Enum.GetValues(typeof(AutopilotMode)).Length;

        private float _sessionApTime;
        private int   _approachesCompleted;
        private AutopilotMode _mostUsedMode = AutopilotMode.Off;
        private int[] _modeUsageCounts;
        #endregion

        #region Lifecycle
        private void OnEnable()
        {
            AutopilotController ap = AutopilotController.Instance;
            if (ap != null)
            {
                ap.OnModeChanged       += HandleModeChanged;
                ap.OnEngagementChanged += HandleEngagementChanged;
                ap.OnAutopilotWarning  += HandleWarning;
            }

            CruiseControlManager cc = CruiseControlManager.Instance;
            if (cc != null)
                cc.OnProfileChanged += HandleProfileChanged;
        }

        private void OnDisable()
        {
            AutopilotController ap = AutopilotController.Instance;
            if (ap != null)
            {
                ap.OnModeChanged       -= HandleModeChanged;
                ap.OnEngagementChanged -= HandleEngagementChanged;
                ap.OnAutopilotWarning  -= HandleWarning;
            }

            CruiseControlManager cc = CruiseControlManager.Instance;
            if (cc != null)
                cc.OnProfileChanged -= HandleProfileChanged;
        }

        private void Update()
        {
            if (AutopilotController.Instance != null && AutopilotController.Instance.IsEngaged)
                _sessionApTime += Time.deltaTime;
        }
        #endregion

        #region Event Handlers
        private void HandleModeChanged(AutopilotMode mode)
        {
            if (mode == AutopilotMode.Off) return;

            int idx = (int)mode;
            if (idx >= 0 && idx < _modeUsageCounts.Length)
            {
                _modeUsageCounts[idx]++;
                TrackMostUsedMode();
            }
        }

        private void HandleEngagementChanged(bool engaged)
        {
            if (engaged)
            {
                TrackEvent("autopilot_engaged",
                    ("mode",     AutopilotController.Instance?.CurrentMode.ToString()),
                    ("altitude", AutopilotController.Instance?.TargetAltitude),
                    ("speed",    AutopilotController.Instance?.TargetSpeed));

                UserBehaviorTracker.Instance?.TrackFeatureDiscovery("autopilot");
            }
            else
            {
                TrackEvent("autopilot_disengaged",
                    ("reason", "manual"));
            }
        }

        private void HandleWarning(string warningKey)
        {
            TrackEvent("autopilot_warning",
                ("warning_type", warningKey));
        }

        private void HandleProfileChanged(CruiseControlManager.CruiseProfile profile)
        {
            TrackEvent("cruise_profile_changed",
                ("profile", profile.ToString()));
        }
        #endregion

        #region Public Tracking API
        /// <summary>Track that an approach was started at the named airport.</summary>
        public void TrackApproachStarted(string airportName)
        {
            TrackEvent("approach_started",
                ("airport", airportName ?? "unknown"));
        }

        /// <summary>Track that an approach was completed (or aborted).</summary>
        public void TrackApproachCompleted(string airportName, bool success)
        {
            if (success) _approachesCompleted++;

            TrackEvent("approach_completed",
                ("airport", airportName ?? "unknown"),
                ("success", success));
        }

        /// <summary>Track that the player manually overrode an autopilot axis.</summary>
        public void TrackOverride(string axis)
        {
            TrackEvent("autopilot_override",
                ("axis", axis));
        }

        /// <summary>Track a cruise profile change (called by <see cref="CruiseControlManager"/>).</summary>
        public void TrackCruiseProfileChanged(string profileName)
        {
            TrackEvent("cruise_profile_changed",
                ("profile", profileName));
        }

        /// <summary>
        /// Log a session summary to the console (extend to send via analytics in production).
        /// </summary>
        public void LogSessionSummary()
        {
            Debug.Log($"[AutopilotAnalytics] Session: AP time={_sessionApTime:F1}s, " +
                      $"most-used={_mostUsedMode}, approaches={_approachesCompleted}");
        }
        #endregion

        #region Private Helpers
        private static void TrackEvent(string eventName, params (string key, object value)[] properties)
        {
            TelemetryDispatcher dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            var builder = TelemetryEventBuilder.Create(eventName).WithCategory("autopilot");
            foreach (var (key, value) in properties)
                builder.WithProperty(key, value);

            dispatcher.EnqueueEvent(builder.Build());
        }

        private void TrackMostUsedMode()
        {
            int max = 0;
            AutopilotMode best = AutopilotMode.Off;
            for (int i = 1; i < _modeUsageCounts.Length; i++)
            {
                if (_modeUsageCounts[i] > max)
                {
                    max  = _modeUsageCounts[i];
                    best = (AutopilotMode)i;
                }
            }
            _mostUsedMode = best;
        }
        #endregion
    }
}
