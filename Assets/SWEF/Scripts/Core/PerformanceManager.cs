using System.Reflection;
using UnityEngine;

namespace SWEF.Core
{
    /// <summary>
    /// Monitors FPS in real-time and automatically adjusts the Cesium 3D Tiles
    /// <c>maximumScreenSpaceError</c> to maintain a target frame rate.
    /// Provides adaptive quality switching (Low / Medium / High) with a cooldown
    /// period to prevent rapid oscillation.
    /// </summary>
    public class PerformanceManager : MonoBehaviour
    {
        /// <summary>Quality presets mapped to Cesium maximumScreenSpaceError values.</summary>
        public enum QualityLevel { Low, Medium, High }

        [Header("FPS Targets")]
        /// <summary>Informational target FPS shown in the Inspector. Used as a baseline reference for threshold tuning.</summary>
        [SerializeField] private int targetFPS = 30;
        [SerializeField] private float fpsSampleInterval = 1.0f;
        [SerializeField] private int qualityUpThreshold = 45;
        [SerializeField] private int qualityDownThreshold = 25;

        [Header("Switching")]
        [SerializeField] private float switchCooldown = 5.0f;

        [Header("Cesium Tileset")]
        [SerializeField] private Component tileset;

        // SSE values per quality level
        private const float SSE_LOW    = 24f;
        private const float SSE_MEDIUM =  8f;
        private const float SSE_HIGH   =  2f;

        /// <summary>The currently active quality level.</summary>
        public QualityLevel CurrentQuality { get; private set; } = QualityLevel.Medium;

        /// <summary>The most recently sampled frames-per-second value.</summary>
        public float CurrentFPS { get; private set; }

        /// <summary>Current FPS exposed as a canonical alias for Phase 26 integration.</summary>
        public float CurrentFps => CurrentFPS;

        /// <summary>The most recent frame time in milliseconds.</summary>
        public float FrameTimeMs { get; private set; }

        private int   _frameCount;
        private float _elapsedSinceLastSample;
        private float _cooldownTimer;

        private void Start()
        {
            ApplyQuality(QualityLevel.Medium);
        }

        private void Update()
        {
            _frameCount++;
            float dt = Time.unscaledDeltaTime;
            FrameTimeMs = dt * 1000f;
            _elapsedSinceLastSample += dt;
            _cooldownTimer           = Mathf.Max(0f, _cooldownTimer - dt);

            // Phase 26 — notify PerformanceProfiler
            SWEF.Performance.PerformanceProfiler.Instance?.RecordFrame(dt);

            if (_elapsedSinceLastSample >= fpsSampleInterval)
            {
                CurrentFPS              = _frameCount / _elapsedSinceLastSample;
                _frameCount             = 0;
                _elapsedSinceLastSample = 0f;
                EvaluateQuality();
            }
        }

        /// <summary>
        /// Evaluates whether the quality level should be raised or lowered based on the
        /// current FPS sample. Respects the cooldown timer to prevent oscillation.
        /// </summary>
        private void EvaluateQuality()
        {
            if (_cooldownTimer > 0f) return;

            if (CurrentFPS < qualityDownThreshold && CurrentQuality != QualityLevel.Low)
            {
                ApplyQuality(CurrentQuality == QualityLevel.High ? QualityLevel.Medium : QualityLevel.Low);
            }
            else if (CurrentFPS > qualityUpThreshold && CurrentQuality != QualityLevel.High)
            {
                ApplyQuality(CurrentQuality == QualityLevel.Low ? QualityLevel.Medium : QualityLevel.High);
            }
        }

        /// <summary>
        /// Applies the given <paramref name="level"/> by setting
        /// <c>maximumScreenSpaceError</c> on the tileset component via reflection,
        /// then resets the cooldown timer.
        /// </summary>
        /// <param name="level">The quality level to apply.</param>
        private void ApplyQuality(QualityLevel level)
        {
            CurrentQuality = level;
            float sse = level switch
            {
                QualityLevel.Low  => SSE_LOW,
                QualityLevel.High => SSE_HIGH,
                _                 => SSE_MEDIUM,
            };

            SetTilesetSSE(sse);
            _cooldownTimer = switchCooldown;

            Debug.Log($"[SWEF] PerformanceManager: quality → {level} (SSE={sse}, FPS={CurrentFPS:F1})");
        }

        /// <summary>
        /// Uses reflection to set the <c>maximumScreenSpaceError</c> property or field
        /// on the assigned tileset component, avoiding a hard compile-time dependency on
        /// the Cesium for Unity package.
        /// </summary>
        private void SetTilesetSSE(float value)
        {
            if (tileset == null) return;

            System.Type t = tileset.GetType();

            // Try property first
            PropertyInfo prop = t.GetProperty("maximumScreenSpaceError",
                BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(tileset, value);
                return;
            }

            // Fall back to field
            FieldInfo field = t.GetField("maximumScreenSpaceError",
                BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(tileset, value);
                return;
            }

            Debug.LogWarning("[SWEF] PerformanceManager: 'maximumScreenSpaceError' not found on tileset component.");
        }
    }
}
