using System;
using UnityEngine;

namespace SWEF.InputSystem
{
    /// <summary>
    /// Phase 57 — Processes raw gamepad axis and button data, applying the active
    /// <see cref="GamepadProfile"/> (dead-zones, sensitivity curve, axis inversion).
    /// <para>
    /// Attach to the same GameObject as <see cref="InputBindingManager"/> or any
    /// persistent controller object.  Poll <see cref="GetAxis"/> and
    /// <see cref="GetButton"/> from flight and camera controllers each frame.
    /// </para>
    /// </summary>
    public class GamepadInputHandler : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static GamepadInputHandler Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Profile")]
        [Tooltip("Gamepad profile used for dead-zone and sensitivity processing. " +
                 "Falls back to GamepadProfile.Default if InputBindingManager has no profile.")]
        [SerializeField] private GamepadProfile gamepadProfile = GamepadProfile.Default;

        [Header("Axis Names (Unity Input Manager)")]
        [SerializeField] private string pitchAxisName    = "Vertical";
        [SerializeField] private string rollAxisName     = "Horizontal";
        [SerializeField] private string yawAxisName      = "Yaw";
        [SerializeField] private string throttleAxisName = "Throttle";
        [SerializeField] private string lookXAxisName    = "RightStickX";
        [SerializeField] private string lookYAxisName    = "RightStickY";

        #endregion

        #region Events

        /// <summary>Fired when a gamepad device is connected.  Carries the device name.</summary>
        public event Action<string> OnGamepadConnected;

        /// <summary>Fired when a gamepad device is disconnected.</summary>
        public event Action OnGamepadDisconnected;

        #endregion

        #region Public Properties

        /// <summary><c>true</c> when at least one gamepad is currently connected.</summary>
        public bool IsGamepadConnected { get; private set; }

        /// <summary>Name of the connected gamepad as reported by Unity, or empty string.</summary>
        public string ConnectedGamepadName { get; private set; } = string.Empty;

        /// <summary>Processed pitch axis value [-1, 1] after dead-zone and curve shaping.</summary>
        public float Pitch    { get; private set; }

        /// <summary>Processed roll axis value [-1, 1].</summary>
        public float Roll     { get; private set; }

        /// <summary>Processed yaw axis value [-1, 1].</summary>
        public float Yaw      { get; private set; }

        /// <summary>Processed throttle axis value [0, 1].</summary>
        public float Throttle { get; private set; }

        /// <summary>Processed camera look-horizontal value [-1, 1].</summary>
        public float LookX    { get; private set; }

        /// <summary>Processed camera look-vertical value [-1, 1].</summary>
        public float LookY    { get; private set; }

        #endregion

        #region Private State

        private bool _previouslyConnected;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Sync profile from InputBindingManager if available.
            if (InputBindingManager.Instance != null &&
                InputBindingManager.Instance.Profile != null)
            {
                gamepadProfile = InputBindingManager.Instance.Profile.gamepadProfile;
            }
        }

        private void Update()
        {
            DetectConnectionChange();
            if (IsGamepadConnected)
                ReadAxes();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Returns the processed value for a named axis, applying the active
        /// <see cref="GamepadProfile"/> dead-zone and sensitivity curve.
        /// </summary>
        /// <param name="axisName">Unity Input Manager axis name.</param>
        public float GetAxis(string axisName)
        {
            if (string.IsNullOrEmpty(axisName)) return 0f;
            float raw = 0f;
            try { raw = Input.GetAxis(axisName); }
            catch { return 0f; }
            return ProcessAxis(raw, invertAxis: false);
        }

        /// <summary>
        /// Returns <c>true</c> while the specified gamepad button is held.
        /// </summary>
        /// <param name="buttonIndex">Joystick button index (0-based).</param>
        public bool GetButton(int buttonIndex)
            => Input.GetKey($"joystick button {buttonIndex}");

        /// <summary>
        /// Returns <c>true</c> during the frame the specified gamepad button was pressed.
        /// </summary>
        public bool GetButtonDown(int buttonIndex)
            => Input.GetKeyDown($"joystick button {buttonIndex}");

        /// <summary>
        /// Returns <c>true</c> during the frame the specified gamepad button was released.
        /// </summary>
        public bool GetButtonUp(int buttonIndex)
            => Input.GetKeyUp($"joystick button {buttonIndex}");

        /// <summary>Applies a new <see cref="GamepadProfile"/> at runtime.</summary>
        public void ApplyProfile(GamepadProfile newProfile)
        {
            gamepadProfile = newProfile;
            Debug.Log($"[SWEF GamepadInputHandler] Profile '{newProfile.profileName}' applied.");
        }

        #endregion

        #region Private — Device Detection

        private void DetectConnectionChange()
        {
            string[] joysticks  = Input.GetJoystickNames();
            bool     connected  = joysticks.Length > 0 && !string.IsNullOrEmpty(joysticks[0]);
            IsGamepadConnected  = connected;

            if (connected && !_previouslyConnected)
            {
                ConnectedGamepadName = joysticks[0];
                OnGamepadConnected?.Invoke(ConnectedGamepadName);
                Debug.Log($"[SWEF GamepadInputHandler] Gamepad connected: {ConnectedGamepadName}");
            }
            else if (!connected && _previouslyConnected)
            {
                ConnectedGamepadName = string.Empty;
                OnGamepadDisconnected?.Invoke();
                Debug.Log("[SWEF GamepadInputHandler] Gamepad disconnected.");
            }

            _previouslyConnected = connected;
        }

        #endregion

        #region Private — Axis Processing

        private void ReadAxes()
        {
            Pitch    = ReadAxis(pitchAxisName,    gamepadProfile.invertPitch);
            Roll     = ReadAxis(rollAxisName,     gamepadProfile.invertRoll);
            Yaw      = ReadAxis(yawAxisName,      gamepadProfile.invertYaw);
            LookX    = ReadAxis(lookXAxisName,    false);
            LookY    = ReadAxis(lookYAxisName,    false);

            // Throttle: remap from [-1, 1] to [0, 1].
            float rawThrottle = ReadAxis(throttleAxisName, false);
            Throttle = (rawThrottle + 1f) * 0.5f;
        }

        private float ReadAxis(string axisName, bool invert)
        {
            float raw = 0f;
            try { raw = Input.GetAxis(axisName); }
            catch { return 0f; }
            float processed = ProcessAxis(raw, invert);
            return processed;
        }

        /// <summary>
        /// Applies dead-zone removal and sensitivity curve from the active
        /// <see cref="GamepadProfile"/> to <paramref name="raw"/>.
        /// </summary>
        private float ProcessAxis(float raw, bool invert)
        {
            float abs = Mathf.Abs(raw);

            // Inner dead-zone
            if (abs < gamepadProfile.deadzoneInner)
                return 0f;

            // Outer dead-zone saturation
            float outerZone = Mathf.Max(gamepadProfile.deadzoneOuter, gamepadProfile.deadzoneInner + 0.01f);
            float normalised = Mathf.Clamp01((abs - gamepadProfile.deadzoneInner) /
                                             (outerZone - gamepadProfile.deadzoneInner));

            // Sensitivity curve lookup
            float curved = SampleCurve(normalised, gamepadProfile.sensitivityCurvePoints);
            float result = Mathf.Sign(raw) * curved;
            return invert ? -result : result;
        }

        /// <summary>
        /// Linearly samples a piecewise curve defined by <paramref name="points"/> at <paramref name="t"/> ∈ [0, 1].
        /// Falls back to linear passthrough when the array is null or empty.
        /// </summary>
        private static float SampleCurve(float t, float[] points)
        {
            if (points == null || points.Length < 2)
                return t;

            float indexF = t * (points.Length - 1);
            int   lo     = Mathf.FloorToInt(indexF);
            int   hi     = Mathf.Min(lo + 1, points.Length - 1);
            float frac   = indexF - lo;
            return Mathf.Lerp(points[lo], points[hi], frac);
        }

        #endregion
    }
}
