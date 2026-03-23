// NotificationSoundController.cs — SWEF Achievement Notification & Popup System

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.AchievementNotification
{
    /// <summary>
    /// MonoBehaviour that manages audio playback for achievement unlock events.
    ///
    /// <para>Features:
    /// <list type="bullet">
    ///   <item>Tier-specific unlock sound selection (Bronze → Secret escalating fanfare).</item>
    ///   <item>Reward-specific chime sound.</item>
    ///   <item>Sound queuing to prevent multiple clips from overlapping.</item>
    /// </list>
    /// </para>
    ///
    /// <para>Assign <see cref="AudioClip"/> references per tier in the Inspector.
    /// Falls back to the Bronze clip if a tier-specific clip is not assigned.</para>
    /// </summary>
    public class NotificationSoundController : MonoBehaviour
    {
        #region Inspector

        [Header("Unlock Sounds (by tier)")]
        [Tooltip("Unlock sound played for Bronze-tier achievements.")]
        [SerializeField] private AudioClip bronzeUnlockClip;

        [Tooltip("Unlock sound played for Silver-tier achievements.")]
        [SerializeField] private AudioClip silverUnlockClip;

        [Tooltip("Unlock sound played for Gold-tier achievements.")]
        [SerializeField] private AudioClip goldUnlockClip;

        [Tooltip("Unlock sound played for Platinum-tier achievements.")]
        [SerializeField] private AudioClip platinumUnlockClip;

        [Tooltip("Unlock sound played for Secret-tier achievements (grand fanfare).")]
        [SerializeField] private AudioClip secretUnlockClip;

        [Header("Reward Sound")]
        [Tooltip("Chime sound played when a reward counter completes.")]
        [SerializeField] private AudioClip rewardChimeClip;

        [Header("Audio Source")]
        [Tooltip("AudioSource used for all notification sounds. Created automatically if null.")]
        [SerializeField] private AudioSource audioSource;

        [Tooltip("Volume applied to all notification sounds.")]
        [SerializeField] [Range(0f, 1f)] private float volume = 0.9f;

        [Header("Queue")]
        [Tooltip("Minimum time (seconds) between consecutive sounds to prevent overlap.")]
        [SerializeField] [Range(0.1f, 2f)] private float minTimeBetweenSounds = 0.3f;

        #endregion

        #region Private state

        private readonly Queue<AudioClip> _soundQueue = new Queue<AudioClip>();
        private Coroutine                 _playbackCoroutine;
        private float                     _lastPlayTime;

        #endregion

        #region Unity lifecycle

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource           = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2-D UI sound
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Enqueues and plays the tier-appropriate unlock fanfare.
        /// Sounds are queued so consecutive unlocks never overlap.
        /// </summary>
        /// <param name="tier">Tier of the unlocked achievement.</param>
        public void PlayUnlockSound(AchievementTier tier)
        {
            var clip = GetClipForTier(tier);
            if (clip != null)
                EnqueueSound(clip);
        }

        /// <summary>Plays the reward chime sound used after the counter animation.</summary>
        public void PlayRewardSound()
        {
            if (rewardChimeClip != null)
                EnqueueSound(rewardChimeClip);
        }

        #endregion

        #region Sound queuing

        private void EnqueueSound(AudioClip clip)
        {
            _soundQueue.Enqueue(clip);
            if (_playbackCoroutine == null)
                _playbackCoroutine = StartCoroutine(PlaybackRoutine());
        }

        private IEnumerator PlaybackRoutine()
        {
            while (_soundQueue.Count > 0)
            {
                // Respect minimum gap.
                float wait = minTimeBetweenSounds - (Time.realtimeSinceStartup - _lastPlayTime);
                if (wait > 0f)
                    yield return new WaitForSeconds(wait);

                var clip = _soundQueue.Dequeue();
                if (clip != null && audioSource != null)
                {
                    audioSource.PlayOneShot(clip, volume);
                    _lastPlayTime = Time.realtimeSinceStartup;
                    yield return new WaitForSeconds(clip.length);
                }
            }
            _playbackCoroutine = null;
        }

        #endregion

        #region Helpers

        private AudioClip GetClipForTier(AchievementTier tier)
        {
            switch (tier)
            {
                case AchievementTier.Silver:   return silverUnlockClip   ?? bronzeUnlockClip;
                case AchievementTier.Gold:     return goldUnlockClip     ?? bronzeUnlockClip;
                case AchievementTier.Platinum: return platinumUnlockClip ?? bronzeUnlockClip;
                case AchievementTier.Secret:   return secretUnlockClip   ?? bronzeUnlockClip;
                default:                       return bronzeUnlockClip;
            }
        }

        #endregion
    }
}
