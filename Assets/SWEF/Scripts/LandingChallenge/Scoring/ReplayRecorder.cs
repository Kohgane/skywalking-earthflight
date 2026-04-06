// ReplayRecorder.cs — Phase 120: Precision Landing Challenge System
// Landing replay: record final approach (last 60s), camera angles, slow-motion, ghost comparison.
// Namespace: SWEF.LandingChallenge

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Records aircraft state during the final approach for replay playback.
    /// Maintains a rolling circular buffer of <see cref="ReplayFrame"/> data.
    /// Supports slow-motion playback speed and ghost comparison overlays.
    /// </summary>
    public class ReplayRecorder : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Recording")]
        [SerializeField] private float bufferDurationSeconds = 60f;
        [SerializeField] private float captureRateFPS        = 10f;

        [Header("Playback")]
        [SerializeField] [Range(0.1f, 2f)] private float playbackSpeed = 1f;

        // ── State ─────────────────────────────────────────────────────────────

        private readonly Queue<ReplayFrame>  _buffer     = new Queue<ReplayFrame>();
        private List<ReplayFrame>            _savedReplay;
        private float                        _captureInterval;
        private float                        _captureTimer;
        private bool                         _isRecording;
        private bool                         _isPlaying;
        private int                          _playbackIndex;
        private float                        _playbackTimer;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised each time a replay frame is available during playback.</summary>
        public event System.Action<ReplayFrame> OnPlaybackFrame;

        /// <summary>Raised when playback finishes.</summary>
        public event System.Action OnPlaybackComplete;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Whether a replay is currently being recorded.</summary>
        public bool IsRecording => _isRecording;

        /// <summary>Whether a saved replay is playing back.</summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>Number of frames in the saved replay.</summary>
        public int SavedFrameCount => _savedReplay?.Count ?? 0;

        /// <summary>Playback speed multiplier.</summary>
        public float PlaybackSpeed { get => playbackSpeed; set => playbackSpeed = Mathf.Clamp(value, 0.1f, 2f); }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Begin recording into the rolling buffer.</summary>
        public void StartRecording()
        {
            _isRecording     = true;
            _captureInterval = 1f / captureRateFPS;
            _captureTimer    = 0f;
            _buffer.Clear();
        }

        /// <summary>Stop recording and freeze the current buffer as the saved replay.</summary>
        public void StopAndSave()
        {
            _isRecording  = false;
            _savedReplay  = new List<ReplayFrame>(_buffer);
        }

        /// <summary>Record one frame of aircraft state into the rolling buffer.</summary>
        public void CaptureFrame(Transform aircraft, float speedKnots, float altFeet,
                                 int gearState, int flapSetting)
        {
            if (!_isRecording) return;
            float startTime = _buffer.Count > 0
                ? _buffer.Peek().TimeOffset
                : 0f;

            var frame = new ReplayFrame
            {
                TimeOffset  = Time.time,
                Position    = aircraft.position,
                Rotation    = aircraft.rotation,
                SpeedKnots  = speedKnots,
                AltitudeFeet = altFeet,
                GearState   = gearState,
                FlapSetting  = flapSetting
            };
            _buffer.Enqueue(frame);

            // Prune old frames outside the buffer window
            while (_buffer.Count > 0 &&
                   (frame.TimeOffset - _buffer.Peek().TimeOffset) > bufferDurationSeconds)
                _buffer.Dequeue();
        }

        /// <summary>Begin playback of the saved replay from the start.</summary>
        public void StartPlayback()
        {
            if (_savedReplay == null || _savedReplay.Count == 0) return;
            _isPlaying     = true;
            _playbackIndex = 0;
            _playbackTimer = 0f;
        }

        /// <summary>Stop playback.</summary>
        public void StopPlayback() => _isPlaying = false;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Update()
        {
            if (_isRecording)
            {
                _captureTimer += Time.deltaTime;
                // External callers drive CaptureFrame; timer triggers are convenience only
                if (_captureTimer >= _captureInterval)
                    _captureTimer = 0f;
            }

            if (_isPlaying && _savedReplay != null && _playbackIndex < _savedReplay.Count)
            {
                _playbackTimer += Time.deltaTime * playbackSpeed;
                var frame = _savedReplay[_playbackIndex];
                if (_playbackTimer >= _captureInterval)
                {
                    OnPlaybackFrame?.Invoke(frame);
                    _playbackIndex++;
                    _playbackTimer = 0f;
                }

                if (_playbackIndex >= _savedReplay.Count)
                {
                    _isPlaying = false;
                    OnPlaybackComplete?.Invoke();
                }
            }
        }
    }
}
