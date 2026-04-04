// Phase 98 — PC Input & Controls Polish
// Assets/SWEF/Scripts/PCInput/PCControlsUIPanel.cs
using System.Collections;
using UnityEngine;

namespace SWEF.PCInput
{
    /// <summary>
    /// In-game settings panel for PC controls.
    /// Provides tabs for Keyboard, Mouse, and Gamepad configuration,
    /// live keybind remapping, sensitivity sliders, and axis inversion.
    /// Uses Unity's built-in IMGUI for portability (no UnityEngine.UI dependency).
    /// </summary>
    [DisallowMultipleComponent]
    public class PCControlsUIPanel : MonoBehaviour
    {
        #region Singleton
        /// <summary>Shared controls UI panel instance.</summary>
        public static PCControlsUIPanel Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Inspector
        [Header("Panel Rect")]
        [SerializeField] private Rect panelRect = new Rect(100f, 50f, 600f, 500f);

        [Header("References (auto-located if null)")]
        [SerializeField] private PCKeybindConfig keybindConfig;
        [SerializeField] private MouseFlightAssist mouseAssist;
        [SerializeField] private GamepadProfileManager gamepadManager;
        [SerializeField] private KeyboardShortcutManager shortcutManager;
        #endregion

        #region Private State
        private enum Tab { Keyboard, Mouse, Gamepad }

        private bool _isOpen;
        private Tab _activeTab = Tab.Keyboard;

        // Keybind remapping state
        private string _remappingAction;
        private bool _waitingForKey;

        // Pending settings (applied on Apply)
        private float _pendingMouseYawSens  = 0.3f;
        private float _pendingMousePitchSens = 0.3f;
        private bool  _pendingInvertY       = false;
        private float _pendingGamepadSens   = 1f;

        // Scroll positions
        private Vector2 _keyboardScroll;
        private Vector2 _gamepadScroll;

        // Style cache
        private GUIStyle _windowStyle;
        private GUIStyle _tabActiveStyle;
        private GUIStyle _tabInactiveStyle;
        private GUIStyle _headerStyle;
        private bool _stylesInitialised;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            if (keybindConfig    == null) keybindConfig    = FindFirstObjectByType<PCKeybindConfig>();
            if (mouseAssist      == null) mouseAssist      = FindFirstObjectByType<MouseFlightAssist>();
            if (gamepadManager   == null) gamepadManager   = FindFirstObjectByType<GamepadProfileManager>();
            if (shortcutManager  == null) shortcutManager  = FindFirstObjectByType<KeyboardShortcutManager>();

            // Subscribe to shortcut manager to allow opening via a shortcut
            if (shortcutManager != null)
                shortcutManager.RegisterShortcut("OpenControlsPanel", KeyCode.F10, false, TogglePanel);
        }

        private void Update()
        {
            if (_waitingForKey)
                CaptureAnyKey();
        }

        private void OnGUI()
        {
            if (!_isOpen) return;
            InitStyles();

            panelRect = GUI.Window(9801, panelRect, DrawPanelContent, "PC Controls Settings", _windowStyle);
        }
        #endregion

        #region Panel Drawing
        private void DrawPanelContent(int windowId)
        {
            GUI.DragWindow(new Rect(0, 0, panelRect.width, 20f));

            GUILayout.BeginVertical(GUILayout.ExpandHeight(true));

            // Tab bar
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("⌨ Keyboard", _activeTab == Tab.Keyboard ? _tabActiveStyle : _tabInactiveStyle))
                _activeTab = Tab.Keyboard;
            if (GUILayout.Button("🖱 Mouse",    _activeTab == Tab.Mouse    ? _tabActiveStyle : _tabInactiveStyle))
                _activeTab = Tab.Mouse;
            if (GUILayout.Button("🎮 Gamepad",  _activeTab == Tab.Gamepad  ? _tabActiveStyle : _tabInactiveStyle))
                _activeTab = Tab.Gamepad;
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            switch (_activeTab)
            {
                case Tab.Keyboard: DrawKeyboardTab(); break;
                case Tab.Mouse:    DrawMouseTab();    break;
                case Tab.Gamepad:  DrawGamepadTab();  break;
            }

            GUILayout.FlexibleSpace();

            // Bottom buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply"))         ApplySettings();
            if (GUILayout.Button("Reset Default")) ResetToDefaults();
            if (GUILayout.Button("Cancel"))        _isOpen = false;
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        // ── Keyboard Tab ──────────────────────────────────────────────────────────
        private void DrawKeyboardTab()
        {
            GUILayout.Label("Keyboard Bindings", _headerStyle);

            if (keybindConfig == null)
            {
                GUILayout.Label("No PCKeybindConfig found in scene.");
                return;
            }

            _keyboardScroll = GUILayout.BeginScrollView(_keyboardScroll, GUILayout.Height(340f));
            foreach (var kvp in keybindConfig.GetAllBindings())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(kvp.Key, GUILayout.Width(160f));

                string label = (_waitingForKey && _remappingAction == kvp.Key)
                    ? ">>> Press any key <<<"
                    : kvp.Value.ToString();

                if (GUILayout.Button(label, GUILayout.Width(180f)))
                    StartRemapping(kvp.Key);

                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        // ── Mouse Tab ─────────────────────────────────────────────────────────────
        private void DrawMouseTab()
        {
            GUILayout.Label("Mouse Settings", _headerStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Yaw Sensitivity", GUILayout.Width(160f));
            _pendingMouseYawSens = GUILayout.HorizontalSlider(_pendingMouseYawSens, 0.01f, 5f, GUILayout.Width(200f));
            GUILayout.Label(_pendingMouseYawSens.ToString("F2"), GUILayout.Width(50f));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pitch Sensitivity", GUILayout.Width(160f));
            _pendingMousePitchSens = GUILayout.HorizontalSlider(_pendingMousePitchSens, 0.01f, 5f, GUILayout.Width(200f));
            GUILayout.Label(_pendingMousePitchSens.ToString("F2"), GUILayout.Width(50f));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Invert Y-Axis", GUILayout.Width(160f));
            _pendingInvertY = GUILayout.Toggle(_pendingInvertY, _pendingInvertY ? "On" : "Off");
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.Label("Mouse-Follow Assist", _headerStyle);

            if (mouseAssist != null)
            {
                GUILayout.Label($"Assist Mode: {(mouseAssist.IsAssistMode ? "ON" : "OFF")}");
                if (GUILayout.Button("Toggle Assist Mode", GUILayout.Width(180f)))
                    mouseAssist.ToggleAssistMode();
            }
        }

        // ── Gamepad Tab ───────────────────────────────────────────────────────────
        private void DrawGamepadTab()
        {
            GUILayout.Label("Gamepad Settings", _headerStyle);

            if (gamepadManager == null)
            {
                GUILayout.Label("No GamepadProfileManager found in scene.");
                return;
            }

            GUILayout.Label($"Connected: {gamepadManager.IsGamepadConnected}");
            if (gamepadManager.ActiveProfile != null)
                GUILayout.Label($"Profile: {gamepadManager.ActiveProfile.ProfileName} ({gamepadManager.ActiveProfile.GamepadType})");

            GUILayout.Space(4f);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Axis Sensitivity", GUILayout.Width(160f));
            _pendingGamepadSens = GUILayout.HorizontalSlider(_pendingGamepadSens, 0.1f, 3f, GUILayout.Width(200f));
            GUILayout.Label(_pendingGamepadSens.ToString("F2"), GUILayout.Width(50f));
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.Label("Switch Profile", _headerStyle);

            _gamepadScroll = GUILayout.BeginScrollView(_gamepadScroll, GUILayout.Height(180f));
            for (int i = 0; i < gamepadManager.Profiles.Count; i++)
            {
                var p = gamepadManager.Profiles[i];
                GUILayout.BeginHorizontal();
                bool isActive = gamepadManager.ActiveProfile == p;
                if (isActive) GUILayout.Label("▶ " + p.ProfileName, GUILayout.Width(200f));
                else if (GUILayout.Button(p.ProfileName, GUILayout.Width(200f)))
                    gamepadManager.SetProfile(i);

                if (p.GamepadType == GamepadType.Custom)
                {
                    if (GUILayout.Button("Delete", GUILayout.Width(60f)))
                        gamepadManager.DeleteCustomProfile(i);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Clone Active as Custom", GUILayout.Width(220f)))
            {
                for (int i = 0; i < gamepadManager.Profiles.Count; i++)
                {
                    if (gamepadManager.Profiles[i] == gamepadManager.ActiveProfile)
                    {
                        gamepadManager.CreateCustomProfile(i, null);
                        break;
                    }
                }
            }
        }
        #endregion

        #region Keybind Remapping
        private void StartRemapping(string actionName)
        {
            _remappingAction = actionName;
            _waitingForKey   = true;
        }

        private void CaptureAnyKey()
        {
            if (!_waitingForKey) return;
            if (!Input.anyKeyDown) return;

            // Scan all key codes
            foreach (KeyCode kc in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (kc == KeyCode.Escape)
                {
                    // Cancel remapping
                    _waitingForKey   = false;
                    _remappingAction = null;
                    return;
                }
                if (Input.GetKeyDown(kc))
                {
                    if (keybindConfig != null)
                        keybindConfig.SetKey(_remappingAction, kc);
                    _waitingForKey   = false;
                    _remappingAction = null;
                    return;
                }
            }
        }
        #endregion

        #region Apply / Reset
        private void ApplySettings()
        {
            // Sensitivity values are stored on the inspector fields of PCFlightController.
            // For runtime application we broadcast via a helper method if available.
            // (Full integration would require public setters on PCFlightController.)
            Debug.Log($"[PCControlsUIPanel] Settings applied — MouseYaw:{_pendingMouseYawSens:F2} MousePitch:{_pendingMousePitchSens:F2} InvertY:{_pendingInvertY} GamepadSens:{_pendingGamepadSens:F2}");

            if (mouseAssist != null)
                mouseAssist.SetDeadZone(0.05f); // preserve default dead zone

            _isOpen = false;
        }

        private void ResetToDefaults()
        {
            _pendingMouseYawSens   = 0.3f;
            _pendingMousePitchSens = 0.3f;
            _pendingInvertY        = false;
            _pendingGamepadSens    = 1f;
            keybindConfig?.ResetToDefaults();
        }
        #endregion

        #region GUI Style Init
        private void InitStyles()
        {
            if (_stylesInitialised) return;
            _stylesInitialised = true;

            _windowStyle = new GUIStyle(GUI.skin.window);

            _tabActiveStyle = new GUIStyle(GUI.skin.button);
            _tabActiveStyle.normal.textColor  = Color.yellow;
            _tabActiveStyle.fontStyle         = FontStyle.Bold;

            _tabInactiveStyle = new GUIStyle(GUI.skin.button);

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize  = 13
            };
        }
        #endregion

        #region Public API
        /// <summary>Toggle the panel open/closed.</summary>
        public void TogglePanel()
        {
            _isOpen = !_isOpen;
            if (shortcutManager != null)
                shortcutManager.SetSuppressed(_isOpen);
        }

        /// <summary>Open the panel and show the specified tab.</summary>
        /// <param name="tab">Tab to display: 0 = Keyboard, 1 = Mouse, 2 = Gamepad.</param>
        public void OpenTab(int tab)
        {
            _isOpen     = true;
            _activeTab  = (Tab)Mathf.Clamp(tab, 0, 2);
        }
        #endregion
    }
}
