// TerrainEventConfig.cs — SWEF Dynamic Terrain Events & Geological Phenomena (Phase 105)
using UnityEngine;

namespace SWEF.TerrainEvents
{
    /// <summary>
    /// Phase 105 — ScriptableObject template that fully describes a type of terrain event.
    ///
    /// <para>Create via <em>Assets → Create → SWEF/TerrainEvents/Terrain Event Config</em>.
    /// Supply to <see cref="TerrainEventManager.SpawnEvent"/> to launch a live instance.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/TerrainEvents/Terrain Event Config", fileName = "NewTerrainEventConfig")]
    public class TerrainEventConfig : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────────

        [Header("Identity")]

        [Tooltip("Unique string identifier used in save data and achievement scripting.")]
        public string eventId;

        [Tooltip("Human-readable display name shown in mission/achievement UI.")]
        public string eventName;

        [Tooltip("Category of this terrain event.")]
        public TerrainEventType eventType;

        // ── Intensity & Duration ──────────────────────────────────────────────────

        [Header("Intensity & Duration")]

        [Tooltip("Maximum intensity this event can reach.")]
        public TerrainEventIntensity maxIntensity = TerrainEventIntensity.Strong;

        [Tooltip("Base total duration in seconds.")]
        [Min(10f)]
        public float baseDuration = 120f;

        [Tooltip("How long the build-up phase lasts in seconds.")]
        [Min(5f)]
        public float buildUpDuration = 30f;

        [Tooltip("Duration of peak intensity in seconds.")]
        [Min(5f)]
        public float peakDuration = 60f;

        [Tooltip("How long the subsiding / aftermath phase lasts in seconds.")]
        [Min(5f)]
        public float subsidingDuration = 30f;

        // ── Spatial Parameters ────────────────────────────────────────────────────

        [Header("Spatial Parameters")]

        [Tooltip("Base effect radius in metres.")]
        [Min(50f)]
        public float effectRadius = 3000f;

        [Tooltip("How quickly the radius grows in metres per second during active phase.")]
        [Min(0f)]
        public float radiusGrowthRate = 2f;

        [Tooltip("Altitude band affected: x = min, y = max metres.")]
        public Vector2 altitudeRange = new Vector2(0f, 15000f);

        // ── Flight Effects ────────────────────────────────────────────────────────

        [Header("Flight Effects")]

        [Tooltip("Turbulence intensity multiplier while inside the effect zone.")]
        [Range(0f, 5f)]
        public float turbulenceMultiplier = 1f;

        [Tooltip("Visibility reduction factor (0 = none, 1 = completely obscured).")]
        [Range(0f, 1f)]
        public float visibilityReduction = 0f;

        [Tooltip("Additional vertical force in Newtons (positive = upward).")]
        public float thermalStrength = 0f;

        // ── Seasonal & Regional Constraints ───────────────────────────────────────

        [Header("Seasonal & Regional")]

        [Tooltip("Biome names where this event can spawn. Leave empty for any biome.")]
        public string[] validBiomes = new string[0];

        [Tooltip("Polar region required for aurora-type events.")]
        public PolarRegion polarRegion = PolarRegion.Neither;

        [Tooltip("Season names during which this event is most likely. Leave empty for any season.")]
        public string[] activeSeasons = new string[0];

        [Tooltip("Probability multiplier during active seasons vs off-season.")]
        [Range(1f, 20f)]
        public float seasonalProbabilityMultiplier = 3f;

        // ── Visual Effects ────────────────────────────────────────────────────────

        [Header("Visual Effects")]

        [Tooltip("Resource path to the VFX prefab.")]
        public string vfxPrefabPath;

        [Tooltip("Particle effect tags used by TerrainEventVFXController.")]
        public string[] particleEffectTags = new string[0];

        [Tooltip("Whether this event deforms the terrain mesh at runtime.")]
        public bool deformsTerrain = false;

        [Tooltip("Maximum terrain deformation depth/height in metres.")]
        [Min(0f)]
        public float maxDeformationAmount = 0f;

        // ── Audio ──────────────────────────────────────────────────────────────────

        [Header("Audio")]

        [Tooltip("Looping ambient audio clip for the duration of the event.")]
        public AudioClip ambientLoop;

        [Tooltip("One-shot audio clip played when the event reaches its peak.")]
        public AudioClip peakClip;

        // ── Mission & Achievement Integration ─────────────────────────────────────

        [Header("Mission & Achievement Integration")]

        [Tooltip("When true, witnessing this event can trigger a contextual mission.")]
        public bool canTriggerMission = true;

        [Tooltip("Mission types that can be generated from this event.")]
        public TerrainEventMissionType[] availableMissionTypes = new TerrainEventMissionType[0];

        [Tooltip("Achievement keys awarded for witnessing this event.")]
        public string[] witnessAchievementKeys = new string[0];
    }
}
