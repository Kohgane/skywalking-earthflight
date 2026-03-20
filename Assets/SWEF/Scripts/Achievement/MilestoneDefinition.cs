using UnityEngine;

namespace SWEF.Achievement
{
    /// <summary>
    /// ScriptableObject that defines a meta-achievement (milestone).
    /// A milestone is completed when all of its required achievements have been unlocked.
    /// Create via <c>Assets > Create > SWEF > Milestone Definition</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMilestone", menuName = "SWEF/Milestone Definition")]
    public class MilestoneDefinition : ScriptableObject
    {
        [Header("Identity")]
        /// <summary>Unique identifier for this milestone.</summary>
        public string id;

        /// <summary>Localization key for the milestone title.</summary>
        public string titleKey;

        /// <summary>Localization key for the milestone description.</summary>
        public string descriptionKey;

        [Header("Visuals")]
        /// <summary>Icon displayed in the milestone completed popup.</summary>
        public Sprite icon;

        [Header("Requirements")]
        /// <summary>All of these achievement IDs must be unlocked for the milestone to complete.</summary>
        public string[] requiredAchievementIds = System.Array.Empty<string>();

        [Header("Rewards")]
        /// <summary>Bonus XP awarded on milestone completion (in addition to individual achievement XP).</summary>
        public int bonusXP = 200;

        /// <summary>Visual tier of this milestone.</summary>
        public AchievementTier tier = AchievementTier.Gold;

        // ── Computed ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> when every required achievement is unlocked.
        /// </summary>
        public bool IsComplete(AchievementManager mgr)
        {
            if (mgr == null) return false;
            foreach (var id in requiredAchievementIds)
                if (!mgr.IsUnlocked(id)) return false;
            return true;
        }
    }
}
