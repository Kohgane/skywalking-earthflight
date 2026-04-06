// AudioAccessibilityUI.cs — Phase 118: Spatial Audio & 3D Soundscape
// Accessibility: visual sound indicators, subtitle system for radio/ATC, mono audio.
// Namespace: SWEF.SpatialAudio

using UnityEngine;
using UnityEngine.UI;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Provides audio accessibility features: visual sound indicators for hearing-impaired
    /// players, subtitle display for radio and ATC communications, and mono audio mode.
    /// </summary>
    public class AudioAccessibilityUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Subtitle Display")]
        [SerializeField] private Text   subtitleText;
        [SerializeField] private float  subtitleDisplayDuration = 4f;

        [Header("Visual Indicators")]
        [SerializeField] private Image  engineIndicator;
        [SerializeField] private Image  warningIndicator;
        [SerializeField] private Image  radioIndicator;

        [Header("Accessibility Options")]
        [SerializeField] private Toggle monoAudioToggle;
        [SerializeField] private Toggle subtitlesToggle;
        [SerializeField] private Toggle visualIndicatorsToggle;

        // ── State ─────────────────────────────────────────────────────────────────

        private float _subtitleTimer;
        private bool  _monoEnabled;
        private bool  _subtitlesEnabled = true;
        private bool  _visualEnabled    = true;

        /// <summary>Whether mono audio output is enabled.</summary>
        public bool MonoAudioEnabled => _monoEnabled;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            monoAudioToggle?.onValueChanged.AddListener(OnMonoToggled);
            subtitlesToggle?.onValueChanged.AddListener(v => _subtitlesEnabled = v);
            visualIndicatorsToggle?.onValueChanged.AddListener(v => _visualEnabled = v);
        }

        private void Update()
        {
            if (_subtitleTimer > 0f)
            {
                _subtitleTimer -= Time.deltaTime;
                if (_subtitleTimer <= 0f && subtitleText != null)
                    subtitleText.text = string.Empty;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Displays a subtitle for the given text and optional duration.</summary>
        public void ShowSubtitle(string text, float duration = -1f)
        {
            if (!_subtitlesEnabled || subtitleText == null) return;
            subtitleText.text = text;
            _subtitleTimer    = duration > 0f ? duration : subtitleDisplayDuration;
        }

        /// <summary>Sets the visual warning indicator flash state.</summary>
        public void SetWarningIndicator(bool active)
        {
            if (!_visualEnabled || warningIndicator == null) return;
            warningIndicator.color = active ? Color.red : new Color(1f, 0f, 0f, 0.2f);
        }

        /// <summary>Sets the radio transmission visual indicator.</summary>
        public void SetRadioIndicator(bool transmitting)
        {
            if (!_visualEnabled || radioIndicator == null) return;
            radioIndicator.color = transmitting ? Color.green : new Color(0f, 1f, 0f, 0.2f);
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void OnMonoToggled(bool enabled)
        {
            _monoEnabled = enabled;
            AudioSettings.speakerMode = enabled ? AudioSpeakerMode.Mono : AudioSpeakerMode.Stereo;
        }
    }
}
