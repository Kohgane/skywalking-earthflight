// WaterData.cs — SWEF Phase 74: Water Interaction & Buoyancy System
using System;
using UnityEngine;

namespace SWEF.Water
{
    #region Enumerations

    /// <summary>Type of water body detected beneath or around the aircraft.</summary>
    public enum WaterBodyType
    {
        Ocean,
        Sea,
        Lake,
        River,
        Pond,
        Reservoir,
        Unknown
    }

    /// <summary>State of the aircraft relative to the water surface.</summary>
    public enum WaterContactState
    {
        /// <summary>Above water, no contact.</summary>
        Airborne,
        /// <summary>Very low altitude, near water surface (&lt;5 m).</summary>
        Skimming,
        /// <summary>Initial contact / splashdown.</summary>
        Touching,
        /// <summary>On water surface, buoyant.</summary>
        Floating,
        /// <summary>Below surface, descending.</summary>
        Sinking,
        /// <summary>Fully underwater.</summary>
        Submerged,
        /// <summary>Controlled water landing in progress.</summary>
        Ditching
    }

    /// <summary>Type of splash effect to play at water contact.</summary>
    public enum SplashType
    {
        /// <summary>Low speed skim.</summary>
        LightSpray,
        /// <summary>Normal contact.</summary>
        MediumSplash,
        /// <summary>High speed impact.</summary>
        HeavySplash,
        /// <summary>Controlled water landing.</summary>
        Touchdown,
        /// <summary>Bouncing off surface.</summary>
        Skip,
        /// <summary>Nose-first entry.</summary>
        DiveEntry,
        /// <summary>Flat impact.</summary>
        BellyFlop,
        /// <summary>Continuous wake from floating / taxiing.</summary>
        WakeTrail
    }

    /// <summary>Underwater visual zone based on depth below the surface.</summary>
    public enum UnderwaterZone
    {
        /// <summary>At water line, partial submersion.</summary>
        Surface,
        /// <summary>0–10 m depth.</summary>
        Shallow,
        /// <summary>10–50 m depth.</summary>
        Mid,
        /// <summary>50 m+ depth.</summary>
        Deep,
        /// <summary>200 m+ depth (very dark).</summary>
        Abyss
    }

    #endregion

    #region Configuration

    /// <summary>
    /// Serializable runtime configuration for the water interaction system.
    /// Assign on <see cref="WaterSurfaceManager"/> and read by all other Water scripts.
    /// </summary>
    [Serializable]
    public class WaterConfig
    {
        [Tooltip("World-space Y coordinate of the sea-level water plane.")]
        public float waterLevel = 0f;

        [Tooltip("Wave peak-to-trough height in metres.")]
        public float waveAmplitude = 0.5f;

        [Tooltip("Wave oscillation frequency in Hz.")]
        public float waveFrequency = 0.3f;

        [Tooltip("Wave propagation speed in m/s.")]
        public float waveSpeed = 2f;

        [Tooltip("Water density in kg/m³ (saltwater default: 1025).")]
        public float waterDensity = 1025f;

        [Tooltip("Water drag multiplier opposing submerged velocity.")]
        public float dragCoefficient = 0.8f;

        [Tooltip("Base upward buoyancy acceleration (m/s²).")]
        public float buoyancyForce = 9.81f;

        [Tooltip("Passive sink speed in m/s when sinking is triggered.")]
        public float sinkRate = 0.5f;

        [Tooltip("Altitude (m) above water surface that triggers Skimming state.")]
        public float skimAltitudeThreshold = 5f;

        [Tooltip("Minimum seconds between consecutive splash effect triggers.")]
        public float splashCooldown = 0.3f;

        [Tooltip("Seconds before a ripple instance fades out completely.")]
        public float rippleLifetime = 3f;

        [Tooltip("Maximum radius (m) a ripple ring expands to before fading.")]
        public float rippleMaxRadius = 50f;

        [Tooltip("Exponential fog density applied when the camera is underwater.")]
        public float underwaterFogDensity = 0.15f;

        [Tooltip("Light attenuation coefficient per metre of depth.")]
        public float underwaterLightFalloff = 0.02f;

        [Tooltip("Maximum simulated depth in metres.")]
        public float maxUnderwaterDepth = 500f;

        [Tooltip("Water colour tint in shallow areas.")]
        public Color shallowWaterColor = new Color(0.2f, 0.6f, 0.8f, 0.85f);

        [Tooltip("Water colour tint in deep areas.")]
        public Color deepWaterColor = new Color(0.02f, 0.05f, 0.15f, 0.95f);

        [Tooltip("Fog colour when the camera is fully submerged.")]
        public Color underwaterFogColor = new Color(0.1f, 0.3f, 0.5f, 1f);
    }

    #endregion

    #region Runtime State

    /// <summary>
    /// Snapshot of the water surface at a sampled world position.
    /// Produced each frame by <see cref="WaterSurfaceManager"/> and consumed by physics / camera systems.
    /// </summary>
    [Serializable]
    public class WaterSurfaceState
    {
        /// <summary>World-space Y of the wave-displaced surface at the sample point.</summary>
        public float heightAtPosition;

        /// <summary>Surface normal vector at the sample point.</summary>
        public Vector3 surfaceNormal;

        /// <summary>Current wave animation phase (radians) for procedural effects.</summary>
        public float wavePhase;

        /// <summary>Heuristic classification of the water body underneath the aircraft.</summary>
        public WaterBodyType bodyType;

        /// <summary>Estimated water temperature in degrees Celsius (aesthetic only).</summary>
        public float temperature;

        /// <summary>Water clarity [0–1]; lower values reduce underwater visibility distance.</summary>
        public float clarity;
    }

    /// <summary>
    /// Live buoyancy and contact state for a <see cref="BuoyancyController"/> instance.
    /// </summary>
    [Serializable]
    public class BuoyancyState
    {
        /// <summary>Current contact state of the aircraft relative to the water surface.</summary>
        public WaterContactState contactState;

        /// <summary>Metres below the water surface (0 = at surface).</summary>
        public float submersionDepth;

        /// <summary>Magnitude of the current upward buoyancy force (N or m/s² depending on mode).</summary>
        public float buoyancyForceMagnitude;

        /// <summary>Magnitude of the current water-drag force opposing velocity.</summary>
        public float dragForceMagnitude;

        /// <summary>Elapsed seconds since first water contact this session.</summary>
        public float timeInWater;

        /// <summary>Elapsed seconds since the aircraft became fully submerged.</summary>
        public float timeSubmerged;

        /// <summary>True when the aircraft is floating steadily without significant oscillation.</summary>
        public bool isStable;

        /// <summary>Current lateral drift velocity imparted by water currents.</summary>
        public Vector3 waterVelocity;
    }

    /// <summary>
    /// Immutable event payload emitted by <see cref="BuoyancyController"/> on each water contact.
    /// </summary>
    [Serializable]
    public class SplashEvent
    {
        /// <summary>Classification of the splash interaction.</summary>
        public SplashType type;

        /// <summary>World-space position of the splash origin.</summary>
        public Vector3 position;

        /// <summary>Aircraft velocity vector at the moment of impact (m/s).</summary>
        public Vector3 velocity;

        /// <summary>Scalar magnitude of the impact force.</summary>
        public float impactForce;

        /// <summary><c>Time.time</c> value at the moment the event was fired.</summary>
        public float timestamp;
    }

    #endregion
}
