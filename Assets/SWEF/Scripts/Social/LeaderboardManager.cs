using System.Collections.Generic;
using UnityEngine;
using SWEF.Flight;

namespace SWEF.Social
{
    /// <summary>
    /// A single recorded flight session used for leaderboard display.
    /// </summary>
    [System.Serializable]
    public class LeaderboardEntry
    {
        public string date;
        public float maxAltitude;
        public float duration;
        public float maxSpeed;
        public int teleportCount;
        public float score;
    }

    /// <summary>
    /// Container for all leaderboard entries, designed for JSON serialisation.
    /// </summary>
    [System.Serializable]
    public class LeaderboardData
    {
        public List<LeaderboardEntry> entries = new();
    }

    /// <summary>
    /// Local personal-records leaderboard. Tracks the current session's max altitude,
    /// max speed, and teleport count in real time, then persists the top
    /// <see cref="maxEntries"/> sessions to <see cref="PlayerPrefs"/> as JSON.
    /// No server required — purely local personal bests.
    /// </summary>
    public class LeaderboardManager : MonoBehaviour
    {
        private const string PrefsKey = "SWEF_Leaderboard";

        [Header("Configuration")]
        [SerializeField] private int maxEntries = 10;

        [Header("Sources")]
        [SerializeField] private AltitudeController altitudeSource;
        [SerializeField] private FlightController flightSource;

        private LeaderboardData _data;
        private float _sessionStartTime;
        private float _sessionMaxAltitude;
        private float _sessionMaxSpeed;
        private int _sessionTeleports;

        private void Awake()
        {
            if (altitudeSource == null)
                altitudeSource = FindFirstObjectByType<AltitudeController>();

            if (flightSource == null)
                flightSource = FindFirstObjectByType<FlightController>();

            LoadData();
        }

        private void Start()
        {
            _sessionStartTime = Time.time;
        }

        private void Update()
        {
            if (altitudeSource != null)
            {
                float alt = altitudeSource.CurrentAltitudeMeters;
                if (alt > _sessionMaxAltitude)
                    _sessionMaxAltitude = alt;
            }

            if (flightSource != null)
            {
                float speed = flightSource.CurrentSpeedMps;
                if (speed > _sessionMaxSpeed)
                    _sessionMaxSpeed = speed;
            }
        }

        /// <summary>Increments the teleport counter for the current session.</summary>
        public void RecordSessionTeleport()
        {
            _sessionTeleports++;
        }

        /// <summary>
        /// Creates a <see cref="LeaderboardEntry"/> from the current session statistics,
        /// appends it to the leaderboard, trims to <see cref="maxEntries"/>, and
        /// persists to <see cref="PlayerPrefs"/>.
        /// </summary>
        public void SubmitSession()
        {
            float duration = Time.time - _sessionStartTime;

            float score = _sessionMaxAltitude * 0.5f
                        + duration * 0.3f
                        + _sessionMaxSpeed * 0.1f
                        + _sessionTeleports * 10f;

            var entry = new LeaderboardEntry
            {
                date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                maxAltitude = _sessionMaxAltitude,
                duration = duration,
                maxSpeed = _sessionMaxSpeed,
                teleportCount = _sessionTeleports,
                score = score
            };

            _data.entries.Add(entry);
            _data.entries.Sort((a, b) => b.score.CompareTo(a.score));

            if (_data.entries.Count > maxEntries)
                _data.entries.RemoveRange(maxEntries, _data.entries.Count - maxEntries);

            SaveData();
            Debug.Log($"[SWEF] LeaderboardManager: session submitted — score {score:N0}");
        }

        /// <summary>Returns the full leaderboard data.</summary>
        public LeaderboardData GetLeaderboard() => _data;

        /// <summary>Returns the top-ranked entry, or <c>null</c> if the board is empty.</summary>
        public LeaderboardEntry GetTopEntry() =>
            _data.entries.Count > 0 ? _data.entries[0] : null;

        /// <summary>Deletes all entries from the leaderboard and clears persistent storage.</summary>
        public void ClearLeaderboard()
        {
            _data = new LeaderboardData();
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
            Debug.Log("[SWEF] LeaderboardManager: leaderboard cleared.");
        }

        // Auto-submit when the app is paused or closed (if the session is long enough).
        private void OnApplicationPause(bool paused)
        {
            if (paused && Time.time - _sessionStartTime > 10f)
                SubmitSession();
        }

        private void OnApplicationQuit()
        {
            if (Time.time - _sessionStartTime > 10f)
                SubmitSession();
        }

        // ── Persistence ──────────────────────────────────────────────────────────

        private void LoadData()
        {
            string json = PlayerPrefs.GetString(PrefsKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                _data = JsonUtility.FromJson<LeaderboardData>(json);
                if (_data == null)
                    _data = new LeaderboardData();
            }
            else
            {
                _data = new LeaderboardData();
            }
        }

        private void SaveData()
        {
            string json = JsonUtility.ToJson(_data);
            PlayerPrefs.SetString(PrefsKey, json);
            PlayerPrefs.Save();
        }
    }
}
