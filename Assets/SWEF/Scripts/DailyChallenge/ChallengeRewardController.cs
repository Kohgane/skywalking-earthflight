using System;
using System.IO;
using UnityEngine;

namespace SWEF.DailyChallenge
{
    /// <summary>
    /// MonoBehaviour that distributes rewards from daily/weekly challenges and season pass.
    /// Acts as the central integration point between the challenge systems and
    /// <see cref="SWEF.Progression.ProgressionManager"/>, <see cref="SWEF.Progression.CosmeticUnlockManager"/>,
    /// and <see cref="SWEF.Progression.SkillTreeManager"/>.
    /// Also tracks the virtual currency (Sky Coins) balance.
    /// Persists currency to <c>Application.persistentDataPath/currency.json</c>.
    /// </summary>
    public class ChallengeRewardController : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static ChallengeRewardController Instance { get; private set; }

        // ── Constants ─────────────────────────────────────────────────────────────
        /// <summary>Maximum streak multiplier cap (10 days → +100 % = ×2.0).</summary>
        private const int MaxStreakDays = 10;

        // ── Persistence ───────────────────────────────────────────────────────────
        private static readonly string SaveFileName = "currency.json";
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        [Serializable]
        private class CurrencySaveData { public int balance; }
        private CurrencySaveData _currencySave = new CurrencySaveData();

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
            LoadCurrency();
        }

        private void OnApplicationQuit() => SaveCurrency();
        private void OnApplicationPause(bool p) { if (p) SaveCurrency(); }

        // ── Public API — Daily Challenges ─────────────────────────────────────────

        /// <summary>
        /// Grants the reward for a completed daily challenge, applying the streak XP multiplier.
        /// </summary>
        /// <param name="def">The challenge definition.</param>
        /// <param name="streakDays">Current consecutive-day streak for multiplier calculation.</param>
        public void GrantDailyChallengeReward(DailyChallengeDefinition def, int streakDays)
        {
            if (def == null) return;

            float multiplier = CalculateStreakMultiplier(streakDays);
            long xp = Mathf.RoundToInt(def.baseXPReward * multiplier);

            AddXP(xp, $"DailyChallenge:{def.challengeId}");
            AddCurrency(def.baseCurrencyReward);

            var spm = SeasonPassManager.Instance;
            spm?.AddSeasonPoints(def.seasonPointReward, $"DailyChallenge:{def.challengeId}");

            Debug.Log($"[SWEF] ChallengeRewardController: Daily reward '{def.challengeId}' — {xp} XP (×{multiplier:F2}), {def.baseCurrencyReward} coins, {def.seasonPointReward} SP.");
        }

        // ── Public API — Weekly Challenges ────────────────────────────────────────

        /// <summary>
        /// Grants the reward for a completed weekly challenge.
        /// </summary>
        /// <param name="def">The weekly challenge definition.</param>
        public void GrantWeeklyChallengeReward(WeeklyChallengeDefinition def)
        {
            if (def == null) return;

            AddXP(def.xpReward, $"WeeklyChallenge:{def.challengeId}");
            AddCurrency(def.currencyReward);

            var spm = SeasonPassManager.Instance;
            spm?.AddSeasonPoints(def.seasonPointReward, $"WeeklyChallenge:{def.challengeId}");

            if (!string.IsNullOrEmpty(def.bonusCosmeticId))
                GrantCosmetic(def.bonusCosmeticId);

            Debug.Log($"[SWEF] ChallengeRewardController: Weekly reward '{def.challengeId}' — {def.xpReward} XP, {def.currencyReward} coins, {def.seasonPointReward} SP.");
        }

        // ── Public API — Season Pass ───────────────────────────────────────────────

        /// <summary>
        /// Distributes a season-pass tier reward by its type.
        /// </summary>
        /// <param name="reward">The reward to distribute.</param>
        public void GrantSeasonReward(SeasonReward reward)
        {
            switch (reward.rewardType)
            {
                case SeasonRewardType.XP:
                    AddXP(reward.amount, $"SeasonReward:T{reward.tier}");
                    break;
                case SeasonRewardType.Currency:
                    AddCurrency(reward.amount);
                    break;
                case SeasonRewardType.Cosmetic:
                    GrantCosmetic(reward.rewardId);
                    break;
                case SeasonRewardType.Title:
                    GrantTitle(reward.rewardId);
                    break;
                case SeasonRewardType.SkillPoint:
                    GrantSkillPoints(reward.amount);
                    break;
            }
            Debug.Log($"[SWEF] ChallengeRewardController: Season reward T{reward.tier} ({reward.rewardType}) granted.");
        }

        // ── Public API — Currency ─────────────────────────────────────────────────

        /// <summary>Returns the current Sky Coins balance.</summary>
        public int GetCurrencyBalance() => _currencySave.balance;

        /// <summary>Adds Sky Coins to the balance.</summary>
        /// <param name="amount">Amount to add (clamped to ≥ 0).</param>
        public void AddCurrency(int amount)
        {
            if (amount <= 0) return;
            _currencySave.balance += amount;
            SaveCurrency();
        }

        /// <summary>
        /// Deducts Sky Coins from the balance.
        /// Returns <c>false</c> and makes no change if funds are insufficient.
        /// </summary>
        /// <param name="amount">Amount to deduct.</param>
        public bool SpendCurrency(int amount)
        {
            if (amount <= 0) return true;
            if (_currencySave.balance < amount) return false;
            _currencySave.balance -= amount;
            SaveCurrency();
            return true;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private static float CalculateStreakMultiplier(int streakDays)
        {
            int capped = Mathf.Clamp(streakDays, 0, MaxStreakDays);
            return 1f + capped * 0.1f; // +10 % per day, capped at +100 %
        }

        private static void AddXP(long amount, string source)
        {
            var pm = SWEF.Progression.ProgressionManager.Instance;
            pm?.AddXP(amount, source);
        }

        private static void GrantCosmetic(string cosmeticId)
        {
            var cum = SWEF.Progression.CosmeticUnlockManager.Instance;
            cum?.UnlockCosmetic(cosmeticId);
        }

        private static void GrantTitle(string titleId)
        {
            // Title grants use the cosmetic system (NameTag category).
            var cum = SWEF.Progression.CosmeticUnlockManager.Instance;
            cum?.UnlockCosmetic(titleId);
        }

        private static void GrantSkillPoints(int count)
        {
            var stm = SWEF.Progression.SkillTreeManager.Instance;
            stm?.AddSkillPoint(count);
        }

        private void LoadCurrency()
        {
            try
            {
                if (!File.Exists(SavePath)) return;
                _currencySave = JsonUtility.FromJson<CurrencySaveData>(File.ReadAllText(SavePath))
                                ?? new CurrencySaveData();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SWEF] ChallengeRewardController: Currency load failed — {e.Message}");
            }
        }

        private void SaveCurrency()
        {
            try
            {
                File.WriteAllText(SavePath, JsonUtility.ToJson(_currencySave, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SWEF] ChallengeRewardController: Currency save failed — {e.Message}");
            }
        }
    }
}
