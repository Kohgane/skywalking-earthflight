using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Wildlife
{
    /// <summary>
    /// Phase 75 — Handles placement, instantiation, pooling, and despawning of wildlife groups.
    /// Uses a per-category object pool to minimise allocations.
    /// </summary>
    public class WildlifeSpawnSystem : MonoBehaviour
    {
        #region Inspector

        [Header("Pooling")]
        [Tooltip("Pre-warm pool size per category on startup.")]
        [SerializeField] private int prewarmPoolSize = 5;

        [Header("Migration")]
        [Tooltip("Seasonal waypoint lists for migratory species (set at design time).")]
        [SerializeField] private List<MigrationPath> migrationPaths = new List<MigrationPath>();

        #endregion

        #region Private State

        private readonly Dictionary<WildlifeCategory, Queue<GameObject>> _pool =
            new Dictionary<WildlifeCategory, Queue<GameObject>>();

        private readonly Dictionary<string, GameObject> _activeGroupObjects =
            new Dictionary<string, GameObject>();

        private int _totalIndividuals;
        private Transform _poolRoot;

        #endregion

        #region Public Properties

        /// <summary>Number of currently active group GameObjects.</summary>
        public int ActiveGroupCount => _activeGroupObjects.Count;

        /// <summary>Total individual entity count across all active groups.</summary>
        public int TotalIndividualCount => _totalIndividuals;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _poolRoot = new GameObject("Wildlife Pool").transform;
            _poolRoot.SetParent(transform);
            PrewarmPool();
        }

        #endregion

        #region Pool Management

        private void PrewarmPool()
        {
            foreach (WildlifeCategory cat in Enum.GetValues(typeof(WildlifeCategory)))
            {
                _pool[cat] = new Queue<GameObject>();
                for (int i = 0; i < prewarmPoolSize; i++)
                    _pool[cat].Enqueue(CreatePooledObject(cat));
            }
        }

        private GameObject CreatePooledObject(WildlifeCategory category)
        {
            var go = new GameObject($"Wildlife_{category}");
            go.transform.SetParent(_poolRoot);
            go.SetActive(false);
            return go;
        }

        private GameObject GetFromPool(WildlifeCategory category)
        {
            if (!_pool.ContainsKey(category))
                _pool[category] = new Queue<GameObject>();
            if (_pool[category].Count > 0)
            {
                var obj = _pool[category].Dequeue();
                obj.SetActive(true);
                return obj;
            }
            return CreatePooledObject(category);
        }

        private void ReturnToPool(WildlifeCategory category, GameObject obj)
        {
            if (obj == null) return;
            obj.SetActive(false);
            obj.transform.SetParent(_poolRoot);
            _pool[category].Enqueue(obj);
        }

        #endregion

        #region Spawn / Despawn

        /// <summary>Spawns a wildlife group at the given world position.</summary>
        public GameObject SpawnGroup(WildlifeSpecies species, Vector3 center, int count)
        {
            if (species == null) return null;

            // Water check for marine species (null-safe)
            if (species.category == WildlifeCategory.MarineMammal ||
                species.category == WildlifeCategory.Fish)
            {
#if SWEF_WATER_AVAILABLE
                var wsm = SWEF.Water.WaterSurfaceManager.Instance;
                if (wsm != null && !wsm.IsOverWater(center))
                    return null;
#endif
            }

            GameObject root = GetFromPool(species.category);
            root.name = $"WildlifeGroup_{species.speciesId}_{Time.time:F0}";
            root.transform.position = center;

            // Add / configure controllers
            var groupCtrl = root.GetComponent<AnimalGroupController>()
                            ?? root.AddComponent<AnimalGroupController>();

            bool isBird = species.category == WildlifeCategory.Bird    ||
                          species.category == WildlifeCategory.Raptor   ||
                          species.category == WildlifeCategory.Seabird  ||
                          species.category == WildlifeCategory.Waterfowl ||
                          species.category == WildlifeCategory.MigratoryBird;

            bool isMarine = species.category == WildlifeCategory.MarineMammal ||
                            species.category == WildlifeCategory.Fish;

            BirdFlockController flockCtrl = null;
            if (isBird)
                flockCtrl = root.GetComponent<BirdFlockController>()
                            ?? root.AddComponent<BirdFlockController>();

            MarineLifeController marineCtrl = null;
            if (isMarine)
                marineCtrl = root.GetComponent<MarineLifeController>()
                             ?? root.AddComponent<MarineLifeController>();

            // Spawn individual boid children (birds only; marine/land handled differently)
            var boidTransforms = new System.Collections.Generic.List<Transform>();
            if (isBird)
            {
                float spread = 10f;
                for (int i = 0; i < count; i++)
                {
                    var boidGO  = new GameObject($"Boid_{i}");
                    boidGO.transform.SetParent(root.transform);
                    boidGO.transform.position = center + UnityEngine.Random.insideUnitSphere * spread;
                    boidGO.AddComponent<AnimalAnimationController>();
                    boidTransforms.Add(boidGO.transform);
                }
            }

            flockCtrl?.InitialiseBoids(boidTransforms, species.baseSpeed, species.fleeSpeed);

            var state = new WildlifeGroupState
            {
                groupId        = root.name,
                species        = species,
                centerPosition = center,
                memberCount    = count,
                spawnTime      = Time.time,
                lifetime       = 240f
            };
            groupCtrl.Initialise(state);

            _activeGroupObjects[state.groupId] = root;
            _totalIndividuals += count;
            root.SetActive(true);
            return root;
        }

        /// <summary>Despawns and pools the group with the given ID.</summary>
        public void DespawnGroup(string groupId)
        {
            if (!_activeGroupObjects.TryGetValue(groupId, out var obj)) return;

            var ctrl = obj.GetComponent<AnimalGroupController>();
            int memberCount = ctrl?.State?.memberCount ?? 0;
            WildlifeCategory cat = ctrl?.State?.species?.category ?? WildlifeCategory.Bird;

            // Remove boid children
            int childCount = obj.transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
                Destroy(obj.transform.GetChild(i).gameObject);

            ReturnToPool(cat, obj);
            _activeGroupObjects.Remove(groupId);
            _totalIndividuals = Mathf.Max(0, _totalIndividuals - memberCount);
        }

        /// <summary>Despawns all active groups.</summary>
        public void DespawnAllGroups()
        {
            var ids = new System.Collections.Generic.List<string>(_activeGroupObjects.Keys);
            foreach (var id in ids) DespawnGroup(id);
        }

        #endregion
    }

    /// <summary>Serializable migration path definition.</summary>
    [Serializable]
    public class MigrationPath
    {
        public string speciesId;
        public List<Vector3> waypoints = new List<Vector3>();
        public int[] activeMonths      = new int[0];
    }
}
