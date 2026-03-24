// RadarJammer.cs — SWEF Radar & Threat Detection System (Phase 67)
using System;
using System.Collections;
using UnityEngine;

namespace SWEF.Radar
{
    /// <summary>
    /// Electronic countermeasures component that can jam nearby enemy radar
    /// systems and deploy chaff / flare bundles.
    /// <para>
    /// Attach to the player aircraft or any object that should have ECM capability.
    /// </para>
    /// </summary>
    public class RadarJammer : MonoBehaviour
    {
        #region Inspector

        [Header("Jammer — State")]
        [Tooltip("Whether the jammer is currently active.")]
        /// <summary>Whether the jammer is currently transmitting.</summary>
        public bool isJamming;

        [Header("Jammer — Parameters")]
        [Tooltip("Effective jamming radius in metres.")]
        [Min(1f)]
        /// <summary>Effective jamming radius in metres.</summary>
        public float jamRange = RadarConfig.JamRange;

        [Tooltip("Jamming effectiveness (0–1).  Reduces enemy radar detection probability.")]
        [Range(0f, 1f)]
        /// <summary>
        /// Jamming effectiveness normalised to [0, 1].  At 1.0 all enemy radar
        /// detections within <see cref="jamRange"/> are blocked.
        /// </summary>
        public float jamEffectiveness = RadarConfig.JamEffectiveness;

        [Tooltip("Power consumed per second while jamming (fuel/power units).")]
        [Min(0f)]
        /// <summary>Power or fuel consumed per second while the jammer is active.</summary>
        public float powerConsumption = 0.1f;

        [Tooltip("Cooldown in seconds before the jammer can be reactivated after deactivation.")]
        [Min(0f)]
        /// <summary>Cooldown in seconds after deactivation before the jammer can be reactivated.</summary>
        public float cooldownTime = RadarConfig.JamCooldown;

        [Header("Countermeasures")]
        [Tooltip("Number of chaff bundles remaining.")]
        [Min(0)]
        /// <summary>Number of chaff bundles remaining.</summary>
        public int chaffCount = 10;

        [Tooltip("Number of flare cartridges remaining.")]
        [Min(0)]
        /// <summary>Number of flare cartridges remaining.</summary>
        public int flareCount = 10;

        [Header("Jammer — References")]
        [Tooltip("Particle system played when chaff is deployed.  Optional.")]
        /// <summary>Optional particle effect played on chaff deployment.</summary>
        [SerializeField] private ParticleSystem _chaffEffect;

        [Tooltip("Particle system played when flares are deployed.  Optional.")]
        /// <summary>Optional particle effect played on flare deployment.</summary>
        [SerializeField] private ParticleSystem _flareEffect;

        #endregion

        #region Runtime State

        /// <summary>Whether the jammer is on cooldown and cannot be activated.</summary>
        public bool isOnCooldown { get; private set; }

        /// <summary>Remaining cooldown time in seconds (0 when ready).</summary>
        public float cooldownRemaining { get; private set; }

        private Coroutine _cooldownRoutine;

        #endregion

        #region Events

        /// <summary>Raised when the jammer is toggled on or off.  Parameter is the new state.</summary>
        public event Action<bool> OnJammerToggled;

        /// <summary>Raised when chaff is deployed.  Parameter is the remaining chaff count.</summary>
        public event Action<int> OnChaffDeployed;

        /// <summary>Raised when flares are deployed.  Parameter is the remaining flare count.</summary>
        public event Action<int> OnFlareDeployed;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (isJamming)
                ConsumeResources();

            if (isOnCooldown && cooldownRemaining > 0f)
                cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (_cooldownRoutine != null) StopCoroutine(_cooldownRoutine);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Toggles the jammer on or off.
        /// <para>
        /// Activation is ignored while on cooldown.  Deactivation starts the
        /// <see cref="cooldownTime"/> timer.
        /// </para>
        /// </summary>
        public void ToggleJammer()
        {
            if (!isJamming && isOnCooldown) return; // Cannot activate during cooldown.

            isJamming = !isJamming;
            OnJammerToggled?.Invoke(isJamming);

            if (!isJamming)
                BeginCooldown();
        }

        /// <summary>
        /// Deploys a chaff bundle that temporarily reduces the radar signature of
        /// this object.  Has no effect when <see cref="chaffCount"/> is zero.
        /// </summary>
        public void DeployChaffs()
        {
            if (chaffCount <= 0) return;
            chaffCount--;

            if (_chaffEffect != null)
                _chaffEffect.Play();

            OnChaffDeployed?.Invoke(chaffCount);
        }

        /// <summary>
        /// Deploys a flare cartridge to distract heat-seeking threats.
        /// Has no effect when <see cref="flareCount"/> is zero.
        /// </summary>
        public void DeployFlares()
        {
            if (flareCount <= 0) return;
            flareCount--;

            if (_flareEffect != null)
                _flareEffect.Play();

            OnFlareDeployed?.Invoke(flareCount);
        }

        #endregion

        #region Private

        private void ConsumeResources()
        {
            // TODO: subtract powerConsumption * Time.deltaTime from an external
            // resource/fuel system when SWEF_FLIGHTCONTROLLER_AVAILABLE is defined.
        }

        private void BeginCooldown()
        {
            isOnCooldown    = true;
            cooldownRemaining = cooldownTime;

            if (_cooldownRoutine != null) StopCoroutine(_cooldownRoutine);
            _cooldownRoutine = StartCoroutine(CooldownRoutine());
        }

        private IEnumerator CooldownRoutine()
        {
            yield return new WaitForSeconds(cooldownTime);
            isOnCooldown     = false;
            cooldownRemaining = 0f;
        }

        #endregion
    }
}
