using System;
using UnityEngine;

namespace SWEF.CloudRendering
{
    /// <summary>
    /// Monitors network conditions and adjusts streaming quality automatically.
    /// Bandwidth is estimated from frame arrival intervals.  Quality degrades
    /// immediately when packet loss exceeds 2 %; an upgrade requires
    /// <see cref="StabilityWindowSec"/> consecutive seconds of good conditions
    /// (hysteresis).
    /// </summary>
    public class AdaptiveBitrateController : MonoBehaviour
    {
        // ── Quality enum ──────────────────────────────────────────────────────────
        /// <summary>Streaming quality presets mapped to target resolution.</summary>
        public enum StreamQuality
        {
            /// <summary>480p — minimum quality for very poor connections.</summary>
            Minimum = 0,
            /// <summary>720p — low quality.</summary>
            Low     = 1,
            /// <summary>1080p — medium / default quality.</summary>
            Medium  = 2,
            /// <summary>1440p — high quality.</summary>
            High    = 3,
            /// <summary>4K — ultra quality for excellent connections.</summary>
            Ultra   = 4,
        }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Bitrate Control")]
        [SerializeField] private StreamQuality initialQuality = StreamQuality.Medium;
        [SerializeField] private float packetLossThreshold = 0.02f; // 2 %
        [SerializeField] private float stabilityWindowSec  = 10f;

        [Header("Bandwidth Estimation")]
        [SerializeField] private float minBandwidthMbps = 2f;
        [SerializeField] private float maxBandwidthMbps = 50f;

        // ── Internal state ────────────────────────────────────────────────────────
        private StreamQuality _currentQuality;
        private float _estimatedBandwidthMbps;
        private float _stableTimer;
        private float _lastFrameArrivalTime;
        private long  _bytesReceived;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when the streaming quality level changes.</summary>
        public event Action<StreamQuality> OnQualityChanged;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>The currently active streaming quality preset.</summary>
        public StreamQuality CurrentQuality => _currentQuality;

        /// <summary>Estimated available bandwidth in Mbps.</summary>
        public float EstimatedBandwidthMbps => _estimatedBandwidthMbps;

        /// <summary>How long (in seconds) conditions must be stable before a quality upgrade.</summary>
        public float StabilityWindowSec => stabilityWindowSec;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            _currentQuality         = initialQuality;
            _estimatedBandwidthMbps = 10f;
        }

        private void Start()
        {
            var client = FindFirstObjectByType<StreamingClient>();
            if (client != null)
                client.OnFrameReceived += OnFrameReceived;
        }

        private void Update()
        {
            if (_currentQuality < StreamQuality.Ultra)
            {
                _stableTimer += Time.deltaTime;
                if (_stableTimer >= stabilityWindowSec)
                {
                    _stableTimer = 0f;
                    TryIncreaseQuality();
                }
            }
        }

        private void OnDestroy()
        {
            var client = FindFirstObjectByType<StreamingClient>();
            if (client != null)
                client.OnFrameReceived -= OnFrameReceived;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Notifies the controller that packet loss has been measured.
        /// Triggers immediate quality degradation when loss exceeds 2 %.
        /// </summary>
        public void ReportPacketLoss(float lossPercent)
        {
            if (lossPercent > packetLossThreshold)
            {
                _stableTimer = 0f; // reset stability window
                DecreaseQuality();
            }
        }

        /// <summary>Forces a specific quality level (e.g. from the debug window).</summary>
        public void ForceQuality(StreamQuality quality)
        {
            SetQuality(quality);
            _stableTimer = 0f;
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void OnFrameReceived(byte[] data)
        {
            _bytesReceived += data.Length;
            float now = Time.realtimeSinceStartup;
            float dt  = now - _lastFrameArrivalTime;
            if (dt > 0f)
                _estimatedBandwidthMbps = Mathf.Clamp((data.Length * 8f) / (dt * 1_000_000f),
                    minBandwidthMbps, maxBandwidthMbps);
            _lastFrameArrivalTime = now;
        }

        private void TryIncreaseQuality()
        {
            float required = QualityBandwidthRequirementMbps((StreamQuality)((int)_currentQuality + 1));
            if (_estimatedBandwidthMbps >= required)
            {
                SetQuality((StreamQuality)Mathf.Min((int)_currentQuality + 1, (int)StreamQuality.Ultra));
            }
        }

        private void DecreaseQuality()
        {
            SetQuality((StreamQuality)Mathf.Max((int)_currentQuality - 1, (int)StreamQuality.Minimum));
        }

        private void SetQuality(StreamQuality quality)
        {
            if (_currentQuality == quality) return;
            _currentQuality = quality;
            OnQualityChanged?.Invoke(quality);
            Debug.Log($"[SWEF] AdaptiveBitrate: quality → {quality}");
        }

        private static float QualityBandwidthRequirementMbps(StreamQuality q)
        {
            return q switch
            {
                StreamQuality.Minimum => 1.5f,
                StreamQuality.Low     => 3f,
                StreamQuality.Medium  => 6f,
                StreamQuality.High    => 12f,
                StreamQuality.Ultra   => 25f,
                _                    => 6f,
            };
        }
    }
}
