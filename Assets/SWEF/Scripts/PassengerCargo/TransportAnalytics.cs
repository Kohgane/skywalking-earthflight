using UnityEngine;

namespace SWEF.PassengerCargo
{
    /// <summary>
    /// MonoBehaviour that emits telemetry events to
    /// <see cref="SWEF.Analytics.TelemetryDispatcher"/> for the passenger/cargo
    /// transport mission system (null-safe).
    ///
    /// Events dispatched:
    ///   transport_contract_accepted
    ///   transport_mission_started
    ///   transport_mission_completed
    ///   transport_mission_failed
    ///   transport_mission_abandoned
    ///   transport_comfort_critical
    ///   transport_cargo_damaged
    ///   transport_vip_completed
    ///   transport_session_summary  (flushed on session end / OnDestroy)
    /// </summary>
    public class TransportAnalytics : MonoBehaviour
    {
        // ── Session counters ──────────────────────────────────────────────────
        private int   _sessionAccepted;
        private int   _sessionCompleted;
        private int   _sessionFailed;
        private int   _sessionAbandoned;
        private float _sessionComfortCriticalCount;
        private float _sessionCargoDamageEvents;

        // ── Unity Lifecycle ───────────────────────────────────────────────────
        private void Start()
        {
            var mgr = TransportMissionManager.Instance;
            if (mgr != null)
            {
                mgr.OnMissionAccepted  += OnAccepted;
                mgr.OnMissionStarted   += OnStarted;
                mgr.OnMissionCompleted += OnCompleted;
                mgr.OnMissionFailed    += OnFailed;
                mgr.OnMissionAbandoned += OnAbandoned;
            }

            if (PassengerComfortSystem.Instance != null)
                PassengerComfortSystem.Instance.OnComfortCritical += OnComfortCritical;

            if (CargoPhysicsController.Instance != null)
                CargoPhysicsController.Instance.OnCargoDamaged += OnCargoDamaged;
        }

        private void OnDestroy()
        {
            FlushSessionSummary();

            var mgr = TransportMissionManager.Instance;
            if (mgr != null)
            {
                mgr.OnMissionAccepted  -= OnAccepted;
                mgr.OnMissionStarted   -= OnStarted;
                mgr.OnMissionCompleted -= OnCompleted;
                mgr.OnMissionFailed    -= OnFailed;
                mgr.OnMissionAbandoned -= OnAbandoned;
            }

            if (PassengerComfortSystem.Instance != null)
                PassengerComfortSystem.Instance.OnComfortCritical -= OnComfortCritical;

            if (CargoPhysicsController.Instance != null)
                CargoPhysicsController.Instance.OnCargoDamaged -= OnCargoDamaged;
        }

        // ── Event handlers ────────────────────────────────────────────────────
        private void OnAccepted(TransportContract contract)
        {
            _sessionAccepted++;
            Dispatch("transport_contract_accepted", new System.Collections.Generic.Dictionary<string,object>
            {
                { "mission_type",    contract.missionType.ToString() },
                { "origin",          contract.origin },
                { "destination",     contract.destination },
                { "base_reward",     contract.baseReward }
            });
        }

        private void OnStarted()
        {
            Dispatch("transport_mission_started");
        }

        private void OnCompleted(DeliveryResult result)
        {
            _sessionCompleted++;
            bool isVip = TransportMissionManager.Instance?.ActiveContract
                         ?.passengerProfile.vipLevel >= 2 ?? false;

            Dispatch("transport_mission_completed", new System.Collections.Generic.Dictionary<string,object>
            {
                { "star_rating",     result.starRating },
                { "comfort_score",   result.comfortScore },
                { "xp_earned",       result.totalXP },
                { "coins_earned",    result.totalCoins },
                { "cargo_damage",    result.damagePercentage },
                { "time_bonus",      result.timeBonus }
            });

            if (isVip)
                Dispatch("transport_vip_completed", new System.Collections.Generic.Dictionary<string,object>
                {
                    { "star_rating", result.starRating }
                });
        }

        private void OnFailed()
        {
            _sessionFailed++;
            Dispatch("transport_mission_failed");
        }

        private void OnAbandoned()
        {
            _sessionAbandoned++;
            Dispatch("transport_mission_abandoned");
        }

        private void OnComfortCritical()
        {
            _sessionComfortCriticalCount++;
            Dispatch("transport_comfort_critical");
        }

        private void OnCargoDamaged(float dmgPercent)
        {
            _sessionCargoDamageEvents++;
            Dispatch("transport_cargo_damaged", new System.Collections.Generic.Dictionary<string,object>
            {
                { "damage_percent", dmgPercent }
            });
        }

        private void FlushSessionSummary()
        {
            Dispatch("transport_session_summary",
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "accepted",       _sessionAccepted },
                    { "completed",      _sessionCompleted },
                    { "failed",         _sessionFailed },
                    { "abandoned",      _sessionAbandoned },
                    { "comfort_crits",  _sessionComfortCriticalCount },
                    { "cargo_damage_events", _sessionCargoDamageEvents }
                });
        }

        // ── Dispatch helper ───────────────────────────────────────────────────
        private static void Dispatch(string eventName,
            System.Collections.Generic.Dictionary<string,object> props = null)
        {
            var td = SWEF.Analytics.TelemetryDispatcher.Instance;
            if (td == null) return;

            var evt = new SWEF.Analytics.TelemetryEvent
            {
                eventName  = eventName,
                properties = props ?? new System.Collections.Generic.Dictionary<string,object>()
            };
            td.EnqueueEvent(evt);
        }
    }
}
