using System;
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

        // ── Phase 17 — Replay export ──────────────────────────────────────────────

        /// <summary>Exports the current recording to a <see cref="SWEF.Replay.ReplayData"/> instance.</summary>
        public SWEF.Replay.ReplayData ExportToReplayData()
        {
            string now = System.DateTime.UtcNow.ToString("o");
            var data = new SWEF.Replay.ReplayData
            {
                replayId   = System.Guid.NewGuid().ToString(),
                playerName = SystemInfo.deviceName,
                createdAt  = now,
                recordedAt = now,
                startLat   = Core.SWEFSession.Lat,
                startLon   = Core.SWEFSession.Lon,
            };

            var srcFrames = GetFrames();
            float firstTime = srcFrames.Count > 0 ? srcFrames[0].time : 0f;
            float totalDist = 0f;
            Vector3 prevPos = Vector3.zero;
            bool firstFrame = true;

            foreach (var f in srcFrames)
            {
                var rf = new SWEF.Replay.ReplayFrame
                {
                    time     = f.time - firstTime,
                    px       = f.position.x,
                    py       = f.position.y,
                    pz       = f.position.z,
                    rx       = f.rotation.x,
                    ry       = f.rotation.y,
                    rz       = f.rotation.z,
                    rw       = f.rotation.w,
                    altitude = f.altitude,
                    speed    = f.speed,
                };
                data.frames.Add(rf);

                if (f.altitude > data.maxAltitudeM) data.maxAltitudeM = f.altitude;
                if (f.speed    > data.maxSpeedMps)  data.maxSpeedMps  = f.speed;

                if (!firstFrame)
                    totalDist += Vector3.Distance(prevPos, f.position);
                else
                    firstFrame = false;

                prevPos = f.position;
            }

            data.totalDistanceKm  = totalDist / 1000f;
            data.totalDurationSec = GetRecordedDuration();

            return data;
        }

        /// <summary>Returns a copy of the recorded frames list.</summary>
        public List<FlightFrame> GetFrames() => new List<FlightFrame>(_frames);

        /// <summary>Returns the total recorded duration in seconds, or 0 when empty.</summary>
        public float GetRecordedDuration()
        {
            if (_frames.Count < 2) return 0f;
            return _frames[_frames.Count - 1].time - _frames[0].time;
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
