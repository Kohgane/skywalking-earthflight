// OrbitalMechanicsController.cs — SWEF Space Station & Orbital Docking System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SpaceStation
{
    /// <summary>
    /// Singleton MonoBehaviour that propagates station positions along simplified
    /// Keplerian (2-body) orbits around Earth.  Does not require a full n-body
    /// physics engine — a circular or low-eccentricity approximation is sufficient
    /// for gameplay purposes.
    /// </summary>
    public class OrbitalMechanicsController : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────────

        /// <summary>Earth standard gravitational parameter μ = GM (m³/s²).</summary>
        public const double EarthMu = 3.986004418e14;

        /// <summary>Earth mean radius in metres.</summary>
        public const double EarthRadius = 6_371_000.0;

        // ── Singleton ─────────────────────────────────────────────────────────────

        public static OrbitalMechanicsController Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Tooltip("Distance threshold (m) within which OnStationInRange is fired.")]
        [SerializeField] private float _rangeThreshold = 50_000f;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Fired when the player altitude comes within <see cref="_rangeThreshold"/> of a station.</summary>
        public event Action<string> OnStationInRange;

        // ── Private state ─────────────────────────────────────────────────────────

        private readonly Dictionary<string, StationDefinition> _stations =
            new Dictionary<string, StationDefinition>(StringComparer.Ordinal);

        private readonly HashSet<string> _inRangeCache = new HashSet<string>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Registers a station definition for orbit propagation.</summary>
        public void RegisterStation(StationDefinition definition)
        {
            if (definition == null) return;
            _stations[definition.stationId] = definition;
        }

        /// <summary>Unregisters a station.</summary>
        public void UnregisterStation(string stationId)
        {
            _stations.Remove(stationId);
            _inRangeCache.Remove(stationId);
        }

        /// <summary>
        /// Returns the station world-space position (centred on Earth) for the given
        /// simulation time using the 2-body Keplerian solution.
        /// </summary>
        /// <param name="stationId">Registered station identifier.</param>
        /// <param name="time">Simulation time in seconds from epoch.</param>
        public Vector3 GetStationPosition(string stationId, double time)
        {
            if (!_stations.TryGetValue(stationId, out StationDefinition def))
                return Vector3.zero;

            double trueAnomalyRad = ComputeTrueAnomaly(def.orbitalParams, time);
            return OrbitalPositionToWorld(def.orbitalParams, trueAnomalyRad);
        }

        /// <summary>
        /// Returns the orbital velocity vector (m/s) of a station at the given time.
        /// </summary>
        public Vector3 GetOrbitalVelocity(string stationId, double time)
        {
            if (!_stations.TryGetValue(stationId, out StationDefinition def))
                return Vector3.zero;

            double r = EarthRadius + def.orbitalParams.altitude;
            // Vis-viva speed for the current radius
            double speed = GetCircularSpeed(def.orbitalParams.altitude);

            // Velocity is tangential to the orbit — perpendicular to the position
            Vector3 pos = GetStationPosition(stationId, time);
            Vector3 perpendicular = new Vector3(-pos.z, 0f, pos.x).normalized;
            return perpendicular * (float)speed;
        }

        /// <summary>
        /// Returns the relative velocity (m/s) between the player position and the
        /// named station.  Positive values mean the player is approaching.
        /// </summary>
        /// <param name="stationId">Target station identifier.</param>
        /// <param name="playerPosition">Player world position.</param>
        /// <param name="playerVelocity">Player velocity (m/s).</param>
        /// <param name="time">Current simulation time.</param>
        public float GetRelativeVelocity(string stationId, Vector3 playerPosition,
            Vector3 playerVelocity, double time)
        {
            Vector3 stationPos = GetStationPosition(stationId, time);
            Vector3 stationVel = GetOrbitalVelocity(stationId, time);
            Vector3 relVel     = playerVelocity - stationVel;
            Vector3 toStation  = (stationPos - playerPosition).normalized;
            return Vector3.Dot(relVel, toStation); // positive = closing
        }

        /// <summary>
        /// Checks whether the player altitude is within range of any registered station
        /// and fires <see cref="OnStationInRange"/> for new in-range transitions.
        /// Should be called from a MonoBehaviour Update or on altitude change.
        /// </summary>
        /// <param name="playerPosition">Current player world position.</param>
        /// <param name="time">Current simulation time.</param>
        public void CheckStationsInRange(Vector3 playerPosition, double time)
        {
            foreach (KeyValuePair<string, StationDefinition> pair in _stations)
            {
                Vector3 stationPos = GetStationPosition(pair.Key, time);
                float distance = Vector3.Distance(playerPosition, stationPos);

                if (distance <= _rangeThreshold)
                {
                    if (_inRangeCache.Add(pair.Key))
                        OnStationInRange?.Invoke(pair.Key);
                }
                else
                {
                    _inRangeCache.Remove(pair.Key);
                }
            }
        }

        // ── Static Helpers ────────────────────────────────────────────────────────

        /// <summary>
        /// Computes the circular orbital speed (m/s) at the given altitude (m).
        /// </summary>
        public static double GetCircularSpeed(double altitudeMetres)
        {
            double r = EarthRadius + altitudeMetres;
            return Math.Sqrt(EarthMu / r);
        }

        /// <summary>
        /// Computes the orbital period (s) for a circular orbit at the given altitude.
        /// </summary>
        public static double GetOrbitalPeriod(double altitudeMetres)
        {
            double r = EarthRadius + altitudeMetres;
            return 2.0 * Math.PI * Math.Sqrt(r * r * r / EarthMu);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private static double ComputeTrueAnomaly(OrbitalParameters orbit, double time)
        {
            double period = orbit.period > 0.0 ? orbit.period : GetOrbitalPeriod(orbit.altitude);
            // Mean motion (rad/s)
            double n = 2.0 * Math.PI / period;
            // Mean anomaly at time
            double M = (orbit.trueAnomaly * Math.PI / 180.0) + n * time;
            // For near-circular orbits eccentricity ≈ 0 → true anomaly ≈ mean anomaly
            if (orbit.eccentricity < 1e-6)
                return M;

            // Solve Kepler's equation iteratively (Newton-Raphson, 5 iterations)
            double E = M;
            for (int i = 0; i < 5; i++)
                E = E - (E - orbit.eccentricity * Math.Sin(E) - M) /
                        (1.0 - orbit.eccentricity * Math.Cos(E));

            double cosE = Math.Cos(E);
            double sinV = Math.Sqrt(1.0 - orbit.eccentricity * orbit.eccentricity) * Math.Sin(E) /
                          (1.0 - orbit.eccentricity * cosE);
            double cosV = (cosE - orbit.eccentricity) / (1.0 - orbit.eccentricity * cosE);
            return Math.Atan2(sinV, cosV);
        }

        private static Vector3 OrbitalPositionToWorld(OrbitalParameters orbit, double trueAnomalyRad)
        {
            double e = orbit.eccentricity;
            double r = EarthRadius + orbit.altitude;
            if (e > 1e-6)
            {
                double semiLatus = r * (1.0 + e * Math.Cos(trueAnomalyRad)) /
                                   (1.0 - e * e);
                r = semiLatus / (1.0 + e * Math.Cos(trueAnomalyRad));
            }

            float inclRad = (float)(orbit.inclination * Math.PI / 180.0);
            float x = (float)(r * Math.Cos(trueAnomalyRad));
            float z = (float)(r * Math.Sin(trueAnomalyRad) * Math.Cos(inclRad));
            float y = (float)(r * Math.Sin(trueAnomalyRad) * Math.Sin(inclRad));
            return new Vector3(x, y, z);
        }
    }
}
