// VFXData.cs — SWEF Particle Effects & VFX System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.VFX
{
    #region Enumerations

    /// <summary>All supported visual effect types available in the VFX system.</summary>
    public enum VFXType
    {
        /// <summary>Jet or propeller engine exhaust flame and smoke trail.</summary>
        EngineExhaust,
        /// <summary>Condensation trail (contrail) produced at high altitude.</summary>
        Contrail,
        /// <summary>Shockwave ring emitted at Mach 1 transition.</summary>
        SonicBoom,
        /// <summary>Atmospheric plasma and fire during high-speed reentry.</summary>
        ReentryFlame,
        /// <summary>Cloud burst and dissipation particle effect.</summary>
        CloudBurst,
        /// <summary>Water droplet splash on surface impact.</summary>
        RainSplash,
        /// <summary>Swirling snow flurry particles in cold biomes.</summary>
        SnowFlurry,
        /// <summary>Wind-driven sand and dust storm particles.</summary>
        SandStorm,
        /// <summary>Electrical lightning arc discharge effect.</summary>
        LightningStrike,
        /// <summary>Shimmering auroral light ribbons at high latitudes.</summary>
        AuroraBorealis,
        /// <summary>Fast-moving point of light streaking across the sky.</summary>
        ShootingStar,
        /// <summary>Celebratory firework burst with coloured sparks.</summary>
        Firework,
        /// <summary>Radial lines that convey high-speed motion blur.</summary>
        SpeedLines,
        /// <summary>Altitude-based atmospheric glow ring effect.</summary>
        AltitudeGlow,
        /// <summary>Vortex swirl emitted from wing tips in humid air.</summary>
        WingTipVortex,
        /// <summary>Dust cloud raised on contact with dry ground surfaces.</summary>
        LandingDust,
        /// <summary>Water spray column on contact with water surfaces.</summary>
        WaterSplash,
        /// <summary>Falling and swirling autumn leaves in forest biomes.</summary>
        LeafScatter,
        /// <summary>Floating pollen particles in spring/temperate biomes.</summary>
        PollenDrift,
        /// <summary>Fine grey ash particles near volcanic terrain.</summary>
        VolcanicAsh
    }

    /// <summary>LOD quality levels for VFX instances.</summary>
    public enum VFXLODLevel
    {
        /// <summary>Full particle count and all sub-emitters active.</summary>
        Full,
        /// <summary>50 % particle count; secondary emitters disabled.</summary>
        Reduced,
        /// <summary>25 % particle count; only primary emitter active.</summary>
        Minimal,
        /// <summary>Single sprite billboard; no particle simulation.</summary>
        Billboard
    }

    /// <summary>Policy applied when a pool is exhausted and no free instances remain.</summary>
    public enum PoolExhaustPolicy
    {
        /// <summary>Silently drop the spawn request; raise <c>OnPoolExhausted</c>.</summary>
        DropRequest,
        /// <summary>Forcibly reclaim the oldest active instance and reuse it.</summary>
        RecycleOldest,
        /// <summary>Grow the pool by one and allocate a new instance.</summary>
        Grow
    }

    #endregion

    #region Data Structs

    /// <summary>
    /// Fully describes a single VFX spawn request including position, orientation,
    /// scale, optional parent transform, and an optional duration override.
    /// </summary>
    [Serializable]
    public struct VFXSpawnRequest
    {
        /// <summary>Type of effect to spawn.</summary>
        [Tooltip("Type of effect to spawn.")]
        public VFXType type;

        /// <summary>World-space position at which to spawn the effect.</summary>
        [Tooltip("World-space spawn position.")]
        public Vector3 position;

        /// <summary>World-space rotation applied to the spawned effect.</summary>
        [Tooltip("World-space spawn rotation.")]
        public Quaternion rotation;

        /// <summary>Uniform scale multiplier applied to the spawned effect.</summary>
        [Tooltip("Uniform scale multiplier (1 = default).")]
        public float scale;

        /// <summary>Optional transform to parent the spawned effect to. Null = world space.</summary>
        [Tooltip("Optional parent transform; null for world space.")]
        public Transform parent;

        /// <summary>
        /// Duration override in seconds. Values ≤ 0 use the profile's default lifetime.
        /// </summary>
        [Tooltip("Duration override in seconds; ≤ 0 uses the profile default.")]
        public float durationOverride;

        /// <summary>Creates a minimal spawn request at a position with default orientation.</summary>
        /// <param name="type">VFX type to spawn.</param>
        /// <param name="position">World-space spawn position.</param>
        public static VFXSpawnRequest At(VFXType type, Vector3 position)
        {
            return new VFXSpawnRequest
            {
                type = type,
                position = position,
                rotation = Quaternion.identity,
                scale = 1f,
                parent = null,
                durationOverride = 0f
            };
        }
    }

    /// <summary>
    /// Opaque handle returned by <see cref="VFXPoolManager.Spawn"/> that uniquely
    /// identifies an active VFX instance and can be passed to
    /// <see cref="VFXPoolManager.Despawn"/> to manually reclaim it.
    /// </summary>
    public struct VFXInstanceHandle
    {
        /// <summary>Unique sequential identifier for this handle. 0 = invalid.</summary>
        public readonly int Id;

        /// <summary>The VFX type this handle refers to.</summary>
        public readonly VFXType Type;

        /// <summary>World-space position at which the effect was spawned.</summary>
        public readonly Vector3 SpawnPosition;

        /// <summary>Game time (<c>Time.time</c>) at which the instance was spawned.</summary>
        public readonly float SpawnTime;

        /// <summary>Returns <c>true</c> if the handle refers to a valid active instance.</summary>
        public bool IsValid => Id != 0;

        /// <summary>Represents an invalid / empty handle.</summary>
        public static readonly VFXInstanceHandle Invalid = new VFXInstanceHandle(0, VFXType.EngineExhaust, Vector3.zero, 0f);

        /// <summary>Initialises a new handle with the given parameters.</summary>
        internal VFXInstanceHandle(int id, VFXType type, Vector3 spawnPosition, float spawnTime)
        {
            Id = id;
            Type = type;
            SpawnPosition = spawnPosition;
            SpawnTime = spawnTime;
        }
    }

    #endregion

    #region ScriptableObject

    /// <summary>
    /// Authored asset that configures one VFX type: the prefab reference, lifetime,
    /// looping behaviour, render layer, pool parameters, and LOD distances.
    /// Create via <c>Assets → Create → SWEF → VFX → VFX Profile</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "VFXProfile", menuName = "SWEF/VFX/VFX Profile")]
    public class VFXProfile : ScriptableObject
    {
        [Header("Identity")]
        /// <summary>VFX type this profile configures.</summary>
        [Tooltip("VFX type this profile configures.")]
        public VFXType vfxType;

        [Header("Prefab")]
        /// <summary>Particle system prefab instantiated by the pool.</summary>
        [Tooltip("Particle system prefab to instantiate.")]
        public GameObject prefab;

        [Header("Lifetime")]
        /// <summary>Default effect lifetime in seconds. Looping effects ignore this.</summary>
        [Tooltip("Default effect lifetime in seconds. Looping effects ignore this.")]
        [SerializeField] private float lifetime = 3f;

        /// <summary>Default effect lifetime in seconds.</summary>
        public float Lifetime => lifetime;

        /// <summary>Whether this effect loops indefinitely until manually despawned.</summary>
        [Tooltip("Loop indefinitely until explicitly despawned.")]
        public bool loop;

        [Header("Rendering")]
        /// <summary>Unity layer index on which to render the particle system.</summary>
        [Tooltip("Unity layer index used to render the particle system.")]
        [SerializeField, Range(0, 31)] private int renderLayer;

        /// <summary>Unity layer index used by this effect.</summary>
        public int RenderLayer => renderLayer;

        [Header("Pool Settings")]
        /// <summary>Number of instances to pre-warm in the pool at startup.</summary>
        [Tooltip("Instances pre-created during pool warm-up.")]
        [SerializeField, Min(0)] private int warmCount = 4;

        /// <summary>Instances pre-created during pool initialisation.</summary>
        public int WarmCount => warmCount;

        /// <summary>Maximum concurrent active instances for this effect type.</summary>
        [Tooltip("Maximum simultaneous active instances (0 = unlimited).")]
        [SerializeField, Min(0)] private int maxInstances = 16;

        /// <summary>Maximum simultaneous active instances. 0 = unlimited.</summary>
        public int MaxInstances => maxInstances;

        /// <summary>Policy applied when the pool is exhausted.</summary>
        [Tooltip("Behaviour when no free instances remain.")]
        public PoolExhaustPolicy exhaustPolicy = PoolExhaustPolicy.DropRequest;

        [Header("LOD Distances")]
        /// <summary>Camera distance thresholds for LOD transitions (x=Full→Reduced, y=Reduced→Minimal, z=Minimal→Billboard).</summary>
        [Tooltip("Distance thresholds: x=Full→Reduced, y=Reduced→Minimal, z=Minimal→Billboard (metres).")]
        public Vector3 lodDistances = new Vector3(200f, 500f, 1000f);

        /// <summary>Particle count multipliers for each LOD level (x=Full, y=Reduced, z=Minimal; Billboard uses no particles).</summary>
        [Tooltip("Particle count scale: x=Full(1.0), y=Reduced(0.5), z=Minimal(0.25).")]
        public Vector3 lodParticleScale = new Vector3(1f, 0.5f, 0.25f);

        /// <summary>Returns the particle count multiplier for the given LOD level.</summary>
        /// <param name="level">LOD level to query.</param>
        public float GetParticleScale(VFXLODLevel level)
        {
            return level switch
            {
                VFXLODLevel.Full     => lodParticleScale.x,
                VFXLODLevel.Reduced  => lodParticleScale.y,
                VFXLODLevel.Minimal  => lodParticleScale.z,
                _                    => 0f
            };
        }
    }

    #endregion
}
