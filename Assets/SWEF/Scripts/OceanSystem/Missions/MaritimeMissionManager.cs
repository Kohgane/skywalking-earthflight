// MaritimeMissionManager.cs — Phase 117: Advanced Ocean & Maritime System
// Maritime mission system: SAR, cargo delivery, patrol, medevac, firefighting.
// Namespace: SWEF.OceanSystem

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Singleton that manages the pool of active maritime missions.
    /// Spawns missions of various types and exposes mission lifecycle events.
    /// </summary>
    public class MaritimeMissionManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────

        /// <summary>Shared singleton instance.</summary>
        public static MaritimeMissionManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Mission Spawning")]
        [SerializeField] private float missionSpawnIntervalSeconds = 300f;
        [SerializeField] private int   maxActiveMissions = 4;

        // ── Private state ─────────────────────────────────────────────────────────

        private readonly List<SARMissionData> _activeMissions = new List<SARMissionData>();
        private float _spawnTimer;
        private int   _nextMissionIndex;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when a new mission is added.</summary>
        public event Action<SARMissionData> OnMissionAdded;

        /// <summary>Raised when a mission is completed.</summary>
        public event Action<SARMissionData> OnMissionCompleted;

        /// <summary>Raised when a mission expires.</summary>
        public event Action<SARMissionData> OnMissionExpired;

        // ── Public Properties ─────────────────────────────────────────────────────

        /// <summary>Read-only list of currently active missions.</summary>
        public IReadOnlyList<SARMissionData> ActiveMissions => _activeMissions;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= missionSpawnIntervalSeconds)
            {
                _spawnTimer = 0f;
                TrySpawnMission();
            }

            TickMissions();
        }

        // ── Mission Lifecycle ─────────────────────────────────────────────────────

        private void TrySpawnMission()
        {
            if (_activeMissions.Count >= maxActiveMissions) return;

            var mission = CreateSARMission();
            _activeMissions.Add(mission);
            OnMissionAdded?.Invoke(mission);
        }

        private SARMissionData CreateSARMission()
        {
            _nextMissionIndex++;
            float angle = UnityEngine.Random.Range(0f, 360f);
            float dist  = UnityEngine.Random.Range(5000f, 30000f);
            var   datum = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * dist,
                                       0f,
                                       Mathf.Cos(angle * Mathf.Deg2Rad) * dist);

            return new SARMissionData
            {
                missionId      = "SAR-" + _nextMissionIndex,
                datumPosition  = datum,
                survivorCount  = UnityEngine.Random.Range(1, 6),
                rescuedCount   = 0,
                searchPattern  = (SearchPattern)UnityEngine.Random.Range(0, 3),
                timeLimitSeconds = 600f,
                isActive       = true
            };
        }

        private void TickMissions()
        {
            for (int i = _activeMissions.Count - 1; i >= 0; i--)
            {
                var m = _activeMissions[i];
                if (!m.isActive) continue;

                if (m.timeLimitSeconds > 0f)
                {
                    m.timeLimitSeconds -= Time.deltaTime;
                    if (m.timeLimitSeconds <= 0f)
                    {
                        m.isActive = false;
                        _activeMissions.RemoveAt(i);
                        OnMissionExpired?.Invoke(m);
                    }
                }
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Marks a survivor rescued on the given mission.</summary>
        public void RecordRescue(string missionId)
        {
            var m = _activeMissions.Find(x => x.missionId == missionId);
            if (m == null) return;

            m.rescuedCount++;
            if (m.rescuedCount >= m.survivorCount)
            {
                m.isActive = false;
                _activeMissions.Remove(m);
                OnMissionCompleted?.Invoke(m);
            }
        }
    }
}
