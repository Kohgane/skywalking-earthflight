using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.Progression
{
    // ── Enums ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Visual / personalisation category for cosmetic items.
    /// </summary>
    public enum CosmeticCategory
    {
        /// <summary>Particle trail effects left behind the aircraft.</summary>
        TrailEffect,
        /// <summary>Texture and paint jobs applied to the aircraft body.</summary>
        AircraftSkin,
        /// <summary>Badge displayed on the player's profile card.</summary>
        Badge,
        /// <summary>Customisable text tag shown under the player's name.</summary>
        NameTag,
        /// <summary>Animated emote playable during multiplayer sessions.</summary>
        Emote
    }

    /// <summary>
    /// Singleton manager for cosmetic items that are unlocked through progression.
    /// Persists equipped/unlocked state to <c>Application.persistentDataPath/cosmetics.json</c>.
    /// </summary>
    public class CosmeticUnlockManager : MonoBehaviour
    {
        // ── Inner class ───────────────────────────────────────────────────────────

        /// <summary>Runtime definition of a single cosmetic item.</summary>
        [Serializable]
        public class CosmeticItem
        {
            /// <summary>Unique cosmetic identifier.</summary>
            public string id;
            /// <summary>Localization key for the cosmetic display name.</summary>
            public string nameKey;
            /// <summary>Visual category this item belongs to.</summary>
            public CosmeticCategory category;
            /// <summary>Rank level at which this item is automatically unlocked (0 = purchasable only).</summary>
            public int unlockedAtRank;
            /// <summary>Whether this item can be obtained through an in-app purchase.</summary>
            public bool isPurchasable;
        }

        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static CosmeticUnlockManager Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when a new cosmetic item is unlocked.</summary>
        public event Action<CosmeticItem> OnCosmeticUnlocked;

        /// <summary>Fired when a cosmetic item is equipped in a category slot.</summary>
        public event Action<CosmeticItem> OnCosmeticEquipped;

        // ── Persistence ───────────────────────────────────────────────────────────
        private static readonly string SaveFileName = "cosmetics.json";
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        [Serializable]
        private class CosmeticsSaveData
        {
            public List<string> unlockedIds  = new List<string>();
            // category index (int) → equipped cosmetic id
            public List<string> equippedByCategory = new List<string>(new string[5]);
        }

        private CosmeticsSaveData _save = new CosmeticsSaveData();

        // ── Runtime state ─────────────────────────────────────────────────────────
        private readonly Dictionary<string, CosmeticItem> _catalog   = new Dictionary<string, CosmeticItem>();
        private readonly HashSet<string>                  _unlockedIds = new HashSet<string>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadCatalog();
            Load();
        }

        private void OnEnable()
        {
            if (ProgressionManager.Instance != null)
                ProgressionManager.Instance.OnRankUp += HandleRankUp;
        }

        private void OnDisable()
        {
            if (ProgressionManager.Instance != null)
                ProgressionManager.Instance.OnRankUp -= HandleRankUp;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns all cosmetic items whose unlock conditions have been met.</summary>
        public List<CosmeticItem> GetUnlockedCosmetics()
        {
            var list = new List<CosmeticItem>();
            foreach (var id in _unlockedIds)
                if (_catalog.TryGetValue(id, out var item)) list.Add(item);
            return list;
        }

        /// <summary>Returns all cosmetic items that are not yet unlocked.</summary>
        public List<CosmeticItem> GetLockedCosmetics()
        {
            var list = new List<CosmeticItem>();
            foreach (var item in _catalog.Values)
                if (!_unlockedIds.Contains(item.id)) list.Add(item);
            return list;
        }

        /// <summary>Returns whether the cosmetic with the given ID has been unlocked.</summary>
        public bool IsUnlocked(string cosmeticId) =>
            !string.IsNullOrEmpty(cosmeticId) && _unlockedIds.Contains(cosmeticId);

        /// <summary>
        /// Equips a cosmetic item in its category slot.
        /// Only unlocked items can be equipped.
        /// </summary>
        /// <param name="cosmeticId">ID of the cosmetic to equip.</param>
        /// <param name="category">Category slot to fill.</param>
        public void EquipCosmetic(string cosmeticId, CosmeticCategory category)
        {
            if (!IsUnlocked(cosmeticId))
            {
                Debug.LogWarning($"[SWEF] CosmeticUnlockManager: Cannot equip locked cosmetic '{cosmeticId}'.");
                return;
            }

            int idx = (int)category;
            while (_save.equippedByCategory.Count <= idx)
                _save.equippedByCategory.Add(string.Empty);
            _save.equippedByCategory[idx] = cosmeticId;

            Save();
            if (_catalog.TryGetValue(cosmeticId, out var item))
                OnCosmeticEquipped?.Invoke(item);
        }

        /// <summary>
        /// Returns the ID of the currently equipped cosmetic for a category, or empty string if none.
        /// </summary>
        public string GetEquipped(CosmeticCategory category)
        {
            int idx = (int)category;
            if (idx < _save.equippedByCategory.Count)
                return _save.equippedByCategory[idx] ?? string.Empty;
            return string.Empty;
        }

        /// <summary>
        /// Manually unlocks a cosmetic by ID (e.g. from in-app purchase or admin grant).
        /// </summary>
        public void UnlockCosmetic(string cosmeticId)
        {
            if (string.IsNullOrEmpty(cosmeticId) || _unlockedIds.Contains(cosmeticId)) return;
            if (!_catalog.TryGetValue(cosmeticId, out var item)) return;

            _unlockedIds.Add(cosmeticId);
            if (!_save.unlockedIds.Contains(cosmeticId))
                _save.unlockedIds.Add(cosmeticId);

            Save();
            OnCosmeticUnlocked?.Invoke(item);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void LoadCatalog()
        {
            // In a full project, cosmetic definitions would be loaded from ScriptableObjects
            // under Resources/Cosmetics/. For now, populate with built-in defaults so the
            // system works without assets.
            foreach (var item in ProgressionDefaultData.GetDefaultCosmetics())
                _catalog[item.id] = item;
            Debug.Log($"[SWEF] CosmeticUnlockManager: Catalog ready with {_catalog.Count} items.");
        }

        private void HandleRankUp(PilotRankData oldRank, PilotRankData newRank)
        {
            // Auto-unlock cosmetics that match the new rank level
            foreach (var item in _catalog.Values)
            {
                if (item.unlockedAtRank > 0 && item.unlockedAtRank == newRank.rankLevel)
                    UnlockCosmetic(item.id);
            }
        }

        // ── Persistence ───────────────────────────────────────────────────────────

        private void Save()
        {
            try { File.WriteAllText(SavePath, JsonUtility.ToJson(_save, true)); }
            catch (Exception ex) { Debug.LogWarning($"[SWEF] CosmeticUnlockManager: Save failed — {ex.Message}"); }
        }

        private void Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    _save = JsonUtility.FromJson<CosmeticsSaveData>(File.ReadAllText(SavePath)) ?? new CosmeticsSaveData();
                    _unlockedIds.Clear();
                    foreach (var id in _save.unlockedIds)
                        _unlockedIds.Add(id);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] CosmeticUnlockManager: Load failed — {ex.Message}");
                _save = new CosmeticsSaveData();
            }
        }

        private void OnApplicationQuit() => Save();
        private void OnApplicationPause(bool p) { if (p) Save(); }
    }
}
