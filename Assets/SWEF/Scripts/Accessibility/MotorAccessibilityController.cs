// MotorAccessibilityController.cs — SWEF Accessibility & Platform Optimization (Phase 93)
using System.Collections;
using UnityEngine;

namespace SWEF.Accessibility
{
    /// <summary>
    /// MonoBehaviour that applies motor-accessibility helpers:
    /// one-handed mode, auto-hover assist, sticky keys, input smoothing,
    /// dwell click, and sequential (switch-access) input.
    ///
    /// <para>Listens to <see cref="AccessibilityManager.OnProfileChanged"/> and
    /// reconfigures itself whenever the active profile changes.</para>
    /// </summary>
    public class MotorAccessibilityController : MonoBehaviour
    {
        // ── Serialised fields ─────────────────────────────────────────────────────
        [Header("Input Smoothing")]
        [SerializeField, Tooltip("Smoothing factor for axis inputs (0 = no smoothing, 1 = max).")]
        [Range(0f, 1f)] private float inputSmoothing = 0.15f;

        [Header("Auto-Hover")]
        [SerializeField, Tooltip("Altitude tolerance before auto-hover corrects (metres).")]
        private float hoverAltitudeTolerance = 5f;

        [Header("Dwell Click")]
        [SerializeField, Tooltip("Seconds to dwell on a UI element before triggering a click.")]
        private float dwellDuration = 1.5f;

        [Header("Sticky Keys")]
        [SerializeField, Tooltip("Modifier key hold duration to activate sticky-key latch.")]
        private float stickyKeyHoldTime = 1f;

        // ── Runtime state ─────────────────────────────────────────────────────────
        private bool  _oneHandedMode;
        private bool  _autoHoverAssist;
        private bool  _sequentialInput;
        private bool  _dwellEnabled;
        private bool  _stickyKeysEnabled;

        private float _throttleSmoothed;
        private float _pitchSmoothed;
        private float _rollSmoothed;

        private Coroutine _dwellCoroutine;
        private GameObject _dwellTarget;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (AccessibilityManager.Instance != null)
            {
                ApplyProfile(AccessibilityManager.Instance.Profile);
                AccessibilityManager.Instance.OnProfileChanged += OnProfileChanged;
            }
        }

        private void OnDestroy()
        {
            if (AccessibilityManager.Instance != null)
                AccessibilityManager.Instance.OnProfileChanged -= OnProfileChanged;
        }

        private void OnProfileChanged()
        {
            if (AccessibilityManager.Instance != null)
                ApplyProfile(AccessibilityManager.Instance.Profile);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Applies motor accessibility settings from <paramref name="profile"/>.</summary>
        public void ApplyProfile(AccessibilityProfile profile)
        {
            _oneHandedMode  = profile.oneHandedMode;
            _autoHoverAssist = profile.autoHoverAssist;

            if (_oneHandedMode)
                Debug.Log("[SWEF] Accessibility: One-handed mode enabled.");

            if (_autoHoverAssist)
                Debug.Log("[SWEF] Accessibility: Auto-hover assist enabled.");
        }

        // ── Input smoothing ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns a smoothed axis value.  Call from flight-control scripts each
        /// <c>Update</c> to reduce tremor / unsteady input impact.
        /// </summary>
        /// <param name="raw">Raw axis value (−1 to 1).</param>
        /// <param name="smoothed">Previous smoothed value (update this ref).</param>
        public float SmoothAxis(float raw, ref float smoothed)
        {
            smoothed = Mathf.Lerp(smoothed, raw, 1f - inputSmoothing);
            return smoothed;
        }

        // ── Dwell click ───────────────────────────────────────────────────────────

        /// <summary>
        /// Begin dwelling on a UI element.  Triggers a click after <see cref="dwellDuration"/> seconds
        /// if focus is not lost.
        /// </summary>
        public void BeginDwell(GameObject target)
        {
            if (!_dwellEnabled || target == null) return;
            if (_dwellTarget == target) return;

            CancelDwell();
            _dwellTarget   = target;
            _dwellCoroutine = StartCoroutine(DwellRoutine(target));
        }

        /// <summary>Cancels any in-progress dwell without triggering a click.</summary>
        public void CancelDwell()
        {
            if (_dwellCoroutine != null) StopCoroutine(_dwellCoroutine);
            _dwellTarget = null;
        }

        private IEnumerator DwellRoutine(GameObject target)
        {
            yield return new WaitForSeconds(dwellDuration);
            if (target != null)
            {
                var button = target.GetComponentInChildren<UnityEngine.UI.Button>();
                button?.onClick?.Invoke();
                Debug.Log($"[SWEF] Accessibility: Dwell click on {target.name}");
            }
            _dwellTarget = null;
        }

        // ── Auto-hover assist ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns a corrective vertical thrust to hold altitude when auto-hover is active.
        /// Designed to be added to the pilot's raw collective input each physics step.
        /// </summary>
        /// <param name="currentAltitude">Aircraft altitude above mean sea level (metres).</param>
        /// <param name="targetAltitude">Desired hold altitude (metres).</param>
        public float GetAutoHoverCorrection(float currentAltitude, float targetAltitude)
        {
            if (!_autoHoverAssist) return 0f;
            float error = targetAltitude - currentAltitude;
            if (Mathf.Abs(error) < hoverAltitudeTolerance) return 0f;
            return Mathf.Clamp(error * 0.01f, -0.3f, 0.3f);
        }
    }
}
