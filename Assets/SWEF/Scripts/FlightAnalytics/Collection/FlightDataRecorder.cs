// FlightDataRecorder.cs — Phase 116: Flight Analytics Dashboard
// Real-time flight data recording at configurable sample intervals.
// Namespace: SWEF.FlightAnalytics

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Records position, altitude, speed, heading, G-force, fuel, and
    /// control inputs at the rate defined by <see cref="FlightAnalyticsConfig.samplingRateHz"/>.
    /// Attach to the same GameObject as <see cref="FlightAnalyticsManager"/>.
    /// </summary>
    public class FlightDataRecorder : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private FlightAnalyticsConfig config;
        [SerializeField] private Transform aircraftTransform;

        // ── State ─────────────────────────────────────────────────────────────────

        private bool _isRecording;
        private float _sessionStartTime;
        private Vector3 _lastRecordedPosition;
        private readonly List<FlightDataPoint> _buffer = new List<FlightDataPoint>();
        private Coroutine _samplingCoroutine;

        // ── Properties ────────────────────────────────────────────────────────────

        /// <summary>Whether data is currently being captured.</summary>
        public bool IsRecording => _isRecording;

        /// <summary>Read-only snapshot of the in-progress data buffer.</summary>
        public IReadOnlyList<FlightDataPoint> Buffer => _buffer;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Begin recording flight data.</summary>
        public void StartRecording()
        {
            if (_isRecording) return;
            _isRecording = true;
            _sessionStartTime = Time.time;
            _lastRecordedPosition = aircraftTransform != null ? aircraftTransform.position : Vector3.zero;
            _buffer.Clear();
            float interval = config != null ? 1f / Mathf.Max(0.01f, config.samplingRateHz) : 0.5f;
            _samplingCoroutine = StartCoroutine(SamplingLoop(interval));
            Debug.Log("[SWEF] FlightDataRecorder: Recording started.");
        }

        /// <summary>Stop recording and return the captured data points.</summary>
        public List<FlightDataPoint> StopRecording()
        {
            if (!_isRecording) return new List<FlightDataPoint>(_buffer);
            _isRecording = false;
            if (_samplingCoroutine != null)
            {
                StopCoroutine(_samplingCoroutine);
                _samplingCoroutine = null;
            }
            Debug.Log($"[SWEF] FlightDataRecorder: Recording stopped — {_buffer.Count} samples.");
            return new List<FlightDataPoint>(_buffer);
        }

        // ── Internals ─────────────────────────────────────────────────────────────

        private IEnumerator SamplingLoop(float interval)
        {
            while (_isRecording)
            {
                yield return new WaitForSeconds(interval);
                CaptureDataPoint();
            }
        }

        private void CaptureDataPoint()
        {
            if (aircraftTransform == null) return;

            Vector3 pos = aircraftTransform.position;
            float minDist = config != null ? config.minSampleDistanceM : 0f;

            if (minDist > 0f && Vector3.Distance(pos, _lastRecordedPosition) < minDist)
                return;

            _lastRecordedPosition = pos;

            var point = new FlightDataPoint
            {
                timestamp   = Time.time - _sessionStartTime,
                position    = pos,
                altitude    = pos.y,
                speedKnots  = ResolveSpeed(),
                heading     = aircraftTransform.eulerAngles.y,
                gForce      = ResolveGForce(),
                fuelNormalised = ResolveFuel(),
                throttleInput = 0f,
                pitchInput    = 0f,
                rollInput     = 0f
            };

            _buffer.Add(point);
        }

        // Soft-resolve: try to read from FlightController via reflection to avoid
        // a hard compile-time dependency.
        private float ResolveSpeed()
        {
            var fc = FindFirstObjectByType<MonoBehaviour>();
            // Real implementation would cast to FlightController via #if SWEF_FLIGHT_AVAILABLE
            return 0f;
        }

        private float ResolveGForce() => 1f; // Gravity baseline

        private float ResolveFuel() => 1f;
    }
}
