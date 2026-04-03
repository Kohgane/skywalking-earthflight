// WorkshopBridge.cs — SWEF Aircraft Workshop & Part Customization (Phase 90)
using UnityEngine;

namespace SWEF.Workshop
{
    /// <summary>
    /// MonoBehaviour integration bridge that connects the Workshop system to the
    /// wider SWEF ecosystem:
    ///
    /// <list type="bullet">
    ///   <item><see cref="SWEF.Progression.ProgressionManager.AddXP"/> — awarded on part unlock.</item>
    ///   <item><see cref="SWEF.Achievement.AchievementManager.ReportProgress"/> — tracks workshop achievements.</item>
    ///   <item><c>SWEF.SocialHub.SocialActivityFeed.PostActivity</c> — posts when a build is shared.</item>
    ///   <item><c>SWEF.Analytics.TelemetryDispatcher.EnqueueEvent</c> — via <see cref="WorkshopAnalytics"/>.</item>
    /// </list>
    ///
    /// <para>All integration calls are null-safe and guarded by compile-time symbols.</para>
    /// </summary>
    public class WorkshopBridge : MonoBehaviour
    {
        // ── Achievement progress keys ──────────────────────────────────────────
        private const string AchievFirstCustomBuild  = "first_custom_build";
        private const string AchievAllPartsUnlocked  = "all_parts_unlocked";
        private const string AchievLegendaryCollector = "legendary_collector";
        private const string AchievSharedBuild       = "shared_build";

        // ── XP constants ───────────────────────────────────────────────────────
        private const int XpPerPartUnlock  = 50;
        private const int XpPerBuildSaved  = 10;
        private const int XpPerBuildShared = 25;

        // ── Unity Lifecycle ────────────────────────────────────────────────────

        private void Start()
        {
            var mgr = WorkshopManager.Instance;
            if (mgr == null) return;

            mgr.OnBuildChanged   += HandleBuildChanged;
            mgr.OnPartEquipped   += HandlePartEquipped;
            mgr.OnWorkshopOpened += HandleWorkshopOpened;
        }

        private void OnDestroy()
        {
            var mgr = WorkshopManager.Instance;
            if (mgr == null) return;

            mgr.OnBuildChanged   -= HandleBuildChanged;
            mgr.OnPartEquipped   -= HandlePartEquipped;
            mgr.OnWorkshopOpened -= HandleWorkshopOpened;
        }

        // ── Event handlers ─────────────────────────────────────────────────────

        private void HandleWorkshopOpened()
        {
            ReportAchievementProgress(AchievFirstCustomBuild, 1);
        }

        private void HandlePartEquipped(AircraftPartData part)
        {
            if (part == null) return;

            // Award XP per part unlock/equip.
#if SWEF_PROGRESSION_AVAILABLE
            var prog = SWEF.Progression.ProgressionManager.Instance;
            prog?.AddXP(XpPerPartUnlock, "workshop_part_equipped");
#endif

            // Track legendary collector achievement.
            if (part.tier == PartTier.Legendary)
                ReportAchievementProgress(AchievLegendaryCollector, 1);
        }

        private void HandleBuildChanged(AircraftBuildData build)
        {
            if (build == null) return;

            // Award small XP on build save (called from WorkshopManager.SaveBuild).
#if SWEF_PROGRESSION_AVAILABLE
            var prog = SWEF.Progression.ProgressionManager.Instance;
            prog?.AddXP(XpPerBuildSaved, "workshop_build_saved");
#endif
        }

        // ── Public: called directly from AircraftShareManager.ShareBuild ──────

        /// <summary>
        /// Called when a build is shared.  Awards XP and reports achievement progress.
        /// </summary>
        /// <param name="buildId">The ID of the build that was shared.</param>
        public static void OnBuildShared(string buildId)
        {
#if SWEF_PROGRESSION_AVAILABLE
            var prog = SWEF.Progression.ProgressionManager.Instance;
            prog?.AddXP(XpPerBuildShared, "workshop_build_shared");
#endif
            ReportAchievementProgressStatic(AchievSharedBuild, 1);

#if SWEF_SOCIAL_AVAILABLE
            var feed = SWEF.SocialHub.SocialActivityFeed.Instance;
            feed?.PostActivity("workshop_build_shared",
                $"Shared a custom aircraft build!", buildId);
#endif
        }

        /// <summary>
        /// Called when a part is unlocked via <see cref="PartUnlockTree"/>.
        /// Awards XP and checks whether all parts are now unlocked.
        /// </summary>
        /// <param name="part">The part that was just unlocked.</param>
        public static void OnPartUnlocked(AircraftPartData part)
        {
            if (part == null) return;

#if SWEF_PROGRESSION_AVAILABLE
            var prog = SWEF.Progression.ProgressionManager.Instance;
            prog?.AddXP(XpPerPartUnlock, "workshop_part_unlocked");
#endif

            WorkshopAnalytics.RecordPartUnlocked(part.partId, part.tier.ToString());
            ReportAchievementProgressStatic(AchievAllPartsUnlocked, 1);

            if (part.tier == PartTier.Legendary)
                ReportAchievementProgressStatic(AchievLegendaryCollector, 1);
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private void ReportAchievementProgress(string key, int amount)
        {
#if SWEF_ACHIEVEMENT_AVAILABLE
            var ach = SWEF.Achievement.AchievementManager.Instance;
            ach?.ReportProgress(key, amount);
#endif
        }

        private static void ReportAchievementProgressStatic(string key, int amount)
        {
#if SWEF_ACHIEVEMENT_AVAILABLE
            var ach = SWEF.Achievement.AchievementManager.Instance;
            ach?.ReportProgress(key, amount);
#endif
        }
    }
}
