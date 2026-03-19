using System;
using UnityEngine;

namespace SWEF.Analytics
{
    /// <summary>
    /// Captures device and runtime performance telemetry with minimal overhead.
    /// Uses a circular buffer for FPS samples and <see cref="Time.unscaledDeltaTime"/>
    /// to avoid per-frame heap allocations.
    /// </summary>
    public class PerformanceTelemetryCollector : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Sampling")]
        [Tooltip("How often (seconds) to emit an fps_sample event.")]
        [SerializeField] private float fpsSampleIntervalSeconds = 60f;

        [Tooltip("FPS below this threshold for more than fpsDurationThreshold seconds triggers fps_drop.")]
        [SerializeField] private float fpsDropThreshold  = 20f;
        [SerializeField] private float fpsDurationThreshold = 2f;

        [Header("Detail mode")]
        [Tooltip("When true, samples FPS every second into the circular buffer.")]
        [SerializeField] private bool detailedTracking = false;

        // ── Circular FPS buffer (60 samples) ─────────────────────────────────────
        private const int BufferSize = 60;
        private readonly float[] _fpsSamples = new float[BufferSize];
        private int   _bufferHead;
        private int   _bufferFilled;

        // ── Timing state ─────────────────────────────────────────────────────────
        private float _sampleTimer;
        private float _bufferTimer;

        // FPS-drop tracking
        private float _lowFpsDuration;
        private bool  _fpsDropFired;

        // Scene load timing
        private float _sceneLoadStart;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            _sceneLoadStart = Time.realtimeSinceStartup;
        }

        private void Start()
        {
            Application.lowMemory += OnLowMemory;
            FireSessionStart();

            float loadTime = Time.realtimeSinceStartup - _sceneLoadStart;
            FireLoadingTime(loadTime);

            // Subscribe to quality changes
            var qpm = Core.QualityPresetManager.Instance != null
                ? Core.QualityPresetManager.Instance
                : FindFirstObjectByType<Core.QualityPresetManager>();
            if (qpm != null)
                qpm.OnQualityChanged += OnQualityChanged;
        }

        private void OnDestroy()
        {
            Application.lowMemory -= OnLowMemory;
        }

        private void Update()
        {
            float dt  = Time.unscaledDeltaTime;
            float fps = dt > 0f ? 1f / dt : 0f;

            // Circular buffer for rolling stats
            _bufferTimer += dt;
            float bufferInterval = detailedTracking ? 1f : fpsSampleIntervalSeconds / BufferSize;
            if (_bufferTimer >= bufferInterval)
            {
                _bufferTimer = 0f;
                _fpsSamples[_bufferHead] = fps;
                _bufferHead = (_bufferHead + 1) % BufferSize;
                if (_bufferFilled < BufferSize) _bufferFilled++;
            }

            // FPS drop detection
            if (fps < fpsDropThreshold)
            {
                _lowFpsDuration += dt;
                if (_lowFpsDuration >= fpsDurationThreshold && !_fpsDropFired)
                {
                    _fpsDropFired = true;
                    FireFpsDrop(fps);
                }
            }
            else
            {
                _lowFpsDuration = 0f;
                _fpsDropFired   = false;
            }

            // Periodic fps_sample event
            _sampleTimer += dt;
            if (_sampleTimer >= fpsSampleIntervalSeconds)
            {
                _sampleTimer = 0f;
                FireFpsSample();
            }
        }

        // ── Public helpers ────────────────────────────────────────────────────────

        /// <summary>Returns the average FPS over the last N samples.</summary>
        public float GetAverageFps()
        {
            if (_bufferFilled == 0) return 0f;
            float sum = 0f;
            for (int i = 0; i < _bufferFilled; i++) sum += _fpsSamples[i];
            return sum / _bufferFilled;
        }

        /// <summary>Returns the 1% low FPS (worst 1 % of samples in the buffer).</summary>
        public float GetOnePctLow()
        {
            if (_bufferFilled == 0) return 0f;
            // Partial insertion-sort to find the bottom 1%
            int worstCount = Mathf.Max(1, Mathf.FloorToInt(_bufferFilled * 0.01f));
            float min = float.MaxValue;
            for (int i = 0; i < _bufferFilled; i++)
                if (_fpsSamples[i] < min) min = _fpsSamples[i];
            return min;
        }

        // ── Fire events ───────────────────────────────────────────────────────────

        private void FireSessionStart()
        {
            var dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            var evt = TelemetryEventBuilder.Create(AnalyticsEvents.SessionStart)
                .WithCategory("performance")
                .WithProperty("deviceModel",   SystemInfo.deviceModel)
                .WithProperty("os",            SystemInfo.operatingSystem)
                .WithProperty("screenW",       Screen.width)
                .WithProperty("screenH",       Screen.height)
                .WithProperty("ramMB",         SystemInfo.systemMemorySize)
                .WithProperty("gpu",           SystemInfo.graphicsDeviceName)
                .WithProperty("appVersion",    Application.version)
                .Build();
            dispatcher.EnqueueEvent(evt);
        }

        private void FireLoadingTime(float seconds)
        {
            var dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            var evt = TelemetryEventBuilder.Create(AnalyticsEvents.LoadingTime)
                .WithCategory("performance")
                .WithProperty("durationSeconds", seconds)
                .Build();
            dispatcher.EnqueueEvent(evt);
        }

        private void FireFpsSample()
        {
            var dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            var evt = TelemetryEventBuilder.Create(AnalyticsEvents.FpsSample)
                .WithCategory("performance")
                .WithProperty("avgFps",    GetAverageFps())
                .WithProperty("onePctLow", GetOnePctLow())
                .Build();
            dispatcher.EnqueueEvent(evt);
        }

        private void FireFpsDrop(float fps)
        {
            var dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            var evt = TelemetryEventBuilder.Create(AnalyticsEvents.FpsDrop)
                .WithCategory("performance")
                .WithProperty("fps",           fps)
                .WithProperty("durationSec",   _lowFpsDuration)
                .Build();
            dispatcher.EnqueueEvent(evt);
        }

        private void OnLowMemory()
        {
            var dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            var evt = TelemetryEventBuilder.Create(AnalyticsEvents.MemoryWarning)
                .WithCategory("performance")
                .WithProperty("ramMB", SystemInfo.systemMemorySize)
                .Build();
            dispatcher.EnqueueCriticalEvent(evt);
        }

        private void OnQualityChanged(Core.QualityPresetManager.QualityLevel level)
        {
            var dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            var evt = TelemetryEventBuilder.Create(AnalyticsEvents.QualityChange)
                .WithCategory("performance")
                .WithProperty("qualityLevel", level.ToString())
                .Build();
            dispatcher.EnqueueEvent(evt);
        }
    }
}
