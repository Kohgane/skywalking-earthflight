// Phase 98 — PC Input & Controls Polish
// Assets/SWEF/Scripts/PCInput/PCInputDiagnostics.cs
using System.Text;
using UnityEngine;

namespace SWEF.PCInput
{
    /// <summary>
    /// Debug overlay that displays the current PC input state.
    /// Shows connected devices, active keybinds, axis values, and button states.
    /// Toggle with F3 (or the "Diagnostics" keybind action).
    /// Integrates with the existing DebugOverlay module when present.
    /// </summary>
    [DisallowMultipleComponent]
    public class PCInputDiagnostics : MonoBehaviour
    {
        #region Singleton
        /// <summary>Shared diagnostics instance.</summary>
        public static PCInputDiagnostics Instance { get; private set; }

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
        [Header("Display")]
        [Tooltip("Whether the overlay is visible at startup.")]
        [SerializeField] private bool showOnStart = false;

        [Tooltip("Position of the overlay on screen.")]
        [SerializeField] private Rect overlayRect = new Rect(10f, 10f, 380f, 500f);

        [Tooltip("Background colour of the overlay panel.")]
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.75f);

        [Header("Update Rate")]
        [Tooltip("How often (seconds) the overlay text is refreshed.")]
        [SerializeField, Range(0.05f, 1f)] private float refreshInterval = 0.1f;
        #endregion

        #region Private State
        private bool _visible;
        private float _refreshTimer;
        private string _cachedText = string.Empty;
        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private PCFlightController _flightCtrl;
        private GamepadProfileManager _gamepadMgr;
        private PCKeybindConfig _keybindCfg;
        private KeyboardShortcutManager _shortcutMgr;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            _visible   = showOnStart;
            _flightCtrl  = FindFirstObjectByType<PCFlightController>();
            _gamepadMgr  = FindFirstObjectByType<GamepadProfileManager>();
            _keybindCfg  = FindFirstObjectByType<PCKeybindConfig>();
            _shortcutMgr = FindFirstObjectByType<KeyboardShortcutManager>();

            // Subscribe to toggle event from shortcut manager
            if (_shortcutMgr != null)
                _shortcutMgr.OnDiagnostics += ToggleOverlay;
        }

        private void OnDestroy()
        {
            if (_shortcutMgr != null)
                _shortcutMgr.OnDiagnostics -= ToggleOverlay;
        }

        private void Update()
        {
#if !UNITY_ANDROID && !UNITY_IOS
            // Fallback toggle: F3
            if (Input.GetKeyDown(KeyCode.F3))
                ToggleOverlay();
#endif
            if (!_visible) return;

            _refreshTimer += Time.deltaTime;
            if (_refreshTimer >= refreshInterval)
            {
                _refreshTimer = 0f;
                _cachedText = BuildDiagnosticsText();
            }
        }

        private void OnGUI()
        {
            if (!_visible) return;
            InitStyles();

            GUI.Box(overlayRect, GUIContent.none, _boxStyle);

            GUILayout.BeginArea(new Rect(overlayRect.x + 8f, overlayRect.y + 8f,
                                         overlayRect.width - 16f, overlayRect.height - 16f));
            GUILayout.Label(_cachedText, _labelStyle);
            GUILayout.EndArea();
        }
        #endregion

        #region Diagnostics Text Builder
        private string BuildDiagnosticsText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== PC INPUT DIAGNOSTICS ===");

            // Connected devices
            sb.AppendLine("\n[Devices]");
            string[] joysticks = Input.GetJoystickNames();
            if (joysticks.Length == 0)
            {
                sb.AppendLine("  No joysticks detected");
            }
            else
            {
                foreach (var j in joysticks)
                    sb.AppendLine($"  • {(string.IsNullOrEmpty(j) ? "(empty slot)" : j)}");
            }

            // Gamepad profile
            if (_gamepadMgr != null)
            {
                sb.AppendLine($"\n[Gamepad] Connected: {_gamepadMgr.IsGamepadConnected}");
                if (_gamepadMgr.ActiveProfile != null)
                    sb.AppendLine($"  Profile: {_gamepadMgr.ActiveProfile.ProfileName}");
                sb.AppendLine($"  Pitch:   {_gamepadMgr.PitchInput:F2}");
                sb.AppendLine($"  Yaw:     {_gamepadMgr.YawInput:F2}");
                sb.AppendLine($"  Roll:    {_gamepadMgr.RollInput:F2}");
                sb.AppendLine($"  Throttle:{_gamepadMgr.ThrottleInput:F2}");
                sb.AppendLine($"  Brake:   {_gamepadMgr.BrakeInput:F2}");
                sb.AppendLine($"  CamH:    {_gamepadMgr.CameraHorizontal:F2}");
                sb.AppendLine($"  CamV:    {_gamepadMgr.CameraVertical:F2}");
            }

            // Keyboard/mouse flight
            if (_flightCtrl != null)
            {
                sb.AppendLine($"\n[Keyboard Flight] Active: {_flightCtrl.IsActive}");
                sb.AppendLine($"  Pitch:    {_flightCtrl.PitchInput:F2}");
                sb.AppendLine($"  Yaw:      {_flightCtrl.YawInput:F2}");
                sb.AppendLine($"  Roll:     {_flightCtrl.RollInput:F2}");
                sb.AppendLine($"  Throttle: {_flightCtrl.Throttle:F2}");
            }

            // Mouse
#if !UNITY_ANDROID && !UNITY_IOS
            sb.AppendLine($"\n[Mouse]");
            sb.AppendLine($"  Pos:    {Input.mousePosition.x:F0}, {Input.mousePosition.y:F0}");
            sb.AppendLine($"  Delta:  {Input.GetAxis("Mouse X"):F2}, {Input.GetAxis("Mouse Y"):F2}");
            sb.AppendLine($"  Scroll: {Input.GetAxis("Mouse ScrollWheel"):F2}");
            sb.AppendLine($"  L:{Input.GetMouseButton(0)} M:{Input.GetMouseButton(2)} R:{Input.GetMouseButton(1)}");
#endif

            // Keybinds (first 8)
            if (_keybindCfg != null)
            {
                sb.AppendLine("\n[Keybinds (sample)]");
                int count = 0;
                foreach (var kvp in _keybindCfg.GetAllBindings())
                {
                    sb.AppendLine($"  {kvp.Key,-20} → {kvp.Value}");
                    if (++count >= 8) { sb.AppendLine("  ..."); break; }
                }
            }

            sb.AppendLine("\n[F3] Toggle this overlay");
            return sb.ToString();
        }
        #endregion

        #region GUI Helpers
        private void InitStyles()
        {
            if (_boxStyle != null) return;

            _boxStyle = new GUIStyle(GUI.skin.box);
            var bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, backgroundColor);
            bgTex.Apply();
            _boxStyle.normal.background = bgTex;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                richText  = false,
                wordWrap  = false,
                alignment = TextAnchor.UpperLeft
            };
            _labelStyle.normal.textColor = Color.white;
        }
        #endregion

        #region Public API
        /// <summary>Show or hide the diagnostics overlay.</summary>
        public void ToggleOverlay() => _visible = !_visible;

        /// <summary>Explicitly set overlay visibility.</summary>
        /// <param name="visible">Whether to show the overlay.</param>
        public void SetVisible(bool visible) => _visible = visible;
        #endregion
    }
}
