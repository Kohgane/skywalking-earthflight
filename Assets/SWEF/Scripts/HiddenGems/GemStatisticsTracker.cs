using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using SWEF.Progression;

namespace SWEF.HiddenGems
{
    // ── Data class ────────────────────────────────────────────────────────────────

    /// <summary>Snapshot of all gem-discovery statistics.</summary>
    [Serializable]
    public class GemStatistics
    {
        public int   totalDiscovered;
        public int   totalGems;
        public float discoveryRatePerHour;
        public float averageDiscoveryAltitude;
        public float averageDiscoverySpeed;
        public string mostVisitedGemId;
        public int   mostVisitedCount;
        public int   currentStreak;
        public int   longestStreak;
        public string lastDiscoveryDate;
        public List<ContinentCompletion> continentCompletions = new List<ContinentCompletion>();
        public List<RarityCount>         rarityDistribution   = new List<RarityCount>();
    }

    [Serializable]
    public class ContinentCompletion
    {
        public GemContinent continent;
        public int          discovered;
        public int          total;
        public float        percent;
    }

    [Serializable]
    public class RarityCount
    {
        public GemRarity rarity;
        public int       count;
    }

    // ── Manager ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tracks player gem-discovery statistics and persists them to
    /// <c>Application.persistentDataPath/hidden_gems_stats.json</c>.
    /// Subscribes to <see cref="HiddenGemManager.OnGemDiscovered"/>.
    /// </summary>
    public class GemStatisticsTracker : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static GemStatisticsTracker Instance { get; private set; }

        // ── Persistence ───────────────────────────────────────────────────────────
        private static readonly string SaveFile = "hidden_gems_stats.json";
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFile);

        // ── Serialised state ──────────────────────────────────────────────────────
        [Serializable]
        private class TrackerSaveData
        {
            public float              totalFlightHoursAtStart;
            public List<string>       discoveryDates = new List<string>(); // ISO 8601 per discovery
            public List<float>        altitudes      = new List<float>();
            public List<float>        speeds         = new List<float>();
            public List<string>       visitHistory   = new List<string>(); // gemId per visit-record
            public int                longestStreak;
        }

        private TrackerSaveData _save = new TrackerSaveData();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        private void OnEnable()
        {
            if (HiddenGemManager.Instance != null)
                HiddenGemManager.Instance.OnGemDiscovered += HandleDiscovery;
        }

        private void OnDisable()
        {
            if (HiddenGemManager.Instance != null)
                HiddenGemManager.Instance.OnGemDiscovered -= HandleDiscovery;
        }

        private void OnApplicationPause(bool p) { if (p) Save(); }
        private void OnApplicationQuit()         { Save(); }

        // ── Event handler ─────────────────────────────────────────────────────────

        private void HandleDiscovery(GemDiscoveryEvent evt)
        {
            _save.discoveryDates.Add(evt.timestamp.ToString("o"));
            _save.altitudes.Add(evt.state.discoveryAltitude);
            _save.speeds.Add(evt.state.discoverySpeed);
            UpdateStreak();
            Save();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Computes and returns a full statistics snapshot.</summary>
        public GemStatistics GetStatistics()
        {
            var mgr = HiddenGemManager.Instance;
            if (mgr == null) return new GemStatistics();

            var disc  = mgr.GetDiscoveredGems();
            var all   = mgr.GetAllGems();
            var stats = new GemStatistics
            {
                totalDiscovered       = disc.Count,
                totalGems             = all.Count,
                lastDiscoveryDate     = _save.discoveryDates.Count > 0
                                            ? _save.discoveryDates[_save.discoveryDates.Count - 1]
                                            : "",
                currentStreak         = ComputeCurrentStreak(),
                longestStreak         = _save.longestStreak,
                averageDiscoveryAltitude = _save.altitudes.Count > 0
                                            ? _save.altitudes.Average()
                                            : 0f,
                averageDiscoverySpeed    = _save.speeds.Count > 0
                                            ? _save.speeds.Average()
                                            : 0f
            };

            // Discovery rate (per hour of flight)
            if (ProgressionManager.Instance != null)
            {
                float hours = ProgressionManager.Instance.TotalFlightTimeSeconds / 3600f;
                stats.discoveryRatePerHour = hours > 0f ? disc.Count / hours : 0f;
            }

            // Most visited gem
            var states = all.Select(g => mgr.GetGemState(g.gemId))
                            .Where(s => s != null && s.timesVisited > 0)
                            .OrderByDescending(s => s.timesVisited)
                            .FirstOrDefault();
            if (states != null)
            {
                stats.mostVisitedGemId = states.gemId;
                stats.mostVisitedCount = states.timesVisited;
            }

            // Continent completions
            foreach (GemContinent c in Enum.GetValues(typeof(GemContinent)))
            {
                var (d, t) = mgr.GetContinentProgress(c);
                stats.continentCompletions.Add(new ContinentCompletion
                {
                    continent  = c,
                    discovered = d,
                    total      = t,
                    percent    = t > 0 ? (float)d / t * 100f : 0f
                });
            }

            // Rarity distribution
            foreach (GemRarity r in Enum.GetValues(typeof(GemRarity)))
            {
                int count = disc.Count(g => g.rarity == r);
                stats.rarityDistribution.Add(new RarityCount { rarity = r, count = count });
            }

            return stats;
        }

        /// <summary>Returns continents ordered by completion percentage (descending).</summary>
        public List<ContinentCompletion> GetContinentLeaderboard()
            => GetStatistics().continentCompletions.OrderByDescending(c => c.percent).ToList();

        /// <summary>Returns the most recently discovered gems, up to <paramref name="count"/>.</summary>
        public List<HiddenGemDefinition> GetRecentDiscoveries(int count)
        {
            var mgr = HiddenGemManager.Instance;
            if (mgr == null) return new List<HiddenGemDefinition>();

            return mgr.GetDiscoveredGems()
                .Select(g => new { gem = g, state = mgr.GetGemState(g.gemId) })
                .Where(x => x.state != null && !string.IsNullOrEmpty(x.state.discoveredDate))
                .OrderByDescending(x => x.state.discoveredDate)
                .Take(count)
                .Select(x => x.gem)
                .ToList();
        }

        // ── Streak calculation ────────────────────────────────────────────────────

        private void UpdateStreak()
        {
            int current = ComputeCurrentStreak();
            if (current > _save.longestStreak)
                _save.longestStreak = current;
        }

        private int ComputeCurrentStreak()
        {
            if (_save.discoveryDates.Count == 0) return 0;

            // Parse all dates to unique UTC day strings
            var days = new HashSet<string>();
            foreach (var d in _save.discoveryDates)
            {
                if (DateTime.TryParse(d, out DateTime dt))
                    days.Add(dt.ToUniversalTime().ToString("yyyy-MM-dd"));
            }

            int streak = 0;
            var today  = DateTime.UtcNow.Date;
            for (int i = 0; i < 365; i++)
            {
                string key = today.AddDays(-i).ToString("yyyy-MM-dd");
                if (!days.Contains(key)) break;
                streak++;
            }
            return streak;
        }

        // ── Persistence ───────────────────────────────────────────────────────────

        private void Load()
        {
            if (!File.Exists(SavePath)) return;
            try
            {
                _save = JsonUtility.FromJson<TrackerSaveData>(File.ReadAllText(SavePath))
                        ?? new TrackerSaveData();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SWEF] GemStatisticsTracker: Load failed — {e.Message}");
            }
        }

        private void Save()
        {
            try   { File.WriteAllText(SavePath, JsonUtility.ToJson(_save, true)); }
            catch (Exception e) { Debug.LogWarning($"[SWEF] GemStatisticsTracker: Save failed — {e.Message}"); }
        }
    }
}
