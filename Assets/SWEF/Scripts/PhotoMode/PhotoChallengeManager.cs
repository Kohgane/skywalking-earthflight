using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.PhotoMode
{
    /// <summary>Photo challenge and mission system for in-game photography objectives.</summary>
    public class PhotoChallengeManager : MonoBehaviour
    {
        #region Enums

        /// <summary>Lifecycle state of a photo challenge.</summary>
        public enum PhotoChallengeState
        {
            Locked,
            Available,
            Active,
            Submitted,
            Completed,
            Failed
        }

        #endregion

        #region Inner Types

        /// <summary>Criteria that a captured photo must satisfy to complete the challenge.</summary>
        [Serializable]
        public class ChallengeCriteria
        {
            [Tooltip("Minimum altitude in metres. 0 = any.")]
            public float minAltitude;

            [Tooltip("Maximum altitude in metres. 0 = any.")]
            public float maxAltitude;

            [Tooltip("Required PhotoFilter. None = any.")]
            public PhotoFilter requiredFilter;

            [Tooltip("Required weather condition keyword (e.g. 'storm', 'clear'). Empty = any.")]
            public string requiredWeather;

            [Tooltip("Required location tag keyword. Empty = any.")]
            public string requiredLocationTag;

            [Tooltip("Whether the subject must include a specific landmark name.")]
            public string requiredLandmark;
        }

        /// <summary>Definition data for a single photo challenge.</summary>
        [Serializable]
        public class PhotoChallengeData
        {
            public string              challengeId;
            public string              title;
            public string              description;
            public ChallengeCriteria   criteria;
            public int                 rewardPoints;
            public int                 difficulty;         // 1 (easy) – 5 (expert)
            public bool                isDaily;
            public bool                isWeekly;
            public string              expiresUtc;         // ISO-8601 or empty

            [NonSerialized] public PhotoChallengeState State;
            [NonSerialized] public float               Score;          // 0-100 after evaluation
        }

        [Serializable]
        private class SaveData
        {
            public List<ChallengeSaveEntry> entries = new List<ChallengeSaveEntry>();
        }

        [Serializable]
        private class ChallengeSaveEntry
        {
            public string challengeId;
            public string state;
            public float  score;
        }

        #endregion

        #region Events

        /// <summary>Fired when a challenge becomes active.</summary>
        public static event Action<PhotoChallengeData> OnChallengeActivated;

        /// <summary>Fired when a challenge is evaluated and marked completed.</summary>
        public static event Action<PhotoChallengeData> OnChallengeCompleted;

        /// <summary>Fired when the reward for a completed challenge is claimed.</summary>
        public static event Action<PhotoChallengeData> OnChallengeRewardClaimed;

        #endregion

        #region Inspector

        [Header("Challenge Definitions")]
        [SerializeField, Tooltip("All available photo challenges.")]
        private List<PhotoChallengeData> _challenges = new List<PhotoChallengeData>();

        [Header("Persistence")]
        [SerializeField, Tooltip("File name for challenge progress JSON within persistentDataPath.")]
        private string _saveFileName = "photo_challenges.json";

        #endregion

        #region Private State

        private string SavePath =>
            Path.Combine(Application.persistentDataPath, _saveFileName);

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            LoadProgress();
        }

        #endregion

        #region Public API

        /// <summary>Return all challenges matching the given state.</summary>
        public List<PhotoChallengeData> GetChallengesByState(PhotoChallengeState state)
        {
            var results = new List<PhotoChallengeData>();
            foreach (var c in _challenges)
                if (c.State == state) results.Add(c);
            return results;
        }

        /// <summary>Activate a challenge so the player is working towards it.</summary>
        public bool ActivateChallenge(string challengeId)
        {
            var challenge = Find(challengeId);
            if (challenge == null || challenge.State != PhotoChallengeState.Available)
                return false;

            challenge.State = PhotoChallengeState.Active;
            SaveProgress();
            OnChallengeActivated?.Invoke(challenge);
            return true;
        }

        /// <summary>Submit a captured photo for evaluation against the active challenge.</summary>
        public float EvaluateSubmission(string challengeId, PhotoMetadata metadata)
        {
            var challenge = Find(challengeId);
            if (challenge == null || challenge.State != PhotoChallengeState.Active)
                return 0f;

            float score = ScoreSubmission(challenge.criteria, metadata);
            challenge.Score = score;

            challenge.State = score >= 50f
                ? PhotoChallengeState.Completed
                : PhotoChallengeState.Failed;

            SaveProgress();

            if (challenge.State == PhotoChallengeState.Completed)
                OnChallengeCompleted?.Invoke(challenge);

            return score;
        }

        /// <summary>Claim the reward for a completed challenge.</summary>
        public bool ClaimReward(string challengeId)
        {
            var challenge = Find(challengeId);
            if (challenge == null || challenge.State != PhotoChallengeState.Completed)
                return false;

            // Mark so it can't be claimed twice
            challenge.State = PhotoChallengeState.Submitted;
            SaveProgress();
            OnChallengeRewardClaimed?.Invoke(challenge);
            return true;
        }

        /// <summary>Unlock a locked challenge (e.g. after an achievement or progression gate).</summary>
        public void UnlockChallenge(string challengeId)
        {
            var challenge = Find(challengeId);
            if (challenge != null && challenge.State == PhotoChallengeState.Locked)
            {
                challenge.State = PhotoChallengeState.Available;
                SaveProgress();
            }
        }

        /// <summary>All registered challenges.</summary>
        public IReadOnlyList<PhotoChallengeData> AllChallenges => _challenges;

        #endregion

        #region Scoring

        private float ScoreSubmission(ChallengeCriteria criteria, PhotoMetadata metadata)
        {
            float score     = 100f;
            float altMetres = (float)metadata.altitude;

            // Altitude check
            if (criteria.minAltitude > 0 && altMetres < criteria.minAltitude)
                score -= 25f;
            if (criteria.maxAltitude > 0 && altMetres > criteria.maxAltitude)
                score -= 25f;

            // Filter check — compare against cameraSettings.filter (enum)
            if (criteria.requiredFilter != PhotoFilter.None &&
                metadata.cameraSettings != null &&
                metadata.cameraSettings.filter != criteria.requiredFilter)
                score -= 20f;

            // Weather check
            if (!string.IsNullOrEmpty(criteria.requiredWeather) &&
                !string.IsNullOrEmpty(metadata.weatherCondition) &&
                !metadata.weatherCondition.ToLowerInvariant()
                    .Contains(criteria.requiredWeather.ToLowerInvariant()))
                score -= 20f;

            // Location / landmark check — match against tags list
            string tagsJoined = metadata.tags != null
                ? string.Join(" ", metadata.tags).ToLowerInvariant()
                : string.Empty;

            if (!string.IsNullOrEmpty(criteria.requiredLocationTag) &&
                !tagsJoined.Contains(criteria.requiredLocationTag.ToLowerInvariant()))
                score -= 15f;

            if (!string.IsNullOrEmpty(criteria.requiredLandmark) &&
                !tagsJoined.Contains(criteria.requiredLandmark.ToLowerInvariant()))
                score -= 15f;

            return Mathf.Clamp(score, 0f, 100f);
        }

        #endregion

        #region Persistence

        private void SaveProgress()
        {
            var save = new SaveData();
            foreach (var c in _challenges)
            {
                save.entries.Add(new ChallengeSaveEntry
                {
                    challengeId = c.challengeId,
                    state       = c.State.ToString(),
                    score       = c.Score
                });
            }

            try
            {
                File.WriteAllText(SavePath, JsonUtility.ToJson(save, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PhotoChallengeManager] Save failed: {ex.Message}");
            }
        }

        private void LoadProgress()
        {
            if (!File.Exists(SavePath)) return;

            try
            {
                string json = File.ReadAllText(SavePath);
                var save = JsonUtility.FromJson<SaveData>(json);
                foreach (var entry in save.entries)
                {
                    var challenge = Find(entry.challengeId);
                    if (challenge == null) continue;
                    if (Enum.TryParse(entry.state, out PhotoChallengeState state))
                        challenge.State = state;
                    challenge.Score = entry.score;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PhotoChallengeManager] Load failed: {ex.Message}");
            }
        }

        private PhotoChallengeData Find(string id)
        {
            foreach (var c in _challenges)
                if (c.challengeId == id) return c;
            return null;
        }

        #endregion
    }
}
