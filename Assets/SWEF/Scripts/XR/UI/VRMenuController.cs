// VRMenuController.cs — Phase 112: VR/XR Flight Experience
// Radial menu system activated by palm-up gesture, gaze-based selection.
// Namespace: SWEF.XR

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.XR
{
    /// <summary>A single item in the VR radial menu.</summary>
    [Serializable]
    public class VRRadialMenuItem
    {
        /// <summary>Display label for this menu item.</summary>
        public string Label;
        /// <summary>Icon sprite for this menu item.</summary>
        public Sprite Icon;
        /// <summary>Callback invoked when this item is selected.</summary>
        public Action OnSelected;
    }

    /// <summary>
    /// Radial menu system for VR. Activated when the player holds their palm
    /// up (OpenPalm gesture). Items are arranged in a circle around the palm
    /// and selected by gazing at them or pointing with the other hand.
    /// </summary>
    public class VRMenuController : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Layout")]
        [SerializeField] private float menuRadius         = 0.15f;
        [SerializeField] private float gazeSelectDuration = 0.8f;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Whether the radial menu is currently open.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>Index of the currently highlighted menu item, or -1.</summary>
        public int HighlightedIndex { get; private set; } = -1;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when the menu opens.</summary>
        public event Action OnMenuOpened;

        /// <summary>Fired when the menu closes.</summary>
        public event Action OnMenuClosed;

        /// <summary>Fired when a menu item is confirmed. Arg: item label.</summary>
        public event Action<string> OnItemSelected;

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly List<VRRadialMenuItem> _items = new List<VRRadialMenuItem>();
        private float _gazeTimer;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            var recognizer = GetComponent<HandGestureRecognizer>();
            if (recognizer != null)
                recognizer.OnGestureStarted += HandleGesture;
        }

        private void Update()
        {
            if (!IsOpen) return;
            UpdateGazeSelection();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Registers a menu item. Items are added in order around the radial ring.</summary>
        public void AddMenuItem(VRRadialMenuItem item)
        {
            if (item != null) _items.Add(item);
        }

        /// <summary>Clears all registered menu items.</summary>
        public void ClearMenuItems() => _items.Clear();

        /// <summary>Opens the radial menu at the specified world position.</summary>
        public void OpenMenu(Vector3 worldPosition)
        {
            if (IsOpen) return;
            IsOpen           = true;
            HighlightedIndex = -1;
            _gazeTimer       = 0f;
            transform.position = worldPosition;
            OnMenuOpened?.Invoke();
        }

        /// <summary>Closes the radial menu without selecting anything.</summary>
        public void CloseMenu()
        {
            if (!IsOpen) return;
            IsOpen = false;
            OnMenuClosed?.Invoke();
        }

        /// <summary>Selects the item at the given index.</summary>
        public void SelectItem(int index)
        {
            if (index < 0 || index >= _items.Count) return;
            VRRadialMenuItem item = _items[index];
            item.OnSelected?.Invoke();
            OnItemSelected?.Invoke(item.Label);
            CloseMenu();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void HandleGesture(XRHandedness hand, XRGestureType gesture)
        {
            if (gesture == XRGestureType.OpenPalm)
            {
                var htc = HandTrackingController.Instance;
                if (htc != null)
                {
                    XRHandState state = htc.GetHandState(hand);
                    OpenMenu(state.PalmPosition);
                }
            }
            else if (IsOpen && gesture == XRGestureType.Pinch)
            {
                if (HighlightedIndex >= 0)
                    SelectItem(HighlightedIndex);
                else
                    CloseMenu();
            }
        }

        private void UpdateGazeSelection()
        {
            if (_items.Count == 0) return;

            // Find the item closest to gaze direction (stub: use Camera.main forward).
            Camera cam = Camera.main;
            if (cam == null) return;

            int closest = -1;
            float minAngle = float.MaxValue;

            for (int i = 0; i < _items.Count; i++)
            {
                float angle = (float)i / _items.Count * 360f;
                float rad   = angle * Mathf.Deg2Rad;
                Vector3 itemWorldPos = transform.position
                    + transform.right   * Mathf.Cos(rad) * menuRadius
                    + transform.up      * Mathf.Sin(rad) * menuRadius;

                Vector3 toItem = (itemWorldPos - cam.transform.position).normalized;
                float gazeAngle = Vector3.Angle(cam.transform.forward, toItem);
                if (gazeAngle < minAngle)
                {
                    minAngle = gazeAngle;
                    closest  = i;
                }
            }

            if (closest != HighlightedIndex)
            {
                HighlightedIndex = closest;
                _gazeTimer = 0f;
            }
            else
            {
                _gazeTimer += Time.deltaTime;
                if (_gazeTimer >= gazeSelectDuration && HighlightedIndex >= 0)
                    SelectItem(HighlightedIndex);
            }
        }
    }
}
