using System;
using System.Collections.Generic;
using UnityEngine;

#if SWEF_WEATHER_AVAILABLE
using SWEF.Weather;
#endif

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 78 — Manages runway assignments and operational status for all
    /// runways registered at the current airport.
    ///
    /// <para>Selects active runways based on wind direction when
    /// <c>SWEF_WEATHER_AVAILABLE</c> is defined and <see cref="WeatherManager"/>
    /// is available; otherwise defaults to the first active runway.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class RunwayManager : MonoBehaviour
    {
        #region Inspector

        [Header("Runway Database")]
        [Tooltip("All runways at the current airport.")]
        [SerializeField] private List<RunwayInfo> runways = new List<RunwayInfo>();

        #endregion

        #region Events

        /// <summary>Fired when a runway is assigned to the player.</summary>
        public event Action<RunwayInfo, FlightPhase> OnRunwayAssigned;

        /// <summary>Fired when a runway's operational status changes.</summary>
        public event Action<RunwayInfo, RunwayStatus> OnRunwayStatusChanged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (runways.Count == 0)
                PopulateDefaultRunways();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Assigns the best available runway for the given flight phase.
        /// Wind direction (if available) is used to prefer a headwind runway.
        /// </summary>
        /// <param name="phase">The current flight phase (Takeoff or Landing).</param>
        /// <returns>The assigned <see cref="RunwayInfo"/>, or null if none available.</returns>
        public RunwayInfo AssignRunway(FlightPhase phase)
        {
            float windDirection = GetWindDirection();
            RunwayInfo best     = null;
            float bestScore     = float.MinValue;

            foreach (var rwy in runways)
            {
                if (rwy.status != RunwayStatus.Active) continue;

                float crosswind = Mathf.Abs(Mathf.DeltaAngle(rwy.heading, windDirection));
                float score = 180f - crosswind;   // higher = more headwind
                if (score > bestScore)
                {
                    bestScore = score;
                    best = rwy;
                }
            }

            if (best != null)
                OnRunwayAssigned?.Invoke(best, phase);

            return best;
        }

        /// <summary>Returns all runways with <see cref="RunwayStatus.Active"/> status.</summary>
        public List<RunwayInfo> GetActiveRunways()
        {
            var result = new List<RunwayInfo>();
            foreach (var rwy in runways)
                if (rwy.status == RunwayStatus.Active) result.Add(rwy);
            return result;
        }

        /// <summary>Sets the operational status of the runway with the given designator.</summary>
        /// <param name="name">Runway designator, e.g. \"27L\".</param>
        /// <param name="status">New status.</param>
        public void SetRunwayStatus(string name, RunwayStatus status)
        {
            foreach (var rwy in runways)
            {
                if (rwy.name == name)
                {
                    rwy.status = status;
                    OnRunwayStatusChanged?.Invoke(rwy, status);
                    return;
                }
            }
            Debug.LogWarning($"[SWEF] RunwayManager: runway '{name}' not found.");
        }

        /// <summary>
        /// Returns ILS approach parameters for the specified runway.
        /// </summary>
        /// <param name="runway">The runway to query.</param>
        /// <returns>A string describing ILS data, or an empty string if ILS is not available.</returns>
        public string GetILSData(RunwayInfo runway)
        {
            if (runway == null || !runway.ILSAvailable) return string.Empty;
            // ILS localiser frequency computed from runway heading (simplified)
            float freq = 108.1f + Mathf.Floor(runway.heading / 18f) * 0.2f;
            freq = Mathf.Clamp(freq, 108.1f, 111.95f);
            return $"ILS RWY {runway.name}: LOC {freq:000.00} MHz, HDG {runway.heading:000}°";
        }

        #endregion

        #region Wind Integration

        private float GetWindDirection()
        {
#if SWEF_WEATHER_AVAILABLE
            var wm = WeatherManager.Instance ?? FindFirstObjectByType<WeatherManager>();
            if (wm != null) return wm.WindDirection;
#endif
            return 270f; // default westerly wind
        }

        #endregion

        #region Defaults

        private void PopulateDefaultRunways()
        {
            runways.Add(new RunwayInfo
            {
                name         = "09",
                heading      = 90f,
                length       = 2500f,
                width        = 45f,
                ILSAvailable = true,
                status       = RunwayStatus.Active
            });
            runways.Add(new RunwayInfo
            {
                name         = "27",
                heading      = 270f,
                length       = 2500f,
                width        = 45f,
                ILSAvailable = true,
                status       = RunwayStatus.Active
            });
        }

        #endregion
    }
}
