using System.Collections.Generic;
using UnityEngine;

namespace SWEF.DailyChallenge
{
    /// <summary>
    /// Static helper that generates default runtime data for the Daily Challenge system.
    /// Used as a fallback when no ScriptableObject assets are present in Resources.
    /// Provides 30+ daily challenge definitions, 10 weekly challenge definitions,
    /// and a complete Season 1 ("Sky Pioneer") definition with 50 tiers.
    /// </summary>
    public static class DailyChallengeDefaultData
    {
        // ── Daily Challenges ──────────────────────────────────────────────────────

        /// <summary>
        /// Returns a list of 30+ default daily challenge definitions covering all types
        /// and all four difficulty tiers.
        /// </summary>
        public static List<DailyChallengeDefinition> GetDefaultDailyChallenges()
        {
            var list = new List<DailyChallengeDefinition>();

            // ── Easy ──────────────────────────────────────────────────────────────
            list.Add(Make("fly_5km_easy",        "challenge_fly_5km_title",       "challenge_fly_5km_desc",
                ChallengeType.FlyDistance,   ChallengeDifficulty.Easy,   5f,     100,  50,  10, Color.green));
            list.Add(Make("alt_1000m_easy",      "challenge_alt_1000m_title",     "challenge_alt_1000m_desc",
                ChallengeType.ReachAltitude, ChallengeDifficulty.Easy,   1000f,  80,   30,  8,  new Color(0.4f, 0.8f, 1f)));
            list.Add(Make("fly_5min_easy",       "challenge_fly_5min_title",      "challenge_fly_5min_desc",
                ChallengeType.FlyDuration,   ChallengeDifficulty.Easy,   300f,   90,   40,  9,  Color.cyan));
            list.Add(Make("photo_1_easy",        "challenge_photo_1_title",       "challenge_photo_1_desc",
                ChallengeType.TakePhotos,    ChallengeDifficulty.Easy,   1f,     75,   25,  7,  new Color(1f, 0.9f, 0.4f)));
            list.Add(Make("visit_1_easy",        "challenge_visit_1_title",       "challenge_visit_1_desc",
                ChallengeType.VisitLocations,ChallengeDifficulty.Easy,   1f,     85,   35,  8,  new Color(0.6f, 1f, 0.6f)));
            list.Add(Make("speed_100_easy",      "challenge_speed_100_title",     "challenge_speed_100_desc",
                ChallengeType.AchieveSpeed,  ChallengeDifficulty.Easy,   100f,   70,   20,  7,  new Color(1f, 0.5f, 0.2f)));
            list.Add(Make("skill_1_easy",        "challenge_skill_1_title",       "challenge_skill_1_desc",
                ChallengeType.UseSkill,      ChallengeDifficulty.Easy,   1f,     60,   20,  6,  new Color(0.8f, 0.6f, 1f)));

            // ── Medium ────────────────────────────────────────────────────────────
            list.Add(Make("fly_25km_med",        "challenge_fly_25km_title",      "challenge_fly_25km_desc",
                ChallengeType.FlyDistance,   ChallengeDifficulty.Medium, 25f,    250, 100, 25, Color.green));
            list.Add(Make("alt_5000m_med",       "challenge_alt_5000m_title",     "challenge_alt_5000m_desc",
                ChallengeType.ReachAltitude, ChallengeDifficulty.Medium, 5000f,  200,  80, 20, new Color(0.4f, 0.8f, 1f)));
            list.Add(Make("fly_15min_med",       "challenge_fly_15min_title",     "challenge_fly_15min_desc",
                ChallengeType.FlyDuration,   ChallengeDifficulty.Medium, 900f,   220,  90, 22, Color.cyan));
            list.Add(Make("visit_3_med",         "challenge_visit_3_title",       "challenge_visit_3_desc",
                ChallengeType.VisitLocations,ChallengeDifficulty.Medium, 3f,     210,  85, 21, new Color(0.6f, 1f, 0.6f)));
            list.Add(Make("formation_1_med",     "challenge_formation_1_title",   "challenge_formation_1_desc",
                ChallengeType.CompleteFormation, ChallengeDifficulty.Medium, 1f, 230,  95, 23, new Color(1f, 0.8f, 0.2f)));
            list.Add(Make("photo_3_med",         "challenge_photo_3_title",       "challenge_photo_3_desc",
                ChallengeType.TakePhotos,    ChallengeDifficulty.Medium, 3f,     180,  70, 18, new Color(1f, 0.9f, 0.4f)));
            list.Add(Make("speed_300_med",       "challenge_speed_300_title",     "challenge_speed_300_desc",
                ChallengeType.AchieveSpeed,  ChallengeDifficulty.Medium, 300f,   200,  80, 20, new Color(1f, 0.5f, 0.2f)));

            // ── Hard ──────────────────────────────────────────────────────────────
            list.Add(Make("fly_100km_hard",      "challenge_fly_100km_title",     "challenge_fly_100km_desc",
                ChallengeType.FlyDistance,   ChallengeDifficulty.Hard,   100f,   500, 200, 50, Color.green));
            list.Add(Make("alt_20000m_hard",     "challenge_alt_20000m_title",    "challenge_alt_20000m_desc",
                ChallengeType.ReachAltitude, ChallengeDifficulty.Hard,   20000f, 450, 180, 45, new Color(0.4f, 0.8f, 1f)));
            list.Add(Make("fly_30min_hard",      "challenge_fly_30min_title",     "challenge_fly_30min_desc",
                ChallengeType.FlyDuration,   ChallengeDifficulty.Hard,   1800f,  480, 190, 48, Color.cyan));
            list.Add(Make("speed_500_hard",      "challenge_speed_500_title",     "challenge_speed_500_desc",
                ChallengeType.AchieveSpeed,  ChallengeDifficulty.Hard,   500f,   460, 185, 46, new Color(1f, 0.5f, 0.2f)));
            list.Add(Make("tour_1_hard",         "challenge_tour_1_title",        "challenge_tour_1_desc",
                ChallengeType.CompleteTour,  ChallengeDifficulty.Hard,   1f,     490, 195, 49, new Color(0.8f, 0.4f, 1f)));
            list.Add(Make("visit_5_hard",        "challenge_visit_5_title",       "challenge_visit_5_desc",
                ChallengeType.VisitLocations,ChallengeDifficulty.Hard,   5f,     440, 175, 44, new Color(0.6f, 1f, 0.6f)));
            list.Add(Make("photo_10_hard",       "challenge_photo_10_title",      "challenge_photo_10_desc",
                ChallengeType.TakePhotos,    ChallengeDifficulty.Hard,   10f,    430, 170, 43, new Color(1f, 0.9f, 0.4f)));

            // ── Elite ─────────────────────────────────────────────────────────────
            list.Add(Make("fly_300km_elite",     "challenge_fly_300km_title",     "challenge_fly_300km_desc",
                ChallengeType.FlyDistance,   ChallengeDifficulty.Elite,  300f,  1000, 400, 100, new Color(1f, 0.84f, 0f)));
            list.Add(Make("karman_elite",        "challenge_karman_title",        "challenge_karman_desc",
                ChallengeType.ReachAltitude, ChallengeDifficulty.Elite, 100000f, 1200, 500, 120, new Color(0.2f, 0.6f, 1f)));
            list.Add(Make("fly_60min_elite",     "challenge_fly_60min_title",     "challenge_fly_60min_desc",
                ChallengeType.FlyDuration,   ChallengeDifficulty.Elite,  3600f,  900, 360,  90, Color.cyan));
            list.Add(Make("mp_5_elite",          "challenge_mp_5_title",          "challenge_mp_5_desc",
                ChallengeType.PlayMultiplayer,ChallengeDifficulty.Elite, 5f,    1100, 450, 110, new Color(1f, 0.3f, 0.3f)));
            list.Add(Make("formation_3_elite",   "challenge_formation_3_title",   "challenge_formation_3_desc",
                ChallengeType.CompleteFormation,ChallengeDifficulty.Elite,3f,    950, 380,  95, new Color(1f, 0.8f, 0.2f)));
            list.Add(Make("tour_3_elite",        "challenge_tour_3_title",        "challenge_tour_3_desc",
                ChallengeType.CompleteTour,  ChallengeDifficulty.Elite,  3f,   1050, 420, 105, new Color(0.8f, 0.4f, 1f)));
            list.Add(Make("speed_1000_elite",    "challenge_speed_1000_title",    "challenge_speed_1000_desc",
                ChallengeType.AchieveSpeed,  ChallengeDifficulty.Elite,  1000f,  980, 390,  98, new Color(1f, 0.3f, 0f), 20));
            list.Add(Make("photo_20_elite",      "challenge_photo_20_title",      "challenge_photo_20_desc",
                ChallengeType.TakePhotos,    ChallengeDifficulty.Elite,  20f,    850, 340,  85, new Color(1f, 0.9f, 0.4f)));

            return list;
        }

        // ── Weekly Challenges ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns 10 default weekly challenge definitions with higher targets and rewards.
        /// </summary>
        public static List<WeeklyChallengeDefinition> GetDefaultWeeklyChallenges()
        {
            var list = new List<WeeklyChallengeDefinition>();

            list.Add(MakeWeekly("weekly_fly_500km",    "weekly_fly_500km_title",    "weekly_fly_500km_desc",
                ChallengeType.FlyDistance,   500f,  2000, 800, 300));
            list.Add(MakeWeekly("weekly_alt_50km",     "weekly_alt_50km_title",     "weekly_alt_50km_desc",
                ChallengeType.ReachAltitude, 50000f, 2500, 1000, 375));
            list.Add(MakeWeekly("weekly_fly_3h",       "weekly_fly_3h_title",       "weekly_fly_3h_desc",
                ChallengeType.FlyDuration,   10800f, 1800, 720, 270));
            list.Add(MakeWeekly("weekly_speed_mach2",  "weekly_speed_mach2_title",  "weekly_speed_mach2_desc",
                ChallengeType.AchieveSpeed,  2450f,  2200, 880, 330));
            list.Add(MakeWeekly("weekly_visit_10",     "weekly_visit_10_title",     "weekly_visit_10_desc",
                ChallengeType.VisitLocations,10f,    1600, 640, 240));
            list.Add(MakeWeekly("weekly_photo_25",     "weekly_photo_25_title",     "weekly_photo_25_desc",
                ChallengeType.TakePhotos,    25f,    1500, 600, 225));
            list.Add(MakeWeekly("weekly_tour_5",       "weekly_tour_5_title",       "weekly_tour_5_desc",
                ChallengeType.CompleteTour,  5f,     2400, 960, 360));
            list.Add(MakeWeekly("weekly_formation_5",  "weekly_formation_5_title",  "weekly_formation_5_desc",
                ChallengeType.CompleteFormation, 5f, 2300, 920, 345, "cosmetic_trail_rainbow"));
            list.Add(MakeWeekly("weekly_mp_10",        "weekly_mp_10_title",        "weekly_mp_10_desc",
                ChallengeType.PlayMultiplayer, 10f,  2600, 1040, 390, "cosmetic_badge_squad"));
            list.Add(MakeWeekly("weekly_skill_5",      "weekly_skill_5_title",      "weekly_skill_5_desc",
                ChallengeType.UseSkill,      5f,     1400, 560, 210));

            return list;
        }

        // ── Season 1 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a <see cref="SeasonDefinition"/> for Season 1 ("Sky Pioneer")
        /// with 50 tiers of free and premium rewards.
        /// </summary>
        public static SeasonDefinition GetDefaultSeason()
        {
            var season = ScriptableObject.CreateInstance<SeasonDefinition>();
            season.seasonId          = "season_1";
            season.seasonNameKey     = "season_1_name";
            season.seasonDescriptionKey = "season_1_desc";
            season.startDate         = "2026-01-01T00:00:00Z";
            season.endDate           = "2026-12-31T23:59:59Z";
            season.totalTiers        = 50;
            season.pointsPerTier     = 100;
            season.themeColor        = new Color(0.2f, 0.6f, 1f);

            // ── Free track ────────────────────────────────────────────────────────
            for (int tier = 1; tier <= 50; tier++)
            {
                var r = new SeasonReward { tier = tier };
                if (tier % 10 == 0)
                {
                    // Every 10th tier: cosmetic.
                    r.rewardType    = SeasonRewardType.Cosmetic;
                    r.rewardId      = $"cosmetic_season1_free_t{tier}";
                    r.amount        = 1;
                    r.displayNameKey = $"season_free_reward_t{tier}";
                }
                else if (tier % 5 == 0)
                {
                    // Every 5th tier: currency.
                    r.rewardType    = SeasonRewardType.Currency;
                    r.amount        = tier * 20;
                    r.displayNameKey = $"season_free_reward_t{tier}";
                }
                else
                {
                    // Standard tiers: XP.
                    r.rewardType    = SeasonRewardType.XP;
                    r.amount        = tier * 100;
                    r.displayNameKey = $"season_free_reward_t{tier}";
                }
                season.freeRewards.Add(r);
            }

            // ── Premium track ─────────────────────────────────────────────────────
            for (int tier = 1; tier <= 50; tier++)
            {
                var r = new SeasonReward { tier = tier };
                if (tier % 10 == 0)
                {
                    // Exclusive cosmetic every 10 tiers.
                    r.rewardType    = SeasonRewardType.Cosmetic;
                    r.rewardId      = $"cosmetic_season1_premium_t{tier}";
                    r.amount        = 1;
                    r.displayNameKey = $"season_premium_reward_t{tier}";
                }
                else if (tier == 25 || tier == 50)
                {
                    // Mid-season and end-season exclusive titles.
                    r.rewardType    = SeasonRewardType.Title;
                    r.rewardId      = tier == 25 ? "title_sky_veteran" : "title_sky_pioneer";
                    r.amount        = 1;
                    r.displayNameKey = tier == 25 ? "season_title_sky_veteran" : "season_title_sky_pioneer";
                }
                else if (tier % 7 == 0)
                {
                    // Skill point every 7 tiers.
                    r.rewardType    = SeasonRewardType.SkillPoint;
                    r.amount        = 1;
                    r.displayNameKey = $"season_premium_reward_t{tier}";
                }
                else if (tier % 3 == 0)
                {
                    // Currency every 3 tiers.
                    r.rewardType    = SeasonRewardType.Currency;
                    r.amount        = tier * 40;
                    r.displayNameKey = $"season_premium_reward_t{tier}";
                }
                else
                {
                    // XP (higher than free).
                    r.rewardType    = SeasonRewardType.XP;
                    r.amount        = tier * 200;
                    r.displayNameKey = $"season_premium_reward_t{tier}";
                }
                season.premiumRewards.Add(r);
            }

            return season;
        }

        // ── Private factories ─────────────────────────────────────────────────────

        private static DailyChallengeDefinition Make(
            string id, string titleKey, string descKey,
            ChallengeType type, ChallengeDifficulty diff,
            float target, int xp, int currency, int sp,
            Color color, int requiredRank = 1)
        {
            var def = ScriptableObject.CreateInstance<DailyChallengeDefinition>();
            def.challengeId        = id;
            def.titleKey           = titleKey;
            def.descriptionKey     = descKey;
            def.challengeType      = type;
            def.difficulty         = diff;
            def.targetValue        = target;
            def.baseXPReward       = xp;
            def.baseCurrencyReward = currency;
            def.seasonPointReward  = sp;
            def.iconColor          = color;
            def.requiredRankLevel  = requiredRank;
            return def;
        }

        private static WeeklyChallengeDefinition MakeWeekly(
            string id, string titleKey, string descKey,
            ChallengeType type, float target,
            int xp, int currency, int sp,
            string cosmeticId = null)
        {
            var def = ScriptableObject.CreateInstance<WeeklyChallengeDefinition>();
            def.challengeId       = id;
            def.titleKey          = titleKey;
            def.descriptionKey    = descKey;
            def.challengeType     = type;
            def.targetValue       = target;
            def.xpReward          = xp;
            def.currencyReward    = currency;
            def.seasonPointReward = sp;
            def.bonusCosmeticId   = cosmeticId ?? string.Empty;
            def.validDurationDays = 7;
            return def;
        }
    }
}
