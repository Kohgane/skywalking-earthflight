using System;
using UnityEngine;

namespace SWEF.PassengerCargo
{
    /// <summary>
    /// Singleton MonoBehaviour that computes a real-time passenger comfort score
    /// (0–100) from G-force, turbulence, bank-angle rate, altitude-change rate,
    /// cabin pressure and estimated noise.
    ///
    /// Weight breakdown:
    ///   G-Force           30 %
    ///   Turbulence        25 %
    ///   Bank-Angle Rate   15 %
    ///   Altitude Rate     15 %
    ///   Cabin Pressure    10 %
    ///   Noise              5 %
    ///
    /// Comfort recovers at +2/s during smooth flight and decays at −5 to −20/s
    /// depending on severity.
    /// </summary>
    public class PassengerComfortSystem : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static PassengerComfortSystem Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Recovery / Decay")]
        [SerializeField] private float recoveryRatePerSec = 2f;
        [SerializeField] private float decayRateMinPerSec = 5f;
        [SerializeField] private float decayRateMaxPerSec = 20f;

        [Header("G-Force Thresholds")]
        [SerializeField] private float gForceComfortMax = 1.5f;
        [SerializeField] private float gForceCritical   = 2.5f;

        [Header("Cabin Pressure")]
        [SerializeField] private float pressureAltitudeMin = 3000f;   // metres — degradation starts
        [SerializeField] private float pressureAltitudeMax = 12000f;  // full pressure penalty

        [Header("Complaint Thresholds")]
        [SerializeField] private float complaintThreshold50 = 50f;
        [SerializeField] private float complaintThreshold30 = 30f;

        // ── Events ────────────────────────────────────────────────────────────
        /// <summary>Fired each frame the comfort score changes significantly (>0.5 pts).</summary>
        public event Action<float> OnComfortChanged;

        /// <summary>Fired when a passenger-complaint threshold is crossed downward.</summary>
        public event Action<string> OnPassengerComplaint;

        /// <summary>Fired when comfort drops below the Critical threshold.</summary>
        public event Action OnComfortCritical;

        // ── State ─────────────────────────────────────────────────────────────
        private float _comfortScore = 100f;
        private bool  _complained50;
        private bool  _complained30;

        private const float HysteresisMargin = 5f;

        // Sampled values from external systems (updated each frame).
        private float _latestGForce;
        private float _latestBankAngleRateDeg;
        private float _prevAltitude;
        private float _altitudeRateMps;

        // External system references (all null-safe).
        private SWEF.Flight.FlightPhysicsIntegrator _physicsIntegrator;
        private SWEF.Flight.FlightController        _flightController;
        private SWEF.Weather.WeatherFlightModifier  _weatherModifier;
        private SWEF.Flight.AltitudeController      _altitude;

        // ── Properties ────────────────────────────────────────────────────────
        /// <summary>Current comfort score in the range [0, 100].</summary>
        public float ComfortScore => _comfortScore;

        /// <summary>Comfort level enum mapped from <see cref="ComfortScore"/>.</summary>
        public ComfortLevel CurrentComfortLevel => ScoreToLevel(_comfortScore);

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
            _flightController  = FindObjectOfType<SWEF.Flight.FlightController>();
            _weatherModifier   = SWEF.Weather.WeatherFlightModifier.Instance;
            _altitude          = FindObjectOfType<SWEF.Flight.AltitudeController>();

            if (_physicsIntegrator != null)
                _physicsIntegrator.OnPhysicsSnapshot += HandleSnapshot;

            if (_altitude != null)
                _prevAltitude = _altitude.CurrentAltitudeMeters;
        }

        private void OnDestroy()
        {
            if (_physicsIntegrator != null)
                _physicsIntegrator.OnPhysicsSnapshot -= HandleSnapshot;
        }

        private void Update()
        {
            SampleAltitudeRate();

            float previous = _comfortScore;
            float delta    = CalculateComfortDelta(Time.deltaTime);

            _comfortScore = Mathf.Clamp(_comfortScore + delta, 0f, 100f);

            if (Mathf.Abs(_comfortScore - previous) > 0.5f)
                OnComfortChanged?.Invoke(_comfortScore);

            CheckComplaints();

            if (_comfortScore < 30f)
                OnComfortCritical?.Invoke();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Resets comfort to 100 (call when a new mission begins).</summary>
        public void ResetComfort()
        {
            _comfortScore = 100f;
            _complained50 = false;
            _complained30 = false;
        }

        // ── Internal Calculations ─────────────────────────────────────────────

        private void HandleSnapshot(SWEF.Flight.FlightPhysicsSnapshot snap)
        {
            _latestGForce = snap.GForce;
        }

        private void SampleAltitudeRate()
        {
            if (_altitude == null) return;
            float alt = _altitude.CurrentAltitudeMeters;
            _altitudeRateMps = (alt - _prevAltitude) / Mathf.Max(Time.deltaTime, 0.0001f);
            _prevAltitude    = alt;

            // Bank angle rate approximation from FlightController transform.
            if (_flightController != null)
            {
                float roll = _flightController.transform.eulerAngles.z;
                // Normalise to [-180, 180].
                if (roll > 180f) roll -= 360f;
                _latestBankAngleRateDeg = Mathf.Abs(roll);
            }
        }

        private float CalculateComfortDelta(float dt)
        {
            float gPenalty        = GetGForcePenalty();
            float turbPenalty     = GetTurbulencePenalty();
            float bankPenalty     = GetBankAngleRatePenalty();
            float altRatePenalty  = GetAltitudeRatePenalty();
            float pressurePenalty = GetCabinPressurePenalty();
            float noisePenalty    = GetNoisePenalty();

            // Weighted composite penalty in [0,1] range.
            float composite = gPenalty        * 0.30f
                            + turbPenalty     * 0.25f
                            + bankPenalty     * 0.15f
                            + altRatePenalty  * 0.15f
                            + pressurePenalty * 0.10f
                            + noisePenalty    * 0.05f;

            if (composite < 0.05f)
                return recoveryRatePerSec * dt;

            float decayRate = Mathf.Lerp(decayRateMinPerSec, decayRateMaxPerSec, composite);
            return -decayRate * dt;
        }

        private float GetGForcePenalty()
        {
            float g = Mathf.Abs(_latestGForce);
            if (g <= gForceComfortMax) return 0f;
            return Mathf.Clamp01((g - gForceComfortMax) / (gForceCritical - gForceComfortMax));
        }

        private float GetTurbulencePenalty()
        {
            if (_weatherModifier == null) _weatherModifier = SWEF.Weather.WeatherFlightModifier.Instance;
            if (_weatherModifier == null) return 0f;
            return Mathf.Clamp01(_weatherModifier.TurbulenceIntensity);
        }

        private float GetBankAngleRatePenalty()
        {
            return Mathf.Clamp01(_latestBankAngleRateDeg / 45f);   // 45 ° = full penalty
        }

        private float GetAltitudeRatePenalty()
        {
            return Mathf.Clamp01(Mathf.Abs(_altitudeRateMps) / 20f);   // 20 m/s = full penalty
        }

        private float GetCabinPressurePenalty()
        {
            if (_altitude == null) return 0f;
            float alt = _altitude.CurrentAltitudeMeters;
            if (alt <= pressureAltitudeMin) return 0f;
            return Mathf.Clamp01((alt - pressureAltitudeMin) /
                                  (pressureAltitudeMax - pressureAltitudeMin));
        }

        private float GetNoisePenalty()
        {
            if (_flightController == null) return 0f;
            return Mathf.Clamp01(_flightController.CurrentSpeedMps / 350f);
        }

        private void CheckComplaints()
        {
            if (!_complained50 && _comfortScore < complaintThreshold50)
            {
                _complained50 = true;
                OnPassengerComplaint?.Invoke("transport_complaint_uncomfortable");
            }
            else if (_complained50 && _comfortScore > complaintThreshold50 + HysteresisMargin)
            {
                _complained50 = false;
            }

            if (!_complained30 && _comfortScore < complaintThreshold30)
            {
                _complained30 = true;
                OnPassengerComplaint?.Invoke("transport_complaint_distressed");
            }
            else if (_complained30 && _comfortScore > complaintThreshold30 + HysteresisMargin)
            {
                _complained30 = false;
            }
        }

        // ── Static Helpers ────────────────────────────────────────────────────

        /// <summary>Maps a raw score value to the <see cref="ComfortLevel"/> enum.</summary>
        public static ComfortLevel ScoreToLevel(float score)
        {
            if (score >= 90f) return ComfortLevel.Excellent;
            if (score >= 70f) return ComfortLevel.Good;
            if (score >= 50f) return ComfortLevel.Fair;
            if (score >= 30f) return ComfortLevel.Poor;
            return ComfortLevel.Critical;
        }
    }
}
