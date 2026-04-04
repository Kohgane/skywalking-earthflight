// TabletTouchZoneManager.cs — SWEF Tablet UI Optimization (Phase 97)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.TabletUI
{
    /// <summary>
    /// Singleton MonoBehaviour that defines and enforces touch zones optimised for
    /// tablet ergonomics.
    ///
    /// <para>Features:</para>
    /// <list type="bullet">
    ///   <item><description>Left and right thumb reach zones sized for a typical two-handed
    ///       tablet grip.</description></item>
    ///   <item><description>Configurable palm-rejection dead zone at the bottom of the
    ///       screen to ignore accidental touches.</description></item>
    ///   <item><description>Multi-finger gesture support: pinch-to-zoom (map) and
    ///       two-finger pan (camera).</description></item>
    ///   <item><description>Minimum tap target size of 48 dp (converted to pixels via
    ///       <c>Screen.dpi</c>).</description></item>
    /// </list>
    /// </summary>
    public class TabletTouchZoneManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static TabletTouchZoneManager Instance { get; private set; }

        // ── Constants ─────────────────────────────────────────────────────────────
        /// <summary>Minimum tap target in density-independent pixels (Android/iOS guideline).</summary>
        public const float MinTapTargetDp = 48f;

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Thumb Reach Zones (0–1 in screen space)")]
        [Tooltip("Normalised width of the left thumb reach zone.")]
        [SerializeField, Range(0f, 0.5f)] private float leftZoneWidthNorm  = 0.30f;

        [Tooltip("Normalised width of the right thumb reach zone.")]
        [SerializeField, Range(0f, 0.5f)] private float rightZoneWidthNorm = 0.30f;

        [Tooltip("Normalised height of each thumb zone (measured from the bottom).")]
        [SerializeField, Range(0f, 1f)]   private float thumbZoneHeightNorm = 0.40f;

        [Header("Palm Dead Zone")]
        [Tooltip("Normalised height of the palm-rejection strip at the bottom of the screen.")]
        [SerializeField, Range(0f, 0.3f)] private float palmDeadZoneHeightNorm = 0.05f;

        [Header("Gesture Sensitivity")]
        [Tooltip("Pixels per second scroll speed multiplier for two-finger pan.")]
        [SerializeField] private float panSensitivity = 1.0f;

        [Tooltip("Pinch zoom sensitivity multiplier.")]
        [SerializeField] private float pinchSensitivity = 1.0f;

        // ── Runtime state ─────────────────────────────────────────────────────────
        private float _prevPinchDistance;
        private bool  _pinchActive;
        private Vector2 _prevPanMidpoint;
        private bool    _panActive;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired each frame while a pinch gesture is active. Parameter: relative scale delta.</summary>
        public event Action<float> OnPinchZoom;

        /// <summary>Fired each frame while a two-finger pan is active. Parameter: delta in screen pixels.</summary>
        public event Action<Vector2> OnTwoFingerPan;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
#if UNITY_IOS || UNITY_ANDROID
            ProcessMultiTouchGestures();
#endif
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the minimum tap target size in screen pixels for the current device.
        /// Falls back to 48px when DPI is not available.
        /// </summary>
        public static float GetMinTapTargetPx()
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : 96f;
            return MinTapTargetDp * (dpi / 160f); // convert dp → px (160 dpi baseline)
        }

        /// <summary>Returns the left thumb-reach zone in screen-pixel coordinates.</summary>
        public Rect GetLeftThumbZone()
        {
            return new Rect(
                0,
                palmDeadZoneHeightNorm * Screen.height,
                leftZoneWidthNorm  * Screen.width,
                thumbZoneHeightNorm * Screen.height);
        }

        /// <summary>Returns the right thumb-reach zone in screen-pixel coordinates.</summary>
        public Rect GetRightThumbZone()
        {
            float zoneWidth = rightZoneWidthNorm * Screen.width;
            return new Rect(
                Screen.width - zoneWidth,
                palmDeadZoneHeightNorm * Screen.height,
                zoneWidth,
                thumbZoneHeightNorm * Screen.height);
        }

        /// <summary>Returns the palm dead zone in screen-pixel coordinates.</summary>
        public Rect GetPalmDeadZone()
        {
            return new Rect(0, 0, Screen.width, palmDeadZoneHeightNorm * Screen.height);
        }

        /// <summary>
        /// Returns true when the screen-space position falls inside the palm dead zone
        /// and should be ignored.
        /// </summary>
        public bool IsInPalmDeadZone(Vector2 screenPos)
        {
            return screenPos.y < palmDeadZoneHeightNorm * Screen.height;
        }

        /// <summary>
        /// Returns true when the screen-space position falls inside the left or right
        /// thumb reach zone.
        /// </summary>
        public bool IsInThumbZone(Vector2 screenPos)
        {
            return GetLeftThumbZone().Contains(screenPos) ||
                   GetRightThumbZone().Contains(screenPos);
        }

        // ── Internal ──────────────────────────────────────────────────────────────
        private void ProcessMultiTouchGestures()
        {
            if (Input.touchCount == 2)
            {
                Touch t0 = Input.GetTouch(0);
                Touch t1 = Input.GetTouch(1);

                // Skip touches in palm dead zone
                if (IsInPalmDeadZone(t0.position) || IsInPalmDeadZone(t1.position))
                {
                    ResetGestureState();
                    return;
                }

                float currentDistance = Vector2.Distance(t0.position, t1.position);
                Vector2 midpoint      = (t0.position + t1.position) * 0.5f;

                bool beginning = t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began;

                if (beginning)
                {
                    _prevPinchDistance = currentDistance;
                    _prevPanMidpoint   = midpoint;
                    _pinchActive       = true;
                    _panActive         = true;
                    return;
                }

                if (_pinchActive && _prevPinchDistance > 0f)
                {
                    float scaleDelta = (currentDistance / _prevPinchDistance - 1f) * pinchSensitivity;
                    if (Mathf.Abs(scaleDelta) > 0.001f)
                        OnPinchZoom?.Invoke(scaleDelta);
                }

                if (_panActive)
                {
                    Vector2 panDelta = (midpoint - _prevPanMidpoint) * panSensitivity;
                    if (panDelta.sqrMagnitude > 0.01f)
                        OnTwoFingerPan?.Invoke(panDelta);
                }

                _prevPinchDistance = currentDistance;
                _prevPanMidpoint   = midpoint;
            }
            else
            {
                ResetGestureState();
            }
        }

        private void ResetGestureState()
        {
            _pinchActive = false;
            _panActive   = false;
        }
    }
}
