// VRRecenterController.cs — Phase 112: VR/XR Flight Experience
// Quick recenter/reset view, seated vs standing mode toggle.
// Namespace: SWEF.XR

using System;
using UnityEngine;

namespace SWEF.XR
{
    /// <summary>Tracking mode: seated (fixed floor) or standing (room-scale).</summary>
    public enum VRTrackingMode
    {
        /// <summary>Seated: tracking origin at head height, no floor offset.</summary>
        Seated,
        /// <summary>Standing/room-scale: tracking origin at floor level.</summary>
        Standing
    }

    /// <summary>
    /// Provides quick head recenter and seated/standing mode toggle for VR.
    /// Also supports a double-tap gesture shortcut to recenter from inside the
    /// cockpit without reaching for a controller menu button.
    /// </summary>
    public class VRRecenterController : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Tracking")]
        [SerializeField] private VRTrackingMode trackingMode = VRTrackingMode.Seated;
        [SerializeField] private float          seatedFloorOffset = 1.2f;

        [Header("Double-Tap Recenter")]
        [SerializeField] private bool  doubleTapRecenterEnabled = true;
        [SerializeField] private float doubleTapWindow         = 0.4f;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Current tracking mode.</summary>
        public VRTrackingMode TrackingMode => trackingMode;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired after a recenter operation completes.</summary>
        public event Action OnRecentered;

        /// <summary>Fired when tracking mode changes.</summary>
        public event Action<VRTrackingMode> OnTrackingModeChanged;

        // ── Private state ─────────────────────────────────────────────────────────
        private float _lastTapTime = -1f;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Recenters the VR view immediately.</summary>
        public void Recenter()
        {
            XRFlightManager.Instance?.RecenterView();
            ApplyFloorOffset();
            OnRecentered?.Invoke();
            Debug.Log("[SWEF] VRRecenterController: View recentered.");
        }

        /// <summary>Toggles between Seated and Standing tracking modes.</summary>
        public void ToggleTrackingMode()
        {
            SetTrackingMode(trackingMode == VRTrackingMode.Seated
                ? VRTrackingMode.Standing
                : VRTrackingMode.Seated);
        }

        /// <summary>Sets a specific tracking mode.</summary>
        public void SetTrackingMode(VRTrackingMode mode)
        {
            trackingMode = mode;
            ApplyFloorOffset();
            OnTrackingModeChanged?.Invoke(mode);
            Debug.Log($"[SWEF] VRRecenterController: Tracking mode set to {mode}.");
        }

        /// <summary>
        /// Call this method when the player activates a recenter tap/button.
        /// Handles double-tap detection for quick recenter.
        /// </summary>
        public void RegisterRecenterTap()
        {
            if (!doubleTapRecenterEnabled)
            {
                Recenter();
                return;
            }

            float now = Time.unscaledTime;
            if (_lastTapTime >= 0f && (now - _lastTapTime) <= doubleTapWindow)
            {
                Recenter();
                _lastTapTime = -1f;
            }
            else
            {
                _lastTapTime = now;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void ApplyFloorOffset()
        {
            float offset = trackingMode == VRTrackingMode.Seated ? seatedFloorOffset : 0f;
            Vector3 pos = transform.localPosition;
            pos.y = offset;
            transform.localPosition = pos;
        }
    }
}
