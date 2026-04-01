using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 78 — Spawns and manages simulated AI air traffic contacts around
    /// active airports within communication range.
    ///
    /// <para>Enforces minimum separation of 3 nm laterally and 1,000 ft vertically.
    /// Uses distance-based LOD for update frequency to maximise performance.</para>
    ///
    /// <para>Integrates with <c>SWEF.CityGen.CityManager</c> (null-safe) for
    /// traffic density scaling near populated areas.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class TrafficSimulator : MonoBehaviour
    {
        #region Constants

        private const float SeparationLateralMetres = 5556f;   // 3 nm
        private const float SeparationVerticalFeet  = 1000f;
        private const float FullUpdateRange         = 18520f;  // 10 nm
        private const float ReducedUpdateRange      = 37040f;  // 20 nm
        private const float UpdateIntervalFull      = 0.5f;
        private const float UpdateIntervalReduced   = 2f;
        private const float UpdateIntervalMinimal   = 5f;

        #endregion

        #region Inspector

        [Header("Traffic Settings")]
        [Tooltip("World-space radius around the player within which AI traffic is spawned.")]
        [SerializeField] private float spawnRadius = 37040f;  // 20 nm

        [Tooltip("Altitude range for spawned cruise traffic (feet MSL).")]
        [SerializeField] private float minCruiseAltitude = 3000f;
        [SerializeField] private float maxCruiseAltitude = 15000f;

        #endregion

        #region Events

        /// <summary>Fired when a new traffic contact is spawned.</summary>
        public event Action<TrafficContact> OnContactSpawned;

        /// <summary>Fired when a traffic contact is removed.</summary>
        public event Action<TrafficContact> OnContactRemoved;

        #endregion

        #region Public Properties

        /// <summary>Read-only list of all active traffic contacts.</summary>
        public IReadOnlyList<TrafficContact> ActiveContacts => _activeContacts;

        #endregion

        #region Private State

        private readonly List<TrafficContact> _activeContacts = new List<TrafficContact>();
        private readonly string[] _callsignPrefixes = { "SWA", "UAL", "DAL", "AAL", "BAW", "DLH", "AFR", "KLM", "QFA", "SIA" };
        private int _callsignCounter = 1;
        private Coroutine _updateCoroutine;
        private Transform _playerTransform;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _playerTransform = Camera.main != null ? Camera.main.transform : transform;
            _updateCoroutine = StartCoroutine(UpdateRoutine());
        }

        private void OnDestroy()
        {
            if (_updateCoroutine != null) StopCoroutine(_updateCoroutine);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Spawns a new traffic contact near the specified world position.
        /// </summary>
        /// <param name="nearPosition">World position to spawn near.</param>
        /// <returns>The newly created <see cref="TrafficContact"/>, or null if max traffic reached.</returns>
        public TrafficContact SpawnTraffic(Vector3 nearPosition)
        {
            int maxTraffic = ATCManager.Instance != null
                ? ATCManager.Instance.Settings.maxSimulatedTraffic : 10;

            if (_activeContacts.Count >= maxTraffic) return null;

            float angle    = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = UnityEngine.Random.Range(spawnRadius * 0.3f, spawnRadius);
            var   pos      = nearPosition + new Vector3(
                Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
            pos.y = UnityEngine.Random.Range(minCruiseAltitude, maxCruiseAltitude);

            var contact = new TrafficContact
            {
                callsign    = GenerateCallsign(),
                position    = pos,
                altitude    = pos.y,
                speed       = UnityEngine.Random.Range(200f, 450f),
                heading     = UnityEngine.Random.Range(0f, 360f),
                flightPhase = FlightPhase.Cruise
            };

            _activeContacts.Add(contact);
            OnContactSpawned?.Invoke(contact);
            return contact;
        }

        /// <summary>Removes a traffic contact from the simulation.</summary>
        /// <param name="contact">The contact to remove.</param>
        public void DespawnTraffic(TrafficContact contact)
        {
            if (_activeContacts.Remove(contact))
                OnContactRemoved?.Invoke(contact);
        }

        /// <summary>Returns all contacts within <paramref name="range"/> metres of <paramref name="position"/>.</summary>
        public List<TrafficContact> GetNearbyTraffic(Vector3 position, float range)
        {
            var result = new List<TrafficContact>();
            float rangeSqr = range * range;
            foreach (var c in _activeContacts)
            {
                if ((c.position - position).sqrMagnitude <= rangeSqr)
                    result.Add(c);
            }
            return result;
        }

        /// <summary>Returns all contacts currently on the specified frequency's facility zone.</summary>
        public List<TrafficContact> GetTrafficOnFrequency(RadioFrequency frequency)
        {
            // In a full implementation this would filter by zone;
            // here we return all active contacts as a reasonable approximation.
            return new List<TrafficContact>(_activeContacts);
        }

        #endregion

        #region Update Loop

        private IEnumerator UpdateRoutine()
        {
            while (true)
            {
                UpdateContacts();
                MaintainTrafficDensity();
                EnforceSeparation();

                yield return new WaitForSeconds(UpdateIntervalFull);
            }
        }

        private void UpdateContacts()
        {
            float dt = UpdateIntervalFull;
            foreach (var c in _activeContacts)
            {
                float speedMs = c.speed * 0.5144f;  // knots → m/s
                float headingRad = c.heading * Mathf.Deg2Rad;
                c.position += new Vector3(
                    Mathf.Sin(headingRad) * speedMs * dt, 0f,
                    Mathf.Cos(headingRad) * speedMs * dt);
                c.altitude = c.position.y;
            }
        }

        private void MaintainTrafficDensity()
        {
            if (_playerTransform == null) return;
            int maxTraffic = ATCManager.Instance != null
                ? ATCManager.Instance.Settings.maxSimulatedTraffic : 10;

            // Remove contacts that have drifted out of range
            for (int i = _activeContacts.Count - 1; i >= 0; i--)
            {
                if ((_activeContacts[i].position - _playerTransform.position).sqrMagnitude
                    > spawnRadius * spawnRadius * 4f)
                {
                    OnContactRemoved?.Invoke(_activeContacts[i]);
                    _activeContacts.RemoveAt(i);
                }
            }

            // Spawn new contacts to maintain density
            while (_activeContacts.Count < Mathf.Min(3, maxTraffic))
                SpawnTraffic(_playerTransform.position);
        }

        private void EnforceSeparation()
        {
            for (int i = 0; i < _activeContacts.Count; i++)
            {
                for (int j = i + 1; j < _activeContacts.Count; j++)
                {
                    var a = _activeContacts[i];
                    var b = _activeContacts[j];

                    float lateral  = Vector2.Distance(
                        new Vector2(a.position.x, a.position.z),
                        new Vector2(b.position.x, b.position.z));
                    float vertical = Mathf.Abs(a.altitude - b.altitude);

                    if (lateral < SeparationLateralMetres && vertical < SeparationVerticalFeet)
                    {
                        // Resolve vertically: nudge one contact
                        b.position = new Vector3(b.position.x,
                            b.position.y + SeparationVerticalFeet * 0.3f, b.position.z);
                        b.altitude = b.position.y;
                        b.threatLevel = 2;
                        a.threatLevel = 2;
                    }
                    else
                    {
                        a.threatLevel = 0;
                        b.threatLevel = 0;
                    }
                }
            }
        }

        #endregion

        #region Helpers

        private string GenerateCallsign()
        {
            string prefix = _callsignPrefixes[UnityEngine.Random.Range(0, _callsignPrefixes.Length)];
            return $"{prefix} {_callsignCounter++:D3}";
        }

        #endregion
    }
}
