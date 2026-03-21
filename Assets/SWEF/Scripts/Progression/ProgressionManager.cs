using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.Progression
{
    /// <summary>
    /// Central singleton manager for player XP, pilot rank, and lifetime flight statistics.
    /// Persists data to <c>Application.persistentDataPath/progression.json</c>.
    /// Call <see cref="AddXP"/> from any system that rewards the player, and subscribe to
    /// <see cref="OnXPGained"/> / <see cref="OnRankUp"/> to react to progression changes.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class ProgressionManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static ProgressionManager Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired whenever XP is added. Parameters: amount, source label.</summary>
        public event Action<long, string> OnXPGained;

        /// <summary>Fired when the player's rank increases. Parameters: old rank, new rank.</summary>
        public event Action<PilotRankData, PilotRankData> OnRankUp;

        /// <summary>Fired whenever flight statistics change (each frame during flight).</summary>
        public event Action OnStatsUpdated;

        // ── Persistence path ──────────────────────────────────────────────────────
        private static readonly string SaveFileName = "progression.json";
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        // ── Loaded rank table ─────────────────────────────────────────────────────
        private List<PilotRankData> _ranks = new List<PilotRankData>();

        // ── Saved state ───────────────────────────────────────────────────────────
        [Serializable]
        private class ProgressionSaveData
        {
            public long  currentXP;
            public int   currentRankLevel;
            public float totalFlightTimeSeconds;
            public float totalDistanceKm;
            public int   totalFlightsCompleted;
            public float topAltitude;
            public float topSpeedMps;
            public List<XPHistoryEntry> xpHistory = new List<XPHistoryEntry>();
        }

        [Serializable]
        private class XPHistoryEntry
        {
            public long   amount;
            public string source;
            public string timestamp;
        }

        private ProgressionSaveData _save = new ProgressionSaveData();

        // ── Public read-only state ─────────────────────────────────────────────────
        /// <summary>Player's total accumulated XP.</summary>
        public long CurrentXP           => _save.currentXP;

        /// <summary>Current rank level (1–50).</summary>
        public int  CurrentRankLevel     => _save.currentRankLevel;

        /// <summary>Total seconds spent in active flight.</summary>
        public float TotalFlightTimeSeconds => _save.totalFlightTimeSeconds;

        /// <summary>Total kilometres flown across all sessions.</summary>
        public float TotalDistanceKm     => _save.totalDistanceKm;

        /// <summary>Total number of completed flights.</summary>
        public int  TotalFlightsCompleted => _save.totalFlightsCompleted;

        /// <summary>Highest altitude (metres) ever reached.</summary>
        public float TopAltitude         => _save.topAltitude;

        /// <summary>Highest speed (m/s) ever reached.</summary>
        public float TopSpeedMps         => _save.topSpeedMps;

        /// <summary>Ordered log of recent XP gains (newest first, capped at 200 entries).</summary>
        public IReadOnlyList<(long amount, string source, string timestamp)> XPHistory
        {
            get
            {
                var list = new List<(long, string, string)>(_save.xpHistory.Count);
                foreach (var e in _save.xpHistory)
                    list.Add((e.amount, e.source, e.timestamp));
                return list;
            }
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadRanks();
            Load();

            // Ensure at least rank 1
            if (_save.currentRankLevel < 1)
                _save.currentRankLevel = 1;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Adds XP to the player's total, checks for rank-up, and fires events.
        /// </summary>
        /// <param name="amount">Amount of XP to add (clamped to ≥ 0).</param>
        /// <param name="source">Human-readable label of the XP source (e.g. "Achievement", "Flight").</param>
        public void AddXP(long amount, string source)
        {
            if (amount <= 0) return;

            _save.currentXP += amount;

            // Record history (newest first, cap at 200)
            _save.xpHistory.Insert(0, new XPHistoryEntry
            {
                amount    = amount,
                source    = source,
                timestamp = DateTime.UtcNow.ToString("o")
            });
            if (_save.xpHistory.Count > 200)
                _save.xpHistory.RemoveAt(_save.xpHistory.Count - 1);

            OnXPGained?.Invoke(amount, source);

            // Check rank-ups (may advance multiple levels at once)
            CheckRankUps();

            Save();
        }

        /// <summary>
        /// Returns the <see cref="PilotRankData"/> for the player's current rank,
        /// or <c>null</c> if no ranks are loaded.
        /// </summary>
        public PilotRankData GetCurrentRank()
        {
            return GetRankByLevel(_save.currentRankLevel);
        }

        /// <summary>
        /// Returns the <see cref="PilotRankData"/> for the next rank,
        /// or <c>null</c> if the player is at maximum rank.
        /// </summary>
        public PilotRankData GetNextRank()
        {
            return GetRankByLevel(_save.currentRankLevel + 1);
        }

        /// <summary>
        /// Returns how much XP is still required to reach the next rank.
        /// Returns 0 if at max rank.
        /// </summary>
        public long GetXPToNextRank()
        {
            var next = GetNextRank();
            if (next == null) return 0L;
            long diff = next.requiredXP - _save.currentXP;
            return diff < 0 ? 0L : diff;
        }

        /// <summary>
        /// Returns a 0–1 float representing progress toward the next rank.
        /// Returns 1 if at max rank.
        /// </summary>
        public float GetProgressToNextRank01()
        {
            var current = GetCurrentRank();
            var next    = GetNextRank();
            if (current == null || next == null) return 1f;

            long rangeStart = current.requiredXP;
            long rangeEnd   = next.requiredXP;
            if (rangeEnd <= rangeStart) return 1f;

            float progress = (float)(_save.currentXP - rangeStart) / (rangeEnd - rangeStart);
            return Mathf.Clamp01(progress);
        }

        /// <summary>Returns the player's total accumulated XP.</summary>
        public long GetTotalXP() => _save.currentXP;

        /// <summary>
        /// Updates per-frame flight statistics. Call once per frame while the player is flying.
        /// </summary>
        /// <param name="deltaTime">Time elapsed this frame (seconds).</param>
        /// <param name="distanceDelta">Distance flown this frame (kilometres).</param>
        /// <param name="currentAltitude">Current altitude in metres (optional, used for top-altitude tracking).</param>
        /// <param name="currentSpeedMps">Current speed in m/s (optional, used for top-speed tracking).</param>
        public void UpdateFlightStats(float deltaTime, float distanceDelta,
                                      float currentAltitude = 0f, float currentSpeedMps = 0f)
        {
            _save.totalFlightTimeSeconds += deltaTime;
            _save.totalDistanceKm       += distanceDelta;

            if (currentAltitude > _save.topAltitude)
                _save.topAltitude = currentAltitude;
            if (currentSpeedMps > _save.topSpeedMps)
                _save.topSpeedMps = currentSpeedMps;

            OnStatsUpdated?.Invoke();
        }

        /// <summary>
        /// Increments the total flights completed counter and saves.
        /// Call when a flight session ends normally.
        /// </summary>
        public void RecordFlightCompleted()
        {
            _save.totalFlightsCompleted++;
            Save();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void LoadRanks()
        {
            var assets = Resources.LoadAll<PilotRankData>("Ranks");
            _ranks.Clear();
            foreach (var r in assets)
                _ranks.Add(r);

            if (_ranks.Count == 0)
            {
                // Fall back to programmatic defaults so the system works without assets
                _ranks = ProgressionDefaultData.GetDefaultRanks();
                Debug.Log("[SWEF] ProgressionManager: No PilotRankData assets found in Resources/Ranks/. Using built-in defaults.");
            }
            else
            {
                _ranks.Sort((a, b) => a.rankLevel.CompareTo(b.rankLevel));
                Debug.Log($"[SWEF] ProgressionManager: Loaded {_ranks.Count} rank definitions.");
            }
        }

        private PilotRankData GetRankByLevel(int level)
        {
            foreach (var r in _ranks)
                if (r.rankLevel == level) return r;
            return null;
        }

        private void CheckRankUps()
        {
            bool rankChanged = false;
            PilotRankData oldRank = GetCurrentRank();

            foreach (var rank in _ranks)
            {
                if (rank.rankLevel <= _save.currentRankLevel) continue;
                if (_save.currentXP >= rank.requiredXP)
                {
                    PilotRankData prev = GetCurrentRank();
                    _save.currentRankLevel = rank.rankLevel;
                    rankChanged = true;

                    OnRankUp?.Invoke(prev, rank);
                    Debug.Log($"[SWEF] ProgressionManager: Rank up! {prev?.rankName} → {rank.rankName}");
                }
            }
        }

        // ── Persistence ───────────────────────────────────────────────────────────

        private void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(_save, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] ProgressionManager: Failed to save progression data — {ex.Message}");
            }
        }

        private void Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    _save = JsonUtility.FromJson<ProgressionSaveData>(json) ?? new ProgressionSaveData();
                    Debug.Log($"[SWEF] ProgressionManager: Loaded progression data (XP={_save.currentXP}, Rank={_save.currentRankLevel}).");
                }
                else
                {
                    _save = new ProgressionSaveData { currentRankLevel = 1 };
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] ProgressionManager: Failed to load progression data — {ex.Message}");
                _save = new ProgressionSaveData { currentRankLevel = 1 };
            }
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) Save();
        }
    }
}
