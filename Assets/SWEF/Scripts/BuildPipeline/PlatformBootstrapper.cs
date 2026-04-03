// PlatformBootstrapper.cs — SWEF Phase 95: Platform Target Matrix & Build Pipeline
// MonoBehaviour that detects the runtime platform and configures quality,
// frame rate, and input subsystems on Awake.
using System;
using UnityEngine;

namespace SWEF.BuildPipeline
{
    /// <summary>
    /// Attach to the boot scene's root GameObject (or a persistent manager).
    ///
    /// <para>On <c>Awake</c> this component:
    /// <list type="number">
    ///   <item>Detects the current platform via <see cref="PlatformTargetMatrix"/>.</item>
    ///   <item>Loads (or generates) a <see cref="BuildProfileConfig"/> for that platform.</item>
    ///   <item>Applies <see cref="QualitySettings"/> and target frame rate.</item>
    ///   <item>Enables/disables input subsystems (touch, keyboard, gamepad, gyro).</item>
    ///   <item>Optionally coordinates with <c>PlatformOptimizer</c> (Phase 93) if present.</item>
    ///   <item>Fires <see cref="OnPlatformDetected"/> for other systems to react.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class PlatformBootstrapper : MonoBehaviour
    {
        // ── Serialised fields ─────────────────────────────────────────────────────

        [Header("Quality Tier → Unity Quality Level mapping")]
        [Tooltip("Unity quality level index to use for QualityTier.Ultra (0 = highest).")]
        [SerializeField] private int ultraQualityIndex  = 5;
        [SerializeField] private int highQualityIndex   = 4;
        [SerializeField] private int mediumQualityIndex = 3;
        [SerializeField] private int lowQualityIndex    = 2;
        [SerializeField] private int potatoQualityIndex = 0;

        [Header("Coordination")]
        [Tooltip("When true, forwards the detected quality tier to SWEF.Accessibility.PlatformOptimizer.")]
        [SerializeField] private bool coordinateWithPlatformOptimizer = true;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Fired after the platform has been fully detected and configured.
        /// Subscribe before <c>Awake</c> completes (e.g. from script execution order).
        /// </summary>
        public static event Action<PlatformTarget, BuildProfileConfig> OnPlatformDetected;

        // ── Runtime state ─────────────────────────────────────────────────────────

        /// <summary>Detected platform target for this session.</summary>
        public static PlatformTarget CurrentPlatform { get; private set; }

        /// <summary>Active build profile for this session.</summary>
        public static BuildProfileConfig ActiveProfile { get; private set; }

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            CurrentPlatform = PlatformTargetMatrix.GetCurrentPlatform();
            ActiveProfile   = LoadProfile(CurrentPlatform);

            ApplyQualitySettings(ActiveProfile);
            ApplyFrameRate(ActiveProfile);
            ApplyInputSubsystems(ActiveProfile);
            CoordinateWithPlatformOptimizer(ActiveProfile);

            Debug.Log($"[PlatformBootstrapper] Platform detected: {CurrentPlatform} " +
                      $"| Category: {PlatformTargetMatrix.GetCategory(CurrentPlatform)} " +
                      $"| Quality: {ActiveProfile.defaultQuality} " +
                      $"| FPS target: {ActiveProfile.targetFrameRate}");

            OnPlatformDetected?.Invoke(CurrentPlatform, ActiveProfile);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private BuildProfileConfig LoadProfile(PlatformTarget platform)
        {
            string resourcePath = $"BuildPipeline/{platform}Profile";
            var profile = Resources.Load<BuildProfileConfig>(resourcePath);
            if (profile == null)
            {
                Debug.LogWarning($"[PlatformBootstrapper] No asset at Resources/{resourcePath}. " +
                                 "Using generated defaults.");
                profile = BuildProfileConfig.CreateDefault(platform);
            }
            return profile;
        }

        private void ApplyQualitySettings(BuildProfileConfig profile)
        {
            int levelIndex = QualityTierToIndex(profile.defaultQuality);
            int clamped    = Mathf.Clamp(levelIndex, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(clamped, applyExpensiveChanges: true);

            if (profile.maxTextureResolution > 0)
            {
                // Unity uses a global texture limit: 0 = full, 1 = half, 2 = quarter …
                // Map our pixel-budget to Unity's mipmap offset (best-effort).
                int mipOffset = profile.maxTextureResolution >= 4096 ? 0
                              : profile.maxTextureResolution >= 2048 ? 1
                              : profile.maxTextureResolution >= 1024 ? 2
                              : 3;
                QualitySettings.globalTextureMipmapLimit = mipOffset;
            }
        }

        private static void ApplyFrameRate(BuildProfileConfig profile)
        {
            if (profile.targetFrameRate > 0)
                Application.targetFrameRate = profile.targetFrameRate;
            else
                Application.targetFrameRate = -1; // platform/vsync default
        }

        private static void ApplyInputSubsystems(BuildProfileConfig profile)
        {
            // Gyroscope
            if (SystemInfo.supportsGyroscope && profile.enableGyroscope)
                Input.gyro.enabled = true;
            else if (SystemInfo.supportsGyroscope)
                Input.gyro.enabled = false;

            // Touch / keyboard / gamepad flags are consumed at runtime by
            // AdaptiveInputManager (Phase 93). Store them in PlatformFeatureGate
            // so any system can query them without depending on the profile directly.
            PlatformFeatureGate.SetOverride("touch",    profile.enableTouchInput);
            PlatformFeatureGate.SetOverride("keyboard", profile.enableKeyboardMouse);
            PlatformFeatureGate.SetOverride("gamepad",  profile.enableGamepad);
            PlatformFeatureGate.SetOverride("xr",       profile.enableXR);
            PlatformFeatureGate.SetOverride("gyroscope", profile.enableGyroscope);
        }

        private static void CoordinateWithPlatformOptimizer(BuildProfileConfig profile)
        {
            // Soft reference to SWEF.Accessibility.PlatformOptimizer (Phase 93).
            // We use reflection to avoid a hard compile-time dependency.
#if SWEF_ACCESSIBILITY_AVAILABLE
            var optimizer = FindObjectOfType<SWEF.Accessibility.PlatformOptimizer>();
            if (optimizer != null)
            {
                // Map our QualityTier to the Accessibility namespace enum via name.
                if (System.Enum.TryParse(profile.defaultQuality.ToString(),
                                          out SWEF.Accessibility.QualityTier accessTier))
                {
                    optimizer.SetQualityTier(accessTier);
                }
            }
#endif
        }

        private int QualityTierToIndex(QualityTier tier)
        {
            switch (tier)
            {
                case QualityTier.Ultra:  return ultraQualityIndex;
                case QualityTier.High:   return highQualityIndex;
                case QualityTier.Medium: return mediumQualityIndex;
                case QualityTier.Low:    return lowQualityIndex;
                case QualityTier.Potato: return potatoQualityIndex;
                default:                 return mediumQualityIndex;
            }
        }
    }
}
