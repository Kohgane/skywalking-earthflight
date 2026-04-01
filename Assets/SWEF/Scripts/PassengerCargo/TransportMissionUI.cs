using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.PassengerCargo
{
    /// <summary>
    /// MonoBehaviour that drives the full-screen transport contract board.
    ///
    /// Features:
    ///   • Available-contracts list with type/destination/reward filters
    ///   • Contract detail card with route info, payload, reward breakdown
    ///   • Accept / Decline flow
    ///   • Active mission status panel
    ///   • Delivery history panel
    ///   • Completion celebration screen with star rating and rewards
    /// </summary>
    public class TransportMissionUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Root Panels")]
        [SerializeField] private GameObject boardPanel;
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private GameObject activeMissionPanel;
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private GameObject historyPanel;

        [Header("Board")]
        [SerializeField] private Transform    contractListRoot;
        [SerializeField] private GameObject   contractCardPrefab;
        [SerializeField] private Dropdown     filterTypeDropdown;
        [SerializeField] private Dropdown     filterSortDropdown;

        [Header("Detail")]
        [SerializeField] private Text   lblDetailOrigin;
        [SerializeField] private Text   lblDetailDestination;
        [SerializeField] private Text   lblDetailType;
        [SerializeField] private Text   lblDetailPayload;
        [SerializeField] private Text   lblDetailReward;
        [SerializeField] private Text   lblDetailTimeLimit;
        [SerializeField] private Text   lblDetailDescription;
        [SerializeField] private Button btnAccept;
        [SerializeField] private Button btnDecline;

        [Header("Active Mission")]
        [SerializeField] private Text   lblActiveOrigin;
        [SerializeField] private Text   lblActiveDestination;
        [SerializeField] private Text   lblActiveMissionType;
        [SerializeField] private Button btnAbandon;

        [Header("Completion")]
        [SerializeField] private Text   lblCompletionTitle;
        [SerializeField] private Text   lblStarRating;
        [SerializeField] private Text   lblCompletionXP;
        [SerializeField] private Text   lblCompletionCoins;
        [SerializeField] private Text   lblCompletionComfort;
        [SerializeField] private Button btnCompletionClose;

        [Header("History")]
        [SerializeField] private Transform historyListRoot;
        [SerializeField] private GameObject historyEntryPrefab;

        // ── State ─────────────────────────────────────────────────────────────
        private readonly List<TransportContract> _availableContracts = new List<TransportContract>();
        private TransportContract                _selectedContract;
        private int                              _pilotRank;

        // ── Unity Lifecycle ───────────────────────────────────────────────────
        private void Start()
        {
            if (btnAccept  != null) btnAccept.onClick.AddListener(OnAcceptClicked);
            if (btnDecline != null) btnDecline.onClick.AddListener(OnDeclineClicked);
            if (btnAbandon != null) btnAbandon.onClick.AddListener(OnAbandonClicked);
            if (btnCompletionClose != null)
                btnCompletionClose.onClick.AddListener(OnCompletionClose);

            var pm = SWEF.Progression.ProgressionManager.Instance;
            if (pm != null) _pilotRank = pm.CurrentRankLevel;

            var manager = TransportMissionManager.Instance;
            if (manager != null)
            {
                manager.OnMissionAccepted  += OnMissionAccepted;
                manager.OnMissionCompleted += ShowCompletionScreen;
                manager.OnMissionFailed    += OnMissionEnded;
                manager.OnMissionAbandoned += OnMissionEnded;
            }

            HideAllPanels();
        }

        private void OnDestroy()
        {
            var manager = TransportMissionManager.Instance;
            if (manager != null)
            {
                manager.OnMissionAccepted  -= OnMissionAccepted;
                manager.OnMissionCompleted -= ShowCompletionScreen;
                manager.OnMissionFailed    -= OnMissionEnded;
                manager.OnMissionAbandoned -= OnMissionEnded;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Opens the contract board (generates fresh contracts).</summary>
        public void OpenBoard()
        {
            var pm = SWEF.Progression.ProgressionManager.Instance;
            _pilotRank = pm != null ? pm.CurrentRankLevel : 0;

            Vector3 playerPos = Vector3.zero;
            var fc = FindObjectOfType<SWEF.Flight.FlightController>();
            if (fc != null) playerPos = fc.transform.position;

            _availableContracts.Clear();
            _availableContracts.AddRange(
                TransportContractGenerator.GenerateContracts(6, playerPos, _pilotRank));

            PopulateContractList();
            HideAllPanels();
            if (boardPanel != null) boardPanel.SetActive(true);
        }

        /// <summary>Closes the board.</summary>
        public void CloseBoard() => HideAllPanels();

        // ── Internal ──────────────────────────────────────────────────────────
        private void PopulateContractList()
        {
            if (contractListRoot == null || contractCardPrefab == null) return;

            foreach (Transform t in contractListRoot)
                Destroy(t.gameObject);

            foreach (var contract in _availableContracts)
            {
                var card = Instantiate(contractCardPrefab, contractListRoot);
                var texts = card.GetComponentsInChildren<Text>();
                if (texts.Length > 0) texts[0].text = $"{contract.origin} → {contract.destination}";
                if (texts.Length > 1) texts[1].text = contract.missionType.ToString();
                if (texts.Length > 2) texts[2].text = $"⊙ {contract.baseReward}";

                var btn = card.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    var c = contract;
                    btn.onClick.AddListener(() => ShowDetail(c));
                }
            }
        }

        private void ShowDetail(TransportContract contract)
        {
            _selectedContract = contract;
            if (detailPanel == null) return;

            if (lblDetailOrigin)      lblDetailOrigin.text      = contract.origin;
            if (lblDetailDestination) lblDetailDestination.text  = contract.destination;
            if (lblDetailType)        lblDetailType.text         = contract.missionType.ToString();
            if (lblDetailReward)
                lblDetailReward.text = $"⊙ {contract.baseReward} + ⊙ {contract.bonusReward} bonus";
            if (lblDetailTimeLimit)
                lblDetailTimeLimit.text = contract.timeLimitSeconds > 0f
                    ? $"{contract.timeLimitSeconds / 60f:F0} min"
                    : "Unlimited";

            bool isPax = contract.missionType == MissionType.PassengerStandard
                      || contract.missionType == MissionType.PassengerVIP
                      || contract.missionType == MissionType.PassengerCharter;

            if (lblDetailPayload)
                lblDetailPayload.text = isPax
                    ? $"{contract.passengerProfile.passengerCount} pax"
                    : $"{contract.cargoManifest.weight:F0} kg — {contract.cargoManifest.category}";

            HideAllPanels();
            detailPanel.SetActive(true);
        }

        private void OnAcceptClicked()
        {
            if (_selectedContract == null) return;

            var manager = TransportMissionManager.Instance;
            if (manager == null) return;

            if (manager.AcceptContract(_selectedContract))
            {
                HideAllPanels();
            }
        }

        private void OnDeclineClicked()
        {
            _selectedContract = null;
            HideAllPanels();
            if (boardPanel != null) boardPanel.SetActive(true);
        }

        private void OnAbandonClicked()
        {
            TransportMissionManager.Instance?.AbandonMission();
        }

        private void OnMissionAccepted(TransportContract contract)
        {
            HideAllPanels();
            if (activeMissionPanel != null) activeMissionPanel.SetActive(true);

            if (lblActiveOrigin)      lblActiveOrigin.text      = contract.origin;
            if (lblActiveDestination) lblActiveDestination.text  = contract.destination;
            if (lblActiveMissionType) lblActiveMissionType.text  = contract.missionType.ToString();
        }

        private void ShowCompletionScreen(DeliveryResult result)
        {
            HideAllPanels();
            if (completionPanel != null) completionPanel.SetActive(true);

            string stars = new string('★', result.starRating).PadRight(5, '☆');
            if (lblStarRating)       lblStarRating.text       = stars;
            if (lblCompletionXP)     lblCompletionXP.text     = $"+{result.totalXP} XP";
            if (lblCompletionCoins)  lblCompletionCoins.text  = $"+{result.totalCoins} ⊙";
            if (lblCompletionComfort)
                lblCompletionComfort.text = $"Comfort: {result.comfortScore:F0}";
        }

        private void OnMissionEnded()  => HideAllPanels();
        private void OnCompletionClose() => HideAllPanels();

        private void HideAllPanels()
        {
            if (boardPanel        != null) boardPanel.SetActive(false);
            if (detailPanel       != null) detailPanel.SetActive(false);
            if (activeMissionPanel!= null) activeMissionPanel.SetActive(false);
            if (completionPanel   != null) completionPanel.SetActive(false);
            if (historyPanel      != null) historyPanel.SetActive(false);
        }
    }
}
