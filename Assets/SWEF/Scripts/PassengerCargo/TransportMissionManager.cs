using System;
using System.IO;
using UnityEngine;

namespace SWEF.PassengerCargo
{
    /// <summary>
    /// Singleton MonoBehaviour — central mission lifecycle manager for transport
    /// (passenger and cargo) missions.
    ///
    /// State machine:
    ///   Idle → Accepted → Loading → InFlight → Approaching → Delivered
    ///                                                          ↓       ↓
    ///                                                      Completed  Failed
    ///   Any → Abandoned
    ///
    /// Active contract state is persisted to <c>transport_active.json</c> in
    /// <see cref="Application.persistentDataPath"/>.
    /// </summary>
    public class TransportMissionManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static TransportMissionManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Approach")]
        [Tooltip("Distance (metres) at which the mission enters Approaching state.")]
        [SerializeField] private float approachRadius = 2000f;

        [Header("Abandon Penalty")]
        [Tooltip("XP penalty applied when a mission is abandoned.")]
        [SerializeField] private long abandonXPPenalty = 50;

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<TransportContract> OnMissionAccepted;
        public event Action                    OnMissionStarted;
        public event Action<DeliveryResult>    OnMissionCompleted;
        public event Action                    OnMissionFailed;
        public event Action                    OnMissionAbandoned;

        // ── Persistence ───────────────────────────────────────────────────────
        private static readonly string ActiveFileName   = "transport_active.json";
        private static readonly string HistoryFileName  = "transport_history.json";
        private static readonly string StatsFileName    = "transport_stats.json";

        private string ActivePath   => Path.Combine(Application.persistentDataPath, ActiveFileName);
        private string HistoryPath  => Path.Combine(Application.persistentDataPath, HistoryFileName);
        private string StatsPath    => Path.Combine(Application.persistentDataPath, StatsFileName);

        // ── State ─────────────────────────────────────────────────────────────
        private TransportContract _activeContract;
        private MissionState      _state = MissionState.Idle;
        private float             _missionStartTime;
        private int               _deliveryStreak;

        private SWEF.Flight.FlightController     _flight;
        private SWEF.Landing.LandingDetector     _landing;
        private SWEF.Landing.AirportRegistry     _airports;

        // ── Properties ────────────────────────────────────────────────────────
        public MissionState       State           => _state;
        public TransportContract  ActiveContract  => _activeContract;
        public bool               HasActiveMission => _state != MissionState.Idle;

        // ── Unity Lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadPersistedState();
        }

        private void Start()
        {
            _flight  = FindObjectOfType<SWEF.Flight.FlightController>();
            _landing = FindObjectOfType<SWEF.Landing.LandingDetector>();
            _airports = SWEF.Landing.AirportRegistry.Instance;

            if (_landing != null)
                _landing.OnTouchdown += HandleTouchdown;
        }

        private void OnDestroy()
        {
            if (_landing != null)
                _landing.OnTouchdown -= HandleTouchdown;
        }

        private void Update()
        {
            if (_state == MissionState.InFlight)
                CheckApproach();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Accepts a transport contract. Only one transport mission may be active.
        /// Returns <c>false</c> if a mission is already active or the contract is null.
        /// </summary>
        public bool AcceptContract(TransportContract contract)
        {
            if (contract == null || _state != MissionState.Idle) return false;

            _activeContract = contract;
            SetState(MissionState.Accepted);

            // Load cargo physics if applicable.
            if (IsCargoMission(contract.missionType) &&
                CargoPhysicsController.Instance != null)
                CargoPhysicsController.Instance.LoadCargo(contract.cargoManifest);

            // Reset passenger comfort.
            if (PassengerComfortSystem.Instance != null)
                PassengerComfortSystem.Instance.ResetComfort();

            // Generate route.
            var routePlanner = SWEF.RoutePlanner.RoutePlannerManager.Instance;
            if (routePlanner != null)
            {
                var route = routePlanner.CreateRoute($"{contract.origin} → {contract.destination}");
                if (route != null)
                    routePlanner.StartNavigation(route);
            }

            PersistActiveState();
            OnMissionAccepted?.Invoke(contract);
            return true;
        }

        /// <summary>
        /// Transitions from Accepted/Loading to InFlight and starts the timer.
        /// </summary>
        public void BeginMission()
        {
            if (_state != MissionState.Accepted && _state != MissionState.Loading) return;

            _missionStartTime = Time.time;
            SetState(MissionState.InFlight);

            if (DeliveryTimerController.Instance != null)
                DeliveryTimerController.Instance.StartTimer(_activeContract.timeLimitSeconds);

            PersistActiveState();
            OnMissionStarted?.Invoke();
        }

        /// <summary>
        /// Finalises a successful delivery and awards rewards.
        /// </summary>
        public void CompleteMission()
        {
            if (_state != MissionState.Approaching && _state != MissionState.Delivered) return;

            SetState(MissionState.Delivered);

            float comfortScore = PassengerComfortSystem.Instance != null
                ? PassengerComfortSystem.Instance.ComfortScore
                : 100f;

            float timeRemaining = DeliveryTimerController.Instance != null
                ? DeliveryTimerController.Instance.TimeRemainingSeconds
                : 0f;

            float cargoDamage = CargoPhysicsController.Instance != null
                ? CargoPhysicsController.Instance.CargoDamagePercent
                : 0f;

            _deliveryStreak++;
            DeliveryResult result = TransportRewardCalculator.CalculateResult(
                _activeContract, comfortScore, timeRemaining,
                _activeContract.timeLimitSeconds, cargoDamage, _deliveryStreak);

            UnloadPayload();
            SetState(MissionState.Completed);
            PersistHistory(result);
            UpdateStats(result);
            OnMissionCompleted?.Invoke(result);
            ClearPersistedState();
        }

        /// <summary>Abandons the active mission with an XP penalty.</summary>
        public void AbandonMission()
        {
            if (_state == MissionState.Idle) return;

            UnloadPayload();
            _deliveryStreak = 0;
            SetState(MissionState.Abandoned);
            ClearPersistedState();
            OnMissionAbandoned?.Invoke();
            SetState(MissionState.Idle);
        }

        /// <summary>Marks the mission as failed (e.g. cargo destroyed, time expired).</summary>
        public void FailMission()
        {
            if (_state == MissionState.Idle) return;

            UnloadPayload();
            _deliveryStreak = 0;
            SetState(MissionState.Failed);
            ClearPersistedState();
            OnMissionFailed?.Invoke();
            SetState(MissionState.Idle);
        }

        // ── Internal ──────────────────────────────────────────────────────────
        private void SetState(MissionState next) => _state = next;

        private void CheckApproach()
        {
            if (_flight == null || _activeContract == null || _airports == null) return;

            var destAirport = _airports.GetAirportById(_activeContract.destination);
            if (destAirport == null) return;

            // Use the nearest-airport helper as an approach proxy.
            var nearest = _airports.GetNearestAirport(_flight.transform.position);
            if (nearest != null && string.Equals(nearest.airportId,
                    _activeContract.destination, StringComparison.OrdinalIgnoreCase))
            {
                float dist = Vector3.Distance(_flight.transform.position,
                                               nearest.runways != null && nearest.runways.Count > 0
                                               ? nearest.runways[0].thresholdPosition
                                               : _flight.transform.position);
                if (dist <= approachRadius)
                    SetState(MissionState.Approaching);
            }
        }

        private void HandleTouchdown(float landingScore)
        {
            if (_state != MissionState.Approaching) return;

            // Confirm we're at the correct destination.
            if (_airports != null && _flight != null && _activeContract != null)
            {
                var nearest = _airports.GetNearestAirport(_flight.transform.position);
                if (nearest == null || !string.Equals(nearest.airportId,
                        _activeContract.destination, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            // Apply landing impact to fragile cargo.
            if (CargoPhysicsController.Instance != null)
                CargoPhysicsController.Instance.ApplyLandingImpact(Mathf.Abs(landingScore * 5f));

            CompleteMission();
        }

        private static bool IsCargoMission(MissionType type) =>
            type == MissionType.CargoStandard   ||
            type == MissionType.CargoFragile     ||
            type == MissionType.CargoHazardous  ||
            type == MissionType.CargoOversized  ||
            type == MissionType.EmergencyMedical;

        private void UnloadPayload()
        {
            if (CargoPhysicsController.Instance != null)
                CargoPhysicsController.Instance.UnloadCargo();

            if (DeliveryTimerController.Instance != null)
                DeliveryTimerController.Instance.StopTimer();
        }

        // ── Persistence ───────────────────────────────────────────────────────
        [Serializable]
        private class ActiveStateData
        {
            public string contractId;
            public int    missionState;
            public float  missionStartTime;
            public int    deliveryStreak;
        }

        private void PersistActiveState()
        {
            if (_activeContract == null) return;
            var data = new ActiveStateData
            {
                contractId        = _activeContract.contractId,
                missionState      = (int)_state,
                missionStartTime  = _missionStartTime,
                deliveryStreak    = _deliveryStreak
            };
            File.WriteAllText(ActivePath, JsonUtility.ToJson(data, true));
        }

        private void ClearPersistedState()
        {
            _activeContract = null;
            if (File.Exists(ActivePath)) File.Delete(ActivePath);
        }

        private void LoadPersistedState()
        {
            // Active contract ScriptableObject references cannot be trivially
            // restored at runtime without an asset registry; log the attempt.
            if (File.Exists(ActivePath))
                Debug.Log("[TransportMissionManager] Found persisted active contract state.");
        }

        [Serializable]
        private class HistoryData
        {
            public DeliveryResult[] entries = new DeliveryResult[0];
        }

        private void PersistHistory(DeliveryResult result)
        {
            HistoryData history = new HistoryData();
            if (File.Exists(HistoryPath))
            {
                try { history = JsonUtility.FromJson<HistoryData>(File.ReadAllText(HistoryPath)); }
                catch { history = new HistoryData(); }
            }

            var list = new System.Collections.Generic.List<DeliveryResult>(history.entries)
                       { result };
            history.entries = list.ToArray();
            File.WriteAllText(HistoryPath, JsonUtility.ToJson(history, true));
        }

        [Serializable]
        private class StatsData
        {
            public int   totalDeliveries;
            public float averageStarRating;
            public int   currentStreak;
            public int   bestStreak;
        }

        private void UpdateStats(DeliveryResult result)
        {
            StatsData stats = new StatsData();
            if (File.Exists(StatsPath))
            {
                try { stats = JsonUtility.FromJson<StatsData>(File.ReadAllText(StatsPath)); }
                catch { stats = new StatsData(); }
            }

            stats.totalDeliveries++;
            stats.averageStarRating =
                (stats.averageStarRating * (stats.totalDeliveries - 1) + result.starRating)
                / stats.totalDeliveries;
            stats.currentStreak = _deliveryStreak;
            if (_deliveryStreak > stats.bestStreak) stats.bestStreak = _deliveryStreak;

            File.WriteAllText(StatsPath, JsonUtility.ToJson(stats, true));
        }
    }
}
