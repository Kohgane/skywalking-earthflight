// AdaptiveMusicHUD.cs — SWEF Dynamic Soundtrack & Adaptive Music System
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.AdaptiveMusic
{
    /// <summary>
    /// HUD panel showing the current music mood, intensity bar,
    /// per-layer activity dots, and an override intensity slider.
    /// </summary>
    public class AdaptiveMusicHUD : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Mood Display")]
        [SerializeField] private Text       _moodLabel;
        [SerializeField] private Image      _moodColorSwatch;

        [Header("Intensity Bar")]
        [SerializeField] private Image      _intensityFill;
        [SerializeField] private Gradient   _intensityGradient;

        [Header("Layer Indicators")]
        [SerializeField] private Transform  _layerDotsParent;

        [Header("Override")]
        [SerializeField] private GameObject _overridePanel;
        [SerializeField] private Slider     _overrideSlider;

        [Header("Auto-hide")]
        [Tooltip("Seconds of inactivity before the HUD panel auto-collapses.")]
        [Range(1f, 30f)]
        [SerializeField] private float _autoHideDelay = 8f;

        // ── State ─────────────────────────────────────────────────────────────

        private Dictionary<MusicLayer, Image> _layerDots = new Dictionary<MusicLayer, Image>();
        private float _lastActivityTime;
        private bool  _isExpanded;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Start()
        {
            BuildLayerDots();
            SetExpanded(false);

            if (_overrideSlider != null)
                _overrideSlider.onValueChanged.AddListener(OnOverrideSliderChanged);

            var mgr = AdaptiveMusicManager.Instance;
            if (mgr != null)
            {
                mgr.OnMoodChanged      += (_, n) => RefreshMood(n);
                mgr.OnIntensityChanged += RefreshIntensity;
                mgr.OnStemActivated    += l => SetLayerActive(l, true);
                mgr.OnStemDeactivated  += l => SetLayerActive(l, false);

                RefreshMood(mgr.CurrentMood);
                RefreshIntensity(mgr.CurrentIntensity);
            }
        }

        private void Update()
        {
            if (_isExpanded && Time.time - _lastActivityTime > _autoHideDelay)
                SetExpanded(false);
        }

        // ── UI Callbacks ──────────────────────────────────────────────────────

        public void ToggleExpand()
        {
            SetExpanded(!_isExpanded);
        }

        private void OnOverrideSliderChanged(float value)
        {
            _lastActivityTime = Time.time;
            AdaptiveMusicManager.Instance?.SetIntensity(value);
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        private void RefreshMood(MusicMood mood)
        {
            if (_moodLabel != null)
                _moodLabel.text = mood.ToString();

            if (_moodColorSwatch != null)
                _moodColorSwatch.color = MoodColor(mood);
        }

        private void RefreshIntensity(float intensity)
        {
            if (_intensityFill != null)
            {
                _intensityFill.fillAmount = intensity;
                if (_intensityGradient != null)
                    _intensityFill.color = _intensityGradient.Evaluate(intensity);
            }

            if (_overrideSlider != null && !_overrideSlider.value.Equals(intensity))
                _overrideSlider.SetValueWithoutNotify(intensity);
        }

        private void SetLayerActive(MusicLayer layer, bool active)
        {
            if (_layerDots.TryGetValue(layer, out var dot))
                dot.color = active ? Color.green : new Color(0.3f, 0.3f, 0.3f);
        }

        private void SetExpanded(bool expanded)
        {
            _isExpanded = expanded;
            if (_overridePanel != null) _overridePanel.SetActive(expanded);
            _lastActivityTime = Time.time;
        }

        private void BuildLayerDots()
        {
            if (_layerDotsParent == null) return;
            var dotImages = _layerDotsParent.GetComponentsInChildren<Image>();
            var layers    = (MusicLayer[])System.Enum.GetValues(typeof(MusicLayer));
            int count     = Mathf.Min(dotImages.Length, layers.Length);
            for (int i = 0; i < count; i++)
                _layerDots[layers[i]] = dotImages[i];
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Color MoodColor(MusicMood mood)
        {
            switch (mood)
            {
                case MusicMood.Peaceful:    return new Color(0.4f, 0.8f, 0.4f);
                case MusicMood.Cruising:    return new Color(0.4f, 0.6f, 1.0f);
                case MusicMood.Adventurous: return new Color(1.0f, 0.7f, 0.2f);
                case MusicMood.Tense:       return new Color(1.0f, 0.4f, 0.1f);
                case MusicMood.Danger:      return new Color(1.0f, 0.1f, 0.1f);
                case MusicMood.Epic:        return new Color(0.6f, 0.2f, 1.0f);
                case MusicMood.Serene:      return new Color(1.0f, 0.9f, 0.5f);
                case MusicMood.Mysterious:  return new Color(0.3f, 0.3f, 0.6f);
                case MusicMood.Triumphant:  return new Color(1.0f, 0.8f, 0.0f);
                default:                    return Color.white;
            }
        }
    }
}
