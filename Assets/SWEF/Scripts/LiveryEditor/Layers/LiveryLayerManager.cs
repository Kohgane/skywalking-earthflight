// LiveryLayerManager.cs — Phase 115: Advanced Aircraft Livery Editor
// Layer stack management: add, remove, reorder, duplicate, merge, group layers.
// Namespace: SWEF.LiveryEditor

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Manages the ordered stack of <see cref="LiveryLayer"/> objects
    /// for the active livery.  Provides add, remove, reorder, duplicate, merge,
    /// and group operations.
    /// </summary>
    public class LiveryLayerManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [SerializeField] private LiveryEditorConfig config;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised whenever the layer stack is modified.</summary>
        public event Action OnLayersChanged;

        /// <summary>Raised when the active (selected) layer changes.</summary>
        public event Action<LiveryLayer> OnActiveLayerChanged;

        // ── Internal state ────────────────────────────────────────────────────────
        private readonly List<LiveryLayer> _layers = new List<LiveryLayer>();
        private LiveryLayer _activeLayer;

        // ── Public accessors ──────────────────────────────────────────────────────
        /// <summary>Read-only ordered layer stack (index 0 = bottom).</summary>
        public IReadOnlyList<LiveryLayer> Layers => _layers.AsReadOnly();

        /// <summary>Currently selected layer in the editor.</summary>
        public LiveryLayer ActiveLayer
        {
            get => _activeLayer;
            set
            {
                _activeLayer = value;
                OnActiveLayerChanged?.Invoke(value);
            }
        }

        /// <summary>Number of layers currently in the stack.</summary>
        public int LayerCount => _layers.Count;

        // ── Add / Remove ──────────────────────────────────────────────────────────

        /// <summary>
        /// Adds a new layer at the top of the stack.
        /// </summary>
        /// <param name="name">Display name.</param>
        /// <param name="type">Layer type.</param>
        /// <returns>The newly created layer.</returns>
        public LiveryLayer AddLayer(string name, LiveryLayerType type)
        {
            int maxLayers = config != null ? config.MaxLayers : 32;
            if (_layers.Count >= maxLayers)
                throw new InvalidOperationException($"Maximum layer count ({maxLayers}) reached.");

            var layer = new LiveryLayer(name, type);
            _layers.Add(layer);
            ActiveLayer = layer;
            OnLayersChanged?.Invoke();
            return layer;
        }

        /// <summary>
        /// Inserts a layer at a specific index.
        /// </summary>
        /// <param name="layer">Layer to insert.</param>
        /// <param name="index">Zero-based insertion index.</param>
        public void InsertLayer(LiveryLayer layer, int index)
        {
            if (layer == null) throw new ArgumentNullException(nameof(layer));
            index = Mathf.Clamp(index, 0, _layers.Count);
            _layers.Insert(index, layer);
            OnLayersChanged?.Invoke();
        }

        /// <summary>
        /// Removes a layer from the stack by reference.
        /// </summary>
        /// <param name="layer">Layer to remove.</param>
        /// <returns><c>true</c> if found and removed.</returns>
        public bool RemoveLayer(LiveryLayer layer)
        {
            if (!_layers.Remove(layer)) return false;
            if (_activeLayer == layer)
                ActiveLayer = _layers.Count > 0 ? _layers[^1] : null;
            OnLayersChanged?.Invoke();
            return true;
        }

        // ── Reorder ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Moves a layer to a new position in the stack.
        /// </summary>
        /// <param name="layer">Layer to move.</param>
        /// <param name="newIndex">Target zero-based index.</param>
        public void MoveLayer(LiveryLayer layer, int newIndex)
        {
            int current = _layers.IndexOf(layer);
            if (current < 0) return;
            _layers.RemoveAt(current);
            newIndex = Mathf.Clamp(newIndex, 0, _layers.Count);
            _layers.Insert(newIndex, layer);
            OnLayersChanged?.Invoke();
        }

        /// <summary>Moves the given layer one position toward the top (higher index).</summary>
        public void MoveLayerUp(LiveryLayer layer)
        {
            int idx = _layers.IndexOf(layer);
            if (idx < _layers.Count - 1) MoveLayer(layer, idx + 1);
        }

        /// <summary>Moves the given layer one position toward the bottom (lower index).</summary>
        public void MoveLayerDown(LiveryLayer layer)
        {
            int idx = _layers.IndexOf(layer);
            if (idx > 0) MoveLayer(layer, idx - 1);
        }

        // ── Duplicate ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Duplicates a layer and inserts the copy directly above the original.
        /// </summary>
        /// <param name="layer">Layer to duplicate.</param>
        /// <returns>The new duplicate layer.</returns>
        public LiveryLayer DuplicateLayer(LiveryLayer layer)
        {
            int idx = _layers.IndexOf(layer);
            if (idx < 0) throw new ArgumentException("Layer not found in stack.", nameof(layer));

            var copy = layer.Duplicate();
            _layers.Insert(idx + 1, copy);
            ActiveLayer = copy;
            OnLayersChanged?.Invoke();
            return copy;
        }

        // ── Group ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Assigns all specified layers to a shared group identifier.
        /// </summary>
        /// <param name="layers">Layers to group.</param>
        /// <param name="groupId">Group identifier string.</param>
        public void GroupLayers(IEnumerable<LiveryLayer> layers, string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId)) throw new ArgumentException("Group id cannot be empty.");
            foreach (var l in layers) l.GroupId = groupId;
            OnLayersChanged?.Invoke();
        }

        /// <summary>Removes the group assignment from a layer.</summary>
        public void UngroupLayer(LiveryLayer layer)
        {
            if (layer == null) return;
            layer.GroupId = null;
            OnLayersChanged?.Invoke();
        }

        // ── Merge ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Merges a layer downward into the layer immediately below it.
        /// The source layer is removed and the destination layer's texture is updated.
        /// </summary>
        /// <param name="layer">Layer to merge down.</param>
        public void MergeDown(LiveryLayer layer)
        {
            int idx = _layers.IndexOf(layer);
            if (idx <= 0) return; // Nothing below

            var below = _layers[idx - 1];
            if (below.IsLocked) return;

            // Apply simple normal blend onto the layer below (pixel-level).
            LayerBlender.BlendOnto(below.LayerTexture, layer.LayerTexture, layer.Opacity, layer.BlendMode);
            _layers.RemoveAt(idx);
            ActiveLayer = below;
            OnLayersChanged?.Invoke();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>Returns the zero-based index of a layer, or -1 if not found.</summary>
        public int IndexOf(LiveryLayer layer) => _layers.IndexOf(layer);

        /// <summary>Clears all layers from the stack.</summary>
        public void ClearAll()
        {
            _layers.Clear();
            _activeLayer = null;
            OnLayersChanged?.Invoke();
        }
    }
}
