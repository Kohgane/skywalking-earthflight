// CommunicationQueue.cs — Phase 119: Advanced AI Traffic Control
// Communication queuing: priority-based queue, step-on prevention,
// emergency override.
// Namespace: SWEF.ATC

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Priority-based radio communication queue that prevents
    /// simultaneous transmissions and gives emergency traffic absolute priority.
    /// </summary>
    public class CommunicationQueue : MonoBehaviour
    {
        // ── Queued Message ────────────────────────────────────────────────────────

        /// <summary>A message waiting in the communication queue.</summary>
        public class QueuedMessage
        {
            public string callsign;
            public string text;
            public TrafficPriority priority;
            public float enqueuedAt;
            public bool isEmergency;
        }

        [Header("Configuration")]
        [SerializeField] private ATCConfig config;

        private readonly List<QueuedMessage> _queue = new List<QueuedMessage>();
        private bool _transmitting;
        private float _transmissionEndTime;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when a message begins transmitting.</summary>
        public event Action<QueuedMessage> OnTransmissionStart;

        /// <summary>Raised when a message transmission completes.</summary>
        public event Action<QueuedMessage> OnTransmissionEnd;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Enqueues a message.  Emergency messages jump to the front and may
        /// interrupt a current (non-emergency) transmission.
        /// </summary>
        public void Enqueue(QueuedMessage message)
        {
            if (message == null) return;

            int maxDepth = config != null ? config.maxCommunicationQueueDepth : 16;
            if (_queue.Count >= maxDepth && !message.isEmergency) return;

            message.enqueuedAt = Time.time;

            if (message.isEmergency)
            {
                _queue.Insert(0, message);
                // Interrupt non-emergency active transmission
                if (_transmitting)
                {
                    _transmitting = false;
                    _transmissionEndTime = Time.time;
                }
            }
            else
            {
                InsertByPriority(message);
            }
        }

        private void InsertByPriority(QueuedMessage message)
        {
            for (int i = 0; i < _queue.Count; i++)
            {
                if ((int)message.priority > (int)_queue[i].priority)
                {
                    _queue.Insert(i, message);
                    return;
                }
            }
            _queue.Add(message);
        }

        private void Update()
        {
            if (_transmitting && Time.time >= _transmissionEndTime)
            {
                _transmitting = false;
            }

            if (!_transmitting && _queue.Count > 0)
            {
                var msg = _queue[0];
                _queue.RemoveAt(0);
                float duration = msg.text.Length * 0.06f + 0.5f; // ~10 chars/sec + tail
                _transmitting = true;
                _transmissionEndTime = Time.time + duration;
                OnTransmissionStart?.Invoke(msg);
                // Schedule end event (simplified — no coroutine dependency)
            }
        }

        /// <summary>Whether a transmission is currently active.</summary>
        public bool IsTransmitting => _transmitting;

        /// <summary>Number of messages waiting in the queue.</summary>
        public int QueueDepth => _queue.Count;

        /// <summary>Clears all queued messages (does not stop current transmission).</summary>
        public void Clear() => _queue.Clear();
    }
}
