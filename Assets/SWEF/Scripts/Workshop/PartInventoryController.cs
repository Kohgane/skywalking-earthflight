// PartInventoryController.cs — SWEF Aircraft Workshop & Part Customization (Phase 90)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SWEF.Workshop
{
    /// <summary>
    /// Singleton MonoBehaviour that manages the player's collection of unlocked
    /// aircraft parts.  Persists to <c>Application.persistentDataPath/workshop_inventory.json</c>.
    /// </summary>
    public class PartInventoryController : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        /// <summary>Global singleton instance.</summary>
        public static PartInventoryController Instance { get; private set; }

        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Persistence")]
        [Tooltip("File name written to Application.persistentDataPath.")]
        [SerializeField] private string _saveFileName = "workshop_inventory.json";

        // ── State ──────────────────────────────────────────────────────────────

        private readonly Dictionary<string, AircraftPartData> _inventory =
            new Dictionary<string, AircraftPartData>(StringComparer.Ordinal);

        // ── Persistence path ───────────────────────────────────────────────────
        private string SavePath => Path.Combine(Application.persistentDataPath, _saveFileName);

        // ── Unity Lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadInventory();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Adds a part to the player's inventory.  No-ops silently if the part is
        /// already present.
        /// </summary>
        /// <param name="part">Part data to register.</param>
        public void AddPart(AircraftPartData part)
        {
            if (part == null)
            {
                Debug.LogWarning("[SWEF] Workshop: AddPart called with null part.");
                return;
            }
            if (string.IsNullOrEmpty(part.partId))
            {
                Debug.LogWarning("[SWEF] Workshop: AddPart called with empty partId.");
                return;
            }
            if (_inventory.ContainsKey(part.partId)) return;

            _inventory[part.partId] = part;
            SaveInventory();
        }

        /// <summary>
        /// Removes a part from the player's inventory by its ID.
        /// </summary>
        /// <param name="partId">The <see cref="AircraftPartData.partId"/> to remove.</param>
        /// <returns><c>true</c> if the part was found and removed; otherwise <c>false</c>.</returns>
        public bool RemovePart(string partId)
        {
            if (string.IsNullOrEmpty(partId))
            {
                Debug.LogWarning("[SWEF] Workshop: RemovePart called with empty partId.");
                return false;
            }
            bool removed = _inventory.Remove(partId);
            if (removed) SaveInventory();
            return removed;
        }

        /// <summary>
        /// Returns <c>true</c> if the player owns the part with the specified ID.
        /// </summary>
        /// <param name="partId">Part ID to check.</param>
        public bool HasPart(string partId)
        {
            if (string.IsNullOrEmpty(partId)) return false;
            return _inventory.ContainsKey(partId);
        }

        /// <summary>
        /// Retrieves a part by its unique ID, or <c>null</c> if not owned.
        /// </summary>
        /// <param name="partId">Part ID to look up.</param>
        public AircraftPartData GetPartById(string partId)
        {
            if (string.IsNullOrEmpty(partId)) return null;
            _inventory.TryGetValue(partId, out var part);
            return part;
        }

        /// <summary>
        /// Returns all owned parts of a specific <see cref="AircraftPartType"/>.
        /// </summary>
        /// <param name="type">The part category to filter by.</param>
        public List<AircraftPartData> GetPartsByType(AircraftPartType type)
        {
            return _inventory.Values.Where(p => p.partType == type).ToList();
        }

        /// <summary>
        /// Returns all owned parts of a specific <see cref="PartTier"/>.
        /// </summary>
        /// <param name="tier">The quality tier to filter by.</param>
        public List<AircraftPartData> GetPartsByTier(PartTier tier)
        {
            return _inventory.Values.Where(p => p.tier == tier).ToList();
        }

        /// <summary>Returns a snapshot of the entire owned part collection.</summary>
        public IReadOnlyCollection<AircraftPartData> GetAllParts() => _inventory.Values;

        // ── Persistence ────────────────────────────────────────────────────────

        /// <summary>Persists the current inventory to disk as JSON.</summary>
        public void SaveInventory()
        {
            try
            {
                var wrapper = new InventoryWrapper { parts = _inventory.Values.ToList() };
                File.WriteAllText(SavePath, JsonUtility.ToJson(wrapper, prettyPrint: true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Workshop: Failed to save inventory — {ex.Message}");
            }
        }

        /// <summary>Loads the inventory from disk.  Silently skips if the file does not exist.</summary>
        public void LoadInventory()
        {
            if (!File.Exists(SavePath)) return;

            try
            {
                string json    = File.ReadAllText(SavePath);
                var wrapper    = JsonUtility.FromJson<InventoryWrapper>(json);
                _inventory.Clear();
                if (wrapper?.parts != null)
                {
                    foreach (var p in wrapper.parts)
                    {
                        if (!string.IsNullOrEmpty(p?.partId))
                            _inventory[p.partId] = p;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Workshop: Failed to load inventory — {ex.Message}");
            }
        }

        // ── Serialisation helper ───────────────────────────────────────────────

        [Serializable]
        private class InventoryWrapper
        {
            public List<AircraftPartData> parts = new List<AircraftPartData>();
        }
    }
}
