using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.MusicPlayer
{
    /// <summary>
    /// In-app lyrics editor that lets users paste, type, or tap-timestamp lyric lines.
    /// <para>
    /// Workflow:
    /// <list type="number">
    ///   <item>Paste or type plain-text lyrics into the text field.</item>
    ///   <item>Enter Tap Mode and tap <c>Tap Timestamp</c> while the song plays to assign
    ///         the current playback time to each line sequentially.</item>
    ///   <item>Fine-tune individual timestamps with the ±0.1 s buttons.</item>
    ///   <item>Preview synchronised lyrics in real-time via <see cref="LyricsDisplayUI"/>.</item>
    ///   <item>Save the edited lyrics through <see cref="LyricsDatabase"/>.</item>
    /// </list>
    /// </para>
    /// <para>The undo/redo stack retains up to <see cref="MaxUndoSteps"/> snapshots.</para>
    /// </summary>
    public class LyricsEditorUI : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────────

        private const int   MaxUndoSteps         = 20;
        private const float TimestampNudgeSeconds = 0.1f;
        private const string LogTag              = "[SWEF][LyricsEditorUI]";

        // ── Inspector — Panels ────────────────────────────────────────────────────

        [Header("Panels")]
        [Tooltip("Root GameObject of the lyrics editor panel.")]
        [SerializeField] private GameObject editorPanel;

        // ── Inspector — Input ──────────────────────────────────────────────────────

        [Header("Text Input")]
        [Tooltip("InputField where users type/paste plain-text lyrics.")]
        [SerializeField] private InputField lyricsInputField;

        // ── Inspector — Line Editor ───────────────────────────────────────────────

        [Header("Line Editor")]
        [Tooltip("ScrollRect containing the per-line editing rows.")]
        [SerializeField] private ScrollRect lineEditorScrollRect;

        [Tooltip("Content RectTransform inside lineEditorScrollRect.")]
        [SerializeField] private RectTransform lineEditorContent;

        [Tooltip("Prefab for a single editable line row. Must contain: Text (line text), Text (timestamp), Button (+0.1s), Button (-0.1s).")]
        [SerializeField] private GameObject lineRowPrefab;

        // ── Inspector — Tap Mode ──────────────────────────────────────────────────

        [Header("Tap Mode")]
        [Tooltip("Button that enters tap-timestamp mode.")]
        [SerializeField] private Button tapModeButton;

        [Tooltip("Button pressed during tap mode to stamp the current playback time.")]
        [SerializeField] private Button tapTimestampButton;

        [Tooltip("Label showing the current tap-mode line index.")]
        [SerializeField] private Text tapModeStatusLabel;

        // ── Inspector — Controls ──────────────────────────────────────────────────

        [Header("Controls")]
        [Tooltip("Button that imports lines from the plain-text input field.")]
        [SerializeField] private Button importTextButton;

        [Tooltip("Button that saves edited lyrics via LyricsDatabase.")]
        [SerializeField] private Button saveButton;

        [Tooltip("Button that cancels edits and closes the editor.")]
        [SerializeField] private Button cancelButton;

        [Tooltip("Undo button.")]
        [SerializeField] private Button undoButton;

        [Tooltip("Redo button.")]
        [SerializeField] private Button redoButton;

        [Tooltip("Preview toggle button — starts/stops live lyrics preview.")]
        [SerializeField] private Button previewToggleButton;

        // ── Inspector — Preview ───────────────────────────────────────────────────

        [Header("Preview")]
        [Tooltip("LyricsDisplayUI used for real-time preview while editing.")]
        [SerializeField] private LyricsDisplayUI previewDisplay;

        // ── State ─────────────────────────────────────────────────────────────────

        private List<EditableLine> _lines      = new List<EditableLine>();
        private bool               _tapMode;
        private int                _tapIndex;
        private bool               _previewActive;

        // ── Line row GameObjects (parallel to _lines) ─────────────────────────────
        private List<LineRowUI> _lineRows = new List<LineRowUI>();

        // ── Undo / Redo ───────────────────────────────────────────────────────────

        private readonly Stack<List<EditableLine>> _undoStack = new Stack<List<EditableLine>>();
        private readonly Stack<List<EditableLine>> _redoStack = new Stack<List<EditableLine>>();

        // ── Nested types ──────────────────────────────────────────────────────────

        [Serializable]
        private class EditableLine
        {
            public string text;
            public float  timestamp; // -1 = untimed

            public EditableLine(string text, float timestamp = -1f)
            {
                this.text      = text;
                this.timestamp = timestamp;
            }

            public EditableLine Clone() => new EditableLine(text, timestamp);
        }

        private class LineRowUI
        {
            public GameObject root;
            public Text       lineText;
            public Text       timestampText;
            public Button     plusButton;
            public Button     minusButton;
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (editorPanel != null)
                editorPanel.SetActive(false);
        }

        private void Start()
        {
            // Wire button callbacks
            if (importTextButton   != null) importTextButton.onClick.AddListener(OnImportText);
            if (saveButton         != null) saveButton.onClick.AddListener(OnSave);
            if (cancelButton       != null) cancelButton.onClick.AddListener(OnCancel);
            if (undoButton         != null) undoButton.onClick.AddListener(OnUndo);
            if (redoButton         != null) redoButton.onClick.AddListener(OnRedo);
            if (tapModeButton      != null) tapModeButton.onClick.AddListener(OnToggleTapMode);
            if (tapTimestampButton != null) tapTimestampButton.onClick.AddListener(OnTapTimestamp);
            if (previewToggleButton != null) previewToggleButton.onClick.AddListener(OnTogglePreview);

            RefreshUndoRedoButtons();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Opens the editor for the given track, pre-loading any existing lyrics.
        /// </summary>
        /// <param name="track">Track whose lyrics will be edited.</param>
        public void OpenForTrack(MusicTrack track)
        {
            if (track == null) return;

            _lines.Clear();
            _undoStack.Clear();
            _redoStack.Clear();
            _tapMode  = false;
            _tapIndex = 0;

            // Pre-load existing lyrics if available
            LrcData existing = LyricsDatabase.Instance != null
                ? LyricsDatabase.Instance.GetCached(track.trackId)
                : null;

            if (existing != null && existing.HasLyrics)
            {
                foreach (LrcLine ll in existing.lines)
                    _lines.Add(new EditableLine(ll.text, ll.timestamp));
            }

            BuildLineRows();
            UpdateInputFieldFromLines();

            if (editorPanel != null)
                editorPanel.SetActive(true);
        }

        /// <summary>Closes the editor panel without saving.</summary>
        public void Close()
        {
            StopPreview();
            if (editorPanel != null)
                editorPanel.SetActive(false);
        }

        // ── Button callbacks ──────────────────────────────────────────────────────

        private void OnImportText()
        {
            if (lyricsInputField == null) return;

            PushUndo();

            string rawText = lyricsInputField.text ?? string.Empty;
            string[] rawLines = rawText.Split(
                new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.RemoveEmptyEntries);

            _lines.Clear();
            foreach (string raw in rawLines)
            {
                string stripped = raw.Trim();
                if (!string.IsNullOrEmpty(stripped))
                    _lines.Add(new EditableLine(stripped));
            }

            _tapIndex = 0;
            BuildLineRows();
            RefreshUndoRedoButtons();
        }

        private void OnSave()
        {
            MusicTrack track = MusicPlayerManager.Instance?.GetCurrentTrack();
            if (track == null)
            {
                Debug.LogWarning($"{LogTag} No current track to save lyrics for.");
                return;
            }

            string lrc = BuildLrcContent();
            LyricsDatabase.Instance?.SaveUserLyrics(track.trackId, lrc);
            Debug.Log($"{LogTag} Lyrics saved for track '{track.trackId}'.");
        }

        private void OnCancel()
        {
            Close();
        }

        private void OnUndo()
        {
            if (_undoStack.Count == 0) return;

            PushRedo();
            _lines = CloneLines(_undoStack.Pop());
            BuildLineRows();
            UpdateInputFieldFromLines();
            RefreshUndoRedoButtons();
        }

        private void OnRedo()
        {
            if (_redoStack.Count == 0) return;

            PushCurrentToUndo();
            _lines = CloneLines(_redoStack.Pop());
            BuildLineRows();
            UpdateInputFieldFromLines();
            RefreshUndoRedoButtons();
        }

        private void OnToggleTapMode()
        {
            _tapMode  = !_tapMode;
            _tapIndex = 0;

            if (tapTimestampButton != null)
                tapTimestampButton.gameObject.SetActive(_tapMode);

            UpdateTapModeStatus();
        }

        private void OnTapTimestamp()
        {
            if (!_tapMode || _tapIndex >= _lines.Count) return;
            if (MusicPlayerManager.Instance == null) return;

            float timeSeconds = MusicPlayerManager.Instance.GetCurrentPlaybackTime();

            PushUndo();

            _lines[_tapIndex].timestamp = timeSeconds;
            RefreshLineRow(_tapIndex);

            _tapIndex++;
            if (_tapIndex >= _lines.Count)
            {
                _tapMode = false;
                if (tapTimestampButton != null)
                    tapTimestampButton.gameObject.SetActive(false);
            }

            UpdateTapModeStatus();
            RefreshUndoRedoButtons();
        }

        private void OnTogglePreview()
        {
            if (_previewActive)
                StopPreview();
            else
                StartPreview();
        }

        // ── Tap mode ──────────────────────────────────────────────────────────────

        private void UpdateTapModeStatus()
        {
            if (tapModeStatusLabel == null) return;
            if (!_tapMode)
            {
                tapModeStatusLabel.text = string.Empty;
                return;
            }
            tapModeStatusLabel.text = _tapIndex < _lines.Count
                ? $"Tap to stamp line {_tapIndex + 1}/{_lines.Count}: \"{_lines[_tapIndex].text}\""
                : "All lines stamped.";
        }

        // ── Line row management ───────────────────────────────────────────────────

        private void BuildLineRows()
        {
            if (lineEditorContent == null || lineRowPrefab == null) return;

            // Clear old rows
            foreach (var row in _lineRows)
                if (row.root != null) Destroy(row.root);
            _lineRows.Clear();

            for (int i = 0; i < _lines.Count; i++)
            {
                int capturedIndex = i;

                GameObject go  = Instantiate(lineRowPrefab, lineEditorContent);
                var        row = new LineRowUI { root = go };

                Text[] texts   = go.GetComponentsInChildren<Text>(true);
                Button[] btns  = go.GetComponentsInChildren<Button>(true);

                if (texts.Length >= 1) row.lineText      = texts[0];
                if (texts.Length >= 2) row.timestampText = texts[1];
                if (btns.Length  >= 1) row.plusButton    = btns[0];
                if (btns.Length  >= 2) row.minusButton   = btns[1];

                if (row.lineText      != null) row.lineText.text = _lines[i].text;
                if (row.timestampText != null) row.timestampText.text = FormatTimestamp(_lines[i].timestamp);

                if (row.plusButton != null)
                    row.plusButton.onClick.AddListener(() => NudgeTimestamp(capturedIndex, +TimestampNudgeSeconds));
                if (row.minusButton != null)
                    row.minusButton.onClick.AddListener(() => NudgeTimestamp(capturedIndex, -TimestampNudgeSeconds));

                _lineRows.Add(row);
            }
        }

        private void RefreshLineRow(int index)
        {
            if (index < 0 || index >= _lineRows.Count) return;
            LineRowUI row = _lineRows[index];
            if (row.timestampText != null)
                row.timestampText.text = FormatTimestamp(_lines[index].timestamp);
        }

        private void NudgeTimestamp(int index, float delta)
        {
            if (index < 0 || index >= _lines.Count) return;

            PushUndo();
            float current = _lines[index].timestamp;
            // If untimed (−1), start from 0 when nudging forward; clamp to 0 otherwise
            _lines[index].timestamp = current < 0f
                ? (delta > 0f ? delta : 0f)
                : Mathf.Max(0f, current + delta);
            RefreshLineRow(index);
            RefreshUndoRedoButtons();

            if (_previewActive)
                PushPreviewUpdate();
        }

        private void UpdateInputFieldFromLines()
        {
            if (lyricsInputField == null) return;
            var sb = new System.Text.StringBuilder();
            foreach (var l in _lines)
            {
                sb.AppendLine(l.text);
            }
            lyricsInputField.text = sb.ToString().TrimEnd();
        }

        // ── Preview ───────────────────────────────────────────────────────────────

        private void StartPreview()
        {
            if (previewDisplay == null) return;
            _previewActive = true;
            PushPreviewUpdate();
            previewDisplay.SetMode(LyricsDisplayMode.Scroll);
        }

        private void StopPreview()
        {
            if (previewDisplay == null) return;
            _previewActive = false;
            previewDisplay.SetMode(LyricsDisplayMode.Hidden);
        }

        private void PushPreviewUpdate()
        {
            if (!_previewActive || previewDisplay == null) return;
            LrcData previewData = BuildLrcData();
            previewDisplay.BuildScrollLines(previewData);
        }

        // ── LRC serialisation ─────────────────────────────────────────────────────

        private string BuildLrcContent()
        {
            var sb = new System.Text.StringBuilder();

            MusicTrack track = MusicPlayerManager.Instance?.GetCurrentTrack();
            if (track != null)
            {
                if (!string.IsNullOrEmpty(track.title))  sb.AppendLine($"[ti:{track.title}]");
                if (!string.IsNullOrEmpty(track.artist)) sb.AppendLine($"[ar:{track.artist}]");
                if (!string.IsNullOrEmpty(track.album))  sb.AppendLine($"[al:{track.album}]");
            }

            foreach (var line in _lines)
            {
                if (line.timestamp >= 0f)
                {
                    int   totalSeconds = Mathf.FloorToInt(line.timestamp);
                    int   minutes      = totalSeconds / 60;
                    int   seconds      = totalSeconds % 60;
                    int   hundredths   = Mathf.RoundToInt((line.timestamp - totalSeconds) * 100);
                    sb.AppendLine($"[{minutes:D2}:{seconds:D2}.{hundredths:D2}]{line.text}");
                }
                else
                {
                    sb.AppendLine(line.text);
                }
            }

            return sb.ToString();
        }

        private LrcData BuildLrcData()
        {
            var data = new LrcData();
            foreach (var el in _lines)
            {
                if (el.timestamp >= 0f)
                    data.lines.Add(new LrcLine { timestamp = el.timestamp, text = el.text });
            }
            data.lines.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));
            return data;
        }

        // ── Undo / Redo helpers ───────────────────────────────────────────────────

        private void PushUndo()
        {
            PushCurrentToUndo();
            _redoStack.Clear();
        }

        private void PushCurrentToUndo()
        {
            if (_undoStack.Count >= MaxUndoSteps)
            {
                // Convert to list to trim oldest entry (Stack has no RemoveLast)
                var arr = _undoStack.ToArray();
                _undoStack.Clear();
                for (int i = arr.Length - 2; i >= 0; i--)
                    _undoStack.Push(arr[i]);
            }
            _undoStack.Push(CloneLines(_lines));
        }

        private void PushRedo()
        {
            _redoStack.Push(CloneLines(_lines));
        }

        private void RefreshUndoRedoButtons()
        {
            if (undoButton != null) undoButton.interactable = _undoStack.Count > 0;
            if (redoButton != null) redoButton.interactable = _redoStack.Count > 0;
        }

        private static List<EditableLine> CloneLines(List<EditableLine> src)
        {
            var copy = new List<EditableLine>(src.Count);
            foreach (var l in src) copy.Add(l.Clone());
            return copy;
        }

        // ── Formatting ────────────────────────────────────────────────────────────

        private static string FormatTimestamp(float seconds)
        {
            if (seconds < 0f) return "--:--.-";
            int   total      = Mathf.FloorToInt(seconds);
            int   mins       = total / 60;
            int   secs       = total % 60;
            int   hundredths = Mathf.RoundToInt((seconds - total) * 100);
            return $"{mins:D2}:{secs:D2}.{hundredths:D2}";
        }
    }
}
