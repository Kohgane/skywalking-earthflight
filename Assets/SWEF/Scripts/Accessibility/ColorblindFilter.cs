using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Accessibility
{
    // ── Enums ────────────────────────────────────────────────────────────────────

    /// <summary>Colorblind simulation/correction mode.</summary>
    public enum ColorblindMode
    {
        /// <summary>No colorblind processing.</summary>
        None,
        /// <summary>Red-weak / red-blind (affects red-green discrimination).</summary>
        Protanopia,
        /// <summary>Green-weak / green-blind (most common red-green colorblindness).</summary>
        Deuteranopia,
        /// <summary>Blue-yellow colorblindness.</summary>
        Tritanopia,
        /// <summary>Complete absence of colour (monochromacy).</summary>
        Achromatopsia
    }

    /// <summary>Whether to simulate colorblindness (for testing) or correct/enhance for colorblind users.</summary>
    public enum ColorblindFilterMode
    {
        /// <summary>Simulate how a colorblind person sees the game (testing aid).</summary>
        Simulate,
        /// <summary>Enhance/shift colours to make the game more distinguishable for colorblind users.</summary>
        Correct
    }

    /// <summary>
    /// Manages colorblind assistance: shader-based colour matrix post-processing,
    /// UI element recolouring, and high-contrast outlines.
    /// </summary>
    public class ColorblindFilter : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static ColorblindFilter Instance { get; private set; }

        // ── PlayerPrefs keys ─────────────────────────────────────────────────────
        private const string KeyMode      = "SWEF_ColorblindMode";
        private const string KeyFilter    = "SWEF_ColorblindFilter";
        private const string KeyIntensity = "SWEF_ColorblindIntensity";
        private const string KeyContrast  = "SWEF_HighContrast";

        // ── Serialised fields ────────────────────────────────────────────────────
        [Header("Colorblind Settings")]
        [SerializeField] private ColorblindMode       mode       = ColorblindMode.None;
        [SerializeField] private ColorblindFilterMode filterMode = ColorblindFilterMode.Correct;
        [SerializeField] [Range(0f, 1f)] private float intensity = 1f;

        [Header("High Contrast")]
        [SerializeField] private bool highContrastEnabled;
        [SerializeField] private Color highContrastOutlineColor = Color.yellow;
        [SerializeField] [Range(1f, 10f)] private float outlineWidth = 3f;

        [Header("Post-Processing")]
        [SerializeField] private Material colorMatrixMaterial; // Assign a shader mat in inspector

        // ── Scientifically-based 3×3 colour matrices ─────────────────────────────
        // Row-major: output [R,G,B] = matrix × input [R,G,B]
        // Sources: Machado et al. 2009, Viénot et al. 1999

        // Simulation matrices (how a colorblind person sees the world)
        private static readonly float[] SimProtanopia = {
            0.567f, 0.433f, 0.000f,
            0.558f, 0.442f, 0.000f,
            0.000f, 0.242f, 0.758f
        };
        private static readonly float[] SimDeuteranopia = {
            0.625f, 0.375f, 0.000f,
            0.700f, 0.300f, 0.000f,
            0.000f, 0.300f, 0.700f
        };
        private static readonly float[] SimTritanopia = {
            0.950f, 0.050f, 0.000f,
            0.000f, 0.433f, 0.567f,
            0.000f, 0.475f, 0.525f
        };
        private static readonly float[] SimAchromatopsia = {
            0.299f, 0.587f, 0.114f,
            0.299f, 0.587f, 0.114f,
            0.299f, 0.587f, 0.114f
        };

        // Correction matrices (enhance distinguishability for colorblind users)
        private static readonly float[] CorProtanopia = {
            0.000f, 2.020f,-2.516f,
            0.000f, 1.000f, 0.000f,
            0.000f, 0.000f, 1.000f
        };
        private static readonly float[] CorDeuteranopia = {
            1.000f, 0.000f, 0.000f,
           -0.332f, 1.250f, 0.082f,
            0.000f, 0.000f, 1.000f
        };
        private static readonly float[] CorTritanopia = {
            1.000f, 0.000f, 0.000f,
            0.000f, 1.000f, 0.000f,
           -0.525f, 0.525f, 0.000f
        };
        private static readonly float[] CorAchromatopsia = {
            1.000f, 0.000f, 0.000f,
            0.000f, 1.000f, 0.000f,
            0.000f, 0.000f, 1.000f
        };

        // ── Custom palette overrides ─────────────────────────────────────────────
        // Maps problematic colour pairs per active mode
        private static readonly Dictionary<ColorblindMode, (Color from, Color to)[]> DefaultPaletteSwaps =
            new Dictionary<ColorblindMode, (Color, Color)[]>
            {
                [ColorblindMode.Protanopia]    = new[] { (Color.red, new Color(0.0f, 0.5f, 1.0f)), (Color.green, new Color(1.0f, 0.6f, 0.0f)) },
                [ColorblindMode.Deuteranopia]  = new[] { (Color.red, new Color(0.0f, 0.5f, 1.0f)), (Color.green, new Color(1.0f, 0.6f, 0.0f)) },
                [ColorblindMode.Tritanopia]    = new[] { (Color.blue, new Color(1.0f, 0.4f, 0.0f)), (Color.yellow, new Color(0.5f, 0.0f, 1.0f)) },
                [ColorblindMode.Achromatopsia] = new[] { (Color.red, new Color(0.3f, 0.3f, 0.3f)), (Color.green, new Color(0.6f, 0.6f, 0.6f)) },
            };

        private readonly Dictionary<string, Color> _customPalette = new Dictionary<string, Color>(StringComparer.Ordinal);

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Fired when the colorblind mode changes.</summary>
        public event Action<ColorblindMode> OnColorblindModeChanged;

        /// <summary>Fired when high-contrast mode is toggled.</summary>
        public event Action<bool> OnContrastModeToggled;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadPreferences();
            ApplyCurrentMode();
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Sets the active colorblind mode and updates the post-processing shader.
        /// </summary>
        public void SetMode(ColorblindMode newMode)
        {
            mode = newMode;
            ApplyCurrentMode();
            PlayerPrefs.SetInt(KeyMode, (int)newMode);
            PlayerPrefs.Save();
            OnColorblindModeChanged?.Invoke(newMode);
        }

        /// <summary>
        /// Switches between simulation and correction filter modes.
        /// </summary>
        public void SetFilterMode(ColorblindFilterMode fm)
        {
            filterMode = fm;
            ApplyCurrentMode();
            PlayerPrefs.SetInt(KeyFilter, (int)fm);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Sets the blend intensity (0 = original colours, 1 = full correction/simulation).
        /// </summary>
        public void SetIntensity(float value)
        {
            intensity = Mathf.Clamp01(value);
            if (colorMatrixMaterial != null)
                colorMatrixMaterial.SetFloat("_Intensity", intensity);
            PlayerPrefs.SetFloat(KeyIntensity, intensity);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Enables or disables high-contrast outline rendering.
        /// </summary>
        public void SetHighContrast(bool enabled)
        {
            highContrastEnabled = enabled;
            PlayerPrefs.SetInt(KeyContrast, enabled ? 1 : 0);
            PlayerPrefs.Save();
            OnContrastModeToggled?.Invoke(enabled);
        }

        /// <summary>
        /// Registers a custom colour override for a named game element.
        /// </summary>
        /// <param name="elementName">Unique element identifier (e.g., "Waypoint").</param>
        /// <param name="color">Replacement colour.</param>
        public void SetCustomPaletteEntry(string elementName, Color color)
        {
            if (!string.IsNullOrEmpty(elementName))
                _customPalette[elementName] = color;
        }

        /// <summary>
        /// Returns the effective colour for a named element, applying custom palette or
        /// default palette swap for the active mode.
        /// </summary>
        public Color ResolveColor(string elementName, Color originalColor)
        {
            if (_customPalette.TryGetValue(elementName, out Color custom)) return custom;
            if (mode != ColorblindMode.None && DefaultPaletteSwaps.TryGetValue(mode, out var swaps))
            {
                foreach (var (from, to) in swaps)
                    if (ColorsSimilar(originalColor, from)) return to;
            }
            return originalColor;
        }

        /// <summary>
        /// Recolours all <see cref="Graphic"/> elements in the given root using the
        /// active palette swap rules for the current mode.
        /// </summary>
        public void RecolourUI(Transform root)
        {
            if (root == null || mode == ColorblindMode.None) return;
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
                graphic.color = ResolveColor(graphic.name, graphic.color);
        }

        // ── Internal helpers ─────────────────────────────────────────────────────
        private void ApplyCurrentMode()
        {
            if (colorMatrixMaterial == null) return;

            float[] matrix = GetMatrix();
            if (matrix == null)
            {
                colorMatrixMaterial.SetFloat("_Intensity", 0f);
                return;
            }

            // Upload the 3×3 matrix as individual rows (shader expects _ColorMatrix_R/G/B)
            colorMatrixMaterial.SetVector("_ColorMatrix_R", new Vector4(matrix[0], matrix[1], matrix[2], 0f));
            colorMatrixMaterial.SetVector("_ColorMatrix_G", new Vector4(matrix[3], matrix[4], matrix[5], 0f));
            colorMatrixMaterial.SetVector("_ColorMatrix_B", new Vector4(matrix[6], matrix[7], matrix[8], 0f));
            colorMatrixMaterial.SetFloat("_Intensity", mode == ColorblindMode.None ? 0f : intensity);
        }

        private float[] GetMatrix()
        {
            bool sim = filterMode == ColorblindFilterMode.Simulate;
            switch (mode)
            {
                case ColorblindMode.Protanopia:    return sim ? SimProtanopia    : CorProtanopia;
                case ColorblindMode.Deuteranopia:  return sim ? SimDeuteranopia  : CorDeuteranopia;
                case ColorblindMode.Tritanopia:    return sim ? SimTritanopia    : CorTritanopia;
                case ColorblindMode.Achromatopsia: return sim ? SimAchromatopsia : CorAchromatopsia;
                default:                           return null;
            }
        }

        private void LoadPreferences()
        {
            int modeRaw = PlayerPrefs.GetInt(KeyMode, 0);
            mode = Enum.IsDefined(typeof(ColorblindMode), modeRaw)
                ? (ColorblindMode)modeRaw
                : ColorblindMode.None;
            int filterRaw = PlayerPrefs.GetInt(KeyFilter, (int)ColorblindFilterMode.Correct);
            filterMode = Enum.IsDefined(typeof(ColorblindFilterMode), filterRaw)
                ? (ColorblindFilterMode)filterRaw
                : ColorblindFilterMode.Correct;
            intensity           = PlayerPrefs.GetFloat(KeyIntensity, 1f);
            highContrastEnabled = PlayerPrefs.GetInt(KeyContrast, 0) == 1;
        }

        private static bool ColorsSimilar(Color a, Color b, float threshold = 0.15f)
        {
            return Mathf.Abs(a.r - b.r) < threshold
                && Mathf.Abs(a.g - b.g) < threshold
                && Mathf.Abs(a.b - b.b) < threshold;
        }
    }
}
