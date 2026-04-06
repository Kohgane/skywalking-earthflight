// ProceduralWorldManager.cs — Phase 113: Procedural City & Airport Generation
// Central singleton that orchestrates all procedural city and airport generation.
// DontDestroyOnLoad. Namespace: SWEF.ProceduralWorld

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Central singleton manager for the Procedural City &amp; Airport Generation system.
    /// Orchestrates city generation, airport generation, terrain analysis, LOD management,
    /// and world streaming. Survives scene loads via <c>DontDestroyOnLoad</c>.
    /// </summary>
    public class ProceduralWorldManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Shared singleton instance.</summary>
        public static ProceduralWorldManager Instance { get; private set; }

        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Configuration")]
        [SerializeField] private ProceduralWorldConfig config;

        [Header("Sub-Systems")]
        [SerializeField] private CityGenerator cityGenerator;
        [SerializeField] private AirportGenerator airportGenerator;
        [SerializeField] private WorldStreamer worldStreamer;
        [SerializeField] private TerrainAnalyzer terrainAnalyzer;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Current generation state of the world system.</summary>
        public GenerationState CurrentState { get; private set; } = GenerationState.Idle;

        /// <summary>Active runtime configuration.</summary>
        public ProceduralWorldConfig Config => config;

        /// <summary>All cities currently loaded in the world.</summary>
        public IReadOnlyList<CityDescription> ActiveCities => _activeCities.AsReadOnly();

        /// <summary>All airports currently loaded in the world.</summary>
        public IReadOnlyList<AirportLayout> ActiveAirports => _activeAirports.AsReadOnly();

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when a city has finished generating.</summary>
        public event Action<CityDescription> OnCityGenerated;

        /// <summary>Raised when an airport has finished generating.</summary>
        public event Action<AirportLayout> OnAirportGenerated;

        /// <summary>Raised when the generation state changes.</summary>
        public event Action<GenerationState> OnStateChanged;

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly List<CityDescription> _activeCities = new List<CityDescription>();
        private readonly List<AirportLayout> _activeAirports = new List<AirportLayout>();
        private readonly HashSet<ChunkCoord> _loadedChunks = new HashSet<ChunkCoord>();

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

            if (config == null)
                config = ScriptableObject.CreateInstance<ProceduralWorldConfig>();
        }

        private void Start()
        {
            TransitionToState(GenerationState.Idle);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Begins generation of a city at the specified world position using the given seed.
        /// </summary>
        /// <param name="worldPosition">Centre of the area to generate.</param>
        /// <param name="seed">Deterministic seed — same seed produces the same city.</param>
        public void GenerateCity(Vector3 worldPosition, int seed)
        {
            if (cityGenerator != null)
                StartCoroutine(GenerateCityCoroutine(worldPosition, seed));
        }

        /// <summary>
        /// Begins generation of an airport at the specified world position.
        /// </summary>
        /// <param name="worldPosition">Reference point for the airport.</param>
        /// <param name="airportType">Type of airport to generate.</param>
        /// <param name="seed">Deterministic seed.</param>
        public void GenerateAirport(Vector3 worldPosition, AirportType airportType, int seed)
        {
            if (airportGenerator != null)
                StartCoroutine(GenerateAirportCoroutine(worldPosition, airportType, seed));
        }

        /// <summary>Clears all active cities and airports and resets to <see cref="GenerationState.Idle"/>.</summary>
        public void ClearAll()
        {
            _activeCities.Clear();
            _activeAirports.Clear();
            _loadedChunks.Clear();
            TransitionToState(GenerationState.Idle);
        }

        /// <summary>Returns true if the specified chunk coordinate is currently loaded.</summary>
        public bool IsChunkLoaded(ChunkCoord coord) => _loadedChunks.Contains(coord);

        /// <summary>Registers a chunk as loaded (called by <see cref="WorldStreamer"/>).</summary>
        internal void RegisterChunk(ChunkCoord coord) => _loadedChunks.Add(coord);

        /// <summary>Unregisters a chunk (called by <see cref="WorldStreamer"/>).</summary>
        internal void UnregisterChunk(ChunkCoord coord) => _loadedChunks.Remove(coord);

        // ── Internal helpers ──────────────────────────────────────────────────────

        private IEnumerator GenerateCityCoroutine(Vector3 position, int seed)
        {
            TransitionToState(GenerationState.AnalyzingTerrain);
            yield return null;

            TransitionToState(GenerationState.GeneratingLayout);
            var city = cityGenerator != null
                ? cityGenerator.Generate(position, seed, config)
                : FallbackCity(position, seed);
            yield return null;

            TransitionToState(GenerationState.PlacingObjects);
            yield return null;

            TransitionToState(GenerationState.ConfiguringLOD);
            yield return null;

            _activeCities.Add(city);
            TransitionToState(GenerationState.Complete);
            OnCityGenerated?.Invoke(city);
        }

        private IEnumerator GenerateAirportCoroutine(Vector3 position, AirportType airportType, int seed)
        {
            TransitionToState(GenerationState.GeneratingLayout);
            yield return null;

            var airport = airportGenerator != null
                ? airportGenerator.Generate(position, airportType, seed, config)
                : FallbackAirport(position, airportType, seed);
            yield return null;

            TransitionToState(GenerationState.PlacingObjects);
            yield return null;

            _activeAirports.Add(airport);
            TransitionToState(GenerationState.Complete);
            OnAirportGenerated?.Invoke(airport);
        }

        private void TransitionToState(GenerationState newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        private static CityDescription FallbackCity(Vector3 position, int seed)
        {
            var rng = new System.Random(seed);
            return new CityDescription
            {
                seed = seed,
                cityName = $"City_{seed}",
                cityType = CityType.Town,
                centre = position,
                radiusMetres = 500f,
                population = rng.Next(1000, 50000)
            };
        }

        private static AirportLayout FallbackAirport(Vector3 position, AirportType type, int seed)
        {
            return new AirportLayout
            {
                icaoCode = $"PW{seed % 100:D2}",
                airportName = $"Airport_{seed}",
                airportType = type,
                referencePoint = position,
                elevationMetres = position.y,
                gateCount = type == AirportType.International ? 20 : 4,
                hasControlTower = type != AirportType.Helipad
            };
        }
    }
}
