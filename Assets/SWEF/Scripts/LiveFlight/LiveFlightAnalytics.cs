// LiveFlightAnalytics.cs — SWEF Live Flight Tracking & Real-World Data Overlay (Phase 103)
using System;
using UnityEngine;

namespace SWEF.LiveFlight
{
    /// <summary>
    /// Static analytics helper for the Live Flight Tracking system.
    ///
    /// <para>When compiled with <c>SWEF_ANALYTICS_AVAILABLE</c> events are forwarded
    /// to <see cref="SWEF.Analytics.TelemetryDispatcher"/>; otherwise they are only
    /// logged locally via <see cref="Debug.Log"/>.</para>
    /// </summary>
    public static class LiveFlightAnalytics
    {
        private const string Category = "LiveFlight";

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Call once when the live flight overlay is first enabled.</summary>
        public static void TrackFeatureEnabled()
        {
            var props = BuildBaseProps();
            Dispatch("live_flight_enabled", props);
        }

        /// <summary>
        /// Call when the player begins following an aircraft.
        /// </summary>
        /// <param name="icao24">The ICAO24 code of the followed aircraft.</param>
        public static void TrackAircraftFollowed(string icao24)
        {
            var props = BuildBaseProps();
            props["icao24"] = icao24 ?? "";
            Dispatch("live_flight_aircraft_followed", props);
        }

        /// <summary>
        /// Call when the API client encounters an unrecoverable fetch error.
        /// </summary>
        /// <param name="provider">Name of the data provider (e.g. "OpenSky").</param>
        /// <param name="error">Short error description.</param>
        public static void TrackAPIError(string provider, string error)
        {
            var props = BuildBaseProps();
            props["provider"] = provider ?? "";
            props["error"]    = error    ?? "";
            Dispatch("live_flight_api_error", props);
        }

        // ── Internals ─────────────────────────────────────────────────────────────

        private static System.Collections.Generic.Dictionary<string, object> BuildBaseProps()
        {
            return new System.Collections.Generic.Dictionary<string, object>
            {
                ["category"] = Category,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        private static void Dispatch(
            string eventName,
            System.Collections.Generic.Dictionary<string, object> properties)
        {
#if SWEF_ANALYTICS_AVAILABLE
            var dispatcher = SWEF.Analytics.TelemetryDispatcher.Instance;
            if (dispatcher != null)
            {
                var evt = new SWEF.Analytics.TelemetryEvent
                {
                    eventId   = Guid.NewGuid().ToString(),
                    eventName = eventName,
                    category  = Category,
                    timestamp = DateTime.UtcNow,
                    properties = properties
                };
                dispatcher.EnqueueEvent(evt);
                return;
            }
#endif
            Debug.Log($"[LiveFlightAnalytics] {eventName}: {FormatProps(properties)}");
        }

        private static string FormatProps(
            System.Collections.Generic.Dictionary<string, object> props)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var kv in props)
                sb.Append($"{kv.Key}={kv.Value} ");
            return sb.ToString().TrimEnd();
        }
    }
}
