// SpaceStationUI.cs — SWEF Space Station & Orbital Docking System
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.SpaceStation
{
    /// <summary>
    /// MonoBehaviour that renders the station interaction panel:
    /// station info, docking port status list, approach/dock button,
    /// interior map, undock button, and available stations catalogue.
    /// </summary>
    public class SpaceStationUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Panel")]
        [SerializeField] private GameObject _panelRoot;

        [Header("Station Info")]
        [SerializeField] private Text _stationNameText;
        [SerializeField] private Text _orbitAltitudeText;
        [SerializeField] private Text _orbitPeriodText;
        [SerializeField] private Text _segmentCountText;

        [Header("Port List")]
        [SerializeField] private Transform _portListParent;
        [SerializeField] private Text      _portEntryPrefab;

        [Header("Buttons")]
        [SerializeField] private Button _approachButton;
        [SerializeField] private Button _undockButton;

        [Header("Catalogue")]
        [SerializeField] private Transform _catalogueParent;
        [SerializeField] private Text      _catalogueEntryPrefab;

        // ── Private state ─────────────────────────────────────────────────────────

        private StationDefinition _selectedStation;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            SetVisible(false);
            if (_approachButton != null) _approachButton.onClick.AddListener(OnApproachClicked);
            if (_undockButton   != null) _undockButton.onClick.AddListener(OnUndockClicked);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Opens the panel and populates it with the given station's data.</summary>
        public void ShowStation(StationDefinition station)
        {
            _selectedStation = station;
            if (station == null) { SetVisible(false); return; }

            SetVisible(true);
            PopulateInfo(station);
            PopulatePortList(station.dockingPorts);
        }

        /// <summary>Updates the station catalogue list.</summary>
        public void PopulateCatalogue(IEnumerable<StationDefinition> stations)
        {
            if (_catalogueParent == null || _catalogueEntryPrefab == null) return;

            foreach (Transform child in _catalogueParent)
                Destroy(child.gameObject);

            foreach (StationDefinition def in stations)
            {
                Text entry = Instantiate(_catalogueEntryPrefab, _catalogueParent);
                entry.text = $"{def.stationId}  Alt: {def.orbitalParams.altitude / 1000.0:F0} km";
            }
        }

        /// <summary>Shows or hides the entire panel.</summary>
        public void SetVisible(bool visible)
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(visible);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void PopulateInfo(StationDefinition station)
        {
            if (_stationNameText  != null) _stationNameText.text  = station.stationId;
            if (_orbitAltitudeText != null)
                _orbitAltitudeText.text = $"{station.orbitalParams.altitude / 1000.0:F0} km";
            if (_orbitPeriodText  != null)
                _orbitPeriodText.text   = $"{station.orbitalParams.period / 60.0:F1} min";
            if (_segmentCountText  != null)
                _segmentCountText.text  = $"{station.segments.Length} segments";
        }

        private void PopulatePortList(DockingPortDefinition[] ports)
        {
            if (_portListParent == null || _portEntryPrefab == null) return;

            foreach (Transform child in _portListParent)
                Destroy(child.gameObject);

            foreach (DockingPortDefinition port in ports)
            {
                Text entry = Instantiate(_portEntryPrefab, _portListParent);
                entry.text = $"{port.portId}  [{port.state}]";
            }
        }

        private void OnApproachClicked()
        {
            if (_selectedStation == null || DockingController.Instance == null) return;
            if (_selectedStation.dockingPorts.Length == 0) return;
            string portId = _selectedStation.dockingPorts[0].portId;
            DockingController.Instance.BeginDockingApproach(_selectedStation.stationId, portId);
        }

        private void OnUndockClicked()
        {
            DockingController.Instance?.Undock();
        }
    }
}
