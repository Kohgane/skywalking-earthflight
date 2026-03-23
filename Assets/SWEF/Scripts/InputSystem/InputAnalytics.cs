using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.InputSystem
{
    /// <summary>
    /// Phase 57 — Collects and forwards input-system telemetry events for the
    /// SWEF analytics pipeline.
    /// <para>
    /// Subscribes to <see cref="InputBindingManager"/>, <see cref="InputDeviceDetector"/>,
    /// and <see cref="InputPresetManager"/> events.  Batches events in memory and
    /// flushes them to the analytics back-end (guarded by
    /// <c>#if SWEF_ANALYTICS_AVAILABLE</c> so the module compiles without the
    /// Analytics system present).
    /// </para>
    /// </summary>
    public class InputAnalytics : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static InputAnalytics Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Telemetry")]
        [Tooltip("Maximum number of events kept in the in-memory batch before auto-flush.")]
        [Range(10, 500)]
        [SerializeField] private int batchFlushThreshold = 50;

        [Tooltip("Auto-flush interval in seconds (0 = flush only when threshold is reached or on session end).")]
        [Range(0f, 300f)]
        [SerializeField] private float autoFlushIntervalSeconds = 60f;

        #endregion

        #region Public Properties

        /// <summary>Total number of rebind events captured this session.</summary>
        public int RebindCount { get; private set; }

        /// <summary>Total number of device-switch events captured this session.</summary>
        public int DeviceSwitchCount { get; private set; }

        /// <summary>Total number of preset-applied events captured this session.</summary>
        public int PresetAppliedCount { get; private set; }

        #endregion

        #region Private State

        private readonly List<InputTelemetryEvent> _batch = new List<InputTelemetryEvent>();
        private float _timeSinceFlush;

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
            if (InputBindingManager.Instance != null)
            {
                InputBindingManager.Instance.OnBindingChanged  += HandleBindingChanged;
                InputBindingManager.Instance.OnPresetApplied   += HandlePresetApplied;
                InputBindingManager.Instance.OnRebindFinished  += HandleRebindFinished;
            }

            if (InputDeviceDetector.Instance != null)
                InputDeviceDetector.Instance.OnDeviceChanged += HandleDeviceChanged;
        }

        private void OnDisable()
        {
            if (InputBindingManager.Instance != null)
            {
                InputBindingManager.Instance.OnBindingChanged  -= HandleBindingChanged;
                InputBindingManager.Instance.OnPresetApplied   -= HandlePresetApplied;
                InputBindingManager.Instance.OnRebindFinished  -= HandleRebindFinished;
            }

            if (InputDeviceDetector.Instance != null)
                InputDeviceDetector.Instance.OnDeviceChanged -= HandleDeviceChanged;
        }

        private void Update()
        {
            if (autoFlushIntervalSeconds <= 0f) return;
            _timeSinceFlush += Time.unscaledDeltaTime;
            if (_timeSinceFlush >= autoFlushIntervalSeconds)
            {
                Flush();
                _timeSinceFlush = 0f;
            }
        }

        private void OnApplicationQuit() => Flush();

        #endregion

        #region Private — Event Handlers

        private void HandleBindingChanged(BindingEntry entry)
        {
            Enqueue(new InputTelemetryEvent
            {
                eventType   = "binding_changed",
                actionName  = entry.actionName,
                stringValue = entry.primaryKey,
                timestamp   = Time.realtimeSinceStartup
            });
        }

        private void HandlePresetApplied(string presetName)
        {
            PresetAppliedCount++;
            Enqueue(new InputTelemetryEvent
            {
                eventType   = "preset_applied",
                stringValue = presetName,
                timestamp   = Time.realtimeSinceStartup
            });
        }

        private void HandleRebindFinished(string actionName, bool success)
        {
            if (!success) return;
            RebindCount++;
            Enqueue(new InputTelemetryEvent
            {
                eventType  = "rebind_completed",
                actionName = actionName,
                timestamp  = Time.realtimeSinceStartup
            });
        }

        private void HandleDeviceChanged(InputDeviceType from, InputDeviceType to)
        {
            DeviceSwitchCount++;
            Enqueue(new InputTelemetryEvent
            {
                eventType   = "device_switched",
                stringValue = $"{from}→{to}",
                timestamp   = Time.realtimeSinceStartup
            });
        }

        #endregion

        #region Private — Batching & Flush

        private void Enqueue(InputTelemetryEvent evt)
        {
            _batch.Add(evt);
            if (_batch.Count >= batchFlushThreshold)
                Flush();
        }

        private void Flush()
        {
            if (_batch.Count == 0) return;

#if SWEF_ANALYTICS_AVAILABLE
            foreach (var evt in _batch)
                SWEF.Analytics.AnalyticsManager.Instance?.Track(evt.eventType, new System.Collections.Generic.Dictionary<string, object>
                {
                    { "action",    evt.actionName  },
                    { "value",     evt.stringValue },
                    { "timestamp", evt.timestamp   }
                });
#else
            // No analytics system available — log to console in development builds.
            if (Debug.isDebugBuild)
            {
                foreach (var evt in _batch)
                    Debug.Log($"[SWEF InputAnalytics] {evt.eventType} | action={evt.actionName} | value={evt.stringValue}");
            }
#endif
            _batch.Clear();
            _timeSinceFlush = 0f;
        }

        #endregion

        // ── Telemetry Event ───────────────────────────────────────────────────────

        /// <summary>A single batched telemetry event recorded by the input system.</summary>
        [Serializable]
        public struct InputTelemetryEvent
        {
            /// <summary>Short identifier for the event type.</summary>
            public string eventType;

            /// <summary>Name of the affected action, if applicable.</summary>
            public string actionName;

            /// <summary>Auxiliary string payload (key name, preset name, device transition).</summary>
            public string stringValue;

            /// <summary>Real-time seconds since startup when the event occurred.</summary>
            public float  timestamp;
        }
    }
}
