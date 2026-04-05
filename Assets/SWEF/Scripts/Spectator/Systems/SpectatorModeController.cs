// SpectatorModeController.cs — SWEF Phase 107: Live Streaming & Spectator Mode
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Spectator
{
    /// <summary>
    /// Phase 107 — Core manager for Spectator Mode.
    ///
    /// <para>Handles enter/exit of spectator mode (detaching from the player
    /// aircraft), maintains the list of observable targets, and drives
    /// camera-mode transitions via <see cref="SpectatorCameraController"/>.</para>
    ///
    /// <para>Subscribe to the public events to react to spectator state changes
    /// from other systems (UI, Analytics, Streaming).</para>
    /// </summary>
    public sealed class SpectatorModeController : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static SpectatorModeController Instance { get; private set; }

        // ── Inspector ──────────────────────────────────────────────────────────
        [SerializeField] private SpectatorConfig config;
        [SerializeField] private SpectatorCameraController cameraController;

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Raised when spectator mode is entered.</summary>
        public event Action OnSpectatorModeEntered;

        /// <summary>Raised when spectator mode is exited and control returns to the player aircraft.</summary>
        public event Action OnSpectatorModeExited;

        /// <summary>Raised when the observed target changes. Argument is the new target transform (may be null).</summary>
        public event Action<Transform> OnTargetChanged;

        /// <summary>Raised when the active camera mode changes.</summary>
        public event Action<SpectatorCameraMode> OnCameraModeChanged;

        // ── Public state ───────────────────────────────────────────────────────

        /// <summary>Returns <c>true</c> when spectator mode is currently active.</summary>
        public bool IsActive { get; private set; }

        /// <summary>Currently observed target aircraft transform. <c>null</c> in FreeCam.</summary>
        public Transform CurrentTarget { get; private set; }

        /// <summary>Currently active camera mode.</summary>
        public SpectatorCameraMode CurrentCameraMode { get; private set; }

        // ── Internal state ─────────────────────────────────────────────────────
        private readonly List<Transform> _targets = new List<Transform>();
        private int _targetIndex = -1;

        // ── Unity lifecycle ────────────────────────────────────────────────────

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

        // ── Public API — spectator mode ────────────────────────────────────────

        /// <summary>
        /// Enters spectator mode, optionally focusing on <paramref name="initialTarget"/>.
        /// If <paramref name="initialTarget"/> is <c>null</c> the camera starts in FreeCam.
        /// </summary>
        public void EnterSpectatorMode(Transform initialTarget = null)
        {
            if (IsActive) return;
            IsActive = true;

            SetTarget(initialTarget);
            SetCameraMode(initialTarget != null ? SpectatorCameraMode.FollowCam : SpectatorCameraMode.FreeCam);

            OnSpectatorModeEntered?.Invoke();
            Debug.Log("[SpectatorModeController] Spectator mode entered.");
        }

        /// <summary>
        /// Exits spectator mode and returns control to the player aircraft.
        /// </summary>
        public void ExitSpectatorMode()
        {
            if (!IsActive) return;
            IsActive = false;

            if (cameraController != null)
                cameraController.Deactivate();

            OnSpectatorModeExited?.Invoke();
            Debug.Log("[SpectatorModeController] Spectator mode exited.");
        }

        // ── Public API — target management ─────────────────────────────────────

        /// <summary>
        /// Registers an aircraft <see cref="Transform"/> as a spectatable target.
        /// </summary>
        public void RegisterTarget(Transform target)
        {
            if (target != null && !_targets.Contains(target))
                _targets.Add(target);
        }

        /// <summary>
        /// Removes a previously registered target. If it is currently selected the
        /// controller switches to the next available target (or FreeCam).
        /// </summary>
        public void UnregisterTarget(Transform target)
        {
            if (!_targets.Remove(target)) return;

            if (CurrentTarget == target)
                SelectNextTarget();
        }

        /// <summary>Returns a read-only snapshot of the current target list.</summary>
        public IReadOnlyList<Transform> GetTargets() => _targets.AsReadOnly();

        /// <summary>
        /// Switches observation to the next target in the list, wrapping around.
        /// Falls back to FreeCam when the list is empty.
        /// </summary>
        public void SelectNextTarget()
        {
            if (_targets.Count == 0)
            {
                SetTarget(null);
                SetCameraMode(SpectatorCameraMode.FreeCam);
                return;
            }
            _targetIndex = (_targetIndex + 1) % _targets.Count;
            SetTarget(_targets[_targetIndex]);
        }

        /// <summary>
        /// Switches observation to the previous target in the list.
        /// Falls back to FreeCam when the list is empty.
        /// </summary>
        public void SelectPreviousTarget()
        {
            if (_targets.Count == 0)
            {
                SetTarget(null);
                SetCameraMode(SpectatorCameraMode.FreeCam);
                return;
            }
            _targetIndex = (_targetIndex - 1 + _targets.Count) % _targets.Count;
            SetTarget(_targets[_targetIndex]);
        }

        /// <summary>
        /// Directly selects a specific <paramref name="target"/> as the observed aircraft.
        /// </summary>
        public void SelectTarget(Transform target)
        {
            int idx = _targets.IndexOf(target);
            if (idx < 0) return;
            _targetIndex = idx;
            SetTarget(target);
        }

        // ── Public API — camera mode ───────────────────────────────────────────

        /// <summary>
        /// Switches to the requested <paramref name="mode"/>.
        /// Automatically switches to FreeCam if a target-dependent mode is requested
        /// while <see cref="CurrentTarget"/> is <c>null</c>.
        /// </summary>
        public void SetCameraMode(SpectatorCameraMode mode)
        {
            if (mode != SpectatorCameraMode.FreeCam && CurrentTarget == null)
                mode = SpectatorCameraMode.FreeCam;

            if (CurrentCameraMode == mode) return;
            CurrentCameraMode = mode;

            if (cameraController != null)
                cameraController.ApplyMode(mode, CurrentTarget);

            OnCameraModeChanged?.Invoke(mode);
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private void SetTarget(Transform target)
        {
            CurrentTarget = target;
            if (target != null)
            {
                int idx = _targets.IndexOf(target);
                if (idx >= 0) _targetIndex = idx;
            }

            if (cameraController != null)
                cameraController.SetTarget(target);

            OnTargetChanged?.Invoke(target);
        }
    }
}
