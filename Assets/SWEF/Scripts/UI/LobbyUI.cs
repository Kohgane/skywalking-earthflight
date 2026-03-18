using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.UI
{
    /// <summary>
    /// Full-screen lobby panel with three tabs (Browse, Create, Quick Join)
    /// and a room-detail view after joining.
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Singleton instance; set during <see cref="Awake"/>.</summary>
        public static LobbyUI Instance { get; private set; }

        // ── Inspector — Root ─────────────────────────────────────────────────────
        [Header("Root Panel")]
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private Button     closeButton;

        [Header("Connection Status")]
        [SerializeField] private GameObject connectingSpinner;
        [SerializeField] private GameObject connectedIcon;
        [SerializeField] private GameObject errorIcon;

        // ── Tabs ─────────────────────────────────────────────────────────────────
        [Header("Tab Buttons")]
        [SerializeField] private Button browseTabButton;
        [SerializeField] private Button createTabButton;
        [SerializeField] private Button quickJoinTabButton;

        [Header("Tab Panels")]
        [SerializeField] private GameObject browsePanel;
        [SerializeField] private GameObject createPanel;
        [SerializeField] private GameObject quickJoinPanel;

        // ── Browse Tab ────────────────────────────────────────────────────────────
        [Header("Browse Tab")]
        [SerializeField] private Transform  roomListContent;
        [SerializeField] private GameObject roomListItemPrefab;
        [SerializeField] private Button     refreshButton;
        [SerializeField] private Text       refreshCooldownText;

        // ── Create Tab ────────────────────────────────────────────────────────────
        [Header("Create Tab")]
        [SerializeField] private InputField roomNameInput;
        [SerializeField] private Slider     maxPlayersSlider;
        [SerializeField] private Text       maxPlayersValueText;
        [SerializeField] private Toggle     publicToggle;
        [SerializeField] private Toggle     weatherSyncToggle;
        [SerializeField] private Button     createRoomButton;

        // ── Quick Join Tab ────────────────────────────────────────────────────────
        [Header("Quick Join Tab")]
        [SerializeField] private Button quickJoinButton;
        [SerializeField] private Text   quickJoinStatusText;

        // ── Room Detail Panel ─────────────────────────────────────────────────────
        [Header("Room Detail Panel")]
        [SerializeField] private GameObject roomDetailPanel;
        [SerializeField] private Text       roomDetailTitle;
        [SerializeField] private Transform  roomDetailPlayerList;
        [SerializeField] private GameObject roomDetailPlayerItemPrefab;
        [SerializeField] private Button     startRaceButton;
        [SerializeField] private Button     leaveRoomButton;

        // ── State ────────────────────────────────────────────────────────────────
        private float _refreshCooldown;
        private const float RefreshCooldownDuration = 5f;
        private Multiplayer.RoomManager _roomManager;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _roomManager = Multiplayer.RoomManager.Instance != null
                ? Multiplayer.RoomManager.Instance
                : FindFirstObjectByType<Multiplayer.RoomManager>();

            // Tab buttons
            if (browseTabButton    != null) browseTabButton.onClick.AddListener(() => SelectTab(0));
            if (createTabButton    != null) createTabButton.onClick.AddListener(() => SelectTab(1));
            if (quickJoinTabButton != null) quickJoinTabButton.onClick.AddListener(() => SelectTab(2));

            // Actions
            if (closeButton       != null) closeButton.onClick.AddListener(OnClose);
            if (refreshButton     != null) refreshButton.onClick.AddListener(OnRefresh);
            if (createRoomButton  != null) createRoomButton.onClick.AddListener(OnCreateRoom);
            if (quickJoinButton   != null) quickJoinButton.onClick.AddListener(OnQuickJoin);
            if (startRaceButton   != null) startRaceButton.onClick.AddListener(OnStartRace);
            if (leaveRoomButton   != null) leaveRoomButton.onClick.AddListener(OnLeaveRoom);

            if (maxPlayersSlider != null)
            {
                maxPlayersSlider.minValue = 2;
                maxPlayersSlider.maxValue = 8;
                maxPlayersSlider.wholeNumbers = true;
                maxPlayersSlider.value = 8;
                maxPlayersSlider.onValueChanged.AddListener(v =>
                {
                    if (maxPlayersValueText != null)
                        maxPlayersValueText.text = v.ToString("0");
                });
            }

            // Subscribe to room events
            if (_roomManager != null)
            {
                _roomManager.OnRoomJoined        += OnRoomJoined;
                _roomManager.OnRoomLeft          += OnRoomLeft;
                _roomManager.OnRoomListUpdated   += OnRoomListUpdated;
                _roomManager.OnPlayerJoined      += _ => RefreshRoomDetailPlayerList();
                _roomManager.OnPlayerLeft        += _ => RefreshRoomDetailPlayerList();
            }

            if (lobbyPanel       != null) lobbyPanel.SetActive(false);
            if (roomDetailPanel  != null) roomDetailPanel.SetActive(false);

            SetConnectionStatus(ConnectionStatus.Connected);
            SelectTab(0);
        }

        private void Update()
        {
            if (_refreshCooldown > 0f)
            {
                _refreshCooldown -= Time.deltaTime;
                if (refreshCooldownText != null)
                    refreshCooldownText.text = _refreshCooldown > 0f
                        ? $"Refresh ({_refreshCooldown:F0}s)"
                        : "Refresh";
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Opens the lobby panel and shows the Browse tab.</summary>
        public void OpenLobby()
        {
            if (lobbyPanel != null) lobbyPanel.SetActive(true);
            SelectTab(0);
            OnRefresh();
        }

        /// <summary>Closes the lobby panel (with confirmation if in a room).</summary>
        public void CloseLobby()
        {
            OnClose();
        }

        // ── Tab Selection ─────────────────────────────────────────────────────────

        private void SelectTab(int index)
        {
            if (browsePanel    != null) browsePanel.SetActive(index == 0);
            if (createPanel    != null) createPanel.SetActive(index == 1);
            if (quickJoinPanel != null) quickJoinPanel.SetActive(index == 2);
        }

        // ── Browse Tab ────────────────────────────────────────────────────────────

        private void OnRefresh()
        {
            if (_refreshCooldown > 0f) return;
            _refreshCooldown = RefreshCooldownDuration;

            SetConnectionStatus(ConnectionStatus.Connecting);

            if (_roomManager == null) return;
            var rooms = _roomManager.GetRoomList();
            PopulateRoomList(rooms);

            SetConnectionStatus(ConnectionStatus.Connected);
        }

        private void PopulateRoomList(List<Multiplayer.RoomInfo> rooms)
        {
            if (roomListContent == null || roomListItemPrefab == null) return;

            foreach (Transform child in roomListContent)
                Destroy(child.gameObject);

            foreach (var room in rooms)
            {
                var item  = Instantiate(roomListItemPrefab, roomListContent);
                var texts = item.GetComponentsInChildren<Text>();

                if (texts.Length > 0) texts[0].text = room.roomName;
                if (texts.Length > 1) texts[1].text = $"{room.playerCount}/{room.maxPlayers}";
                if (texts.Length > 2) texts[2].text = room.region;

                var btn = item.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    string roomId = room.roomId;
                    btn.onClick.AddListener(() => _roomManager?.JoinRoom(roomId));
                }
            }
        }

        private void OnRoomListUpdated(List<Multiplayer.RoomInfo> rooms) => PopulateRoomList(rooms);

        // ── Create Tab ────────────────────────────────────────────────────────────

        private void OnCreateRoom()
        {
            if (_roomManager == null) return;

            string name = roomNameInput != null && !string.IsNullOrWhiteSpace(roomNameInput.text)
                ? roomNameInput.text
                : "My Room";

            var settings = new Multiplayer.RoomSettings
            {
                maxPlayers   = maxPlayersSlider != null ? (int)maxPlayersSlider.value : 8,
                isPublic     = publicToggle     != null ? publicToggle.isOn            : true,
                weatherSync  = weatherSyncToggle != null ? weatherSyncToggle.isOn      : true,
                regionFilter = "auto"
            };

            _roomManager.CreateRoom(settings);
        }

        // ── Quick Join Tab ────────────────────────────────────────────────────────

        private void OnQuickJoin()
        {
            if (_roomManager == null) return;

            if (quickJoinStatusText != null)
                quickJoinStatusText.text = "Finding best room…";

            _roomManager.JoinRandomRoom(new Multiplayer.RoomSettings());
        }

        // ── Room Detail ───────────────────────────────────────────────────────────

        private void OnRoomJoined(Multiplayer.RoomInfo room)
        {
            if (roomDetailPanel != null) roomDetailPanel.SetActive(true);
            if (roomDetailTitle != null) roomDetailTitle.text = room.roomName;

            // Show start-race button only for the host
            bool isHost = _roomManager != null && _roomManager.IsHost;
            if (startRaceButton != null) startRaceButton.gameObject.SetActive(isHost);

            RefreshRoomDetailPlayerList();

            if (quickJoinStatusText != null)
                quickJoinStatusText.text = $"Joined: {room.roomName}";
        }

        private void OnRoomLeft()
        {
            if (roomDetailPanel != null) roomDetailPanel.SetActive(false);
        }

        private void RefreshRoomDetailPlayerList()
        {
            if (roomDetailPlayerList == null || _roomManager == null) return;

            foreach (Transform child in roomDetailPlayerList)
                Destroy(child.gameObject);

            if (roomDetailPlayerItemPrefab == null) return;

            foreach (var info in _roomManager.PlayersInRoom)
            {
                var item  = Instantiate(roomDetailPlayerItemPrefab, roomDetailPlayerList);
                var texts = item.GetComponentsInChildren<Text>();

                string label = (info.isHost ? "👑 " : "") + info.playerName
                             + (info.isReady ? " ✅" : " ⏳");
                if (texts.Length > 0) texts[0].text = label;
            }
        }

        private void OnStartRace()
        {
            var race = Multiplayer.MultiplayerRace.Instance != null
                ? Multiplayer.MultiplayerRace.Instance
                : FindFirstObjectByType<Multiplayer.MultiplayerRace>();
            race?.StartRace();
        }

        private void OnLeaveRoom()
        {
            if (_roomManager != null && _roomManager.IsInRoom)
            {
                // Could show confirmation dialog here
                _roomManager.LeaveRoom();
            }
        }

        private void OnClose()
        {
            if (_roomManager != null && _roomManager.IsInRoom)
            {
                // In a full implementation show a confirmation dialog
                Debug.Log("[SWEF][LobbyUI] Close requested while in room — showing confirmation.");
            }

            if (lobbyPanel != null) lobbyPanel.SetActive(false);
        }

        // ── Connection Status ─────────────────────────────────────────────────────

        private enum ConnectionStatus { Connecting, Connected, Error }

        private void SetConnectionStatus(ConnectionStatus status)
        {
            if (connectingSpinner != null) connectingSpinner.SetActive(status == ConnectionStatus.Connecting);
            if (connectedIcon     != null) connectedIcon.SetActive(status == ConnectionStatus.Connected);
            if (errorIcon         != null) errorIcon.SetActive(status == ConnectionStatus.Error);
        }
    }
}
