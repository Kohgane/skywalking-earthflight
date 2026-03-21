using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using SWEF.Flight;
using SWEF.Minimap;
using SWEF.Achievement;
using SWEF.Progression;
using SWEF.Analytics;

namespace SWEF.HiddenGems
{
    /// <summary>
    /// Central singleton manager for the Hidden Gems &amp; Secret Locations system.
    /// Loads all <see cref="HiddenGemDefinition"/> records from <see cref="HiddenGemDatabase"/>,
    /// persists <see cref="HiddenGemState"/> data as JSON, and runs per-frame proximity
    /// detection using a spatial hash grid.
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public class HiddenGemManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static HiddenGemManager Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when the player discovers a gem for the first time.</summary>
        public event Action<GemDiscoveryEvent> OnGemDiscovered;

        /// <summary>Fired when the player toggles the favourite flag on a gem.</summary>
        public event Action<string, bool> OnGemFavorited;

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Proximity Detection")]
        [Tooltip("Grid cell size in world units used for spatial hashing.")]
        [SerializeField] private float gridCellSize = 10000f;

        [Tooltip("Maximum distance from the player at which proximity checks are run.")]
        [SerializeField] private float maxProximityCheckRange = 10000f;

        // ── Persistence ───────────────────────────────────────────────────────────
        private static readonly string SaveFile  = "hidden_gems.json";
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFile);

        // ── Internal state ────────────────────────────────────────────────────────
        private List<HiddenGemDefinition>             _allGems   = new List<HiddenGemDefinition>();
        private Dictionary<string, HiddenGemState>   _states    = new Dictionary<string, HiddenGemState>();
        private Dictionary<Vector2Int, List<string>> _spatialGrid = new Dictionary<Vector2Int, List<string>>();

        // Cache of gem lookups by id
        private Dictionary<string, HiddenGemDefinition> _byId = new Dictionary<string, HiddenGemDefinition>();

        // References (auto-found)
        private FlightController _flightController;
        private MinimapManager   _minimapManager;

        // ── Serialization helpers ─────────────────────────────────────────────────
        [Serializable]
        private class SaveData
        {
            public List<HiddenGemState> states = new List<HiddenGemState>();
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

            LoadDefinitions();
            LoadStates();
            BuildSpatialGrid();

            Debug.Log($"[SWEF] HiddenGemManager: {_allGems.Count} gems loaded.");
        }

        private void Start()
        {
            _flightController = FindFirstObjectByType<FlightController>();
            _minimapManager   = MinimapManager.Instance;

            RegisterMinimapBlips();
        }

        private void Update()
        {
            if (_flightController == null) return;
            CheckProximity(_flightController.transform.position,
                           _flightController.Velocity.magnitude);
        }

        private void OnApplicationPause(bool paused) { if (paused) SaveStates(); }
        private void OnApplicationQuit()             { SaveStates(); }

        // ── Initialisation ────────────────────────────────────────────────────────

        private void LoadDefinitions()
        {
            _allGems = HiddenGemDatabase.GetAllGems();
            foreach (var gem in _allGems)
                _byId[gem.gemId] = gem;
        }

        private void LoadStates()
        {
            if (!File.Exists(SavePath))
            {
                InitDefaultStates();
                return;
            }
            try
            {
                string json = File.ReadAllText(SavePath);
                var save = JsonUtility.FromJson<SaveData>(json);
                if (save?.states != null)
                    foreach (var s in save.states)
                        _states[s.gemId] = s;

                // Ensure every gem has a state entry
                InitMissingStates();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SWEF] HiddenGemManager: Failed to load states — {e.Message}");
                InitDefaultStates();
            }
        }

        private void InitDefaultStates()
        {
            _states.Clear();
            foreach (var gem in _allGems)
                _states[gem.gemId] = new HiddenGemState { gemId = gem.gemId };
        }

        private void InitMissingStates()
        {
            foreach (var gem in _allGems)
                if (!_states.ContainsKey(gem.gemId))
                    _states[gem.gemId] = new HiddenGemState { gemId = gem.gemId };
        }

        private void SaveStates()
        {
            try
            {
                var save = new SaveData { states = _states.Values.ToList() };
                File.WriteAllText(SavePath, JsonUtility.ToJson(save, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SWEF] HiddenGemManager: Failed to save states — {e.Message}");
            }
        }

        // ── Spatial hashing ───────────────────────────────────────────────────────

        private void BuildSpatialGrid()
        {
            _spatialGrid.Clear();
            foreach (var gem in _allGems)
            {
                Vector2Int cell = WorldToCell(GemToWorld(gem));
                if (!_spatialGrid.ContainsKey(cell))
                    _spatialGrid[cell] = new List<string>();
                _spatialGrid[cell].Add(gem.gemId);
            }
        }

        private Vector2Int WorldToCell(Vector3 pos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(pos.x / gridCellSize),
                Mathf.FloorToInt(pos.z / gridCellSize));
        }

        /// <summary>
        /// Approximate world-space position from GPS coordinates.
        /// Uses an equirectangular projection centred on the origin.
        /// </summary>
        private static Vector3 GemToWorld(HiddenGemDefinition gem)
        {
            const float MetersPerDeg = 111320f;
            float x = (float)(gem.longitude * MetersPerDeg * Math.Cos(gem.latitude * Math.PI / 180.0));
            float z = (float)(gem.latitude  * MetersPerDeg);
            return new Vector3(x, gem.altitudeHint, z);
        }

        // ── Proximity detection ───────────────────────────────────────────────────

        private void CheckProximity(Vector3 playerPos, float playerSpeed)
        {
            Vector2Int playerCell = WorldToCell(playerPos);
            int cellRadius = Mathf.CeilToInt(maxProximityCheckRange / gridCellSize) + 1;

            for (int dx = -cellRadius; dx <= cellRadius; dx++)
            {
                for (int dz = -cellRadius; dz <= cellRadius; dz++)
                {
                    var cell = new Vector2Int(playerCell.x + dx, playerCell.y + dz);
                    if (!_spatialGrid.TryGetValue(cell, out var ids)) continue;

                    foreach (string id in ids)
                    {
                        if (!_byId.TryGetValue(id, out var gem)) continue;
                        var state = _states[id];
                        if (state.isDiscovered) continue;

                        // Check unlock requirement
                        if (!string.IsNullOrEmpty(gem.unlockRequirement) &&
                            !IsUnlockMet(gem.unlockRequirement)) continue;

                        float dist = Vector3.Distance(playerPos, GemToWorld(gem));
                        if (dist <= gem.discoveryRadiusMeters)
                        {
                            TriggerDiscovery(gem, state, playerPos.y, playerSpeed);
                        }
                    }
                }
            }
        }

        private bool IsUnlockMet(string requirement)
        {
            // Format: "discover_N_continent" or "discover_N_category"
            if (string.IsNullOrEmpty(requirement)) return true;
            var parts = requirement.Split('_');
            if (parts.Length < 3) return true;
            if (!int.TryParse(parts[1], out int needed)) return true;
            string scopeStr = string.Join("_", parts, 2, parts.Length - 2);

            int found = 0;
            // Try continent
            foreach (GemContinent c in Enum.GetValues(typeof(GemContinent)))
            {
                if (c.ToString().ToLowerInvariant() == scopeStr)
                {
                    found = GetDiscoveredGems().Count(g => g.continent == c);
                    return found >= needed;
                }
            }
            // Try category
            foreach (GemCategory cat in Enum.GetValues(typeof(GemCategory)))
            {
                if (cat.ToString().ToLowerInvariant() == scopeStr)
                {
                    found = GetDiscoveredGems().Count(g => g.category == cat);
                    return found >= needed;
                }
            }
            return true;
        }

        private void TriggerDiscovery(HiddenGemDefinition gem, HiddenGemState state,
                                      float altitude, float speed)
        {
            state.isDiscovered       = true;
            state.discoveredDate     = DateTime.UtcNow.ToString("o");
            state.discoveryAltitude  = altitude;
            state.discoverySpeed     = speed;
            state.timesVisited++;

            var evt = new GemDiscoveryEvent
            {
                gem       = gem,
                state     = state,
                timestamp = DateTime.UtcNow
            };

            // Award XP
            if (ProgressionManager.Instance != null)
                ProgressionManager.Instance.AddXP(gem.xpReward, $"gem_discovered_{gem.gemId}");

            // Achievement reporting
            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.ReportProgress("gems_discovered_total", 1f);
                AchievementManager.Instance.ReportProgress($"gems_discovered_{gem.continent.ToString().ToLowerInvariant()}", 1f);
                AchievementManager.Instance.ReportProgress($"gems_discovered_{gem.category.ToString().ToLowerInvariant()}", 1f);
                AchievementManager.Instance.ReportProgress($"gems_{gem.rarity.ToString().ToLowerInvariant()}_found", 1f);
            }

            // Analytics — use UserBehaviorTracker if available
            var tracker = FindFirstObjectByType<UserBehaviorTracker>();
            tracker?.TrackFeatureDiscovery($"hidden_gem_{gem.gemId}");

            // Register minimap blip
            RegisterDiscoveredBlip(gem);

            OnGemDiscovered?.Invoke(evt);
            SaveStates();
        }

        // ── Minimap integration ───────────────────────────────────────────────────

        private void RegisterMinimapBlips()
        {
            if (_minimapManager == null) return;
            foreach (var gem in _allGems)
            {
                var state = _states[gem.gemId];
                if (state.isDiscovered)
                    RegisterDiscoveredBlip(gem);
                else if (gem.isHintVisible)
                    RegisterHintBlip(gem);
            }
        }

        private void RegisterDiscoveredBlip(HiddenGemDefinition gem)
        {
            if (_minimapManager == null) return;
            var blip = new MinimapBlip
            {
                blipId        = "hiddengem_" + gem.gemId,
                iconType      = MinimapIconType.PointOfInterest,
                worldPosition = GemToWorld(gem),
                label         = gem.nameKey,
                color         = RarityToColor(gem.rarity),
                isActive      = true,
                isPulsing     = false,
                customIconId  = gem.iconOverride
            };
            blip.metadata["gemId"]    = gem.gemId;
            blip.metadata["rarity"]   = gem.rarity.ToString();
            blip.metadata["category"] = gem.category.ToString();
            _minimapManager.RegisterBlip(blip);
        }

        private void RegisterHintBlip(HiddenGemDefinition gem)
        {
            if (_minimapManager == null) return;
            var blip = new MinimapBlip
            {
                blipId        = "hiddengem_hint_" + gem.gemId,
                iconType      = MinimapIconType.PointOfInterest,
                worldPosition = GemToWorld(gem),
                label         = "???",
                color         = new Color(0.7f, 0.7f, 0.7f, 0.5f),
                isActive      = true,
                isPulsing     = true,
                customIconId  = ""
            };
            blip.metadata["gemId"]  = gem.gemId;
            blip.metadata["isHint"] = "true";
            _minimapManager.RegisterBlip(blip);
        }

        private static Color RarityToColor(GemRarity r) => r switch
        {
            GemRarity.Common    => new Color(0.667f, 0.667f, 0.667f),
            GemRarity.Uncommon  => new Color(0.118f, 1.000f, 0.000f),
            GemRarity.Rare      => new Color(0.000f, 0.439f, 1.000f),
            GemRarity.Epic      => new Color(0.639f, 0.204f, 0.933f),
            GemRarity.Legendary => new Color(1.000f, 0.502f, 0.000f),
            _                   => Color.white
        };

        // ── Public query API ──────────────────────────────────────────────────────

        /// <summary>Returns all gem definitions.</summary>
        public List<HiddenGemDefinition> GetAllGems() => _allGems;

        /// <summary>Returns all gems that have been discovered.</summary>
        public List<HiddenGemDefinition> GetDiscoveredGems()
            => _allGems.Where(g => _states.TryGetValue(g.gemId, out var s) && s.isDiscovered).ToList();

        /// <summary>Returns all gems not yet discovered.</summary>
        public List<HiddenGemDefinition> GetUndiscoveredGems()
            => _allGems.Where(g => !(_states.TryGetValue(g.gemId, out var s) && s.isDiscovered)).ToList();

        /// <summary>Returns all gems on the given continent.</summary>
        public List<HiddenGemDefinition> GetGemsByContinent(GemContinent continent)
            => _allGems.Where(g => g.continent == continent).ToList();

        /// <summary>Returns all gems with the given category.</summary>
        public List<HiddenGemDefinition> GetGemsByCategory(GemCategory category)
            => _allGems.Where(g => g.category == category).ToList();

        /// <summary>Returns all gems with the given rarity.</summary>
        public List<HiddenGemDefinition> GetGemsByRarity(GemRarity rarity)
            => _allGems.Where(g => g.rarity == rarity).ToList();

        /// <summary>Returns the state for the specified gem, or null.</summary>
        public HiddenGemState GetGemState(string gemId)
            => _states.TryGetValue(gemId, out var s) ? s : null;

        /// <summary>Returns true if the gem with the given id has been discovered.</summary>
        public bool IsGemDiscovered(string gemId)
            => _states.TryGetValue(gemId, out var s) && s.isDiscovered;

        /// <summary>Returns (discovered, total) counts across all gems.</summary>
        public (int discovered, int total) GetDiscoveryProgress()
            => (GetDiscoveredGems().Count, _allGems.Count);

        /// <summary>Returns (discovered, total) counts for a specific continent.</summary>
        public (int discovered, int total) GetContinentProgress(GemContinent continent)
        {
            var gems = GetGemsByContinent(continent);
            int disc = gems.Count(g => _states.TryGetValue(g.gemId, out var s) && s.isDiscovered);
            return (disc, gems.Count);
        }

        /// <summary>Toggles the favourite flag on the given gem and persists.</summary>
        public void ToggleFavorite(string gemId)
        {
            if (!_states.TryGetValue(gemId, out var state)) return;
            state.isFavorited = !state.isFavorited;
            OnGemFavorited?.Invoke(gemId, state.isFavorited);
            SaveStates();
        }

        /// <summary>Increments timesVisited for the given gem and persists.</summary>
        public void RecordVisit(string gemId)
        {
            if (_states.TryGetValue(gemId, out var state))
            {
                state.timesVisited++;
                SaveStates();
            }
        }

        /// <summary>Marks photoTaken for the given gem and persists.</summary>
        public void RecordPhoto(string gemId)
        {
            if (_states.TryGetValue(gemId, out var state))
            {
                state.photoTaken = true;
                SaveStates();
            }
        }

        /// <summary>
        /// Returns the nearest undiscovered gem and the approximate distance to it,
        /// or (null, float.MaxValue) if none.
        /// </summary>
        public (HiddenGemDefinition gem, float distance) GetNearestUndiscoveredGem()
        {
            if (_flightController == null)
                return (null, float.MaxValue);

            Vector3 playerPos = _flightController.transform.position;
            HiddenGemDefinition nearest = null;
            float minDist = float.MaxValue;

            foreach (var gem in GetUndiscoveredGems())
            {
                float d = Vector3.Distance(playerPos, GemToWorld(gem));
                if (d < minDist)
                {
                    minDist = d;
                    nearest = gem;
                }
            }
            return (nearest, minDist);
        }

        /// <summary>
        /// Force-discovers a gem by id (useful for debug/testing).
        /// Does not award XP or fire achievement callbacks.
        /// </summary>
        public void ForceDiscover(string gemId)
        {
            if (!_byId.TryGetValue(gemId, out var gem)) return;
            var state = _states[gemId];
            if (state.isDiscovered) return;
            TriggerDiscovery(gem, state, 0f, 0f);
        }

        // ── Static world-position accessor (used by other systems) ────────────────

        /// <summary>
        /// Returns the approximate world-space Vector3 for the given gem definition.
        /// </summary>
        public static Vector3 GetWorldPosition(HiddenGemDefinition gem) => GemToWorld(gem);
    }
}
