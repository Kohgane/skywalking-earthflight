// LiveFlightPanelUI.cs — SWEF Live Flight Tracking & Real-World Data Overlay (Phase 103)
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.LiveFlight
{
    /// <summary>
    /// Full-screen panel showing a sortable, searchable list of all tracked aircraft.
    /// Each row displays callsign, type, altitude, speed, country and distance.
    /// Tapping a row starts follow mode.
    /// </summary>
    public class LiveFlightPanelUI : MonoBehaviour
    {
        // ── Sort order ────────────────────────────────────────────────────────────
        public enum SortMode { Distance, Altitude, Speed }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform  listContent;
        [SerializeField] private GameObject rowPrefab;

        [Header("Search")]
        [SerializeField] private TMPro.TMP_InputField searchInputField;

        [Header("Sort Buttons")]
        [SerializeField] private Button sortDistanceButton;
        [SerializeField] private Button sortAltitudeButton;
        [SerializeField] private Button sortSpeedButton;

        [Header("Toggle")]
        [SerializeField] private Button toggleVisibilityButton;

        // ── State ─────────────────────────────────────────────────────────────────
        private List<LiveAircraftInfo> _allAircraft = new List<LiveAircraftInfo>();
        private SortMode               _sortMode    = SortMode.Distance;
        private string                 _searchQuery = "";
        private bool                   _liveVisible = true;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Start()
        {
            if (LiveFlightAPIClient.Instance != null)
                LiveFlightAPIClient.Instance.OnAircraftDataReceived += OnDataReceived;

            sortDistanceButton?.onClick.AddListener(() => SetSort(SortMode.Distance));
            sortAltitudeButton?.onClick.AddListener(() => SetSort(SortMode.Altitude));
            sortSpeedButton?.onClick.AddListener(()    => SetSort(SortMode.Speed));
            toggleVisibilityButton?.onClick.AddListener(ToggleLiveVisibility);

            if (searchInputField != null)
                searchInputField.onValueChanged.AddListener(OnSearchChanged);

            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (LiveFlightAPIClient.Instance != null)
                LiveFlightAPIClient.Instance.OnAircraftDataReceived -= OnDataReceived;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows or hides the full-screen aircraft list panel.</summary>
        public void SetPanelVisible(bool visible)
        {
            if (panelRoot != null) panelRoot.SetActive(visible);
            if (visible) RebuildList();
        }

        /// <summary>Changes the active sort column and refreshes the list.</summary>
        public void SetSort(SortMode mode)
        {
            _sortMode = mode;
            RebuildList();
        }

        // ── Internals ─────────────────────────────────────────────────────────────

        private void OnDataReceived(List<LiveAircraftInfo> data)
        {
            _allAircraft = data;
            if (panelRoot != null && panelRoot.activeSelf)
                RebuildList();
        }

        private void OnSearchChanged(string query)
        {
            _searchQuery = query;
            RebuildList();
        }

        private void ToggleLiveVisibility()
        {
            _liveVisible = !_liveVisible;
            LiveAircraftRenderer.Instance?.SetVisible(_liveVisible);

#if SWEF_LOCALIZATION_AVAILABLE
            // Localization hook placeholder — update button label via LocalizationManager.
#endif
        }

        private void RebuildList()
        {
            if (listContent == null) return;

            // Clear existing rows
            foreach (Transform child in listContent)
                Destroy(child.gameObject);

            var sorted = GetFilteredSorted();
            Camera cam = Camera.main;

            foreach (var info in sorted)
            {
                float dist = cam != null
                    ? Vector3.Distance(cam.transform.position,
                        new Vector3(
                            (float)(info.longitude * Mathf.Deg2Rad * 6_371_000.0),
                            info.altitude,
                            (float)(info.latitude  * Mathf.Deg2Rad * 6_371_000.0)))
                    : 0f;

                var row = rowPrefab != null
                    ? Instantiate(rowPrefab, listContent)
                    : CreateDefaultRow(listContent);

                PopulateRow(row, info, dist);
            }
        }

        private List<LiveAircraftInfo> GetFilteredSorted()
        {
            var list = new List<LiveAircraftInfo>();
            string q = _searchQuery?.ToUpperInvariant() ?? "";

            foreach (var a in _allAircraft)
            {
                if (!string.IsNullOrEmpty(q))
                {
                    bool match = (a.callsign?.ToUpperInvariant().Contains(q) ?? false)
                              || (a.icao24?.ToUpperInvariant().Contains(q)   ?? false);
                    if (!match) continue;
                }
                list.Add(a);
            }

            Camera cam = Camera.main;
            Vector3 camPos = cam != null ? cam.transform.position : Vector3.zero;

            switch (_sortMode)
            {
                case SortMode.Altitude:
                    list.Sort((a, b) => b.altitude.CompareTo(a.altitude));
                    break;
                case SortMode.Speed:
                    list.Sort((a, b) => b.velocity.CompareTo(a.velocity));
                    break;
                default: // Distance
                    list.Sort((a, b) =>
                    {
                        float da = Vector3.Distance(camPos, new Vector3(
                            (float)(a.longitude * Mathf.Deg2Rad * 6_371_000.0), a.altitude,
                            (float)(a.latitude  * Mathf.Deg2Rad * 6_371_000.0)));
                        float db = Vector3.Distance(camPos, new Vector3(
                            (float)(b.longitude * Mathf.Deg2Rad * 6_371_000.0), b.altitude,
                            (float)(b.latitude  * Mathf.Deg2Rad * 6_371_000.0)));
                        return da.CompareTo(db);
                    });
                    break;
            }

            return list;
        }

        private void PopulateRow(GameObject row, LiveAircraftInfo info, float dist)
        {
            // Try to find labelled child TMP components by name.
            SetChildText(row, "Callsign", info.callsign);
            SetChildText(row, "Type",     info.aircraftType);
            SetChildText(row, "Altitude", $"{info.altitude:F0} m");
            SetChildText(row, "Speed",    $"{info.velocity:F0} m/s");
            SetChildText(row, "Country",  info.originCountry);
            SetChildText(row, "Distance", $"{dist / 1000f:F0} km");

            var btn = row.GetComponentInChildren<Button>();
            if (btn != null)
            {
                string icao = info.icao24;
                btn.onClick.AddListener(() =>
                {
                    var follow = FindFirstObjectByType<LiveFlightFollowController>();
                    follow?.FollowAircraft(icao);
                });
            }
        }

        private static void SetChildText(GameObject root, string childName, string text)
        {
            var t = root.transform.Find(childName);
            if (t == null) return;
            var tm = t.GetComponent<TMPro.TextMeshProUGUI>();
            if (tm != null) tm.text = text;
        }

        private static GameObject CreateDefaultRow(Transform parent)
        {
            var go = new GameObject("Row");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }
    }
}
