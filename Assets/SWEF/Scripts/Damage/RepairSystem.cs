// RepairSystem.cs — SWEF Damage & Repair System (Phase 66)
using System;
using System.Collections;
using UnityEngine;

namespace SWEF.Damage
{
    /// <summary>
    /// Handles aircraft repair mechanics in three distinct modes:
    /// <see cref="RepairMode.Emergency"/>, <see cref="RepairMode.FieldRepair"/>,
    /// and <see cref="RepairMode.FullRepair"/>.
    ///
    /// <para>Attach to the same GameObject as <see cref="DamageModel"/>.</para>
    /// </summary>
    public class RepairSystem : MonoBehaviour
    {
        #region Inspector

        [Header("Emergency Repair")]
        [Tooltip("Minimum seconds that must elapse between emergency repair uses.")]
        /// <summary>Minimum seconds between emergency repair uses.</summary>
        public float emergencyRepairCooldown = DamageConfig.EmergencyRepairCooldown;

        [Tooltip("Health points restored to every part during one emergency repair.")]
        /// <summary>Health points restored per part during one emergency repair burst.</summary>
        public float emergencyRepairAmount = DamageConfig.EmergencyRepairAmount;

        [Tooltip("Maximum emergency-repair charges available per flight.")]
        /// <summary>Maximum emergency-repair charges per flight.</summary>
        public int emergencyRepairCharges = DamageConfig.MaxEmergencyCharges;

        [Header("Field Repair")]
        [Tooltip("Health restored per second during a field repair (stationary on ground).")]
        /// <summary>Health per second during field repair.</summary>
        public float fieldRepairRate = 10f;

        [Header("Full Repair")]
        [Tooltip("Health restored per second at a designated repair station.")]
        /// <summary>Health per second during full repair at a repair station.</summary>
        public float fullRepairRate = 25f;

        #endregion

        #region Public State

        /// <summary>Currently active repair mode.</summary>
        public RepairMode currentMode { get; private set; } = RepairMode.None;

        /// <summary><c>true</c> while any repair is in progress.</summary>
        public bool isRepairing => currentMode != RepairMode.None;

        /// <summary>Game time (<c>Time.time</c>) when the last emergency repair was used.</summary>
        public float lastEmergencyRepairTime { get; private set; } = float.NegativeInfinity;

        /// <summary>Remaining emergency-repair charges this flight.</summary>
        public int remainingEmergencyCharges { get; private set; }

        #endregion

        #region Events

        /// <summary>Raised when a repair session begins.</summary>
        public event Action<RepairMode> OnRepairStarted;

        /// <summary>Raised when all parts are fully repaired or repair is manually stopped.</summary>
        public event Action OnRepairCompleted;

        /// <summary>Raised each time a part receives repair health during a session.</summary>
        public event Action<AircraftPart, float> OnPartRepaired;

        #endregion

        #region Private State

        private DamageModel _model;
        private Coroutine   _repairCoroutine;

        #endregion

        #region Unity

        private void Awake()
        {
            _model = GetComponent<DamageModel>();
            if (_model == null)
                _model = GetComponentInParent<DamageModel>();

            remainingEmergencyCharges = emergencyRepairCharges;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Returns <c>true</c> if the emergency repair is off cooldown and the
        /// player still has charges remaining.
        /// </summary>
        public bool CanEmergencyRepair()
        {
            return remainingEmergencyCharges > 0
                && (Time.time - lastEmergencyRepairTime) >= emergencyRepairCooldown;
        }

        /// <summary>
        /// Performs an instant partial heal of every aircraft part.
        /// Can be triggered in flight.  Subject to cooldown and charge limits.
        /// </summary>
        public void StartEmergencyRepair()
        {
            if (!CanEmergencyRepair()) return;

            lastEmergencyRepairTime = Time.time;
            remainingEmergencyCharges--;

            if (_model != null)
            {
                foreach (AircraftPart part in Enum.GetValues(typeof(AircraftPart)))
                {
                    PartHealth ph = _model.GetPartHealth(part);
                    if (ph == null || ph.isDestroyed) continue;

                    ph.Repair(emergencyRepairAmount);
                    OnPartRepaired?.Invoke(part, emergencyRepairAmount);
                }
            }

            OnRepairStarted?.Invoke(RepairMode.Emergency);
            OnRepairCompleted?.Invoke();
        }

        /// <summary>
        /// Begins a slow field repair session (intended for when the aircraft is
        /// stationary on the ground).  Runs as a coroutine until all parts are
        /// fully repaired or <see cref="StopRepair"/> is called.
        /// </summary>
        public void StartFieldRepair()
        {
            BeginRepairSession(RepairMode.FieldRepair, fieldRepairRate);
        }

        /// <summary>
        /// Begins a fast full-repair session at a designated repair station.
        /// Runs as a coroutine until all parts are fully repaired or
        /// <see cref="StopRepair"/> is called.
        /// </summary>
        public void StartFullRepair()
        {
            BeginRepairSession(RepairMode.FullRepair, fullRepairRate);
        }

        /// <summary>Cancels any active repair session immediately.</summary>
        public void StopRepair()
        {
            if (_repairCoroutine != null)
            {
                StopCoroutine(_repairCoroutine);
                _repairCoroutine = null;
            }
            currentMode = RepairMode.None;
        }

        #endregion

        #region Private

        private void BeginRepairSession(RepairMode mode, float ratePerSecond)
        {
            if (isRepairing) StopRepair();

            currentMode      = mode;
            _repairCoroutine = StartCoroutine(RepairCoroutine(ratePerSecond));
            OnRepairStarted?.Invoke(mode);
        }

        private IEnumerator RepairCoroutine(float ratePerSecond)
        {
            while (true)
            {
                bool anyDamaged = false;

                if (_model != null)
                {
                    foreach (AircraftPart part in Enum.GetValues(typeof(AircraftPart)))
                    {
                        PartHealth ph = _model.GetPartHealth(part);
                        if (ph == null) continue;
                        if (ph.currentHealth >= ph.maxHealth) continue;

                        float delta = ratePerSecond * Time.deltaTime;
                        ph.Repair(delta);
                        OnPartRepaired?.Invoke(part, delta);
                        anyDamaged = true;
                    }
                }

                if (!anyDamaged)
                {
                    // All parts fully repaired.
                    currentMode      = RepairMode.None;
                    _repairCoroutine = null;
                    OnRepairCompleted?.Invoke();
                    yield break;
                }

                yield return null;
            }
        }

        #endregion
    }
}
