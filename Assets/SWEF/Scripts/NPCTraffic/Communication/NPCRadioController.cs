// NPCRadioController.cs — Phase 110: Dynamic NPC & Air Traffic Ecosystem
// Simulates NPC radio communications: callsign generation, ATC-NPC exchanges,
// frequency monitoring, and message queuing for player to hear.
// Namespace: SWEF.NPCTraffic

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.NPCTraffic
{
    /// <summary>
    /// Phase 110 — Simulates radio communication between NPC aircraft and ATC,
    /// and optionally with the player.  Generates realistic radio exchanges
    /// keyed to NPC behaviour state transitions and queues them for playback
    /// on the appropriate frequency.
    /// </summary>
    public sealed class NPCRadioController : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static NPCRadioController Instance { get; private set; }

        #endregion

        #region Events

        /// <summary>Fired when a new radio message is ready. Attach a UI/audio handler.</summary>
        public event Action<NPCRadioMessage> OnMessageTransmitted;

        /// <summary>Fired when a message is directed at or concerns the player.</summary>
        public event Action<NPCRadioMessage> OnPlayerRelevantMessage;

        #endregion

        #region Inspector

        [Header("Frequency")]
        [Tooltip("Default ground ATC frequency in MHz.")]
        [SerializeField] private float _groundFrequencyMHz = 121.9f;

        [Tooltip("Default tower ATC frequency in MHz.")]
        [SerializeField] private float _towerFrequencyMHz = 118.1f;

        [Tooltip("Default approach/departure frequency in MHz.")]
        [SerializeField] private float _approachFrequencyMHz = 119.1f;

        [Header("Timing")]
        [Tooltip("Minimum seconds between generated messages on one frequency.")]
        [Range(2f, 30f)]
        [SerializeField] private float _minMessageIntervalSeconds = 5f;

        [Tooltip("Maximum seconds between generated messages on one frequency.")]
        [Range(5f, 120f)]
        [SerializeField] private float _maxMessageIntervalSeconds = 30f;

        #endregion

        #region Private State

        private readonly Queue<NPCRadioMessage> _messageQueue = new Queue<NPCRadioMessage>();
        private Coroutine _broadcastCoroutine;

        private readonly string[] _atcCallsigns =
            { "GROUND", "TOWER", "APPROACH", "DEPARTURE", "RADAR" };

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            _broadcastCoroutine = StartCoroutine(BroadcastLoop());
            SubscribeToNPCEvents();
        }

        private void OnDisable()
        {
            if (_broadcastCoroutine != null) StopCoroutine(_broadcastCoroutine);
            UnsubscribeFromNPCEvents();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Queues a radio message for broadcast on the specified frequency.
        /// </summary>
        /// <param name="message">Message to broadcast.</param>
        public void QueueMessage(NPCRadioMessage message)
        {
            _messageQueue.Enqueue(message);
        }

        /// <summary>
        /// Generates and queues an ATC → NPC clearance exchange appropriate
        /// for the given behaviour state.
        /// </summary>
        /// <param name="npcData">The NPC involved in the exchange.</param>
        /// <param name="newState">The behaviour state the NPC is entering.</param>
        public void GenerateStateExchange(NPCAircraftData npcData, NPCBehaviorState newState)
        {
            if (npcData == null) return;

            (float freq, NPCMessageType type, string npcContent, string atcContent) =
                BuildExchangeContent(npcData, newState);

            // NPC transmission
            QueueMessage(new NPCRadioMessage
            {
                FrequencyMHz          = freq,
                MessageType           = type,
                SenderCallsign        = npcData.Callsign,
                ReceiverCallsign      = GetATCCallsign(newState),
                Content               = npcContent,
                TimestampSeconds      = Time.time,
                AudioDurationSeconds  = UnityEngine.Random.Range(2f, 5f),
                IsPlayerRelevant      = false
            });

            // ATC response
            QueueMessage(new NPCRadioMessage
            {
                FrequencyMHz          = freq,
                MessageType           = type,
                SenderCallsign        = GetATCCallsign(newState),
                ReceiverCallsign      = npcData.Callsign,
                Content               = atcContent,
                TimestampSeconds      = Time.time + UnityEngine.Random.Range(1f, 3f),
                AudioDurationSeconds  = UnityEngine.Random.Range(1f, 4f),
                IsPlayerRelevant      = false
            });
        }

        #endregion

        #region Private — Broadcast

        private IEnumerator BroadcastLoop()
        {
            while (true)
            {
                if (_messageQueue.Count > 0)
                {
                    NPCRadioMessage msg = _messageQueue.Dequeue();
                    OnMessageTransmitted?.Invoke(msg);
                    if (msg.IsPlayerRelevant) OnPlayerRelevantMessage?.Invoke(msg);
                    yield return new WaitForSeconds(msg.AudioDurationSeconds + 0.5f);
                }
                else
                {
                    yield return new WaitForSeconds(
                        UnityEngine.Random.Range(_minMessageIntervalSeconds, _maxMessageIntervalSeconds));
                }
            }
        }

        #endregion

        #region Private — Content Generation

        private (float freq, NPCMessageType type, string npcText, string atcText)
            BuildExchangeContent(NPCAircraftData npc, NPCBehaviorState state)
        {
            return state switch
            {
                NPCBehaviorState.Taxiing =>
                    (_groundFrequencyMHz, NPCMessageType.TaxiClearance,
                     $"{npc.Callsign}, ready to taxi runway.",
                     $"{npc.Callsign}, taxi to runway, hold short."),

                NPCBehaviorState.Takeoff =>
                    (_towerFrequencyMHz, NPCMessageType.TakeoffClearance,
                     $"{npc.Callsign}, ready for departure.",
                     $"{npc.Callsign}, wind calm, cleared for takeoff."),

                NPCBehaviorState.Climbing =>
                    (_towerFrequencyMHz, NPCMessageType.Departure,
                     $"{npc.Callsign}, airborne, climbing.",
                     $"{npc.Callsign}, radar contact, continue climb."),

                NPCBehaviorState.Cruising =>
                    (_approachFrequencyMHz, NPCMessageType.CruiseCheckIn,
                     $"{npc.Callsign}, maintaining flight level.",
                     $"{npc.Callsign}, identified, proceed on course."),

                NPCBehaviorState.Descending =>
                    (_approachFrequencyMHz, NPCMessageType.DescentClearance,
                     $"{npc.Callsign}, requesting descent.",
                     $"{npc.Callsign}, descend and maintain."),

                NPCBehaviorState.Approach =>
                    (_approachFrequencyMHz, NPCMessageType.ApproachReport,
                     $"{npc.Callsign}, established on approach.",
                     $"{npc.Callsign}, continue approach, cleared to land."),

                NPCBehaviorState.Landing =>
                    (_towerFrequencyMHz, NPCMessageType.LandingReport,
                     $"{npc.Callsign}, touchdown.",
                     $"{npc.Callsign}, vacate when able."),

                NPCBehaviorState.Emergency =>
                    (_towerFrequencyMHz, NPCMessageType.Emergency,
                     $"MAYDAY, MAYDAY, MAYDAY. {npc.Callsign}, emergency, request immediate clearance.",
                     $"{npc.Callsign}, MAYDAY acknowledged, all traffic clear, you are cleared direct."),

                _ =>
                    (_approachFrequencyMHz, NPCMessageType.PositionReport,
                     $"{npc.Callsign}, position report.",
                     $"{npc.Callsign}, roger.")
            };
        }

        private string GetATCCallsign(NPCBehaviorState state) =>
            state switch
            {
                NPCBehaviorState.Taxiing  => _atcCallsigns[0], // GROUND
                NPCBehaviorState.Takeoff  => _atcCallsigns[1], // TOWER
                NPCBehaviorState.Landing  => _atcCallsigns[1], // TOWER
                NPCBehaviorState.Climbing => _atcCallsigns[3], // DEPARTURE
                _                        => _atcCallsigns[2]  // APPROACH
            };

        #endregion

        #region Private — Event Wiring

        private void SubscribeToNPCEvents()
        {
            if (NPCTrafficManager.Instance == null) return;
            NPCTrafficManager.Instance.OnNPCSpawned += HandleNPCSpawned;
        }

        private void UnsubscribeFromNPCEvents()
        {
            if (NPCTrafficManager.Instance == null) return;
            NPCTrafficManager.Instance.OnNPCSpawned -= HandleNPCSpawned;
        }

        private void HandleNPCSpawned(NPCAircraftData data)
        {
            // Generate a startup check-in when an NPC spawns
            if (data == null) return;
            GenerateStateExchange(data, NPCBehaviorState.Climbing);
        }

        #endregion
    }
}
