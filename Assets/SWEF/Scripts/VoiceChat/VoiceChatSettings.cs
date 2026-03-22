using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SWEF.Localization;
using SWEF.Settings;

namespace SWEF.VoiceChat
{
    /// <summary>
    /// Settings panel for the Voice Chat system.
    /// <para>
    /// Exposes controls for microphone device selection, input sensitivity,
    /// output volume, codec/quality selection, noise suppression, echo
    /// cancellation, AGC, push-to-talk key binding, spatial audio, music
    /// duck amount, proximity range, and a test-microphone feature.
    /// </para>
    /// </summary>
    public class VoiceChatSettings : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Microphone Device")]
        [Tooltip("Dropdown listing available microphone devices.")]
        [SerializeField] private Dropdown micDeviceDropdown;

        [Header("Mode")]
        [Tooltip("Dropdown for selecting voice chat mode (Off/PTT/VoiceActivated/AlwaysOn).")]
        [SerializeField] private Dropdown modeDropdown;

        [Header("Input Sensitivity")]
        [Tooltip("Slider controlling the voice-activation threshold (0–1).")]
        [SerializeField] private Slider sensitivitySlider;

        [Tooltip("Label displaying the current sensitivity value.")]
        [SerializeField] private Text sensitivityLabel;

        [Header("Output Volume")]
        [Tooltip("Slider controlling master playback volume (0–1).")]
        [SerializeField] private Slider outputVolumeSlider;

        [Tooltip("Label displaying the current output volume.")]
        [SerializeField] private Text outputVolumeLabel;

        [Header("Quality / Codec")]
        [Tooltip("Dropdown for codec selection (Opus/Speex/PCM).")]
        [SerializeField] private Dropdown codecDropdown;

        [Tooltip("Dropdown for quality/sample-rate selection.")]
        [SerializeField] private Dropdown qualityDropdown;

        [Header("Processing Toggles")]
        [Tooltip("Toggle for noise suppression.")]
        [SerializeField] private Toggle noiseSuppressionToggle;

        [Tooltip("Toggle for echo cancellation.")]
        [SerializeField] private Toggle echoCancellationToggle;

        [Tooltip("Toggle for auto gain control.")]
        [SerializeField] private Toggle autoGainToggle;

        [Header("Spatial Audio")]
        [Tooltip("Toggle for 3D spatial audio.")]
        [SerializeField] private Toggle spatialAudioToggle;

        [Header("Push-to-Talk")]
        [Tooltip("Button that captures the next keypress as the PTT binding.")]
        [SerializeField] private Button pttBindButton;

        [Tooltip("Label displaying the current PTT key name.")]
        [SerializeField] private Text pttKeyLabel;

        [Header("Music Ducking")]
        [Tooltip("Toggle to enable/disable music ducking during voice activity.")]
        [SerializeField] private Toggle duckMusicToggle;

        [Tooltip("Slider controlling the duck amount (0–0.8).")]
        [SerializeField] private Slider duckAmountSlider;

        [Header("Proximity Range")]
        [Tooltip("Slider controlling the maximum proximity voice range (0–1000 m).")]
        [SerializeField] private Slider proximityRangeSlider;

        [Tooltip("Label displaying the current proximity range.")]
        [SerializeField] private Text proximityRangeLabel;

        [Header("Test Microphone")]
        [Tooltip("Button to begin a microphone test recording.")]
        [SerializeField] private Button testMicButton;

        [Tooltip("Button to play back the test recording.")]
        [SerializeField] private Button playTestButton;

        [Tooltip("Label displaying test microphone status.")]
        [SerializeField] private Text testMicStatusLabel;

        [Header("References (auto-found if null)")]
        [SerializeField] private VoiceChatManager voiceChatManager;
        #endregion

        #region Internal State
        private bool     _awaitingKeyBind = false;
        private AudioClip _testClip;
        private AudioSource _testSource;
        private Coroutine   _testCoroutine;
        private const int   TestRecordSeconds = 3;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (voiceChatManager == null)
                voiceChatManager = VoiceChatManager.Instance;

            _testSource = gameObject.AddComponent<AudioSource>();
        }

        private void Start()
        {
            PopulateMicDevices();
            PopulateModeDropdown();
            PopulateCodecDropdown();
            PopulateQualityDropdown();
            BindControls();
            RefreshFromConfig();
            ApplyLocalization();
        }

        private void Update()
        {
            if (_awaitingKeyBind && Input.anyKeyDown)
            {
                foreach (KeyCode kc in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(kc))
                    {
                        voiceChatManager.Config.pushToTalkKey = kc;
                        if (pttKeyLabel != null)
                            pttKeyLabel.text = kc.ToString();
                        _awaitingKeyBind = false;
                        voiceChatManager.SaveConfig();
                        break;
                    }
                }
            }
        }
        #endregion

        #region Populate Dropdowns
        private void PopulateMicDevices()
        {
            if (micDeviceDropdown == null) return;
            micDeviceDropdown.ClearOptions();
            var options = new List<Dropdown.OptionData>
                { new Dropdown.OptionData(GetLocalizedText("voice_chat_microphone") + " (Default)") };
            foreach (string device in Microphone.devices)
                options.Add(new Dropdown.OptionData(device));
            micDeviceDropdown.AddOptions(options);
        }

        private void PopulateModeDropdown()
        {
            if (modeDropdown == null) return;
            modeDropdown.ClearOptions();
            modeDropdown.AddOptions(new List<string>
            {
                "Off",
                GetLocalizedText("voice_chat_push_to_talk"),
                GetLocalizedText("voice_chat_voice_activated"),
                "Always On"
            });
        }

        private void PopulateCodecDropdown()
        {
            if (codecDropdown == null) return;
            codecDropdown.ClearOptions();
            codecDropdown.AddOptions(new List<string> { "Opus", "Speex", "PCM" });
        }

        private void PopulateQualityDropdown()
        {
            if (qualityDropdown == null) return;
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new List<string>
            {
                GetLocalizedText("voice_chat_quality") + " Low (8kHz)",
                GetLocalizedText("voice_chat_quality") + " Medium (16kHz)",
                GetLocalizedText("voice_chat_quality") + " High (24kHz)",
                GetLocalizedText("voice_chat_quality") + " Ultra (48kHz)"
            });
        }
        #endregion

        #region Control Binding
        private void BindControls()
        {
            if (micDeviceDropdown != null)
                micDeviceDropdown.onValueChanged.AddListener(OnMicDeviceChanged);
            if (modeDropdown != null)
                modeDropdown.onValueChanged.AddListener(v =>
                    { voiceChatManager.Config.mode = (VoiceChatMode)v; SaveConfig(); });
            if (sensitivitySlider != null)
                sensitivitySlider.onValueChanged.AddListener(v =>
                    { voiceChatManager.Config.voiceActivationThreshold = v;
                      if (sensitivityLabel) sensitivityLabel.text = v.ToString("F2");
                      SaveConfig(); });
            if (outputVolumeSlider != null)
                outputVolumeSlider.onValueChanged.AddListener(v =>
                    { voiceChatManager.Config.masterVolume = v;
                      if (outputVolumeLabel) outputVolumeLabel.text = Mathf.RoundToInt(v * 100) + "%";
                      SaveConfig(); });
            if (codecDropdown != null)
                codecDropdown.onValueChanged.AddListener(v =>
                    { voiceChatManager.Config.codec = (VoiceCodec)v; SaveConfig(); });
            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.AddListener(v =>
                    { voiceChatManager.Config.quality = (VoiceQuality)v; SaveConfig(); });
            if (noiseSuppressionToggle != null)
                noiseSuppressionToggle.onValueChanged.AddListener(v =>
                    { voiceChatManager.Config.noiseSuppression = v; SaveConfig(); });
            if (echoCancellationToggle != null)
                echoCancellationToggle.onValueChanged.AddListener(v =>
                    { voiceChatManager.Config.echoCancellation = v; SaveConfig(); });
            if (autoGainToggle != null)
                autoGainToggle.onValueChanged.AddListener(v =>
                    { voiceChatManager.Config.autoGainControl = v; SaveConfig(); });
            if (spatialAudioToggle != null)
                spatialAudioToggle.onValueChanged.AddListener(v =>
                    { voiceChatManager.Config.spatialAudioEnabled = v; SaveConfig(); });
            if (duckMusicToggle != null)
                duckMusicToggle.onValueChanged.AddListener(v =>
                    { voiceChatManager.Config.duckMusicOnVoice = v; SaveConfig(); });
            if (duckAmountSlider != null)
                duckAmountSlider.onValueChanged.AddListener(v =>
                    { voiceChatManager.Config.duckAmount = Mathf.Clamp(v, 0f, 0.8f); SaveConfig(); });
            if (proximityRangeSlider != null)
                proximityRangeSlider.onValueChanged.AddListener(v =>
                    { voiceChatManager.Config.maxProximityRange = v;
                      if (proximityRangeLabel) proximityRangeLabel.text = Mathf.RoundToInt(v) + " m";
                      SaveConfig(); });
            if (pttBindButton != null)
                pttBindButton.onClick.AddListener(StartKeyBind);
            if (testMicButton != null)
                testMicButton.onClick.AddListener(StartMicTest);
            if (playTestButton != null)
                playTestButton.onClick.AddListener(PlayTestRecording);
        }

        private void OnMicDeviceChanged(int idx)
        {
            if (voiceChatManager == null) return;
            string device = idx == 0 ? string.Empty : Microphone.devices[idx - 1];
            voiceChatManager.Config.inputDevice = device;
            voiceChatManager.StopCapture();
            voiceChatManager.StartCapture();
            SaveConfig();
        }
        #endregion

        #region Refresh
        private void RefreshFromConfig()
        {
            if (voiceChatManager == null) return;
            VoiceChatConfig cfg = voiceChatManager.Config;

            if (modeDropdown != null)           modeDropdown.value          = (int)cfg.mode;
            if (sensitivitySlider != null)      sensitivitySlider.value     = cfg.voiceActivationThreshold;
            if (sensitivityLabel != null)       sensitivityLabel.text       = cfg.voiceActivationThreshold.ToString("F2");
            if (outputVolumeSlider != null)     outputVolumeSlider.value    = cfg.masterVolume;
            if (outputVolumeLabel != null)      outputVolumeLabel.text      = Mathf.RoundToInt(cfg.masterVolume * 100) + "%";
            if (codecDropdown != null)          codecDropdown.value         = (int)cfg.codec;
            if (qualityDropdown != null)        qualityDropdown.value       = (int)cfg.quality;
            if (noiseSuppressionToggle != null) noiseSuppressionToggle.isOn = cfg.noiseSuppression;
            if (echoCancellationToggle != null) echoCancellationToggle.isOn = cfg.echoCancellation;
            if (autoGainToggle != null)         autoGainToggle.isOn         = cfg.autoGainControl;
            if (spatialAudioToggle != null)     spatialAudioToggle.isOn     = cfg.spatialAudioEnabled;
            if (duckMusicToggle != null)        duckMusicToggle.isOn        = cfg.duckMusicOnVoice;
            if (duckAmountSlider != null)       duckAmountSlider.value      = cfg.duckAmount;
            if (proximityRangeSlider != null)   proximityRangeSlider.value  = cfg.maxProximityRange;
            if (proximityRangeLabel != null)    proximityRangeLabel.text    = Mathf.RoundToInt(cfg.maxProximityRange) + " m";
            if (pttKeyLabel != null)            pttKeyLabel.text            = cfg.pushToTalkKey.ToString();
        }

        private void SaveConfig() => voiceChatManager?.SaveConfig();
        #endregion

        #region PTT Key Binding
        private void StartKeyBind()
        {
            _awaitingKeyBind = true;
            if (pttKeyLabel != null)
                pttKeyLabel.text = "...";
        }
        #endregion

        #region Test Microphone
        private void StartMicTest()
        {
            if (_testCoroutine != null) StopCoroutine(_testCoroutine);
            _testCoroutine = StartCoroutine(RecordTestCoroutine());
        }

        private IEnumerator RecordTestCoroutine()
        {
            if (testMicStatusLabel != null)
                testMicStatusLabel.text = GetLocalizedText("voice_chat_test_mic") + "…";

            _testClip = Microphone.Start(null, false, TestRecordSeconds, 16000);
            yield return new WaitForSeconds(TestRecordSeconds);
            Microphone.End(null);

            if (testMicStatusLabel != null)
                testMicStatusLabel.text = "✓ Recorded";
        }

        private void PlayTestRecording()
        {
            if (_testClip == null || _testSource == null) return;
            _testSource.clip = _testClip;
            _testSource.Play();
        }
        #endregion

        #region Localization
        private void ApplyLocalization() { /* Label text set inline via GetLocalizedText */ }

        private string GetLocalizedText(string key)
        {
            var loc = LocalizationManager.Instance;
            return loc != null ? loc.GetText(key) : key;
        }
        #endregion
    }
}
