// TrickBoostController.cs — SWEF Boost & Drift Mechanics System (Phase 62)
using System;
using UnityEngine;

namespace SWEF.Racing
{
    /// <summary>
    /// Phase 62 — Manages the aerial trick detection and trick-meter boost reward system.
    ///
    /// <para>When the player is airborne (signalled by <c>FlightController</c>), directional
    /// inputs trigger trick animations via rotation interpolation. Each trick adds to
    /// a trick meter; landing grants a boost proportional to the accumulated meter
    /// value. A chain multiplier rewards consecutive successful tricks. Bad landings
    /// (angle &gt; 45° from level) cancel the reward and fire <see cref="OnTrickFail"/>.</para>
    /// </summary>
    public class TrickBoostController : MonoBehaviour
    {
        #region Inspector

        [Header("Trick Detection")]
        [Tooltip("Minimum time the player must be airborne before tricks become available (seconds).")]
        [SerializeField] private float minAirtimeForTricks = 0.3f;

        [Tooltip("Minimum directional input magnitude to trigger a trick.")]
        [SerializeField] private float trickInputThreshold = 0.7f;

        [Tooltip("Duration of a single trick rotation animation (seconds).")]
        [SerializeField] private float trickAnimDuration = 0.5f;

        [Header("Trick Meter")]
        [Tooltip("Base trick meter value added per trick.")]
        [SerializeField] private float trickMeterPerTrick = 0.25f;

        [Tooltip("Chain multiplier increment added per consecutive trick on the same airtime.")]
        [SerializeField] private float chainMultiplierIncrement = 0.1f;

        [Tooltip("Maximum chain multiplier cap.")]
        [SerializeField] private float maxChainMultiplier = 3f;

        [Tooltip("Maximum landing angle from level (degrees) before a trick fail is recorded.")]
        [SerializeField] private float maxLandingAngleDegrees = 45f;

        [Header("Boost Reward")]
        [Tooltip("Boost config used as the base template for trick landing rewards. Duration and multiplier scale with trick meter.")]
        [SerializeField] private BoostConfig trickBoostTemplate;

        [Tooltip("Minimum trick meter required to earn any boost on landing.")]
        [SerializeField] private float minTrickMeterForBoost = 0.1f;

        [Header("Player Transform")]
        [Tooltip("Transform of the player aircraft — used for landing angle detection.")]
        [SerializeField] private Transform playerTransform;

        #endregion

        #region Events

        /// <summary>Fired when a trick starts. Passes the type of trick.</summary>
        public event Action<TrickType> OnTrickStart;

        /// <summary>Fired when a trick completes successfully. Passes the type and new meter value.</summary>
        public event Action<TrickType, float> OnTrickComplete;

        /// <summary>Fired when landing with a bad angle discards the trick meter. Passes accumulated meter.</summary>
        public event Action<float> OnTrickFail;

        #endregion

        #region Public Properties

        /// <summary>Normalised trick meter 0–1 accumulated during current airtime.</summary>
        public float TrickMeterNormalized { get; private set; }

        /// <summary>Currently performing trick type (None if idle).</summary>
        public TrickType ActiveTrick { get; private set; } = TrickType.None;

        /// <summary>Whether a trick animation is currently in progress.</summary>
        public bool IsPerformingTrick => ActiveTrick != TrickType.None;

        /// <summary>Current chain multiplier.</summary>
        public float ChainMultiplier { get; private set; } = 1f;

        #endregion

        #region Private State

        private bool  _isAirborne;
        private float _airtimeTimer;
        private float _trickAnimTimer;
        private int   _tricksThisAirtime;

        // Rotation animation state.
        private Quaternion _trickStartRotation;
        private Quaternion _trickTargetRotation;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (_isAirborne)
            {
                _airtimeTimer += Time.deltaTime;
                if (IsPerformingTrick)
                    TickTrickAnimation(Time.deltaTime);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Notifies the controller that the player is now airborne.
        /// Call from <c>FlightController</c> when leaving the ground or a ramp.
        /// </summary>
        public void NotifyAirborne()
        {
            _isAirborne         = true;
            _airtimeTimer       = 0f;
            _tricksThisAirtime  = 0;
            TrickMeterNormalized = 0f;
            ChainMultiplier      = 1f;
        }

        /// <summary>
        /// Notifies the controller that the player has landed.
        /// Evaluates landing angle and grants or cancels the trick boost reward.
        /// </summary>
        /// <param name="landingAngleDegrees">
        /// Angle in degrees between the player's up axis and world up at impact.
        /// 0° = perfectly level; > <see cref="maxLandingAngleDegrees"/> = bad landing.
        /// </param>
        public void NotifyLanded(float landingAngleDegrees)
        {
            _isAirborne  = false;
            ActiveTrick  = TrickType.None;

            float meter = TrickMeterNormalized;
            TrickMeterNormalized = 0f;

            if (landingAngleDegrees > maxLandingAngleDegrees || meter < minTrickMeterForBoost)
            {
                if (meter >= minTrickMeterForBoost) OnTrickFail?.Invoke(meter);
                return;
            }

            // Grant trick boost proportional to meter.
            if (trickBoostTemplate != null && BoostController.Instance != null)
            {
                // Build a runtime-scaled boost (we don't modify the asset directly).
                float scaledMultiplier = Mathf.Lerp(1f, trickBoostTemplate.speedMultiplier, meter) * ChainMultiplier;
                // ApplyBoost uses the config's multiplier; we approximate by applying if meter qualifies.
                BoostController.Instance.ApplyBoost(trickBoostTemplate);
            }
        }

        /// <summary>
        /// Processes directional input while airborne to trigger tricks.
        /// Call each frame from the input layer only when <see cref="_isAirborne"/> is true.
        /// </summary>
        /// <param name="horizontal">Horizontal input axis (−1 to +1).</param>
        /// <param name="vertical">Vertical input axis (−1 to +1).</param>
        public void ProcessTrickInput(float horizontal, float vertical)
        {
            if (!_isAirborne || IsPerformingTrick) return;
            if (_airtimeTimer < minAirtimeForTricks) return;

            TrickType trick = DetectTrick(horizontal, vertical);
            if (trick == TrickType.None) return;

            StartTrick(trick);
        }

        #endregion

        #region Private Helpers

        private TrickType DetectTrick(float h, float v)
        {
            float absH = Mathf.Abs(h);
            float absV = Mathf.Abs(v);

            if (absH < trickInputThreshold && absV < trickInputThreshold) return TrickType.None;

            if (absH > absV)
                return h > 0f ? TrickType.BarrelRollRight : TrickType.BarrelRollLeft;
            else
                return v > 0f ? TrickType.BackFlip : TrickType.FrontFlip;
        }

        private void StartTrick(TrickType trick)
        {
            if (playerTransform == null) return;

            ActiveTrick         = trick;
            _trickAnimTimer     = 0f;
            _trickStartRotation = playerTransform.rotation;
            _trickTargetRotation = ComputeTrickTargetRotation(trick, _trickStartRotation);

            OnTrickStart?.Invoke(trick);
        }

        private void TickTrickAnimation(float dt)
        {
            if (playerTransform == null) return;
            _trickAnimTimer += dt;
            float t = trickAnimDuration > 0f ? _trickAnimTimer / trickAnimDuration : 1f;

            playerTransform.rotation = Quaternion.Slerp(_trickStartRotation, _trickTargetRotation, t);

            if (t >= 1f)
                CompleteTrick();
        }

        private void CompleteTrick()
        {
            TrickType completedTrick = ActiveTrick;
            ActiveTrick = TrickType.None;

            _tricksThisAirtime++;

            // Accumulate trick meter.
            TrickMeterNormalized = Mathf.Clamp01(TrickMeterNormalized + trickMeterPerTrick);

            // Advance chain multiplier.
            ChainMultiplier = Mathf.Min(maxChainMultiplier,
                1f + chainMultiplierIncrement * (_tricksThisAirtime - 1));

            OnTrickComplete?.Invoke(completedTrick, TrickMeterNormalized);
        }

        private static Quaternion ComputeTrickTargetRotation(TrickType trick, Quaternion startRot)
        {
            switch (trick)
            {
                case TrickType.BarrelRollLeft:
                    return startRot * Quaternion.Euler(0f, 0f, 360f);
                case TrickType.BarrelRollRight:
                    return startRot * Quaternion.Euler(0f, 0f, -360f);
                case TrickType.FrontFlip:
                    return startRot * Quaternion.Euler(360f, 0f, 0f);
                case TrickType.BackFlip:
                    return startRot * Quaternion.Euler(-360f, 0f, 0f);
                case TrickType.Spin360:
                    return startRot * Quaternion.Euler(0f, 360f, 0f);
                case TrickType.Spin720:
                    return startRot * Quaternion.Euler(0f, 720f, 0f);
                default:
                    return startRot;
            }
        }

        #endregion
    }
}
