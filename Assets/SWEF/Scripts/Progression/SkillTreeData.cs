using System;
using UnityEngine;

namespace SWEF.Progression
{
    // ── Enums ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Broad category grouping for skill tree nodes.
    /// </summary>
    public enum SkillCategory
    {
        /// <summary>Skills that improve aircraft handling and control.</summary>
        FlightHandling,
        /// <summary>Skills that enhance world exploration capabilities.</summary>
        Exploration,
        /// <summary>Skills related to multiplayer and social interactions.</summary>
        Social,
        /// <summary>Skills that improve the in-game camera and photography.</summary>
        Photography,
        /// <summary>Skills that boost stamina and endurance-related stats.</summary>
        Endurance
    }

    /// <summary>
    /// The gameplay effect a skill node applies when unlocked.
    /// </summary>
    public enum SkillEffect
    {
        /// <summary>Increases maximum aircraft speed.</summary>
        SpeedBoost,
        /// <summary>Improves yaw/pitch/roll responsiveness.</summary>
        TurnRateBoost,
        /// <summary>Raises the effective altitude ceiling.</summary>
        AltitudeBoost,
        /// <summary>Multiplies all XP gains by a percentage bonus.</summary>
        XPMultiplier,
        /// <summary>Reduces energy drain and increases endurance.</summary>
        StaminaBoost,
        /// <summary>Extends the photo/screenshot zoom range.</summary>
        CameraRange,
        /// <summary>Extends the detection radius for nearby players.</summary>
        SocialRange,
        /// <summary>Expands the area in which world events can be detected and joined.</summary>
        EventRadius,
        /// <summary>Increases XP and score earned from formation flying.</summary>
        FormationBonus,
        /// <summary>Reduces speed and handling penalties during adverse weather.</summary>
        WeatherResistance
    }

    // ── ScriptableObject ─────────────────────────────────────────────────────────

    /// <summary>
    /// Defines a single node in the pilot skill tree.
    /// Create via <c>Assets → Create → SWEF → Progression → SkillTreeData</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/Progression/SkillTreeData", fileName = "SkillTreeData")]
    public class SkillTreeData : ScriptableObject
    {
        // ── Identity ───────────────────────────────────────────────────────────

        [Header("Identity")]
        /// <summary>Unique identifier for this skill (e.g. "skill_speed_1").</summary>
        [SerializeField] public string skillId;

        /// <summary>Localization key for the skill name.</summary>
        [SerializeField] public string skillNameKey;

        /// <summary>Localization key for the skill description.</summary>
        [SerializeField] public string descriptionKey;

        // ── Tree structure ─────────────────────────────────────────────────────

        [Header("Tree Structure")]
        /// <summary>Category this skill belongs to.</summary>
        [SerializeField] public SkillCategory category;

        /// <summary>Tier within its category (1 = base, 5 = apex).</summary>
        [Range(1, 5)]
        [SerializeField] public int tier = 1;

        /// <summary>Skill-point cost to unlock this node.</summary>
        [SerializeField] public int skillPointCost = 1;

        /// <summary>
        /// Skill IDs that must be unlocked before this node becomes available.
        /// Empty array means no prerequisites.
        /// </summary>
        [SerializeField] public string[] prerequisiteSkillIds = Array.Empty<string>();

        // ── Effect ─────────────────────────────────────────────────────────────

        [Header("Effect")]
        /// <summary>The gameplay effect this skill applies.</summary>
        [SerializeField] public SkillEffect effect;

        /// <summary>
        /// Magnitude of the effect — interpret as a percentage bonus (e.g. 0.05 = +5 %)
        /// or a flat value depending on the <see cref="effect"/> type.
        /// </summary>
        [SerializeField] public float effectValue;
    }
}
