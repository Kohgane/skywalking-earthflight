// ColorPickerController.cs — Phase 115: Advanced Aircraft Livery Editor
// Color picker: RGB, HSV, hex input, eyedropper, recent colors, team color palette.
// Namespace: SWEF.LiveryEditor

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Manages the active paint colour for the livery editor.
    /// Tracks the current colour, maintains a recent-colour history, and
    /// provides RGB / HSV / Hex conversion helpers used by the UI.
    /// </summary>
    public class ColorPickerController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [SerializeField] private LiveryEditorConfig config;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised whenever the active colour changes.</summary>
        public event Action<Color> OnColorChanged;

        // ── Public properties ─────────────────────────────────────────────────────
        /// <summary>Currently selected paint colour.</summary>
        public Color CurrentColor
        {
            get => _currentColor;
            set => SetColor(value);
        }

        /// <summary>Read-only list of recently used colours (newest first).</summary>
        public IReadOnlyList<Color> RecentColors => _recentColors.AsReadOnly();

        // ── Internal state ────────────────────────────────────────────────────────
        private Color _currentColor = Color.white;
        private readonly List<Color> _recentColors = new List<Color>();

        // ── Colour setters ────────────────────────────────────────────────────────

        /// <summary>Sets the active colour and pushes the previous colour to history.</summary>
        public void SetColor(Color color)
        {
            if (_currentColor == color) return;

            PushRecent(_currentColor);
            _currentColor = color;
            OnColorChanged?.Invoke(_currentColor);
        }

        /// <summary>Sets colour from HSV components (each in range 0–1).</summary>
        public void SetFromHSV(float h, float s, float v)
        {
            SetColor(Color.HSVToRGB(
                Mathf.Clamp01(h),
                Mathf.Clamp01(s),
                Mathf.Clamp01(v)));
        }

        /// <summary>
        /// Sets colour from a CSS hex string (e.g. <c>#FF8800</c> or <c>FF8800</c>).
        /// Returns <c>false</c> if the string cannot be parsed.
        /// </summary>
        public bool SetFromHex(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return false;
            if (!hex.StartsWith("#")) hex = "#" + hex;
            if (ColorUtility.TryParseHtmlString(hex, out Color c))
            {
                SetColor(c);
                return true;
            }
            return false;
        }

        // ── Eyedropper ────────────────────────────────────────────────────────────

        /// <summary>
        /// Samples a pixel colour from a texture at the given UV coordinate.
        /// </summary>
        /// <param name="texture">Source texture to sample.</param>
        /// <param name="uv">Normalised UV position (0–1).</param>
        public void Eyedrop(Texture2D texture, Vector2 uv)
        {
            if (texture == null) return;
            int x = Mathf.RoundToInt(uv.x * (texture.width  - 1));
            int y = Mathf.RoundToInt(uv.y * (texture.height - 1));
            SetColor(texture.GetPixel(x, y));
        }

        // ── Colour conversion helpers ─────────────────────────────────────────────

        /// <summary>Returns the current colour as an HSV tuple (H, S, V) each in 0–1.</summary>
        public (float H, float S, float V) ToHSV()
        {
            Color.RGBToHSV(_currentColor, out float h, out float s, out float v);
            return (h, s, v);
        }

        /// <summary>Returns the current colour as a 6-digit CSS hex string (no alpha).</summary>
        public string ToHex() => ColorUtility.ToHtmlStringRGB(_currentColor);

        // ── Recent colour history ─────────────────────────────────────────────────

        private void PushRecent(Color color)
        {
            int max = config != null ? config.RecentColorCount : 16;
            _recentColors.Remove(color);
            _recentColors.Insert(0, color);
            while (_recentColors.Count > max)
                _recentColors.RemoveAt(_recentColors.Count - 1);
        }

        /// <summary>Selects a colour from the recent history by index.</summary>
        public void SelectRecent(int index)
        {
            if (index >= 0 && index < _recentColors.Count)
                SetColor(_recentColors[index]);
        }

        /// <summary>Clears the recent colour history.</summary>
        public void ClearRecent() => _recentColors.Clear();
    }
}
