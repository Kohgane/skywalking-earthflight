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
    /// Singleton MonoBehaviour that manages the XR camera rig and switches
    /// between mobile and VR modes. Keeps both rigs synced in position
    /// relative to CesiumGeoreference.
    /// </summary>
    public class XRRigManager : MonoBehaviour
    {
        /// <summary>Active rendering/input rig mode.</summary>
        public enum RigMode { Mobile, XR }

        // ── Singleton ─────────────────────────────────────────────────────────────
        public static XRRigManager Instance { get; private set; }

        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Rigs")]
        [SerializeField] private GameObject mobileRig;
        [SerializeField] private GameObject xrRig;
        [SerializeField] private Camera     xrCamera;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Currently active rig mode.</summary>
        public RigMode CurrentMode { get; private set; } = RigMode.Mobile;

        /// <summary>Fired whenever the rig mode changes.</summary>
        public event Action<RigMode> OnRigModeChanged;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Auto-find mobile rig via FlightController if not assigned
            if (mobileRig == null)
            {
                var fc = FindFirstObjectByType<FlightController>();
                if (fc != null)
                    mobileRig = fc.gameObject;
            }

            // Auto-switch to XR if a headset is already connected
            if (XRPlatformDetector.IsXRActive)
            {
                Debug.Log($"[SWEF] XRRigManager: XR device detected on Awake — switching to XR mode.");
                SetRigMode(RigMode.XR);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Switches the active rig mode, enables/disables the appropriate rig,
        /// and transfers position/rotation from the previously active rig.
        /// </summary>
        public void SetRigMode(RigMode mode)
        {
            if (CurrentMode == mode) return;

            Vector3    pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;

            // Capture transform of the currently active rig
            GameObject activeRig = CurrentMode == RigMode.Mobile ? mobileRig : xrRig;
            if (activeRig != null)
            {
                pos = activeRig.transform.position;
                rot = activeRig.transform.rotation;
            }

            CurrentMode = mode;

            bool toXR = mode == RigMode.XR;

            // Enable/disable rigs
            if (mobileRig != null) mobileRig.SetActive(!toXR);
            if (xrRig     != null)
            {
                xrRig.SetActive(toXR);
                if (toXR)
                {
                    xrRig.transform.position = pos;
                    xrRig.transform.rotation = rot;
                }
            }

            // Fallback: if XR rig is missing, stay on mobile
            if (toXR && xrRig == null)
            {
                Debug.LogWarning("[SWEF] XRRigManager: XR mode requested but xrRig is not assigned — falling back to Mobile.");
                CurrentMode = RigMode.Mobile;
                if (mobileRig != null) mobileRig.SetActive(true);
                return;
            }

            Debug.Log($"[SWEF] XRRigManager: Rig mode changed to {CurrentMode}");
            OnRigModeChanged?.Invoke(CurrentMode);
        }

        /// <summary>
        /// Recenters the XR headset view by calling
        /// <c>XRInputSubsystem.TryRecenter()</c> on all active subsystems.
        /// No-op when the XR Management package is absent.
        /// </summary>
        public void RecenterXR()
        {
#if UNITY_XR_MANAGEMENT
            var subsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            foreach (var subsystem in subsystems)
            {
                if (subsystem.running)
                    subsystem.TryRecenter();
            }
            Debug.Log("[SWEF] XRRigManager: Recenter requested.");
#else
            Debug.LogWarning("[SWEF] XRRigManager: RecenterXR called but XR Management is not available.");
#endif
        }
    }
}
