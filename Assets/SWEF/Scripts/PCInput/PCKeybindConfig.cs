// Phase 98 — PC Input & Controls Polish
// Assets/SWEF/Scripts/PCInput/PCKeybindConfig.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.PCInput
{
    /// <summary>
    /// Stores all PC keyboard/mouse bindings. Persists custom bindings to
    /// PlayerPrefs (JSON). Supports conflict detection and reset-to-defaults.
    /// Attach to the same GameObject as <see cref="PCFlightController"/>.
    /// </summary>
    public class PCKeybindConfig : MonoBehaviour
    {
        #region Singleton
        /// <summary>Shared keybind config instance.</summary>
        public static PCKeybindConfig Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadFromPlayerPrefs();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Events
        /// <summary>Fired when a keybind is changed at runtime.</summary>
        public event Action<string, KeyCode> OnKeybindChanged;

        /// <summary>Fired when a conflicting binding is detected.</summary>
        public event Action<string, string, KeyCode> OnKeybindConflict;
        #endregion

        #region Default Bindings
        private static readonly Dictionary<string, KeyCode> DefaultBindings = new Dictionary<string, KeyCode>
        {
            // Flight controls
            { "PitchUp",         KeyCode.W             },
            { "PitchDown",       KeyCode.S             },
            { "YawLeft",         KeyCode.A             },
            { "YawRight",        KeyCode.D             },
            { "RollLeft",        KeyCode.Q             },
            { "RollRight",       KeyCode.E             },
            { "ThrottleUp",      KeyCode.LeftShift     },
            { "ThrottleDown",    KeyCode.LeftControl   },
            { "ToggleAutopilot", KeyCode.Space         },
            // Camera
            { "FreeCamera",      KeyCode.F             },
            { "CameraPreset1",   KeyCode.Alpha1        },
            { "CameraPreset2",   KeyCode.Alpha2        },
            { "CameraPreset3",   KeyCode.Alpha3        },
            { "CameraPreset4",   KeyCode.Alpha4        },
            { "CameraPreset5",   KeyCode.Alpha5        },
            // UI shortcuts
            { "ToggleMinimap",   KeyCode.M             },
            { "ToggleHUD",       KeyCode.Tab           },
            { "Pause",           KeyCode.P             },
            { "Screenshot",      KeyCode.F12           },
            { "Menu",            KeyCode.Escape        },
            { "Help",            KeyCode.F1            },
            { "Diagnostics",     KeyCode.F3            },
            { "QuickSave",       KeyCode.S             },   // used only with LeftControl modifier
        };
        #endregion

        #region Private State
        private const string PlayerPrefsKey = "SWEF_PCKeybinds";
        private readonly Dictionary<string, KeyCode> _bindings = new Dictionary<string, KeyCode>();
        #endregion

        #region Persistence
        /// <summary>Load custom bindings from PlayerPrefs; falls back to defaults for missing keys.</summary>
        public void LoadFromPlayerPrefs()
        {
            ResetToDefaults(silent: true);

            string json = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var saved = JsonUtility.FromJson<SerializableBindings>(json);
                if (saved?.entries == null) return;
                foreach (var entry in saved.entries)
                {
                    if (System.Enum.TryParse<KeyCode>(entry.keyCode, out KeyCode kc))
                        _bindings[entry.action] = kc;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PCKeybindConfig] Failed to parse saved keybinds: {e.Message}");
            }
        }

        /// <summary>Persist current bindings to PlayerPrefs.</summary>
        public void SaveToPlayerPrefs()
        {
            var data = new SerializableBindings();
            foreach (var kvp in _bindings)
                data.entries.Add(new BindingEntry { action = kvp.Key, keyCode = kvp.Value.ToString() });

            PlayerPrefs.SetString(PlayerPrefsKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
        #endregion

        #region Public API
        /// <summary>Return the current <see cref="KeyCode"/> for an action, or the fallback if not found.</summary>
        public KeyCode GetKey(string actionName, KeyCode fallback = KeyCode.None)
        {
            if (_bindings.TryGetValue(actionName, out KeyCode kc)) return kc;
            return fallback;
        }

        /// <summary>
        /// Remap an action to a new key. Performs conflict detection before applying.
        /// </summary>
        /// <param name="actionName">Logical action name.</param>
        /// <param name="newKey">New <see cref="KeyCode"/> to bind.</param>
        /// <returns><c>true</c> if the binding was applied; <c>false</c> if a conflict prevented it.</returns>
        public bool SetKey(string actionName, KeyCode newKey)
        {
            // Conflict detection
            foreach (var kvp in _bindings)
            {
                if (kvp.Key != actionName && kvp.Value == newKey)
                {
                    OnKeybindConflict?.Invoke(actionName, kvp.Key, newKey);
                    Debug.LogWarning($"[PCKeybindConfig] Key conflict: '{newKey}' already bound to '{kvp.Key}'.");
                    return false;
                }
            }

            _bindings[actionName] = newKey;
            OnKeybindChanged?.Invoke(actionName, newKey);
            SaveToPlayerPrefs();
            return true;
        }

        /// <summary>Reset all bindings to defaults.</summary>
        public void ResetToDefaults() => ResetToDefaults(silent: false);

        private void ResetToDefaults(bool silent)
        {
            _bindings.Clear();
            foreach (var kvp in DefaultBindings)
                _bindings[kvp.Key] = kvp.Value;

            if (!silent)
            {
                SaveToPlayerPrefs();
                foreach (var kvp in _bindings)
                    OnKeybindChanged?.Invoke(kvp.Key, kvp.Value);
            }
        }

        /// <summary>Returns a read-only snapshot of all current bindings.</summary>
        public IReadOnlyDictionary<string, KeyCode> GetAllBindings() => _bindings;
        #endregion

        #region Serialisation Helpers
        [Serializable]
        private class SerializableBindings
        {
            public List<BindingEntry> entries = new List<BindingEntry>();
        }

        [Serializable]
        private class BindingEntry
        {
            public string action;
            public string keyCode;
        }
        #endregion
    }
}
