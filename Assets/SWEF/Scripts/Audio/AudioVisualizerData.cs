using System;
using UnityEngine;

namespace SWEF.Audio
{
    /// <summary>
    /// Provides real-time audio spectrum and waveform data sampled from the scene's
    /// <see cref="AudioListener"/>. Supports bass/mid/treble level extraction and
    /// simple beat detection via energy threshold comparison.
    /// </summary>
    public class AudioVisualizerData : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Sampling")]
        [SerializeField] private int          sampleSize  = 256;
        [SerializeField] private FFTWindow    fftWindow   = FFTWindow.Hanning;

        [Header("Beat Detection")]
        [SerializeField] private float beatEnergyThreshold   = 1.3f;   // ratio vs history average
        [SerializeField] private int   beatHistoryLength      = 43;     // ~1s at 43 fps

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when a beat is detected. Intensity is the energy ratio.</summary>
        public event Action<float> OnBeat;

        // ── Runtime ───────────────────────────────────────────────────────────────
        private float[] _spectrumData;
        private float[] _outputData;
        private float[] _beatHistory;
        private int     _beatHistoryIndex;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            _spectrumData  = new float[sampleSize];
            _outputData    = new float[sampleSize];
            _beatHistory   = new float[beatHistoryLength];
        }

        private void Update()
        {
            AudioListener.GetSpectrumData(_spectrumData, 0, fftWindow);
            AudioListener.GetOutputData(_outputData, 0);
            DetectBeat();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns the current spectrum data (copies into caller's array or returns shared buffer).</summary>
        public float[] GetSpectrumData(int samples = 256)
        {
            if (samples == sampleSize) return _spectrumData;
            var result = new float[samples];
            int step = Mathf.Max(1, sampleSize / samples);
            for (int i = 0; i < samples; i++)
                result[i] = _spectrumData[Mathf.Min(i * step, sampleSize - 1)];
            return result;
        }

        /// <summary>Returns the current output waveform data.</summary>
        public float[] GetOutputData(int samples = 256)
        {
            if (samples == sampleSize) return _outputData;
            var result = new float[samples];
            int step = Mathf.Max(1, sampleSize / samples);
            for (int i = 0; i < samples; i++)
                result[i] = _outputData[Mathf.Min(i * step, sampleSize - 1)];
            return result;
        }

        /// <summary>Returns average energy in the bass band (bins 1–4).</summary>
        public float GetBassLevel()  => BandAverage(1, 4);

        /// <summary>Returns average energy in the mid band (bins 5–64).</summary>
        public float GetMidLevel()   => BandAverage(5, 64);

        /// <summary>Returns average energy in the treble band (bins 65–128).</summary>
        public float GetTrebleLevel() => BandAverage(65, Mathf.Min(128, sampleSize - 1));

        /// <summary>Returns overall RMS level from the output waveform.</summary>
        public float GetOverallLevel()
        {
            float sum = 0f;
            foreach (var s in _outputData) sum += s * s;
            return Mathf.Sqrt(sum / _outputData.Length);
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private float BandAverage(int start, int end)
        {
            start = Mathf.Clamp(start, 0, sampleSize - 1);
            end   = Mathf.Clamp(end,   0, sampleSize - 1);
            if (start > end) return 0f;

            float sum = 0f;
            for (int i = start; i <= end; i++) sum += _spectrumData[i];
            return sum / (end - start + 1);
        }

        private void DetectBeat()
        {
            float energy = GetBassLevel();

            // Running average
            float avgEnergy = 0f;
            foreach (var h in _beatHistory) avgEnergy += h;
            avgEnergy /= beatHistoryLength;

            if (avgEnergy > 0f && energy > avgEnergy * beatEnergyThreshold)
                OnBeat?.Invoke(energy / avgEnergy);

            _beatHistory[_beatHistoryIndex] = energy;
            _beatHistoryIndex = (_beatHistoryIndex + 1) % beatHistoryLength;
        }
    }
}
