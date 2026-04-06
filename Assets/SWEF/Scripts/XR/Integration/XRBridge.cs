// XRBridge.cs — Phase 112: VR/XR Flight Experience
// Integration bridge connecting VR systems to existing SWEF sub-systems.
// Namespace: SWEF.XR

using System;
using UnityEngine;

#if SWEF_XR_AVAILABLE
// XR-specific integration code compiled only when the XR package is present.
#endif

namespace SWEF.XR
{
    /// <summary>
    /// Integration bridge that connects the VR/XR subsystem to the existing
    /// SWEF Flight, Weather, and Achievement systems.
    /// Listens to <see cref="XRFlightManager"/> events and routes them to the
    /// appropriate SWEF sub-systems at runtime.
    /// </summary>
    public class XRBridge : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Shared singleton instance.</summary>
        public static XRBridge Instance { get; private set; }

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Whether the XR bridge is active and listening for events.</summary>
        public bool IsActive { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when a VR flight session begins.</summary>
        public event Action OnVRSessionStarted;

        /// <summary>Fired when a VR flight session ends.</summary>
        public event Action OnVRSessionEnded;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            SubscribeToXREvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromXREvents();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Activates or deactivates the bridge.</summary>
        public void SetActive(bool active)
        {
            IsActive = active;
            if (active) SubscribeToXREvents();
            else        UnsubscribeFromXREvents();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void SubscribeToXREvents()
        {
            var manager = XRFlightManager.Instance;
            if (manager == null) return;

            manager.OnSessionStateChanged += HandleSessionStateChanged;
            manager.OnXRReady             += HandleXRReady;
            IsActive = true;
        }

        private void UnsubscribeFromXREvents()
        {
            var manager = XRFlightManager.Instance;
            if (manager == null) return;

            manager.OnSessionStateChanged -= HandleSessionStateChanged;
            manager.OnXRReady             -= HandleXRReady;
        }

        private void HandleXRReady()
        {
            Debug.Log("[SWEF] XRBridge: XR session ready — notifying sub-systems.");
            OnVRSessionStarted?.Invoke();

#if SWEF_FLIGHT_AVAILABLE
            // Hook into FlightController for XR input pass-through.
#endif
#if SWEF_WEATHER_AVAILABLE
            // Notify weather system to enable VR volumetric effects.
#endif
#if SWEF_ACHIEVEMENT_AVAILABLE
            // Unlock 'First VR Flight' achievement.
#endif
        }

        private void HandleSessionStateChanged(XRSessionState state)
        {
            Debug.Log($"[SWEF] XRBridge: Session state → {state}.");
            if (state == XRSessionState.Stopped)
                OnVRSessionEnded?.Invoke();
        }
    }
}
