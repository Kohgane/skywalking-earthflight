using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.InputSystem
{
    /// <summary>
    /// Phase 57 — Manages save, load, and application of named
    /// <see cref="InputPreset"/> configurations.
    /// <para>
    /// Works in concert with <see cref="InputBindingManager"/> — presets are applied
    /// through that manager, while this class handles the persistence layer
    /// (PlayerPrefs JSON) and the runtime preset catalogue.
    /// </para>
    /// </summary>
    public class InputPresetManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static InputPresetManager Instance { get; private set; }

        #endregion

        #region Constants

        private const string PrefsKeyCustomList  = "SWEF_InputPresets_Names";
        private const string PrefsKeyPresetPrefix = "SWEF_InputPreset_";
        private const string ActivePresetKey     = "SWEF_InputPreset_Active";

        #endregion

        #region Events

        /// <summary>Fired when a preset is saved (new or updated).  Carries the preset name.</summary>
        public event Action<string> OnPresetSaved;

        /// <summary>Fired when a preset is deleted.  Carries the preset name.</summary>
        public event Action<string> OnPresetDeleted;

        /// <summary>Fired when a preset is activated.  Carries the preset name.</summary>
        public event Action<string> OnPresetActivated;

        #endregion

        #region Public Properties

        /// <summary>All presets currently registered (built-in + custom).</summary>
        public IReadOnlyList<InputPreset> Presets => _presets;

        /// <summary>Name of the currently active preset, or empty when using a custom configuration.</summary>
        public string ActivePresetName { get; private set; } = string.Empty;

        #endregion

        #region Private State

        private readonly List<InputPreset> _presets = new List<InputPreset>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadBuiltinPresets();
            LoadCustomPresets();
            RestoreActivePreset();
        }

        #endregion

        #region Public API — Preset Catalogue

        /// <summary>
        /// Returns the <see cref="InputPreset"/> with the given <paramref name="presetName"/>,
        /// or <c>null</c> if not found.
        /// </summary>
        public InputPreset? GetPreset(string presetName)
        {
            foreach (var p in _presets)
            {
                if (p.presetName == presetName)
                    return p;
            }
            return null;
        }

        /// <summary>
        /// Activates the named preset by forwarding it to <see cref="InputBindingManager"/>
        /// and persisting the active preset name.
        /// </summary>
        public bool ActivatePreset(string presetName)
        {
            foreach (var p in _presets)
            {
                if (p.presetName != presetName) continue;

                if (InputBindingManager.Instance != null)
                    InputBindingManager.Instance.ApplyPreset(p);

                ActivePresetName = presetName;
                PlayerPrefs.SetString(ActivePresetKey, presetName);
                PlayerPrefs.Save();
                OnPresetActivated?.Invoke(presetName);
                return true;
            }
            Debug.LogWarning($"[SWEF InputPresetManager] Preset '{presetName}' not found.");
            return false;
        }

        #endregion

        #region Public API — Saving & Deleting

        /// <summary>
        /// Captures the current binding map from <see cref="InputBindingManager"/> and
        /// saves it as a custom preset with the given <paramref name="presetName"/>.
        /// Overwrites any existing preset with the same name.
        /// </summary>
        public void SaveCurrentAsPreset(string presetName, string description = "")
        {
            if (string.IsNullOrWhiteSpace(presetName))
            {
                Debug.LogWarning("[SWEF InputPresetManager] Preset name must not be empty.");
                return;
            }

            if (InputBindingManager.Instance == null) return;

            var bindings = new List<BindingEntry>(InputBindingManager.Instance.Bindings.Values);
            var preset = new InputPreset
            {
                presetName  = presetName,
                description = description,
                bindings    = bindings
            };

            // Add or replace in memory.
            for (int i = 0; i < _presets.Count; i++)
            {
                if (_presets[i].presetName == presetName)
                {
                    _presets[i] = preset;
                    PersistCustomPreset(preset);
                    OnPresetSaved?.Invoke(presetName);
                    return;
                }
            }

            _presets.Add(preset);
            PersistCustomPreset(preset);
            PersistCustomNameList();
            OnPresetSaved?.Invoke(presetName);
            Debug.Log($"[SWEF InputPresetManager] Preset '{presetName}' saved ({bindings.Count} bindings).");
        }

        /// <summary>
        /// Removes the named custom preset.  Built-in presets cannot be deleted.
        /// Returns <c>true</c> on success.
        /// </summary>
        public bool DeletePreset(string presetName)
        {
            for (int i = 0; i < _presets.Count; i++)
            {
                if (_presets[i].presetName != presetName) continue;

                // Don't allow deleting built-in presets loaded from the profile.
                if (IsBuiltin(presetName))
                {
                    Debug.LogWarning($"[SWEF InputPresetManager] Cannot delete built-in preset '{presetName}'.");
                    return false;
                }

                _presets.RemoveAt(i);
                PlayerPrefs.DeleteKey(PrefsKeyPresetPrefix + presetName);
                PersistCustomNameList();

                if (ActivePresetName == presetName)
                    ActivePresetName = string.Empty;

                OnPresetDeleted?.Invoke(presetName);
                Debug.Log($"[SWEF InputPresetManager] Preset '{presetName}' deleted.");
                return true;
            }
            return false;
        }

        #endregion

        #region Private — Load / Persist

        private void LoadBuiltinPresets()
        {
            if (InputBindingManager.Instance == null) return;
            var profile = InputBindingManager.Instance.Profile;
            if (profile == null || profile.defaultPresets == null) return;

            foreach (var preset in profile.defaultPresets)
                _presets.Add(preset);
        }

        private void LoadCustomPresets()
        {
            string rawNames = PlayerPrefs.GetString(PrefsKeyCustomList, string.Empty);
            if (string.IsNullOrEmpty(rawNames)) return;

            foreach (string name in rawNames.Split(','))
            {
                string trimmed = name.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                string json = PlayerPrefs.GetString(PrefsKeyPresetPrefix + trimmed, string.Empty);
                if (string.IsNullOrEmpty(json)) continue;

                try
                {
                    var wrapper = JsonUtility.FromJson<PresetWrapper>(json);
                    if (wrapper?.preset != null)
                        _presets.Add(wrapper.preset);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SWEF InputPresetManager] Failed to load preset '{trimmed}': {ex.Message}");
                }
            }
        }

        private void RestoreActivePreset()
        {
            string saved = PlayerPrefs.GetString(ActivePresetKey, string.Empty);
            if (!string.IsNullOrEmpty(saved))
                ActivePresetName = saved;
        }

        private void PersistCustomPreset(InputPreset preset)
        {
            string json = JsonUtility.ToJson(new PresetWrapper { preset = preset });
            PlayerPrefs.SetString(PrefsKeyPresetPrefix + preset.presetName, json);
            PlayerPrefs.Save();
        }

        private void PersistCustomNameList()
        {
            var customNames = new List<string>();
            foreach (var p in _presets)
            {
                if (!IsBuiltin(p.presetName))
                    customNames.Add(p.presetName);
            }
            PlayerPrefs.SetString(PrefsKeyCustomList, string.Join(",", customNames));
            PlayerPrefs.Save();
        }

        private bool IsBuiltin(string presetName)
        {
            if (InputBindingManager.Instance == null) return false;
            var profile = InputBindingManager.Instance.Profile;
            if (profile?.defaultPresets == null) return false;

            foreach (var p in profile.defaultPresets)
            {
                if (p.presetName == presetName)
                    return true;
            }
            return false;
        }

        #endregion

        #region JSON Wrapper

        [Serializable]
        private class PresetWrapper
        {
            public InputPreset preset;
        }

        #endregion
    }
}
