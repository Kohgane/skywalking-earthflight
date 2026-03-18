using System;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.UI
{
    /// <summary>
    /// Repositions all registered interactive HUD elements to one side of the screen
    /// to improve reachability for one-handed play.
    /// Original positions are cached so the layout can be fully restored when the mode
    /// is disabled.
    /// Settings are persisted in PlayerPrefs under <c>SWEF_OneHandedMode</c> and
    /// <c>SWEF_HandPreference</c>.
    /// </summary>
    public class OneHandedModeController : MonoBehaviour
    {
        // ── Enums ─────────────────────────────────────────────────────────────────
        /// <summary>Preferred hand for one-handed layout.</summary>
        public enum HandPreference { Right, Left }

        // ── PlayerPrefs keys ─────────────────────────────────────────────────────
        private const string KeyOneHandedMode   = "SWEF_OneHandedMode";
        private const string KeyHandPreference  = "SWEF_HandPreference";

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Layout")]
        [SerializeField] private HandPreference preferredHand    = HandPreference.Right;
        [SerializeField] [Range(0.1f, 0.9f)] private float reachZoneWidth = 0.4f;

        [Header("Elements to reposition")]
        [SerializeField] private RectTransform[] interactiveElements;

        // ── State ─────────────────────────────────────────────────────────────────
        private bool      _oneHandedEnabled;
        private Vector2[] _originalPositions;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when one-handed mode is toggled. Argument is the new enabled state.</summary>
        public event Action<bool> OnOneHandedModeChanged;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Whether one-handed mode is currently active.</summary>
        public bool IsOneHandedModeEnabled => _oneHandedEnabled;

        /// <summary>Current hand preference.</summary>
        public HandPreference ActiveHandPreference => preferredHand;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            CacheOriginalPositions();
            LoadPreferences();
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Enables or disables the one-handed layout and persists the setting.</summary>
        public void SetOneHandedMode(bool enabled)
        {
            _oneHandedEnabled = enabled;
            PlayerPrefs.SetInt(KeyOneHandedMode, enabled ? 1 : 0);
            PlayerPrefs.Save();

            if (enabled)
                ApplyOneHandedLayout();
            else
                RestoreOriginalLayout();

            OnOneHandedModeChanged?.Invoke(enabled);
            Debug.Log($"[SWEF] OneHandedMode: {(enabled ? "enabled" : "disabled")} ({preferredHand})");
        }

        /// <summary>Switches the preferred hand side and re-applies the layout if active.</summary>
        public void SetHandPreference(HandPreference hand)
        {
            preferredHand = hand;
            PlayerPrefs.SetInt(KeyHandPreference, (int)hand);
            PlayerPrefs.Save();

            if (_oneHandedEnabled)
                ApplyOneHandedLayout();
        }

        // ── Internal ─────────────────────────────────────────────────────────────

        private void LoadPreferences()
        {
            _oneHandedEnabled = PlayerPrefs.GetInt(KeyOneHandedMode,  0) == 1;
            preferredHand     = (HandPreference)PlayerPrefs.GetInt(KeyHandPreference, (int)HandPreference.Right);

            if (_oneHandedEnabled)
                ApplyOneHandedLayout();
        }

        private void CacheOriginalPositions()
        {
            if (interactiveElements == null) return;

            _originalPositions = new Vector2[interactiveElements.Length];
            for (int i = 0; i < interactiveElements.Length; i++)
            {
                if (interactiveElements[i] != null)
                    _originalPositions[i] = interactiveElements[i].anchoredPosition;
            }
        }

        private void ApplyOneHandedLayout()
        {
            if (interactiveElements == null) return;

            float screenWidth  = Screen.width;
            float zoneStart    = preferredHand == HandPreference.Right
                ? screenWidth * (1f - reachZoneWidth)
                : 0f;
            float zoneCenter   = zoneStart + screenWidth * reachZoneWidth * 0.5f;
            float halfZone     = screenWidth * reachZoneWidth * 0.5f;

            // Stack elements vertically within the reach zone
            float stepY  = -80f;
            float startY = 0f;

            for (int i = 0; i < interactiveElements.Length; i++)
            {
                var el = interactiveElements[i];
                if (el == null) continue;

                // Convert screen-space zone centre to the element's canvas space
                float targetX  = preferredHand == HandPreference.Right
                    ?  halfZone + (float)i * 0f     // align to right zone
                    : -halfZone;
                float targetY  = startY + i * stepY;

                el.anchoredPosition = new Vector2(targetX, targetY);
            }
        }

        private void RestoreOriginalLayout()
        {
            if (interactiveElements == null || _originalPositions == null) return;

            for (int i = 0; i < interactiveElements.Length; i++)
            {
                if (interactiveElements[i] != null && i < _originalPositions.Length)
                    interactiveElements[i].anchoredPosition = _originalPositions[i];
            }
        }
    }
}
