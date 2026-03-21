using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace SWEF.DailyChallenge
{
    /// <summary>
    /// Runtime snapshot of a weekly challenge.
    /// </summary>
    [Serializable]
    public class ActiveWeeklyChallenge
    {
        /// <summary>Backing definition (not directly serialized).</summary>
        [NonSerialized] public WeeklyChallengeDefinition definition;

        /// <summary>Stable challenge id.</summary>
        public string challengeId;

        /// <summary>Accumulated progress.</summary>
        public float currentProgress;

        /// <summary>Whether the challenge has been fully completed.</summary>
        public bool isCompleted;

        /// <summary>Whether the reward has been claimed.</summary>
        public bool isClaimed;

        /// <summary>UTC ISO 8601 completion timestamp.</summary>
        public string completedTimeUtc;
    }

    /// <summary>
    /// Singleton MonoBehaviour that manages weekly challenges.
    /// Selects 2 challenges every Monday UTC 00:00 using a week-of-year seed for deterministic selection.
    /// Persists state to <c>Application.persistentDataPath/weekly_challenges.json</c>.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class WeeklyChallengeManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static WeeklyChallengeManager Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when a weekly challenge is fully completed.</summary>
        public event Action<ActiveWeeklyChallenge> OnWeeklyChallengeCompleted;

        /// <summary>Fired at the start of each new week.</summary>
        public event Action OnWeeklyReset;

        // ── Persistence ───────────────────────────────────────────────────────────
        private static readonly string SaveFileName = "weekly_challenges.json";
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        // ── Internal state ────────────────────────────────────────────────────────
        private readonly List<WeeklyChallengeDefinition> _catalog = new List<WeeklyChallengeDefinition>();
        private readonly List<ActiveWeeklyChallenge> _weekChallenges = new List<ActiveWeeklyChallenge>();

        [Serializable]
        private class SaveData
        {
            public string lastWeekKey = string.Empty;
            public List<ActiveWeeklyChallenge> challenges = new List<ActiveWeeklyChallenge>();
        }

        private SaveData _save = new SaveData();

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
            RefreshIfNewWeek();
        }

        private void Update()
        {
            if (GetWeekKey() != _save.lastWeekKey)
                RefreshIfNewWeek();
        }

        private void OnApplicationQuit() => Save();
        private void OnApplicationPause(bool p) { if (p) Save(); }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns this week's active weekly challenges.</summary>
        public IReadOnlyList<ActiveWeeklyChallenge> GetWeeklyChallenges() => _weekChallenges;

        /// <summary>
        /// Accumulates progress for all matching weekly challenges.
        /// Called by <see cref="DailyChallengeTracker"/>.
        /// </summary>
        public void ReportProgress(ChallengeType type, float amount)
        {
            bool anyUpdated = false;
            foreach (var wc in _weekChallenges)
            {
                if (wc.isCompleted || wc.definition == null) continue;
                if (wc.definition.challengeType != type) continue;

                wc.currentProgress += amount;
                anyUpdated = true;

                if (wc.currentProgress >= wc.definition.targetValue)
                {
                    wc.currentProgress = wc.definition.targetValue;
                    wc.isCompleted = true;
                    wc.completedTimeUtc = DateTime.UtcNow.ToString("O");
                    OnWeeklyChallengeCompleted?.Invoke(wc);
                }
            }
            if (anyUpdated) Save();
        }

        /// <summary>
        /// Claims the reward for a completed, unclaimed weekly challenge.
        /// Returns <c>true</c> on success.
        /// </summary>
        public bool ClaimReward(string challengeId)
        {
            foreach (var wc in _weekChallenges)
            {
                if (wc.challengeId != challengeId) continue;
                if (!wc.isCompleted || wc.isClaimed) return false;

                wc.isClaimed = true;
                var ctrl = ChallengeRewardController.Instance;
                if (ctrl != null && wc.definition != null)
                    ctrl.GrantWeeklyChallengeReward(wc.definition);
                Save();
                return true;
            }
            return false;
        }

        /// <summary>Returns time remaining until next Monday UTC 00:00 reset.</summary>
        public TimeSpan GetTimeUntilWeeklyReset()
        {
            var now = DateTime.UtcNow;
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0) daysUntilMonday = 7;
            var nextMonday = now.Date.AddDays(daysUntilMonday);
            return nextMonday - now;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private static string GetWeekKey()
        {
            var now = DateTime.UtcNow;
            int week = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                now, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
            return $"{now.Year}W{week:D2}";
        }

        private static int GetWeekSeed()
        {
            var now = DateTime.UtcNow;
            int week = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                now, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
            return now.Year * 100 + week;
        }

        private void RefreshIfNewWeek()
        {
            string weekKey = GetWeekKey();
            if (_save.lastWeekKey == weekKey) return;

            _save.lastWeekKey = weekKey;
            SelectWeeklyChallenges();
            OnWeeklyReset?.Invoke();
            Save();
        }

        private void SelectWeeklyChallenges()
        {
            _weekChallenges.Clear();
            if (_catalog.Count == 0) return;

            var rng = new System.Random(GetWeekSeed());
            var pool = new List<WeeklyChallengeDefinition>(_catalog);

            for (int i = 0; i < 2 && pool.Count > 0; i++)
            {
                int idx = rng.Next(pool.Count);
                var chosen = pool[idx];
                pool.RemoveAt(idx);
                _weekChallenges.Add(new ActiveWeeklyChallenge
                {
                    challengeId = chosen.challengeId,
                    definition  = chosen
                });
            }
            Debug.Log($"[SWEF] WeeklyChallengeManager: Selected {_weekChallenges.Count} weekly challenges.");
        }

        private void LoadCatalog()
        {
            var assets = Resources.LoadAll<WeeklyChallengeDefinition>("WeeklyChallenges");
            foreach (var a in assets) _catalog.Add(a);

            if (_catalog.Count == 0)
            {
                foreach (var def in DailyChallengeDefaultData.GetDefaultWeeklyChallenges())
                    _catalog.Add(def);
            }
            Debug.Log($"[SWEF] WeeklyChallengeManager: Catalog ready with {_catalog.Count} challenges.");
        }

        private void RehydrateDefinitions()
        {
            foreach (var wc in _save.challenges)
                wc.definition = _catalog.Find(d => d.challengeId == wc.challengeId);
        }

        private void Save()
        {
            _save.challenges.Clear();
            foreach (var wc in _weekChallenges)
                _save.challenges.Add(wc);
            try
            {
                File.WriteAllText(SavePath, JsonUtility.ToJson(_save, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SWEF] WeeklyChallengeManager: Save failed — {e.Message}");
            }
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return;
                _save = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath)) ?? new SaveData();
                RehydrateDefinitions();

                string weekKey = GetWeekKey();
                if (_save.lastWeekKey == weekKey)
                {
                    _weekChallenges.Clear();
                    foreach (var wc in _save.challenges)
                        if (wc.definition != null)
                            _weekChallenges.Add(wc);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SWEF] WeeklyChallengeManager: Load failed — {e.Message}");
                _save = new SaveData();
            }
        }
    }
}
