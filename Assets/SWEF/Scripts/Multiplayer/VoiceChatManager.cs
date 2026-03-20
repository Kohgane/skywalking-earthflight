using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Multiplayer
{
    // ── Enums ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Voice channel scope controlling who hears a player's voice.
    /// </summary>
    public enum VoiceChannel
    {
        /// <summary>Spatial audio heard by all nearby players (distance attenuation).</summary>
        Proximity,
        /// <summary>Private team channel — always full volume for team members.</summary>
        Team,
        /// <summary>Global broadcast heard by all players at full volume.</summary>
        Global
    }

    /// <summary>
    /// Input trigger mode for the microphone.
    /// </summary>
    public enum VoiceInputMode
    {
        /// <summary>Microphone is active only while the push-to-talk key is held.</summary>
        PushToTalk,
        /// <summary>Microphone is always open; voice activity detection handles muting.</summary>
        OpenMic
    }

    // ── Data Classes ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Runtime state for a single voice participant.
    /// </summary>
    [Serializable]
    public class VoiceParticipant
    {
        /// <summary>Player identifier.</summary>
        public string playerId;
        /// <summary>Display name.</summary>
        public string displayName;
        /// <summary>Whether this participant is muted by the local player.</summary>
        public bool isMutedByLocal;
        /// <summary>Whether the participant has self-muted.</summary>
        public bool isSelfMuted;
        /// <summary>Current RMS amplitude of the participant's audio (0–1).</summary>
        public float currentAmplitude;
        /// <summary>True if the participant's amplitude exceeds the VAD threshold.</summary>
        public bool isSpeaking;
        /// <summary>World-space position (used for proximity attenuation).</summary>
        public Vector3 worldPosition;
        /// <summary>Team identifier (empty = no team).</summary>
        public string teamId;
        /// <summary>Unity AudioSource driving this participant's spatial audio.</summary>
        public AudioSource audioSource;
    }

    // ── VoiceChatManager ──────────────────────────────────────────────────────────

    /// <summary>
    /// Manages in-game voice communication for multiplayer sessions.
    ///
    /// <para>Features:
    /// <list type="bullet">
    /// <item>Proximity-based spatial audio with configurable falloff (default 500 m)</item>
    /// <item>Team voice channel at full volume regardless of distance</item>
    /// <item>Push-to-talk and open-mic modes with configurable keybind</item>
    /// <item>Voice activity detection (VAD) via amplitude threshold</item>
    /// <item>Per-player mute, global mute, self-mute</item>
    /// <item>Codec simulation (configurable bitrate)</item>
    /// <item><see cref="OnPlayerSpeaking"/> event for speaker UI indicators</item>
    /// </list>
    /// </para>
    /// </summary>
    public class VoiceChatManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static VoiceChatManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Microphone")]
        [Tooltip("Input mode: push-to-talk or open mic.")]
        [SerializeField] private VoiceInputMode inputMode = VoiceInputMode.PushToTalk;

        [Tooltip("Key used for push-to-talk.")]
        [SerializeField] private KeyCode pushToTalkKey = KeyCode.V;

        [Header("Voice Activity Detection")]
        [Tooltip("RMS amplitude threshold below which silence is detected (0–1).")]
        [SerializeField] private float vadThreshold = 0.02f;

        [Tooltip("Seconds of silence before VAD gates the microphone.")]
        [SerializeField] private float vadReleaseSec = 0.3f;

        [Header("Proximity Audio")]
        [Tooltip("Maximum range in metres for proximity voice attenuation.")]
        [SerializeField] private float proximityRangeM = 500f;

        [Tooltip("Minimum distance at which full volume is heard.")]
        [SerializeField] private float proximityMinDistM = 10f;

        [Tooltip("Audio rolloff exponent for proximity falloff (1 = linear, 2 = squared).")]
        [SerializeField] private float proximityRolloff = 1.5f;

        [Header("Audio Processing")]
        [Tooltip("Noise gate below this amplitude is clamped to silence.")]
        [SerializeField] private float noiseGateThreshold = 0.01f;

        [Tooltip("Target RMS amplitude after normalisation (0–1).")]
        [SerializeField] private float normalisationTarget = 0.5f;

        [Header("Codec Simulation")]
        [Tooltip("Simulated Opus-like codec bitrate in kbps.")]
        [SerializeField] private int codecBitrateKbps = 32;

        [Header("Channels")]
        [Tooltip("Active voice channel for the local player.")]
        [SerializeField] private VoiceChannel activeChannel = VoiceChannel.Proximity;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>
        /// Fired when a player's speaking state changes.
        /// Parameters: playerId, RMS amplitude (0 when silent).
        /// </summary>
        public event Action<string, float> OnPlayerSpeaking;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Whether the local microphone is currently transmitting.</summary>
        public bool IsTransmitting { get; private set; }

        /// <summary>Whether the local player has self-muted.</summary>
        public bool IsSelfMuted { get; private set; }

        /// <summary>Whether all remote players are muted by the local player.</summary>
        public bool IsAllMuted { get; private set; }

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly Dictionary<string, VoiceParticipant> _participants = new();
        private string _localPlayerId;
        private string _localTeamId;

        // Microphone capture state
        private AudioClip  _micClip;
        private bool       _micOpen;
        private int        _micLastSamplePos;
        private float      _vadReleaseTimer;
        private float[]    _sampleBuffer = new float[1024];

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _localPlayerId = Guid.NewGuid().ToString("N").Substring(0, 8);

            OpenMicrophone();
        }

        private void Update()
        {
            ProcessMicrophoneInput();
            UpdateProximityVolumes();
        }

        private void OnDestroy()
        {
            CloseMicrophone();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Registers a remote player as a voice participant.
        /// </summary>
        /// <param name="playerId">Remote player identifier.</param>
        /// <param name="displayName">Display name shown in UI.</param>
        /// <param name="teamId">Team identifier, or empty for no team.</param>
        public void AddParticipant(string playerId, string displayName, string teamId = "")
        {
            if (_participants.ContainsKey(playerId)) return;

            var participant = new VoiceParticipant
            {
                playerId    = playerId,
                displayName = displayName,
                teamId      = teamId
            };

            // Create a spatial AudioSource for this participant.
            var go = new GameObject($"VoiceAudio_{playerId}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.spatialBlend     = 1f; // full 3D
            src.minDistance      = proximityMinDistM;
            src.maxDistance      = proximityRangeM;
            src.rolloffMode      = AudioRolloffMode.Custom;
            src.loop             = true;
            participant.audioSource = src;

            _participants[playerId] = participant;
            Debug.Log($"[SWEF][VoiceChatManager] Added participant {displayName} ({playerId}).");
        }

        /// <summary>
        /// Removes a voice participant (e.g. when they disconnect).
        /// </summary>
        /// <param name="playerId">Participant to remove.</param>
        public void RemoveParticipant(string playerId)
        {
            if (!_participants.TryGetValue(playerId, out var p)) return;

            if (p.audioSource != null)
                Destroy(p.audioSource.gameObject);

            _participants.Remove(playerId);
        }

        /// <summary>
        /// Mutes or unmutes a specific remote player.
        /// </summary>
        /// <param name="playerId">Target player.</param>
        /// <param name="muted">True to mute, false to unmute.</param>
        public void SetMute(string playerId, bool muted)
        {
            if (!_participants.TryGetValue(playerId, out var p)) return;
            p.isMutedByLocal = muted;
            if (p.audioSource != null) p.audioSource.mute = muted || IsAllMuted;
        }

        /// <summary>
        /// Mutes or unmutes all remote participants simultaneously.
        /// </summary>
        /// <param name="muted">True to mute all, false to restore individual states.</param>
        public void SetMuteAll(bool muted)
        {
            IsAllMuted = muted;
            foreach (var p in _participants.Values)
                if (p.audioSource != null)
                    p.audioSource.mute = muted || p.isMutedByLocal;
        }

        /// <summary>
        /// Toggles the local player's self-mute state.
        /// </summary>
        public void ToggleSelfMute()
        {
            IsSelfMuted = !IsSelfMuted;
            Debug.Log($"[SWEF][VoiceChatManager] Self-mute: {IsSelfMuted}");
        }

        /// <summary>
        /// Switches the active voice channel.
        /// </summary>
        /// <param name="channel">New channel to transmit on.</param>
        public void SetActiveChannel(VoiceChannel channel)
        {
            activeChannel = channel;
            Debug.Log($"[SWEF][VoiceChatManager] Voice channel set to {channel}.");
        }

        /// <summary>
        /// Receives a compressed audio packet from a remote player.
        /// Decompresses and plays it through the participant's AudioSource.
        /// </summary>
        /// <param name="playerId">Sender's player ID.</param>
        /// <param name="compressedAudio">Simulated compressed audio buffer.</param>
        /// <param name="channel">Channel on which this packet was sent.</param>
        public void ReceiveAudioPacket(string playerId, byte[] compressedAudio, VoiceChannel channel)
        {
            if (!_participants.TryGetValue(playerId, out var p)) return;
            if (p.isMutedByLocal || IsAllMuted) return;

            // Decompress (simulation): convert bytes back to float samples.
            float[] samples = DecompressAudio(compressedAudio);
            float rms       = CalculateRms(samples);

            bool wasSpeaking = p.isSpeaking;
            p.isSpeaking      = rms > vadThreshold;
            p.currentAmplitude = rms;

            if (p.isSpeaking != wasSpeaking || p.isSpeaking)
                OnPlayerSpeaking?.Invoke(playerId, p.isSpeaking ? rms : 0f);

            // In production: create an AudioClip from samples and play on AudioSource.
        }

        /// <summary>
        /// Updates a remote player's world-space position for proximity volume calculations.
        /// </summary>
        /// <param name="playerId">Remote player identifier.</param>
        /// <param name="worldPosition">Current world position.</param>
        public void UpdateParticipantPosition(string playerId, Vector3 worldPosition)
        {
            if (!_participants.TryGetValue(playerId, out var p)) return;
            p.worldPosition = worldPosition;
            if (p.audioSource != null)
                p.audioSource.transform.position = worldPosition;
        }

        // ── Microphone handling ───────────────────────────────────────────────────

        private void OpenMicrophone()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("[SWEF][VoiceChatManager] No microphone detected.");
                return;
            }

            _micClip = Microphone.Start(null, true, 1, 44100);
            _micOpen = true;
            Debug.Log("[SWEF][VoiceChatManager] Microphone opened.");
        }

        private void CloseMicrophone()
        {
            if (_micOpen) Microphone.End(null);
            _micOpen = false;
        }

        private void ProcessMicrophoneInput()
        {
            if (!_micOpen || _micClip == null) return;
            if (IsSelfMuted) { IsTransmitting = false; return; }

            bool pttActive = inputMode == VoiceInputMode.PushToTalk && Input.GetKey(pushToTalkKey);
            bool openActive = inputMode == VoiceInputMode.OpenMic;

            if (!pttActive && !openActive) { IsTransmitting = false; return; }

            int currentPos = Microphone.GetPosition(null);
            if (currentPos < _micLastSamplePos) _micLastSamplePos = 0; // wrap-around

            int sampleCount = currentPos - _micLastSamplePos;
            if (sampleCount <= 0) return;

            if (_sampleBuffer.Length < sampleCount)
                _sampleBuffer = new float[sampleCount];

            _micClip.GetData(_sampleBuffer, _micLastSamplePos);
            _micLastSamplePos = currentPos;

            // Apply noise gate and normalisation.
            ProcessAudio(_sampleBuffer, sampleCount);

            float rms = CalculateRms(_sampleBuffer, sampleCount);

            // Voice activity detection.
            if (openActive)
            {
                if (rms < vadThreshold)
                {
                    _vadReleaseTimer += Time.deltaTime;
                    if (_vadReleaseTimer >= vadReleaseSec)
                        IsTransmitting = false;
                }
                else
                {
                    _vadReleaseTimer = 0f;
                    IsTransmitting = true;
                }
            }
            else
            {
                IsTransmitting = pttActive && (rms > noiseGateThreshold);
            }

            if (IsTransmitting)
            {
                byte[] compressed = CompressAudio(_sampleBuffer, sampleCount);
                // In production: send compressed to NetworkTransport.
                OnPlayerSpeaking?.Invoke(_localPlayerId, rms);
            }
        }

        // ── Proximity volume ──────────────────────────────────────────────────────

        private void UpdateProximityVolumes()
        {
            Vector3 localPos = transform.position;

            foreach (var p in _participants.Values)
            {
                if (p.audioSource == null || p.isMutedByLocal || IsAllMuted) continue;

                if (activeChannel == VoiceChannel.Team && p.teamId == _localTeamId && !string.IsNullOrEmpty(_localTeamId))
                {
                    p.audioSource.volume = 1f;
                    p.audioSource.spatialBlend = 0f;
                    continue;
                }

                float dist = Vector3.Distance(localPos, p.worldPosition);
                if (dist >= proximityRangeM)
                {
                    p.audioSource.volume = 0f;
                }
                else
                {
                    float t = Mathf.Clamp01((dist - proximityMinDistM) / (proximityRangeM - proximityMinDistM));
                    p.audioSource.volume = Mathf.Pow(1f - t, proximityRolloff);
                }

                p.audioSource.spatialBlend = 1f;
            }
        }

        // ── Audio processing helpers ──────────────────────────────────────────────

        private void ProcessAudio(float[] samples, int count)
        {
            // Noise gate
            float rms = CalculateRms(samples, count);
            if (rms < noiseGateThreshold)
            {
                Array.Clear(samples, 0, count);
                return;
            }

            // Normalise
            if (rms > 0.0001f)
            {
                float gain = normalisationTarget / rms;
                gain = Mathf.Min(gain, 4f); // cap at +12 dB to avoid distortion
                for (int i = 0; i < count; i++)
                    samples[i] = Mathf.Clamp(samples[i] * gain, -1f, 1f);
            }
        }

        private static float CalculateRms(float[] samples, int count = -1)
        {
            if (count < 0) count = samples.Length;
            float sum = 0f;
            for (int i = 0; i < count; i++) sum += samples[i] * samples[i];
            return Mathf.Sqrt(sum / Mathf.Max(1, count));
        }

        /// <summary>
        /// Simulates Opus-like compression by quantising samples to the target bitrate.
        /// In production this would call a native codec library.
        /// </summary>
        private byte[] CompressAudio(float[] samples, int count)
        {
            // Simulation: each float compressed to 8 bits at the given bitrate ratio.
            float compressionRatio = Mathf.Clamp01(codecBitrateKbps / 128f);
            int outputSize = Mathf.Max(1, Mathf.RoundToInt(count * compressionRatio));
            return new byte[outputSize]; // placeholder compressed buffer
        }

        /// <summary>
        /// Decompresses an audio buffer received from the network.
        /// In production this would call a native codec library.
        /// </summary>
        private static float[] DecompressAudio(byte[] compressed)
        {
            if (compressed == null || compressed.Length == 0) return Array.Empty<float>();
            // Simulation: expand bytes to floats.
            var samples = new float[compressed.Length * 4];
            return samples; // placeholder
        }
    }
}
