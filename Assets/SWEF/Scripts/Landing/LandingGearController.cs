// LandingGearController.cs — SWEF Landing & Airport System (Phase 68)
using System;
using System.Collections;
using UnityEngine;

namespace SWEF.Landing
{
    /// <summary>
    /// Phase 68 — Manages landing gear deployment and retraction.
    ///
    /// <para>Drives optional per-leg <see cref="Animator"/> controllers,
    /// plays audio cues, and fires <see cref="OnGearStateChanged"/> for HUD
    /// and <see cref="LandingUI"/> subscribers.  Integrates with the CockpitHUD
    /// <c>WarningSystem</c> (Phase 65) to alert when gear is retracted on approach.</para>
    /// </summary>
    public class LandingGearController : MonoBehaviour
    {
        #region Inspector

        [Header("Gear Controller — Timing")]
        [Tooltip("Time in seconds to fully deploy the landing gear.")]
        [SerializeField] private float deployTime = LandingConfig.GearDeployTime;

        [Tooltip("Time in seconds to fully retract the landing gear.")]
        [SerializeField] private float retractTime = LandingConfig.GearRetractTime;

        [Header("Gear Controller — Auto Deploy")]
        [Tooltip("AGL altitude (m) below which a gear-down reminder is issued.")]
        [SerializeField] private float autoDeployAltitude = LandingConfig.AutoDeployAltitude;

        [Tooltip("Whether the auto-deploy altitude reminder is active.")]
        [SerializeField] private bool autoDeployEnabled = true;

        [Header("Gear Controller — Animation")]
        [Tooltip("Optional Animator components, one per gear leg (nose, left main, right main…).")]
        [SerializeField] private Animator[] gearAnimators = Array.Empty<Animator>();

        [Header("Gear Controller — Audio")]
        [Tooltip("AudioSource used for gear sounds.")]
        [SerializeField] private AudioSource gearAudioSource;

        [Tooltip("Clip played when gear begins deploying.")]
        [SerializeField] private AudioClip gearDeploySound;

        [Tooltip("Clip played when gear begins retracting.")]
        [SerializeField] private AudioClip gearRetractSound;

        [Tooltip("Clip played when gear locks in position.")]
        [SerializeField] private AudioClip gearLockedSound;

        #endregion

        #region Public State

        /// <summary>Current gear state.</summary>
        public GearState CurrentState { get; private set; } = GearState.Retracted;

        /// <summary>Normalised deployment progress: 0 = fully retracted, 1 = fully deployed.</summary>
        public float DeployProgress { get; private set; }

        /// <summary><c>true</c> when gear is fully deployed and locked.</summary>
        public bool IsFullyDeployed => CurrentState == GearState.Deployed;

        /// <summary><c>true</c> when gear is in a safe (locked) position — either deployed or retracted.</summary>
        public bool IsGearSafe => CurrentState == GearState.Deployed || CurrentState == GearState.Retracted;

        #endregion

        #region Events

        /// <summary>Fired every time the <see cref="GearState"/> changes.</summary>
        public event Action<GearState> OnGearStateChanged;

        #endregion

        #region Private State

        private Coroutine _gearCoroutine;

        // Animator parameter hash.
        private static readonly int DeployProgressHash = Animator.StringToHash("DeployProgress");

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (autoDeployEnabled)
                CheckAutoDeployWarning();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Toggles gear: deploys if retracted, retracts if deployed.
        /// No-op while gear is in transit or damaged.
        /// </summary>
        public void ToggleGear()
        {
            switch (CurrentState)
            {
                case GearState.Retracted: DeployGear();  break;
                case GearState.Deployed:  RetractGear(); break;
            }
        }

        /// <summary>Begins deployment of the landing gear.</summary>
        public void DeployGear()
        {
            if (CurrentState == GearState.Deployed || CurrentState == GearState.Deploying) return;
            if (CurrentState == GearState.Damaged) return;
            StartGearCoroutine(deploying: true);
        }

        /// <summary>Begins retraction of the landing gear.</summary>
        public void RetractGear()
        {
            if (CurrentState == GearState.Retracted || CurrentState == GearState.Retracting) return;
            if (CurrentState == GearState.Damaged) return;
            StartGearCoroutine(deploying: false);
        }

        /// <summary>
        /// Marks gear as damaged, preventing further deploy/retract operations.
        /// </summary>
        public void DamageGear()
        {
            if (_gearCoroutine != null)
            {
                StopCoroutine(_gearCoroutine);
                _gearCoroutine = null;
            }
            SetState(GearState.Damaged);
        }

        #endregion

        #region Gear Coroutine

        private void StartGearCoroutine(bool deploying)
        {
            if (_gearCoroutine != null)
                StopCoroutine(_gearCoroutine);
            _gearCoroutine = StartCoroutine(MoveGear(deploying));
        }

        private IEnumerator MoveGear(bool deploying)
        {
            GearState inTransit = deploying ? GearState.Deploying : GearState.Retracting;
            SetState(inTransit);
            PlayClip(deploying ? gearDeploySound : gearRetractSound);

            float duration = deploying ? deployTime : retractTime;
            float startProgress = DeployProgress;
            float targetProgress = deploying ? 1f : 0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                DeployProgress = Mathf.Lerp(startProgress, targetProgress, t);
                UpdateAnimators(DeployProgress);
                yield return null;
            }

            DeployProgress = targetProgress;
            UpdateAnimators(DeployProgress);
            PlayClip(gearLockedSound);

            GearState locked = deploying ? GearState.Deployed : GearState.Retracted;
            SetState(locked);
            _gearCoroutine = null;
        }

        #endregion

        #region Helpers

        private void SetState(GearState next)
        {
            if (CurrentState == next) return;
            CurrentState = next;
            OnGearStateChanged?.Invoke(CurrentState);
        }

        private void UpdateAnimators(float progress)
        {
            foreach (Animator anim in gearAnimators)
            {
                if (anim != null)
                    anim.SetFloat(DeployProgressHash, progress);
            }
        }

        private void PlayClip(AudioClip clip)
        {
            if (gearAudioSource == null || clip == null) return;
            gearAudioSource.PlayOneShot(clip);
        }

        private void CheckAutoDeployWarning()
        {
            if (CurrentState != GearState.Retracted) return;
            if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 2000f))
                return;
            if (hit.distance < autoDeployAltitude)
            {
#if SWEF_WARNINGSYSTEM_AVAILABLE
                var ws = FindFirstObjectByType<SWEF.CockpitHUD.WarningSystem>();
                ws?.AddWarning("GEAR UP", "GEAR NOT DOWN", SWEF.CockpitHUD.WarningLevel.Warning);
#endif
            }
        }

        #endregion
    }
}
