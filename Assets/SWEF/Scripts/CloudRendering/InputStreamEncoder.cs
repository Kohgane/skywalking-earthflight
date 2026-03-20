using System;
using System.IO;
using UnityEngine;

namespace SWEF.CloudRendering
{
    /// <summary>
    /// Captures local input (touch, gyro, buttons) and serialises it to a compact
    /// binary format, then forwards the packets to the cloud server at a configurable
    /// rate (default 60 Hz).  Includes lightweight input prediction for latency
    /// compensation.
    /// </summary>
    public class InputStreamEncoder : MonoBehaviour
    {
        // ── Nested types ──────────────────────────────────────────────────────────

        /// <summary>
        /// A snapshot of all relevant player inputs captured at a single point in time.
        /// </summary>
        [Serializable]
        public struct InputSnapshot
        {
            /// <summary>Unix timestamp in milliseconds when this snapshot was captured.</summary>
            public long timestampMs;

            /// <summary>Primary touch position (normalised 0–1 in screen space).</summary>
            public Vector2 touchPosition;

            /// <summary>Secondary touch position for pinch/zoom gestures.</summary>
            public Vector2 touch2Position;

            /// <summary>Number of active touches (0, 1, or 2).</summary>
            public int touchCount;

            /// <summary>Device orientation from the gyroscope.</summary>
            public Quaternion gyroRotation;

            /// <summary>Bitmask of active buttons (bit 0 = throttle up, bit 1 = throttle down, etc.).</summary>
            public int buttonStates;

            /// <summary>Throttle value in the range 0–1.</summary>
            public float throttle;

            /// <summary>Target altitude in metres.</summary>
            public float altitude;

            /// <summary>Predicted next throttle value used for client-side prediction.</summary>
            public float predictedThrottle;
        }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Encoding")]
        [SerializeField] private float sendRateHz = 60f;

        // ── Internal state ────────────────────────────────────────────────────────
        private float _sendInterval;
        private float _sendTimer;
        private InputSnapshot _lastSnapshot;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            _sendInterval = 1f / Mathf.Max(1f, sendRateHz);
        }

        private void Update()
        {
            if (!ShouldSendInput()) return;

            _sendTimer -= Time.deltaTime;
            if (_sendTimer <= 0f)
            {
                _sendTimer = _sendInterval;
                var snapshot = CaptureCurrentInput();
                SendInput(snapshot);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Serialises <paramref name="snapshot"/> to a compact binary packet and
        /// sends it to the cloud server via <see cref="StreamingClient"/> when
        /// available, otherwise stores it for retrieval by the caller.
        /// </summary>
        public void SendInput(InputSnapshot snapshot)
        {
            byte[] packet = Encode(snapshot);
            _lastSnapshot = snapshot;

            // Forward to StreamingClient when available
            var client = StreamingClient.Instance;
            if (client != null && client.IsConnected)
                client.SendPacket(packet);
        }

        /// <summary>Captures and returns the current input state as an <see cref="InputSnapshot"/>.</summary>
        public InputSnapshot CaptureCurrentInput()
        {
            var flight  = FindFirstObjectByType<SWEF.Flight.FlightController>();
            var altCtrl = FindFirstObjectByType<SWEF.Flight.AltitudeController>();

            var snap = new InputSnapshot
            {
                timestampMs      = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                touchCount       = Input.touchCount,
                touchPosition    = Input.touchCount > 0 ? NormalisedTouchPos(Input.GetTouch(0).position) : Vector2.zero,
                touch2Position   = Input.touchCount > 1 ? NormalisedTouchPos(Input.GetTouch(1).position) : Vector2.zero,
                gyroRotation     = Input.gyro.enabled ? Input.gyro.attitude : Quaternion.identity,
                throttle         = flight != null ? flight.Throttle01 : 0f,
                altitude         = altCtrl != null ? altCtrl.TargetAltitudeMeters : 0f,
                buttonStates     = 0,
            };

            // Simple linear prediction: assume same throttle next frame
            snap.predictedThrottle = snap.throttle;

            return snap;
        }

        /// <summary>Encodes an <see cref="InputSnapshot"/> into a minimal binary representation.</summary>
        public static byte[] Encode(InputSnapshot snap)
        {
            using var ms = new MemoryStream(64);
            using var bw = new BinaryWriter(ms);
            bw.Write(snap.timestampMs);
            bw.Write(snap.touchCount);
            bw.Write(snap.touchPosition.x);
            bw.Write(snap.touchPosition.y);
            bw.Write(snap.touch2Position.x);
            bw.Write(snap.touch2Position.y);
            bw.Write(snap.gyroRotation.x);
            bw.Write(snap.gyroRotation.y);
            bw.Write(snap.gyroRotation.z);
            bw.Write(snap.gyroRotation.w);
            bw.Write(snap.buttonStates);
            bw.Write(snap.throttle);
            bw.Write(snap.altitude);
            bw.Write(snap.predictedThrottle);
            return ms.ToArray();
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private bool ShouldSendInput()
        {
            var mgr = CloudRenderingManager.Instance;
            return mgr != null && mgr.IsCloudMode;
        }

        private static Vector2 NormalisedTouchPos(Vector2 screenPos)
        {
            if (Screen.width == 0 || Screen.height == 0) return Vector2.zero;
            return new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
        }
    }
}
