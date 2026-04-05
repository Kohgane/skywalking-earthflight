// SquadronUI.cs — Phase 109: Clan/Squadron System
// Full squadron management panel — tabs for info, members, missions, events, base, leaderboard, chat.
// Namespace: SWEF.Squadron

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Squadron
{
    /// <summary>
    /// Phase 109 — Full-screen squadron management panel with tabs for:
    /// Squadron Info/Settings, Member List, Mission Board, Event Calendar,
    /// Base Management, Leaderboard, Recruitment Settings, and Chat.
    /// </summary>
    public sealed class SquadronUI : MonoBehaviour
    {
        // ── Inspector references ───────────────────────────────────────────────

        [Header("Tab Buttons")]
        [SerializeField] private Button tabInfo;
        [SerializeField] private Button tabMembers;
        [SerializeField] private Button tabMissions;
        [SerializeField] private Button tabEvents;
        [SerializeField] private Button tabBase;
        [SerializeField] private Button tabLeaderboard;
        [SerializeField] private Button tabRecruitment;
        [SerializeField] private Button tabChat;

        [Header("Tab Panels")]
        [SerializeField] private GameObject panelInfo;
        [SerializeField] private GameObject panelMembers;
        [SerializeField] private GameObject panelMissions;
        [SerializeField] private GameObject panelEvents;
        [SerializeField] private GameObject panelBase;
        [SerializeField] private GameObject panelLeaderboard;
        [SerializeField] private GameObject panelRecruitment;
        [SerializeField] private GameObject panelChat;

        [Header("Info Tab")]
        [SerializeField] private Text squadronNameText;
        [SerializeField] private Text squadronTagText;
        [SerializeField] private Text squadronDescText;
        [SerializeField] private Text squadronLevelText;
        [SerializeField] private Text squadronXPText;
        [SerializeField] private Text squadronStatusText;
        [SerializeField] private Button disbandButton;
        [SerializeField] private Button leaveButton;

        [Header("Members Tab")]
        [SerializeField] private Transform memberListContainer;
        [SerializeField] private GameObject memberRowPrefab;

        [Header("Missions Tab")]
        [SerializeField] private Transform missionListContainer;
        [SerializeField] private GameObject missionCardPrefab;

        [Header("Events Tab")]
        [SerializeField] private Transform eventListContainer;
        [SerializeField] private GameObject eventCardPrefab;

        [Header("Leaderboard Tab")]
        [SerializeField] private Transform leaderboardContainer;
        [SerializeField] private GameObject leaderboardRowPrefab;

        [Header("Chat Tab")]
        [SerializeField] private Transform chatMessageContainer;
        [SerializeField] private GameObject chatMessagePrefab;
        [SerializeField] private InputField chatInputField;
        [SerializeField] private Button chatSendButton;

        // ── State ──────────────────────────────────────────────────────────────

        private readonly List<GameObject> _memberRows       = new List<GameObject>();
        private readonly List<GameObject> _missionCards     = new List<GameObject>();
        private readonly List<GameObject> _eventCards       = new List<GameObject>();
        private readonly List<GameObject> _leaderboardRows  = new List<GameObject>();
        private readonly List<GameObject> _chatMessages     = new List<GameObject>();

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Start()
        {
            WireTabButtons();
            WireChatButton();
            WireActionButtons();
            ShowTab(panelInfo);
            RefreshAll();
        }

        private void OnEnable()
        {
            if (SquadronManager.Instance != null)
            {
                SquadronManager.Instance.OnMemberJoined  += _ => RefreshMembers();
                SquadronManager.Instance.OnMemberLeft    += _ => RefreshMembers();
                SquadronManager.Instance.OnMemberPromoted += _ => RefreshMembers();
                SquadronManager.Instance.OnMemberDemoted  += _ => RefreshMembers();
            }

            if (SquadronChatController.Instance != null)
                SquadronChatController.Instance.OnMessageReceived += AppendChatMessage;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Refreshes all visible data from the squadron managers.</summary>
        public void RefreshAll()
        {
            RefreshInfo();
            RefreshMembers();
            RefreshMissions();
            RefreshEvents();
            RefreshLeaderboard();
            RefreshChat();
        }

        // ── Tab navigation ─────────────────────────────────────────────────────

        private void WireTabButtons()
        {
            void Wire(Button btn, GameObject panel)
            {
                if (btn == null) return;
                btn.onClick.AddListener(() => ShowTab(panel));
            }

            Wire(tabInfo,        panelInfo);
            Wire(tabMembers,     panelMembers);
            Wire(tabMissions,    panelMissions);
            Wire(tabEvents,      panelEvents);
            Wire(tabBase,        panelBase);
            Wire(tabLeaderboard, panelLeaderboard);
            Wire(tabRecruitment, panelRecruitment);
            Wire(tabChat,        panelChat);
        }

        private void ShowTab(GameObject activePanel)
        {
            foreach (var p in new[]
            {
                panelInfo, panelMembers, panelMissions, panelEvents,
                panelBase, panelLeaderboard, panelRecruitment, panelChat
            })
            {
                if (p != null) p.SetActive(p == activePanel);
            }
        }

        // ── Info tab ───────────────────────────────────────────────────────────

        private void RefreshInfo()
        {
            var info = SquadronManager.Instance?.CurrentSquadron;
            if (info == null) return;

            if (squadronNameText   != null) squadronNameText.text   = info.name;
            if (squadronTagText    != null) squadronTagText.text    = $"[{info.tag}]";
            if (squadronDescText   != null) squadronDescText.text   = info.description;
            if (squadronLevelText  != null) squadronLevelText.text  = $"Level {info.level}";
            if (squadronXPText     != null) squadronXPText.text     = $"{info.totalXP} XP";
            if (squadronStatusText != null) squadronStatusText.text = info.status.ToString();
        }

        private void WireActionButtons()
        {
            if (disbandButton != null)
                disbandButton.onClick.AddListener(() => SquadronManager.Instance?.DisbandSquadron());

            if (leaveButton != null)
                leaveButton.onClick.AddListener(() => SquadronManager.Instance?.LeaveSquadron());
        }

        // ── Members tab ────────────────────────────────────────────────────────

        private void RefreshMembers()
        {
            ClearList(_memberRows, memberListContainer);
            if (memberListContainer == null || memberRowPrefab == null) return;

            var members = SquadronManager.Instance?.GetMembers();
            if (members == null) return;

            foreach (var member in members)
            {
                var row = Instantiate(memberRowPrefab, memberListContainer);
                _memberRows.Add(row);

                var texts = row.GetComponentsInChildren<Text>();
                if (texts.Length > 0) texts[0].text = member.displayName;
                if (texts.Length > 1) texts[1].text = member.rank.ToString();
                if (texts.Length > 2) texts[2].text = $"{member.contributionXP} XP";
            }
        }

        // ── Missions tab ───────────────────────────────────────────────────────

        private void RefreshMissions()
        {
            ClearList(_missionCards, missionListContainer);
            if (missionListContainer == null || missionCardPrefab == null) return;

            var missions = SquadronMissionController.Instance?.GetActiveMissions();
            if (missions == null) return;

            foreach (var mission in missions)
            {
                var card = Instantiate(missionCardPrefab, missionListContainer);
                _missionCards.Add(card);

                var texts = card.GetComponentsInChildren<Text>();
                if (texts.Length > 0) texts[0].text = mission.title;
                if (texts.Length > 1) texts[1].text  = mission.missionType.ToString();
                if (texts.Length > 2)
                    texts[2].text = $"{mission.completedObjectives.Count}/{mission.objectives.Count} objectives";
            }
        }

        // ── Events tab ─────────────────────────────────────────────────────────

        private void RefreshEvents()
        {
            ClearList(_eventCards, eventListContainer);
            if (eventListContainer == null || eventCardPrefab == null) return;

            var events = SquadronEventScheduler.Instance?.GetUpcomingEvents();
            if (events == null) return;

            foreach (var ev in events)
            {
                var card = Instantiate(eventCardPrefab, eventListContainer);
                _eventCards.Add(card);

                var texts = card.GetComponentsInChildren<Text>();
                if (texts.Length > 0) texts[0].text = ev.title;
                if (texts.Length > 1) texts[1].text  = ev.eventType.ToString();
            }
        }

        // ── Leaderboard tab ────────────────────────────────────────────────────

        private void RefreshLeaderboard()
        {
            ClearList(_leaderboardRows, leaderboardContainer);
            if (leaderboardContainer == null || leaderboardRowPrefab == null) return;

            var board = SquadronLeaderboardController.Instance?.GetLeaderboard(
                SquadronLeaderboardCategory.TotalXP,
                SquadronLeaderboardPeriod.AllTime);
            if (board == null) return;

            foreach (var entry in board)
            {
                var row = Instantiate(leaderboardRowPrefab, leaderboardContainer);
                _leaderboardRows.Add(row);

                var texts = row.GetComponentsInChildren<Text>();
                if (texts.Length > 0) texts[0].text = $"#{entry.rank}";
                if (texts.Length > 1) texts[1].text  = entry.squadronName;
                if (texts.Length > 2) texts[2].text  = entry.score.ToString();
            }
        }

        // ── Chat tab ───────────────────────────────────────────────────────────

        private void WireChatButton()
        {
            if (chatSendButton != null)
                chatSendButton.onClick.AddListener(SendChatMessage);
        }

        private void SendChatMessage()
        {
            if (chatInputField == null || string.IsNullOrWhiteSpace(chatInputField.text)) return;
            SquadronChatController.Instance?.SendMessage(chatInputField.text);
            chatInputField.text = string.Empty;
        }

        private void RefreshChat()
        {
            ClearList(_chatMessages, chatMessageContainer);
            var messages = SquadronChatController.Instance?.Messages;
            if (messages == null) return;

            foreach (var msg in messages)
                AppendChatMessage(msg);
        }

        private void AppendChatMessage(SquadronChatMessage msg)
        {
            if (chatMessageContainer == null || chatMessagePrefab == null) return;

            var item = Instantiate(chatMessagePrefab, chatMessageContainer);
            _chatMessages.Add(item);

            var texts = item.GetComponentsInChildren<Text>();
            if (texts.Length > 0) texts[0].text = msg.isSystem ? "[System]" : msg.senderName;
            if (texts.Length > 1) texts[1].text = msg.text;
        }

        // ── Utility ────────────────────────────────────────────────────────────

        private static void ClearList(List<GameObject> list, Transform container)
        {
            foreach (var go in list)
                if (go != null) Object.Destroy(go);
            list.Clear();
        }
    }
}
