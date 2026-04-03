// SpeedHackDetector.cs — SWEF Anti-Cheat & Security Hardening (Phase 92)
using System;
using UnityEngine;

namespace SWEF.Security
{
    /// <summary>
    /// MonoBehaviour component that detects time-scale manipulation and
    /// physics-step anomalies indicative of a speed hack.
    ///
    /// <para>Attach to the same GameObject as <see cref="CheatDetectionManager"/>
    /// or to the player aircraft.</para>
    ///
    /// <para>Detection methods:</para>
    /// <list type="bullet">
    ///   <item>Compares <c>Time.unscaledDeltaTime</c> vs <c>Time.deltaTime</c> to detect time-scale manipulation.</item>
    ///   <item>Compares actual aircraft speed against <c>maxSpeedThreshold × toleranceMultiplier</c>.</item>
    ///   <item>Monitors physics fixed-step drift over a rolling window.</item>
    /// </list>
    /// </summary>
    public class SpeedHackDetector : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [SerializeField, Tooltip("Reference to the shared SecurityConfig.")]
        private SecurityConfig _config;

        [SerializeField, Tooltip("Optional: the Rigidbody whose speed is monitored. If null, skipped.")]
        private Rigidbody _monitoredRigidbody;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>
        /// Fired when a speed hack or time-scale manipulation is detected.
        /// The string parameter contains a human-readable description.
        /// </summary>
        public event Action<string> OnViolationDetected;

        // ── Private state ─────────────────────────────────────────────────────

        private SecurityConfig Config => _config ?? SecurityConfig.Default();

        private float _physicsStepAccumulator;
        private int   _physicsFrameCount;
        private const int PhysicsWindowFrames = 60;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Update()
        {
            CheckTimeScaleManipulation();
            CheckAircraftSpeed();
        }

        private void FixedUpdate()
        {
            TrackPhysicsStep();
        }

        // ── Detection logic ───────────────────────────────────────────────────

        /// <summary>
        /// Compares scaled vs unscaled delta to detect time-scale cheating.
        /// </summary>
        private void CheckTimeScaleManipulation()
        {
            float unscaled = Time.unscaledDeltaTime;
            float scaled   = Time.deltaTime;

            if (unscaled <= 0f) return;

            // If the game is intentionally paused Time.timeScale == 0; skip.
            if (Mathf.Approximately(Time.timeScale, 0f)) return;

            float expectedScaled = unscaled * Time.timeScale;
            float delta          = Mathf.Abs(scaled - expectedScaled);
            float threshold      = Config.timeScaleAnomalyThreshold;

            if (delta > threshold)
            {
                string msg = $"Time-scale anomaly: scaled={scaled:F4} " +
                             $"expected={expectedScaled:F4} delta={delta:F4} " +
                             $"timeScale={Time.timeScale:F4}";
                OnViolationDetected?.Invoke(msg);
            }
        }

        /// <summary>
        /// Checks whether the monitored Rigidbody's speed exceeds the configured maximum.
        /// </summary>
        private void CheckAircraftSpeed()
        {
            if (_monitoredRigidbody == null) return;

            float speed     = _monitoredRigidbody.velocity.magnitude;
            float maxAllowed = Config.maxSpeedThreshold * Config.speedToleranceMultiplier;

            if (speed > maxAllowed)
            {
                string msg = $"Speed exceeded threshold: speed={speed:F1} m/s " +
                             $"maxAllowed={maxAllowed:F1} m/s";
                OnViolationDetected?.Invoke(msg);
            }
        }

        /// <summary>
        /// Accumulates physics time steps to detect fixed-update drift.
        /// </summary>
        private void TrackPhysicsStep()
        {
            _physicsStepAccumulator += Time.fixedUnscaledDeltaTime;
            _physicsFrameCount++;

            if (_physicsFrameCount >= PhysicsWindowFrames)
            {
                float avgStep    = _physicsStepAccumulator / PhysicsWindowFrames;
                float expectedStep = Time.fixedDeltaTime;
                float drift      = Mathf.Abs(avgStep - expectedStep);

                if (drift > Config.timeScaleAnomalyThreshold * 2f)
                {
                    string msg = $"Physics step drift detected: avgStep={avgStep:F4} " +
                                 $"expected={expectedStep:F4} drift={drift:F4}";
                    OnViolationDetected?.Invoke(msg);
                }

                _physicsStepAccumulator = 0f;
                _physicsFrameCount      = 0;
            }
        }
    }
}
