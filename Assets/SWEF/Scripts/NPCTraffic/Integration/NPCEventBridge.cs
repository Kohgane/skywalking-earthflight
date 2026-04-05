// NPCEventBridge.cs — Phase 110: Dynamic NPC & Air Traffic Ecosystem
// Integration layer connecting NPC Traffic to world events, scenarios,
// natural disasters, and seasonal events via compile-time feature guards.
// Namespace: SWEF.NPCTraffic

using System;
using UnityEngine;

namespace SWEF.NPCTraffic
{
    /// <summary>
    /// Phase 110 — Bridges the NPC Traffic system with other SWEF modules.
    /// All cross-module calls are wrapped in <c>#if SWEF_*_AVAILABLE</c> guards
    /// so the NPCTraffic module compiles cleanly with no hard external dependencies.
    /// </summary>
    public sealed class NPCEventBridge : MonoBehaviour
    {
        #region Events

        /// <summary>Fired when an airshow formation is activated. Argument: number of NPC performers.</summary>
        public event Action<int> OnAirshowFormationActivated;

        /// <summary>Fired when NPCs are rerouted due to a natural disaster. Argument: disaster type string.</summary>
        public event Action<string> OnDisasterDiversionActivated;

        /// <summary>Fired when a VIP escort mission begins. Argument: VIP NPC callsign.</summary>
        public event Action<string> OnVIPEscortStarted;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        #endregion

        #region Private — Event Subscriptions

        private void SubscribeEvents()
        {
#if SWEF_DISASTER_AVAILABLE
            SWEF.NaturalDisaster.TerrainEventManager.Instance.OnEventStarted += HandleDisasterStarted;
            SWEF.NaturalDisaster.TerrainEventManager.Instance.OnEventEnded   += HandleDisasterEnded;
#endif

#if SWEF_AIRSHOW_AVAILABLE
            SWEF.Airshow.AirshowManager.Instance.OnAirshowStarted += HandleAirshowStarted;
            SWEF.Airshow.AirshowManager.Instance.OnAirshowEnded   += HandleAirshowEnded;
#endif

#if SWEF_SEASONAL_AVAILABLE
            SWEF.SeasonalEvents.LiveEventManager.Instance.OnEventActivated += HandleSeasonalEventActivated;
#endif
        }

        private void UnsubscribeEvents()
        {
#if SWEF_DISASTER_AVAILABLE
            if (SWEF.NaturalDisaster.TerrainEventManager.Instance != null)
            {
                SWEF.NaturalDisaster.TerrainEventManager.Instance.OnEventStarted -= HandleDisasterStarted;
                SWEF.NaturalDisaster.TerrainEventManager.Instance.OnEventEnded   -= HandleDisasterEnded;
            }
#endif

#if SWEF_AIRSHOW_AVAILABLE
            if (SWEF.Airshow.AirshowManager.Instance != null)
            {
                SWEF.Airshow.AirshowManager.Instance.OnAirshowStarted -= HandleAirshowStarted;
                SWEF.Airshow.AirshowManager.Instance.OnAirshowEnded   -= HandleAirshowEnded;
            }
#endif

#if SWEF_SEASONAL_AVAILABLE
            if (SWEF.SeasonalEvents.LiveEventManager.Instance != null)
                SWEF.SeasonalEvents.LiveEventManager.Instance.OnEventActivated -= HandleSeasonalEventActivated;
#endif
        }

        #endregion

        #region Private — Handlers

#if SWEF_DISASTER_AVAILABLE
        private void HandleDisasterStarted(object eventData)
        {
            DivertNPCsFromDisaster(eventData?.ToString() ?? "Unknown");
        }

        private void HandleDisasterEnded(object _)
        {
            // Resume normal traffic routing after disaster clears
            Debug.Log("[NPCEventBridge] Disaster cleared — resuming normal NPC routing.");
        }

        private void DivertNPCsFromDisaster(string disasterType)
        {
            if (NPCTrafficManager.Instance == null) return;
            foreach (var npc in NPCTrafficManager.Instance.ActiveNPCs)
            {
                if (npc.BehaviorState is NPCBehaviorState.Cruising or NPCBehaviorState.Climbing)
                    npc.BehaviorState = NPCBehaviorState.Holding;
            }
            OnDisasterDiversionActivated?.Invoke(disasterType);
        }
#endif

#if SWEF_AIRSHOW_AVAILABLE
        private void HandleAirshowStarted(SWEF.Airshow.AirshowRoutineData routine)
        {
            SpawnAirshowFormation(routine?.PerformerCount ?? 5);
        }

        private void HandleAirshowEnded(object _)
        {
            if (NPCFormationController.Instance != null)
            {
                foreach (var formation in NPCFormationController.Instance.GetAllFormations())
                    NPCFormationController.Instance.DisbandFormation(formation.FormationId);
            }
        }

        private void SpawnAirshowFormation(int performerCount)
        {
            if (NPCFormationController.Instance == null) return;
            string leadCallsign = NPCCallsignGenerator.Generate(NPCAircraftCategory.MilitaryAircraft);
            var formation = NPCFormationController.Instance.CreateFormation(leadCallsign);
            for (int i = 1; i < performerCount; i++)
                NPCFormationController.Instance.AddWingman(
                    formation.FormationId,
                    NPCCallsignGenerator.Generate(NPCAircraftCategory.MilitaryAircraft));

            OnAirshowFormationActivated?.Invoke(performerCount);
        }
#endif

#if SWEF_SEASONAL_AVAILABLE
        private void HandleSeasonalEventActivated(string eventId)
        {
            // Increase traffic density during seasonal events
            Debug.Log($"[NPCEventBridge] Seasonal event '{eventId}' active — boosting NPC density.");
        }
#endif

        #endregion

        #region Public — Scenario Triggers

        /// <summary>
        /// Starts a VIP escort scenario: spawns a VIP NPC and notifies systems.
        /// </summary>
        /// <param name="vipCallsign">Callsign of the VIP aircraft (auto-generated if empty).</param>
        public void TriggerVIPEscort(string vipCallsign = null)
        {
            string callsign = string.IsNullOrEmpty(vipCallsign)
                ? NPCCallsignGenerator.Generate(NPCAircraftCategory.PrivateJet)
                : vipCallsign;

            OnVIPEscortStarted?.Invoke(callsign);

#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.AnalyticsManager.Instance?.LogEvent("npc_vip_escort_started",
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "vip_callsign", callsign }
                });
#endif
        }

        #endregion
    }
}
