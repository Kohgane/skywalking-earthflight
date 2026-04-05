// NPCAircraftController.cs — Phase 110: Dynamic NPC & Air Traffic Ecosystem
// Individual NPC AI behaviour: state machine, TCAS, route following.
// Namespace: SWEF.NPCTraffic

using System;
using System.Collections;
using UnityEngine;

namespace SWEF.NPCTraffic
{
    /// <summary>
    /// Phase 110 — Controls a single NPC aircraft's AI behaviour.
    /// Implements the state machine (Taxiing → Takeoff → Climbing → Cruising →
    /// Descending → Approach → Landing → Holding → Emergency), route following,
    /// and TCAS-like collision avoidance with the player and other NPCs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NPCAircraftController : MonoBehaviour
    {
        #region Events

        /// <summary>Fired when the NPC transitions to a new behaviour state.</summary>
        public event Action<NPCBehaviorState> OnStateChanged;

        /// <summary>Fired when the NPC completes its route and lands.</summary>
        public event Action<string> OnRouteCompleted;

        /// <summary>Fired when a TCAS resolution advisory is issued.</summary>
        public event Action<string, Vector3> OnTCASAdvisory;

        #endregion

        #region Public State

        /// <summary>Aircraft data snapshot managed by this controller.</summary>
        public NPCAircraftData Data { get; private set; }

        /// <summary>Currently assigned route.</summary>
        public NPCRoute AssignedRoute { get; private set; }

        #endregion

        #region Inspector

        [Header("TCAS")]
        [Tooltip("Minimum safe separation distance in metres.")]
        [SerializeField] private float _tcasSeparationMetres = 300f;

        [Tooltip("Seconds between TCAS scan cycles.")]
        [SerializeField] private float _tcasScanIntervalSeconds = 2f;

        #endregion

        #region Private State

        private NPCFlightProfile _profile;
        private Coroutine        _aiCoroutine;
        private Coroutine        _tcasCoroutine;
        private bool             _initialized;

        #endregion

        #region Public API

        /// <summary>
        /// Initialises the controller with aircraft data and route.
        /// Call immediately after spawning before enabling updates.
        /// </summary>
        /// <param name="data">NPC aircraft data to control.</param>
        /// <param name="route">Initial flight route.</param>
        public void Initialise(NPCAircraftData data, NPCRoute route)
        {
            Data           = data;
            AssignedRoute  = route;

            _profile = NPCTrafficManager.Instance != null
                ? NPCTrafficManager.Instance.GetProfile(data.Category)
                : new NPCFlightProfile { Category = data.Category, CruiseSpeedKnots = 200f };

            _initialized = true;
        }

        /// <summary>Assigns a new route and restarts the AI coroutine.</summary>
        /// <param name="route">New route to fly.</param>
        public void AssignRoute(NPCRoute route)
        {
            AssignedRoute = route;
            if (_initialized && isActiveAndEnabled)
            {
                if (_aiCoroutine != null) StopCoroutine(_aiCoroutine);
                _aiCoroutine = StartCoroutine(AILoop());
            }
        }

        /// <summary>
        /// Commands the NPC into a holding pattern at its current position.
        /// </summary>
        public void EnterHolding()
        {
            TransitionTo(NPCBehaviorState.Holding);
        }

        /// <summary>
        /// Commands the NPC to execute an emergency diversion.
        /// </summary>
        public void DeclareEmergency()
        {
            TransitionTo(NPCBehaviorState.Emergency);
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (_initialized)
            {
                _aiCoroutine   = StartCoroutine(AILoop());
                _tcasCoroutine = StartCoroutine(TCASLoop());
            }
        }

        private void OnDisable()
        {
            if (_aiCoroutine   != null) StopCoroutine(_aiCoroutine);
            if (_tcasCoroutine != null) StopCoroutine(_tcasCoroutine);
        }

        #endregion

        #region Private — AI State Machine

        private IEnumerator AILoop()
        {
            if (Data == null) yield break;

            TransitionTo(NPCBehaviorState.Taxiing);
            yield return new WaitForSeconds(UnityEngine.Random.Range(2f, 8f));

            TransitionTo(NPCBehaviorState.Takeoff);
            yield return new WaitForSeconds(1.5f);

            TransitionTo(NPCBehaviorState.Climbing);
            yield return ClimbToAltitude();

            TransitionTo(NPCBehaviorState.Cruising);
            yield return FollowRoute();

            TransitionTo(NPCBehaviorState.Descending);
            yield return DescendToApproach();

            TransitionTo(NPCBehaviorState.Approach);
            yield return new WaitForSeconds(UnityEngine.Random.Range(60f, 120f));

            TransitionTo(NPCBehaviorState.Landing);
            yield return new WaitForSeconds(UnityEngine.Random.Range(20f, 40f));

            TransitionTo(NPCBehaviorState.Parked);
            OnRouteCompleted?.Invoke(Data.Id);

            if (AssignedRoute != null && AssignedRoute.IsLooping)
            {
                AssignedRoute.CurrentWaypointIndex = 0;
                _aiCoroutine = StartCoroutine(AILoop());
            }
        }

        private IEnumerator ClimbToAltitude()
        {
            if (Data == null || _profile == null) yield break;

            float targetAlt = AssignedRoute?.CruiseAltitudeMetres ?? _profile.CruiseAltitudeMetres;
            while (Data.AltitudeMetres < targetAlt - 50f)
            {
                Data.AltitudeMetres += _profile.ClimbRateMs * Time.deltaTime;
                Data.SpeedKnots     =  Mathf.Lerp(Data.SpeedKnots, _profile.CruiseSpeedKnots, Time.deltaTime * 0.5f);
                yield return null;
            }
            Data.AltitudeMetres = targetAlt;
        }

        private IEnumerator DescendToApproach()
        {
            if (Data == null || _profile == null) yield break;

            const float approachAlt = 300f;
            while (Data.AltitudeMetres > approachAlt + 20f)
            {
                Data.AltitudeMetres -= _profile.DescentRateMs * Time.deltaTime;
                Data.SpeedKnots     =  Mathf.Lerp(Data.SpeedKnots, _profile.MinSpeedKnots * 1.3f, Time.deltaTime * 0.3f);
                yield return null;
            }
            Data.AltitudeMetres = approachAlt;
        }

        private IEnumerator FollowRoute()
        {
            if (AssignedRoute == null || AssignedRoute.Waypoints.Count == 0) yield break;

            while (AssignedRoute.CurrentWaypointIndex < AssignedRoute.Waypoints.Count)
            {
                NPCWaypoint target = AssignedRoute.Waypoints[AssignedRoute.CurrentWaypointIndex];
                yield return MoveTowardWaypoint(target);
                AssignedRoute.CurrentWaypointIndex++;
            }
        }

        private IEnumerator MoveTowardWaypoint(NPCWaypoint waypoint)
        {
            if (Data == null || _profile == null) yield break;

            float speedMs = _profile.CruiseSpeedKnots * 0.5144f; // knots → m/s
            while (true)
            {
                Vector3 delta = waypoint.WorldPosition - Data.WorldPosition;
                float   dist  = delta.magnitude;

                if (dist < 500f) yield break;

                Data.WorldPosition += delta.normalized * speedMs * Time.deltaTime;

                float heading = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
                if (heading < 0f) heading += 360f;
                Data.HeadingDeg = Mathf.MoveTowardsAngle(Data.HeadingDeg, heading,
                    _profile.TurnRateDegPerSec * Time.deltaTime);

                yield return null;
            }
        }

        #endregion

        #region Private — TCAS

        private IEnumerator TCASLoop()
        {
            var wait = new WaitForSeconds(_tcasScanIntervalSeconds);
            while (true)
            {
                yield return wait;
                RunTCASScan();
            }
        }

        private void RunTCASScan()
        {
            if (Data == null || NPCTrafficManager.Instance == null) return;

            float threatDistSq = _tcasSeparationMetres * _tcasSeparationMetres;

            foreach (NPCAircraftData other in NPCTrafficManager.Instance.ActiveNPCs)
            {
                if (other.Id == Data.Id) continue;

                if ((other.WorldPosition - Data.WorldPosition).sqrMagnitude < threatDistSq)
                {
                    OnTCASAdvisory?.Invoke(other.Callsign, other.WorldPosition);
                    Data.AltitudeMetres += 300f; // simple climb RA
                    break;
                }
            }
        }

        #endregion

        #region Private — Helpers

        private void TransitionTo(NPCBehaviorState newState)
        {
            if (Data == null) return;
            Data.BehaviorState = newState;
            OnStateChanged?.Invoke(newState);
        }

        #endregion
    }
}
