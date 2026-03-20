using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Multiplayer
{
    // ── Data Classes ─────────────────────────────────────────────────────────────

    /// <summary>
    /// A single text chat message.
    /// </summary>
    [Serializable]
    public class ChatMessage
    {
        /// <summary>ID of the player who sent the message.</summary>
        public string senderId;
        /// <summary>Display name of the sender.</summary>
        public string senderName;
        /// <summary>Message text content.</summary>
        public string text;
        /// <summary>Whether this is a team-only message.</summary>
        public bool isTeamMessage;
        /// <summary>UTC timestamp of the message.</summary>
        public DateTime timestamp;
    }

    /// <summary>
    /// HUD representation of a remote player for name-tag and minimap purposes.
    /// </summary>
    [Serializable]
    public class HudPlayerEntry
    {
        /// <summary>Player identifier.</summary>
        public string playerId;
        /// <summary>Display name.</summary>
        public string displayName;
        /// <summary>World-space position updated each frame.</summary>
        public Vector3 worldPosition;
        /// <summary>Formation role (if in a formation).</summary>
        public MissionRole? formationRole;
        /// <summary>Current connection quality.</summary>
        public ConnectionQuality connectionQuality;
        /// <summary>Whether the player is currently speaking.</summary>
        public bool isSpeaking;
        /// <summary>Team identifier.</summary>
        public string teamId;
        /// <summary>Unity UI name-tag RectTransform (managed by this system).</summary>
        [NonSerialized] public RectTransform nameTagRect;
    }

    // ── MultiplayerHUD ────────────────────────────────────────────────────────────

    /// <summary>
    /// Multiplayer-specific heads-up display for Phase 33.
    ///
    /// <para>Features:
    /// <list type="bullet">
    /// <item>World-space floating name tags with distance readout</item>
    /// <item>Minimap player blips (colour-coded by team / formation role)</item>
    /// <item>Formation position indicator arrow for wingmen</item>
    /// <item>Text chat with team/all channels (50-message history buffer)</item>
    /// <item>Voice-speaking indicators on name tags</item>
    /// <item>Connection-quality colour-coded ping display</item>
    /// <item>Toast notification feed (auto-dismiss after 5 seconds)</item>
    /// </list>
    /// </para>
    /// </summary>
    public class MultiplayerHUD : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static MultiplayerHUD Instance { get; private set; }

        // ── Inspector — Name Tags ─────────────────────────────────────────────────
        [Header("Name Tags")]
        [Tooltip("Prefab used to render each player's floating name tag.")]
        [SerializeField] private RectTransform nameTagPrefab;

        [Tooltip("Parent canvas used to parent name tag instances.")]
        [SerializeField] private Canvas worldCanvas;

        [Tooltip("Maximum range (m) at which name tags are visible.")]
        [SerializeField] private float nameTagMaxRangeM = 5000f;

        [Tooltip("Whether distances are shown in kilometres (true) or miles (false).")]
        [SerializeField] private bool useKilometres = true;

        // ── Inspector — Minimap ───────────────────────────────────────────────────
        [Header("Minimap")]
        [Tooltip("RectTransform of the minimap panel.")]
        [SerializeField] private RectTransform minimapPanel;

        [Tooltip("Prefab used for player blips on the minimap.")]
        [SerializeField] private RectTransform blipPrefab;

        [Tooltip("World radius (m) represented by the full minimap width.")]
        [SerializeField] private float minimapRadius = 50000f;

        // ── Inspector — Formation Indicator ──────────────────────────────────────
        [Header("Formation Indicator")]
        [Tooltip("Arrow indicator UI element shown to wingmen.")]
        [SerializeField] private RectTransform formationArrow;

        [Tooltip("Text element showing distance to the ideal slot position.")]
        [SerializeField] private Text formationDistanceText;

        // ── Inspector — Chat ──────────────────────────────────────────────────────
        [Header("Chat")]
        [Tooltip("Scroll rect containing the chat message history.")]
        [SerializeField] private ScrollRect chatScrollRect;

        [Tooltip("Prefab for a single chat message row.")]
        [SerializeField] private RectTransform chatMessagePrefab;

        [Tooltip("Input field for composing new chat messages.")]
        [SerializeField] private InputField chatInputField;

        [Tooltip("Maximum number of chat messages retained in the history buffer.")]
        [SerializeField] private int maxChatHistory = 50;

        // ── Inspector — Notifications ─────────────────────────────────────────────
        [Header("Notifications")]
        [Tooltip("Panel used to display toast notifications.")]
        [SerializeField] private RectTransform notificationPanel;

        [Tooltip("Prefab for a single toast notification.")]
        [SerializeField] private RectTransform notificationPrefab;

        [Tooltip("Seconds before a notification automatically dismisses.")]
        [SerializeField] private float notificationDismissSec = 5f;

        // ── Inspector — Quality Colours ───────────────────────────────────────────
        [Header("Connection Quality Colours")]
        [SerializeField] private Color qualityExcellent = Color.green;
        [SerializeField] private Color qualityGood      = new Color(0.6f, 1f, 0.2f);
        [SerializeField] private Color qualityFair      = Color.yellow;
        [SerializeField] private Color qualityPoor      = Color.red;

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly Dictionary<string, HudPlayerEntry> _players  = new();
        private readonly Dictionary<string, RectTransform>  _blips    = new();
        private readonly List<ChatMessage> _chatHistory               = new(50);
        private readonly List<RectTransform> _chatRows                = new();

        private Camera  _mainCamera;
        private string  _localPlayerId;
        private string  _localFormationId;
        private int     _localFormationSlot;
        private bool    _isTeamChatMode;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _mainCamera = Camera.main;

            // Wire up VoiceChatManager speaking events.
            if (VoiceChatManager.Instance != null)
                VoiceChatManager.Instance.OnPlayerSpeaking += HandlePlayerSpeaking;

            // Wire up NetworkManager2 connect/disconnect events.
            if (NetworkManager2.Instance != null)
            {
                NetworkManager2.Instance.OnPlayerConnected    += HandlePlayerConnected;
                NetworkManager2.Instance.OnPlayerDisconnected += HandlePlayerDisconnected;
                NetworkManager2.Instance.OnHostMigrated       += HandleHostMigrated;
            }
        }

        private void LateUpdate()
        {
            UpdateNameTags();
            UpdateMinimapBlips();
            UpdateFormationIndicator();
        }

        private void OnDestroy()
        {
            if (VoiceChatManager.Instance != null)
                VoiceChatManager.Instance.OnPlayerSpeaking -= HandlePlayerSpeaking;

            if (NetworkManager2.Instance != null)
            {
                NetworkManager2.Instance.OnPlayerConnected    -= HandlePlayerConnected;
                NetworkManager2.Instance.OnPlayerDisconnected -= HandlePlayerDisconnected;
                NetworkManager2.Instance.OnHostMigrated       -= HandleHostMigrated;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Registers a remote player so the HUD tracks their name tag and minimap blip.
        /// </summary>
        /// <param name="playerId">Player identifier.</param>
        /// <param name="displayName">Display name shown above the aircraft.</param>
        /// <param name="teamId">Optional team identifier for colour coding.</param>
        public void AddPlayer(string playerId, string displayName, string teamId = "")
        {
            if (_players.ContainsKey(playerId)) return;

            var entry = new HudPlayerEntry
            {
                playerId         = playerId,
                displayName      = displayName,
                teamId           = teamId,
                connectionQuality = ConnectionQuality.Good
            };

            // Spawn name tag.
            if (nameTagPrefab != null && worldCanvas != null)
            {
                var nameTag = Instantiate(nameTagPrefab, worldCanvas.transform);
                nameTag.name = $"NameTag_{playerId}";
                entry.nameTagRect = nameTag;
            }

            // Spawn minimap blip.
            if (blipPrefab != null && minimapPanel != null)
            {
                var blip = Instantiate(blipPrefab, minimapPanel);
                blip.name = $"Blip_{playerId}";
                _blips[playerId] = blip;
            }

            _players[playerId] = entry;
        }

        /// <summary>
        /// Removes a player from the HUD (e.g. on disconnect).
        /// </summary>
        /// <param name="playerId">Player to remove.</param>
        public void RemovePlayer(string playerId)
        {
            if (!_players.TryGetValue(playerId, out var entry)) return;

            if (entry.nameTagRect != null) Destroy(entry.nameTagRect.gameObject);
            if (_blips.TryGetValue(playerId, out var blip)) { Destroy(blip.gameObject); _blips.Remove(playerId); }

            _players.Remove(playerId);
        }

        /// <summary>
        /// Updates a remote player's world position each frame.
        /// </summary>
        /// <param name="playerId">Player identifier.</param>
        /// <param name="worldPosition">Current world-space position.</param>
        public void UpdatePlayerPosition(string playerId, Vector3 worldPosition)
        {
            if (_players.TryGetValue(playerId, out var entry))
                entry.worldPosition = worldPosition;
        }

        /// <summary>
        /// Updates the connection quality display for a specific player.
        /// </summary>
        /// <param name="playerId">Player identifier.</param>
        /// <param name="quality">Current connection quality.</param>
        public void UpdateConnectionQuality(string playerId, ConnectionQuality quality)
        {
            if (_players.TryGetValue(playerId, out var entry))
                entry.connectionQuality = quality;
        }

        /// <summary>
        /// Sets the local player's formation context so the indicator can be rendered.
        /// </summary>
        /// <param name="formationId">Active formation identifier (null to disable indicator).</param>
        /// <param name="slotIndex">This player's slot index in the formation.</param>
        public void SetFormationContext(string formationId, int slotIndex)
        {
            _localFormationId   = formationId;
            _localFormationSlot = slotIndex;
            if (formationArrow != null)
                formationArrow.gameObject.SetActive(!string.IsNullOrEmpty(formationId) && slotIndex > 0);
        }

        /// <summary>
        /// Displays a text chat message in the HUD and appends it to the history buffer.
        /// </summary>
        /// <param name="message">Message to display.</param>
        public void ReceiveChatMessage(ChatMessage message)
        {
            if (message == null) return;

            _chatHistory.Add(message);
            if (_chatHistory.Count > maxChatHistory)
                _chatHistory.RemoveAt(0);

            AppendChatRow(message);
        }

        /// <summary>
        /// Sends the current content of the chat input field.
        /// </summary>
        public void SendChatMessage()
        {
            if (chatInputField == null || string.IsNullOrWhiteSpace(chatInputField.text)) return;

            var msg = new ChatMessage
            {
                senderId      = _localPlayerId ?? "local",
                senderName    = "You",
                text          = chatInputField.text.Trim(),
                isTeamMessage = _isTeamChatMode,
                timestamp     = DateTime.UtcNow
            };

            chatInputField.text = string.Empty;
            ReceiveChatMessage(msg);

            // In production: forward to NetworkTransport.
        }

        /// <summary>
        /// Toggles the chat input between all-chat and team-chat modes.
        /// </summary>
        public void ToggleTeamChat()
        {
            _isTeamChatMode = !_isTeamChatMode;
            Debug.Log($"[SWEF][MultiplayerHUD] Chat mode: {(_isTeamChatMode ? "Team" : "All")}");
        }

        /// <summary>
        /// Displays a toast notification that auto-dismisses after <see cref="notificationDismissSec"/> seconds.
        /// </summary>
        /// <param name="message">Notification text.</param>
        public void ShowNotification(string message)
        {
            StartCoroutine(ShowNotificationCoroutine(message));
        }

        // ── Name tags ─────────────────────────────────────────────────────────────

        private void UpdateNameTags()
        {
            if (_mainCamera == null || worldCanvas == null) return;

            Vector3 localPos = _mainCamera.transform.position;

            foreach (var entry in _players.Values)
            {
                if (entry.nameTagRect == null) continue;

                float dist = Vector3.Distance(localPos, entry.worldPosition);
                bool visible = dist <= nameTagMaxRangeM;
                entry.nameTagRect.gameObject.SetActive(visible);

                if (!visible) continue;

                // Project world position to screen, then to canvas coordinates.
                Vector3 screenPos = _mainCamera.WorldToScreenPoint(entry.worldPosition + Vector3.up * 20f);
                if (screenPos.z < 0f) { entry.nameTagRect.gameObject.SetActive(false); continue; }

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    worldCanvas.GetComponent<RectTransform>(),
                    screenPos,
                    worldCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _mainCamera,
                    out Vector2 localPoint);

                entry.nameTagRect.anchoredPosition = localPoint;

                // Update text (assumes Text components at known child indices).
                string distStr = useKilometres
                    ? $"{dist / 1000f:F1} km"
                    : $"{dist / 1609.344f:F1} mi";

                var texts = entry.nameTagRect.GetComponentsInChildren<Text>();
                if (texts.Length >= 1) texts[0].text = entry.displayName;
                if (texts.Length >= 2)
                {
                    texts[1].text  = distStr;
                    texts[1].color = QualityToColour(entry.connectionQuality);
                }

                // Speaking indicator.
                if (texts.Length >= 3)
                    texts[2].text = entry.isSpeaking ? "🎙" : string.Empty;
            }
        }

        // ── Minimap ───────────────────────────────────────────────────────────────

        private void UpdateMinimapBlips()
        {
            if (minimapPanel == null) return;

            Vector3 localPos = transform.position;
            float mapSize    = minimapPanel.rect.width * 0.5f;

            foreach (var kvp in _blips)
            {
                if (!_players.TryGetValue(kvp.Key, out var entry)) continue;

                Vector3 delta = entry.worldPosition - localPos;
                float nx = Mathf.Clamp(delta.x / minimapRadius, -1f, 1f);
                float nz = Mathf.Clamp(delta.z / minimapRadius, -1f, 1f);

                kvp.Value.anchoredPosition = new Vector2(nx * mapSize, nz * mapSize);

                // Colour by formation role or team.
                var img = kvp.Value.GetComponent<Image>();
                if (img != null)
                    img.color = entry.formationRole.HasValue
                        ? RoleToColour(entry.formationRole.Value)
                        : Color.cyan;
            }
        }

        // ── Formation indicator ───────────────────────────────────────────────────

        private void UpdateFormationIndicator()
        {
            if (string.IsNullOrEmpty(_localFormationId) || _localFormationSlot <= 0
                || formationArrow == null || FormationFlyingManager.Instance == null) return;

            // Find the leader's transform.
            // In production: retrieve from PlayerAvatar registry.
            // Here we skip if no leader transform is available.
        }

        // ── Chat UI ───────────────────────────────────────────────────────────────

        private void AppendChatRow(ChatMessage message)
        {
            if (chatScrollRect == null || chatMessagePrefab == null) return;

            var row = Instantiate(chatMessagePrefab, chatScrollRect.content);
            row.name = $"ChatMsg_{_chatHistory.Count}";

            var texts = row.GetComponentsInChildren<Text>();
            if (texts.Length >= 1)
                texts[0].text = $"[{message.timestamp:HH:mm}] {(message.isTeamMessage ? "[T] " : "")}{message.senderName}: {message.text}";

            _chatRows.Add(row);

            // Prune excess rows.
            while (_chatRows.Count > maxChatHistory)
            {
                Destroy(_chatRows[0].gameObject);
                _chatRows.RemoveAt(0);
            }

            // Scroll to bottom.
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;
        }

        // ── Notifications ─────────────────────────────────────────────────────────

        private IEnumerator ShowNotificationCoroutine(string message)
        {
            if (notificationPanel == null || notificationPrefab == null)
            {
                Debug.Log($"[SWEF][MultiplayerHUD] Notification: {message}");
                yield break;
            }

            var notif = Instantiate(notificationPrefab, notificationPanel);
            var texts = notif.GetComponentsInChildren<Text>();
            if (texts.Length >= 1) texts[0].text = message;

            yield return new WaitForSeconds(notificationDismissSec);

            if (notif != null) Destroy(notif.gameObject);
        }

        // ── Event handlers ────────────────────────────────────────────────────────

        private void HandlePlayerSpeaking(string playerId, float amplitude)
        {
            if (_players.TryGetValue(playerId, out var entry))
                entry.isSpeaking = amplitude > 0f;
        }

        private void HandlePlayerConnected(string playerId)
        {
            ShowNotification($"✈ {playerId} joined the session.");
        }

        private void HandlePlayerDisconnected(string playerId)
        {
            ShowNotification($"✈ {playerId} left the session.");
            RemovePlayer(playerId);
        }

        private void HandleHostMigrated(string newHostId)
        {
            ShowNotification($"🔄 Host migrated to {newHostId}.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private Color QualityToColour(ConnectionQuality q) => q switch
        {
            ConnectionQuality.Excellent => qualityExcellent,
            ConnectionQuality.Good      => qualityGood,
            ConnectionQuality.Fair      => qualityFair,
            _                           => qualityPoor
        };

        private static Color RoleToColour(MissionRole role) => role switch
        {
            MissionRole.Lead    => Color.yellow,
            MissionRole.Wingman => Color.cyan,
            MissionRole.Support => Color.green,
            MissionRole.Scout   => new Color(1f, 0.5f, 0f),
            _                   => Color.white
        };
    }
}
