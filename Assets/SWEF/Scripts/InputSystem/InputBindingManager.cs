using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.InputSystem
{
    /// <summary>
    /// Phase 57 — Central singleton that manages all custom key/button bindings for
    /// the Input Rebinding &amp; Controller Support System.
    /// <para>
    /// Attach to a persistent bootstrap GameObject.  Wire an
    /// <see cref="InputSystemProfile"/> asset in the inspector, or create one via
    /// <em>Assets → Create → SWEF → InputSystem → Input System Profile</em>.
    /// </para>
    /// </summary>
    public class InputBindingManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static InputBindingManager Instance { get; private set; }

        #endregion

        #region Constants

        private const string KeyPrefix = "SWEF_Bind_";

        #endregion

        #region Inspector

        [Header("Profile")]
        [Tooltip("Input system profile asset — contains default presets, gamepad profile, and touch layout.")]
        [SerializeField] private InputSystemProfile profile;

        [Header("Default Bindings")]
        [Tooltip("Master list of all rebindable actions.  Loaded from profile.defaultPresets[0] if empty.")]
        [SerializeField] private List<BindingEntry> defaultBindings = new List<BindingEntry>
        {
            new BindingEntry { actionName = "ThrottleUp",    category = InputActionCategory.Flight,      primaryKey = "W",         gamepadButton = "Right Trigger", isRebindable = true  },
            new BindingEntry { actionName = "ThrottleDown",  category = InputActionCategory.Flight,      primaryKey = "S",         gamepadButton = "Left Trigger",  isRebindable = true  },
            new BindingEntry { actionName = "YawLeft",       category = InputActionCategory.Flight,      primaryKey = "A",         gamepadButton = "Horizontal",    isRebindable = true  },
            new BindingEntry { actionName = "YawRight",      category = InputActionCategory.Flight,      primaryKey = "D",         gamepadButton = "Horizontal",    isRebindable = true  },
            new BindingEntry { actionName = "PitchUp",       category = InputActionCategory.Flight,      primaryKey = "UpArrow",   gamepadButton = "Vertical",      isRebindable = true  },
            new BindingEntry { actionName = "PitchDown",     category = InputActionCategory.Flight,      primaryKey = "DownArrow", gamepadButton = "Vertical",      isRebindable = true  },
            new BindingEntry { actionName = "RollLeft",      category = InputActionCategory.Flight,      primaryKey = "Q",         gamepadButton = "joystick button 4", isRebindable = true  },
            new BindingEntry { actionName = "RollRight",     category = InputActionCategory.Flight,      primaryKey = "E",         gamepadButton = "joystick button 5", isRebindable = true  },
            new BindingEntry { actionName = "Boost",         category = InputActionCategory.Flight,      primaryKey = "LeftShift", gamepadButton = "joystick button 0", isRebindable = true  },
            new BindingEntry { actionName = "Brake",         category = InputActionCategory.Flight,      primaryKey = "Space",     gamepadButton = "joystick button 1", isRebindable = true  },
            new BindingEntry { actionName = "CameraSwitch",  category = InputActionCategory.Camera,      primaryKey = "C",         gamepadButton = "joystick button 8", isRebindable = true  },
            new BindingEntry { actionName = "CameraZoomIn",  category = InputActionCategory.Camera,      primaryKey = "KeypadPlus",gamepadButton = "Right Stick Y",     isRebindable = true  },
            new BindingEntry { actionName = "CameraZoomOut", category = InputActionCategory.Camera,      primaryKey = "KeypadMinus",gamepadButton = "Right Stick Y",    isRebindable = true  },
            new BindingEntry { actionName = "Pause",         category = InputActionCategory.UI,          primaryKey = "Escape",    gamepadButton = "joystick button 7", isRebindable = false },
            new BindingEntry { actionName = "Confirm",       category = InputActionCategory.UI,          primaryKey = "Return",    gamepadButton = "joystick button 0", isRebindable = true  },
            new BindingEntry { actionName = "Back",          category = InputActionCategory.UI,          primaryKey = "Backspace", gamepadButton = "joystick button 1", isRebindable = true  },
            new BindingEntry { actionName = "ChatToggle",    category = InputActionCategory.Social,      primaryKey = "T",         gamepadButton = "",                  isRebindable = true  },
            new BindingEntry { actionName = "VoiceToggle",   category = InputActionCategory.Social,      primaryKey = "V",         gamepadButton = "joystick button 9", isRebindable = true  },
            new BindingEntry { actionName = "PhotoCapture",  category = InputActionCategory.PhotoMode,   primaryKey = "F12",       gamepadButton = "joystick button 2", isRebindable = true  },
            new BindingEntry { actionName = "PhotoFilter",   category = InputActionCategory.PhotoMode,   primaryKey = "F",         gamepadButton = "joystick button 3", isRebindable = true  },
            new BindingEntry { actionName = "MusicPlay",     category = InputActionCategory.MusicPlayer, primaryKey = "M",         gamepadButton = "",                  isRebindable = true  },
            new BindingEntry { actionName = "MusicNext",     category = InputActionCategory.MusicPlayer, primaryKey = "Period",    gamepadButton = "",                  isRebindable = true  },
            new BindingEntry { actionName = "MusicPrev",     category = InputActionCategory.MusicPlayer, primaryKey = "Comma",     gamepadButton = "",                  isRebindable = true  },
        };

        #endregion

        #region Events

        /// <summary>Fired when a binding is changed.  Carries the updated <see cref="BindingEntry"/>.</summary>
        public event Action<BindingEntry> OnBindingChanged;

        /// <summary>Fired when all bindings are reset to defaults.</summary>
        public event Action OnBindingsReset;

        /// <summary>Fired when a preset is applied.  Carries the preset name.</summary>
        public event Action<string> OnPresetApplied;

        /// <summary>Fired when rebind listening begins.  Carries the action name.</summary>
        public event Action<string> OnRebindStarted;

        /// <summary>Fired when rebind listening ends (success or timeout).  Carries success flag.</summary>
        public event Action<string, bool> OnRebindFinished;

        #endregion

        #region Public Properties

        /// <summary>Read-only view of the current binding map.</summary>
        public IReadOnlyDictionary<string, BindingEntry> Bindings => _bindings;

        /// <summary>The loaded profile asset (may be null if unassigned in inspector).</summary>
        public InputSystemProfile Profile => profile;

        /// <summary>
        /// When <c>true</c> the manager is actively listening for a new key press
        /// to complete an in-progress rebind.
        /// </summary>
        public bool IsRebinding { get; private set; }

        /// <summary>Action name currently being rebound, or <c>null</c> when idle.</summary>
        public string RebindingAction { get; private set; }

        #endregion

        #region Private State

        private readonly Dictionary<string, BindingEntry> _bindings = new Dictionary<string, BindingEntry>();
        private Coroutine _rebindCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildDefaultMap();
            if (profile != null && profile.persistBindings)
                LoadSavedBindings();
        }

        #endregion

        #region Public API — Querying

        /// <summary>
        /// Returns the <see cref="BindingEntry"/> for <paramref name="actionName"/>, or the
        /// default struct value if the action is not found.
        /// </summary>
        public BindingEntry GetBinding(string actionName)
        {
            if (_bindings.TryGetValue(actionName, out BindingEntry entry))
                return entry;
            Debug.LogWarning($"[SWEF InputBindingManager] Unknown action '{actionName}'");
            return default;
        }

        /// <summary>
        /// Returns <c>true</c> during the frame the primary key for
        /// <paramref name="actionName"/> was pressed down.
        /// </summary>
        public bool GetActionDown(string actionName)
        {
            if (!_bindings.TryGetValue(actionName, out BindingEntry entry)) return false;
            if (TryParseKey(entry.primaryKey,   out KeyCode pk) && Input.GetKeyDown(pk)) return true;
            if (TryParseKey(entry.secondaryKey, out KeyCode sk) && Input.GetKeyDown(sk)) return true;
            return false;
        }

        /// <summary>
        /// Returns <c>true</c> while the primary key for <paramref name="actionName"/> is held.
        /// </summary>
        public bool GetActionHeld(string actionName)
        {
            if (!_bindings.TryGetValue(actionName, out BindingEntry entry)) return false;
            if (TryParseKey(entry.primaryKey,   out KeyCode pk) && Input.GetKey(pk)) return true;
            if (TryParseKey(entry.secondaryKey, out KeyCode sk) && Input.GetKey(sk)) return true;
            return false;
        }

        /// <summary>
        /// Returns <c>true</c> during the frame the key for <paramref name="actionName"/>
        /// was released.
        /// </summary>
        public bool GetActionUp(string actionName)
        {
            if (!_bindings.TryGetValue(actionName, out BindingEntry entry)) return false;
            if (TryParseKey(entry.primaryKey,   out KeyCode pk) && Input.GetKeyUp(pk)) return true;
            if (TryParseKey(entry.secondaryKey, out KeyCode sk) && Input.GetKeyUp(sk)) return true;
            return false;
        }

        /// <summary>Returns all binding entries that belong to <paramref name="category"/>.</summary>
        public List<BindingEntry> GetBindingsByCategory(InputActionCategory category)
        {
            var result = new List<BindingEntry>();
            foreach (var pair in _bindings)
            {
                if (pair.Value.category == category)
                    result.Add(pair.Value);
            }
            return result;
        }

        #endregion

        #region Public API — Rebinding

        /// <summary>
        /// Directly assigns <paramref name="newKey"/> as the primary key for
        /// <paramref name="actionName"/> and persists the change.
        /// </summary>
        public void SetPrimaryKey(string actionName, string newKey)
        {
            if (!ValidateRebind(actionName)) return;
            var entry = _bindings[actionName];
            entry.primaryKey = newKey;
            _bindings[actionName] = entry;
            SaveBinding(actionName, entry);
            OnBindingChanged?.Invoke(entry);
        }

        /// <summary>
        /// Directly assigns <paramref name="newKey"/> as the secondary key for
        /// <paramref name="actionName"/> and persists the change.
        /// </summary>
        public void SetSecondaryKey(string actionName, string newKey)
        {
            if (!ValidateRebind(actionName)) return;
            var entry = _bindings[actionName];
            entry.secondaryKey = newKey;
            _bindings[actionName] = entry;
            SaveBinding(actionName, entry);
            OnBindingChanged?.Invoke(entry);
        }

        /// <summary>
        /// Starts an interactive rebind listening window for the primary key slot of
        /// <paramref name="actionName"/>.  The next key press is applied as the new binding.
        /// </summary>
        public void StartRebind(string actionName)
        {
            if (IsRebinding)
            {
                Debug.LogWarning("[SWEF InputBindingManager] Already rebinding — cancel first.");
                return;
            }
            if (!ValidateRebind(actionName)) return;
            _rebindCoroutine = StartCoroutine(RebindListenRoutine(actionName));
        }

        /// <summary>Cancels an in-progress rebind listening window.</summary>
        public void CancelRebind()
        {
            if (!IsRebinding) return;
            if (_rebindCoroutine != null)
            {
                StopCoroutine(_rebindCoroutine);
                _rebindCoroutine = null;
            }
            string actionName = RebindingAction;
            IsRebinding      = false;
            RebindingAction  = null;
            OnRebindFinished?.Invoke(actionName, false);
        }

        /// <summary>Resets all bindings to the defaults defined in <see cref="defaultBindings"/>.</summary>
        public void ResetAllBindings()
        {
            foreach (var entry in defaultBindings)
                _bindings[entry.actionName] = entry;

            if (profile != null && profile.persistBindings)
                ClearPersistedBindings();

            OnBindingsReset?.Invoke();
            Debug.Log("[SWEF InputBindingManager] All bindings reset to defaults.");
        }

        /// <summary>
        /// Resets the single binding for <paramref name="actionName"/> to its default value.
        /// </summary>
        public void ResetBinding(string actionName)
        {
            foreach (var def in defaultBindings)
            {
                if (def.actionName == actionName)
                {
                    _bindings[actionName] = def;
                    PlayerPrefs.DeleteKey(KeyPrefix + actionName + "_primary");
                    PlayerPrefs.DeleteKey(KeyPrefix + actionName + "_secondary");
                    PlayerPrefs.Save();
                    OnBindingChanged?.Invoke(def);
                    return;
                }
            }
            Debug.LogWarning($"[SWEF InputBindingManager] No default found for action '{actionName}'");
        }

        #endregion

        #region Public API — Presets

        /// <summary>
        /// Applies a named preset from <see cref="InputSystemProfile.defaultPresets"/> by name.
        /// Returns <c>true</c> on success.
        /// </summary>
        public bool ApplyPreset(string presetName)
        {
            if (profile == null || profile.defaultPresets == null) return false;
            foreach (var preset in profile.defaultPresets)
            {
                if (preset.presetName == presetName)
                {
                    ApplyPreset(preset);
                    return true;
                }
            }
            Debug.LogWarning($"[SWEF InputBindingManager] Preset '{presetName}' not found.");
            return false;
        }

        /// <summary>Applies the provided <paramref name="preset"/> directly.</summary>
        public void ApplyPreset(InputPreset preset)
        {
            if (preset.bindings == null) return;
            foreach (var entry in preset.bindings)
            {
                _bindings[entry.actionName] = entry;
                if (profile != null && profile.persistBindings)
                    SaveBinding(entry.actionName, entry);
            }
            OnPresetApplied?.Invoke(preset.presetName);
            Debug.Log($"[SWEF InputBindingManager] Preset '{preset.presetName}' applied ({preset.bindings.Count} bindings).");
        }

        #endregion

        #region Internal — Build & Persistence

        private void BuildDefaultMap()
        {
            _bindings.Clear();
            foreach (var entry in defaultBindings)
                _bindings[entry.actionName] = entry;
        }

        private void LoadSavedBindings()
        {
            foreach (var actionName in new List<string>(_bindings.Keys))
            {
                string primaryKey   = KeyPrefix + actionName + "_primary";
                string secondaryKey = KeyPrefix + actionName + "_secondary";

                if (PlayerPrefs.HasKey(primaryKey) || PlayerPrefs.HasKey(secondaryKey))
                {
                    var entry = _bindings[actionName];
                    if (PlayerPrefs.HasKey(primaryKey))
                        entry.primaryKey = PlayerPrefs.GetString(primaryKey);
                    if (PlayerPrefs.HasKey(secondaryKey))
                        entry.secondaryKey = PlayerPrefs.GetString(secondaryKey);
                    _bindings[actionName] = entry;
                }
            }
        }

        private void SaveBinding(string actionName, BindingEntry entry)
        {
            PlayerPrefs.SetString(KeyPrefix + actionName + "_primary",   entry.primaryKey   ?? string.Empty);
            PlayerPrefs.SetString(KeyPrefix + actionName + "_secondary", entry.secondaryKey ?? string.Empty);
            PlayerPrefs.Save();
        }

        private void ClearPersistedBindings()
        {
            foreach (string actionName in _bindings.Keys)
            {
                PlayerPrefs.DeleteKey(KeyPrefix + actionName + "_primary");
                PlayerPrefs.DeleteKey(KeyPrefix + actionName + "_secondary");
            }
            PlayerPrefs.Save();
        }

        #endregion

        #region Internal — Rebind Coroutine

        private IEnumerator RebindListenRoutine(string actionName)
        {
            IsRebinding     = true;
            RebindingAction = actionName;
            OnRebindStarted?.Invoke(actionName);

            float timeout = profile != null ? profile.rebindTimeoutSeconds : 8f;
            float elapsed = 0f;

            // Skip the first frame so the key that opened the UI doesn't immediately bind.
            yield return null;

            while (elapsed < timeout)
            {
                elapsed += Time.unscaledDeltaTime;

                // Check every KeyCode for a new press.
                foreach (KeyCode kc in Enum.GetValues(typeof(KeyCode)))
                {
                    if (kc == KeyCode.Escape) continue; // reserved for cancel
                    if (Input.GetKeyDown(kc))
                    {
                        SetPrimaryKey(actionName, kc.ToString());
                        IsRebinding     = false;
                        RebindingAction = null;
                        _rebindCoroutine = null;
                        OnRebindFinished?.Invoke(actionName, true);
                        yield break;
                    }
                }

                yield return null;
            }

            // Timed out.
            IsRebinding      = false;
            RebindingAction  = null;
            _rebindCoroutine = null;
            OnRebindFinished?.Invoke(actionName, false);
            Debug.Log($"[SWEF InputBindingManager] Rebind for '{actionName}' timed out.");
        }

        #endregion

        #region Internal — Helpers

        private bool ValidateRebind(string actionName)
        {
            if (!_bindings.TryGetValue(actionName, out BindingEntry entry))
            {
                Debug.LogWarning($"[SWEF InputBindingManager] Unknown action '{actionName}'");
                return false;
            }
            if (!entry.isRebindable)
            {
                Debug.LogWarning($"[SWEF InputBindingManager] Action '{actionName}' is not rebindable.");
                return false;
            }
            return true;
        }

        private static bool TryParseKey(string keyName, out KeyCode keyCode)
        {
            if (string.IsNullOrEmpty(keyName))
            {
                keyCode = KeyCode.None;
                return false;
            }
            return Enum.TryParse(keyName, ignoreCase: true, out keyCode);
        }

        #endregion
    }
}
