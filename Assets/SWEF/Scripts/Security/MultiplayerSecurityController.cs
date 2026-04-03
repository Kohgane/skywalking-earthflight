// MultiplayerSecurityController.cs — SWEF Anti-Cheat & Security Hardening (Phase 92)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Security
{
    /// <summary>
    /// Singleton MonoBehaviour that enforces multiplayer security rules:
    /// packet validation, rate limiting, replay-attack prevention, and
    /// player state plausibility checks.
    ///
    /// <para>Integrates with <see cref="CheatDetectionManager"/> for event routing
    /// and with <see cref="SecurityLogger"/> for persistence.</para>
    /// </summary>
    public class MultiplayerSecurityController : MonoBehaviour
    {
        #region Singleton
        /// <summary>Shared instance.</summary>
        public static MultiplayerSecurityController Instance { get; private set; }
        #endregion

        #region Inspector
        [SerializeField, Tooltip("Reference to the shared SecurityConfig.")]
        private SecurityConfig _config;
        #endregion

        #region Events
        /// <summary>Fired when a player is kicked. Parameter: player ID.</summary>
        public event Action<string, string> OnPlayerKicked;
        /// <summary>Fired when a player is banned. Parameters: player ID, duration minutes.</summary>
        public event Action<string, int, string> OnPlayerBanned;
        #endregion

        #region Private state
        private SecurityConfig Config => _config ?? SecurityConfig.Default();

        // Per-player rate limiters (keyed by playerId)
        private readonly Dictionary<string, RateLimiter> _rateLimiters =
            new Dictionary<string, RateLimiter>();

        // Replay-attack prevention: per-player set of seen sequence numbers
        private readonly Dictionary<string, HashSet<long>> _seenSequenceNumbers =
            new Dictionary<string, HashSet<long>>();

        // Timestamp window for replay detection (seconds)
        private const float ReplayWindowSeconds = 30f;

        // Per-player last-known sequence number for ordering checks
        private readonly Dictionary<string, long> _lastSequence =
            new Dictionary<string, long>();

        // Ban records: playerId → ban expiry UTC (or DateTime.MaxValue for permanent)
        private readonly Dictionary<string, DateTime> _banExpiry =
            new Dictionary<string, DateTime>();

        // Violation counts per player (used to escalate ban tier)
        private readonly Dictionary<string, int> _violationCounts =
            new Dictionary<string, int>();
        #endregion

        #region Unity lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        #endregion

        #region Packet validation
        /// <summary>
        /// Validates an incoming network message.
        /// Checks format, sender identity, rate limit, and replay protection.
        /// </summary>
        /// <param name="senderId">Claimed sender player ID.</param>
        /// <param name="messageType">Message category (e.g. "chat", "position", "action").</param>
        /// <param name="sequenceNumber">Monotonic sequence number attached to the packet.</param>
        /// <param name="timestampUtc">UTC time embedded in the packet (seconds since epoch).</param>
        /// <param name="payload">Raw message payload (checked for null/empty).</param>
        /// <returns>Validation result; check <see cref="ValidationResult.isValid"/>.</returns>
        public ValidationResult ValidatePacket(
            string senderId,
            string messageType,
            long   sequenceNumber,
            double timestampUtc,
            string payload)
        {
            if (string.IsNullOrEmpty(senderId))
                return ValidationResult.Invalid("Packet has no sender ID.");

            if (IsBanned(senderId))
                return ValidationResult.Invalid($"Sender {senderId} is banned.");

            if (string.IsNullOrEmpty(messageType))
                return ValidationResult.Invalid("Packet has no message type.");

            if (string.IsNullOrEmpty(payload))
                return ValidationResult.Invalid("Packet has empty payload.");

            // Timestamp window check
            double now      = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            double ageSecs  = Math.Abs(now - timestampUtc);
            if (ageSecs > ReplayWindowSeconds)
                return ValidationResult.Invalid(
                    $"Packet timestamp too old or future: age={ageSecs:F1}s window={ReplayWindowSeconds}s");

            // Replay sequence check
            if (!_seenSequenceNumbers.TryGetValue(senderId, out var seenSet))
            {
                seenSet = new HashSet<long>();
                _seenSequenceNumbers[senderId] = seenSet;
            }
            if (seenSet.Contains(sequenceNumber))
                return ValidationResult.Invalid(
                    $"Replay attack: sequence {sequenceNumber} already seen from {senderId}.");
            seenSet.Add(sequenceNumber);

            // Rate limit check
            if (!CheckRateLimit(senderId, messageType))
                return ValidationResult.Invalid(
                    $"Rate limit exceeded for {senderId} action type '{messageType}'.");

            return ValidationResult.Valid();
        }
        #endregion

        #region Rate limiting
        /// <summary>
        /// Checks and records a rate-limited action for the specified player.
        /// </summary>
        /// <param name="playerId">Player ID.</param>
        /// <param name="actionType">Action category: "chat", "position", or "action".</param>
        /// <returns><c>true</c> if the action is within limits.</returns>
        public bool CheckRateLimit(string playerId, string actionType)
        {
            var limiter = GetOrCreateLimiter(playerId);
            string key  = $"{playerId}_{actionType}";

            int   maxRate;
            float window = Config.rateLimitWindowSeconds;

            switch (actionType?.ToLowerInvariant())
            {
                case "chat":     maxRate = Config.chatRateLimitPerSecond;     break;
                case "position": maxRate = Config.positionRateLimitPerSecond; break;
                default:         maxRate = Config.actionRateLimitPerSecond;   break;
            }

            bool allowed = limiter.IsAllowed(key, maxRate, window);
            if (!allowed)
                SecurityAnalytics.RecordRateLimitHit(playerId, actionType);

            return allowed;
        }
        #endregion

        #region Player state validation
        /// <summary>
        /// Validates a player's reported position and speed for plausibility.
        /// </summary>
        /// <param name="playerId">Player ID.</param>
        /// <param name="reportedPosition">World position reported by the client.</param>
        /// <param name="reportedSpeed">Speed in m/s reported by the client.</param>
        /// <returns>Validation result.</returns>
        public ValidationResult ValidatePlayerState(
            string  playerId,
            Vector3 reportedPosition,
            float   reportedSpeed)
        {
            if (reportedSpeed > Config.maxSpeedThreshold * Config.speedToleranceMultiplier)
                return ValidationResult.Invalid(
                    $"Player {playerId} reported impossible speed: {reportedSpeed:F1} m/s " +
                    $"(max allowed: {Config.maxSpeedThreshold * Config.speedToleranceMultiplier:F1})");

            // Altitude plausibility (y-axis in Unity world space)
            if (!InputSanitizer.ValidateAltitude(reportedPosition.y) &&
                reportedPosition.y < -10_000f)
                return ValidationResult.Invalid(
                    $"Player {playerId} reported implausible altitude: {reportedPosition.y:F1}");

            return ValidationResult.Valid();
        }
        #endregion

        #region Kick / Ban
        /// <summary>
        /// Removes a player from the current session.
        /// </summary>
        /// <param name="playerId">Player to kick.</param>
        /// <param name="reason">Human-readable reason.</param>
        public void KickPlayer(string playerId, string reason)
        {
            if (string.IsNullOrEmpty(playerId)) return;
            Debug.LogWarning($"[SWEF] Security: Kicking player {playerId} — {reason}");

            var evt = SecurityEventData.Create(
                SecurityEventType.InvalidPacket,
                SecuritySeverity.High,
                playerId,
                $"Kicked: {reason}",
                SecurityAction.Kicked);

            SecurityLogger.Instance?.LogEvent(evt);
            SecurityAnalytics.RecordPlayerKicked(playerId, reason);
            OnPlayerKicked?.Invoke(playerId, reason);

#if SWEF_MULTIPLAYER_AVAILABLE
            SWEF.Multiplayer.MultiplayerSessionManager.Instance?.KickParticipant(playerId, reason);
#endif
        }

        /// <summary>
        /// Bans a player for a duration determined by their violation tier.
        /// </summary>
        /// <param name="playerId">Player to ban.</param>
        /// <param name="durationMinutes">Ban duration in minutes (0 = permanent).</param>
        /// <param name="reason">Human-readable reason.</param>
        public void BanPlayer(string playerId, int durationMinutes, string reason)
        {
            if (string.IsNullOrEmpty(playerId)) return;
            Debug.LogWarning(
                $"[SWEF] Security: Banning player {playerId} for {durationMinutes} min — {reason}");

            _banExpiry[playerId] = durationMinutes <= 0
                ? DateTime.MaxValue
                : DateTime.UtcNow.AddMinutes(durationMinutes);

            var evt = SecurityEventData.Create(
                SecurityEventType.InvalidPacket,
                SecuritySeverity.Critical,
                playerId,
                $"Banned ({durationMinutes} min): {reason}",
                SecurityAction.Banned);

            SecurityLogger.Instance?.LogEvent(evt);
            SecurityAnalytics.RecordPlayerBanned(playerId, durationMinutes, reason);
            OnPlayerBanned?.Invoke(playerId, durationMinutes, reason);

            KickPlayer(playerId, $"Banned: {reason}");
        }

        /// <summary>Returns <c>true</c> if the player is currently under an active ban.</summary>
        /// <param name="playerId">Player ID to check.</param>
        public bool IsBanned(string playerId)
        {
            if (!_banExpiry.TryGetValue(playerId, out DateTime expiry)) return false;
            if (expiry == DateTime.MaxValue)         return true;
            if (DateTime.UtcNow < expiry)            return true;
            _banExpiry.Remove(playerId);             // Expired
            return false;
        }
        #endregion

        #region Private helpers
        private RateLimiter GetOrCreateLimiter(string playerId)
        {
            if (!_rateLimiters.TryGetValue(playerId, out var limiter))
            {
                limiter = new RateLimiter();
                _rateLimiters[playerId] = limiter;
            }
            return limiter;
        }
        #endregion
    }
}
