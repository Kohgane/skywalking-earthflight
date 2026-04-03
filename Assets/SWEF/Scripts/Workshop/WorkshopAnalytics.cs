// WorkshopAnalytics.cs — SWEF Aircraft Workshop & Part Customization (Phase 90)
using UnityEngine;

namespace SWEF.Workshop
{
    /// <summary>
    /// Static helper that wraps all Workshop telemetry events and forwards them
    /// to <c>SWEF.Analytics.TelemetryDispatcher.EnqueueEvent</c>.
    ///
    /// <para>All calls are guarded by <c>#if SWEF_ANALYTICS_AVAILABLE</c> so the
    /// class compiles cleanly even when the Analytics module is absent.</para>
    ///
    /// <para>Event types dispatched:</para>
    /// <list type="bullet">
    ///   <item><c>workshop_opened</c></item>
    ///   <item><c>workshop_closed</c></item>
    ///   <item><c>part_equipped</c></item>
    ///   <item><c>part_unequipped</c></item>
    ///   <item><c>build_saved</c></item>
    ///   <item><c>build_loaded</c></item>
    ///   <item><c>build_shared</c></item>
    ///   <item><c>build_imported</c></item>
    ///   <item><c>paint_applied</c></item>
    ///   <item><c>decal_placed</c></item>
    ///   <item><c>part_unlocked</c></item>
    /// </list>
    /// </summary>
    public static class WorkshopAnalytics
    {
        /// <summary>Records that the Workshop was opened.</summary>
        public static void RecordWorkshopOpened()
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("workshop_opened", null);
#endif
        }

        /// <summary>Records that the Workshop was closed.</summary>
        public static void RecordWorkshopClosed()
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("workshop_closed", null);
#endif
        }

        /// <summary>Records that a part was equipped into the active build.</summary>
        /// <param name="partId">Equipped part ID.</param>
        /// <param name="partType">String representation of the part's <see cref="AircraftPartType"/>.</param>
        public static void RecordPartEquipped(string partId, string partType)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("part_equipped",
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "part_id",   partId   },
                    { "part_type", partType }
                });
#endif
        }

        /// <summary>Records that a part was unequipped from the active build.</summary>
        /// <param name="partId">Unequipped part ID.</param>
        /// <param name="partType">String representation of the part's <see cref="AircraftPartType"/>.</param>
        public static void RecordPartUnequipped(string partId, string partType)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("part_unequipped",
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "part_id",   partId   },
                    { "part_type", partType }
                });
#endif
        }

        /// <summary>Records that the active build was saved.</summary>
        /// <param name="buildId">Build ID that was saved.</param>
        public static void RecordBuildSaved(string buildId)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("build_saved",
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "build_id", buildId }
                });
#endif
        }

        /// <summary>Records that a saved build was loaded into the editor.</summary>
        /// <param name="buildId">Build ID that was loaded.</param>
        public static void RecordBuildLoaded(string buildId)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("build_loaded",
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "build_id", buildId }
                });
#endif
        }

        /// <summary>Records that a build was exported and shared.</summary>
        /// <param name="buildId">Build ID that was shared.</param>
        public static void RecordBuildShared(string buildId)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("build_shared",
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "build_id", buildId }
                });
#endif
        }

        /// <summary>Records that an imported build was successfully decoded.</summary>
        /// <param name="buildName">Display name of the imported build.</param>
        public static void RecordBuildImported(string buildName)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("build_imported",
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "build_name", buildName }
                });
#endif
        }

        /// <summary>Records that a paint scheme was applied to the active build.</summary>
        /// <param name="patternName">String representation of the chosen <see cref="PaintPattern"/>.</param>
        public static void RecordPaintApplied(string patternName)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("paint_applied",
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "pattern", patternName }
                });
#endif
        }

        /// <summary>Records that a decal was placed on the aircraft.</summary>
        /// <param name="texturePath">Resource path of the placed decal texture.</param>
        public static void RecordDecalPlaced(string texturePath)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("decal_placed",
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "texture", texturePath }
                });
#endif
        }

        /// <summary>Records that a part was unlocked via the unlock tree.</summary>
        /// <param name="partId">ID of the newly unlocked part.</param>
        /// <param name="tier">Tier name of the unlocked part.</param>
        public static void RecordPartUnlocked(string partId, string tier)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("part_unlocked",
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "part_id", partId },
                    { "tier",    tier   }
                });
#endif
        }
    }
}
