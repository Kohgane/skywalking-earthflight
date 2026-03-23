using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace SWEF.MusicPlayer
{
    /// <summary>
    /// Sleep timer for the In-Flight Music Player.
    /// <para>
    /// Counts down from a configurable duration and gradually fades the music volume
    /// over the last 30 seconds before the timer expires.  When the timer ends, music
    /// is either paused or stopped based on <see cref="StopOnExpiry"/>.
    /// </para>
    /// <para>
    /// Timer state is preserved across app-backgrounding by recording the real time
    /// at activation via <see cref="Time.realtimeSinceStartup"/>, so the remaining
    /// time is correctly restored when the app returns to the foreground.
    /// </para>
    /// <para>
    /// Exposes <see cref="RemainingTime"/>, <see cref="IsActive"/>, <see cref="Cancel()"/>,
    /// and <see cref="SetDuration(float)"/> for UI integration.
    /// </para>
    /// </summary>
    public class MusicSleepTimer : MonoBehaviour
    {
        // ── Preset enum ───────────────────────────────────────────────────────────
        /// <summary>Built-in timer presets.</summary>
        public enum TimerPreset
        {
            /// <summary>15-minute preset.</summary>
            FifteenMinutes,
            /// <summary>30-minute preset.</summary>
            ThirtyMinutes,
            /// <summary>1-hour preset.</summary>
            OneHour,
            /// <summary>2-hour preset.</summary>
            TwoHours,
            /// <summary>User-defined duration.</summary>
            Custom
        }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Timer Settings")]
        [Tooltip("Duration of the sleep timer in minutes (used when preset is Custom).")]
        [Range(1f, 240f)]
        [SerializeField] private float customDurationMinutes = 30f;

        [Tooltip("Duration of the fade-out in seconds before the timer expires.")]
        [Range(5f, 120f)]
        [SerializeField] private float fadeOutDuration = 30f;

        [Tooltip("When true, music is stopped when the timer ends; when false, it is paused.")]
        [SerializeField] private bool stopOnExpiry = false;

        [Header("Events")]
        [Tooltip("UnityEvent fired one minute before the timer expires.")]
        public UnityEvent onOneMinuteWarning;

        [Tooltip("UnityEvent fired when the timer expires.")]
        public UnityEvent onTimerExpired;

        // ── C# Events ─────────────────────────────────────────────────────────────
        /// <summary>Fired one minute before the timer expires.</summary>
        public event Action OnOneMinuteWarning;

        /// <summary>Fired when the timer expires and music is stopped/paused.</summary>
        public event Action OnTimerExpired;

        /// <summary>Fired whenever <see cref="RemainingTime"/> changes (once per second).</summary>
        public event Action<float> OnRemainingTimeChanged;

        // ── Private state ─────────────────────────────────────────────────────────
        private bool      _isActive;
        private float     _durationSeconds;
        private float     _startRealtime;       // Time.realtimeSinceStartup at activation
        private float     _remainingSeconds;
        private bool      _warningFired;
        private bool      _fadeStarted;
        private float     _originalVolume = 1f;
        private float     _lastTickSecond;
        private Coroutine _timerCoroutine;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Whether the sleep timer is currently counting down.</summary>
        public bool IsActive => _isActive;

        /// <summary>Remaining time in seconds. 0 when the timer is inactive.</summary>
        public float RemainingTime => _isActive ? Mathf.Max(0f, _remainingSeconds) : 0f;

        /// <summary>
        /// When true, music is stopped when the timer expires; when false, it is paused.
        /// </summary>
        public bool StopOnExpiry
        {
            get => stopOnExpiry;
            set => stopOnExpiry = value;
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void OnApplicationPause(bool paused)
        {
            if (!_isActive) return;

            if (paused)
            {
                // Record snapshot so we can restore on resume
                _remainingSeconds = CalculateRemaining();
            }
            else
            {
                // Resume: recalibrate start time so countdown continues from snapshot
                _startRealtime = Time.realtimeSinceStartup - (_durationSeconds - _remainingSeconds);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Activates the sleep timer using a built-in <see cref="TimerPreset"/>.
        /// </summary>
        /// <param name="preset">The timer preset to use.</param>
        public void Activate(TimerPreset preset)
        {
            SetDuration(PresetToMinutes(preset));
            StartTimer();
        }

        /// <summary>
        /// Sets the timer duration in minutes and activates the countdown.
        /// Can be called while a timer is already running to restart it.
        /// </summary>
        /// <param name="minutes">Duration in minutes [1, 240].</param>
        public void SetDuration(float minutes)
        {
            customDurationMinutes = Mathf.Clamp(minutes, 1f, 240f);
            if (_isActive)
                StartTimer(); // restart with new duration
        }

        /// <summary>
        /// Cancels the active sleep timer and restores music volume immediately.
        /// Does nothing if the timer is not active.
        /// </summary>
        public void Cancel()
        {
            if (!_isActive) return;

            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }

            RestoreVolume();
            _isActive    = false;
            _warningFired = false;
            _fadeStarted  = false;
            OnRemainingTimeChanged?.Invoke(0f);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void StartTimer()
        {
            if (_timerCoroutine != null)
                StopCoroutine(_timerCoroutine);

            _durationSeconds = customDurationMinutes * 60f;
            _startRealtime   = Time.realtimeSinceStartup;
            _remainingSeconds = _durationSeconds;
            _warningFired    = false;
            _fadeStarted     = false;
            _isActive        = true;
            _lastTickSecond  = _durationSeconds;

            if (MusicPlayerManager.Instance != null)
            {
                AudioSource src = MusicPlayerManager.Instance.GetComponent<AudioSource>();
                _originalVolume = src != null ? src.volume : 1f;
            }

            _timerCoroutine = StartCoroutine(RunTimer());
        }

        private IEnumerator RunTimer()
        {
            while (_remainingSeconds > 0f)
            {
                _remainingSeconds = CalculateRemaining();

                // Fire second-level tick for UI
                float floorSec = Mathf.Floor(_remainingSeconds);
                if (floorSec < _lastTickSecond)
                {
                    _lastTickSecond = floorSec;
                    OnRemainingTimeChanged?.Invoke(_remainingSeconds);
                }

                // One-minute warning
                if (!_warningFired && _remainingSeconds <= 60f)
                {
                    _warningFired = true;
                    OnOneMinuteWarning?.Invoke();
                    onOneMinuteWarning?.Invoke();
                }

                // Start gradual fade-out
                if (!_fadeStarted && _remainingSeconds <= fadeOutDuration)
                {
                    _fadeStarted = true;
                    StartCoroutine(FadeOutVolume());
                }

                yield return null;
            }

            ExpireTimer();
        }

        private IEnumerator FadeOutVolume()
        {
            AudioSource src = GetMusicSource();
            if (src == null) yield break;

            float startVolume = src.volume;
            float elapsed     = 0f;

            while (elapsed < fadeOutDuration && _isActive)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                if (src != null)
                    src.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            if (src != null)
                src.volume = 0f;
        }

        private void ExpireTimer()
        {
            _isActive        = false;
            _timerCoroutine  = null;
            _remainingSeconds = 0f;

            AudioSource src = GetMusicSource();
            if (src != null)
            {
                if (stopOnExpiry)
                    src.Stop();
                else
                    src.Pause();
            }

            OnTimerExpired?.Invoke();
            onTimerExpired?.Invoke();
            OnRemainingTimeChanged?.Invoke(0f);
        }

        private void RestoreVolume()
        {
            AudioSource src = GetMusicSource();
            if (src != null)
                src.volume = _originalVolume;
        }

        private float CalculateRemaining()
        {
            float elapsed = Time.realtimeSinceStartup - _startRealtime;
            return Mathf.Max(0f, _durationSeconds - elapsed);
        }

        private AudioSource GetMusicSource()
        {
            if (MusicPlayerManager.Instance == null) return null;
            return MusicPlayerManager.Instance.GetComponent<AudioSource>();
        }

        private static float PresetToMinutes(TimerPreset preset)
        {
            switch (preset)
            {
                case TimerPreset.FifteenMinutes: return 15f;
                case TimerPreset.ThirtyMinutes:  return 30f;
                case TimerPreset.OneHour:         return 60f;
                case TimerPreset.TwoHours:        return 120f;
                default:                          return 30f;
            }
        }
    }
}
