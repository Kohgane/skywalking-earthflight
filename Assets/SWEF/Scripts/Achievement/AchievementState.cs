using UnityEngine;

namespace SWEF.Achievement
{
    /// <summary>
    /// Serializable runtime state for a single achievement.
    /// Persisted as part of the achievements.json save file.
    /// </summary>
    [System.Serializable]
    public class AchievementState
    {
        /// <summary>Matches <see cref="AchievementDefinition.id"/>.</summary>
        public string achievementId;

        /// <summary>Accumulated progress toward <c>targetValue</c>.</summary>
        public float currentValue;

        /// <summary>Whether the achievement has been unlocked.</summary>
        public bool unlocked;

        /// <summary>ISO-8601 timestamp at which the achievement was unlocked (empty if not yet unlocked).</summary>
        public string unlockDateISO;

        /// <summary>Whether the unlock notification popup has already been shown to the player.</summary>
        public bool notificationShown;

        // ── Computed ──────────────────────────────────────────────────────────────

        // Backing reference set by AchievementManager after load.
        [System.NonSerialized]
        private float _targetValue = 1f;

        /// <summary>Sets the target value so that <see cref="Progress01"/> can be computed.</summary>
        public void SetTarget(float target) => _targetValue = Mathf.Max(target, 0.0001f);

        /// <summary>Normalised progress in the range [0, 1].</summary>
        public float Progress01 => Mathf.Clamp01(currentValue / _targetValue);
    }
}
