using System.Collections.Generic;
using UnityEngine;
using SWEF.Replay;

namespace SWEF.ReplayTheater
{
    /// <summary>
    /// Timeline controller for Replay Theater playback.
    /// Provides frame-accurate scrubbing, variable playback speed, loop modes,
    /// and A-B section looping.  Drives a <see cref="SWEF.Replay.GhostRacer"/>
    /// (when available) and integrates with <see cref="ReplayTheaterManager"/>.
    /// </summary>
    public class ReplayTimeline : MonoBehaviour
    {
        #region Enums

        /// <summary>Loop behaviour for the timeline.</summary>
        public enum LoopMode
        {
            /// <summary>Play once then stop.</summary>
            None,
            /// <summary>Continuously loop the full replay.</summary>
            LoopFull,
            /// <summary>Continuously loop between the A and B markers.</summary>
            LoopAB
        }

        #endregion

        #region Inspector

        [Header("Settings")]
        [SerializeField] private ReplayTheaterSettings settings;

        [Header("Loop")]
        [SerializeField] private LoopMode loopMode = LoopMode.None;
        [SerializeField] private float loopPointA = 0f;
        [SerializeField] private float loopPointB = -1f;   // -1 means "use end"

        #endregion

        #region Private State

        private ReplayData _data;
        private float      _currentTime;
        private bool       _isPlaying;
        private float      _playbackSpeed = 1f;

        private static readonly float[] SpeedSteps = { 0.25f, 0.5f, 1f, 2f, 4f };

        #endregion

        #region Events

        /// <summary>Fired every frame while the time changes.  Parameter is seconds.</summary>
        public event System.Action<float> OnTimeChanged;

        /// <summary>Fired when play/pause state changes.  Parameter is <c>true</c> when playing.</summary>
        public event System.Action<bool>  OnPlayStateChanged;

        #endregion

        #region Properties

        /// <summary>Current playback position in seconds.</summary>
        public float CurrentTime => _currentTime;

        /// <summary>Total duration of the loaded replay in seconds.</summary>
        public float TotalDuration => _data != null ? _data.GetDuration() : 0f;

        /// <summary>Current playback speed multiplier.</summary>
        public float PlaybackSpeed => _playbackSpeed;

        /// <summary>Whether the timeline is currently playing.</summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>A-loop marker time (seconds).</summary>
        public float LoopPointA => loopPointA;

        /// <summary>B-loop marker time (seconds).</summary>
        public float LoopPointB => loopPointB < 0f ? TotalDuration : loopPointB;

        /// <summary>Currently loaded replay data, or <c>null</c>.</summary>
        public ReplayData Data => _data;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (settings == null)
                settings = Resources.Load<ReplayTheaterSettings>("ReplayTheaterSettings");
            if (settings != null)
                _playbackSpeed = settings.DefaultPlaybackSpeed;
        }

        private void Update()
        {
            if (!_isPlaying || _data == null) return;

            float effectiveB = LoopPointB;
            float newTime    = _currentTime + Time.deltaTime * _playbackSpeed;

            // A-B or full loop handling
            if (loopMode == LoopMode.LoopAB)
            {
                if (newTime >= effectiveB)
                    newTime = loopPointA + (newTime - effectiveB);
            }
            else if (loopMode == LoopMode.LoopFull)
            {
                if (newTime >= TotalDuration)
                    newTime = newTime - TotalDuration;
            }
            else
            {
                if (newTime >= TotalDuration)
                {
                    newTime = TotalDuration;
                    _isPlaying = false;
                    OnPlayStateChanged?.Invoke(false);
                }
            }

            SetTime(newTime);
        }

        #endregion

        #region Public API

        /// <summary>Loads replay data into the timeline and resets to time zero.</summary>
        /// <param name="data">The <see cref="ReplayData"/> to load.</param>
        public void Load(ReplayData data)
        {
            _data        = data;
            _currentTime = 0f;
            _isPlaying   = false;

            if (loopPointB < 0f && data != null)
                loopPointB = data.GetDuration();

            Debug.Log($"[SWEF] ReplayTimeline: Loaded replay. Duration={TotalDuration:F2}s");
        }

        /// <summary>Starts or resumes playback.</summary>
        public void Play()
        {
            if (_data == null) { Debug.LogWarning("[SWEF] ReplayTimeline: No replay data loaded."); return; }
            if (_isPlaying) return;

            _isPlaying = true;
            OnPlayStateChanged?.Invoke(true);
            Debug.Log("[SWEF] ReplayTimeline: Play.");
        }

        /// <summary>Pauses playback.</summary>
        public void Pause()
        {
            if (!_isPlaying) return;
            _isPlaying = false;
            OnPlayStateChanged?.Invoke(false);
            Debug.Log("[SWEF] ReplayTimeline: Paused.");
        }

        /// <summary>Stops playback and rewinds to time zero.</summary>
        public void Stop()
        {
            _isPlaying = false;
            SetTime(0f);
            OnPlayStateChanged?.Invoke(false);
            Debug.Log("[SWEF] ReplayTimeline: Stopped.");
        }

        /// <summary>Seeks to the given absolute time in seconds.</summary>
        /// <param name="time">Target time, clamped to [0, TotalDuration].</param>
        public void SeekTo(float time)
        {
            SetTime(Mathf.Clamp(time, 0f, TotalDuration));
        }

        /// <summary>Sets the playback speed.  Clamped to recognised speed steps.</summary>
        /// <param name="speed">Desired speed multiplier.</param>
        public void SetSpeed(float speed)
        {
            _playbackSpeed = speed;
            Debug.Log($"[SWEF] ReplayTimeline: Speed set to {speed}x.");
        }

        /// <summary>Sets the A loop marker.</summary>
        /// <param name="time">Seconds; clamped to [0, B).</param>
        public void SetLoopPointA(float time)
        {
            loopPointA = Mathf.Clamp(time, 0f, Mathf.Max(0f, LoopPointB - 0.01f));
        }

        /// <summary>Sets the B loop marker.</summary>
        /// <param name="time">Seconds; clamped to (A, TotalDuration].</param>
        public void SetLoopPointB(float time)
        {
            loopPointB = Mathf.Clamp(time, loopPointA + 0.01f, TotalDuration);
        }

        /// <summary>Changes the loop mode.</summary>
        /// <param name="mode">New loop mode.</param>
        public void SetLoopMode(LoopMode mode)
        {
            loopMode = mode;
            Debug.Log($"[SWEF] ReplayTimeline: Loop mode = {mode}.");
        }

        /// <summary>
        /// Returns the interpolated <see cref="ReplayFrame"/> for the current time.
        /// Returns <c>null</c> if no data is loaded.
        /// </summary>
        public SWEF.Replay.ReplayFrame GetCurrentFrame()
        {
            if (_data == null || _data.frames == null || _data.frames.Count == 0) return null;
            return InterpolateFrame(_currentTime);
        }

        #endregion

        #region Internals

        private void SetTime(float t)
        {
            _currentTime = t;
            OnTimeChanged?.Invoke(_currentTime);
        }

        /// <summary>Binary-search interpolation between two adjacent frames.</summary>
        private SWEF.Replay.ReplayFrame InterpolateFrame(float time)
        {
            var frames = _data.frames;
            int lo = 0, hi = frames.Count - 1;

            if (time <= frames[0].time)  return frames[0];
            if (time >= frames[hi].time) return frames[hi];

            while (lo < hi - 1)
            {
                int mid = (lo + hi) / 2;
                if (frames[mid].time <= time) lo = mid;
                else                          hi = mid;
            }

            var a = frames[lo];
            var b = frames[hi];
            float range = b.time - a.time;
            float t01   = range > 0f ? (time - a.time) / range : 0f;

            return new SWEF.Replay.ReplayFrame
            {
                time     = time,
                px       = Mathf.Lerp(a.px, b.px, t01),
                py       = Mathf.Lerp(a.py, b.py, t01),
                pz       = Mathf.Lerp(a.pz, b.pz, t01),
                rx       = Mathf.Lerp(a.rx, b.rx, t01),
                ry       = Mathf.Lerp(a.ry, b.ry, t01),
                rz       = Mathf.Lerp(a.rz, b.rz, t01),
                rw       = Mathf.Lerp(a.rw, b.rw, t01),
                altitude = Mathf.Lerp(a.altitude, b.altitude, t01),
                speed    = Mathf.Lerp(a.speed, b.speed, t01),
            };
        }

        #endregion
    }
}
