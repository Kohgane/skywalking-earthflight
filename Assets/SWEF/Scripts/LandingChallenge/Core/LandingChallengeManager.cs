// LandingChallengeManager.cs — Phase 120: Precision Landing Challenge System
// Central manager singleton. DontDestroyOnLoad.
// Manages challenge creation, scoring, leaderboards, and progression.
// Namespace: SWEF.LandingChallenge

using System;
using System.Collections.Generic;
using UnityEngine;

#if SWEF_LANDING_CHALLENGE_AVAILABLE
using SWEF.Analytics;
using SWEF.Achievement;
#endif

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Central singleton that manages the full lifecycle of the
    /// Precision Landing Challenge System: challenge registration, session
    /// orchestration, scoring, progression persistence, and reward dispatch.
    /// Survives scene loads via <c>DontDestroyOnLoad</c>.
    /// </summary>
    public class LandingChallengeManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────

        /// <summary>Shared singleton instance.</summary>
        public static LandingChallengeManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Configuration")]
        [SerializeField] private LandingChallengeConfig config;

        [Header("Sub-systems")]
        [SerializeField] private LandingScoringEngine scoringEngine;
        [SerializeField] private LandingProgressionSystem progressionSystem;
        [SerializeField] private LandingLeaderboardManager leaderboardManager;

        // ── State ─────────────────────────────────────────────────────────────

        private readonly List<ChallengeDefinition>       _challenges    = new List<ChallengeDefinition>();
        private readonly Dictionary<string, ChallengeProgress> _progress = new Dictionary<string, ChallengeProgress>();
        private ChallengeDefinition                       _activeChallenge;
        private ChallengeStatus                          _sessionStatus = ChallengeStatus.Available;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised when a challenge session starts.</summary>
        public event Action<ChallengeDefinition> OnChallengeStarted;

        /// <summary>Raised when a landing result is available.</summary>
        public event Action<LandingResult> OnLandingScored;

        /// <summary>Raised when a challenge is completed (passed).</summary>
        public event Action<ChallengeDefinition, LandingResult> OnChallengeCompleted;

        /// <summary>Raised when a challenge attempt fails.</summary>
        public event Action<ChallengeDefinition> OnChallengeFailed;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Currently active challenge definition, or <c>null</c> if none.</summary>
        public ChallengeDefinition ActiveChallenge => _activeChallenge;

        /// <summary>Read-only list of all registered challenge definitions.</summary>
        public IReadOnlyList<ChallengeDefinition> AllChallenges => _challenges;

        /// <summary>Configuration asset used by this manager.</summary>
        public LandingChallengeConfig Config => config;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            RegisterDefaultChallenges();
            LoadProgress();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveProgress();
        }

        private void OnApplicationQuit()
        {
            SaveProgress();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Register a custom challenge definition at runtime.</summary>
        public void RegisterChallenge(ChallengeDefinition def)
        {
            if (def == null || string.IsNullOrEmpty(def.ChallengeId)) return;
            if (_challenges.Exists(c => c.ChallengeId == def.ChallengeId)) return;
            _challenges.Add(def);
        }

        /// <summary>Begin a challenge session for the specified challenge ID.</summary>
        /// <returns><c>true</c> if the session started successfully.</returns>
        public bool StartChallenge(string challengeId)
        {
            var def = _challenges.Find(c => c.ChallengeId == challengeId);
            if (def == null) return false;

            if (!IsChallengeUnlocked(challengeId)) return false;

            _activeChallenge = def;
            _sessionStatus   = ChallengeStatus.InProgress;

            EnsureProgress(challengeId);
            _progress[challengeId].Status = ChallengeStatus.InProgress;
            _progress[challengeId].AttemptCount++;

            OnChallengeStarted?.Invoke(def);
            return true;
        }

        /// <summary>Submit a touchdown event for scoring.</summary>
        public LandingResult SubmitTouchdown(TouchdownData touchdown, List<ApproachSnapshot> approach)
        {
            if (_activeChallenge == null || _sessionStatus != ChallengeStatus.InProgress)
                return null;

            var result = scoringEngine != null
                ? scoringEngine.Score(touchdown, approach, _activeChallenge, config)
                : FallbackScore(touchdown);

            UpdateProgress(_activeChallenge.ChallengeId, result);
            OnLandingScored?.Invoke(result);

            if (result.Grade != LandingGrade.Crash)
            {
                _progress[_activeChallenge.ChallengeId].Status = ChallengeStatus.Completed;
                OnChallengeCompleted?.Invoke(_activeChallenge, result);
            }
            else
            {
                _progress[_activeChallenge.ChallengeId].Status = ChallengeStatus.Failed;
                OnChallengeFailed?.Invoke(_activeChallenge);
            }

            _sessionStatus   = ChallengeStatus.Available;
            _activeChallenge = null;
            SaveProgress();
            return result;
        }

        /// <summary>Returns <c>true</c> when all prerequisites for a challenge are met.</summary>
        public bool IsChallengeUnlocked(string challengeId)
        {
            var def = _challenges.Find(c => c.ChallengeId == challengeId);
            if (def == null) return false;

            foreach (var pre in def.Prerequisites)
            {
                if (!_progress.TryGetValue(pre, out var pp)) return false;
                if (pp.Status != ChallengeStatus.Completed) return false;
            }
            return true;
        }

        /// <summary>Returns the progress record for a challenge, or <c>null</c>.</summary>
        public ChallengeProgress GetProgress(string challengeId)
        {
            _progress.TryGetValue(challengeId, out var p);
            return p;
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void RegisterDefaultChallenges()
        {
            RegisterChallenge(new ChallengeDefinition
            {
                ChallengeId = "std_jfk_22r",
                DisplayName = "JFK ILS 22R",
                Description = "Precision ILS approach to Runway 22R at KJFK.",
                Type        = ChallengeType.Standard,
                Difficulty  = DifficultyLevel.Beginner,
                AirportICAO = "KJFK",
                RunwayId    = "22R",
                Weather     = WeatherPreset.Clear,
                StarThresholds = new float[] { 600f, 800f, 950f }
            });
            RegisterChallenge(new ChallengeDefinition
            {
                ChallengeId  = "carrier_cvn68",
                DisplayName  = "Carrier Trap — CVN-68",
                Description  = "Arrested carrier landing on a pitching deck.",
                Type         = ChallengeType.CarrierLanding,
                Difficulty   = DifficultyLevel.Expert,
                AirportICAO  = "CARRIER",
                RunwayId     = "3-WIRE",
                Weather      = WeatherPreset.PartlyCloudy,
                StarThresholds = new float[] { 700f, 850f, 970f },
                Prerequisites = new System.Collections.Generic.List<string> { "std_jfk_22r" }
            });
            RegisterChallenge(new ChallengeDefinition
            {
                ChallengeId  = "mountain_lukla",
                DisplayName  = "Lukla STOL Approach",
                Description  = "Approach into Tenzing-Hillary Airport (Lukla) at 9,383 ft.",
                Type         = ChallengeType.MountainApproach,
                Difficulty   = DifficultyLevel.Advanced,
                AirportICAO  = "VNLK",
                RunwayId     = "06",
                Weather      = WeatherPreset.PartlyCloudy,
                StarThresholds = new float[] { 600f, 820f, 960f },
                Prerequisites = new System.Collections.Generic.List<string> { "std_jfk_22r" }
            });
            RegisterChallenge(new ChallengeDefinition
            {
                ChallengeId  = "xwind_gibraltar",
                DisplayName  = "Gibraltar Crosswind",
                Description  = "Extreme crosswind landing at Gibraltar Airport.",
                Type         = ChallengeType.CrosswindLanding,
                Difficulty   = DifficultyLevel.Advanced,
                AirportICAO  = "LXGB",
                RunwayId     = "09",
                Weather      = WeatherPreset.Crosswind,
                StarThresholds = new float[] { 600f, 800f, 940f },
                Prerequisites = new System.Collections.Generic.List<string> { "std_jfk_22r" }
            });
            RegisterChallenge(new ChallengeDefinition
            {
                ChallengeId  = "short_sba",
                DisplayName  = "Santa Barbara Short Field",
                Description  = "Maximum performance short-field landing at KSBA.",
                Type         = ChallengeType.ShortField,
                Difficulty   = DifficultyLevel.Intermediate,
                AirportICAO  = "KSBA",
                RunwayId     = "15R",
                Weather      = WeatherPreset.Clear,
                StarThresholds = new float[] { 600f, 800f, 950f }
            });
        }

        private void EnsureProgress(string challengeId)
        {
            if (!_progress.ContainsKey(challengeId))
                _progress[challengeId] = new ChallengeProgress { ChallengeId = challengeId, Status = ChallengeStatus.Available };
        }

        private void UpdateProgress(string challengeId, LandingResult result)
        {
            EnsureProgress(challengeId);
            var p = _progress[challengeId];

            if (result.TotalScore > p.BestScore)
            {
                p.BestScore = result.TotalScore;
                p.BestGrade = result.Grade;
                p.StarsEarned = result.Stars;
            }
            p.LastAttempt = DateTime.UtcNow;
        }

        private LandingResult FallbackScore(TouchdownData td)
        {
            float score = Mathf.Clamp01(1f - Mathf.Abs(td.CentrelineOffsetMetres) / 30f) * 1000f;
            return new LandingResult
            {
                TotalScore = score,
                Grade      = score >= 950f ? LandingGrade.Perfect  :
                             score >= 850f ? LandingGrade.Excellent :
                             score >= 700f ? LandingGrade.Good      :
                             score >= 500f ? LandingGrade.Fair      :
                             score >= 200f ? LandingGrade.Poor      : LandingGrade.Crash,
                Stars      = score >= 950f ? 3 : score >= 800f ? 2 : score >= 600f ? 1 : 0,
                Timestamp  = DateTime.UtcNow
            };
        }

        private void SaveProgress()
        {
            try
            {
                var list = new List<ChallengeProgress>(_progress.Values);
                var json = JsonUtility.ToJson(new ProgressWrapper { items = list });
                System.IO.File.WriteAllText(SavePath, json);
            }
            catch (Exception ex) { Debug.LogWarning($"[LandingChallengeManager] Save failed: {ex.Message}"); }
        }

        private void LoadProgress()
        {
            try
            {
                if (!System.IO.File.Exists(SavePath)) return;
                var json    = System.IO.File.ReadAllText(SavePath);
                var wrapper = JsonUtility.FromJson<ProgressWrapper>(json);
                foreach (var p in wrapper.items)
                    _progress[p.ChallengeId] = p;
            }
            catch (Exception ex) { Debug.LogWarning($"[LandingChallengeManager] Load failed: {ex.Message}"); }
        }

        private string SavePath =>
            System.IO.Path.Combine(Application.persistentDataPath, "landing_challenge_progress.json");

        [Serializable]
        private class ProgressWrapper
        {
            public List<ChallengeProgress> items = new List<ChallengeProgress>();
        }
    }
}
