// LiveFlightHUD.cs — SWEF Live Flight Tracking & Real-World Data Overlay (Phase 103)
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LiveFlight
{
    /// <summary>
    /// Head-up display overlay showing live flight statistics and filter controls.
    ///
    /// <para>Provides nearby aircraft count, closest aircraft summary, altitude/type/
    /// country filters, and tap-to-select with an info popup and Follow button.</para>
    ///
    /// <para>Minimap blip integration is compiled only when
    /// <c>SWEF_MINIMAP_AVAILABLE</c> is defined.</para>
    /// </summary>
    public class LiveFlightHUD : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Configuration")]
        [SerializeField] private LiveFlightConfig config;

        [Header("HUD Labels")]
        [SerializeField] private TMPro.TextMeshProUGUI nearbyCountText;
        [SerializeField] private TMPro.TextMeshProUGUI closestCallsignText;
        [SerializeField] private TMPro.TextMeshProUGUI closestDistanceText;
        [SerializeField] private GameObject            hudRoot;

        [Header("Info Popup")]
        [SerializeField] private GameObject            infoPopup;
        [SerializeField] private TMPro.TextMeshProUGUI popupCallsignText;
        [SerializeField] private TMPro.TextMeshProUGUI popupAltText;
        [SerializeField] private TMPro.TextMeshProUGUI popupSpeedText;
        [SerializeField] private TMPro.TextMeshProUGUI popupTypeText;
        [SerializeField] private TMPro.TextMeshProUGUI popupCountryText;
        [SerializeField] private UnityEngine.UI.Button followButton;

        [Header("Filters")]
        [SerializeField] private Vector2    altitudeRange  = new Vector2(0f, 20000f);
        [SerializeField] private string     filterType     = "";   // empty = all
        [SerializeField] private string     filterCountry  = "";   // empty = all

        // ── State ─────────────────────────────────────────────────────────────────
        private List<LiveAircraftInfo> _lastData    = new List<LiveAircraftInfo>();
        private LiveAircraftInfo       _selectedAircraft;
        private bool                   _hudVisible  = true;

#if SWEF_MINIMAP_AVAILABLE
        [Header("Minimap (optional)")]
        [SerializeField] private SWEF.Minimap.MinimapManager minimapManager;
#endif

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Start()
        {
            if (LiveFlightAPIClient.Instance != null)
                LiveFlightAPIClient.Instance.OnAircraftDataReceived += OnDataReceived;

            if (infoPopup != null)  infoPopup.SetActive(false);
            if (followButton != null)
                followButton.onClick.AddListener(OnFollowButtonClicked);
        }

        private void OnDestroy()
        {
            if (LiveFlightAPIClient.Instance != null)
                LiveFlightAPIClient.Instance.OnAircraftDataReceived -= OnDataReceived;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Show or hide the entire HUD overlay.</summary>
        public void SetHUDVisible(bool visible)
        {
            _hudVisible = visible;
            if (hudRoot != null) hudRoot.SetActive(visible);
        }

        /// <summary>
        /// Selects an aircraft by ICAO code, showing the info popup and enabling
        /// the Follow button.
        /// </summary>
        public void SelectAircraft(string icao24)
        {
            foreach (var a in _lastData)
            {
                if (a.icao24 == icao24)
                {
                    _selectedAircraft = a;
                    ShowInfoPopup(a);
                    return;
                }
            }
        }

        /// <summary>Updates filter settings and refreshes the display.</summary>
        public void SetAltitudeFilter(float min, float max)
        {
            altitudeRange = new Vector2(min, max);
            RefreshHUD(FilterAircraft(_lastData));
        }

        /// <summary>Filters by ICAO aircraft type designator (e.g. "B737").  Empty string = all.</summary>
        public void SetTypeFilter(string type)   { filterType    = type;    RefreshHUD(FilterAircraft(_lastData)); }

        /// <summary>Filters by origin country name.  Empty string = all.</summary>
        public void SetCountryFilter(string ctry){ filterCountry = ctry;   RefreshHUD(FilterAircraft(_lastData)); }

        // ── Filter logic ──────────────────────────────────────────────────────────

        /// <summary>
        /// Applies the current altitude, type and country filters to
        /// <paramref name="source"/> and returns the matching subset.
        /// </summary>
        public List<LiveAircraftInfo> FilterAircraft(List<LiveAircraftInfo> source)
        {
            var result = new List<LiveAircraftInfo>();
            foreach (var a in source)
            {
                if (a.altitude < altitudeRange.x || a.altitude > altitudeRange.y) continue;
                if (!string.IsNullOrEmpty(filterType)    && a.aircraftType    != filterType)    continue;
                if (!string.IsNullOrEmpty(filterCountry) && a.originCountry   != filterCountry) continue;
                result.Add(a);
            }
            return result;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void OnDataReceived(List<LiveAircraftInfo> data)
        {
            _lastData = data;
            var filtered = FilterAircraft(data);
            RefreshHUD(filtered);

#if SWEF_MINIMAP_AVAILABLE
            if (minimapManager != null)
            {
                // Minimap blip update would be handled by LiveFlightMinimapOverlay;
                // this guard is present for safety if referenced directly.
            }
#endif
        }

        private void RefreshHUD(List<LiveAircraftInfo> visible)
        {
            if (!_hudVisible) return;

            if (nearbyCountText != null)
                nearbyCountText.text = $"{visible.Count} aircraft";

            LiveAircraftInfo closest = FindClosest(visible);
            if (closestCallsignText != null)
                closestCallsignText.text = closest.icao24 != null ? closest.callsign : "—";
        }

        private static LiveAircraftInfo FindClosest(List<LiveAircraftInfo> list)
        {
            LiveAircraftInfo best = default;
            float bestDist = float.MaxValue;

            var cam = Camera.main;
            if (cam == null || list.Count == 0) return best;

            Vector3 camPos = cam.transform.position;
            foreach (var a in list)
            {
                Vector3 pos = new Vector3(
                    (float)(a.longitude * Mathf.Deg2Rad * 6_371_000.0),
                    a.altitude,
                    (float)(a.latitude  * Mathf.Deg2Rad * 6_371_000.0));
                float d = Vector3.Distance(camPos, pos);
                if (d < bestDist) { bestDist = d; best = a; }
            }
            return best;
        }

        private void ShowInfoPopup(LiveAircraftInfo info)
        {
            if (infoPopup == null) return;
            infoPopup.SetActive(true);
            if (popupCallsignText != null) popupCallsignText.text = info.callsign;
            if (popupAltText      != null) popupAltText.text      = $"{info.altitude:F0} m";
            if (popupSpeedText    != null) popupSpeedText.text    = $"{info.velocity:F0} m/s";
            if (popupTypeText     != null) popupTypeText.text     = info.aircraftType;
            if (popupCountryText  != null) popupCountryText.text  = info.originCountry;
        }

        private void OnFollowButtonClicked()
        {
            if (string.IsNullOrEmpty(_selectedAircraft.icao24)) return;
            var follow = FindFirstObjectByType<LiveFlightFollowController>();
            follow?.FollowAircraft(_selectedAircraft.icao24);
        }
    }
}
