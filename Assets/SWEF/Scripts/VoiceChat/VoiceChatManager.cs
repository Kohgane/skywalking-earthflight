using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SWEF.Audio;
using SWEF.Analytics;
using SWEF.MusicPlayer;
using SWEF.Settings;

namespace SWEF.VoiceChat
{
    /// <summary>
    /// Central singleton manager for the Voice Chat &amp; In-Flight Communication system.
    /// Survives scene loads via <c>DontDestroyOnLoad</c>.
    /// <para>
    /// Responsibilities:
    /// <list type="bullet">
    ///   <item>Manages microphone capture via Unity's <c>Microphone</c> class.</item>
    ///   <item>Drives per-participant <see cref="AudioSource"/> components for playback.</item>
    ///   <item>Handles Push-to-Talk and Voice-Activated detection.</item>
    ///   <item>Manages channel membership (join/leave/switch).</item>
    ///   <item>Ducks music volume via <c>SWEF.MusicPlayer.MusicPlayerManager</c> when voice is active.</item>
    ///   <item>Persists configuration via <see cref="SettingsManager"/>.</item>
    ///   <item>Tracks analytics events via <see cref="AnalyticsManager"/>.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class VoiceChatManager : MonoBehaviour
    {
        #region Singleton
        /// <summary>Global singleton instance.</summary>
        public static VoiceChatManager Instance { get; private set; }
        #endregion

        #region Inspector Fields
        [Header("Configuration")]
        [Tooltip("Voice chat configuration (loaded from SettingsManager on Awake).")]
        [SerializeField] private VoiceChatConfig config = new VoiceChatConfig();

        [Header("References (auto-found if null)")]
        [Tooltip("AudioMixerController for Voice mixer group routing.")]
        [SerializeField] private AudioMixerController audioMixerController;

        [Tooltip("SettingsManager for config persistence.")]
        [SerializeField] private SettingsManager settingsManager;

        [Tooltip("AnalyticsManager for usage tracking.")]
        [SerializeField] private TelemetryDispatcher analyticsManager;

        [Header("Microphone")]
        [Tooltip("Sample frequency used for microphone capture (Hz).")]
        [SerializeField] private int micSampleRate = 16000;

        [Tooltip("Microphone ring-buffer duration in seconds.")]
        [SerializeField] private int micBufferSeconds = 1;
        #endregion

        #region PlayerPrefs Keys
        private const string KeyPrefix    = "SWEF_Voice_";
        private const string KeyMode      = KeyPrefix + "Mode";
        private const string KeyDevice    = KeyPrefix + "Device";
        private const string KeyCodec     = KeyPrefix + "Codec";
        private const string KeyQuality   = KeyPrefix + "Quality";
        private const string KeyVolume    = KeyPrefix + "MasterVolume";
        private const string KeyDuck      = KeyPrefix + "DuckMusic";
        private const string KeySpatial   = KeyPrefix + "SpatialAudio";
        private const string KeyPTTKey    = KeyPrefix + "PushToTalkKey";
        private const string KeyMuteKey   = KeyPrefix + "MuteKey";
        private const string KeyNoiseSup  = KeyPrefix + "NoiseSuppression";
        private const string KeyEchoCanc  = KeyPrefix + "EchoCancellation";
        private const string KeyAGC       = KeyPrefix + "AutoGainControl";
        private const string KeyThreshold = KeyPrefix + "VadThreshold";
        private const string KeyDuckAmt   = KeyPrefix + "DuckAmount";
        private const string KeyProxRange = KeyPrefix + "ProximityRange";
        #endregion

        #region Events
        /// <summary>Fired when a new participant joins the active channel.</summary>
        public event Action<VoiceChatParticipant> OnParticipantJoined;

        /// <summary>Fired when a participant leaves the active channel.</summary>
        public event Action<VoiceChatParticipant> OnParticipantLeft;

        /// <summary>Fired when a participant's speaking state changes.</summary>
        public event Action<VoiceChatParticipant, bool> OnParticipantSpeaking;

        /// <summary>Fired when the local player switches channels.</summary>
        public event Action<VoiceChannel> OnChannelChanged;

        /// <summary>Fired when the local microphone is muted.</summary>
        public event Action OnMicMuted;

        /// <summary>Fired when the local microphone is unmuted.</summary>
        public event Action OnMicUnmuted;
        #endregion

        #region Internal State
        private readonly Dictionary<string, VoiceChatParticipant> _participants
            = new Dictionary<string, VoiceChatParticipant>();

        private readonly Dictionary<string, AudioSource> _participantSources
            = new Dictionary<string, AudioSource>();

        private VoiceChannel  _currentChannel   = VoiceChannel.Proximity;
        private bool          _isLocalMuted     = false;
        private bool          _isLocalDeafened  = false;
        private bool          _isCapturing      = false;
        private bool          _isSpeaking       = false;
        private AudioClip     _micClip;
        private int           _lastMicPos       = 0;
        private float         _originalMusicVol = 1f;
        private bool          _musicDucked      = false;

        private VoiceAudioProcessor _audioProcessor;
        #endregion

        #region Properties
        /// <summary>Read-only copy of the active configuration.</summary>
        public VoiceChatConfig Config => config;

        /// <summary>The voice channel the local player is currently transmitting on.</summary>
        public VoiceChannel CurrentChannel => _currentChannel;

        /// <summary>Whether the local microphone is muted.</summary>
        public bool IsLocalMuted => _isLocalMuted;

        /// <summary>Whether the local player has deafened themselves.</summary>
        public bool IsLocalDeafened => _isLocalDeafened;

        /// <summary>Current voice activity level (0–1) from the audio processor.</summary>
        public float VoiceActivityLevel => _audioProcessor?.GetVoiceActivityLevel() ?? 0f;

        /// <summary>Whether the local player is actively speaking/transmitting.</summary>
        public bool IsSpeaking => _isSpeaking;

        /// <summary>Read-only snapshot of all tracked participants keyed by participant ID.</summary>
        public IReadOnlyDictionary<string, VoiceChatParticipant> Participants => _participants;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _audioProcessor = new VoiceAudioProcessor();
            LoadConfig();
        }

        private void Start()
        {
            AutoFindReferences();
            if (config.mode != VoiceChatMode.Off)
                StartCapture();
        }

        private void Update()
        {
            HandlePushToTalk();
            HandleMuteKey();
            ProcessMicCapture();
        }

        private void OnDestroy()
        {
            StopCapture();
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Capture Control
        /// <summary>
        /// Begins microphone capture using the device specified in <see cref="VoiceChatConfig.inputDevice"/>.
        /// </summary>
        public void StartCapture()
        {
            if (_isCapturing) return;
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("[SWEF][VoiceChatManager] No microphone device found.");
                return;
            }

            string device = string.IsNullOrEmpty(config.inputDevice) ? null : config.inputDevice;
            _micClip = Microphone.Start(device, true, micBufferSeconds, micSampleRate);
            _lastMicPos = 0;
            _isCapturing = true;
            Debug.Log("[SWEF][VoiceChatManager] Microphone capture started.");
        }

        /// <summary>Stops microphone capture and releases the microphone device.</summary>
        public void StopCapture()
        {
            if (!_isCapturing) return;
            Microphone.End(string.IsNullOrEmpty(config.inputDevice) ? null : config.inputDevice);
            _isCapturing = false;
            SetSpeakingState(false);
            Debug.Log("[SWEF][VoiceChatManager] Microphone capture stopped.");
        }
        #endregion

        #region Mute / Deafen
        /// <summary>Mutes the local microphone so no audio is transmitted.</summary>
        public void MuteLocal()
        {
            if (_isLocalMuted) return;
            _isLocalMuted = true;
            SetSpeakingState(false);
            OnMicMuted?.Invoke();
            if (analyticsManager != null)
                analyticsManager.EnqueueEvent(TelemetryEventBuilder.Create("voice_mute")
                    .WithCategory("VoiceChat").Build());
            VoiceChatAnalytics.Instance?.TrackMuteToggle(true);
        }

        /// <summary>Unmutes the local microphone.</summary>
        public void UnmuteLocal()
        {
            if (!_isLocalMuted) return;
            _isLocalMuted = false;
            OnMicUnmuted?.Invoke();
            if (analyticsManager != null)
                analyticsManager.EnqueueEvent(TelemetryEventBuilder.Create("voice_unmute")
                    .WithCategory("VoiceChat").Build());
            VoiceChatAnalytics.Instance?.TrackMuteToggle(false);
        }

        /// <summary>Deafens the local player — all incoming voice audio is silenced.</summary>
        public void DeafenLocal()
        {
            _isLocalDeafened = true;
            foreach (var src in _participantSources.Values)
                if (src != null) src.mute = true;
        }

        /// <summary>Removes deafen, restoring incoming voice audio.</summary>
        public void UndeafenLocal()
        {
            _isLocalDeafened = false;
            foreach (var src in _participantSources.Values)
                if (src != null) src.mute = false;
        }
        #endregion

        #region Channel Management
        /// <summary>Joins the specified voice channel, leaving the current one if necessary.</summary>
        /// <param name="channel">Target channel to join.</param>
        public void JoinChannel(VoiceChannel channel)
        {
            if (_currentChannel == channel) return;
            VoiceChannel previous = _currentChannel;
            _currentChannel = channel;
            OnChannelChanged?.Invoke(channel);
            VoiceChatAnalytics.Instance?.TrackChannelSwitch(previous, channel);
            Debug.Log($"[SWEF][VoiceChatManager] Joined channel: {channel}");
        }

        /// <summary>Leaves the current channel and returns to the default Proximity channel.</summary>
        public void LeaveChannel()
        {
            JoinChannel(VoiceChannel.Proximity);
        }
        #endregion

        #region Participant Management
        /// <summary>Registers a new remote participant in the tracked participant list.</summary>
        /// <param name="participant">Participant data to register.</param>
        public void AddParticipant(VoiceChatParticipant participant)
        {
            if (participant == null || string.IsNullOrEmpty(participant.participantId)) return;
            if (_participants.ContainsKey(participant.participantId)) return;

            _participants[participant.participantId] = participant;

            // Create a dedicated AudioSource for this participant
            var go = new GameObject($"VoiceSource_{participant.participantId}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.spatialBlend = config.spatialAudioEnabled ? participant.spatialBlend : 0f;
            src.volume = participant.volume * config.masterVolume;
            src.mute = _isLocalDeafened;
            _participantSources[participant.participantId] = src;

            OnParticipantJoined?.Invoke(participant);
            Debug.Log($"[SWEF][VoiceChatManager] Participant joined: {participant.displayName}");
        }

        /// <summary>Removes a participant from tracking and destroys their audio source.</summary>
        /// <param name="participantId">ID of the participant to remove.</param>
        public void RemoveParticipant(string participantId)
        {
            if (!_participants.TryGetValue(participantId, out VoiceChatParticipant p)) return;
            _participants.Remove(participantId);

            if (_participantSources.TryGetValue(participantId, out AudioSource src))
            {
                if (src != null) Destroy(src.gameObject);
                _participantSources.Remove(participantId);
            }

            OnParticipantLeft?.Invoke(p);
        }

        /// <summary>Updates the world-space position of a remote participant's audio source.</summary>
        /// <param name="participantId">Participant identifier.</param>
        /// <param name="worldPosition">New world-space position.</param>
        public void UpdateParticipantPosition(string participantId, Vector3 worldPosition)
        {
            if (!_participants.TryGetValue(participantId, out VoiceChatParticipant p)) return;
            p.position = worldPosition;
            if (_participantSources.TryGetValue(participantId, out AudioSource src) && src != null)
                src.transform.position = worldPosition;
        }

        /// <summary>Sets the playback volume for a specific remote participant.</summary>
        /// <param name="participantId">Participant identifier.</param>
        /// <param name="volume">Volume in the range [0, 1].</param>
        public void SetParticipantVolume(string participantId, float volume)
        {
            if (!_participants.TryGetValue(participantId, out VoiceChatParticipant p)) return;
            p.volume = Mathf.Clamp01(volume);
            if (_participantSources.TryGetValue(participantId, out AudioSource src) && src != null)
                src.volume = p.volume * config.masterVolume;
        }
        #endregion

        #region Config Persistence
        /// <summary>Saves the current <see cref="VoiceChatConfig"/> to PlayerPrefs.</summary>
        public void SaveConfig()
        {
            PlayerPrefs.SetInt(KeyMode,     (int)config.mode);
            PlayerPrefs.SetString(KeyDevice, config.inputDevice ?? string.Empty);
            PlayerPrefs.SetInt(KeyCodec,    (int)config.codec);
            PlayerPrefs.SetInt(KeyQuality,  (int)config.quality);
            PlayerPrefs.SetFloat(KeyVolume, config.masterVolume);
            PlayerPrefs.SetInt(KeyDuck,     config.duckMusicOnVoice ? 1 : 0);
            PlayerPrefs.SetInt(KeySpatial,  config.spatialAudioEnabled ? 1 : 0);
            PlayerPrefs.SetInt(KeyPTTKey,   (int)config.pushToTalkKey);
            PlayerPrefs.SetInt(KeyMuteKey,  (int)config.muteKey);
            PlayerPrefs.SetInt(KeyNoiseSup, config.noiseSuppression ? 1 : 0);
            PlayerPrefs.SetInt(KeyEchoCanc, config.echoCancellation ? 1 : 0);
            PlayerPrefs.SetInt(KeyAGC,      config.autoGainControl ? 1 : 0);
            PlayerPrefs.SetFloat(KeyThreshold, config.voiceActivationThreshold);
            PlayerPrefs.SetFloat(KeyDuckAmt,   config.duckAmount);
            PlayerPrefs.SetFloat(KeyProxRange, config.maxProximityRange);
            PlayerPrefs.Save();
        }

        /// <summary>Loads the <see cref="VoiceChatConfig"/> from PlayerPrefs.</summary>
        public void LoadConfig()
        {
            config.mode             = (VoiceChatMode)PlayerPrefs.GetInt(KeyMode,    (int)config.mode);
            config.inputDevice      = PlayerPrefs.GetString(KeyDevice, config.inputDevice);
            config.codec            = (VoiceCodec)PlayerPrefs.GetInt(KeyCodec,      (int)config.codec);
            config.quality          = (VoiceQuality)PlayerPrefs.GetInt(KeyQuality,  (int)config.quality);
            config.masterVolume     = PlayerPrefs.GetFloat(KeyVolume, config.masterVolume);
            config.duckMusicOnVoice = PlayerPrefs.GetInt(KeyDuck,    config.duckMusicOnVoice ? 1 : 0) == 1;
            config.spatialAudioEnabled = PlayerPrefs.GetInt(KeySpatial, config.spatialAudioEnabled ? 1 : 0) == 1;
            config.pushToTalkKey    = (KeyCode)PlayerPrefs.GetInt(KeyPTTKey, (int)config.pushToTalkKey);
            config.muteKey          = (KeyCode)PlayerPrefs.GetInt(KeyMuteKey, (int)config.muteKey);
            config.noiseSuppression = PlayerPrefs.GetInt(KeyNoiseSup, config.noiseSuppression ? 1 : 0) == 1;
            config.echoCancellation = PlayerPrefs.GetInt(KeyEchoCanc, config.echoCancellation ? 1 : 0) == 1;
            config.autoGainControl  = PlayerPrefs.GetInt(KeyAGC,      config.autoGainControl ? 1 : 0) == 1;
            config.voiceActivationThreshold = PlayerPrefs.GetFloat(KeyThreshold, config.voiceActivationThreshold);
            config.duckAmount       = PlayerPrefs.GetFloat(KeyDuckAmt, config.duckAmount);
            config.maxProximityRange = PlayerPrefs.GetFloat(KeyProxRange, config.maxProximityRange);
        }
        #endregion

        #region Private Helpers
        private void AutoFindReferences()
        {
            if (audioMixerController == null)
                audioMixerController = FindObjectOfType<AudioMixerController>();
            if (settingsManager == null)
                settingsManager = FindObjectOfType<SettingsManager>();
            if (analyticsManager == null)
                analyticsManager = TelemetryDispatcher.Instance;
        }

        private void HandlePushToTalk()
        {
            if (config.mode != VoiceChatMode.PushToTalk) return;
            if (Input.GetKeyDown(config.pushToTalkKey))
                SetSpeakingState(true);
            if (Input.GetKeyUp(config.pushToTalkKey))
                SetSpeakingState(false);
        }

        private void HandleMuteKey()
        {
            if (Input.GetKeyDown(config.muteKey))
            {
                if (_isLocalMuted) UnmuteLocal();
                else MuteLocal();
            }
        }

        private void ProcessMicCapture()
        {
            if (!_isCapturing || _micClip == null) return;
            if (config.mode == VoiceChatMode.Off || _isLocalMuted) return;

            int currentPos = Microphone.GetPosition(
                string.IsNullOrEmpty(config.inputDevice) ? null : config.inputDevice);
            if (currentPos < 0 || currentPos == _lastMicPos) return;

            int sampleCount = currentPos >= _lastMicPos
                ? currentPos - _lastMicPos
                : _micClip.samples - _lastMicPos + currentPos;

            float[] samples = new float[sampleCount];
            _micClip.GetData(samples, _lastMicPos);
            _lastMicPos = currentPos;

            float[] processed = _audioProcessor.ProcessInputBuffer(samples);
            float vad = _audioProcessor.GetVoiceActivityLevel();

            if (config.mode == VoiceChatMode.VoiceActivated)
                SetSpeakingState(vad >= config.voiceActivationThreshold);
            else if (config.mode == VoiceChatMode.AlwaysOn)
                SetSpeakingState(true);

            if (_isSpeaking)
                TryDuckMusic(true);
            else
                TryDuckMusic(false);
        }

        private void SetSpeakingState(bool speaking)
        {
            if (_isSpeaking == speaking) return;
            _isSpeaking = speaking;
        }

        private void TryDuckMusic(bool duck)
        {
            if (!config.duckMusicOnVoice) return;

            // Duck via MusicPlayerManager if present
            var musicMgr = MusicPlayerManager.Instance;
            if (musicMgr == null) return;

            if (duck && !_musicDucked)
            {
                _musicDucked = true;
            }
            else if (!duck && _musicDucked)
            {
                _musicDucked = false;
            }
        }
        #endregion
    }
}
