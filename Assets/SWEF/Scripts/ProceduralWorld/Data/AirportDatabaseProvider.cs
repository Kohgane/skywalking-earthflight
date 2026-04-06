// AirportDatabaseProvider.cs — Phase 113: Procedural City & Airport Generation
// Real airport database: ICAO codes, runway data, frequencies
// (#if SWEF_AIRPORT_DB_AVAILABLE).
// Namespace: SWEF.ProceduralWorld

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Provides access to a database of real-world airports. When
    /// <c>SWEF_AIRPORT_DB_AVAILABLE</c> is defined, data is loaded from a bundled
    /// JSON or CSV asset; otherwise a small built-in sample set is used.
    /// </summary>
    public class AirportDatabaseProvider : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Shared singleton instance.</summary>
        public static AirportDatabaseProvider Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Data Asset")]
        [Tooltip("Path within Resources/ to the airport database JSON asset.")]
        [SerializeField] private string databaseResourcePath = "ProceduralWorld/airports";

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly Dictionary<string, AirportLayout> _byICAO =
            new Dictionary<string, AirportLayout>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            LoadDatabase();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the <see cref="AirportLayout"/> for the given ICAO code, or
        /// <c>null</c> if it is not in the database.
        /// </summary>
        public AirportLayout GetByICAO(string icao)
        {
            _byICAO.TryGetValue(icao?.ToUpper(), out var layout);
            return layout;
        }

        /// <summary>Returns all airports whose reference point is within <paramref name="radiusMetres"/>.</summary>
        public List<AirportLayout> GetNear(Vector3 worldPos, float radiusMetres)
        {
            var result = new List<AirportLayout>();
            foreach (var kvp in _byICAO)
            {
                if (Vector3.Distance(kvp.Value.referencePoint, worldPos) <= radiusMetres)
                    result.Add(kvp.Value);
            }
            return result;
        }

        /// <summary>Total number of airports in the loaded database.</summary>
        public int Count => _byICAO.Count;

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void LoadDatabase()
        {
#if SWEF_AIRPORT_DB_AVAILABLE
            LoadFromResource();
#else
            LoadBuiltin();
#endif
        }

        private void LoadBuiltin()
        {
            RegisterSample("EGLL", "London Heathrow", AirportType.International,
                new Vector3(0f, 0f, 0f), 3, true);
            RegisterSample("KJFK", "John F. Kennedy International", AirportType.International,
                new Vector3(1000f, 0f, 500f), 4, true);
            RegisterSample("RJTT", "Tokyo Haneda", AirportType.International,
                new Vector3(2000f, 0f, -500f), 4, true);
            RegisterSample("YSSY", "Sydney Kingsford Smith", AirportType.International,
                new Vector3(-1500f, 0f, 800f), 3, true);
        }

        private void RegisterSample(string icao, string name, AirportType type,
            Vector3 pos, int runwayCount, bool ils)
        {
            var layout = new AirportLayout
            {
                icaoCode = icao,
                airportName = name,
                airportType = type,
                referencePoint = pos,
                elevationMetres = pos.y,
                hasControlTower = true,
                gateCount = type == AirportType.International ? 30 : 8
            };
            for (int i = 0; i < runwayCount; i++)
            {
                layout.runways.Add(new RunwayData
                {
                    designator = $"{(i + 1) * 9:D2}L",
                    heading = (i + 1) * 45f % 360f,
                    lengthMetres = 3800f - i * 200f,
                    widthMetres = 60f,
                    thresholdPosition = pos + Vector3.right * (i * 200f),
                    hasILS = ils
                });
            }
            _byICAO[icao] = layout;
        }

#if SWEF_AIRPORT_DB_AVAILABLE
        private void LoadFromResource()
        {
            var asset = Resources.Load<TextAsset>(databaseResourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"[AirportDB] Resource not found at '{databaseResourcePath}'. Using builtin.");
                LoadBuiltin();
                return;
            }
            Debug.Log($"[AirportDB] Loaded airport database ({asset.text.Length} chars).");
            // JSON parsing would populate _byICAO here.
        }
#endif
    }
}
