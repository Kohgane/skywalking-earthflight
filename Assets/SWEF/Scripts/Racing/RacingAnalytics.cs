// RacingAnalytics.cs — SWEF Boost & Drift Mechanics System (Phase 62)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Racing
{
    /// <summary>
    /// Periodic analytics snapshot for the racing session.
    /// </summary>
    [Serializable]
    public sealed class RacingAnalyticsSnapshot
    {
        /// <summary>UTC timestamp when this snapshot was captured (ISO-8601).</summary>
        public string timestamp;

        /// <summary>Total seconds of active boost during the session.</summary>
        public float totalBoostTimeSeconds;

        /// <summary>Number of boost activations per <see cref="BoostType"/>.</summary>
        public Dictionary<BoostType, int> boostUsagePerType;

        /// <summary>Highest drift level achieved during the session.</summary>
        public DriftLevel highestDriftLevel;

        /// <summary>Average drift level achieved (as a numeric average of <see cref="DriftLevel"/> values).</summary>
        public float averageDriftLevel;

        /// <summary>Number of drifts that were cancelled without releasing.</summary>
        public int driftCancelCount;

        /// <summary>Total drift releases recorded.</summary>
        public int driftReleaseCount;

        /// <summary>Drift cancel rate (0–1): cancels / (cancels + releases).</summary>
        public float driftCancelRate;

        /// <summary>Most frequent drift direction.</summary>
        public DriftDirection mostCommonDriftDirection;

        /// <summary>Percentage of races where slipstream was used at least once (0–1).</summary>
        public float slipstreamUtilizationPercent;

        /// <summary>Distribution of <see cref="StartBoostGrade"/> results.</summary>
        public Dictionary<StartBoostGrade, int> startBoostGradeDistribution;

        /// <summary>Ratio of successful trick completions to total trick attempts (0–1).</summary>
        public float trickSuccessRatio;

        /// <summary>Total trick attempts this session.</summary>
        public int totalTrickAttempts;

        /// <summary>Total successful trick completions this session.</summary>
        public int totalTrickSuccesses;
    }

    /// <summary>
    /// Phase 62 — Static utility class that tracks racing analytics for the session.
    ///
    /// <para>Subscribe to events from all racing controllers at startup and call
    /// <see cref="TakeSnapshot"/> periodically (e.g. every 60 s) or on race end.
    /// Pass the snapshot to <c>AnalyticsLogger</c> via the
    /// <c>SWEF_ANALYTICS_AVAILABLE</c> compile guard.</para>
    /// </summary>
    public static class RacingAnalytics
    {
        #region Private State

        private static readonly Dictionary<BoostType, int>      BoostUsage     = new Dictionary<BoostType, int>();
        private static readonly Dictionary<StartBoostGrade, int> StartGrades    = new Dictionary<StartBoostGrade, int>();

        private static float  _totalBoostTimeSeconds;
        private static float  _boostStartTime = -1f;
        private static bool   _boostActive;

        private static int        _driftReleaseCount;
        private static int        _driftCancelCount;
        private static DriftLevel _highestDriftLevel  = DriftLevel.None;
        private static float      _driftLevelSum;

        private static int _leftDriftCount;
        private static int _rightDriftCount;

        private static bool  _slipstreamUsedThisRace;
        private static int   _racesWithSlipstream;
        private static int   _totalRaces;

        private static int _trickAttempts;
        private static int _trickSuccesses;

        private static bool _subscribed;

        #endregion

        #region Public API

        /// <summary>
        /// Subscribes to all available racing controller events.
        /// Call once at session start from a MonoBehaviour initialiser.
        /// </summary>
        public static void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;

            if (BoostController.Instance != null)
            {
                BoostController.Instance.OnBoostStart += OnBoostStart;
                BoostController.Instance.OnBoostEnd   += OnBoostEnd;
            }

            if (DriftController.Instance != null)
            {
                DriftController.Instance.OnDriftStart   += OnDriftStart;
                DriftController.Instance.OnDriftLevelUp += OnDriftLevelUp;
                DriftController.Instance.OnDriftRelease += OnDriftRelease;
                DriftController.Instance.OnDriftCancel  += OnDriftCancel;
            }

            if (SlipstreamController.Instance != null)
            {
                SlipstreamController.Instance.OnSlipstreamEnter += OnSlipstreamEnter;
            }
        }

        /// <summary>
        /// Unsubscribes from all racing controller events.
        /// Call on session end or when the analytics system is torn down.
        /// </summary>
        public static void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;

            if (BoostController.Instance != null)
            {
                BoostController.Instance.OnBoostStart -= OnBoostStart;
                BoostController.Instance.OnBoostEnd   -= OnBoostEnd;
            }

            if (DriftController.Instance != null)
            {
                DriftController.Instance.OnDriftStart   -= OnDriftStart;
                DriftController.Instance.OnDriftLevelUp -= OnDriftLevelUp;
                DriftController.Instance.OnDriftRelease -= OnDriftRelease;
                DriftController.Instance.OnDriftCancel  -= OnDriftCancel;
            }

            if (SlipstreamController.Instance != null)
            {
                SlipstreamController.Instance.OnSlipstreamEnter -= OnSlipstreamEnter;
            }
        }

        /// <summary>
        /// Records a trick attempt (called when a trick starts airborne).
        /// </summary>
        public static void RecordTrickAttempt()
        {
            _trickAttempts++;
        }

        /// <summary>
        /// Records a successful trick completion.
        /// </summary>
        public static void RecordTrickSuccess()
        {
            _trickSuccesses++;
        }

        /// <summary>
        /// Records a start-boost result from <see cref="StartBoostController"/>.
        /// </summary>
        /// <param name="grade">Grade achieved at race start.</param>
        public static void RecordStartBoostGrade(StartBoostGrade grade)
        {
            if (!StartGrades.ContainsKey(grade)) StartGrades[grade] = 0;
            StartGrades[grade]++;
        }

        /// <summary>
        /// Marks the end of a race — used to compute slipstream utilisation rate.
        /// </summary>
        public static void RecordRaceEnd()
        {
            _totalRaces++;
            if (_slipstreamUsedThisRace) _racesWithSlipstream++;
            _slipstreamUsedThisRace = false;
        }

        /// <summary>
        /// Resets all accumulated session data.
        /// </summary>
        public static void ResetSession()
        {
            BoostUsage.Clear();
            StartGrades.Clear();
            _totalBoostTimeSeconds  = 0f;
            _boostActive            = false;
            _driftReleaseCount      = 0;
            _driftCancelCount       = 0;
            _highestDriftLevel      = DriftLevel.None;
            _driftLevelSum          = 0f;
            _leftDriftCount         = 0;
            _rightDriftCount        = 0;
            _slipstreamUsedThisRace = false;
            _racesWithSlipstream    = 0;
            _totalRaces             = 0;
            _trickAttempts          = 0;
            _trickSuccesses         = 0;
        }

        /// <summary>
        /// Creates and returns a snapshot of the current analytics state.
        /// </summary>
        /// <returns>A populated <see cref="RacingAnalyticsSnapshot"/> instance.</returns>
        public static RacingAnalyticsSnapshot TakeSnapshot()
        {
            // Flush any open boost timer.
            if (_boostActive)
                _totalBoostTimeSeconds += Time.realtimeSinceStartup - _boostStartTime;

            int totalDriftEvents = _driftReleaseCount + _driftCancelCount;
            float cancelRate = totalDriftEvents > 0
                ? (float)_driftCancelCount / totalDriftEvents
                : 0f;

            float avgDriftLevel = _driftReleaseCount > 0
                ? _driftLevelSum / _driftReleaseCount
                : 0f;

            float slipstreamUtil = _totalRaces > 0
                ? (float)_racesWithSlipstream / _totalRaces
                : 0f;

            float trickRatio = _trickAttempts > 0
                ? (float)_trickSuccesses / _trickAttempts
                : 0f;

            DriftDirection mostCommonDir;
            if (_leftDriftCount == 0 && _rightDriftCount == 0)
                mostCommonDir = DriftDirection.None;
            else
                mostCommonDir = _leftDriftCount >= _rightDriftCount
                    ? DriftDirection.Left
                    : DriftDirection.Right;

            return new RacingAnalyticsSnapshot
            {
                timestamp                    = DateTime.UtcNow.ToString("o"),
                totalBoostTimeSeconds        = _totalBoostTimeSeconds,
                boostUsagePerType            = new Dictionary<BoostType, int>(BoostUsage),
                highestDriftLevel            = _highestDriftLevel,
                averageDriftLevel            = avgDriftLevel,
                driftCancelCount             = _driftCancelCount,
                driftReleaseCount            = _driftReleaseCount,
                driftCancelRate              = cancelRate,
                mostCommonDriftDirection     = mostCommonDir,
                slipstreamUtilizationPercent = slipstreamUtil,
                startBoostGradeDistribution  = new Dictionary<StartBoostGrade, int>(StartGrades),
                trickSuccessRatio            = trickRatio,
                totalTrickAttempts           = _trickAttempts,
                totalTrickSuccesses          = _trickSuccesses
            };
        }

        /// <summary>
        /// Serialises the current snapshot to a JSON string and optionally forwards
        /// it to <c>AnalyticsLogger</c>.
        /// </summary>
        /// <returns>JSON string representation of the snapshot.</returns>
        public static string ExportSnapshotJson()
        {
            var snapshot = TakeSnapshot();
#if SWEF_ANALYTICS_AVAILABLE
            // AnalyticsLogger.Instance?.LogEvent("racing_snapshot", JsonUtility.ToJson(snapshot));
#endif
            return JsonUtility.ToJson(snapshot, prettyPrint: true);
        }

        #endregion

        #region Event Handlers

        private static void OnBoostStart(BoostConfig config)
        {
            BoostType type = config?.boostType ?? BoostType.BoostPad;
            if (!BoostUsage.ContainsKey(type)) BoostUsage[type] = 0;
            BoostUsage[type]++;

            if (!_boostActive)
            {
                _boostActive    = true;
                _boostStartTime = Time.realtimeSinceStartup;
            }
        }

        private static void OnBoostEnd(BoostType type)
        {
            if (_boostActive)
            {
                _totalBoostTimeSeconds += Time.realtimeSinceStartup - _boostStartTime;
                _boostActive = false;
            }
        }

        private static void OnDriftStart(DriftDirection dir)
        {
            if (dir == DriftDirection.Left)  _leftDriftCount++;
            if (dir == DriftDirection.Right) _rightDriftCount++;
        }

        private static void OnDriftLevelUp(DriftLevel level)
        {
            if (level > _highestDriftLevel) _highestDriftLevel = level;
        }

        private static void OnDriftRelease(DriftLevel level, BoostConfig reward)
        {
            _driftReleaseCount++;
            _driftLevelSum += (int)level;
        }

        private static void OnDriftCancel(DriftLevel level)
        {
            _driftCancelCount++;
        }

        private static void OnSlipstreamEnter(string leadPlayerId)
        {
            _slipstreamUsedThisRace = true;
        }

        #endregion
    }
}
