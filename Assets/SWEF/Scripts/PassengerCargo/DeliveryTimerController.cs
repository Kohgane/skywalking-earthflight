using System;
using UnityEngine;

namespace SWEF.PassengerCargo
{
    /// <summary>
    /// MonoBehaviour countdown timer for transport missions.
    ///
    /// Phases:
    ///   Green    — > 50 % time remaining
    ///   Yellow   — 25–50 %
    ///   Red      — &lt; 25 %
    ///   Overtime — 0 % (mission still completable with penalty)
    ///
    /// The timer pauses during Loading/Unloading states and the UI can subscribe to
    /// phase-change and expiry events.
    /// </summary>
    public class DeliveryTimerController : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static DeliveryTimerController Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────
        /// <summary>Fired whenever the timer phase changes.</summary>
        public event Action<TimerPhase> OnTimerPhaseChanged;

        /// <summary>Fired when the countdown reaches zero (transitions to Overtime).</summary>
        public event Action OnTimeExpired;

        // ── State ─────────────────────────────────────────────────────────────
        private float      _totalSeconds;
        private float      _remainingSeconds;
        private bool       _running;
        private bool       _paused;
        private TimerPhase _currentPhase;

        // ── Properties ────────────────────────────────────────────────────────
        /// <summary>Seconds remaining; negative in Overtime.</summary>
        public float TimeRemainingSeconds => _remainingSeconds;

        /// <summary>Fraction of time remaining [0,1].</summary>
        public float TimeRemainingFraction =>
            _totalSeconds > 0f ? Mathf.Max(0f, _remainingSeconds / _totalSeconds) : 1f;

        /// <summary>Current timer phase.</summary>
        public TimerPhase CurrentPhase => _currentPhase;

        /// <summary>True while the timer is counting down or in Overtime.</summary>
        public bool IsRunning => _running;

        // ── Unity Lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (!_running || _paused) return;

            _remainingSeconds -= Time.deltaTime;
            EvaluatePhase();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Starts the delivery countdown.
        /// If <paramref name="totalSeconds"/> is &lt;= 0 the timer runs indefinitely
        /// without expiry (no time limit contract).
        /// </summary>
        public void StartTimer(float totalSeconds)
        {
            _totalSeconds     = totalSeconds > 0f ? totalSeconds : float.PositiveInfinity;
            _remainingSeconds = _totalSeconds;
            _running          = true;
            _paused           = false;
            _currentPhase     = TimerPhase.Green;
        }

        /// <summary>Pauses the countdown (e.g. during loading).</summary>
        public void PauseTimer() => _paused = true;

        /// <summary>Resumes a paused countdown.</summary>
        public void ResumeTimer() => _paused = false;

        /// <summary>Stops and resets the timer.</summary>
        public void StopTimer()
        {
            _running          = false;
            _paused           = false;
            _remainingSeconds = 0f;
        }

        // ── Internal ──────────────────────────────────────────────────────────
        private void EvaluatePhase()
        {
            TimerPhase target;

            if (_remainingSeconds <= 0f)
            {
                target = TimerPhase.Overtime;
                if (_currentPhase != TimerPhase.Overtime)
                    OnTimeExpired?.Invoke();
            }
            else
            {
                float frac = TimeRemainingFraction;
                if (frac > 0.50f)      target = TimerPhase.Green;
                else if (frac > 0.25f) target = TimerPhase.Yellow;
                else                   target = TimerPhase.Red;
            }

            if (target != _currentPhase)
            {
                _currentPhase = target;
                OnTimerPhaseChanged?.Invoke(_currentPhase);
            }
        }
    }
}
