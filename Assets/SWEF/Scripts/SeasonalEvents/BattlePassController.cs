// Phase 99 — Seasonal Live Events & Battle Pass
// Assets/SWEF/Scripts/SeasonalEvents/BattlePassController.cs
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.SeasonalEvents
{
    /// <summary>
    /// Manages battle-pass progression for both the free and premium tracks.
    ///
    /// <para>Responsibilities:</para>
    /// <list type="bullet">
    ///   <item>Track the player's current XP and tier within the active season.</item>
    ///   <item>Award XP from flights, challenges, missions, and achievements.</item>
    ///   <item>Unlock tier rewards and fire corresponding events.</item>
    ///   <item>Handle premium-pass ownership (integrates with IAP concept).</item>
    ///   <item>Persist state to <c>Application.persistentDataPath/battle_pass.json</c>.</item>
    /// </list>
    /// </summary>
    [DefaultExecutionOrder(-45)]
    [DisallowMultipleComponent]
    public class BattlePassController : MonoBehaviour
    {
        #region Singleton
        /// <summary>Global singleton instance.</summary>
        public static BattlePassController Instance { get; private set; }

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
        /// <summary>
        /// Fired when the player advances to a new tier.
        /// Parameters: new tier number, whether the player owns the premium pass.
        /// </summary>
        public event Action<int, bool> OnTierUnlocked;

        /// <summary>
        /// Fired whenever XP is added.
        /// Parameters: XP amount gained, source description (e.g. "flight", "challenge").
        /// </summary>
        public event Action<int, string> OnXPEarned;

        /// <summary>
        /// Fired when the player completes the final tier.
        /// Parameter: whether the player owns the premium pass.
        /// </summary>
        public event Action<bool> OnPassCompleted;
        #endregion

        #region Inspector
        [Header("Tier Configuration")]
        [Tooltip("XP required to advance one tier (applied uniformly when no tier list is loaded).")]
        [SerializeField] private int xpPerTier = 500;

        [Tooltip("Maximum number of tiers in the season. Overridden by season data when available.")]
        [SerializeField] private int maxTiers = 50;
        #endregion

        #region Persistence
        private static readonly string SaveFileName = "battle_pass.json";
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        [Serializable]
        private class SaveData
        {
            public string seasonId;
            public int currentTier;
            public int currentXP;
            public bool isPremium;
            public List<int> claimedFreeTiers = new List<int>();
            public List<int> claimedPremiumTiers = new List<int>();
        }

        private SaveData _save = new SaveData();
        #endregion

        #region State
        private List<BattlePassTier> _tiers = new List<BattlePassTier>();

        /// <summary>Current battle-pass tier (1-based).</summary>
        public int CurrentTier => _save.currentTier;

        /// <summary>Accumulated XP within the current tier.</summary>
        public int CurrentXP => _save.currentXP;

        /// <summary>Whether the player owns the premium pass for the active season.</summary>
        public bool IsPremium => _save.isPremium;

        /// <summary>XP needed to reach the next tier from the current XP total.</summary>
        public int XPForNextTier
        {
            get
            {
                int nextTier = _save.currentTier + 1;
                var tierData = GetTierData(nextTier);
                if (tierData != null)
                    return Mathf.Max(0, tierData.RequiredXP - _save.currentXP);
                return Mathf.Max(0, xpPerTier - (_save.currentXP % xpPerTier));
            }
        }

        /// <summary>0–1 progress fraction within the current tier.</summary>
        public float TierProgressFraction
        {
            get
            {
                if (xpPerTier <= 0) return 0f;
                return Mathf.Clamp01((float)(_save.currentXP % xpPerTier) / xpPerTier);
            }
        }
        #endregion

        #region Unity Lifecycle
        private void OnApplicationPause(bool paused) { if (paused) Save(); }
        private void OnApplicationQuit() => Save();
        #endregion

        #region XP & Tier Progression
        /// <summary>
        /// Awards XP to the player and handles tier advancement.
        /// </summary>
        /// <param name="amount">XP amount to award (clamped to &gt;= 0).</param>
        /// <param name="source">Human-readable source label (e.g. "flight", "challenge", "mission").</param>
        public void AddXP(int amount, string source = "unknown")
        {
            if (amount <= 0) return;

            _save.currentXP += amount;
            OnXPEarned?.Invoke(amount, source ?? "unknown");

            CheckTierAdvancement();
            Save();
        }

        private void CheckTierAdvancement()
        {
            int tierMax = _tiers.Count > 0 ? _tiers.Count : maxTiers;

            while (_save.currentXP >= xpPerTier && _save.currentTier < tierMax)
            {
                _save.currentXP -= xpPerTier;
                _save.currentTier++;

                Debug.Log($"[SWEF] BattlePassController: Tier unlocked — {_save.currentTier}");
                OnTierUnlocked?.Invoke(_save.currentTier, _save.isPremium);

                // Grant rewards for newly reached tier
                GrantTierRewards(_save.currentTier);

                if (_save.currentTier >= tierMax)
                {
                    _save.currentXP = 0;
                    OnPassCompleted?.Invoke(_save.isPremium);
                    break;
                }
            }
        }

        private void GrantTierRewards(int tier)
        {
            var tierData = GetTierData(tier);
            if (tierData == null) return;

            // Free reward
            if (tierData.FreeReward != null && !_save.claimedFreeTiers.Contains(tier))
            {
                _save.claimedFreeTiers.Add(tier);
                Debug.Log($"[SWEF] BattlePassController: Free reward '{tierData.FreeReward.DisplayName}' granted at tier {tier}.");
            }

            // Premium reward
            if (_save.isPremium && tierData.PremiumReward != null && !_save.claimedPremiumTiers.Contains(tier))
            {
                _save.claimedPremiumTiers.Add(tier);
                Debug.Log($"[SWEF] BattlePassController: Premium reward '{tierData.PremiumReward.DisplayName}' granted at tier {tier}.");
            }
        }

        private BattlePassTier GetTierData(int tier)
        {
            if (_tiers == null) return null;
            foreach (var t in _tiers)
                if (t.TierNumber == tier) return t;
            return null;
        }
        #endregion

        #region Premium Pass
        /// <summary>
        /// Unlocks the premium pass for the active season.
        /// Retroactively grants any premium rewards the player has already passed.
        /// </summary>
        public void UnlockPremiumPass()
        {
            if (_save.isPremium) return;
            _save.isPremium = true;

            // Back-fill premium rewards up to current tier
            for (int t = 1; t <= _save.currentTier; t++)
                GrantTierRewards(t);

            Save();
            Debug.Log("[SWEF] BattlePassController: Premium pass unlocked.");
        }
        #endregion

        #region Season Tier Data
        /// <summary>
        /// Loads tier data for a new season. Called by <see cref="SeasonManager"/> on season start.
        /// </summary>
        public void InitialiseSeason(SeasonData season)
        {
            if (season == null) return;

            // Reset only if season changed
            if (_save.seasonId != season.SeasonId)
            {
                _save = new SaveData { seasonId = season.SeasonId, currentTier = 0, currentXP = 0 };
            }

            maxTiers = season.TierCount;
            BuildTierList(season);
            Save();
        }

        private void BuildTierList(SeasonData season)
        {
            _tiers.Clear();
            int freeCount = season.FreeTrackRewards?.Count ?? 0;
            int premiumCount = season.PremiumTrackRewards?.Count ?? 0;
            int total = Mathf.Max(season.TierCount, Mathf.Max(freeCount, premiumCount));

            for (int i = 0; i < total; i++)
            {
                var tier = new BattlePassTier
                {
                    TierNumber = i + 1,
                    RequiredXP = (i + 1) * xpPerTier,
                    FreeReward    = (i < freeCount)    ? season.FreeTrackRewards[i]    : null,
                    PremiumReward = (i < premiumCount) ? season.PremiumTrackRewards[i] : null
                };
                _tiers.Add(tier);
            }
        }

        /// <summary>Returns a preview of the reward for a specific tier and track.</summary>
        public BattlePassReward GetNextRewardPreview(bool isPremium)
        {
            var tierData = GetTierData(_save.currentTier + 1);
            return tierData?.GetReward(isPremium);
        }
        #endregion

        #region End-of-Season
        /// <summary>
        /// Called by <see cref="SeasonManager"/> to distribute end-of-season rewards.
        /// </summary>
        public void DistributeEndOfSeasonRewards(SeasonData season)
        {
            if (season == null) return;
            Debug.Log($"[SWEF] BattlePassController: Distributing end-of-season rewards for '{season.SeasonName}', tier {_save.currentTier}.");
        }
        #endregion

        #region Persistence
        private void Load()
        {
            if (!File.Exists(SavePath)) return;
            try
            {
                var json = File.ReadAllText(SavePath);
                _save = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] BattlePassController: Load failed — {ex.Message}");
                _save = new SaveData();
            }
        }

        private void Save()
        {
            try
            {
                File.WriteAllText(SavePath, JsonUtility.ToJson(_save, prettyPrint: true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] BattlePassController: Save failed — {ex.Message}");
            }
        }
        #endregion
    }
}
