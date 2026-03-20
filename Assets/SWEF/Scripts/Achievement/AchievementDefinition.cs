using UnityEngine;

namespace SWEF.Achievement
{
    /// <summary>Achievement tier from Bronze to Diamond.</summary>
    public enum AchievementTier
    {
        Bronze   = 0,
        Silver   = 1,
        Gold     = 2,
        Platinum = 3,
        Diamond  = 4
    }

    /// <summary>Achievement category.</summary>
    public enum AchievementCategory
    {
        Flight,
        Altitude,
        Speed,
        Exploration,
        Social,
        Collection,
        Challenge,
        Special
    }

    /// <summary>
    /// ScriptableObject that defines a single achievement's metadata.
    /// Create via <c>Assets > Create > SWEF > Achievement Definition</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAchievement", menuName = "SWEF/Achievement Definition")]
    public class AchievementDefinition : ScriptableObject
    {
        [Header("Identity")]
        /// <summary>Unique string identifier used as the persistence key.</summary>
        public string id;

        /// <summary>Localization key for the achievement title.</summary>
        public string titleKey;

        /// <summary>Localization key for the achievement description.</summary>
        public string descriptionKey;

        [Header("Visuals")]
        /// <summary>Icon displayed in the achievement gallery and notifications.</summary>
        public Sprite icon;

        [Header("Classification")]
        /// <summary>Rarity / prestige tier.</summary>
        public AchievementTier tier = AchievementTier.Bronze;

        /// <summary>Gameplay category used for gallery filtering.</summary>
        public AchievementCategory category = AchievementCategory.Flight;

        [Header("Progress")]
        /// <summary>
        /// Target value the player must accumulate before the achievement unlocks.
        /// Use 1 for boolean (unlock-once) achievements.
        /// </summary>
        public float targetValue = 1f;

        /// <summary>When true the achievement is hidden from the gallery until unlocked.</summary>
        public bool isHidden;

        [Header("Rewards")]
        /// <summary>Experience points awarded when the achievement is unlocked.</summary>
        public int xpReward = 50;
    }
}
