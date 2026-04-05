// SquadronChatController.cs — Phase 109: Clan/Squadron System
// Squadron-only text chat: history, announcements, system messages, profanity filter.
// Namespace: SWEF.Squadron

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SWEF.Squadron
{
    /// <summary>
    /// Phase 109 — Controls the squadron chat channel.
    /// Handles text messages, officer/leader pinned announcements, mission briefing
    /// messages, auto-generated system messages, and message history persistence.
    /// Integrates with the profanity filter when <c>SWEF_SECURITY_AVAILABLE</c> is defined.
    /// </summary>
    public sealed class SquadronChatController : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        /// <summary>Global singleton instance.</summary>
        public static SquadronChatController Instance { get; private set; }

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Raised when a new message is added to the chat history.</summary>
        public event Action<SquadronChatMessage> OnMessageReceived;

        // ── State ──────────────────────────────────────────────────────────────

        private readonly List<SquadronChatMessage> _messages = new List<SquadronChatMessage>();

        /// <summary>Read-only view of the chat message history.</summary>
        public IReadOnlyList<SquadronChatMessage> Messages => _messages.AsReadOnly();

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadChat();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Sends a text message from the local player to the squadron chat.
        /// </summary>
        /// <param name="text">Message body (max <see cref="SquadronConfig.ChatMessageMaxLength"/> chars).</param>
        /// <param name="pin">If true the message is pinned as an announcement (Officer+ only).</param>
        /// <returns>The created <see cref="SquadronChatMessage"/>, or null if rejected.</returns>
        public SquadronChatMessage SendMessage(string text, bool pin = false)
        {
            var manager = SquadronManager.Instance;
            if (manager?.LocalMember == null)
            {
                Debug.LogWarning("[SquadronChatController] Not in a squadron.");
                return null;
            }

            if (string.IsNullOrWhiteSpace(text)) return null;

            if (text.Length > SquadronConfig.ChatMessageMaxLength)
                text = text.Substring(0, SquadronConfig.ChatMessageMaxLength);

            text = FilterProfanity(text);

            if (pin && manager.LocalMember.rank > SquadronRank.Officer)
            {
                Debug.LogWarning("[SquadronChatController] Only officers/leaders can pin messages.");
                pin = false;
            }

            return AddMessage(
                senderId    : manager.LocalMember.memberId,
                senderName  : manager.LocalMember.displayName,
                senderRank  : manager.LocalMember.rank,
                text        : text,
                isPinned    : pin,
                isSystem    : false);
        }

        /// <summary>
        /// Posts a system-generated message (member joined, promoted, etc.).
        /// </summary>
        public SquadronChatMessage PostSystemMessage(string text)
        {
            return AddMessage(
                senderId    : "system",
                senderName  : "System",
                senderRank  : SquadronRank.Leader,
                text        : text,
                isPinned    : false,
                isSystem    : true);
        }

        /// <summary>
        /// Posts a mission briefing message pinned to the chat.
        /// </summary>
        public SquadronChatMessage PostMissionBriefing(string missionTitle, string briefingText)
        {
            string text = $"[MISSION BRIEFING — {missionTitle}]\n{briefingText}";
            return AddMessage(
                senderId    : "mission_system",
                senderName  : "Mission Control",
                senderRank  : SquadronRank.Leader,
                text        : text,
                isPinned    : true,
                isSystem    : true);
        }

        /// <summary>Returns all pinned messages.</summary>
        public List<SquadronChatMessage> GetPinnedMessages()
            => _messages.Where(m => m.isPinned).ToList();

        /// <summary>Clears all pinned messages (Leader only).</summary>
        public bool ClearPinnedMessages()
        {
            var manager = SquadronManager.Instance;
            if (manager?.LocalMember?.rank != SquadronRank.Leader) return false;

            foreach (var m in _messages)
                m.isPinned = false;

            SaveChat();
            return true;
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private SquadronChatMessage AddMessage(
            string senderId, string senderName, SquadronRank senderRank,
            string text, bool isPinned, bool isSystem)
        {
            var message = new SquadronChatMessage
            {
                messageId  = Guid.NewGuid().ToString(),
                squadronId = SquadronManager.Instance?.CurrentSquadron?.squadronId ?? string.Empty,
                senderId   = senderId,
                senderName = senderName,
                senderRank = senderRank,
                text       = text,
                sentAt     = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                isPinned   = isPinned,
                isSystem   = isSystem
            };

            _messages.Add(message);

            // Trim history to max size (remove oldest non-pinned first)
            while (_messages.Count > SquadronConfig.ChatHistoryMax)
            {
                int oldestUnpinned = _messages.FindIndex(m => !m.isPinned);
                if (oldestUnpinned >= 0)
                    _messages.RemoveAt(oldestUnpinned);
                else
                    _messages.RemoveAt(0);
            }

            SaveChat();
            OnMessageReceived?.Invoke(message);
            return message;
        }

        private static string FilterProfanity(string text)
        {
#if SWEF_SECURITY_AVAILABLE
            return SWEF.Security.ProfanityFilter.Instance?.Filter(text) ?? text;
#else
            return text;
#endif
        }

        // ── Persistence ────────────────────────────────────────────────────────

        private void SaveChat()
        {
            try
            {
                var wrapper = new MessageListWrapper { messages = _messages };
                File.WriteAllText(
                    Path.Combine(Application.persistentDataPath, SquadronConfig.ChatDataFile),
                    JsonUtility.ToJson(wrapper, true));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SquadronChatController] Save error: {ex.Message}");
            }
        }

        private void LoadChat()
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, SquadronConfig.ChatDataFile);
                if (!File.Exists(path)) return;

                var wrapper = JsonUtility.FromJson<MessageListWrapper>(File.ReadAllText(path));
                if (wrapper?.messages == null) return;

                _messages.Clear();
                _messages.AddRange(wrapper.messages);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SquadronChatController] Load error: {ex.Message}");
            }
        }

        [Serializable]
        private class MessageListWrapper
        {
            public List<SquadronChatMessage> messages;
        }
    }
}
