using System;
using UnityEngine;

namespace SWEF.CloudRendering
{
    /// <summary>
    /// Continuously assesses network quality using ping, packet loss, and
    /// bandwidth estimates.  Calculates a composite network quality score (0–100)
    /// updated every second and raises events when the score crosses thresholds.
    /// </summary>
    public class NetworkQualityMonitor : MonoBehaviour
    {
        // ── Quality enum ──────────────────────────────────────────────────────────
        /// <summary>Qualitative network quality categories.</summary>
        public enum NetworkQuality { Good, Fair, Poor, Critical }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Thresholds")]
        [SerializeField] private float goodScoreThreshold     = 75f;
        [SerializeField] private float fairScoreThreshold     = 50f;
        [SerializeField] private float poorScoreThreshold     = 25f;
        [SerializeField] private float updateIntervalSec      = 1f;

        // ── Internal state ────────────────────────────────────────────────────────
        private float _networkScore   = 100f;
        private float _pingMs         = 0f;
        private float _packetLossPct  = 0f;
        private float _bandwidthMbps  = 0f;
        private NetworkQuality _lastQuality = NetworkQuality.Good;
        private float _updateTimer;
        private int   _framesReceived;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised every update interval when quality changes category.</summary>
        public event Action<NetworkQuality> OnNetworkQualityChanged;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Composite network quality score in the range 0–100.</summary>
        public float NetworkScore => _networkScore;

        /// <summary>Most recently measured round-trip ping in milliseconds.</summary>
        public float PingMs => _pingMs;

        /// <summary>Estimated packet loss percentage (0–100).</summary>
        public float PacketLossPercent => _packetLossPct;

        /// <summary>Estimated available bandwidth in Mbps.</summary>
        public float BandwidthMbps => _bandwidthMbps;

        /// <summary>Current qualitative network quality category.</summary>
        public NetworkQuality CurrentQuality => ClassifyScore(_networkScore);

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Start()
        {
            var client = FindFirstObjectByType<StreamingClient>();
            if (client != null)
            {
                client.OnLatencyUpdated += OnLatencyUpdated;
                client.OnFrameReceived  += OnFrameReceived;
            }
        }

        private void Update()
        {
            _updateTimer -= Time.deltaTime;
            if (_updateTimer <= 0f)
            {
                _updateTimer = updateIntervalSec;
                RefreshScore();
            }
        }

        private void OnDestroy()
        {
            var client = FindFirstObjectByType<StreamingClient>();
            if (client != null)
            {
                client.OnLatencyUpdated -= OnLatencyUpdated;
                client.OnFrameReceived  -= OnFrameReceived;
            }
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void OnLatencyUpdated(float ms)
        {
            _pingMs = ms;
        }

        private void OnFrameReceived(byte[] _)
        {
            _framesReceived++;
        }

        private void RefreshScore()
        {
            // Ping component: 0–40 points (ideal <50 ms, max penalty at >300 ms)
            float pingScore = Mathf.Clamp01(1f - (_pingMs - 50f) / 250f) * 40f;

            // Packet loss component: 0–40 points
            float lossScore = Mathf.Clamp01(1f - _packetLossPct / 5f) * 40f;

            // Bandwidth component: 0–20 points
            float bwScore = Mathf.Clamp01(_bandwidthMbps / 20f) * 20f;

            _networkScore = Mathf.Clamp(pingScore + lossScore + bwScore, 0f, 100f);

            // Notify bandwidth to adaptive bitrate controller
            FindFirstObjectByType<AdaptiveBitrateController>()
                ?.ReportPacketLoss(_packetLossPct / 100f);

            // Raise event if quality category changed
            var quality = ClassifyScore(_networkScore);
            if (quality != _lastQuality)
            {
                _lastQuality = quality;
                OnNetworkQualityChanged?.Invoke(quality);
                Debug.Log($"[SWEF] NetworkQuality → {quality} (score: {_networkScore:F0})");
            }

            // Reset interval counters
            _framesReceived = 0;
        }

        private NetworkQuality ClassifyScore(float score)
        {
            if (score >= goodScoreThreshold) return NetworkQuality.Good;
            if (score >= fairScoreThreshold) return NetworkQuality.Fair;
            if (score >= poorScoreThreshold) return NetworkQuality.Poor;
            return NetworkQuality.Critical;
        }
    }
}
