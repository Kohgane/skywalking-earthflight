using System;
using System.Collections;
using UnityEngine;

namespace SWEF.Wildlife
{
    /// <summary>
    /// Phase 75 — Controls a single wildlife group instance.
    /// Drives the per-group state machine and aircraft-threat reaction.
    /// </summary>
    [DisallowMultipleComponent]
    public class AnimalGroupController : MonoBehaviour
    {
        #region Inspector

        [Header("Group State")]
        [SerializeField] private WildlifeGroupState state = new WildlifeGroupState();

        [Header("AI Timings")]
        [Tooltip("Seconds between AI update ticks.")]
        [SerializeField] private float aiTickRate = 0.2f;

        [Tooltip("Seconds the group remains in Fleeing before returning to Roaming.")]
        [SerializeField] private float fleeReturnTime = 8f;

        [Tooltip("Minimum terrain clearance in metres.")]
        [SerializeField] private float terrainClearance = 5f;

        #endregion

        #region Events

        /// <summary>Fired when the group transitions to a new behavior state.</summary>
        public event Action<WildlifeBehavior> OnBehaviorChanged;

        /// <summary>Fired when the group is first discovered by the player.</summary>
        public event Action<WildlifeGroupState> OnDiscovered;

        #endregion

        #region Public Properties

        /// <summary>The current state snapshot for this group.</summary>
        public WildlifeGroupState State => state;

        #endregion

        #region Private State

        private Transform _playerTransform;
        private BirdFlockController _flockController;
        private MarineLifeController _marineController;
        private Coroutine _aiCoroutine;
        private float _fleeTimer;
        private bool _wasAware;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _flockController  = GetComponent<BirdFlockController>();
            _marineController = GetComponent<MarineLifeController>();
        }

        private void Start()
        {
            var cam = Camera.main;
            if (cam != null) _playerTransform = cam.transform;
            _aiCoroutine = StartCoroutine(AILoop());
        }

        private void OnDestroy()
        {
            if (_aiCoroutine != null) StopCoroutine(_aiCoroutine);
        }

        #endregion

        #region AI Loop

        private IEnumerator AILoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(aiTickRate);
                TickAI();
            }
        }

        private void TickAI()
        {
            if (state.species == null) return;

            UpdateThreatLevel();
            UpdateBehavior();
            CheckDiscovery();
            ApplyTerrainFollowing();
        }

        private void UpdateThreatLevel()
        {
            float dist = GetDistanceToPlayer();
            WildlifeThreatLevel prev = state.threatLevel;

            if      (dist > state.species.awareDistance)  state.threatLevel = WildlifeThreatLevel.None;
            else if (dist > state.species.fleeDistance)   state.threatLevel = WildlifeThreatLevel.Aware;
            else if (dist > state.species.fleeDistance * 0.6f) state.threatLevel = WildlifeThreatLevel.Alarmed;
            else if (dist > state.species.fleeDistance * 0.3f) state.threatLevel = WildlifeThreatLevel.Fleeing;
            else                                           state.threatLevel = WildlifeThreatLevel.Panicked;

            if (state.threatLevel != prev && state.threatLevel >= WildlifeThreatLevel.Fleeing)
                BeginFlee();
        }

        private void UpdateBehavior()
        {
            switch (state.currentBehavior)
            {
                case WildlifeBehavior.Fleeing:
                    _fleeTimer -= aiTickRate;
                    if (_fleeTimer <= 0f && state.threatLevel <= WildlifeThreatLevel.Aware)
                        SetBehavior(WildlifeBehavior.Roaming);
                    break;

                case WildlifeBehavior.Roaming:
                    if (state.threatLevel >= WildlifeThreatLevel.Fleeing)
                        BeginFlee();
                    break;

                case WildlifeBehavior.Idle:
                    if (state.threatLevel >= WildlifeThreatLevel.Alarmed)
                        SetBehavior(WildlifeBehavior.Roaming);
                    break;
            }

            // Update group position (simple forward movement)
            if (state.currentBehavior != WildlifeBehavior.Idle &&
                state.currentBehavior != WildlifeBehavior.Sleeping)
            {
                float speed = (state.currentBehavior == WildlifeBehavior.Fleeing)
                    ? state.species.fleeSpeed
                    : state.species.baseSpeed;
                if (state.groupVelocity == Vector3.zero)
                    state.groupVelocity = transform.forward * speed;
                state.centerPosition += state.groupVelocity * aiTickRate;
                transform.position    = state.centerPosition;
            }
        }

        private void BeginFlee()
        {
            if (_playerTransform != null)
            {
                Vector3 away = (transform.position - _playerTransform.position).normalized;
                if (state.threatLevel == WildlifeThreatLevel.Panicked)
                    away = UnityEngine.Random.insideUnitSphere.normalized;
                state.groupVelocity = away * state.species.fleeSpeed;
            }
            _fleeTimer = fleeReturnTime;
            SetBehavior(WildlifeBehavior.Fleeing);
        }

        private void CheckDiscovery()
        {
            if (state.isDiscovered) return;
            float dist = GetDistanceToPlayer();
            if (dist <= state.species.awareDistance)
            {
                state.isDiscovered = true;
                OnDiscovered?.Invoke(state);

                WildlifeManager.Instance?.ReportDiscovery(state.species);

                var record = new WildlifeEncounterRecord
                {
                    speciesId        = state.species.speciesId,
                    groupId          = state.groupId,
                    encounterPosition = state.centerPosition,
                    encounterAltitude = state.centerPosition.y,
                    encounterTime    = Time.time,
                    category         = state.species.category,
                    groupSize        = state.memberCount,
                    closestApproach  = dist
                };
                WildlifeManager.Instance?.RecordEncounter(record);
            }
        }

        private void ApplyTerrainFollowing()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 200f))
            {
                float targetY = hit.point.y + terrainClearance + state.species.minAltitude;
                if (transform.position.y < targetY)
                {
                    var pos = transform.position;
                    pos.y = targetY;
                    transform.position = pos;
                    state.centerPosition = pos;
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>Returns the current distance to the player aircraft.</summary>
        public float GetDistanceToPlayer()
        {
            if (_playerTransform == null) return float.MaxValue;
            return Vector3.Distance(state.centerPosition, _playerTransform.position);
        }

        /// <summary>Manually sets the group to a specific behavior state.</summary>
        public void SetBehavior(WildlifeBehavior behavior)
        {
            if (state.currentBehavior == behavior) return;
            state.currentBehavior = behavior;
            _flockController?.OnBehaviorChanged(behavior);
            _marineController?.OnBehaviorChanged(behavior);
            OnBehaviorChanged?.Invoke(behavior);
        }

        /// <summary>Forces the group into an immediate panic scatter.</summary>
        public void ForceScatter()
        {
            state.threatLevel = WildlifeThreatLevel.Panicked;
            BeginFlee();
        }

        /// <summary>Initialises this controller with a group state snapshot.</summary>
        public void Initialise(WildlifeGroupState groupState)
        {
            state = groupState;
            transform.position = state.centerPosition;
        }

        #endregion
    }
}
