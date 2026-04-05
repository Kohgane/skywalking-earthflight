// AirportActivityManager.cs — Phase 110: Dynamic NPC & Air Traffic Ecosystem
// Dynamic airport activation: ground vehicles, gate assignment, runway usage,
// and distance-based LOD-style activity scaling.
// Namespace: SWEF.NPCTraffic

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.NPCTraffic
{
    // ════════════════════════════════════════════════════════════════════════════
    // Supporting data classes
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Represents a gate or parking stand at an active airport.</summary>
    [Serializable]
    public class AirportGate
    {
        /// <summary>Gate identifier (e.g. "A14").</summary>
        public string GateId;

        /// <summary>World-space position of the gate.</summary>
        public Vector3 WorldPosition;

        /// <summary>Callsign of the NPC currently occupying this gate, or empty.</summary>
        public string OccupyingCallsign;

        /// <summary>Whether the gate is currently occupied by an NPC aircraft.</summary>
        public bool IsOccupied => !string.IsNullOrEmpty(OccupyingCallsign);
    }

    /// <summary>Runtime activity state of a single airport.</summary>
    [Serializable]
    public class AirportActivityState
    {
        /// <summary>ICAO code of this airport.</summary>
        public string ICAO;

        /// <summary>World-space position.</summary>
        public Vector3 WorldPosition;

        /// <summary>Whether this airport is currently in the "active" LOD tier.</summary>
        public bool IsActive;

        /// <summary>Distance to the player the last time this was evaluated (metres).</summary>
        public float LastPlayerDistanceMetres;

        /// <summary>Activity level 0–1, used to scale ground vehicle / NPC traffic density.</summary>
        public float ActivityLevel;

        /// <summary>Gates available at this airport.</summary>
        public List<AirportGate> Gates = new List<AirportGate>();

        /// <summary>Number of NPC aircraft currently assigned to this airport.</summary>
        public int AssignedNPCCount;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Manager
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Phase 110 — Makes airports "come alive" as the player approaches.
    /// Manages activity levels, gate assignments, runway usage, and ground
    /// vehicle activation using a distance-based LOD approach.
    /// </summary>
    public sealed class AirportActivityManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static AirportActivityManager Instance { get; private set; }

        #endregion

        #region Events

        /// <summary>Fired when an airport transitions to active state. Argument is ICAO code.</summary>
        public event Action<string> OnAirportActivated;

        /// <summary>Fired when an airport becomes inactive. Argument is ICAO code.</summary>
        public event Action<string> OnAirportDeactivated;

        /// <summary>Fired when a gate is assigned to an NPC. Arguments: ICAO, gate ID, callsign.</summary>
        public event Action<string, string, string> OnGateAssigned;

        /// <summary>Fired when a gate is vacated. Arguments: ICAO, gate ID.</summary>
        public event Action<string, string> OnGateVacated;

        #endregion

        #region Inspector

        [Header("Activation Distances")]
        [Tooltip("Distance in metres at which an airport becomes fully active.")]
        [SerializeField] private float _fullActivationDistanceMetres = 20000f;

        [Tooltip("Distance in metres beyond which airports are deactivated.")]
        [SerializeField] private float _deactivationDistanceMetres = 50000f;

        [Tooltip("Maximum number of simultaneously active airports.")]
        [Range(1, 20)]
        [SerializeField] private int _maxActiveAirports = 5;

        #endregion

        #region Public State

        /// <summary>Read-only snapshot of tracked airports.</summary>
        public IReadOnlyList<AirportActivityState> TrackedAirports => _airports;

        #endregion

        #region Private State

        private readonly List<AirportActivityState> _airports = new List<AirportActivityState>();
        private Transform _playerTransform;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (_playerTransform == null) return;
            UpdateAirportActivation();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Registers an airport for activity tracking.
        /// </summary>
        /// <param name="icao">ICAO code.</param>
        /// <param name="worldPos">World-space position.</param>
        /// <param name="gateCount">Number of gates to generate.</param>
        public void RegisterAirport(string icao, Vector3 worldPos, int gateCount = 10)
        {
            if (_airports.Exists(a => a.ICAO == icao)) return;

            var state = new AirportActivityState
            {
                ICAO          = icao,
                WorldPosition = worldPos,
                IsActive      = false,
                ActivityLevel = 0f
            };

            for (int i = 0; i < gateCount; i++)
            {
                float angle = (360f / gateCount) * i * Mathf.Deg2Rad;
                state.Gates.Add(new AirportGate
                {
                    GateId        = $"{(char)('A' + i / 10)}{i % 10 + 1}",
                    WorldPosition = worldPos + new Vector3(Mathf.Sin(angle) * 200f, 0f, Mathf.Cos(angle) * 200f)
                });
            }

            _airports.Add(state);
        }

        /// <summary>
        /// Overrides the player transform reference used for distance evaluation.
        /// </summary>
        public void SetPlayerTransform(Transform playerTransform)
        {
            _playerTransform = playerTransform;
        }

        /// <summary>
        /// Assigns the nearest free gate at an airport to an NPC callsign.
        /// </summary>
        /// <param name="icao">Airport ICAO code.</param>
        /// <param name="callsign">NPC callsign requesting the gate.</param>
        /// <returns>The assigned gate ID, or <c>null</c> if no free gate exists.</returns>
        public string AssignGate(string icao, string callsign)
        {
            AirportActivityState airport = _airports.Find(a => a.ICAO == icao);
            if (airport == null) return null;

            AirportGate gate = airport.Gates.Find(g => !g.IsOccupied);
            if (gate == null) return null;

            gate.OccupyingCallsign = callsign;
            airport.AssignedNPCCount++;
            OnGateAssigned?.Invoke(icao, gate.GateId, callsign);
            return gate.GateId;
        }

        /// <summary>
        /// Vacates a gate previously assigned to a callsign.
        /// </summary>
        /// <param name="icao">Airport ICAO code.</param>
        /// <param name="callsign">NPC callsign vacating the gate.</param>
        public void VacateGate(string icao, string callsign)
        {
            AirportActivityState airport = _airports.Find(a => a.ICAO == icao);
            if (airport == null) return;

            AirportGate gate = airport.Gates.Find(g => g.OccupyingCallsign == callsign);
            if (gate == null) return;

            gate.OccupyingCallsign = string.Empty;
            airport.AssignedNPCCount = Mathf.Max(0, airport.AssignedNPCCount - 1);
            OnGateVacated?.Invoke(icao, gate.GateId);
        }

        /// <summary>
        /// Returns the number of free gates at the given airport.
        /// </summary>
        /// <param name="icao">Airport ICAO code.</param>
        public int GetFreeGateCount(string icao)
        {
            AirportActivityState airport = _airports.Find(a => a.ICAO == icao);
            if (airport == null) return 0;
            int free = 0;
            foreach (AirportGate g in airport.Gates) if (!g.IsOccupied) free++;
            return free;
        }

        #endregion

        #region Private — Activation Logic

        private void UpdateAirportActivation()
        {
            Vector3 playerPos      = _playerTransform.position;
            int     activeCount    = 0;

            foreach (AirportActivityState airport in _airports)
            {
                float dist = Vector3.Distance(playerPos, airport.WorldPosition);
                airport.LastPlayerDistanceMetres = dist;

                if (dist <= _fullActivationDistanceMetres && activeCount < _maxActiveAirports)
                {
                    float t = 1f - Mathf.Clamp01(dist / _fullActivationDistanceMetres);
                    airport.ActivityLevel = t;

                    if (!airport.IsActive)
                    {
                        airport.IsActive = true;
                        OnAirportActivated?.Invoke(airport.ICAO);
                    }
                    activeCount++;
                }
                else if (dist > _deactivationDistanceMetres && airport.IsActive)
                {
                    airport.IsActive      = false;
                    airport.ActivityLevel = 0f;
                    OnAirportDeactivated?.Invoke(airport.ICAO);
                }
            }
        }

        #endregion
    }
}
