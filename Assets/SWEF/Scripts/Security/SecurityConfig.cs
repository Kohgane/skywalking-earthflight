// SecurityConfig.cs — SWEF Anti-Cheat & Security Hardening (Phase 92)
using System;
using UnityEngine;

namespace SWEF.Security
{
    /// <summary>
    /// Hash algorithm used for save-file checksums.
    /// </summary>
    public enum HashAlgorithmChoice
    {
        /// <summary>SHA-256 (default, recommended).</summary>
        SHA256,
        /// <summary>SHA-512 (higher security, larger signatures).</summary>
        SHA512
    }

    /// <summary>
    /// Serializable configuration asset for the entire Security system.
    /// Expose via <see cref="CheatDetectionManager"/> inspector or load from Resources.
    /// </summary>
    [Serializable]
    public class SecurityConfig
    {
        // ── Speed / Physics ────────────────────────────────────────────────────

        [Header("Speed Detection")]
        [Tooltip("Maximum permitted aircraft speed in m/s before a speed-hack flag is raised.")]
        public float maxSpeedThreshold = 3000f;

        [Tooltip("Tolerance multiplier applied to the aircraft's own max speed (e.g. 1.5 = 50 % margin).")]
        public float speedToleranceMultiplier = 1.5f;

        [Tooltip("Ratio of Time.deltaTime / Time.unscaledDeltaTime outside which time-scale manipulation is flagged.")]
        public float timeScaleAnomalyThreshold = 0.05f;

        // ── Position / Teleport ────────────────────────────────────────────────

        [Header("Teleport Detection")]
        [Tooltip("Maximum allowed position delta per physics tick (metres). Jumps beyond this are flagged.")]
        public float maxTeleportDistancePerTick = 5000f;

        [Tooltip("Tolerance multiplier: maxVelocity × deltaTime × this factor = allowed jump.")]
        public float teleportToleranceMultiplier = 2.0f;

        [Tooltip("Number of recent frames kept in the position history ring buffer.")]
        public int positionHistoryFrames = 60;

        // ── Economy ───────────────────────────────────────────────────────────

        [Header("Economy Limits")]
        [Tooltip("Maximum XP a player can legitimately earn per minute.")]
        public float maxXpGainPerMinute = 5000f;

        [Tooltip("Maximum in-game currency a player can legitimately earn per minute.")]
        public float maxCurrencyGainPerMinute = 10000f;

        // ── Rate Limiting ─────────────────────────────────────────────────────

        [Header("Rate Limiting")]
        [Tooltip("Maximum chat messages per second before rate limiting kicks in.")]
        public int chatRateLimitPerSecond = 5;

        [Tooltip("Maximum position update packets per second per player.")]
        public int positionRateLimitPerSecond = 30;

        [Tooltip("Maximum generic action packets per second per player.")]
        public int actionRateLimitPerSecond = 10;

        [Tooltip("Sliding window duration in seconds for rate-limit counters.")]
        public float rateLimitWindowSeconds = 1f;

        // ── Save Integrity ────────────────────────────────────────────────────

        [Header("Save File Integrity")]
        [Tooltip("Hash algorithm used to sign save files.")]
        public HashAlgorithmChoice hashAlgorithm = HashAlgorithmChoice.SHA256;

        [Tooltip("When true every save is automatically backed up before writing.")]
        public bool autoBackupOnSave = true;

        [Tooltip("Number of rolling backups to keep per save file.")]
        public int maxBackupsPerFile = 3;

        // ── Ban Tiers ─────────────────────────────────────────────────────────

        [Header("Ban Duration Tiers (minutes)")]
        [Tooltip("Duration of a first-offence ban in minutes.")]
        public int banTier1Minutes = 60;

        [Tooltip("Duration of a second-offence ban in minutes.")]
        public int banTier2Minutes = 1440;    // 24 h

        [Tooltip("Duration of a third-offence ban in minutes (0 = permanent).")]
        public int banTier3Minutes = 0;       // permanent

        // ── Periodic Checks ───────────────────────────────────────────────────

        [Header("Periodic Integrity Checks")]
        [Tooltip("How often (seconds) the CheatDetectionManager runs a full integrity sweep.")]
        public float integrityCheckIntervalSeconds = 30f;

        // ── Defaults ──────────────────────────────────────────────────────────

        /// <summary>Returns a <see cref="SecurityConfig"/> with reasonable defaults.</summary>
        public static SecurityConfig Default() => new SecurityConfig();
    }
}
