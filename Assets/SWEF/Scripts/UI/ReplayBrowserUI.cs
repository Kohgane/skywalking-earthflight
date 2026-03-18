using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SWEF.Replay;

namespace SWEF.UI
{
    /// <summary>
    /// Replay browser panel. Lists all saved replays with per-item actions
    /// (play, share, ghost-race, view path, delete). Supports sorting and pagination.
    /// </summary>
    public class ReplayBrowserUI : MonoBehaviour
    {
        // ── Sort mode ─────────────────────────────────────────────────────────────
        private enum ReplaySortMode
        {
            Newest          = 0,
            Oldest          = 1,
            HighestAltitude = 2,
            LongestDuration = 3,
            FastestSpeed    = 4,
        }

        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Panel")]
        [SerializeField] private GameObject replayPanel;
        [SerializeField] private Button     toggleButton;
        [SerializeField] private Button     importButton;
        [SerializeField] private Text       storageInfoText;
        [SerializeField] private Text       emptyStateText;

        [Header("List")]
        [SerializeField] private Transform  replayListContent;
        [SerializeField] private GameObject replayItemPrefab;

        [Header("Pagination")]
        [SerializeField] private int    itemsPerPage    = 8;
        [SerializeField] private Button prevPageButton;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private Text   pageIndicatorText;

        [Header("Sorting")]
        [SerializeField] private Dropdown sortDropdown;

        [Header("Dependencies")]
        [SerializeField] private ReplayFileManager  fileManager;
        [SerializeField] private ReplayShareManager shareManager;
        [SerializeField] private GhostRacer         ghostRacer;
        [SerializeField] private FlightPathRenderer pathRenderer;
        [SerializeField] private Recorder.FlightPlayback playback;

        // ── Private state ─────────────────────────────────────────────────────────
        private List<ReplayFileInfo> _allReplays  = new List<ReplayFileInfo>();
        private ReplaySortMode       _sortMode    = ReplaySortMode.Newest;
        private int                  _currentPage = 0;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (fileManager  == null) fileManager  = FindFirstObjectByType<ReplayFileManager>();
            if (shareManager == null) shareManager = FindFirstObjectByType<ReplayShareManager>();
            if (ghostRacer   == null) ghostRacer   = FindFirstObjectByType<GhostRacer>();
            if (pathRenderer == null) pathRenderer = FindFirstObjectByType<FlightPathRenderer>();
            if (playback     == null) playback     = FindFirstObjectByType<Recorder.FlightPlayback>();

            if (toggleButton != null)
                toggleButton.onClick.AddListener(TogglePanel);
            if (importButton != null)
                importButton.onClick.AddListener(OnImportPressed);
            if (prevPageButton != null)
                prevPageButton.onClick.AddListener(PreviousPage);
            if (nextPageButton != null)
                nextPageButton.onClick.AddListener(NextPage);
            if (sortDropdown != null)
                sortDropdown.onValueChanged.AddListener(OnSortChanged);

            if (fileManager != null)
            {
                fileManager.OnReplaySaved   += _ => Refresh();
                fileManager.OnReplayDeleted += _ => Refresh();
            }

            // Start hidden
            if (replayPanel != null) replayPanel.SetActive(false);
        }

        private void OnEnable()
        {
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Reloads the replay list from disk and rebuilds the UI.</summary>
        public void Refresh()
        {
            if (fileManager == null) return;

            _allReplays  = fileManager.ListReplays();
            _currentPage = 0;

            SortReplays();
            BuildPage();
            UpdateStorageInfo();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void TogglePanel()
        {
            if (replayPanel == null) return;
            bool active = !replayPanel.activeSelf;
            replayPanel.SetActive(active);
            if (active) Refresh();
        }

        private void OnImportPressed()
        {
            if (shareManager == null) return;
            var imported = shareManager.ImportFromClipboard();
            if (imported != null && fileManager != null)
            {
                fileManager.SaveReplay(imported);
                Refresh();
            }
        }

        private void OnSortChanged(int index)
        {
            _sortMode    = (ReplaySortMode)index;
            _currentPage = 0;
            SortReplays();
            BuildPage();
        }

        private void SortReplays()
        {
            switch (_sortMode)
            {
                case ReplaySortMode.Newest:
                    _allReplays.Sort((a, b) => string.Compare(b.createdAt, a.createdAt, StringComparison.Ordinal));
                    break;
                case ReplaySortMode.Oldest:
                    _allReplays.Sort((a, b) => string.Compare(a.createdAt, b.createdAt, StringComparison.Ordinal));
                    break;
                case ReplaySortMode.HighestAltitude:
                    _allReplays.Sort((a, b) => b.maxAltitudeM.CompareTo(a.maxAltitudeM));
                    break;
                case ReplaySortMode.LongestDuration:
                    _allReplays.Sort((a, b) => b.durationSec.CompareTo(a.durationSec));
                    break;
                case ReplaySortMode.FastestSpeed:
                    _allReplays.Sort((a, b) => b.maxSpeedMps.CompareTo(a.maxSpeedMps));
                    break;
            }
        }

        private void BuildPage()
        {
            // Clear existing items
            if (replayListContent != null)
            {
                foreach (Transform child in replayListContent)
                    Destroy(child.gameObject);
            }

            bool empty = _allReplays.Count == 0;
            if (emptyStateText != null) emptyStateText.gameObject.SetActive(empty);
            if (empty) { UpdatePagination(); return; }

            int startIdx = _currentPage * itemsPerPage;
            int endIdx   = Mathf.Min(startIdx + itemsPerPage, _allReplays.Count);

            for (int i = startIdx; i < endIdx; i++)
            {
                var info = _allReplays[i];
                SpawnItem(info);
            }

            UpdatePagination();
        }

        private void SpawnItem(ReplayFileInfo info)
        {
            if (replayItemPrefab == null || replayListContent == null) return;

            var go = Instantiate(replayItemPrefab, replayListContent);
            go.name = $"ReplayItem_{info.replayId}";

            // Wire up text fields by name convention
            SetChildText(go, "NameText",     info.playerName ?? "Unknown");
            SetChildText(go, "DateText",     FormatDate(info.createdAt));
            SetChildText(go, "DurationText", FormatDuration(info.durationSec));
            SetChildText(go, "AltitudeText", $"{info.maxAltitudeM / 1000f:F1} km");

            // Wire buttons by name convention
            string id = info.replayId; // capture for lambda
            WireButton(go, "PlayButton",      () => OnPlayItem(id));
            WireButton(go, "ShareButton",     () => OnShareItem(id));
            WireButton(go, "GhostRaceButton", () => OnGhostRaceItem(id));
            WireButton(go, "DeleteButton",    () => OnDeleteItem(id, go));
            WireButton(go, "ViewPathButton",  () => OnViewPathItem(id));
        }

        // ── Item actions ──────────────────────────────────────────────────────────

        private void OnPlayItem(string replayId)
        {
            if (playback == null || fileManager == null) return;
            var data = fileManager.LoadReplay(replayId);
            if (data == null) return;
            // FlightPlayback works off FlightRecorder frames; for replay-from-file, a
            // future phase will add a LoadReplayIntoPlayback helper. For now, log intent.
            Debug.Log($"[SWEF] ReplayBrowserUI: Play requested for '{replayId}'.");
        }

        private void OnShareItem(string replayId)
        {
            if (shareManager == null) return;
            shareManager.ShareReplay(replayId);
        }

        private void OnGhostRaceItem(string replayId)
        {
            if (ghostRacer == null || fileManager == null) return;
            var data = fileManager.LoadReplay(replayId);
            if (data == null) return;
            ghostRacer.StartRace(data);
            if (replayPanel != null) replayPanel.SetActive(false);
        }

        private void OnDeleteItem(string replayId, GameObject itemGo)
        {
            if (fileManager == null) return;
            // Confirm via a simple dialog (Unity does not have a built-in dialog;
            // we rely on the existing ErrorHandler pattern and delete directly).
            bool deleted = fileManager.DeleteReplay(replayId);
            if (deleted && itemGo != null) Destroy(itemGo);
        }

        private void OnViewPathItem(string replayId)
        {
            if (pathRenderer == null || fileManager == null) return;
            var data = fileManager.LoadReplay(replayId);
            if (data == null) return;
            pathRenderer.RenderPath(data);
            pathRenderer.SetVisible(true);
        }

        // ── Pagination ────────────────────────────────────────────────────────────

        private void PreviousPage()
        {
            if (_currentPage > 0) { _currentPage--; BuildPage(); }
        }

        private void NextPage()
        {
            int maxPage = Mathf.Max(0, (_allReplays.Count - 1) / itemsPerPage);
            if (_currentPage < maxPage) { _currentPage++; BuildPage(); }
        }

        private void UpdatePagination()
        {
            int maxPage = _allReplays.Count > 0
                ? (_allReplays.Count - 1) / itemsPerPage
                : 0;

            if (prevPageButton != null)  prevPageButton.interactable  = _currentPage > 0;
            if (nextPageButton != null)  nextPageButton.interactable  = _currentPage < maxPage;
            if (pageIndicatorText != null)
                pageIndicatorText.text = _allReplays.Count > 0
                    ? $"{_currentPage + 1} / {maxPage + 1}"
                    : "–";
        }

        private void UpdateStorageInfo()
        {
            if (storageInfoText == null || fileManager == null) return;
            int  count = _allReplays.Count;
            long bytes = fileManager.GetTotalReplaySizeBytes();
            storageInfoText.text = $"{count} replay{(count == 1 ? "" : "s")} • {FormatBytes(bytes)}";
        }

        // ── Formatting helpers ────────────────────────────────────────────────────

        private static string FormatDuration(float seconds)
        {
            int m = (int)seconds / 60;
            int s = (int)seconds % 60;
            return m > 0 ? $"{m}m {s:00}s" : $"{s}s";
        }

        private static string FormatDate(string iso8601)
        {
            if (DateTime.TryParse(iso8601, out DateTime dt))
                return dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            return iso8601 ?? "–";
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024)         return $"{bytes} B";
            if (bytes < 1024 * 1024)  return $"{bytes / 1024.0:F1} KB";
            return                           $"{bytes / (1024.0 * 1024.0):F1} MB";
        }

        // ── Prefab helper utilities ───────────────────────────────────────────────

        private static void SetChildText(GameObject parent, string childName, string value)
        {
            var t = parent.transform.Find(childName);
            if (t != null)
            {
                var txt = t.GetComponent<Text>();
                if (txt != null) txt.text = value;
            }
        }

        private static void WireButton(GameObject parent, string childName, UnityEngine.Events.UnityAction action)
        {
            var t = parent.transform.Find(childName);
            if (t != null)
            {
                var btn = t.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(action);
            }
        }
    }
}
