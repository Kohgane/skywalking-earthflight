// BeatSyncClock.cs — SWEF Dynamic Soundtrack & Adaptive Music System (Phase 83)
using System;
using UnityEngine;

namespace SWEF.AdaptiveMusic
{
    /// <summary>
    /// Master musical clock that tracks BPM, beat count, bar count, and measure
    /// position using Unity's DSP clock for sample-accurate scheduling.
    ///
    /// <para>Events are fired on the main thread via <see cref="Update"/>; downstream
    /// code must not make blocking calls from event handlers.</para>
    /// </summary>
    public class BeatSyncClock : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Timing")]
        [Tooltip("Starting BPM (beats per minute).")]
        [SerializeField, Range(40f, 240f)] private float bpm = 120f;

        [Tooltip("Beats per bar (e.g. 4 for 4/4 time).")]
        [SerializeField, Range(2, 8)] private int beatsPerBar = 4;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Fired once per beat.</summary>
        public event Action OnBeat;

        /// <summary>Fired once per bar (every <see cref="beatsPerBar"/> beats).</summary>
        public event Action OnBar;

        /// <summary>Fired on the first beat of every bar (alias for OnBar).</summary>
        public event Action OnDownbeat;

        // ── State ─────────────────────────────────────────────────────────────────
        private float _currentBpm;
        private float _targetBpm;
        private int   _bpmChangeBars;   // bars over which BPM lerps to target

        private double _nextBeatDspTime;
        private int    _beatCount;      // total beats since clock started
        private int    _barCount;       // total bars since clock started
        private int    _beatInBar;      // 0-based position within current bar

        private bool _isRunning;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _currentBpm = bpm;
            _targetBpm  = bpm;
        }

        private void OnEnable()
        {
            _nextBeatDspTime = AudioSettings.dspTime + BeatDuration(_currentBpm);
            _isRunning       = true;
        }

        private void OnDisable() => _isRunning = false;

        private void Update()
        {
            if (!_isRunning)
                return;

            double now = AudioSettings.dspTime;
            while (_nextBeatDspTime <= now)
            {
                FireBeat();
                _nextBeatDspTime += BeatDuration(_currentBpm);
            }

            // Lerp BPM toward target
            if (!Mathf.Approximately(_currentBpm, _targetBpm))
            {
                float lerpRate = _bpmChangeBars > 0
                    ? Time.deltaTime / (float)(_bpmChangeBars * beatsPerBar * BeatDuration(_currentBpm))
                    : 1f;
                _currentBpm = Mathf.Lerp(_currentBpm, _targetBpm, lerpRate);
                if (Mathf.Abs(_currentBpm - _targetBpm) < 0.05f)
                    _currentBpm = _targetBpm;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Immediately sets the BPM to <paramref name="newBpm"/>.</summary>
        public void SetBPM(float newBpm)
        {
            _currentBpm  = Mathf.Clamp(newBpm, 20f, 400f);
            _targetBpm   = _currentBpm;
            _bpmChangeBars = 0;
        }

        /// <summary>
        /// Gradually lerps the BPM to <paramref name="newBpm"/> over
        /// <paramref name="bars"/> musical bars.
        /// </summary>
        public void SetBPMOverBars(float newBpm, int bars)
        {
            _targetBpm     = Mathf.Clamp(newBpm, 20f, 400f);
            _bpmChangeBars = Mathf.Max(1, bars);
        }

        /// <summary>Returns the current BPM.</summary>
        public float GetCurrentBPM() => _currentBpm;

        /// <summary>Returns the total number of beats elapsed since the clock started.</summary>
        public int GetCurrentBeat() => _beatCount;

        /// <summary>Returns the total number of bars elapsed since the clock started.</summary>
        public int GetCurrentBar() => _barCount;

        /// <summary>
        /// Returns the fractional progress through the current bar (0 = downbeat, 1 = next downbeat).
        /// </summary>
        public float GetBarProgress01()
        {
            if (_beatCount == 0)
                return 0f;
            return (float)_beatInBar / beatsPerBar;
        }

        /// <summary>
        /// Returns the DSP time of the next bar start, for sample-accurate stem scheduling.
        /// </summary>
        public double GetNextBarTime()
        {
            int beatsUntilNextBar = beatsPerBar - _beatInBar;
            return _nextBeatDspTime + beatsUntilNextBar * BeatDuration(_currentBpm);
        }

        // ── Internals ─────────────────────────────────────────────────────────────

        private void FireBeat()
        {
            _beatCount++;
            _beatInBar = (_beatCount - 1) % beatsPerBar;

            OnBeat?.Invoke();

            if (_beatInBar == 0)
            {
                _barCount++;
                OnBar?.Invoke();
                OnDownbeat?.Invoke();
            }
        }

        private static double BeatDuration(float bpmValue)
        {
            if (bpmValue <= 0f)
                return 0.5;
            return 60.0 / bpmValue;
        }
    }
}
