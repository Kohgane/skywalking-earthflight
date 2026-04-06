// FlightAnalyticsTests.cs — Phase 116: Flight Analytics Dashboard
// Comprehensive NUnit EditMode tests (45+ tests) covering:
// enums, config, data recording, statistics engine, heatmap generation,
// chart data, report generation, leaderboard, ranking system.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SWEF.FlightAnalytics;

[TestFixture]
public class FlightAnalyticsTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // AnalyticsCategory enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AnalyticsCategory_AllValuesAreDefined()
    {
        var values = (AnalyticsCategory[])Enum.GetValues(typeof(AnalyticsCategory));
        Assert.GreaterOrEqual(values.Length, 7, "At least 7 categories required");
        Assert.Contains(AnalyticsCategory.FlightPerformance, values);
        Assert.Contains(AnalyticsCategory.Navigation,        values);
        Assert.Contains(AnalyticsCategory.Landing,           values);
        Assert.Contains(AnalyticsCategory.Weather,           values);
        Assert.Contains(AnalyticsCategory.Fuel,              values);
        Assert.Contains(AnalyticsCategory.Social,            values);
        Assert.Contains(AnalyticsCategory.Achievement,       values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ChartType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ChartType_AllValuesAreDefined()
    {
        var values = (ChartType[])Enum.GetValues(typeof(ChartType));
        Assert.Contains(ChartType.Line,    values);
        Assert.Contains(ChartType.Bar,     values);
        Assert.Contains(ChartType.Pie,     values);
        Assert.Contains(ChartType.Radar,   values);
        Assert.Contains(ChartType.Heatmap, values);
        Assert.Contains(ChartType.Scatter, values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TimeRange enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TimeRange_AllValuesAreDefined()
    {
        var values = (TimeRange[])Enum.GetValues(typeof(TimeRange));
        Assert.Contains(TimeRange.LastFlight, values);
        Assert.Contains(TimeRange.Today,      values);
        Assert.Contains(TimeRange.Week,       values);
        Assert.Contains(TimeRange.Month,      values);
        Assert.Contains(TimeRange.AllTime,    values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // StatAggregation enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void StatAggregation_AllValuesAreDefined()
    {
        var values = (StatAggregation[])Enum.GetValues(typeof(StatAggregation));
        Assert.Contains(StatAggregation.Average, values);
        Assert.Contains(StatAggregation.Sum,     values);
        Assert.Contains(StatAggregation.Min,     values);
        Assert.Contains(StatAggregation.Max,     values);
        Assert.Contains(StatAggregation.Median,  values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ExportFormat enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ExportFormat_AllValuesAreDefined()
    {
        var values = (ExportFormat[])Enum.GetValues(typeof(ExportFormat));
        Assert.Contains(ExportFormat.CSV,  values);
        Assert.Contains(ExportFormat.JSON, values);
        Assert.Contains(ExportFormat.PDF,  values);
        Assert.Contains(ExportFormat.PNG,  values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TrendDirection enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TrendDirection_AllValuesAreDefined()
    {
        var values = (TrendDirection[])Enum.GetValues(typeof(TrendDirection));
        Assert.Contains(TrendDirection.Improving, values);
        Assert.Contains(TrendDirection.Declining, values);
        Assert.Contains(TrendDirection.Stable,    values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PilotTier enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void PilotTier_AllValuesAreDefined()
    {
        var values = (PilotTier[])Enum.GetValues(typeof(PilotTier));
        Assert.Contains(PilotTier.Student,    values);
        Assert.Contains(PilotTier.Private,    values);
        Assert.Contains(PilotTier.Commercial, values);
        Assert.Contains(PilotTier.Captain,    values);
        Assert.Contains(PilotTier.Ace,        values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FlightDataPoint data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void FlightDataPoint_DefaultValuesAreZero()
    {
        var pt = new FlightDataPoint();
        Assert.AreEqual(0f, pt.timestamp);
        Assert.AreEqual(0f, pt.altitude);
        Assert.AreEqual(0f, pt.speedKnots);
    }

    [Test]
    public void FlightDataPoint_CanSetAllFields()
    {
        var pt = new FlightDataPoint
        {
            timestamp      = 10f,
            altitude       = 5000f,
            speedKnots     = 250f,
            heading        = 270f,
            gForce         = 1.2f,
            fuelNormalised = 0.8f
        };
        Assert.AreEqual(10f,   pt.timestamp);
        Assert.AreEqual(5000f, pt.altitude);
        Assert.AreEqual(250f,  pt.speedKnots);
        Assert.AreEqual(270f,  pt.heading);
        Assert.AreEqual(1.2f,  pt.gForce,         0.001f);
        Assert.AreEqual(0.8f,  pt.fuelNormalised,  0.001f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FlightSessionRecord data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void FlightSessionRecord_DefaultListsAreNotNull()
    {
        var session = new FlightSessionRecord();
        Assert.IsNotNull(session.airportsVisited);
        Assert.IsNotNull(session.dataPoints);
    }

    [Test]
    public void FlightSessionRecord_CanAddDataPoints()
    {
        var session = new FlightSessionRecord();
        session.dataPoints.Add(new FlightDataPoint { altitude = 1000f });
        session.dataPoints.Add(new FlightDataPoint { altitude = 2000f });
        Assert.AreEqual(2, session.dataPoints.Count);
    }

    [Test]
    public void FlightSessionRecord_CanAddAirports()
    {
        var session = new FlightSessionRecord();
        session.airportsVisited.Add("EGLL");
        session.airportsVisited.Add("KJFK");
        Assert.AreEqual(2, session.airportsVisited.Count);
        Assert.Contains("EGLL", session.airportsVisited);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HeatmapCell and HeatmapData
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void HeatmapCell_NormalisedIsWithinRange()
    {
        var cell = new HeatmapCell { x = 0, y = 0, value = 5f, normalised = 0.5f };
        Assert.GreaterOrEqual(cell.normalised, 0f);
        Assert.LessOrEqual(cell.normalised,    1f);
    }

    [Test]
    public void HeatmapData_DefaultCellsListIsNotNull()
    {
        var data = new HeatmapData();
        Assert.IsNotNull(data.cells);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ChartSeries and ChartData
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ChartSeries_DefaultListsAreNotNull()
    {
        var series = new ChartSeries();
        Assert.IsNotNull(series.values);
        Assert.IsNotNull(series.xLabels);
    }

    [Test]
    public void ChartData_DefaultSeriesListIsNotNull()
    {
        var chart = new ChartData();
        Assert.IsNotNull(chart.series);
    }

    [Test]
    public void ChartData_CanSetProperties()
    {
        var chart = new ChartData
        {
            title     = "Test Chart",
            chartType = ChartType.Line,
            xAxisLabel = "X",
            yAxisLabel = "Y"
        };
        Assert.AreEqual("Test Chart", chart.title);
        Assert.AreEqual(ChartType.Line, chart.chartType);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FlightReport
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void FlightReport_DefaultListsAreNotNull()
    {
        var report = new FlightReport();
        Assert.IsNotNull(report.highlights);
        Assert.IsNotNull(report.improvements);
        Assert.IsNotNull(report.metrics);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FlightStatisticsEngine
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void StatisticsEngine_Aggregate_EmptyList_ReturnsZeroStats()
    {
        var go     = new GameObject();
        var engine = go.AddComponent<FlightStatisticsEngine>();
        var stats  = engine.Aggregate(new List<FlightSessionRecord>());
        Assert.AreEqual(0, stats.flightCount);
        Assert.AreEqual(0f, stats.totalHours);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void StatisticsEngine_Aggregate_SingleSession_CorrectFlightCount()
    {
        var go      = new GameObject();
        var engine  = go.AddComponent<FlightStatisticsEngine>();
        var session = new FlightSessionRecord { durationSeconds = 3600f, performanceScore = 80f };
        var stats   = engine.Aggregate(new List<FlightSessionRecord> { session });
        Assert.AreEqual(1, stats.flightCount);
        Assert.AreEqual(1f, stats.totalHours, 0.001f);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void StatisticsEngine_Aggregate_MultipleSessions_SumsHours()
    {
        var go     = new GameObject();
        var engine = go.AddComponent<FlightStatisticsEngine>();
        var sessions = new List<FlightSessionRecord>
        {
            new FlightSessionRecord { durationSeconds = 3600f },
            new FlightSessionRecord { durationSeconds = 7200f }
        };
        var stats = engine.Aggregate(sessions);
        Assert.AreEqual(2, stats.flightCount);
        Assert.AreEqual(3f, stats.totalHours, 0.001f);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void StatisticsEngine_AggregateValues_Average()
    {
        var go     = new GameObject();
        var engine = go.AddComponent<FlightStatisticsEngine>();
        var values = new List<float> { 10f, 20f, 30f };
        Assert.AreEqual(20f, engine.Aggregate(values, StatAggregation.Average), 0.001f);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void StatisticsEngine_AggregateValues_Sum()
    {
        var go     = new GameObject();
        var engine = go.AddComponent<FlightStatisticsEngine>();
        var values = new List<float> { 10f, 20f, 30f };
        Assert.AreEqual(60f, engine.Aggregate(values, StatAggregation.Sum), 0.001f);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void StatisticsEngine_AggregateValues_Min()
    {
        var go     = new GameObject();
        var engine = go.AddComponent<FlightStatisticsEngine>();
        var values = new List<float> { 10f, 5f, 30f };
        Assert.AreEqual(5f, engine.Aggregate(values, StatAggregation.Min), 0.001f);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void StatisticsEngine_AggregateValues_Max()
    {
        var go     = new GameObject();
        var engine = go.AddComponent<FlightStatisticsEngine>();
        var values = new List<float> { 10f, 5f, 30f };
        Assert.AreEqual(30f, engine.Aggregate(values, StatAggregation.Max), 0.001f);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void StatisticsEngine_AggregateValues_Median_OddCount()
    {
        var go     = new GameObject();
        var engine = go.AddComponent<FlightStatisticsEngine>();
        var values = new List<float> { 30f, 10f, 20f };
        Assert.AreEqual(20f, engine.Aggregate(values, StatAggregation.Median), 0.001f);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void StatisticsEngine_StandardDeviation_ConstantList_ReturnsZero()
    {
        var go     = new GameObject();
        var engine = go.AddComponent<FlightStatisticsEngine>();
        var values = new List<float> { 5f, 5f, 5f, 5f };
        Assert.AreEqual(0f, engine.StandardDeviation(values), 0.001f);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void StatisticsEngine_MovingAverage_LengthMatchesInput()
    {
        var go     = new GameObject();
        var engine = go.AddComponent<FlightStatisticsEngine>();
        var values = new List<float> { 1f, 2f, 3f, 4f, 5f };
        var ma     = engine.MovingAverage(values, 3);
        Assert.AreEqual(values.Count, ma.Count);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void StatisticsEngine_Percentile_50th_EqualsMedian()
    {
        var go     = new GameObject();
        var engine = go.AddComponent<FlightStatisticsEngine>();
        var values = new List<float> { 10f, 20f, 30f, 40f, 50f };
        Assert.AreEqual(30f, engine.Percentile(values, 50f), 0.001f);
        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TrendAnalyzer
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TrendAnalyzer_DetectTrend_Improving_ReturnsImproving()
    {
        var go       = new GameObject();
        var analyzer = go.AddComponent<TrendAnalyzer>();
        var scores   = new List<float> { 50f, 55f, 60f, 65f, 70f };
        Assert.AreEqual(TrendDirection.Improving, analyzer.DetectTrend(scores));
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void TrendAnalyzer_DetectTrend_Declining_ReturnsDeclining()
    {
        var go       = new GameObject();
        var analyzer = go.AddComponent<TrendAnalyzer>();
        var scores   = new List<float> { 80f, 75f, 70f, 65f, 60f };
        Assert.AreEqual(TrendDirection.Declining, analyzer.DetectTrend(scores));
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void TrendAnalyzer_DetectTrend_Flat_ReturnsStable()
    {
        var go       = new GameObject();
        var analyzer = go.AddComponent<TrendAnalyzer>();
        var scores   = new List<float> { 70f, 70f, 70f, 70f };
        Assert.AreEqual(TrendDirection.Stable, analyzer.DetectTrend(scores));
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void TrendAnalyzer_ProgressionCurve_LengthMatchesInput()
    {
        var go       = new GameObject();
        var analyzer = go.AddComponent<TrendAnalyzer>();
        var scores   = new List<float> { 50f, 60f, 70f, 80f, 90f };
        var curve    = analyzer.ProgressionCurve(scores);
        Assert.AreEqual(scores.Count, curve.Count);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void TrendAnalyzer_EstimateFlightsToTarget_AlreadyReached_ReturnsZero()
    {
        var go       = new GameObject();
        var analyzer = go.AddComponent<TrendAnalyzer>();
        var scores   = new List<float> { 50f, 60f, 70f, 90f };
        int result   = analyzer.EstimateFlightsToTarget(scores, 85f);
        Assert.AreEqual(0, result);
        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ComparisonEngine
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ComparisonEngine_Compare_ReturnsExpectedDelta()
    {
        var go     = new GameObject();
        var engine = go.AddComponent<ComparisonEngine>();
        var a = new FlightSessionRecord { performanceScore = 80f };
        var b = new FlightSessionRecord { performanceScore = 70f };
        var delta = engine.Compare(a, b);
        Assert.AreEqual(10f, delta["performanceScore"], 0.001f);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void ComparisonEngine_PersonalBest_ReturnsHighestPerformance()
    {
        var go      = new GameObject();
        var engine  = go.AddComponent<ComparisonEngine>();
        var sessions = new List<FlightSessionRecord>
        {
            new FlightSessionRecord { performanceScore = 60f },
            new FlightSessionRecord { performanceScore = 95f },
            new FlightSessionRecord { performanceScore = 75f }
        };
        var best = engine.PersonalBest(sessions, "performanceScore");
        Assert.AreEqual(95f, best.performanceScore, 0.001f);
        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AchievementCorrelator
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AchievementCorrelator_FlightCountProgress_ComputesRatioCorrectly()
    {
        var go         = new GameObject();
        var correlator = go.AddComponent<AchievementCorrelator>();
        var progress   = correlator.FlightCountProgress("ach_001", 25, 100);
        Assert.AreEqual(0.25f, progress.ratio, 0.001f);
        Assert.AreEqual(75, progress.estimatedFlightsRemaining);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void AchievementCorrelator_UnlockRate_CorrectCalculation()
    {
        var go         = new GameObject();
        var correlator = go.AddComponent<AchievementCorrelator>();
        Assert.AreEqual(0.1f, correlator.UnlockRate(10, 100), 0.001f);
        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FlightHeatmapGenerator
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void HeatmapGenerator_EmptySessions_ReturnsEmptyHeatmap()
    {
        var go        = new GameObject();
        var generator = go.AddComponent<FlightHeatmapGenerator>();
        var data      = generator.GenerateFlightDensityMap(new List<FlightSessionRecord>());
        Assert.IsNotNull(data);
        Assert.AreEqual(0, data.cells.Count);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void HeatmapGenerator_SessionWithDataPoints_ProducesCells()
    {
        var go        = new GameObject();
        var generator = go.AddComponent<FlightHeatmapGenerator>();
        var session   = new FlightSessionRecord();
        session.dataPoints.Add(new FlightDataPoint { position = Vector3.zero });
        session.dataPoints.Add(new FlightDataPoint { position = new Vector3(100f, 0f, 100f) });

        var data = generator.GenerateFlightDensityMap(new List<FlightSessionRecord> { session });
        Assert.IsNotNull(data);
        Assert.Greater(data.cells.Count, 0);
        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AltitudeHeatmap
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AltitudeHeatmap_BandCount_IsPositive()
    {
        Assert.Greater(AltitudeHeatmap.BandCount, 0);
    }

    [Test]
    public void AltitudeHeatmap_TimeInBands_LengthEqualsBandCount()
    {
        var go      = new GameObject();
        var heatmap = go.AddComponent<AltitudeHeatmap>();
        var bands   = heatmap.ComputeTimeInBands(new List<FlightSessionRecord>());
        Assert.AreEqual(AltitudeHeatmap.BandCount, bands.Length);
        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LandingHeatmap
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LandingHeatmap_NoRecords_AverageOffsetIsZero()
    {
        var go      = new GameObject();
        var heatmap = go.AddComponent<LandingHeatmap>();
        Assert.AreEqual(0f, heatmap.AverageCentrelineOffset(), 0.001f);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void LandingHeatmap_RecordTouchdown_CountIncreases()
    {
        var go      = new GameObject();
        var heatmap = go.AddComponent<LandingHeatmap>();
        heatmap.RecordTouchdown(Vector3.zero, 500f, 5f, -2f, 80f);
        heatmap.RecordTouchdown(Vector3.zero, 600f, -3f, -1.5f, 90f);
        Assert.AreEqual(2, heatmap.Records.Count);
        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FlightReportGenerator
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ReportGenerator_NullSession_ReturnsNull()
    {
        var go        = new GameObject();
        var generator = go.AddComponent<FlightReportGenerator>();
        Assert.IsNull(generator.GeneratePostFlightReport(null));
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void ReportGenerator_ValidSession_ReturnsReport()
    {
        var go        = new GameObject();
        var generator = go.AddComponent<FlightReportGenerator>();
        var session   = new FlightSessionRecord
        {
            startTimeUtc      = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            durationSeconds   = 3600f,
            distanceNm        = 200f,
            performanceScore  = 75f,
            landingScore      = 80f,
            fuelEfficiencyScore = 70f,
            aircraftId        = "A320"
        };
        session.airportsVisited.Add("EGLL");
        session.airportsVisited.Add("KJFK");

        var report = generator.GeneratePostFlightReport(session);
        Assert.IsNotNull(report);
        Assert.IsFalse(string.IsNullOrEmpty(report.title));
        Assert.Greater(report.metrics.Count, 0);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void ReportGenerator_HighScore_AddsTopScoreHighlight()
    {
        var go        = new GameObject();
        var generator = go.AddComponent<FlightReportGenerator>();
        var session   = new FlightSessionRecord
        {
            startTimeUtc     = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            performanceScore = 95f,
            landingScore     = -1f
        };
        var report = generator.GeneratePostFlightReport(session);
        Assert.IsTrue(report.highlights.Exists(h => h.Contains("Excellent")));
        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // WeeklyDigestGenerator
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DigestGenerator_EmptySessions_ReturnsSummaryWithNoFlights()
    {
        var go        = new GameObject();
        var generator = go.AddComponent<WeeklyDigestGenerator>();
        var report    = generator.GenerateDigest(new List<FlightSessionRecord>(), "Test Week");
        Assert.IsNotNull(report);
        Assert.IsTrue(report.summary.Contains("No flights"));
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void DigestGenerator_TwoSessions_CorrectTotalFlights()
    {
        var go        = new GameObject();
        var generator = go.AddComponent<WeeklyDigestGenerator>();
        var sessions  = new List<FlightSessionRecord>
        {
            new FlightSessionRecord { durationSeconds = 3600f },
            new FlightSessionRecord { durationSeconds = 7200f }
        };
        var report = generator.GenerateDigest(sessions, "Test Week");
        Assert.IsNotNull(report);
        Assert.AreEqual(2f, report.metrics["totalFlights"], 0.001f);
        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ShareableCardGenerator
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ShareableCardGenerator_NullSession_ReturnsNull()
    {
        var go        = new GameObject();
        var generator = go.AddComponent<ShareableCardGenerator>();
        Assert.IsNull(generator.BuildFlightCard(null, "Pilot"));
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void ShareableCardGenerator_ValidSession_ReturnsCard()
    {
        var go        = new GameObject();
        var generator = go.AddComponent<ShareableCardGenerator>();
        var session   = new FlightSessionRecord
        {
            departureAirport  = "EGLL",
            arrivalAirport    = "KJFK",
            durationSeconds   = 7200f,
            distanceNm        = 3000f,
            performanceScore  = 85f,
            aircraftId        = "B777"
        };
        var card = generator.BuildFlightCard(session, "TestPilot");
        Assert.IsNotNull(card);
        Assert.AreEqual("TestPilot", card.pilotName);
        Assert.IsTrue(card.route.Contains("EGLL"));
        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LeaderboardManager
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LeaderboardManager_SubmitScore_AddsEntry()
    {
        var go  = new GameObject();
        var mgr = go.AddComponent<LeaderboardManager>();
        mgr.SubmitScore(LeaderboardManager.Categories.PerformanceScore, "p1", "Pilot1", 80f, PilotTier.Private);
        var board = mgr.GetBoard(LeaderboardManager.Categories.PerformanceScore);
        Assert.AreEqual(1, board.Count);
        Assert.AreEqual("p1", board[0].playerId);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void LeaderboardManager_SubmitHigherScore_UpdatesEntry()
    {
        var go  = new GameObject();
        var mgr = go.AddComponent<LeaderboardManager>();
        mgr.SubmitScore(LeaderboardManager.Categories.PerformanceScore, "p1", "Pilot1", 70f, PilotTier.Student);
        mgr.SubmitScore(LeaderboardManager.Categories.PerformanceScore, "p1", "Pilot1", 90f, PilotTier.Private);
        var board = mgr.GetBoard(LeaderboardManager.Categories.PerformanceScore);
        Assert.AreEqual(1, board.Count);
        Assert.AreEqual(90f, board[0].score, 0.001f);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void LeaderboardManager_MultiplePlayers_SortedDescending()
    {
        var go  = new GameObject();
        var mgr = go.AddComponent<LeaderboardManager>();
        mgr.SubmitScore(LeaderboardManager.Categories.TotalDistance, "p1", "A", 100f, PilotTier.Student);
        mgr.SubmitScore(LeaderboardManager.Categories.TotalDistance, "p2", "B", 300f, PilotTier.Captain);
        mgr.SubmitScore(LeaderboardManager.Categories.TotalDistance, "p3", "C", 200f, PilotTier.Private);
        var board = mgr.GetBoard(LeaderboardManager.Categories.TotalDistance);
        Assert.AreEqual(300f, board[0].score, 0.001f);
        Assert.AreEqual(100f, board[2].score, 0.001f);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void LeaderboardManager_GetPlayerRank_ReturnsCorrectRank()
    {
        var go  = new GameObject();
        var mgr = go.AddComponent<LeaderboardManager>();
        mgr.SubmitScore(LeaderboardManager.Categories.LandingAccuracy, "p1", "A", 90f, PilotTier.Captain);
        mgr.SubmitScore(LeaderboardManager.Categories.LandingAccuracy, "p2", "B", 70f, PilotTier.Private);
        Assert.AreEqual(1, mgr.GetPlayerRank(LeaderboardManager.Categories.LandingAccuracy, "p1"));
        Assert.AreEqual(2, mgr.GetPlayerRank(LeaderboardManager.Categories.LandingAccuracy, "p2"));
        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PlayerRankingSystem
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void PlayerRankingSystem_InitialTier_IsStudent()
    {
        var go     = new GameObject();
        var system = go.AddComponent<PlayerRankingSystem>();
        Assert.AreEqual(PilotTier.Student, system.Tier);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void PlayerRankingSystem_LoadHighRating_TierIsAce()
    {
        var go     = new GameObject();
        var system = go.AddComponent<PlayerRankingSystem>();
        system.LoadRating(2000f);
        Assert.AreEqual(PilotTier.Ace, system.Tier);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void PlayerRankingSystem_UpdateRating_RatingChanges()
    {
        var go     = new GameObject();
        var system = go.AddComponent<PlayerRankingSystem>();
        float before = system.Rating;
        system.UpdateRating(1f, 0.5f); // perfect score vs expected 0.5
        Assert.AreNotEqual(before, system.Rating);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void PlayerRankingSystem_ExpectedScore_IsBetweenZeroAndOne()
    {
        var go     = new GameObject();
        var system = go.AddComponent<PlayerRankingSystem>();
        float expected = system.ExpectedScore(1200f);
        Assert.GreaterOrEqual(expected, 0f);
        Assert.LessOrEqual(expected, 1f);
        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AnalyticsTelemetry
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AnalyticsTelemetry_TrackChartView_MostViewedReturnsCorrectChart()
    {
        var go        = new GameObject();
        var telemetry = go.AddComponent<AnalyticsTelemetry>();
        telemetry.TrackChartView(ChartType.Line);
        telemetry.TrackChartView(ChartType.Line);
        telemetry.TrackChartView(ChartType.Bar);
        Assert.AreEqual("Line", telemetry.MostViewedChart());
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void AnalyticsTelemetry_NoViews_MostViewedReturnsNull()
    {
        var go        = new GameObject();
        var telemetry = go.AddComponent<AnalyticsTelemetry>();
        Assert.IsNull(telemetry.MostViewedChart());
        GameObject.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FlightSessionTracker (MonoBehaviour)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SessionTracker_BeginSession_CreatesSessionWithId()
    {
        var go      = new GameObject();
        var tracker = go.AddComponent<FlightSessionTracker>();
        tracker.BeginSession("A320", "EGLL");
        Assert.IsNotNull(tracker.CurrentSession);
        Assert.IsFalse(string.IsNullOrEmpty(tracker.CurrentSession.sessionId));
        Assert.AreEqual("A320", tracker.CurrentSession.aircraftId);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void SessionTracker_EndSession_ReturnsRecord()
    {
        var go      = new GameObject();
        var tracker = go.AddComponent<FlightSessionTracker>();
        tracker.BeginSession("B737");
        var session = tracker.EndSession("KJFK");
        Assert.IsNotNull(session);
        Assert.AreEqual("KJFK", session.arrivalAirport);
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void SessionTracker_VisitAirport_AddsToList()
    {
        var go      = new GameObject();
        var tracker = go.AddComponent<FlightSessionTracker>();
        tracker.BeginSession("B737");
        tracker.VisitAirport("LFPG");
        Assert.IsTrue(tracker.CurrentSession.airportsVisited.Contains("LFPG"));
        GameObject.DestroyImmediate(go);
    }

    [Test]
    public void SessionTracker_VisitSameAirportTwice_NoDuplicate()
    {
        var go      = new GameObject();
        var tracker = go.AddComponent<FlightSessionTracker>();
        tracker.BeginSession("B737");
        tracker.VisitAirport("EGLL");
        tracker.VisitAirport("EGLL");
        int count = tracker.CurrentSession.airportsVisited.FindAll(a => a == "EGLL").Count;
        Assert.AreEqual(1, count);
        GameObject.DestroyImmediate(go);
    }
}
