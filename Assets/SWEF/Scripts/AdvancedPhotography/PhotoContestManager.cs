// PhotoContestManager.cs — SWEF Advanced Photography & Drone Camera System (Phase 89)
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if SWEF_ACHIEVEMENT_AVAILABLE
using SWEF.Achievement;
#endif

namespace SWEF.AdvancedPhotography
{
    // ── Contest state ─────────────────────────────────────────────────────────────

    /// <summary>Lifecycle state of a photo contest.</summary>
    public enum ContestState { Upcoming, Active, Judging, Complete }

    // ── Submission ────────────────────────────────────────────────────────────────

    /// <summary>A single player's submission to a photo contest.</summary>
    [Serializable]
    public class ContestSubmission
    {
        public string submissionId = "";
        public string contestId    = "";
        public PhotoMetadata metadata;
        public float aiScore;
        public int   voteCount;
        public float finalScore;
    }

    // ── Contest runtime ───────────────────────────────────────────────────────────

    /// <summary>Runtime state wrapper for an active <see cref="PhotoChallenge"/>.</summary>
    [Serializable]
    public class ActiveContest
    {
        public PhotoChallenge challenge;
        public ContestState   state      = ContestState.Upcoming;
        public List<ContestSubmission> submissions = new List<ContestSubmission>();
    }

    // ── Manager ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Phase 89 — Singleton MonoBehaviour that manages community photo contests.
    ///
    /// <para>Handles contest lifecycle (Upcoming → Active → Judging → Complete),
    /// AI-scored submissions, simulated community voting, and leaderboard persistence.</para>
    /// </summary>
    public sealed class PhotoContestManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static PhotoContestManager Instance { get; private set; }

        #endregion

        #region Events

        /// <summary>Fired when a contest transitions to Active state.</summary>
        public event Action<ActiveContest> OnContestStarted;

        /// <summary>Fired when a photo is successfully submitted.</summary>
        public event Action<ContestSubmission> OnPhotoSubmitted;

        /// <summary>Fired when a contest transitions to Judging state.</summary>
        public event Action<ActiveContest> OnContestEnded;

        /// <summary>Fired when final results are available (Complete state).</summary>
        public event Action<ActiveContest> OnResultsAvailable;

        #endregion

        #region Inspector

        [Header("Contests")]
        [Tooltip("Photo challenges loaded at start. Assign via Inspector or load from Resources.")]
        [SerializeField] private List<PhotoChallenge> _challenges = new List<PhotoChallenge>();

        #endregion

        #region Private State

        private List<ActiveContest> _contests = new List<ActiveContest>();
        private string _savePath;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _savePath = Path.Combine(Application.persistentDataPath, "PhotoContests");
            Directory.CreateDirectory(_savePath);

            foreach (var challenge in _challenges)
                _contests.Add(new ActiveContest { challenge = challenge, state = ContestState.Upcoming });
        }

        private void Start()
        {
            ActivateDueContests();
        }

        #endregion

        #region Public API

        /// <summary>Returns all currently active contests.</summary>
        public List<ActiveContest> GetActiveContests()
        {
            var result = new List<ActiveContest>();
            foreach (var c in _contests)
                if (c.state == ContestState.Active) result.Add(c);
            return result;
        }

        /// <summary>
        /// Submits a photo to the contest matching <paramref name="metadata"/>'s subject.
        /// The submission is AI-scored based on composition, subject match, and criteria.
        /// </summary>
        public ContestSubmission SubmitPhoto(PhotoMetadata metadata, Texture2D texture)
        {
            if (metadata == null) return null;

            ActiveContest contest = FindBestContest(metadata);
            if (contest == null)
            {
                Debug.LogWarning("[SWEF] PhotoContestManager: no active contest found for submission.");
                return null;
            }

            // Check submission limit per player (simplified: no player ID tracking here)
            if (contest.submissions.Count >= AdvancedPhotographyConfig.ContestMaxSubmissionsPerPlayer * 10)
            {
                Debug.LogWarning("[SWEF] PhotoContestManager: contest submission limit reached.");
                return null;
            }

            float aiScore = ScoreSubmission(metadata, contest.challenge);

            var submission = new ContestSubmission
            {
                submissionId = Guid.NewGuid().ToString(),
                contestId    = contest.challenge.challengeId,
                metadata     = metadata,
                aiScore      = aiScore,
                voteCount    = 0,
                finalScore   = aiScore
            };

            contest.submissions.Add(submission);
            SaveContest(contest);

            OnPhotoSubmitted?.Invoke(submission);
            AdvancedPhotographyAnalytics.RecordPhotoSubmittedToContest(contest.challenge.challengeId);
            Debug.Log($"[SWEF] PhotoContestManager: photo submitted to '{contest.challenge.title}', score={aiScore:0.00}");

            return submission;
        }

        /// <summary>Returns submissions for a contest, sorted by final score descending.</summary>
        public List<ContestSubmission> GetLeaderboard(string contestId)
        {
            var contest = _contests.Find(c => c.challenge.challengeId == contestId);
            if (contest == null) return new List<ContestSubmission>();

            var sorted = new List<ContestSubmission>(contest.submissions);
            sorted.Sort((a, b) => b.finalScore.CompareTo(a.finalScore));
            return sorted;
        }

        /// <summary>Adds a simulated community vote to a submission.</summary>
        public void VoteForPhoto(string submissionId)
        {
            foreach (var contest in _contests)
            {
                var sub = contest.submissions.Find(s => s.submissionId == submissionId);
                if (sub != null)
                {
                    sub.voteCount++;
                    sub.finalScore = sub.aiScore * 0.7f + (sub.voteCount / 100f) * 0.3f;
                    SaveContest(contest);
                    return;
                }
            }
        }

        /// <summary>Returns the result submissions for a completed contest.</summary>
        public List<ContestSubmission> GetContestResults(string contestId)
        {
            return GetLeaderboard(contestId);
        }

        #endregion

        #region Private — Contest Management

        private void ActivateDueContests()
        {
            foreach (var contest in _contests)
            {
                if (contest.state == ContestState.Upcoming)
                {
                    contest.state = ContestState.Active;
                    OnContestStarted?.Invoke(contest);
                    Debug.Log($"[SWEF] PhotoContestManager: contest '{contest.challenge.title}' activated");
                }
            }
        }

        private ActiveContest FindBestContest(PhotoMetadata metadata)
        {
            ActiveContest best = null;

            foreach (var c in _contests)
            {
                if (c.state != ContestState.Active) continue;

                // Subject match
                if (c.challenge.targetSubject == PhotoSubject.Landscape ||
                    (metadata.subjects != null && metadata.subjects.Contains(c.challenge.targetSubject)))
                {
                    best = c;
                    break;
                }
            }

            // Fallback to first active contest
            if (best == null)
                best = _contests.Find(c => c.state == ContestState.Active);

            return best;
        }

        private float ScoreSubmission(PhotoMetadata metadata, PhotoChallenge challenge)
        {
            float score = 0f;

            // Composition score (40 %)
            score += metadata.compositionScore * 0.4f;

            // Subject match (30 %)
            bool subjectMatch = metadata.subjects != null &&
                                metadata.subjects.Contains(challenge.targetSubject);
            score += (subjectMatch ? 1f : 0f) * 0.3f;

            // Biome match (20 %)
            if (!string.IsNullOrEmpty(challenge.targetBiome))
                score += (metadata.biome == challenge.targetBiome ? 1f : 0f) * 0.2f;
            else
                score += 0.2f; // full score when biome is irrelevant

            // Random creativity factor (10 %)
            score += UnityEngine.Random.Range(0f, 0.1f);

            return Mathf.Clamp01(score);
        }

        private void SaveContest(ActiveContest contest)
        {
            try
            {
                string json = JsonUtility.ToJson(contest, prettyPrint: false);
                string path = Path.Combine(_savePath, $"{contest.challenge.challengeId}.json");
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] PhotoContestManager: save failed — {ex.Message}");
            }
        }

        private void FinaliseContest(ActiveContest contest)
        {
            contest.state = ContestState.Judging;
            OnContestEnded?.Invoke(contest);

            // Simulate judging delay
            contest.state = ContestState.Complete;
            OnResultsAvailable?.Invoke(contest);
            SaveContest(contest);

#if SWEF_ACHIEVEMENT_AVAILABLE
            AchievementManager.Instance?.TryUnlock("photo_contest_winner");
#endif
        }

        #endregion
    }
}
