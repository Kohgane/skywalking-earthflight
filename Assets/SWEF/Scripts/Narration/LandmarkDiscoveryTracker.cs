using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SWEF.Achievement;
using SWEF.Journal;

namespace SWEF.Narration
{
    /// <summary>
    /// Tracks which landmarks the player has heard narrations for, persisting
    /// discovery state to JSON.  Integrates with <see cref="AchievementManager"/>
    /// for milestones and <see cref="JournalManager"/> for flight logbook entries.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class LandmarkDiscoveryTracker : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static LandmarkDiscoveryTracker Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired the first time a landmark narration is heard.</summary>
        public event Action<string> OnFirstDiscovery;

        /// <summary>Fired whenever the discovery count changes.</summary>
        public event Action<int> OnDiscoveryCountChanged;

        // ── State ─────────────────────────────────────────────────────────────────
        private readonly Dictionary<string, LandmarkDiscoveryState> _states =
            new Dictionary<string, LandmarkDiscoveryState>();

        // ── Persistence ───────────────────────────────────────────────────────────
        private static readonly string SaveFile = "landmark_discoveries.json";
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFile);

        // ── References ────────────────────────────────────────────────────────────
        private AchievementManager _achievements;
        private JournalManager     _journal;

        // ── Serialization ─────────────────────────────────────────────────────────
        [Serializable]
        private class SaveData
        {
            public List<LandmarkDiscoveryState> states = new List<LandmarkDiscoveryState>();
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
            LoadStates();
        }

        private void Start()
        {
            _achievements = AchievementManager.Instance;
            _journal      = JournalManager.Instance;
        }

        private void OnApplicationPause(bool paused) { if (paused) SaveStates(); }
        private void OnApplicationQuit()             { SaveStates(); }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Record a narration visit for the given landmark ID.</summary>
        public void RecordVisit(string landmarkId)
        {
            if (string.IsNullOrEmpty(landmarkId)) return;

            bool isFirst = !_states.ContainsKey(landmarkId);

            if (!_states.TryGetValue(landmarkId, out var state))
            {
                state = new LandmarkDiscoveryState { landmarkId = landmarkId };
                _states[landmarkId] = state;
            }

            state.visitCount++;
            state.lastVisitedUtc = DateTime.UtcNow.ToString("o");
            if (isFirst)
                state.firstDiscoveredUtc = state.lastVisitedUtc;

            if (isFirst)
            {
                OnFirstDiscovery?.Invoke(landmarkId);
                OnDiscoveryCountChanged?.Invoke(DiscoveredCount);
                ReportAchievementMilestones();
            }

            SaveStates();
        }

        /// <summary>Returns true if the player has heard narration for this landmark.</summary>
        public bool IsDiscovered(string landmarkId) => _states.ContainsKey(landmarkId);

        /// <summary>Total number of unique landmarks discovered.</summary>
        public int DiscoveredCount => _states.Count;

        /// <summary>Number of times the player has visited a specific landmark.</summary>
        public int GetVisitCount(string landmarkId) =>
            _states.TryGetValue(landmarkId, out var s) ? s.visitCount : 0;

        /// <summary>Returns all current discovery states (read-only snapshot).</summary>
        public IReadOnlyDictionary<string, LandmarkDiscoveryState> AllStates => _states;

        // ── Achievement milestones ────────────────────────────────────────────────

        private void ReportAchievementMilestones()
        {
            if (_achievements == null) return;
            int count = DiscoveredCount;
            _achievements.ReportProgress("narration_first_landmark",   count);
            _achievements.ReportProgress("narration_10_landmarks",     count);
            _achievements.ReportProgress("narration_25_landmarks",     count);
            _achievements.ReportProgress("narration_all_landmarks",    count);
        }

        // ── Persistence ───────────────────────────────────────────────────────────

        private void LoadStates()
        {
            if (!File.Exists(SavePath)) return;
            try
            {
                var save = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                if (save?.states == null) return;
                foreach (var s in save.states)
                    if (!string.IsNullOrEmpty(s.landmarkId))
                        _states[s.landmarkId] = s;

                Debug.Log($"[SWEF] LandmarkDiscoveryTracker: {_states.Count} discoveries loaded.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SWEF] LandmarkDiscoveryTracker: load failed — {e.Message}");
            }
        }

        private void SaveStates()
        {
            try
            {
                var save = new SaveData { states = new List<LandmarkDiscoveryState>(_states.Values) };
                File.WriteAllText(SavePath, JsonUtility.ToJson(save, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SWEF] LandmarkDiscoveryTracker: save failed — {e.Message}");
            }
        }
    }

    // ── LandmarkDiscoveryState ────────────────────────────────────────────────────

    /// <summary>Persistent state for a single landmark discovery record.</summary>
    [Serializable]
    public class LandmarkDiscoveryState
    {
        public string landmarkId;
        public int    visitCount;
        public string firstDiscoveredUtc;
        public string lastVisitedUtc;
    }
}
