// SquadronHUD.cs — Phase 109: Clan/Squadron System
// In-flight squadron HUD — member positions, mission progress, quick chat, formation.
// Namespace: SWEF.Squadron

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Squadron
{
    /// <summary>
    /// Phase 109 — In-flight HUD overlay for the squadron system.
    /// Displays member status dots, active mission progress, quick chat shortcuts,
    /// and formation position indicators.
    /// </summary>
    public sealed class SquadronHUD : MonoBehaviour
    {
        // ── Inspector references ───────────────────────────────────────────────

        [Header("Member Status")]
        [SerializeField] private Transform memberDotContainer;
        [SerializeField] private GameObject memberDotPrefab;

        [Header("Mission Progress")]
        [SerializeField] private GameObject missionProgressPanel;
        [SerializeField] private Text missionTitleText;
        [SerializeField] private Text missionProgressText;
        [SerializeField] private Slider missionProgressBar;

        [Header("Quick Chat")]
        [SerializeField] private Button[] quickChatButtons;
        [SerializeField] private string[] quickChatMessages;

        [Header("Formation")]
        [SerializeField] private GameObject formationPanel;
        [SerializeField] private Text formationPositionText;

        [Header("Squadron Info")]
        [SerializeField] private Text squadronNameText;
        [SerializeField] private Text memberCountText;

        // ── State ──────────────────────────────────────────────────────────────

        private readonly List<GameObject> _memberDots = new List<GameObject>();
        private SquadronMission _trackedMission;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void OnEnable()
        {
            SubscribeEvents();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            UpdateMissionProgress();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Sets the mission to track in the progress overlay.</summary>
        public void TrackMission(SquadronMission mission)
        {
            _trackedMission = mission;
            if (missionProgressPanel != null)
                missionProgressPanel.SetActive(mission != null);
        }

        /// <summary>Refreshes all HUD elements from current squadron state.</summary>
        public void Refresh()
        {
            RefreshSquadronInfo();
            RefreshMemberDots();
            SetupQuickChatButtons();

            var active = SquadronMissionController.Instance?.GetActiveMissions();
            TrackMission(active != null && active.Count > 0 ? active[0] : null);
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private void RefreshSquadronInfo()
        {
            var info = SquadronManager.Instance?.CurrentSquadron;
            if (squadronNameText  != null) squadronNameText.text  = info != null ? $"[{info.tag}] {info.name}" : string.Empty;
            if (memberCountText   != null) memberCountText.text   = info != null ? $"{info.memberCount}/{info.maxMembers}" : string.Empty;
        }

        private void RefreshMemberDots()
        {
            foreach (var dot in _memberDots)
                if (dot != null) Destroy(dot);
            _memberDots.Clear();

            if (memberDotContainer == null || memberDotPrefab == null) return;

            var members = SquadronManager.Instance?.GetMembers();
            if (members == null) return;

            foreach (var member in members)
            {
                var dot = Instantiate(memberDotPrefab, memberDotContainer);
                _memberDots.Add(dot);

                // Set tooltip / label via first Text child
                var label = dot.GetComponentInChildren<Text>();
                if (label != null) label.text = member.displayName;

                // Colour by rank
                var img = dot.GetComponent<Image>();
                if (img != null)
                    img.color = RankColour(member.rank);
            }
        }

        private void SetupQuickChatButtons()
        {
            if (quickChatButtons == null || quickChatMessages == null) return;

            for (int i = 0; i < quickChatButtons.Length; i++)
            {
                if (quickChatButtons[i] == null) continue;
                quickChatButtons[i].onClick.RemoveAllListeners();

                if (i < quickChatMessages.Length)
                {
                    string msg = quickChatMessages[i];
                    quickChatButtons[i].onClick.AddListener(() =>
                        SquadronChatController.Instance?.SendMessage(msg));
                }
            }
        }

        private void UpdateMissionProgress()
        {
            if (_trackedMission == null || missionProgressPanel == null) return;

            if (missionTitleText != null)
                missionTitleText.text = _trackedMission.title;

            int total     = _trackedMission.objectives.Count;
            int completed = _trackedMission.completedObjectives.Count;
            float pct     = total > 0 ? (float)completed / total : 0f;

            if (missionProgressText != null)
                missionProgressText.text = $"{completed}/{total}";

            if (missionProgressBar != null)
                missionProgressBar.value = pct;
        }

        private static Color RankColour(SquadronRank rank) => rank switch
        {
            SquadronRank.Leader  => Color.yellow,
            SquadronRank.Officer => new Color(1f, 0.6f, 0f),
            SquadronRank.Veteran => Color.cyan,
            SquadronRank.Member  => Color.white,
            _                   => Color.grey
        };

        // ── Event subscriptions ────────────────────────────────────────────────

        private void SubscribeEvents()
        {
            if (SquadronManager.Instance != null)
            {
                SquadronManager.Instance.OnMemberJoined   += _ => RefreshMemberDots();
                SquadronManager.Instance.OnMemberLeft      += _ => RefreshMemberDots();
                SquadronManager.Instance.OnMemberPromoted  += _ => RefreshMemberDots();
            }

            if (SquadronMissionController.Instance != null)
            {
                SquadronMissionController.Instance.OnMissionStarted   += m => TrackMission(m);
                SquadronMissionController.Instance.OnMissionCompleted += _ => TrackMission(null);
                SquadronMissionController.Instance.OnMissionFailed    += _ => TrackMission(null);
            }
        }

        private void UnsubscribeEvents()
        {
            // Event lambdas are short-lived; no explicit removal needed for ephemeral HUD.
        }
    }
}
