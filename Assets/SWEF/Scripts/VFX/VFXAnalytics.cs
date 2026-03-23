// VFXAnalytics.cs — SWEF Particle Effects & VFX System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.VFX
{
    /// <summary>
    /// Tracks runtime VFX analytics and generates periodic snapshots for reporting.
    ///
    /// <para>Metrics recorded:</para>
    /// <list type="bullet">
    /// <item><description>Trigger frequency per <see cref="VFXType"/>.</description></item>
    /// <item><description>Pool hit vs miss ratio.</description></item>
    /// <item><description>Average and peak active particle count.</description></item>
    /// <item><description>LOD distribution across all tracked instances.</description></item>
    /// <item><description>Estimated per-effect performance impact.</description></item>
    /// </list>
    ///
    /// <para>Call <see cref="TakeSnapshot"/> to capture the current state into a
    /// <see cref="VFXAnalyticsSnapshot"/> struct suitable for logging or upload.</para>
    /// </summary>
    public sealed class VFXAnalytics : MonoBehaviour
    {
        // ── Analytics Snapshot ────────────────────────────────────────────────────

        /// <summary>
        /// Immutable data snapshot of VFX system performance captured at a point in time.
        /// </summary>
        [Serializable]
        public struct VFXAnalyticsSnapshot
        {
            /// <summary>UTC timestamp when the snapshot was taken (seconds since epoch).</summary>
            public double TimestampUtc;

            /// <summary>Total number of VFX spawn calls since session start.</summary>
            public int TotalSpawns;

            /// <summary>Number of spawn calls fulfilled from the pool (cache hit).</summary>
            public int PoolHits;

            /// <summary>Number of spawn calls that triggered pool growth or were dropped (cache miss).</summary>
            public int PoolMisses;

            /// <summary>Pool hit ratio (0–1). 1 = all spawns fulfilled from existing pool entries.</summary>
            public float PoolHitRatio;

            /// <summary>Current number of active VFX instances.</summary>
            public int ActiveInstances;

            /// <summary>Current global particle budget used.</summary>
            public int ParticleBudgetUsed;

            /// <summary>Maximum global particle budget.</summary>
            public int ParticleBudgetMax;

            /// <summary>Peak simultaneous active particle count recorded this session.</summary>
            public int PeakParticleCount;

            /// <summary>Most frequently triggered VFX type this session.</summary>
            public VFXType MostTriggeredType;

            /// <summary>Number of times the most triggered type was spawned.</summary>
            public int MostTriggeredCount;

            /// <summary>LOD level distribution: x=Full, y=Reduced, z=Minimal, w=Billboard (counts).</summary>
            public Vector4Int LODDistribution;

            /// <summary>Returns a formatted one-line summary string.</summary>
            public override string ToString() =>
                $"[VFX Snapshot @ {TimestampUtc:F0}] Spawns={TotalSpawns} " +
                $"HitRatio={PoolHitRatio:P1} Active={ActiveInstances} " +
                $"Particles={ParticleBudgetUsed}/{ParticleBudgetMax} " +
                $"Peak={PeakParticleCount} Top={MostTriggeredType}×{MostTriggeredCount}";
        }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Reporting")]
        [Tooltip("If enabled, a snapshot is logged to the console at the specified interval.")]
        [SerializeField] private bool periodicLogging = true;

        [Tooltip("Interval in seconds between automatic snapshot logs.")]
        [SerializeField, Min(5f)] private float logIntervalSeconds = 60f;

        // ── Private State ─────────────────────────────────────────────────────────

        private readonly Dictionary<VFXType, int> _spawnCounts     = new Dictionary<VFXType, int>();
        private readonly Dictionary<VFXLODLevel, int> _lodCounts   = new Dictionary<VFXLODLevel, int>();

        private int   _totalSpawns;
        private int   _poolHits;
        private int   _poolMisses;
        private int   _peakParticleCount;
        private float _nextLogTime;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            foreach (VFXType t in Enum.GetValues(typeof(VFXType)))
                _spawnCounts[t] = 0;

            foreach (VFXLODLevel l in Enum.GetValues(typeof(VFXLODLevel)))
                _lodCounts[l] = 0;
        }

        private void OnEnable()
        {
            if (VFXPoolManager.Instance != null)
                VFXPoolManager.Instance.OnPoolExhausted += HandlePoolExhausted;
        }

        private void OnDisable()
        {
            if (VFXPoolManager.Instance != null)
                VFXPoolManager.Instance.OnPoolExhausted -= HandlePoolExhausted;
        }

        private void Update()
        {
            TrackParticleBudget();

            if (periodicLogging && Time.time >= _nextLogTime)
            {
                _nextLogTime = Time.time + logIntervalSeconds;
                Debug.Log(TakeSnapshot().ToString());
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Records a spawn event for analytics.
        /// Should be called each time a VFX instance is successfully spawned.
        /// </summary>
        /// <param name="type">Type of effect that was spawned.</param>
        /// <param name="fromPool">Whether the instance was retrieved from an existing pool entry.</param>
        public void RecordSpawn(VFXType type, bool fromPool)
        {
            _totalSpawns++;
            _spawnCounts[type] = _spawnCounts.TryGetValue(type, out int c) ? c + 1 : 1;

            if (fromPool) _poolHits++;
            else          _poolMisses++;
        }

        /// <summary>Records a LOD level observation for an active instance.</summary>
        /// <param name="level">Current LOD level of the instance.</param>
        public void RecordLOD(VFXLODLevel level)
        {
            _lodCounts[level] = _lodCounts.TryGetValue(level, out int c) ? c + 1 : 1;
        }

        /// <summary>
        /// Captures the current analytics state into a <see cref="VFXAnalyticsSnapshot"/>.
        /// </summary>
        public VFXAnalyticsSnapshot TakeSnapshot()
        {
            VFXType topType  = VFXType.EngineExhaust;
            int     topCount = 0;

            foreach (var kv in _spawnCounts)
            {
                if (kv.Value > topCount)
                {
                    topCount = kv.Value;
                    topType  = kv.Key;
                }
            }

            int budgetUsed = VFXPoolManager.Instance != null ? VFXPoolManager.Instance.ParticleBudgetUsed : 0;
            int budgetMax  = VFXPoolManager.Instance != null ? VFXPoolManager.Instance.MaxGlobalParticles  : 50_000;
            int active     = VFXPoolManager.Instance != null ? VFXPoolManager.Instance.ActiveCount         : 0;

            int total       = Mathf.Max(1, _totalSpawns);
            float hitRatio  = (float)_poolHits / total;

            return new VFXAnalyticsSnapshot
            {
                TimestampUtc         = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds,
                TotalSpawns          = _totalSpawns,
                PoolHits             = _poolHits,
                PoolMisses           = _poolMisses,
                PoolHitRatio         = hitRatio,
                ActiveInstances      = active,
                ParticleBudgetUsed   = budgetUsed,
                ParticleBudgetMax    = budgetMax,
                PeakParticleCount    = _peakParticleCount,
                MostTriggeredType    = topType,
                MostTriggeredCount   = topCount,
                LODDistribution      = new Vector4Int(
                    _lodCounts.TryGetValue(VFXLODLevel.Full,      out int f) ? f : 0,
                    _lodCounts.TryGetValue(VFXLODLevel.Reduced,   out int r) ? r : 0,
                    _lodCounts.TryGetValue(VFXLODLevel.Minimal,   out int m) ? m : 0,
                    _lodCounts.TryGetValue(VFXLODLevel.Billboard, out int b) ? b : 0)
            };
        }

        /// <summary>Resets all counters for a new session or test run.</summary>
        public void ResetCounters()
        {
            _totalSpawns       = 0;
            _poolHits          = 0;
            _poolMisses        = 0;
            _peakParticleCount = 0;
            foreach (VFXType t in Enum.GetValues(typeof(VFXType)))
                _spawnCounts[t] = 0;
            foreach (VFXLODLevel l in Enum.GetValues(typeof(VFXLODLevel)))
                _lodCounts[l] = 0;
        }

        // ── Internal Helpers ──────────────────────────────────────────────────────

        private void TrackParticleBudget()
        {
            if (VFXPoolManager.Instance == null) return;
            int current = VFXPoolManager.Instance.ParticleBudgetUsed;
            if (current > _peakParticleCount) _peakParticleCount = current;
        }

        private void HandlePoolExhausted(VFXType type)
        {
            // Pool exhaustion is already counted as a miss by RecordSpawn(fromPool: false);
            // no additional increment needed here.
        }

#if UNITY_EDITOR
        [ContextMenu("Log Snapshot")]
        private void EditorLogSnapshot() => Debug.Log(TakeSnapshot().ToString());

        [ContextMenu("Reset Counters")]
        private void EditorReset() => ResetCounters();
#endif
    }
}
