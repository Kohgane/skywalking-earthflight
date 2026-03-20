using System;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Localization
{
    /// <summary>
    /// Component that swaps the sprite of a <see cref="Image"/> based on the active language.
    /// Useful for localized logos, tutorial badges, or culture-specific graphics.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public class LocalizedImage : MonoBehaviour
    {
        /// <summary>Maps a <see cref="SystemLanguage"/> to a <see cref="Sprite"/>.</summary>
        [Serializable]
        public struct LanguageSprite
        {
            /// <summary>Target language for this sprite.</summary>
            public SystemLanguage language;
            /// <summary>Sprite to display for <see cref="language"/>.</summary>
            public Sprite sprite;
        }

        [SerializeField] private LanguageSprite[] languageSprites;
        [SerializeField] private Sprite defaultSprite;

        private Image _image;

        // ── Unity lifecycle ──────────────────────────────────────────────────────
        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += OnLanguageChanged;
            Refresh();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
        }

        private void Start()
        {
            Refresh();
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Forces an immediate sprite refresh for the current language.</summary>
        public void Refresh()
        {
            var mgr = LocalizationManager.Instance;
            SystemLanguage lang = mgr != null ? mgr.CurrentLanguage : SystemLanguage.English;

            Sprite found = defaultSprite;
            if (languageSprites != null)
            {
                foreach (var ls in languageSprites)
                {
                    if (ls.language == lang && ls.sprite != null)
                    {
                        found = ls.sprite;
                        break;
                    }
                }
            }

            if (_image != null)
                _image.sprite = found;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────
        private void OnLanguageChanged(SystemLanguage _) => Refresh();
    }
}
