using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Localization
{
    /// <summary>
    /// Component that auto-updates a <see cref="Text"/> or <c>TMPro.TextMeshProUGUI</c>
    /// whenever the active language changes.
    /// Attach to any UI Text or TextMeshProUGUI GameObject and set the
    /// <see cref="localizationKey"/> in the Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string localizationKey;

        // Cached references (auto-detected in Awake)
        private Text _legacyText;
        private object _tmpText; // TMPro.TextMeshProUGUI — late-bound to avoid hard dependency

        private object[] _formatArgs;

        // ── Unity lifecycle ──────────────────────────────────────────────────────
        private void Awake()
        {
            _legacyText = GetComponent<Text>();
            if (_legacyText == null)
            {
                // Try to get TextMeshProUGUI via reflection to avoid a hard assembly dependency
                var type = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
                if (type != null)
                    _tmpText = GetComponent(type);
            }
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

        /// <summary>
        /// Sets optional format arguments used with <c>string.Format</c> when resolving the key.
        /// Triggers an immediate refresh.
        /// </summary>
        /// <param name="args">Format arguments passed to <see cref="LocalizationManager.GetText(string, object[])"/>.</param>
        public void SetFormatArgs(params object[] args)
        {
            _formatArgs = args;
            Refresh();
        }

        /// <summary>
        /// Forces an immediate text refresh from the current language.
        /// </summary>
        public void Refresh()
        {
            if (string.IsNullOrEmpty(localizationKey)) return;

            var mgr = LocalizationManager.Instance;
            if (mgr == null) return;

            string text = (_formatArgs != null && _formatArgs.Length > 0)
                ? mgr.GetText(localizationKey, _formatArgs)
                : mgr.GetText(localizationKey);

            SetText(text);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────
        private void OnLanguageChanged(SystemLanguage _) => Refresh();

        private void SetText(string value)
        {
            if (_legacyText != null)
            {
                _legacyText.text = value;
                return;
            }

            if (_tmpText != null)
            {
                // Set via reflection: _tmpText.text = value
                var prop = _tmpText.GetType().GetProperty("text");
                prop?.SetValue(_tmpText, value);
            }
        }
    }
}
