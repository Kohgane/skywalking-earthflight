// VRCockpitController.cs — Phase 112: VR/XR Flight Experience
// Virtual cockpit environment with interactive controls.
// Namespace: SWEF.XR

using System;
using UnityEngine;

namespace SWEF.XR
{
    /// <summary>
    /// Manages the virtual cockpit environment in VR, including interactive
    /// controls (throttle, yoke/stick, switches) and cockpit state.
    /// Requires a <see cref="VRCockpitLayout"/> for instrument positioning.
    /// </summary>
    public class VRCockpitController : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("References")]
        [SerializeField] private VRCockpitLayout  layout;
        [SerializeField] private VRInstrumentPanel instrumentPanel;

        [Header("Control Transforms")]
        [SerializeField] private Transform throttleHandle;
        [SerializeField] private Transform yokeHandle;
        [SerializeField] private Transform rudderPedals;

        [Header("Control Ranges")]
        [SerializeField] private float throttleMinY = -0.1f;
        [SerializeField] private float throttleMaxY =  0.1f;
        [SerializeField] private float yokePitchAngle = 30f;
        [SerializeField] private float yokeRollAngle  = 45f;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Current throttle position [0..1].</summary>
        public float ThrottleNormalized { get; private set; }

        /// <summary>Current yoke pitch input [-1..1].</summary>
        public float YokePitch { get; private set; }

        /// <summary>Current yoke roll input [-1..1].</summary>
        public float YokeRoll { get; private set; }

        /// <summary>Whether the cockpit is currently active (entered by player).</summary>
        public bool IsOccupied { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when the player enters the cockpit.</summary>
        public event Action OnCockpitEntered;

        /// <summary>Fired when the player exits the cockpit.</summary>
        public event Action OnCockpitExited;

        /// <summary>Fired when a cockpit control value changes. Args: control name, value.</summary>
        public event Action<string, float> OnControlChanged;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (layout == null)
                layout = GetComponent<VRCockpitLayout>();
            if (instrumentPanel == null)
                instrumentPanel = GetComponentInChildren<VRInstrumentPanel>();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Places the player into the cockpit seat.</summary>
        public void EnterCockpit()
        {
            if (IsOccupied) return;
            IsOccupied = true;
            OnCockpitEntered?.Invoke();
            Debug.Log("[SWEF] VRCockpitController: Cockpit entered.");
        }

        /// <summary>Removes the player from the cockpit seat.</summary>
        public void ExitCockpit()
        {
            if (!IsOccupied) return;
            IsOccupied = false;
            OnCockpitExited?.Invoke();
            Debug.Log("[SWEF] VRCockpitController: Cockpit exited.");
        }

        /// <summary>
        /// Sets throttle position from a world-space grab position.
        /// Clamps and normalises to [0..1].
        /// </summary>
        public void SetThrottleFromGrabPosition(Vector3 worldPos)
        {
            if (throttleHandle == null) return;
            float localY = transform.InverseTransformPoint(worldPos).y;
            float t = Mathf.InverseLerp(throttleMinY, throttleMaxY, localY);
            SetThrottle(t);
        }

        /// <summary>Sets throttle to a normalised value [0..1].</summary>
        public void SetThrottle(float normalized)
        {
            float clamped = Mathf.Clamp01(normalized);
            if (Mathf.Approximately(ThrottleNormalized, clamped)) return;
            ThrottleNormalized = clamped;
            if (throttleHandle != null)
            {
                float localY = Mathf.Lerp(throttleMinY, throttleMaxY, clamped);
                Vector3 lp = throttleHandle.localPosition;
                throttleHandle.localPosition = new Vector3(lp.x, localY, lp.z);
            }
            OnControlChanged?.Invoke("Throttle", clamped);
        }

        /// <summary>Sets yoke pitch/roll from normalised input.</summary>
        public void SetYokeInput(float pitch, float roll)
        {
            YokePitch = Mathf.Clamp(pitch, -1f, 1f);
            YokeRoll  = Mathf.Clamp(roll,  -1f, 1f);
            if (yokeHandle != null)
            {
                yokeHandle.localRotation = Quaternion.Euler(
                    YokePitch * yokePitchAngle,
                    0f,
                    -YokeRoll * yokeRollAngle);
            }
            OnControlChanged?.Invoke("YokePitch", YokePitch);
            OnControlChanged?.Invoke("YokeRoll",  YokeRoll);
        }
    }
}
