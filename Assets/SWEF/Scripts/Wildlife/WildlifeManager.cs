using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Wildlife
{
    /// <summary>
    /// Phase 53 — Central singleton that manages all wildlife spawning, lifecycle,
    /// and cross-system integration for Skywalking Earthflight.
    ///
    /// <para>Attach to a persistent GameObject in the bootstrap scene.
    /// The manager streams animal groups in and out of the world based on player
    /// proximity, queries TerrainBiomeMapper for biome data, and reacts to time-of-day
    /// and weather conditions.</para>
    ///
    /// <para>Integration points:
    /// <list type="bullet">
    ///   <item><c>SWEF.Terrain.TerrainBiomeMapper</c> — biome-based fauna selection.</item>
    ///   <item><c>SWEF.Ocean.OceanManager</c> — marine life placement.</item>
    ///   <item><c>SWEF.Weather.WeatherManager</c> — storm shelter / post-rain activity.</item>
    ///   <item><c>SWEF.TimeOfDay.TimeOfDayManager</c> — nocturnal vs diurnal animals.</item>
    ///   <item><c>SWEF.Flight.FlightController</c> — altitude-aware fauna visibility.</item>
    ///   <item><c>SWEF.Achievement.AchievementManager</c> — species-discovery achievements.</item>
    ///   <item><c>SWEF.Journal.JournalManager</c> — encounter journal entries.</item>
    ///   <item><see cref="WildlifeSettings"/> — all runtime tuning lives here.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class WildlifeManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static WildlifeManager Instance { get; private set; }

        #endregion

        #region Constants

        private const float StreamingCheckInterval   = 3f;    // seconds between streaming ticks; balances responsiveness vs. CPU cost
        private const float EncounterProximityRadius = 200f;  // distance for encounter logging
        private const int   ChunkSize                = 1000;  // world units per wildlife chunk

        #endregion

        #region Inspector

        [Header("Settings")]
        [SerializeField] private WildlifeSettings settings = new WildlifeSettings();

        [Header("Species Catalogue")]
        [Tooltip("All animal species available for spawning.")]
        [SerializeField] private List<AnimalSpecies> speciesCatalogue = new List<AnimalSpecies>();

        [Tooltip("Active migration routes.")]
        [SerializeField] private List<MigrationRoute> migrationRoutes = new List<MigrationRoute>();

        [Header("References")]
        [Tooltip("Transform used as the player/camera origin for streaming. Resolved at runtime if null.")]
        [SerializeField] private Transform playerTransform;

        [Tooltip("Spawn system component. Resolved at runtime if null.")]
        [SerializeField] private WildlifeSpawnSystem spawnSystem;

        [Tooltip("Audio controller for wildlife sounds. Resolved at runtime if null.")]
        [SerializeField] private WildlifeAudioController audioController;

        #endregion

        #region Events

        /// <summary>Fired after a new animal group is added to the world.</summary>
        public event Action<AnimalGroup> OnAnimalSpawned;

        /// <summary>Fired after an animal group is removed from the world.</summary>
        public event Action<AnimalGroup> OnAnimalDespawned;

        /// <summary>Fired when the player gets close enough to log an encounter.</summary>
        public event Action<WildlifeEncounter> OnWildlifeEncounter;

        /// <summary>Fired the first time the player encounters a Rare or Legendary species.</summary>
        public event Action<AnimalSpecies> OnRareAnimalFound;

        #endregion

        #region Public Properties

        /// <summary>Read-only view of all currently active animal groups.</summary>
        public IReadOnlyList<AnimalGroup> ActiveGroups => _activeGroups;

        /// <summary>Exposes the runtime settings object.</summary>
        public WildlifeSettings Settings => settings;

        /// <summary>Total number of individual animals currently visible.</summary>
        public int TotalVisibleAnimals => _totalVisibleAnimals;

        #endregion

        #region Private State

        private readonly List<AnimalGroup>  _activeGroups        = new List<AnimalGroup>();
        private readonly HashSet<string>    _discoveredSpecies   = new HashSet<string>();
        private readonly List<WildlifeEncounter> _encounterLog   = new List<WildlifeEncounter>();
        private readonly Dictionary<Vector2Int, float> _chunkCooldowns = new Dictionary<Vector2Int, float>();

        private int   _totalVisibleAnimals;
        private float _streamingTimer;

        // Cached component references
        private Component _biomeMapper;
        private Component _oceanManager;
        private Component _weatherManager;
        private Component _timeOfDayManager;
        private Component _flightController;
        private Component _achievementManager;
        private Component _journalManager;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            ResolveReferences();
        }

        private void Start()
        {
            StartCoroutine(StreamingRoutine());
        }

        private void Update()
        {
            CheckProximityEncounters();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Returns all active groups whose center is within <paramref name="radius"/> of
        /// <paramref name="pos"/>.
        /// </summary>
        public List<AnimalGroup> GetNearbyAnimals(Vector3 pos, float radius)
        {
            var result = new List<AnimalGroup>();
            float sqr = radius * radius;
            foreach (var group in _activeGroups)
            {
                if ((group.centerPosition - pos).sqrMagnitude <= sqr)
                    result.Add(group);
            }
            return result;
        }

        /// <summary>
        /// Returns all species in the catalogue that list <paramref name="biome"/> as a habitat.
        /// </summary>
        public List<AnimalSpecies> GetSpeciesInBiome(BiomeHabitat biome)
        {
            var result = new List<AnimalSpecies>();
            foreach (var s in speciesCatalogue)
            {
                if (s.habitats.Contains(biome))
                    result.Add(s);
            }
            return result;
        }

        /// <summary>Returns the full encounter log.</summary>
        public IReadOnlyList<WildlifeEncounter> GetEncounterLog() => _encounterLog;

        /// <summary>Returns the set of all species names the player has discovered.</summary>
        public IReadOnlyCollection<string> DiscoveredSpecies => _discoveredSpecies;

        /// <summary>Registers an externally created animal group with the manager.</summary>
        public void RegisterGroup(AnimalGroup group)
        {
            if (group == null) return;
            _activeGroups.Add(group);
            _totalVisibleAnimals += group.memberCount;
            OnAnimalSpawned?.Invoke(group);
        }

        /// <summary>Removes an animal group and fires the despawn event.</summary>
        public void UnregisterGroup(AnimalGroup group)
        {
            if (group == null) return;
            if (_activeGroups.Remove(group))
            {
                _totalVisibleAnimals = Mathf.Max(0, _totalVisibleAnimals - group.memberCount);
                OnAnimalDespawned?.Invoke(group);
            }
        }

        #endregion

        #region Streaming

        private IEnumerator StreamingRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(StreamingCheckInterval);
                if (playerTransform == null) continue;

                DespawnDistantGroups();
                SpawnNearbyGroups();
            }
        }

        private void SpawnNearbyGroups()
        {
            if (spawnSystem == null) return;
            if (_totalVisibleAnimals >= settings.maxAnimalsVisible) return;

            Vector3 origin = playerTransform.position;
            float   alt    = GetPlayerAltitude();

            // Determine current biome for land spawning
            BiomeHabitat currentBiome = SampleBiome(origin);
            List<AnimalSpecies> candidates = GetFilteredCandidates(currentBiome, alt);

            foreach (var species in candidates)
            {
                if (_totalVisibleAnimals >= settings.maxAnimalsVisible) break;

                Vector2Int chunk = WorldToChunk(origin);
                if (IsChunkOnCooldown(chunk)) continue;

                spawnSystem.RequestSpawn(species, origin, settings.spawnRadius);
                SetChunkCooldown(chunk);
            }
        }

        private void DespawnDistantGroups()
        {
            if (playerTransform == null) return;
            Vector3 origin = playerTransform.position;
            float sqrDespawn = settings.despawnRadius * settings.despawnRadius;

            for (int i = _activeGroups.Count - 1; i >= 0; i--)
            {
                var group = _activeGroups[i];
                if ((group.centerPosition - origin).sqrMagnitude > sqrDespawn)
                {
                    UnregisterGroup(group);
                }
            }
        }

        #endregion

        #region Encounter Detection

        private void CheckProximityEncounters()
        {
            if (!settings.encounterLogEnabled) return;
            if (playerTransform == null) return;

            Vector3 playerPos = playerTransform.position;
            float   sqrDist   = EncounterProximityRadius * EncounterProximityRadius;

            foreach (var group in _activeGroups)
            {
                if ((group.centerPosition - playerPos).sqrMagnitude > sqrDist) continue;

                string name = group.species != null ? group.species.speciesName : "Unknown";
                bool firstTime = _discoveredSpecies.Add(name);

                var encounter = new WildlifeEncounter
                {
                    speciesName      = name,
                    position         = group.centerPosition,
                    timestamp        = Time.time,
                    wasPhotographed  = false,
                    distanceFromPlayer = Vector3.Distance(playerPos, group.centerPosition)
                };
                _encounterLog.Add(encounter);
                OnWildlifeEncounter?.Invoke(encounter);

                if (firstTime)
                {
                    NotifyAchievement(name);
                    NotifyJournal(encounter);

                    if (group.species != null &&
                        (group.species.rarity == AnimalRarity.Rare ||
                         group.species.rarity == AnimalRarity.Legendary))
                    {
                        OnRareAnimalFound?.Invoke(group.species);
                    }
                }
            }
        }

        #endregion

        #region Biome & Altitude Helpers

        private BiomeHabitat SampleBiome(Vector3 worldPos)
        {
#if SWEF_TERRAIN_AVAILABLE
            var mapper = _biomeMapper as SWEF.Terrain.TerrainBiomeMapper;
            if (mapper != null)
                return (BiomeHabitat)(int)mapper.GetBiomeAt(worldPos);
#endif
            return BiomeHabitat.Grassland;
        }

        private float GetPlayerAltitude()
        {
#if SWEF_FLIGHT_AVAILABLE
            var fc = _flightController as SWEF.Flight.FlightController;
            if (fc != null) return fc.Altitude;
#endif
            return playerTransform != null ? playerTransform.position.y : 0f;
        }

        private List<AnimalSpecies> GetFilteredCandidates(BiomeHabitat biome, float altitude)
        {
            bool isNight = IsNightTime();
            var  result  = new List<AnimalSpecies>();

            foreach (var s in speciesCatalogue)
            {
                if (!s.habitats.Contains(biome)) continue;
                if (altitude < s.minAltitude || altitude > s.maxAltitude) continue;

                bool correctTime =
                    s.activityPattern == TimeActivity.AllDay ||
                    (isNight  && s.activityPattern == TimeActivity.Nocturnal) ||
                    (!isNight && s.activityPattern == TimeActivity.Diurnal) ||
                    s.activityPattern == TimeActivity.Crepuscular;

                if (!correctTime) continue;

                // Category filters
                if (s.kingdom == AnimalKingdom.Bird   && !settings.enableBirds)       continue;
                if (s.kingdom == AnimalKingdom.Insect && !settings.enableInsects)     continue;
                if (s.kingdom == AnimalKingdom.Fish   && !settings.enableMarineLife)  continue;
                if (!s.flightCapable && !s.swimCapable && !settings.enableLandAnimals) continue;

                result.Add(s);
            }
            return result;
        }

        private bool IsNightTime()
        {
#if SWEF_TIMEOFDAY_AVAILABLE
            var todm = _timeOfDayManager as SWEF.TimeOfDay.TimeOfDayManager;
            if (todm != null) return todm.IsNight;
#endif
            float hour = (Time.time / 3600f) % 24f;
            return hour < 6f || hour > 20f;
        }

        #endregion

        #region Chunk Utilities

        private static Vector2Int WorldToChunk(Vector3 pos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(pos.x / ChunkSize),
                Mathf.FloorToInt(pos.z / ChunkSize));
        }

        private bool IsChunkOnCooldown(Vector2Int chunk)
        {
            return _chunkCooldowns.TryGetValue(chunk, out float expiry) && Time.time < expiry;
        }

        private void SetChunkCooldown(Vector2Int chunk, float duration = 10f)
        {
            _chunkCooldowns[chunk] = Time.time + duration;
        }

        #endregion

        #region Cross-System Notifications

        private void NotifyAchievement(string speciesName)
        {
#if SWEF_ACHIEVEMENT_AVAILABLE
            var am = _achievementManager as SWEF.Achievement.AchievementManager;
            am?.RecordProgress("wildlife_species_discovered", 1);
#endif
        }

        private void NotifyJournal(WildlifeEncounter encounter)
        {
#if SWEF_JOURNAL_AVAILABLE
            var jm = _journalManager as SWEF.Journal.JournalManager;
            jm?.AddAutoEntry($"Spotted {encounter.speciesName} at altitude {encounter.position.y:F0}m.");
#endif
        }

        #endregion

        #region Reference Resolution

        private void ResolveReferences()
        {
            if (playerTransform == null)
            {
                var cam = Camera.main;
                if (cam != null) playerTransform = cam.transform;
            }

            if (spawnSystem == null)
                spawnSystem = FindFirstObjectByType<WildlifeSpawnSystem>();

            if (audioController == null)
                audioController = FindFirstObjectByType<WildlifeAudioController>();

#if SWEF_TERRAIN_AVAILABLE
            if (_biomeMapper == null)
                _biomeMapper = FindFirstObjectByType<SWEF.Terrain.TerrainBiomeMapper>();
#endif
#if SWEF_OCEAN_AVAILABLE
            if (_oceanManager == null)
                _oceanManager = SWEF.Ocean.OceanManager.Instance;
#endif
#if SWEF_WEATHER_AVAILABLE
            if (_weatherManager == null)
                _weatherManager = FindFirstObjectByType<SWEF.Weather.WeatherManager>();
#endif
#if SWEF_TIMEOFDAY_AVAILABLE
            if (_timeOfDayManager == null)
                _timeOfDayManager = FindFirstObjectByType<SWEF.TimeOfDay.TimeOfDayManager>();
#endif
#if SWEF_FLIGHT_AVAILABLE
            if (_flightController == null)
                _flightController = FindFirstObjectByType<SWEF.Flight.FlightController>();
#endif
#if SWEF_ACHIEVEMENT_AVAILABLE
            if (_achievementManager == null)
                _achievementManager = FindFirstObjectByType<SWEF.Achievement.AchievementManager>();
#endif
#if SWEF_JOURNAL_AVAILABLE
            if (_journalManager == null)
                _journalManager = FindFirstObjectByType<SWEF.Journal.JournalManager>();
#endif
        }

        #endregion
    }
}
