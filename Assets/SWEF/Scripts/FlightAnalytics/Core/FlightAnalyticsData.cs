// FlightAnalyticsData.cs — Phase 116: Flight Analytics Dashboard
// Enums and data models for the flight analytics system.
// Namespace: SWEF.FlightAnalytics

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    // ── Analytics Category ────────────────────────────────────────────────────────

    /// <summary>High-level category for grouping analytics data.</summary>
    public enum AnalyticsCategory
    {
        /// <summary>Core flight performance metrics (speed, altitude, G-force).</summary>
        FlightPerformance,
        /// <summary>Navigation accuracy and route adherence.</summary>
        Navigation,
        /// <summary>Landing quality, touchdown scores, and approach analysis.</summary>
        Landing,
        /// <summary>Weather conditions encountered during flights.</summary>
        Weather,
        /// <summary>Fuel consumption, efficiency, and range metrics.</summary>
        Fuel,
        /// <summary>Social interactions, shared flights, and community activity.</summary>
        Social,
        /// <summary>Achievement progress and unlock milestones.</summary>
        Achievement
    }

    // ── Chart Type ────────────────────────────────────────────────────────────────

    /// <summary>Visualization style for a chart in the analytics dashboard.</summary>
    public enum ChartType
    {
        /// <summary>Continuous line chart for trend data over time.</summary>
        Line,
        /// <summary>Discrete bar chart for comparing categories.</summary>
        Bar,
        /// <summary>Proportional pie/donut chart.</summary>
        Pie,
        /// <summary>Multi-axis radar/spider chart for skill profiles.</summary>
        Radar,
        /// <summary>Density heatmap overlay on a geographic or image canvas.</summary>
        Heatmap,
        /// <summary>Individual data point scatter plot.</summary>
        Scatter
    }

    // ── Time Range ────────────────────────────────────────────────────────────────

    /// <summary>Time window for filtering analytics data.</summary>
    public enum TimeRange
    {
        /// <summary>Only the most recently completed flight session.</summary>
        LastFlight,
        /// <summary>All sessions within the current calendar day.</summary>
        Today,
        /// <summary>The last 7 calendar days.</summary>
        Week,
        /// <summary>The last 30 calendar days.</summary>
        Month,
        /// <summary>Entire recorded history (no date filter).</summary>
        AllTime
    }

    // ── Stat Aggregation ──────────────────────────────────────────────────────────

    /// <summary>Statistical aggregation method applied to a series of values.</summary>
    public enum StatAggregation
    {
        /// <summary>Arithmetic mean of all values.</summary>
        Average,
        /// <summary>Total sum of all values.</summary>
        Sum,
        /// <summary>Smallest value in the set.</summary>
        Min,
        /// <summary>Largest value in the set.</summary>
        Max,
        /// <summary>Middle value when sorted (50th percentile).</summary>
        Median
    }

    // ── Export Format ─────────────────────────────────────────────────────────────

    /// <summary>File format for exporting analytics reports.</summary>
    public enum ExportFormat
    {
        /// <summary>Comma-separated values for spreadsheet import.</summary>
        CSV,
        /// <summary>Structured JSON data file.</summary>
        JSON,
        /// <summary>Formatted PDF report document.</summary>
        PDF,
        /// <summary>PNG image snapshot of the dashboard or chart.</summary>
        PNG
    }

    // ── Trend Direction ───────────────────────────────────────────────────────────

    /// <summary>Direction of a detected performance trend.</summary>
    public enum TrendDirection
    {
        /// <summary>Metric is improving over time.</summary>
        Improving,
        /// <summary>Metric is declining over time.</summary>
        Declining,
        /// <summary>Metric is neither clearly improving nor declining.</summary>
        Stable
    }

    // ── Pilot Tier ────────────────────────────────────────────────────────────────

    /// <summary>Pilot skill tier used in the ranking system.</summary>
    public enum PilotTier
    {
        /// <summary>Beginner pilot still learning the basics.</summary>
        Student,
        /// <summary>Competent private pilot.</summary>
        Private,
        /// <summary>Experienced commercial pilot.</summary>
        Commercial,
        /// <summary>Senior airline transport pilot.</summary>
        Captain,
        /// <summary>Elite ace pilot with exceptional skill.</summary>
        Ace
    }

    // ── Data Models ───────────────────────────────────────────────────────────────

    /// <summary>A single sampled data point recorded during a flight.</summary>
    [Serializable]
    public class FlightDataPoint
    {
        /// <summary>Timestamp (seconds since session start) of this sample.</summary>
        public float timestamp;
        /// <summary>World-space position of the aircraft.</summary>
        public Vector3 position;
        /// <summary>Altitude above sea level in metres.</summary>
        public float altitude;
        /// <summary>Airspeed in knots.</summary>
        public float speedKnots;
        /// <summary>Magnetic heading in degrees (0–360).</summary>
        public float heading;
        /// <summary>Vertical G-force experienced by the aircraft.</summary>
        public float gForce;
        /// <summary>Remaining fuel as a normalised value (0–1).</summary>
        public float fuelNormalised;
        /// <summary>Throttle input as a normalised value (0–1).</summary>
        public float throttleInput;
        /// <summary>Pitch control input (−1 to +1).</summary>
        public float pitchInput;
        /// <summary>Roll control input (−1 to +1).</summary>
        public float rollInput;
    }

    /// <summary>Complete record for a single flight session.</summary>
    [Serializable]
    public class FlightSessionRecord
    {
        /// <summary>Unique identifier for this session.</summary>
        public string sessionId;
        /// <summary>UTC timestamp when the session started.</summary>
        public long startTimeUtc;
        /// <summary>UTC timestamp when the session ended.</summary>
        public long endTimeUtc;
        /// <summary>Total flight duration in seconds.</summary>
        public float durationSeconds;
        /// <summary>Total distance flown in nautical miles.</summary>
        public float distanceNm;
        /// <summary>ICAO code of the departure airport (if any).</summary>
        public string departureAirport;
        /// <summary>ICAO code of the destination airport (if any).</summary>
        public string arrivalAirport;
        /// <summary>All airports and waypoints visited during the flight.</summary>
        public List<string> airportsVisited = new List<string>();
        /// <summary>Aircraft type identifier used for this session.</summary>
        public string aircraftId;
        /// <summary>Computed performance score (0–100).</summary>
        public float performanceScore;
        /// <summary>Overall landing quality score (0–100); −1 if no landing.</summary>
        public float landingScore;
        /// <summary>Fuel efficiency score (0–100).</summary>
        public float fuelEfficiencyScore;
        /// <summary>Sampled data points recorded during the session.</summary>
        public List<FlightDataPoint> dataPoints = new List<FlightDataPoint>();
    }

    /// <summary>Aggregated statistics computed over a set of sessions.</summary>
    [Serializable]
    public class AggregatedStats
    {
        /// <summary>Total number of flights included in this aggregate.</summary>
        public int flightCount;
        /// <summary>Total flight hours across all included flights.</summary>
        public float totalHours;
        /// <summary>Total distance flown in nautical miles.</summary>
        public float totalDistanceNm;
        /// <summary>Average performance score across all flights.</summary>
        public float avgPerformanceScore;
        /// <summary>Average landing score across scored landings.</summary>
        public float avgLandingScore;
        /// <summary>Average fuel efficiency score.</summary>
        public float avgFuelEfficiency;
        /// <summary>Best (highest) performance score achieved.</summary>
        public float bestPerformanceScore;
        /// <summary>Best (highest) landing score achieved.</summary>
        public float bestLandingScore;
        /// <summary>Unique airports visited.</summary>
        public int uniqueAirports;
    }

    /// <summary>A single cell in a geographic or landing-zone heatmap.</summary>
    [Serializable]
    public class HeatmapCell
    {
        /// <summary>Grid column index.</summary>
        public int x;
        /// <summary>Grid row index.</summary>
        public int y;
        /// <summary>Accumulated density value for this cell.</summary>
        public float value;
        /// <summary>Normalised value (0–1) relative to the hottest cell.</summary>
        public float normalised;
    }

    /// <summary>A resolved heatmap ready for rendering.</summary>
    [Serializable]
    public class HeatmapData
    {
        /// <summary>Width of the heatmap grid in cells.</summary>
        public int width;
        /// <summary>Height of the heatmap grid in cells.</summary>
        public int height;
        /// <summary>All cells with non-zero density.</summary>
        public List<HeatmapCell> cells = new List<HeatmapCell>();
        /// <summary>Maximum raw density value (used for normalisation).</summary>
        public float maxValue;
    }

    /// <summary>A single data series for chart rendering.</summary>
    [Serializable]
    public class ChartSeries
    {
        /// <summary>Display label for this series.</summary>
        public string label;
        /// <summary>Data values; for time-series, index corresponds to time step.</summary>
        public List<float> values = new List<float>();
        /// <summary>Optional X-axis labels (for bar/scatter charts).</summary>
        public List<string> xLabels = new List<string>();
        /// <summary>Base colour for rendering this series.</summary>
        public Color color = Color.cyan;
    }

    /// <summary>Full chart descriptor ready for ChartRenderer.</summary>
    [Serializable]
    public class ChartData
    {
        /// <summary>Title displayed above the chart.</summary>
        public string title;
        /// <summary>Visualization type.</summary>
        public ChartType chartType;
        /// <summary>One or more data series.</summary>
        public List<ChartSeries> series = new List<ChartSeries>();
        /// <summary>X-axis label.</summary>
        public string xAxisLabel;
        /// <summary>Y-axis label.</summary>
        public string yAxisLabel;
    }

    /// <summary>A generated post-flight or weekly report.</summary>
    [Serializable]
    public class FlightReport
    {
        /// <summary>Unique identifier for this report.</summary>
        public string reportId;
        /// <summary>Human-readable report title.</summary>
        public string title;
        /// <summary>UTC timestamp when the report was generated.</summary>
        public long generatedAtUtc;
        /// <summary>Overall performance score (0–100).</summary>
        public float overallScore;
        /// <summary>Narrative summary of the flight or period.</summary>
        public string summary;
        /// <summary>Notable highlights (best moments, personal records).</summary>
        public List<string> highlights = new List<string>();
        /// <summary>Areas identified for improvement.</summary>
        public List<string> improvements = new List<string>();
        /// <summary>Key numeric metrics included in the report.</summary>
        public Dictionary<string, float> metrics = new Dictionary<string, float>();
    }

    /// <summary>A leaderboard entry for a single pilot.</summary>
    [Serializable]
    public class LeaderboardEntry
    {
        /// <summary>Platform-specific player identifier.</summary>
        public string playerId;
        /// <summary>Display name shown in the leaderboard.</summary>
        public string displayName;
        /// <summary>The metric value this entry is ranked on.</summary>
        public float score;
        /// <summary>Rank position (1 = top).</summary>
        public int rank;
        /// <summary>Pilot skill tier.</summary>
        public PilotTier tier;
    }
}
