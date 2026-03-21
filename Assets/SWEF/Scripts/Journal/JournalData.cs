using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Journal
{
    // ── Sort enum ─────────────────────────────────────────────────────────────────

    /// <summary>Fields by which the journal list can be sorted.</summary>
    public enum JournalSortBy
    {
        /// <summary>Sort by flight date (most recent first by default).</summary>
        Date,
        /// <summary>Sort by flight duration.</summary>
        Duration,
        /// <summary>Sort by distance flown.</summary>
        Distance,
        /// <summary>Sort by maximum altitude reached.</summary>
        Altitude,
        /// <summary>Sort by maximum speed.</summary>
        Speed,
        /// <summary>Sort by XP earned during the flight.</summary>
        XP
    }

    // ── FlightLogEntry ────────────────────────────────────────────────────────────

    /// <summary>
    /// Complete record of a single flight session.
    /// Stored in <c>Application.persistentDataPath/flight_journal.json</c>.
    /// </summary>
    [Serializable]
    public class FlightLogEntry
    {
        [Header("Identity")]
        /// <summary>Unique identifier (GUID) for this log entry.</summary>
        public string logId;

        /// <summary>ISO-8601 string representation of the flight start date/time (UTC).</summary>
        public string flightDate;

        [Header("Route")]
        /// <summary>Departure location — GPS coordinate string or nearest landmark name.</summary>
        public string departureLocation;

        /// <summary>Arrival location — GPS coordinate string or nearest landmark name.</summary>
        public string arrivalLocation;

        [Header("Performance")]
        /// <summary>Total flight duration in seconds.</summary>
        public float durationSeconds;

        /// <summary>Total distance flown in kilometres.</summary>
        public float distanceKm;

        /// <summary>Maximum altitude reached in metres.</summary>
        public float maxAltitudeM;

        /// <summary>Average speed in km/h over the entire flight.</summary>
        public float avgSpeedKmh;

        /// <summary>Peak speed in km/h reached during the flight.</summary>
        public float maxSpeedKmh;

        [Header("Altitude Profile")]
        /// <summary>
        /// Altitude samples taken every 5 seconds during the flight (metres).
        /// Used to draw a sparkline graph on the journal card.
        /// </summary>
        public float[] altitudeProfile = Array.Empty<float>();

        [Header("Environment")]
        /// <summary>Weather condition string captured from WeatherManager at flight start.</summary>
        public string weatherCondition;

        /// <summary>Highest atmosphere layer reached during the flight.</summary>
        public string atmosphereLayer;

        [Header("Activities")]
        /// <summary>Name of the guided tour completed during this flight, or empty string.</summary>
        public string tourName;

        /// <summary>IDs of achievements unlocked during this flight.</summary>
        public string[] achievementsUnlocked = Array.Empty<string>();

        /// <summary>File paths of screenshots taken during this flight (up to 5).</summary>
        public string[] screenshotPaths = Array.Empty<string>();

        /// <summary>Linked replay file ID, or empty string if no replay was saved.</summary>
        public string replayFileId;

        [Header("Progression")]
        /// <summary>Pilot rank name at the time this flight occurred.</summary>
        public string pilotRankAtTime;

        /// <summary>Total XP earned during this flight.</summary>
        public int xpEarned;

        [Header("User Data")]
        /// <summary>User-defined tags (e.g. "scenic", "record", "night").</summary>
        public string[] tags = Array.Empty<string>();

        /// <summary>Free-text notes written by the player. Maximum 500 characters.</summary>
        public string notes;

        /// <summary>Whether this entry has been marked as a favourite.</summary>
        public bool isFavorite;

        [Header("Comparison")]
        /// <summary>
        /// Hash of the flight path for route comparison and deduplication.
        /// Built from a discretized sequence of position samples.
        /// </summary>
        public string flightPathHash;
    }

    // ── JournalFilter ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Criteria used to filter and sort the list of <see cref="FlightLogEntry"/> records.
    /// </summary>
    [Serializable]
    public class JournalFilter
    {
        [Header("Date Range")]
        /// <summary>ISO-8601 lower bound (inclusive). Empty = no lower bound.</summary>
        public string dateFrom;

        /// <summary>ISO-8601 upper bound (inclusive). Empty = no upper bound.</summary>
        public string dateTo;

        [Header("Duration Range (seconds)")]
        /// <summary>Minimum flight duration in seconds. 0 = no lower bound.</summary>
        public float minDuration;

        /// <summary>Maximum flight duration in seconds. 0 = no upper bound.</summary>
        public float maxDuration;

        [Header("Altitude Range (metres)")]
        /// <summary>Minimum peak altitude in metres. 0 = no lower bound.</summary>
        public float minAltitude;

        /// <summary>Maximum peak altitude in metres. 0 = no upper bound.</summary>
        public float maxAltitude;

        [Header("Content Filters")]
        /// <summary>Filter by weather condition string (partial match). Empty = all weather.</summary>
        public string weatherFilter;

        /// <summary>Filter by tour name (partial match). Empty = all tours.</summary>
        public string tourFilter;

        /// <summary>Only return entries that have at least one of these tags. Empty = no tag filter.</summary>
        public string[] tagsFilter = Array.Empty<string>();

        /// <summary>When true, only return entries marked as favourite.</summary>
        public bool favoritesOnly;

        [Header("Search")]
        /// <summary>
        /// Free-text query searched across <c>notes</c>, <c>departureLocation</c>,
        /// <c>arrivalLocation</c>, <c>tags</c>, <c>tourName</c>, and <c>weatherCondition</c>.
        /// Empty = no text filter.
        /// </summary>
        public string searchQuery;

        [Header("Sorting")]
        /// <summary>Field to sort results by.</summary>
        public JournalSortBy sortBy = JournalSortBy.Date;

        /// <summary>When true, results are returned newest/largest first.</summary>
        public bool sortDescending = true;
    }

    // ── JournalStatistics ─────────────────────────────────────────────────────────

    /// <summary>
    /// Aggregate statistics computed from all <see cref="FlightLogEntry"/> records.
    /// Generated on demand by <see cref="JournalManager.GetStatistics"/>.
    /// </summary>
    [Serializable]
    public class JournalStatistics
    {
        [Header("Totals")]
        /// <summary>Total number of recorded flights.</summary>
        public int totalFlights;

        /// <summary>Cumulative distance flown across all flights in kilometres.</summary>
        public float totalDistanceKm;

        /// <summary>Total time spent flying across all flights in hours.</summary>
        public float totalDurationHours;

        [Header("Records")]
        /// <summary>Highest altitude ever reached in metres.</summary>
        public float highestAltitudeEver;

        /// <summary>Fastest speed ever reached in km/h.</summary>
        public float fastestSpeedEver;

        /// <summary>Duration in seconds of the longest single flight.</summary>
        public float longestFlightSeconds;

        [Header("Favourites")]
        /// <summary>Most frequently occurring weather condition across all flights.</summary>
        public string favoriteWeather;

        /// <summary>Departure or arrival location that appears most often.</summary>
        public string mostVisitedLocation;

        [Header("Recency")]
        /// <summary>Number of flights completed in the current calendar week.</summary>
        public int flightsThisWeek;

        /// <summary>Number of flights completed in the current calendar month.</summary>
        public int flightsThisMonth;

        [Header("Streaks")]
        /// <summary>Number of consecutive calendar days that include at least one flight (ending today).</summary>
        public int currentStreak;

        /// <summary>Longest consecutive-day streak ever recorded.</summary>
        public int longestStreak;

        [Header("Averages")]
        /// <summary>Mean flight duration in seconds.</summary>
        public float averageFlightDuration;

        /// <summary>Mean peak altitude in metres across all flights.</summary>
        public float averageAltitude;
    }
}
