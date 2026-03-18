using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.UI
{
    using SWEF.Core;

    /// <summary>
    /// Paged flight-journal viewer.
    /// Shows 5 entries per page with date, max altitude, duration, and distance.
    /// Also displays cumulative stats: total flights, total time, max altitude, total distance.
    /// Requires a <see cref="FlightJournal"/> and <see cref="SaveManager"/> in the scene.
    /// </summary>
    public class FlightJournalUI : MonoBehaviour
    {
        // ── Config ───────────────────────────────────────────────────────────
        private const int EntriesPerPage = 5;

        // ── Panel ────────────────────────────────────────────────────────────
        [Header("Panel")]
        [SerializeField] private GameObject journalPanel;

        // ── Entry rows ───────────────────────────────────────────────────────
        [Header("Entry Rows (5 slots)")]
        [Tooltip("Assign exactly 5 row GameObjects. Each row needs a Text[] child layout: [0]=date, [1]=maxAlt, [2]=duration, [3]=distance.")]
        [SerializeField] private GameObject[] entryRows = new GameObject[EntriesPerPage];
        private Text[][]     rowTexts;   // resolved at runtime
        [SerializeField] private Button[]     deleteButtons = new Button[EntriesPerPage];

        // ── Stats ────────────────────────────────────────────────────────────
        [Header("Stats Labels")]
        [SerializeField] private Text totalFlightsText;
        [SerializeField] private Text totalTimeText;
        [SerializeField] private Text maxAltitudeText;
        [SerializeField] private Text totalDistanceText;

        // ── Paging ───────────────────────────────────────────────────────────
        [Header("Paging")]
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text   pageLabel;

        // ── Internal ─────────────────────────────────────────────────────────
        private FlightJournal _journal;
        private SaveManager   _save;
        private int           _currentPage;
        private List<JournalEntry> _entries = new List<JournalEntry>();

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            _journal = FindFirstObjectByType<FlightJournal>();
            _save    = FindFirstObjectByType<SaveManager>();

            if (prevButton != null) prevButton.onClick.AddListener(OnPrev);
            if (nextButton != null) nextButton.onClick.AddListener(OnNext);

            // Wire delete buttons
            for (int i = 0; i < EntriesPerPage && i < deleteButtons.Length; i++)
            {
                int captured = i;
                if (deleteButtons[i] != null)
                    deleteButtons[i].onClick.AddListener(() => OnDeleteEntry(captured));
            }

            // Resolve Text children in each row
            rowTexts = new Text[EntriesPerPage][];
            for (int i = 0; i < EntriesPerPage && i < entryRows.Length; i++)
            {
                if (entryRows[i] != null)
                    rowTexts[i] = entryRows[i].GetComponentsInChildren<Text>(includeInactive: true);
            }

            if (journalPanel != null)
                journalPanel.SetActive(false);
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Opens the journal panel and refreshes its content.</summary>
        public void Open()
        {
            if (journalPanel != null)
                journalPanel.SetActive(true);
            _currentPage = 0;
            Refresh();
        }

        /// <summary>Closes the journal panel.</summary>
        public void Close()
        {
            if (journalPanel != null)
                journalPanel.SetActive(false);
        }

        /// <summary>Toggles the journal panel open/closed.</summary>
        public void Toggle()
        {
            if (journalPanel != null && journalPanel.activeSelf)
                Close();
            else
                Open();
        }

        // ── Internal helpers ─────────────────────────────────────────────────

        private void Refresh()
        {
            _entries = _journal != null
                ? _journal.GetAllEntries()
                : new List<JournalEntry>();

            RefreshStats();
            RefreshPage();
        }

        private void RefreshStats()
        {
            if (_save == null) return;

            if (totalFlightsText  != null) totalFlightsText.text  = $"Flights: {_save.Data.totalFlights}";
            if (totalTimeText     != null) totalTimeText.text     = $"Total Time: {FormatDuration(_save.Data.totalFlightTimeSec)}";
            if (maxAltitudeText   != null) maxAltitudeText.text   = $"Max Altitude: {_save.Data.allTimeMaxAltitudeKm:F1} km";
            if (totalDistanceText != null) totalDistanceText.text = $"Total Distance: {_save.Data.totalDistanceKm:F1} km";
        }

        private void RefreshPage()
        {
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)_entries.Count / EntriesPerPage));
            _currentPage = Mathf.Clamp(_currentPage, 0, totalPages - 1);

            int start = _currentPage * EntriesPerPage;

            for (int i = 0; i < EntriesPerPage; i++)
            {
                int entryIdx = start + i;
                bool visible = entryIdx < _entries.Count;

                if (entryRows != null && i < entryRows.Length && entryRows[i] != null)
                    entryRows[i].SetActive(visible);

                if (deleteButtons != null && i < deleteButtons.Length && deleteButtons[i] != null)
                    deleteButtons[i].gameObject.SetActive(visible);

                if (!visible) continue;

                var e = _entries[entryIdx];
                if (rowTexts != null && i < rowTexts.Length && rowTexts[i] != null && rowTexts[i].Length >= 4)
                {
                    // Parse recordedAt for a short display date
                    string date = e.recordedAt.Length >= 10 ? e.recordedAt.Substring(0, 10) : e.recordedAt;
                    rowTexts[i][0].text = date;
                    rowTexts[i][1].text = $"{e.maxAltitudeKm:F1} km";
                    rowTexts[i][2].text = FormatDuration(e.durationSec);
                    rowTexts[i][3].text = $"{e.distanceKm:F1} km";
                }
            }

            if (pageLabel != null)
                pageLabel.text = $"{_currentPage + 1} / {totalPages}";

            if (prevButton != null) prevButton.interactable = _currentPage > 0;
            if (nextButton != null) nextButton.interactable = _currentPage < totalPages - 1;
        }

        private void OnPrev()
        {
            _currentPage--;
            RefreshPage();
        }

        private void OnNext()
        {
            _currentPage++;
            RefreshPage();
        }

        private void OnDeleteEntry(int rowIndex)
        {
            int entryIdx = _currentPage * EntriesPerPage + rowIndex;
            if (_journal == null || entryIdx >= _entries.Count) return;

            _journal.DeleteEntry(_entries[entryIdx].id);
            Refresh();
        }

        private static string FormatDuration(float totalSec)
        {
            int h = (int)(totalSec / 3600f);
            int m = (int)((totalSec % 3600f) / 60f);
            int s = (int)(totalSec % 60f);
            return h > 0 ? $"{h}h {m:D2}m" : $"{m}m {s:D2}s";
        }
    }
}
