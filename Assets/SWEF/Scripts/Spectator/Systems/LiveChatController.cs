// LiveChatController.cs — SWEF Phase 107: Live Streaming & Spectator Mode
using System;
using System.Collections.Generic;
using UnityEngine;

#if SWEF_SECURITY_AVAILABLE
using SWEF.Security;
#endif

namespace SWEF.Spectator
{
    /// <summary>
    /// Phase 107 — Manages the live-chat overlay displayed during stream sessions.
    ///
    /// <para>Responsibilities:</para>
    /// <list type="bullet">
    ///   <item>Accept incoming chat messages from a platform bridge.</item>
    ///   <item>Apply per-viewer rate limiting (<see cref="SpectatorConfig.chatRateLimitSeconds"/>).</item>
    ///   <item>Filter messages through the profanity filter when
    ///     <c>SWEF_SECURITY_AVAILABLE</c> is defined.</item>
    ///   <item>Parse and dispatch <see cref="ChatCommandType"/> commands.</item>
    ///   <item>Maintain a bounded display queue for the chat overlay.</item>
    /// </list>
    /// </summary>
    public sealed class LiveChatController : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static LiveChatController Instance { get; private set; }

        // ── Inspector ──────────────────────────────────────────────────────────
        [SerializeField] private SpectatorConfig config;

        /// <summary>Where the chat overlay panel is anchored.</summary>
        [SerializeField] private ChatOverlayPosition overlayPosition = ChatOverlayPosition.BottomLeft;

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Raised when a message passes validation and is added to the queue.</summary>
        public event Action<ChatMessage> OnMessageReceived;

        /// <summary>Raised when a viewer sends a chat command.</summary>
        public event Action<ChatCommand> OnCommandReceived;

        // ── Public state ───────────────────────────────────────────────────────

        /// <summary>Read-only view of the current display message queue.</summary>
        public IReadOnlyList<ChatMessage> Messages => _messages.AsReadOnly();

        /// <summary>Current overlay anchor position.</summary>
        public ChatOverlayPosition OverlayPosition
        {
            get => overlayPosition;
            set => overlayPosition = value;
        }

        // ── Internal state ─────────────────────────────────────────────────────
        private readonly List<ChatMessage> _messages = new List<ChatMessage>();

        // Maps viewerId → game time of last accepted message
        private readonly Dictionary<string, float> _rateLimitMap =
            new Dictionary<string, float>(StringComparer.Ordinal);

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (config == null) return;
            PruneExpiredMessages();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to submit a new <paramref name="viewerMessage"/> from
        /// <paramref name="viewerId"/>. Applies rate limiting and profanity
        /// filtering before enqueuing. Returns <c>true</c> if accepted.
        /// </summary>
        public bool SubmitMessage(string viewerId, string viewerName, string viewerMessage)
        {
            if (config == null) return false;
            if (string.IsNullOrWhiteSpace(viewerId) || string.IsNullOrWhiteSpace(viewerMessage))
                return false;

            // Rate limiting
            if (!CheckRateLimit(viewerId)) return false;

            // Profanity filter
            string filtered = FilterMessage(viewerMessage);

            // Check for chat command
            if (TryParseCommand(viewerId, viewerName, filtered, out ChatCommand cmd))
            {
                OnCommandReceived?.Invoke(cmd);
                // Commands still appear in chat
            }

            var msg = new ChatMessage
            {
                viewerId   = viewerId,
                viewerName = viewerName,
                text       = filtered,
                timestamp  = Time.time,
                isCommand  = cmd != null,
            };

            EnqueueMessage(msg);
            return true;
        }

        /// <summary>Clears all messages from the display queue.</summary>
        public void ClearChat() => _messages.Clear();

        // ── Private helpers ────────────────────────────────────────────────────

        private bool CheckRateLimit(string viewerId)
        {
            float now = Time.time;
            if (_rateLimitMap.TryGetValue(viewerId, out float last) &&
                now - last < config.chatRateLimitSeconds)
                return false;

            _rateLimitMap[viewerId] = now;
            return true;
        }

        private string FilterMessage(string text)
        {
#if SWEF_SECURITY_AVAILABLE
            return ProfanityFilter.Sanitise(text);
#else
            return text;
#endif
        }

        private bool TryParseCommand(string viewerId, string viewerName, string text,
                                     out ChatCommand cmd)
        {
            cmd = null;
            if (string.IsNullOrWhiteSpace(text) || !text.StartsWith("!", StringComparison.Ordinal))
                return false;

            string[] parts = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string keyword = parts[0].ToLowerInvariant();

            ChatCommandType type;
            switch (keyword)
            {
                case "!camera": type = ChatCommandType.Camera; break;
                case "!follow": type = ChatCommandType.Follow; break;
                case "!stats":  type = ChatCommandType.Stats;  break;
                default:        type = ChatCommandType.Unknown; break;
            }

            cmd = new ChatCommand
            {
                viewerId  = viewerId,
                viewerName = viewerName,
                type      = type,
                args      = parts.Length > 1 ? parts[1..] : Array.Empty<string>(),
                timestamp = Time.time,
            };
            return true;
        }

        private void EnqueueMessage(ChatMessage msg)
        {
            _messages.Add(msg);
            while (config != null && _messages.Count > config.chatMaxMessages)
                _messages.RemoveAt(0);

            OnMessageReceived?.Invoke(msg);
        }

        private void PruneExpiredMessages()
        {
            float now = Time.time;
            _messages.RemoveAll(m => now - m.timestamp > config.chatMessageLifetime);
        }
    }

    // ── Supporting types ───────────────────────────────────────────────────────

    /// <summary>A single live-chat message.</summary>
    [Serializable]
    public sealed class ChatMessage
    {
        /// <summary>Platform viewer ID.</summary>
        public string viewerId;
        /// <summary>Display name of the viewer.</summary>
        public string viewerName;
        /// <summary>Filtered message text.</summary>
        public string text;
        /// <summary>Game time when the message was accepted.</summary>
        public float timestamp;
        /// <summary>Whether this message was parsed as a chat command.</summary>
        public bool isCommand;
    }

    /// <summary>A parsed chat command submitted by a viewer.</summary>
    [Serializable]
    public sealed class ChatCommand
    {
        /// <summary>Platform viewer ID who issued the command.</summary>
        public string viewerId;
        /// <summary>Display name of the viewer.</summary>
        public string viewerName;
        /// <summary>Parsed command type.</summary>
        public ChatCommandType type;
        /// <summary>Arguments following the command keyword.</summary>
        public string[] args;
        /// <summary>Game time when the command was parsed.</summary>
        public float timestamp;
    }

    /// <summary>Screen anchor positions for the chat overlay panel.</summary>
    public enum ChatOverlayPosition
    {
        BottomLeft,
        BottomRight,
        TopLeft,
        TopRight,
    }
}
