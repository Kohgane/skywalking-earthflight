using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.VoiceChat
{
    // ── VoicePacket ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Represents a single encoded voice data packet transmitted over the network.
    /// </summary>
    [Serializable]
    public class VoicePacket
    {
        /// <summary>Participant ID of the sender.</summary>
        public string senderId;

        /// <summary>Channel the packet is being broadcast on.</summary>
        public VoiceChannel channel;

        /// <summary>UTC timestamp (Unix milliseconds) when the packet was captured.</summary>
        public long timestamp;

        /// <summary>Codec used to compress <see cref="compressedData"/>.</summary>
        public VoiceCodec codec;

        /// <summary>Encoded audio payload.</summary>
        public byte[] compressedData;

        /// <summary>Monotonically increasing sequence number for packet ordering and loss detection.</summary>
        public int sequenceNumber;
    }

    // ── IVoiceTransport ───────────────────────────────────────────────────────────

    /// <summary>
    /// Abstract transport interface for sending and receiving voice packets.
    /// Concrete implementations back this with VOIP services, relays, or peer-to-peer.
    /// </summary>
    public interface IVoiceTransport
    {
        /// <summary>Fires when an incoming voice packet is received from the network.</summary>
        event Action<VoicePacket> OnVoicePacketReceived;

        /// <summary>Whether the transport currently has an active connection.</summary>
        bool IsConnected { get; }

        /// <summary>Establishes the transport connection.</summary>
        void Connect();

        /// <summary>Tears down the transport connection.</summary>
        void Disconnect();

        /// <summary>
        /// Sends a voice packet to a specific remote participant.
        /// </summary>
        /// <param name="data">Encoded audio payload.</param>
        /// <param name="targetId">Destination participant ID.</param>
        void SendVoicePacket(byte[] data, string targetId);

        /// <summary>
        /// Broadcasts a voice packet to all participants in a channel.
        /// </summary>
        /// <param name="data">Encoded audio payload.</param>
        /// <param name="channel">Target channel.</param>
        void BroadcastVoicePacket(byte[] data, VoiceChannel channel);
    }

    // ── DefaultVoiceTransport ─────────────────────────────────────────────────────

    /// <summary>
    /// Placeholder <see cref="IVoiceTransport"/> implementation using Unity's
    /// logging as a stand-in for a real VOIP service (e.g., Vivox, Photon Voice).
    /// <para>
    /// Replace this class with a production implementation before shipping.
    /// </para>
    /// </summary>
    public class DefaultVoiceTransport : IVoiceTransport
    {
        #region IVoiceTransport
        /// <inheritdoc/>
        public event Action<VoicePacket> OnVoicePacketReceived;

        /// <inheritdoc/>
        public bool IsConnected { get; private set; } = false;

        /// <inheritdoc/>
        public void Connect()
        {
            IsConnected = true;
            Debug.Log("[SWEF][VoiceTransport] Transport connected (stub).");
        }

        /// <inheritdoc/>
        public void Disconnect()
        {
            IsConnected = false;
            Debug.Log("[SWEF][VoiceTransport] Transport disconnected (stub).");
        }

        /// <inheritdoc/>
        public void SendVoicePacket(byte[] data, string targetId)
        {
            if (!IsConnected) return;
            // Stub — forward to loopback for testing
            SimulateReceive(data, targetId, VoiceChannel.Private);
        }

        /// <inheritdoc/>
        public void BroadcastVoicePacket(byte[] data, VoiceChannel channel)
        {
            if (!IsConnected) return;
            // Stub — loopback for testing
            SimulateReceive(data, "local", channel);
        }
        #endregion

        #region Private
        private int _sequenceCounter = 0;

        private void SimulateReceive(byte[] data, string senderId, VoiceChannel channel)
        {
            var packet = new VoicePacket
            {
                senderId       = senderId,
                channel        = channel,
                timestamp      = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                codec          = VoiceCodec.PCM,
                compressedData = data,
                sequenceNumber = _sequenceCounter++
            };
            OnVoicePacketReceived?.Invoke(packet);
        }
        #endregion
    }

    // ── VoiceNetworkTransport ─────────────────────────────────────────────────────

    /// <summary>
    /// MonoBehaviour wrapper around an <see cref="IVoiceTransport"/> with jitter buffer
    /// management and packet loss concealment (PLC).
    /// </summary>
    public class VoiceNetworkTransport : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Jitter Buffer")]
        [Tooltip("Target number of packets to buffer before playback (reduces jitter).")]
        [SerializeField] private int jitterBufferSize = 3;

        [Tooltip("Maximum number of packets the buffer may hold before older packets are dropped.")]
        [SerializeField] private int maxBufferSize = 10;
        #endregion

        #region Internal State
        private IVoiceTransport _transport;
        private readonly Dictionary<string, Queue<VoicePacket>> _jitterBuffers
            = new Dictionary<string, Queue<VoicePacket>>();
        private readonly Dictionary<string, int> _lastSequenceNumbers
            = new Dictionary<string, int>();
        private readonly Dictionary<string, VoicePacket> _lastPackets
            = new Dictionary<string, VoicePacket>();
        #endregion

        #region Events
        /// <summary>Fires with a dequeued packet ready for decoding and playback.</summary>
        public event Action<VoicePacket> OnPacketReady;
        #endregion

        #region Properties
        /// <summary>Whether the underlying transport is connected.</summary>
        public bool IsConnected => _transport?.IsConnected ?? false;
        #endregion

        #region Public API
        /// <summary>
        /// Initialises the transport. Defaults to <see cref="DefaultVoiceTransport"/>
        /// if none is provided.
        /// </summary>
        /// <param name="transport">Optional custom transport implementation.</param>
        public void Initialise(IVoiceTransport transport = null)
        {
            _transport = transport ?? new DefaultVoiceTransport();
            _transport.OnVoicePacketReceived += OnRawPacketReceived;
            _transport.Connect();
        }

        /// <summary>Disconnects and tears down the transport.</summary>
        public void Shutdown()
        {
            if (_transport != null)
            {
                _transport.OnVoicePacketReceived -= OnRawPacketReceived;
                _transport.Disconnect();
            }
        }

        /// <summary>
        /// Sends encoded audio data to a specific remote participant.
        /// </summary>
        /// <param name="data">Encoded audio payload.</param>
        /// <param name="targetId">Destination participant ID.</param>
        public void SendVoicePacket(byte[] data, string targetId)
            => _transport?.SendVoicePacket(data, targetId);

        /// <summary>
        /// Broadcasts encoded audio data to all participants on a channel.
        /// </summary>
        /// <param name="data">Encoded audio payload.</param>
        /// <param name="channel">Target voice channel.</param>
        public void BroadcastVoicePacket(byte[] data, VoiceChannel channel)
            => _transport?.BroadcastVoicePacket(data, channel);
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Initialise();
        }

        private void Update()
        {
            DrainJitterBuffers();
        }

        private void OnDestroy()
        {
            Shutdown();
        }
        #endregion

        #region Jitter Buffer & PLC
        private void OnRawPacketReceived(VoicePacket packet)
        {
            if (packet == null) return;
            string id = packet.senderId;

            if (!_jitterBuffers.ContainsKey(id))
                _jitterBuffers[id] = new Queue<VoicePacket>();

            var buffer = _jitterBuffers[id];

            // Drop oldest packet if buffer is full
            while (buffer.Count >= maxBufferSize)
                buffer.Dequeue();

            buffer.Enqueue(packet);
        }

        private void DrainJitterBuffers()
        {
            foreach (var kvp in _jitterBuffers)
            {
                string id     = kvp.Key;
                Queue<VoicePacket> buffer = kvp.Value;

                if (buffer.Count < jitterBufferSize) continue;

                VoicePacket packet = buffer.Dequeue();

                // Packet Loss Concealment — if sequence gap detected, repeat last frame
                if (_lastSequenceNumbers.TryGetValue(id, out int lastSeq)
                    && packet.sequenceNumber > lastSeq + 1
                    && _lastPackets.TryGetValue(id, out VoicePacket lastPkt)
                    && lastPkt != null)
                {
                    // Fire a concealment packet for each missing slot (up to 3)
                    int missing = Mathf.Min(packet.sequenceNumber - lastSeq - 1, 3);
                    for (int i = 0; i < missing; i++)
                        OnPacketReady?.Invoke(lastPkt);
                }

                _lastSequenceNumbers[id] = packet.sequenceNumber;
                _lastPackets[id]         = packet;
                OnPacketReady?.Invoke(packet);
            }
        }
        #endregion
    }
}
