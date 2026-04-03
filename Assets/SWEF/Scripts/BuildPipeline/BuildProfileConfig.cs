// BuildProfileConfig.cs — SWEF Phase 95: Platform Target Matrix & Build Pipeline
// ScriptableObject that stores per-platform build settings.
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
#endif

namespace SWEF.BuildPipeline
{
    // ── Quality tier (re-exported for build-pipeline consumers) ─────────────────

    /// <summary>
    /// Quality preset tiers — matches <c>SWEF.Accessibility.QualityTier</c>
    /// so both systems share the same vocabulary without a hard dependency.
    /// </summary>
    public enum QualityTier
    {
        Ultra,
        High,
        Medium,
        Low,
        /// <summary>Minimum-spec "potato" mode.</summary>
        Potato
    }

    // ── ScriptableObject ─────────────────────────────────────────────────────────

    /// <summary>
    /// Per-platform build configuration stored as a Unity <see cref="ScriptableObject"/>
    /// asset in <c>Resources/BuildPipeline/</c>.
    ///
    /// <para>Consumed at editor-time by <see cref="CIBuildRunner"/> and at
    /// runtime by <see cref="PlatformBootstrapper"/>.</para>
    /// </summary>
    [CreateAssetMenu(
        fileName = "BuildProfileConfig",
        menuName = "SWEF/Build Pipeline/Build Profile Config",
        order = 200)]
    public class BuildProfileConfig : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────────

        [Header("Platform Identity")]
        [Tooltip("Target platform this profile applies to.")]
        public PlatformTarget target;

        [Tooltip("Application/bundle identifier (e.g. com.kohgane.swef).")]
        public string bundleIdentifier = "com.kohgane.swef";

        [Tooltip("Human-readable product name shown in stores and OS menus.")]
        public string productName = "Skywalking: Earth Flight";

        // ── Build options ─────────────────────────────────────────────────────────

        [Header("Build Options")]
#if UNITY_EDITOR
        [Tooltip("Unity BuildOptions flags (bitfield). Development, AutoRunPlayer, etc.")]
        public BuildOptions buildOptions = BuildOptions.None;
#else
        // BuildOptions is an Editor-only type; store as int at runtime.
        [HideInInspector]
        public int buildOptionsRaw = 0;
#endif

        // ── Quality ───────────────────────────────────────────────────────────────

        [Header("Quality")]
        [Tooltip("Default quality tier applied on first launch for this platform.")]
        public QualityTier defaultQuality = QualityTier.High;

        [Tooltip("Target frame rate (0 = platform default / vsync).")]
        [Range(0, 120)]
        public int targetFrameRate = 60;

        [Tooltip("Maximum texture resolution in pixels on the longest edge (0 = unlimited).")]
        [Range(0, 8192)]
        public int maxTextureResolution = 2048;

        // ── XR ───────────────────────────────────────────────────────────────────

        [Header("XR")]
        [Tooltip("Enable Unity XR plug-in management for this build.")]
        public bool enableXR = false;

        // ── Input subsystems ──────────────────────────────────────────────────────

        [Header("Input Subsystems")]
        [Tooltip("Enable device gyroscope / attitude sensor.")]
        public bool enableGyroscope = false;

        [Tooltip("Enable touch-screen input module.")]
        public bool enableTouchInput = false;

        [Tooltip("Enable keyboard + mouse input.")]
        public bool enableKeyboardMouse = true;

        [Tooltip("Enable gamepad / controller input.")]
        public bool enableGamepad = true;

        // ── Factory helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Creates and returns a sensible default <see cref="BuildProfileConfig"/>
        /// for the given <paramref name="platform"/> without requiring an asset file.
        /// Useful for CI/CD where assets may not be present.
        /// </summary>
        public static BuildProfileConfig CreateDefault(PlatformTarget platform)
        {
            var cfg = CreateInstance<BuildProfileConfig>();
            cfg.target        = platform;
            cfg.productName   = "Skywalking: Earth Flight";
            cfg.bundleIdentifier = "com.kohgane.swef";

            switch (platform)
            {
                case PlatformTarget.WindowsPC:
                    cfg.defaultQuality     = QualityTier.Ultra;
                    cfg.targetFrameRate    = 0; // vsync
                    cfg.maxTextureResolution = 0;
                    cfg.enableKeyboardMouse  = true;
                    cfg.enableGamepad        = true;
                    cfg.enableTouchInput     = false;
                    cfg.enableGyroscope      = false;
                    break;

                case PlatformTarget.macOS:
                    cfg.defaultQuality     = QualityTier.High;
                    cfg.targetFrameRate    = 0;
                    cfg.maxTextureResolution = 0;
                    cfg.enableKeyboardMouse  = true;
                    cfg.enableGamepad        = true;
                    cfg.enableTouchInput     = false;
                    cfg.enableGyroscope      = false;
                    break;

                case PlatformTarget.iOS:
                    cfg.defaultQuality     = QualityTier.High;
                    cfg.targetFrameRate    = 60;
                    cfg.maxTextureResolution = 2048;
                    cfg.enableKeyboardMouse  = false;
                    cfg.enableGamepad        = true;
                    cfg.enableTouchInput     = true;
                    cfg.enableGyroscope      = true;
                    break;

                case PlatformTarget.Android:
                    cfg.defaultQuality     = QualityTier.Medium;
                    cfg.targetFrameRate    = 60;
                    cfg.maxTextureResolution = 2048;
                    cfg.enableKeyboardMouse  = false;
                    cfg.enableGamepad        = true;
                    cfg.enableTouchInput     = true;
                    cfg.enableGyroscope      = true;
                    break;

                case PlatformTarget.iPadOS:
                    cfg.defaultQuality     = QualityTier.High;
                    cfg.targetFrameRate    = 60;
                    cfg.maxTextureResolution = 4096;
                    cfg.enableKeyboardMouse  = false;
                    cfg.enableGamepad        = true;
                    cfg.enableTouchInput     = true;
                    cfg.enableGyroscope      = true;
                    break;

                case PlatformTarget.AndroidTablet:
                    cfg.defaultQuality     = QualityTier.Medium;
                    cfg.targetFrameRate    = 60;
                    cfg.maxTextureResolution = 2048;
                    cfg.enableKeyboardMouse  = false;
                    cfg.enableGamepad        = true;
                    cfg.enableTouchInput     = true;
                    cfg.enableGyroscope      = true;
                    break;

                case PlatformTarget.MetaQuest:
                    cfg.defaultQuality     = QualityTier.Medium;
                    cfg.targetFrameRate    = 72;
                    cfg.maxTextureResolution = 2048;
                    cfg.enableXR           = true;
                    cfg.enableKeyboardMouse  = false;
                    cfg.enableGamepad        = true;
                    cfg.enableTouchInput     = false;
                    cfg.enableGyroscope      = true;
                    break;

                case PlatformTarget.VisionPro:
                    cfg.defaultQuality     = QualityTier.High;
                    cfg.targetFrameRate    = 90;
                    cfg.maxTextureResolution = 4096;
                    cfg.enableXR           = true;
                    cfg.enableKeyboardMouse  = false;
                    cfg.enableGamepad        = false;
                    cfg.enableTouchInput     = true;
                    cfg.enableGyroscope      = false;
                    break;
            }

            return cfg;
        }
    }
}
