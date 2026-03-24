// ContrailEmitter.cs — SWEF Contrail & Exhaust Trail System (Phase 71)
using UnityEngine;

namespace SWEF.Contrail
{
    /// <summary>
    /// Attached to each contrail emission point (engine nozzle, wingtip, smoke outlet).
    ///
    /// <para>Manages a <see cref="TrailRenderer"/> and an optional
    /// <see cref="ParticleSystem"/> to produce a visually convincing trail that
    /// adapts its width, opacity and emission rate to current flight conditions.</para>
    ///
    /// <para>Register this emitter with <see cref="ContrailManager"/> at runtime via
    /// <see cref="ContrailManager.RegisterEmitter"/>.</para>
    /// </summary>
    [AddComponentMenu("SWEF/Contrail/Contrail Emitter")]
    public class ContrailEmitter : MonoBehaviour
    {
        #region Inspector

        [Header("Identity")]
        [Tooltip("Classifies the type of trail this emitter produces.")]
        /// <summary>Classifies the type of trail this emitter produces.</summary>
        public ContrailType trailType = ContrailType.Condensation;

        [Header("Emission Point")]
        [Tooltip("World-space transform at which the trail originates. Defaults to this Transform if null.")]
        /// <summary>World-space position where the trail is emitted. Defaults to this Transform.</summary>
        public Transform emitPoint;

        [Header("Trail Components")]
        [Tooltip("Unity TrailRenderer used to render the continuous ribbon trail.")]
        /// <summary>Unity TrailRenderer used to render the continuous ribbon trail.</summary>
        public TrailRenderer trailRenderer;

        [Tooltip("Supplementary particle system for volumetric trail appearance. May be null.")]
        /// <summary>Supplementary particle system for volumetric appearance. Optional.</summary>
        public ParticleSystem trailParticles;

        [Header("Width")]
        [Tooltip("Trail width (metres) at the emission point.")]
        /// <summary>Trail width (metres) at the emission point.</summary>
        [Min(0f)]
        public float baseWidth = ContrailConfig.BaseContrailWidth;

        [Tooltip("Trail width (metres) at its maximum expansion point.")]
        /// <summary>Trail width (metres) at the far end of the trail.</summary>
        [Min(0f)]
        public float endWidth = 5f;

        [Tooltip("How much the trail width increases per m/s of aircraft speed beyond the minimum trail speed.")]
        /// <summary>Width increase (metres) per m/s above <see cref="minSpeedForTrail"/>.</summary>
        [Min(0f)]
        public float widthBySpeedMultiplier = 0.01f;

        [Header("Speed Gate")]
        [Tooltip("Minimum aircraft speed (m/s) below which no trail is emitted.")]
        /// <summary>Minimum aircraft speed (m/s) required to emit a trail.</summary>
        [Min(0f)]
        public float minSpeedForTrail = ContrailConfig.MinTrailSpeed;

        [Header("Color Gradients")]
        [Tooltip("Color over trail lifetime for condensation / normal trails (white → transparent).")]
        /// <summary>Gradient applied to the TrailRenderer for normal contrail trails.</summary>
        public Gradient normalGradient;

        [Tooltip("Color over trail lifetime for exhaust trails (orange → dark grey → transparent).")]
        /// <summary>Gradient applied to the TrailRenderer for exhaust trails.</summary>
        public Gradient exhaustGradient;

        [Header("Particles")]
        [Tooltip("Particles emitted per second when the emitter is active.")]
        /// <summary>Particles emitted per second when the emitter is active.</summary>
        [Min(0f)]
        public float emissionRate = 100f;

        #endregion

        #region Public State

        /// <summary><c>true</c> while the emitter is actively producing a trail.</summary>
        public bool isEmitting { get; private set; }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (emitPoint == null)
                emitPoint = transform;

            InitDefaultGradients();

            if (trailRenderer != null)
                ApplyGradient();

            StopEmitting();
        }

        private void OnEnable()
        {
            ContrailManager manager = ContrailManager.Instance;
            if (manager != null)
                manager.RegisterEmitter(this);
        }

        private void OnDisable()
        {
            ContrailManager manager = ContrailManager.Instance;
            if (manager != null)
                manager.UnregisterEmitter(this);
        }

        #endregion

        #region Public API

        /// <summary>Starts emitting the trail effect.</summary>
        public void StartEmitting()
        {
            isEmitting = true;

            if (trailRenderer != null)
                trailRenderer.emitting = true;

            if (trailParticles != null)
            {
                var emission = trailParticles.emission;
                emission.enabled = true;
                trailParticles.Play();
            }
        }

        /// <summary>Stops emitting new trail segments. Existing segments continue to fade normally.</summary>
        public void StopEmitting()
        {
            isEmitting = false;

            if (trailRenderer != null)
                trailRenderer.emitting = false;

            if (trailParticles != null)
            {
                var emission = trailParticles.emission;
                emission.enabled = false;
            }
        }

        /// <summary>
        /// Called each update tick by <see cref="ContrailManager"/> to adapt the trail
        /// to current flight conditions.
        /// </summary>
        /// <param name="speed">Aircraft speed in m/s.</param>
        /// <param name="throttle">Normalised throttle in [0, 1].</param>
        /// <param name="altitude">Aircraft altitude in metres.</param>
        /// <param name="temperature">Ambient air temperature in °C.</param>
        public void UpdateEmission(float speed, float throttle, float altitude, float temperature)
        {
            if (speed < minSpeedForTrail)
            {
                if (isEmitting)
                    StopEmitting();
                return;
            }

            if (!isEmitting)
                StartEmitting();

            if (trailRenderer != null)
                UpdateTrailRenderer(speed, throttle);

            if (trailParticles != null)
                UpdateParticleEmission(throttle);
        }

        #endregion

        #region Private Helpers

        private void UpdateTrailRenderer(float speed, float throttle)
        {
            float speedBonus = Mathf.Max(0f, speed - minSpeedForTrail) * widthBySpeedMultiplier;
            float currentBase = baseWidth + speedBonus;
            float currentEnd  = Mathf.Lerp(endWidth, ContrailConfig.MaxContrailWidth, throttle) + speedBonus * 0.5f;

            trailRenderer.startWidth = currentBase;
            trailRenderer.endWidth   = currentEnd;

            Gradient gradient = trailType == ContrailType.Exhaust ? exhaustGradient : normalGradient;
            trailRenderer.colorGradient = gradient;
        }

        private void UpdateParticleEmission(float throttle)
        {
            var emission = trailParticles.emission;
            var rate     = emission.rateOverTime;
            rate.constant = emissionRate * Mathf.Lerp(0.5f, 1f, throttle);
            emission.rateOverTime = rate;
        }

        private void InitDefaultGradients()
        {
            if (normalGradient == null || normalGradient.colorKeys.Length == 0)
            {
                normalGradient = new Gradient();
                normalGradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(ContrailConfig.ContrailStartColor.a, 0f), new GradientAlphaKey(0f, 1f) });
            }

            if (exhaustGradient == null || exhaustGradient.colorKeys.Length == 0)
            {
                exhaustGradient = new Gradient();
                exhaustGradient.SetKeys(
                    new[]
                    {
                        new GradientColorKey(new Color(1f, 0.6f, 0.2f), 0f),
                        new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 1f)
                    },
                    new[] { new GradientAlphaKey(ContrailConfig.ExhaustStartColor.a, 0f), new GradientAlphaKey(0f, 1f) });
            }
        }

        private void ApplyGradient()
        {
            Gradient gradient = trailType == ContrailType.Exhaust ? exhaustGradient : normalGradient;
            trailRenderer.colorGradient = gradient;
        }

        #endregion
    }
}
