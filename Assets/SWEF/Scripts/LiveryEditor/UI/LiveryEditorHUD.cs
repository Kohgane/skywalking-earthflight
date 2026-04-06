// LiveryEditorHUD.cs — Phase 115: Advanced Aircraft Livery Editor
// Compact HUD for quick livery switching during flight.
// Namespace: SWEF.LiveryEditor

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Compact heads-up display overlay for in-flight livery switching.
    /// Displays the active livery name and provides a quick-swap wheel for
    /// recently saved liveries without opening the full editor.
    /// </summary>
    public class LiveryEditorHUD : MonoBehaviour
    {
        // ── Inspector references ──────────────────────────────────────────────────
        [Header("Sub-System References")]
        [SerializeField] private LiveryEditorManager manager;

        [Header("HUD Settings")]
        [Tooltip("Maximum number of liveries shown in the quick-swap list.")]
        [SerializeField, Range(1, 10)] private int maxQuickSlots = 5;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when the user selects a quick-swap livery slot.</summary>
        public event Action<int> OnSlotSelected;

        /// <summary>Raised when the user opens the full editor from the HUD.</summary>
        public event Action OnOpenEditorRequested;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Whether the HUD is currently visible.</summary>
        public bool IsVisible { get; private set; }

        /// <summary>Index of the currently active quick-slot (-1 = none).</summary>
        public int ActiveSlotIndex { get; private set; } = -1;

        // ── Internal state ────────────────────────────────────────────────────────
        private readonly List<LiverySaveData> _quickSlots = new List<LiverySaveData>();

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows or hides the HUD.</summary>
        public void SetVisible(bool visible) => IsVisible = visible;

        /// <summary>Refreshes the quick-slot list from the manager's saved liveries.</summary>
        public void RefreshSlots()
        {
            _quickSlots.Clear();
            if (manager == null) return;

            var all = manager.GetAllLiveries();
            int take = Mathf.Min(maxQuickSlots, all.Count);
            for (int i = 0; i < take; i++) _quickSlots.Add(all[i]);
        }

        /// <summary>Selects and applies the livery in the given quick-slot.</summary>
        /// <param name="slotIndex">Zero-based slot index.</param>
        public void SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _quickSlots.Count) return;
            ActiveSlotIndex = slotIndex;
            manager?.LoadLivery(_quickSlots[slotIndex]);
            OnSlotSelected?.Invoke(slotIndex);
        }

        /// <summary>Requests the full editor to be opened.</summary>
        public void OpenEditor()
        {
            manager?.SetEditorOpen(true);
            OnOpenEditorRequested?.Invoke();
        }

        /// <summary>Returns the livery at a given quick-slot index, or <c>null</c>.</summary>
        public LiverySaveData GetSlotLivery(int index) =>
            (index >= 0 && index < _quickSlots.Count) ? _quickSlots[index] : null;

        /// <summary>Number of populated quick-slots.</summary>
        public int SlotCount => _quickSlots.Count;
    }
}
