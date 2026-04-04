// Phase 99 — Seasonal Live Events & Battle Pass
// Assets/SWEF/Scripts/SeasonalEvents/SeasonalChallengeManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.SeasonalEvents
{
    /// <summary>
    /// Manages weekly and daily challenges tied to the current season.
    ///
    /// <para>Responsibilities:</para>
    /// <list type="bullet">
    ///   <item>Maintain the active pool of daily and weekly seasonal challenges.</item>
    ///   <item>Rotate weekly challenges every Monday at 00:00 UTC.</item>
    ///   <item>Grant battle-pass XP (including seasonal bonus XP) on completion.</item>
    ///   <item>Persist completion state to <c>Application.persistentDataPath/seasonal_challenges.json</c>.</item>
    ///   <item>Integrate with the existing DailyChallenge system concept via event hooks.</item>
    /// </list>
    /// </summary>
    [DefaultExecutionOrder(-44)]
    [DisallowMultipleComponent]
    public class SeasonalChallengeManager : MonoBehaviour
    {
        #region Singleton
        /// <summary>Global singleton instance.</summary>
        public static SeasonalChallengeManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Events
        /// <summary>Fired when a seasonal challenge is marked as completed.</summary>
        public event Action<SeasonalChallenge> OnChallengeCompleted;

        /// <summary>Fired when the weekly challenge pool has been refreshed.</summary>
        public event Action OnWeeklyChallengesRefreshed;
        #endregion

        #region State
        private readonly List<SeasonalChallenge> _dailyChallenges  = new List<SeasonalChallenge>();
        private readonly List<SeasonalChallenge> _weeklyChallenges = new List<SeasonalChallenge>();
        private Coroutine _refreshCoroutine;
        private DateTime _lastWeeklyRefresh = DateTime.MinValue;

        /// <summary>Current daily challenges (may be expired).</summary>
        public IReadOnlyList<SeasonalChallenge> DailyChallenges  => _dailyChallenges.AsReadOnly();

        /// <summary>Current weekly challenges.</summary>
        public IReadOnlyList<SeasonalChallenge> WeeklyChallenges => _weeklyChallenges.AsReadOnly();
        #endregion

        #region Persistence
        private static readonly string SaveFileName = "seasonal_challenges.json";
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        [Serializable]
        private class SaveData
        {
            public List<SeasonalChallenge> daily  = new List<SeasonalChallenge>();
            public List<SeasonalChallenge> weekly = new List<SeasonalChallenge>();
            public string lastWeeklyRefresh;
        }
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            CheckWeeklyRefresh();
            _refreshCoroutine = StartCoroutine(DailyRefreshCoroutine());
        }

        private void OnApplicationPause(bool paused) { if (paused) Save(); }
        private void OnApplicationQuit() => Save();
        #endregion

        #region Challenge Management
        /// <summary>
        /// Loads a set of daily challenges for the active season.
        /// Intended to be called by <see cref="SeasonManager"/> or a server fetch.
        /// </summary>
        public void SetDailyChallenges(IEnumerable<SeasonalChallenge> challenges)
        {
            if (challenges == null) return;
            _dailyChallenges.Clear();
            _dailyChallenges.AddRange(challenges);
            Save();
        }

        /// <summary>
        /// Loads a set of weekly challenges for the active season.
        /// </summary>
        public void SetWeeklyChallenges(IEnumerable<SeasonalChallenge> challenges)
        {
            if (challenges == null) return;
            _weeklyChallenges.Clear();
            _weeklyChallenges.AddRange(challenges);
            _lastWeeklyRefresh = DateTime.UtcNow;
            Save();
            OnWeeklyChallengesRefreshed?.Invoke();
        }

        /// <summary>
        /// Records progress toward a challenge. Completes it if <paramref name="progress"/>
        /// meets or exceeds the numeric target.
        /// </summary>
        public void ReportProgress(string challengeId, float progress)
        {
            var challenge = FindChallenge(challengeId);
            if (challenge == null || challenge.IsCompleted) return;

            challenge.Progress = progress;

            if (float.TryParse(challenge.Target, out float target) && progress >= target)
                CompleteChallenge(challenge);
            else
                Save();
        }

        /// <summary>Directly marks a challenge as completed and distributes XP.</summary>
        public void CompleteChallenge(string challengeId)
        {
            var challenge = FindChallenge(challengeId);
            if (challenge != null) CompleteChallenge(challenge);
        }

        private void CompleteChallenge(SeasonalChallenge challenge)
        {
            if (challenge == null || challenge.IsCompleted) return;

            challenge.IsCompleted = true;

            // Award XP via BattlePassController
            int xp = challenge.TotalXP;
            BattlePassController.Instance?.AddXP(xp, $"challenge:{challenge.ChallengeId}");

            Debug.Log($"[SWEF] SeasonalChallengeManager: Challenge '{challenge.Title}' completed (+{xp} XP).");
            OnChallengeCompleted?.Invoke(challenge);
            Save();
        }

        private SeasonalChallenge FindChallenge(string id)
        {
            foreach (var c in _dailyChallenges)
                if (c.ChallengeId == id) return c;
            foreach (var c in _weeklyChallenges)
                if (c.ChallengeId == id) return c;
            return null;
        }
        #endregion

        #region Weekly Rotation
        private void CheckWeeklyRefresh()
        {
            var now = DateTime.UtcNow;
            // Rotate on Monday 00:00 UTC
            var nextMonday = GetNextMonday(_lastWeeklyRefresh);
            if (now >= nextMonday)
            {
                RefreshWeeklyChallenges();
            }
        }

        private void RefreshWeeklyChallenges()
        {
            // In a real implementation, fetch from server or generate procedurally.
            // Here we clear stale weekly challenges and fire the refresh event so the
            // UI and server-fetch logic can respond.
            foreach (var c in _weeklyChallenges)
            {
                if (c.IsExpired()) c.IsCompleted = false; // allow re-issue
            }

            _lastWeeklyRefresh = DateTime.UtcNow;
            Debug.Log("[SWEF] SeasonalChallengeManager: Weekly challenges refreshed.");
            OnWeeklyChallengesRefreshed?.Invoke();
            Save();
        }

        private static DateTime GetNextMonday(DateTime from)
        {
            // Advance to the next Monday 00:00 UTC after 'from'
            var d = from.Date.AddDays(1);
            while (d.DayOfWeek != DayOfWeek.Monday) d = d.AddDays(1);
            return DateTime.SpecifyKind(d, DateTimeKind.Utc);
        }

        private IEnumerator DailyRefreshCoroutine()
        {
            while (true)
            {
                // Check once per hour
                yield return new WaitForSeconds(3600f);
                CheckWeeklyRefresh();
            }
        }
        #endregion

        #region Persistence
        private void Load()
        {
            if (!File.Exists(SavePath)) return;
            try
            {
                var json = File.ReadAllText(SavePath);
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null) return;

                if (data.daily != null)  { _dailyChallenges.Clear();  _dailyChallenges.AddRange(data.daily); }
                if (data.weekly != null) { _weeklyChallenges.Clear(); _weeklyChallenges.AddRange(data.weekly); }

                if (!string.IsNullOrEmpty(data.lastWeeklyRefresh) &&
                    DateTime.TryParse(data.lastWeeklyRefresh, null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                    _lastWeeklyRefresh = dt.ToUniversalTime();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] SeasonalChallengeManager: Load failed — {ex.Message}");
            }
        }

        private void Save()
        {
            try
            {
                var data = new SaveData
                {
                    daily  = new List<SeasonalChallenge>(_dailyChallenges),
                    weekly = new List<SeasonalChallenge>(_weeklyChallenges),
                    lastWeeklyRefresh = _lastWeeklyRefresh.ToString("O")
                };
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, prettyPrint: true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] SeasonalChallengeManager: Save failed — {ex.Message}");
            }
        }
        #endregion
    }
}
