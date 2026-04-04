// Phase 100 — AI Co-Pilot & Smart Assistant
// Assets/SWEF/Scripts/AICoPilot/EmergencyAdvisor.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.AICoPilot
{
    /// <summary>
    /// Classifies the type of emergency situation detected by <see cref="EmergencyAdvisor"/>.
    /// </summary>
    public enum EmergencyType
    {
        /// <summary>One or more engines have failed.</summary>
        EngineFailure,

        /// <summary>Fuel level is critically low.</summary>
        LowFuel,

        /// <summary>Severe weather encountered (icing, severe turbulence, microburst).</summary>
        SevereWeather,

        /// <summary>Terrain collision risk — GPWS-style proximity warning.</summary>
        TerrainProximity,

        /// <summary>Aircraft system malfunction (hydraulics, electrical, etc.).</summary>
        SystemMalfunction,

        /// <summary>Aerodynamic stall detected.</summary>
        Stall
    }

    /// <summary>
    /// Monitors flight telemetry for emergency conditions, provides step-by-step procedure guidance,
    /// and integrates with the SWEF.Emergency module.
    /// Emergency advisories take priority over all other AI messages.
    /// </summary>
    [DisallowMultipleComponent]
    public class EmergencyAdvisor : MonoBehaviour
    {
        #region Events

        /// <summary>
        /// Fired when a new emergency situation is detected.
        /// </summary>
        /// <param name="type">Type of emergency.</param>
        /// <param name="procedure">First step of the emergency procedure.</param>
        public event Action<EmergencyType, string> OnEmergencyDetected;

        /// <summary>
        /// Fired when a previously active emergency is resolved.
        /// </summary>
        /// <param name="type">Type of emergency that was resolved.</param>
        public event Action<EmergencyType> OnEmergencyResolved;

        #endregion

        #region Inspector

        [Header("Detection Thresholds")]
        [Tooltip("Fuel percentage below which a LowFuel emergency is declared.")]
        [SerializeField][Range(0f, 1f)] private float _criticalFuelThreshold = 0.05f;

        [Tooltip("Altitude AGL (m) below which a TerrainProximity emergency is declared.")]
        [SerializeField] private float _gpwsAltitudeM = 100f;

        [Tooltip("Airspeed (m/s) at which aerodynamic stall is declared (with high AOA).")]
        [SerializeField] private float _stallSpeedMs = 35f;

        [Header("Cooldowns")]
        [Tooltip("Minimum seconds before re-announcing the same active emergency.")]
        [SerializeField] private float _repeatIntervalSeconds = 20f;

        #endregion

        #region Private State

        private readonly HashSet<EmergencyType> _activeEmergencies = new HashSet<EmergencyType>();
        private readonly Dictionary<EmergencyType, float> _lastAnnouncedTime = new Dictionary<EmergencyType, float>();

        // Simulated flight state.
        private float _fuelPercent = 1f;
        private float _altitudeAGL = 5000f;
        private float _airspeedMs  = 100f;
        private bool  _engineRunning = true;
        private bool  _systemMalfunction;
        private bool  _severeWeather;

        // Emergency procedure steps (localisation-ready string tables).
        private static readonly Dictionary<EmergencyType, string[]> ProcedureSteps =
            new Dictionary<EmergencyType, string[]>
            {
                { EmergencyType.EngineFailure,   new[] { "Maintain best glide speed.", "Declare MAYDAY on 121.5 MHz.", "Identify suitable landing site.", "Execute forced landing checklist." } },
                { EmergencyType.LowFuel,         new[] { "Divert to nearest airport immediately.", "Declare MAYDAY fuel.", "Request priority landing.", "Squawk 7700." } },
                { EmergencyType.SevereWeather,   new[] { "Exit weather system — turn 180° if possible.", "Reduce speed to turbulence penetration speed.", "Declare emergency if control is compromised.", "Request ATC vectors clear of weather." } },
                { EmergencyType.TerrainProximity, new[] { "PULL UP — maximum climb power.", "Raise flaps to takeoff setting.", "Turn away from terrain.", "Declare MAYDAY." } },
                { EmergencyType.SystemMalfunction, new[] { "Identify failed system via annunciator.", "Apply memory items from QRH.", "Declare PAN-PAN or MAYDAY as appropriate.", "Land at nearest suitable airport." } },
                { EmergencyType.Stall,           new[] { "Lower nose immediately — reduce angle of attack.", "Apply full power.", "Level wings.", "Recover to normal flight envelope." } }
            };

        #endregion

        #region Public API

        /// <summary>
        /// Called periodically by <see cref="AICoPilotManager"/> to evaluate emergency conditions.
        /// </summary>
        public void EvaluateEmergencies()
        {
            ReadTelemetry();

            CheckEngineFailure();
            CheckLowFuel();
            CheckTerrainProximity();
            CheckStall();
            CheckSevereWeather();
            CheckSystemMalfunction();
        }

        /// <summary>
        /// Updates the telemetry values used for emergency detection.
        /// </summary>
        /// <param name="fuelPercent">Fuel remaining as a fraction 0–1.</param>
        /// <param name="altitudeAGL">Altitude above ground level in metres.</param>
        /// <param name="airspeedMs">Indicated airspeed in m/s.</param>
        /// <param name="engineRunning">Whether the main engine(s) are running.</param>
        /// <param name="systemMalfunction">Whether a system malfunction is active.</param>
        /// <param name="severeWeather">Whether severe weather is being traversed.</param>
        public void UpdateTelemetry(float fuelPercent, float altitudeAGL, float airspeedMs,
                                    bool engineRunning, bool systemMalfunction, bool severeWeather)
        {
            _fuelPercent        = fuelPercent;
            _altitudeAGL        = altitudeAGL;
            _airspeedMs         = airspeedMs;
            _engineRunning      = engineRunning;
            _systemMalfunction  = systemMalfunction;
            _severeWeather      = severeWeather;
        }

        /// <summary>Returns true if the specified emergency type is currently active.</summary>
        /// <param name="type">Emergency type to query.</param>
        public bool IsEmergencyActive(EmergencyType type) => _activeEmergencies.Contains(type);

        #endregion

        #region Private Detection Methods

        private void ReadTelemetry()
        {
            // In production, bind to SWEF.Emergency or SWEF.Flight module.
        }

        private void CheckEngineFailure()
        {
            if (!_engineRunning && _altitudeAGL > 30f)
                DeclareEmergency(EmergencyType.EngineFailure);
            else
                ResolveIfActive(EmergencyType.EngineFailure, _engineRunning);
        }

        private void CheckLowFuel()
        {
            if (_fuelPercent <= _criticalFuelThreshold)
                DeclareEmergency(EmergencyType.LowFuel);
            else
                ResolveIfActive(EmergencyType.LowFuel, _fuelPercent > _criticalFuelThreshold);
        }

        private void CheckTerrainProximity()
        {
            if (_altitudeAGL < _gpwsAltitudeM && _airspeedMs > 10f)
                DeclareEmergency(EmergencyType.TerrainProximity);
            else
                ResolveIfActive(EmergencyType.TerrainProximity, _altitudeAGL >= _gpwsAltitudeM);
        }

        private void CheckStall()
        {
            if (_airspeedMs < _stallSpeedMs && _altitudeAGL > 30f)
                DeclareEmergency(EmergencyType.Stall);
            else
                ResolveIfActive(EmergencyType.Stall, _airspeedMs >= _stallSpeedMs);
        }

        private void CheckSevereWeather()
        {
            if (_severeWeather)
                DeclareEmergency(EmergencyType.SevereWeather);
            else
                ResolveIfActive(EmergencyType.SevereWeather, !_severeWeather);
        }

        private void CheckSystemMalfunction()
        {
            if (_systemMalfunction)
                DeclareEmergency(EmergencyType.SystemMalfunction);
            else
                ResolveIfActive(EmergencyType.SystemMalfunction, !_systemMalfunction);
        }

        private void DeclareEmergency(EmergencyType type)
        {
            bool isNew = _activeEmergencies.Add(type);

            if (isNew)
            {
                string procedure = GetFirstStep(type);
                OnEmergencyDetected?.Invoke(type, procedure);
                _lastAnnouncedTime[type] = Time.time;
                return;
            }

            // Re-announce at interval.
            if (_lastAnnouncedTime.TryGetValue(type, out float lastTime) &&
                Time.time - lastTime >= _repeatIntervalSeconds)
            {
                _lastAnnouncedTime[type] = Time.time;
                OnEmergencyDetected?.Invoke(type, GetFirstStep(type));
            }
        }

        private void ResolveIfActive(EmergencyType type, bool resolved)
        {
            if (!resolved) return;
            if (_activeEmergencies.Remove(type))
                OnEmergencyResolved?.Invoke(type);
        }

        private static string GetFirstStep(EmergencyType type)
        {
            if (ProcedureSteps.TryGetValue(type, out var steps) && steps.Length > 0)
                return steps[0];
            return "Follow emergency checklist.";
        }

        #endregion
    }
}
