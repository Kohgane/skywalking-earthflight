using System.Collections.Generic;
using UnityEngine;

namespace SWEF.UI
{
    /// <summary>
    /// Simple JSON-based localization system supporting Korean (ko), English (en),
    /// and Japanese (ja). Translation files are loaded from <c>Resources/Localization/</c>.
    /// Falls back to the key itself when a translation is not found.
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        /// <summary>The set of languages supported by SWEF.</summary>
        public enum SupportedLanguage { en, ko, ja }

        [SerializeField] private SupportedLanguage defaultLanguage = SupportedLanguage.en;

        /// <summary>Singleton instance; persists across scene loads.</summary>
        public static LocalizationManager Instance { get; private set; }

        /// <summary>The language currently in use.</summary>
        public SupportedLanguage CurrentLanguage { get; private set; }

        /// <summary>Fired whenever the active language changes.</summary>
        public event System.Action OnLanguageChanged;

        private Dictionary<string, string> _currentDict = new Dictionary<string, string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SupportedLanguage detected = DetectSystemLanguage();
            LoadLanguage(detected);
        }

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Returns the localized string for <paramref name="key"/>.
        /// Falls back to the key itself when no translation is found.
        /// </summary>
        public string Get(string key)
        {
            if (_currentDict.TryGetValue(key, out string value))
                return value;
            return key;
        }

        /// <summary>
        /// Switches to the requested <paramref name="lang"/>, reloads the dictionary,
        /// and fires <see cref="OnLanguageChanged"/>.
        /// </summary>
        public void SetLanguage(SupportedLanguage lang)
        {
            LoadLanguage(lang);
            OnLanguageChanged?.Invoke();
        }

        // ------------------------------------------------------------------ //
        //  Private helpers                                                     //
        // ------------------------------------------------------------------ //

        private SupportedLanguage DetectSystemLanguage()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Korean:   return SupportedLanguage.ko;
                case SystemLanguage.Japanese: return SupportedLanguage.ja;
                default:                      return defaultLanguage;
            }
        }

        /// <summary>
        /// Loads the JSON file at <c>Resources/Localization/{lang}.json</c> and builds
        /// the internal translation dictionary.
        /// </summary>
        private void LoadLanguage(SupportedLanguage lang)
        {
            CurrentLanguage = lang;
            _currentDict.Clear();

            TextAsset asset = Resources.Load<TextAsset>($"Localization/{lang}");
            if (asset == null)
            {
                Debug.LogWarning($"[SWEF] LocalizationManager: no file found for language '{lang}'. Falling back to en.");
                if (lang != SupportedLanguage.en)
                {
                    asset = Resources.Load<TextAsset>("Localization/en");
                    if (asset == null)
                    {
                        Debug.LogWarning("[SWEF] LocalizationManager: fallback 'en' file also missing.");
                        return;
                    }
                    CurrentLanguage = SupportedLanguage.en;
                }
                else
                {
                    return;
                }
            }

            LocalizationData data = JsonUtility.FromJson<LocalizationData>(asset.text);
            if (data == null || data.keys == null)
            {
                Debug.LogWarning("[SWEF] LocalizationManager: failed to parse JSON data.");
                return;
            }

            foreach (LocalizationEntry entry in data.keys)
            {
                if (!string.IsNullOrEmpty(entry.k))
                    _currentDict[entry.k] = entry.v ?? string.Empty;
            }

            Debug.Log($"[SWEF] LocalizationManager: loaded {_currentDict.Count} keys for '{CurrentLanguage}'.");
        }

        // ------------------------------------------------------------------ //
        //  Serializable inner types for JSON deserialization                   //
        // ------------------------------------------------------------------ //

        /// <summary>Top-level JSON wrapper: <c>{"keys": [...]}</c></summary>
        [System.Serializable]
        public class LocalizationData
        {
            public System.Collections.Generic.List<LocalizationEntry> keys;
        }

        /// <summary>Single key/value pair: <c>{"k":"ui_start","v":"Start"}</c></summary>
        [System.Serializable]
        public class LocalizationEntry
        {
            public string k;
            public string v;
        }
    }
}
