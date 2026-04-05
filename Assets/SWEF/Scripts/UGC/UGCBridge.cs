// UGCBridge.cs — SWEF Phase 108: User-Generated Content (UGC) Editor
using UnityEngine;

namespace SWEF.UGC
{
    /// <summary>
    /// Phase 108 — Wires UGC events to other SWEF systems (Progression, Achievement,
    /// Social, Mission).  All cross-system calls are guarded by <c>#if SWEF_*_AVAILABLE</c>
    /// compile symbols so the UGC module compiles cleanly without any dependency.
    ///
    /// <para>Attach to a persistent scene object alongside other singleton managers.</para>
    /// </summary>
    public sealed class UGCBridge : MonoBehaviour
    {
        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        // ── Private setup ──────────────────────────────────────────────────────

        private void SubscribeEvents()
        {
            if (UGCEditorManager.Instance != null)
            {
                UGCEditorManager.Instance.OnProjectCreated += OnProjectCreated;
            }

            if (UGCPublishManager.Instance != null)
            {
                UGCPublishManager.Instance.OnContentPublished    += OnContentPublished;
                UGCPublishManager.Instance.OnContentDownloaded   += OnContentDownloaded;
                UGCPublishManager.Instance.OnContentInstalled    += OnContentInstalled;
            }
        }

        private void UnsubscribeEvents()
        {
            if (UGCEditorManager.Instance != null)
            {
                UGCEditorManager.Instance.OnProjectCreated -= OnProjectCreated;
            }

            if (UGCPublishManager.Instance != null)
            {
                UGCPublishManager.Instance.OnContentPublished    -= OnContentPublished;
                UGCPublishManager.Instance.OnContentDownloaded   -= OnContentDownloaded;
                UGCPublishManager.Instance.OnContentInstalled    -= OnContentInstalled;
            }
        }

        // ── Event handlers ─────────────────────────────────────────────────────

        private void OnProjectCreated(UGCContent content)
        {
#if SWEF_ACHIEVEMENT_AVAILABLE
            SWEF.Achievement.AchievementManager.Instance?.UnlockAchievement("first_ugc_created");
#endif

#if SWEF_ANALYTICS_AVAILABLE
            UGCAnalytics.TrackEditorOpened();
            UGCAnalytics.TrackProjectCreated(content.contentType.ToString());
#endif
        }

        private void OnContentPublished(UGCContent content)
        {
#if SWEF_PROGRESSION_AVAILABLE
            SWEF.Progression.ProgressionManager.Instance?.AddXP(500, "ugc_published");
#endif

#if SWEF_ACHIEVEMENT_AVAILABLE
            SWEF.Achievement.AchievementManager.Instance?.UnlockAchievement("ugc_published");
            if (content.status == UGCStatus.Featured)
                SWEF.Achievement.AchievementManager.Instance?.UnlockAchievement("ugc_featured");
            if (content.downloadCount >= 100)
                SWEF.Achievement.AchievementManager.Instance?.UnlockAchievement("ugc_100_downloads");
#endif

#if SWEF_SOCIAL_AVAILABLE
            SWEF.Social.SocialFeedManager.Instance?.PostActivity(
                "ugc_published",
                $"Published new UGC: {content.title}",
                content.contentId);
#endif

#if SWEF_ANALYTICS_AVAILABLE
            UGCAnalytics.TrackContentPublished(content.contentId, content.contentType.ToString());
#endif
        }

        private void OnContentDownloaded(UGCContent content)
        {
#if SWEF_ANALYTICS_AVAILABLE
            UGCAnalytics.TrackContentDownloaded(content.contentId);
#endif
        }

        private void OnContentInstalled(UGCContent content)
        {
#if SWEF_MISSION_AVAILABLE
            // Register UGC content as a playable custom mission
            SWEF.Mission.MissionManager.Instance?.RegisterCustomMission(content.contentId, content.title);
#endif
        }

        /// <summary>
        /// Called by the UGC playback system when a player completes a community content experience.
        /// Grants XP and fires achievements.
        /// </summary>
        public void NotifyContentCompleted(string contentId, float completionTimeSeconds)
        {
#if SWEF_PROGRESSION_AVAILABLE
            SWEF.Progression.ProgressionManager.Instance?.AddXP(100, "ugc_content_completed");
#endif

#if SWEF_ACHIEVEMENT_AVAILABLE
            SWEF.Achievement.AchievementManager.Instance?.RecordProgress("ugc_content_played", 1);
#endif

#if SWEF_ANALYTICS_AVAILABLE
            UGCAnalytics.TrackContentPlayed(contentId, completionTimeSeconds);
#endif
        }

        /// <summary>
        /// Called when the creator receives a 5-star review.
        /// </summary>
        public void NotifyFiveStarReview(string contentId)
        {
#if SWEF_ACHIEVEMENT_AVAILABLE
            SWEF.Achievement.AchievementManager.Instance?.UnlockAchievement("ugc_5star_creator");
#endif
        }
    }
}
