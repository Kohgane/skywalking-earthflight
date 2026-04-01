// BeatSyncClock.cs — SWEF Dynamic Soundtrack & Adaptive Music System
using System;
using UnityEngine;

namespace SWEF.AdaptiveMusic
{
    /// <summary>
    /// Master musical clock that tracks BPM, beat count, bar count, and measure position.
    /// Fires <see cref="OnBeat"/>, <see cref="OnBar"/>, and <see cref="OnDownbeat"/> events
    /// so that other components can synchronize stem starts/stops with musical time.
    /// </summary>
    public class BeatSyncClock : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("BPM")]
        [Tooltip("Initial beats per minute. Must be in range 20–300.")]
        [Range(20f, 300f)]
        [SerializeField] private float _bpm = 80f;

        [Tooltip("Beats per bar (time signature numerator).")]
        [Range(2, 8)]
        [SerializeField] private int _beatsPerBar = 4;

        [Tooltip("Bars per phrase (used for downbeat events).")]
        [Range(1, 16)]
        [SerializeField] private int _barsPerPhrase = 4;

        [Tooltip("Number of bars over which a BPM change is interpolated.")]
        [Range(1, 16)]
        [SerializeField] private int _bpmChangeBars = 2;

        // ── Constants ──────────────────────────────────────────────────────────

        public const float MinBpm = 20f;
        public const float MaxBpm = 300f;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Fired every beat.</summary>
        public event Action OnBeat;

        /// <summary>Fired on the first beat of every bar.</summary>
        public event Action OnBar;

        /// <summary>Fired on the first beat of every phrase (every N bars).</summary>
        public event Action OnDownbeat;

        // ── State ─────────────────────────────────────────────────────────────

        private float _currentBpm;
        private float _targetBpm;
        private bool  _isLerpingBpm;
        private int   _bpmChangeBarsRemaining;

        private double _nextBeatDspTime;
        private int    _beatInBar;   // 0-based within bar
        private int    _barInPhrase; // 0-based within phrase
        private int    _totalBeats;
        private int    _totalBars;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            _currentBpm = Mathf.Clamp(_bpm, MinBpm, MaxBpm);
            _targetBpm  = _currentBpm;
            _nextBeatDspTime = AudioSettings.dspTime;
        }

        private void Update()
        {
            double dspNow = AudioSettings.dspTime;
            while (dspNow >= _nextBeatDspTime)
            {
                FireBeat();
                _nextBeatDspTime += SecondsPerBeat(_currentBpm);

                // Lerp BPM toward target one beat at a time
                if (_isLerpingBpm)
                {
                    _bpmChangeBarsRemaining--;
                    if (_bpmChangeBarsRemaining <= 0)
                    {
                        _currentBpm  = _targetBpm;
                        _isLerpingBpm = false;
                    }
                    else
                    {
                        float t = 1f - (float)_bpmChangeBarsRemaining / (_bpmChangeBars * _beatsPerBar);
                        _currentBpm = Mathf.Lerp(_currentBpm, _targetBpm, t);
                    }
                }
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Sets a new BPM target, interpolating over the configured number of bars.</summary>
        public void SetBPM(float newBpm)
        {
            newBpm = Mathf.Clamp(newBpm, MinBpm, MaxBpm);
            if (Mathf.Approximately(newBpm, _currentBpm))
                return;

            _targetBpm               = newBpm;
            _isLerpingBpm            = true;
            _bpmChangeBarsRemaining  = _bpmChangeBars * _beatsPerBar;
        }

        /// <summary>Instantly sets BPM without any interpolation.</summary>
        public void SetBPMImmediate(float newBpm)
        {
            _currentBpm   = Mathf.Clamp(newBpm, MinBpm, MaxBpm);
            _targetBpm    = _currentBpm;
            _isLerpingBpm = false;
        }

        /// <summary>Returns the current effective BPM.</summary>
        public float GetCurrentBPM() => _currentBpm;

        /// <summary>Returns the total number of beats elapsed since this clock started.</summary>
        public int GetCurrentBeat() => _totalBeats;

        /// <summary>Returns 0-based beat index within the current bar.</summary>
        public int GetBeatInBar() => _beatInBar;

        /// <summary>Returns a 0–1 progress value within the current bar.</summary>
        public float GetBarProgress01()
        {
            double barDuration = SecondsPerBeat(_currentBpm) * _beatsPerBar;
            double elapsed     = AudioSettings.dspTime - (_nextBeatDspTime - SecondsPerBeat(_currentBpm) * _beatInBar);
            return Mathf.Clamp01((float)(elapsed / barDuration));
        }

        /// <summary>
        /// Returns the DSP time of the start of the next bar, suitable for scheduling
        /// an <see cref="AudioSource.PlayScheduled"/> call.
        /// </summary>
        public double GetNextBarTime()
        {
            int beatsUntilNextBar = _beatsPerBar - _beatInBar;
            return _nextBeatDspTime + SecondsPerBeat(_currentBpm) * (beatsUntilNextBar - 1);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static double SecondsPerBeat(float bpm) => 60.0 / bpm;

        private void FireBeat()
        {
            _totalBeats++;
            OnBeat?.Invoke();

            _beatInBar++;
            if (_beatInBar >= _beatsPerBar)
            {
                _beatInBar = 0;
                _totalBars++;
                OnBar?.Invoke();

                _barInPhrase++;
                if (_barInPhrase >= _barsPerPhrase)
                {
                    _barInPhrase = 0;
                    OnDownbeat?.Invoke();
                }
            }
        }
    }
}
