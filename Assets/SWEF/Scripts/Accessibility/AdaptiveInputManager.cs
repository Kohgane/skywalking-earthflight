using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Accessibility
{
    // ── Enums ────────────────────────────────────────────────────────────────────

    /// <summary>Abstract game actions decoupled from physical inputs.</summary>
    public enum InputAction
    {
        Throttle, Pitch, Roll, Yaw, Brake, Boost,
        Menu, Pause, CameraToggle, Map,
        SequentialCycle, SequentialConfirm
    }

    /// <summary>Available input modes.</summary>
    public enum InputMode
    {
        /// <summary>Standard two-handed control.</summary>
        Standard,
        /// <summary>One-handed layout using left hand only.</summary>
        OneHandedLeft,
        /// <summary>One-handed layout using right hand only.</summary>
        OneHandedRight,
        /// <summary>Switch-access sequential scanning mode.</summary>
        Sequential
    }

    /// <summary>Sensitivity response curve shapes.</summary>
    public enum SensitivityCurve
    {
        /// <summary>Output = input (1:1).</summary>
        Linear,
        /// <summary>Output = input² — gentle at centre, aggressive at edges.</summary>
        Exponential,
        /// <summary>Smooth S-curve — gentle at both ends, linear in the middle.</summary>
        SCurve
    }

    // ── Data classes ─────────────────────────────────────────────────────────────

    /// <summary>Per-axis dead-zone and sensitivity settings.</summary>
    [Serializable]
    public class AxisSettings
    {
        /// <summary>Normalised dead-zone radius (0–1).</summary>
        [Range(0f, 0.5f)] public float deadZone = 0.1f;

        /// <summary>Sensitivity multiplier applied after dead-zone removal.</summary>
        [Range(0.1f, 5f)] public float sensitivity = 1f;

        /// <summary>Response curve shape for this axis.</summary>
        public SensitivityCurve curve = SensitivityCurve.Linear;

        /// <summary>
        /// Applies dead-zone removal and curve shaping to a raw input value in [-1, 1].
        /// </summary>
        public float Process(float raw)
        {
            float abs = Mathf.Abs(raw);
            if (abs < deadZone) return 0f;
            float remapped = (abs - deadZone) / (1f - deadZone);
            float shaped = ApplyCurve(remapped);
            return Mathf.Sign(raw) * shaped * sensitivity;
        }

        private float ApplyCurve(float v)
        {
            switch (curve)
            {
                case SensitivityCurve.Exponential: return v * v;
                case SensitivityCurve.SCurve:      return v * v * (3f - 2f * v);
                default:                           return v;
            }
        }
    }

    /// <summary>
    /// Manages adaptive input remapping, one-handed layouts, sequential scanning,
    /// and per-axis dead-zone / sensitivity for accessible flight control.
    /// Integrates with <c>Assets/SWEF/Scripts/Flight/</c>.
    /// </summary>
    public class AdaptiveInputManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static AdaptiveInputManager Instance { get; private set; }

        // ── PlayerPrefs keys ─────────────────────────────────────────────────────
        private const string KeyRemapJson   = "SWEF_InputRemap";
        private const string KeyInputMode   = "SWEF_InputMode";

        // ── Serialised fields ────────────────────────────────────────────────────
        [Header("Input Mode")]
        [SerializeField] private InputMode inputMode = InputMode.Standard;

        [Header("One-Handed Mode")]
        [SerializeField] private bool  autoLevelAssist     = true;
        [SerializeField] [Range(0f, 5f)] private float autoLevelStrength = 1.5f;

        [Header("Sequential Input")]
        [SerializeField] private InputAction[] sequentialActions =
        {
            InputAction.Throttle, InputAction.Pitch, InputAction.Roll,
            InputAction.Yaw, InputAction.Brake, InputAction.Boost, InputAction.Menu
        };
        [SerializeField] [Range(0.1f, 3f)] private float sequentialCyclePause = 0.5f;

        [Header("Hold vs Toggle")]
        [SerializeField] private bool boostToggle = false;
        [SerializeField] private bool brakeToggle = false;

        [Header("Turbo / Repeat")]
        [SerializeField] private bool  turboEnabled    = false;
        [SerializeField] [Range(2f, 30f)] private float turboRepeatRate = 10f;

        [Header("Touch Accessibility")]
        [SerializeField] [Range(44f, 200f)] private float minTouchTargetPx = 88f;
        [SerializeField] [Range(0.1f, 3f)]  private float touchSensitivity  = 1f;

        [Header("Axis Settings")]
        [SerializeField] private AxisSettings pitchAxis = new AxisSettings();
        [SerializeField] private AxisSettings rollAxis  = new AxisSettings();
        [SerializeField] private AxisSettings yawAxis   = new AxisSettings();

        // ── Runtime state ─────────────────────────────────────────────────────────
        private Dictionary<InputAction, KeyCode> _keyMap;
        private int   _sequentialIndex;
        private bool  _boostActive;
        private bool  _brakeActive;
        private Coroutine _turboRoutine;
        private bool  _gyroEnabled;

        /// <summary>Current input mode.</summary>
        public InputMode CurrentMode => inputMode;

        /// <summary>Current sequential scan index.</summary>
        public int SequentialIndex => _sequentialIndex;

        // ── Default key bindings ─────────────────────────────────────────────────
        private static readonly Dictionary<InputAction, KeyCode> DefaultKeyMap =
            new Dictionary<InputAction, KeyCode>
            {
                { InputAction.Throttle,         KeyCode.W         },
                { InputAction.Pitch,            KeyCode.UpArrow   },
                { InputAction.Roll,             KeyCode.LeftArrow },
                { InputAction.Yaw,              KeyCode.Q         },
                { InputAction.Brake,            KeyCode.S         },
                { InputAction.Boost,            KeyCode.LeftShift },
                { InputAction.Menu,             KeyCode.Escape    },
                { InputAction.Pause,            KeyCode.P         },
                { InputAction.CameraToggle,     KeyCode.C         },
                { InputAction.Map,              KeyCode.M         },
                { InputAction.SequentialCycle,  KeyCode.Space     },
                { InputAction.SequentialConfirm,KeyCode.Return    },
            };

        // ── One-handed left/right layouts ────────────────────────────────────────
        private static readonly Dictionary<InputAction, KeyCode> OneHandedLeftMap =
            new Dictionary<InputAction, KeyCode>
            {
                { InputAction.Throttle, KeyCode.W }, { InputAction.Brake,  KeyCode.S },
                { InputAction.Roll,     KeyCode.A }, { InputAction.Pitch,  KeyCode.D },
                { InputAction.Yaw,      KeyCode.Q }, { InputAction.Boost,  KeyCode.E },
                { InputAction.Menu,     KeyCode.Escape }, { InputAction.Pause, KeyCode.Tab },
                { InputAction.SequentialCycle,   KeyCode.Space  },
                { InputAction.SequentialConfirm, KeyCode.F      },
            };

        private static readonly Dictionary<InputAction, KeyCode> OneHandedRightMap =
            new Dictionary<InputAction, KeyCode>
            {
                { InputAction.Throttle, KeyCode.Keypad8 }, { InputAction.Brake,  KeyCode.Keypad2 },
                { InputAction.Roll,     KeyCode.Keypad4 }, { InputAction.Pitch,  KeyCode.Keypad6 },
                { InputAction.Yaw,      KeyCode.Keypad7 }, { InputAction.Boost,  KeyCode.Keypad9 },
                { InputAction.Menu,     KeyCode.Escape   }, { InputAction.Pause, KeyCode.KeypadEnter },
                { InputAction.SequentialCycle,   KeyCode.Keypad0     },
                { InputAction.SequentialConfirm, KeyCode.KeypadPeriod},
            };

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Fired when a key binding is changed.</summary>
        public event Action<InputAction, KeyCode> OnInputRemapped;

        /// <summary>Fired when the input mode changes.</summary>
        public event Action<InputMode> OnInputModeChanged;

        /// <summary>Fired when any axis sensitivity setting changes.</summary>
        public event Action OnSensitivityChanged;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadRemappingProfile();
            ApplyModeLayout(inputMode);
        }

        private void OnDestroy()
        {
            if (_turboRoutine != null) StopCoroutine(_turboRoutine);
        }

        // ── Key remapping ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the <see cref="KeyCode"/> currently bound to the given action.
        /// Falls back to the default binding if the action is not mapped.
        /// </summary>
        public KeyCode GetKey(InputAction action)
        {
            if (_keyMap != null && _keyMap.TryGetValue(action, out KeyCode kc)) return kc;
            if (DefaultKeyMap.TryGetValue(action, out KeyCode def))              return def;
            return KeyCode.None;
        }

        /// <summary>
        /// Remaps an action to a new key. Saves the profile.
        /// </summary>
        public void Remap(InputAction action, KeyCode key)
        {
            if (_keyMap == null) _keyMap = new Dictionary<InputAction, KeyCode>(DefaultKeyMap);
            _keyMap[action] = key;
            SaveRemappingProfile();
            OnInputRemapped?.Invoke(action, key);
        }

        /// <summary>Resets all key bindings to factory defaults.</summary>
        public void ResetToDefaults()
        {
            _keyMap = new Dictionary<InputAction, KeyCode>(DefaultKeyMap);
            SaveRemappingProfile();
        }

        // ── Input mode ───────────────────────────────────────────────────────────

        /// <summary>
        /// Switches to a new input mode at runtime without restart.
        /// </summary>
        public void SetInputMode(InputMode mode)
        {
            inputMode = mode;
            ApplyModeLayout(mode);
            PlayerPrefs.SetInt(KeyInputMode, (int)mode);
            PlayerPrefs.Save();
            OnInputModeChanged?.Invoke(mode);
        }

        private void ApplyModeLayout(InputMode mode)
        {
            switch (mode)
            {
                case InputMode.OneHandedLeft:
                    _keyMap = new Dictionary<InputAction, KeyCode>(OneHandedLeftMap);
                    _gyroEnabled = true;
                    break;
                case InputMode.OneHandedRight:
                    _keyMap = new Dictionary<InputAction, KeyCode>(OneHandedRightMap);
                    _gyroEnabled = true;
                    break;
                default:
                    if (_keyMap == null)
                        _keyMap = new Dictionary<InputAction, KeyCode>(DefaultKeyMap);
                    _gyroEnabled = mode == InputMode.OneHandedLeft || mode == InputMode.OneHandedRight;
                    break;
            }
        }

        // ── Gyroscope steering ───────────────────────────────────────────────────

        /// <summary>
        /// Returns gyroscope-derived pitch/roll for one-handed mode.
        /// Returns <see cref="Vector2.zero"/> if gyro is unavailable or disabled.
        /// </summary>
        public Vector2 GetGyroInput()
        {
            if (!_gyroEnabled || !SystemInfo.supportsGyroscope) return Vector2.zero;
            Input.gyro.enabled = true;
            Vector3 grav = Input.gyro.gravity;
            return new Vector2(pitchAxis.Process(-grav.y), rollAxis.Process(grav.x));
        }

        // ── Auto-level assist ────────────────────────────────────────────────────

        /// <summary>
        /// Returns a corrective roll torque to gradually level the aircraft.
        /// Call this from the flight controller when in one-handed mode.
        /// </summary>
        public float GetAutoLevelRoll(float currentRollDeg)
        {
            if (!autoLevelAssist) return 0f;
            return -currentRollDeg * autoLevelStrength * Time.deltaTime;
        }

        // ── Sequential scanning ──────────────────────────────────────────────────

        /// <summary>
        /// Advances to the next action in the sequential scan list.
        /// </summary>
        public InputAction CycleSequential()
        {
            _sequentialIndex = (_sequentialIndex + 1) % sequentialActions.Length;
            return sequentialActions[_sequentialIndex];
        }

        /// <summary>Returns the action currently highlighted by the sequential scanner.</summary>
        public InputAction GetCurrentSequentialAction()
            => sequentialActions[Mathf.Clamp(_sequentialIndex, 0, sequentialActions.Length - 1)];

        // ── Axis processing ──────────────────────────────────────────────────────

        /// <summary>
        /// Processes a raw pitch axis value through dead-zone removal and curve shaping.
        /// </summary>
        public float ProcessPitch(float raw) => pitchAxis.Process(raw);

        /// <summary>
        /// Processes a raw roll axis value through dead-zone removal and curve shaping.
        /// </summary>
        public float ProcessRoll(float raw) => rollAxis.Process(raw);

        /// <summary>
        /// Processes a raw yaw axis value through dead-zone removal and curve shaping.
        /// </summary>
        public float ProcessYaw(float raw) => yawAxis.Process(raw);

        /// <summary>
        /// Updates axis settings and fires <see cref="OnSensitivityChanged"/>.
        /// </summary>
        public void SetAxisSettings(AxisSettings pitch, AxisSettings roll, AxisSettings yaw)
        {
            if (pitch != null) pitchAxis = pitch;
            if (roll  != null) rollAxis  = roll;
            if (yaw   != null) yawAxis   = yaw;
            OnSensitivityChanged?.Invoke();
        }

        // ── Hold vs Toggle ───────────────────────────────────────────────────────

        /// <summary>
        /// Processes a boost input press, honouring hold-vs-toggle configuration.
        /// Returns <c>true</c> while boost should be active.
        /// </summary>
        public bool ProcessBoost(bool buttonDown, bool buttonHeld)
        {
            if (boostToggle)
            {
                if (buttonDown) _boostActive = !_boostActive;
                return _boostActive;
            }
            return buttonHeld;
        }

        /// <summary>
        /// Processes a brake input press, honouring hold-vs-toggle configuration.
        /// </summary>
        public bool ProcessBrake(bool buttonDown, bool buttonHeld)
        {
            if (brakeToggle)
            {
                if (buttonDown) _brakeActive = !_brakeActive;
                return _brakeActive;
            }
            return buttonHeld;
        }

        // ── Turbo / repeat ───────────────────────────────────────────────────────

        /// <summary>Enables or disables auto-repeat for held buttons.</summary>
        public void SetTurbo(bool enabled)
        {
            turboEnabled = enabled;
            if (!enabled && _turboRoutine != null)
            {
                StopCoroutine(_turboRoutine);
                _turboRoutine = null;
            }
        }

        /// <summary>
        /// Starts a turbo repeat coroutine that fires <paramref name="onRepeat"/> at
        /// <see cref="turboRepeatRate"/> Hz while the button is held.
        /// </summary>
        public Coroutine StartTurbo(Action onRepeat) =>
            turboEnabled ? (_turboRoutine = StartCoroutine(TurboLoop(onRepeat))) : null;

        private IEnumerator TurboLoop(Action onRepeat)
        {
            float interval = 1f / Mathf.Max(turboRepeatRate, 0.1f);
            while (true)
            {
                onRepeat?.Invoke();
                yield return new WaitForSeconds(interval);
            }
        }

        // ── Persistence ──────────────────────────────────────────────────────────
        private void SaveRemappingProfile()
        {
            var data = new InputRemapData();
            foreach (var kvp in _keyMap)
                data.entries.Add(new InputRemapEntry { action = (int)kvp.Key, key = (int)kvp.Value });
            PlayerPrefs.SetString(KeyRemapJson, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private void LoadRemappingProfile()
        {
            int modeRaw = PlayerPrefs.GetInt(KeyInputMode, (int)InputMode.Standard);
            inputMode = Enum.IsDefined(typeof(InputMode), modeRaw)
                ? (InputMode)modeRaw
                : InputMode.Standard;
            string json = PlayerPrefs.GetString(KeyRemapJson, string.Empty);
            _keyMap = new Dictionary<InputAction, KeyCode>(DefaultKeyMap);
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var data = JsonUtility.FromJson<InputRemapData>(json);
                foreach (var entry in data.entries)
                    _keyMap[(InputAction)entry.action] = (KeyCode)entry.key;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF AdaptiveInput] Failed to load remap profile: {ex.Message}");
            }
        }

        // ── Inner serialisable types ─────────────────────────────────────────────
        [Serializable] private class InputRemapEntry { public int action; public int key; }
        [Serializable] private class InputRemapData   { public List<InputRemapEntry> entries = new List<InputRemapEntry>(); }
    }
}
