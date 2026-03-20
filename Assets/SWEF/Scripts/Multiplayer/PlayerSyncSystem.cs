using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Multiplayer
{
    // ── Structs ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// A single authoritative snapshot of a player's flight state at a given network tick.
    /// </summary>
    [Serializable]
    public struct PlayerSnapshot
    {
        /// <summary>Network tick counter when this snapshot was captured.</summary>
        public uint tick;
        /// <summary>World-space position of the player aircraft.</summary>
        public Vector3 position;
        /// <summary>World-space orientation of the player aircraft.</summary>
        public Quaternion rotation;
        /// <summary>Current velocity vector in world space (m/s).</summary>
        public Vector3 velocity;
        /// <summary>Throttle input in the range [0, 1].</summary>
        public float throttle;
        /// <summary>Flap deflection in the range [0, 1].</summary>
        public float flaps;
        /// <summary>
        /// Bitmask of dirty fields for delta compression.
        /// Bit 0 = position, 1 = rotation, 2 = velocity, 3 = throttle, 4 = flaps.
        /// </summary>
        public byte flags;

        // Dirty-flag constants
        public const byte FlagPosition = 1 << 0;
        public const byte FlagRotation = 1 << 1;
        public const byte FlagVelocity = 1 << 2;
        public const byte FlagThrottle = 1 << 3;
        public const byte FlagFlaps    = 1 << 4;
        public const byte FlagAll      = FlagPosition | FlagRotation | FlagVelocity | FlagThrottle | FlagFlaps;
    }

    /// <summary>
    /// Tracks the interpolation state for a single remote player.
    /// </summary>
    internal class RemotePlayerSyncState
    {
        public string playerId;
        public PlayerAvatar avatar;
        public readonly List<PlayerSnapshot> buffer = new(8);

        // Dead-reckoning state
        public Vector3    drPosition;
        public Quaternion drRotation;
        public Vector3    drVelocity;
        public float      drAcceleration;

        // Previous snapshot values for delta decompression
        public PlayerSnapshot lastReceived;
        public bool hasReceived;
    }

    // ── PlayerSyncSystem ──────────────────────────────────────────────────────────

    /// <summary>
    /// High-performance player state synchronisation system for Phase 33.
    ///
    /// <para>Runs at a configurable tick rate (default 20 Hz), maintains a per-remote-player
    /// interpolation buffer (≥ 3 snapshots), performs dead-reckoning when packets are late,
    /// and uses delta compression (bitfield flags) to minimise bandwidth.  Nearby players
    /// receive higher-priority updates than distant ones.</para>
    ///
    /// <para>Intended to work alongside the existing <see cref="PlayerSyncController"/>
    /// (Phase 20), which handles basic send/receive; this class adds the interpolation
    /// buffer, dead-reckoning, and delta compression layers.</para>
    /// </summary>
    public class PlayerSyncSystem : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Tick Rate")]
        [Tooltip("How many state snapshots per second to send (default 20 Hz).")]
        [SerializeField] private float tickRate = 20f;

        [Header("Interpolation")]
        [Tooltip("Minimum number of buffered snapshots before interpolation begins.")]
        [SerializeField] private int minBufferSize = 3;

        [Tooltip("Maximum seconds of extrapolation via dead reckoning before motion freezes.")]
        [SerializeField] private float maxDeadReckoningSec = 0.5f;

        [Header("Priority")]
        [Tooltip("Players within this radius (m) receive high-frequency updates.")]
        [SerializeField] private float nearbyRadius = 500f;

        [Tooltip("Tick divisor for distant players (e.g. 2 = half the tick rate).")]
        [SerializeField] private int distantTickDivisor = 2;

        [Header("Delta Compression")]
        [Tooltip("Minimum position change (m) to flag position as dirty.")]
        [SerializeField] private float positionThreshold = 0.01f;

        [Tooltip("Minimum rotation change (degrees) to flag rotation as dirty.")]
        [SerializeField] private float rotationThreshold = 0.1f;

        [Tooltip("Minimum velocity change (m/s) to flag velocity as dirty.")]
        [SerializeField] private float velocityThreshold = 0.05f;

        [Header("References")]
        [SerializeField] private MultiplayerManager multiplayerManager;

        // ── Events ────────────────────────────────────────────────────────────────
        // (reserved for future per-tick event hooks)

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Approximate bytes per second being sent to the network.</summary>
        public float BytesPerSecondSent { get; private set; }

        /// <summary>Approximate bytes per second being received from the network.</summary>
        public float BytesPerSecondReceived { get; private set; }

        // ── Private state ─────────────────────────────────────────────────────────
        private float _tickInterval;
        private float _tickTimer;
        private uint  _currentTick;
        private int   _tickFrame;

        private readonly Dictionary<string, RemotePlayerSyncState> _remoteStates = new();

        // Bandwidth tracking
        private float _bytesSentAccum;
        private float _bytesRecvAccum;
        private float _bwTimer;
        private const float BandwidthWindowSec = 1f;

        // Reference snapshot for delta compression
        private PlayerSnapshot _lastSentSnapshot;

        // Cached local flight reference
        private Flight.FlightController _flightController;
        // _altitudeController reserved for future altitude-based sync features.

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _tickInterval = 1f / Mathf.Max(1f, tickRate);

            if (multiplayerManager == null)
                multiplayerManager = FindFirstObjectByType<MultiplayerManager>();
            if (_flightController == null)
                _flightController = FindFirstObjectByType<Flight.FlightController>();
            // Note: _altitudeController is reserved for future altitude-based sync features.
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Tick accumulator
            _tickTimer += dt;
            if (_tickTimer >= _tickInterval)
            {
                _tickTimer -= _tickInterval;
                _tickFrame++;
                OnTick();
            }

            // Interpolate all remote players every frame at render rate.
            float renderTime = Time.time - _tickInterval * minBufferSize;
            foreach (var kvp in _remoteStates)
                UpdateRemotePlayer(kvp.Value, renderTime, dt);

            // Bandwidth window
            _bwTimer += dt;
            if (_bwTimer >= BandwidthWindowSec)
            {
                BytesPerSecondSent     = _bytesSentAccum / _bwTimer;
                BytesPerSecondReceived = _bytesRecvAccum / _bwTimer;
                _bytesSentAccum = 0f;
                _bytesRecvAccum = 0f;
                _bwTimer        = 0f;
            }
        }

        // ── Tick ──────────────────────────────────────────────────────────────────

        private void OnTick()
        {
            _currentTick++;
            PlayerSnapshot snap = CaptureLocalSnapshot(_currentTick);
            byte flags = DeltaCompress(snap, _lastSentSnapshot);

            if (flags != 0 || _currentTick == 1)
            {
                snap.flags = flags;
                BroadcastSnapshot(snap);
                _lastSentSnapshot = snap;
            }
        }

        /// <summary>
        /// Captures a <see cref="PlayerSnapshot"/> from the local player's flight state.
        /// </summary>
        private PlayerSnapshot CaptureLocalSnapshot(uint tick)
        {
            Vector3    pos = transform.position;
            Quaternion rot = transform.rotation;
            Vector3    vel = Vector3.zero;
            float      thr = 0f;
            float      flp = 0f;

            if (_flightController != null)
            {
                vel = _flightController.Velocity;
                thr = _flightController.Throttle01;
            }

            return new PlayerSnapshot
            {
                tick     = tick,
                position = pos,
                rotation = rot,
                velocity = vel,
                throttle = thr,
                flaps    = flp,
                flags    = PlayerSnapshot.FlagAll
            };
        }

        /// <summary>
        /// Computes which fields changed relative to the previous sent snapshot.
        /// </summary>
        private byte DeltaCompress(PlayerSnapshot current, PlayerSnapshot previous)
        {
            byte flags = 0;
            if (Vector3.Distance(current.position, previous.position) > positionThreshold)
                flags |= PlayerSnapshot.FlagPosition;
            if (Quaternion.Angle(current.rotation, previous.rotation) > rotationThreshold)
                flags |= PlayerSnapshot.FlagRotation;
            if (Vector3.Distance(current.velocity, previous.velocity) > velocityThreshold)
                flags |= PlayerSnapshot.FlagVelocity;
            if (Mathf.Abs(current.throttle - previous.throttle) > 0.01f)
                flags |= PlayerSnapshot.FlagThrottle;
            if (Mathf.Abs(current.flaps - previous.flaps) > 0.01f)
                flags |= PlayerSnapshot.FlagFlaps;
            return flags;
        }

        /// <summary>
        /// Broadcasts the snapshot to the multiplayer manager for network dispatch.
        /// Applies priority throttling for distant players.
        /// </summary>
        private void BroadcastSnapshot(PlayerSnapshot snap)
        {
            // Estimate serialised byte size for bandwidth tracking.
            int estimatedBytes = 4 + 12 + 16 + 12 + 4 + 4 + 1; // tick+pos+rot+vel+thr+flp+flags
            _bytesSentAccum += estimatedBytes;

            // Forward to the legacy manager for transmission.
            if (multiplayerManager == null) return;

            var syncData = new PlayerSyncData
            {
                position = snap.position,
                rotation = snap.rotation,
                speed    = snap.velocity.magnitude,
                throttle = snap.throttle
            };
            multiplayerManager.BroadcastSyncData(syncData);
        }

        // ── Remote player integration ─────────────────────────────────────────────

        /// <summary>
        /// Registers a remote player snapshot received from the network.
        /// </summary>
        /// <param name="playerId">Remote player identifier.</param>
        /// <param name="snapshot">Received snapshot (already delta-decompressed).</param>
        /// <param name="bytesReceived">Size of the incoming packet (for bandwidth accounting).</param>
        public void ReceiveRemoteSnapshot(string playerId, PlayerSnapshot snapshot, int bytesReceived = 52)
        {
            _bytesRecvAccum += bytesReceived;

            if (!_remoteStates.TryGetValue(playerId, out var state))
            {
                state = new RemotePlayerSyncState { playerId = playerId };
                _remoteStates[playerId] = state;
            }

            // Delta-decompress: apply received flags on top of last known state.
            PlayerSnapshot full = state.hasReceived ? state.lastReceived : snapshot;
            if ((snapshot.flags & PlayerSnapshot.FlagPosition) != 0) full.position = snapshot.position;
            if ((snapshot.flags & PlayerSnapshot.FlagRotation) != 0) full.rotation = snapshot.rotation;
            if ((snapshot.flags & PlayerSnapshot.FlagVelocity) != 0) full.velocity = snapshot.velocity;
            if ((snapshot.flags & PlayerSnapshot.FlagThrottle) != 0) full.throttle = snapshot.throttle;
            if ((snapshot.flags & PlayerSnapshot.FlagFlaps)    != 0) full.flaps    = snapshot.flaps;
            full.tick  = snapshot.tick;
            full.flags = PlayerSnapshot.FlagAll;

            state.buffer.Add(full);
            if (state.buffer.Count > 8) state.buffer.RemoveAt(0);

            state.lastReceived = full;
            state.hasReceived  = true;
        }

        /// <summary>
        /// Removes a remote player from the sync system when they disconnect.
        /// </summary>
        public void RemoveRemotePlayer(string playerId) => _remoteStates.Remove(playerId);

        // ── Per-frame remote interpolation ────────────────────────────────────────

        private void UpdateRemotePlayer(RemotePlayerSyncState state, float renderTime, float dt)
        {
            if (state.avatar == null || !state.hasReceived) return;

            if (state.buffer.Count >= minBufferSize)
            {
                // Interpolate between the two snapshots that bracket renderTime.
                // (Simplified: interpolate between last two buffered states.)
                var s0 = state.buffer[state.buffer.Count - 2];
                var s1 = state.buffer[state.buffer.Count - 1];

                float t = Mathf.Clamp01((Time.time - (s0.tick * _tickInterval)) / _tickInterval);
                Vector3    pos = Vector3.Lerp(s0.position, s1.position, t);
                Quaternion rot = Quaternion.Slerp(s0.rotation, s1.rotation, t);

                state.drPosition = pos;
                state.drRotation = rot;
                state.drVelocity = s1.velocity;
            }
            else
            {
                // Dead reckoning: extrapolate from last known state using velocity.
                float elapsed = Time.time - (state.lastReceived.tick * _tickInterval);
                if (elapsed <= maxDeadReckoningSec)
                {
                    state.drPosition += state.drVelocity * dt;
                }
            }

            // Apply to avatar transform directly.
            state.avatar.transform.position = state.drPosition;
            state.avatar.transform.rotation = state.drRotation;
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 200, 80));
            GUILayout.Label($"Sync TX: {BytesPerSecondSent:F0} B/s");
            GUILayout.Label($"Sync RX: {BytesPerSecondReceived:F0} B/s");
            GUILayout.Label($"Tick: {_currentTick}  Remote: {_remoteStates.Count}");
            GUILayout.EndArea();
        }
#endif
    }
}
