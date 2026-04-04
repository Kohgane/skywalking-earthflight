// Phase 100 — AI Co-Pilot & Smart Assistant
// Assets/SWEF/Scripts/AICoPilot/NavigationAssistant.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.AICoPilot
{
    /// <summary>
    /// Provides turn-by-turn navigation guidance and waypoint callouts.
    /// Integrates with the SWEF.FlightPlan module for waypoint-based routing.
    /// </summary>
    [DisallowMultipleComponent]
    public class NavigationAssistant : MonoBehaviour
    {
        #region Events

        /// <summary>Fired when ARIA issues a navigation callout message.</summary>
        /// <param name="message">Navigation guidance text.</param>
        public event Action<string> OnNavigationCallout;

        /// <summary>Fired when the aircraft is approaching a waypoint.</summary>
        /// <param name="waypointName">Name of the approaching waypoint.</param>
        /// <param name="distanceKm">Current distance to the waypoint in kilometres.</param>
        public event Action<string, float> OnWaypointApproaching;

        #endregion

        #region Inspector

        [Header("Waypoint Alerting")]
        [Tooltip("Distance (km) at which a 'waypoint approaching' callout is issued.")]
        [SerializeField] private float _waypointAlertDistanceKm = 10f;

        [Tooltip("Distance (km) at which a short final callout is issued.")]
        [SerializeField] private float _finalApproachDistanceKm = 2f;

        [Header("Heading")]
        [Tooltip("Heading deviation (degrees) before a correction suggestion is issued.")]
        [SerializeField] private float _headingDeviationDeg = 15f;

        [Header("Cooldowns")]
        [Tooltip("Minimum seconds between repeated navigation callouts of the same type.")]
        [SerializeField] private float _calloutCooldownSeconds = 30f;

        #endregion

        #region Private State

        private readonly Dictionary<string, float> _cooldownTimestamps = new Dictionary<string, float>();

        // Simulated navigation state — replace with FlightPlan module bindings.
        private string _nextWaypointName = string.Empty;
        private float _nextWaypointDistanceKm;
        private float _nextWaypointBearingDeg;
        private float _currentHeadingDeg;
        private string _destinationName = string.Empty;
        private float _destinationDistanceKm;
        private float _etaSeconds;

        private bool _waypointAlertFired;
        private bool _finalApproachAlertFired;

        #endregion

        #region Public API

        /// <summary>
        /// Called periodically by <see cref="AICoPilotManager"/> to evaluate navigation state.
        /// </summary>
        public void EvaluateNavigation()
        {
            ReadNavigationState();

            CheckWaypointProximity();
            CheckHeadingDeviation();
            CheckETA();
        }

        /// <summary>
        /// Updates simulated navigation state.
        /// </summary>
        /// <param name="waypointName">Name of the active next waypoint.</param>
        /// <param name="waypointDistanceKm">Distance to next waypoint in km.</param>
        /// <param name="waypointBearingDeg">True bearing to next waypoint.</param>
        /// <param name="currentHeadingDeg">Aircraft current heading.</param>
        /// <param name="destinationName">Name of the final destination.</param>
        /// <param name="destinationDistanceKm">Distance to destination in km.</param>
        /// <param name="etaSeconds">Estimated time to destination in seconds.</param>
        public void UpdateNavigationState(string waypointName, float waypointDistanceKm,
                                          float waypointBearingDeg, float currentHeadingDeg,
                                          string destinationName, float destinationDistanceKm,
                                          float etaSeconds)
        {
            bool waypointChanged = waypointName != _nextWaypointName;
            if (waypointChanged)
            {
                _waypointAlertFired = false;
                _finalApproachAlertFired = false;
            }

            _nextWaypointName        = waypointName;
            _nextWaypointDistanceKm  = waypointDistanceKm;
            _nextWaypointBearingDeg  = waypointBearingDeg;
            _currentHeadingDeg       = currentHeadingDeg;
            _destinationName         = destinationName;
            _destinationDistanceKm   = destinationDistanceKm;
            _etaSeconds              = etaSeconds;
        }

        #endregion

        #region Private Methods

        private void ReadNavigationState()
        {
            // In production, pull from SWEF.FlightPlan.FlightPlanManager or similar.
        }

        private void CheckWaypointProximity()
        {
            if (string.IsNullOrEmpty(_nextWaypointName)) return;

            if (_nextWaypointDistanceKm <= _finalApproachDistanceKm && !_finalApproachAlertFired)
            {
                _finalApproachAlertFired = true;
                string msg = $"Approaching {_nextWaypointName} — {_nextWaypointDistanceKm:F1} km. Prepare for waypoint passage.";
                OnNavigationCallout?.Invoke(msg);
                OnWaypointApproaching?.Invoke(_nextWaypointName, _nextWaypointDistanceKm);
            }
            else if (_nextWaypointDistanceKm <= _waypointAlertDistanceKm && !_waypointAlertFired)
            {
                _waypointAlertFired = true;
                string msg = $"Waypoint {_nextWaypointName} in {_nextWaypointDistanceKm:F0} km.";
                OnNavigationCallout?.Invoke(msg);
                OnWaypointApproaching?.Invoke(_nextWaypointName, _nextWaypointDistanceKm);
            }
        }

        private void CheckHeadingDeviation()
        {
            if (string.IsNullOrEmpty(_nextWaypointName)) return;

            float deviation = AngleDelta(_currentHeadingDeg, _nextWaypointBearingDeg);
            if (Mathf.Abs(deviation) < _headingDeviationDeg) return;

            if (!CanIssue("heading_correction")) return;
            MarkCooldown("heading_correction");

            string dir = deviation > 0 ? "right" : "left";
            int targetHeading = Mathf.RoundToInt(_nextWaypointBearingDeg) % 360;
            string msg = $"Turn {dir} heading {targetHeading:000} to intercept {_nextWaypointName}.";
            OnNavigationCallout?.Invoke(msg);
        }

        private void CheckETA()
        {
            if (string.IsNullOrEmpty(_destinationName) || _etaSeconds <= 0f) return;
            if (!CanIssue("eta_callout")) return;

            int etaMin = Mathf.RoundToInt(_etaSeconds / 60f);
            if (etaMin == 60 || etaMin == 30 || etaMin == 10 || etaMin == 5)
            {
                MarkCooldown("eta_callout");
                string msg = $"{_destinationName} — {etaMin} minutes remaining. Distance {_destinationDistanceKm:F0} km.";
                OnNavigationCallout?.Invoke(msg);
            }
        }

        private static float AngleDelta(float from, float to)
        {
            return (to - from + 540f) % 360f - 180f;
        }

        private bool CanIssue(string key)
        {
            if (!_cooldownTimestamps.TryGetValue(key, out float lastTime)) return true;
            return Time.time - lastTime >= _calloutCooldownSeconds;
        }

        private void MarkCooldown(string key) => _cooldownTimestamps[key] = Time.time;

        #endregion
    }
}
