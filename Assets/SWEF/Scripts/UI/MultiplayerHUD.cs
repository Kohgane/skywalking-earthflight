using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.UI
{
    /// <summary>
    /// In-game HUD overlay for multiplayer sessions.
    /// Displays room info, a collapsible player list, a simulated ping indicator,
    /// quick-action buttons, and a race overlay with countdown and live rankings.
    /// </summary>
    public class MultiplayerHUD : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Singleton instance; set during <see cref="Awake"/>.</summary>
        public static MultiplayerHUD Instance { get; private set; }

        // ── Inspector — Root & Animation ─────────────────────────────────────────
        [Header("Root")]
        [SerializeField] private CanvasGroup hudCanvasGroup;
        [SerializeField] private RectTransform hudPanel;

        [Header("Room Info")]
        [SerializeField] private Text roomNameText;
        [SerializeField] private Text playerCountText;
        [SerializeField] private Text pingText;

        [Header("Player List")]
        [SerializeField] private GameObject playerListPanel;
        [SerializeField] private Transform  playerListContent;
        [SerializeField] private GameObject playerListItemPrefab;
        [SerializeField] private Button     playerListToggleButton;

        [Header("Quick Actions")]
        [SerializeField] private Button leaveRoomButton;
        [SerializeField] private Button pingButton;
        [SerializeField] private Button readyButton;

        [Header("Race Overlay")]
        [SerializeField] private GameObject raceOverlayPanel;
        [SerializeField] private Text       countdownText;
        [SerializeField] private Transform  rankingContent;
        [SerializeField] private GameObject rankingItemPrefab;
        [SerializeField] private Text       checkpointNotification;

        // ── State ────────────────────────────────────────────────────────────────
        private bool _isVisible;
        private bool _playerListExpanded = true;
        private float _simulatedPingMs;
        private Coroutine _animCoroutine;
        private Coroutine _checkpointFadeCoroutine;
        private float _pingTimer;

        private Multiplayer.RoomManager    _roomManager;
        private Multiplayer.MultiplayerRace _race;
        private Multiplayer.ProximityChat  _proximityChat;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _roomManager   = Multiplayer.RoomManager.Instance != null
                ? Multiplayer.RoomManager.Instance
                : FindFirstObjectByType<Multiplayer.RoomManager>();

            _race          = Multiplayer.MultiplayerRace.Instance != null
                ? Multiplayer.MultiplayerRace.Instance
                : FindFirstObjectByType<Multiplayer.MultiplayerRace>();

            _proximityChat = Multiplayer.ProximityChat.Instance != null
                ? Multiplayer.ProximityChat.Instance
                : FindFirstObjectByType<Multiplayer.ProximityChat>();

            // Wire quick-action buttons
            if (leaveRoomButton != null)    leaveRoomButton.onClick.AddListener(OnLeaveRoom);
            if (pingButton != null)         pingButton.onClick.AddListener(OnSendPing);
            if (readyButton != null)        readyButton.onClick.AddListener(OnToggleReady);
            if (playerListToggleButton != null)
                playerListToggleButton.onClick.AddListener(TogglePlayerList);

            // Subscribe to room events
            if (_roomManager != null)
            {
                _roomManager.OnRoomJoined   += _ => RefreshRoomInfo();
                _roomManager.OnRoomLeft      += () => SetVisible(false);
                _roomManager.OnPlayerJoined  += _ => RefreshPlayerList();
                _roomManager.OnPlayerLeft    += _ => RefreshPlayerList();
            }

            // Subscribe to race events
            if (_race != null)
            {
                _race.OnRaceStateChanged     += OnRaceStateChanged;
                _race.OnCheckpointReached    += OnCheckpointReached;
            }

            // Hide HUD initially
            SetVisible(false);
        }

        private void Update()
        {
            // Simulate ping fluctuation
            _pingTimer += Time.deltaTime;
            if (_pingTimer >= 2f)
            {
                _pingTimer = 0f;
                _simulatedPingMs = Random.Range(20f, 180f);
                RefreshPingDisplay();
            }

            // Update race overlay if racing
            if (_race != null && _race.State == Multiplayer.RaceState.Countdown)
                RefreshCountdown();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Shows or hides the HUD panel with a slide-in/out animation.
        /// </summary>
        /// <param name="visible">True to show, false to hide.</param>
        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(AnimateVisibility(visible));
        }

        /// <summary>Refreshes all displayed room information.</summary>
        public void RefreshRoomInfo()
        {
            if (_roomManager == null || !_roomManager.IsInRoom) return;

            var room = _roomManager.CurrentRoom;
            if (roomNameText    != null) roomNameText.text    = room.roomName;
            if (playerCountText != null) playerCountText.text = $"{room.playerCount}/{room.maxPlayers}";

            RefreshPlayerList();
            SetVisible(true);
        }

        // ── Internal Helpers ─────────────────────────────────────────────────────

        private void RefreshPlayerList()
        {
            if (playerListContent == null || _roomManager == null) return;

            // Clear existing items
            foreach (Transform child in playerListContent)
                Destroy(child.gameObject);

            if (playerListItemPrefab == null) return;

            foreach (Multiplayer.PlayerInfo info in _roomManager.PlayersInRoom)
            {
                var item = Instantiate(playerListItemPrefab, playerListContent);
                var texts = item.GetComponentsInChildren<Text>();

                // Populate: [0] name, [1] altitude/speed, [2] host crown (enabled/disabled)
                if (texts.Length > 0) texts[0].text = info.isHost ? $"👑 {info.playerName}" : info.playerName;
                if (texts.Length > 1) texts[1].text = "— alt/speed —";

                // Mute button
                var muteBtn = item.GetComponentInChildren<Button>();
                if (muteBtn != null)
                {
                    string pid = info.playerId;
                    muteBtn.onClick.AddListener(() => OnMutePlayer(pid));
                }
            }
        }

        private void RefreshPingDisplay()
        {
            if (pingText == null) return;

            string colTag;
            if (_simulatedPingMs < 80f)       colTag = "<color=#00ff00>";
            else if (_simulatedPingMs < 150f)  colTag = "<color=#ffff00>";
            else                               colTag = "<color=#ff4444>";

            pingText.text = $"{colTag}[{_simulatedPingMs:F0}ms]</color>";
        }

        private void RefreshCountdown()
        {
            if (countdownText == null || _race == null) return;
            float remaining = _race.CountdownRemaining;
            countdownText.text = remaining > 0f ? $"{Mathf.CeilToInt(remaining)}" : "GO!";
        }

        private void TogglePlayerList()
        {
            _playerListExpanded = !_playerListExpanded;
            if (playerListPanel != null) playerListPanel.SetActive(_playerListExpanded);
        }

        // ── Button Callbacks ─────────────────────────────────────────────────────

        private void OnLeaveRoom()
        {
            if (_roomManager != null) _roomManager.LeaveRoom();
        }

        private void OnSendPing()
        {
            if (_proximityChat != null) _proximityChat.SendPing();
        }

        private void OnToggleReady()
        {
            Debug.Log("[SWEF][MultiplayerHUD] Toggle ready.");
        }

        private void OnMutePlayer(string playerId)
        {
            if (_proximityChat != null) _proximityChat.SetPlayerMuted(playerId, true);
        }

        // ── Race Event Handlers ───────────────────────────────────────────────────

        private void OnRaceStateChanged(Multiplayer.RaceState state)
        {
            bool showOverlay = state == Multiplayer.RaceState.Countdown
                            || state == Multiplayer.RaceState.Racing
                            || state == Multiplayer.RaceState.Finished;

            if (raceOverlayPanel != null) raceOverlayPanel.SetActive(showOverlay);

            if (state == Multiplayer.RaceState.Racing)
                StartCoroutine(RefreshRankingsLoop());
        }

        private void OnCheckpointReached(string playerId, int checkpointIndex)
        {
            if (checkpointNotification == null) return;
            var info = _roomManager?.PlayersInRoom.Count > 0
                ? _roomManager.PlayersInRoom[0]
                : null;
            // Look up the specific player who hit the checkpoint
            if (_roomManager != null)
            {
                foreach (var p in _roomManager.PlayersInRoom)
                {
                    if (p.playerId == playerId) { info = p; break; }
                }
            }
            string name = info?.playerName ?? playerId;
            checkpointNotification.text = $"{name} reached checkpoint {checkpointIndex + 1}!";

            if (_checkpointFadeCoroutine != null) StopCoroutine(_checkpointFadeCoroutine);
            _checkpointFadeCoroutine = StartCoroutine(FadeNotification());
        }

        private IEnumerator FadeNotification()
        {
            if (checkpointNotification == null) yield break;

            checkpointNotification.gameObject.SetActive(true);
            yield return new WaitForSeconds(3f);
            checkpointNotification.gameObject.SetActive(false);
        }

        private IEnumerator RefreshRankingsLoop()
        {
            while (_race != null && _race.State == Multiplayer.RaceState.Racing)
            {
                RefreshRankings();
                yield return new WaitForSeconds(0.5f);
            }
            RefreshRankings();
        }

        private void RefreshRankings()
        {
            if (rankingContent == null || rankingItemPrefab == null || _race == null) return;

            foreach (Transform child in rankingContent)
                Destroy(child.gameObject);

            int rank = 1;
            foreach (var result in _race.Leaderboard)
            {
                var item   = Instantiate(rankingItemPrefab, rankingContent);
                var texts  = item.GetComponentsInChildren<Text>();
                if (texts.Length > 0) texts[0].text = $"#{rank} {result.playerName}";
                if (texts.Length > 1) texts[1].text = $"{result.finalAltitude:F0}m";
                rank++;
            }
        }

        // ── Animation ────────────────────────────────────────────────────────────

        private IEnumerator AnimateVisibility(bool show)
        {
            float duration = 0.3f;
            float elapsed  = 0f;

            float startAlpha  = hudCanvasGroup != null ? hudCanvasGroup.alpha : (show ? 0f : 1f);
            float targetAlpha = show ? 1f : 0f;

            float panelWidth  = hudPanel != null ? hudPanel.rect.width : 300f;
            Vector2 startPos  = hudPanel != null ? hudPanel.anchoredPosition : Vector2.zero;
            Vector2 targetPos = show
                ? new Vector2(-10f, startPos.y)                    // visible position
                : new Vector2(panelWidth + 10f, startPos.y);       // slid off-screen right

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (hudCanvasGroup != null) hudCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                if (hudPanel       != null) hudPanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

                yield return null;
            }

            if (hudCanvasGroup != null)
            {
                hudCanvasGroup.alpha             = targetAlpha;
                hudCanvasGroup.interactable      = show;
                hudCanvasGroup.blocksRaycasts    = show;
            }
        }
    }
}
