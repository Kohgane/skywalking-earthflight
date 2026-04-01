// MusicTransitionController.cs — SWEF Dynamic Soundtrack & Adaptive Music System
using System.Collections;
using UnityEngine;

namespace SWEF.AdaptiveMusic
{
    /// <summary>
    /// Handles mood-to-mood transitions: configurable crossfade durations,
    /// minimum mood duration to prevent rapid flickering, bar-quantized transitions
    /// when a <see cref="BeatSyncClock"/> is available, and optional stinger clips
    /// on specific mood pairings.
    /// </summary>
    public class MusicTransitionController : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private AdaptiveMusicProfile _profile;
        [SerializeField] private BeatSyncClock        _beatClock;
        [SerializeField] private StemMixer            _stemMixer;

        [Header("Transition Settings")]
        [Tooltip("When true, transitions wait for the next bar boundary before starting.")]
        [SerializeField] private bool _barQuantizedTransitions = true;

        // ── Events ────────────────────────────────────────────────────────────

        public event System.Action<MusicMood, MusicMood> OnTransitionStarted;
        public event System.Action<MusicMood>            OnTransitionCompleted;

        // ── State ─────────────────────────────────────────────────────────────

        private MusicMood _currentMood = MusicMood.Peaceful;
        private MusicMood _pendingMood = MusicMood.Peaceful;
        private bool      _transitionPending;
        private float     _currentMoodStartTime;
        private bool      _isTransitioning;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            _currentMoodStartTime = Time.time;
        }

        private void OnEnable()
        {
            if (_beatClock != null)
                _beatClock.OnBar += OnBarBoundary;
        }

        private void OnDisable()
        {
            if (_beatClock != null)
                _beatClock.OnBar -= OnBarBoundary;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Current active mood.</summary>
        public MusicMood CurrentMood => _currentMood;

        /// <summary>
        /// Requests a transition to <paramref name="newMood"/>.
        /// The request is queued and will fire after the minimum mood duration has elapsed.
        /// If <paramref name="newMood"/> is the same as the current mood, this is a no-op.
        /// </summary>
        public void RequestTransition(MusicMood newMood)
        {
            if (newMood == _currentMood)
                return;

            _pendingMood      = newMood;
            _transitionPending = true;

            // Check if the minimum duration has already passed
            float elapsed = Time.time - _currentMoodStartTime;
            float minDuration = _profile != null ? _profile.minimumMoodDuration : 8f;
            if (elapsed >= minDuration && !_isTransitioning)
            {
                if (_barQuantizedTransitions && _beatClock != null)
                {
                    // Wait for the next bar boundary (handled in OnBarBoundary)
                }
                else
                {
                    ExecuteTransition();
                }
            }
        }

        /// <summary>
        /// Forces an immediate transition to <paramref name="newMood"/>, bypassing
        /// minimum duration and bar quantization.
        /// </summary>
        public void ForceTransition(MusicMood newMood)
        {
            if (newMood == _currentMood)
                return;

            _pendingMood       = newMood;
            _transitionPending = true;
            ExecuteTransition();
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void OnBarBoundary()
        {
            if (!_transitionPending || _isTransitioning)
                return;

            float elapsed    = Time.time - _currentMoodStartTime;
            float minDuration = _profile != null ? _profile.minimumMoodDuration : 8f;
            if (elapsed >= minDuration)
                ExecuteTransition();
        }

        private void ExecuteTransition()
        {
            if (!_transitionPending)
                return;

            _transitionPending = false;
            _isTransitioning   = true;

            float crossfadeDuration = _profile != null
                ? _profile.GetCrossfadeDuration(_currentMood, _pendingMood)
                : 3f;

            string stingerPath = _profile != null
                ? _profile.GetStingerPath(_currentMood, _pendingMood)
                : string.Empty;

            MusicMood from = _currentMood;
            MusicMood to   = _pendingMood;

            OnTransitionStarted?.Invoke(from, to);

            if (!string.IsNullOrEmpty(stingerPath) && _stemMixer != null)
                _stemMixer.PlayStinger(stingerPath);

            _currentMood          = to;
            _currentMoodStartTime = Time.time;

            StartCoroutine(TransitionCoroutine(from, to, crossfadeDuration));
        }

        private IEnumerator TransitionCoroutine(MusicMood from, MusicMood to, float duration)
        {
            yield return new WaitForSeconds(duration);
            _isTransitioning = false;
            OnTransitionCompleted?.Invoke(to);
        }
    }
}
