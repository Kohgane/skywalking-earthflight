// Phase 98 — PC Input & Controls Polish
// Assets/SWEF/Scripts/PCInput/MouseFlightAssist.cs
using System;
using UnityEngine;

namespace SWEF.PCInput
{
    /// <summary>
    /// Mouse-based flight assistance for smoother PC flight.
    /// Supports a configurable dead zone, progressive sensitivity curve,
    /// and optional mouse-follow mode (aircraft steers toward cursor).
    /// </summary>
    [DisallowMultipleComponent]
    public class MouseFlightAssist : MonoBehaviour
    {
        #region Singleton
        /// <summary>Shared mouse flight assist instance.</summary>
        public static MouseFlightAssist Instance { get; private set; }

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
        [Header("Dead Zone")]
        [Tooltip("Radius (0–0.5) of screen-center dead zone where no input is generated.")]
        [SerializeField, Range(0f, 0.45f)] private float deadZoneRadius = 0.05f;

        [Header("Sensitivity Curve")]
        [Tooltip("Maximum input magnitude applied when cursor is at the screen edge.")]
        [SerializeField, Range(0.1f, 3f)] private float maxInputMagnitude = 1f;

        [Tooltip("Exponent of the progressive sensitivity curve (1 = linear).")]
        [SerializeField, Range(1f, 4f)] private float sensitivityExponent = 2f;

        [Header("Mouse-Follow Mode")]
        [Tooltip("When enabled the aircraft gently steers toward the cursor position.")]
        [SerializeField] private bool mouseFollowMode = false;

        [Tooltip("Follow strength when mouse-follow mode is active.")]
        [SerializeField, Range(0.1f, 2f)] private float followStrength = 0.5f;

        [Header("Visual Indicator")]
        [Tooltip("Optional cursor indicator transform (UI Image) showing flight direction.")]
        [SerializeField] private RectTransform cursorIndicator;
        #endregion

        #region Events
        /// <summary>Fired when assist mode is toggled. Argument is the new state.</summary>
        public event Action<bool> OnAssistModeChanged;
        #endregion

        #region Public State
        /// <summary>Whether mouse-follow assist mode is active.</summary>
        public bool IsAssistMode => mouseFollowMode;

        /// <summary>Current computed pitch assist input [-1, 1].</summary>
        public float PitchAssist { get; private set; }

        /// <summary>Current computed yaw assist input [-1, 1].</summary>
        public float YawAssist { get; private set; }
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
#if !UNITY_ANDROID && !UNITY_IOS
            ComputeAssistInputs();
            UpdateCursorIndicator();
#endif
        }
        #endregion

        #region Input Computation
#if !UNITY_ANDROID && !UNITY_IOS
        private void ComputeAssistInputs()
        {
            // Normalised cursor position: (0,0) = screen centre, range [-0.5, 0.5]
            Vector2 screenCentre = new Vector2(0.5f, 0.5f);
            Vector2 normPos = new Vector2(
                Input.mousePosition.x / Screen.width,
                Input.mousePosition.y / Screen.height) - screenCentre;

            float distance = normPos.magnitude; // 0 at centre, ~0.7 at corner

            if (distance <= deadZoneRadius)
            {
                PitchAssist = 0f;
                YawAssist   = 0f;
                return;
            }

            // Remap: 0 at edge of dead zone → 1 at screen edge (0.5f distance)
            float remapped = Mathf.Clamp01((distance - deadZoneRadius) / (0.5f - deadZoneRadius));

            // Progressive curve
            float curve = Mathf.Pow(remapped, sensitivityExponent);

            Vector2 direction = normPos.normalized;
            float strength = curve * maxInputMagnitude * (mouseFollowMode ? followStrength : 1f);

            YawAssist   =  direction.x * strength;
            PitchAssist = -direction.y * strength;
        }

        private void UpdateCursorIndicator()
        {
            if (cursorIndicator == null) return;
            // Place indicator at cursor position in screen space
            cursorIndicator.position = Input.mousePosition;

            // Rotate indicator to show flight direction
            float angle = Mathf.Atan2(YawAssist, -PitchAssist) * Mathf.Rad2Deg;
            cursorIndicator.localRotation = Quaternion.Euler(0f, 0f, angle);

            // Scale based on magnitude
            float mag = new Vector2(YawAssist, PitchAssist).magnitude;
            float scale = Mathf.Lerp(0.5f, 1.5f, mag / maxInputMagnitude);
            cursorIndicator.localScale = Vector3.one * scale;
        }
#endif
        #endregion

        #region Public API
        /// <summary>Toggle between direct control and assist mode.</summary>
        public void ToggleAssistMode()
        {
            mouseFollowMode = !mouseFollowMode;
            OnAssistModeChanged?.Invoke(mouseFollowMode);
        }

        /// <summary>Explicitly set mouse-follow mode.</summary>
        /// <param name="enabled">Whether to enable assist mode.</param>
        public void SetAssistMode(bool enabled)
        {
            if (mouseFollowMode == enabled) return;
            mouseFollowMode = enabled;
            OnAssistModeChanged?.Invoke(mouseFollowMode);
        }

        /// <summary>Set the dead zone radius at runtime.</summary>
        /// <param name="radius">Dead zone radius in normalised screen units [0, 0.45].</param>
        public void SetDeadZone(float radius)
        {
            deadZoneRadius = Mathf.Clamp(radius, 0f, 0.45f);
        }
        #endregion
    }
}
