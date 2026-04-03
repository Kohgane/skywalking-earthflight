// PlatformTargetMatrix.cs — SWEF Phase 95: Platform Target Matrix & Build Pipeline
// Detects the current runtime platform and exposes category/feature helpers.
using UnityEngine;

namespace SWEF.BuildPipeline
{
    // ── Enums ────────────────────────────────────────────────────────────────────

    /// <summary>All supported build/runtime target platforms.</summary>
    public enum PlatformTarget
    {
        WindowsPC,
        macOS,
        iOS,
        Android,
        iPadOS,
        AndroidTablet,
        MetaQuest,
        VisionPro
    }

    /// <summary>Broad device-category grouping used for UX and quality decisions.</summary>
    public enum PlatformCategory
    {
        PC,
        Mobile,
        Tablet,
        XR
    }

    // ── Static class ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Runtime platform detection and classification for SWEF.
    ///
    /// <para>SWEF must NOT be exclusive to any single device category.
    /// Primary targets: Windows PC, macOS, iOS, Android.
    /// High-priority targets: iPad, Android Tablet.
    /// Secondary/planned: Meta Quest, Apple Vision Pro.</para>
    /// </summary>
    public static class PlatformTargetMatrix
    {
        // ── Tablet detection constants ────────────────────────────────────────────

        /// <summary>
        /// Minimum screen diagonal in inches for a device to be treated as a tablet.
        /// iPads start at 7.9 in; 7-inch is a common small-tablet threshold.
        /// </summary>
        public const float TabletDiagonalThresholdInches = 7f;

        /// <summary>
        /// Minimum short-side pixel count (at native resolution) used as a fallback
        /// tablet heuristic when DPI is unavailable.
        /// </summary>
        public const int TabletShortSidePixelThreshold = 1024;

        // ── Platform detection ────────────────────────────────────────────────────

        /// <summary>
        /// Returns the current <see cref="PlatformTarget"/> at runtime.
        ///
        /// <para>Phone vs. tablet distinction for iOS/Android is made via
        /// <see cref="IsTablet()"/>.</para>
        /// </summary>
        public static PlatformTarget GetCurrentPlatform()
        {
#if UNITY_EDITOR
            // In the Editor, honour the active build target so tests get
            // predictable results when switching between platforms.
            return GetFromActiveBuildTarget();
#elif UNITY_STANDALONE_WIN
            return PlatformTarget.WindowsPC;
#elif UNITY_STANDALONE_OSX
            return PlatformTarget.macOS;
#elif UNITY_IOS
            if (IsVisionPro()) return PlatformTarget.VisionPro;
            if (IsTablet())    return PlatformTarget.iPadOS;
            return PlatformTarget.iOS;
#elif UNITY_ANDROID
            if (IsMetaQuest()) return PlatformTarget.MetaQuest;
            if (IsTablet())    return PlatformTarget.AndroidTablet;
            return PlatformTarget.Android;
#else
            return PlatformTarget.WindowsPC; // safe fallback
#endif
        }

        // ── Category ─────────────────────────────────────────────────────────────

        /// <summary>Returns the broad <see cref="PlatformCategory"/> for a given target.</summary>
        public static PlatformCategory GetCategory(PlatformTarget target)
        {
            switch (target)
            {
                case PlatformTarget.WindowsPC:
                case PlatformTarget.macOS:
                    return PlatformCategory.PC;

                case PlatformTarget.iOS:
                case PlatformTarget.Android:
                    return PlatformCategory.Mobile;

                case PlatformTarget.iPadOS:
                case PlatformTarget.AndroidTablet:
                    return PlatformCategory.Tablet;

                case PlatformTarget.MetaQuest:
                case PlatformTarget.VisionPro:
                    return PlatformCategory.XR;

                default:
                    return PlatformCategory.PC;
            }
        }

        // ── Primary-platform flag ─────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> for the four primary shipping targets:
        /// Windows PC, macOS, iOS, and Android (phone).
        /// Tablets and XR are high-priority / secondary respectively.
        /// </summary>
        public static bool IsPrimaryPlatform(PlatformTarget target)
        {
            return target == PlatformTarget.WindowsPC
                || target == PlatformTarget.macOS
                || target == PlatformTarget.iOS
                || target == PlatformTarget.Android;
        }

        // ── Tablet heuristic ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> when the current device is likely a tablet.
        ///
        /// <para>Two-pass heuristic:
        /// 1. If DPI &gt; 0 — compute physical screen diagonal in inches.
        /// 2. Fallback: short side of the screen &gt;= <see cref="TabletShortSidePixelThreshold"/> px.</para>
        /// </summary>
        public static bool IsTablet()
        {
            float dpi = Screen.dpi;

            if (dpi > 0f)
            {
                float widthInches  = Screen.width  / dpi;
                float heightInches = Screen.height / dpi;
                float diagonal = Mathf.Sqrt(widthInches * widthInches + heightInches * heightInches);
                return diagonal >= TabletDiagonalThresholdInches;
            }

            // DPI unavailable — fall back to pixel count
            int shortSide = Mathf.Min(Screen.width, Screen.height);
            return shortSide >= TabletShortSidePixelThreshold;
        }

        // ── Feature gate ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> if the specified feature is available on the given platform.
        ///
        /// <para>This method provides built-in defaults. Individual features can be
        /// further overridden at runtime via <see cref="PlatformFeatureGate"/>.</para>
        ///
        /// <para>Supported feature IDs: <c>"xr"</c>, <c>"gyroscope"</c>, <c>"gps"</c>,
        /// <c>"touch"</c>, <c>"keyboard"</c>, <c>"gamepad"</c>, <c>"haptics"</c>,
        /// <c>"arcore"</c>, <c>"arkit"</c>.</para>
        /// </summary>
        public static bool SupportsFeature(string featureId, PlatformTarget target)
        {
            if (string.IsNullOrEmpty(featureId)) return false;

            switch (featureId.ToLowerInvariant())
            {
                case "xr":
                    return target == PlatformTarget.MetaQuest
                        || target == PlatformTarget.VisionPro;

                case "gyroscope":
                    return target == PlatformTarget.iOS
                        || target == PlatformTarget.Android
                        || target == PlatformTarget.iPadOS
                        || target == PlatformTarget.AndroidTablet
                        || target == PlatformTarget.MetaQuest;

                case "gps":
                    return target == PlatformTarget.iOS
                        || target == PlatformTarget.Android
                        || target == PlatformTarget.iPadOS
                        || target == PlatformTarget.AndroidTablet;

                case "touch":
                    return target != PlatformTarget.WindowsPC
                        && target != PlatformTarget.macOS;

                case "keyboard":
                    return target == PlatformTarget.WindowsPC
                        || target == PlatformTarget.macOS;

                case "gamepad":
                    return target == PlatformTarget.WindowsPC
                        || target == PlatformTarget.macOS
                        || target == PlatformTarget.iOS
                        || target == PlatformTarget.Android
                        || target == PlatformTarget.iPadOS
                        || target == PlatformTarget.AndroidTablet
                        || target == PlatformTarget.MetaQuest;

                case "haptics":
                    return target == PlatformTarget.iOS
                        || target == PlatformTarget.Android
                        || target == PlatformTarget.iPadOS
                        || target == PlatformTarget.AndroidTablet
                        || target == PlatformTarget.MetaQuest;

                case "arcore":
                    return target == PlatformTarget.Android
                        || target == PlatformTarget.AndroidTablet;

                case "arkit":
                    return target == PlatformTarget.iOS
                        || target == PlatformTarget.iPadOS
                        || target == PlatformTarget.VisionPro;

                default:
                    return false;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private static bool IsTabletFromCurrentScreen() => IsTablet();

        private static bool IsVisionPro()
        {
#if UNITY_IOS
            // visionOS reports itself as a separate platform in Unity 2022.3+.
            // As a runtime guard, check the model string when available.
            return SystemInfo.deviceModel != null
                && SystemInfo.deviceModel.StartsWith("RealityDevice", System.StringComparison.OrdinalIgnoreCase);
#else
            return false;
#endif
        }

        private static bool IsMetaQuest()
        {
#if UNITY_ANDROID
            string model = SystemInfo.deviceModel ?? string.Empty;
            return model.IndexOf("Quest", System.StringComparison.OrdinalIgnoreCase) >= 0
                || model.IndexOf("Oculus", System.StringComparison.OrdinalIgnoreCase) >= 0;
#else
            return false;
#endif
        }

#if UNITY_EDITOR
        private static PlatformTarget GetFromActiveBuildTarget()
        {
            switch (UnityEditor.EditorUserBuildSettings.activeBuildTarget)
            {
                case UnityEditor.BuildTarget.StandaloneWindows:
                case UnityEditor.BuildTarget.StandaloneWindows64:
                    return PlatformTarget.WindowsPC;
                case UnityEditor.BuildTarget.StandaloneOSX:
                    return PlatformTarget.macOS;
                case UnityEditor.BuildTarget.iOS:
                    return IsTablet() ? PlatformTarget.iPadOS : PlatformTarget.iOS;
                case UnityEditor.BuildTarget.Android:
                    return IsTablet() ? PlatformTarget.AndroidTablet : PlatformTarget.Android;
                default:
                    return PlatformTarget.WindowsPC;
            }
        }
#endif
    }
}
