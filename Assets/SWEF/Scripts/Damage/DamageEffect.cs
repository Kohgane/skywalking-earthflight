// DamageEffect.cs — SWEF Damage & Repair System (Phase 66)
using UnityEngine;

namespace SWEF.Damage
{
    /// <summary>
    /// Visual and audio effects controller for the Damage &amp; Repair system.
    ///
    /// <para>Subscribes to <see cref="DamageModel"/> events and plays the
    /// appropriate particles and sounds in response to damage state changes.</para>
    ///
    /// <para>Attach to the same GameObject as <see cref="DamageModel"/> (or a
    /// child) and populate the inspector references.</para>
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class DamageEffect : MonoBehaviour
    {
        #region Inspector

        [Header("Particle Systems")]
        [Tooltip("Looping smoke effect shown when a part has Moderate or worse damage.")]
        /// <summary>Looping smoke effect for Moderate or worse damage.</summary>
        [SerializeField] private ParticleSystem smokeEffect;

        [Tooltip("Looping fire effect shown for Severe or Critical damage.")]
        /// <summary>Looping fire effect for Severe or Critical damage.</summary>
        [SerializeField] private ParticleSystem fireEffect;

        [Tooltip("One-shot spark burst played at the impact point.")]
        /// <summary>One-shot spark burst played at the impact point.</summary>
        [SerializeField] private ParticleSystem sparkEffect;

        [Tooltip("One-shot debris burst played at the impact point.")]
        /// <summary>One-shot debris burst played at the impact point.</summary>
        [SerializeField] private ParticleSystem debrisEffect;

        [Header("Audio")]
        [Tooltip("Random pool of collision impact sounds.")]
        /// <summary>Random pool of collision impact sounds.</summary>
        [SerializeField] private AudioClip[] impactSounds;

        [Tooltip("Sounds played when the airframe is under structural stress.")]
        /// <summary>Sounds played under structural stress.</summary>
        [SerializeField] private AudioClip[] metalStressClips;

        [Tooltip("Alarm sounds triggered by severe / critical damage.")]
        /// <summary>Alarm sounds triggered by severe / critical damage.</summary>
        [SerializeField] private AudioClip[] alarmClips;

        /// <summary>AudioSource used for all one-shot and looping audio.</summary>
        [SerializeField] private AudioSource audioSource;

        [Header("Materials")]
        [Tooltip("Damage-overlay material applied to parts at Moderate or worse.")]
        /// <summary>Damage-overlay material for Moderate or worse.</summary>
        [SerializeField] private Material damagedMaterial;

        [Tooltip("Burn/char material applied to parts at Severe or Critical damage.")]
        /// <summary>Burn/char material for Severe or Critical damage.</summary>
        [SerializeField] private Material burntMaterial;

        #endregion

        #region Private State

        private DamageModel _model;

        #endregion

        #region Unity

        private void Awake()
        {
            _model = GetComponent<DamageModel>();
            if (_model == null)
                _model = GetComponentInParent<DamageModel>();

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (_model != null)
            {
                _model.OnDamageReceived           += HandleDamageReceived;
                _model.OnPartDamageLevelChanged    += HandlePartLevelChanged;
                _model.OnAircraftDestroyed         += HandleAircraftDestroyed;
            }
        }

        private void OnDisable()
        {
            if (_model != null)
            {
                _model.OnDamageReceived           -= HandleDamageReceived;
                _model.OnPartDamageLevelChanged    -= HandlePartLevelChanged;
                _model.OnAircraftDestroyed         -= HandleAircraftDestroyed;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Spawns impact particles and plays a random impact sound at the
        /// <see cref="DamageData.impactPoint"/> specified in <paramref name="data"/>.
        /// </summary>
        /// <param name="data">Damage event describing the impact.</param>
        public void PlayImpactEffect(DamageData data)
        {
            if (data == null) return;

            SpawnEffect(sparkEffect,  data.impactPoint);
            SpawnEffect(debrisEffect, data.impactPoint);
            PlayRandomClip(impactSounds);
        }

        /// <summary>
        /// Adjusts persistent visual effects (smoke, fire) to match the
        /// <see cref="DamageLevel"/> of <paramref name="part"/>.
        /// </summary>
        /// <param name="part">Part whose level changed.</param>
        /// <param name="level">New damage level.</param>
        public void UpdateDamageVisuals(AircraftPart part, DamageLevel level)
        {
            switch (level)
            {
                case DamageLevel.None:
                case DamageLevel.Minor:
                    StopEffect(smokeEffect);
                    StopEffect(fireEffect);
                    break;

                case DamageLevel.Moderate:
                    StartEffect(smokeEffect);
                    StopEffect(fireEffect);
                    PlayRandomClip(metalStressClips);
                    break;

                case DamageLevel.Severe:
                case DamageLevel.Critical:
                    StartEffect(smokeEffect);
                    StartEffect(fireEffect);
                    PlayRandomClip(alarmClips);
                    break;

                case DamageLevel.Destroyed:
                    StartEffect(smokeEffect);
                    StartEffect(fireEffect);
                    break;
            }
        }

        /// <summary>Plays an explosion particle effect at <paramref name="position"/>.</summary>
        /// <param name="position">World-space position of the explosion.</param>
        public void PlayExplosion(Vector3 position)
        {
            SpawnEffect(fireEffect,   position);
            SpawnEffect(debrisEffect, position);
            SpawnEffect(sparkEffect,  position);
        }

        #endregion

        #region Event Handlers

        private void HandleDamageReceived(DamageData data)
        {
            PlayImpactEffect(data);
        }

        private void HandlePartLevelChanged(AircraftPart part, DamageLevel level)
        {
            UpdateDamageVisuals(part, level);
        }

        private void HandleAircraftDestroyed()
        {
            PlayExplosion(transform.position);
        }

        #endregion

        #region Helpers

        private void SpawnEffect(ParticleSystem ps, Vector3 worldPos)
        {
            if (ps == null) return;
            ps.transform.position = worldPos;
            ps.Play();
        }

        private void StartEffect(ParticleSystem ps)
        {
            if (ps == null || ps.isPlaying) return;
            ps.Play();
        }

        private void StopEffect(ParticleSystem ps)
        {
            if (ps == null || !ps.isPlaying) return;
            ps.Stop();
        }

        private void PlayRandomClip(AudioClip[] clips)
        {
            if (audioSource == null || clips == null || clips.Length == 0) return;
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip != null)
                audioSource.PlayOneShot(clip);
        }

        #endregion
    }
}
