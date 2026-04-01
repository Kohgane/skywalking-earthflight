using System;
using UnityEngine;

namespace SWEF.PassengerCargo
{
    /// <summary>
    /// Singleton MonoBehaviour that tracks cargo weight effects on the flight model.
    ///
    /// Exposes computed values (<see cref="TotalMassKg"/>,
    /// <see cref="FuelMultiplier"/>, <see cref="CGShiftMetres"/>) that other
    /// flight systems can read. Damage tracking monitors fragile cargo from
    /// G-force spikes and turbulence.
    ///
    /// Integration note: <c>AeroPhysicsModel</c> and <c>FuelConsumptionModel</c>
    /// do not yet expose external-override properties; values are available for
    /// future integration or custom flight-model extensions.
    /// </summary>
    public class CargoPhysicsController : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static CargoPhysicsController Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Mass / CG")]
        [Tooltip("Base aircraft mass in kg (without payload).")]
        [SerializeField] private float baseMassKg              = 8000f;

        [Tooltip("Maximum CG shift (metres) when cargo is fully aft-loaded.")]
        [SerializeField] private float maxCGShiftMetres        = 0.6f;

        [Header("Fuel Multiplier")]
        [Tooltip("Fuel consumption multiplier added per 1000 kg of payload.")]
        [SerializeField] private float fuelMultiplierPer1000kg = 0.08f;

        [Header("Damage")]
        [Tooltip("G-force above which fragile cargo starts taking damage.")]
        [SerializeField] private float fragileGForceThreshold  = 1.8f;

        [Tooltip("Turbulence intensity above which fragile cargo takes damage.")]
        [SerializeField] private float fragileTurbThreshold    = 0.4f;

        [Tooltip("Vertical speed (m/s) on landing that causes fragile damage.")]
        [SerializeField] private float hardLandingThreshold    = 4f;

        // ── Events ────────────────────────────────────────────────────────────
        /// <summary>Fired when cargo receives damage. Parameter: cumulative damage %.</summary>
        public event Action<float> OnCargoDamaged;

        /// <summary>Fired when cumulative damage reaches 100 %.</summary>
        public event Action OnCargoDestroyed;

        // ── State ─────────────────────────────────────────────────────────────
        private CargoManifest _manifest;
        private float         _cargoDamagePercent;
        private bool          _cargoActive;
        private bool          _destroyed;

        // Latest G-force sampled from FlightPhysicsIntegrator snapshot.
        private float _latestGForce;

        private SWEF.Flight.FlightPhysicsIntegrator _physicsIntegrator;

        // ── Properties ────────────────────────────────────────────────────────
        /// <summary>Total aircraft mass including current payload (kg).</summary>
        public float TotalMassKg =>
            _manifest != null ? baseMassKg + _manifest.weight : baseMassKg;

        /// <summary>
        /// Fuel consumption multiplier from current payload weight (1.0 = no effect).
        /// </summary>
        public float FuelMultiplier
        {
            get
            {
                if (_manifest == null) return 1f;
                float extra = (_manifest.weight / 1000f) * fuelMultiplierPer1000kg;
                return 1f + extra;
            }
        }

        /// <summary>
        /// Centre-of-gravity shift in metres (+ve = aft, −ve = forward).
        /// </summary>
        public float CGShiftMetres
        {
            get
            {
                if (_manifest == null) return 0f;
                float cgFraction = Mathf.Clamp01(_manifest.volume /
                                                  (_manifest.weight / 100f));
                return Mathf.Lerp(-maxCGShiftMetres, maxCGShiftMetres, cgFraction);
            }
        }

        /// <summary>Current cargo damage 0–100 %.</summary>
        public float CargoDamagePercent => _cargoDamagePercent;

        // ── Unity Lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _physicsIntegrator = FindObjectOfType<SWEF.Flight.FlightPhysicsIntegrator>();
            if (_physicsIntegrator != null)
                _physicsIntegrator.OnPhysicsSnapshot += HandleSnapshot;
        }

        private void OnDestroy()
        {
            if (_physicsIntegrator != null)
                _physicsIntegrator.OnPhysicsSnapshot -= HandleSnapshot;
        }

        private void Update()
        {
            if (!_cargoActive || _destroyed) return;
            TrackCargoDamage();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Loads a new cargo manifest and begins monitoring for damage.
        /// </summary>
        public void LoadCargo(CargoManifest manifest)
        {
            _manifest           = manifest;
            _cargoDamagePercent = 0f;
            _destroyed          = false;
            _cargoActive        = true;
        }

        /// <summary>Unloads the cargo and resets computed values.</summary>
        public void UnloadCargo()
        {
            _manifest    = null;
            _cargoActive = false;
        }

        /// <summary>
        /// Applies an instantaneous landing impact to fragile cargo.
        /// </summary>
        public void ApplyLandingImpact(float verticalSpeedMps)
        {
            if (!_cargoActive || _manifest == null || _destroyed) return;
            if (verticalSpeedMps >= hardLandingThreshold)
            {
                float severity = (verticalSpeedMps - hardLandingThreshold) / hardLandingThreshold;
                ApplyDamage(severity * _manifest.fragilityRating * 25f);
            }
        }

        // ── Internal ──────────────────────────────────────────────────────────
        private void HandleSnapshot(SWEF.Flight.FlightPhysicsSnapshot snap)
        {
            _latestGForce = snap.GForce;
        }

        private void TrackCargoDamage()
        {
            if (_manifest == null || _manifest.fragilityRating <= 0f) return;

            float gPenalty    = 0f;
            float turbPenalty = 0f;

            float g = Mathf.Abs(_latestGForce);
            if (g > fragileGForceThreshold)
                gPenalty = (g - fragileGForceThreshold) * _manifest.fragilityRating * 2f
                           * Time.deltaTime;

            var wm = SWEF.Weather.WeatherFlightModifier.Instance;
            if (wm != null)
            {
                float turb = wm.TurbulenceIntensity;
                if (turb > fragileTurbThreshold)
                    turbPenalty = (turb - fragileTurbThreshold) * _manifest.fragilityRating * 3f
                                  * Time.deltaTime;
            }

            float dmg = gPenalty + turbPenalty;
            if (dmg > 0f) ApplyDamage(dmg);
        }

        private void ApplyDamage(float amount)
        {
            _cargoDamagePercent = Mathf.Min(_cargoDamagePercent + amount, 100f);
            OnCargoDamaged?.Invoke(_cargoDamagePercent);

            if (!_destroyed && _cargoDamagePercent >= 100f)
            {
                _destroyed = true;
                OnCargoDestroyed?.Invoke();
            }
        }
    }
}
