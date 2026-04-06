// AirportInfrastructure.cs — Phase 113: Procedural City & Airport Generation
// Support infrastructure: hangars, fuel depots, fire stations, parking areas, access roads.
// Namespace: SWEF.ProceduralWorld

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Generates airport support infrastructure including hangars, fuel depots,
    /// fire stations, parking areas, and perimeter access roads.
    /// </summary>
    public class AirportInfrastructure : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Infrastructure Prefabs")]
        [SerializeField] private GameObject hangarPrefab;
        [SerializeField] private GameObject fuelDepotPrefab;
        [SerializeField] private GameObject fireStationPrefab;

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly List<GameObject> _spawned = new List<GameObject>();

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Spawns all support infrastructure for the given airport layout.</summary>
        public void SpawnInfrastructure(AirportLayout airport, Transform parent)
        {
            ClearInfrastructure();
            SpawnHangars(airport, parent);
            SpawnFuelDepot(airport, parent);
            SpawnFireStation(airport, parent);
            SpawnParkingArea(airport, parent);
        }

        /// <summary>Removes all spawned infrastructure objects.</summary>
        public void ClearInfrastructure()
        {
            foreach (var go in _spawned)
                if (go != null) Destroy(go);
            _spawned.Clear();
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void SpawnHangars(AirportLayout airport, Transform parent)
        {
            int count = airport.airportType switch
            {
                AirportType.International => 6,
                AirportType.Military      => 8,
                AirportType.Regional      => 3,
                _                         => 1
            };

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = airport.referencePoint + new Vector3(-300f - i * 60f, 0f, -100f);
                var go = SpawnBox(pos, new Vector3(50f, 8f, 30f), $"Hangar_{i}", hangarPrefab, parent);
                _spawned.Add(go);
            }
        }

        private void SpawnFuelDepot(AirportLayout airport, Transform parent)
        {
            Vector3 pos = airport.referencePoint + new Vector3(250f, 0f, -150f);
            _spawned.Add(SpawnBox(pos, new Vector3(30f, 10f, 30f), "FuelDepot", fuelDepotPrefab, parent));
        }

        private void SpawnFireStation(AirportLayout airport, Transform parent)
        {
            Vector3 pos = airport.referencePoint + new Vector3(0f, 0f, -200f);
            _spawned.Add(SpawnBox(pos, new Vector3(25f, 6f, 15f), "FireStation", fireStationPrefab, parent));
        }

        private void SpawnParkingArea(AirportLayout airport, Transform parent)
        {
            Vector3 pos = airport.referencePoint + new Vector3(0f, 0f, 600f);
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "ParkingArea";
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(200f, 0.05f, 150f);
            _spawned.Add(go);
        }

        private static GameObject SpawnBox(Vector3 pos, Vector3 scale, string name, GameObject prefab, Transform parent)
        {
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, pos, Quaternion.identity, parent);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.SetParent(parent, false);
                go.transform.position = pos;
                go.transform.localScale = scale;
            }
            return go;
        }
    }
}
