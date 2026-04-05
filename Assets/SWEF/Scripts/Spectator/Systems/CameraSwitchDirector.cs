// CameraSwitchDirector.cs — SWEF Phase 107: Live Streaming & Spectator Mode
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SWEF.Spectator
{
    /// <summary>
    /// Phase 107 — Automated camera-switch director for spectator and live-stream sessions.
    ///
    /// <para>When <see cref="IsAutoDirectorActive"/> is <c>true</c> the director
    /// selects the "most interesting" camera angle at configurable intervals.
    /// Commentators may also trigger immediate cuts via <see cref="ManualCut"/>.</para>
    ///
    /// <para>Notable flight events (speed records, near-misses, overtakes, etc.)
    /// may trigger an early cut to keep the broadcast exciting.</para>
    /// </summary>
    public sealed class CameraSwitchDirector : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static CameraSwitchDirector Instance { get; private set; }

        // ── Inspector ──────────────────────────────────────────────────────────
        [SerializeField] private SpectatorConfig config;
        [SerializeField] private SpectatorModeController spectatorController;

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Raised when the director performs a camera cut.
        /// Arguments: new camera mode and the transition effect used.
        /// </summary>
        public event Action<SpectatorCameraMode, CameraTransitionEffect> OnCameraSwitch;

        // ── Public state ───────────────────────────────────────────────────────

        /// <summary>Returns <c>true</c> when the auto-director is running.</summary>
        public bool IsAutoDirectorActive { get; private set; }

        /// <summary>Returns <c>true</c> when manual override is in effect.</summary>
        public bool IsManualOverride { get; private set; }

        // ── Internal state ─────────────────────────────────────────────────────
        private float _nextCutTime;
        private float _manualOverrideEndTime;

        private static readonly SpectatorCameraMode[] _autoModes =
        {
            SpectatorCameraMode.FollowCam,
            SpectatorCameraMode.OrbitCam,
            SpectatorCameraMode.CinematicCam,
        };

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (!IsAutoDirectorActive || config == null) return;

            // Expire manual override
            if (IsManualOverride && Time.time >= _manualOverrideEndTime)
                IsManualOverride = false;

            if (!IsManualOverride && Time.time >= _nextCutTime)
                AutoCut();
        }

        // ── Public API — director lifecycle ────────────────────────────────────

        /// <summary>Starts the auto-director.</summary>
        public void StartAutoDirector()
        {
            IsAutoDirectorActive = true;
            ScheduleNextCut();
            Debug.Log("[CameraSwitchDirector] Auto-director started.");
        }

        /// <summary>Stops the auto-director.</summary>
        public void StopAutoDirector()
        {
            IsAutoDirectorActive = false;
            Debug.Log("[CameraSwitchDirector] Auto-director stopped.");
        }

        // ── Public API — manual override ───────────────────────────────────────

        /// <summary>
        /// Performs an immediate manual cut to <paramref name="mode"/> using
        /// <paramref name="effect"/>, suppressing auto-cuts for
        /// <paramref name="overrideDurationSeconds"/>.
        /// </summary>
        public void ManualCut(SpectatorCameraMode mode, CameraTransitionEffect effect,
                              float overrideDurationSeconds = 10f)
        {
            IsManualOverride    = true;
            _manualOverrideEndTime = Time.time + overrideDurationSeconds;

            ApplyCut(mode, effect);
        }

        // ── Public API — event triggers ────────────────────────────────────────

        /// <summary>
        /// Notifies the director that a notable <paramref name="eventType"/> has
        /// occurred. The director may perform an early unscheduled cut to capture
        /// the action.
        /// </summary>
        public void NotifyFlightEvent(FlightEventType eventType)
        {
            if (!IsAutoDirectorActive || IsManualOverride) return;

            // High-priority events trigger an immediate cut
            switch (eventType)
            {
                case FlightEventType.NearMiss:
                case FlightEventType.SpeedRecord:
                case FlightEventType.Overtake:
                    AutoCut();
                    break;
            }
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private void AutoCut()
        {
            SpectatorCameraMode mode    = SelectMode();
            CameraTransitionEffect fx   = SelectTransition();
            ApplyCut(mode, fx);
            ScheduleNextCut();
        }

        private SpectatorCameraMode SelectMode()
        {
            return _autoModes[Random.Range(0, _autoModes.Length)];
        }

        private CameraTransitionEffect SelectTransition()
        {
            // Weighted selection: Cut 50%, Crossfade 35%, WhipPan 15%
            float r = Random.value;
            if (r < 0.50f) return CameraTransitionEffect.Cut;
            if (r < 0.85f) return CameraTransitionEffect.Crossfade;
            return CameraTransitionEffect.WhipPan;
        }

        private void ApplyCut(SpectatorCameraMode mode, CameraTransitionEffect effect)
        {
            if (spectatorController != null)
                spectatorController.SetCameraMode(mode);

            OnCameraSwitch?.Invoke(mode, effect);
            Debug.Log($"[CameraSwitchDirector] Cut → {mode} with {effect}.");
        }

        private void ScheduleNextCut()
        {
            if (config == null) { _nextCutTime = Time.time + 10f; return; }
            _nextCutTime = Time.time + Random.Range(config.directorMinCutInterval,
                                                    config.directorMaxCutInterval);
        }
    }
}
