// AdaptiveMusicUI.cs — SWEF Dynamic Soundtrack & Adaptive Music System (Phase 83)
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SWEF.AdaptiveMusic
{
    /// <summary>
    /// Full-screen settings/library panel for the Adaptive Music System.
    /// <list type="bullet">
    ///   <item>Enable/disable adaptive music.</item>
    ///   <item>Master volume slider.</item>
    ///   <item>Crossfade speed preference.</item>
    ///   <item>Mood sensitivity slider.</item>
    ///   <item>Music mode selector (adaptive / playlist / hybrid).</item>
    ///   <item>Profile selector (if multiple profiles available).</item>
    ///   <item>Preview mode — listen to each mood at configurable intensities.</item>
    /// </list>
    /// </summary>
    public class AdaptiveMusicUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("References")]
        [SerializeField] private AdaptiveMusicManager adaptiveManager;
        [SerializeField] private GameObject           panelRoot;

        [Header("Controls")]
        [SerializeField] private Toggle               adaptiveEnabledToggle;
        [SerializeField] private Slider               masterVolumeSlider;
        [SerializeField] private Slider               crossfadeSpeedSlider;
        [SerializeField] private Slider               moodSensitivitySlider;
        [SerializeField] private TMP_Dropdown         musicModeDropdown;
        [SerializeField] private TMP_Dropdown         profileDropdown;

        [Header("Preview Mode")]
        [SerializeField] private Toggle               previewModeToggle;
        [SerializeField] private TMP_Dropdown         previewMoodDropdown;
        [SerializeField] private Slider               previewIntensitySlider;

        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI      titleLabel;
        [SerializeField] private TextMeshProUGUI      masterVolumeLabel;
        [SerializeField] private TextMeshProUGUI      crossfadeSpeedLabel;
        [SerializeField] private TextMeshProUGUI      moodSensitivityLabel;

        // ── PlayerPrefs keys ──────────────────────────────────────────────────────
        private const string PrefMasterVolume    = "SWEF_Music_MasterVolume";
        private const string PrefCrossfadeSpeed  = "SWEF_Music_CrossfadeSpeed";
        private const string PrefMoodSensitivity = "SWEF_Music_MoodSensitivity";

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (adaptiveManager == null)
                adaptiveManager = FindFirstObjectByType<AdaptiveMusicManager>();

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void Start()
        {
            PopulateMoodDropdown();
            SyncControlsToState();
            RegisterCallbacks();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Opens or closes the settings panel.</summary>
        public void SetVisible(bool visible)
        {
            if (panelRoot != null)
                panelRoot.SetActive(visible);

            if (visible)
                SyncControlsToState();
        }

        /// <summary>Toggles panel visibility.</summary>
        public void Toggle() => SetVisible(panelRoot != null && !panelRoot.activeSelf);

        // ── Internals ─────────────────────────────────────────────────────────────

        private void SyncControlsToState()
        {
            if (adaptiveManager == null) return;

            if (adaptiveEnabledToggle != null)
                adaptiveEnabledToggle.isOn = adaptiveManager.IsEnabled;

            if (masterVolumeSlider != null)
                masterVolumeSlider.value = PlayerPrefs.GetFloat(PrefMasterVolume, 1f);

            if (crossfadeSpeedSlider != null)
                crossfadeSpeedSlider.value = PlayerPrefs.GetFloat(PrefCrossfadeSpeed, 1f);

            if (moodSensitivitySlider != null)
                moodSensitivitySlider.value = PlayerPrefs.GetFloat(PrefMoodSensitivity, 1f);

            if (musicModeDropdown != null)
                musicModeDropdown.value = (int)adaptiveManager.Mode;
        }

        private void RegisterCallbacks()
        {
            if (adaptiveEnabledToggle != null)
                adaptiveEnabledToggle.onValueChanged.AddListener(v => adaptiveManager?.SetEnabled(v));

            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(v =>
                {
                    adaptiveManager?.SetMasterVolume(v);
                    if (masterVolumeLabel != null)
                        masterVolumeLabel.text = $"{v * 100f:0}%";
                });

            if (crossfadeSpeedSlider != null)
                crossfadeSpeedSlider.onValueChanged.AddListener(v =>
                {
                    PlayerPrefs.SetFloat(PrefCrossfadeSpeed, v);
                    if (crossfadeSpeedLabel != null)
                        crossfadeSpeedLabel.text = $"{v:0.0}×";
                });

            if (moodSensitivitySlider != null)
                moodSensitivitySlider.onValueChanged.AddListener(v =>
                {
                    PlayerPrefs.SetFloat(PrefMoodSensitivity, v);
                    if (moodSensitivityLabel != null)
                        moodSensitivityLabel.text = $"{v:0.0}×";
                });

            if (musicModeDropdown != null)
                musicModeDropdown.onValueChanged.AddListener(v =>
                    adaptiveManager?.SetMode((MusicMode)v));

            if (previewModeToggle != null)
                previewModeToggle.onValueChanged.AddListener(OnPreviewToggled);

            if (previewMoodDropdown != null)
                previewMoodDropdown.onValueChanged.AddListener(_ => ApplyPreview());

            if (previewIntensitySlider != null)
                previewIntensitySlider.onValueChanged.AddListener(_ => ApplyPreview());
        }

        private void PopulateMoodDropdown()
        {
            if (previewMoodDropdown == null) return;

            previewMoodDropdown.ClearOptions();
            foreach (MusicMood mood in System.Enum.GetValues(typeof(MusicMood)))
                previewMoodDropdown.options.Add(new TMP_Dropdown.OptionData(mood.ToString()));
            previewMoodDropdown.RefreshShownValue();
        }

        private void OnPreviewToggled(bool active)
        {
            if (active)
                ApplyPreview();
        }

        private void ApplyPreview()
        {
            if (previewModeToggle == null || !previewModeToggle.isOn) return;
            if (adaptiveManager == null) return;

            int moodIndex = previewMoodDropdown != null ? previewMoodDropdown.value : 0;
            MusicMood mood = (MusicMood)moodIndex;

            float intensity = previewIntensitySlider != null ? previewIntensitySlider.value : 0.5f;

            adaptiveManager.SetMood(mood);
            adaptiveManager.SetIntensity(intensity);
        }
    }
}
