using System;
using System.Collections.Generic;
using UnityEngine;
using SWEF.Multiplayer;

namespace SWEF.VoiceChat
{
    /// <summary>
    /// Manages the lifecycle of all voice channels (Proximity, Team, Global, Private, ATC).
    /// <para>
    /// The channel manager sits between <see cref="VoiceChatManager"/> and the individual
    /// transport layer, deciding which participants hear each other based on channel membership
    /// and proximity range.
    /// </para>
    /// </summary>
    public class VoiceChannelManager : MonoBehaviour
    {
        #region Singleton
        /// <summary>Global singleton instance.</summary>
        public static VoiceChannelManager Instance { get; private set; }
        #endregion

        #region Inspector Fields
        [Header("Proximity")]
        [Tooltip("Range in metres within which players are auto-added to the Proximity channel.")]
        [SerializeField] private float proximityRange = 500f;

        [Tooltip("How frequently (seconds) the proximity membership list is refreshed.")]
        [SerializeField] private float proximityRefreshInterval = 2f;

        [Header("Global Channel")]
        [Tooltip("Minimum seconds between Global channel transmissions (rate-limit).")]
        [SerializeField] private float globalRateLimitSeconds = 5f;
        #endregion

        #region Internal State
        private readonly Dictionary<VoiceChannel, HashSet<string>> _channelMembers
            = new Dictionary<VoiceChannel, HashSet<string>>();

        private readonly Dictionary<string, HashSet<VoiceChannel>> _mutedInChannel
            = new Dictionary<string, HashSet<VoiceChannel>>();

        private float _proximityTimer = 0f;
        private readonly Dictionary<string, float> _globalLastTransmit
            = new Dictionary<string, float>();

        private MultiplayerManager _multiplayerManager;
        #endregion

        #region Events
        /// <summary>Fired when a participant joins a channel.</summary>
        public event Action<string, VoiceChannel> OnParticipantJoinedChannel;

        /// <summary>Fired when a participant leaves a channel.</summary>
        public event Action<string, VoiceChannel> OnParticipantLeftChannel;
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

            foreach (VoiceChannel ch in Enum.GetValues(typeof(VoiceChannel)))
                _channelMembers[ch] = new HashSet<string>();
        }

        private void Start()
        {
            _multiplayerManager = FindObjectOfType<MultiplayerManager>();
        }

        private void Update()
        {
            _proximityTimer -= Time.deltaTime;
            if (_proximityTimer <= 0f)
            {
                _proximityTimer = proximityRefreshInterval;
                RefreshProximityChannel();
            }
        }
        #endregion

        #region Public API — Channel Operations
        /// <summary>
        /// Adds a participant to the specified channel.
        /// </summary>
        /// <param name="participantId">Participant to add.</param>
        /// <param name="channel">Target channel.</param>
        public void JoinChannel(string participantId, VoiceChannel channel)
        {
            if (string.IsNullOrEmpty(participantId)) return;
            if (_channelMembers[channel].Add(participantId))
                OnParticipantJoinedChannel?.Invoke(participantId, channel);
        }

        /// <summary>
        /// Removes a participant from the specified channel.
        /// </summary>
        /// <param name="participantId">Participant to remove.</param>
        /// <param name="channel">Target channel.</param>
        public void LeaveChannel(string participantId, VoiceChannel channel)
        {
            if (_channelMembers[channel].Remove(participantId))
                OnParticipantLeftChannel?.Invoke(participantId, channel);
        }

        /// <summary>
        /// Removes a participant from all channels (e.g. on disconnect).
        /// </summary>
        /// <param name="participantId">Participant ID to remove globally.</param>
        public void RemoveParticipantFromAllChannels(string participantId)
        {
            foreach (var ch in _channelMembers.Keys)
                LeaveChannel(participantId, ch);

            _mutedInChannel.Remove(participantId);
        }

        /// <summary>
        /// Returns the list of participant IDs currently in the specified channel.
        /// </summary>
        /// <param name="channel">Channel to query.</param>
        /// <returns>List of participant IDs.</returns>
        public List<string> GetChannelParticipants(VoiceChannel channel)
        {
            return new List<string>(_channelMembers[channel]);
        }

        /// <summary>
        /// Silences a participant within a specific channel without affecting other channels.
        /// </summary>
        /// <param name="participantId">Participant to mute.</param>
        /// <param name="channel">Channel in which to apply the mute.</param>
        public void MuteParticipantInChannel(string participantId, VoiceChannel channel)
        {
            if (!_mutedInChannel.TryGetValue(participantId, out HashSet<VoiceChannel> set))
            {
                set = new HashSet<VoiceChannel>();
                _mutedInChannel[participantId] = set;
            }
            set.Add(channel);
        }

        /// <summary>
        /// Removes a per-channel mute for the specified participant.
        /// </summary>
        /// <param name="participantId">Participant to unmute.</param>
        /// <param name="channel">Channel in which to lift the mute.</param>
        public void UnmuteParticipantInChannel(string participantId, VoiceChannel channel)
        {
            if (_mutedInChannel.TryGetValue(participantId, out HashSet<VoiceChannel> set))
                set.Remove(channel);
        }

        /// <summary>
        /// Returns whether the given participant is muted in the given channel.
        /// </summary>
        /// <param name="participantId">Participant to check.</param>
        /// <param name="channel">Channel to check.</param>
        /// <returns><c>true</c> if muted in the channel.</returns>
        public bool IsParticipantMutedInChannel(string participantId, VoiceChannel channel)
        {
            return _mutedInChannel.TryGetValue(participantId, out HashSet<VoiceChannel> set)
                   && set.Contains(channel);
        }
        #endregion

        #region Public API — Special Channels
        /// <summary>
        /// Creates a private one-on-one channel and auto-joins both the local player and the target.
        /// </summary>
        /// <param name="localPlayerId">Local player ID.</param>
        /// <param name="targetPlayerId">Remote participant ID to call.</param>
        public void CreatePrivateChannel(string localPlayerId, string targetPlayerId)
        {
            JoinChannel(localPlayerId,  VoiceChannel.Private);
            JoinChannel(targetPlayerId, VoiceChannel.Private);
            Debug.Log($"[SWEF][VoiceChannelManager] Private channel opened: {localPlayerId} ↔ {targetPlayerId}");
        }

        /// <summary>
        /// Checks whether the Global channel rate-limit allows a participant to transmit.
        /// Records the transmission time if allowed.
        /// </summary>
        /// <param name="participantId">Participant requesting transmission.</param>
        /// <returns><c>true</c> if the participant may transmit.</returns>
        public bool CanTransmitGlobal(string participantId)
        {
            float now = Time.time;
            if (_globalLastTransmit.TryGetValue(participantId, out float last)
                && now - last < globalRateLimitSeconds)
                return false;

            _globalLastTransmit[participantId] = now;
            return true;
        }
        #endregion

        #region Private Helpers
        private void RefreshProximityChannel()
        {
            if (_multiplayerManager == null) return;

            var nearby = _multiplayerManager.GetNearbyPlayers(proximityRange);
            if (nearby == null) return;

            // Build set of current nearby IDs
            var nearbyIds = new HashSet<string>();
            foreach (var player in nearby)
                nearbyIds.Add(player.playerId);

            HashSet<string> current = _channelMembers[VoiceChannel.Proximity];
            var toRemove = new List<string>();

            foreach (string id in current)
                if (!nearbyIds.Contains(id))
                    toRemove.Add(id);

            foreach (string id in toRemove)
                LeaveChannel(id, VoiceChannel.Proximity);

            foreach (string id in nearbyIds)
                JoinChannel(id, VoiceChannel.Proximity);
        }
        #endregion
    }
}
