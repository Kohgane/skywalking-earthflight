using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SWEF.Editor
{
    /// <summary>
    /// SWEF → Analytics Debug editor window.
    /// Provides a live view of the telemetry pipeline, manual flush/clear,
    /// test event firing, consent overrides, and A/B test inspection.
    /// </summary>
    public class AnalyticsDebugWindow : EditorWindow
    {
        // ── Scroll positions ──────────────────────────────────────────────────────
        private Vector2 _eventsScroll;
        private Vector2 _abTestScroll;

        // ── UI state ─────────────────────────────────────────────────────────────
        private bool _showEvents  = true;
        private bool _showABTests = true;
        private bool _showPerf    = true;

        private readonly Dictionary<string, bool> _expandedEvents = new Dictionary<string, bool>();

        [MenuItem("SWEF/Analytics Debug")]
        private static void OpenWindow()
        {
            var win = GetWindow<AnalyticsDebugWindow>("Analytics Debug");
            win.minSize = new Vector2(400f, 600f);
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use this window.", MessageType.Info);
                return;
            }

            DrawHeader();
            EditorGUILayout.Space(4f);
            DrawActions();
            EditorGUILayout.Space(4f);
            DrawConsent();
            EditorGUILayout.Space(4f);
            DrawPerformance();
            EditorGUILayout.Space(4f);
            DrawRecentEvents();
            EditorGUILayout.Space(4f);
            DrawABTests();
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        // ── Sections ─────────────────────────────────────────────────────────────

        private void DrawHeader()
        {
            var dispatcher = Analytics.TelemetryDispatcher.Instance;
            EditorGUILayout.LabelField("SWEF Analytics Pipeline", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Queue size: {(dispatcher != null ? dispatcher.QueueCount.ToString() : "—")} events");
            EditorGUILayout.LabelField($"Endpoint:   {(dispatcher != null ? "configured" : "no instance")}");
        }

        private void DrawActions()
        {
            var dispatcher = Analytics.TelemetryDispatcher.Instance;
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Flush Now"))
                dispatcher?.FlushNow();

            if (GUILayout.Button("Clear Queue"))
                dispatcher?.ClearQueue();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Fire Test Events", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("flight_start"))
                FireTestEvent(Analytics.AnalyticsEvents.FlightStart, "flight");
            if (GUILayout.Button("iap_completed"))
                FireTestEvent(Analytics.AnalyticsEvents.IapCompleted, "purchase");
            if (GUILayout.Button("error_caught"))
                FireTestEvent(Analytics.AnalyticsEvents.ErrorCaught, "error");

            EditorGUILayout.EndHorizontal();
        }

        private void DrawConsent()
        {
            var pcm = Analytics.PrivacyConsentManager.Instance;
            EditorGUILayout.LabelField("Privacy & Consent", EditorStyles.boldLabel);

            if (pcm == null)
            {
                EditorGUILayout.LabelField("PrivacyConsentManager not found.");
                return;
            }

            var level = pcm.GetCurrentConsent();
            EditorGUILayout.LabelField($"Current consent: {level}");

            EditorGUILayout.BeginHorizontal();
            foreach (Analytics.PrivacyConsentManager.ConsentLevel l in
                System.Enum.GetValues(typeof(Analytics.PrivacyConsentManager.ConsentLevel)))
            {
                if (GUILayout.Button(l.ToString()))
                    pcm.SetConsent(l);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Delete All Data"))  pcm.RequestDataDeletion();
            if (GUILayout.Button("Export JSON"))      Debug.Log(pcm.ExportUserData());
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPerformance()
        {
            _showPerf = EditorGUILayout.Foldout(_showPerf, "Performance");
            if (!_showPerf) return;

            var collector = Object.FindAnyObjectByType<Analytics.PerformanceTelemetryCollector>();
            if (collector == null)
            {
                EditorGUILayout.LabelField("PerformanceTelemetryCollector not found.");
                return;
            }

            EditorGUILayout.LabelField($"Avg FPS:    {collector.GetAverageFps():0.0}");
            EditorGUILayout.LabelField($"1% Low FPS: {collector.GetOnePctLow():0.0}");
        }

        private void DrawRecentEvents()
        {
            _showEvents = EditorGUILayout.Foldout(_showEvents, "Recent Events (last 20)");
            if (!_showEvents) return;

            var dispatcher = Analytics.TelemetryDispatcher.Instance;
            if (dispatcher == null) { EditorGUILayout.LabelField("—"); return; }

            var events = dispatcher.GetRecentEvents(20);
            _eventsScroll = EditorGUILayout.BeginScrollView(_eventsScroll, GUILayout.Height(200f));

            for (int i = events.Count - 1; i >= 0; i--)
            {
                var e = events[i];
                string key = e.eventId ?? i.ToString();

                if (!_expandedEvents.ContainsKey(key)) _expandedEvents[key] = false;
                _expandedEvents[key] = EditorGUILayout.Foldout(
                    _expandedEvents[key],
                    $"[{e.sequenceNumber}] {e.eventName} ({e.category})");

                if (_expandedEvents[key] && e.properties != null)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("timestamp", e.timestamp.ToString("HH:mm:ss.fff"));
                    foreach (var kvp in e.properties)
                        EditorGUILayout.LabelField(kvp.Key, kvp.Value?.ToString() ?? "null");
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawABTests()
        {
            _showABTests = EditorGUILayout.Foldout(_showABTests, "A/B Tests");
            if (!_showABTests) return;

            var abm = Analytics.ABTestManager.Instance;
            if (abm == null) { EditorGUILayout.LabelField("ABTestManager not found."); return; }

            _abTestScroll = EditorGUILayout.BeginScrollView(_abTestScroll, GUILayout.Height(120f));

            foreach (var test in abm.GetAllTests())
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(test.testName, GUILayout.Width(140f));
                EditorGUILayout.LabelField($"→ {test.assignedVariant}", GUILayout.Width(100f));

                if (test.variants != null)
                {
                    foreach (string v in test.variants)
                    {
                        if (GUILayout.Button(v, GUILayout.Width(80f)))
                            abm.OverrideVariant(test.testId, v);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static void FireTestEvent(string name, string category)
        {
            var dispatcher = Analytics.TelemetryDispatcher.Instance;
            if (dispatcher == null) { Debug.LogWarning("[SWEF] No TelemetryDispatcher found."); return; }

            var evt = Analytics.TelemetryEventBuilder.Create(name)
                .WithCategory(category)
                .WithProperty("source", "editor_debug_window")
                .Build();
            dispatcher.EnqueueEvent(evt);
            Debug.Log($"[SWEF] Analytics Debug: test event '{name}' enqueued.");
        }
    }
}
