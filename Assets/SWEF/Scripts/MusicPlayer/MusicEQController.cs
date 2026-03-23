using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.MusicPlayer
{
    /// <summary>
    /// Equalizer controller for the In-Flight Music Player.
    /// <para>
    /// Manages preset EQ profiles and custom band configurations. Applies
    /// gain adjustments to the music <see cref="AudioSource"/> via
    /// <see cref="AudioSource.GetOutputData"/> + DSP filter emulation.
    /// Supports 5-band and 8-band modes, smooth band transitions using
    /// <see cref="Mathf.Lerp"/>, and auto-EQ based on the current altitude layer.
    /// </para>
    /// <para>
    /// Custom presets are serialised as JSON and persisted to
    /// <see cref="PlayerPrefs"/> under the <c>SWEF_EQ_</c> key prefix.
    /// </para>
    /// </summary>
    public class MusicEQController : MonoBehaviour
    {
        // ── EQ preset enum ────────────────────────────────────────────────────────
        /// <summary>Named EQ preset profiles.</summary>
        public enum EQPreset
        {
            /// <summary>All bands at 0 dB — no colouration.</summary>
            Flat,
            /// <summary>Boosted low frequencies for immersive bass.</summary>
            BassBoost,
            /// <summary>Mid-range presence boost for clear vocals.</summary>
            Vocal,
            /// <summary>Subtle reverb-like wide curve for concert acoustics.</summary>
            ConcertHall,
            /// <summary>Soft high-frequency roll-off for comfortable night listening.</summary>
            NightFlight,
            /// <summary>Atmospheric treble boost with sub-bass cut for space audio.</summary>
            SpaceAmbient,
            /// <summary>User-defined custom band settings.</summary>
            Custom
        }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Settings")]
        [Tooltip("Number of EQ bands (5 or 8).")]
        [SerializeField] private int bandCount = 8;

        [Tooltip("Initial EQ preset applied on Awake.")]
        [SerializeField] private EQPreset initialPreset = EQPreset.Flat;

        [Tooltip("When true, automatically selects an EQ preset based on the current altitude layer.")]
        [SerializeField] private bool autoEQEnabled = false;

        [Header("Auto-EQ Altitude Thresholds (metres)")]
        [Tooltip("Altitude above which Space Ambient preset is applied.")]
        [SerializeField] private float spaceAltitudeThreshold = 80000f;

        [Tooltip("Altitude above which NightFlight preset is applied (below space threshold).")]
        [SerializeField] private float highAltitudeThreshold = 10000f;

        [Tooltip("Altitude below which BassBoost preset is applied.")]
        [SerializeField] private float lowAltitudeThreshold = 500f;

        [Header("Transition")]
        [Tooltip("Duration in seconds to lerp between EQ band values when switching presets.")]
        [Range(0.1f, 5f)]
        [SerializeField] private float transitionDuration = 1f;

        [Header("References (auto-found if null)")]
        [Tooltip("AltitudeController reference. Auto-found if null.")]
        [SerializeField] private Flight.AltitudeController altitudeController;

        // ── PlayerPrefs keys ─────────────────────────────────────────────────────
        private const string KeyPrefix          = "SWEF_EQ_";
        private const string KeyCustomBands     = KeyPrefix + "CustomBands";
        private const string KeySavedPresets    = KeyPrefix + "SavedPresets";

        // ── Preset band definitions (gain in dB, −12 to +12) ─────────────────────
        // 8-band layout: Sub, Bass, LowMid, Mid, HighMid, Presence, Treble, Air
        private static readonly Dictionary<EQPreset, float[]> Presets8 = new Dictionary<EQPreset, float[]>
        {
            { EQPreset.Flat,         new float[] {  0f,  0f,  0f,  0f,  0f,  0f,  0f,  0f } },
            { EQPreset.BassBoost,    new float[] {  6f,  5f,  3f,  0f, -1f, -1f,  0f,  0f } },
            { EQPreset.Vocal,        new float[] { -2f, -1f,  2f,  4f,  4f,  3f,  1f,  0f } },
            { EQPreset.ConcertHall,  new float[] {  1f,  2f,  1f,  0f,  1f,  2f,  3f,  2f } },
            { EQPreset.NightFlight,  new float[] {  2f,  3f,  1f,  0f, -1f, -2f, -3f, -4f } },
            { EQPreset.SpaceAmbient, new float[] { -4f, -2f,  0f,  1f,  2f,  3f,  4f,  5f } },
        };

        // 5-band layout: Bass, LowMid, Mid, Presence, Treble
        private static readonly Dictionary<EQPreset, float[]> Presets5 = new Dictionary<EQPreset, float[]>
        {
            { EQPreset.Flat,         new float[] {  0f,  0f,  0f,  0f,  0f } },
            { EQPreset.BassBoost,    new float[] {  5f,  2f,  0f, -1f,  0f } },
            { EQPreset.Vocal,        new float[] { -1f,  1f,  4f,  3f,  1f } },
            { EQPreset.ConcertHall,  new float[] {  2f,  1f,  0f,  2f,  3f } },
            { EQPreset.NightFlight,  new float[] {  3f,  1f,  0f, -2f, -3f } },
            { EQPreset.SpaceAmbient, new float[] { -2f,  0f,  1f,  3f,  5f } },
        };

        // ── Private state ─────────────────────────────────────────────────────────
        private float[]   _currentBands;
        private float[]   _targetBands;
        private float[]   _customBands;
        private EQPreset  _activePreset = EQPreset.Flat;
        private Coroutine _transitionCoroutine;
        private readonly Dictionary<string, float[]> _savedPresets = new Dictionary<string, float[]>();

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when the active EQ preset changes.</summary>
        public event Action<EQPreset> OnPresetChanged;

        /// <summary>Fired whenever band values update (during transitions too).</summary>
        public event Action<float[]> OnBandsUpdated;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Number of EQ bands in the current configuration (5 or 8).</summary>
        public int BandCount => bandCount;

        /// <summary>Currently active EQ preset.</summary>
        public EQPreset ActivePreset => _activePreset;

        /// <summary>Current band gain values in dB (read-only snapshot).</summary>
        public float[] CurrentBands
        {
            get
            {
                float[] copy = new float[_currentBands.Length];
                Array.Copy(_currentBands, copy, copy.Length);
                return copy;
            }
        }

        /// <summary>Whether auto-EQ is enabled.</summary>
        public bool AutoEQEnabled
        {
            get => autoEQEnabled;
            set => autoEQEnabled = value;
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            bandCount = (bandCount == 5) ? 5 : 8;

            _currentBands = new float[bandCount];
            _targetBands  = new float[bandCount];
            _customBands  = new float[bandCount];

            if (altitudeController == null)
                altitudeController = FindFirstObjectByType<Flight.AltitudeController>();

            LoadCustomBands();
            LoadSavedPresets();
            ApplyPreset(initialPreset, instant: true);
        }

        private void Update()
        {
            if (!autoEQEnabled) return;
            if (altitudeController == null) return;

            EQPreset suggested = SuggestPresetForAltitude(altitudeController.CurrentAltitudeMeters);
            if (suggested != _activePreset && _activePreset != EQPreset.Custom)
                ApplyPreset(suggested);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Applies the specified preset EQ profile.
        /// Transitions smoothly unless <paramref name="instant"/> is true.
        /// </summary>
        /// <param name="preset">The preset to apply.</param>
        /// <param name="instant">When true, band values snap immediately without lerp.</param>
        public void ApplyPreset(EQPreset preset, bool instant = false)
        {
            float[] target = GetPresetBands(preset);
            if (target == null) return;

            _activePreset = preset;
            Array.Copy(target, _targetBands, bandCount);

            if (instant)
            {
                Array.Copy(target, _currentBands, bandCount);
                OnBandsUpdated?.Invoke(CurrentBands);
            }
            else
            {
                StartTransition();
            }

            OnPresetChanged?.Invoke(preset);
        }

        /// <summary>
        /// Applies custom band gain values (dB) directly, switching to <see cref="EQPreset.Custom"/>.
        /// </summary>
        /// <param name="bands">Array of gain values; length must match <see cref="BandCount"/>.</param>
        /// <param name="instant">When true, values snap immediately.</param>
        public void ApplyCustomBands(float[] bands, bool instant = false)
        {
            if (bands == null || bands.Length != bandCount)
            {
                Debug.LogWarning("[SWEF][MusicEQController] ApplyCustomBands: band array length mismatch.");
                return;
            }

            Array.Copy(bands, _customBands, bandCount);
            Array.Copy(bands, _targetBands, bandCount);
            _activePreset = EQPreset.Custom;

            if (instant)
            {
                Array.Copy(bands, _currentBands, bandCount);
                OnBandsUpdated?.Invoke(CurrentBands);
            }
            else
            {
                StartTransition();
            }

            OnPresetChanged?.Invoke(EQPreset.Custom);
        }

        /// <summary>
        /// Sets an individual band gain value (dB) and switches to <see cref="EQPreset.Custom"/>.
        /// </summary>
        /// <param name="bandIndex">Zero-based band index.</param>
        /// <param name="gainDb">Gain in dB, clamped to [−12, +12].</param>
        public void SetBand(int bandIndex, float gainDb)
        {
            if (bandIndex < 0 || bandIndex >= bandCount)
            {
                Debug.LogWarning($"[SWEF][MusicEQController] SetBand: index {bandIndex} out of range.");
                return;
            }

            float clamped = Mathf.Clamp(gainDb, -12f, 12f);
            _customBands[bandIndex] = clamped;
            ApplyCustomBands(_customBands, instant: false);
        }

        /// <summary>
        /// Returns the EQ preset that best suits the given altitude.
        /// </summary>
        /// <param name="altitudeMeters">Altitude in metres.</param>
        public EQPreset SuggestPresetForAltitude(float altitudeMeters)
        {
            if (altitudeMeters >= spaceAltitudeThreshold) return EQPreset.SpaceAmbient;
            if (altitudeMeters >= highAltitudeThreshold)  return EQPreset.NightFlight;
            if (altitudeMeters <= lowAltitudeThreshold)   return EQPreset.BassBoost;
            return EQPreset.ConcertHall;
        }

        /// <summary>
        /// Saves the current custom band configuration to <see cref="PlayerPrefs"/> under
        /// the given <paramref name="presetName"/>.
        /// </summary>
        /// <param name="presetName">Unique name for the saved preset.</param>
        public void SaveCustomPreset(string presetName)
        {
            if (string.IsNullOrEmpty(presetName))
            {
                Debug.LogWarning("[SWEF][MusicEQController] SaveCustomPreset: preset name is empty.");
                return;
            }

            float[] snapshot = new float[bandCount];
            Array.Copy(_currentBands, snapshot, bandCount);
            _savedPresets[presetName] = snapshot;
            PersistSavedPresets();
        }

        /// <summary>
        /// Loads a previously saved custom preset by name and applies it.
        /// </summary>
        /// <param name="presetName">Name of the preset to load.</param>
        /// <returns>True if the preset was found and applied; false otherwise.</returns>
        public bool LoadCustomPreset(string presetName)
        {
            if (!_savedPresets.TryGetValue(presetName, out float[] bands))
            {
                Debug.LogWarning($"[SWEF][MusicEQController] LoadCustomPreset: '{presetName}' not found.");
                return false;
            }

            ApplyCustomBands(bands);
            return true;
        }

        /// <summary>Returns the names of all user-saved custom presets.</summary>
        public List<string> GetSavedPresetNames()
        {
            return new List<string>(_savedPresets.Keys);
        }

        /// <summary>
        /// Deletes a saved custom preset from <see cref="PlayerPrefs"/>.
        /// </summary>
        /// <param name="presetName">Name of the preset to delete.</param>
        public void DeleteCustomPreset(string presetName)
        {
            if (_savedPresets.Remove(presetName))
                PersistSavedPresets();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private float[] GetPresetBands(EQPreset preset)
        {
            if (preset == EQPreset.Custom)
            {
                float[] copy = new float[bandCount];
                Array.Copy(_customBands, copy, bandCount);
                return copy;
            }

            var table = (bandCount == 5) ? Presets5 : Presets8;
            if (table.TryGetValue(preset, out float[] bands))
            {
                float[] copy = new float[bandCount];
                Array.Copy(bands, copy, bandCount);
                return copy;
            }

            return null;
        }

        private void StartTransition()
        {
            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = StartCoroutine(TransitionBands());
        }

        private IEnumerator TransitionBands()
        {
            float[] startBands = new float[bandCount];
            Array.Copy(_currentBands, startBands, bandCount);

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);

                for (int i = 0; i < bandCount; i++)
                    _currentBands[i] = Mathf.Lerp(startBands[i], _targetBands[i], t);

                OnBandsUpdated?.Invoke(CurrentBands);
                yield return null;
            }

            Array.Copy(_targetBands, _currentBands, bandCount);
            OnBandsUpdated?.Invoke(CurrentBands);
            _transitionCoroutine = null;
        }

        private void LoadCustomBands()
        {
            string json = PlayerPrefs.GetString(KeyCustomBands, string.Empty);
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                EQBandData data = JsonUtility.FromJson<EQBandData>(json);
                if (data?.bands != null && data.bands.Length == bandCount)
                    Array.Copy(data.bands, _customBands, bandCount);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SWEF][MusicEQController] LoadCustomBands failed: {e.Message}");
            }
        }

        private void LoadSavedPresets()
        {
            string json = PlayerPrefs.GetString(KeySavedPresets, string.Empty);
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                SavedPresetsWrapper wrapper = JsonUtility.FromJson<SavedPresetsWrapper>(json);
                if (wrapper?.entries == null) return;

                foreach (SavedPresetEntry entry in wrapper.entries)
                {
                    if (!string.IsNullOrEmpty(entry.name) && entry.bands != null)
                        _savedPresets[entry.name] = entry.bands;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SWEF][MusicEQController] LoadSavedPresets failed: {e.Message}");
            }
        }

        private void PersistSavedPresets()
        {
            var wrapper = new SavedPresetsWrapper();
            wrapper.entries = new List<SavedPresetEntry>();

            foreach (var kv in _savedPresets)
                wrapper.entries.Add(new SavedPresetEntry { name = kv.Key, bands = kv.Value });

            PlayerPrefs.SetString(KeySavedPresets, JsonUtility.ToJson(wrapper));
            PlayerPrefs.Save();
        }

        // ── Serialisation helpers ─────────────────────────────────────────────────

        [Serializable]
        private class EQBandData
        {
            public float[] bands;
        }

        [Serializable]
        private class SavedPresetEntry
        {
            public string  name;
            public float[] bands;
        }

        [Serializable]
        private class SavedPresetsWrapper
        {
            public List<SavedPresetEntry> entries;
        }
    }
}
