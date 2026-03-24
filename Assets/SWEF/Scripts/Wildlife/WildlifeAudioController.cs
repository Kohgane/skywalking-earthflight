using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Wildlife
{
    /// <summary>
    /// Phase 75 — Manages spatial audio for wildlife encounters.
    /// Uses one 3D AudioSource per group for performance.
    /// </summary>
    public class WildlifeAudioController : MonoBehaviour
    {
        #region Inspector

        [Header("Audio Settings")]
        [Tooltip("Maximum volume for wildlife group audio.")]
        [SerializeField, Range(0f, 1f)] private float maxGroupVolume = 0.8f;

        [Tooltip("Ambient wildlife volume when groups are nearby.")]
        [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.3f;

        [Tooltip("3D AudioSource rolloff maximum distance.")]
        [SerializeField] private float audioMaxDistance = 500f;

        #endregion

        #region Private State

        private readonly Dictionary<string, AudioSource> _groupSources = new Dictionary<string, AudioSource>();
        private float _masterVolume = 1f;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            SubscribeToManager();
        }

        private void OnDestroy()
        {
            UnsubscribeFromManager();
        }

        #endregion

        #region Manager Integration

        private void SubscribeToManager()
        {
            var mgr = WildlifeManager.Instance;
            if (mgr == null) return;
            mgr.OnGroupSpawned    += HandleGroupSpawned;
            mgr.OnGroupDespawned  += HandleGroupDespawned;
            mgr.OnBirdStrike      += HandleBirdStrike;
        }

        private void UnsubscribeFromManager()
        {
            var mgr = WildlifeManager.Instance;
            if (mgr == null) return;
            mgr.OnGroupSpawned   -= HandleGroupSpawned;
            mgr.OnGroupDespawned -= HandleGroupDespawned;
            mgr.OnBirdStrike     -= HandleBirdStrike;
        }

        private void HandleGroupSpawned(WildlifeGroupState group)  => PlayGroupAudio(group);
        private void HandleGroupDespawned(string groupId)           => StopGroupAudio(groupId);
        private void HandleBirdStrike(WildlifeSpecies s, Vector3 p) => PlayBirdStrikeAudio(p);

        #endregion

        #region Audio Playback

        /// <summary>Creates or updates the spatial audio source for a wildlife group.</summary>
        public void PlayGroupAudio(WildlifeGroupState group)
        {
            if (group == null) return;
            if (IsReducedAudioMode()) return;

            if (!_groupSources.TryGetValue(group.groupId, out var src) || src == null)
            {
                var go = new GameObject($"WildlifeAudio_{group.groupId}");
                go.transform.SetParent(transform);
                go.transform.position = group.centerPosition;
                src = go.AddComponent<AudioSource>();
                src.spatialBlend  = 1f;
                src.rolloffMode   = AudioRolloffMode.Logarithmic;
                src.maxDistance   = audioMaxDistance;
                src.loop          = true;
                src.volume        = Mathf.Clamp01(
                    maxGroupVolume * (group.memberCount / 20f) * _masterVolume);
                _groupSources[group.groupId] = src;
            }

            // Resolve audio clip via AudioManager (null-safe)
#if SWEF_AUDIO_AVAILABLE
            var am = SWEF.Audio.AudioManager.Instance;
            if (am != null)
            {
                string clipKey = GetClipKeyForCategory(group.species.category,
                    group.currentBehavior);
                var clip = am.GetClip(clipKey);
                if (clip != null && src.clip != clip)
                {
                    src.clip = clip;
                    src.Play();
                }
            }
#endif
        }

        /// <summary>Stops and removes the spatial audio source for a wildlife group.</summary>
        public void StopGroupAudio(string groupId)
        {
            if (_groupSources.TryGetValue(groupId, out var src) && src != null)
            {
                src.Stop();
                Destroy(src.gameObject);
            }
            _groupSources.Remove(groupId);
        }

        /// <summary>Sets the master ambient wildlife volume.</summary>
        public void SetAmbientWildlifeVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
        }

        private void PlayBirdStrikeAudio(Vector3 position)
        {
#if SWEF_AUDIO_AVAILABLE
            var am = SWEF.Audio.AudioManager.Instance;
            am?.PlayOneShot("bird_strike_impact", position);
#endif
        }

        #endregion

        #region Helpers

        private static string GetClipKeyForCategory(WildlifeCategory cat, WildlifeBehavior behavior)
        {
            string suffix = behavior == WildlifeBehavior.Fleeing ? "_alarm" : "_idle";
            switch (cat)
            {
                case WildlifeCategory.Bird:          return "bird_chirp"     + suffix;
                case WildlifeCategory.Raptor:        return "raptor_cry"     + suffix;
                case WildlifeCategory.Seabird:       return "seabird_squawk" + suffix;
                case WildlifeCategory.Waterfowl:     return "waterfowl_call" + suffix;
                case WildlifeCategory.MigratoryBird: return "bird_flock"     + suffix;
                case WildlifeCategory.MarineMammal:  return "whale_song"     + suffix;
                case WildlifeCategory.Fish:          return "water_ambient"  + suffix;
                case WildlifeCategory.LandMammal:    return "land_animal"    + suffix;
                case WildlifeCategory.Insect:        return "insect_swarm"   + suffix;
                default:                             return "wildlife_ambient";
            }
        }

        private bool IsReducedAudioMode()
        {
#if SWEF_ACCESSIBILITY_AVAILABLE
            var am = SWEF.Accessibility.AccessibilityManager.Instance;
            if (am != null && am.IsReducedAudioEnabled) return true;
#endif
            return false;
        }

        #endregion
    }
}
