using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Narration
{
    /// <summary>
    /// Settings panel UI that exposes all <see cref="NarrationConfig"/> properties
    /// to the player via sliders, toggles, and dropdowns.  Applies changes live via
    /// <see cref="NarrationManager.ApplyConfig"/>.
    /// </summary>
    public class NarrationSettingsUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Panel")]
        [SerializeField] private GameObject settingsPanel;

        [Header("Toggles")]
        [SerializeField] private Toggle enableToggle;
        [SerializeField] private Toggle autoPlayToggle;
        [SerializeField] private Toggle duckMusicToggle;
        [SerializeField] private Toggle preferAudioToggle;
        [SerializeField] private Toggle showSubtitlesToggle;
        [SerializeField] private Toggle autoAdvanceToggle;
        [SerializeField] private Toggle showMinimapToggle;
        [SerializeField] private Toggle showProximityToggle;
        [SerializeField] private Toggle funFactsToggle;
        [SerializeField] private Toggle discoveryModeToggle;

        [Header("Sliders")]
        [SerializeField] private Slider narrationVolumeSlider;
        [SerializeField] private Slider duckAmountSlider;
        [SerializeField] private Slider subtitleFontSizeSlider;
        [SerializeField] private Slider segmentPauseSlider;
        [SerializeField] private Slider cooldownSlider;
        [SerializeField] private Slider narrationSpeedSlider;

        [Header("Labels (optional)")]
        [SerializeField] private Text narrationVolumeLabel;
        [SerializeField] private Text narrationSpeedLabel;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);

            // Close button.
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
        }

        private void OnEnable()
        {
            RefreshFromConfig();
            RegisterListeners();
        }

        private void OnDisable()
        {
            UnregisterListeners();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        public void Show()
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);
            RefreshFromConfig();
        }

        public void Hide()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        // ── Refresh UI from config ────────────────────────────────────────────────

        private void RefreshFromConfig()
        {
            var mgr = NarrationManager.Instance;
            if (mgr == null) return;
            var cfg = mgr.Config;

            SetToggle(enableToggle,          cfg.enabled);
            SetToggle(autoPlayToggle,        cfg.autoPlayOnProximity);
            SetToggle(duckMusicToggle,       cfg.duckMusicDuringNarration);
            SetToggle(preferAudioToggle,     cfg.preferAudioNarration);
            SetToggle(showSubtitlesToggle,   cfg.showSubtitles);
            SetToggle(autoAdvanceToggle,     cfg.autoAdvanceSegments);
            SetToggle(showMinimapToggle,     cfg.showMinimapIcons);
            SetToggle(showProximityToggle,   cfg.showProximityIndicator);
            SetToggle(funFactsToggle,        cfg.enableFunFacts);
            SetToggle(discoveryModeToggle,   cfg.discoveryMode);

            SetSlider(narrationVolumeSlider, cfg.narrationVolume, 0f, 1f);
            SetSlider(duckAmountSlider,      cfg.duckAmount,      0f, 0.8f);
            SetSlider(subtitleFontSizeSlider,cfg.subtitleFontSize,10f, 36f);
            SetSlider(segmentPauseSlider,    cfg.segmentPauseDuration, 0f, 5f);
            SetSlider(cooldownSlider,        cfg.cooldownBetweenNarrations, 0f, 120f);
            SetSlider(narrationSpeedSlider,  cfg.narrationSpeed,  0.5f, 2f);

            UpdateLabels(cfg);
        }

        private void UpdateLabels(NarrationConfig cfg)
        {
            if (narrationVolumeLabel != null)
                narrationVolumeLabel.text = $"{Mathf.RoundToInt(cfg.narrationVolume * 100)}%";
            if (narrationSpeedLabel != null)
                narrationSpeedLabel.text = $"{cfg.narrationSpeed:F1}×";
        }

        // ── Listeners ─────────────────────────────────────────────────────────────

        private void RegisterListeners()
        {
            AddToggle(enableToggle,          v => ApplyField(c => c.enabled = v));
            AddToggle(autoPlayToggle,        v => ApplyField(c => c.autoPlayOnProximity = v));
            AddToggle(duckMusicToggle,       v => ApplyField(c => c.duckMusicDuringNarration = v));
            AddToggle(preferAudioToggle,     v => ApplyField(c => c.preferAudioNarration = v));
            AddToggle(showSubtitlesToggle,   v => ApplyField(c => c.showSubtitles = v));
            AddToggle(autoAdvanceToggle,     v => ApplyField(c => c.autoAdvanceSegments = v));
            AddToggle(showMinimapToggle,     v => ApplyField(c => c.showMinimapIcons = v));
            AddToggle(showProximityToggle,   v => ApplyField(c => c.showProximityIndicator = v));
            AddToggle(funFactsToggle,        v => ApplyField(c => c.enableFunFacts = v));
            AddToggle(discoveryModeToggle,   v => ApplyField(c => c.discoveryMode = v));

            AddSlider(narrationVolumeSlider, v => { ApplyField(c => c.narrationVolume = v); UpdateLabels(NarrationManager.Instance?.Config); });
            AddSlider(duckAmountSlider,      v => ApplyField(c => c.duckAmount = v));
            AddSlider(subtitleFontSizeSlider,v => ApplyField(c => c.subtitleFontSize = v));
            AddSlider(segmentPauseSlider,    v => ApplyField(c => c.segmentPauseDuration = v));
            AddSlider(cooldownSlider,        v => ApplyField(c => c.cooldownBetweenNarrations = v));
            AddSlider(narrationSpeedSlider,  v => { ApplyField(c => c.narrationSpeed = v); UpdateLabels(NarrationManager.Instance?.Config); });
        }

        private void UnregisterListeners()
        {
            // Toggles and sliders manage listeners through AddListener — removing all
            // listeners from the specific callbacks is handled by OnDisable/OnEnable cycle.
        }

        // ── Apply helpers ─────────────────────────────────────────────────────────

        private void ApplyField(System.Action<NarrationConfig> setter)
        {
            var mgr = NarrationManager.Instance;
            if (mgr == null) return;
            setter(mgr.Config);
            mgr.ApplyConfig(mgr.Config);
        }


        // ── Static UI helpers ─────────────────────────────────────────────────────

        private static void SetToggle(Toggle t, bool value)
        {
            if (t == null) return;
            t.SetIsOnWithoutNotify(value);
        }

        private static void SetSlider(Slider s, float value, float min, float max)
        {
            if (s == null) return;
            s.minValue = min;
            s.maxValue = max;
            s.SetValueWithoutNotify(value);
        }

        private static void AddToggle(Toggle t, UnityEngine.Events.UnityAction<bool> action)
        {
            if (t != null) t.onValueChanged.AddListener(action);
        }

        private static void AddSlider(Slider s, UnityEngine.Events.UnityAction<float> action)
        {
            if (s != null) s.onValueChanged.AddListener(action);
        }
    }
}
