using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SWEF.Achievement;
using SWEF.Flight;
using SWEF.GuidedTour;
using SWEF.Progression;
using SWEF.Recorder;
using SWEF.Screenshot;
using SWEF.Weather;

namespace SWEF.Journal
{
    /// <summary>
    /// Central singleton manager for the Flight Journal &amp; Logbook System.
    /// Automatically starts and finalises a <see cref="FlightLogEntry"/> whenever
    /// <see cref="FlightController.IsFlying"/> changes.  Persists the full journal
    /// to <c>Application.persistentDataPath/flight_journal.json</c>.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class JournalManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static JournalManager Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired after a new <see cref="FlightLogEntry"/> is committed to the journal.</summary>
        public event Action<FlightLogEntry> OnNewEntryAdded;

        /// <summary>Fired after an existing entry's notes, tags, or favourite state is changed.</summary>
        public event Action<FlightLogEntry> OnEntryUpdated;

        /// <summary>Fired after an entry is deleted from the journal.</summary>
        public event Action<string> OnEntryDeleted;

        // ── Internal state ────────────────────────────────────────────────────────
        private List<FlightLogEntry> _entries = new List<FlightLogEntry>();
        private FlightLogEntry _activeEntry;
        private bool _wasFlying;

        private static readonly string SaveFileName = "flight_journal.json";
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        // ── References ────────────────────────────────────────────────────────────
        private FlightController    _flight;
        private AltitudeController  _altitude;
        private FlightRecorder      _recorder;
        private AchievementManager  _achievements;
        private ProgressionManager  _progression;
        private WeatherManager      _weather;
        private TourManager         _tourManager;
        private ScreenshotController _screenshot;
        private JournalAutoRecorder  _autoRecorder;

        // ── Serialisation wrapper ─────────────────────────────────────────────────
        [Serializable]
        private class JournalSaveData
        {
            public List<FlightLogEntry> entries = new List<FlightLogEntry>();
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

            _flight       = FindFirstObjectByType<FlightController>();
            _altitude     = FindFirstObjectByType<AltitudeController>();
            _recorder     = FindFirstObjectByType<FlightRecorder>();
            _achievements = FindFirstObjectByType<AchievementManager>();
            _progression  = FindFirstObjectByType<ProgressionManager>();
            _weather      = FindFirstObjectByType<WeatherManager>();
            _tourManager  = FindFirstObjectByType<TourManager>();
            _screenshot   = FindFirstObjectByType<ScreenshotController>();
            _autoRecorder = GetComponent<JournalAutoRecorder>();
            if (_autoRecorder == null)
                _autoRecorder = gameObject.AddComponent<JournalAutoRecorder>();

            LoadJournal();
            SubscribeEvents();

            Debug.Log($"[SWEF] JournalManager: Initialised. {_entries.Count} entries loaded.");
        }

        private void Update()
        {
            bool flying = _flight != null && _flight.IsFlying;
            if (flying && !_wasFlying)
                BeginEntry();
            else if (!flying && _wasFlying)
                EndEntry();
            _wasFlying = flying;
        }

        private void OnApplicationPause(bool pause) { if (pause) SaveJournal(); }
        private void OnApplicationQuit()            { SaveJournal(); }

        // ── Event subscriptions ───────────────────────────────────────────────────
        private void SubscribeEvents()
        {
            if (_achievements != null)
                _achievements.OnAchievementUnlocked += HandleAchievementUnlocked;

            if (_screenshot != null)
                _screenshot.OnScreenshotCaptured += HandleScreenshotCaptured;

            if (_tourManager != null)
                _tourManager.OnTourCompleted += HandleTourCompleted;
        }

        private void UnsubscribeEvents()
        {
            if (_achievements != null)
                _achievements.OnAchievementUnlocked -= HandleAchievementUnlocked;

            if (_screenshot != null)
                _screenshot.OnScreenshotCaptured -= HandleScreenshotCaptured;

            if (_tourManager != null)
                _tourManager.OnTourCompleted -= HandleTourCompleted;
        }

        private void OnDestroy() => UnsubscribeEvents();

        // ── Flight lifecycle ──────────────────────────────────────────────────────
        private void BeginEntry()
        {
            _activeEntry = new FlightLogEntry
            {
                logId            = Guid.NewGuid().ToString(),
                flightDate       = DateTime.UtcNow.ToString("o"),
                weatherCondition = _weather != null ? _weather.CurrentWeather.description : string.Empty,
                pilotRankAtTime  = _progression != null ? (_progression.GetCurrentRank()?.rankName ?? string.Empty) : string.Empty,
                achievementsUnlocked = Array.Empty<string>(),
                screenshotPaths      = Array.Empty<string>(),
                tags                 = Array.Empty<string>(),
                altitudeProfile      = Array.Empty<float>(),
            };

            _autoRecorder?.BeginRecording(_activeEntry);
            Debug.Log($"[SWEF] JournalManager: Flight started — entry {_activeEntry.logId}");
        }

        private void EndEntry()
        {
            if (_activeEntry == null) return;

            _autoRecorder?.StopRecording(_activeEntry);

            // Discard spurious short flights.
            if (_activeEntry.durationSeconds < 10f)
            {
                Debug.Log("[SWEF] JournalManager: Flight < 10 s — entry discarded.");
                _activeEntry = null;
                return;
            }

            // Link replay if recorder was active (use entry logId as fallback replay reference).
            if (_recorder != null && _recorder.IsRecording)
                _activeEntry.replayFileId = _activeEntry.logId;

            // XP snapshot.
            if (_progression != null)
                _activeEntry.xpEarned = _autoRecorder?.XpEarnedDuringFlight ?? 0;

            _entries.Insert(0, _activeEntry);
            SaveJournal();
            OnNewEntryAdded?.Invoke(_activeEntry);

            Debug.Log($"[SWEF] JournalManager: Entry saved — {_activeEntry.logId} ({_activeEntry.durationSeconds:F0}s)");
            _activeEntry = null;
        }

        // ── Event handlers ────────────────────────────────────────────────────────
        private void HandleAchievementUnlocked(AchievementDefinition def)
        {
            if (_activeEntry == null || def == null) return;
            var list = new List<string>(_activeEntry.achievementsUnlocked) { def.id };
            _activeEntry.achievementsUnlocked = list.ToArray();
        }

        private void HandleScreenshotCaptured(string path)
        {
            if (_activeEntry == null || string.IsNullOrEmpty(path)) return;
            if (_activeEntry.screenshotPaths.Length >= 5) return;
            var list = new List<string>(_activeEntry.screenshotPaths) { path };
            _activeEntry.screenshotPaths = list.ToArray();
        }

        private void HandleTourCompleted(TourData tour)
        {
            if (_activeEntry == null || tour == null) return;
            _activeEntry.tourName = tour.tourName;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns all journal entries (newest first).</summary>
        public List<FlightLogEntry> GetAllEntries() => new List<FlightLogEntry>(_entries);

        /// <summary>Returns entries that match the given <paramref name="filter"/>.</summary>
        public List<FlightLogEntry> GetFilteredEntries(JournalFilter filter)
        {
            var engine = new JournalSearchEngine();
            return engine.ApplyFilter(new List<FlightLogEntry>(_entries), filter);
        }

        /// <summary>Returns the entry with the given <paramref name="logId"/>, or <c>null</c>.</summary>
        public FlightLogEntry GetEntry(string logId)
        {
            if (string.IsNullOrEmpty(logId)) return null;
            return _entries.Find(e => e.logId == logId);
        }

        /// <summary>Removes the entry with the given <paramref name="logId"/> from the journal.</summary>
        public void DeleteEntry(string logId)
        {
            int idx = _entries.FindIndex(e => e.logId == logId);
            if (idx < 0) return;
            _entries.RemoveAt(idx);
            SaveJournal();
            OnEntryDeleted?.Invoke(logId);
        }

        /// <summary>
        /// Updates the free-text notes on the specified entry.
        /// Notes are clamped to 500 characters.
        /// </summary>
        public void UpdateEntryNotes(string logId, string notes)
        {
            var entry = GetEntry(logId);
            if (entry == null) return;
            entry.notes = notes != null && notes.Length > 500 ? notes.Substring(0, 500) : notes;
            SaveJournal();
            OnEntryUpdated?.Invoke(entry);
        }

        /// <summary>Replaces the tags array on the specified entry.</summary>
        public void UpdateEntryTags(string logId, string[] tags)
        {
            var entry = GetEntry(logId);
            if (entry == null) return;
            entry.tags = tags ?? Array.Empty<string>();
            SaveJournal();
            OnEntryUpdated?.Invoke(entry);
        }

        /// <summary>Toggles the favourite flag on the specified entry.</summary>
        public void ToggleFavorite(string logId)
        {
            var entry = GetEntry(logId);
            if (entry == null) return;
            entry.isFavorite = !entry.isFavorite;
            SaveJournal();
            OnEntryUpdated?.Invoke(entry);
        }

        /// <summary>Computes and returns aggregate statistics from the entire journal.</summary>
        public JournalStatistics GetStatistics()
        {
            var stats = new JournalStatistics();
            stats.totalFlights = _entries.Count;
            if (_entries.Count == 0) return stats;

            var now     = DateTime.UtcNow;
            var weekAgo  = now.AddDays(-7);
            var monthAgo = now.AddDays(-30);

            var weatherCount  = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var locationCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var e in _entries)
            {
                stats.totalDistanceKm    += e.distanceKm;
                stats.totalDurationHours += e.durationSeconds / 3600f;

                if (e.maxAltitudeM    > stats.highestAltitudeEver) stats.highestAltitudeEver = e.maxAltitudeM;
                if (e.maxSpeedKmh     > stats.fastestSpeedEver)    stats.fastestSpeedEver    = e.maxSpeedKmh;
                if (e.durationSeconds > stats.longestFlightSeconds) stats.longestFlightSeconds = e.durationSeconds;

                stats.averageAltitude        += e.maxAltitudeM;
                stats.averageFlightDuration  += e.durationSeconds;

                if (!string.IsNullOrEmpty(e.weatherCondition))
                {
                    weatherCount.TryGetValue(e.weatherCondition, out int wc);
                    weatherCount[e.weatherCondition] = wc + 1;
                }

                foreach (var loc in new[] { e.departureLocation, e.arrivalLocation })
                {
                    if (!string.IsNullOrEmpty(loc))
                    {
                        locationCount.TryGetValue(loc, out int lc);
                        locationCount[loc] = lc + 1;
                    }
                }

                if (DateTime.TryParse(e.flightDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime dt))
                {
                    if (dt >= weekAgo)  stats.flightsThisWeek++;
                    if (dt >= monthAgo) stats.flightsThisMonth++;
                }
            }

            stats.averageAltitude       /= _entries.Count;
            stats.averageFlightDuration /= _entries.Count;

            // Favourite weather / location.
            int maxW = 0;
            foreach (var kv in weatherCount)
                if (kv.Value > maxW) { maxW = kv.Value; stats.favoriteWeather = kv.Key; }

            int maxL = 0;
            foreach (var kv in locationCount)
                if (kv.Value > maxL) { maxL = kv.Value; stats.mostVisitedLocation = kv.Key; }

            // Streak calculation.
            ComputeStreaks(stats);

            return stats;
        }

        /// <summary>
        /// Serialises the given entry as a formatted JSON string suitable for sharing.
        /// </summary>
        public string ExportEntry(string logId)
        {
            var entry = GetEntry(logId);
            if (entry == null) return string.Empty;
            return JsonUtility.ToJson(entry, true);
        }

        /// <summary>Returns the <paramref name="count"/> most recently added entries.</summary>
        public List<FlightLogEntry> GetRecentEntries(int count)
        {
            int take = Mathf.Min(count, _entries.Count);
            return _entries.GetRange(0, take);
        }

        // ── Streak helper ─────────────────────────────────────────────────────────
        private void ComputeStreaks(JournalStatistics stats)
        {
            var flightDays = new HashSet<string>();
            foreach (var e in _entries)
            {
                if (DateTime.TryParse(e.flightDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime dt))
                    flightDays.Add(dt.ToUniversalTime().Date.ToString("yyyy-MM-dd"));
            }

            var today    = DateTime.UtcNow.Date;
            int current  = 0;
            int longest  = 0;
            int run      = 0;
            var check    = today;

            // Current streak: count backwards from today.
            while (flightDays.Contains(check.ToString("yyyy-MM-dd")))
            {
                current++;
                check = check.AddDays(-1);
            }

            // Longest streak: iterate all known days in order.
            var sortedDays = new List<string>(flightDays);
            sortedDays.Sort();
            DateTime? prev = null;
            foreach (var d in sortedDays)
            {
                if (!DateTime.TryParse(d, out DateTime cur)) continue;
                if (prev.HasValue && (cur.Date - prev.Value.Date).Days == 1)
                    run++;
                else
                    run = 1;
                if (run > longest) longest = run;
                prev = cur;
            }

            stats.currentStreak = current;
            stats.longestStreak = longest;
        }

        // ── Persistence ───────────────────────────────────────────────────────────
        private void SaveJournal()
        {
            try
            {
                var data = new JournalSaveData { entries = _entries };
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] JournalManager: Save failed — {ex.Message}");
            }
        }

        private void LoadJournal()
        {
            if (!File.Exists(SavePath)) return;
            try
            {
                string json = File.ReadAllText(SavePath);
                var data = JsonUtility.FromJson<JournalSaveData>(json);
                if (data?.entries != null)
                    _entries = data.entries;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] JournalManager: Load failed — {ex.Message}");
            }
        }
    }
}
