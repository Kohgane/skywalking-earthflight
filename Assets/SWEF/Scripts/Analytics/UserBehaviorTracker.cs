using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Analytics
{
    /// <summary>
    /// Tracks user engagement patterns: screen views, button clicks, tutorial progress,
    /// feature discovery, and session summaries.
    /// </summary>
    public class UserBehaviorTracker : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static UserBehaviorTracker Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Button Tracking")]
        [Tooltip("Only buttons whose GameObject name appears in this list will be tracked automatically. Leave empty to track all button clicks manually via TrackButtonClick().")]
        [SerializeField] private string[] trackedButtonNames = new string[0];

        // Pre-built HashSet for O(1) lookup
        private readonly HashSet<string> _trackedButtonSet = new HashSet<string>();

        // ── Feature discovery ─────────────────────────────────────────────────────
        private const string PrefsDiscoveredKey = "SWEF_DiscoveredFeatures";
        private HashSet<string> _discoveredFeatures;

        // ── Screen view tracking ──────────────────────────────────────────────────
        private readonly Dictionary<string, float>  _screenOpenTimes  = new Dictionary<string, float>();
        private readonly List<string>               _visitedScreens   = new List<string>();

        // ── Funnel tracking ───────────────────────────────────────────────────────
        // Each funnel: ordered step names → current step index
        private readonly Dictionary<string, FunnelState> _funnels = new Dictionary<string, FunnelState>();

        // ── Session state ─────────────────────────────────────────────────────────
        private int _flightCount;
        private readonly HashSet<string> _featuresUsed = new HashSet<string>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Populate the tracked-button lookup set
            if (trackedButtonNames != null)
            {
                foreach (string n in trackedButtonNames)
                    if (!string.IsNullOrEmpty(n)) _trackedButtonSet.Add(n);
            }

            LoadDiscoveredFeatures();

            // Register default flight funnel
            RegisterFunnel("main_flow", new[]
            {
                "app_open", "flight_start", "altitude_5km", "screenshot_taken", "share_completed"
            });
        }

        private void OnApplicationQuit()
        {
            FireSessionSummary();
        }

        // ── Screen view API ───────────────────────────────────────────────────────

        /// <summary>Record that a screen / UI panel was opened.</summary>
        public void TrackScreenOpen(string screenName)
        {
            if (string.IsNullOrEmpty(screenName)) return;
            _screenOpenTimes[screenName] = Time.realtimeSinceStartup;
            if (!_visitedScreens.Contains(screenName)) _visitedScreens.Add(screenName);

            var dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            var evt = TelemetryEventBuilder.Create(AnalyticsEvents.ScreenView)
                .WithCategory("ui")
                .WithProperty("screen", screenName)
                .WithProperty("action", "open")
                .Build();
            dispatcher.EnqueueEvent(evt);
        }

        /// <summary>Record that a screen / UI panel was closed and report how long it was open.</summary>
        public void TrackScreenClose(string screenName)
        {
            if (string.IsNullOrEmpty(screenName)) return;

            float duration = 0f;
            if (_screenOpenTimes.TryGetValue(screenName, out float openTime))
            {
                duration = Time.realtimeSinceStartup - openTime;
                _screenOpenTimes.Remove(screenName);
            }

            var dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            var evt = TelemetryEventBuilder.Create(AnalyticsEvents.ScreenView)
                .WithCategory("ui")
                .WithProperty("screen",      screenName)
                .WithProperty("action",      "close")
                .WithProperty("durationSec", duration)
                .Build();
            dispatcher.EnqueueEvent(evt);
        }

        // ── Button click API ──────────────────────────────────────────────────────

        /// <summary>Manually record a button click event.</summary>
        public void TrackButtonClick(string buttonName)
        {
            var dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            var evt = TelemetryEventBuilder.Create(AnalyticsEvents.ButtonClick)
                .WithCategory("ui")
                .WithProperty("button", buttonName)
                .Build();
            dispatcher.EnqueueEvent(evt);
        }

        /// <summary>Attach a listener to a UnityEngine.UI.Button by name.</summary>
        public void WireButton(Button btn)
        {
            if (btn == null) return;
            string name = btn.gameObject.name;
            // Only wire if the button is in the tracked set (or if no filter is configured)
            if (_trackedButtonSet.Count > 0 && !_trackedButtonSet.Contains(name)) return;
            btn.onClick.AddListener(() => TrackButtonClick(name));
        }

        // ── Settings change API ───────────────────────────────────────────────────

        /// <summary>Record a settings change with before/after values.</summary>
        public void TrackSettingsChange(string setting, object before, object after)
        {
            var dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            var evt = TelemetryEventBuilder.Create(AnalyticsEvents.SettingsChange)
                .WithCategory("ui")
                .WithProperty("setting", setting)
                .WithProperty("before",  before)
                .WithProperty("after",   after)
                .Build();
            dispatcher.EnqueueEvent(evt);
        }

        // ── Tutorial API ──────────────────────────────────────────────────────────

        /// <summary>Record a tutorial step completion.</summary>
        public void TrackTutorialStep(int step, string stepName, bool skipped = false)
        {
            var dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            var evt = TelemetryEventBuilder.Create(AnalyticsEvents.TutorialStep)
                .WithCategory("ui")
                .WithProperty("step",     step)
                .WithProperty("stepName", stepName)
                .WithProperty("skipped",  skipped)
                .Build();
            dispatcher.EnqueueEvent(evt);
        }

        // ── Feature discovery API ─────────────────────────────────────────────────

        /// <summary>
        /// Record the first-ever use of a named feature.
        /// Subsequent calls for the same feature are silently ignored.
        /// </summary>
        public void TrackFeatureDiscovery(string featureName)
        {
            _featuresUsed.Add(featureName);
            if (_discoveredFeatures.Contains(featureName)) return;

            _discoveredFeatures.Add(featureName);
            SaveDiscoveredFeatures();

            var dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            var evt = TelemetryEventBuilder.Create(AnalyticsEvents.FeatureDiscovery)
                .WithCategory("ui")
                .WithProperty("feature", featureName)
                .Build();
            dispatcher.EnqueueEvent(evt);
        }

        // ── Flight count ──────────────────────────────────────────────────────────

        /// <summary>Increment the session flight count.</summary>
        public void IncrementFlightCount() => _flightCount++;

        // ── Funnel API ────────────────────────────────────────────────────────────

        /// <summary>Register a named conversion funnel.</summary>
        public void RegisterFunnel(string funnelName, string[] steps)
        {
            _funnels[funnelName] = new FunnelState { steps = steps, currentStep = 0 };
        }

        /// <summary>Advance any funnels that have this event as their next expected step.</summary>
        public void AdvanceFunnel(string stepName)
        {
            foreach (var kvp in _funnels)
            {
                var state = kvp.Value;
                if (state.currentStep < state.steps.Length &&
                    state.steps[state.currentStep] == stepName)
                {
                    state.currentStep++;
                    if (state.currentStep >= state.steps.Length)
                        FireFunnelCompleted(kvp.Key, state.steps);
                }
            }
        }

        // ── Internal ─────────────────────────────────────────────────────────────

        private void FireSessionSummary()
        {
            var dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            var evt = TelemetryEventBuilder.Create(AnalyticsEvents.SessionSummary)
                .WithCategory("ui")
                .WithProperty("sessionDurationSec", Time.realtimeSinceStartup)
                .WithProperty("screensVisited",      _visitedScreens.Count)
                .WithProperty("flightCount",         _flightCount)
                .WithProperty("featuresUsed",        _featuresUsed.Count)
                .Build();
            dispatcher.EnqueueEvent(evt);
        }

        private void FireFunnelCompleted(string funnelName, string[] steps)
        {
            var dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            var evt = TelemetryEventBuilder.Create("funnel_completed")
                .WithCategory("ui")
                .WithProperty("funnel",   funnelName)
                .WithProperty("steps",    steps.Length)
                .Build();
            dispatcher.EnqueueEvent(evt);
        }

        // ── Feature discovery persistence ─────────────────────────────────────────

        private void LoadDiscoveredFeatures()
        {
            _discoveredFeatures = new HashSet<string>();
            string raw = PlayerPrefs.GetString(PrefsDiscoveredKey, "");
            if (!string.IsNullOrEmpty(raw))
            {
                foreach (string f in raw.Split(','))
                    if (!string.IsNullOrEmpty(f)) _discoveredFeatures.Add(f);
            }
        }

        private void SaveDiscoveredFeatures()
        {
            PlayerPrefs.SetString(PrefsDiscoveredKey, string.Join(",", _discoveredFeatures));
            PlayerPrefs.Save();
        }

        // ── Nested types ─────────────────────────────────────────────────────────

        private class FunnelState
        {
            public string[] steps;
            public int      currentStep;
        }
    }
}
