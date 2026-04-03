// SecurityAnalytics.cs — SWEF Anti-Cheat & Security Hardening (Phase 92)
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Security
{
    /// <summary>
    /// Static helper that wraps all Security telemetry events and forwards them
    /// to <c>SWEF.Analytics.TelemetryDispatcher.EnqueueEvent</c>.
    ///
    /// <para>All calls are guarded by <c>#if SWEF_ANALYTICS_AVAILABLE</c> so the
    /// class compiles cleanly even when the Analytics module is absent.</para>
    ///
    /// <para>Event types dispatched:</para>
    /// <list type="bullet">
    ///   <item><c>cheat_detected</c></item>
    ///   <item><c>save_tamper_detected</c></item>
    ///   <item><c>player_kicked</c></item>
    ///   <item><c>player_banned</c></item>
    ///   <item><c>rate_limit_hit</c></item>
    ///   <item><c>packet_invalid</c></item>
    ///   <item><c>save_integrity_check</c></item>
    ///   <item><c>backup_restored</c></item>
    /// </list>
    /// </summary>
    public static class SecurityAnalytics
    {
        /// <summary>Records that a cheat was detected for a player.</summary>
        /// <param name="cheatType">Type of cheat (e.g. "SpeedHack").</param>
        /// <param name="playerId">Affected player ID.</param>
        /// <param name="severity">Severity tier label.</param>
        public static void RecordCheatDetected(string cheatType, string playerId, string severity)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("cheat_detected",
                new Dictionary<string, object>
                {
                    { "cheat_type", cheatType },
                    { "player_id",  playerId  },
                    { "severity",   severity  }
                });
#endif
        }

        /// <summary>Records that save-file tampering was detected.</summary>
        /// <param name="filePath">Path of the affected file.</param>
        public static void RecordSaveTamperDetected(string filePath)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("save_tamper_detected",
                new Dictionary<string, object>
                {
                    { "file_path", filePath }
                });
#endif
        }

        /// <summary>Records that a player was kicked from a session.</summary>
        /// <param name="playerId">Kicked player ID.</param>
        /// <param name="reason">Kick reason.</param>
        public static void RecordPlayerKicked(string playerId, string reason)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("player_kicked",
                new Dictionary<string, object>
                {
                    { "player_id", playerId },
                    { "reason",    reason   }
                });
#endif
        }

        /// <summary>Records that a player was banned.</summary>
        /// <param name="playerId">Banned player ID.</param>
        /// <param name="durationMinutes">Ban duration in minutes (0 = permanent).</param>
        /// <param name="reason">Ban reason.</param>
        public static void RecordPlayerBanned(string playerId, int durationMinutes, string reason)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("player_banned",
                new Dictionary<string, object>
                {
                    { "player_id",         playerId        },
                    { "duration_minutes",  durationMinutes },
                    { "reason",            reason          }
                });
#endif
        }

        /// <summary>Records that a player hit a rate limit.</summary>
        /// <param name="playerId">Rate-limited player ID.</param>
        /// <param name="actionKey">Action that was rate-limited.</param>
        public static void RecordRateLimitHit(string playerId, string actionKey)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("rate_limit_hit",
                new Dictionary<string, object>
                {
                    { "player_id",  playerId  },
                    { "action_key", actionKey }
                });
#endif
        }

        /// <summary>Records that an invalid packet was received.</summary>
        /// <param name="senderId">Sender's player ID.</param>
        /// <param name="reason">Reason the packet was rejected.</param>
        public static void RecordPacketInvalid(string senderId, string reason)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("packet_invalid",
                new Dictionary<string, object>
                {
                    { "sender_id", senderId },
                    { "reason",    reason   }
                });
#endif
        }

        /// <summary>Records the result of a periodic save-integrity check.</summary>
        /// <param name="filePath">Path of the checked file.</param>
        /// <param name="passed">Whether the check passed.</param>
        public static void RecordSaveIntegrityCheck(string filePath, bool passed)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("save_integrity_check",
                new Dictionary<string, object>
                {
                    { "file_path", filePath },
                    { "passed",    passed   }
                });
#endif
        }

        /// <summary>Records that a save-file backup was restored.</summary>
        /// <param name="filePath">Path of the restored file.</param>
        public static void RecordBackupRestored(string filePath)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent("backup_restored",
                new Dictionary<string, object>
                {
                    { "file_path", filePath }
                });
#endif
        }
    }
}
