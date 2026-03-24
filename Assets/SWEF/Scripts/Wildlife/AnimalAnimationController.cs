using UnityEngine;

namespace SWEF.Wildlife
{
    /// <summary>
    /// Phase 75 — Handles procedural and keyframe animation for wildlife entities.
    /// Supports birds, marine life, and land animals with LOD-aware switching.
    /// </summary>
    public class AnimalAnimationController : MonoBehaviour
    {
        #region Inspector

        [Header("Wing Animation")]
        [Tooltip("Transform of the left wing bone (optional).")]
        [SerializeField] private Transform leftWingBone;

        [Tooltip("Transform of the right wing bone (optional).")]
        [SerializeField] private Transform rightWingBone;

        [Header("Tail Animation")]
        [Tooltip("Transform of the tail bone for marine life (optional).")]
        [SerializeField] private Transform tailBone;

        [Header("LOD")]
        [Tooltip("Distance at which full skeletal animation switches to procedural.")]
        [SerializeField] private float fullAnimDistance  = 150f;

        [Tooltip("Distance at which procedural animation switches to billboard.")]
        [SerializeField] private float proceduralDistance = 500f;

        [Header("Category")]
        [SerializeField] private WildlifeCategory animalCategory = WildlifeCategory.Bird;

        #endregion

        #region Properties

        /// <summary>Wing flap frequency in cycles per second.</summary>
        public float WingFlapFrequency { get; set; } = 3f;

        #endregion

        #region Private State

        private Animator _animator;
        private WildlifeBehavior _currentBehavior = WildlifeBehavior.Roaming;
        private float _speed;
        private float _time;
        private Transform _playerTransform;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            var cam = Camera.main;
            if (cam != null) _playerTransform = cam.transform;
        }

        private void Update()
        {
            _time += Time.deltaTime;
            float distToPlayer = _playerTransform != null
                ? Vector3.Distance(transform.position, _playerTransform.position)
                : 0f;

            if (distToPlayer < fullAnimDistance)
                AnimateFull();
            else if (distToPlayer < proceduralDistance)
                AnimateProcedural();
            // else: billboard/dot — no per-frame animation
        }

        #endregion

        #region Animation Tiers

        private void AnimateFull()
        {
            if (_animator != null && _animator.isActiveAndEnabled)
            {
                _animator.SetFloat("Speed", _speed);
                _animator.SetInteger("Behavior", (int)_currentBehavior);
                return;
            }
            AnimateProcedural();
        }

        private void AnimateProcedural()
        {
            switch (animalCategory)
            {
                case WildlifeCategory.Bird:
                case WildlifeCategory.Raptor:
                case WildlifeCategory.Seabird:
                case WildlifeCategory.Waterfowl:
                case WildlifeCategory.MigratoryBird:
                    AnimateBirdProcedural();
                    break;
                case WildlifeCategory.MarineMammal:
                case WildlifeCategory.Fish:
                    AnimateMarineProcedural();
                    break;
                case WildlifeCategory.LandMammal:
                    AnimateLandProcedural();
                    break;
            }
        }

        private void AnimateBirdProcedural()
        {
            if (_currentBehavior == WildlifeBehavior.Diving)
            {
                // Wings tucked
                ApplyWingRotation(0f);
                return;
            }
            bool gliding = _currentBehavior == WildlifeBehavior.Circling;
            float freq   = gliding ? 0.5f : WingFlapFrequency;
            float amp    = gliding ? 5f : 30f;
            float angle  = Mathf.Sin(_time * freq * Mathf.PI * 2f) * amp;
            ApplyWingRotation(angle);
        }

        private void AnimateMarineProcedural()
        {
            if (tailBone == null) return;
            float angle = Mathf.Sin(_time * 2f * Mathf.PI) * 20f;
            tailBone.localRotation = Quaternion.Euler(0f, angle, 0f);
        }

        private void AnimateLandProcedural()
        {
            // Minimal procedural: slight body bob with speed
            float bob = Mathf.Sin(_time * _speed * 2f) * 0.05f;
            Vector3 pos = transform.localPosition;
            pos.y = bob;
            transform.localPosition = pos;
        }

        private void ApplyWingRotation(float angle)
        {
            if (leftWingBone  != null) leftWingBone.localRotation  = Quaternion.Euler(angle, 0f, 0f);
            if (rightWingBone != null) rightWingBone.localRotation = Quaternion.Euler(-angle, 0f, 0f);
        }

        #endregion

        #region Public API

        /// <summary>Maps a wildlife behavior to the appropriate animation state.</summary>
        public void SetAnimationState(WildlifeBehavior behavior)
        {
            _currentBehavior = behavior;
            switch (behavior)
            {
                case WildlifeBehavior.Fleeing:
                    WingFlapFrequency = 6f; break;
                case WildlifeBehavior.Circling:
                    WingFlapFrequency = 1f; break;
                case WildlifeBehavior.Diving:
                    WingFlapFrequency = 0f; break;
                default:
                    WingFlapFrequency = 3f; break;
            }
        }

        /// <summary>Adjusts animation playback rate based on movement speed.</summary>
        public void SetSpeed(float speed)
        {
            _speed = speed;
            WingFlapFrequency = Mathf.Lerp(2f, 6f, speed / 30f);
        }

        #endregion
    }
}
