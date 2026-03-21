using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SWEF.Localization;
using SWEF.Replay;

namespace SWEF.Journal
{
    /// <summary>
    /// Single-flight detail view.
    /// Displays all metadata for a <see cref="FlightLogEntry"/> and allows
    /// editing notes/tags, toggling favourite, watching replay, and sharing.
    /// </summary>
    public class JournalDetailUI : MonoBehaviour
    {
        // ── Inspector — Layout ────────────────────────────────────────────────────
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;

        // ── Inspector — Info fields ───────────────────────────────────────────────
        [Header("Info Fields")]
        [SerializeField] private TMP_Text dateText;
        [SerializeField] private TMP_Text routeText;
        [SerializeField] private TMP_Text durationText;
        [SerializeField] private TMP_Text distanceText;
        [SerializeField] private TMP_Text maxAltText;
        [SerializeField] private TMP_Text avgSpeedText;
        [SerializeField] private TMP_Text maxSpeedText;
        [SerializeField] private TMP_Text weatherText;
        [SerializeField] private TMP_Text atmosphereText;
        [SerializeField] private TMP_Text tourText;
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text xpText;

        // ── Inspector — Editors ───────────────────────────────────────────────────
        [Header("Notes & Tags")]
        [SerializeField] private TMP_InputField notesInput;
        [SerializeField] private TMP_InputField tagInput;
        [SerializeField] private Button         addTagButton;
        [SerializeField] private Transform      tagsContainer;
        [SerializeField] private GameObject     tagChipPrefab;

        // ── Inspector — Buttons ───────────────────────────────────────────────────
        [Header("Buttons")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button favoriteButton;
        [SerializeField] private Button watchReplayButton;
        [SerializeField] private Button shareButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Image  favoriteIcon;

        // ── Inspector — Screenshot gallery ───────────────────────────────────────
        [Header("Screenshot Gallery")]
        [SerializeField] private ScrollRect screenshotScrollRect;
        [SerializeField] private Transform  screenshotContainer;
        [SerializeField] private GameObject screenshotThumbPrefab;

        // ── Inspector — Share controller ──────────────────────────────────────────
        [Header("Share")]
        [SerializeField] private JournalShareController shareController;

        // ── State ─────────────────────────────────────────────────────────────────
        private FlightLogEntry _entry;
        private List<FlightLogEntry> _allEntries;
        private int _currentIndex;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (backButton        != null) backButton.onClick.AddListener(Hide);
            if (prevButton        != null) prevButton.onClick.AddListener(ShowPrevious);
            if (nextButton        != null) nextButton.onClick.AddListener(ShowNext);
            if (favoriteButton    != null) favoriteButton.onClick.AddListener(ToggleFavourite);
            if (watchReplayButton != null) watchReplayButton.onClick.AddListener(WatchReplay);
            if (shareButton       != null) shareButton.onClick.AddListener(ShareEntry);
            if (deleteButton      != null) deleteButton.onClick.AddListener(DeleteEntry);
            if (addTagButton      != null) addTagButton.onClick.AddListener(AddTag);

            if (notesInput != null)
                notesInput.onEndEdit.AddListener(OnNotesSaved);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows the detail view for the specified <paramref name="entry"/>.</summary>
        public void Show(FlightLogEntry entry)
        {
            if (entry == null) return;
            _entry = entry;

            if (JournalManager.Instance != null)
            {
                _allEntries = JournalManager.Instance.GetAllEntries();
                _currentIndex = _allEntries.FindIndex(e => e.logId == entry.logId);
            }

            if (panelRoot != null) panelRoot.SetActive(true);
            Populate();
        }

        /// <summary>Hides the detail panel.</summary>
        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        // ── Internal ──────────────────────────────────────────────────────────────
        private void Populate()
        {
            if (_entry == null) return;

            SetText(dateText,       FormatDate(_entry.flightDate));
            SetText(routeText,      $"{_entry.departureLocation} → {_entry.arrivalLocation}");
            SetText(durationText,   FormatDuration(_entry.durationSeconds));
            SetText(distanceText,   $"{_entry.distanceKm:F1} km");
            SetText(maxAltText,     $"{_entry.maxAltitudeM:F0} m");
            SetText(avgSpeedText,   $"{_entry.avgSpeedKmh:F0} km/h");
            SetText(maxSpeedText,   $"{_entry.maxSpeedKmh:F0} km/h");
            SetText(weatherText,    _entry.weatherCondition);
            SetText(atmosphereText, _entry.atmosphereLayer);
            SetText(tourText,       _entry.tourName);
            SetText(rankText,       _entry.pilotRankAtTime);
            SetText(xpText,         $"+{_entry.xpEarned} XP");

            if (notesInput != null)
                notesInput.text = _entry.notes ?? string.Empty;

            UpdateFavoriteIcon();
            RebuildTags();
            RebuildScreenshots();

            bool hasReplay = !string.IsNullOrEmpty(_entry.replayFileId);
            if (watchReplayButton != null) watchReplayButton.interactable = hasReplay;

            UpdateNavButtons();
        }

        private void RebuildTags()
        {
            if (tagsContainer == null) return;
            foreach (Transform child in tagsContainer)
                Destroy(child.gameObject);

            if (_entry.tags == null) return;
            foreach (var tag in _entry.tags)
            {
                if (tagChipPrefab == null) break;
                var chip = Instantiate(tagChipPrefab, tagsContainer);
                var tmp  = chip.GetComponentInChildren<TMP_Text>();
                if (tmp != null) tmp.text = tag;

                string t = tag;
                var btn = chip.GetComponentInChildren<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => RemoveTag(t));
            }
        }

        private void RebuildScreenshots()
        {
            if (screenshotContainer == null) return;
            foreach (Transform child in screenshotContainer)
                Destroy(child.gameObject);

            if (_entry.screenshotPaths == null) return;
            foreach (var path in _entry.screenshotPaths)
            {
                if (screenshotThumbPrefab == null) break;
                var thumb = Instantiate(screenshotThumbPrefab, screenshotContainer);
                // Load texture at runtime.
                if (System.IO.File.Exists(path))
                {
                    var tex = new Texture2D(2, 2);
                    tex.LoadImage(System.IO.File.ReadAllBytes(path));
                    var img = thumb.GetComponentInChildren<RawImage>();
                    if (img != null) img.texture = tex;
                }
            }
        }

        private void OnNotesSaved(string text)
        {
            if (_entry == null) return;
            JournalManager.Instance?.UpdateEntryNotes(_entry.logId, text);
        }

        private void AddTag()
        {
            if (tagInput == null || string.IsNullOrWhiteSpace(tagInput.text)) return;
            string tag = tagInput.text.Trim().ToLowerInvariant();
            var tags = new List<string>(_entry.tags ?? Array.Empty<string>());
            if (!tags.Contains(tag))
            {
                tags.Add(tag);
                JournalManager.Instance?.UpdateEntryTags(_entry.logId, tags.ToArray());
                _entry = JournalManager.Instance?.GetEntry(_entry.logId) ?? _entry;
                tagInput.text = string.Empty;
                RebuildTags();
            }
        }

        private void RemoveTag(string tag)
        {
            if (_entry == null) return;
            var tags = new List<string>(_entry.tags ?? Array.Empty<string>());
            tags.Remove(tag);
            JournalManager.Instance?.UpdateEntryTags(_entry.logId, tags.ToArray());
            _entry = JournalManager.Instance?.GetEntry(_entry.logId) ?? _entry;
            RebuildTags();
        }

        private void ToggleFavourite()
        {
            if (_entry == null) return;
            JournalManager.Instance?.ToggleFavorite(_entry.logId);
            _entry = JournalManager.Instance?.GetEntry(_entry.logId) ?? _entry;
            UpdateFavoriteIcon();
        }

        private void UpdateFavoriteIcon()
        {
            if (favoriteIcon != null)
                favoriteIcon.color = (_entry != null && _entry.isFavorite) ? Color.yellow : Color.grey;
        }

        private void WatchReplay()
        {
            if (_entry == null || string.IsNullOrEmpty(_entry.replayFileId)) return;
            var rfm = FindFirstObjectByType<ReplayFileManager>();
            if (rfm == null)
            {
                Debug.LogWarning("[SWEF] JournalDetailUI: ReplayFileManager not found.");
                return;
            }
            var replayData = rfm.LoadReplay(_entry.replayFileId);
            if (replayData == null)
            {
                Debug.LogWarning($"[SWEF] JournalDetailUI: Replay '{_entry.replayFileId}' could not be loaded.");
                return;
            }
            // Start ghost race via the GhostRacer system.
            var ghost = FindFirstObjectByType<SWEF.Replay.GhostRacer>();
            if (ghost != null)
                ghost.StartRace(replayData);
            else
                Debug.LogWarning("[SWEF] JournalDetailUI: GhostRacer not found — cannot start replay.");
        }

        private void ShareEntry()
        {
            if (_entry == null) return;
            if (shareController != null)
                shareController.Share(_entry);
        }

        private void DeleteEntry()
        {
            if (_entry == null) return;
            JournalManager.Instance?.DeleteEntry(_entry.logId);
            Hide();
        }

        private void ShowPrevious()
        {
            if (_allEntries == null || _currentIndex <= 0) return;
            _currentIndex--;
            _entry = _allEntries[_currentIndex];
            Populate();
        }

        private void ShowNext()
        {
            if (_allEntries == null || _currentIndex >= _allEntries.Count - 1) return;
            _currentIndex++;
            _entry = _allEntries[_currentIndex];
            Populate();
        }

        private void UpdateNavButtons()
        {
            if (prevButton != null) prevButton.interactable = _currentIndex > 0;
            if (nextButton != null) nextButton.interactable = _allEntries != null && _currentIndex < _allEntries.Count - 1;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────
        private static void SetText(TMP_Text label, string value)
        {
            if (label != null) label.text = value ?? string.Empty;
        }

        private static string FormatDate(string isoDate)
        {
            if (DateTime.TryParse(isoDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime dt))
                return dt.ToLocalTime().ToString("MMMM dd, yyyy  HH:mm");
            return isoDate ?? string.Empty;
        }

        private static string FormatDuration(float seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.Hours > 0
                ? $"{ts.Hours}h {ts.Minutes:D2}m {ts.Seconds:D2}s"
                : $"{ts.Minutes}m {ts.Seconds:D2}s";
        }
    }
}
