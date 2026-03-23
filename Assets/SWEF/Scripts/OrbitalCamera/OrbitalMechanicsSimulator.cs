// OrbitalMechanicsSimulator.cs — SWEF Satellite View & Orbital Camera System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.OrbitalCamera
{
    /// <summary>
    /// Provides simplified Keplerian orbital mechanics calculations including
    /// orbital period, velocity, ground-track path, and the day/night terminator.
    /// </summary>
    public class OrbitalMechanicsSimulator : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Profile")]
        [Tooltip("Orbital camera profile — mechanics config is read from here.")]
        [SerializeField] private OrbitalCameraProfile profile;

        [Header("Orbit Parameters")]
        [Tooltip("Orbital inclination in degrees (0 = equatorial, 90 = polar).")]
        [SerializeField, Range(0f, 180f)] private float inclinationDegrees = 51.6f;

        [Tooltip("Orbital eccentricity (0 = circular).")]
        [SerializeField, Range(0f, 0.99f)] private float eccentricity = 0f;

        [Header("Sun Direction")]
        [Tooltip("Normalised direction toward the Sun in world space.")]
        [SerializeField] private Vector3 sunDirection = new Vector3(1f, 0f, 0f);

        #endregion

        #region Constants

        private const double EarthRadiusKm = 6371.0;

        #endregion

        #region Public API

        /// <summary>
        /// Returns the instantaneous orbital velocity vector at the current camera
        /// altitude, perpendicular to the nadir and orbit normal.
        /// </summary>
        public Vector3 GetOrbitalVelocity()
        {
            var ctrl = OrbitalCameraController.Instance;
            if (ctrl == null) return Vector3.zero;

            var altKm    = (double)ctrl.GetCurrentAltitudeKm();
            var speedKms = OrbitalSpeedKmPerSec(altKm);

            // Orbit tangent: perpendicular to nadir in the orbit plane
            var nadir  = -ctrl.transform.forward;
            var normal = Quaternion.Euler(inclinationDegrees, 0f, 0f) * Vector3.up;
            var tangent = Vector3.Cross(normal, nadir).normalized;
            return tangent * (float)speedKms;
        }

        /// <summary>Returns the Keplerian orbital period in seconds for the current altitude.</summary>
        public float GetOrbitalPeriodSeconds()
        {
            var ctrl = OrbitalCameraController.Instance;
            var altKm = ctrl != null ? (double)ctrl.GetCurrentAltitudeKm() : 400.0;
            return (float)OrbitalPeriodSeconds(altKm);
        }

        /// <summary>
        /// Samples <paramref name="sampleCount"/> ground-track points over one
        /// orbital period, returning them as (latitude, longitude) pairs in degrees.
        /// </summary>
        /// <param name="sampleCount">Number of samples to generate.</param>
        /// <returns>List of Vector2 where x = latitude, y = longitude (degrees).</returns>
        public List<Vector2> GetGroundTrackPoints(int sampleCount)
        {
            sampleCount = Mathf.Max(sampleCount, 2);
            var points = new List<Vector2>(sampleCount);

            var ctrl    = OrbitalCameraController.Instance;
            var altKm   = ctrl != null ? (double)ctrl.GetCurrentAltitudeKm() : 400.0;
            var period  = OrbitalPeriodSeconds(altKm);
            var incRad  = inclinationDegrees * Math.PI / 180.0;

            // Earth rotation per second (degrees)
            const double earthRotRateDegPerSec = 360.0 / 86164.1; // sidereal day

            for (var i = 0; i < sampleCount; i++)
            {
                var t = period * i / (sampleCount - 1);
                var angle = 2.0 * Math.PI * t / period;

                var lat = Math.Asin(Math.Sin(incRad) * Math.Sin(angle)) * 180.0 / Math.PI;
                var lonOffset = Math.Atan2(
                    Math.Cos(incRad) * Math.Sin(angle),
                    Math.Cos(angle)) * 180.0 / Math.PI;
                var lon = lonOffset - earthRotRateDegPerSec * t;
                lon = ((lon + 180.0) % 360.0 + 360.0) % 360.0 - 180.0;

                points.Add(new Vector2((float)lat, (float)lon));
            }

            return points;
        }

        /// <summary>
        /// Returns the subsolar longitude (degrees) — the longitude directly beneath
        /// the Sun — which defines one edge of the day/night terminator at the equator.
        /// </summary>
        public float GetDayNightTerminatorLongitude()
        {
            // Project sun direction onto the equatorial plane
            var sunFlat = new Vector3(sunDirection.x, 0f, sunDirection.z);
            if (sunFlat == Vector3.zero) return 0f;
            var angle = Mathf.Atan2(sunFlat.z, sunFlat.x) * Mathf.Rad2Deg;
            return angle;
        }

        #endregion

        #region Private Mechanics

        private double OrbitalSpeedKmPerSec(double altKm)
        {
            var mu = GetMu();
            var r  = EarthRadiusKm + altKm;
            return Math.Sqrt(mu / r);
        }

        private double OrbitalPeriodSeconds(double altKm)
        {
            var mu = GetMu();
            var r  = EarthRadiusKm + altKm;
            return 2.0 * Math.PI * Math.Sqrt(r * r * r / mu);
        }

        private double GetMu()
        {
            const double defaultMu = 398600.4418; // km³/s²
            return profile != null ? profile.mechanicsConfig.gravitationalParameter : defaultMu;
        }

        #endregion
    }
}
