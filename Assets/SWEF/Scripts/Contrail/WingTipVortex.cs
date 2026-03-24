// WingTipVortex.cs — SWEF Contrail & Exhaust Trail System (Phase 71)
using UnityEngine;

namespace SWEF.Contrail
{
    /// <summary>
    /// Generates wingtip vortex trails during high-G or high-lift manoeuvres.
    ///
    /// <para>Vortex intensity is driven by G-force and speed, with bank angle
    /// providing per-wing weighting: the outer (high-lift) wing produces a
    /// stronger vortex than the inner wing during turns.</para>
    ///
    /// <para>Attach to the aircraft root.  Assign <see cref="leftWingTip"/> and
    /// <see cref="rightWingTip"/> transform references and the corresponding
    /// particle systems / trail renderers.</para>
    /// </summary>
    [AddComponentMenu("SWEF/Contrail/Wing Tip Vortex")]
    public class WingTipVortex : MonoBehaviour
    {
        #region Inspector

        [Header("Wing Tip Transforms")]
        [Tooltip("Transform at the left wingtip where the vortex originates.")]
        /// <summary>Transform at the left wingtip.</summary>
        public Transform leftWingTip;

        [Tooltip("Transform at the right wingtip where the vortex originates.")]
        /// <summary>Transform at the right wingtip.</summary>
        public Transform rightWingTip;

        [Header("Particle Systems")]
        [Tooltip("Particle system for the left wingtip vortex cloud.")]
        /// <summary>Left wingtip vortex particle system.</summary>
        public ParticleSystem leftVortex;

        [Tooltip("Particle system for the right wingtip vortex cloud.")]
        /// <summary>Right wingtip vortex particle system.</summary>
        public ParticleSystem rightVortex;

        [Header("Trail Renderers")]
        [Tooltip("TrailRenderer for the left wingtip vortex ribbon trail.")]
        /// <summary>TrailRenderer for the left wingtip vortex.</summary>
        public TrailRenderer leftTrail;

        [Tooltip("TrailRenderer for the right wingtip vortex ribbon trail.")]
        /// <summary>TrailRenderer for the right wingtip vortex.</summary>
        public TrailRenderer rightTrail;

        [Header("Generation Thresholds")]
        [Tooltip("Minimum G-force below which no vortex trails are generated.")]
        /// <summary>Minimum G-force required to generate vortex trails.</summary>
        [Min(0f)]
        public float minGForceForVortex = ContrailConfig.VortexGForceThreshold;

        [Tooltip("Minimum aircraft speed (m/s) required for vortex generation.")]
        /// <summary>Minimum aircraft speed (m/s) required for vortex generation.</summary>
        [Min(0f)]
        public float minSpeedForVortex = ContrailConfig.VortexMinSpeed;

        [Header("Intensity")]
        [Tooltip("Global multiplier applied to the calculated vortex intensity.")]
        /// <summary>Global vortex intensity multiplier.</summary>
        [Min(0f)]
        public float vortexIntensityScale = 1f;

        [Tooltip("How strongly ambient humidity (0–1) amplifies vortex visibility.")]
        /// <summary>Humidity influence factor on vortex visibility.</summary>
        [Range(0f, 1f)]
        public float humidityInfluence = 0.5f;

        [Header("Appearance")]
        [Tooltip("Color gradient over vortex trail lifetime (white → transparent).")]
        /// <summary>Color gradient over vortex trail lifetime.</summary>
        public Gradient vortexColorGradient;

        [Tooltip("Maximum width (metres) of the vortex TrailRenderer.")]
        /// <summary>Maximum vortex trail width in metres.</summary>
        [Min(0f)]
        public float maxVortexWidth = ContrailConfig.MaxVortexWidth;

        #endregion

        #region Public State

        /// <summary>Current vortex intensity for the left wingtip (0–1).</summary>
        public float leftIntensity  { get; private set; }

        /// <summary>Current vortex intensity for the right wingtip (0–1).</summary>
        public float rightIntensity { get; private set; }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (vortexColorGradient == null || vortexColorGradient.colorKeys.Length == 0)
            {
                vortexColorGradient = new Gradient();
                vortexColorGradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) });
            }

            ApplyTrailGradients();
            SetVorticesActive(false, false);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Recalculates and applies per-wing vortex intensity based on current flight state.
        /// </summary>
        /// <param name="gForce">Current G-force magnitude experienced by the aircraft.</param>
        /// <param name="speed">Aircraft speed in m/s.</param>
        /// <param name="humidity">Ambient relative humidity in [0, 1].</param>
        /// <param name="bankAngle">Aircraft bank angle in degrees; positive = right-wing down.</param>
        public void UpdateVortices(float gForce, float speed, float humidity, float bankAngle)
        {
            bool meetsThresholds = gForce >= minGForceForVortex && speed >= minSpeedForVortex;

            if (!meetsThresholds)
            {
                leftIntensity  = 0f;
                rightIntensity = 0f;
                SetVorticesActive(false, false);
                return;
            }

            // Base intensity from G-force and speed.
            float gFactor    = Mathf.Clamp01((gForce - minGForceForVortex) / minGForceForVortex);
            float speedFactor = Mathf.Clamp01((speed - minSpeedForVortex) / minSpeedForVortex);
            float humFactor   = Mathf.Lerp(1f - humidityInfluence, 1f, humidity);
            float baseIntensity = gFactor * speedFactor * humFactor * vortexIntensityScale;

            // Bank angle distributes load: outer wing (high-lift) gets more intensity.
            float bankNorm = Mathf.Clamp(bankAngle / 90f, -1f, 1f);  // −1 = full left bank, +1 = full right bank
            float rightBonus = Mathf.Clamp01( bankNorm);   // right wing is outer when banking right
            float leftBonus  = Mathf.Clamp01(-bankNorm);   // left wing is outer when banking left

            leftIntensity  = Mathf.Clamp01(baseIntensity * (1f + leftBonus  * 0.5f));
            rightIntensity = Mathf.Clamp01(baseIntensity * (1f + rightBonus * 0.5f));

            ApplyIntensity(leftVortex,  leftTrail,  leftIntensity);
            ApplyIntensity(rightVortex, rightTrail, rightIntensity);

            SetVorticesActive(leftIntensity > 0f, rightIntensity > 0f);
        }

        #endregion

        #region Private Helpers

        private void ApplyIntensity(ParticleSystem ps, TrailRenderer trail, float intensity)
        {
            if (ps != null)
            {
                var emission = ps.emission;
                emission.rateOverTimeMultiplier = intensity * 100f;  // 100 = baseline emission rate
            }

            if (trail != null)
            {
                float width = maxVortexWidth * intensity;
                trail.startWidth = width;
                trail.endWidth   = 0f;
                trail.colorGradient = vortexColorGradient;
            }
        }

        private void SetVorticesActive(bool leftActive, bool rightActive)
        {
            if (leftVortex != null)
            {
                if (leftActive  && !leftVortex.isPlaying)  leftVortex.Play();
                if (!leftActive && leftVortex.isPlaying)   leftVortex.Stop();
            }

            if (rightVortex != null)
            {
                if (rightActive  && !rightVortex.isPlaying) rightVortex.Play();
                if (!rightActive && rightVortex.isPlaying)  rightVortex.Stop();
            }

            if (leftTrail  != null) leftTrail.emitting  = leftActive;
            if (rightTrail != null) rightTrail.emitting = rightActive;
        }

        private void ApplyTrailGradients()
        {
            if (leftTrail  != null) leftTrail.colorGradient  = vortexColorGradient;
            if (rightTrail != null) rightTrail.colorGradient = vortexColorGradient;
        }

        #endregion
    }
}
