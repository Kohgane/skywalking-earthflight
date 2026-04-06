// OceanSystemConfig.cs — Phase 117: Advanced Ocean & Maritime System
// ScriptableObject configuration for ocean, wave and maritime simulation.
// Namespace: SWEF.OceanSystem

using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — ScriptableObject that holds all tunable parameters for the
    /// Ocean &amp; Maritime System. Create via
    /// <em>Assets → Create → SWEF → OceanSystem → Config</em>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/OceanSystem/Config", fileName = "OceanSystemConfig")]
    public class OceanSystemConfig : ScriptableObject
    {
        // ── Wave Parameters ───────────────────────────────────────────────────────

        [Header("Wave Parameters")]
        [Tooltip("Base swell wave height multiplier (metres).")]
        [Range(0f, 20f)] public float swellAmplitude = 1.5f;

        [Tooltip("Number of Gerstner wave octaves to simulate.")]
        [Range(1, 8)] public int waveOctaves = 4;

        [Tooltip("Global wave animation time scale.")]
        [Range(0.1f, 5f)] public float waveTimeScale = 1f;

        [Tooltip("Choppiness factor applied to wave crests.")]
        [Range(0f, 2f)] public float choppiness = 0.8f;

        [Tooltip("Dominant swell direction in degrees (0 = North).")]
        [Range(0f, 360f)] public float swellDirection = 270f;

        // ── Water Quality ─────────────────────────────────────────────────────────

        [Header("Water Quality")]
        [Tooltip("Reflection render quality (0 = low, 3 = ultra).")]
        [Range(0, 3)] public int reflectionQuality = 2;

        [Tooltip("Foam density on wave crests.")]
        [Range(0f, 1f)] public float foamDensity = 0.5f;

        [Tooltip("Subsurface scattering colour for shallow water.")]
        public Color subsurfaceColour = new Color(0.04f, 0.6f, 0.5f, 1f);

        [Tooltip("Deep water colour.")]
        public Color deepWaterColour = new Color(0.01f, 0.1f, 0.25f, 1f);

        [Tooltip("Water transparency depth in metres.")]
        [Range(0.5f, 50f)] public float transparencyDepth = 8f;

        // ── Tide Cycle ────────────────────────────────────────────────────────────

        [Header("Tide Cycle")]
        [Tooltip("Enable tidal water level simulation.")]
        public bool enableTides = true;

        [Tooltip("Duration of a complete tidal cycle in real seconds.")]
        [Range(60f, 86400f)] public float tidalCycleDuration = 900f;

        [Tooltip("Tidal range (max rise above mean sea level) in metres.")]
        [Range(0f, 15f)] public float tidalRange = 3f;

        [Tooltip("Spring tide multiplier (full/new moon).")]
        [Range(1f, 2f)] public float springTideMultiplier = 1.4f;

        // ── Buoyancy ──────────────────────────────────────────────────────────────

        [Header("Buoyancy")]
        [Tooltip("Seawater density used in buoyancy calculations (kg/m³).")]
        [Range(900f, 1100f)] public float waterDensity = 1025f;

        [Tooltip("Linear drag coefficient applied to objects in water.")]
        [Range(0f, 5f)] public float waterLinearDrag = 1.5f;

        [Tooltip("Angular drag coefficient applied to objects in water.")]
        [Range(0f, 5f)] public float waterAngularDrag = 2f;

        // ── Ocean Currents ────────────────────────────────────────────────────────

        [Header("Ocean Currents")]
        [Tooltip("Enable surface current simulation.")]
        public bool enableCurrents = true;

        [Tooltip("Maximum surface current speed in m/s.")]
        [Range(0f, 5f)] public float maxCurrentSpeed = 1.5f;

        // ── Aircraft Carrier ──────────────────────────────────────────────────────

        [Header("Aircraft Carrier")]
        [Tooltip("Default catapult launch acceleration in m/s².")]
        [Range(50f, 500f)] public float catapultAcceleration = 120f;

        [Tooltip("Number of arrestor wires on carrier deck.")]
        [Range(2, 6)] public int arrestorWireCount = 4;

        [Tooltip("Arrestor wire spacing in metres.")]
        [Range(5f, 20f)] public float arrestorWireSpacing = 12f;

        [Tooltip("Maximum deceleration force from arrestor wire (G).")]
        [Range(2f, 6f)] public float arrestorMaxDecelG = 4f;

        // ── Maritime Traffic ──────────────────────────────────────────────────────

        [Header("Maritime Traffic")]
        [Tooltip("Maximum number of AI vessels active simultaneously.")]
        [Range(0, 64)] public int maxActiveVessels = 20;

        [Tooltip("Vessel spawn radius around origin in metres.")]
        [Range(1000f, 100000f)] public float vesselSpawnRadius = 30000f;

        [Tooltip("Minimum vessel speed in knots.")]
        [Range(1f, 10f)] public float minVesselSpeedKnots = 4f;

        [Tooltip("Maximum cargo vessel speed in knots.")]
        [Range(5f, 30f)] public float maxCargoVesselSpeedKnots = 18f;

        // ── Water Landing Assist ──────────────────────────────────────────────────

        [Header("Water Landing Assist")]
        [Tooltip("Enable automatic water landing assistance.")]
        public bool waterLandingAssist = true;

        [Tooltip("Maximum safe touchdown vertical speed (m/s) before structural damage.")]
        [Range(1f, 10f)] public float maxSafeTouchdownSpeed = 3f;

        // ── Visual Effects ────────────────────────────────────────────────────────

        [Header("Visual Effects")]
        [Tooltip("Enable caustic light projection under water.")]
        public bool enableCaustics = true;

        [Tooltip("Wake foam lifetime in seconds.")]
        [Range(5f, 120f)] public float wakeFoamLifetime = 30f;

        [Tooltip("Splash particle emission intensity.")]
        [Range(0f, 2f)] public float splashIntensity = 1f;
    }
}
