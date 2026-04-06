// LayerHistoryController.cs — Phase 115: Advanced Aircraft Livery Editor
// Undo/redo system: action history stack, snapshot-based undo for complex operations.
// Namespace: SWEF.LiveryEditor

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Manages undo/redo history for the livery editor.
    /// Stores <see cref="LayerSnapshot"/> entries for lightweight operations and
    /// full-texture snapshots for complex ones (merge, filter).
    /// </summary>
    public class LayerHistoryController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [SerializeField] private LiveryEditorConfig config;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised after an undo operation completes.</summary>
        public event Action<LayerSnapshot> OnUndone;

        /// <summary>Raised after a redo operation completes.</summary>
        public event Action<LayerSnapshot> OnRedone;

        // ── Internal state ────────────────────────────────────────────────────────
        private readonly List<LayerSnapshot> _history  = new List<LayerSnapshot>();
        private int _cursor = -1; // Points to the current state

        // ── Public properties ─────────────────────────────────────────────────────
        /// <summary>Whether there is at least one action available to undo.</summary>
        public bool CanUndo => _cursor > 0;

        /// <summary>Whether there is at least one action available to redo.</summary>
        public bool CanRedo => _cursor < _history.Count - 1;

        /// <summary>Number of entries currently in the history stack.</summary>
        public int HistoryCount => _history.Count;

        // ── Record ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Records a new state snapshot.  Clears any redo entries above the
        /// current cursor position before adding the new entry.
        /// </summary>
        /// <param name="layerId">ID of the layer being recorded.</param>
        /// <param name="serializedState">JSON or raw string representing the layer state.</param>
        public void Record(string layerId, string serializedState)
        {
            int maxDepth = config != null ? config.UndoHistoryDepth : 50;

            // Discard redo future.
            if (_cursor < _history.Count - 1)
                _history.RemoveRange(_cursor + 1, _history.Count - _cursor - 1);

            _history.Add(LayerSnapshot.Create(layerId, serializedState));
            _cursor = _history.Count - 1;

            // Trim oldest entries if over the limit.
            while (_history.Count > maxDepth)
            {
                _history.RemoveAt(0);
                _cursor = Mathf.Max(0, _cursor - 1);
            }
        }

        // ── Undo / Redo ───────────────────────────────────────────────────────────

        /// <summary>
        /// Moves back one step in history and returns the snapshot that should be restored.
        /// Returns <c>null</c> if there is nothing to undo.
        /// </summary>
        public LayerSnapshot Undo()
        {
            if (!CanUndo) return null;
            _cursor--;
            var snap = _history[_cursor];
            OnUndone?.Invoke(snap);
            return snap;
        }

        /// <summary>
        /// Moves forward one step in history and returns the snapshot that should be applied.
        /// Returns <c>null</c> if there is nothing to redo.
        /// </summary>
        public LayerSnapshot Redo()
        {
            if (!CanRedo) return null;
            _cursor++;
            var snap = _history[_cursor];
            OnRedone?.Invoke(snap);
            return snap;
        }

        /// <summary>Clears the entire history stack and resets the cursor.</summary>
        public void Clear()
        {
            _history.Clear();
            _cursor = -1;
        }

        /// <summary>Returns the snapshot at the current cursor position, or <c>null</c>.</summary>
        public LayerSnapshot CurrentSnapshot() =>
            _cursor >= 0 && _cursor < _history.Count ? _history[_cursor] : null;
    }
}
