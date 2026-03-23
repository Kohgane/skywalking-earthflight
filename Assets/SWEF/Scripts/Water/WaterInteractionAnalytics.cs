// WaterInteractionAnalytics.cs — SWEF Ocean & Water Interaction System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Water
{
    #region Analytics Data

    /// <summary>
    /// Snapshot of all water-interaction metrics accumulated during the current session.
    /// Returned by <see cref="WaterInteractionAnalytics.GetWaterInteractionSummary"/>.
    /// </summary>
    [Serializable]
    public class WaterInteractionSummary
    {
        /// <summary>Total number of water-entry splash events recorded.</summary>
        public int totalSplashCount;

        /// <summary>Total cumulative time spent underwater (seconds).</summary>
        public float totalUnderwaterSeconds;

        /// <summary>Number of buoyancy events (submersion start) recorded.</summary>
        public int totalBuoyancyEvents;

        /// <summary>Number of successful water landings (positive entry at low velocity).</summary>
        public int totalWaterLandings;

        /// <summary>Average entry velocity across all recorded splash events (m/s).</summary>
        public float averageEntryVelocity;

        /// <summary>Maximum single-event entry velocity recorded this session (m/s).</summary>
        public float peakEntryVelocity;

        /// <summary>Timestamp (UTC) of the most recent splash event.  Null if no events yet.</summary>
        public DateTime? lastSplashTime;
    }

    #endregion

    /// <summary>
    /// Phase 55 — Static utility class that tracks water-interaction metrics for the
    /// current session without requiring a scene object.
    ///
    /// <para>All public methods are thread-safe via a lock object.  Data is held in
    /// memory and resets when <see cref="ResetSession"/> is called or when the application
    /// restarts.</para>
    ///
    /// <para>Integration: called automatically by <see cref="SplashEffectController"/>,
    /// <see cref="BuoyancyController"/>, and <see cref="UnderwaterCameraTransition"/>.
    /// Consumer systems (Achievement, Journal, Analytics) should subscribe via
    /// <see cref="OnAnalyticsUpdated"/> or poll <see cref="GetWaterInteractionSummary"/>.</para>
    /// </summary>
    public static class WaterInteractionAnalytics
    {
        #region Events

        /// <summary>
        /// Fired after any metric is updated.
        /// Listeners receive an up-to-date <see cref="WaterInteractionSummary"/> snapshot.
        /// </summary>
        public static event Action<WaterInteractionSummary> OnAnalyticsUpdated;

        #endregion

        #region Private State

        private static readonly object _lock = new object();

        private static int   _splashCount;
        private static float _totalUnderwaterSeconds;
        private static int   _buoyancyEvents;
        private static int   _waterLandings;
        private static float _velocitySum;
        private static float _peakVelocity;
        private static DateTime? _lastSplashTime;

        private static float _underwaterEntryTime = -1f;

        // Threshold: entry speed below which a landing is considered "successful" (m/s)
        private const float WaterLandingSpeedThreshold = 15f;

        #endregion

        #region Public API

        /// <summary>
        /// Records a single splash event.  Called automatically by
        /// <see cref="SplashEffectController"/> on every surface crossing.
        /// </summary>
        /// <param name="data">Event payload from <see cref="SplashEffectController"/>.</param>
        public static void RecordSplash(SplashEventData data)
        {
            if (data == null) return;

            lock (_lock)
            {
                _splashCount++;
                _velocitySum  += data.velocityMagnitude;
                _peakVelocity  = Mathf.Max(_peakVelocity, data.velocityMagnitude);
                _lastSplashTime = DateTime.UtcNow;

                if (data.isEntry && data.velocityMagnitude <= WaterLandingSpeedThreshold)
                    _waterLandings++;
            }

            OnAnalyticsUpdated?.Invoke(GetWaterInteractionSummary());
        }

        /// <summary>
        /// Records the start of an underwater period.  Call when the camera or tracked
        /// object crosses below the surface.  Pair with <see cref="RecordUnderwaterExit"/>.
        /// </summary>
        public static void RecordUnderwaterEntry()
        {
            lock (_lock)
            {
                _underwaterEntryTime = Time.time;
                _buoyancyEvents++;
            }

            OnAnalyticsUpdated?.Invoke(GetWaterInteractionSummary());
        }

        /// <summary>
        /// Records the end of an underwater period and accumulates time to
        /// <see cref="WaterInteractionSummary.totalUnderwaterSeconds"/>.
        /// </summary>
        public static void RecordUnderwaterExit()
        {
            lock (_lock)
            {
                if (_underwaterEntryTime >= 0f)
                {
                    _totalUnderwaterSeconds += Mathf.Max(0f, Time.time - _underwaterEntryTime);
                    _underwaterEntryTime = -1f;
                }
            }

            OnAnalyticsUpdated?.Invoke(GetWaterInteractionSummary());
        }

        /// <summary>
        /// Manually accumulates underwater time without relying on entry/exit pair tracking.
        /// Useful for frame-by-frame accumulation in non-event-driven contexts.
        /// </summary>
        /// <param name="seconds">Duration in seconds to add.</param>
        public static void RecordUnderwaterTime(float seconds)
        {
            if (seconds <= 0f) return;

            lock (_lock)
            {
                _totalUnderwaterSeconds += seconds;
            }

            OnAnalyticsUpdated?.Invoke(GetWaterInteractionSummary());
        }

        /// <summary>
        /// Records a buoyancy event (object became submerged) without a paired underwater
        /// timer.  Increments <see cref="WaterInteractionSummary.totalBuoyancyEvents"/>.
        /// </summary>
        public static void RecordBuoyancyEvent()
        {
            lock (_lock) { _buoyancyEvents++; }
            OnAnalyticsUpdated?.Invoke(GetWaterInteractionSummary());
        }

        /// <summary>
        /// Returns a point-in-time snapshot of all accumulated water-interaction metrics
        /// for the current session.
        /// </summary>
        /// <returns>Immutable snapshot of session analytics.</returns>
        public static WaterInteractionSummary GetWaterInteractionSummary()
        {
            lock (_lock)
            {
                return new WaterInteractionSummary
                {
                    totalSplashCount      = _splashCount,
                    totalUnderwaterSeconds = _totalUnderwaterSeconds,
                    totalBuoyancyEvents   = _buoyancyEvents,
                    totalWaterLandings    = _waterLandings,
                    averageEntryVelocity  = _splashCount > 0 ? _velocitySum / _splashCount : 0f,
                    peakEntryVelocity     = _peakVelocity,
                    lastSplashTime        = _lastSplashTime
                };
            }
        }

        /// <summary>
        /// Resets all session metrics to zero.  Typically called at the start of a new
        /// flight session or when the user manually requests a stats reset.
        /// </summary>
        public static void ResetSession()
        {
            lock (_lock)
            {
                _splashCount            = 0;
                _totalUnderwaterSeconds = 0f;
                _buoyancyEvents         = 0;
                _waterLandings          = 0;
                _velocitySum            = 0f;
                _peakVelocity           = 0f;
                _lastSplashTime         = null;
                _underwaterEntryTime    = -1f;
            }

            OnAnalyticsUpdated?.Invoke(GetWaterInteractionSummary());
        }

        #endregion
    }
}
