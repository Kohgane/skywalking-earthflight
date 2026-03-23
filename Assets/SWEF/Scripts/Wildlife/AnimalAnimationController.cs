using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Wildlife
{
    /// <summary>
    /// Phase 53 — Manages animations for wildlife members.
    ///
    /// <para>Provides procedural animation fallbacks (walk/fly/swim cycles),
    /// speed-based blending, idle variation, behavior-specific clips, and
    /// LOD-aware animation reduction.  Designed to be attached to the same
    /// GameObject as <see cref="AnimalGroupController"/>.</para>
    /// </summary>
    public class AnimalAnimationController : MonoBehaviour
    {
        #region Constants

        private const float WingFlapFrequencySmall  = 8f;   // Hz
        private const float WingFlapFrequencyLarge  = 2f;
        private const float TailWagFrequency        = 3f;
        private const float IdleVariationInterval   = 4f;
        private const float ScaleVariationMax       = 0.15f; // ±15 %
        private const float AnimationLOD0Distance   = 150f;
        private const float AnimationLOD1Distance   = 600f;

        #endregion

        #region Inspector

        [Header("Animation Settings")]
        [Tooltip("Animator on the root model. If null, procedural animation is used exclusively.")]
        [SerializeField] private Animator modelAnimator;

        [Tooltip("Transforms representing wing bones for procedural flap.")]
        [SerializeField] private List<Transform> wingBones = new List<Transform>();

        [Tooltip("Transform representing the tail bone for procedural wag.")]
        [SerializeField] private Transform tailBone;

        [Tooltip("Species reference used to select animation parameters.")]
        [SerializeField] private AnimalSpecies species;

        [Header("LOD")]
        [Tooltip("Camera used for LOD distance checks. Resolved at runtime if null.")]
        [SerializeField] private Camera lodCamera;

        #endregion

        #region Animator Parameter Hashes

        private static readonly int _hashSpeed    = Animator.StringToHash("Speed");
        private static readonly int _hashBehavior = Animator.StringToHash("Behavior");
        private static readonly int _hashIdle      = Animator.StringToHash("IdleVariant");

        #endregion

        #region Private State

        private float _idleTimer;
        private float _flapPhase;
        private float _tailPhase;
        private float _scaleMultiplier = 1f;
        private int   _currentLOD;       // 0 = full, 1 = simplified, 2 = static

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Apply per-instance scale variation
            _scaleMultiplier = 1f + Random.Range(-ScaleVariationMax, ScaleVariationMax);
            transform.localScale *= _scaleMultiplier;

            if (lodCamera == null) lodCamera = Camera.main;
        }

        private void Update()
        {
            UpdateLOD();

            if (_currentLOD >= 2) return;  // no animation at LOD2+

            if (_currentLOD == 0)
                UpdateProceduralAnimation();
            else
                UpdateSimplifiedAnimation();

            _idleTimer += Time.deltaTime;
            if (_idleTimer >= IdleVariationInterval)
            {
                _idleTimer = 0f;
                TriggerIdleVariation();
            }
        }

        #endregion

        #region Public API

        /// <summary>Called by <see cref="AnimalGroupController"/> when behavior state changes.</summary>
        public void OnBehaviorChanged(AnimalBehavior behavior)
        {
            if (modelAnimator == null) return;
            modelAnimator.SetInteger(_hashBehavior, (int)behavior);
        }

        /// <summary>Updates the movement speed parameter on the animator.</summary>
        public void SetSpeed(float speed)
        {
            if (modelAnimator != null)
                modelAnimator.SetFloat(_hashSpeed, speed);
        }

        /// <summary>Initialises the controller with a species definition.</summary>
        public void Initialise(AnimalSpecies animalSpecies)
        {
            species = animalSpecies;
        }

        #endregion

        #region LOD

        private void UpdateLOD()
        {
            if (lodCamera == null) return;
            float dist = Vector3.Distance(transform.position, lodCamera.transform.position);

            int newLOD;
            if (dist < AnimationLOD0Distance)       newLOD = 0;
            else if (dist < AnimationLOD1Distance)  newLOD = 1;
            else                                    newLOD = 2;

            if (newLOD == _currentLOD) return;
            _currentLOD = newLOD;

            // Enable/disable animator based on LOD
            if (modelAnimator != null)
                modelAnimator.enabled = _currentLOD < 2;
        }

        #endregion

        #region Procedural Animation

        private void UpdateProceduralAnimation()
        {
            if (species == null) return;

            if (species.flightCapable)
                AnimateWingFlap();
            else if (species.swimCapable)
                AnimateTailWag();
        }

        private void UpdateSimplifiedAnimation()
        {
            // At LOD1 only animate the tail/wings at reduced frequency
            if (species == null) return;
            if (species.flightCapable)
            {
                _flapPhase += WingFlapFrequencyLarge * Time.deltaTime;
                ApplyWingRotation(Mathf.Sin(_flapPhase) * 20f);
            }
        }

        private void AnimateWingFlap()
        {
            float freq = species.size <= AnimalSize.Small ? WingFlapFrequencySmall : WingFlapFrequencyLarge;
            _flapPhase += freq * Time.deltaTime;
            ApplyWingRotation(Mathf.Sin(_flapPhase) * 35f);
        }

        private void ApplyWingRotation(float angle)
        {
            foreach (var bone in wingBones)
            {
                if (bone == null) continue;
                bone.localRotation = Quaternion.Euler(angle, 0f, 0f);
            }
        }

        private void AnimateTailWag()
        {
            _tailPhase += TailWagFrequency * Time.deltaTime;
            if (tailBone != null)
                tailBone.localRotation = Quaternion.Euler(0f, Mathf.Sin(_tailPhase) * 25f, 0f);
        }

        private void TriggerIdleVariation()
        {
            if (modelAnimator == null) return;
            modelAnimator.SetInteger(_hashIdle, Random.Range(0, 3));
        }

        #endregion
    }
}
