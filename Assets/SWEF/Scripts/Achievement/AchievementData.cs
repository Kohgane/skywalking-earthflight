using System.Collections.Generic;

namespace SWEF.Achievement
{
    /// <summary>
    /// Describes a default achievement used to pre-populate ScriptableObjects via the
    /// <see cref="AchievementEditorWindow"/>.
    /// </summary>
    public struct AchievementDefaultInfo
    {
        public string              id;
        public string              titleKey;
        public string              descriptionKey;
        public AchievementCategory category;
        public AchievementTier     tier;
        public float               targetValue;
        public int                 xpReward;
        public bool                isHidden;
    }

    /// <summary>
    /// Static helper that returns the 30 default achievement definitions shipped
    /// with SWEF Phase 31.
    /// </summary>
    public static class AchievementData
    {
        /// <summary>Returns all 30 default achievement definitions.</summary>
        public static List<AchievementDefaultInfo> GetDefaults()
        {
            return new List<AchievementDefaultInfo>
            {
                // ── Flight (5) ────────────────────────────────────────────────────
                new AchievementDefaultInfo
                {
                    id = "first_flight", titleKey = "ach_first_flight_title",
                    descriptionKey = "ach_first_flight_desc",
                    category = AchievementCategory.Flight, tier = AchievementTier.Bronze,
                    targetValue = 1f, xpReward = 25
                },
                new AchievementDefaultInfo
                {
                    id = "flight_time_1h", titleKey = "ach_flight_time_1h_title",
                    descriptionKey = "ach_flight_time_1h_desc",
                    category = AchievementCategory.Flight, tier = AchievementTier.Bronze,
                    targetValue = 3600f, xpReward = 50
                },
                new AchievementDefaultInfo
                {
                    id = "flight_time_10h", titleKey = "ach_flight_time_10h_title",
                    descriptionKey = "ach_flight_time_10h_desc",
                    category = AchievementCategory.Flight, tier = AchievementTier.Silver,
                    targetValue = 36000f, xpReward = 150
                },
                new AchievementDefaultInfo
                {
                    id = "flight_time_100h", titleKey = "ach_flight_time_100h_title",
                    descriptionKey = "ach_flight_time_100h_desc",
                    category = AchievementCategory.Flight, tier = AchievementTier.Gold,
                    targetValue = 360000f, xpReward = 500
                },
                new AchievementDefaultInfo
                {
                    id = "flight_marathon", titleKey = "ach_flight_marathon_title",
                    descriptionKey = "ach_flight_marathon_desc",
                    category = AchievementCategory.Flight, tier = AchievementTier.Platinum,
                    targetValue = 1800f, xpReward = 200
                },

                // ── Altitude (5) ──────────────────────────────────────────────────
                new AchievementDefaultInfo
                {
                    id = "altitude_1km", titleKey = "ach_altitude_1km_title",
                    descriptionKey = "ach_altitude_1km_desc",
                    category = AchievementCategory.Altitude, tier = AchievementTier.Bronze,
                    targetValue = 1000f, xpReward = 25
                },
                new AchievementDefaultInfo
                {
                    id = "altitude_10km", titleKey = "ach_altitude_10km_title",
                    descriptionKey = "ach_altitude_10km_desc",
                    category = AchievementCategory.Altitude, tier = AchievementTier.Bronze,
                    targetValue = 10000f, xpReward = 50
                },
                new AchievementDefaultInfo
                {
                    id = "altitude_50km", titleKey = "ach_altitude_50km_title",
                    descriptionKey = "ach_altitude_50km_desc",
                    category = AchievementCategory.Altitude, tier = AchievementTier.Silver,
                    targetValue = 50000f, xpReward = 150
                },
                new AchievementDefaultInfo
                {
                    id = "altitude_100km", titleKey = "ach_altitude_100km_title",
                    descriptionKey = "ach_altitude_100km_desc",
                    category = AchievementCategory.Altitude, tier = AchievementTier.Gold,
                    targetValue = 100000f, xpReward = 300
                },
                new AchievementDefaultInfo
                {
                    id = "altitude_karman", titleKey = "ach_altitude_karman_title",
                    descriptionKey = "ach_altitude_karman_desc",
                    category = AchievementCategory.Altitude, tier = AchievementTier.Diamond,
                    targetValue = 100000f, xpReward = 1000, isHidden = true
                },

                // ── Speed (4) ─────────────────────────────────────────────────────
                new AchievementDefaultInfo
                {
                    id = "speed_100", titleKey = "ach_speed_100_title",
                    descriptionKey = "ach_speed_100_desc",
                    category = AchievementCategory.Speed, tier = AchievementTier.Bronze,
                    targetValue = 100f, xpReward = 25
                },
                new AchievementDefaultInfo
                {
                    id = "speed_500", titleKey = "ach_speed_500_title",
                    descriptionKey = "ach_speed_500_desc",
                    category = AchievementCategory.Speed, tier = AchievementTier.Silver,
                    targetValue = 500f, xpReward = 100
                },
                new AchievementDefaultInfo
                {
                    id = "speed_mach1", titleKey = "ach_speed_mach1_title",
                    descriptionKey = "ach_speed_mach1_desc",
                    category = AchievementCategory.Speed, tier = AchievementTier.Gold,
                    targetValue = 343f, xpReward = 300
                },
                new AchievementDefaultInfo
                {
                    id = "speed_mach5", titleKey = "ach_speed_mach5_title",
                    descriptionKey = "ach_speed_mach5_desc",
                    category = AchievementCategory.Speed, tier = AchievementTier.Diamond,
                    targetValue = 1715f, xpReward = 750, isHidden = true
                },

                // ── Exploration (5) ───────────────────────────────────────────────
                new AchievementDefaultInfo
                {
                    id = "explore_1", titleKey = "ach_explore_1_title",
                    descriptionKey = "ach_explore_1_desc",
                    category = AchievementCategory.Exploration, tier = AchievementTier.Bronze,
                    targetValue = 1f, xpReward = 25
                },
                new AchievementDefaultInfo
                {
                    id = "explore_5", titleKey = "ach_explore_5_title",
                    descriptionKey = "ach_explore_5_desc",
                    category = AchievementCategory.Exploration, tier = AchievementTier.Bronze,
                    targetValue = 5f, xpReward = 75
                },
                new AchievementDefaultInfo
                {
                    id = "explore_25", titleKey = "ach_explore_25_title",
                    descriptionKey = "ach_explore_25_desc",
                    category = AchievementCategory.Exploration, tier = AchievementTier.Silver,
                    targetValue = 25f, xpReward = 200
                },
                new AchievementDefaultInfo
                {
                    id = "explore_50", titleKey = "ach_explore_50_title",
                    descriptionKey = "ach_explore_50_desc",
                    category = AchievementCategory.Exploration, tier = AchievementTier.Gold,
                    targetValue = 50f, xpReward = 400
                },
                new AchievementDefaultInfo
                {
                    id = "explore_100", titleKey = "ach_explore_100_title",
                    descriptionKey = "ach_explore_100_desc",
                    category = AchievementCategory.Exploration, tier = AchievementTier.Platinum,
                    targetValue = 100f, xpReward = 750
                },

                // ── Distance (4) ──────────────────────────────────────────────────
                new AchievementDefaultInfo
                {
                    id = "distance_100km", titleKey = "ach_distance_100km_title",
                    descriptionKey = "ach_distance_100km_desc",
                    category = AchievementCategory.Flight, tier = AchievementTier.Bronze,
                    targetValue = 100000f, xpReward = 50
                },
                new AchievementDefaultInfo
                {
                    id = "distance_1000km", titleKey = "ach_distance_1000km_title",
                    descriptionKey = "ach_distance_1000km_desc",
                    category = AchievementCategory.Flight, tier = AchievementTier.Silver,
                    targetValue = 1000000f, xpReward = 200
                },
                new AchievementDefaultInfo
                {
                    id = "distance_10000km", titleKey = "ach_distance_10000km_title",
                    descriptionKey = "ach_distance_10000km_desc",
                    category = AchievementCategory.Flight, tier = AchievementTier.Gold,
                    targetValue = 10000000f, xpReward = 500
                },
                new AchievementDefaultInfo
                {
                    id = "distance_earth_circumference", titleKey = "ach_distance_earth_circumference_title",
                    descriptionKey = "ach_distance_earth_circumference_desc",
                    category = AchievementCategory.Flight, tier = AchievementTier.Diamond,
                    targetValue = 40075000f, xpReward = 1500, isHidden = true
                },

                // ── Collection (3) ────────────────────────────────────────────────
                new AchievementDefaultInfo
                {
                    id = "screenshots_10", titleKey = "ach_screenshots_10_title",
                    descriptionKey = "ach_screenshots_10_desc",
                    category = AchievementCategory.Collection, tier = AchievementTier.Bronze,
                    targetValue = 10f, xpReward = 75
                },
                new AchievementDefaultInfo
                {
                    id = "screenshots_50", titleKey = "ach_screenshots_50_title",
                    descriptionKey = "ach_screenshots_50_desc",
                    category = AchievementCategory.Collection, tier = AchievementTier.Silver,
                    targetValue = 50f, xpReward = 200
                },
                new AchievementDefaultInfo
                {
                    id = "favorites_25", titleKey = "ach_favorites_25_title",
                    descriptionKey = "ach_favorites_25_desc",
                    category = AchievementCategory.Collection, tier = AchievementTier.Silver,
                    targetValue = 25f, xpReward = 150
                },

                // ── Challenge (2) ─────────────────────────────────────────────────
                new AchievementDefaultInfo
                {
                    id = "comfort_mode_space", titleKey = "ach_comfort_mode_space_title",
                    descriptionKey = "ach_comfort_mode_space_desc",
                    category = AchievementCategory.Challenge, tier = AchievementTier.Gold,
                    targetValue = 1f, xpReward = 300, isHidden = true
                },
                new AchievementDefaultInfo
                {
                    id = "night_flight_karman", titleKey = "ach_night_flight_karman_title",
                    descriptionKey = "ach_night_flight_karman_desc",
                    category = AchievementCategory.Challenge, tier = AchievementTier.Gold,
                    targetValue = 1f, xpReward = 500, isHidden = true
                },

                // ── Special (2) ───────────────────────────────────────────────────
                new AchievementDefaultInfo
                {
                    id = "all_continents", titleKey = "ach_all_continents_title",
                    descriptionKey = "ach_all_continents_desc",
                    category = AchievementCategory.Special, tier = AchievementTier.Platinum,
                    targetValue = 7f, xpReward = 1000, isHidden = true
                },
                new AchievementDefaultInfo
                {
                    id = "speed_run_space", titleKey = "ach_speed_run_space_title",
                    descriptionKey = "ach_speed_run_space_desc",
                    category = AchievementCategory.Special, tier = AchievementTier.Diamond,
                    targetValue = 1f, xpReward = 2000, isHidden = true
                }
            };
        }
    }
}
