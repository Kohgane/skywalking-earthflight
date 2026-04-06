// SatelliteTrackingUI.cs — Phase 114: Satellite & Space Debris Tracking
// Main tracking dashboard: satellite list, orbit view, search, filters, favourites.
// Namespace: SWEF.SatelliteTracking

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_TMPRO
using TMPro;
#endif

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Main satellite tracking dashboard UI controller.
    /// Manages the satellite list panel, search bar, type/orbit filters, and favourites toggle.
    /// </summary>
    public class SatelliteTrackingUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Panels")]
        [SerializeField] private GameObject trackingDashboardPanel;
        [SerializeField] private GameObject orbitViewPanel;
        [SerializeField] private GameObject satelliteInfoPanel;

        [Header("List")]
        [SerializeField] private Transform satelliteListContent;
        [SerializeField] private GameObject satelliteListItemPrefab;

        [Header("Filters")]
#if UNITY_TMPRO
        [SerializeField] private TMP_InputField searchInputField;
        [SerializeField] private TMP_Dropdown typeFilterDropdown;
        [SerializeField] private TMP_Dropdown orbitFilterDropdown;
#else
        [SerializeField] private InputField searchInputField;
        [SerializeField] private Dropdown typeFilterDropdown;
        [SerializeField] private Dropdown orbitFilterDropdown;
#endif

        [Header("Toggles")]
        [SerializeField] private Toggle favouritesToggle;
        [SerializeField] private Toggle activeOnlyToggle;

        [Header("Buttons")]
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button closeButton;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when the user selects a satellite from the list.</summary>
        public event Action<SatelliteRecord> OnSatelliteSelected;

        // ── Private state ─────────────────────────────────────────────────────────
        private SatelliteType? _typeFilter;
        private OrbitType?     _orbitFilter;
        private bool           _favOnly;
        private bool           _activeOnly;
        private string         _searchQuery = string.Empty;
        private readonly List<SatelliteRecord> _currentList = new List<SatelliteRecord>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            SetupControls();
            RefreshList();

            var mgr = SatelliteTrackingManager.Instance;
            if (mgr != null)
            {
                mgr.OnSatelliteAdded   += _ => RefreshList();
                mgr.OnSatelliteUpdated += _ => { };
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows the tracking dashboard.</summary>
        public void Show() => trackingDashboardPanel?.SetActive(true);

        /// <summary>Hides the tracking dashboard.</summary>
        public void Hide() => trackingDashboardPanel?.SetActive(false);

        /// <summary>Refreshes the satellite list using current filter settings.</summary>
        public void RefreshList()
        {
            _currentList.Clear();

            var mgr = SatelliteTrackingManager.Instance;
            if (mgr == null) return;

            var filtered = SatelliteCatalogFilter.CompositeFilter(
                mgr.TrackedSatellites,
                _typeFilter, _orbitFilter,
                activeOnly:   _activeOnly,
                nameQuery:    _searchQuery);

            if (_favOnly)
                filtered = SatelliteCatalogFilter.FavouritesOnly(filtered);

            foreach (var r in filtered) _currentList.Add(r);

            PopulateList();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void SetupControls()
        {
            if (refreshButton != null) refreshButton.onClick.AddListener(RefreshList);
            if (closeButton   != null) closeButton.onClick.AddListener(Hide);

            if (favouritesToggle != null)
                favouritesToggle.onValueChanged.AddListener(v => { _favOnly = v; RefreshList(); });

            if (activeOnlyToggle != null)
                activeOnlyToggle.onValueChanged.AddListener(v => { _activeOnly = v; RefreshList(); });

#if UNITY_TMPRO
            if (searchInputField != null)
                searchInputField.onValueChanged.AddListener(v => { _searchQuery = v; RefreshList(); });
#else
            if (searchInputField != null)
                searchInputField.onValueChanged.AddListener(v => { _searchQuery = v; RefreshList(); });
#endif
        }

        private void PopulateList()
        {
            if (satelliteListContent == null || satelliteListItemPrefab == null) return;

            // Clear existing items
            foreach (Transform child in satelliteListContent)
                Destroy(child.gameObject);

            // Create new items (cap at 100 for performance)
            int count = Mathf.Min(_currentList.Count, 100);
            for (int i = 0; i < count; i++)
            {
                var record = _currentList[i];
                var item = Instantiate(satelliteListItemPrefab, satelliteListContent);

                // Try to set name label
                var label = item.GetComponentInChildren<Text>();
                if (label != null) label.text = $"{record.name} ({record.noradId})";

                // Bind click
                var btn = item.GetComponent<Button>();
                if (btn != null)
                {
                    var captured = record;
                    btn.onClick.AddListener(() => HandleSatelliteSelected(captured));
                }
            }
        }

        private void HandleSatelliteSelected(SatelliteRecord record)
        {
            OnSatelliteSelected?.Invoke(record);
            if (satelliteInfoPanel != null) satelliteInfoPanel.SetActive(true);
        }
    }
}
