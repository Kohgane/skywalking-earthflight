using UnityEngine;

namespace SWEF.Progression
{
    /// <summary>
    /// ScriptableObject that configures XP rewards granted for various in-game activities.
    /// Load from <c>Resources/XPSourceConfig</c>, or call <see cref="GetDefault"/> for
    /// built-in sensible values when no asset is present.
    /// Create via <c>Assets → Create → SWEF → Progression → XPSourceConfig</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/Progression/XPSourceConfig", fileName = "XPSourceConfig")]
    public class XPSourceConfig : ScriptableObject
    {
        // ── Flight ────────────────────────────────────────────────────────────────
        [Header("Flight")]
        /// <summary>XP granted per minute of active flight.</summary>
        [SerializeField] public int xpPerFlightMinute = 10;

        /// <summary>XP granted per kilometre flown.</summary>
        [SerializeField] public int xpPerKmFlown = 2;

        // ── Activities ────────────────────────────────────────────────────────────
        [Header("Activities")]
        /// <summary>XP granted each time an achievement is unlocked.</summary>
        [SerializeField] public int xpPerAchievementUnlock = 100;

        /// <summary>XP granted upon completing a world event.</summary>
        [SerializeField] public int xpPerEventCompletion = 150;

        /// <summary>XP granted at the end of a multiplayer session.</summary>
        [SerializeField] public int xpPerMultiplayerSession = 75;

        /// <summary>XP granted per minute spent flying in formation with others.</summary>
        [SerializeField] public int xpPerFormationMinute = 15;

        /// <summary>XP granted upon completing a guided tour.</summary>
        [SerializeField] public int xpPerTourCompleted = 200;

        /// <summary>XP granted each time the player takes a photo (screenshot).</summary>
        [SerializeField] public int xpPerPhotoTaken = 20;

        /// <summary>XP granted when a replay is shared.</summary>
        [SerializeField] public int xpPerReplayShared = 30;

        // ── Daily Bonus ───────────────────────────────────────────────────────────
        [Header("Bonuses")]
        /// <summary>Flat XP bonus awarded on the player's first flight of each calendar day.</summary>
        [SerializeField] public int xpBonusFirstFlightOfDay = 50;

        /// <summary>XP multiplier active on Saturdays and Sundays (default 1.0 = no bonus).</summary>
        [SerializeField] public float xpMultiplierWeekend = 1.0f;

        /// <summary>XP multiplier active during special in-game events (default 1.5).</summary>
        [SerializeField] public float xpMultiplierEvent = 1.5f;

        // ── Static factory ────────────────────────────────────────────────────────

        /// <summary>
        /// Creates and returns a new <see cref="XPSourceConfig"/> instance populated with
        /// sensible default values.  Used as a fallback when no asset exists in Resources.
        /// </summary>
        public static XPSourceConfig GetDefault()
        {
            var cfg = CreateInstance<XPSourceConfig>();
            cfg.xpPerFlightMinute        = 10;
            cfg.xpPerKmFlown             = 2;
            cfg.xpPerAchievementUnlock   = 100;
            cfg.xpPerEventCompletion     = 150;
            cfg.xpPerMultiplayerSession  = 75;
            cfg.xpPerFormationMinute     = 15;
            cfg.xpPerTourCompleted       = 200;
            cfg.xpPerPhotoTaken          = 20;
            cfg.xpPerReplayShared        = 30;
            cfg.xpBonusFirstFlightOfDay  = 50;
            cfg.xpMultiplierWeekend      = 1.0f;
            cfg.xpMultiplierEvent        = 1.5f;
            return cfg;
        }
    }
}
