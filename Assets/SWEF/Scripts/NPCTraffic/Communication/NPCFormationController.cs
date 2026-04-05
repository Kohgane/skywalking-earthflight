// NPCFormationController.cs — Phase 110: Dynamic NPC & Air Traffic Ecosystem
// Formation flight management: invitations, player join/leave, position keeping.
// Namespace: SWEF.NPCTraffic

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.NPCTraffic
{
    /// <summary>
    /// Phase 110 — Manages formation flight groups between NPC aircraft and
    /// optionally the player.  Handles invitations, join/leave protocol,
    /// and maintaining formation spacing.
    /// </summary>
    public sealed class NPCFormationController : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static NPCFormationController Instance { get; private set; }

        #endregion

        #region Events

        /// <summary>Fired when an NPC invites the player to join a formation. Argument: formation data.</summary>
        public event Action<NPCFormationData> OnFormationInviteReceived;

        /// <summary>Fired when the player successfully joins a formation. Argument: formation ID.</summary>
        public event Action<string> OnPlayerJoinedFormation;

        /// <summary>Fired when the player leaves or is removed from a formation. Argument: formation ID.</summary>
        public event Action<string> OnPlayerLeftFormation;

        /// <summary>Fired when a formation is disbanded. Argument: formation ID.</summary>
        public event Action<string> OnFormationDisbanded;

        #endregion

        #region Inspector

        [Header("Formation Settings")]
        [Tooltip("Distance at which an NPC will invite the player to join its formation (metres).")]
        [SerializeField] private float _inviteRangeMetres = 2000f;

        [Tooltip("Desired wingtip separation distance in metres.")]
        [SerializeField] private float _formationSeparationMetres = 150f;

        [Tooltip("Seconds between proximity checks for formation invitations.")]
        [SerializeField] private float _inviteCheckIntervalSeconds = 10f;

        #endregion

        #region Private State

        private readonly Dictionary<string, NPCFormationData> _formations =
            new Dictionary<string, NPCFormationData>();

        private string   _playerFormationId;
        private float    _nextInviteCheckTime;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (Time.time >= _nextInviteCheckTime)
            {
                _nextInviteCheckTime = Time.time + _inviteCheckIntervalSeconds;
                CheckProximityInvites();
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Creates a new formation with the specified NPC as lead.
        /// </summary>
        /// <param name="leadCallsign">Callsign of the lead aircraft.</param>
        /// <returns>The newly created <see cref="NPCFormationData"/>.</returns>
        public NPCFormationData CreateFormation(string leadCallsign)
        {
            var data = new NPCFormationData
            {
                FormationId        = Guid.NewGuid().ToString("N")[..8],
                LeadCallsign       = leadCallsign,
                SeparationMetres   = _formationSeparationMetres,
                IsActive           = true
            };

            _formations[data.FormationId] = data;
            return data;
        }

        /// <summary>
        /// Adds a wingman NPC to an existing formation.
        /// </summary>
        /// <param name="formationId">Formation identifier.</param>
        /// <param name="wingmanCallsign">Callsign to add.</param>
        /// <returns><c>true</c> if successfully added.</returns>
        public bool AddWingman(string formationId, string wingmanCallsign)
        {
            if (!_formations.TryGetValue(formationId, out NPCFormationData data)) return false;
            if (!data.WingmanCallsigns.Contains(wingmanCallsign))
                data.WingmanCallsigns.Add(wingmanCallsign);
            return true;
        }

        /// <summary>
        /// Accepts a formation invitation, adding the player to the formation.
        /// </summary>
        /// <param name="formationId">Formation the player is joining.</param>
        /// <returns><c>true</c> if join was successful.</returns>
        public bool PlayerJoinFormation(string formationId)
        {
            if (!_formations.TryGetValue(formationId, out NPCFormationData data)) return false;
            if (data.PlayerIsWingman) return false;

            data.PlayerIsWingman = true;
            _playerFormationId   = formationId;
            OnPlayerJoinedFormation?.Invoke(formationId);
            return true;
        }

        /// <summary>
        /// Removes the player from their current formation.
        /// </summary>
        public void PlayerLeaveFormation()
        {
            if (string.IsNullOrEmpty(_playerFormationId)) return;
            if (_formations.TryGetValue(_playerFormationId, out NPCFormationData data))
                data.PlayerIsWingman = false;

            string id = _playerFormationId;
            _playerFormationId = null;
            OnPlayerLeftFormation?.Invoke(id);
        }

        /// <summary>
        /// Disbands a formation, removing all members.
        /// </summary>
        /// <param name="formationId">Formation to disband.</param>
        public void DisbandFormation(string formationId)
        {
            if (!_formations.ContainsKey(formationId)) return;

            if (_playerFormationId == formationId)
            {
                _playerFormationId = null;
                OnPlayerLeftFormation?.Invoke(formationId);
            }

            _formations.Remove(formationId);
            OnFormationDisbanded?.Invoke(formationId);
        }

        /// <summary>
        /// Returns the formation the player is currently in, or <c>null</c>.
        /// </summary>
        public NPCFormationData GetPlayerFormation()
        {
            if (string.IsNullOrEmpty(_playerFormationId)) return null;
            _formations.TryGetValue(_playerFormationId, out NPCFormationData data);
            return data;
        }

        /// <summary>
        /// Returns a read-only view of all active formations.
        /// </summary>
        public IEnumerable<NPCFormationData> GetAllFormations() => _formations.Values;

        #endregion

        #region Private — Proximity Invite Check

        private void CheckProximityInvites()
        {
            if (NPCTrafficManager.Instance == null) return;

            // Find a player position via soft reflection
            Transform playerTransform = FindPlayerTransform();
            if (playerTransform == null) return;

            // If player is already in a formation, do not invite again
            if (!string.IsNullOrEmpty(_playerFormationId)) return;

            NPCAircraftData nearest = NPCTrafficManager.Instance.GetNearestNPC(
                playerTransform.position, _inviteRangeMetres);

            if (nearest == null) return;

            // 20 % chance the nearest NPC sends a formation invite
            if (UnityEngine.Random.value > 0.2f) return;

            NPCFormationData formation = CreateFormation(nearest.Callsign);
            OnFormationInviteReceived?.Invoke(formation);

            // Also generate a radio message
            if (NPCRadioController.Instance != null)
            {
                NPCRadioController.Instance.QueueMessage(new NPCRadioMessage
                {
                    FrequencyMHz         = 123.45f,
                    MessageType          = NPCMessageType.FormationInvite,
                    SenderCallsign       = nearest.Callsign,
                    ReceiverCallsign     = "PLAYER",
                    Content              = $"{nearest.Callsign}, requesting formation join. Do you concur?",
                    TimestampSeconds     = Time.time,
                    AudioDurationSeconds = 3f,
                    IsPlayerRelevant     = true
                });
            }
        }

        private static Transform FindPlayerTransform()
        {
            Component fc = FindObjectOfType(
                Type.GetType("SWEF.Flight.FlightController, Assembly-CSharp") ?? typeof(MonoBehaviour)) as Component;
            return fc != null ? fc.transform : null;
        }

        #endregion
    }
}
