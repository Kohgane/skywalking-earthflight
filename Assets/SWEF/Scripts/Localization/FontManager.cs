using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Localization
{
    /// <summary>
    /// Manages font switching for CJK and Latin character sets.
    /// Auto-switches fonts on language change for proper glyph rendering.
    /// Attach to a persistent GameObject alongside <see cref="LocalizationManager"/>.
    /// </summary>
    public class FontManager : MonoBehaviour
    {
        [Header("Fonts")]
        [SerializeField] private Font defaultFont;
        [SerializeField] private Font cjkFont;
        [SerializeField] private Font koreanFont;

        // Optional TMP_FontAsset support — stored as Object to avoid hard TMP dependency
        [Header("TextMeshPro Fonts (optional)")]
        [SerializeField] private Object defaultTMPFont;
        [SerializeField] private Object cjkTMPFont;
        [SerializeField] private Object koreanTMPFont;

        // Registry of LocalizedText components that should receive font updates
        private readonly List<LocalizedText> _registeredTexts = new List<LocalizedText>();

        // ── Unity lifecycle ──────────────────────────────────────────────────────
        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Returns the appropriate <see cref="Font"/> for <paramref name="lang"/>.</summary>
        public Font GetFontForLanguage(SystemLanguage lang)
        {
            switch (lang)
            {
                case SystemLanguage.Korean:
                    return koreanFont != null ? koreanFont : (cjkFont != null ? cjkFont : defaultFont);

                case SystemLanguage.Japanese:
                case SystemLanguage.ChineseSimplified:
                    return cjkFont != null ? cjkFont : defaultFont;

                default:
                    return defaultFont;
            }
        }

        /// <summary>Returns the appropriate TMP_FontAsset (as <see cref="Object"/>) for <paramref name="lang"/>.</summary>
        public Object GetTMPFontForLanguage(SystemLanguage lang)
        {
            switch (lang)
            {
                case SystemLanguage.Korean:
                    return koreanTMPFont != null ? koreanTMPFont : (cjkTMPFont != null ? cjkTMPFont : defaultTMPFont);

                case SystemLanguage.Japanese:
                case SystemLanguage.ChineseSimplified:
                    return cjkTMPFont != null ? cjkTMPFont : defaultTMPFont;

                default:
                    return defaultTMPFont;
            }
        }

        /// <summary>
        /// Registers a <see cref="LocalizedText"/> component so that font changes are
        /// pushed to it when the language switches.
        /// </summary>
        public void Register(LocalizedText lt)
        {
            if (lt != null && !_registeredTexts.Contains(lt))
                _registeredTexts.Add(lt);
        }

        /// <summary>Unregisters a previously registered <see cref="LocalizedText"/> component.</summary>
        public void Unregister(LocalizedText lt)
        {
            _registeredTexts.Remove(lt);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────
        private void OnLanguageChanged(SystemLanguage lang)
        {
            Font font          = GetFontForLanguage(lang);
            Object tmpFont     = GetTMPFontForLanguage(lang);

            // Clean up destroyed refs
            _registeredTexts.RemoveAll(t => t == null);

            foreach (var lt in _registeredTexts)
            {
                ApplyFont(lt, font, tmpFont);
            }
        }

        private static void ApplyFont(LocalizedText lt, Font font, Object tmpFont)
        {
            // Legacy Text
            var legacyText = lt.GetComponent<UnityEngine.UI.Text>();
            if (legacyText != null && font != null)
            {
                legacyText.font = font;
                return;
            }

            // TextMeshProUGUI via reflection
            if (tmpFont != null)
            {
                var type = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
                if (type != null)
                {
                    var tmp = lt.GetComponent(type);
                    if (tmp != null)
                    {
                        var prop = type.GetProperty("font");
                        prop?.SetValue(tmp, tmpFont);
                    }
                }
            }
        }
    }
}
