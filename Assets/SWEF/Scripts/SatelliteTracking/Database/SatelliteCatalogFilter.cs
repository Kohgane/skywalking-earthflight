// SatelliteCatalogFilter.cs — Phase 114: Satellite & Space Debris Tracking
// Filter satellites by type, orbit, country, visibility, magnitude.
// Namespace: SWEF.SatelliteTracking

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Provides fluent filter operations on a collection of <see cref="SatelliteRecord"/>
    /// objects for UI lists, pass prediction, and radar display.
    /// </summary>
    public static class SatelliteCatalogFilter
    {
        // ── Filter methods ────────────────────────────────────────────────────────

        /// <summary>Filters records to only those of the given satellite type.</summary>
        public static IEnumerable<SatelliteRecord> ByType(
            IEnumerable<SatelliteRecord> source, SatelliteType type)
            => source.Where(r => r.satelliteType == type);

        /// <summary>Filters records to only those in the given orbital regime.</summary>
        public static IEnumerable<SatelliteRecord> ByOrbit(
            IEnumerable<SatelliteRecord> source, OrbitType orbit)
            => source.Where(r => r.orbitType == orbit);

        /// <summary>Filters records by country (case-insensitive).</summary>
        public static IEnumerable<SatelliteRecord> ByCountry(
            IEnumerable<SatelliteRecord> source, string country)
            => source.Where(r =>
                string.Equals(r.country, country, StringComparison.OrdinalIgnoreCase));

        /// <summary>Filters to records that are currently active.</summary>
        public static IEnumerable<SatelliteRecord> ActiveOnly(
            IEnumerable<SatelliteRecord> source)
            => source.Where(r => r.status == SatelliteStatus.Active);

        /// <summary>Filters to records brighter than the given visual magnitude threshold.</summary>
        public static IEnumerable<SatelliteRecord> BrighterThan(
            IEnumerable<SatelliteRecord> source, float maxMagnitude)
            => source.Where(r => r.visualMagnitude <= maxMagnitude);

        /// <summary>Filters to records currently within the given altitude band (km).</summary>
        public static IEnumerable<SatelliteRecord> InAltitudeRange(
            IEnumerable<SatelliteRecord> source, float minAltKm, float maxAltKm)
            => source.Where(r =>
                r.currentState != null &&
                r.currentState.altitudeKm >= minAltKm &&
                r.currentState.altitudeKm <= maxAltKm);

        /// <summary>Filters to user favourites.</summary>
        public static IEnumerable<SatelliteRecord> FavouritesOnly(
            IEnumerable<SatelliteRecord> source)
            => source.Where(r => r.isFavourite);

        /// <summary>Performs a case-insensitive name search.</summary>
        public static IEnumerable<SatelliteRecord> ByNameContains(
            IEnumerable<SatelliteRecord> source, string query)
            => source.Where(r =>
                r.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);

        /// <summary>Sorts records by visual magnitude (brightest first).</summary>
        public static IEnumerable<SatelliteRecord> SortedByMagnitude(
            IEnumerable<SatelliteRecord> source)
            => source.OrderBy(r => r.visualMagnitude);

        /// <summary>Sorts records alphabetically by name.</summary>
        public static IEnumerable<SatelliteRecord> SortedByName(
            IEnumerable<SatelliteRecord> source)
            => source.OrderBy(r => r.name, StringComparer.OrdinalIgnoreCase);

        /// <summary>Sorts records by NORAD catalogue ID.</summary>
        public static IEnumerable<SatelliteRecord> SortedByNoradId(
            IEnumerable<SatelliteRecord> source)
            => source.OrderBy(r => r.noradId);

        /// <summary>
        /// Applies a composite filter: type, orbit, country, active-only, max magnitude.
        /// Pass null/empty strings or enum value -1 to skip that filter.
        /// </summary>
        public static IEnumerable<SatelliteRecord> CompositeFilter(
            IEnumerable<SatelliteRecord> source,
            SatelliteType? type       = null,
            OrbitType? orbit          = null,
            string country            = null,
            bool activeOnly           = false,
            float maxMagnitude        = float.MaxValue,
            string nameQuery          = null)
        {
            var q = source;
            if (type.HasValue)                           q = ByType(q, type.Value);
            if (orbit.HasValue)                          q = ByOrbit(q, orbit.Value);
            if (!string.IsNullOrEmpty(country))          q = ByCountry(q, country);
            if (activeOnly)                              q = ActiveOnly(q);
            if (maxMagnitude < float.MaxValue)           q = BrighterThan(q, maxMagnitude);
            if (!string.IsNullOrEmpty(nameQuery))        q = ByNameContains(q, nameQuery);
            return q;
        }
    }
}
