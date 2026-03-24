// Phase 72 — Autopilot & Cruise Control System
// Assets/SWEF/Scripts/Autopilot/AutopilotInputHandler.cs
using UnityEngine;
using SWEF.Accessibility;

namespace SWEF.Autopilot
{
    /// <summary>
    /// Handles keyboard shortcuts and touch gestures for autopilot controls.
    /// Integrates with <see cref="AdaptiveInputManager"/> where available for
    /// remappable key bindings.
    /// </summary>
    [DisallowMultipleComponent]
    public class AutopilotInputHandler : MonoBehaviour
    {
        #region Inspector
        [Header("Default Key Bindings")]
        [SerializeField] private KeyCode keyAltHold    = KeyCode.Z;
        [SerializeField] private KeyCode keyHdgHold    = KeyCode.X;
        [SerializeField] private KeyCode keySpdHold    = KeyCode.C;
        [SerializeField] private KeyCode keyFullAP     = KeyCode.V;
        [SerializeField] private KeyCode keyApproach   = KeyCode.B;
        [SerializeField] private KeyCode keySpeedUp    = KeyCode.Equals;    // '+'
        [SerializeField] private KeyCode keySpeedDown  = KeyCode.Minus;     // '-'

        [Header("Touch Settings")]
        [Tooltip("Maximum seconds between two taps to count as a double-tap.")]
        [SerializeField] private float doubleTapWindow = 0.35f;
        #endregion

        #region Private — double-tap tracking
        private float _lastTapAltTime  = -1f;
        private float _lastTapHdgTime  = -1f;
        private float _lastTapSpdTime  = -1f;
        #endregion

        #region Lifecycle
        private void Update()
        {
            ProcessKeyboard();
            ProcessManualOverride();
        }
        #endregion

        #region Keyboard
        private void ProcessKeyboard()
        {
            if (Input.GetKeyDown(keyAltHold))   ToggleMode(AutopilotMode.AltitudeHold);
            if (Input.GetKeyDown(keyHdgHold))   ToggleMode(AutopilotMode.HeadingHold);
            if (Input.GetKeyDown(keySpdHold))   ToggleMode(AutopilotMode.SpeedHold);
            if (Input.GetKeyDown(keyFullAP))    ToggleMode(AutopilotMode.FullAutopilot);
            if (Input.GetKeyDown(keyApproach))  StartApproachToNearest();

            if (Input.GetKeyDown(keySpeedUp))   CruiseControlManager.Instance?.IncrementSpeed();
            if (Input.GetKeyDown(keySpeedDown)) CruiseControlManager.Instance?.DecrementSpeed();
        }
        #endregion

        #region Touch Gestures (double-tap on UI zones)
        /// <summary>Call from a UI button's onClick to register a double-tap on the altitude display.</summary>
        public void OnAltitudeTapped()
        {
            if (IsDoubleTap(ref _lastTapAltTime))
                ToggleMode(AutopilotMode.AltitudeHold);
        }

        /// <summary>Call from a UI button's onClick to register a double-tap on the compass.</summary>
        public void OnCompassTapped()
        {
            if (IsDoubleTap(ref _lastTapHdgTime))
                ToggleMode(AutopilotMode.HeadingHold);
        }

        /// <summary>Call from a UI button's onClick to register a double-tap on the speed indicator.</summary>
        public void OnSpeedTapped()
        {
            if (IsDoubleTap(ref _lastTapSpdTime))
                ToggleMode(AutopilotMode.SpeedHold);
        }

        private bool IsDoubleTap(ref float lastTapTime)
        {
            float now = Time.unscaledTime;
            bool isDouble = (now - lastTapTime) <= doubleTapWindow;
            lastTapTime = isDouble ? -1f : now;
            return isDouble;
        }
        #endregion

        #region Manual Override Detection
        private void ProcessManualOverride()
        {
            AutopilotController ap = AutopilotController.Instance;
            if (ap == null || !ap.IsEngaged) return;

            // Detect player input on pitch/yaw/roll axes
            float pitch = Input.GetAxis("Vertical");
            float yaw   = Input.GetAxis("Horizontal");

            if (Mathf.Abs(pitch) > 0.1f || Mathf.Abs(yaw) > 0.1f)
            {
                AutopilotAnalytics.Instance?.TrackOverride("pitch_yaw");
                ap.Disengage();
            }
        }
        #endregion

        #region Helpers
        private static void ToggleMode(AutopilotMode mode)
        {
            AutopilotController ap = AutopilotController.Instance;
            if (ap == null) return;

            if (ap.IsEngaged && ap.CurrentMode == mode)
                ap.Disengage();
            else
                ap.Engage(mode);
        }

        private static void StartApproachToNearest()
        {
            AutopilotController ap = AutopilotController.Instance;
            if (ap == null) return;

            var registry = Landing.AirportRegistry.Instance;
            if (registry == null) return;

            var nearest = registry.GetNearestAirport(ap.transform.position);
            if (nearest != null) ap.StartApproach(nearest);
        }
        #endregion
    }
}
