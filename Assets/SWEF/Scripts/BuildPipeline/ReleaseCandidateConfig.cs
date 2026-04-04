// ReleaseCandidateConfig.cs — SWEF Phase 102: Final QA & Release Candidate Prep
// Provides compile-time constants and runtime accessors for the v1.0.0-rc1 build.
// Consumed by CIBuildRunner, SWEFBuildPreprocessor, and store-submission tooling.
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SWEF.BuildPipeline
{
    // ── Version constants ─────────────────────────────────────────────────────────

    /// <summary>
    /// Compile-time constants for the SWEF v1.0.0-rc1 release candidate.
    ///
    /// <para>These values are applied to Unity Player Settings by
    /// <see cref="RCPlayerSettingsApplicator"/> at build time.</para>
    /// </summary>
    public static class ReleaseCandidateVersion
    {
        /// <summary>Semver version string for the release candidate.</summary>
        public const string Version          = "1.0.0-rc1";

        /// <summary>Marketing-friendly version shown to players.</summary>
        public const string DisplayVersion   = "1.0.0 RC1";

        /// <summary>Integer build number — increment on every uploaded build.</summary>
        public const int    BuildNumber      = 1;

        /// <summary>Company name as it should appear in all store submissions.</summary>
        public const string CompanyName      = "Kohgane";

        /// <summary>Full product title.</summary>
        public const string ProductName      = "Skywalking: Earth Flight";

        /// <summary>Base bundle identifier / application ID.</summary>
        public const string BundleIdentifier = "com.kohgane.swef";

        /// <summary>
        /// Whether this is a development (debug) build.
        /// Must be <c>false</c> for all RC builds submitted to stores.
        /// </summary>
        public const bool   IsDevelopment    = false;
    }

    // ── Per-platform RC settings ──────────────────────────────────────────────────

    /// <summary>
    /// Release candidate player settings for each target platform.
    /// </summary>
    public static class RCPlatformSettings
    {
        // ── Windows PC ────────────────────────────────────────────────────────

        /// <summary>x64 standalone — 64-bit Windows executable.</summary>
        public static readonly RCPlatformProfile WindowsPC = new RCPlatformProfile
        {
            PlatformName      = "Windows PC",
            UnityBuildTarget  = "StandaloneWindows64",
            BundleIdentifier  = ReleaseCandidateVersion.BundleIdentifier,
            TargetFps         = 60,
            DefaultQuality    = "High",
            ScriptingBackend  = "IL2CPP",
            Architecture      = "x86_64",
            StripEngineCode   = true,
            ManagedStripping  = "Medium",
            Defines           = new[] { "SWEF_RELEASE", "SWEF_PLATFORM_PC" },
            MinOSVersion      = "Windows 10 (10.0)"
        };

        // ── macOS ─────────────────────────────────────────────────────────────

        /// <summary>Universal macOS — Intel x64 + Apple Silicon ARM64.</summary>
        public static readonly RCPlatformProfile macOS = new RCPlatformProfile
        {
            PlatformName      = "macOS",
            UnityBuildTarget  = "StandaloneOSX",
            BundleIdentifier  = ReleaseCandidateVersion.BundleIdentifier + ".mac",
            TargetFps         = 60,
            DefaultQuality    = "High",
            ScriptingBackend  = "IL2CPP",
            Architecture      = "Universal",   // Intel x64 + Apple Silicon
            StripEngineCode   = true,
            ManagedStripping  = "Medium",
            Defines           = new[] { "SWEF_RELEASE", "SWEF_PLATFORM_PC" },
            MinOSVersion      = "macOS 13.0"
        };

        // ── iOS ───────────────────────────────────────────────────────────────

        /// <summary>iOS ARM64 — iPhone and iPad (split at runtime by TabletLayoutManager).</summary>
        public static readonly RCPlatformProfile iOS = new RCPlatformProfile
        {
            PlatformName      = "iOS",
            UnityBuildTarget  = "iOS",
            BundleIdentifier  = ReleaseCandidateVersion.BundleIdentifier,
            TargetFps         = 60,            // ProMotion iPads run at 120; capped to 60 for battery
            DefaultQuality    = "High",
            ScriptingBackend  = "IL2CPP",
            Architecture      = "ARM64",
            StripEngineCode   = true,
            ManagedStripping  = "High",
            Defines           = new[] { "SWEF_RELEASE", "SWEF_PLATFORM_MOBILE", "SWEF_PLATFORM_IOS" },
            MinOSVersion      = "iOS 15.0"
        };

        // ── Android ───────────────────────────────────────────────────────────

        /// <summary>Android — ARM64 primary, ARMv7 secondary.</summary>
        public static readonly RCPlatformProfile Android = new RCPlatformProfile
        {
            PlatformName      = "Android",
            UnityBuildTarget  = "Android",
            BundleIdentifier  = ReleaseCandidateVersion.BundleIdentifier,
            TargetFps         = 30,
            DefaultQuality    = "Medium",
            ScriptingBackend  = "IL2CPP",
            Architecture      = "ARM64+ARMv7",
            StripEngineCode   = true,
            ManagedStripping  = "High",
            Defines           = new[] { "SWEF_RELEASE", "SWEF_PLATFORM_MOBILE", "SWEF_PLATFORM_ANDROID" },
            MinOSVersion      = "Android 8.0 (API 26)"
        };
    }

    // ── Profile struct ────────────────────────────────────────────────────────────

    /// <summary>
    /// Describes all player settings and compilation flags for a single platform RC build.
    /// </summary>
    public sealed class RCPlatformProfile
    {
        /// <summary>Human-readable platform name.</summary>
        public string   PlatformName;

        /// <summary>Unity BuildTarget string (matches <c>BuildTarget</c> enum names).</summary>
        public string   UnityBuildTarget;

        /// <summary>Bundle identifier / application ID.</summary>
        public string   BundleIdentifier;

        /// <summary>Target frame rate applied via <c>Application.targetFrameRate</c>.</summary>
        public int      TargetFps;

        /// <summary>Default Unity quality level name applied at first launch.</summary>
        public string   DefaultQuality;

        /// <summary>Scripting backend ("IL2CPP" or "Mono").</summary>
        public string   ScriptingBackend;

        /// <summary>CPU architecture(s) to include in the build.</summary>
        public string   Architecture;

        /// <summary>Whether engine-code stripping is enabled (reduces binary size).</summary>
        public bool     StripEngineCode;

        /// <summary>Managed code stripping level ("Low", "Medium", "High", "Disabled").</summary>
        public string   ManagedStripping;

        /// <summary>Scripting define symbols added for this platform's RC build.</summary>
        public string[] Defines;

        /// <summary>Minimum OS version string for the store submission.</summary>
        public string   MinOSVersion;
    }

#if UNITY_EDITOR
    // ── Editor applicator ─────────────────────────────────────────────────────────

    /// <summary>
    /// Editor utility that writes <see cref="RCPlatformSettings"/> values into
    /// Unity's Player Settings for the currently active build target.
    ///
    /// <para>Run via <c>SWEF → Release Candidate → Apply RC Player Settings</c>.</para>
    /// </summary>
    public static class RCPlayerSettingsApplicator
    {
        [MenuItem("SWEF/Release Candidate/Apply RC Player Settings (Active Platform)")]
        public static void ApplyForActivePlatform()
        {
            var profile = GetProfileForActiveBuildTarget();
            if (profile == null)
            {
                Debug.LogWarning("[SWEF RC] No RC profile defined for the current build target. Skipping.");
                return;
            }

            Apply(profile);
        }

        [MenuItem("SWEF/Release Candidate/Apply RC Player Settings — Windows PC")]
        public static void ApplyWindows() => Apply(RCPlatformSettings.WindowsPC);

        [MenuItem("SWEF/Release Candidate/Apply RC Player Settings — macOS")]
        public static void ApplyMacOS() => Apply(RCPlatformSettings.macOS);

        [MenuItem("SWEF/Release Candidate/Apply RC Player Settings — iOS")]
        public static void ApplyiOS() => Apply(RCPlatformSettings.iOS);

        [MenuItem("SWEF/Release Candidate/Apply RC Player Settings — Android")]
        public static void ApplyAndroid() => Apply(RCPlatformSettings.Android);

        // ── Core ──────────────────────────────────────────────────────────────

        private static void Apply(RCPlatformProfile profile)
        {
            // Identity
            PlayerSettings.companyName               = ReleaseCandidateVersion.CompanyName;
            PlayerSettings.productName               = ReleaseCandidateVersion.ProductName;
            PlayerSettings.bundleVersion             = ReleaseCandidateVersion.Version;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Unknown, profile.BundleIdentifier);

            // Quality / FPS (runtime; also set here as default)
            Application.targetFrameRate = profile.TargetFps;

            Debug.Log($"[SWEF RC] Applied RC player settings for {profile.PlatformName} — " +
                      $"v{ReleaseCandidateVersion.Version} ({ReleaseCandidateVersion.BuildNumber}).");
        }

        private static RCPlatformProfile GetProfileForActiveBuildTarget()
        {
            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.StandaloneWindows64: return RCPlatformSettings.WindowsPC;
                case BuildTarget.StandaloneOSX:       return RCPlatformSettings.macOS;
                case BuildTarget.iOS:                 return RCPlatformSettings.iOS;
                case BuildTarget.Android:             return RCPlatformSettings.Android;
                default:                              return null;
            }
        }
    }
#endif
}
