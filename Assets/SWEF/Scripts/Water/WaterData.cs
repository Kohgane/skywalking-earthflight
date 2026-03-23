// WaterData.cs — SWEF Ocean & Water Interaction System
using System;
using UnityEngine;

namespace SWEF.Water
{
    #region Enumerations

    /// <summary>Describes the current surface agitation level of a water body.</summary>
    public enum WaterState
    {
        /// <summary>Flat, near-glassy surface; minimal waves.</summary>
        Calm,
        /// <summary>Moderate wave amplitude with visible foam patches.</summary>
        Choppy,
        /// <summary>High-amplitude storm waves with heavy foam and spray.</summary>
        Stormy
    }

    #endregion

    #region Surface Configuration

    /// <summary>
    /// Per-wave-layer parameters for Gerstner wave simulation.
    /// One instance per octave (up to 4 octaves are evaluated by
    /// <see cref="WaterSurfaceManager"/>).
    /// </summary>
    [Serializable]
    public class WaveLayer
    {
        /// <summary>Peak-to-trough height of this wave layer in world units.</summary>
        [Tooltip("Peak-to-trough wave height in world units.")]
        public float amplitude = 0.5f;

        /// <summary>Number of wave crests per world unit.</summary>
        [Tooltip("Spatial frequency: wave crests per world unit.")]
        public float frequency = 0.2f;

        /// <summary>Wave propagation speed in world units per second.</summary>
        [Tooltip("Wave propagation speed in world units per second.")]
        public float speed = 1.5f;

        /// <summary>
        /// Direction the wave travels (XZ plane).
        /// The vector is normalised at runtime; zero vectors fall back to <c>Vector2.right</c>.
        /// </summary>
        [Tooltip("XZ direction vector the wave travels toward.")]
        public Vector2 direction = Vector2.right;

        /// <summary>Gerstner sharpness factor [0–1]. Higher values produce sharper crests.</summary>
        [Range(0f, 1f)]
        [Tooltip("Gerstner crest sharpness factor (0 = sine, 1 = sharp).")]
        public float steepness = 0.3f;
    }

    /// <summary>
    /// High-level water surface configuration: wave layers and foam threshold.
    /// Assign directly to <see cref="WaterInteractionProfile"/>.
    /// </summary>
    [Serializable]
    public class WaterSurfaceConfig
    {
        /// <summary>Wave octaves. Up to four layers are evaluated by <see cref="WaterSurfaceManager"/>.</summary>
        [Tooltip("Wave octaves (max 4 evaluated).")]
        public WaveLayer[] waveLayers = new WaveLayer[]
        {
            new WaveLayer { amplitude = 0.4f,  frequency = 0.15f, speed = 1.2f, direction = new Vector2(1f,  0.3f), steepness = 0.3f },
            new WaveLayer { amplitude = 0.2f,  frequency = 0.30f, speed = 2.0f, direction = new Vector2(0.6f, 1f), steepness = 0.25f },
            new WaveLayer { amplitude = 0.12f, frequency = 0.55f, speed = 1.7f, direction = new Vector2(-0.4f, 1f), steepness = 0.2f },
            new WaveLayer { amplitude = 0.06f, frequency = 1.00f, speed = 2.5f, direction = new Vector2(1f, -0.5f), steepness = 0.15f },
        };

        /// <summary>
        /// Fractional wave height at which foam begins to appear [0–1].
        /// A value of 0.8 means foam appears when the wave crest reaches 80 % of the max amplitude.
        /// </summary>
        [Range(0f, 1f)]
        [Tooltip("Wave height fraction [0–1] above which foam is rendered.")]
        public float foamThreshold = 0.75f;
    }

    #endregion

    #region Buoyancy Settings

    /// <summary>
    /// Physics parameters that drive buoyancy calculations in <see cref="BuoyancyController"/>.
    /// Tune these per object type (aircraft, boat, debris, etc.).
    /// </summary>
    [Serializable]
    public class BuoyancySettings
    {
        /// <summary>Upward force applied per submerged sample point per unit of depth.</summary>
        [Tooltip("Upward force coefficient per sample point per metre of submersion.")]
        public float buoyancyForce = 20f;

        /// <summary>Linear drag multiplier applied to the Rigidbody when fully submerged.</summary>
        [Tooltip("Linear drag added while any sample point is submerged.")]
        public float waterDrag = 3f;

        /// <summary>Angular drag multiplier applied to the Rigidbody when submerged.</summary>
        [Tooltip("Angular drag added while any sample point is submerged.")]
        public float waterAngularDrag = 1.5f;

        /// <summary>
        /// Depth threshold in world units below the water surface at which a sample point
        /// is considered fully submerged.  Points shallower than this receive a scaled force.
        /// </summary>
        [Tooltip("Depth (m) at which a sample point is treated as fully submerged.")]
        public float submergeDepthThreshold = 0.5f;
    }

    #endregion

    #region Splash Effect Configuration

    /// <summary>Immutable event payload emitted by <see cref="SplashEffectController"/> on each splash.</summary>
    [Serializable]
    public class SplashEventData
    {
        /// <summary>World-space position of the splash origin.</summary>
        public Vector3 position;

        /// <summary>Entry/exit velocity at impact (m/s).</summary>
        public float velocityMagnitude;

        /// <summary>World time at which the splash occurred.</summary>
        public float timestamp;

        /// <summary>Whether the splash was caused by water entry (<c>true</c>) or exit (<c>false</c>).</summary>
        public bool isEntry;
    }

    /// <summary>
    /// Tuning parameters for the splash VFX and audio system in <see cref="SplashEffectController"/>.
    /// </summary>
    [Serializable]
    public class SplashEffectConfig
    {
        /// <summary>
        /// Resource path (relative to a <c>Resources/</c> folder) of the splash particle prefab.
        /// E.g. "Water/SplashEffect".
        /// </summary>
        [Tooltip("Resources path of the splash particle system prefab.")]
        public string splashParticlePrefabPath = "Water/SplashEffect";

        /// <summary>Minimum and maximum force applied to splash particles on impact.</summary>
        [Tooltip("Minimum splash particle force at the lowest valid entry speed.")]
        public float splashForceMin = 5f;

        /// <summary>Maximum splash force applied to particles at peak entry speed.</summary>
        [Tooltip("Maximum splash particle force at the highest expected entry speed.")]
        public float splashForceMax = 50f;

        /// <summary>
        /// Entry speed (m/s) that maps to <see cref="splashForceMax"/>.
        /// Speeds above this value are clamped.
        /// </summary>
        [Tooltip("Entry speed (m/s) that produces the maximum splash force.")]
        public float maxEntrySpeed = 40f;

        /// <summary>Minimum time (s) between consecutive splash triggers to prevent spam.</summary>
        [Tooltip("Minimum seconds between successive splash events on the same object.")]
        public float cooldown = 0.3f;

        /// <summary>Pool size for pre-allocated splash particle instances.</summary>
        [Tooltip("Number of splash particle instances to pre-allocate in the pool.")]
        public int poolSize = 8;
    }

    #endregion

    #region Underwater Settings

    /// <summary>
    /// Visual and post-processing settings applied by <see cref="UnderwaterCameraTransition"/>
    /// when the camera is below the water surface.
    /// </summary>
    [Serializable]
    public class UnderwaterSettings
    {
        /// <summary>Fog colour tint applied underwater (typically a deep blue-green).</summary>
        [Tooltip("Fog colour when the camera is below the water surface.")]
        public Color fogColor = new Color(0.04f, 0.18f, 0.35f, 1f);

        /// <summary>Exponential fog density when fully submerged.</summary>
        [Tooltip("Exponential fog density applied underwater.")]
        public float fogDensity = 0.06f;

        /// <summary>Caustic pattern intensity multiplier [0–2].</summary>
        [Range(0f, 2f)]
        [Tooltip("Caustic light pattern intensity multiplier.")]
        public float causticsIntensity = 0.8f;

        /// <summary>
        /// Shift applied to the scene ambient light colour while underwater.
        /// Added to the existing ambient colour as an HDR tint.
        /// </summary>
        [Tooltip("Additive HDR tint applied to ambient lighting underwater.")]
        public Color ambientLightShift = new Color(-0.3f, -0.2f, 0.1f, 0f);

        /// <summary>Target post-processing volume weight when fully submerged [0–1].</summary>
        [Range(0f, 1f)]
        [Tooltip("Post-processing volume weight reached at full submersion depth.")]
        public float postProcessingVolumeWeight = 1f;

        /// <summary>Amplitude of the sinusoidal UV distortion effect (normalised screen UV).</summary>
        [Tooltip("Amplitude of the sinusoidal screen-space distortion applied underwater.")]
        public float distortionAmplitude = 0.008f;

        /// <summary>Speed of the UV distortion oscillation (Hz).</summary>
        [Tooltip("Frequency (Hz) of the underwater distortion animation.")]
        public float distortionSpeed = 1.2f;

        /// <summary>Depth (m) below the surface at which full attenuation is reached.</summary>
        [Tooltip("Depth (m) at which maximum light attenuation is applied.")]
        public float maxAttenuationDepth = 20f;

        /// <summary>Transition duration (s) between air and underwater visual states.</summary>
        [Tooltip("Seconds for the fog/lighting transition when crossing the water surface.")]
        public float transitionDuration = 0.4f;
    }

    #endregion

    #region Ripple System Settings

    /// <summary>
    /// Configuration for the dynamic ripple ring system in <see cref="WaterRippleSystem"/>.
    /// </summary>
    [Serializable]
    public class RippleSettings
    {
        /// <summary>Number of concentric ripple rings emitted per impact.</summary>
        [Tooltip("Number of concentric rings per ripple event.")]
        public int ringCount = 3;

        /// <summary>Outward propagation speed of ripple rings (world units per second).</summary>
        [Tooltip("Outward propagation speed of ripple rings (m/s).")]
        public float propagationSpeed = 4f;

        /// <summary>Maximum radius a ripple ring can reach before it is recycled.</summary>
        [Tooltip("Maximum ripple ring radius before the ring fades out.")]
        public float maxRadius = 8f;

        /// <summary>Total lifetime of a single ripple event (s), after which all rings have faded.</summary>
        [Tooltip("Lifetime (s) of a full ripple event.")]
        public float lifetime = 2.5f;

        /// <summary>Width of each ripple ring line in world units.</summary>
        [Tooltip("Width of each rendered ripple ring.")]
        public float ringWidth = 0.15f;

        /// <summary>Maximum simultaneous active ripple events in the pool.</summary>
        [Tooltip("Maximum simultaneous ripple events; older ones are recycled first.")]
        public int maxActiveRipples = 16;
    }

    #endregion

    #region Profile ScriptableObject

    /// <summary>
    /// Unified ScriptableObject profile that bundles all water interaction settings.
    /// Create via <em>Assets → Create → SWEF → WaterInteractionProfile</em>.
    /// Assign to <see cref="WaterSurfaceManager"/> for runtime use.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/WaterInteractionProfile", fileName = "WaterInteractionProfile")]
    public class WaterInteractionProfile : ScriptableObject
    {
        #region Fields

        [Header("Surface & Waves")]
        [Tooltip("Wave layer configuration for the Gerstner simulation.")]
        public WaterSurfaceConfig surface = new WaterSurfaceConfig();

        [Header("Buoyancy Physics")]
        [Tooltip("Physics parameters for all BuoyancyController instances that reference this profile.")]
        public BuoyancySettings buoyancy = new BuoyancySettings();

        [Header("Splash Effects")]
        [Tooltip("VFX and audio parameters for splash triggers.")]
        public SplashEffectConfig splash = new SplashEffectConfig();

        [Header("Underwater Camera")]
        [Tooltip("Visual and post-processing parameters for the underwater camera state.")]
        public UnderwaterSettings underwater = new UnderwaterSettings();

        [Header("Ripple System")]
        [Tooltip("Dynamic ripple ring generation parameters.")]
        public RippleSettings ripple = new RippleSettings();

        #endregion
    }

    #endregion
}
