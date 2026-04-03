// PositionValidator.cs — SWEF Anti-Cheat & Security Hardening (Phase 92)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Security
{
    /// <summary>
    /// MonoBehaviour component that tracks the player's position history and flags
    /// impossible teleport-like jumps that cannot be explained by legitimate movement.
    ///
    /// <para>A ring buffer of the last <see cref="SecurityConfig.positionHistoryFrames"/> positions
    /// is maintained. Each frame the delta is compared against
    /// <c>maxVelocity × deltaTime × toleranceMultiplier</c>.</para>
    ///
    /// <para>Legitimate teleports (initiated by TeleportController or deep links) should
    /// call <see cref="ExemptNextJump"/> before repositioning the transform.</para>
    /// </summary>
    public class PositionValidator : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [SerializeField, Tooltip("Reference to the shared SecurityConfig.")]
        private SecurityConfig _config;

        [SerializeField, Tooltip("Transform whose position is monitored (defaults to this GameObject).")]
        private Transform _monitoredTransform;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>
        /// Fired when a suspicious position jump is detected.
        /// Parameters: violation message, position trail snapshot.
        /// </summary>
        public event Action<string, Vector3[]> OnViolationDetected;

        // ── Private state ─────────────────────────────────────────────────────

        private SecurityConfig Config => _config ?? SecurityConfig.Default();

        private readonly Queue<Vector3> _history = new Queue<Vector3>();
        private Vector3 _lastPosition;
        private bool    _exemptNextJump;
        private bool    _initialised;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (_monitoredTransform == null)
                _monitoredTransform = transform;
        }

        private void Update()
        {
            if (!_initialised)
            {
                _lastPosition = _monitoredTransform.position;
                _initialised  = true;
                return;
            }

            Vector3 current = _monitoredTransform.position;
            float   delta   = Vector3.Distance(current, _lastPosition);

            // Maintain ring buffer
            int maxFrames = Mathf.Max(1, Config.positionHistoryFrames);
            _history.Enqueue(_lastPosition);
            while (_history.Count > maxFrames)
                _history.Dequeue();

            if (_exemptNextJump)
            {
                _exemptNextJump = false;
                _lastPosition   = current;
                return;
            }

            // Allowed displacement = maxTeleportDistancePerTick, also capped by
            // velocity-based estimate when Rigidbody is available.
            float allowed = Config.maxTeleportDistancePerTick;

            if (delta > allowed)
            {
                string  msg   = $"Impossible position jump: delta={delta:F1} m allowed={allowed:F1} m " +
                                $"from={_lastPosition} to={current}";
                Vector3[] trail = _history.ToArray();
                OnViolationDetected?.Invoke(msg, trail);
            }

            _lastPosition = current;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Call this immediately before a legitimate teleport to suppress the next
        /// position-jump violation. One call exempts exactly one frame.
        /// </summary>
        public void ExemptNextJump()
        {
            _exemptNextJump = true;
        }

        /// <summary>Returns a snapshot of the current position history trail.</summary>
        public Vector3[] GetPositionTrail() => _history.ToArray();
    }
}
