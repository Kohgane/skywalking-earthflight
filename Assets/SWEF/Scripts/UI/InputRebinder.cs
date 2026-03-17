using System;
using UnityEngine;

namespace SWEF.UI
{
    /// <summary>
    /// Keyboard/gamepad input rebinder for editor and desktop builds.
    /// Maintains a list of named key bindings that can be remapped at runtime
    /// and are persisted in PlayerPrefs. Use <see cref="GetKeyDown"/> and
    /// <see cref="GetKeyHeld"/> as drop-in replacements for <c>Input.GetKeyDown</c>
    /// and <c>Input.GetKey</c>.
    /// </summary>
    public class InputRebinder : MonoBehaviour
    {
        // ── Key binding ──────────────────────────────────────────────────────────
        /// <summary>A named key binding with a default and an overridable current key.</summary>
        [Serializable]
        public struct KeyBinding
        {
            public string  actionName;
            public KeyCode defaultKey;
            public KeyCode currentKey;
        }

        // ── Default bindings ─────────────────────────────────────────────────────
        [Header("Bindings")]
        [SerializeField] private KeyBinding[] bindings = new KeyBinding[]
        {
            new KeyBinding { actionName = "ThrottleUp",    defaultKey = KeyCode.W,          currentKey = KeyCode.W          },
            new KeyBinding { actionName = "ThrottleDown",  defaultKey = KeyCode.S,          currentKey = KeyCode.S          },
            new KeyBinding { actionName = "YawLeft",       defaultKey = KeyCode.A,          currentKey = KeyCode.A          },
            new KeyBinding { actionName = "YawRight",      defaultKey = KeyCode.D,          currentKey = KeyCode.D          },
            new KeyBinding { actionName = "PitchUp",       defaultKey = KeyCode.UpArrow,    currentKey = KeyCode.UpArrow    },
            new KeyBinding { actionName = "PitchDown",     defaultKey = KeyCode.DownArrow,  currentKey = KeyCode.DownArrow  },
            new KeyBinding { actionName = "RollLeft",      defaultKey = KeyCode.Q,          currentKey = KeyCode.Q          },
            new KeyBinding { actionName = "RollRight",     defaultKey = KeyCode.E,          currentKey = KeyCode.E          },
            new KeyBinding { actionName = "Pause",         defaultKey = KeyCode.Escape,     currentKey = KeyCode.Escape     },
            new KeyBinding { actionName = "Screenshot",    defaultKey = KeyCode.F12,        currentKey = KeyCode.F12        },
        };

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            LoadSavedBindings();
        }

        // ── Public API ───────────────────────────────────────────────────────────
        /// <summary>Updates the key for the named action and persists the change.</summary>
        public void SetBinding(string actionName, KeyCode newKey)
        {
            for (int i = 0; i < bindings.Length; i++)
            {
                if (bindings[i].actionName == actionName)
                {
                    bindings[i].currentKey = newKey;
                    PlayerPrefs.SetInt(PrefsKey(actionName), (int)newKey);
                    PlayerPrefs.Save();
                    return;
                }
            }
            Debug.LogWarning($"[SWEF] InputRebinder: unknown action '{actionName}'");
        }

        /// <summary>Resets all bindings to their defaults and clears saved overrides.</summary>
        public void ResetAllBindings()
        {
            for (int i = 0; i < bindings.Length; i++)
            {
                bindings[i].currentKey = bindings[i].defaultKey;
                PlayerPrefs.DeleteKey(PrefsKey(bindings[i].actionName));
            }
            PlayerPrefs.Save();
        }

        /// <summary>Returns the current <see cref="KeyCode"/> for the named action.</summary>
        public KeyCode GetKey(string actionName)
        {
            foreach (var b in bindings)
            {
                if (b.actionName == actionName)
                    return b.currentKey;
            }
            Debug.LogWarning($"[SWEF] InputRebinder: unknown action '{actionName}'");
            return KeyCode.None;
        }

        /// <summary>
        /// Returns <c>true</c> during the frame the key for <paramref name="actionName"/>
        /// was pressed down (equivalent to <c>Input.GetKeyDown</c>).
        /// </summary>
        public bool GetKeyDown(string actionName)
            => Input.GetKeyDown(GetKey(actionName));

        /// <summary>
        /// Returns <c>true</c> while the key for <paramref name="actionName"/> is held
        /// (equivalent to <c>Input.GetKey</c>).
        /// </summary>
        public bool GetKeyHeld(string actionName)
            => Input.GetKey(GetKey(actionName));

        // ── Internal ─────────────────────────────────────────────────────────────
        private void LoadSavedBindings()
        {
            for (int i = 0; i < bindings.Length; i++)
            {
                string key = PrefsKey(bindings[i].actionName);
                if (PlayerPrefs.HasKey(key))
                    bindings[i].currentKey = (KeyCode)PlayerPrefs.GetInt(key);
            }
        }

        private static string PrefsKey(string actionName)
            => $"SWEF_Key_{actionName}";
    }
}
