// WingmanAI.cs — SWEF Flight Formation & Wingman AI System (Phase 63)
using System;
using UnityEngine;

namespace SWEF.Formation
{
    /// <summary>
    /// Describes the current behavioural state of a <see cref="WingmanAI"/>.
    /// </summary>
    public enum WingmanState
    {
        /// <summary>Manoeuvring to reach the assigned formation slot.</summary>
        Forming,

        /// <summary>Holding position in the assigned formation slot.</summary>
        Following,

        /// <summary>Temporarily leaving formation at the player's command.</summary>
        Breaking,

        /// <summary>Pursuing and engaging a designated hostile target.</summary>
        Attacking,

        /// <summary>Returning to the assigned formation slot after a task.</summary>
        Returning,

        /// <summary>Orbiting a protected escort target.</summary>
        Escorting,
    }

    /// <summary>
    /// MonoBehaviour that drives a single AI wingman aircraft.
    /// <para>
    /// The wingman uses <see cref="Vector3.MoveTowards"/> for position and
    /// <see cref="Quaternion.Slerp"/> for rotation to smoothly reach its
    /// assigned formation slot.  Forward obstacle avoidance is performed via
    /// three raycasts (forward, 45° left, 45° right).
    /// </para>
    /// </summary>
    public sealed class WingmanAI : MonoBehaviour
    {
        #region Events

        /// <summary>Raised whenever the wingman's <see cref="currentState"/> changes.</summary>
        public event Action<WingmanState> OnStateChanged;

        #endregion

        #region Inspector — References

        [Header("References")]
        [Tooltip("Transform of the player (formation leader).")]
        [SerializeField] private Transform _leader;

        [Tooltip("Personality asset that modifies this wingman's behaviour.")]
        [SerializeField] private WingmanPersonality personality;

        #endregion

        #region Inspector — Flight Parameters

        [Header("Flight Parameters")]
        [Tooltip("Maximum flight speed in metres per second.")]
        [SerializeField] private float maxSpeed = 250f;

        [Tooltip("Acceleration in metres per second squared.")]
        [SerializeField] private float acceleration = 30f;

        [Tooltip("Rotational interpolation speed (Slerp factor per second).")]
        [SerializeField] private float turnSpeed = 3f;

        #endregion

        #region Inspector — Formation

        [Header("Formation")]
        [Tooltip("Distance threshold in metres at which the wingman is considered " +
                 "\"in position\" within its slot.")]
        [SerializeField] private float _formationTolerance = 2f;

        #endregion

        #region Inspector — Obstacle Avoidance

        [Header("Obstacle Avoidance")]
        [Tooltip("Raycast range for forward obstacle detection (metres).")]
        [SerializeField] private float avoidanceRange = 50f;

        [Tooltip("Layer mask used for obstacle raycasts.")]
        [SerializeField] private LayerMask obstacleMask = Physics.DefaultRaycastLayers;

        #endregion

        #region Runtime State

        /// <summary>Current behavioural state.</summary>
        public WingmanState currentState { get; private set; } = WingmanState.Forming;

        /// <summary>Zero-based index of the slot this wingman occupies.</summary>
        public int assignedSlot { get; set; } = 0;

        private Transform _attackTarget;
        private Transform _escortTarget;
        private Vector3   _targetSlotPosition;
        private float     _currentSpeed;

        #endregion

        #region Public Properties

        /// <summary>
        /// <see langword="true"/> if the wingman is within
        /// <see cref="FormationTolerance"/> of its assigned slot.
        /// </summary>
        public bool IsInFormation =>
            Vector3.Distance(transform.position, _targetSlotPosition) <= EffectiveTolerance;

        /// <summary>Personality asset (may be <see langword="null"/>).</summary>
        public WingmanPersonality Personality => personality;

        /// <summary>The player / leader transform this wingman follows.</summary>
        public Transform leader
        {
            get => _leader;
            set => _leader = value;
        }

        /// <summary>Base distance threshold (metres) to consider the wingman "in position".</summary>
        public float formationTolerance
        {
            get => _formationTolerance;
            set => _formationTolerance = value;
        }

        private float EffectiveTolerance =>
            personality != null
                ? personality.GetEffectiveTolerance(_formationTolerance)
                : _formationTolerance;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            switch (currentState)
            {
                case WingmanState.Forming:
                case WingmanState.Following:
                case WingmanState.Returning:
                    MoveTowardSlot();
                    if (IsInFormation && currentState != WingmanState.Following)
                        SetState(WingmanState.Following);
                    break;

                case WingmanState.Attacking:
                    if (_attackTarget != null)
                        MoveTowardTarget(_attackTarget.position);
                    else
                        CommandReturn();
                    break;

                case WingmanState.Escorting:
                    if (_escortTarget != null)
                        OrbitEscortTarget();
                    else
                        CommandReturn();
                    break;

                case WingmanState.Breaking:
                    // Free-flight: maintain heading, no specific destination.
                    FlyForward();
                    break;
            }
        }

        #endregion

        #region Public Commands

        /// <summary>
        /// Commands the wingman to attack the given target.
        /// Transitions to <see cref="WingmanState.Attacking"/>.
        /// </summary>
        /// <param name="target">Transform of the hostile target.</param>
        public void CommandAttack(Transform target)
        {
            _attackTarget = target;
            SetState(WingmanState.Attacking);
        }

        /// <summary>
        /// Commands the wingman to break from the formation and fly freely.
        /// Transitions to <see cref="WingmanState.Breaking"/>.
        /// </summary>
        public void CommandBreak()
        {
            _attackTarget = null;
            _escortTarget = null;
            SetState(WingmanState.Breaking);
        }

        /// <summary>
        /// Commands the wingman to return to its formation slot.
        /// Transitions to <see cref="WingmanState.Returning"/>.
        /// </summary>
        public void CommandReturn()
        {
            _attackTarget = null;
            _escortTarget = null;
            SetState(WingmanState.Returning);
        }

        /// <summary>
        /// Commands the wingman to escort (orbit and protect) the given target.
        /// Transitions to <see cref="WingmanState.Escorting"/>.
        /// </summary>
        /// <param name="escortTarget">Transform of the VIP / cargo to protect.</param>
        public void CommandEscort(Transform escortTarget)
        {
            _escortTarget = escortTarget;
            SetState(WingmanState.Escorting);
        }

        /// <summary>
        /// Assigns the world-space slot position the wingman should fly toward
        /// each frame. Called by <see cref="FormationManager"/> every Update.
        /// </summary>
        /// <param name="slotWorldPosition">Target world-space position.</param>
        public void SetSlotPosition(Vector3 slotWorldPosition)
        {
            _targetSlotPosition = slotWorldPosition;
        }

        #endregion

        #region Private — Movement

        private void MoveTowardSlot()
        {
            MoveTowardTarget(_targetSlotPosition);
        }

        private void MoveTowardTarget(Vector3 destination)
        {
            Vector3 avoidance = CalculateAvoidance();
            Vector3 desired   = (destination - transform.position + avoidance).normalized;

            // Smoothly rotate toward desired direction.
            if (desired != Vector3.zero)
            {
                float effectiveTurn = personality != null
                    ? turnSpeed * Mathf.Lerp(0.5f, 1.5f, personality.SkillLevel)
                    : turnSpeed;

                Quaternion targetRot  = Quaternion.LookRotation(desired, Vector3.up);
                transform.rotation    = Quaternion.Slerp(
                    transform.rotation, targetRot,
                    effectiveTurn * Time.deltaTime);
            }

            // Accelerate toward max speed.
            float effectiveAccel = personality != null
                ? acceleration * Mathf.Lerp(0.5f, 1.5f, personality.SkillLevel)
                : acceleration;

            _currentSpeed = Mathf.MoveTowards(_currentSpeed, maxSpeed,
                effectiveAccel * Time.deltaTime);

            transform.position = Vector3.MoveTowards(
                transform.position, destination,
                _currentSpeed * Time.deltaTime);
        }

        private void FlyForward()
        {
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, maxSpeed,
                acceleration * Time.deltaTime);

            transform.position += transform.forward * (_currentSpeed * Time.deltaTime);
        }

        private void OrbitEscortTarget()
        {
            // Maintain a loose orbit around the escort target at slot distance.
            float orbitRadius = FormationConfig.DefaultSpacing * 2f;
            Vector3 toTarget  = _escortTarget.position - transform.position;
            float   dist      = toTarget.magnitude;

            Vector3 destination;
            if (dist > orbitRadius + 5f)
            {
                destination = _escortTarget.position - toTarget.normalized * orbitRadius;
            }
            else
            {
                // Orbit tangentially.
                Vector3 tangent = Vector3.Cross(toTarget.normalized, Vector3.up);
                destination     = transform.position + tangent * maxSpeed * Time.deltaTime;
            }

            MoveTowardTarget(destination);
        }

        #endregion

        #region Private — Obstacle Avoidance

        private Vector3 CalculateAvoidance()
        {
            Vector3 avoidance = Vector3.zero;
            float   range     = avoidanceRange;

            // Forward ray.
            if (Physics.Raycast(transform.position, transform.forward, out _, range, obstacleMask))
                avoidance += Vector3.up;

            // 45° left ray.
            Vector3 leftDir = Quaternion.AngleAxis(-45f, Vector3.up) * transform.forward;
            if (Physics.Raycast(transform.position, leftDir, out _, range, obstacleMask))
                avoidance += transform.right;

            // 45° right ray.
            Vector3 rightDir = Quaternion.AngleAxis(45f, Vector3.up) * transform.forward;
            if (Physics.Raycast(transform.position, rightDir, out _, range, obstacleMask))
                avoidance -= transform.right;

            return avoidance;
        }

        #endregion

        #region Private — State

        private void SetState(WingmanState newState)
        {
            if (currentState == newState) return;
            currentState = newState;
            OnStateChanged?.Invoke(currentState);
        }

        #endregion
    }
}
