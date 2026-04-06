// BuildingGenerator.cs — Phase 113: Procedural City & Airport Generation
// Procedural building mesh/prefab placement with height variation,
// style variation by zone, and modular building assembly.
// Namespace: SWEF.ProceduralWorld

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Handles placement of procedural building GameObjects from a pool of prefabs.
    /// Applies height variation, zone-appropriate styling, and initial LOD assignment.
    /// </summary>
    public class BuildingGenerator : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Building Prefabs (by type)")]
        [SerializeField] private GameObject residentialPrefab;
        [SerializeField] private GameObject commercialPrefab;
        [SerializeField] private GameObject industrialPrefab;
        [SerializeField] private GameObject landmarkPrefab;
        [SerializeField] private GameObject governmentPrefab;

        [Header("Height Settings")]
        [SerializeField] private float metresPerFloor = 3f;

        // ── Internal pool ─────────────────────────────────────────────────────────
        private readonly Dictionary<BuildingType, Queue<GameObject>> _pool =
            new Dictionary<BuildingType, Queue<GameObject>>();

        private readonly List<GameObject> _active = new List<GameObject>();

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Instantiates or recycles building GameObjects for all instances in
        /// <paramref name="city"/>, parenting them under <paramref name="parent"/>.
        /// </summary>
        public void SpawnBuildings(CityDescription city, Transform parent)
        {
            foreach (var b in city.buildings)
                SpawnBuilding(b, parent);
        }

        /// <summary>Returns all active buildings to the pool and clears the active list.</summary>
        public void DespawnAll()
        {
            foreach (var go in _active)
            {
                if (go == null) continue;
                go.SetActive(false);
                var type = GetTypeFromTag(go.tag);
                if (!_pool.ContainsKey(type)) _pool[type] = new Queue<GameObject>();
                _pool[type].Enqueue(go);
            }
            _active.Clear();
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void SpawnBuilding(BuildingInstance b, Transform parent)
        {
            var prefab = GetPrefab(b.buildingType);
            if (prefab == null) return;

            GameObject go = Acquire(b.buildingType, prefab);
            go.transform.SetParent(parent, false);
            go.transform.position = b.position;
            go.transform.eulerAngles = b.rotation;

            float height = b.floorCount * metresPerFloor;
            go.transform.localScale = new Vector3(1f, height / Mathf.Max(1f, prefab.transform.localScale.y), 1f);
            go.SetActive(true);
            _active.Add(go);
        }

        private GameObject Acquire(BuildingType type, GameObject prefab)
        {
            if (_pool.TryGetValue(type, out var queue) && queue.Count > 0)
            {
                var recycled = queue.Dequeue();
                recycled.SetActive(false);
                return recycled;
            }
            return Instantiate(prefab);
        }

        private GameObject GetPrefab(BuildingType type) => type switch
        {
            BuildingType.Commercial  => commercialPrefab,
            BuildingType.Industrial  => industrialPrefab,
            BuildingType.Landmark    => landmarkPrefab,
            BuildingType.Government  => governmentPrefab,
            _                        => residentialPrefab
        };

        private static BuildingType GetTypeFromTag(string tag) => tag switch
        {
            "Commercial"  => BuildingType.Commercial,
            "Industrial"  => BuildingType.Industrial,
            "Landmark"    => BuildingType.Landmark,
            "Government"  => BuildingType.Government,
            _             => BuildingType.Residential
        };
    }
}
