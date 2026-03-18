using System;
using UnityEngine;
using SWEF.Flight;

#if UNITY_XR_MANAGEMENT
using UnityEngine.XR;
using System.Collections.Generic;
#endif

namespace SWEF.XR
{
    /// <summary>
    /// Abstracts XR controller input into SWEF's existing input model.
    /// Bridges XR controllers to <see cref="FlightController"/> and
    /// <see cref="AltitudeController"/> interfaces via Unity's InputDevice API.
    /// </summary>
    public class XRInputAdapter : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Flight References")]
        [SerializeField] private FlightController  flightController;
        [SerializeField] private AltitudeController altitudeController;

        [Header("Input Tuning")]
        [SerializeField] private float thumbstickDeadzone = 0.15f;
        [SerializeField] private float triggerThreshold   = 0.1f;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>True when XR controllers are connected and providing input.</summary>
        public bool IsXRInputActive { get; private set; }

        /// <summary>Fires with the button name whenever an XR button is pressed.</summary>
        public event Action<string> OnXRButtonPressed;

        // ── Private state ─────────────────────────────────────────────────────────
        private bool _prevLeftGrip;
        private bool _prevRightGrip;
        private bool _prevMenuLeft;
        private bool _prevMenuRight;
        private bool _prevRightTriggerPressed;
        private bool _prevLeftTriggerPressed;

        private SWEF.Screenshot.ScreenshotController _screenshotController;
        private SWEF.UI.HudBinder                    _hudBinder;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
#if !UNITY_XR_MANAGEMENT
            Debug.LogWarning("[SWEF] XRInputAdapter: XR Management package not present — disabling XR input adapter.");
            enabled = false;
            return;
#endif

            if (flightController == null)
                flightController = FindFirstObjectByType<FlightController>();

            if (altitudeController == null)
                altitudeController = FindFirstObjectByType<AltitudeController>();

            _screenshotController = FindFirstObjectByType<SWEF.Screenshot.ScreenshotController>();
            _hudBinder            = FindFirstObjectByType<SWEF.UI.HudBinder>();
        }

        private void Update()
        {
#if UNITY_XR_MANAGEMENT
            PollControllers();
#endif
        }

        // ── Private helpers ───────────────────────────────────────────────────────

#if UNITY_XR_MANAGEMENT
        private void PollControllers()
        {
            // Discover left and right hand devices
            var leftDevices  = new List<InputDevice>();
            var rightDevices = new List<InputDevice>();

            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
                leftDevices);
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
                rightDevices);

            IsXRInputActive = leftDevices.Count > 0 || rightDevices.Count > 0;
            if (!IsXRInputActive) return;

            InputDevice left  = leftDevices.Count  > 0 ? leftDevices[0]  : default;
            InputDevice right = rightDevices.Count > 0 ? rightDevices[0] : default;

            // ── Thumbsticks → flight axes ──────────────────────────────────────
            if (flightController != null)
            {
                Vector2 leftStick  = Vector2.zero;
                Vector2 rightStick = Vector2.zero;

                left.TryGetFeatureValue(CommonUsages.primary2DAxis,  out leftStick);
                right.TryGetFeatureValue(CommonUsages.primary2DAxis, out rightStick);

                float throttle = ApplyDeadzone(leftStick.y);
                float yaw      = ApplyDeadzone(leftStick.x);
                float roll     = ApplyDeadzone(rightStick.x);
                float pitch    = ApplyDeadzone(rightStick.y);

                flightController.SetInputFromXR(throttle, yaw, pitch, roll);
            }

            // ── Triggers → altitude ────────────────────────────────────────────
            float rightTrigger = 0f;
            float leftTrigger  = 0f;
            right.TryGetFeatureValue(CommonUsages.trigger, out rightTrigger);
            left.TryGetFeatureValue(CommonUsages.trigger,  out leftTrigger);

            bool rightTriggerPressed = rightTrigger > triggerThreshold;
            bool leftTriggerPressed  = leftTrigger  > triggerThreshold;

            if (altitudeController != null)
            {
                if (rightTriggerPressed)
                    altitudeController.SetTargetAltitude(
                        altitudeController.CurrentAltitudeMeters + 100f * Time.deltaTime);
                if (leftTriggerPressed)
                    altitudeController.SetTargetAltitude(
                        altitudeController.CurrentAltitudeMeters - 100f * Time.deltaTime);
            }

            // ── Grip buttons → comfort mode / screenshot ───────────────────────
            bool leftGrip  = false;
            bool rightGrip = false;
            left.TryGetFeatureValue(CommonUsages.gripButton,  out leftGrip);
            right.TryGetFeatureValue(CommonUsages.gripButton, out rightGrip);

            if (leftGrip && !_prevLeftGrip)
            {
                OnXRButtonPressed?.Invoke("LeftGrip");
                // Toggle comfort mode via FlightController
                if (flightController != null)
                    flightController.comfortMode = !flightController.comfortMode;
            }

            if (rightGrip && !_prevRightGrip)
            {
                OnXRButtonPressed?.Invoke("RightGrip");
                // Screenshot
                _screenshotController?.CaptureScreenshot();
            }

            _prevLeftGrip  = leftGrip;
            _prevRightGrip = rightGrip;

            // ── Menu buttons → pause / HUD ─────────────────────────────────────
            bool menuLeft  = false;
            bool menuRight = false;
            left.TryGetFeatureValue(CommonUsages.menuButton,     out menuLeft);
            right.TryGetFeatureValue(CommonUsages.secondaryButton, out menuRight);

            if (menuLeft && !_prevMenuLeft)
            {
                OnXRButtonPressed?.Invoke("MenuLeft");
                SWEF.Core.PauseManager.Instance?.TogglePause();
            }

            if (menuRight && !_prevMenuRight)
            {
                OnXRButtonPressed?.Invoke("MenuRight");
                // Toggle HUD visibility via HudBinder if available
                if (_hudBinder != null)
                    _hudBinder.gameObject.SetActive(!_hudBinder.gameObject.activeSelf);
            }

            _prevMenuLeft  = menuLeft;
            _prevMenuRight = menuRight;
            _prevRightTriggerPressed = rightTriggerPressed;
            _prevLeftTriggerPressed  = leftTriggerPressed;
        }

        private float ApplyDeadzone(float value)
        {
            return Mathf.Abs(value) < thumbstickDeadzone ? 0f : value;
        }
#endif
    }
}
