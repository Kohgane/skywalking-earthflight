// OrbitalCameraAnalytics.cs — SWEF Satellite View & Orbital Camera System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.OrbitalCamera
{
    /// <summary>
    /// Snapshot returned by <see cref="OrbitalCameraAnalytics.GetOrbitalAnalyticsSummary"/>.
    /// </summary>
    [Serializable]
    public sealed class OrbitalAnalyticsSummary
    {
        /// <summary>Highest altitude reached during this session (km).</summary>
        public float maxAltitudeKm;

        /// <summary>Total number of completed orbits recorded.</summary>
        public int totalOrbitCount;

        /// <summary>Total number of space-to-ground or ground-to-space transitions.</summary>
        public int totalTransitionCount;

        /// <summary>Number of descending transitions.</summary>
        public int descendingTransitionCount;

        /// <summary>Number of ascending transitions.</summary>
        public int ascendingTransitionCount;

        /// <summary>Cumulative time spent in each <see cref="AltitudeZone"/> (seconds).</summary>
        public Dictionary<AltitudeZone, float> timePerZoneSeconds;

        /// <summary>The zone in which the most time has been spent.</summary>
        public AltitudeZone mostVisitedZone;
    }

    /// <summary>
    /// Static utility class for tracking orbital camera analytics across the session.
    /// All state is reset when the application quits.
    /// </summary>
    public static class OrbitalCameraAnalytics
    {
        #region Private State

        private static float _maxAltitudeKm;
        private static int   _orbitCount;
        private static int   _totalTransitions;
        private static int   _descendingCount;
        private static int   _ascendingCount;

        private static readonly Dictionary<AltitudeZone, float> TimePerZone
            = new Dictionary<AltitudeZone, float>
            {
                { AltitudeZone.Ground,          0f },
                { AltitudeZone.LowAtmosphere,   0f },
                { AltitudeZone.HighAtmosphere,  0f },
                { AltitudeZone.NearSpace,       0f },
                { AltitudeZone.LowOrbit,        0f },
                { AltitudeZone.HighOrbit,       0f }
            };

        private static AltitudeZone _lastZone = AltitudeZone.Ground;
        private static float        _lastZoneTimestamp;

        #endregion

        #region Public API

        /// <summary>
        /// Records a new altitude sample and updates the max-altitude stat.
        /// Should be called whenever the camera altitude changes noticeably.
        /// </summary>
        /// <param name="newAltitudeKm">Current altitude in kilometres.</param>
        public static void RecordAltitudeChange(float newAltitudeKm)
        {
            if (newAltitudeKm > _maxAltitudeKm)
                _maxAltitudeKm = newAltitudeKm;

            var zone = AltitudeToZone(newAltitudeKm);
            if (zone != _lastZone)
            {
                FlushZoneTime();
                _lastZone          = zone;
                _lastZoneTimestamp = Time.realtimeSinceStartup;
            }
        }

        /// <summary>
        /// Records a completed space-to-ground or ground-to-space transition.
        /// </summary>
        /// <param name="isDescending"><c>true</c> for descent, <c>false</c> for ascent.</param>
        /// <param name="duration">Duration of the transition in seconds (stored for potential future aggregation).</param>
        public static void RecordTransition(bool isDescending, float duration)
        {
            _totalTransitions++;
            if (isDescending) _descendingCount++;
            else              _ascendingCount++;
            // duration is accepted for API consistency and future average-duration tracking.
            _ = duration;
        }

        /// <summary>Records that one complete orbit has been performed.</summary>
        public static void RecordOrbitComplete()
        {
            _orbitCount++;
        }

        /// <summary>
        /// Returns an immutable snapshot of all orbital analytics gathered so far.
        /// </summary>
        public static OrbitalAnalyticsSummary GetOrbitalAnalyticsSummary()
        {
            FlushZoneTime();

            var mostVisited     = AltitudeZone.Ground;
            var mostVisitedTime = 0f;
            foreach (var kv in TimePerZone)
            {
                if (kv.Value > mostVisitedTime)
                {
                    mostVisitedTime = kv.Value;
                    mostVisited     = kv.Key;
                }
            }

            return new OrbitalAnalyticsSummary
            {
                maxAltitudeKm               = _maxAltitudeKm,
                totalOrbitCount             = _orbitCount,
                totalTransitionCount        = _totalTransitions,
                descendingTransitionCount   = _descendingCount,
                ascendingTransitionCount    = _ascendingCount,
                timePerZoneSeconds          = new Dictionary<AltitudeZone, float>(TimePerZone),
                mostVisitedZone             = mostVisited
            };
        }

        /// <summary>Resets all tracked analytics to zero.</summary>
        public static void Reset()
        {
            _maxAltitudeKm    = 0f;
            _orbitCount       = 0;
            _totalTransitions = 0;
            _descendingCount  = 0;
            _ascendingCount   = 0;
            foreach (var zone in TimePerZone.Keys)
                TimePerZone[zone] = 0f;
            _lastZoneTimestamp = Time.realtimeSinceStartup;
        }

        #endregion

        #region Private Helpers

        private static void FlushZoneTime()
        {
            var now  = Time.realtimeSinceStartup;
            var diff = now - _lastZoneTimestamp;
            if (diff > 0f && TimePerZone.ContainsKey(_lastZone))
                TimePerZone[_lastZone] += diff;
            _lastZoneTimestamp = now;
        }

        private static AltitudeZone AltitudeToZone(float altKm)
        {
            if (altKm < 1f)    return AltitudeZone.Ground;
            if (altKm < 10f)   return AltitudeZone.LowAtmosphere;
            if (altKm < 50f)   return AltitudeZone.HighAtmosphere;
            if (altKm < 200f)  return AltitudeZone.NearSpace;
            if (altKm < 2000f) return AltitudeZone.LowOrbit;
            return AltitudeZone.HighOrbit;
        }

        #endregion
    }
}
