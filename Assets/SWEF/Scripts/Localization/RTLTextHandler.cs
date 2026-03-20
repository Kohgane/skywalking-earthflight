using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Localization
{
    /// <summary>
    /// Right-to-left text support utility.
    /// Provides infrastructure for Arabic/Hebrew support in future phases.
    /// The current supported language list contains no RTL languages, but
    /// the system is ready to be extended.
    /// </summary>
    public static class RTLTextHandler
    {
        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> when <paramref name="lang"/> is a right-to-left language.
        /// </summary>
        public static bool IsRTLLanguage(SystemLanguage lang)
        {
            // No RTL languages in current supported set.
            // Add SystemLanguage.Arabic, SystemLanguage.Hebrew here when supported.
            return false;
        }

        /// <summary>
        /// Processes a string for RTL display.
        /// For LTR languages this is a no-op and returns the input unchanged.
        /// For RTL languages the character order is reversed for correct visual rendering.
        /// </summary>
        /// <param name="input">Input string to process.</param>
        /// <returns>Processed string suitable for rendering.</returns>
        public static string ProcessRTL(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var mgr = LocalizationManager.Instance;
            if (mgr == null || !IsRTLLanguage(mgr.CurrentLanguage))
                return input;

            // Reverse character order for RTL scripts
            char[] chars = input.ToCharArray();
            System.Array.Reverse(chars);
            return new string(chars);
        }

        /// <summary>
        /// Aligns a <see cref="Text"/> component based on the current language direction.
        /// LTR → <see cref="TextAnchor.MiddleLeft"/>;
        /// RTL → <see cref="TextAnchor.MiddleRight"/>.
        /// </summary>
        /// <param name="text">Target Text component.</param>
        public static void AlignText(Text text)
        {
            if (text == null) return;
            var mgr = LocalizationManager.Instance;
            bool isRTL = mgr != null && IsRTLLanguage(mgr.CurrentLanguage);
            text.alignment = isRTL ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        }

        /// <summary>
        /// Aligns any UI component that exposes a <c>text</c> property (e.g. TextMeshProUGUI)
        /// via reflection, based on the current language direction.
        /// </summary>
        /// <param name="textComponent">Text component to align.</param>
        public static void AlignTextComponent(Component textComponent)
        {
            if (textComponent == null) return;

            if (textComponent is Text legacyText)
            {
                AlignText(legacyText);
                return;
            }

            // Reflection-based alignment for TextMeshProUGUI
            var mgr = LocalizationManager.Instance;
            bool isRTL = mgr != null && IsRTLLanguage(mgr.CurrentLanguage);
            var alignProp = textComponent.GetType().GetProperty("alignment");
            if (alignProp != null)
            {
                // TMPro.TextAlignmentOptions: TopLeft=257, TopRight=260
                alignProp.SetValue(textComponent, isRTL ? 260 : 257);
            }
        }
    }
}
