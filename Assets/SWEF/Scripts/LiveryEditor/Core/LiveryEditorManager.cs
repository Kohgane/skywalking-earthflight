// LiveryEditorManager.cs — Phase 115: Advanced Aircraft Livery Editor
// Central singleton manager. DontDestroyOnLoad.
// Namespace: SWEF.LiveryEditor

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Central singleton for the Advanced Aircraft Livery Editor.
    /// Manages livery creation, persistence, loading, and application to aircraft.
    /// Survives scene loads via <c>DontDestroyOnLoad</c>.
    /// </summary>
    public class LiveryEditorManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Shared singleton instance.</summary>
        public static LiveryEditorManager Instance { get; private set; }

        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Configuration")]
        [SerializeField] private LiveryEditorConfig config;

        [Header("Sub-Systems")]
        [SerializeField] private LiveryLayerManager layerManager;
        [SerializeField] private DecalLibrary decalLibrary;
        [SerializeField] private LiveryTemplateLibrary templateLibrary;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Currently active livery data being edited.</summary>
        public LiverySaveData ActiveLivery { get; private set; }

        /// <summary>Whether the editor is currently open.</summary>
        public bool IsEditorOpen { get; private set; }

        /// <summary>Runtime configuration (read-only access).</summary>
        public LiveryEditorConfig Config => config;

        /// <summary>Layer manager sub-system.</summary>
        public LiveryLayerManager LayerManager => layerManager;

        /// <summary>Decal library sub-system.</summary>
        public DecalLibrary DecalLibrary => decalLibrary;

        /// <summary>Template library sub-system.</summary>
        public LiveryTemplateLibrary TemplateLibrary => templateLibrary;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when a new livery is created.</summary>
        public event Action<LiverySaveData> OnLiveryCreated;

        /// <summary>Raised when a livery is saved to disk.</summary>
        public event Action<LiverySaveData> OnLiverySaved;

        /// <summary>Raised when a livery is loaded from disk.</summary>
        public event Action<LiverySaveData> OnLiveryLoaded;

        /// <summary>Raised when a livery is applied to an aircraft.</summary>
        public event Action<string, LiverySaveData> OnLiveryApplied;

        /// <summary>Raised when the editor is opened or closed.</summary>
        public event Action<bool> OnEditorToggled;

        // ── Internal state ────────────────────────────────────────────────────────
        private readonly List<LiverySaveData> _savedLiveries = new List<LiverySaveData>();
        private float _autoSaveTimer;

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
            Debug.Log("[SWEF] LiveryEditorManager: initialised.");
        }

        private void Update()
        {
            if (config == null || config.AutoSaveIntervalSeconds <= 0f) return;
            if (ActiveLivery == null || !IsEditorOpen) return;

            _autoSaveTimer += Time.deltaTime;
            if (_autoSaveTimer >= config.AutoSaveIntervalSeconds)
            {
                _autoSaveTimer = 0f;
                SaveActiveLivery();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API — Livery lifecycle ─────────────────────────────────────────

        /// <summary>
        /// Creates a new blank livery with default settings.
        /// </summary>
        /// <param name="liveryName">Display name for the new livery.</param>
        /// <param name="aircraftId">Compatible aircraft identifier.</param>
        /// <returns>The newly created <see cref="LiverySaveData"/>.</returns>
        public LiverySaveData CreateNewLivery(string liveryName, string aircraftId)
        {
            if (string.IsNullOrWhiteSpace(liveryName))
                throw new ArgumentException("Livery name cannot be empty.", nameof(liveryName));

            int resolution = config != null ? config.DefaultTextureResolution : 2048;

            var livery = new LiverySaveData
            {
                TextureResolution = resolution,
                Metadata = new LiveryMetadata
                {
                    LiveryId         = Guid.NewGuid().ToString(),
                    Name             = liveryName,
                    Author           = "Player",
                    CreatedAtUtc     = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    ModifiedAtUtc    = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    FormatVersion    = 1,
                    CompatibleAircraftIds = new List<string> { aircraftId }
                }
            };

            ActiveLivery = livery;
            _savedLiveries.Add(livery);
            OnLiveryCreated?.Invoke(livery);
            Debug.Log($"[SWEF] LiveryEditorManager: created livery '{liveryName}' for aircraft '{aircraftId}'.");
            return livery;
        }

        /// <summary>
        /// Saves the currently active livery.
        /// </summary>
        public void SaveActiveLivery()
        {
            if (ActiveLivery == null)
            {
                Debug.LogWarning("[SWEF] LiveryEditorManager: no active livery to save.");
                return;
            }

            ActiveLivery.Metadata.ModifiedAtUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            OnLiverySaved?.Invoke(ActiveLivery);
            Debug.Log($"[SWEF] LiveryEditorManager: saved livery '{ActiveLivery.Metadata.Name}'.");
        }

        /// <summary>
        /// Loads a livery from a <see cref="LiverySaveData"/> instance and sets it as active.
        /// </summary>
        /// <param name="livery">Livery data to load.</param>
        public void LoadLivery(LiverySaveData livery)
        {
            if (livery == null) throw new ArgumentNullException(nameof(livery));

            ActiveLivery = livery;
            OnLiveryLoaded?.Invoke(livery);
            Debug.Log($"[SWEF] LiveryEditorManager: loaded livery '{livery.Metadata.Name}'.");
        }

        /// <summary>
        /// Applies the active livery to a specific aircraft.
        /// </summary>
        /// <param name="aircraftId">Target aircraft identifier.</param>
        public void ApplyActiveLivery(string aircraftId)
        {
            if (ActiveLivery == null)
            {
                Debug.LogWarning("[SWEF] LiveryEditorManager: no active livery to apply.");
                return;
            }

            OnLiveryApplied?.Invoke(aircraftId, ActiveLivery);
            Debug.Log($"[SWEF] LiveryEditorManager: applied livery '{ActiveLivery.Metadata.Name}' to '{aircraftId}'.");
        }

        /// <summary>
        /// Opens or closes the livery editor UI.
        /// </summary>
        /// <param name="open">Pass <c>true</c> to open, <c>false</c> to close.</param>
        public void SetEditorOpen(bool open)
        {
            IsEditorOpen = open;
            _autoSaveTimer = 0f;
            OnEditorToggled?.Invoke(open);
        }

        /// <summary>Returns all liveries saved in this session.</summary>
        public IReadOnlyList<LiverySaveData> GetAllLiveries() => _savedLiveries.AsReadOnly();

        /// <summary>
        /// Deletes a livery from the in-memory catalogue.
        /// </summary>
        /// <param name="liveryId">Unique identifier of the livery to delete.</param>
        /// <returns><c>true</c> if the livery was found and removed.</returns>
        public bool DeleteLivery(string liveryId)
        {
            int idx = _savedLiveries.FindIndex(l => l.Metadata.LiveryId == liveryId);
            if (idx < 0) return false;

            if (ActiveLivery?.Metadata.LiveryId == liveryId) ActiveLivery = null;
            _savedLiveries.RemoveAt(idx);
            return true;
        }
    }
}
