using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Replay
{
    /// <summary>
    /// Phase 48 — Central singleton that orchestrates all recording and playback
    /// operations for the Replay &amp; Flight Recording System.
    /// <para>
    /// Attach to a persistent GameObject in the bootstrap scene.  Wire the optional
    /// <see cref="flightTransform"/> inspector field (or let the manager locate the
    /// player aircraft via <c>FindFirstObjectByType</c> at startup).
    /// </para>
    /// </summary>
    public class FlightRecorderManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static FlightRecorderManager Instance { get; private set; }

        #endregion

        #region Constants

        private const float DefaultCaptureRateFps = 30f;
        private const int   RingBufferCapacity    = 18000; // ~10 min @ 30 fps

        #endregion

        #region Inspector

        [Header("Aircraft Source")]
        [Tooltip("Transform of the player aircraft. Resolved at runtime if left empty.")]
        [SerializeField] private Transform flightTransform;

        [Header("Settings")]
        [SerializeField] private RecordingSettings settings = new RecordingSettings();

        #endregion

        #region Events

        /// <summary>Fired when a new recording session begins.</summary>
        public event Action OnRecordingStarted;

        /// <summary>Fired when the current recording is finalised.</summary>
        public event Action<FlightRecording> OnRecordingStopped;

        /// <summary>Fired when playback of a saved recording starts.</summary>
        public event Action<FlightRecording> OnPlaybackStarted;

        /// <summary>Fired when playback reaches the end or is explicitly stopped.</summary>
        public event Action OnPlaybackFinished;

        /// <summary>Fired each time a new frame is appended to the ring buffer.</summary>
        public event Action<FlightFrame> OnFrameCaptured;

        #endregion

        #region Public Properties

        /// <summary>Current lifecycle state of the recorder.</summary>
        public RecordingState State { get; private set; } = RecordingState.Idle;

        /// <summary>The most recently completed <see cref="FlightRecording"/>, or <c>null</c>.</summary>
        public FlightRecording LastRecording { get; private set; }

        /// <summary>Current elapsed recording time in seconds.</summary>
        public float RecordingTime { get; private set; }

        /// <summary>Exposes the current <see cref="RecordingSettings"/>.</summary>
        public RecordingSettings Settings => settings;

        #endregion

        #region Private State

        // Ring buffer implemented as a fixed-size circular array.
        private FlightFrame[] _ringBuffer;
        private int           _head;         // index of next write position
        private int           _frameCount;   // total frames in buffer (capped at capacity)
        private Coroutine     _captureCoroutine;
        private float         _captureInterval;

        // Optional integration references resolved at runtime.
        private Flight.FlightController   _flightController;
        private Flight.AltitudeController _altitudeController;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitialiseBuffer();
            ResolveReferences();
        }

        #endregion

        #region Public API — Recording

        /// <summary>Starts a fresh recording session.</summary>
        public void StartRecording()
        {
            if (State == RecordingState.Recording) return;

            ResetBuffer();
            RecordingTime = 0f;
            State         = RecordingState.Recording;

            _captureInterval  = 1f / Mathf.Max(settings.captureRate, 1f);
            _captureCoroutine = StartCoroutine(CaptureLoop());

            OnRecordingStarted?.Invoke();
            Debug.Log("[SWEF] FlightRecorderManager: Recording started.");
        }

        /// <summary>Temporarily suspends frame capture without discarding buffered data.</summary>
        public void PauseRecording()
        {
            if (State != RecordingState.Recording) return;
            State = RecordingState.Paused;
            if (_captureCoroutine != null)
            {
                StopCoroutine(_captureCoroutine);
                _captureCoroutine = null;
            }
            Debug.Log("[SWEF] FlightRecorderManager: Recording paused.");
        }

        /// <summary>Resumes a paused recording session.</summary>
        public void ResumeRecording()
        {
            if (State != RecordingState.Paused) return;
            State             = RecordingState.Recording;
            _captureCoroutine = StartCoroutine(CaptureLoop());
            Debug.Log("[SWEF] FlightRecorderManager: Recording resumed.");
        }

        /// <summary>
        /// Finalises the recording, builds a <see cref="FlightRecording"/>, and fires
        /// <see cref="OnRecordingStopped"/>.
        /// </summary>
        /// <returns>The completed recording, or <c>null</c> if no frames were captured.</returns>
        public FlightRecording StopRecording()
        {
            if (State == RecordingState.Idle) return null;

            State = RecordingState.Stopped;
            if (_captureCoroutine != null)
            {
                StopCoroutine(_captureCoroutine);
                _captureCoroutine = null;
            }

            LastRecording = BuildRecording();
            OnRecordingStopped?.Invoke(LastRecording);

            if (settings.autoSave && RecordingStorageManager.Instance != null)
                RecordingStorageManager.Instance.SaveRecording(LastRecording);

            State = RecordingState.Idle;
            Debug.Log($"[SWEF] FlightRecorderManager: Recording stopped — {_frameCount} frames, {RecordingTime:F1}s.");
            return LastRecording;
        }

        #endregion

        #region Public API — Playback

        /// <summary>Notifies listeners that playback of <paramref name="recording"/> has begun.</summary>
        public void NotifyPlaybackStarted(FlightRecording recording)
        {
            OnPlaybackStarted?.Invoke(recording);
        }

        /// <summary>Notifies listeners that playback has ended.</summary>
        public void NotifyPlaybackFinished()
        {
            OnPlaybackFinished?.Invoke();
        }

        #endregion

        #region Private — Capture Loop

        private IEnumerator CaptureLoop()
        {
            var wait = new WaitForSeconds(_captureInterval);
            while (State == RecordingState.Recording)
            {
                CaptureFrame();
                yield return wait;
            }
        }

        private void CaptureFrame()
        {
            if (flightTransform == null) return;

            float speed = 0f;
            float alt   = 0f;
            float throttle = 0f;
            Vector3 velocity = Vector3.zero;

            if (_flightController != null)
            {
                speed    = _flightController.CurrentSpeedMps;
                throttle = _flightController.Throttle01;
                velocity = _flightController.Velocity;
            }
            if (_altitudeController != null)
                alt = _altitudeController.CurrentAltitudeMeters;

            var frame = new FlightFrame
            {
                position  = flightTransform.position,
                rotation  = flightTransform.rotation,
                velocity  = velocity,
                altitude  = alt,
                timestamp = RecordingTime,
                throttle  = throttle,
                pitchInput = 0f,
                rollInput  = 0f,
                yawInput   = 0f,
                speed      = speed
            };

            WriteFrame(frame);
            RecordingTime += _captureInterval;

            OnFrameCaptured?.Invoke(frame);
        }

        #endregion

        #region Private — Ring Buffer

        private void InitialiseBuffer()
        {
            int capacity   = Mathf.Max(RingBufferCapacity, 1);
            _ringBuffer    = new FlightFrame[capacity];
            ResetBuffer();
        }

        private void ResetBuffer()
        {
            _head       = 0;
            _frameCount = 0;
        }

        private void WriteFrame(FlightFrame frame)
        {
            _ringBuffer[_head] = frame;
            _head = (_head + 1) % _ringBuffer.Length;
            if (_frameCount < _ringBuffer.Length) _frameCount++;

            // Auto-trim: keep only maxDuration worth of frames.
            int maxFrames = Mathf.CeilToInt(settings.maxDuration * settings.captureRate);
            while (_frameCount > maxFrames && _frameCount > 0)
            {
                // Oldest frame is at (_head - _frameCount + _ringBuffer.Length) % _ringBuffer.Length
                _frameCount--;
            }
        }

        private List<FlightFrame> DrainBuffer()
        {
            var list = new List<FlightFrame>(_frameCount);
            int oldest = (_head - _frameCount + _ringBuffer.Length) % _ringBuffer.Length;
            for (int i = 0; i < _frameCount; i++)
            {
                int idx = (oldest + i) % _ringBuffer.Length;
                list.Add(_ringBuffer[idx]);
            }
            return list;
        }

        #endregion

        #region Private — Build Recording

        private FlightRecording BuildRecording()
        {
            var frames = DrainBuffer();
            float maxAlt = 0f, maxSpd = 0f, totalDist = 0f;

            for (int i = 0; i < frames.Count; i++)
            {
                if (frames[i].altitude > maxAlt) maxAlt = frames[i].altitude;
                if (frames[i].speed    > maxSpd) maxSpd = frames[i].speed;
                if (i > 0) totalDist += Vector3.Distance(frames[i].position, frames[i - 1].position) / 1000f;
            }

            return new FlightRecording
            {
                recordingId     = Guid.NewGuid().ToString(),
                pilotName       = "Player",
                aircraftType    = ResolveAircraftType(),
                date            = DateTime.UtcNow.ToString("o"),
                duration        = RecordingTime,
                routeName       = string.Empty,
                frames          = frames,
                maxAltitude     = maxAlt,
                maxSpeed        = maxSpd,
                totalDistanceKm = totalDist
            };
        }

        private string ResolveAircraftType()
        {
            if (_flightController != null) return _flightController.gameObject.name;
            if (flightTransform   != null) return flightTransform.name;
            return "Unknown";
        }

        #endregion

        #region Private — Reference Resolution

        private void ResolveReferences()
        {
            if (flightTransform == null)
            {
                var fc = FindFirstObjectByType<Flight.FlightController>();
                if (fc != null) flightTransform = fc.transform;
            }
            _flightController   = FindFirstObjectByType<Flight.FlightController>();
            _altitudeController = FindFirstObjectByType<Flight.AltitudeController>();
        }

        #endregion
    }
}
