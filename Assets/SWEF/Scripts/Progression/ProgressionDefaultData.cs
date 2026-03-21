using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Progression
{
    /// <summary>
    /// Static helper that generates default runtime data for the Progression system.
    /// Used as a fallback when no ScriptableObject assets are present in Resources.
    /// </summary>
    public static class ProgressionDefaultData
    {
        // ── Rank generation ───────────────────────────────────────────────────────

        /// <summary>Base XP value used in the exponential XP curve (baseXP × level^1.5).</summary>
        private const float BaseXP = 500f;

        /// <summary>
        /// Generates and returns a list of 50 <see cref="PilotRankData"/> instances
        /// using an exponential XP curve (<c>baseXP × level^1.5</c>).
        /// Tier boundaries:
        /// Trainee 1–5, Cadet 6–12, Pilot 13–20, Captain 21–28,
        /// Commander 29–36, Ace 37–42, Legend 43–48, Skywalker 49–50.
        /// </summary>
        public static List<PilotRankData> GetDefaultRanks()
        {
            var ranks = new List<PilotRankData>(50);
            for (int level = 1; level <= 50; level++)
            {
                var data = ScriptableObject.CreateInstance<PilotRankData>();
                data.rankId      = $"rank_{level:D2}";
                data.rankLevel   = level;
                data.rankTier    = TierForLevel(level);
                data.rankName    = DefaultRankName(level, data.rankTier);
                data.rankNameKey = $"progression_rank_{level:D2}";
                data.requiredXP  = CumulativeXP(level);
                data.rankColor   = TierColor(data.rankTier);
                ranks.Add(data);
            }
            return ranks;
        }

        // ── Skill generation ──────────────────────────────────────────────────────

        /// <summary>
        /// Generates and returns 25 default <see cref="SkillTreeData"/> instances:
        /// 5 per <see cref="SkillCategory"/>, across tiers 1–5.
        /// </summary>
        public static List<SkillTreeData> GetDefaultSkills()
        {
            var skills = new List<SkillTreeData>(25);

            foreach (SkillCategory category in Enum.GetValues(typeof(SkillCategory)))
            {
                for (int tier = 1; tier <= 5; tier++)
                {
                    var data = ScriptableObject.CreateInstance<SkillTreeData>();
                    string catShort = CategoryShortName(category);
                    data.skillId       = $"skill_{catShort}_{tier}";
                    data.skillNameKey  = $"skill_{catShort}_{tier}_name";
                    data.descriptionKey= $"skill_{catShort}_{tier}_desc";
                    data.category      = category;
                    data.tier          = tier;
                    data.skillPointCost= tier;
                    data.effect        = DefaultEffectForCategory(category);
                    data.effectValue   = 0.05f * tier; // 5% per tier

                    // Chain prerequisites within the same category
                    if (tier > 1)
                        data.prerequisiteSkillIds = new[] { $"skill_{catShort}_{tier - 1}" };

                    skills.Add(data);
                }
            }
            return skills;
        }

        // ── XP config ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a <see cref="XPSourceConfig"/> with sensible default values.
        /// Equivalent to <see cref="XPSourceConfig.GetDefault"/>.
        /// </summary>
        public static XPSourceConfig GetDefaultXPConfig() => XPSourceConfig.GetDefault();

        // ── Cosmetics ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a list of default cosmetic items (one per category, unlocked at varying ranks).
        /// </summary>
        public static List<CosmeticUnlockManager.CosmeticItem> GetDefaultCosmetics()
        {
            return new List<CosmeticUnlockManager.CosmeticItem>
            {
                new CosmeticUnlockManager.CosmeticItem { id = "trail_basic",   nameKey = "cosmetic_trail_basic",    category = CosmeticCategory.TrailEffect,  unlockedAtRank = 1  },
                new CosmeticUnlockManager.CosmeticItem { id = "trail_gold",    nameKey = "cosmetic_trail_gold",     category = CosmeticCategory.TrailEffect,  unlockedAtRank = 20 },
                new CosmeticUnlockManager.CosmeticItem { id = "trail_rainbow", nameKey = "cosmetic_trail_rainbow",  category = CosmeticCategory.TrailEffect,  unlockedAtRank = 40 },
                new CosmeticUnlockManager.CosmeticItem { id = "skin_default",  nameKey = "cosmetic_skin_default",   category = CosmeticCategory.AircraftSkin, unlockedAtRank = 1  },
                new CosmeticUnlockManager.CosmeticItem { id = "skin_camo",     nameKey = "cosmetic_skin_camo",      category = CosmeticCategory.AircraftSkin, unlockedAtRank = 15 },
                new CosmeticUnlockManager.CosmeticItem { id = "skin_neon",     nameKey = "cosmetic_skin_neon",      category = CosmeticCategory.AircraftSkin, unlockedAtRank = 35 },
                new CosmeticUnlockManager.CosmeticItem { id = "badge_wings",   nameKey = "cosmetic_badge_wings",    category = CosmeticCategory.Badge,        unlockedAtRank = 5  },
                new CosmeticUnlockManager.CosmeticItem { id = "badge_ace",     nameKey = "cosmetic_badge_ace",      category = CosmeticCategory.Badge,        unlockedAtRank = 37 },
                new CosmeticUnlockManager.CosmeticItem { id = "badge_legend",  nameKey = "cosmetic_badge_legend",   category = CosmeticCategory.Badge,        unlockedAtRank = 43 },
                new CosmeticUnlockManager.CosmeticItem { id = "tag_rookie",    nameKey = "cosmetic_tag_rookie",     category = CosmeticCategory.NameTag,      unlockedAtRank = 1  },
                new CosmeticUnlockManager.CosmeticItem { id = "tag_skywalker", nameKey = "cosmetic_tag_skywalker",  category = CosmeticCategory.NameTag,      unlockedAtRank = 49 },
                new CosmeticUnlockManager.CosmeticItem { id = "emote_wave",    nameKey = "cosmetic_emote_wave",     category = CosmeticCategory.Emote,        unlockedAtRank = 10 },
                new CosmeticUnlockManager.CosmeticItem { id = "emote_salute",  nameKey = "cosmetic_emote_salute",   category = CosmeticCategory.Emote,        unlockedAtRank = 25 },
            };
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private static long CumulativeXP(int level)
        {
            if (level <= 1) return 0L;
            return (long)(BaseXP * Math.Pow(level, 1.5));
        }

        private static RankTier TierForLevel(int level)
        {
            if (level <= 5)  return RankTier.Trainee;
            if (level <= 12) return RankTier.Cadet;
            if (level <= 20) return RankTier.Pilot;
            if (level <= 28) return RankTier.Captain;
            if (level <= 36) return RankTier.Commander;
            if (level <= 42) return RankTier.Ace;
            if (level <= 48) return RankTier.Legend;
            return RankTier.Skywalker;
        }

        private static string DefaultRankName(int level, RankTier tier)
        {
            return $"{tier} {RomanNumeral(level)}";
        }

        private static Color TierColor(RankTier tier)
        {
            switch (tier)
            {
                case RankTier.Trainee:   return new Color(0.6f, 0.6f, 0.6f);
                case RankTier.Cadet:     return new Color(0.3f, 0.7f, 1.0f);
                case RankTier.Pilot:     return new Color(0.2f, 0.8f, 0.2f);
                case RankTier.Captain:   return new Color(1.0f, 0.8f, 0.0f);
                case RankTier.Commander: return new Color(1.0f, 0.5f, 0.0f);
                case RankTier.Ace:       return new Color(1.0f, 0.2f, 0.2f);
                case RankTier.Legend:    return new Color(0.8f, 0.2f, 1.0f);
                case RankTier.Skywalker: return new Color(0.0f, 0.9f, 1.0f);
                default:                 return Color.white;
            }
        }

        private static string CategoryShortName(SkillCategory category)
        {
            switch (category)
            {
                case SkillCategory.FlightHandling: return "flight";
                case SkillCategory.Exploration:    return "explore";
                case SkillCategory.Social:         return "social";
                case SkillCategory.Photography:    return "photo";
                case SkillCategory.Endurance:      return "endure";
                default:                           return category.ToString().ToLower();
            }
        }

        private static SkillEffect DefaultEffectForCategory(SkillCategory category)
        {
            switch (category)
            {
                case SkillCategory.FlightHandling: return SkillEffect.SpeedBoost;
                case SkillCategory.Exploration:    return SkillEffect.EventRadius;
                case SkillCategory.Social:         return SkillEffect.FormationBonus;
                case SkillCategory.Photography:    return SkillEffect.CameraRange;
                case SkillCategory.Endurance:      return SkillEffect.StaminaBoost;
                default:                           return SkillEffect.XPMultiplier;
            }
        }

        private static string RomanNumeral(int n)
        {
            // Simple helper for display: just return the number if > 10 to keep names readable
            if (n > 10) return n.ToString();
            string[] numerals = { "I","II","III","IV","V","VI","VII","VIII","IX","X" };
            return numerals[n - 1];
        }
    }
}
