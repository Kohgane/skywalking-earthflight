using System;
using UnityEngine;

namespace SWEF.VoiceChat
{
    /// <summary>
    /// Audio processing pipeline for the Voice Chat system.
    /// <para>
    /// Applies in order: noise gate → noise suppression → auto gain control →
    /// echo cancellation stub → voice activity level measurement.
    /// </para>
    /// </summary>
    public class VoiceAudioProcessor
    {
        #region Configuration
        /// <summary>RMS amplitude threshold below which audio is considered silence.</summary>
        public float NoiseGateThreshold { get; set; } = 0.005f;

        /// <summary>Whether spectral noise suppression is enabled.</summary>
        public bool NoiseSuppressionEnabled { get; set; } = true;

        /// <summary>Whether automatic gain control is enabled.</summary>
        public bool AutoGainControlEnabled { get; set; } = true;

        /// <summary>Target RMS level for auto gain control (0–1).</summary>
        public float AgcTargetLevel { get; set; } = 0.3f;

        /// <summary>Maximum gain multiplier applied by AGC.</summary>
        public float AgcMaxGain { get; set; } = 10f;

        /// <summary>Smoothing coefficient for AGC envelope follower.</summary>
        public float AgcSmoothing { get; set; } = 0.95f;
        #endregion

        #region Internal State
        private float _voiceActivityLevel = 0f;
        private float _agcGain            = 1f;
        private float _noiseFloor         = 0f;
        private const float NoiseFloorSmoothing = 0.98f;
        private const float VadSmoothing        = 0.7f;
        #endregion

        #region Public API
        /// <summary>
        /// Processes a raw microphone sample buffer through the full pipeline.
        /// </summary>
        /// <param name="samples">Raw PCM samples from the microphone capture.</param>
        /// <returns>Processed sample buffer ready for encoding and transmission.</returns>
        public float[] ProcessInputBuffer(float[] samples)
        {
            if (samples == null || samples.Length == 0)
                return Array.Empty<float>();

            float[] output = (float[])samples.Clone();

            float rms = CalculateRms(output);

            // 1. Update noise floor estimate (slow-tracking average of quiet frames)
            if (rms < NoiseGateThreshold * 2f)
                _noiseFloor = _noiseFloor * NoiseFloorSmoothing + rms * (1f - NoiseFloorSmoothing);

            // 2. Noise gate — zero out samples below the threshold
            ApplyNoiseGate(output, rms);

            // 3. Spectral noise suppression (simple spectral subtraction approximation)
            if (NoiseSuppressionEnabled)
                ApplyNoiseSuppression(output);

            // 4. Auto gain control
            if (AutoGainControlEnabled)
                ApplyAutoGainControl(output);

            // 5. Echo cancellation stub
            ApplyEchoCancellationStub(output);

            // 6. Update voice activity level (smoothed RMS of processed output)
            float outRms = CalculateRms(output);
            _voiceActivityLevel = _voiceActivityLevel * VadSmoothing
                                + outRms * (1f - VadSmoothing);

            return output;
        }

        /// <summary>
        /// Returns the current voice activity level as a smoothed RMS value in [0, 1].
        /// Values above <see cref="VoiceChatConfig.voiceActivationThreshold"/> indicate speech.
        /// </summary>
        /// <returns>Voice activity level in the range [0, 1].</returns>
        public float GetVoiceActivityLevel()
        {
            return Mathf.Clamp01(_voiceActivityLevel);
        }
        #endregion

        #region Pipeline Steps
        private void ApplyNoiseGate(float[] samples, float rms)
        {
            if (rms >= NoiseGateThreshold) return;
            float fadeRatio = rms / Mathf.Max(NoiseGateThreshold, 0.0001f);
            for (int i = 0; i < samples.Length; i++)
                samples[i] *= fadeRatio;
        }

        private void ApplyNoiseSuppression(float[] samples)
        {
            // Simple time-domain noise floor subtraction — subtract estimated noise
            // amplitude from each sample, preserving sign.
            float noiseAmp = _noiseFloor * 1.5f;
            for (int i = 0; i < samples.Length; i++)
            {
                float s = samples[i];
                float absS = Mathf.Abs(s);
                if (absS <= noiseAmp)
                    samples[i] = 0f;
                else
                    samples[i] = Mathf.Sign(s) * (absS - noiseAmp);
            }
        }

        private void ApplyAutoGainControl(float[] samples)
        {
            float rms = CalculateRms(samples);
            if (rms < 0.0001f) return;

            float desiredGain = AgcTargetLevel / rms;
            desiredGain = Mathf.Clamp(desiredGain, 0.1f, AgcMaxGain);

            // Smooth the gain to avoid zipper noise
            _agcGain = _agcGain * AgcSmoothing + desiredGain * (1f - AgcSmoothing);

            for (int i = 0; i < samples.Length; i++)
                samples[i] = Mathf.Clamp(samples[i] * _agcGain, -1f, 1f);
        }

        private void ApplyEchoCancellationStub(float[] samples)
        {
            // Stub — real echo cancellation requires platform-specific AEC
            // (e.g., WebRTC AEC on mobile, Windows AEC on PC).
            // Platform integrations should override this via subclassing or
            // a delegate before this processor is used in production.
        }
        #endregion

        #region Utilities
        /// <summary>Computes the Root Mean Square (RMS) amplitude of a sample buffer.</summary>
        /// <param name="samples">Sample buffer.</param>
        /// <returns>RMS value in [0, 1].</returns>
        public static float CalculateRms(float[] samples)
        {
            if (samples == null || samples.Length == 0) return 0f;
            float sum = 0f;
            for (int i = 0; i < samples.Length; i++)
                sum += samples[i] * samples[i];
            return Mathf.Sqrt(sum / samples.Length);
        }
        #endregion
    }
}
