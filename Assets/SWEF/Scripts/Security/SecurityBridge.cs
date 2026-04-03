// SecurityBridge.cs — SWEF Anti-Cheat & Security Hardening (Phase 92)
using UnityEngine;

namespace SWEF.Security
{
    /// <summary>
    /// Static bridge that connects the Security system to the wider SWEF ecosystem:
    /// <see cref="SWEF.Multiplayer.MultiplayerSessionManager"/>,
    /// <see cref="SWEF.Achievement.AchievementManager"/>,
    /// <see cref="SWEF.SocialHub.SocialActivityFeed"/>, and
    /// <see cref="SWEF.Analytics.TelemetryDispatcher"/>.
    ///
    /// <para>All integration calls are guarded by compile-time symbols so the class
    /// compiles cleanly even when dependent modules are absent.</para>
    /// </summary>
    public static class SecurityBridge
    {
        // ── Achievement keys ─────────────────────────────────────────────────

        /// <summary>Achievement awarded after 1 000 flights with no security violations.</summary>
        private const string AchievCleanRecord = "clean_record";

        // ── Save / Load hooks ─────────────────────────────────────────────────

        /// <summary>
        /// Must be called before every JSON save to create a backup and sign the file.
        /// Wrap all persistence write paths with this method.
        /// </summary>
        /// <param name="path">Absolute path of the file about to be written.</param>
        public static void OnBeforeSave(string path)
        {
            SaveFileValidator.CreateBackup(path);
        }

        /// <summary>
        /// Must be called after every JSON save to append the HMAC signature.
        /// </summary>
        /// <param name="path">Absolute path of the file that was just written.</param>
        public static void OnAfterSave(string path)
        {
            SaveFileValidator.SignSaveFile(path);
        }

        /// <summary>
        /// Must be called before any save-file load.
        /// If tampering is detected, restores the backup and returns <c>false</c>
        /// to signal that the caller should reload.
        /// </summary>
        /// <param name="path">Absolute path of the file about to be loaded.</param>
        /// <returns><c>true</c> if the file is intact; <c>false</c> if a restore was performed.</returns>
        public static bool OnBeforeLoad(string path)
        {
            var result = SaveFileValidator.DetectTampering(path);
            if (result.isValid) return true;

            SecurityAnalytics.RecordSaveTamperDetected(path);

            var evt = SecurityEventData.Create(
                SecurityEventType.SaveTamper,
                SecuritySeverity.Critical,
                "local",
                $"Tampered save detected on load: {System.IO.Path.GetFileName(path)} — " +
                string.Join("; ", result.violations),
                SecurityAction.Reverted);

            SecurityLogger.Instance?.LogEvent(evt);

#if SWEF_SOCIAL_AVAILABLE
            SWEF.SocialHub.SocialActivityFeed.Instance?.PostActivity(
                "security_save_restored",
                "A save file was restored from backup after tampering was detected.");
#endif

            bool restored = SaveFileValidator.RestoreFromBackup(path);
            if (restored)
                SecurityAnalytics.RecordBackupRestored(path);

            return false; // Caller should re-read the restored file
        }

        // ── Multiplayer integration ───────────────────────────────────────────

        /// <summary>
        /// Kicks a player from the current multiplayer session via
        /// <see cref="MultiplayerSecurityController"/> and (if available)
        /// <see cref="SWEF.Multiplayer.MultiplayerSessionManager"/>.
        /// </summary>
        /// <param name="playerId">Player to kick.</param>
        /// <param name="reason">Human-readable reason.</param>
        public static void KickPlayer(string playerId, string reason)
        {
            MultiplayerSecurityController.Instance?.KickPlayer(playerId, reason);

            PostAdminActivity("security_player_kicked",
                $"Player {playerId} was kicked: {reason}");
        }

        /// <summary>
        /// Bans a player via <see cref="MultiplayerSecurityController"/>.
        /// </summary>
        /// <param name="playerId">Player to ban.</param>
        /// <param name="durationMinutes">Ban duration (0 = permanent).</param>
        /// <param name="reason">Human-readable reason.</param>
        public static void BanPlayer(string playerId, int durationMinutes, string reason)
        {
            MultiplayerSecurityController.Instance?.BanPlayer(playerId, durationMinutes, reason);

            PostAdminActivity("security_player_banned",
                $"Player {playerId} was banned for {durationMinutes} min: {reason}");
        }

        // ── Achievement integration ───────────────────────────────────────────

        /// <summary>
        /// Reports progress towards the <c>clean_record</c> achievement
        /// (1 000 flights with no violations).
        /// </summary>
        /// <param name="flightsCompleted">Number of clean flights completed.</param>
        public static void ReportCleanFlight(int flightsCompleted)
        {
#if SWEF_ACHIEVEMENT_AVAILABLE
            SWEF.Achievement.AchievementManager.Instance?
                .ReportProgress(AchievCleanRecord, flightsCompleted);
#endif
        }

        // ── Workshop validation hook ──────────────────────────────────────────

        /// <summary>
        /// Called when a Workshop build is about to be loaded.
        /// Validates the build data and raises a violation on duplication anomalies.
        /// </summary>
        /// <param name="build">Build data to validate.</param>
        /// <returns><c>true</c> if the build is valid.</returns>
        public static bool ValidateWorkshopBuild(
#if SWEF_WORKSHOP_AVAILABLE
            SWEF.Workshop.AircraftBuildData build
#else
            object build
#endif
        )
        {
            var result = InputSanitizer.ValidateBuildData(build);
            if (!result.isValid)
            {
                var evt = SecurityEventData.Create(
                    SecurityEventType.PartDuplication,
                    SecuritySeverity.Medium,
                    "local",
                    "Build validation failed: " + string.Join("; ", result.violations),
                    SecurityAction.Logged);

                SecurityLogger.Instance?.LogEvent(evt);
                CheatDetectionManager.Instance?.OnCheatDetected?.Invoke(evt);
            }
            return result.isValid;
        }

        // ── ProgressionManager hook ───────────────────────────────────────────

        /// <summary>
        /// Must be called from <c>ProgressionManager.AddXP</c> (or its wrapper) so
        /// the cheat-detection manager can monitor XP gain rate.
        /// </summary>
        /// <param name="amount">XP amount being added.</param>
        public static void OnXpGain(float amount)
        {
            CheatDetectionManager.Instance?.ReportXpGain(amount);
        }

        /// <summary>
        /// Must be called from <c>ProgressionManager.AddCurrency</c> (or equivalent).
        /// Returns the validated (possibly corrected) amount.
        /// </summary>
        /// <param name="amount">Currency delta.</param>
        /// <param name="source">Transaction source.</param>
        /// <returns>Validated amount to apply.</returns>
        public static float OnCurrencyChange(float amount, string source)
        {
            if (CheatDetectionManager.Instance != null)
                return CheatDetectionManager.Instance.ValidateCurrencyChange(amount, source);
            return amount;
        }

        // ── FriendSystemController hook ───────────────────────────────────────

        /// <summary>
        /// Sanitizes a player-supplied display name before it is stored or displayed.
        /// </summary>
        /// <param name="rawName">Raw display name.</param>
        /// <returns>Sanitized display name.</returns>
        public static string SanitizeDisplayName(string rawName)
        {
            return InputSanitizer.SanitizeDisplayName(rawName);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static void PostAdminActivity(string activityType, string detail)
        {
#if SWEF_SOCIAL_AVAILABLE
            SWEF.SocialHub.SocialActivityFeed.Instance?.PostActivity(activityType, detail);
#endif
        }
    }
}
