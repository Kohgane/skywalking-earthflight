// Phase 72 — Autopilot & Cruise Control System
// Assets/SWEF/Scripts/Autopilot/CruiseControlManager.cs
using System;
using UnityEngine;
using SWEF.Flight;
using SWEF.Fuel;

namespace SWEF.Autopilot
{
    /// <summary>
    /// Dedicated cruise-control logic with Economy / Normal / Sport profiles.
    /// Works alongside <see cref="AutopilotController"/> and integrates with
    /// <see cref="FuelManager"/> for consumption estimation.
    /// </summary>
    [DisallowMultipleComponent]
    public class CruiseControlManager : MonoBehaviour
    {
        #region Singleton
        /// <summary>Shared cruise-control instance.</summary>
        public static CruiseControlManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _flightController = FindObjectOfType<FlightController>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Cruise Profiles
        /// <summary>Available cruise-control driving profiles.</summary>
        public enum CruiseProfile { Economy, Normal, Sport }
        #endregion

        #region Inspector
        [Header("Speed Settings")]
        [Tooltip("Speed increment per tap (km/h).")]
        [SerializeField] private float speedIncrement = 10f;

        [Tooltip("Speed factor applied in Economy profile.")]
        [SerializeField] private float economySpeedFactor = 0.85f;

        [Tooltip("Speed factor applied in Sport profile.")]
        [SerializeField] private float sportSpeedFactor = 1.15f;

        [Header("Fuel Estimation")]
        [Tooltip("Base fuel consumption at optimal cruise speed (L/hr).")]
        [SerializeField] private float baseFuelConsumptionLph = 120f;

        [Tooltip("Optimal speed used as reference for consumption scaling (km/h).")]
        [SerializeField] private float optimalCruiseSpeedKmh = 350f;
        #endregion

        #region Public State
        /// <summary>Current cruise state.</summary>
        public CruiseControlState State { get; private set; } = CruiseControlState.Disabled;

        /// <summary>Active cruise profile.</summary>
        public CruiseProfile ActiveProfile { get; private set; } = CruiseProfile.Normal;

        /// <summary>Current speed target in km/h (after profile factor).</summary>
        public float TargetSpeed { get; private set; }

        /// <summary>Raw (pre-profile) speed in km/h.</summary>
        public float BaseTargetSpeed { get; private set; }
        #endregion

        #region Events
        /// <summary>Fired when the cruise state changes.</summary>
        public event Action<CruiseControlState> OnCruiseStateChanged;

        /// <summary>Fired when the active profile changes.</summary>
        public event Action<CruiseProfile> OnProfileChanged;

        /// <summary>Fired when the speed target changes.</summary>
        public event Action<float> OnSpeedTargetChanged;
        #endregion

        #region Public API
        /// <summary>Set an explicit cruise speed (km/h). Applies the current profile factor.</summary>
        public void SetCruiseSpeed(float kmh)
        {
            BaseTargetSpeed = Mathf.Max(0f, kmh);
            ApplyProfileFactor();
            NotifySpeedChanged();
        }

        /// <summary>Increase cruise speed by one increment.</summary>
        public void IncrementSpeed()
        {
            BaseTargetSpeed += speedIncrement;
            ApplyProfileFactor();
            NotifySpeedChanged();
        }

        /// <summary>Decrease cruise speed by one increment.</summary>
        public void DecrementSpeed()
        {
            BaseTargetSpeed = Mathf.Max(0f, BaseTargetSpeed - speedIncrement);
            ApplyProfileFactor();
            NotifySpeedChanged();
        }

        /// <summary>Switch to a different cruise profile and reapply to current target.</summary>
        public void SetProfile(CruiseProfile profile)
        {
            if (ActiveProfile == profile) return;
            ActiveProfile = profile;
            ApplyProfileFactor();
            OnProfileChanged?.Invoke(profile);

            AutopilotAnalytics.Instance?.TrackCruiseProfileChanged(profile.ToString());
        }

        /// <summary>Estimated remaining range in km at the current cruise speed and fuel level.</summary>
        public float GetEstimatedRange()
        {
            float consumptionLph = GetEstimatedFuelConsumptionRate();
            if (consumptionLph <= 0f) return 0f;

            float fuelL = FuelManager.Instance != null ? FuelManager.Instance.TotalFuel : 0f;
            float hoursRemaining = fuelL / consumptionLph;
            return hoursRemaining * TargetSpeed; // km
        }

        /// <summary>Estimated fuel consumption rate at current cruise speed (L/hr).</summary>
        public float GetEstimatedFuelConsumptionRate()
        {
            if (TargetSpeed <= 0f || optimalCruiseSpeedKmh <= 0f) return 0f;
            // Simplified cubic model: consumption ∝ (speed/optimal)³
            float ratio = TargetSpeed / optimalCruiseSpeedKmh;
            return baseFuelConsumptionLph * Mathf.Pow(ratio, 3f);
        }
        #endregion

        #region Private — cached refs
        private FlightController _flightController;
        #endregion

        #region Private
        private void ApplyProfileFactor()
        {
            float factor;
            switch (ActiveProfile)
            {
                case CruiseProfile.Economy: factor = economySpeedFactor; break;
                case CruiseProfile.Sport:   factor = sportSpeedFactor;   break;
                default:                    factor = 1f;                  break;
            }
            TargetSpeed = BaseTargetSpeed * factor;
        }

        private void NotifySpeedChanged()
        {
            OnSpeedTargetChanged?.Invoke(TargetSpeed);

            // Propagate to AutopilotController if speed hold is active
            AutopilotController ap = AutopilotController.Instance;
            if (ap != null && ap.IsEngaged &&
                (ap.CurrentMode == AutopilotMode.SpeedHold || ap.CurrentMode == AutopilotMode.FullAutopilot))
            {
                ap.SetTargetSpeed(TargetSpeed);
            }

            UpdateCruiseState();
        }

        private void UpdateCruiseState()
        {
            FlightController fc = _flightController;
            if (fc == null) return;

            float currentKmh = fc.CurrentSpeedMps * 3.6f;
            float delta       = TargetSpeed - currentKmh;

            CruiseControlState newState;
            if (TargetSpeed <= 0f)
                newState = CruiseControlState.Disabled;
            else if (Mathf.Abs(delta) < 5f)
                newState = CruiseControlState.Maintaining;
            else
                newState = delta > 0f ? CruiseControlState.Accelerating : CruiseControlState.Decelerating;

            if (newState != State)
            {
                State = newState;
                OnCruiseStateChanged?.Invoke(State);
            }
        }
        #endregion
    }
}
