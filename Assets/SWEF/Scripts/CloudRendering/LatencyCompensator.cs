using System.Collections.Generic;
using UnityEngine;

namespace SWEF.CloudRendering
{
    /// <summary>
    /// Client-side prediction and reconciliation system.
    /// Maintains a local simulation state that runs ahead of the server by
    /// <see cref="PredictionHorizonMs"/> milliseconds, then smoothly blends
    /// the predicted state with the authoritative server state as frames arrive.
    /// Tracks a rolling average latency over the last 60 samples for jitter
    /// buffer sizing.
    /// </summary>
    public class LatencyCompensator : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Prediction")]
        [SerializeField] private float predictionHorizonMs = 100f;
        [SerializeField] private float blendSpeed = 8f;

        [Header("Jitter Buffer")]
        [SerializeField] private int jitterBufferFrames = 3;

        // ── Internal state ────────────────────────────────────────────────────────
        private const int LatencySampleCount = 60;
        private readonly Queue<float> _latencySamples = new Queue<float>(LatencySampleCount);
        private float _latencySum;

        private float _currentLatencyMs;
        private float _predictedThrottle;
        private float _serverThrottle;
        private float _blendedThrottle;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Rolling average latency in milliseconds (last 60 samples).</summary>
        public float CurrentLatencyMs => _currentLatencyMs;

        /// <summary>How far ahead the client predicts, in milliseconds.</summary>
        public float PredictionHorizonMs
        {
            get => predictionHorizonMs;
            set => predictionHorizonMs = Mathf.Max(0f, value);
        }

        /// <summary>Number of frames held in the jitter buffer.</summary>
        public int JitterBufferFrames => jitterBufferFrames;

        /// <summary>Current blended throttle value after prediction and reconciliation.</summary>
        public float BlendedThrottle => _blendedThrottle;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Start()
        {
            var client = FindFirstObjectByType<StreamingClient>();
            if (client != null)
                client.OnLatencyUpdated += RecordLatencySample;
        }

        private void Update()
        {
            // Continuously blend predicted state toward server state
            _blendedThrottle = Mathf.Lerp(_blendedThrottle, _serverThrottle,
                blendSpeed * Time.deltaTime);
        }

        private void OnDestroy()
        {
            var client = FindFirstObjectByType<StreamingClient>();
            if (client != null)
                client.OnLatencyUpdated -= RecordLatencySample;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Records a new latency measurement and updates the rolling average.</summary>
        public void RecordLatencySample(float ms)
        {
            if (_latencySamples.Count >= LatencySampleCount)
                _latencySum -= _latencySamples.Dequeue();

            _latencySamples.Enqueue(ms);
            _latencySum += ms;
            _currentLatencyMs = _latencySum / _latencySamples.Count;

            // Adjust jitter buffer size based on measured variance
            UpdateJitterBuffer();
        }

        /// <summary>Applies a server-authoritative throttle value for reconciliation.</summary>
        public void ApplyServerState(float serverThrottle)
        {
            _serverThrottle = serverThrottle;
        }

        /// <summary>Stores the locally predicted throttle value before the server confirms it.</summary>
        public void SetPredictedState(float predictedThrottle)
        {
            _predictedThrottle = predictedThrottle;
            _blendedThrottle   = predictedThrottle;
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void UpdateJitterBuffer()
        {
            // Heuristic: one extra buffer frame per 30 ms of average latency
            jitterBufferFrames = Mathf.Clamp(Mathf.RoundToInt(_currentLatencyMs / 30f), 1, 8);
        }
    }
}
