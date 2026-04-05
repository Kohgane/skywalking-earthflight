// NPCTrafficUI.cs — Phase 110: Dynamic NPC & Air Traffic Ecosystem
// Full-screen settings and information panel for the NPC Traffic system.
// Namespace: SWEF.NPCTraffic

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.NPCTraffic
{
    /// <summary>
    /// Phase 110 — Full-screen settings panel and traffic information browser
    /// for the NPC Traffic system.  Shows active NPC list, density controls,
    /// airport status, and formation management.
    /// Attach to a Canvas panel GameObject.
    /// </summary>
    public sealed class NPCTrafficUI : MonoBehaviour
    {
        #region Inspector

        [Header("Panel")]
        [Tooltip("Root panel shown/hidden by TogglePanel().")]
        [SerializeField] private GameObject _rootPanel;

        [Header("Density Controls")]
        [Tooltip("Dropdown for selecting traffic density preset.")]
        [SerializeField] private Dropdown _densityDropdown;

        [Tooltip("Label displaying the current effective max NPC count.")]
        [SerializeField] private Text _effectiveMaxLabel;

        [Header("NPC List")]
        [Tooltip("ScrollView content transform where NPC list entries are parented.")]
        [SerializeField] private RectTransform _npcListContent;

        [Tooltip("Prefab for a single NPC list entry row.")]
        [SerializeField] private GameObject _npcEntryPrefab;

        [Header("Airport Status")]
        [Tooltip("ScrollView content for airport activity list.")]
        [SerializeField] private RectTransform _airportListContent;

        [Tooltip("Prefab for a single airport status row.")]
        [SerializeField] private GameObject _airportEntryPrefab;

        [Header("Formation")]
        [Tooltip("Label showing current formation status.")]
        [SerializeField] private Text _formationStatusLabel;

        [Tooltip("Button to leave current formation.")]
        [SerializeField] private Button _leaveFormationButton;

        #endregion

        #region Private State

        private bool _isVisible;
        private readonly List<GameObject> _npcEntries     = new List<GameObject>();
        private readonly List<GameObject> _airportEntries = new List<GameObject>();

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            SubscribeEvents();
            PopulateDensityDropdown();
            if (_leaveFormationButton != null)
                _leaveFormationButton.onClick.AddListener(HandleLeaveFormation);
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            if (_leaveFormationButton != null)
                _leaveFormationButton.onClick.RemoveListener(HandleLeaveFormation);
        }

        private void Update()
        {
            if (_isVisible)
            {
                RefreshNPCList();
                RefreshFormationStatus();
            }
        }

        #endregion

        #region Public API

        /// <summary>Opens or closes the traffic UI panel.</summary>
        public void TogglePanel()
        {
            _isVisible = !_isVisible;
            if (_rootPanel != null) _rootPanel.SetActive(_isVisible);

            if (_isVisible)
            {
                RefreshNPCList();
                RefreshAirportList();
                RefreshFormationStatus();
            }
        }

        /// <summary>Forces the panel open.</summary>
        public void ShowPanel()
        {
            _isVisible = true;
            if (_rootPanel != null) _rootPanel.SetActive(true);
            RefreshNPCList();
            RefreshAirportList();
        }

        /// <summary>Forces the panel closed.</summary>
        public void HidePanel()
        {
            _isVisible = false;
            if (_rootPanel != null) _rootPanel.SetActive(false);
        }

        #endregion

        #region Private — Density

        private void PopulateDensityDropdown()
        {
            if (_densityDropdown == null) return;
            _densityDropdown.ClearOptions();
            _densityDropdown.AddOptions(new List<string>
                { "None", "Sparse", "Normal", "Dense" });

            if (NPCTrafficManager.Instance != null)
                _densityDropdown.value = (int)NPCTrafficManager.Instance.CurrentDensity;

            _densityDropdown.onValueChanged.AddListener(HandleDensityChanged);
        }

        private void HandleDensityChanged(int value)
        {
            // Density change is handled at runtime via config; this just logs for now
            Debug.Log($"[NPCTrafficUI] Density selection: {(NPCTrafficDensity)value}");
            UpdateEffectiveMaxLabel();
        }

        private void UpdateEffectiveMaxLabel()
        {
            if (_effectiveMaxLabel == null || NPCTrafficManager.Instance == null) return;
            _effectiveMaxLabel.text = $"Max NPCs: {NPCTrafficManager.Instance.EffectiveMaxNPCs}";
        }

        #endregion

        #region Private — NPC List

        private void RefreshNPCList()
        {
            if (_npcListContent == null || _npcEntryPrefab == null ||
                NPCTrafficManager.Instance == null) return;

            IReadOnlyList<NPCAircraftData> npcs = NPCTrafficManager.Instance.ActiveNPCs;

            // Grow pool
            while (_npcEntries.Count < npcs.Count)
            {
                GameObject entry = Instantiate(_npcEntryPrefab, _npcListContent);
                _npcEntries.Add(entry);
            }

            // Hide extras
            for (int i = npcs.Count; i < _npcEntries.Count; i++)
                _npcEntries[i].SetActive(false);

            for (int i = 0; i < npcs.Count; i++)
            {
                _npcEntries[i].SetActive(true);
                Text label = _npcEntries[i].GetComponentInChildren<Text>();
                if (label != null)
                {
                    NPCAircraftData n = npcs[i];
                    label.text = $"{n.Callsign}  {n.Category}  {n.AltitudeMetres:F0}m  {n.BehaviorState}";
                }
            }
        }

        #endregion

        #region Private — Airport List

        private void RefreshAirportList()
        {
            if (_airportListContent == null || _airportEntryPrefab == null ||
                AirportActivityManager.Instance == null) return;

            IReadOnlyList<AirportActivityState> airports = AirportActivityManager.Instance.TrackedAirports;

            while (_airportEntries.Count < airports.Count)
            {
                GameObject entry = Instantiate(_airportEntryPrefab, _airportListContent);
                _airportEntries.Add(entry);
            }

            for (int i = airports.Count; i < _airportEntries.Count; i++)
                _airportEntries[i].SetActive(false);

            for (int i = 0; i < airports.Count; i++)
            {
                _airportEntries[i].SetActive(true);
                Text label = _airportEntries[i].GetComponentInChildren<Text>();
                if (label != null)
                {
                    AirportActivityState a = airports[i];
                    string status = a.IsActive ? $"ACTIVE ({a.ActivityLevel * 100f:F0}%)" : "Inactive";
                    label.text = $"{a.ICAO}  {status}  NPCs: {a.AssignedNPCCount}";
                }
            }
        }

        #endregion

        #region Private — Formation

        private void RefreshFormationStatus()
        {
            if (_formationStatusLabel == null || NPCFormationController.Instance == null) return;

            NPCFormationData f = NPCFormationController.Instance.GetPlayerFormation();
            if (f == null)
            {
                _formationStatusLabel.text = "No formation";
                if (_leaveFormationButton != null) _leaveFormationButton.interactable = false;
            }
            else
            {
                _formationStatusLabel.text = $"Formation: {f.FormationId}  Lead: {f.LeadCallsign}  Members: {f.WingmanCallsigns.Count + 1}";
                if (_leaveFormationButton != null) _leaveFormationButton.interactable = true;
            }
        }

        private void HandleLeaveFormation()
        {
            NPCFormationController.Instance?.PlayerLeaveFormation();
            RefreshFormationStatus();
        }

        #endregion

        #region Private — Event Subscriptions

        private void SubscribeEvents()
        {
            if (NPCTrafficManager.Instance != null)
            {
                NPCTrafficManager.Instance.OnNPCSpawned    += _ => UpdateEffectiveMaxLabel();
                NPCTrafficManager.Instance.OnDensityChanged += _ => UpdateEffectiveMaxLabel();
            }
        }

        private void UnsubscribeEvents()
        {
            // Lambdas cannot be unsubscribed; listeners auto-clear when the component is destroyed
        }

        #endregion
    }
}
