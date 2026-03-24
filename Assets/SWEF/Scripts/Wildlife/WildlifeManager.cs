using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Wildlife
{
    /// <summary>
    /// Phase 75 — Central singleton that manages all wildlife spawning, lifecycle, and
    /// cross-system integration for SWEF. Attach to a persistent GameObject in the bootstrap scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class WildlifeManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static WildlifeManager Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Configuration")]
        [SerializeField] private WildlifeConfig config = new WildlifeConfig();

        [Header("Species Database")]
        [Tooltip("All wildlife species registered for spawning.")]
        [SerializeField] private List<WildlifeSpecies> speciesDatabase = new List<WildlifeSpecies>();

        [Header("References")]
        [Tooltip("Player/camera transform used as origin. Resolved at runtime if null.")]
        [SerializeField] private Transform playerTransform;

        [Tooltip("Spawn system component. Auto-resolved if null.")]
        [SerializeField] private WildlifeSpawnSystem spawnSystem;

        [Tooltip("Audio controller for wildlife sounds. Auto-resolved if null.")]
        [SerializeField] private WildlifeAudioController audioController;

        #endregion

        #region Events

        /// <summary>Fired when a new wildlife group is spawned.</summary>
        public event Action<WildlifeGroupState> OnGroupSpawned;

        /// <summary>Fired when a wildlife group is removed.</summary>
        public event Action<string> OnGroupDespawned;

        /// <summary>Fired the first time the player encounters a species.</summary>
        public event Action<WildlifeSpecies> OnSpeciesDiscovered;

        /// <summary>Fired when a bird strike occurs.</summary>
        public event Action<WildlifeSpecies, Vector3> OnBirdStrike;

        /// <summary>Fired when a new encounter record is created.</summary>
        public event Action<WildlifeEncounterRecord> OnEncounterRecorded;

        #endregion

        #region Public Properties

        /// <summary>Read-only view of all currently active wildlife groups.</summary>
        public IReadOnlyList<WildlifeGroupState> ActiveGroups => _activeGroups;

        /// <summary>Exposes the runtime configuration object.</summary>
        public WildlifeConfig Config => config;

        /// <summary>Total number of individual wildlife entities currently active.</summary>
        public int TotalIndividuals => _totalIndividuals;

        #endregion

        #region Private State

        private readonly List<WildlifeGroupState> _activeGroups = new List<WildlifeGroupState>();
        private readonly HashSet<string> _discoveredSpecies = new HashSet<string>();
        private int _totalIndividuals;
        private Coroutine _spawnCoroutine;
        private static int _groupIdCounter;

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
            RegisterDefaultSpecies();
            ApplyQualityScaling();
        }

        private void Start()
        {
            _spawnCoroutine = StartCoroutine(SpawnLoop());
        }

        private void Update()
        {
            if (config.enableBirdStrikes)
                CheckBirdStrikes();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region Initialization

        private void ResolveReferences()
        {
            if (playerTransform == null)
            {
                var cam = Camera.main;
                if (cam != null)
                    playerTransform = cam.transform;
            }
            if (spawnSystem == null)
                spawnSystem = GetComponent<WildlifeSpawnSystem>();
            if (audioController == null)
                audioController = GetComponent<WildlifeAudioController>();
        }

        private void ApplyQualityScaling()
        {
            float m = config.qualityScaleMultiplier;
            config.maxActiveGroups = Mathf.Max(1, Mathf.RoundToInt(config.maxActiveGroups * m));
            config.maxIndividualsTotal = Mathf.Max(1, Mathf.RoundToInt(config.maxIndividualsTotal * m));
        }

        private void RegisterDefaultSpecies()
        {
            // Raptors
            RegisterSpecies(MakeSpecies("bald_eagle",      "wildlife_species_bald_eagle",     "wildlife_desc_bald_eagle",     WildlifeCategory.Raptor,      new[]{ SpawnBiome.Forest, SpawnBiome.Mountain }, 200f, 1500f, 12f, 25f, 200f, 600f, 1, 3, 0.6f, 2f));
            // Seabirds
            RegisterSpecies(MakeSpecies("seagull",         "wildlife_species_seagull",        "wildlife_desc_seagull",        WildlifeCategory.Seabird,     new[]{ SpawnBiome.Coast, SpawnBiome.Ocean },       0f,  300f,  8f, 18f, 100f, 300f, 2, 12, 2f, 0f));
            RegisterSpecies(MakeSpecies("albatross",       "wildlife_species_albatross",      "wildlife_desc_albatross",      WildlifeCategory.Seabird,     new[]{ SpawnBiome.Ocean },                         50f, 800f, 14f, 28f, 150f, 400f, 1,  4, 1f, 1f));
            // Waterfowl
            RegisterSpecies(MakeSpecies("canada_goose",    "wildlife_species_canada_goose",   "wildlife_desc_canada_goose",   WildlifeCategory.Waterfowl,   new[]{ SpawnBiome.Lake, SpawnBiome.River, SpawnBiome.Grassland }, 0f, 400f, 10f, 20f, 120f, 350f, 4, 20, 1.5f, 0f, true));
            // Birds
            RegisterSpecies(MakeSpecies("sparrow",         "wildlife_species_sparrow",        "wildlife_desc_sparrow",        WildlifeCategory.Bird,        new[]{ SpawnBiome.Forest, SpawnBiome.Grassland, SpawnBiome.Urban }, 0f, 150f, 7f, 15f, 80f, 200f, 10, 60, 3f, 0f));
            RegisterSpecies(MakeSpecies("arctic_tern",     "wildlife_species_arctic_tern",    "wildlife_desc_arctic_tern",    WildlifeCategory.MigratoryBird, new[]{ SpawnBiome.Arctic, SpawnBiome.Ocean, SpawnBiome.Coast }, 0f, 500f, 11f, 22f, 120f, 350f, 2, 15, 0.8f, 1f, true, new[]{3,4,9,10}));
            RegisterSpecies(MakeSpecies("flamingo",        "wildlife_species_flamingo",       "wildlife_desc_flamingo",       WildlifeCategory.Bird,        new[]{ SpawnBiome.Wetland, SpawnBiome.Lake },      0f,  200f,  9f, 18f, 100f, 300f, 5, 30, 0.7f, 1f));
            RegisterSpecies(MakeSpecies("pelican",         "wildlife_species_pelican",        "wildlife_desc_pelican",        WildlifeCategory.Seabird,     new[]{ SpawnBiome.Coast, SpawnBiome.Lake },         0f,  250f, 10f, 20f, 120f, 350f, 3, 12, 1f, 0f));
            // Marine Mammals
            RegisterSpecies(MakeSpecies("dolphin",         "wildlife_species_dolphin",        "wildlife_desc_dolphin",        WildlifeCategory.MarineMammal, new[]{ SpawnBiome.Ocean, SpawnBiome.Coast },       0f,   10f, 15f, 30f, 200f, 600f, 3, 10, 1.5f, 0f));
            RegisterSpecies(MakeSpecies("humpback_whale",  "wildlife_species_humpback_whale", "wildlife_desc_humpback_whale", WildlifeCategory.MarineMammal, new[]{ SpawnBiome.Ocean },                         0f,    5f,  5f, 15f, 400f, 1000f, 1, 3, 0.5f, 1f));
            RegisterSpecies(MakeSpecies("blue_whale",      "wildlife_species_blue_whale",     "wildlife_desc_blue_whale",     WildlifeCategory.MarineMammal, new[]{ SpawnBiome.Ocean },                         0f,    5f,  4f, 12f, 500f, 1200f, 1, 2, 0.2f, 2f));
            // Fish
            RegisterSpecies(MakeSpecies("salmon",          "wildlife_species_salmon",         "wildlife_desc_salmon",         WildlifeCategory.Fish,        new[]{ SpawnBiome.River, SpawnBiome.Lake, SpawnBiome.Ocean }, 0f, 5f, 3f, 10f, 50f, 150f, 10, 50, 1.5f, 0f));
            // Land Mammals
            RegisterSpecies(MakeSpecies("deer",            "wildlife_species_deer",           "wildlife_desc_deer",           WildlifeCategory.LandMammal,  new[]{ SpawnBiome.Forest, SpawnBiome.Grassland },  0f,   50f,  5f, 14f, 200f, 500f, 2,  8, 2f, 0f));
            RegisterSpecies(MakeSpecies("bison",           "wildlife_species_bison",          "wildlife_desc_bison",          WildlifeCategory.LandMammal,  new[]{ SpawnBiome.Grassland },                      0f,   50f,  6f, 16f, 300f, 700f, 5, 40, 1f, 0f));
            // Insects
            RegisterSpecies(MakeSpecies("butterfly",       "wildlife_species_butterfly",      "wildlife_desc_butterfly",      WildlifeCategory.Insect,      new[]{ SpawnBiome.Forest, SpawnBiome.Grassland, SpawnBiome.Tropical }, 0f, 50f, 2f, 8f, 30f, 80f, 20, 100, 2f, 0f));
        }

        private static WildlifeSpecies MakeSpecies(
            string id, string nameKey, string descKey, WildlifeCategory cat,
            SpawnBiome[] biomes, float minAlt, float maxAlt,
            float speed, float fleeSpeed, float fleeDist, float awareDist,
            int minGroup, int maxGroup, float weight, float rarity,
            bool migratory = false, int[] migrationMonths = null)
        {
            return new WildlifeSpecies
            {
                speciesId        = id,
                displayNameKey   = nameKey,
                descriptionKey   = descKey,
                category         = cat,
                preferredBiomes  = biomes,
                activityPattern  = ActivityPattern.Diurnal,
                minAltitude      = minAlt,
                maxAltitude      = maxAlt,
                baseSpeed        = speed,
                fleeSpeed        = fleeSpeed,
                fleeDistance     = fleeDist,
                awareDistance    = awareDist,
                minGroupSize     = minGroup,
                maxGroupSize     = maxGroup,
                spawnWeight      = weight,
                isMigratory      = migratory,
                migratorySeason  = migrationMonths ?? new int[0],
                rarityTier       = rarity
            };
        }

        #endregion

        #region Species Database

        /// <summary>Registers a species into the runtime database.</summary>
        public void RegisterSpecies(WildlifeSpecies species)
        {
            if (species == null || string.IsNullOrEmpty(species.speciesId)) return;
            speciesDatabase.Add(species);
        }

        /// <summary>Returns the species with the given ID, or null if not found.</summary>
        public WildlifeSpecies GetSpeciesById(string id)
        {
            for (int i = 0; i < speciesDatabase.Count; i++)
                if (speciesDatabase[i].speciesId == id)
                    return speciesDatabase[i];
            return null;
        }

        /// <summary>Returns all species whose preferred biomes include the given biome.</summary>
        public List<WildlifeSpecies> GetSpeciesForBiome(SpawnBiome biome)
        {
            var result = new List<WildlifeSpecies>();
            foreach (var s in speciesDatabase)
                foreach (var b in s.preferredBiomes)
                    if (b == biome) { result.Add(s); break; }
            return result;
        }

        #endregion

        #region Spawn / Despawn Loop

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(config.spawnInterval);
                TrySpawnGroup();
                DespawnExpiredGroups();
            }
        }

        private void TrySpawnGroup()
        {
            if (_activeGroups.Count >= config.maxActiveGroups) return;
            if (_totalIndividuals >= config.maxIndividualsTotal) return;
            if (playerTransform == null) return;

            // Determine current biome (null-safe)
            SpawnBiome biome = SpawnBiome.Grassland;
#if SWEF_BIOME_AVAILABLE
            var biomeClassifier = SWEF.Biome.BiomeClassifier.Instance;
            if (biomeClassifier != null)
                biome = MapToSpawnBiome(biomeClassifier.GetBiomeAt(playerTransform.position));
#endif

            // Filter species by biome and altitude
            float playerAlt = playerTransform.position.y;
            var candidates = GetSpeciesForBiome(biome);
            candidates.RemoveAll(s =>
                playerAlt < s.minAltitude || playerAlt > s.maxAltitude + 200f);

            // Filter by time of day (null-safe)
#if SWEF_TIMEOFDAY_AVAILABLE
            bool isNight = false;
            var tod = SWEF.TimeOfDay.TimeOfDayManager.Instance;
            if (tod != null) isNight = tod.IsNight;
            candidates.RemoveAll(s =>
                (s.activityPattern == ActivityPattern.Diurnal && isNight) ||
                (s.activityPattern == ActivityPattern.Nocturnal && !isNight));
#endif

            if (candidates.Count == 0) return;

            // Weighted random selection
            WildlifeSpecies selected = WeightedRandom(candidates);
            if (selected == null) return;

            // Random ring position around player
            float angle  = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist   = UnityEngine.Random.Range(config.spawnRadius * 0.5f, config.spawnRadius);
            Vector3 spawnPos = playerTransform.position
                + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            spawnPos.y = UnityEngine.Random.Range(selected.minAltitude, selected.maxAltitude);

            int count = UnityEngine.Random.Range(selected.minGroupSize, selected.maxGroupSize + 1);
            if (_totalIndividuals + count > config.maxIndividualsTotal)
                count = config.maxIndividualsTotal - _totalIndividuals;
            if (count <= 0) return;

            // Instantiate via spawn system if available
            if (spawnSystem != null)
                spawnSystem.SpawnGroup(selected, spawnPos, count);

            var group = new WildlifeGroupState
            {
                groupId         = "grp_" + (++_groupIdCounter),
                species         = selected,
                centerPosition  = spawnPos,
                groupVelocity   = Vector3.zero,
                currentBehavior = WildlifeBehavior.Roaming,
                threatLevel     = WildlifeThreatLevel.None,
                memberCount     = count,
                spawnTime       = Time.time,
                lifetime        = UnityEngine.Random.Range(120f, 300f),
                isDiscovered    = false
            };

            _activeGroups.Add(group);
            _totalIndividuals += count;
            OnGroupSpawned?.Invoke(group);
        }

        private void DespawnExpiredGroups()
        {
            if (playerTransform == null) return;
            float now = Time.time;
            for (int i = _activeGroups.Count - 1; i >= 0; i--)
            {
                var g = _activeGroups[i];
                float dist = Vector3.Distance(g.centerPosition, playerTransform.position);
                bool tooFar  = dist > config.despawnRadius;
                bool expired = (now - g.spawnTime) > g.lifetime;
                if (tooFar || expired)
                {
                    if (spawnSystem != null)
                        spawnSystem.DespawnGroup(g.groupId);
                    _totalIndividuals = Mathf.Max(0, _totalIndividuals - g.memberCount);
                    OnGroupDespawned?.Invoke(g.groupId);
                    _activeGroups.RemoveAt(i);
                }
            }
        }

        #endregion

        #region Bird Strike Detection

        private void CheckBirdStrikes()
        {
            if (playerTransform == null) return;
            foreach (var g in _activeGroups)
            {
                if (g.species.category != WildlifeCategory.Bird &&
                    g.species.category != WildlifeCategory.Raptor &&
                    g.species.category != WildlifeCategory.Seabird &&
                    g.species.category != WildlifeCategory.Waterfowl &&
                    g.species.category != WildlifeCategory.MigratoryBird) continue;

                float dist = Vector3.Distance(g.centerPosition, playerTransform.position);
                if (dist <= config.birdStrikeDistance)
                {
                    OnBirdStrike?.Invoke(g.species, g.centerPosition);
#if SWEF_DAMAGE_AVAILABLE
                    var dm = SWEF.Damage.DamageModel.Instance;
                    dm?.ApplyBirdStrikeDamage(g.species.category);
#endif
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>Marks a species as discovered and fires the discovery event if new.</summary>
        public void ReportDiscovery(WildlifeSpecies species)
        {
            if (species == null) return;
            if (_discoveredSpecies.Add(species.speciesId))
                OnSpeciesDiscovered?.Invoke(species);
        }

        /// <summary>Records a wildlife encounter and fires the encounter event.</summary>
        public void RecordEncounter(WildlifeEncounterRecord record)
        {
            if (record == null) return;
            OnEncounterRecorded?.Invoke(record);
        }

        /// <summary>Returns all groups within the specified radius of a position.</summary>
        public List<WildlifeGroupState> GetNearbyGroups(Vector3 pos, float radius)
        {
            var result = new List<WildlifeGroupState>();
            foreach (var g in _activeGroups)
                if (Vector3.Distance(g.centerPosition, pos) <= radius)
                    result.Add(g);
            return result;
        }

        /// <summary>Returns the set of discovered species IDs.</summary>
        public IReadOnlyCollection<string> DiscoveredSpecies => _discoveredSpecies;

        #endregion

        #region Helpers

        private static WildlifeSpecies WeightedRandom(List<WildlifeSpecies> list)
        {
            if (list.Count == 0) return null;
            float total = 0f;
            foreach (var s in list) total += s.spawnWeight;
            float roll = UnityEngine.Random.Range(0f, total);
            float acc  = 0f;
            foreach (var s in list)
            {
                acc += s.spawnWeight;
                if (roll <= acc) return s;
            }
            return list[list.Count - 1];
        }

        private static SpawnBiome MapToSpawnBiome(object biome)
        {
            // Fallback mapping; real implementation resolves SWEF.Biome enum
            return SpawnBiome.Grassland;
        }

        #endregion
    }
}
