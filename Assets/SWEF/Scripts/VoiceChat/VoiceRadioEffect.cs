using System;
using UnityEngine;

namespace SWEF.VoiceChat
{
    /// <summary>
    /// Applies aviation-style radio filter effects to incoming voice audio.
    /// <para>
    /// Effects per channel:
    /// <list type="bullet">
    ///   <item><see cref="VoiceChannel.ATC"/> — heavy band-pass, heavy static noise, squelch clicks.</item>
    ///   <item><see cref="VoiceChannel.Global"/> — light band-pass, subtle static.</item>
    ///   <item><see cref="VoiceChannel.Proximity"/> — minimal or no filter.</item>
    ///   <item><see cref="VoiceChannel.Team"/> / <see cref="VoiceChannel.Private"/> — medium filter.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class VoiceRadioEffect : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Band-Pass")]
        [Tooltip("Normalised low-cut frequency for the band-pass filter (0–1, maps to 300–3400 Hz).")]
        [SerializeField] private float bandPassLow  = 0.1f;

        [Tooltip("Normalised high-cut frequency for the band-pass filter (0–1, maps to 300–3400 Hz).")]
        [SerializeField] private float bandPassHigh = 0.85f;

        [Header("Static Noise")]
        [Tooltip("Amplitude of the static noise layer at full radio intensity.")]
        [SerializeField] private float staticAmplitude = 0.04f;

        [Header("Squelch")]
        [Tooltip("AudioClip played when a transmission starts (click/open squelch).")]
        [SerializeField] private AudioClip squelchOpenClip;

        [Tooltip("AudioClip played when a transmission ends (click/close squelch).")]
        [SerializeField] private AudioClip squelchCloseClip;

        [Tooltip("AudioSource used to play squelch sounds.")]
        [SerializeField] private AudioSource squelchSource;
        #endregion

        #region Internal State
        private float _currentIntensity = 0f;

        // Per-channel default intensities
        private static readonly float[] ChannelIntensities = new float[]
        {
            0.05f,  // Proximity  — near-clean
            0.35f,  // Team       — light filter
            0.45f,  // Global     — medium filter
            0.30f,  // Private    — light-medium
            0.90f,  // ATC        — heavy aviation radio
        };
        #endregion

        #region Public API
        /// <summary>
        /// Applies the aviation radio filter appropriate for the given channel to a sample buffer.
        /// </summary>
        /// <param name="samples">Raw PCM sample buffer to process (modified in-place).</param>
        /// <param name="channel">Voice channel determining filter intensity.</param>
        /// <returns>Filtered sample buffer (same reference as <paramref name="samples"/>).</returns>
        public float[] ApplyRadioFilter(float[] samples, VoiceChannel channel)
        {
            if (samples == null || samples.Length == 0) return samples;

            float intensity = ChannelIntensities[(int)channel];
            SetRadioEffectIntensity(intensity);

            if (_currentIntensity <= 0.01f) return samples;

            ApplyBandPass(samples, _currentIntensity);
            AddStaticNoise(samples, _currentIntensity);
            ApplyGainCompression(samples, _currentIntensity);

            return samples;
        }

        /// <summary>
        /// Sets the radio effect intensity manually.
        /// </summary>
        /// <param name="intensity">Effect strength in the range [0, 1] (0 = clean, 1 = heavy radio).</param>
        public void SetRadioEffectIntensity(float intensity)
        {
            _currentIntensity = Mathf.Clamp01(intensity);
        }

        /// <summary>
        /// Plays the squelch-open sound (transmission start click).
        /// </summary>
        public void PlaySquelchOpen()
        {
            if (squelchSource != null && squelchOpenClip != null)
                squelchSource.PlayOneShot(squelchOpenClip);
        }

        /// <summary>
        /// Plays the squelch-close sound (transmission end click).
        /// </summary>
        public void PlaySquelchClose()
        {
            if (squelchSource != null && squelchCloseClip != null)
                squelchSource.PlayOneShot(squelchCloseClip);
        }
        #endregion

        #region Filter Implementation
        private void ApplyBandPass(float[] samples, float intensity)
        {
            // Simple first-order IIR band-pass approximation in the time domain.
            // Blend between original signal and a high-passed + low-passed signal.
            // The blend ratio is driven by intensity.

            float alpha = Mathf.Lerp(0.05f, 0.4f, bandPassLow);  // low-cut coefficient
            float beta  = Mathf.Lerp(0.3f,  0.9f, bandPassHigh); // high-cut coefficient

            float prev = 0f;
            float hpPrev = samples.Length > 0 ? samples[0] : 0f;

            for (int i = 0; i < samples.Length; i++)
            {
                float raw = samples[i];

                // Low-pass (gentle roll-off above bandPassHigh)
                float lp = prev + beta * (raw - prev);
                prev = lp;

                // High-pass (remove low frequencies below bandPassLow)
                float hp = alpha * (hpPrev + raw - (i > 0 ? samples[i - 1] : raw));
                hpPrev = hp;

                // Band = intersection of LP and HP
                float filtered = Mathf.Lerp(raw, lp * 0.7f + hp * 0.3f, intensity);
                samples[i] = filtered;
            }
        }

        private void AddStaticNoise(float[] samples, float intensity)
        {
            float noiseLevel = staticAmplitude * intensity;
            for (int i = 0; i < samples.Length; i++)
                samples[i] += UnityEngine.Random.Range(-noiseLevel, noiseLevel);
        }

        private void ApplyGainCompression(float[] samples, float intensity)
        {
            // Mild compression / saturation typical of radio transmitters
            float threshold = Mathf.Lerp(1.0f, 0.6f, intensity);
            for (int i = 0; i < samples.Length; i++)
            {
                float s = samples[i];
                if (Mathf.Abs(s) > threshold)
                    s = Mathf.Sign(s) * (threshold + (Mathf.Abs(s) - threshold) * 0.3f);
                samples[i] = Mathf.Clamp(s, -1f, 1f);
            }
        }
        #endregion
    }
}
