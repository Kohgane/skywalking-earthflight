// TerrainSurveyUI.cs — SWEF Terrain Scanning & Geological Survey System
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// Optional dependency guard — WaypointNavigator
#if SWEF_GUIDEDTOUR_AVAILABLE
using SWEF.GuidedTour;
#endif

namespace SWEF.TerrainSurvey
{
    /// <summary>
    /// Full-screen survey catalog: scrollable POI list with filters (by feature type,
    /// by date, by altitude range), navigate-to-POI, export, and a statistics panel.
    /// </summary>
    public class TerrainSurveyUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Panel Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("POI List")]
        [SerializeField] private Transform  poiListContainer;
        [SerializeField] private GameObject poiEntryPrefab;

        [Header("Filters")]
        [SerializeField] private Dropdown   featureTypeFilter;
        [SerializeField] private Dropdown   sortOrderDropdown;
        [SerializeField] private Slider     altitudeMinSlider;
        [SerializeField] private Slider     altitudeMaxSlider;
        [SerializeField] private Text       altitudeRangeLabel;

        [Header("Statistics Panel")]
        [SerializeField] private Text       statsLabel;
        [SerializeField] private GameObject statsPanel;

        [Header("Export")]
        [SerializeField] private Button     exportButton;

        // ── State ─────────────────────────────────────────────────────────────────
        private GeologicalFeatureType? _activeFeatureFilter = null;
        private float _filterAltMin = 0f;
        private float _filterAltMax = 9000f;
        private bool  _sortByDate   = true;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (SurveyPOIManager.Instance != null)
                SurveyPOIManager.Instance.OnPOIDiscovered += OnPOIDiscovered;
        }

        private void OnDisable()
        {
            if (SurveyPOIManager.Instance != null)
                SurveyPOIManager.Instance.OnPOIDiscovered -= OnPOIDiscovered;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Opens the survey catalog panel.</summary>
        public void OpenCatalog()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            RefreshList();
            RefreshStats();
            TerrainSurveyAnalytics.TrackCatalogOpened();
        }

        /// <summary>Closes the survey catalog panel.</summary>
        public void CloseCatalog()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        // ── Filter callbacks (wired in Inspector) ─────────────────────────────────

        public void OnFeatureFilterChanged(int index)
        {
            // index 0 = All
            _activeFeatureFilter = index == 0 ? (GeologicalFeatureType?)null
                                              : (GeologicalFeatureType)(index - 1);
            RefreshList();
        }

        public void OnSortOrderChanged(int index)
        {
            _sortByDate = index == 0;
            RefreshList();
        }

        public void OnAltitudeRangeChanged(float _)
        {
            if (altitudeMinSlider != null) _filterAltMin = altitudeMinSlider.value;
            if (altitudeMaxSlider != null) _filterAltMax = altitudeMaxSlider.value;

            if (altitudeRangeLabel != null)
                altitudeRangeLabel.text = $"{_filterAltMin:F0}m – {_filterAltMax:F0}m";

            RefreshList();
        }

        // ── Navigate to POI ───────────────────────────────────────────────────────

        /// <summary>Navigates the aircraft to the given POI via WaypointNavigator.</summary>
        public void NavigateToPOI(SurveyPOI poi)
        {
            if (poi == null) return;

#if SWEF_GUIDEDTOUR_AVAILABLE
            if (WaypointNavigator.Instance != null)
                WaypointNavigator.Instance.SetManualTarget(poi.position);
#else
            Debug.Log($"[SurveyUI] Navigate to: {poi.position} (WaypointNavigator unavailable)");
#endif
            TerrainSurveyAnalytics.TrackNavigateToPOI(poi);
        }

        // ── Export ────────────────────────────────────────────────────────────────

        public void ExportSurveyData()
        {
            if (SurveyPOIManager.Instance == null) return;

            var pois = SurveyPOIManager.Instance.GetAllPOIs();
            var lines = new List<string> { "id,featureType,x,y,z,timestamp" };
            foreach (var p in pois)
                lines.Add($"{p.id},{p.featureType},{p.position.x:F2},{p.position.y:F2}," +
                          $"{p.position.z:F2},{p.discoveredTimestamp}");

            string csv  = string.Join("\n", lines);
            string path = System.IO.Path.Combine(Application.persistentDataPath, "survey_export.csv");
            System.IO.File.WriteAllText(path, csv);
            Debug.Log($"[SurveyUI] Export written to: {path}");
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void OnPOIDiscovered(SurveyPOI _)
        {
            if (panelRoot != null && panelRoot.activeSelf)
                RefreshList();
        }

        private void RefreshList()
        {
            if (poiListContainer == null || poiEntryPrefab == null) return;

            // Clear existing entries
            foreach (Transform child in poiListContainer)
                Destroy(child.gameObject);

            if (SurveyPOIManager.Instance == null) return;

            IEnumerable<SurveyPOI> pois = SurveyPOIManager.Instance.GetAllPOIs();

            // Feature filter
            if (_activeFeatureFilter.HasValue)
                pois = pois.Where(p => p.featureType == _activeFeatureFilter.Value);

            // Altitude range filter
            pois = pois.Where(p => p.position.y >= _filterAltMin && p.position.y <= _filterAltMax);

            // Sort
            pois = _sortByDate
                ? pois.OrderByDescending(p => p.discoveredTimestamp)
                : pois.OrderByDescending(p => p.position.y);

            foreach (var poi in pois)
            {
                GameObject entry = Instantiate(poiEntryPrefab, poiListContainer);
                PopulatePOIEntry(entry, poi);
            }
        }

        private void PopulatePOIEntry(GameObject entry, SurveyPOI poi)
        {
            // Populate via standard Text/Button children by tag or name convention
            var labels = entry.GetComponentsInChildren<Text>();
            if (labels.Length > 0) labels[0].text = poi.nameLocKey;
            if (labels.Length > 1) labels[1].text = $"{poi.position.y:F0} m";
            if (labels.Length > 2) labels[2].text = poi.featureType.ToString();

            var button = entry.GetComponentInChildren<Button>();
            if (button != null)
            {
                var captured = poi;
                button.onClick.AddListener(() => NavigateToPOI(captured));
            }
        }

        private void RefreshStats()
        {
            if (statsLabel == null || SurveyPOIManager.Instance == null) return;

            var allPois = SurveyPOIManager.Instance.GetAllPOIs();
            var groups  = allPois.GroupBy(p => p.featureType)
                                 .OrderByDescending(g => g.Count());

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Total discoveries: {allPois.Count}");
            foreach (var grp in groups)
                sb.AppendLine($"  {grp.Key}: {grp.Count()}");

            statsLabel.text = sb.ToString();
        }
    }
}
