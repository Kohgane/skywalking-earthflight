// Phase 98 — PC Input & Controls Polish
// Assets/SWEF/Scripts/PCInput/KeyboardShortcutManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.PCInput
{
    /// <summary>
    /// Global keyboard shortcut handler for non-flight actions.
    /// Shortcuts are suppressed when a text input field is focused.
    /// </summary>
    /// <remarks>
    /// Default shortcuts:
    /// <list type="table">
    ///   <item><term>M</term><description>Toggle minimap</description></item>
    ///   <item><term>Tab</term><description>Toggle HUD</description></item>
    ///   <item><term>P</term><description>Pause</description></item>
    ///   <item><term>F12</term><description>Screenshot</description></item>
    ///   <item><term>Esc</term><description>Menu / cancel</description></item>
    ///   <item><term>F1</term><description>Help overlay</description></item>
    ///   <item><term>F3</term><description>Input diagnostics overlay</description></item>
    ///   <item><term>Ctrl+S</term><description>Quick save</description></item>
    /// </list>
    /// </remarks>
    [DisallowMultipleComponent]
    public class KeyboardShortcutManager : MonoBehaviour
    {
        #region Singleton
        /// <summary>Shared keyboard shortcut manager instance.</summary>
        public static KeyboardShortcutManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            RegisterDefaultShortcuts();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Events
        /// <summary>Fired when the minimap toggle shortcut is pressed.</summary>
        public event Action OnToggleMinimap;

        /// <summary>Fired when the HUD toggle shortcut is pressed.</summary>
        public event Action OnToggleHUD;

        /// <summary>Fired when the pause shortcut is pressed.</summary>
        public event Action OnPause;

        /// <summary>Fired when the screenshot shortcut is pressed.</summary>
        public event Action OnScreenshot;

        /// <summary>Fired when the menu/cancel shortcut is pressed.</summary>
        public event Action OnMenuCancel;

        /// <summary>Fired when the help overlay shortcut is pressed.</summary>
        public event Action OnHelp;

        /// <summary>Fired when the quick-save shortcut is pressed.</summary>
        public event Action OnQuickSave;

        /// <summary>Fired when the diagnostics overlay shortcut is pressed.</summary>
        public event Action OnDiagnostics;

        /// <summary>Fired whenever any registered shortcut is triggered. Argument is the action name.</summary>
        public event Action<string> OnShortcutFired;
        #endregion

        #region Public State
        /// <summary>Whether shortcuts are currently suppressed (e.g., text field focused).</summary>
        public bool ShortcutsSuppressed { get; private set; }
        #endregion

        #region Private State
        private readonly List<ShortcutEntry> _shortcuts = new List<ShortcutEntry>();
        private PCKeybindConfig _keybindConfig;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            _keybindConfig = FindFirstObjectByType<PCKeybindConfig>();
        }

        private void Update()
        {
#if !UNITY_ANDROID && !UNITY_IOS
            if (ShortcutsSuppressed) return;
            ProcessShortcuts();
#endif
        }
        #endregion

        #region Shortcut Registration
        private void RegisterDefaultShortcuts()
        {
            _shortcuts.Clear();
            _shortcuts.Add(new ShortcutEntry("ToggleMinimap", KeyCode.M,      false, () => OnToggleMinimap?.Invoke()));
            _shortcuts.Add(new ShortcutEntry("ToggleHUD",      KeyCode.Tab,    false, () => OnToggleHUD?.Invoke()));
            _shortcuts.Add(new ShortcutEntry("Pause",          KeyCode.P,      false, () => OnPause?.Invoke()));
            _shortcuts.Add(new ShortcutEntry("Screenshot",     KeyCode.F12,    false, () => OnScreenshot?.Invoke()));
            _shortcuts.Add(new ShortcutEntry("Menu",           KeyCode.Escape, false, () => OnMenuCancel?.Invoke()));
            _shortcuts.Add(new ShortcutEntry("Help",           KeyCode.F1,     false, () => OnHelp?.Invoke()));
            _shortcuts.Add(new ShortcutEntry("Diagnostics",    KeyCode.F3,     false, () => OnDiagnostics?.Invoke()));
            _shortcuts.Add(new ShortcutEntry("QuickSave",      KeyCode.S,      true,  () => OnQuickSave?.Invoke()));
        }

        /// <summary>Register a custom shortcut at runtime.</summary>
        /// <param name="actionName">Logical action name shown in the help overlay.</param>
        /// <param name="key">Key that triggers the action.</param>
        /// <param name="requireCtrl">Whether Ctrl must be held.</param>
        /// <param name="callback">Callback invoked when the shortcut fires.</param>
        public void RegisterShortcut(string actionName, KeyCode key, bool requireCtrl, Action callback)
        {
            if (string.IsNullOrEmpty(actionName) || callback == null) return;
            _shortcuts.Add(new ShortcutEntry(actionName, key, requireCtrl, callback));
        }

        /// <summary>Returns all registered shortcut entries (for displaying a help overlay).</summary>
        public IReadOnlyList<ShortcutEntry> GetAllShortcuts() => _shortcuts;
        #endregion

        #region Input Processing
#if !UNITY_ANDROID && !UNITY_IOS
        private void ProcessShortcuts()
        {
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            foreach (var shortcut in _shortcuts)
            {
                if (shortcut.RequireCtrl && !ctrl) continue;
                if (!shortcut.RequireCtrl && ctrl)  continue;

                KeyCode key = ResolveKey(shortcut);
                if (Input.GetKeyDown(key))
                {
                    shortcut.Callback?.Invoke();
                    OnShortcutFired?.Invoke(shortcut.ActionName);
                }
            }
        }

        private KeyCode ResolveKey(ShortcutEntry shortcut)
        {
            if (_keybindConfig != null)
                return _keybindConfig.GetKey(shortcut.ActionName, shortcut.DefaultKey);
            return shortcut.DefaultKey;
        }
#endif
        #endregion

        #region Public API
        /// <summary>Suppress or restore all shortcuts (e.g., when a text input is focused).</summary>
        /// <param name="suppressed">Whether to suppress shortcuts.</param>
        public void SetSuppressed(bool suppressed) => ShortcutsSuppressed = suppressed;
        #endregion

        #region Data Types
        /// <summary>Describes a single keyboard shortcut registration.</summary>
        public class ShortcutEntry
        {
            /// <summary>Logical action name.</summary>
            public string ActionName { get; }
            /// <summary>Default key code.</summary>
            public KeyCode DefaultKey { get; }
            /// <summary>Whether Ctrl must be held.</summary>
            public bool RequireCtrl { get; }
            /// <summary>Callback invoked when the shortcut fires.</summary>
            public Action Callback { get; }

            internal ShortcutEntry(string actionName, KeyCode key, bool requireCtrl, Action callback)
            {
                ActionName  = actionName;
                DefaultKey  = key;
                RequireCtrl = requireCtrl;
                Callback    = callback;
            }
        }
        #endregion
    }
}
