using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SWEF.Flight;

namespace SWEF.Recorder
{
    /// <summary>
    /// Records flight path data (position, rotation, altitude, speed) at fixed intervals.
    /// Stores up to a configurable max duration. Provides playback data for replay.
    /// </summary>
    public class FlightRecorder : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private FlightController flight;
        [SerializeField] private AltitudeController altitude;

        [Header("Recording Settings")]
        [SerializeField] private float recordIntervalSec = 0.5f;
        [SerializeField] private float maxRecordDurationSec = 300f;

        /// <summary>A single recorded snapshot of the flight state.</summary>
        [System.Serializable]
        public struct FlightFrame
        {
            public float time;
            public Vector3 position;
            public Quaternion rotation;
            public float altitude;
            public float speed;
        }

        private readonly List<FlightFrame> _frames = new List<FlightFrame>();
        private Coroutine _recordCoroutine;

        /// <summary>Whether recording is currently active.</summary>
        public bool IsRecording { get; private set; }

        /// <summary>Read-only view of the recorded frames.</summary>
        public IReadOnlyList<FlightFrame> Frames => _frames;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (flight == null)
                flight = FindFirstObjectByType<FlightController>();
            if (altitude == null)
                altitude = FindFirstObjectByType<AltitudeController>();
        }

        // ── Public API ────────────────────────────────────────────────────────────
        /// <summary>Clears any existing recording and starts capturing frames.</summary>
        public void StartRecording()
        {
            if (IsRecording) return;
            ClearRecording();
            IsRecording = true;
            _recordCoroutine = StartCoroutine(RecordCoroutine());
            Debug.Log("[SWEF] FlightRecorder: Recording started.");
        }

        /// <summary>Stops an active recording.</summary>
        public void StopRecording()
        {
            if (!IsRecording) return;
            IsRecording = false;
            if (_recordCoroutine != null)
            {
                StopCoroutine(_recordCoroutine);
                _recordCoroutine = null;
            }
            Debug.Log($"[SWEF] FlightRecorder: Recording stopped. {_frames.Count} frames captured.");
        }

        /// <summary>Clears all recorded frames.</summary>
        public void ClearRecording()
        {
            StopRecording();
            _frames.Clear();
            Debug.Log("[SWEF] FlightRecorder: Recording cleared.");
        }

        // ── Internal ─────────────────────────────────────────────────────────────
        private IEnumerator RecordCoroutine()
        {
            int maxFrameCount = Mathf.FloorToInt(maxRecordDurationSec / Mathf.Max(recordIntervalSec, 0.05f));

            while (IsRecording)
            {
                if (flight != null)
                {
                    var frame = new FlightFrame
                    {
                        time     = Time.time,
                        position = flight.transform.position,
                        rotation = flight.transform.rotation,
                        altitude = altitude != null ? altitude.CurrentAltitudeMeters : 0f,
                        speed    = flight.CurrentSpeedMps
                    };
                    _frames.Add(frame);

                    if (_frames.Count >= maxFrameCount)
                    {
                        Debug.LogWarning("[SWEF] FlightRecorder: Max duration reached, stopping recording.");
                        StopRecording();
                        yield break;
                    }
                }

                yield return new WaitForSeconds(recordIntervalSec);
            }
        }
    }
}
