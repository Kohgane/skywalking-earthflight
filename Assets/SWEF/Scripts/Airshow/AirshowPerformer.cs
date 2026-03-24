// Phase 73 — Flight Formation Display & Airshow System
// Assets/SWEF/Scripts/Airshow/AirshowPerformer.cs
using System;
using UnityEngine;
using SWEF.Autopilot;
using SWEF.Contrail;

namespace SWEF.Airshow
{
    /// <summary>
    /// Attached to each aircraft participating in an airshow.
    /// Handles maneuver execution, AI path following (via PIDController),
    /// local-player HUD guidance, smoke control, and per-maneuver scoring.
    /// </summary>
    public class AirshowPerformer : MonoBehaviour
    {
        #region Inspector
        [Header("Performer Settings")]
        [SerializeField] private int slotIndex;
        [SerializeField] private bool isLocalPlayer;

        [Header("AI Path Following")]
        [Tooltip("PID gains applied to all three axes during AI maneuver following.")]
        [SerializeField] private float pidKp = 1.2f;
        [SerializeField] private float pidKi = 0.05f;
        [SerializeField] private float pidKd = 0.3f;

        [Header("Smoke")]
        [SerializeField] private ContrailEmitter contrailEmitter;
        #endregion

        #region Public State
        /// <summary>Assigned performer slot index (0 = lead).</summary>
        public int SlotIndex => slotIndex;

        /// <summary>True if this performer is controlled by the local player.</summary>
        public bool IsLocalPlayer => isLocalPlayer;

        /// <summary>Currently executing maneuver type.</summary>
        public ManeuverType CurrentManeuver { get; private set; } = ManeuverType.StraightAndLevel;

        /// <summary>Progress through current maneuver, 0–1.</summary>
        public float ManeuverProgress { get; private set; }

        // Per-maneuver scores (updated each frame during execution)
        /// <summary>How close the performer was to the scheduled start time (0–100).</summary>
        public float TimingScore { get; private set; }

        /// <summary>Positional accuracy relative to the target position (0–100).</summary>
        public float PositionScore { get; private set; }

        /// <summary>G-force consistency score (0–100).</summary>
        public float SmoothnessScore { get; private set; }
        #endregion

        #region Events
        /// <summary>Fired when a maneuver execution begins.</summary>
        public event Action<ManeuverType> OnManeuverStarted;

        /// <summary>Fired when a maneuver execution completes.</summary>
        public event Action<ManeuverType> OnManeuverCompleted;

        /// <summary>Fired when the smoke state changes.</summary>
        public event Action<bool> OnSmokeToggled;
        #endregion

        #region Private
        private ManeuverStep _currentStep;
        private float _maneuverStartTime;
        private float _maneuverElapsed;
        private bool _isExecuting;

        // PID controllers for AI path following
        private PIDController _pitchPID;
        private PIDController _rollPID;
        private PIDController _yawPID;

        // G-force history for smoothness scoring
        private readonly float[] _gForceHistory = new float[60];
        private int _gForceIndex;

        private Vector3 _previousVelocity;
        #endregion

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            _pitchPID = new PIDController(pidKp, pidKi, pidKd, -1f, 1f);
            _rollPID  = new PIDController(pidKp, pidKi, pidKd, -1f, 1f);
            _yawPID   = new PIDController(pidKp, pidKi, pidKd, -1f, 1f);
        }

        private void OnEnable()
        {
            AirshowManager.Instance?.RegisterPerformer(this);
        }

        private void OnDisable()
        {
            AirshowManager.Instance?.UnregisterPerformer(this);
        }

        private void Update()
        {
            if (!_isExecuting) return;

            _maneuverElapsed = Time.time - _maneuverStartTime;
            ManeuverProgress = Mathf.Clamp01(_maneuverElapsed / Mathf.Max(_currentStep.duration, 0.001f));

            RecordGForce();

            if (!isLocalPlayer)
                UpdateAIControl();
            else
                UpdatePlayerGuidance();

            UpdateScores();

            if (ManeuverProgress >= 1f)
                FinishManeuver();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Begins execution of the given maneuver step.</summary>
        /// <param name="step">The maneuver step to execute.</param>
        public void ExecuteManeuver(ManeuverStep step)
        {
            _currentStep = step;
            CurrentManeuver = step.type;
            _maneuverStartTime = Time.time;
            _maneuverElapsed = 0f;
            ManeuverProgress = 0f;
            _isExecuting = true;

            TimingScore  = AirshowScoreCalculator.CalculateTimingScore(
                Time.time, _maneuverStartTime,
                AirshowManager.Instance != null
                    ? AirshowManager.Instance.Config.maneuverTimingTolerance
                    : 1.5f);

            SetSmokeEnabled(step.smokeEnabled);
            if (step.smokeEnabled)
                SetSmokeColor(step.smokeColor);

            OnManeuverStarted?.Invoke(CurrentManeuver);
        }

        /// <summary>Enables or disables smoke trail emission for this performer.</summary>
        public void SetSmokeEnabled(bool enabled)
        {
            if (contrailEmitter == null) return;
            if (enabled) contrailEmitter.StartEmitting();
            else         contrailEmitter.StopEmitting();
            OnSmokeToggled?.Invoke(enabled);
        }

        /// <summary>Sets the smoke trail to a named color preset.</summary>
        public void SetSmokeColor(SmokeColor color)
        {
            SetCustomSmokeColor(ColorFromSmokeEnum(color));
        }

        /// <summary>Sets the smoke trail to an arbitrary RGB color (Custom mode).</summary>
        public void SetCustomSmokeColor(Color color)
        {
            if (contrailEmitter == null) return;
            if (contrailEmitter.trailRenderer != null)
            {
                Gradient g = new Gradient();
                g.SetKeys(
                    new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
                contrailEmitter.trailRenderer.colorGradient = g;
            }
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private void UpdateAIControl()
        {
            if (_currentStep == null) return;

            Vector3 target = (_currentStep.relativePosition +
                (AirshowManager.Instance != null
                    ? AirshowManager.Instance.ActiveRoutine?.venueCenter ?? Vector3.zero
                    : Vector3.zero));

            Vector3 toTarget = target - transform.position;
            if (toTarget.sqrMagnitude < 1f) return;

            Vector3 desiredDir = toTarget.normalized;
            float dt = Time.deltaTime;

            float pitchError = Vector3.SignedAngle(transform.forward, desiredDir, transform.right);
            float yawError   = Vector3.SignedAngle(transform.forward, desiredDir, transform.up);

            float pitchInput = _pitchPID.Update(pitchError, dt);
            float yawInput   = _yawPID.Update(yawError,   dt);

            transform.Rotate(pitchInput * dt * 30f, yawInput * dt * 30f, 0f, Space.Self);
            transform.position += transform.forward * (50f * dt);
        }

        private void UpdatePlayerGuidance()
        {
            // Guidance data is consumed by AirshowHUD — no direct transform manipulation for local player
        }

        private void RecordGForce()
        {
            Vector3 velocity = transform.position - _previousVelocity;
            float acceleration = velocity.magnitude / Mathf.Max(Time.deltaTime, 0.001f);
            float gForce = acceleration / 9.81f;
            _gForceHistory[_gForceIndex % _gForceHistory.Length] = gForce;
            _gForceIndex++;
            _previousVelocity = transform.position;
        }

        private void UpdateScores()
        {
            if (_currentStep == null || AirshowManager.Instance == null) return;

            float tolerance = AirshowManager.Instance.Config.positionTolerance;
            Vector3 venueCenter = AirshowManager.Instance.ActiveRoutine?.venueCenter ?? Vector3.zero;
            Vector3 expected = _currentStep.relativePosition + venueCenter;

            PositionScore  = AirshowScoreCalculator.CalculatePositionScore(transform.position, expected, tolerance);
            SmoothnessScore = AirshowScoreCalculator.CalculateSmoothnessScore(_gForceHistory);
        }

        private void FinishManeuver()
        {
            _isExecuting = false;
            SetSmokeEnabled(false);
            OnManeuverCompleted?.Invoke(CurrentManeuver);
        }

        private static Color ColorFromSmokeEnum(SmokeColor sc)
        {
            return sc switch
            {
                SmokeColor.Red    => Color.red,
                SmokeColor.Blue   => Color.blue,
                SmokeColor.Green  => Color.green,
                SmokeColor.Yellow => Color.yellow,
                SmokeColor.Orange => new Color(1f, 0.5f, 0f),
                SmokeColor.Purple => new Color(0.5f, 0f, 0.5f),
                SmokeColor.Pink   => new Color(1f, 0.41f, 0.71f),
                SmokeColor.Black  => Color.black,
                _                 => Color.white
            };
        }
    }
}
