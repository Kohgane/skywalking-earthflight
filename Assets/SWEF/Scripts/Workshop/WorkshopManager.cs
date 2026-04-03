// WorkshopManager.cs — SWEF Aircraft Workshop & Part Customization (Phase 90)
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.Workshop
{
    /// <summary>
    /// Singleton MonoBehaviour that acts as the central coordinator for the
    /// Aircraft Workshop system.  Responsible for:
    /// <list type="bullet">
    ///   <item>Opening and closing workshop mode.</item>
    ///   <item>Loading and saving named aircraft builds (JSON persistence).</item>
    ///   <item>Applying an active build to the live aircraft (updating flight performance).</item>
    ///   <item>Delegating part inventory and unlock operations.</item>
    /// </list>
    ///
    /// Builds are persisted to
    /// <c>Application.persistentDataPath/workshop_builds.json</c>.
    /// </summary>
    public class WorkshopManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        /// <summary>Global singleton instance.</summary>
        public static WorkshopManager Instance { get; private set; }

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Raised whenever the active build is modified (part equipped/unequipped, paint changed, etc.).</summary>
        public event Action<AircraftBuildData> OnBuildChanged;

        /// <summary>Raised when a part is equipped into the active build.</summary>
        public event Action<AircraftPartData> OnPartEquipped;

        /// <summary>Raised when a part is removed from the active build.</summary>
        public event Action<AircraftPartData> OnPartUnequipped;

        /// <summary>Raised when the workshop UI is opened.</summary>
        public event Action OnWorkshopOpened;

        /// <summary>Raised when the workshop UI is closed.</summary>
        public event Action OnWorkshopClosed;

        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Persistence")]
        [Tooltip("File name written to Application.persistentDataPath for saved builds.")]
        [SerializeField] private string _buildsFileName = "workshop_builds.json";

        [Header("References")]
        [Tooltip("PartInventoryController instance.  Auto-found if null.")]
        [SerializeField] private PartInventoryController _inventory;

        [Tooltip("PartUnlockTree instance.  Auto-found if null.")]
        [SerializeField] private PartUnlockTree _unlockTree;

        // ── State ──────────────────────────────────────────────────────────────

        private readonly List<AircraftBuildData> _savedBuilds = new List<AircraftBuildData>();

        /// <summary>The build currently loaded in the Workshop editor.</summary>
        public AircraftBuildData ActiveBuild { get; private set; }

        /// <summary>Whether the Workshop mode is currently active.</summary>
        public bool IsOpen { get; private set; }

        private string SavePath => Path.Combine(Application.persistentDataPath, _buildsFileName);

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

            ResolveReferences();
            LoadBuilds();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Workshop Mode ──────────────────────────────────────────────────────

        /// <summary>
        /// Opens the workshop, optionally loading a specific saved build into the editor.
        /// Fires <see cref="OnWorkshopOpened"/>.
        /// </summary>
        /// <param name="buildId">
        /// Optional build ID to load.  If <c>null</c> or not found a new empty build
        /// is created automatically.
        /// </param>
        public void OpenWorkshop(string buildId = null)
        {
            if (IsOpen)
            {
                Debug.LogWarning("[SWEF] Workshop: OpenWorkshop called while workshop is already open.");
                return;
            }

            IsOpen = true;

            if (!string.IsNullOrEmpty(buildId))
                LoadBuildById(buildId);
            else if (ActiveBuild == null)
                ActiveBuild = new AircraftBuildData();

            WorkshopAnalytics.RecordWorkshopOpened();
            OnWorkshopOpened?.Invoke();
        }

        /// <summary>
        /// Closes the workshop.  Any unsaved changes to the active build are
        /// discarded.  Fires <see cref="OnWorkshopClosed"/>.
        /// </summary>
        public void CloseWorkshop()
        {
            if (!IsOpen)
            {
                Debug.LogWarning("[SWEF] Workshop: CloseWorkshop called while workshop is not open.");
                return;
            }

            IsOpen = false;
            WorkshopAnalytics.RecordWorkshopClosed();
            OnWorkshopClosed?.Invoke();
        }

        // ── Build Management ───────────────────────────────────────────────────

        /// <summary>
        /// Saves the currently active build (or a provided build) to the
        /// persistent save file.  A new entry is added if the build ID is new;
        /// an existing entry is overwritten.
        /// </summary>
        /// <param name="build">Build to save.  Defaults to <see cref="ActiveBuild"/> if <c>null</c>.</param>
        public void SaveBuild(AircraftBuildData build = null)
        {
            var target = build ?? ActiveBuild;
            if (target == null)
            {
                Debug.LogWarning("[SWEF] Workshop: SaveBuild called but no active build exists.");
                return;
            }

            target.lastSavedUtc = DateTime.UtcNow.ToString("o");

            int idx = _savedBuilds.FindIndex(b => b.buildId == target.buildId);
            if (idx >= 0)
                _savedBuilds[idx] = target;
            else
                _savedBuilds.Add(target);

            PersistBuilds();
            WorkshopAnalytics.RecordBuildSaved(target.buildId);
        }

        /// <summary>
        /// Loads a saved build by ID and sets it as the <see cref="ActiveBuild"/>.
        /// </summary>
        /// <param name="buildId">The build ID to load.</param>
        /// <returns><c>true</c> if found and loaded; <c>false</c> otherwise.</returns>
        public bool LoadBuildById(string buildId)
        {
            if (string.IsNullOrEmpty(buildId))
            {
                Debug.LogWarning("[SWEF] Workshop: LoadBuildById called with empty buildId.");
                return false;
            }

            var found = _savedBuilds.Find(b => b.buildId == buildId);
            if (found == null)
            {
                Debug.LogWarning($"[SWEF] Workshop: Build '{buildId}' not found.");
                return false;
            }

            ActiveBuild = found;
            WorkshopAnalytics.RecordBuildLoaded(buildId);
            OnBuildChanged?.Invoke(ActiveBuild);
            return true;
        }

        /// <summary>Returns a snapshot of all saved builds.</summary>
        public IReadOnlyList<AircraftBuildData> GetAllBuilds() => _savedBuilds;

        // ── Part Equip / Unequip ───────────────────────────────────────────────

        /// <summary>
        /// Equips a part into the active build, replacing any existing part of the
        /// same <see cref="AircraftPartType"/>.
        /// </summary>
        /// <param name="part">Part to equip.</param>
        public void EquipPart(AircraftPartData part)
        {
            if (ActiveBuild == null)
            {
                Debug.LogWarning("[SWEF] Workshop: EquipPart called but no active build exists.");
                return;
            }
            if (part == null)
            {
                Debug.LogWarning("[SWEF] Workshop: EquipPart called with null part.");
                return;
            }
            if (!part.isUnlocked)
            {
                Debug.LogWarning($"[SWEF] Workshop: EquipPart — part '{part.partId}' is not yet unlocked.");
                return;
            }

            // Remove any existing part of the same type first.
            UnequipPartByType(part.partType, silent: true);

            ActiveBuild.equippedPartIds.Add(part.partId);
            RefreshCachedStats();
            WorkshopAnalytics.RecordPartEquipped(part.partId, part.partType.ToString());
            OnPartEquipped?.Invoke(part);
            OnBuildChanged?.Invoke(ActiveBuild);
        }

        /// <summary>
        /// Removes the currently equipped part of the given type from the active build.
        /// </summary>
        /// <param name="type">Part slot to clear.</param>
        public void UnequipPartByType(AircraftPartType type)
        {
            UnequipPartByType(type, silent: false);
        }

        // ── Apply Build ────────────────────────────────────────────────────────

        /// <summary>
        /// Applies the active build to the live aircraft by refreshing all
        /// cached performance stats and notifying subscribers.
        /// </summary>
        public void ApplyActiveBuild()
        {
            if (ActiveBuild == null)
            {
                Debug.LogWarning("[SWEF] Workshop: ApplyActiveBuild called but no active build exists.");
                return;
            }

            RefreshCachedStats();
            OnBuildChanged?.Invoke(ActiveBuild);
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private void ResolveReferences()
        {
            if (_inventory == null)
                _inventory = FindFirstObjectByType<PartInventoryController>();
            if (_unlockTree == null)
                _unlockTree = FindFirstObjectByType<PartUnlockTree>();
        }

        private void UnequipPartByType(AircraftPartType type, bool silent)
        {
            if (ActiveBuild == null) return;

            string removedId = null;
            for (int i = ActiveBuild.equippedPartIds.Count - 1; i >= 0; i--)
            {
                var partData = _inventory?.GetPartById(ActiveBuild.equippedPartIds[i]);
                if (partData != null && partData.partType == type)
                {
                    removedId = ActiveBuild.equippedPartIds[i];
                    ActiveBuild.equippedPartIds.RemoveAt(i);
                    break;
                }
            }

            if (!silent && removedId != null)
            {
                var unequipped = _inventory?.GetPartById(removedId);
                if (unequipped != null)
                {
                    WorkshopAnalytics.RecordPartUnequipped(removedId, unequipped.partType.ToString());
                    OnPartUnequipped?.Invoke(unequipped);
                    OnBuildChanged?.Invoke(ActiveBuild);
                }
            }
        }

        private void RefreshCachedStats()
        {
            if (ActiveBuild == null || _inventory == null) return;

            ActiveBuild.cachedMaxSpeed           = PerformanceSimulator.ComputeMaxSpeed(ActiveBuild, _inventory);
            ActiveBuild.cachedClimbRate          = PerformanceSimulator.ComputeClimbRate(ActiveBuild, _inventory);
            ActiveBuild.cachedManeuverability    = PerformanceSimulator.ComputeManeuverability(ActiveBuild, _inventory);
            ActiveBuild.cachedFuelEfficiency     = PerformanceSimulator.ComputeFuelEfficiency(ActiveBuild, _inventory);
            ActiveBuild.cachedStructuralIntegrity = PerformanceSimulator.ComputeStructuralIntegrity(ActiveBuild, _inventory);
            ActiveBuild.cachedWeightBalance      = PerformanceSimulator.ComputeWeightBalance(ActiveBuild, _inventory);
        }

        private void PersistBuilds()
        {
            try
            {
                var wrapper = new BuildsWrapper { builds = _savedBuilds };
                File.WriteAllText(SavePath, JsonUtility.ToJson(wrapper, prettyPrint: true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Workshop: Failed to save builds — {ex.Message}");
            }
        }

        private void LoadBuilds()
        {
            if (!File.Exists(SavePath)) return;

            try
            {
                string json = File.ReadAllText(SavePath);
                var wrapper = JsonUtility.FromJson<BuildsWrapper>(json);
                _savedBuilds.Clear();
                if (wrapper?.builds != null) _savedBuilds.AddRange(wrapper.builds);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Workshop: Failed to load builds — {ex.Message}");
            }
        }

        // ── Serialisation helper ───────────────────────────────────────────────

        [Serializable]
        private class BuildsWrapper
        {
            public List<AircraftBuildData> builds = new List<AircraftBuildData>();
        }
    }
}
