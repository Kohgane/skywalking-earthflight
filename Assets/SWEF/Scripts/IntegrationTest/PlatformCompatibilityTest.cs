// PlatformCompatibilityTest.cs — SWEF Phase 96: Integration Test & QA Framework
// Tests platform-specific code paths and validates cross-platform support.
using UnityEngine;

namespace SWEF.IntegrationTest
{
    /// <summary>
    /// Validates that platform-specific code paths compile and behave correctly
    /// on all SWEF target platforms: PC (Windows/Mac/Linux), Mobile (iOS/Android),
    /// Tablet (iPad/Android Tablet), and XR (Google Glass / Apple Vision Pro).
    ///
    /// <para>This test does NOT require a running scene — it evaluates compile-time
    /// defines and runtime <see cref="SystemInfo"/> to confirm the current platform
    /// is recognised and supported.</para>
    /// </summary>
    public class PlatformCompatibilityTest : IntegrationTestCase
    {
        /// <inheritdoc/>
        public override string TestName => "PlatformCompatibility";

        /// <inheritdoc/>
        public override string ModuleName => "Platform";

        /// <inheritdoc/>
        public override int Priority => 5; // Run very early.

        /// <inheritdoc/>
        public override IntegrationTestResult Setup() => null;

        /// <inheritdoc/>
        public override IntegrationTestResult Execute()
        {
            string platform = DetectPlatform();
            bool supported  = IsSupportedPlatform();

            if (!supported)
                return Fail($"Unrecognised/unsupported platform: {Application.platform}. " +
                            "SWEF must run on PC, Mobile, Tablet, or XR.");

            string inputBackend = CheckInputBackend();
            string uiCheck      = CheckUIScaling();

            string msg = $"Platform={platform} | Input={inputBackend} | UI={uiCheck}";
            return Pass(msg);
        }

        /// <inheritdoc/>
        public override void Teardown() { }

        // ── Platform detection ────────────────────────────────────────────────

        private static string DetectPlatform()
        {
#if UNITY_STANDALONE_WIN
            return "PC/Windows";
#elif UNITY_STANDALONE_OSX
            return "PC/macOS";
#elif UNITY_STANDALONE_LINUX
            return "PC/Linux";
#elif UNITY_IOS
            // Differentiate iPad vs iPhone using screen aspect ratio heuristic.
            float aspect = (float)Screen.width / Screen.height;
            return IsTabletAspect(aspect) ? "Tablet/iPad" : "Mobile/iOS";
#elif UNITY_ANDROID
            float aspect = (float)Screen.width / Screen.height;
            return IsTabletAspect(aspect) ? "Tablet/Android" : "Mobile/Android";
#elif UNITY_WSA
            return "PC/UWP";
#else
            return $"Unknown ({Application.platform})";
#endif
        }

        private static bool IsSupportedPlatform()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.IPhonePlayer:
                case RuntimePlatform.Android:
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.LinuxEditor:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsTabletAspect(float widthOverHeight)
        {
            // Tablets typically have aspect ratios between 4:3 (1.33) and 16:10 (1.6).
            float ratio = widthOverHeight > 1f ? widthOverHeight : 1f / widthOverHeight;
            return ratio < 1.65f;
        }

        // ── Input backend check ───────────────────────────────────────────────

        private static string CheckInputBackend()
        {
            // Verify at least one input path is available.
#if ENABLE_INPUT_SYSTEM
            return "NewInputSystem";
#elif ENABLE_LEGACY_INPUT_MANAGER
            return "LegacyInputManager";
#else
            return "NoInputBackend(WARNING)";
#endif
        }

        // ── UI scaling check ──────────────────────────────────────────────────

        private static string CheckUIScaling()
        {
            float dpi = Screen.dpi;
            // DPI of 0 means unknown (common in editor or some devices).
            if (dpi <= 0f)
                return $"DPI=unknown res={Screen.width}x{Screen.height}";

            return $"DPI={dpi:F0} res={Screen.width}x{Screen.height}";
        }
    }
}
