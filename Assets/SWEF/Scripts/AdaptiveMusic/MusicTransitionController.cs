// MusicTransitionController.cs — SWEF Dynamic Soundtrack & Adaptive Music System (Phase 83)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.AdaptiveMusic
{
    /// <summary>
    /// Handles mood-to-mood transitions with:
    /// <list type="bullet">
    ///   <item>Per-pair configurable crossfade durations.</item>
    ///   <item>Minimum mood hold time to prevent rapid flicker (anti-flicker).</item>
    ///   <item>Optional stinger AudioSource clip played at the moment of transition.</item>
    ///   <item>Bar-quantised transition timing when a <see cref="BeatSyncClock"/> is available.</item>
    /// </list>
    /// </summary>
    public class MusicTransitionController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("References")]
        [Tooltip("Optional BeatSyncClock for bar-quantised transitions.")]
        [SerializeField] private BeatSyncClock beatSyncClock;

        [Tooltip("AudioSource used for stinger clips.")]
        [SerializeField] private AudioSource stingerSource;

        [Header("Configuration")]
        [Tooltip("Profile defining crossfade durations and stinger clips.")]
        [SerializeField] private AdaptiveMusicProfile profile;

        [Tooltip("When true, transitions are delayed until the next bar boundary.")]
        [SerializeField] private bool useBarQuantisedTransitions = true;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Raised when a mood transition begins. Parameters: (fromMood, toMood, crossfadeDuration).
        /// </summary>
        public event Action<MusicMood, MusicMood, float> OnTransitionStarted;

        // ── State ─────────────────────────────────────────────────────────────────
        private MusicMood  _currentMood = MusicMood.Peaceful;
        private MusicMood  _pendingMood;
        private bool       _hasPending;
        private float      _moodHeldFor;          // seconds the current mood has been active
        private float      _minimumHoldTime = 8f; // from profile

        private Coroutine  _pendingTransitionCoroutine;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (stingerSource == null)
            {
                GameObject go = new GameObject("StingerSource");
                go.transform.SetParent(transform, false);
                stingerSource = go.AddComponent<AudioSource>();
                stingerSource.playOnAwake  = false;
                stingerSource.spatialBlend = 0f;
            }

            if (profile != null)
                _minimumHoldTime = profile.minimumMoodHoldTime;
        }

        private void Update()
        {
            _moodHeldFor += Time.deltaTime;

            if (_hasPending && _moodHeldFor >= _minimumHoldTime)
            {
                if (_pendingTransitionCoroutine == null)
                    _pendingTransitionCoroutine = StartCoroutine(BeginTransition(_pendingMood));
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Current mood that is actively playing.</summary>
        public MusicMood CurrentMood => _currentMood;

        /// <summary>
        /// Requests a transition to <paramref name="newMood"/>.
        /// If the current mood has not yet held for <c>minimumMoodHoldTime</c> seconds the
        /// request is queued; the queue always holds the most-recently-requested mood (last wins).
        /// Ignored if <paramref name="newMood"/> equals the current mood.
        /// </summary>
        public void RequestMood(MusicMood newMood)
        {
            if (newMood == _currentMood && !_hasPending)
                return;

            _pendingMood = newMood;
            _hasPending  = true;
        }

        /// <summary>
        /// Sets the profile. Can be called at runtime to switch music profiles.
        /// </summary>
        public void SetProfile(AdaptiveMusicProfile newProfile)
        {
            profile          = newProfile;
            _minimumHoldTime = newProfile != null ? newProfile.minimumMoodHoldTime : 8f;
        }

        // ── Internals ─────────────────────────────────────────────────────────────

        private IEnumerator BeginTransition(MusicMood target)
        {
            MusicMood from     = _currentMood;
            float     duration = profile != null
                ? profile.GetCrossfadeDuration(from, target)
                : 3f;

            // Wait for next bar boundary if requested
            if (useBarQuantisedTransitions && beatSyncClock != null)
            {
                double nextBar = beatSyncClock.GetNextBarTime();
                double waitSec = nextBar - AudioSettings.dspTime;
                if (waitSec > 0.0 && waitSec < 10.0)
                    yield return new WaitForSeconds((float)waitSec);
            }

            // Play stinger if configured
            if (profile != null)
            {
                AudioClip stinger = profile.GetStinger(from, target);
                if (stinger != null && stingerSource != null)
                {
                    stingerSource.PlayOneShot(stinger);
                }
            }

            _currentMood   = target;
            _moodHeldFor   = 0f;
            _hasPending    = false;
            _pendingTransitionCoroutine = null;

            OnTransitionStarted?.Invoke(from, target, duration);
        }
    }
}
