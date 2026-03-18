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
            // Phase 18 — Cinema achievements
            new AchievementDef { id = "first_photo",             title = "Photographer Pro 📷",       description = "Capture your first photo in Photo Mode",         emoji = "📷" },
            new AchievementDef { id = "golden_hour_photo",       title = "Golden Hour ✨",             description = "Capture a photo during golden hour",             emoji = "✨" },
            new AchievementDef { id = "cinematic_path_created",  title = "Film Director 🎬",           description = "Create and save your first cinematic camera path", emoji = "🎬" },
            new AchievementDef { id = "night_flight",            title = "Night Owl 🦉",               description = "Fly for 5+ minutes during night time",           emoji = "🦉" },
            // Phase 19 — Weather achievements
            new AchievementDef { id = "storm_chaser",            title = "Storm Chaser ⛈️",            description = "Fly through 10 thunderstorms",                   emoji = "⛈️" },
            new AchievementDef { id = "snowbird",                title = "Snowbird ❄️",                description = "Fly in snow conditions",                         emoji = "❄️" },
            new AchievementDef { id = "clear_skies",             title = "Clear Skies ☀️",             description = "Complete a full flight in perfect clear weather", emoji = "☀️" },
            // Phase 20 — Multiplayer achievements
            new AchievementDef { id = "first_multiplayer_flight", title = "Social Flyer 🌐",           description = "Join a multiplayer room for the first time",      emoji = "🌐" },
            new AchievementDef { id = "social_butterfly",         title = "Social Butterfly 🦋",       description = "Fly with 5+ different players across sessions",   emoji = "🦋" },
            new AchievementDef { id = "race_winner",              title = "Race Winner 🏆",             description = "Win a multiplayer altitude race",                 emoji = "🏆" },
            new AchievementDef { id = "race_participant_10",      title = "Veteran Racer 🎽",           description = "Participate in 10 multiplayer races",             emoji = "🎽" },
            new AchievementDef { id = "ping_master",              title = "Ping Master 📡",             description = "Send 50 pings to other players",                  emoji = "📡" },
        };

        // ── Inspector refs ───────────────────────────────────────────────────────
        [Header("Refs")]
        [SerializeField] private AltitudeController altitudeSource;
        [SerializeField] private FlightController flight;

        [Header("Phase 18 — Cinema")]
        [SerializeField] private SWEF.Cinema.TimeOfDayController timeOfDayController;

        [Header("Phase 19 — Weather")]
        [Tooltip("WeatherStateManager reference (auto-resolved if null).")]
        [SerializeField] private SWEF.Weather.WeatherStateManager weatherStateManager;

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Fired when a new achievement is unlocked.</summary>
        public event System.Action<AchievementDef> OnAchievementUnlocked;

        // ── State ────────────────────────────────────────────────────────────────
        private bool  _firstFrameDone;
        private float _nightFlightSeconds;
        private int   _thunderstormCount;
        private bool  _inThunderstorm;
        private bool  _clearFlightActive;

        // Phase 20 — Multiplayer counters (persisted via PlayerPrefs)
        private const string KEY_UNIQUE_PLAYERS = "SWEF_MP_UniquePlayers";
        private const string KEY_RACE_COUNT     = "SWEF_MP_RaceCount";
        private const string KEY_PING_COUNT     = "SWEF_MP_PingCount";
        private int  _raceCount;
        private int  _pingCount;

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
            if (timeOfDayController == null)
                timeOfDayController = FindFirstObjectByType<SWEF.Cinema.TimeOfDayController>();
            if (weatherStateManager == null)
                weatherStateManager = SWEF.Weather.WeatherStateManager.Instance != null
                    ? SWEF.Weather.WeatherStateManager.Instance
                    : FindFirstObjectByType<SWEF.Weather.WeatherStateManager>();

            // Phase 20 — Load persisted multiplayer counters
            _raceCount = PlayerPrefs.GetInt(KEY_RACE_COUNT, 0);
            _pingCount  = PlayerPrefs.GetInt(KEY_PING_COUNT, 0);

            // Subscribe to multiplayer events
            var roomManager = SWEF.Multiplayer.RoomManager.Instance != null
                ? SWEF.Multiplayer.RoomManager.Instance
                : FindFirstObjectByType<SWEF.Multiplayer.RoomManager>();
            if (roomManager != null)
                roomManager.OnRoomJoined += _ => NotifyMultiplayerRoomJoined();

            var race = SWEF.Multiplayer.MultiplayerRace.Instance != null
                ? SWEF.Multiplayer.MultiplayerRace.Instance
                : FindFirstObjectByType<SWEF.Multiplayer.MultiplayerRace>();
            if (race != null)
            {
                race.OnRaceStateChanged += state =>
                {
                    if (state == SWEF.Multiplayer.RaceState.Racing)
                        NotifyRaceStarted();
                };
                race.OnRaceFinished += results =>
                {
                    if (results != null && results.Count > 0)
                    {
                        var localId = FindFirstObjectByType<SWEF.Multiplayer.MultiplayerManager>()?.LocalPlayerId ?? "";
                        if (!string.IsNullOrEmpty(localId) && results[0].playerId == localId)
                            TryUnlock("race_winner");
                    }
                };
            }

            var chat = SWEF.Multiplayer.ProximityChat.Instance != null
                ? SWEF.Multiplayer.ProximityChat.Instance
                : FindFirstObjectByType<SWEF.Multiplayer.ProximityChat>();
            if (chat != null)
                chat.OnPingReceived += (_, __) => NotifyPingSent();
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

            // Phase 18 — night_flight: accumulate time flying during night
            if (timeOfDayController != null && timeOfDayController.IsNight && speed > 0f)
            {
                _nightFlightSeconds += Time.deltaTime;
                if (_nightFlightSeconds >= 300f) // 5 minutes
                    TryUnlock("night_flight");
            }

            // Phase 19 — Weather achievements
            if (weatherStateManager != null && speed > 0f)
            {
                var cond = weatherStateManager.ActiveWeather?.condition ?? SWEF.Weather.WeatherCondition.Clear;

                // Storm Chaser: fly through thunderstorms
                if (cond == SWEF.Weather.WeatherCondition.Thunderstorm)
                {
                    if (!_inThunderstorm)
                    {
                        _inThunderstorm = true;
                        _thunderstormCount++;
                        if (_thunderstormCount >= 10)
                            TryUnlock("storm_chaser");
                    }
                }
                else
                {
                    _inThunderstorm = false;
                }

                // Snowbird: fly in any snow condition
                if (cond == SWEF.Weather.WeatherCondition.Snow ||
                    cond == SWEF.Weather.WeatherCondition.HeavySnow)
                    TryUnlock("snowbird");

                // Clear Skies: start tracking a clear flight
                if (cond == SWEF.Weather.WeatherCondition.Clear)
                    _clearFlightActive = true;
                else
                    _clearFlightActive = false;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────
        /// <summary>
        /// Call when a flight session ends in clear weather to award the "Clear Skies" achievement.
        /// Invoked by <see cref="SWEF.Core.FlightJournal"/> on session end.
        /// </summary>
        public void NotifyFlightEndedInClearSkies()
        {
            if (_clearFlightActive)
                TryUnlock("clear_skies");
        }

        /// <summary>Returns true if the achievement with the given id has been unlocked.</summary>
        public bool IsUnlocked(string id) =>
            PlayerPrefs.GetInt($"SWEF_ACH_{id}", 0) == 1;

        // ── Phase 20 — Multiplayer Achievement Notifiers ──────────────────────────

        /// <summary>
        /// Call when the local player joins a multiplayer room for the first time.
        /// Awards <c>first_multiplayer_flight</c>.
        /// </summary>
        public void NotifyMultiplayerRoomJoined()
        {
            TryUnlock("first_multiplayer_flight");

            // Track unique player sessions for social_butterfly
            int sessions = PlayerPrefs.GetInt(KEY_UNIQUE_PLAYERS, 0) + 1;
            PlayerPrefs.SetInt(KEY_UNIQUE_PLAYERS, sessions);
            PlayerPrefs.Save();
            if (sessions >= 5)
                TryUnlock("social_butterfly");
        }

        /// <summary>
        /// Call when the local player participates in a multiplayer race start.
        /// Awards <c>race_participant_10</c> after 10 races.
        /// </summary>
        public void NotifyRaceStarted()
        {
            _raceCount++;
            PlayerPrefs.SetInt(KEY_RACE_COUNT, _raceCount);
            PlayerPrefs.Save();
            if (_raceCount >= 10)
                TryUnlock("race_participant_10");
        }

        /// <summary>
        /// Call when the local player sends a ping to another player.
        /// Awards <c>ping_master</c> after 50 pings.
        /// </summary>
        public void NotifyPingSent()
        {
            _pingCount++;
            PlayerPrefs.SetInt(KEY_PING_COUNT, _pingCount);
            PlayerPrefs.Save();
            if (_pingCount >= 50)
                TryUnlock("ping_master");
        }



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
