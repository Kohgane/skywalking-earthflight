using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Localization
{
    /// <summary>
    /// Language selection panel UI controller.
    /// Presents all supported languages with their native names, highlights
    /// the currently active language with a checkmark, and applies the
    /// selection through <see cref="LocalizationManager"/>.
    /// Intended to be opened as a sub-panel of SettingsUI.
    /// </summary>
    public class LocalizationUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button     closeButton;

        [Header("Language List")]
        /// <summary>Parent transform where language button rows are instantiated.</summary>
        [SerializeField] private Transform  buttonContainer;
        /// <summary>Prefab containing a Button and a child Text for the language name.</summary>
        [SerializeField] private GameObject languageButtonPrefab;
        /// <summary>Optional checkmark GameObject shown on the active-language button.</summary>
        [SerializeField] private Sprite     checkmarkSprite;

        [Header("Preview")]
        /// <summary>Text component that shows a sample translated string while hovering.</summary>
        [SerializeField] private Text previewText;
        private const string PreviewKey = "tutorial.welcome";

        // Runtime state
        private readonly List<LanguageButtonRow> _rows = new List<LanguageButtonRow>();

        // ── Inner helper ─────────────────────────────────────────────────────────
        private class LanguageButtonRow
        {
            public SystemLanguage Language;
            public Button         Button;
            public Text           Label;
            public Image          Checkmark;
        }

        // ── Unity lifecycle ──────────────────────────────────────────────────────
        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(ClosePanel);

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += OnLanguageChanged;
            RebuildButtons();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Opens the language selection panel.</summary>
        public void OpenPanel()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            RebuildButtons();
        }

        /// <summary>Closes the language selection panel.</summary>
        public void ClosePanel()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────
        private void RebuildButtons()
        {
            // Clear existing rows
            foreach (var row in _rows)
            {
                if (row.Button != null)
                    Destroy(row.Button.gameObject);
            }
            _rows.Clear();

            if (buttonContainer == null || languageButtonPrefab == null) return;

            var mgr = LocalizationManager.Instance;
            foreach (var lang in LocalizationManager.SupportedLanguages)
            {
                var go  = Instantiate(languageButtonPrefab, buttonContainer);
                var btn = go.GetComponent<Button>();
                var lbl = go.GetComponentInChildren<Text>();
                Image checkmark = null;

                if (lbl != null)
                    lbl.text = LocalizationManager.GetNativeName(lang);

                // Create checkmark Image inside the button if a sprite is assigned
                if (checkmarkSprite != null)
                {
                    var cmGO = new GameObject("Checkmark", typeof(Image));
                    cmGO.transform.SetParent(go.transform, false);
                    checkmark = cmGO.GetComponent<Image>();
                    checkmark.sprite = checkmarkSprite;
                    checkmark.preserveAspect = true;
                }

                var capturedLang = lang;
                if (btn != null)
                {
                    btn.onClick.AddListener(() => OnLanguageButtonClicked(capturedLang));
                }

                // Hover events via EventTrigger for preview text
                SetupHoverPreview(go, capturedLang);

                var row = new LanguageButtonRow
                {
                    Language  = lang,
                    Button    = btn,
                    Label     = lbl,
                    Checkmark = checkmark,
                };
                _rows.Add(row);
            }

            RefreshCheckmarks();
        }

        private void SetupHoverPreview(GameObject go, SystemLanguage lang)
        {
            if (previewText == null) return;

            var trigger = go.GetComponent<UnityEngine.EventSystems.EventTrigger>()
                          ?? go.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var onEnter = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
            };
            onEnter.callback.AddListener(_ =>
            {
                var dict = LanguageDatabase.LoadLanguage(lang);
                if (dict != null && dict.TryGetValue(PreviewKey, out string preview))
                    previewText.text = preview;
                else
                    previewText.text = LocalizationManager.GetNativeName(lang);
            });
            trigger.triggers.Add(onEnter);

            var onExit = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit
            };
            onExit.callback.AddListener(_ =>
            {
                var locMgr = LocalizationManager.Instance;
                previewText.text = locMgr != null ? locMgr.GetText(PreviewKey) : string.Empty;
            });
            trigger.triggers.Add(onExit);
        }

        private void OnLanguageButtonClicked(SystemLanguage lang)
        {
            var mgr = LocalizationManager.Instance;
            if (mgr != null) mgr.CurrentLanguage = lang;
        }

        private void OnLanguageChanged(SystemLanguage _) => RefreshCheckmarks();

        private void RefreshCheckmarks()
        {
            var mgr = LocalizationManager.Instance;
            SystemLanguage current = mgr != null ? mgr.CurrentLanguage : SystemLanguage.English;

            foreach (var row in _rows)
            {
                if (row.Checkmark != null)
                    row.Checkmark.gameObject.SetActive(row.Language == current);
            }
        }
    }
}
