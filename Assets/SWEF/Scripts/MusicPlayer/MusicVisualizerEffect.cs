using System.Collections.Generic;
using UnityEngine;
using SWEF.UI;

namespace SWEF.MusicPlayer
{
    /// <summary>
    /// Audio-reactive visual effects for the In-Flight Music Player.
    /// <para>
    /// Reads spectrum data from the music player's <see cref="AudioSource"/> using
    /// <see cref="AudioSource.GetSpectrumData"/>, collapses it into 8 bands,
    /// detects beats with a threshold-based algorithm, and exposes the processed
    /// data for use by visual subsystems (shaders, particle systems, etc.).
    /// </para>
    /// <para>
    /// Respects <see cref="AccessibilityController.ReducedMotionEnabled"/> — reduces or
    /// disables animated effects.  Only processes audio data while this component
    /// is active and enabled (perf-friendly).
    /// Toggled via <see cref="MusicPlayerConfig.visualizerEnabled"/>.
    /// </para>
    /// </summary>
    public class MusicVisualizerEffect : MonoBehaviour
    {
        // ── Visualizer modes ──────────────────────────────────────────────────────
        /// <summary>Available visual rendering modes for the visualiser.</summary>
        public enum VisualizerMode
        {
            /// <summary>Vertical frequency bars.</summary>
            Bars,
            /// <summary>Scrolling waveform.</summary>
            Waveform,
            /// <summary>Particle burst on beats.</summary>
            Particles,
            /// <summary>Pulsing ring driven by amplitude.</summary>
            Ring,
            /// <summary>Visualiser disabled — no processing.</summary>
            Disabled
        }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Settings")]
        [Tooltip("Initial visualizer rendering mode.")]
        [SerializeField] private VisualizerMode mode = VisualizerMode.Bars;

        [Tooltip("Number of FFT samples used for spectrum analysis (must be a power of 2).")]
        [SerializeField] private int fftSamples = 512;

        [Header("Beat Detection")]
        [Tooltip("Average amplitude must exceed this threshold to register a beat.")]
        [Range(0f, 1f)]
        [SerializeField] private float beatThreshold = 0.15f;

        [Tooltip("Minimum seconds between two detected beats (debounce).")]
        [SerializeField] private float beatCooldown = 0.25f;

        [Header("Smoothing")]
        [Tooltip("Exponential smoothing factor applied each frame (0 = no smoothing, 1 = fully frozen).")]
        [Range(0f, 0.99f)]
        [SerializeField] private float smoothingFactor = 0.85f;

        // ── Private state ─────────────────────────────────────────────────────────
        private float[]   _rawSpectrum;
        private float[]   _smoothedSpectrum;
        private float[]   _bandValues;          // 8 normalised bands
        private float     _beatIntensity;
        private float     _averageAmplitude;
        private float     _beatCooldownTimer;
        private AudioSource _trackedSource;
        private AccessibilityController _accessibilityController;

        // Band frequency boundaries (8 bands spanning human-audible range)
        // Each entry is the *start* index into _rawSpectrum for that band.
        private static readonly int[] BandBoundaries = { 0, 2, 4, 8, 16, 32, 64, 128 };

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>8 normalised frequency-band amplitudes (0–1), smoothed each frame.</summary>
        public float[] SpectrumBands => _bandValues;

        /// <summary>Peak-detected beat intensity (0–1).</summary>
        public float BeatIntensity => _beatIntensity;

        /// <summary>Average amplitude across all frequency bands (0–1).</summary>
        public float AverageAmplitude => _averageAmplitude;

        /// <summary>Currently active visualiser mode.</summary>
        public VisualizerMode CurrentMode => mode;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            _rawSpectrum      = new float[fftSamples];
            _smoothedSpectrum = new float[fftSamples];
            _bandValues       = new float[8];
            _accessibilityController = FindFirstObjectByType<AccessibilityController>();
        }

        private void Update()
        {
            if (!isActiveAndEnabled) return;
            if (!IsEnabled())
            {
                ResetOutput();
                return;
            }

            EnsureTrackedSource();
            if (_trackedSource == null) return;

            ProcessSpectrum();
            ComputeBands();
            DetectBeat();
            ComputeAverageAmplitude();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns the 8 normalised frequency-band amplitudes (0–1).</summary>
        public float[] GetSpectrumBands() => _bandValues;

        /// <summary>Returns the current peak-detected beat intensity (0–1).</summary>
        public float GetBeatIntensity() => _beatIntensity;

        /// <summary>Returns the current average amplitude across all bands (0–1).</summary>
        public float GetAverageAmplitude() => _averageAmplitude;

        /// <summary>Switches the active visualiser mode.</summary>
        public void SetVisualizerMode(VisualizerMode newMode)
        {
            mode = newMode;
            if (newMode == VisualizerMode.Disabled)
                ResetOutput();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void EnsureTrackedSource()
        {
            if (_trackedSource != null && _trackedSource.isPlaying) return;

            // Attempt to find the MusicPlayerManager's AudioSource
            if (MusicPlayerManager.Instance != null)
            {
                AudioSource src = MusicPlayerManager.Instance.GetComponent<AudioSource>();
                if (src != null)
                {
                    _trackedSource = src;
                    return;
                }
            }

            // Fallback: search scene
            _trackedSource = FindFirstObjectByType<AudioSource>();
        }

        private void ProcessSpectrum()
        {
            _trackedSource.GetSpectrumData(_rawSpectrum, 0, FFTWindow.BlackmanHarris);

            // Exponential smoothing
            for (int i = 0; i < _rawSpectrum.Length; i++)
            {
                _smoothedSpectrum[i] = _smoothedSpectrum[i] * smoothingFactor
                                     + _rawSpectrum[i]      * (1f - smoothingFactor);
            }
        }

        private void ComputeBands()
        {
            // Map spectrum bins into 8 bands
            for (int b = 0; b < 8; b++)
            {
                int start = BandBoundaries[b];
                int end   = (b < 7) ? BandBoundaries[b + 1] : _smoothedSpectrum.Length;

                float sum = 0f;
                for (int i = start; i < end && i < _smoothedSpectrum.Length; i++)
                    sum += _smoothedSpectrum[i];

                int count     = Mathf.Max(1, end - start);
                _bandValues[b] = Mathf.Clamp01(sum / count * 100f); // scale for visibility
            }
        }

        private void DetectBeat()
        {
            _beatCooldownTimer -= Time.deltaTime;

            // Check reduced motion — suppress intense beat effects
            bool reducedMotion = _accessibilityController != null
                && _accessibilityController.ReducedMotionEnabled;

            float threshold = reducedMotion ? beatThreshold * 2f : beatThreshold;

            if (_averageAmplitude > threshold && _beatCooldownTimer <= 0f)
            {
                _beatIntensity     = Mathf.Clamp01(_averageAmplitude / threshold);
                _beatCooldownTimer = beatCooldown;
            }
            else
            {
                // Decay beat intensity
                _beatIntensity = Mathf.MoveTowards(_beatIntensity, 0f, Time.deltaTime * 3f);
            }
        }

        private void ComputeAverageAmplitude()
        {
            float sum = 0f;
            for (int i = 0; i < _bandValues.Length; i++)
                sum += _bandValues[i];
            _averageAmplitude = sum / _bandValues.Length;
        }

        private void ResetOutput()
        {
            for (int i = 0; i < _bandValues.Length; i++)
                _bandValues[i] = 0f;
            _beatIntensity    = 0f;
            _averageAmplitude = 0f;
        }

        private bool IsEnabled()
        {
            if (mode == VisualizerMode.Disabled) return false;
            if (MusicPlayerManager.Instance == null) return false;
            return MusicPlayerManager.Instance.Config.visualizerEnabled;
        }
    }
}
