// TerminalGenerator.cs — Phase 113: Procedural City & Airport Generation
// Terminal building generation: gates, taxiways, aprons, control towers.
// Namespace: SWEF.ProceduralWorld

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Generates airport terminal buildings, control towers, aprons, and taxiways
    /// for a procedural airport based on its <see cref="AirportLayout"/>.
    /// </summary>
    public class TerminalGenerator : MonoBehaviour
    {
        // ── Inspector ───────────────────────────────────────────────────────────────────────────────
        [Header("Prefabs")]
        [SerializeField] private GameObject terminalPrefab;
        [SerializeField] private GameObject controlTowerPrefab;
        [SerializeField] private GameObject gateBridgePrefab;

        [Header("Scale")]
        [SerializeField] private float metresPerGate = 15f;
        [SerializeField] private float controlTowerHeight = 40f;

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly List<GameObject> _spawnedObjects = new List<GameObject>();

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Spawns all terminal structures for the given <paramref name="airport"/>
        /// under the supplied <paramref name="parent"/> transform.
        /// </summary>
        public void SpawnTerminal(AirportLayout airport, Transform parent)
        {
            ClearTerminal();
            SpawnMainBuilding(airport, parent);
            if (airport.hasControlTower) SpawnControlTower(airport, parent);
            SpawnGateBridges(airport, parent);
        }

        /// <summary>Removes all spawned terminal objects.</summary>
        public void ClearTerminal()
        {
            foreach (var go in _spawnedObjects)
                if (go != null) Destroy(go);
            _spawnedObjects.Clear();
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void SpawnMainBuilding(AirportLayout airport, Transform parent)
        {
            var prefab = terminalPrefab != null ? terminalPrefab : null;
            float width = airport.gateCount * metresPerGate;
            Vector3 pos = airport.referencePoint + Vector3.forward * 300f;

            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, pos, Quaternion.identity, parent);
                go.transform.localScale = new Vector3(width / 50f, 1f, 1f);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Terminal_{airport.icaoCode}";
                go.transform.SetParent(parent, false);
                go.transform.position = pos;
                go.transform.localScale = new Vector3(width, 12f, 60f);
            }
            _spawnedObjects.Add(go);
        }

        private void SpawnControlTower(AirportLayout airport, Transform parent)
        {
            var prefab = controlTowerPrefab;
            Vector3 pos = airport.referencePoint + new Vector3(80f, 0f, 200f);

            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, pos, Quaternion.identity, parent);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = $"Tower_{airport.icaoCode}";
                go.transform.SetParent(parent, false);
                go.transform.position = pos;
                go.transform.localScale = new Vector3(8f, controlTowerHeight * 0.5f, 8f);
            }
            _spawnedObjects.Add(go);
        }

        private void SpawnGateBridges(AirportLayout airport, Transform parent)
        {
            if (gateBridgePrefab == null || airport.gateCount == 0) return;
            float startX = airport.referencePoint.x - airport.gateCount * metresPerGate * 0.5f;
            for (int i = 0; i < airport.gateCount; i++)
            {
                float x = startX + i * metresPerGate + metresPerGate * 0.5f;
                Vector3 pos = new Vector3(x, airport.referencePoint.y, airport.referencePoint.z + 350f);
                var go = Instantiate(gateBridgePrefab, pos, Quaternion.identity, parent);
                _spawnedObjects.Add(go);
            }
        }
    }
}
