using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.CityGen
{
    /// <summary>
    /// Phase 52 — Central singleton that manages procedural city and landmark generation
    /// for Skywalking Earthflight.
    ///
    /// <para>Attach to a persistent GameObject in the bootstrap scene.
    /// The manager streams settlements in and out of the world based on camera proximity
    /// using a seed-based deterministic placement algorithm.</para>
    ///
    /// <para>Integration points:
    /// <list type="bullet">
    ///   <item><c>SWEF.Ocean.OceanManager.IsPositionUnderwater</c> — avoids water.</item>
    ///   <item><c>SWEF.Narration</c> — landmark discovery triggers narration.</item>
    ///   <item><see cref="CityGenSettings"/> — all runtime tuning lives here.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class CityManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static CityManager Instance { get; private set; }

        #endregion

        #region Constants

        private const float StreamingCheckInterval = 2f;   // seconds between streaming ticks
        private const float MinFlatSlopeAngle      = 15f;  // terrain slope threshold (degrees)
        private const int   MaxCandidatesPerSeed   = 64;   // candidate positions checked per frame

        #endregion

        #region Inspector

        [Header("Settings")]
        [SerializeField] private CityGenSettings settings = new CityGenSettings();

        [Header("References")]
        [Tooltip("Camera used for proximity culling. Resolved at runtime if null.")]
        [SerializeField] private Camera streamingCamera;

        [Tooltip("Layout generator used to build city block data. Resolved at runtime if null.")]
        [SerializeField] private CityLayoutGenerator layoutGenerator;

        [Tooltip("Building generator. Resolved at runtime if null.")]
        [SerializeField] private ProceduralBuildingGenerator buildingGenerator;

        [Tooltip("Road renderer. Resolved at runtime if null.")]
        [SerializeField] private RoadNetworkRenderer roadRenderer;

        [Tooltip("Landmark placer. Resolved at runtime if null.")]
        [SerializeField] private LandmarkPlacer landmarkPlacer;

        [Header("Prototype Settlement Definitions")]
        [Tooltip("Catalogue of settlement archetypes used during procedural placement.")]
        [SerializeField] private List<SettlementDefinition> settlementCatalogue = new List<SettlementDefinition>();

        #endregion

        #region Events

        /// <summary>Fired after a city is loaded and its geometry instantiated.</summary>
        public event Action<SettlementDefinition, Vector3> OnCityLoaded;

        /// <summary>Fired when a city is streamed out and its geometry destroyed.</summary>
        public event Action<SettlementDefinition, Vector3> OnCityUnloaded;

        /// <summary>Fired the first time the player approaches a landmark.</summary>
        public event Action<LandmarkDefinition> OnLandmarkDiscovered;

        #endregion

        #region Public Properties

        /// <summary>Read-only view of every currently loaded settlement.</summary>
        public IReadOnlyList<ActiveSettlement> ActiveSettlements => _activeSettlements;

        /// <summary>Exposes the runtime settings object.</summary>
        public CityGenSettings Settings => settings;

        #endregion

        #region Internal Types

        /// <summary>Tracks a settlement that has been placed and loaded into the world.</summary>
        public sealed class ActiveSettlement
        {
            public SettlementDefinition definition;
            public Vector3              worldCenter;
            public int                  seed;
            public GameObject           rootObject;
            public CityLayout           layout;
            public bool                 isFullyLoaded;
        }

        #endregion

        #region Private State

        private readonly List<ActiveSettlement> _activeSettlements = new List<ActiveSettlement>();
        private Coroutine _streamingCoroutine;
        private System.Random _rng;

        // Optional runtime references
        private Ocean.OceanManager _oceanManager;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _rng = new System.Random(settings.generationSeed);
        }

        private void Start()
        {
            ResolveReferences();
            _streamingCoroutine = StartCoroutine(StreamingLoop());
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Public API

        /// <summary>Returns the nearest loaded settlement to <paramref name="pos"/>, or <c>null</c>.</summary>
        public ActiveSettlement GetNearestSettlement(Vector3 pos)
        {
            ActiveSettlement nearest = null;
            float bestSqr = float.MaxValue;
            foreach (var s in _activeSettlements)
            {
                float sqr = (s.worldCenter - pos).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; nearest = s; }
            }
            return nearest;
        }

        /// <summary>Returns all registered landmarks within <paramref name="radius"/> of <paramref name="pos"/>.</summary>
        public List<LandmarkDefinition> GetLandmarksInRadius(Vector3 pos, float radius)
        {
            if (landmarkPlacer == null) return new List<LandmarkDefinition>();
            return landmarkPlacer.GetLandmarksInRadius(pos, radius);
        }

        /// <summary>Immediately unloads all active settlements and reloads with the current seed.</summary>
        public void Rebuild()
        {
            UnloadAll();
            _rng = new System.Random(settings.generationSeed);
        }

        #endregion

        #region Streaming

        private IEnumerator StreamingLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(StreamingCheckInterval);
                if (streamingCamera == null) continue;

                Vector3 camPos = streamingCamera.transform.position;
                UnloadDistantSettlements(camPos);
                TryLoadNearbySettlements(camPos);
            }
        }

        private void TryLoadNearbySettlements(Vector3 camPos)
        {
            if (_activeSettlements.Count >= settings.maxVisibleCities) return;
            if (settlementCatalogue == null || settlementCatalogue.Count == 0) return;

            // Generate deterministic candidate positions around the camera.
            int toGenerate = settings.maxVisibleCities - _activeSettlements.Count;
            int catalogCount = settlementCatalogue.Count;

            for (int i = 0; i < toGenerate; i++)
            {
                Vector3 candidate = GenerateCandidatePosition(camPos, i);
                if (!IsValidPlacement(candidate)) continue;
                if (IsAlreadyLoaded(candidate)) continue;

                SettlementDefinition def = settlementCatalogue[_rng.Next(catalogCount)];
                LoadSettlement(def, candidate);
            }
        }

        private Vector3 GenerateCandidatePosition(Vector3 camPos, int index)
        {
            // Incorporate camera position into angle offset so placement varies
            // as the player moves around the world.
            float posHash = (camPos.x * 0.031f + camPos.z * 0.017f);
            float angle   = (index * (360f / settings.maxVisibleCities) + posHash) * Mathf.Deg2Rad;
            float radius  = 800f + (index % 3) * 400f;
            return new Vector3(
                camPos.x + Mathf.Cos(angle) * radius,
                0f,
                camPos.z + Mathf.Sin(angle) * radius
            );
        }

        private bool IsValidPlacement(Vector3 pos)
        {
            // Reject positions under water.
            if (_oceanManager != null && _oceanManager.IsPositionUnderwater(pos))
                return false;

            return true;
        }

        private bool IsAlreadyLoaded(Vector3 pos)
        {
            const float MinSeparation = 400f;
            float sqr = MinSeparation * MinSeparation;
            foreach (var s in _activeSettlements)
                if ((s.worldCenter - pos).sqrMagnitude < sqr) return true;
            return false;
        }

        private void LoadSettlement(SettlementDefinition def, Vector3 center)
        {
            int localSeed = settings.generationSeed ^ (int)(center.x * 31) ^ (int)(center.z * 17);

            CityLayout layout = null;
            if (layoutGenerator != null)
                layout = layoutGenerator.GenerateLayout(def, center, localSeed);

            var root = new GameObject($"Settlement_{def.settlementType}_{_activeSettlements.Count}");
            root.transform.position = center;

            var active = new ActiveSettlement
            {
                definition    = def,
                worldCenter   = center,
                seed          = localSeed,
                rootObject    = root,
                layout        = layout,
                isFullyLoaded = true
            };
            _activeSettlements.Add(active);

            if (layout != null)
            {
                if (buildingGenerator != null)
                    buildingGenerator.GenerateSettlement(layout, root.transform);
                if (roadRenderer != null)
                    roadRenderer.RenderNetwork(layout.roadNetwork, root.transform);
            }

            OnCityLoaded?.Invoke(def, center);
        }

        private void UnloadDistantSettlements(Vector3 camPos)
        {
            float unloadSqr = settings.landmarkRenderDistance * 2f;
            unloadSqr *= unloadSqr;

            for (int i = _activeSettlements.Count - 1; i >= 0; i--)
            {
                var s = _activeSettlements[i];
                if ((s.worldCenter - camPos).sqrMagnitude > unloadSqr)
                {
                    if (s.rootObject != null) Destroy(s.rootObject);
                    _activeSettlements.RemoveAt(i);
                    OnCityUnloaded?.Invoke(s.definition, s.worldCenter);
                }
            }
        }

        private void UnloadAll()
        {
            for (int i = _activeSettlements.Count - 1; i >= 0; i--)
            {
                var s = _activeSettlements[i];
                if (s.rootObject != null) Destroy(s.rootObject);
                OnCityUnloaded?.Invoke(s.definition, s.worldCenter);
            }
            _activeSettlements.Clear();
        }

        #endregion

        #region Reference Resolution

        private void ResolveReferences()
        {
            if (streamingCamera == null)
                streamingCamera = Camera.main;

            if (layoutGenerator == null)
                layoutGenerator = FindFirstObjectByType<CityLayoutGenerator>();

            if (buildingGenerator == null)
                buildingGenerator = FindFirstObjectByType<ProceduralBuildingGenerator>();

            if (roadRenderer == null)
                roadRenderer = FindFirstObjectByType<RoadNetworkRenderer>();

            if (landmarkPlacer == null)
                landmarkPlacer = FindFirstObjectByType<LandmarkPlacer>();

            // Optional Ocean integration.
            _oceanManager = Ocean.OceanManager.Instance;
            if (_oceanManager == null)
                _oceanManager = FindFirstObjectByType<Ocean.OceanManager>();

            // Default settlement catalogue.
            if (settlementCatalogue.Count == 0)
                PopulateDefaultCatalogue();
        }

        private void PopulateDefaultCatalogue()
        {
            settlementCatalogue.Add(new SettlementDefinition
            {
                settlementType  = SettlementType.City,
                minPopulation   = 100000,
                maxPopulation   = 1000000,
                areaRadius      = 600f,
                buildingDensity = 0.5f,
                roadDensity     = 0.4f,
                architectureStyle = ArchitectureStyle.Modern
            });
            settlementCatalogue.Add(new SettlementDefinition
            {
                settlementType  = SettlementType.Town,
                minPopulation   = 5000,
                maxPopulation   = 50000,
                areaRadius      = 250f,
                buildingDensity = 0.35f,
                roadDensity     = 0.3f,
                architectureStyle = ArchitectureStyle.Classical
            });
            settlementCatalogue.Add(new SettlementDefinition
            {
                settlementType  = SettlementType.Village,
                minPopulation   = 200,
                maxPopulation   = 5000,
                areaRadius      = 100f,
                buildingDensity = 0.2f,
                roadDensity     = 0.2f,
                architectureStyle = ArchitectureStyle.Classical
            });
        }

        #endregion
    }
}
