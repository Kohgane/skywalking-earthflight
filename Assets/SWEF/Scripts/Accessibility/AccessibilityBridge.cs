// AccessibilityBridge.cs — SWEF Accessibility & Platform Optimization (Phase 93)
using UnityEngine;

namespace SWEF.Accessibility
{
    /// <summary>
    /// Static integration bridge that connects <see cref="AccessibilityManager"/> with
    /// the rest of the SWEF system graph.
    ///
    /// <para>All cross-system calls are guarded by compile-time symbols so the class
    /// compiles cleanly even when sibling systems are absent.</para>
    ///
    /// <list type="bullet">
    ///   <item><c>SWEF_COCKPITHUD_AVAILABLE</c> — CockpitHUD HUD-scale hook</item>
    ///   <item><c>SWEF_MUSIC_AVAILABLE</c> — volume-channel handshake</item>
    ///   <item><c>SWEF_VOICECOMMAND_AVAILABLE</c> — subtitle feed for voice assistant</item>
    ///   <item><c>SWEF_ACHIEVEMENT_AVAILABLE</c> — accessibility achievement reporting</item>
    ///   <item><c>SWEF_ANALYTICS_AVAILABLE</c> — telemetry via TelemetryDispatcher</item>
    /// </list>
    /// </summary>
    public static class AccessibilityBridge
    {
        // ── Called by AccessibilityManager after profile load/change ──────────────

        /// <summary>
        /// Broadcasts the current accessibility profile to all dependent systems.
        /// Call after <see cref="AccessibilityManager"/> applies a new profile.
        /// </summary>
        public static void NotifyProfileChanged(AccessibilityProfile profile)
        {
            if (profile == null) return;

            SyncCockpitHUD(profile);
            SyncMusicVolume(profile);
            SyncSubtitles(profile);
            SyncAchievements(profile);
            AccessibilityAnalytics.RecordProfileApplied(profile.profileName);
        }

        // ── CockpitHUD ────────────────────────────────────────────────────────────

        private static void SyncCockpitHUD(AccessibilityProfile profile)
        {
#if SWEF_COCKPITHUD_AVAILABLE
            var hud = SWEF.CockpitHUD.HUDController.Instance;
            if (hud != null)
                hud.SetScale(profile.hudScale);
#endif
            if (HUDScaleController.Instance != null)
                HUDScaleController.Instance.ApplyProfile(profile);
        }

        // ── Music / Audio ─────────────────────────────────────────────────────────

        private static void SyncMusicVolume(AccessibilityProfile profile)
        {
            if (AudioAccessibilityController.Instance != null)
                AudioAccessibilityController.Instance.SetMasterVolume(1f); // profiles don't carry volume yet — placeholder
        }

        // ── Subtitles ─────────────────────────────────────────────────────────────

        private static void SyncSubtitles(AccessibilityProfile profile)
        {
            if (SubtitleController.Instance != null)
                SubtitleController.Instance.ApplyProfile(profile);
        }

        // ── Achievements ──────────────────────────────────────────────────────────

        private static void SyncAchievements(AccessibilityProfile profile)
        {
#if SWEF_ACHIEVEMENT_AVAILABLE
            // Report that the player has engaged accessibility features
            if (profile.subtitleEnabled || profile.screenReaderEnabled ||
                profile.colorBlindMode != ColorBlindMode.None || profile.highContrastUI)
            {
                SWEF.Achievement.AchievementManager.ReportProgress("accessibility_enabled", 1);
            }
#endif
        }

        // ── Voice command → subtitle feed ────────────────────────────────────────

        /// <summary>
        /// Feeds a recognized voice-command transcript to the subtitle system.
        /// Call from VoiceRecognitionController after a successful recognition.
        /// </summary>
        public static void FeedVoiceSubtitle(string transcript)
        {
            if (SubtitleController.Instance != null)
                SubtitleController.Instance.ShowSubtitle("Voice", transcript, 2f);
        }

        // ── Security save/load hooks ───────────────────────────────────────────────

        /// <summary>
        /// Invoked by the save system after writing accessibility settings.
        /// Validates the file if <c>SWEF_SECURITY_AVAILABLE</c>.
        /// </summary>
        public static void OnSettingsSaved(string filePath)
        {
#if SWEF_SECURITY_AVAILABLE
            SWEF.Security.SaveFileValidator.VerifySaveFile(filePath);
#endif
            Debug.Log($"[SWEF] Accessibility: Settings saved → {filePath}");
        }
    }
}
