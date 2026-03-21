using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.DailyChallenge
{
    /// <summary>
    /// Runtime snapshot of a challenge for the current day.
    /// </summary>
    [Serializable]
    public class ActiveChallenge
    {
        /// <summary>Backing definition (not serialized directly — stored by id).</summary>
        [NonSerialized] public DailyChallengeDefinition definition;

        /// <summary>Stable challenge id matching <see cref="DailyChallengeDefinition.challengeId"/>.</summary>
        public string challengeId;

        /// <summary>Accumulated progress toward <see cref="DailyChallengeDefinition.targetValue"/>.</summary>
        public float currentProgress;

        /// <summary>Whether the player has finished this challenge.</summary>
        public bool isCompleted;

        /// <summary>Whether the completion reward has been claimed.</summary>
        public bool isClaimed;

        /// <summary>UTC time of completion, serialized as ISO 8601.</summary>
        public string completedTimeUtc;

        /// <summary>Returns completion time or null.</summary>
        [NonSerialized] public DateTime? completedTime;
    }

    /// <summary>
    /// Singleton MonoBehaviour that manages daily challenge selection, progress and completion.
    /// Selects 3 challenges (1 Easy, 1 Medium, 1 Hard) plus 1 rotating Elite bonus each UTC day.
    /// Uses a deterministic seed so all devices show the same challenges on the same date.
    /// Persists state to <c>Application.persistentDataPath/daily_challenges.json</c>.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class DailyChallengeManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static DailyChallengeManager Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when a challenge's progress value changes.</summary>
        public event Action<ActiveChallenge> OnChallengeProgressUpdated;

        /// <summary>Fired when a challenge reaches 100 % completion.</summary>
        public event Action<ActiveChallenge> OnChallengeCompleted;

        /// <summary>Fired at UTC midnight when the daily set resets.</summary>
        public event Action OnDailyReset;

        /// <summary>Fired when the consecutive-day streak count changes.</summary>
        public event Action<int> OnStreakUpdated;

        // ── Persistence ───────────────────────────────────────────────────────────
        private static readonly string SaveFileName = "daily_challenges.json";
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        // ── Internal state ────────────────────────────────────────────────────────
        private readonly List<DailyChallengeDefinition> _catalog = new List<DailyChallengeDefinition>();
        private readonly List<ActiveChallenge> _todayChallenges = new List<ActiveChallenge>();

        [Serializable]
        private class SaveData
        {
            public string lastDateUtc = string.Empty;
            public int streak;
            public string lastCompletionDateUtc = string.Empty;
            public List<ActiveChallenge> challenges = new List<ActiveChallenge>();
        }

        private SaveData _save = new SaveData();
        private int _playerRankLevel = 1;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadCatalog();
            Load();
            RefreshIfNewDay();
        }

        private void Update()
        {
            // Check for date rollover every frame (cheap string comparison).
            string todayKey = GetTodayKey();
            if (_save.lastDateUtc != todayKey)
                RefreshIfNewDay();
        }

        private void OnApplicationQuit() => Save();
        private void OnApplicationPause(bool p) { if (p) Save(); }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns the active challenge list for today (3 standard + 1 Elite).</summary>
        public IReadOnlyList<ActiveChallenge> GetTodaysChallenges() => _todayChallenges;

        /// <summary>
        /// Accumulates progress for all active challenges of the given type.
        /// Called by <see cref="DailyChallengeTracker"/>.
        /// </summary>
        /// <param name="type">Challenge category.</param>
        /// <param name="amount">Progress delta in natural units.</param>
        public void ReportProgress(ChallengeType type, float amount)
        {
            bool anyUpdated = false;
            foreach (var ac in _todayChallenges)
            {
                if (ac.isCompleted) continue;
                if (ac.definition == null) continue;
                if (ac.definition.challengeType != type) continue;

                ac.currentProgress += amount;
                OnChallengeProgressUpdated?.Invoke(ac);
                anyUpdated = true;

                if (ac.currentProgress >= ac.definition.targetValue)
                {
                    ac.currentProgress = ac.definition.targetValue;
                    ac.isCompleted = true;
                    ac.completedTime = DateTime.UtcNow;
                    ac.completedTimeUtc = ac.completedTime.Value.ToString("O");
                    OnChallengeCompleted?.Invoke(ac);
                    UpdateStreak();
                }
            }
            if (anyUpdated) Save();
        }

        /// <summary>
        /// Claims the reward for a completed, unclaimed challenge.
        /// Returns <c>true</c> if successful; <c>false</c> if already claimed or not complete.
        /// </summary>
        /// <param name="challengeId">Id of the challenge to claim.</param>
        public bool ClaimReward(string challengeId)
        {
            foreach (var ac in _todayChallenges)
            {
                if (ac.challengeId != challengeId) continue;
                if (!ac.isCompleted || ac.isClaimed) return false;

                ac.isClaimed = true;
                var ctrl = ChallengeRewardController.Instance;
                if (ctrl != null && ac.definition != null)
                    ctrl.GrantDailyChallengeReward(ac.definition, GetDailyStreak());
                Save();
                return true;
            }
            return false;
        }

        /// <summary>Returns the number of consecutive UTC days with at least one challenge completed.</summary>
        public int GetDailyStreak() => _save.streak;

        /// <summary>Returns the time remaining until the next UTC midnight reset.</summary>
        public TimeSpan GetTimeUntilReset()
        {
            var now = DateTime.UtcNow;
            var midnight = now.Date.AddDays(1);
            return midnight - now;
        }

        /// <summary>Forces a manual refresh — re-selects today's challenges (useful for testing).</summary>
        public void ForceRefresh()
        {
            _save.lastDateUtc = string.Empty;
            RefreshIfNewDay();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private static string GetTodayKey()
        {
            var u = DateTime.UtcNow;
            return (u.Year * 10000 + u.Month * 100 + u.Day).ToString();
        }

        private static int GetDaySeed()
        {
            var u = DateTime.UtcNow;
            return u.Year * 10000 + u.Month * 100 + u.Day;
        }

        private void RefreshIfNewDay()
        {
            string todayKey = GetTodayKey();
            if (_save.lastDateUtc == todayKey) return;

            _save.lastDateUtc = todayKey;
            SelectTodaysChallenges();
            OnDailyReset?.Invoke();
            Save();
        }

        private void SelectTodaysChallenges()
        {
            _todayChallenges.Clear();
            _save.challenges.Clear();

            // Fetch player rank for gating (null-safe).
            var pm = SWEF.Progression.ProgressionManager.Instance;
            _playerRankLevel = pm != null ? pm.CurrentRankLevel : 1;

            var rng = new System.Random(GetDaySeed());

            TryAddByDifficulty(ChallengeDifficulty.Easy, rng);
            TryAddByDifficulty(ChallengeDifficulty.Medium, rng);
            TryAddByDifficulty(ChallengeDifficulty.Hard, rng);
            TryAddByDifficulty(ChallengeDifficulty.Elite, rng);

            // Mirror to save list.
            foreach (var ac in _todayChallenges)
                _save.challenges.Add(ac);

            Debug.Log($"[SWEF] DailyChallengeManager: Selected {_todayChallenges.Count} challenges for today.");
        }

        private void TryAddByDifficulty(ChallengeDifficulty diff, System.Random rng)
        {
            var pool = new List<DailyChallengeDefinition>();
            foreach (var def in _catalog)
            {
                if (def.difficulty == diff && def.requiredRankLevel <= _playerRankLevel)
                    pool.Add(def);
            }
            if (pool.Count == 0)
            {
                // Fallback: ignore rank gate.
                foreach (var def in _catalog)
                    if (def.difficulty == diff) pool.Add(def);
            }
            if (pool.Count == 0) return;

            var chosen = pool[rng.Next(pool.Count)];
            var ac = new ActiveChallenge
            {
                challengeId = chosen.challengeId,
                definition  = chosen
            };
            _todayChallenges.Add(ac);
        }

        private void UpdateStreak()
        {
            var today = DateTime.UtcNow.Date;
            if (string.IsNullOrEmpty(_save.lastCompletionDateUtc))
            {
                _save.streak = 1;
            }
            else if (DateTime.TryParse(_save.lastCompletionDateUtc, out var last))
            {
                var diff = (today - last.Date).Days;
                if (diff == 0)
                {
                    // Same day — already counted.
                    return;
                }
                else if (diff == 1)
                {
                    _save.streak++;
                }
                else
                {
                    _save.streak = 1;
                }
            }
            else
            {
                _save.streak = 1;
            }
            _save.lastCompletionDateUtc = today.ToString("O");
            OnStreakUpdated?.Invoke(_save.streak);
        }

        private void LoadCatalog()
        {
            var assets = Resources.LoadAll<DailyChallengeDefinition>("DailyChallenges");
            foreach (var a in assets) _catalog.Add(a);

            // Fallback to built-in defaults when no assets are present.
            if (_catalog.Count == 0)
            {
                foreach (var def in DailyChallengeDefaultData.GetDefaultDailyChallenges())
                    _catalog.Add(def);
            }
            Debug.Log($"[SWEF] DailyChallengeManager: Catalog ready with {_catalog.Count} challenges.");
        }

        private void RehydrateDefinitions()
        {
            foreach (var ac in _save.challenges)
            {
                ac.definition = _catalog.Find(d => d.challengeId == ac.challengeId);
                if (!string.IsNullOrEmpty(ac.completedTimeUtc) &&
                    DateTime.TryParse(ac.completedTimeUtc, out var dt))
                    ac.completedTime = dt;
            }
        }

        private void Save()
        {
            try
            {
                _save.challenges.Clear();
                foreach (var ac in _todayChallenges)
                    _save.challenges.Add(ac);
                File.WriteAllText(SavePath, JsonUtility.ToJson(_save, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SWEF] DailyChallengeManager: Save failed — {e.Message}");
            }
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return;
                _save = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath)) ?? new SaveData();
                RehydrateDefinitions();

                // Restore today's challenges if date matches.
                string todayKey = GetTodayKey();
                if (_save.lastDateUtc == todayKey)
                {
                    _todayChallenges.Clear();
                    foreach (var ac in _save.challenges)
                    {
                        if (ac.definition != null)
                            _todayChallenges.Add(ac);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SWEF] DailyChallengeManager: Load failed — {e.Message}");
                _save = new SaveData();
            }
        }
    }
}
