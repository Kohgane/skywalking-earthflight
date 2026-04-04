// Phase 98 — PC Input & Controls Polish
// Assets/SWEF/Scripts/PCInput/PCFlightController.cs
using System;
using UnityEngine;

#if !UNITY_ANDROID && !UNITY_IOS
using SWEF.Autopilot;
using SWEF.Flight;
#endif

namespace SWEF.PCInput
{
    /// <summary>
    /// Full WASD + mouse flight control for PC/desktop platforms.
    /// Additive layer on top of the existing InputSystem — does not replace mobile touch input.
    /// </summary>
    /// <remarks>
    /// Key bindings:
    /// <list type="bullet">
    ///   <item>W/S — pitch (nose up / down)</item>
    ///   <item>A/D — yaw (turn left / right)</item>
    ///   <item>Q/E — roll left / right</item>
    ///   <item>Shift — throttle up</item>
    ///   <item>Ctrl  — throttle down</item>
    ///   <item>Space — toggle autopilot</item>
    ///   <item>Mouse — fine pitch/yaw (configurable sensitivity)</item>
    ///   <item>Mouse wheel — zoom camera</item>
    ///   <item>Right-click + drag — free look</item>
    /// </list>
    /// All bindings are remappable via <see cref="PCKeybindConfig"/>.
    /// </remarks>
    [DisallowMultipleComponent]
    public class PCFlightController : MonoBehaviour
    {
        #region Singleton
        /// <summary>Shared PC flight controller instance.</summary>
        public static PCFlightController Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitialiseConfig();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Inspector
        [Header("References")]
        [Tooltip("Keybind configuration asset (auto-located if null).")]
        [SerializeField] private PCKeybindConfig keybindConfig;

        [Tooltip("Mouse flight assist (auto-located if null).")]
        [SerializeField] private MouseFlightAssist mouseFlightAssist;

        [Header("Keyboard Sensitivity")]
        [Tooltip("Pitch/yaw input strength from WASD keys.")]
        [SerializeField, Range(0.1f, 5f)] private float keyboardSensitivity = 1f;

        [Tooltip("Roll input strength from Q/E keys.")]
        [SerializeField, Range(0.1f, 5f)] private float rollSensitivity = 1f;

        [Header("Mouse Sensitivity")]
        [Tooltip("Mouse X-axis (yaw) sensitivity multiplier.")]
        [SerializeField, Range(0.01f, 5f)] private float mouseYawSensitivity = 0.3f;

        [Tooltip("Mouse Y-axis (pitch) sensitivity multiplier.")]
        [SerializeField, Range(0.01f, 5f)] private float mousePitchSensitivity = 0.3f;

        [Header("Throttle")]
        [Tooltip("Throttle change rate per second.")]
        [SerializeField, Range(0.05f, 1f)] private float throttleRate = 0.2f;

        [Header("Smoothing")]
        [Tooltip("Input smoothing coefficient (lower = more responsive, higher = smoother).")]
        [SerializeField, Range(0f, 0.95f)] private float inputSmoothing = 0.15f;
        #endregion

        #region Public State
        /// <summary>Current throttle value in [0, 1].</summary>
        public float Throttle { get; private set; } = 0f;

        /// <summary>Current smoothed pitch input in [-1, 1].</summary>
        public float PitchInput { get; private set; }

        /// <summary>Current smoothed yaw input in [-1, 1].</summary>
        public float YawInput { get; private set; }

        /// <summary>Current smoothed roll input in [-1, 1].</summary>
        public float RollInput { get; private set; }

        /// <summary>Whether the controller is enabled and processing input.</summary>
        public bool IsActive { get; private set; } = true;
        #endregion

        #region Events
        /// <summary>Fired when the autopilot toggle key is pressed.</summary>
        public event Action OnAutopilotToggleRequested;

        /// <summary>Fired when throttle changes. Argument is the new throttle value [0,1].</summary>
        public event Action<float> OnThrottleChanged;
        #endregion

        #region Private State
        private float _rawPitch;
        private float _rawYaw;
        private float _rawRoll;
        private bool _freeLookActive;
        private float _lastThrottle;
        #endregion

        #region Unity Lifecycle
        private void InitialiseConfig()
        {
            if (keybindConfig == null)
                keybindConfig = FindFirstObjectByType<PCKeybindConfig>();
            if (mouseFlightAssist == null)
                mouseFlightAssist = FindFirstObjectByType<MouseFlightAssist>();
        }

        private void Update()
        {
#if !UNITY_ANDROID && !UNITY_IOS
            if (!IsActive) return;
            HandleKeyboardFlight();
            HandleMouseFlight();
            HandleThrottle();
            HandleSpecialKeys();
            SmoothInputs();
#endif
        }
        #endregion

        #region Input Handling
#if !UNITY_ANDROID && !UNITY_IOS
        private void HandleKeyboardFlight()
        {
            KeyCode pitchUp   = GetKey("PitchUp",   KeyCode.W);
            KeyCode pitchDown = GetKey("PitchDown",  KeyCode.S);
            KeyCode yawLeft   = GetKey("YawLeft",    KeyCode.A);
            KeyCode yawRight  = GetKey("YawRight",   KeyCode.D);
            KeyCode rollLeft  = GetKey("RollLeft",   KeyCode.Q);
            KeyCode rollRight = GetKey("RollRight",  KeyCode.E);

            _rawPitch = 0f;
            if (Input.GetKey(pitchUp))   _rawPitch -= keyboardSensitivity;
            if (Input.GetKey(pitchDown)) _rawPitch += keyboardSensitivity;

            _rawYaw = 0f;
            if (Input.GetKey(yawLeft))   _rawYaw -= keyboardSensitivity;
            if (Input.GetKey(yawRight))  _rawYaw += keyboardSensitivity;

            _rawRoll = 0f;
            if (Input.GetKey(rollLeft))  _rawRoll -= rollSensitivity;
            if (Input.GetKey(rollRight)) _rawRoll += rollSensitivity;
        }

        private void HandleMouseFlight()
        {
            _freeLookActive = Input.GetMouseButton(1); // right-click

            if (!_freeLookActive && (mouseFlightAssist == null || !mouseFlightAssist.IsAssistMode))
            {
                float mouseX = Input.GetAxis("Mouse X") * mouseYawSensitivity;
                float mouseY = Input.GetAxis("Mouse Y") * mousePitchSensitivity;

                _rawYaw   += mouseX;
                _rawPitch -= mouseY;
            }
        }

        private void HandleThrottle()
        {
            KeyCode throttleUp   = GetKey("ThrottleUp",   KeyCode.LeftShift);
            KeyCode throttleDown = GetKey("ThrottleDown",  KeyCode.LeftControl);

            if (Input.GetKey(throttleUp))
                Throttle = Mathf.Clamp01(Throttle + throttleRate * Time.deltaTime);
            if (Input.GetKey(throttleDown))
                Throttle = Mathf.Clamp01(Throttle - throttleRate * Time.deltaTime);

            if (!Mathf.Approximately(Throttle, _lastThrottle))
            {
                OnThrottleChanged?.Invoke(Throttle);
                _lastThrottle = Throttle;
            }
        }

        private void HandleSpecialKeys()
        {
            KeyCode autopilot = GetKey("ToggleAutopilot", KeyCode.Space);
            if (Input.GetKeyDown(autopilot))
                OnAutopilotToggleRequested?.Invoke();
        }

        private void SmoothInputs()
        {
            PitchInput = Mathf.Lerp(PitchInput, Mathf.Clamp(_rawPitch, -1f, 1f), 1f - inputSmoothing);
            YawInput   = Mathf.Lerp(YawInput,   Mathf.Clamp(_rawYaw,   -1f, 1f), 1f - inputSmoothing);
            RollInput  = Mathf.Lerp(RollInput,  Mathf.Clamp(_rawRoll,  -1f, 1f), 1f - inputSmoothing);
        }

        private KeyCode GetKey(string actionName, KeyCode fallback)
        {
            if (keybindConfig != null)
                return keybindConfig.GetKey(actionName, fallback);
            return fallback;
        }
#endif
        #endregion

        #region Public API
        /// <summary>Enable or disable PC flight input processing.</summary>
        public void SetActive(bool active) => IsActive = active;

        /// <summary>Set throttle directly (e.g., from a gamepad trigger).</summary>
        /// <param name="value">Throttle value in [0, 1].</param>
        public void SetThrottle(float value)
        {
            Throttle = Mathf.Clamp01(value);
            OnThrottleChanged?.Invoke(Throttle);
            _lastThrottle = Throttle;
        }
        #endregion
    }
}
