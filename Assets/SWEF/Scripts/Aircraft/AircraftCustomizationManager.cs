using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using UnityEngine;

namespace SWEF.Aircraft
{
    /// <summary>
    /// Central singleton that manages the player's unlocked skins, active loadout,
    /// and all loadout persistence. Subscribes to IAP and other systems to grant skins
    /// automatically when conditions are met.
    /// </summary>
    public class AircraftCustomizationManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────

        /// <summary>Singleton instance; persists across scene loads.</summary>
        public static AircraftCustomizationManager Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when a new skin ID is added to the unlocked list.</summary>
        public event Action<string> OnSkinUnlocked;

        /// <summary>Raised whenever the active loadout changes (equip, switch, create).</summary>
        public event Action<AircraftLoadout> OnLoadoutChanged;

        /// <summary>Raised after the customization data has been persisted.</summary>
        public event Action OnCustomizationSaved;

        // ── Constants ─────────────────────────────────────────────────────────────

        private const string SaveFileName = "aircraft_customization.json";

        // ── Internal state ────────────────────────────────────────────────────────

        private AircraftSkinRegistry _registry;
        private AircraftCustomizationSaveData _saveData = new AircraftCustomizationSaveData();

        // ── Properties ────────────────────────────────────────────────────────────

        /// <summary>The currently equipped loadout.</summary>
        public AircraftLoadout ActiveLoadout { get; private set; }

        /// <summary>Read-only view of all unlocked skin IDs.</summary>
        public ReadOnlyCollection<string> UnlockedSkinIds =>
            _saveData.unlockedSkinIds.AsReadOnly();

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

            _registry = FindObjectOfType<AircraftSkinRegistry>();
            if (_registry == null)
                Debug.LogWarning("[AircraftCustomizationManager] AircraftSkinRegistry not found in scene.");

            Load();
            EnsureDefaultLoadout();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void EnsureDefaultLoadout()
        {
            if (_saveData.loadouts == null)
                _saveData.loadouts = new System.Collections.Generic.List<AircraftLoadout>();

            if (_saveData.loadouts.Count == 0)
            {
                var defaultLoadout = new AircraftLoadout
                {
                    loadoutId = Guid.NewGuid().ToString(),
                    loadoutName = "Default"
                };

                // Auto-equip default skins
                if (_registry != null)
                {
                    foreach (var skin in _registry.GetDefaultSkins())
                    {
                        if (!IsSkinUnlocked(skin.skinId))
                            _saveData.unlockedSkinIds.Add(skin.skinId);
                        defaultLoadout.SetSkinForPart(skin.partType, skin.skinId);
                    }
                }

                _saveData.loadouts.Add(defaultLoadout);
                _saveData.activeLoadoutId = defaultLoadout.loadoutId;
            }

            SetActiveFromId(_saveData.activeLoadoutId);
        }

        private void SetActiveFromId(string id)
        {
            foreach (var loadout in _saveData.loadouts)
            {
                if (loadout.loadoutId == id)
                {
                    ActiveLoadout = loadout;
                    return;
                }
            }

            // Fallback to first
            if (_saveData.loadouts.Count > 0)
            {
                ActiveLoadout = _saveData.loadouts[0];
                _saveData.activeLoadoutId = ActiveLoadout.loadoutId;
            }
        }

        private string SaveFilePath =>
            Path.Combine(Application.persistentDataPath, SaveFileName);

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Adds <paramref name="skinId"/> to the player's unlocked collection and fires
        /// <see cref="OnSkinUnlocked"/>. No-op if already unlocked.
        /// </summary>
        public void UnlockSkin(string skinId)
        {
            if (string.IsNullOrEmpty(skinId)) return;
            if (IsSkinUnlocked(skinId)) return;

            _saveData.unlockedSkinIds.Add(skinId);
            OnSkinUnlocked?.Invoke(skinId);
            Save();
        }

        /// <summary>Returns <c>true</c> if the player has unlocked <paramref name="skinId"/>.</summary>
        public bool IsSkinUnlocked(string skinId)
        {
            if (string.IsNullOrEmpty(skinId)) return false;
            return _saveData.unlockedSkinIds.Contains(skinId);
        }

        /// <summary>
        /// Equips <paramref name="skinId"/> in the given <paramref name="part"/> slot of
        /// the active loadout, then fires <see cref="OnLoadoutChanged"/> and saves.
        /// </summary>
        public void EquipSkin(AircraftPartType part, string skinId)
        {
            if (ActiveLoadout == null)
            {
                Debug.LogWarning("[AircraftCustomizationManager] No active loadout to equip skin into.");
                return;
            }
            if (!IsSkinUnlocked(skinId))
            {
                Debug.LogWarning($"[AircraftCustomizationManager] Skin '{skinId}' is not unlocked.");
                return;
            }

            ActiveLoadout.SetSkinForPart(part, skinId);
            OnLoadoutChanged?.Invoke(ActiveLoadout);
            Save();
        }

        /// <summary>Returns the skin ID currently equipped in <paramref name="part"/>.</summary>
        public string GetEquippedSkin(AircraftPartType part)
        {
            if (ActiveLoadout == null) return string.Empty;
            return ActiveLoadout.GetSkinForPart(part);
        }

        /// <summary>Creates a new loadout with the given display name and returns it.</summary>
        public AircraftLoadout CreateLoadout(string name)
        {
            var loadout = new AircraftLoadout
            {
                loadoutId = Guid.NewGuid().ToString(),
                loadoutName = string.IsNullOrEmpty(name) ? "New Loadout" : name
            };
            _saveData.loadouts.Add(loadout);
            Save();
            return loadout;
        }

        /// <summary>
        /// Deletes the loadout with the given ID.
        /// Returns <c>false</c> if the loadout does not exist or is the last remaining one.
        /// </summary>
        public bool DeleteLoadout(string loadoutId)
        {
            if (string.IsNullOrEmpty(loadoutId)) return false;
            if (_saveData.loadouts.Count <= 1)
            {
                Debug.LogWarning("[AircraftCustomizationManager] Cannot delete the last loadout.");
                return false;
            }

            int idx = _saveData.loadouts.FindIndex(l => l.loadoutId == loadoutId);
            if (idx < 0) return false;

            _saveData.loadouts.RemoveAt(idx);

            if (_saveData.activeLoadoutId == loadoutId)
                SetActiveFromId(_saveData.loadouts[0].loadoutId);

            Save();
            return true;
        }

        /// <summary>
        /// Switches the active loadout to the one with the given ID.
        /// Returns <c>false</c> if not found.
        /// </summary>
        public bool SwitchLoadout(string loadoutId)
        {
            foreach (var loadout in _saveData.loadouts)
            {
                if (loadout.loadoutId == loadoutId)
                {
                    ActiveLoadout = loadout;
                    _saveData.activeLoadoutId = loadoutId;
                    OnLoadoutChanged?.Invoke(ActiveLoadout);
                    Save();
                    return true;
                }
            }
            return false;
        }

        /// <summary>Returns a copy of all saved loadouts.</summary>
        public List<AircraftLoadout> GetAllLoadouts()
        {
            return new List<AircraftLoadout>(_saveData.loadouts);
        }

        /// <summary>
        /// Toggles the favourite status of <paramref name="skinId"/>.
        /// </summary>
        public void ToggleFavorite(string skinId)
        {
            if (string.IsNullOrEmpty(skinId)) return;
            if (_saveData.favoriteSkins.Contains(skinId))
                _saveData.favoriteSkins.Remove(skinId);
            else
                _saveData.favoriteSkins.Add(skinId);
            Save();
        }

        /// <summary>Returns <c>true</c> if <paramref name="skinId"/> is a favourite.</summary>
        public bool IsFavorite(string skinId)
        {
            if (string.IsNullOrEmpty(skinId)) return false;
            return _saveData.favoriteSkins.Contains(skinId);
        }

        /// <summary>Persists the current customization data to disk as JSON.</summary>
        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(_saveData, true);
                File.WriteAllText(SaveFilePath, json);
                OnCustomizationSaved?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AircraftCustomizationManager] Save failed: {ex.Message}");
            }
        }

        /// <summary>Restores customization data from the JSON save file, or starts fresh.</summary>
        public void Load()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    string json = File.ReadAllText(SaveFilePath);
                    _saveData = JsonUtility.FromJson<AircraftCustomizationSaveData>(json)
                                ?? new AircraftCustomizationSaveData();
                }
                else
                {
                    _saveData = new AircraftCustomizationSaveData();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AircraftCustomizationManager] Load failed: {ex.Message}");
                _saveData = new AircraftCustomizationSaveData();
            }
        }
    }
}
