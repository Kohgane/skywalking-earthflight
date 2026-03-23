using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Wildlife
{
    /// <summary>
    /// Phase 53 — Controls a group of animals that move and behave together.
    ///
    /// <para>Implements a simplified boid-like algorithm (separation, alignment,
    /// cohesion) for natural group motion, a behavior state machine, player-proximity
    /// reactions, and terrain-surface following for land animals.</para>
    /// </summary>
    [RequireComponent(typeof(AnimalAnimationController))]
    public class AnimalGroupController : MonoBehaviour
    {
        #region Constants

        private const float SeparationWeight  = 1.5f;
        private const float AlignmentWeight   = 1.0f;
        private const float CohesionWeight    = 1.0f;
        private const float FleeWeight        = 3.0f;
        private const float TerrainRayLength  = 100f;
        private const float BehaviorTickRate  = 0.5f;  // seconds between state-machine ticks

        #endregion

        #region Inspector

        [Header("Group Data")]
        [SerializeField] private AnimalGroup groupData = new AnimalGroup();

        [Header("Behavior Tuning")]
        [Tooltip("Distance at which the group detects the player and may flee.")]
        [SerializeField] private float fleeDetectionRadius = 80f;

        [Tooltip("Altitude above which player is ignored (bird-eye view safe zone).")]
        [SerializeField] private float safeAltitudeThreshold = 300f;

        [Tooltip("Speed variation applied to each member (0 = none, 1 = ±100%).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float speedVariation = 0.15f;

        [Tooltip("Layer mask used for terrain height raycasts.")]
        [SerializeField] private LayerMask terrainLayerMask = ~0;

        [Header("Audio")]
        [Tooltip("Audio controller used to trigger group sounds.")]
        [SerializeField] private WildlifeAudioController audioController;

        #endregion

        #region Public Properties

        /// <summary>The data object describing this group's species and state.</summary>
        public AnimalGroup GroupData => groupData;

        /// <summary>Current behavioral state of the group.</summary>
        public AnimalBehavior CurrentBehavior => groupData.currentBehavior;

        #endregion

        #region Private State

        private readonly List<Transform> _members = new List<Transform>();
        private Transform _playerTransform;
        private AnimalAnimationController _animController;

        private float _behaviorTimer;
        private Vector3 _targetDirection;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _animController = GetComponent<AnimalAnimationController>();
        }

        private void Start()
        {
            _playerTransform = Camera.main != null ? Camera.main.transform : null;

            if (audioController == null)
                audioController = FindFirstObjectByType<WildlifeAudioController>();

            _targetDirection = groupData.movementDirection != Vector3.zero
                ? groupData.movementDirection
                : Random.insideUnitSphere.normalized;
            _targetDirection.y = 0f;
        }

        private void Update()
        {
            _behaviorTimer += Time.deltaTime;
            if (_behaviorTimer >= BehaviorTickRate)
            {
                _behaviorTimer = 0f;
                UpdateBehaviorState();
            }

            MoveGroup();
        }

        #endregion

        #region Public API

        /// <summary>Initialises this controller with the provided group data.</summary>
        public void Initialise(AnimalGroup data, List<Transform> members)
        {
            groupData = data;
            _members.Clear();
            _members.AddRange(members);
        }

        /// <summary>Immediately forces the group into a fleeing state.</summary>
        public void Startle(Vector3 threatPosition)
        {
            Vector3 away = (transform.position - threatPosition).normalized;
            away.y = 0f;
            _targetDirection = away;
            SetBehavior(AnimalBehavior.Fleeing);
        }

        #endregion

        #region Behavior State Machine

        private void UpdateBehaviorState()
        {
            bool playerNear = IsPlayerNearby();

            switch (groupData.currentBehavior)
            {
                case AnimalBehavior.Grazing:
                    if (playerNear)   SetBehavior(AnimalBehavior.Fleeing);
                    else if (ShouldStartMoving()) SetBehavior(AnimalBehavior.Migrating);
                    break;

                case AnimalBehavior.Migrating:
                    if (playerNear)   SetBehavior(AnimalBehavior.Fleeing);
                    else if (ShouldRest()) SetBehavior(AnimalBehavior.Resting);
                    break;

                case AnimalBehavior.Fleeing:
                    if (!playerNear)  SetBehavior(AnimalBehavior.Grazing);
                    break;

                case AnimalBehavior.Resting:
                    if (playerNear)   SetBehavior(AnimalBehavior.Fleeing);
                    else if (ShouldStartMoving()) SetBehavior(AnimalBehavior.Grazing);
                    break;

                case AnimalBehavior.Flying:
                case AnimalBehavior.Swimming:
                    if (playerNear)   SetBehavior(AnimalBehavior.Fleeing);
                    break;
            }
        }

        private void SetBehavior(AnimalBehavior behavior)
        {
            if (groupData.currentBehavior == behavior) return;
            groupData.currentBehavior = behavior;
            _animController?.OnBehaviorChanged(behavior);
            audioController?.PlayGroupSound(groupData, behavior);
        }

        private bool IsPlayerNearby()
        {
            if (_playerTransform == null) return false;
            float playerAlt = _playerTransform.position.y;
            if (playerAlt > safeAltitudeThreshold) return false;
            return Vector3.Distance(transform.position, _playerTransform.position) < fleeDetectionRadius;
        }

        private bool ShouldStartMoving() => Random.value < 0.3f;
        private bool ShouldRest()        => Random.value < 0.2f;

        #endregion

        #region Movement

        private void MoveGroup()
        {
            if (groupData.currentBehavior == AnimalBehavior.Resting) return;

            float speed  = groupData.species != null ? groupData.species.baseSpeed : 3f;
            float flee   = groupData.currentBehavior == AnimalBehavior.Fleeing ? FleeWeight : 1f;

            _targetDirection = ComputeBoidDirection();

            Vector3 move = _targetDirection * (speed * flee * Time.deltaTime);
            transform.position += move;

            groupData.centerPosition = transform.position;
            groupData.movementDirection = _targetDirection;

            // Terrain following for land animals
            if (groupData.species == null || (!groupData.species.flightCapable && !groupData.species.swimCapable))
                SnapToTerrain();

            if (_targetDirection != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(_targetDirection), Time.deltaTime * 3f);
        }

        private Vector3 ComputeBoidDirection()
        {
            if (_members.Count == 0)
                return _targetDirection;

            Vector3 separation = Vector3.zero;
            Vector3 alignment  = _targetDirection;
            Vector3 cohesion   = Vector3.zero;
            Vector3 flee       = Vector3.zero;

            foreach (var m in _members)
            {
                if (m == null) continue;
                Vector3 diff = transform.position - m.position;
                float dist = diff.magnitude;
                if (dist < 1f) dist = 1f;
                separation += diff / (dist * dist);
                cohesion   += m.position;
            }

            if (_members.Count > 0) cohesion = (cohesion / _members.Count - transform.position).normalized;

            if (groupData.currentBehavior == AnimalBehavior.Fleeing && _playerTransform != null)
                flee = (transform.position - _playerTransform.position).normalized * FleeWeight;

            Vector3 result = (separation  * SeparationWeight +
                              alignment   * AlignmentWeight  +
                              cohesion    * CohesionWeight   +
                              flee).normalized;
            result.y = 0f;
            return result == Vector3.zero ? _targetDirection : result;
        }

        private void SnapToTerrain()
        {
            if (Physics.Raycast(transform.position + Vector3.up * 50f,
                                Vector3.down, out RaycastHit hit,
                                TerrainRayLength, terrainLayerMask))
            {
                Vector3 p = transform.position;
                p.y = hit.point.y;
                transform.position = p;
            }
        }

        #endregion
    }
}
