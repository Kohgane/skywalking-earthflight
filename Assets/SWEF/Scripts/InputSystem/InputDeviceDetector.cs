using System;
using UnityEngine;

namespace SWEF.InputSystem
{
    /// <summary>
    /// Phase 57 — Detects the active input device type at runtime and notifies
    /// other systems when the player switches between keyboard, gamepad, or touch.
    /// <para>
    /// Other systems should read <see cref="CurrentDevice"/> and subscribe to
    /// <see cref="OnDeviceChanged"/> rather than polling Unity's raw input APIs
    /// directly.  Detection runs every <see cref="detectionIntervalSeconds"/> seconds
    /// to keep per-frame overhead negligible.
    /// </para>
    /// </summary>
    public class InputDeviceDetector : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static InputDeviceDetector Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Detection")]
        [Tooltip("How often (seconds) to poll for device-type changes.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float detectionIntervalSeconds = 0.25f;

        [Tooltip("When true, touch input is preferred over keyboard when both are active on a touchscreen PC.")]
        [SerializeField] private bool preferTouchOnHybridDevice = true;

        [Header("Startup")]
        [Tooltip("Override the initial device type instead of auto-detecting. Set to Keyboard to let auto-detection run normally.")]
        [SerializeField] private InputDeviceType startupOverride = InputDeviceType.Keyboard;
        [SerializeField] private bool applyStartupOverride = false;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the active device type changes.
        /// Carries the previous and new <see cref="InputDeviceType"/>.
        /// </summary>
        public event Action<InputDeviceType, InputDeviceType> OnDeviceChanged;

        #endregion

        #region Public Properties

        /// <summary>Currently active input device type.</summary>
        public InputDeviceType CurrentDevice { get; private set; } = InputDeviceType.Keyboard;

        #endregion

        #region Private State

        private float _timeSinceLastDetection;

        // Cached last-known states to avoid allocating arrays every poll.
        private bool _prevGamepadConnected;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (applyStartupOverride)
            {
                CurrentDevice = startupOverride;
            }
            else if (InputBindingManager.Instance != null &&
                     InputBindingManager.Instance.Profile != null)
            {
                CurrentDevice = InputBindingManager.Instance.Profile.defaultDeviceType;
            }
        }

        private void Update()
        {
            _timeSinceLastDetection += Time.unscaledDeltaTime;
            if (_timeSinceLastDetection < detectionIntervalSeconds) return;
            _timeSinceLastDetection = 0f;

            DetectDevice();
        }

        #endregion

        #region Private — Detection Logic

        private void DetectDevice()
        {
            InputDeviceType detected = Detect();
            if (detected == CurrentDevice) return;

            InputDeviceType previous = CurrentDevice;
            CurrentDevice = detected;
            Debug.Log($"[SWEF InputDeviceDetector] Device changed: {previous} → {detected}");
            OnDeviceChanged?.Invoke(previous, detected);
        }

        private InputDeviceType Detect()
        {
            // Touch — highest priority on touch-capable devices.
            if (Input.touchCount > 0 && (preferTouchOnHybridDevice || !Application.isEditor))
                return InputDeviceType.Touch;

            // Gamepad.
            string[] joysticks = Input.GetJoystickNames();
            bool gamepadConnected = joysticks.Length > 0 && !string.IsNullOrEmpty(joysticks[0]);
            if (gamepadConnected)
            {
                // Check for any gamepad axis or button activity.
                if (HasGamepadActivity())
                    return InputDeviceType.Gamepad;
            }

            // Keyboard / mouse (default).
            if (HasKeyboardActivity())
                return InputDeviceType.Keyboard;

            // Return previous device when no new input detected.
            return CurrentDevice;
        }

        private static bool HasGamepadActivity()
        {
            // Sample the most common gamepad axes and buttons.
            if (Mathf.Abs(GetSafeAxis("Horizontal"))   > 0.1f) return true;
            if (Mathf.Abs(GetSafeAxis("Vertical"))     > 0.1f) return true;
            if (Mathf.Abs(GetSafeAxis("Yaw"))          > 0.1f) return true;
            if (Mathf.Abs(GetSafeAxis("Throttle"))     > 0.1f) return true;
            if (Mathf.Abs(GetSafeAxis("RightStickX"))  > 0.1f) return true;
            if (Mathf.Abs(GetSafeAxis("RightStickY"))  > 0.1f) return true;

            for (int i = 0; i <= 9; i++)
            {
                if (Input.GetKey($"joystick button {i}")) return true;
            }
            return false;
        }

        private static bool HasKeyboardActivity()
        {
            // A non-mouse key was pressed this frame.
            return Input.anyKey && !Input.GetMouseButton(0)
                                && !Input.GetMouseButton(1)
                                && !Input.GetMouseButton(2);
        }

        private static float GetSafeAxis(string axisName)
        {
            try   { return Input.GetAxis(axisName); }
            catch { return 0f; }
        }

        #endregion
    }
}
