using UnityEngine;
using SWEF.Flight;

namespace SWEF.Achievement
{
    /// <summary>
    /// Tracks and awards achievements based on flight milestones.
    /// Persists unlocked achievements in PlayerPrefs.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static AchievementManager Instance { get; private set; }

        // ── Achievement definition ────────────────────────────────────────────────
        /// <summary>Metadata for a single achievement.</summary>
        [System.Serializable]
        public struct AchievementDef
        {
            public string id;
            public string title;
            public string description;
            public string emoji;
        }

        /// <summary>All achievable milestones in the game.</summary>
        public static readonly AchievementDef[] Definitions = new AchievementDef[]
        {
            new AchievementDef { id = "first_flight",     title = "First Flight ✈️",        description = "Complete your first flight",              emoji = "✈️" },
            new AchievementDef { id = "reach_10km",       title = "Sky High 🌤️",            description = "Reach 10,000 meters altitude",            emoji = "🌤️" },
            new AchievementDef { id = "reach_karman",     title = "Edge of Space 🌍",        description = "Cross the Kármán line at 100km",          emoji = "🌍" },
            new AchievementDef { id = "reach_120km",      title = "Space Pioneer 🚀",        description = "Reach 120,000 meters",                    emoji = "🚀" },
            new AchievementDef { id = "mach1",            title = "Sound Barrier 💥",        description = "Break the sound barrier (Mach 1)",        emoji = "💥" },
            new AchievementDef { id = "orbital_speed",    title = "Orbital Velocity ⚡",     description = "Reach orbital speed (7,900 m/s)",         emoji = "⚡" },
            new AchievementDef { id = "first_teleport",   title = "World Traveler 🗺️",      description = "Teleport to a new location",              emoji = "🗺️" },
            new AchievementDef { id = "first_screenshot", title = "Photographer 📸",         description = "Take your first screenshot",              emoji = "📸" },
            // Phase 17 — Replay achievements
            new AchievementDef { id = "first_ghost_race", title = "Ghost Hunter 👻",         description = "Complete your first ghost race",          emoji = "👻" },
            new AchievementDef { id = "replay_shared",    title = "Flight Broadcaster 📡",   description = "Share a replay with another player",      emoji = "📡" },
        };

        // ── Inspector refs ───────────────────────────────────────────────────────
        [Header("Refs")]
        [SerializeField] private AltitudeController altitudeSource;
        [SerializeField] private FlightController flight;

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Fired when a new achievement is unlocked.</summary>
        public event System.Action<AchievementDef> OnAchievementUnlocked;

        // ── State ────────────────────────────────────────────────────────────────
        private bool _firstFrameDone;

        /// <summary>Number of achievements the player has unlocked.</summary>
        public int UnlockedCount
        {
            get
            {
                int count = 0;
                foreach (var def in Definitions)
                    if (IsUnlocked(def.id)) count++;
                return count;
            }
        }

        /// <summary>Total number of achievements.</summary>
        public int TotalCount => Definitions.Length;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (altitudeSource == null)
                altitudeSource = FindFirstObjectByType<AltitudeController>();
            if (flight == null)
                flight = FindFirstObjectByType<FlightController>();
        }

        private void Update()
        {
            // first_flight: unlock on first Update (if scene is loaded, player is flying)
            if (!_firstFrameDone)
            {
                _firstFrameDone = true;
                TryUnlock("first_flight");
            }

            float alt   = altitudeSource != null ? altitudeSource.CurrentAltitudeMeters : 0f;
            float speed = flight          != null ? flight.CurrentSpeedMps               : 0f;

            if (alt   >= 10000f)  TryUnlock("reach_10km");
            if (alt   >= 100000f) TryUnlock("reach_karman");
            if (alt   >= 120000f) TryUnlock("reach_120km");
            if (speed >= 343f)    TryUnlock("mach1");
            if (speed >= 7900f)   TryUnlock("orbital_speed");
        }

        // ── Public API ────────────────────────────────────────────────────────────
        /// <summary>Returns true if the achievement with the given id has been unlocked.</summary>
        public bool IsUnlocked(string id) =>
            PlayerPrefs.GetInt($"SWEF_ACH_{id}", 0) == 1;

        /// <summary>
        /// Attempts to unlock the achievement. Returns true if it was newly unlocked,
        /// false if it was already unlocked or the id is not found.
        /// </summary>
        public bool TryUnlock(string id)
        {
            if (IsUnlocked(id)) return false;

            // Find definition
            AchievementDef? found = null;
            foreach (var def in Definitions)
            {
                if (def.id == id)
                {
                    found = def;
                    break;
                }
            }

            if (found == null)
            {
                Debug.LogWarning($"[SWEF] AchievementManager: Unknown achievement id '{id}'.");
                return false;
            }

            PlayerPrefs.SetInt($"SWEF_ACH_{id}", 1);
            PlayerPrefs.Save();

            Debug.Log($"[SWEF] Achievement unlocked: {found.Value.title}");
            OnAchievementUnlocked?.Invoke(found.Value);
            return true;
        }
    }
}
