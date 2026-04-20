using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightSchool
{
    /// <summary>
    /// Polls the active flight state each frame and enforces the set of
    /// <see cref="FlightConstraint"/>s attached to a lesson (Phase 84).
    /// Violations accrue penalty seconds and call
    /// <see cref="FlightInstructor.AddDeviationPenalty"/>; the HUD is notified
    /// via <see cref="OnConstraintViolated"/> and <see cref="OnConstraintWarning"/>.
    /// </summary>
    public class FlightConstraintEnforcer : MonoBehaviour
    {
        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Fired continuously while the envelope is violated.</summary>
        public event Action<FlightConstraint, float> OnConstraintViolated;

        /// <summary>Fired once when the player crosses into the warning margin.</summary>
        public event Action<FlightConstraint> OnConstraintWarning;

        /// <summary>Fired when the player re-enters the envelope after a violation.</summary>
        public event Action<FlightConstraint> OnConstraintRestored;

        // ── Inspector ────────────────────────────────────────────────────────────

        [SerializeField] private FlightInstructor instructor;

        [Tooltip("Multiplier applied to all constraint penalties (useful for difficulty tuning).")]
        [Range(0f, 4f)] [SerializeField] private float penaltyMultiplier = 1f;

        // ── Public state ─────────────────────────────────────────────────────────

        /// <summary>Constraints currently being enforced.</summary>
        public IReadOnlyList<FlightConstraint> ActiveConstraints => _constraints;

        // ── Internal state ───────────────────────────────────────────────────────

        private readonly List<FlightConstraint> _constraints = new List<FlightConstraint>();
        private readonly Dictionary<FlightConstraint, float> _timeOutsideEnvelope =
            new Dictionary<FlightConstraint, float>();
        private readonly HashSet<FlightConstraint> _currentlyViolated = new HashSet<FlightConstraint>();
        private readonly HashSet<FlightConstraint> _currentlyWarned   = new HashSet<FlightConstraint>();

        // Externally supplied sampled flight state. Systems that integrate with
        // the Flight subsystem push values here each frame via <see cref="PushFlightSample"/>.
        private float _altitude, _speed, _heading, _bankAngle, _gForce, _geofenceDistance;
        private bool  _hasSample;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (instructor == null) instructor = GetComponent<FlightInstructor>();
        }

        private void Update()
        {
            if (!_hasSample || _constraints.Count == 0) return;

            foreach (var c in _constraints)
                EvaluateConstraint(c);
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Replaces the active constraint list with a new set. Pass <c>null</c> or
        /// an empty list to disable enforcement.
        /// </summary>
        public void SetConstraints(List<FlightConstraint> constraints)
        {
            _constraints.Clear();
            _timeOutsideEnvelope.Clear();
            _currentlyViolated.Clear();
            _currentlyWarned.Clear();

            if (constraints == null) return;

            foreach (var c in constraints)
                if (c != null) _constraints.Add(c);
        }

        /// <summary>Adds a single constraint to the active set.</summary>
        public void AddConstraint(FlightConstraint constraint)
        {
            if (constraint == null) return;
            _constraints.Add(constraint);
        }

        /// <summary>Clears all active constraints and resets violation counters.</summary>
        public void ClearAll() => SetConstraints(null);

        /// <summary>
        /// Pushes the latest flight-state sample into the enforcer.
        /// External flight systems should call this every frame with current telemetry.
        /// </summary>
        public void PushFlightSample(float altitude, float speed, float heading,
                                      float bankAngle, float gForce, float geofenceDistance)
        {
            _altitude         = altitude;
            _speed            = speed;
            _heading          = heading;
            _bankAngle        = bankAngle;
            _gForce           = gForce;
            _geofenceDistance = geofenceDistance;
            _hasSample        = true;
        }

        /// <summary>Returns the accumulated seconds spent outside <paramref name="constraint"/>.</summary>
        public float GetTimeOutside(FlightConstraint constraint)
        {
            if (constraint == null) return 0f;
            return _timeOutsideEnvelope.TryGetValue(constraint, out float t) ? t : 0f;
        }

        /// <summary>
        /// Returns the current sampled value that <paramref name="constraint"/> is evaluated against.
        /// Exposed for HUD display.
        /// </summary>
        public float GetCurrentValueFor(FlightConstraint constraint)
        {
            if (constraint == null) return 0f;
            return ValueFor(constraint.type);
        }

        // ── Internal helpers ─────────────────────────────────────────────────────

        private void EvaluateConstraint(FlightConstraint c)
        {
            float value = ValueFor(c.type);

            if (c.IsWithin(value))
            {
                if (_currentlyViolated.Remove(c))
                    OnConstraintRestored?.Invoke(c);

                _currentlyWarned.Remove(c);
                return;
            }

            if (c.IsInWarningZone(value))
            {
                if (_currentlyWarned.Add(c))
                    OnConstraintWarning?.Invoke(c);
                return;
            }

            // Violation
            float dt = Time.deltaTime;
            if (!_timeOutsideEnvelope.TryGetValue(c, out float accrued)) accrued = 0f;
            accrued += dt;
            _timeOutsideEnvelope[c] = accrued;

            float penalty = c.penaltyPerSecond * penaltyMultiplier * dt;
            instructor?.AddDeviationPenalty(penalty);

            _currentlyViolated.Add(c);
            OnConstraintViolated?.Invoke(c, penalty);
        }

        /// <summary>
        /// Returns the sampled flight value corresponding to <paramref name="type"/>.
        /// Falls back to 0 for unrecognised enum members.
        /// </summary>
        public float ValueFor(ConstraintType type)
        {
            switch (type)
            {
                case ConstraintType.AltitudeRange:   return _altitude;
                case ConstraintType.SpeedRange:      return _speed;
                case ConstraintType.HeadingRange:    return _heading;
                case ConstraintType.BankAngleLimit:  return _bankAngle;
                case ConstraintType.GForceLimit:     return _gForce;
                case ConstraintType.GeofenceRadius:  return _geofenceDistance;
                default:                             return 0f;
            }
        }
    }
}
