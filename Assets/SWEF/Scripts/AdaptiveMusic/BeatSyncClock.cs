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
        private float _bpmLerpElapsed;  // seconds elapsed since BPM change started
        private float _bpmLerpDuration; // total seconds for BPM transition

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

            // Lerp BPM toward target using accumulated elapsed time
            if (!Mathf.Approximately(_currentBpm, _targetBpm))
            {
                if (_bpmLerpDuration > 0f)
                {
                    _bpmLerpElapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(_bpmLerpElapsed / _bpmLerpDuration);
                    _currentBpm = Mathf.Lerp(_currentBpm, _targetBpm, t);
                    if (t >= 1f)
                    {
                        _currentBpm      = _targetBpm;
                        _bpmLerpDuration = 0f;
                        _bpmLerpElapsed  = 0f;
                    }
                }
                else
                {
                    _currentBpm = _targetBpm;
                }
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
            float startBpm     = _currentBpm;
            _targetBpm         = Mathf.Clamp(newBpm, 20f, 400f);
            _bpmChangeBars     = Mathf.Max(1, bars);
            // Calculate total duration in seconds for the BPM transition
            _bpmLerpDuration   = _bpmChangeBars * beatsPerBar * (float)BeatDuration(startBpm);
            _bpmLerpElapsed    = 0f;
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
        /// When called at the downbeat (<see cref="GetBarProgress01"/> == 0), returns the
        /// start of the following bar (i.e., always returns a time in the future).
        /// </summary>
        public double GetNextBarTime()
        {
            // _beatInBar is 0-based; beatsPerBar - _beatInBar gives beats remaining until next bar.
            // When _beatInBar == 0 (just fired a downbeat), this returns the next bar start,
            // which is one full bar (beatsPerBar beats) in the future.
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
