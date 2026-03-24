using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Emergency
{
    /// <summary>
    /// Phase 76 — Post-landing / crash rescue dispatch simulation.
    /// Selects appropriate rescue unit types by location (airport, water, remote),
    /// moves units toward the incident site, and fires arrival events.
    /// Integrates with SWEF.Analytics (null-safe).
    /// </summary>
    [DisallowMultipleComponent]
    public class RescueSimulationController : MonoBehaviour
    {
        #region Inspector

        [Header("Configuration")]
        [Tooltip("Base speed of ground rescue units in m/s.")]
        [SerializeField] private float groundUnitSpeed = 20f;

        [Tooltip("Speed of helicopter rescue units in m/s.")]
        [SerializeField] private float helicopterSpeed = 60f;

        [Tooltip("Speed of coast guard vessels in m/s.")]
        [SerializeField] private float coastGuardSpeed = 15f;

        [Tooltip("Dispatch origins for each rescue unit type (world positions).")]
        [SerializeField] private List<Vector3> rescueBasePositions = new List<Vector3>();

        #endregion

        #region Events

        /// <summary>Fired when a rescue unit is dispatched.</summary>
        public event Action<RescueUnit> OnUnitDispatched;

        /// <summary>Fired when a rescue unit arrives at the scene.</summary>
        public event Action<RescueUnit> OnUnitArrived;

        /// <summary>Fired when all dispatched units have arrived.</summary>
        public event Action OnAllUnitsArrived;

        #endregion

        #region Private State

        private readonly List<RescueUnit> _activeUnits = new List<RescueUnit>();
        private readonly List<Coroutine> _moveCoroutines = new List<Coroutine>();
        private static int _unitIdCounter;

        #endregion

        #region Public API

        /// <summary>Dispatch appropriate rescue units for the resolved emergency.</summary>
        /// <param name="resolution">The outcome record of the resolved emergency.</param>
        /// <param name="incidentPosition">World position of the incident.</param>
        /// <param name="landingSite">Landing site used, if any.</param>
        public void DispatchRescue(EmergencyResolution resolution, Vector3 incidentPosition,
            EmergencyLandingSite landingSite = null)
        {
            _activeUnits.Clear();
            foreach (var c in _moveCoroutines)
                if (c != null) StopCoroutine(c);
            _moveCoroutines.Clear();

            var unitTypes = SelectUnitTypes(resolution, landingSite);
            foreach (var unitType in unitTypes)
            {
                var unit = CreateUnit(unitType, incidentPosition);
                _activeUnits.Add(unit);
                OnUnitDispatched?.Invoke(unit);

                var co = StartCoroutine(MoveUnit(unit));
                _moveCoroutines.Add(co);
            }

            ReportAnalytics(resolution, unitTypes.Count);
        }

        /// <summary>Read-only view of all currently active rescue units.</summary>
        public IReadOnlyList<RescueUnit> ActiveUnits => _activeUnits;

        #endregion

        #region Private Helpers

        private List<RescueUnitType> SelectUnitTypes(EmergencyResolution resolution,
            EmergencyLandingSite site)
        {
            var types = new List<RescueUnitType>();

            if (site != null && site.isWaterLanding)
            {
                types.Add(RescueUnitType.CoastGuard);
                types.Add(RescueUnitType.Helicopter);
            }
            else if (site != null && site.isFieldLanding)
            {
                types.Add(RescueUnitType.Helicopter);
                types.Add(RescueUnitType.Ambulance);
            }
            else
            {
                types.Add(RescueUnitType.FireTruck);
                types.Add(RescueUnitType.Ambulance);
            }

            if (resolution.finalPhase == EmergencyPhase.Crashed)
                types.Add(RescueUnitType.Helicopter);

            return types;
        }

        private RescueUnit CreateUnit(RescueUnitType type, Vector3 target)
        {
            Vector3 origin = rescueBasePositions.Count > 0
                ? rescueBasePositions[UnityEngine.Random.Range(0, rescueBasePositions.Count)]
                : target + UnityEngine.Random.insideUnitSphere * 3000f;

            float speed = type == RescueUnitType.Helicopter ? helicopterSpeed
                        : type == RescueUnitType.CoastGuard  ? coastGuardSpeed
                        : groundUnitSpeed;

            float dist = Vector3.Distance(origin, target);
            float arrival = dist / Mathf.Max(speed, 0.1f);

            return new RescueUnit
            {
                unitId         = $"rescue_{++_unitIdCounter:D4}",
                type           = type,
                position       = origin,
                targetPosition = target,
                speed          = speed,
                arrivalTime    = arrival,
                hasArrived     = false,
                displayNameKey = $"rescue_unit_{type.ToString().ToLowerInvariant()}"
            };
        }

        private IEnumerator MoveUnit(RescueUnit unit)
        {
            while (!unit.hasArrived)
            {
                float step = unit.speed * Time.deltaTime;
                unit.position = Vector3.MoveTowards(unit.position, unit.targetPosition, step);
                unit.arrivalTime = Vector3.Distance(unit.position, unit.targetPosition)
                                   / Mathf.Max(unit.speed, 0.1f);

                if (Vector3.Distance(unit.position, unit.targetPosition) < 1f)
                {
                    unit.position   = unit.targetPosition;
                    unit.hasArrived = true;
                    unit.arrivalTime = 0f;
                    OnUnitArrived?.Invoke(unit);

                    if (_activeUnits.TrueForAll(u => u.hasArrived))
                        OnAllUnitsArrived?.Invoke();
                }

                yield return null;
            }
        }

        private void ReportAnalytics(EmergencyResolution resolution, int unitCount)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.Instance?.Track("emergency_rescue_dispatched", new System.Collections.Generic.Dictionary<string, object>
            {
                { "emergency_type", resolution.type.ToString() },
                { "unit_count",     unitCount },
                { "was_successful", resolution.wasSuccessful }
            });
#endif
        }

        #endregion
    }
}
