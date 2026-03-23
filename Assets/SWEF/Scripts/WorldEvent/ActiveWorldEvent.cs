// ActiveWorldEvent.cs — SWEF Dynamic Event & World Quest System (Phase 64)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.WorldEvent
{
    /// <summary>
    /// Runtime MonoBehaviour attached to a world-space GameObject that represents a
    /// live instance of a world event.  Manages objective tracking, lifecycle
    /// transitions, and the visual beacon pillar visible from distance.
    /// </summary>
    public sealed class ActiveWorldEvent : MonoBehaviour
    {
        // ── Data ─────────────────────────────────────────────────────────────────

        [Header("Event Data")]
        [Tooltip("Template ScriptableObject that defines this event's rules and rewards.")]
        /// <summary>Template ScriptableObject that defines this event's rules and rewards.</summary>
        public WorldEventData eventData;

        // ── Runtime State ────────────────────────────────────────────────────────

        /// <summary>Current lifecycle status of this event instance.</summary>
        public EventStatus status { get; private set; } = EventStatus.Pending;

        /// <summary>World-space position of this event.</summary>
        public Vector3 worldPosition { get; private set; }

        /// <summary>Remaining seconds until this event expires.</summary>
        public float remainingTime { get; private set; }

        /// <summary>Distance from this event to the player; updated every frame.</summary>
        public float distanceToPlayer { get; private set; }

        /// <summary>Whether the player has accepted and is actively tracking this event.</summary>
        public bool isTracked { get; set; }

        [Header("Objectives")]
        [Tooltip("All objectives that must be completed to finish this event.")]
        /// <summary>All objectives that must be completed to finish this event.</summary>
        public List<EventObjective> objectives = new List<EventObjective>();

        /// <summary>Fraction of objectives completed (0 = none, 1 = all).</summary>
        public float completionPercent
        {
            get
            {
                if (objectives == null || objectives.Count == 0) return 0f;
                int completed = 0;
                foreach (var obj in objectives)
                    if (obj.isCompleted) completed++;
                return (float)completed / objectives.Count;
            }
        }

        // ── Visual ───────────────────────────────────────────────────────────────

        [Header("Visuals")]
        [Tooltip("Optional particle system used for the pulsing beacon light.")]
        /// <summary>Optional particle system used for the pulsing beacon light.</summary>
        [SerializeField] private ParticleSystem _beaconParticles;

        [Tooltip("Optional light component that pulses at the event location.")]
        /// <summary>Optional light component that pulses at the event location.</summary>
        [SerializeField] private Light _beaconLight;

        private Transform _playerTransform;
        private Coroutine _expiryCoroutine;
        private float _beaconPulseTime;

        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Raised whenever the event transitions to a new <see cref="EventStatus"/>.</summary>
        public event Action<EventStatus> OnStatusChanged;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Start()
        {
            worldPosition = transform.position;
            remainingTime = eventData != null ? eventData.duration : 180f;

            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                _playerTransform = playerObj.transform;
        }

        private void Update()
        {
            if (status == EventStatus.Pending || status == EventStatus.Active)
            {
                if (_playerTransform != null)
                    distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

                UpdateBeaconVisuals();
            }
        }

        /// <summary>
        /// Called when the player enters the event area.  Starts objective tracking
        /// and begins the expiry countdown.
        /// </summary>
        public void Activate()
        {
            if (status != EventStatus.Pending) return;

            status = EventStatus.Active;
            OnStatusChanged?.Invoke(status);

            _expiryCoroutine = StartCoroutine(ExpiryCountdown());
        }

        /// <summary>
        /// Marks the event as successfully completed; notifies the manager and
        /// stops the expiry coroutine.
        /// </summary>
        public void Complete()
        {
            if (status != EventStatus.Active) return;

            StopExpiryCoroutine();
            status = EventStatus.Completed;
            OnStatusChanged?.Invoke(status);

            StopBeacon();
            WorldEventManager.Instance?.NotifyEventCompleted(this);
        }

        /// <summary>
        /// Marks the event as failed; notifies the manager.
        /// </summary>
        public void Fail()
        {
            if (status != EventStatus.Active && status != EventStatus.Pending) return;

            StopExpiryCoroutine();
            status = EventStatus.Failed;
            OnStatusChanged?.Invoke(status);

            StopBeacon();
            WorldEventManager.Instance?.NotifyEventFailed(this);
        }

        /// <summary>
        /// Called by the expiry coroutine when time runs out.
        /// </summary>
        public void Expire()
        {
            if (status == EventStatus.Completed || status == EventStatus.Failed) return;

            status = EventStatus.Expired;
            OnStatusChanged?.Invoke(status);

            StopBeacon();
            WorldEventManager.Instance?.NotifyEventExpired(this);
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private IEnumerator ExpiryCountdown()
        {
            while (remainingTime > 0f)
            {
                yield return null;
                // Safety guard: coroutine may still be running for one frame after StopExpiryCoroutine
                // is called from another code path (e.g. Complete/Fail triggered externally).
                if (status != EventStatus.Active && status != EventStatus.Pending) yield break;
                remainingTime -= Time.deltaTime;
            }

            remainingTime = 0f;
            Expire();
        }

        private void StopExpiryCoroutine()
        {
            if (_expiryCoroutine != null)
            {
                StopCoroutine(_expiryCoroutine);
                _expiryCoroutine = null;
            }
        }

        private void UpdateBeaconVisuals()
        {
            _beaconPulseTime += Time.deltaTime;
            float pulse = 0.5f + 0.5f * Mathf.Sin(_beaconPulseTime * Mathf.PI * 2f);

            if (_beaconLight != null)
                _beaconLight.intensity = Mathf.Lerp(0.5f, 2f, pulse);
        }

        private void StopBeacon()
        {
            if (_beaconParticles != null)
                _beaconParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            if (_beaconLight != null)
                _beaconLight.enabled = false;
        }

        private void OnDestroy()
        {
            StopExpiryCoroutine();
        }
    }
}
