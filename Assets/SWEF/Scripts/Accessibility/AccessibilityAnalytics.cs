// AccessibilityAnalytics.cs — SWEF Accessibility & Platform Optimization (Phase 93)
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Accessibility
{
    /// <summary>
    /// Static helper that wraps all Accessibility telemetry events and forwards them
    /// to <c>SWEF.Analytics.TelemetryDispatcher.EnqueueEvent</c>.
    ///
    /// <para>All calls are guarded by <c>#if SWEF_ANALYTICS_AVAILABLE</c> so the
    /// class compiles cleanly even when the Analytics module is absent.</para>
    ///
    /// <para>Events dispatched:</para>
    /// <list type="bullet">
    ///   <item><c>accessibility_profile_applied</c></item>
    ///   <item><c>accessibility_preset_selected</c></item>
    ///   <item><c>colorblind_mode_changed</c></item>
    ///   <item><c>subtitle_toggled</c></item>
    ///   <item><c>screen_reader_toggled</c></item>
    ///   <item><c>quality_tier_changed</c></item>
    ///   <item><c>input_remap_saved</c></item>
    ///   <item><c>memory_cleanup_triggered</c></item>
    /// </list>
    /// </summary>
    public static class AccessibilityAnalytics
    {
        /// <summary>Records that an accessibility profile was applied.</summary>
        public static void RecordProfileApplied(string profileName)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("accessibility_profile_applied",
                new Dictionary<string, object>
                {
                    { "profile_name", profileName }
                });
#endif
        }

        /// <summary>Records that a preset was selected from the accessibility menu.</summary>
        public static void RecordPresetSelected(string presetName)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("accessibility_preset_selected",
                new Dictionary<string, object>
                {
                    { "preset_name", presetName }
                });
#endif
        }

        /// <summary>Records a color-blind mode change.</summary>
        public static void RecordColorBlindModeChanged(string mode)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("colorblind_mode_changed",
                new Dictionary<string, object>
                {
                    { "mode", mode }
                });
#endif
        }

        /// <summary>Records subtitle feature toggle.</summary>
        public static void RecordSubtitleToggled(bool enabled)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("subtitle_toggled",
                new Dictionary<string, object>
                {
                    { "enabled", enabled }
                });
#endif
        }

        /// <summary>Records screen-reader feature toggle.</summary>
        public static void RecordScreenReaderToggled(bool enabled)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("screen_reader_toggled",
                new Dictionary<string, object>
                {
                    { "enabled", enabled }
                });
#endif
        }

        /// <summary>Records a quality tier change (manual or auto-scaled).</summary>
        public static void RecordQualityTierChanged(string fromTier, string toTier, bool automatic)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("quality_tier_changed",
                new Dictionary<string, object>
                {
                    { "from_tier",  fromTier  },
                    { "to_tier",    toTier    },
                    { "automatic",  automatic }
                });
#endif
        }

        /// <summary>Records that the user saved a custom input remap.</summary>
        public static void RecordInputRemapSaved(int actionCount)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("input_remap_saved",
                new Dictionary<string, object>
                {
                    { "action_count", actionCount }
                });
#endif
        }

        /// <summary>Records that a memory cleanup was triggered.</summary>
        public static void RecordMemoryCleanup(float usageMBBefore)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("memory_cleanup_triggered",
                new Dictionary<string, object>
                {
                    { "usage_mb_before", usageMBBefore }
                });
#endif
        }
    }
}
