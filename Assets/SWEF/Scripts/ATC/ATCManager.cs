using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if SWEF_LANDING_AVAILABLE
using SWEF.Landing;
#endif

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 78 — Central singleton that manages all ATC facilities, active frequencies,
    /// player zone detection, and clearance lifecycle for Skywalking Earthflight.
    ///
    /// <para>Attach to a persistent GameObject in the bootstrap scene.
    /// The manager streams ATC zones in and out based on player position.</para>
    ///
    /// <para>Integration points:
    /// <list type="bullet">
    ///   <item><c>SWEF.Landing.AirportRegistry</c> — queries known airports for facility generation
    ///   (null-safe, <c>#define SWEF_LANDING_AVAILABLE</c>).</item>
    /// </list>
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class ATCManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static ATCManager Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Settings")]
        [SerializeField] private ATCSettings settings = new ATCSettings();

        [Header("Airspace Zones")]
        [Tooltip("Pre-authored airspace zones. Additional zones may be generated procedurally at runtime.")]
        [SerializeField] private List<AirspaceZone> airspaceZones = new List<AirspaceZone>();

        [Header("References")]
        [Tooltip("Camera used to determine player world position. Resolved at runtime if null.")]
        [SerializeField] private Transform playerTransform;

        #endregion

        #region Events

        /// <summary>Fired when a new clearance is issued to the player.</summary>
        public event Action<ATCInstruction> OnClearanceReceived;

        /// <summary>Fired when the current clearance expires without acknowledgement.</summary>
        public event Action<ATCInstruction> OnClearanceExpired;

        /// <summary>Fired when the active radio frequency changes.</summary>
        public event Action<RadioFrequency> OnFrequencyChanged;

        /// <summary>Fired when the player is handed off to a new ATC facility.</summary>
        public event Action<ATCFacilityType, RadioFrequency> OnHandoff;

        /// <summary>Fired when the player declares an emergency.</summary>
        public event Action OnEmergencyDeclared;

        /// <summary>Fired when an ongoing emergency is cancelled.</summary>
        public event Action OnEmergencyCancelled;

        #endregion

        #region Public Properties

        /// <summary>Currently active clearance instruction, or null if none.</summary>
        public ATCInstruction CurrentClearance { get; private set; }

        /// <summary>Currently active COMM1 frequency.</summary>
        public RadioFrequency ActiveFrequency { get; private set; }

        /// <summary>Standby COMM1 frequency.</summary>
        public RadioFrequency StandbyFrequency { get; private set; }

        /// <summary>Whether the player is currently in an emergency state.</summary>
        public bool IsEmergency { get; private set; }

        /// <summary>The airspace zone the player is currently inside, or null.</summary>
        public AirspaceZone CurrentZone { get; private set; }

        /// <summary>Exposes the runtime settings object.</summary>
        public ATCSettings Settings => settings;

        #endregion

        #region Private State

        private readonly List<AirspaceZone> _activeZones = new List<AirspaceZone>();
        private Coroutine _zoneCheckCoroutine;
        private Coroutine _clearanceExpiryCoroutine;

        private const float ZoneCheckInterval = 2f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitialiseDefaults();
        }

        private void Start()
        {
            ResolveReferences();
            _zoneCheckCoroutine = StartCoroutine(ZoneCheckRoutine());
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Requests a clearance of the specified type from the current ATC facility.
        /// </summary>
        /// <param name="type">The clearance being requested.</param>
        public void RequestClearance(Clearance type)
        {
            var instruction = BuildInstruction(type);
            IssueClearance(instruction);
        }

        /// <summary>
        /// Acknowledges the current clearance ("Wilco").
        /// Resets the expiry timer if the clearance is time-limited.
        /// </summary>
        public void AcknowledgeClearance()
        {
            if (_clearanceExpiryCoroutine != null)
            {
                StopCoroutine(_clearanceExpiryCoroutine);
                _clearanceExpiryCoroutine = null;
            }
        }

        /// <summary>Tunes the active (COMM1) frequency.</summary>
        /// <param name="frequency">The new frequency to tune.</param>
        public void TuneFrequency(RadioFrequency frequency)
        {
            ActiveFrequency = frequency;
            OnFrequencyChanged?.Invoke(frequency);
        }

        /// <summary>Swaps the active and standby COMM1 frequencies.</summary>
        public void SwapFrequency()
        {
            var temp = ActiveFrequency;
            ActiveFrequency = StandbyFrequency;
            StandbyFrequency = temp;
            OnFrequencyChanged?.Invoke(ActiveFrequency);
        }

        /// <summary>Tunes directly to the primary frequency for the specified facility type
        /// within the current zone, if available.</summary>
        /// <param name="facilityType">Target facility type.</param>
        public void ContactFacility(ATCFacilityType facilityType)
        {
            if (CurrentZone != null && CurrentZone.facilityType == facilityType)
            {
                TuneFrequency(CurrentZone.frequency);
                return;
            }

            foreach (var zone in _activeZones)
            {
                if (zone.facilityType == facilityType)
                {
                    TuneFrequency(zone.frequency);
                    return;
                }
            }
        }

        /// <summary>Declares an emergency on the current frequency (squawk 7700).</summary>
        public void DeclareEmergency()
        {
            if (!IsEmergency)
            {
                IsEmergency = true;
                OnEmergencyDeclared?.Invoke();
            }
        }

        /// <summary>Cancels an active emergency declaration.</summary>
        public void CancelEmergency()
        {
            if (IsEmergency)
            {
                IsEmergency = false;
                OnEmergencyCancelled?.Invoke();
            }
        }

        #endregion

        #region Streaming

        private IEnumerator ZoneCheckRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(ZoneCheckInterval);
                if (playerTransform != null)
                    UpdateZones(playerTransform.position);
            }
        }

        private void UpdateZones(Vector3 playerPos)
        {
            AirspaceZone closestZone = null;
            float closestDistSqr = float.MaxValue;

            foreach (var zone in airspaceZones)
            {
                float distSqr = (new Vector2(playerPos.x - zone.center.x,
                                             playerPos.z - zone.center.z)).sqrMagnitude;
                if (distSqr < zone.radius * zone.radius)
                {
                    if (distSqr < closestDistSqr)
                    {
                        closestDistSqr = distSqr;
                        closestZone = zone;
                    }
                }
            }

            if (closestZone != CurrentZone)
            {
                CurrentZone = closestZone;
                if (settings.autoTuneFrequency && closestZone != null)
                    TuneFrequency(closestZone.frequency);
            }
        }

        #endregion

        #region Clearance Lifecycle

        private ATCInstruction BuildInstruction(Clearance type)
        {
            return new ATCInstruction
            {
                clearanceType   = type,
                expirationTime  = Time.time + 300f  // 5-minute default expiry
            };
        }

        private void IssueClearance(ATCInstruction instruction)
        {
            CurrentClearance = instruction;
            OnClearanceReceived?.Invoke(instruction);

            if (_clearanceExpiryCoroutine != null)
                StopCoroutine(_clearanceExpiryCoroutine);

            if (instruction.expirationTime > 0f)
                _clearanceExpiryCoroutine = StartCoroutine(ClearanceExpiryRoutine(instruction));
        }

        private IEnumerator ClearanceExpiryRoutine(ATCInstruction instruction)
        {
            float wait = instruction.expirationTime - Time.time;
            if (wait > 0f) yield return new WaitForSeconds(wait);

            if (CurrentClearance == instruction)
            {
                OnClearanceExpired?.Invoke(instruction);
                CurrentClearance = null;
            }
        }

        #endregion

        #region Reference Resolution

        private void InitialiseDefaults()
        {
            ActiveFrequency  = new RadioFrequency { valueMHz = 121.5f, name = "Guard", facilityType = ATCFacilityType.Center };
            StandbyFrequency = new RadioFrequency { valueMHz = 118.0f, name = "Tower", facilityType = ATCFacilityType.Tower };
        }

        private void ResolveReferences()
        {
            if (playerTransform == null)
            {
                var cam = Camera.main;
                if (cam != null) playerTransform = cam.transform;
            }

#if SWEF_LANDING_AVAILABLE
            var registry = AirportRegistry.Instance ?? FindFirstObjectByType<AirportRegistry>();
            if (registry != null)
                GenerateZonesFromAirports(registry);
#endif
        }

#if SWEF_LANDING_AVAILABLE
        private void GenerateZonesFromAirports(AirportRegistry registry)
        {
            foreach (var airport in registry.GetAllAirports())
            {
                if (airport == null) continue;
                var zone = new AirspaceZone
                {
                    center       = airport.position,
                    radius       = 9260f,  // 5 nm CTR
                    floorAltitude   = 0f,
                    ceilingAltitude = 3000f,
                    facilityType = ATCFacilityType.Tower,
                    name         = airport.icaoCode + " Tower",
                    frequency    = new RadioFrequency
                    {
                        valueMHz     = 118.1f,
                        name         = airport.icaoCode + " Tower",
                        facilityType = ATCFacilityType.Tower
                    }
                };
                airspaceZones.Add(zone);
            }
        }
#endif

        #endregion
    }
}
