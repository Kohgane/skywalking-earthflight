// BuoyancyController.cs — SWEF Phase 74: Water Interaction & Buoyancy System
using System;
using UnityEngine;

namespace SWEF.Water
{
    /// <summary>
    /// Phase 74 — Per-aircraft MonoBehaviour that drives physics-based buoyancy
    /// when the aircraft is on or in the water.
    ///
    /// <para>State machine: Airborne → Skimming → Touching → Floating / Sinking → Submerged</para>
    ///
    /// <para>Null-safe integration points:</para>
    /// <list type="bullet">
    ///   <item><see cref="SWEF.Damage.DamageModel"/> — impact damage + water ingress over time.</item>
    ///   <item><see cref="SWEF.Landing.LandingDetector"/> — ditching touchdown confirmation.</item>
    /// </list>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class BuoyancyController : MonoBehaviour
    {
        #region Inspector

        [Header("Buoyancy Settings")]
        [Tooltip("Half-length of the aircraft used to compute wave rocking torque (m).")]
        [SerializeField] private float aircraftHalfLength = 10f;

        [Tooltip("Speed threshold (m/s) below which Touching transitions to Floating.")]
        [SerializeField] private float floatSpeedThreshold = 15f;

        [Tooltip("Maximum pitch angle (degrees) for a controlled ditching (shallow enough to float).")]
        [SerializeField] private float maxFloatPitchAngle = 15f;

        [Tooltip("Wave rocking torque scale multiplier.")]
        [SerializeField] private float waveRockingScale = 0.5f;

        [Header("Ditching")]
        [Tooltip("Target speed (m/s) at end of controlled ditching flare.")]
        [SerializeField] private float ditchingTargetSpeed = 30f;

        [Tooltip("Rate (m/s²) at which altitude is reduced during ditching approach.")]
        [SerializeField] private float ditchingDescentRate = 2f;

        [Header("Damage Integration")]
        [Tooltip("Minimum impact force (N) that triggers structural damage.")]
        [SerializeField] private float impactDamageThreshold = 500f;

        [Tooltip("Water ingress damage per second while fully submerged.")]
        [SerializeField] private float ingressDamagePerSecond = 5f;

        [Header("Debug")]
        [SerializeField] private bool showGizmos = true;

        #endregion

        #region Events

        /// <summary>Fired on each water contact event with a <see cref="SplashEvent"/> payload.</summary>
        public event Action<SplashEvent> OnWaterContact;

        /// <summary>Fired when the buoyancy state machine transitions to a new state.</summary>
        public event Action<WaterContactState> OnStateChanged;

        /// <summary>Fired when a controlled ditching sequence completes successfully.</summary>
        public event Action OnDitchingComplete;

        /// <summary>Fired when the aircraft begins sinking below the surface.</summary>
        public event Action OnSinking;

        #endregion

        #region Constants

        /// <summary>Standard seawater density (kg/m³) used as the buoyancy normalisation baseline.</summary>
        private const float StandardSeawaterDensity = 1025f;

        #endregion

        #region Public Properties

        /// <summary>Current live buoyancy state.</summary>
        public BuoyancyState State { get; private set; } = new BuoyancyState();

        /// <summary>Returns the fractional submersion depth [0–1], where 1 = fully submerged.</summary>
        public float GetSubmersionPercent()
        {
            if (_config == null) return 0f;
            return Mathf.Clamp01(State.submersionDepth / aircraftHalfLength);
        }

        /// <summary>Returns <c>true</c> if the aircraft still has enough buoyancy to float.</summary>
        public bool CanFloat()
        {
            return State.contactState != WaterContactState.Sinking
                && State.contactState != WaterContactState.Submerged
                && GetSubmersionPercent() < 0.9f;
        }

        #endregion

        #region Private State

        private Rigidbody _rb;
        private WaterConfig _config;
        private bool _ditchingActive;
        private float _splashCooldown;

        // Null-safe cross-system component references
        private Component _damageModel;
        private Component _landingDetector;
        private bool _crossSystemCacheDone;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            _config = WaterSurfaceManager.Instance != null
                ? WaterSurfaceManager.Instance.Config
                : new WaterConfig();

            CacheCrossSystemReferences();
        }

        private void FixedUpdate()
        {
            if (_config == null) return;

            _splashCooldown -= Time.fixedDeltaTime;

            float waterHeight = WaterSurfaceManager.Instance != null
                ? WaterSurfaceManager.Instance.GetWaterHeight(transform.position)
                : _config.waterLevel;

            float depth = waterHeight - transform.position.y;
            UpdateState(depth);
            ApplyForces(depth, waterHeight);
            HandleDamageIntegration(depth);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Initiates a controlled ditching sequence — gradually reduces altitude, flares
        /// the nose, and decelerates to floating speed.
        /// </summary>
        public void InitiateDitching()
        {
            if (_ditchingActive) return;
            _ditchingActive = true;
            TransitionTo(WaterContactState.Ditching);
        }

        #endregion

        #region State Machine

        private void UpdateState(float depth)
        {
            WaterContactState prev = State.contactState;
            WaterContactState next = prev;

            float altitude = -depth; // positive = above water
            float speed = _rb.linearVelocity.magnitude;

            switch (prev)
            {
                case WaterContactState.Airborne:
                    if (altitude < _config.skimAltitudeThreshold) next = WaterContactState.Skimming;
                    break;

                case WaterContactState.Skimming:
                    if (altitude > _config.skimAltitudeThreshold) next = WaterContactState.Airborne;
                    else if (depth > 0f) next = WaterContactState.Touching;
                    break;

                case WaterContactState.Touching:
                    if (depth <= 0f) { next = WaterContactState.Skimming; break; }
                    float pitchAngle = Mathf.Abs(transform.eulerAngles.x);
                    if (pitchAngle > 180f) pitchAngle = 360f - pitchAngle;
                    bool shallowAngle = pitchAngle < maxFloatPitchAngle;
                    if (speed < floatSpeedThreshold && shallowAngle)
                        next = WaterContactState.Floating;
                    else if (speed >= floatSpeedThreshold || !shallowAngle)
                        next = WaterContactState.Sinking;
                    break;

                case WaterContactState.Floating:
                    if (depth <= 0f) { next = WaterContactState.Skimming; break; }
                    if (!CanFloat()) next = WaterContactState.Sinking;
                    break;

                case WaterContactState.Sinking:
                    if (depth > aircraftHalfLength * 2f) next = WaterContactState.Submerged;
                    break;

                case WaterContactState.Submerged:
                    if (depth < 0f) next = WaterContactState.Touching;
                    break;

                case WaterContactState.Ditching:
                    // Ditching resolves in ApplyForces
                    break;
            }

            if (next != prev)
                TransitionTo(next);

            // Update time counters
            if (State.contactState != WaterContactState.Airborne)
                State.timeInWater += Time.fixedDeltaTime;

            if (State.contactState == WaterContactState.Submerged)
                State.timeSubmerged += Time.fixedDeltaTime;

            State.submersionDepth = Mathf.Max(0f, depth);
        }

        private void TransitionTo(WaterContactState next)
        {
            WaterContactState prev = State.contactState;
            State.contactState = next;
            OnStateChanged?.Invoke(next);

            if ((prev == WaterContactState.Skimming || prev == WaterContactState.Airborne)
                && (next == WaterContactState.Touching || next == WaterContactState.Floating))
            {
                FireSplashEvent();
            }

            if (next == WaterContactState.Sinking)
            {
                OnSinking?.Invoke();
                _ditchingActive = false;
            }

            if (next == WaterContactState.Ditching)
                _ditchingActive = true;
        }

        #endregion

        #region Physics

        private void ApplyForces(float depth, float waterHeight)
        {
            if (depth <= 0f && State.contactState == WaterContactState.Airborne)
                return;

            WaterSurfaceManager wsm = WaterSurfaceManager.Instance;
            Vector3 surfaceNormal = wsm != null
                ? wsm.GetSurfaceNormal(transform.position)
                : Vector3.up;

            float submergedFraction = GetSubmersionPercent();

            // Archimedes buoyancy — upward force proportional to submerged volume
            if (submergedFraction > 0f)
            {
                float buoyancy = _config.buoyancyForce * submergedFraction * _config.waterDensity / StandardSeawaterDensity;
                State.buoyancyForceMagnitude = buoyancy;
                _rb.AddForce(Vector3.up * buoyancy, ForceMode.Acceleration);
            }

            // Water drag — opposes velocity, scales with submersion
            if (submergedFraction > 0f)
            {
                Vector3 drag = -_rb.linearVelocity * _config.dragCoefficient * submergedFraction;
                State.dragForceMagnitude = drag.magnitude;
                _rb.AddForce(drag, ForceMode.Acceleration);

                // Angular damping
                _rb.angularVelocity = Vector3.Lerp(_rb.angularVelocity, Vector3.zero, 5f * Time.fixedDeltaTime);
            }

            // Wave rocking torque
            if (submergedFraction > 0f && wsm != null)
            {
                Vector3 frontPos = transform.position + transform.forward * aircraftHalfLength;
                Vector3 backPos  = transform.position - transform.forward * aircraftHalfLength;
                Vector3 frontNorm = wsm.GetSurfaceNormal(frontPos);
                Vector3 backNorm  = wsm.GetSurfaceNormal(backPos);
                Vector3 normalDiff = frontNorm - backNorm;
                _rb.AddTorque(normalDiff * waveRockingScale, ForceMode.Acceleration);
            }

            // Current drift
            if (submergedFraction > 0f)
            {
                _rb.AddForce(State.waterVelocity * submergedFraction * 0.1f, ForceMode.Acceleration);
            }

            // Controlled ditching
            if (_ditchingActive && State.contactState == WaterContactState.Ditching)
            {
                float speed = _rb.linearVelocity.magnitude;
                // Gradually reduce altitude
                if (transform.position.y > waterHeight + 0.2f)
                {
                    _rb.AddForce(Vector3.down * ditchingDescentRate, ForceMode.Acceleration);
                }
                // Decelerate to floating speed
                if (speed > ditchingTargetSpeed)
                {
                    _rb.AddForce(-_rb.linearVelocity.normalized * 2f, ForceMode.Acceleration);
                }
                // Flare nose up gently
                Quaternion targetRot = Quaternion.Euler(5f, transform.eulerAngles.y, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * 0.5f);

                // Complete ditching when slow enough and near surface
                if (speed < floatSpeedThreshold * 0.8f && Mathf.Abs(transform.position.y - waterHeight) < 1f)
                {
                    _ditchingActive = false;
                    TransitionTo(WaterContactState.Floating);
                    OnDitchingComplete?.Invoke();
                }
            }

            // Passive sinking
            if (State.contactState == WaterContactState.Sinking)
            {
                _rb.AddForce(Vector3.down * _config.sinkRate, ForceMode.Acceleration);
            }

            State.isStable = State.contactState == WaterContactState.Floating
                && _rb.linearVelocity.magnitude < 2f
                && _rb.angularVelocity.magnitude < 0.1f;
        }

        #endregion

        #region Splash

        private void FireSplashEvent()
        {
            if (_splashCooldown > 0f) return;
            _splashCooldown = _config.splashCooldown;

            float speed = _rb.linearVelocity.magnitude;
            SplashType type;
            if (speed < 10f)       type = SplashType.LightSpray;
            else if (speed < 40f)  type = SplashType.MediumSplash;
            else                   type = SplashType.HeavySplash;

            float pitchAngle = transform.eulerAngles.x;
            if (pitchAngle > 180f) pitchAngle = 360f - pitchAngle;
            if (pitchAngle > 30f)  type = SplashType.DiveEntry;
            else if (pitchAngle < 5f && speed > 30f) type = SplashType.BellyFlop;

            var evt = new SplashEvent
            {
                type        = type,
                position    = transform.position,
                velocity    = _rb.linearVelocity,
                impactForce = _rb.linearVelocity.magnitude * _rb.mass,
                timestamp   = Time.time,
            };

            OnWaterContact?.Invoke(evt);
        }

        #endregion

        #region Damage Integration

        private void HandleDamageIntegration(float depth)
        {
            if (_damageModel == null) return;

            // Impact damage on first hard contact
            if (State.contactState == WaterContactState.Touching)
            {
                float force = _rb.linearVelocity.magnitude * _rb.mass;
                if (force > impactDamageThreshold)
                {
                    TryApplyDamage((force - impactDamageThreshold) * 0.01f);
                }
            }

            // Water ingress damage while submerged
            if (State.contactState == WaterContactState.Submerged || State.contactState == WaterContactState.Sinking)
            {
                TryApplyDamage(ingressDamagePerSecond * Time.fixedDeltaTime);
            }
        }

        private void TryApplyDamage(float amount)
        {
            if (_damageModel == null) return;
            try
            {
                var method = _damageModel.GetType().GetMethod("ApplyDamage");
                method?.Invoke(_damageModel, new object[] { amount });
            }
            catch { }
        }

        #endregion

        #region Cross-System Cache

        private void CacheCrossSystemReferences()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var dmType = assembly.GetType("SWEF.Damage.DamageModel");
                if (dmType != null) _damageModel = GetComponent(dmType) as Component;

                var ldType = assembly.GetType("SWEF.Landing.LandingDetector");
                if (ldType != null) _landingDetector = GetComponent(ldType) as Component;
            }
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawLine(transform.position - transform.forward * aircraftHalfLength,
                            transform.position + transform.forward * aircraftHalfLength);
        }

        #endregion
    }
}
