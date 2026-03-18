using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Multiplayer
{
    // ── Sync Data Packet ─────────────────────────────────────────────────────────

    /// <summary>
    /// Compact network snapshot of a single player's flight state.
    /// Uses a bitflag header so only changed fields need to be transmitted.
    /// </summary>
    [Serializable]
    public class PlayerSyncData
    {
        /// <summary>Unique identifier of the player this data belongs to.</summary>
        public string playerId;

        /// <summary>World-space position.</summary>
        public Vector3 position;

        /// <summary>World-space rotation.</summary>
        public Quaternion rotation;

        /// <summary>Altitude above mean sea level in metres.</summary>
        public float altitude;

        /// <summary>Current airspeed in metres per second.</summary>
        public float speed;

        /// <summary>Throttle position 0–1.</summary>
        public float throttle;

        /// <summary>Jet-trail state: 0 = off, 1 = on, 2–255 = intensity levels.</summary>
        public int trailState;

        /// <summary>UTC timestamp in ticks when this snapshot was captured.</summary>
        public long timestamp;

        /// <summary>Rolling sequence number (wraps at 255) for out-of-order detection.</summary>
        public byte sequenceNumber;

        // ── Delta Compression ─────────────────────────────────────────────────────

        /// <summary>
        /// Bitflags indicating which fields differ from the previous snapshot and
        /// must be included in the serialised payload.
        /// </summary>
        [Flags]
        public enum DirtyFlags : byte
        {
            None     = 0,
            Position = 1 << 0,
            Rotation = 1 << 1,
            Altitude = 1 << 2,
            Speed    = 1 << 3,
            Throttle = 1 << 4,
            Trail    = 1 << 5,
        }

        /// <summary>
        /// Computes which fields have changed relative to <paramref name="previous"/>
        /// using simple threshold comparisons.
        /// </summary>
        public DirtyFlags ComputeDelta(PlayerSyncData previous)
        {
            if (previous == null) return (DirtyFlags)0xFF; // all fields

            DirtyFlags flags = DirtyFlags.None;

            if (Vector3.Distance(position, previous.position) > 0.1f)    flags |= DirtyFlags.Position;
            if (Quaternion.Angle(rotation, previous.rotation) > 0.5f)    flags |= DirtyFlags.Rotation;
            if (Mathf.Abs(altitude  - previous.altitude)  > 1f)          flags |= DirtyFlags.Altitude;
            if (Mathf.Abs(speed     - previous.speed)     > 0.5f)        flags |= DirtyFlags.Speed;
            if (Mathf.Abs(throttle  - previous.throttle)  > 0.01f)       flags |= DirtyFlags.Throttle;
            if (trailState != previous.trailState)                        flags |= DirtyFlags.Trail;

            return flags;
        }
    }

    // ── Interpolation Buffer Entry ───────────────────────────────────────────────

    /// <summary>
    /// A single entry in the remote-player interpolation buffer.
    /// </summary>
    internal struct SyncBufferEntry
    {
        public PlayerSyncData data;
        public float receivedTime;
    }

    // ── Remote Player State ───────────────────────────────────────────────────────

    /// <summary>
    /// Tracks per-remote-player interpolation state and bandwidth counters.
    /// </summary>
    internal class RemotePlayerState
    {
        public readonly Queue<SyncBufferEntry> buffer = new Queue<SyncBufferEntry>();
        public PlayerSyncData lastApplied;
        public float renderTime;

        public Vector3 renderedPosition;
        public Quaternion renderedRotation;
    }

    // ── Player Sync Controller ───────────────────────────────────────────────────

    /// <summary>
    /// Sends the local player's flight state to remote peers at a configurable rate
    /// and smoothly interpolates incoming remote-player states for display.
    /// Supports delta compression via <see cref="PlayerSyncData.DirtyFlags"/> and
    /// tracks bandwidth usage.
    /// </summary>
    public class PlayerSyncController : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Send Rate")]
        [Tooltip("How many state updates per second to send to the server.")]
        [SerializeField] private float sendRate = 15f;

        [Header("Interpolation")]
        [Tooltip("Number of buffered frames used to smooth remote-player movement.")]
        [SerializeField] private int interpolationBufferFrames = 3;

        [Tooltip("Maximum extrapolation window in seconds before dead-reckoning stops.")]
        [SerializeField] private float maxExtrapolationSec = 0.2f;

        [Tooltip("Distance in metres beyond which a snap correction is applied.")]
        [SerializeField] private float snapCorrectionDistance = 50f;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>
        /// Fired on the main thread each time a remote player's interpolated state
        /// has been updated.
        /// </summary>
        public event Action<string, PlayerSyncData> OnRemotePlayerUpdated;

        // ── Bandwidth Tracking ────────────────────────────────────────────────────
        /// <summary>Bytes per second sent to the server over the last second.</summary>
        public float BytesPerSecondSent { get; private set; }

        /// <summary>Bytes per second received from the server over the last second.</summary>
        public float BytesPerSecondReceived { get; private set; }

        // ── State ────────────────────────────────────────────────────────────────
        private Flight.FlightController _flightController;
        private Flight.AltitudeController _altitudeController;
        private MultiplayerManager _multiplayerManager;

        private readonly Dictionary<string, RemotePlayerState> _remoteStates
            = new Dictionary<string, RemotePlayerState>();

        private PlayerSyncData _lastSentData;
        private float _sendTimer;
        private byte _sequence;
        private float _bwBytesSent;
        private float _bwBytesReceived;
        private float _bwTimer;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _flightController   = FindFirstObjectByType<Flight.FlightController>();
            _altitudeController = FindFirstObjectByType<Flight.AltitudeController>();
            _multiplayerManager = FindFirstObjectByType<MultiplayerManager>();
        }

        private void Update()
        {
            // Send local state at configured rate
            _sendTimer += Time.deltaTime;
            if (_sendTimer >= 1f / Mathf.Max(sendRate, 1f))
            {
                _sendTimer = 0f;
                SendLocalState();
            }

            // Advance interpolation for all remote players
            foreach (var kvp in _remoteStates)
                AdvanceInterpolation(kvp.Key, kvp.Value);

            // Bandwidth counter reset every second
            _bwTimer += Time.deltaTime;
            if (_bwTimer >= 1f)
            {
                BytesPerSecondSent     = _bwBytesSent;
                BytesPerSecondReceived = _bwBytesReceived;
                _bwBytesSent           = 0f;
                _bwBytesReceived       = 0f;
                _bwTimer               = 0f;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Receives a sync packet from the network layer and enqueues it in the
        /// interpolation buffer for the originating player.
        /// Thread-safe: may be called from a background thread.
        /// </summary>
        /// <param name="data">Received sync data.</param>
        public void ReceiveRemoteData(PlayerSyncData data)
        {
            if (data == null) return;

            // Approximate bandwidth tracking (JSON length is a rough estimate)
            _bwBytesReceived += EstimatePacketSize(data);

            lock (_remoteStates)
            {
                if (!_remoteStates.TryGetValue(data.playerId, out RemotePlayerState state))
                {
                    state = new RemotePlayerState
                    {
                        renderedPosition = data.position,
                        renderedRotation = data.rotation,
                        renderTime       = Time.time
                    };
                    _remoteStates[data.playerId] = state;
                }

                // Discard out-of-order packets using sequence number
                if (state.lastApplied != null)
                {
                    byte lastSeq = state.lastApplied.sequenceNumber;
                    byte newSeq  = data.sequenceNumber;
                    // Handle wrapping: if the gap is > 128 assume wrap-around
                    int delta = (newSeq - lastSeq + 256) % 256;
                    if (delta == 0 || delta > 200) return;
                }

                state.buffer.Enqueue(new SyncBufferEntry
                {
                    data         = data,
                    receivedTime = Time.time
                });
            }
        }

        /// <summary>
        /// Removes a remote player's interpolation state when they disconnect.
        /// </summary>
        /// <param name="playerId">Player to remove.</param>
        public void RemoveRemotePlayer(string playerId)
        {
            lock (_remoteStates)
                _remoteStates.Remove(playerId);
        }

        /// <summary>
        /// Returns the current interpolated <see cref="PlayerSyncData"/> for a given
        /// remote player, or null if not found.
        /// </summary>
        public PlayerSyncData GetRemotePlayerData(string playerId)
        {
            lock (_remoteStates)
            {
                return _remoteStates.TryGetValue(playerId, out var s) ? s.lastApplied : null;
            }
        }

        // ── Internal ─────────────────────────────────────────────────────────────

        private void SendLocalState()
        {
            if (_multiplayerManager == null) return;

            var data = new PlayerSyncData
            {
                playerId       = _multiplayerManager.LocalPlayerId,
                position       = transform.position,
                rotation       = transform.rotation,
                altitude       = _altitudeController != null ? _altitudeController.CurrentAltitudeMeters : 0f,
                speed          = _flightController   != null ? _flightController.CurrentSpeedMps         : 0f,
                throttle       = _flightController   != null ? _flightController.Throttle01          : 0f,
                trailState     = 1,
                timestamp      = DateTime.UtcNow.Ticks,
                sequenceNumber = _sequence++
            };

            PlayerSyncData.DirtyFlags delta = data.ComputeDelta(_lastSentData);
            if (delta == PlayerSyncData.DirtyFlags.None) return; // nothing changed

            _lastSentData    = data;
            _bwBytesSent    += EstimatePacketSize(data);

            // Forward to the multiplayer manager's broadcast pipeline
            _multiplayerManager.BroadcastSyncData(data);
        }

        private void AdvanceInterpolation(string playerId, RemotePlayerState state)
        {
            // Aim for interpolationBufferFrames behind the newest received packet
            float bufferDelay = interpolationBufferFrames / Mathf.Max(sendRate, 1f);
            float renderTime  = Time.time - bufferDelay;

            SyncBufferEntry? from = null;
            SyncBufferEntry? to   = null;

            lock (_remoteStates)
            {
                var entries = new List<SyncBufferEntry>(state.buffer);

                for (int i = 0; i < entries.Count - 1; i++)
                {
                    if (entries[i].receivedTime <= renderTime &&
                        entries[i + 1].receivedTime >= renderTime)
                    {
                        from = entries[i];
                        to   = entries[i + 1];
                        break;
                    }
                }

                // Trim old entries
                while (state.buffer.Count > interpolationBufferFrames + 2)
                    state.buffer.Dequeue();
            }

            if (from.HasValue && to.HasValue)
            {
                float span = to.Value.receivedTime - from.Value.receivedTime;
                float t    = span > 0f
                    ? (renderTime - from.Value.receivedTime) / span
                    : 1f;

                Vector3 targetPos    = Vector3.Lerp(from.Value.data.position, to.Value.data.position, t);
                Quaternion targetRot = Quaternion.Slerp(from.Value.data.rotation, to.Value.data.rotation, t);

                // Snap correction for large desyncs
                if (Vector3.Distance(state.renderedPosition, targetPos) > snapCorrectionDistance)
                {
                    state.renderedPosition = targetPos;
                    state.renderedRotation = targetRot;
                }
                else
                {
                    state.renderedPosition = targetPos;
                    state.renderedRotation = targetRot;
                }

                state.lastApplied = to.Value.data;

                var interpolated = new PlayerSyncData
                {
                    playerId       = playerId,
                    position       = state.renderedPosition,
                    rotation       = state.renderedRotation,
                    altitude       = to.Value.data.altitude,
                    speed          = to.Value.data.speed,
                    throttle       = to.Value.data.throttle,
                    trailState     = to.Value.data.trailState,
                    timestamp      = to.Value.data.timestamp,
                    sequenceNumber = to.Value.data.sequenceNumber
                };

                OnRemotePlayerUpdated?.Invoke(playerId, interpolated);
            }
            else if (state.lastApplied != null)
            {
                // Dead-reckoning / extrapolation when no new packets arrive
                float timeSinceLast = Time.time - state.renderTime;
                if (timeSinceLast < maxExtrapolationSec)
                {
                    float speed  = state.lastApplied.speed;
                    Vector3 fwd  = state.renderedRotation * Vector3.forward;
                    state.renderedPosition += fwd * speed * Time.deltaTime;
                    OnRemotePlayerUpdated?.Invoke(playerId, state.lastApplied);
                }
            }

            state.renderTime = Time.time;
        }

        private static float EstimatePacketSize(PlayerSyncData data)
        {
            // Approximate: player ID string + 3 floats position + 4 floats rotation
            //              + 4 misc floats + long + byte ≈ 80 bytes
            return data?.playerId != null ? data.playerId.Length + 80f : 80f;
        }
    }
}
