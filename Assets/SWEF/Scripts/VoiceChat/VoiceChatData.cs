using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.VoiceChat
{
    // ── Enums ─────────────────────────────────────────────────────────────────────

    /// <summary>Operating mode for the local microphone capture.</summary>
    public enum VoiceChatMode
    {
        /// <summary>Voice chat is disabled entirely.</summary>
        Off,
        /// <summary>Microphone is active only while the push-to-talk key is held.</summary>
        PushToTalk,
        /// <summary>Microphone activates automatically when voice activity is detected.</summary>
        VoiceActivated,
        /// <summary>Microphone is always transmitting.</summary>
        AlwaysOn
    }

    /// <summary>Communication channel scope.</summary>
    public enum VoiceChannel
    {
        /// <summary>Heard only by nearby players within <see cref="VoiceChatConfig.maxProximityRange"/> metres.</summary>
        Proximity,
        /// <summary>Heard by all members of the local player's team/group.</summary>
        Team,
        /// <summary>Server-wide broadcast channel (rate-limited).</summary>
        Global,
        /// <summary>One-on-one private voice call.</summary>
        Private,
        /// <summary>Air Traffic Control roleplay channel — heavy radio effect applied.</summary>
        ATC
    }

    /// <summary>Voice codec used for encoding transmitted audio.</summary>
    public enum VoiceCodec
    {
        /// <summary>Opus — recommended, low-latency, high-quality.</summary>
        Opus,
        /// <summary>Speex — legacy codec, lower CPU usage.</summary>
        Speex,
        /// <summary>Raw PCM — uncompressed, highest quality, highest bandwidth.</summary>
        PCM
    }

    /// <summary>Audio sample-rate tier for voice quality.</summary>
    public enum VoiceQuality
    {
        /// <summary>8 kHz — suitable for narrow-band radio-style speech.</summary>
        Low,
        /// <summary>16 kHz — wideband, natural speech quality.</summary>
        Medium,
        /// <summary>24 kHz — high quality.</summary>
        High,
        /// <summary>48 kHz — studio-grade ultra quality.</summary>
        Ultra
    }

    // ── VoiceChatParticipant ──────────────────────────────────────────────────────

    /// <summary>
    /// Runtime state snapshot for a single voice chat participant.
    /// </summary>
    [Serializable]
    public class VoiceChatParticipant
    {
        /// <summary>Unique identifier matching the multiplayer session participant ID.</summary>
        public string participantId;

        /// <summary>Human-readable display name shown in the UI.</summary>
        public string displayName;

        /// <summary>Whether this participant has been muted by the local player.</summary>
        public bool isMuted;

        /// <summary>Whether this participant is currently transmitting voice data.</summary>
        public bool isSpeaking;

        /// <summary>Whether this participant has deafened themselves (not receiving audio).</summary>
        public bool isDeafened;

        /// <summary>Per-participant playback volume in the range [0, 1].</summary>
        [Range(0f, 1f)]
        public float volume = 1f;

        /// <summary>
        /// Spatial blend for the participant's <c>AudioSource</c> (0 = 2D, 1 = full 3D).
        /// </summary>
        [Range(0f, 1f)]
        public float spatialBlend = 1f;

        /// <summary>The voice channel this participant is currently transmitting on.</summary>
        public VoiceChannel currentChannel = VoiceChannel.Proximity;

        /// <summary>World-space position used for 3D spatial audio calculations.</summary>
        public Vector3 position;

        /// <summary>Metres between this participant and the local player.</summary>
        public float distanceFromLocal;

        /// <summary>Network signal strength in the range [0, 1].</summary>
        [Range(0f, 1f)]
        public float signalStrength = 1f;

        /// <summary>Round-trip latency in milliseconds.</summary>
        public int latencyMs;
    }

    // ── VoiceChatConfig ───────────────────────────────────────────────────────────

    /// <summary>
    /// Serialisable configuration object for the Voice Chat system.
    /// Persisted via <c>SWEF.Settings.SettingsManager</c>.
    /// </summary>
    [Serializable]
    public class VoiceChatConfig
    {
        /// <summary>Microphone activation mode.</summary>
        public VoiceChatMode mode = VoiceChatMode.PushToTalk;

        /// <summary>Name of the selected microphone device. Empty string means system default.</summary>
        public string inputDevice = string.Empty;

        /// <summary>Codec used for encoding and decoding voice audio.</summary>
        public VoiceCodec codec = VoiceCodec.Opus;

        /// <summary>Sample-rate / quality tier for voice capture.</summary>
        public VoiceQuality quality = VoiceQuality.Medium;

        /// <summary>RMS amplitude threshold (0–1) below which the voice is gated in VAD mode.</summary>
        [Range(0f, 1f)]
        public float voiceActivationThreshold = 0.02f;

        /// <summary>Whether to apply simple spectral noise suppression to the input signal.</summary>
        public bool noiseSuppression = true;

        /// <summary>Whether to apply echo cancellation (stub — platform-specific).</summary>
        public bool echoCancellation = true;

        /// <summary>Whether to apply automatic gain control to normalise input levels.</summary>
        public bool autoGainControl = true;

        /// <summary>Maximum radius in metres for the Proximity voice channel.</summary>
        public float maxProximityRange = 500f;

        /// <summary>Whether to spatialise voice audio in 3D world space.</summary>
        public bool spatialAudioEnabled = true;

        /// <summary>Whether to duck the in-flight music player when someone is speaking.</summary>
        public bool duckMusicOnVoice = true;

        /// <summary>Relative volume reduction applied to music during voice activity (0–0.8).</summary>
        [Range(0f, 0.8f)]
        public float duckAmount = 0.5f;

        /// <summary>Keyboard key used for push-to-talk transmission.</summary>
        public KeyCode pushToTalkKey = KeyCode.V;

        /// <summary>Keyboard key used to toggle the local microphone mute.</summary>
        public KeyCode muteKey = KeyCode.M;

        /// <summary>Master playback volume for all incoming voice audio (0–1).</summary>
        [Range(0f, 1f)]
        public float masterVolume = 0.8f;

        /// <summary>Whether to show a HUD notification when participants join or leave a channel.</summary>
        public bool notifyOnJoinLeave = true;
    }
}
