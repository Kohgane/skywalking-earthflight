// StartBoostController.cs — SWEF Boost & Drift Mechanics System (Phase 62)
using System;
using UnityEngine;

namespace SWEF.Racing
{
    /// <summary>
    /// Phase 62 — Manages the race-start countdown timing-window boost system
    /// (inspired by Mario Kart's start boost mechanic).
    ///
    /// <para>The controller tracks a configurable countdown (3-2-1-GO) and evaluates
    /// the player's throttle press timing relative to the GO signal.
    /// Timing zones: too early = engine stall penalty, perfect = mega boost,
    /// good = normal boost, late or no input = no boost.</para>
    ///
    /// <para>Connect to the race start system by calling
    /// <see cref="BeginCountdown"/> from the race manager and
    /// <see cref="RegisterThrottleInput"/> from the input layer.</para>
    /// </summary>
    public class StartBoostController : MonoBehaviour
    {
        #region Inspector

        [Header("Timing Windows (seconds offset from GO signal)")]
        [Tooltip("Half-window for a Perfect start (±this value from GO).")]
        [SerializeField] private float perfectWindow = 0.1f;

        [Tooltip("Half-window for a Good start (±this value from GO).")]
        [SerializeField] private float goodWindow = 0.3f;

        [Tooltip("Duration of the engine-stall penalty in seconds.")]
        [SerializeField] private float stallDuration = 0.5f;

        [Header("Countdown")]
        [Tooltip("Total countdown duration in seconds (e.g. 3 seconds for 3-2-1-GO).")]
        [SerializeField] private float countdownDuration = 3f;

        [Header("Boost Configs")]
        [Tooltip("Boost config granted for a Perfect start.")]
        [SerializeField] private BoostConfig perfectBoost;

        [Tooltip("Boost config granted for a Good start.")]
        [SerializeField] private BoostConfig goodBoost;

        [Tooltip("Boost config granted for an Ok start.")]
        [SerializeField] private BoostConfig okBoost;

        [Header("Rev Meter")]
        [Tooltip("Minimum throttle press value required to register a start-boost attempt.")]
        [SerializeField] private float minThrottleThreshold = 0.8f;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the countdown completes and a start-boost grade is determined.
        /// Passes the grade and the boost config applied (may be null for Miss/Stall).
        /// </summary>
        public event Action<StartBoostGrade, BoostConfig> OnStartBoostResult;

        /// <summary>Fired each time the countdown integer value advances (3 → 2 → 1 → GO).</summary>
        public event Action<int> OnCountdownTick;

        /// <summary>Fired when the GO signal is issued.</summary>
        public event Action OnGoSignal;

        #endregion

        #region Public Properties

        /// <summary>Whether the countdown is currently running.</summary>
        public bool IsCountdownActive { get; private set; }

        /// <summary>Normalised countdown progress 0 (start) → 1 (GO), driven by <see cref="BeginCountdown"/>.</summary>
        public float CountdownProgress { get; private set; }

        /// <summary>
        /// Normalised rev meter level (0–1) based on current throttle input.
        /// Bind to a rev meter UI widget.
        /// </summary>
        public float RevMeterNormalized { get; private set; }

        /// <summary>Whether the engine stall penalty is currently active.</summary>
        public bool IsStalled { get; private set; }

        #endregion

        #region Private State

        private float _countdownTimer;
        private bool  _goFired;
        private float _goFireTime;
        private bool  _resultFired;
        private float _throttleAtInput = -1f;
        private float _inputTime       = float.MinValue;
        private float _stallTimer;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (IsCountdownActive)
                TickCountdown(Time.deltaTime);

            if (IsStalled)
            {
                _stallTimer -= Time.deltaTime;
                if (_stallTimer <= 0f) IsStalled = false;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Starts the countdown sequence.
        /// Call from the race manager when the loading/lobby phase ends.
        /// </summary>
        public void BeginCountdown()
        {
            _countdownTimer = 0f;
            _goFired        = false;
            _resultFired    = false;
            _throttleAtInput = -1f;
            _inputTime       = float.MinValue;
            CountdownProgress = 0f;
            IsCountdownActive = true;
        }

        /// <summary>
        /// Resets the controller to its idle state (e.g. between races).
        /// </summary>
        public void Reset()
        {
            IsCountdownActive = false;
            IsStalled         = false;
            CountdownProgress = 0f;
            RevMeterNormalized = 0f;
            _goFired          = false;
            _resultFired      = false;
        }

        /// <summary>
        /// Registers the player's throttle press value.
        /// Call from the input layer whenever the throttle axis changes.
        /// </summary>
        /// <param name="throttleValue">0–1 normalised throttle value.</param>
        public void RegisterThrottleInput(float throttleValue)
        {
            RevMeterNormalized = Mathf.Clamp01(throttleValue);

            if (!IsCountdownActive) return;
            if (throttleValue < minThrottleThreshold) return;
            if (_throttleAtInput >= 0f) return; // Already registered once.

            _throttleAtInput = throttleValue;
            _inputTime       = Time.time;

            // If pressed before GO — evaluate immediately for early stall.
            if (!_goFired)
            {
                float timeUntilGo = countdownDuration - _countdownTimer;
                if (timeUntilGo > goodWindow)
                {
                    // Too early — stall.
                    IsStalled   = true;
                    _stallTimer = stallDuration;
                    FireResult(StartBoostGrade.Stall, null);
                }
                // Otherwise we wait until GO to evaluate.
            }
        }

        #endregion

        #region Private Helpers

        private void TickCountdown(float dt)
        {
            float prevTime  = _countdownTimer;
            _countdownTimer += dt;
            CountdownProgress = Mathf.Clamp01(_countdownTimer / countdownDuration);

            // Fire integer tick events (3, 2, 1).
            int prevTick = Mathf.FloorToInt(prevTime);
            int currTick = Mathf.FloorToInt(_countdownTimer);
            if (currTick != prevTick && currTick <= 3)
                OnCountdownTick?.Invoke(3 - currTick + 1);

            // Fire GO at end.
            if (!_goFired && _countdownTimer >= countdownDuration)
            {
                _goFired    = true;
                _goFireTime = Time.time;
                OnGoSignal?.Invoke();

                // Evaluate result if the player already pressed.
                if (!_resultFired && _throttleAtInput >= 0f)
                    EvaluateResult();
            }

            // Late window — if no press by 0.3 s after GO, it's a Miss.
            if (_goFired && !_resultFired && Time.time > _goFireTime + goodWindow)
            {
                if (_throttleAtInput < 0f)
                    FireResult(StartBoostGrade.Miss, null);
                else
                    EvaluateResult();
            }
        }

        private void EvaluateResult()
        {
            if (_resultFired) return;
            if (IsStalled) return; // Already fired stall.

            float offset = Mathf.Abs(_inputTime - _goFireTime);
            StartBoostGrade grade;
            BoostConfig     reward;

            if (offset <= perfectWindow)
            {
                grade  = StartBoostGrade.Perfect;
                reward = perfectBoost;
            }
            else if (offset <= goodWindow)
            {
                grade  = StartBoostGrade.Good;
                reward = goodBoost;
            }
            else
            {
                grade  = StartBoostGrade.Ok;
                reward = okBoost;
            }

            FireResult(grade, reward);
        }

        private void FireResult(StartBoostGrade grade, BoostConfig reward)
        {
            _resultFired      = true;
            IsCountdownActive = false;

            if (reward != null && BoostController.Instance != null)
                BoostController.Instance.ApplyBoost(reward);

            OnStartBoostResult?.Invoke(grade, reward);
        }

        #endregion
    }
}
