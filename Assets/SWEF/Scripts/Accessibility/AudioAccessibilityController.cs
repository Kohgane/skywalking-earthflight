// AudioAccessibilityController.cs — SWEF Accessibility & Platform Optimization (Phase 93)
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Accessibility
{
    /// <summary>
    /// Singleton MonoBehaviour providing audio accessibility features:
    /// per-channel volume sliders, mono audio downmix, audio description
    /// narration, and visual sound indicators.
    ///
    /// <para>Integrates with Unity's <c>AudioMixer</c> channels when available.</para>
    /// </summary>
    public class AudioAccessibilityController : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static AudioAccessibilityController Instance { get; private set; }

        // ── Serialised fields ─────────────────────────────────────────────────────
        [Header("Volume Channels (0–1)")]
        [SerializeField, Range(0f, 1f), Tooltip("Master volume.")]
        private float masterVolume = 1f;

        [SerializeField, Range(0f, 1f), Tooltip("Music channel volume.")]
        private float musicVolume = 1f;

        [SerializeField, Range(0f, 1f), Tooltip("Sound effects volume.")]
        private float sfxVolume = 1f;

        [SerializeField, Range(0f, 1f), Tooltip("Voice / dialogue volume.")]
        private float voiceVolume = 1f;

        [SerializeField, Range(0f, 1f), Tooltip("Ambient environment volume.")]
        private float ambientVolume = 1f;

        [Header("Features")]
        [SerializeField, Tooltip("Downmix stereo audio to mono.")]
        private bool monoAudio;

        [SerializeField, Tooltip("Speak audio description narrations for key visual events.")]
        private bool audioDescriptionsEnabled;

        [Header("Visual Sound Indicator")]
        [SerializeField, Tooltip("UI element that pulses to indicate loud sounds.")]
        private Image soundIndicatorIcon;

        [SerializeField, Tooltip("Minimum audio amplitude to activate the indicator.")]
        private float indicatorThreshold = 0.3f;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when any volume channel changes; provides channel name and new value.</summary>
        public event Action<string, float> OnVolumeChanged;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            ApplyAllVolumes();

            if (AccessibilityManager.Instance != null)
            {
                audioDescriptionsEnabled = AccessibilityManager.Instance.Profile.audioDescriptions;
                AccessibilityManager.Instance.OnProfileChanged += OnProfileChanged;
            }
        }

        private void OnDestroy()
        {
            if (AccessibilityManager.Instance != null)
                AccessibilityManager.Instance.OnProfileChanged -= OnProfileChanged;
        }

        private void OnProfileChanged()
        {
            if (AccessibilityManager.Instance != null)
                audioDescriptionsEnabled = AccessibilityManager.Instance.Profile.audioDescriptions;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Sets the master volume (0–1) and applies it.</summary>
        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            AudioListener.volume = masterVolume;
            OnVolumeChanged?.Invoke("Master", masterVolume);
        }

        /// <summary>Sets the music channel volume (0–1).</summary>
        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            ApplyMixerChannel("Music", musicVolume);
            OnVolumeChanged?.Invoke("Music", musicVolume);
        }

        /// <summary>Sets the SFX channel volume (0–1).</summary>
        public void SetSFXVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            ApplyMixerChannel("SFX", sfxVolume);
            OnVolumeChanged?.Invoke("SFX", sfxVolume);
        }

        /// <summary>Sets the voice / dialogue channel volume (0–1).</summary>
        public void SetVoiceVolume(float value)
        {
            voiceVolume = Mathf.Clamp01(value);
            ApplyMixerChannel("Voice", voiceVolume);
            OnVolumeChanged?.Invoke("Voice", voiceVolume);
        }

        /// <summary>Sets the ambient environment channel volume (0–1).</summary>
        public void SetAmbientVolume(float value)
        {
            ambientVolume = Mathf.Clamp01(value);
            ApplyMixerChannel("Ambient", ambientVolume);
            OnVolumeChanged?.Invoke("Ambient", ambientVolume);
        }

        /// <summary>Enables or disables mono audio downmix.</summary>
        public void SetMonoAudio(bool enabled)
        {
            monoAudio = enabled;
            // Unity does not expose a direct mono-mix API; this flag is read by
            // AudioSource wrapper components that implement the downmix.
            Debug.Log($"[SWEF] Accessibility: Mono audio {(enabled ? "enabled" : "disabled")}.");
        }

        /// <summary>
        /// Plays an audio description narration for a visual event.
        /// Routes through <see cref="ScreenReaderBridge"/> when available.
        /// </summary>
        /// <param name="description">Text description of the visual event.</param>
        public void PlayAudioDescription(string description)
        {
            if (!audioDescriptionsEnabled || string.IsNullOrEmpty(description)) return;

#if SWEF_ACCESSIBILITY_SCREENREADER
            ScreenReaderBridge.Announce(description, SpeechPriority.Low);
#else
            Debug.Log($"[SWEF] Accessibility: AudioDescription — {description}");
#endif
        }

        /// <summary>
        /// Pulses the visual sound indicator icon for the given number of seconds.
        /// Call from audio systems when a loud sound plays.
        /// </summary>
        /// <param name="amplitude">Normalised amplitude (0–1).</param>
        public void PulseIndicator(float amplitude)
        {
            if (soundIndicatorIcon == null || amplitude < indicatorThreshold) return;
            soundIndicatorIcon.color = Color.Lerp(Color.white, Color.red, amplitude);
            soundIndicatorIcon.gameObject.SetActive(true);
            CancelInvoke(nameof(HideIndicator));
            Invoke(nameof(HideIndicator), 0.5f);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void ApplyAllVolumes()
        {
            AudioListener.volume = masterVolume;
            ApplyMixerChannel("Music",   musicVolume);
            ApplyMixerChannel("SFX",     sfxVolume);
            ApplyMixerChannel("Voice",   voiceVolume);
            ApplyMixerChannel("Ambient", ambientVolume);
        }

        /// <summary>
        /// Stub: sets an AudioMixer exposed parameter named <c>{channelName}Volume</c>
        /// if an AudioMixer is wired up.  Replace body with actual mixer reference when available.
        /// </summary>
        private void ApplyMixerChannel(string channelName, float linearValue)
        {
            // Convert linear 0–1 to decibels (Unity convention)
            float db = linearValue > 0.0001f
                ? Mathf.Log10(linearValue) * 20f
                : -80f;

            // AudioMixer integration: assign via mixer.SetFloat($"{channelName}Volume", db)
            // Requires a serialized AudioMixer reference — intentionally left as a named stub
            // so project can wire it up without coupling to a specific mixer asset.
            Debug.Log($"[SWEF] Accessibility: {channelName}Volume = {db:F1} dB");
        }

        private void HideIndicator()
        {
            if (soundIndicatorIcon != null)
                soundIndicatorIcon.gameObject.SetActive(false);
        }
    }
}
