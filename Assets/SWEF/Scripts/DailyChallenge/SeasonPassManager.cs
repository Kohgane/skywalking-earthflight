using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.DailyChallenge
{
    /// <summary>
    /// Singleton MonoBehaviour that manages season-pass progression and reward claiming.
    /// Loads the active <see cref="SeasonDefinition"/> from <c>Resources/Seasons/</c> and
    /// persists player state to <c>Application.persistentDataPath/season_pass.json</c>.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class SeasonPassManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static SeasonPassManager Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when season points are added. Parameter: amount gained.</summary>
        public event Action<int> OnSeasonPointsGained;

        /// <summary>Fired when the player advances to a new tier. Parameter: new tier number.</summary>
        public event Action<int> OnTierAdvanced;

        /// <summary>Fired when a reward is successfully claimed.</summary>
        public event Action<SeasonReward> OnRewardClaimed;

        /// <summary>Fired when the active season ends.</summary>
        public event Action OnSeasonEnded;

        // ── Persistence ───────────────────────────────────────────────────────────
        private static readonly string SaveFileName = "season_pass.json";
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        // ── Loaded data ───────────────────────────────────────────────────────────
        private SeasonDefinition _activeSeason;

        // ── Saved state ───────────────────────────────────────────────────────────
        [Serializable]
        private class SaveData
        {
            public string activeSeasonId = string.Empty;
            public int currentSeasonPoints;
            public int currentTier;
            public bool isPremiumUnlocked;
            public List<int> claimedFreeTiers    = new List<int>();
            public List<int> claimedPremiumTiers = new List<int>();
        }

        private SaveData _save = new SaveData();
        private readonly HashSet<int> _claimedFreeSet    = new HashSet<int>();
        private readonly HashSet<int> _claimedPremiumSet = new HashSet<int>();

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

            LoadSeasonDefinition();
            Load();
        }

        private void OnApplicationQuit() => Save();
        private void OnApplicationPause(bool p) { if (p) Save(); }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Adds season-pass points and checks for tier advancement.
        /// </summary>
        /// <param name="points">Points to add.</param>
        /// <param name="source">Human-readable source label.</param>
        public void AddSeasonPoints(int points, string source)
        {
            if (_activeSeason == null || !IsSeasonActive()) return;
            if (points <= 0) return;

            _save.currentSeasonPoints += points;
            OnSeasonPointsGained?.Invoke(points);

            CheckTierAdvancement();
            Save();
            Debug.Log($"[SWEF] SeasonPassManager: +{points} season points from '{source}'. Total: {_save.currentSeasonPoints}");
        }

        /// <summary>Returns the player's current season tier (0 = no tier yet).</summary>
        public int GetCurrentTier() => _save.currentTier;

        /// <summary>Returns a 0–1 fraction showing progress toward the next tier.</summary>
        public float GetProgressToNextTier01()
        {
            if (_activeSeason == null) return 0f;
            int pointsInTier = _save.currentSeasonPoints - _save.currentTier * _activeSeason.pointsPerTier;
            return Mathf.Clamp01((float)pointsInTier / _activeSeason.pointsPerTier);
        }

        /// <summary>Returns the number of points still needed to reach the next tier.</summary>
        public int GetPointsToNextTier()
        {
            if (_activeSeason == null) return 0;
            int threshold = (_save.currentTier + 1) * _activeSeason.pointsPerTier;
            return Mathf.Max(0, threshold - _save.currentSeasonPoints);
        }

        /// <summary>
        /// Claims the free-track reward for the specified tier.
        /// Returns <c>true</c> on success.
        /// </summary>
        public bool ClaimFreeReward(int tier)
        {
            if (_activeSeason == null) return false;
            if (tier > _save.currentTier) return false;
            if (_claimedFreeSet.Contains(tier)) return false;
            if (tier < 1 || tier > _activeSeason.totalTiers) return false;

            var reward = GetFreeReward(tier);
            if (!reward.HasValue) return false;

            _claimedFreeSet.Add(tier);
            _save.claimedFreeTiers.Add(tier);
            GrantReward(reward.Value);
            OnRewardClaimed?.Invoke(reward.Value);
            Save();
            return true;
        }

        /// <summary>
        /// Claims the premium-track reward for the specified tier.
        /// Requires <see cref="isPremiumUnlocked"/> to be <c>true</c>.
        /// Returns <c>true</c> on success.
        /// </summary>
        public bool ClaimPremiumReward(int tier)
        {
            if (_activeSeason == null) return false;
            if (!_save.isPremiumUnlocked) return false;
            if (tier > _save.currentTier) return false;
            if (_claimedPremiumSet.Contains(tier)) return false;
            if (tier < 1 || tier > _activeSeason.totalTiers) return false;

            var reward = GetPremiumReward(tier);
            if (!reward.HasValue) return false;

            _claimedPremiumSet.Add(tier);
            _save.claimedPremiumTiers.Add(tier);
            GrantReward(reward.Value);
            OnRewardClaimed?.Invoke(reward.Value);
            Save();
            return true;
        }

        /// <summary>Unlocks the premium track (call after IAP confirmation).</summary>
        public void UnlockPremium()
        {
            _save.isPremiumUnlocked = true;
            Save();
            Debug.Log("[SWEF] SeasonPassManager: Premium track unlocked.");
        }

        /// <summary>Whether the premium track is currently unlocked.</summary>
        public bool IsPremiumUnlocked => _save.isPremiumUnlocked;

        /// <summary>Returns the active <see cref="SeasonDefinition"/>, or <c>null</c> if between seasons.</summary>
        public SeasonDefinition GetActiveSeason() => IsSeasonActive() ? _activeSeason : null;

        /// <summary>Returns time remaining in the active season, or <see cref="TimeSpan.Zero"/>.</summary>
        public TimeSpan GetTimeRemaining()
        {
            if (_activeSeason == null) return TimeSpan.Zero;
            var remaining = _activeSeason.GetEndDateUtc() - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        /// <summary>Returns <c>true</c> if a season is currently active.</summary>
        public bool IsSeasonActive()
        {
            if (_activeSeason == null) return false;
            var now = DateTime.UtcNow;
            return now >= _activeSeason.GetStartDateUtc() && now < _activeSeason.GetEndDateUtc();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void CheckTierAdvancement()
        {
            if (_activeSeason == null) return;
            int newTier = _save.currentSeasonPoints / _activeSeason.pointsPerTier;
            newTier = Mathf.Min(newTier, _activeSeason.totalTiers);
            if (newTier > _save.currentTier)
            {
                _save.currentTier = newTier;
                OnTierAdvanced?.Invoke(newTier);
                Debug.Log($"[SWEF] SeasonPassManager: Advanced to tier {newTier}.");
            }
        }

        private SeasonReward? GetFreeReward(int tier)
        {
            foreach (var r in _activeSeason.freeRewards)
                if (r.tier == tier) return r;
            return null;
        }

        private SeasonReward? GetPremiumReward(int tier)
        {
            foreach (var r in _activeSeason.premiumRewards)
                if (r.tier == tier) return r;
            return null;
        }

        private void GrantReward(SeasonReward reward)
        {
            var ctrl = ChallengeRewardController.Instance;
            if (ctrl != null)
                ctrl.GrantSeasonReward(reward);
        }

        private void LoadSeasonDefinition()
        {
            var assets = Resources.LoadAll<SeasonDefinition>("Seasons");
            var now = DateTime.UtcNow;
            foreach (var s in assets)
            {
                if (now >= s.GetStartDateUtc() && now < s.GetEndDateUtc())
                {
                    _activeSeason = s;
                    break;
                }
            }
            // Fallback: most recent season by start date.
            if (_activeSeason == null && assets.Length > 0)
                _activeSeason = assets[assets.Length - 1];

            // Ultimate fallback: generated default.
            if (_activeSeason == null)
                _activeSeason = DailyChallengeDefaultData.GetDefaultSeason();

            Debug.Log($"[SWEF] SeasonPassManager: Active season '{_activeSeason?.seasonId}'.");
        }

        private void Save()
        {
            _save.claimedFreeTiers.Clear();
            _save.claimedPremiumTiers.Clear();
            foreach (var t in _claimedFreeSet)    _save.claimedFreeTiers.Add(t);
            foreach (var t in _claimedPremiumSet) _save.claimedPremiumTiers.Add(t);
            try
            {
                File.WriteAllText(SavePath, JsonUtility.ToJson(_save, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SWEF] SeasonPassManager: Save failed — {e.Message}");
            }
        }

        private void Load()
        {
            _claimedFreeSet.Clear();
            _claimedPremiumSet.Clear();
            try
            {
                if (!File.Exists(SavePath)) return;
                _save = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath)) ?? new SaveData();
                foreach (var t in _save.claimedFreeTiers)    _claimedFreeSet.Add(t);
                foreach (var t in _save.claimedPremiumTiers) _claimedPremiumSet.Add(t);

                // Reset if season changed.
                if (_activeSeason != null && _save.activeSeasonId != _activeSeason.seasonId)
                {
                    _save = new SaveData { activeSeasonId = _activeSeason.seasonId };
                    Debug.Log("[SWEF] SeasonPassManager: New season — progress reset.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SWEF] SeasonPassManager: Load failed — {e.Message}");
                _save = new SaveData();
            }
        }
    }
}
