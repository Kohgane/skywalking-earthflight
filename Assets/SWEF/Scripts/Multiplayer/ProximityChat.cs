using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Multiplayer
{
    /// <summary>
    /// Distance-based proximity voice-chat simulator.
    /// Plays pre-recorded radio chatter clips at volume levels scaled by distance.
    /// Supports manual pings and per-player muting.
    /// </summary>
    public class ProximityChat : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Singleton instance; set during <see cref="Awake"/>.</summary>
        public static ProximityChat Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Audio Clips")]
        [Tooltip("Short pilot radio phrases played as proximity chat.")]
        [SerializeField] private AudioClip[] radioChatClips;

        [Tooltip("Radio static/crackle overlay clip looped per remote player.")]
        [SerializeField] private AudioClip staticClip;

        [Header("Range & Timing")]
        [Tooltip("Maximum distance in metres at which voice is audible.")]
        [SerializeField] private float chatRange = 2000f;

        [Tooltip("Minimum seconds between automatic chatter events per player.")]
        [SerializeField] private float chatCooldown = 15f;

        [Header("Volume")]
        [Tooltip("Master voice-chat volume (0–1). Overridden by MultiplayerSettings.")]
        [SerializeField] private float masterVolume = 0.7f;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>
        /// Fired when a ping is received from a remote player.
        /// </summary>
        public event Action<string, Vector3> OnPingReceived;

        /// <summary>
        /// Fired when a radio chatter clip plays for a remote player.
        /// </summary>
        public event Action<string> OnChatPlayed;

        // ── State ────────────────────────────────────────────────────────────────
        private MultiplayerManager _multiplayerManager;
        private RoomManager        _roomManager;

        /// <summary>Set of player IDs that have been muted by the local player.</summary>
        private readonly HashSet<string> _mutedPlayers = new HashSet<string>();

        /// <summary>Cooldown timer per player (seconds since last chatter).</summary>
        private readonly Dictionary<string, float> _chatTimers = new Dictionary<string, float>();

        /// <summary>Per-player AudioSource for static overlay.</summary>
        private readonly Dictionary<string, AudioSource> _staticSources = new Dictionary<string, AudioSource>();

        private bool _allMuted;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _multiplayerManager = FindFirstObjectByType<MultiplayerManager>();
            _roomManager        = RoomManager.Instance != null
                ? RoomManager.Instance
                : FindFirstObjectByType<RoomManager>();
        }

        private void Update()
        {
            if (!(_roomManager != null && _roomManager.IsInRoom)) return;

            // Advance chatter timers
            var keys = new List<string>(_chatTimers.Keys);
            foreach (string id in keys)
                _chatTimers[id] += Time.deltaTime;

            UpdateProximityAudio();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Mutes or un-mutes a specific remote player.
        /// </summary>
        /// <param name="playerId">Player to mute/unmute.</param>
        /// <param name="muted">True to mute; false to unmute.</param>
        public void SetPlayerMuted(string playerId, bool muted)
        {
            if (muted)
                _mutedPlayers.Add(playerId);
            else
                _mutedPlayers.Remove(playerId);

            if (_staticSources.TryGetValue(playerId, out AudioSource src))
                src.mute = muted;

            Debug.Log($"[SWEF][ProximityChat] Player '{playerId}' {(muted ? "muted" : "unmuted")}.");
        }

        /// <summary>
        /// Mutes or un-mutes all remote players at once.
        /// </summary>
        /// <param name="muted">True to mute all; false to restore individual mutes.</param>
        public void SetAllMuted(bool muted)
        {
            _allMuted = muted;
            foreach (var src in _staticSources.Values)
                src.mute = muted;

            Debug.Log($"[SWEF][ProximityChat] All players {(muted ? "muted" : "unmuted")}.");
        }

        /// <summary>
        /// Sends a ping to all nearby players and fires <see cref="OnPingReceived"/>
        /// on the local player's HUD.
        /// </summary>
        public void SendPing()
        {
            Vector3 pos = transform.position;
            string localId = _multiplayerManager != null
                ? _multiplayerManager.LocalPlayerId
                : "local";

            Debug.Log("[SWEF][ProximityChat] Ping sent.");
            // In a real implementation this would be serialised and sent over the network.
            // For now simulate the callback on self to drive the HUD indicator.
            OnPingReceived?.Invoke(localId, pos);
        }

        /// <summary>Sets the master voice-chat volume (0–1).</summary>
        /// <param name="volume">New volume level.</param>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
        }

        // ── Internal ─────────────────────────────────────────────────────────────

        private void UpdateProximityAudio()
        {
            if (_multiplayerManager == null) return;

            var players = _multiplayerManager.GetNearbyPlayers(chatRange);
            if (players == null) return;

            foreach (PlayerInfo info in players)
            {
                if (info.playerId == (_multiplayerManager != null ? _multiplayerManager.LocalPlayerId : ""))
                    continue;

                // Get the sync data to determine player position
                var syncCtrl = FindFirstObjectByType<PlayerSyncController>();
                PlayerSyncData syncData = syncCtrl != null
                    ? syncCtrl.GetRemotePlayerData(info.playerId)
                    : null;

                if (syncData == null) continue;

                float distance = Vector3.Distance(transform.position, syncData.position);
                float volume   = CalculateVolume(distance);

                // Static overlay
                UpdateStaticSource(info.playerId, syncData.position, distance, volume);

                // Automatic chatter
                if (!_allMuted && !_mutedPlayers.Contains(info.playerId) && radioChatClips != null
                    && radioChatClips.Length > 0)
                {
                    if (!_chatTimers.ContainsKey(info.playerId))
                        _chatTimers[info.playerId] = chatCooldown; // allow first play quickly

                    if (_chatTimers[info.playerId] >= chatCooldown)
                    {
                        _chatTimers[info.playerId] = 0f;
                        PlayChatter(info.playerId, syncData.position, volume);
                    }
                }
            }
        }

        private float CalculateVolume(float distance)
        {
            return (1f - Mathf.Clamp01(distance / chatRange)) * masterVolume;
        }

        private void UpdateStaticSource(string playerId, Vector3 position, float distance, float volume)
        {
            if (staticClip == null) return;

            if (!_staticSources.TryGetValue(playerId, out AudioSource src))
            {
                var go = new GameObject($"ProximityStatic_{playerId}");
                go.transform.SetParent(transform);
                src = go.AddComponent<AudioSource>();
                src.clip      = staticClip;
                src.loop      = true;
                src.spatialBlend = 1f;
                _staticSources[playerId] = src;
            }

            src.transform.position = position;

            // Static intensity increases with distance
            float staticRatio = Mathf.Clamp01(distance / chatRange);
            src.volume = staticRatio * masterVolume * 0.4f;
            src.mute   = _allMuted || _mutedPlayers.Contains(playerId);

            if (!src.isPlaying)
                src.Play();
        }

        private void PlayChatter(string playerId, Vector3 position, float volume)
        {
            if (radioChatClips == null || radioChatClips.Length == 0) return;

            AudioClip clip = radioChatClips[UnityEngine.Random.Range(0, radioChatClips.Length)];
            if (clip == null) return;

            AudioSource.PlayClipAtPoint(clip, position, volume);
            OnChatPlayed?.Invoke(playerId);
            Debug.Log($"[SWEF][ProximityChat] Radio chatter played for '{playerId}'.");
        }

        private void OnDestroy()
        {
            foreach (var src in _staticSources.Values)
            {
                if (src != null) Destroy(src.gameObject);
            }
            _staticSources.Clear();
        }
    }
}
