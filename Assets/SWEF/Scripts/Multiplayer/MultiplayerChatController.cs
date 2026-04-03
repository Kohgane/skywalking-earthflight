using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.Multiplayer
{
    /// <summary>
    /// MonoBehaviour that handles in-session communication: text chat, emotes,
    /// location pings, and system alerts.
    /// Chat history (last 100 messages) is persisted to <c>chat_history.json</c>.
    /// </summary>
    public class MultiplayerChatController : MonoBehaviour
    {
        #region Constants
        private const string PersistenceFileName = "chat_history.json";
        private const int MaxChatHistory = 100;
        #endregion

        #region Inspector
        [Header("Chat Settings")]
        [SerializeField, Tooltip("Maximum characters allowed in a single chat message.")]
        private int maxMessageLength = 256;

        [SerializeField, Tooltip("Enable placeholder profanity filter (replaces bad words with **).")]
        private bool enableProfanityFilter = false;
        #endregion

        #region Events
        /// <summary>Fired when any new message is received (including local outgoing messages).</summary>
        public event Action<MultiplayerMessageData> OnMessageReceived;
        #endregion

        #region Public Properties
        /// <summary>All messages in the current chat history (read-only snapshot).</summary>
        public IReadOnlyList<MultiplayerMessageData> ChatHistory => _chatHistory.AsReadOnly();
        #endregion

        #region Private State
        private readonly List<MultiplayerMessageData> _chatHistory = new List<MultiplayerMessageData>();
        private string _persistencePath;

        // Predefined pilot emotes
        private static readonly string[] ValidEmotes =
        {
            "wave", "salute", "barrel_roll", "thumbs_up",
            "thumbs_down", "shrug", "point", "heart"
        };
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _persistencePath = Path.Combine(Application.persistentDataPath, PersistenceFileName);
            LoadChatHistory();
        }
        #endregion

        #region Chat
        /// <summary>
        /// Sends a text chat message from the local player.
        /// Applies the profanity filter if enabled.
        /// </summary>
        /// <param name="text">Message text to send.</param>
        public void SendChatMessage(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogWarning("[SWEF] Multiplayer: SendChatMessage called with empty text.");
                return;
            }
            if (!MultiplayerSessionManager.Instance || !MultiplayerSessionManager.Instance.IsInSession)
            {
                Debug.LogWarning("[SWEF] Multiplayer: SendChatMessage — not in a session.");
                return;
            }

            string content = text.Length > maxMessageLength
                ? text.Substring(0, maxMessageLength)
                : text;

            if (enableProfanityFilter)
                content = ApplyProfanityFilter(content);

            string localId = PlayerProfileManager.Instance?.GetLocalProfile()?.playerId ?? "local";
            var msg = BuildMessage(localId, MessageType.Chat, content);

            AddMessage(msg);
            MultiplayerAnalytics.RecordChatMessageSent();
            OnMessageReceived?.Invoke(msg);
        }

        /// <summary>
        /// Sends a predefined pilot emote.
        /// </summary>
        /// <param name="emoteName">Emote identifier (e.g. "wave", "salute").</param>
        public void SendEmote(string emoteName)
        {
            if (string.IsNullOrEmpty(emoteName))
            {
                Debug.LogWarning("[SWEF] Multiplayer: SendEmote called with null/empty emote name.");
                return;
            }
            if (!IsValidEmote(emoteName))
            {
                Debug.LogWarning($"[SWEF] Multiplayer: Unknown emote '{emoteName}'.");
                return;
            }
            if (!MultiplayerSessionManager.Instance || !MultiplayerSessionManager.Instance.IsInSession)
            {
                Debug.LogWarning("[SWEF] Multiplayer: SendEmote — not in a session.");
                return;
            }

            string localId = PlayerProfileManager.Instance?.GetLocalProfile()?.playerId ?? "local";
            var msg = BuildMessage(localId, MessageType.Emote, emoteName);

            AddMessage(msg);
            MultiplayerAnalytics.RecordEmoteUsed(emoteName);
            OnMessageReceived?.Invoke(msg);
        }

        /// <summary>
        /// Sends a location ping, sharing the local player's current position as a temporary marker.
        /// </summary>
        public void SendLocationPing()
        {
            if (!MultiplayerSessionManager.Instance || !MultiplayerSessionManager.Instance.IsInSession)
            {
                Debug.LogWarning("[SWEF] Multiplayer: SendLocationPing — not in a session.");
                return;
            }

            var localProfile = PlayerProfileManager.Instance?.GetLocalProfile();
            if (localProfile == null) return;

            string pingContent = $"{localProfile.currentLatitude:F4},{localProfile.currentLongitude:F4},{localProfile.currentAltitude:F0}";
            var msg = BuildMessage(localProfile.playerId, MessageType.Ping, pingContent);

            AddMessage(msg);
            OnMessageReceived?.Invoke(msg);
        }

        /// <summary>
        /// Broadcasts a system alert message to all session participants.
        /// </summary>
        /// <param name="alertText">The alert text to broadcast.</param>
        public void SendSystemAlert(string alertText)
        {
            if (string.IsNullOrEmpty(alertText)) return;
            var msg = BuildMessage("system", MessageType.SystemAlert, alertText);
            AddMessage(msg);
            OnMessageReceived?.Invoke(msg);
        }

        /// <summary>
        /// Returns all available emote identifiers.
        /// </summary>
        public static IReadOnlyList<string> GetAvailableEmotes() =>
            Array.AsReadOnly(ValidEmotes);
        #endregion

        #region Profanity Filter (placeholder)
        private static string ApplyProfanityFilter(string text)
        {
            // Placeholder: real implementation would use a word list loaded from Resources.
            // Returns input unchanged; extend with actual filter logic before release.
            return text;
        }
        #endregion

        #region Helpers
        private static MultiplayerMessageData BuildMessage(string senderId, MessageType type, string content) =>
            new MultiplayerMessageData
            {
                messageId = Guid.NewGuid().ToString(),
                senderId = senderId,
                messageType = type,
                content = content,
                timestamp = DateTime.UtcNow.ToString("o"),
                targetId = string.Empty
            };

        private static bool IsValidEmote(string emote)
        {
            foreach (var e in ValidEmotes)
                if (e == emote) return true;
            return false;
        }

        private void AddMessage(MultiplayerMessageData msg)
        {
            _chatHistory.Add(msg);
            if (_chatHistory.Count > MaxChatHistory)
                _chatHistory.RemoveAt(0);
            SaveChatHistory();
        }
        #endregion

        #region Persistence
        private void SaveChatHistory()
        {
            try
            {
                var wrapper = new ChatHistoryWrapper { messages = _chatHistory };
                File.WriteAllText(_persistencePath, JsonUtility.ToJson(wrapper, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Multiplayer: Failed to save chat history — {ex.Message}");
            }
        }

        private void LoadChatHistory()
        {
            if (!File.Exists(_persistencePath)) return;
            try
            {
                string json = File.ReadAllText(_persistencePath);
                var wrapper = JsonUtility.FromJson<ChatHistoryWrapper>(json);
                if (wrapper?.messages != null)
                    _chatHistory.AddRange(wrapper.messages);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Multiplayer: Failed to load chat history — {ex.Message}");
            }
        }

        [Serializable]
        private class ChatHistoryWrapper
        {
            public List<MultiplayerMessageData> messages;
        }
        #endregion
    }
}
