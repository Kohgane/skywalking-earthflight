using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.InputSystem
{
    /// <summary>
    /// Phase 57 — Manages virtual on-screen joystick and touch-gesture processing
    /// for mobile and tablet builds.
    /// <para>
    /// Implements a fixed/floating virtual joystick and tap-gesture recognition
    /// driven by the <see cref="TouchControlLayout"/> from the active
    /// <see cref="InputSystemProfile"/>.  Exposes the same axis names as
    /// <see cref="GamepadInputHandler"/> so flight controllers can stay device-agnostic.
    /// </para>
    /// </summary>
    public class TouchInputHandler : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static TouchInputHandler Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Layout")]
        [Tooltip("Touch control layout. Synced from InputSystemProfile at startup.")]
        [SerializeField] private TouchControlLayout layout = TouchControlLayout.Default;

        [Header("Joystick Behaviour")]
        [Tooltip("When true the joystick thumb returns to centre on release.")]
        [SerializeField] private bool autoCenter = true;

        [Tooltip("When true the joystick base drifts towards the first-touch position (floating joystick).")]
        [SerializeField] private bool floatingBase = false;

        #endregion

        #region Events

        /// <summary>Fired when a touch-gesture tap is detected.  Carries the screen position.</summary>
        public event Action<Vector2> OnTap;

        /// <summary>Fired when a swipe gesture is detected.  Carries the swipe delta vector.</summary>
        public event Action<Vector2> OnSwipe;

        /// <summary>Fired when a pinch gesture begins.  Carries the initial finger distance.</summary>
        public event Action<float> OnPinchStart;

        /// <summary>
        /// Fired each frame while a pinch gesture is active.
        /// Carries the normalised scale delta (positive = spreading, negative = pinching).
        /// </summary>
        public event Action<float> OnPinchDelta;

        #endregion

        #region Public Properties

        /// <summary>Horizontal joystick axis value [-1, 1].</summary>
        public float JoystickX { get; private set; }

        /// <summary>Vertical joystick axis value [-1, 1].</summary>
        public float JoystickY { get; private set; }

        /// <summary>
        /// <c>true</c> while the virtual joystick thumb is being held by the player.
        /// </summary>
        public bool JoystickActive { get; private set; }

        /// <summary>
        /// <c>true</c> when a two-finger pinch gesture is currently in progress.
        /// </summary>
        public bool PinchActive { get; private set; }

        /// <summary>Active touch control layout (normalised screen coordinates).</summary>
        public TouchControlLayout Layout => layout;

        #endregion

        #region Private State

        // Joystick tracking
        private int     _joystickFingerId = -1;
        private Vector2 _joystickBaseScreenPos;
        private Vector2 _joystickBaseOrigin;
        private float   _joystickRadiusPx;

        // Swipe tracking
        private int     _swipeFingerId   = -1;
        private Vector2 _swipeStartPos;
        private const float SwipeMinDistance = 40f; // pixels

        // Pinch tracking
        private float _prevPinchDistance;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Sync layout from the active profile.
            if (InputBindingManager.Instance != null &&
                InputBindingManager.Instance.Profile != null)
            {
                layout = InputBindingManager.Instance.Profile.touchLayout;
            }

            RecalculateJoystickMetrics();
        }

        private void OnRectTransformDimensionsChange()
        {
            RecalculateJoystickMetrics();
        }

        private void Update()
        {
            ProcessTouches();
        }

        #endregion

        #region Public API

        /// <summary>Applies a new <see cref="TouchControlLayout"/> at runtime.</summary>
        public void ApplyLayout(TouchControlLayout newLayout)
        {
            layout = newLayout;
            RecalculateJoystickMetrics();
        }

        #endregion

        #region Private — Touch Processing

        private void ProcessTouches()
        {
            if (Input.touchCount == 0)
            {
                if (JoystickActive)
                    ReleaseJoystick();
                return;
            }

            // Two-finger pinch.
            if (Input.touchCount >= 2)
            {
                ProcessPinch();
                return;
            }

            PinchActive = false;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        HandleTouchBegan(touch);
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        HandleTouchMoved(touch);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        HandleTouchEnded(touch);
                        break;
                }
            }
        }

        private void HandleTouchBegan(Touch touch)
        {
            Vector2 pos = touch.position;

            if (_joystickFingerId == -1 && IsInsideJoystickZone(pos))
            {
                _joystickFingerId = touch.fingerId;
                JoystickActive    = true;

                if (floatingBase)
                {
                    _joystickBaseScreenPos = pos;
                    RecalculateJoystickMetrics();
                }
            }
            else if (_swipeFingerId == -1)
            {
                _swipeFingerId = touch.fingerId;
                _swipeStartPos = pos;
            }
        }

        private void HandleTouchMoved(Touch touch)
        {
            if (touch.fingerId == _joystickFingerId)
                UpdateJoystick(touch.position);

            if (touch.fingerId == _swipeFingerId)
            {
                Vector2 delta = touch.position - _swipeStartPos;
                if (delta.magnitude > SwipeMinDistance)
                {
                    OnSwipe?.Invoke(delta * layout.gestureSensitivity);
                    _swipeStartPos = touch.position;
                }
            }
        }

        private void HandleTouchEnded(Touch touch)
        {
            if (touch.fingerId == _joystickFingerId)
                ReleaseJoystick();

            if (touch.fingerId == _swipeFingerId)
            {
                Vector2 delta = touch.position - _swipeStartPos;
                if (delta.magnitude < SwipeMinDistance)
                    OnTap?.Invoke(touch.position);
                _swipeFingerId = -1;
            }
        }

        private void UpdateJoystick(Vector2 screenPos)
        {
            Vector2 delta = screenPos - _joystickBaseScreenPos;
            Vector2 clamped = Vector2.ClampMagnitude(delta, _joystickRadiusPx);
            JoystickX = clamped.x / _joystickRadiusPx;
            JoystickY = clamped.y / _joystickRadiusPx;
        }

        private void ReleaseJoystick()
        {
            _joystickFingerId = -1;
            JoystickActive    = false;
            if (autoCenter)
            {
                JoystickX = 0f;
                JoystickY = 0f;
            }
        }

        private void ProcessPinch()
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            float dist = Vector2.Distance(t0.position, t1.position);

            if (!PinchActive)
            {
                PinchActive        = true;
                _prevPinchDistance = dist;
                OnPinchStart?.Invoke(dist);
            }
            else
            {
                float delta = (dist - _prevPinchDistance) / Screen.width;
                OnPinchDelta?.Invoke(delta * layout.gestureSensitivity);
                _prevPinchDistance = dist;
            }
        }

        private bool IsInsideJoystickZone(Vector2 screenPos)
        {
            return Vector2.Distance(screenPos, _joystickBaseScreenPos) < _joystickRadiusPx * 2f;
        }

        private void RecalculateJoystickMetrics()
        {
            _joystickBaseScreenPos = new Vector2(
                layout.joystickPosition.x * Screen.width,
                layout.joystickPosition.y * Screen.height);
            _joystickRadiusPx = layout.joystickSize * Screen.width * 0.5f;

            if (!floatingBase)
                _joystickBaseOrigin = _joystickBaseScreenPos;
        }

        #endregion
    }
}
