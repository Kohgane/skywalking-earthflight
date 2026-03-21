using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Journal
{
    /// <summary>
    /// Separates all filtering, searching, and sorting logic from the UI layer.
    /// Consumed by <see cref="JournalManager.GetFilteredEntries"/> and the
    /// <see cref="JournalPanelUI"/> directly.
    /// </summary>
    public class JournalSearchEngine
    {
        /// <summary>
        /// Applies the given <paramref name="filter"/> to <paramref name="entries"/> and
        /// returns a new list containing only the matching entries, sorted as requested.
        /// </summary>
        /// <param name="entries">Source list (not modified).</param>
        /// <param name="filter">Filter and sort criteria to apply.</param>
        /// <returns>Filtered and sorted copy of the relevant entries.</returns>
        public List<FlightLogEntry> ApplyFilter(List<FlightLogEntry> entries, JournalFilter filter)
        {
            if (entries == null) return new List<FlightLogEntry>();
            if (filter  == null) return new List<FlightLogEntry>(entries);

            var result = new List<FlightLogEntry>(entries.Count);

            string queryLower   = filter.searchQuery?.Trim().ToLowerInvariant();
            bool   hasQuery     = !string.IsNullOrEmpty(queryLower);
            bool   hasDateFrom  = !string.IsNullOrEmpty(filter.dateFrom);
            bool   hasDateTo    = !string.IsNullOrEmpty(filter.dateTo);
            bool   hasWeather   = !string.IsNullOrEmpty(filter.weatherFilter);
            bool   hasTour      = !string.IsNullOrEmpty(filter.tourFilter);
            bool   hasTags      = filter.tagsFilter != null && filter.tagsFilter.Length > 0;

            foreach (var e in entries)
            {
                // ── Favorites only ──────────────────────────────────────────────
                if (filter.favoritesOnly && !e.isFavorite) continue;

                // ── Date range ──────────────────────────────────────────────────
                if (hasDateFrom)
                {
                    if (string.Compare(e.flightDate, filter.dateFrom, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                }
                if (hasDateTo)
                {
                    if (string.Compare(e.flightDate, filter.dateTo, StringComparison.OrdinalIgnoreCase) > 0)
                        continue;
                }

                // ── Duration range ──────────────────────────────────────────────
                if (filter.minDuration > 0f && e.durationSeconds < filter.minDuration) continue;
                if (filter.maxDuration > 0f && e.durationSeconds > filter.maxDuration) continue;

                // ── Altitude range ──────────────────────────────────────────────
                if (filter.minAltitude > 0f && e.maxAltitudeM < filter.minAltitude) continue;
                if (filter.maxAltitude > 0f && e.maxAltitudeM > filter.maxAltitude) continue;

                // ── Weather filter ──────────────────────────────────────────────
                if (hasWeather)
                {
                    if (e.weatherCondition == null ||
                        e.weatherCondition.IndexOf(filter.weatherFilter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                }

                // ── Tour filter ─────────────────────────────────────────────────
                if (hasTour)
                {
                    if (e.tourName == null ||
                        e.tourName.IndexOf(filter.tourFilter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                }

                // ── Tag filter (any match) ──────────────────────────────────────
                if (hasTags)
                {
                    if (e.tags == null || e.tags.Length == 0) continue;
                    bool tagMatch = false;
                    foreach (var ft in filter.tagsFilter)
                    {
                        foreach (var et in e.tags)
                        {
                            if (string.Equals(et, ft, StringComparison.OrdinalIgnoreCase))
                            { tagMatch = true; break; }
                        }
                        if (tagMatch) break;
                    }
                    if (!tagMatch) continue;
                }

                // ── Full-text search ────────────────────────────────────────────
                if (hasQuery && !MatchesQuery(e, queryLower)) continue;

                result.Add(e);
            }

            // ── Sorting ─────────────────────────────────────────────────────────
            result.Sort((a, b) =>
            {
                int cmp = CompareBy(a, b, filter.sortBy);
                return filter.sortDescending ? -cmp : cmp;
            });

            return result;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static bool MatchesQuery(FlightLogEntry e, string lowerQuery)
        {
            if (Contains(e.notes,              lowerQuery)) return true;
            if (Contains(e.departureLocation,  lowerQuery)) return true;
            if (Contains(e.arrivalLocation,    lowerQuery)) return true;
            if (Contains(e.tourName,           lowerQuery)) return true;
            if (Contains(e.weatherCondition,   lowerQuery)) return true;
            if (e.tags != null)
            {
                foreach (var t in e.tags)
                    if (Contains(t, lowerQuery)) return true;
            }
            return false;
        }

        private static bool Contains(string source, string query)
        {
            if (string.IsNullOrEmpty(source)) return false;
            return source.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int CompareBy(FlightLogEntry a, FlightLogEntry b, JournalSortBy sortBy)
        {
            switch (sortBy)
            {
                case JournalSortBy.Date:
                    return string.Compare(a.flightDate, b.flightDate, StringComparison.OrdinalIgnoreCase);
                case JournalSortBy.Duration:
                    return a.durationSeconds.CompareTo(b.durationSeconds);
                case JournalSortBy.Distance:
                    return a.distanceKm.CompareTo(b.distanceKm);
                case JournalSortBy.Altitude:
                    return a.maxAltitudeM.CompareTo(b.maxAltitudeM);
                case JournalSortBy.Speed:
                    return a.maxSpeedKmh.CompareTo(b.maxSpeedKmh);
                case JournalSortBy.XP:
                    return a.xpEarned.CompareTo(b.xpEarned);
                default:
                    return 0;
            }
        }
    }
}
