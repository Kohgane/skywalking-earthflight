using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 78 — Detects player entry and exit from controlled airspace zones,
    /// classifies airspace as controlled vs uncontrolled, and enforces
    /// clearance requirements for zone entry.
    ///
    /// <para>Integrates with <c>SWEF.Flight.FlightController</c> for player
    /// world position (null-safe).</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class AirspaceController : MonoBehaviour
    {
        #region Inspector

        [Header("Airspace Database")]
        [Tooltip("All known airspace zone definitions. Zones may also be added at runtime.")]
        [SerializeField] private List<AirspaceZone> zones = new List<AirspaceZone>();

        [Tooltip("Altitude in feet MSL above which all airspace is considered Class A (IFR only).")]
        [SerializeField] private float classAFloorFt = 18000f;

        #endregion

        #region Events

        /// <summary>Fired when the player enters a new controlled airspace zone.</summary>
        public event Action<AirspaceZone> OnZoneEntered;

        /// <summary>Fired when the player exits a controlled airspace zone.</summary>
        public event Action<AirspaceZone> OnZoneExited;

        /// <summary>Fired when the player penetrates a zone without a valid clearance.</summary>
        public event Action<AirspaceZone> OnUnauthorizedEntry;

        #endregion

        #region Public Properties

        /// <summary>The zone the player is currently inside, or null if in uncontrolled airspace.</summary>
        public AirspaceZone CurrentZone { get; private set; }

        #endregion

        #region Private State

        private Transform _playerTransform;
        private readonly HashSet<AirspaceZone> _previousZones = new HashSet<AirspaceZone>();

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _playerTransform = Camera.main != null ? Camera.main.transform : transform;
        }

        private void Update()
        {
            if (_playerTransform == null) return;
            CheckZones(_playerTransform.position);
        }

        #endregion

        #region Public API

        /// <summary>Returns the innermost zone that contains <paramref name="position"/>.</summary>
        /// <param name="position">World-space position to test.</param>
        public AirspaceZone GetCurrentZone(Vector3 position)
        {
            AirspaceZone closest = null;
            float closestRadius  = float.MaxValue;

            foreach (var zone in zones)
            {
                if (IsInsideZone(position, zone) && zone.radius < closestRadius)
                {
                    closestRadius = zone.radius;
                    closest = zone;
                }
            }
            return closest;
        }

        /// <summary>Returns true if <paramref name="position"/> at <paramref name="altitudeFt"/>
        /// is within any controlled airspace zone.</summary>
        public bool IsInControlledAirspace(Vector3 position, float altitudeFt)
        {
            if (altitudeFt >= classAFloorFt) return true;
            return GetCurrentZone(position) != null;
        }

        /// <summary>Returns the <see cref="ATCFacilityType"/> managing the given zone.</summary>
        public ATCFacilityType GetZoneFacility(AirspaceZone zone)
        {
            return zone?.facilityType ?? ATCFacilityType.Center;
        }

        /// <summary>
        /// Checks whether a clearance is required to enter <paramref name="zone"/>.
        /// Fires <see cref="OnUnauthorizedEntry"/> if no clearance is held.
        /// </summary>
        /// <param name="zone">The zone to check.</param>
        /// <returns>True if entry is permitted; false if a clearance is required but not held.</returns>
        public bool RequestEntry(AirspaceZone zone)
        {
            if (zone == null) return true;
            var mgr = ATCManager.Instance;
            bool hasClearance = mgr != null && mgr.CurrentClearance != null;
            if (!hasClearance)
                OnUnauthorizedEntry?.Invoke(zone);
            return hasClearance;
        }

        /// <summary>Adds an airspace zone definition at runtime.</summary>
        public void AddZone(AirspaceZone zone)
        {
            if (zone != null && !zones.Contains(zone))
                zones.Add(zone);
        }

        #endregion

        #region Zone Detection

        private void CheckZones(Vector3 playerPos)
        {
            var currentZones = new HashSet<AirspaceZone>();

            foreach (var zone in zones)
            {
                if (IsInsideZone(playerPos, zone))
                    currentZones.Add(zone);
            }

            // Entered zones
            foreach (var zone in currentZones)
            {
                if (!_previousZones.Contains(zone))
                {
                    OnZoneEntered?.Invoke(zone);
                    RequestEntry(zone);
                }
            }

            // Exited zones
            foreach (var zone in _previousZones)
            {
                if (!currentZones.Contains(zone))
                    OnZoneExited?.Invoke(zone);
            }

            _previousZones.Clear();
            foreach (var zone in currentZones)
                _previousZones.Add(zone);

            CurrentZone = GetCurrentZone(playerPos);
        }

        private bool IsInsideZone(Vector3 position, AirspaceZone zone)
        {
            float lateralDist = Vector2.Distance(
                new Vector2(position.x, position.z),
                new Vector2(zone.center.x, zone.center.z));

            if (lateralDist > zone.radius) return false;

            float altFt = position.y * 3.28084f; // metres → feet
            return altFt >= zone.floorAltitude && altFt <= zone.ceilingAltitude;
        }

        #endregion
    }
}
