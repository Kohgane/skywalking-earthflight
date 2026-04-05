// VRTeleportController.cs — Phase 112: VR/XR Flight Experience
// Teleport locomotion for moving around the cockpit/cabin, snap-to-seat.
// Namespace: SWEF.XR

using System;
using UnityEngine;

namespace SWEF.XR
{
    /// <summary>
    /// Provides arc-based teleport locomotion for VR.
    /// Renders a parabolic arc from the hand to the target position and
    /// snaps to named anchor points (e.g. pilot/co-pilot seats).
    /// </summary>
    public class VRTeleportController : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Arc Settings")]
        [SerializeField] private float arcGravity       = 9.81f;
        [SerializeField] private float arcInitialSpeed  = 8f;
        [SerializeField] private int   arcSegments      = 20;
        [SerializeField] private float maxArcDistance   = 8f;

        [Header("Line Renderer")]
        [SerializeField] private LineRenderer arcLineRenderer;

        [Header("Target Indicator")]
        [SerializeField] private GameObject teleportTargetMarker;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Whether a valid teleport destination is currently targeted.</summary>
        public bool HasValidTarget { get; private set; }

        /// <summary>Current world-space teleport destination.</summary>
        public Vector3 TargetPosition { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when a teleport is executed. Arg: destination position.</summary>
        public event Action<Vector3> OnTeleported;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Updates the teleport arc from the given hand origin and direction.</summary>
        public void AimArc(Vector3 origin, Vector3 direction)
        {
            Vector3[] points = ComputeArcPoints(origin, direction.normalized * arcInitialSpeed);

            HasValidTarget = false;
            TargetPosition = Vector3.zero;

            for (int i = 1; i < points.Length; i++)
            {
                Vector3 segStart = points[i - 1];
                Vector3 segEnd   = points[i];
                if (Physics.Linecast(segStart, segEnd, out RaycastHit hit))
                {
                    TargetPosition = hit.point;
                    HasValidTarget = true;
                    TrimArc(points, i, hit.point);
                    break;
                }
            }

            UpdateArcVisual(points);
            UpdateTargetMarker();
        }

        /// <summary>Hides the teleport arc without teleporting.</summary>
        public void HideArc()
        {
            HasValidTarget = false;
            if (arcLineRenderer != null) arcLineRenderer.enabled = false;
            if (teleportTargetMarker != null) teleportTargetMarker.SetActive(false);
        }

        /// <summary>Executes teleport to the current target position if valid.</summary>
        public void ExecuteTeleport()
        {
            if (!HasValidTarget) return;
            transform.position = TargetPosition;
            HideArc();
            OnTeleported?.Invoke(TargetPosition);
            Debug.Log($"[SWEF] VRTeleportController: Teleported to {TargetPosition}.");
        }

        /// <summary>Snap directly to a named seat / anchor position.</summary>
        public void SnapToSeat(Transform seatAnchor)
        {
            if (seatAnchor == null) return;
            transform.position = seatAnchor.position;
            transform.rotation = seatAnchor.rotation;
            OnTeleported?.Invoke(seatAnchor.position);
            Debug.Log($"[SWEF] VRTeleportController: Snapped to seat '{seatAnchor.name}'.");
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private Vector3[] ComputeArcPoints(Vector3 origin, Vector3 velocity)
        {
            var points = new Vector3[arcSegments + 1];
            float dt = 0.1f;
            Vector3 pos = origin;
            Vector3 vel = velocity;
            points[0] = pos;
            for (int i = 1; i <= arcSegments; i++)
            {
                vel += Vector3.down * arcGravity * dt;
                pos += vel * dt;
                points[i] = pos;
                if (Vector3.Distance(origin, pos) > maxArcDistance) break;
            }
            return points;
        }

        private static void TrimArc(Vector3[] points, int hitIndex, Vector3 hitPoint)
        {
            points[hitIndex] = hitPoint;
            for (int i = hitIndex + 1; i < points.Length; i++)
                points[i] = hitPoint;
        }

        private void UpdateArcVisual(Vector3[] points)
        {
            if (arcLineRenderer == null) return;
            arcLineRenderer.enabled = true;
            arcLineRenderer.positionCount = points.Length;
            arcLineRenderer.SetPositions(points);
        }

        private void UpdateTargetMarker()
        {
            if (teleportTargetMarker == null) return;
            teleportTargetMarker.SetActive(HasValidTarget);
            if (HasValidTarget)
                teleportTargetMarker.transform.position = TargetPosition;
        }
    }
}
