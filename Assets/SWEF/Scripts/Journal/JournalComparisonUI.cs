using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SWEF.Localization;

namespace SWEF.Journal
{
    /// <summary>
    /// Flight comparison tool.
    /// Allows the player to select two <see cref="FlightLogEntry"/> records and view
    /// side-by-side deltas for all numeric fields plus overlaid altitude profiles.
    /// </summary>
    public class JournalComparisonUI : MonoBehaviour
    {
        // ── Inspector — Panel ─────────────────────────────────────────────────────
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button     closeButton;

        // ── Inspector — Slot A ────────────────────────────────────────────────────
        [Header("Flight Slot A")]
        [SerializeField] private TMP_Dropdown slotADropdown;
        [SerializeField] private TMP_Text     slotADateText;
        [SerializeField] private TMP_Text     slotARouteText;
        [SerializeField] private TMP_Text     slotADurationText;
        [SerializeField] private TMP_Text     slotADistanceText;
        [SerializeField] private TMP_Text     slotAAltitudeText;
        [SerializeField] private TMP_Text     slotASpeedText;
        [SerializeField] private TMP_Text     slotAXPText;

        // ── Inspector — Slot B ────────────────────────────────────────────────────
        [Header("Flight Slot B")]
        [SerializeField] private TMP_Dropdown slotBDropdown;
        [SerializeField] private TMP_Text     slotBDateText;
        [SerializeField] private TMP_Text     slotBRouteText;
        [SerializeField] private TMP_Text     slotBDurationText;
        [SerializeField] private TMP_Text     slotBDistanceText;
        [SerializeField] private TMP_Text     slotBAltitudeText;
        [SerializeField] private TMP_Text     slotBSpeedText;
        [SerializeField] private TMP_Text     slotBXPText;

        // ── Inspector — Delta row ─────────────────────────────────────────────────
        [Header("Delta Row")]
        [SerializeField] private TMP_Text deltaDurationText;
        [SerializeField] private TMP_Text deltaDistanceText;
        [SerializeField] private TMP_Text deltaAltitudeText;
        [SerializeField] private TMP_Text deltaSpeedText;
        [SerializeField] private TMP_Text deltaXPText;
        [SerializeField] private TMP_Text sameRouteText;

        // ── State ─────────────────────────────────────────────────────────────────
        private List<FlightLogEntry> _entries = new List<FlightLogEntry>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (closeButton   != null) closeButton.onClick.AddListener(Hide);
            if (slotADropdown != null) slotADropdown.onValueChanged.AddListener(_ => Refresh());
            if (slotBDropdown != null) slotBDropdown.onValueChanged.AddListener(_ => Refresh());
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows the comparison panel and populates the entry dropdowns.</summary>
        public void Show()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            PopulateDropdowns();
            Refresh();
        }

        /// <summary>Hides the comparison panel.</summary>
        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        /// <summary>
        /// Preselects the two entries to compare and opens the panel.
        /// </summary>
        public void CompareEntries(FlightLogEntry a, FlightLogEntry b)
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            PopulateDropdowns();

            int idxA = _entries.FindIndex(e => e.logId == a?.logId);
            int idxB = _entries.FindIndex(e => e.logId == b?.logId);
            if (slotADropdown != null && idxA >= 0) slotADropdown.value = idxA;
            if (slotBDropdown != null && idxB >= 0) slotBDropdown.value = idxB;

            Refresh();
        }

        // ── Internal ──────────────────────────────────────────────────────────────
        private void PopulateDropdowns()
        {
            _entries = JournalManager.Instance != null
                ? JournalManager.Instance.GetAllEntries()
                : new List<FlightLogEntry>();

            var options = new System.Collections.Generic.List<string>(_entries.Count);
            foreach (var e in _entries)
                options.Add($"{FormatDate(e.flightDate)} — {e.departureLocation} → {e.arrivalLocation}");

            if (slotADropdown != null) { slotADropdown.ClearOptions(); slotADropdown.AddOptions(options); }
            if (slotBDropdown != null) { slotBDropdown.ClearOptions(); slotBDropdown.AddOptions(options); }
        }

        private void Refresh()
        {
            FlightLogEntry a = GetSelectedEntry(slotADropdown);
            FlightLogEntry b = GetSelectedEntry(slotBDropdown);

            PopulateSlot(a, slotADateText, slotARouteText, slotADurationText, slotADistanceText, slotAAltitudeText, slotASpeedText, slotAXPText);
            PopulateSlot(b, slotBDateText, slotBRouteText, slotBDurationText, slotBDistanceText, slotBAltitudeText, slotBSpeedText, slotBXPText);
            PopulateDeltas(a, b);
        }

        private FlightLogEntry GetSelectedEntry(TMP_Dropdown dropdown)
        {
            if (dropdown == null || _entries.Count == 0) return null;
            int idx = Mathf.Clamp(dropdown.value, 0, _entries.Count - 1);
            return _entries[idx];
        }

        private static void PopulateSlot(FlightLogEntry e,
            TMP_Text date, TMP_Text route, TMP_Text duration,
            TMP_Text distance, TMP_Text altitude, TMP_Text speed, TMP_Text xp)
        {
            if (e == null) return;
            SetText(date,     FormatDate(e.flightDate));
            SetText(route,    $"{e.departureLocation} → {e.arrivalLocation}");
            SetText(duration, FormatDuration(e.durationSeconds));
            SetText(distance, $"{e.distanceKm:F1} km");
            SetText(altitude, $"{e.maxAltitudeM:F0} m");
            SetText(speed,    $"{e.maxSpeedKmh:F0} km/h");
            SetText(xp,       $"{e.xpEarned} XP");
        }

        private void PopulateDeltas(FlightLogEntry a, FlightLogEntry b)
        {
            if (a == null || b == null)
            {
                SetText(deltaDurationText, "—");
                SetText(deltaDistanceText, "—");
                SetText(deltaAltitudeText, "—");
                SetText(deltaSpeedText,    "—");
                SetText(deltaXPText,       "—");
                SetText(sameRouteText,     string.Empty);
                return;
            }

            SetDelta(deltaDurationText, a.durationSeconds, b.durationSeconds, " s");
            SetDelta(deltaDistanceText, a.distanceKm,      b.distanceKm,      " km");
            SetDelta(deltaAltitudeText, a.maxAltitudeM,    b.maxAltitudeM,    " m");
            SetDelta(deltaSpeedText,    a.maxSpeedKmh,     b.maxSpeedKmh,     " km/h");
            SetDelta(deltaXPText,       a.xpEarned,        b.xpEarned,        " XP");

            bool sameRoute = !string.IsNullOrEmpty(a.flightPathHash)
                          && a.flightPathHash == b.flightPathHash;
            if (sameRouteText != null)
            {
                sameRouteText.text  = sameRoute ? Localize("journal_compare_same_route") : string.Empty;
                sameRouteText.color = sameRoute ? Color.green : Color.white;
            }
        }

        private static void SetDelta(TMP_Text label, float valA, float valB, string unit)
        {
            if (label == null) return;
            float delta = valA - valB;
            string sign = delta >= 0 ? "+" : string.Empty;
            label.text  = $"{sign}{delta:F1}{unit}";
            label.color = delta >= 0 ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.9f, 0.3f, 0.3f);
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null) label.text = value ?? string.Empty;
        }

        private static string FormatDate(string iso)
        {
            if (System.DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind, out System.DateTime dt))
                return dt.ToLocalTime().ToString("MMM dd, yyyy");
            return iso ?? string.Empty;
        }

        private static string FormatDuration(float seconds)
        {
            var ts = System.TimeSpan.FromSeconds(seconds);
            return ts.Hours > 0 ? $"{ts.Hours}h {ts.Minutes:D2}m" : $"{ts.Minutes}m {ts.Seconds:D2}s";
        }

        private static string Localize(string key)
        {
            var loc = LocalizationManager.Instance;
            return loc != null ? loc.GetText(key) : key;
        }
    }
}
