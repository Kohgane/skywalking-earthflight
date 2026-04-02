// DisasterData.cs — SWEF Natural Disaster & Dynamic World Events (Phase 86)
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.NaturalDisaster
{
    /// <summary>
    /// Phase 86 — ScriptableObject template that fully describes a type of natural disaster.
    ///
    /// <para>Create via <em>Assets → Create → SWEF/NaturalDisaster/Disaster Data</em>.
    /// Pass a DisasterData asset to <see cref="DisasterManager.SpawnDisaster"/> to launch a
    /// live instance.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/NaturalDisaster/Disaster Data", fileName = "NewDisasterData")]
    public class DisasterData : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────────

        [Header("Identity")]

        [Tooltip("Unique string identifier used in save data and scripting.")]
        public string disasterId;

        [Tooltip("Human-readable display name shown in the HUD and warnings.")]
        public string disasterName;

        [Tooltip("Primary category of this disaster.")]
        public DisasterType type;

        // ── Severity & Duration ───────────────────────────────────────────────────

        [Header("Severity & Duration")]

        [Tooltip("Maximum severity this disaster can reach.")]
        public DisasterSeverity maxSeverity = DisasterSeverity.Severe;

        [Tooltip("Base total duration in seconds.")]
        [Min(10f)]
        public float baseDuration = 300f;

        [Tooltip("How long the warning phase lasts in seconds.")]
        [Min(5f)]
        public float warningDuration = 60f;

        [Tooltip("Duration of peak intensity in seconds.")]
        [Min(5f)]
        public float peakDuration = 60f;

        [Tooltip("How long aftermath effects linger in seconds.")]
        [Min(5f)]
        public float aftermathDuration = 120f;

        // ── Hazard Zone ───────────────────────────────────────────────────────────

        [Header("Hazard Zone")]

        [Tooltip("Base hazard zone radius in metres.")]
        [Min(100f)]
        public float hazardRadius = 5000f;

        [Tooltip("Rate at which the hazard zone expands per second (metres/sec).")]
        [Min(0f)]
        public float expansionRate = 5f;

        [Tooltip("Hazard types this disaster produces.")]
        public List<HazardZoneType> hazardTypes = new List<HazardZoneType>();

        // ── Spawn Constraints ─────────────────────────────────────────────────────

        [Header("Spawn Constraints")]

        [Tooltip("BiomeType names where this disaster can spawn. Leave empty for any biome.")]
        public string[] validBiomes = new string[0];

        [Tooltip("Minimum (x) and maximum (y) altitude in metres affected by this disaster.")]
        public Vector2 altitudeRange = new Vector2(0f, 10000f);

        // ── Flight Effects ────────────────────────────────────────────────────────

        [Header("Flight Effects")]

        [Tooltip("Turbulence intensity multiplier applied while inside the hazard zone.")]
        [Range(0f, 5f)]
        public float turbulenceMultiplier = 1f;

        [Tooltip("Visibility reduction factor (0 = none, 1 = completely obscured).")]
        [Range(0f, 1f)]
        public float visibilityReduction = 0.3f;

        [Tooltip("Upward (+) or downward (−) thermal force in Newtons.")]
        public float thermalStrength = 0f;

        // ── Audio / Visual ────────────────────────────────────────────────────────

        [Header("Audio / Visual")]

        [Tooltip("Icon shown on the HUD tracker and minimap.")]
        public Sprite disasterIcon;

        [Tooltip("One-shot warning sound played when the disaster enters the Warning phase.")]
        public AudioClip warningSound;

        [Tooltip("Looping ambient sound played throughout the disaster's life.")]
        public AudioClip ambientLoop;

        [Tooltip("Resource path used to load the disaster visual prefab at runtime.")]
        public string visualPrefabPath;

        [Tooltip("Identifiers for atmospheric particle effects (e.g. \"ash\", \"smoke\", \"debris\").")]
        public string[] atmosphericEffects = new string[0];

        // ── Rescue Mission Integration ────────────────────────────────────────────

        [Header("Rescue Mission")]

        [Tooltip("When true, this disaster can auto-trigger a rescue mission.")]
        public bool canTriggerRescueMission = true;

        [Tooltip("Difficulty rating of auto-generated rescue missions (1 = easiest, 5 = hardest).")]
        [Range(1, 5)]
        public int rescueMissionDifficulty = 3;

        [Tooltip("Probability (0–1) that a rescue mission is auto-generated when this disaster reaches Onset.")]
        [Range(0f, 1f)]
        public float rescueMissionChance = 0.5f;
    }
}
