using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SWEF.UI;
using SWEF.Localization;
using SWEF.Accessibility;

namespace SWEF.VoiceChat
{
    /// <summary>
    /// HUD overlay UI for the Voice Chat system.
    /// <para>
    /// Displays microphone status, active speaker list, channel selector,
    /// per-participant volume sliders, push-to-talk indicator, and a voice
    /// activity level meter.
    /// </para>
    /// </summary>
    public class VoiceChatUI : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Mic Status")]
        [Tooltip("Image that shows the microphone icon (enabled/disabled/speaking states).")]
        [SerializeField] private Image micStatusIcon;

        [Tooltip("Sprite shown when the microphone is active.")]
        [SerializeField] private Sprite spriteMicActive;

        [Tooltip("Sprite shown when the microphone is muted.")]
        [SerializeField] private Sprite spriteMicMuted;

        [Tooltip("Sprite shown while the local player is transmitting/speaking.")]
        [SerializeField] private Sprite spriteMicSpeaking;

        [Tooltip("Label for the mute/deafen status text.")]
        [SerializeField] private Text micStatusLabel;

        [Header("Voice Activity Meter")]
        [Tooltip("Slider used as a voice-activity level meter (0–1).")]
        [SerializeField] private Slider voiceActivitySlider;

        [Header("Active Speakers")]
        [Tooltip("Parent transform under which speaker entries are instantiated.")]
        [SerializeField] private Transform speakerListParent;

        [Tooltip("Prefab instantiated for each active speaker entry.")]
        [SerializeField] private GameObject speakerEntryPrefab;

        [Header("Channel Selector")]
        [Tooltip("Dropdown for switching between voice channels.")]
        [SerializeField] private Dropdown channelDropdown;

        [Header("Push-to-Talk Indicator")]
        [Tooltip("Visual indicator shown while push-to-talk key is held.")]
        [SerializeField] private GameObject pushToTalkIndicator;

        [Header("Participant Volume Sliders")]
        [Tooltip("Scroll view content parent for per-participant volume sliders.")]
        [SerializeField] private Transform participantVolumeParent;

        [Tooltip("Prefab containing a participant volume slider entry.")]
        [SerializeField] private GameObject participantVolumePrefab;

        [Header("References (auto-found if null)")]
        [SerializeField] private VoiceChatManager voiceChatManager;
        [SerializeField] private HudBinder hudBinder;
        #endregion

        #region Internal State
        private readonly Dictionary<string, GameObject> _speakerEntries   = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, GameObject> _volumeSliders     = new Dictionary<string, GameObject>();
        private VoiceAudioProcessor _processor;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (voiceChatManager == null)
                voiceChatManager = VoiceChatManager.Instance;
        }

        private void Start()
        {
            SubscribeEvents();
            SetupChannelDropdown();
            ApplyLocalization();
            ApplyAccessibility();
        }

        private void Update()
        {
            UpdateVoiceActivityMeter();
            UpdateMicStatus();
            UpdatePushToTalkIndicator();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }
        #endregion

        #region Event Subscription
        private void SubscribeEvents()
        {
            if (voiceChatManager == null) return;
            voiceChatManager.OnParticipantJoined   += OnParticipantJoined;
            voiceChatManager.OnParticipantLeft     += OnParticipantLeft;
            voiceChatManager.OnParticipantSpeaking += OnParticipantSpeaking;
            voiceChatManager.OnChannelChanged      += OnChannelChanged;
            voiceChatManager.OnMicMuted            += RefreshMicStatusImmediate;
            voiceChatManager.OnMicUnmuted          += RefreshMicStatusImmediate;
        }

        private void UnsubscribeEvents()
        {
            if (voiceChatManager == null) return;
            voiceChatManager.OnParticipantJoined   -= OnParticipantJoined;
            voiceChatManager.OnParticipantLeft     -= OnParticipantLeft;
            voiceChatManager.OnParticipantSpeaking -= OnParticipantSpeaking;
            voiceChatManager.OnChannelChanged      -= OnChannelChanged;
            voiceChatManager.OnMicMuted            -= RefreshMicStatusImmediate;
            voiceChatManager.OnMicUnmuted          -= RefreshMicStatusImmediate;
        }
        #endregion

        #region UI Updates
        private void UpdateMicStatus()
        {
            if (micStatusIcon == null || voiceChatManager == null) return;

            if (voiceChatManager.IsLocalMuted)
            {
                micStatusIcon.sprite = spriteMicMuted;
                SetLabelText(micStatusLabel, "voice_chat_muted");
            }
            else if (voiceChatManager.IsSpeaking)
            {
                micStatusIcon.sprite = spriteMicSpeaking;
                SetLabelText(micStatusLabel, "voice_chat_speaking");
            }
            else if (voiceChatManager.IsCapturing)
            {
                micStatusIcon.sprite = spriteMicActive;
                SetLabelText(micStatusLabel, "voice_chat_unmuted");
            }
        }

        private void UpdateVoiceActivityMeter()
        {
            if (voiceActivitySlider == null || voiceChatManager == null) return;
            voiceActivitySlider.value = voiceChatManager.IsCapturing
                ? voiceChatManager.VoiceActivityLevel
                : 0f;
        }

        private void UpdatePushToTalkIndicator()
        {
            if (pushToTalkIndicator == null || voiceChatManager == null) return;
            bool pttHeld = voiceChatManager.Config.mode == VoiceChatMode.PushToTalk
                           && Input.GetKey(voiceChatManager.Config.pushToTalkKey);
            pushToTalkIndicator.SetActive(pttHeld);
        }

        private void OnParticipantJoined(VoiceChatParticipant participant)
        {
            AddSpeakerEntry(participant);
            AddVolumeSlider(participant);
        }

        private void OnParticipantLeft(VoiceChatParticipant participant)
        {
            RemoveSpeakerEntry(participant.participantId);
            RemoveVolumeSlider(participant.participantId);
        }

        private void OnParticipantSpeaking(VoiceChatParticipant participant, bool speaking)
        {
            if (_speakerEntries.TryGetValue(participant.participantId, out GameObject entry))
                entry.SetActive(speaking);
        }

        private void OnChannelChanged(VoiceChannel channel)
        {
            if (channelDropdown != null)
                channelDropdown.value = (int)channel;
        }

        private void RefreshMicStatusImmediate() { /* Force-update on next frame via Update */ }
        #endregion

        #region Speaker List
        private void AddSpeakerEntry(VoiceChatParticipant participant)
        {
            if (speakerListParent == null || speakerEntryPrefab == null) return;
            if (_speakerEntries.ContainsKey(participant.participantId)) return;

            GameObject entry = Instantiate(speakerEntryPrefab, speakerListParent);
            var nameLabel = entry.GetComponentInChildren<Text>();
            if (nameLabel != null) nameLabel.text = participant.displayName;
            entry.SetActive(participant.isSpeaking);
            _speakerEntries[participant.participantId] = entry;
        }

        private void RemoveSpeakerEntry(string participantId)
        {
            if (_speakerEntries.TryGetValue(participantId, out GameObject entry))
            {
                Destroy(entry);
                _speakerEntries.Remove(participantId);
            }
        }
        #endregion

        #region Volume Sliders
        private void AddVolumeSlider(VoiceChatParticipant participant)
        {
            if (participantVolumeParent == null || participantVolumePrefab == null) return;
            if (_volumeSliders.ContainsKey(participant.participantId)) return;

            GameObject sliderGo = Instantiate(participantVolumePrefab, participantVolumeParent);
            var slider = sliderGo.GetComponentInChildren<Slider>();
            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.value    = participant.volume;
                string id = participant.participantId;
                slider.onValueChanged.AddListener(v =>
                    voiceChatManager?.SetParticipantVolume(id, v));
            }
            _volumeSliders[participant.participantId] = sliderGo;
        }

        private void RemoveVolumeSlider(string participantId)
        {
            if (_volumeSliders.TryGetValue(participantId, out GameObject go))
            {
                Destroy(go);
                _volumeSliders.Remove(participantId);
            }
        }
        #endregion

        #region Channel Dropdown
        private void SetupChannelDropdown()
        {
            if (channelDropdown == null) return;
            channelDropdown.ClearOptions();

            var options = new System.Collections.Generic.List<Dropdown.OptionData>
            {
                new Dropdown.OptionData(GetLocalizedText("voice_chat_channel_proximity")),
                new Dropdown.OptionData(GetLocalizedText("voice_chat_channel_team")),
                new Dropdown.OptionData(GetLocalizedText("voice_chat_channel_global")),
                new Dropdown.OptionData(GetLocalizedText("voice_chat_channel_private")),
                new Dropdown.OptionData(GetLocalizedText("voice_chat_channel_atc")),
            };
            channelDropdown.AddOptions(options);
            channelDropdown.onValueChanged.AddListener(idx =>
                voiceChatManager?.JoinChannel((VoiceChannel)idx));
        }
        #endregion

        #region Localization & Accessibility
        private void ApplyLocalization()
        {
            SetLabelText(micStatusLabel, "voice_chat_unmuted");
        }

        private void ApplyAccessibility()
        {
            var acc = AccessibilityManager.Instance;
            if (acc == null) return;

            float scale = acc.Profile.uiScale;
            if (scale > 0f)
            {
                var rt = GetComponent<RectTransform>();
                if (rt != null) rt.localScale = Vector3.one * scale;
            }
        }

        private void SetLabelText(Text label, string key)
        {
            if (label == null) return;
            label.text = GetLocalizedText(key);
        }

        private string GetLocalizedText(string key)
        {
            var loc = LocalizationManager.Instance;
            return loc != null ? loc.GetText(key) : key;
        }
        #endregion
    }
}
