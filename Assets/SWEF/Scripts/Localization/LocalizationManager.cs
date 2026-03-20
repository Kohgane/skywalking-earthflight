using System;
using UnityEngine;

namespace SWEF.Localization
{
    /// <summary>
    /// Singleton MonoBehaviour managing all localization state for SWEF.
    /// Handles language detection, persistence, runtime switching, and
    /// event broadcasting for subscriber components.
    /// Persists the selected language via PlayerPrefs key <c>SWEF_Language</c>.
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static LocalizationManager Instance { get; private set; }

        // ── Constants ────────────────────────────────────────────────────────────
        private const string KeyLanguage = "SWEF_Language";

        /// <summary>Supported languages in SWEF.</summary>
        public static readonly SystemLanguage[] SupportedLanguages =
        {
            SystemLanguage.English,
            SystemLanguage.Korean,
            SystemLanguage.Japanese,
            SystemLanguage.ChineseSimplified,
            SystemLanguage.Spanish,
            SystemLanguage.French,
            SystemLanguage.German,
            SystemLanguage.Portuguese,
        };

        // ── State ────────────────────────────────────────────────────────────────
        private SystemLanguage _currentLanguage = SystemLanguage.English;

        /// <summary>
        /// Gets or sets the active language.
        /// Setting fires <see cref="OnLanguageChanged"/> and re-loads the language data.
        /// </summary>
        public SystemLanguage CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage == value) return;
                _currentLanguage = value;
                PlayerPrefs.SetString(KeyLanguage, value.ToString());
                PlayerPrefs.Save();
                OnLanguageChanged?.Invoke(value);
                Debug.Log($"[SWEF] Language switched to: {value}");
            }
        }

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Fired whenever the active language changes.</summary>
        public static event Action<SystemLanguage> OnLanguageChanged;

        // ── Unity lifecycle ──────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Initialize();
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Initializes the localization system.
        /// Restores the persisted language or auto-detects from the device on first launch.
        /// Called automatically by <see cref="Start"/> and can be called explicitly by BootManager.
        /// </summary>
        public void Initialize()
        {
            string saved = PlayerPrefs.GetString(KeyLanguage, string.Empty);
            if (!string.IsNullOrEmpty(saved) && Enum.TryParse(saved, out SystemLanguage parsed))
            {
                _currentLanguage = IsSupportedLanguage(parsed) ? parsed : SystemLanguage.English;
            }
            else
            {
                _currentLanguage = DetectDeviceLanguage();
                PlayerPrefs.SetString(KeyLanguage, _currentLanguage.ToString());
                PlayerPrefs.Save();
            }

            // Pre-load the current language into cache
            LanguageDatabase.LoadLanguage(_currentLanguage);

            Debug.Log($"[SWEF] LocalizationManager initialized — language: {_currentLanguage}");
        }

        /// <summary>
        /// Returns the localized string for <paramref name="key"/> in the current language.
        /// Falls back to English when the key is missing.
        /// </summary>
        /// <param name="key">Dot-separated localization key (e.g. <c>"hud.altitude"</c>).</param>
        /// <returns>Localized string, or <paramref name="key"/> itself if not found.</returns>
        public string GetText(string key)
        {
            var dict = LanguageDatabase.LoadLanguage(_currentLanguage);
            if (dict != null && dict.TryGetValue(key, out string value))
                return value;

            // Fall back to English
            if (_currentLanguage != SystemLanguage.English)
            {
                var fallback = LanguageDatabase.LoadLanguage(SystemLanguage.English);
                if (fallback != null && fallback.TryGetValue(key, out string fallbackValue))
                    return fallbackValue;
            }

            Debug.LogWarning($"[SWEF] Localization key not found: '{key}'");
            return key;
        }

        /// <summary>
        /// Returns a formatted localized string using <see cref="string.Format(string, object[])"/>.
        /// </summary>
        /// <param name="key">Dot-separated localization key.</param>
        /// <param name="args">Format arguments.</param>
        /// <returns>Formatted localized string.</returns>
        public string GetText(string key, params object[] args)
        {
            string raw = GetText(key);
            try
            {
                return string.Format(raw, args);
            }
            catch (FormatException ex)
            {
                Debug.LogWarning($"[SWEF] Localization format error for key '{key}': {ex.Message}");
                return raw;
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        /// <summary>Returns whether <paramref name="lang"/> is in the supported-language list.</summary>
        public static bool IsSupportedLanguage(SystemLanguage lang)
        {
            foreach (var l in SupportedLanguages)
                if (l == lang) return true;
            return false;
        }

        /// <summary>Returns the native display name for a given <see cref="SystemLanguage"/>.</summary>
        public static string GetNativeName(SystemLanguage lang)
        {
            switch (lang)
            {
                case SystemLanguage.English:           return "English";
                case SystemLanguage.Korean:            return "한국어";
                case SystemLanguage.Japanese:          return "日本語";
                case SystemLanguage.ChineseSimplified: return "简体中文";
                case SystemLanguage.Spanish:           return "Español";
                case SystemLanguage.French:            return "Français";
                case SystemLanguage.German:            return "Deutsch";
                case SystemLanguage.Portuguese:        return "Português";
                default:                               return lang.ToString();
            }
        }

        // ── Private helpers ──────────────────────────────────────────────────────
        private static SystemLanguage DetectDeviceLanguage()
        {
            SystemLanguage device = Application.systemLanguage;
            return IsSupportedLanguage(device) ? device : SystemLanguage.English;
        }
    }
}
