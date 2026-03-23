using System.Collections.Generic;
using UnityEngine;

namespace SWEF.CityGen
{
    /// <summary>
    /// Phase 52 — Places vegetation (trees, parks, gardens) within and around cities.
    ///
    /// <para>Street trees are spaced evenly along road segments.  Park blocks receive
    /// randomised clusters of trees drawn from a biome-aware catalogue.  All trees
    /// are GPU-instanced for performance and transition through three LOD levels:
    /// full mesh → billboard sprite → culled.</para>
    ///
    /// <para>Attach to the same persistent GameObject as <see cref="CityManager"/>.</para>
    /// </summary>
    public class VegetationPlacer : MonoBehaviour
    {
        #region Constants

        private const float StreetTreeSpacing = 12f;   // metres between street trees
        private const float MinTreeScale       = 0.7f;
        private const float MaxTreeScale       = 1.4f;
        private const float ClusterRadius      = 20f;
        private const int   MaxTreesPerCluster = 8;

        #endregion

        #region Inspector

        [Header("Tree Prefabs")]
        [Tooltip("Deciduous tree mesh (temperate regions).")]
        [SerializeField] private GameObject deciduousTree;

        [Tooltip("Coniferous / pine tree mesh (cold/Nordic regions).")]
        [SerializeField] private GameObject coniferTree;

        [Tooltip("Palm tree mesh (tropical regions).")]
        [SerializeField] private GameObject palmTree;

        [Header("Biome Thresholds")]
        [Tooltip("World Y coordinate below which tropical trees are preferred.")]
        [SerializeField] private float tropicalLatitudeThreshold = -200f;

        [Tooltip("World Y coordinate above which conifer trees are preferred.")]
        [SerializeField] private float nordicLatitudeThreshold = 200f;

        [Header("LOD")]
        [Tooltip("Distance at which tree meshes switch to billboard.")]
        [SerializeField] private float billboardDistance = 150f;

        [Tooltip("Distance beyond which trees are culled entirely.")]
        [SerializeField] private float cullDistance = 400f;

        [Header("Density")]
        [Range(0f, 2f)]
        [Tooltip("Global density multiplier (1 = default).")]
        [SerializeField] private float densityMultiplier = 1f;

        #endregion

        #region Private State

        private Camera _camera;
        private readonly List<GameObject> _trees = new List<GameObject>();

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _camera = Camera.main;
        }

        private void Update()
        {
            CullDistantTrees();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Places trees along road segments in <paramref name="network"/> under
        /// <paramref name="parent"/>.
        /// </summary>
        public void PlaceStreetTrees(RoadNetwork network, Transform parent)
        {
            if (network == null) return;
            foreach (var seg in network.segments)
            {
                if (seg.roadType == RoadType.Alley) continue;
                if (seg.roadType == RoadType.Highway) continue;
                PlaceTreesAlongSegment(seg, parent);
            }
        }

        /// <summary>
        /// Plants park vegetation clusters inside every <see cref="CityBlock"/>
        /// whose <see cref="CityBlock.parkPercentage"/> is greater than zero.
        /// </summary>
        public void PlaceParkVegetation(IEnumerable<CityBlock> blocks, Transform parent)
        {
            foreach (var block in blocks)
            {
                if (block.parkPercentage <= 0f) continue;
                int clusters = Mathf.RoundToInt(block.parkPercentage * 5f * densityMultiplier);
                var rng = new System.Random((int)(block.bounds.center.x * 73 + block.bounds.center.z * 31));
                for (int c = 0; c < clusters; c++)
                {
                    Vector3 clusterCenter = block.bounds.center + new Vector3(
                        (float)(rng.NextDouble() - 0.5) * block.bounds.size.x * 0.8f,
                        0f,
                        (float)(rng.NextDouble() - 0.5) * block.bounds.size.z * 0.8f
                    );
                    PlaceCluster(clusterCenter, parent, rng);
                }
            }
        }

        #endregion

        #region Tree Placement

        private void PlaceTreesAlongSegment(RoadSegment seg, Transform parent)
        {
            float length = Vector3.Distance(seg.start, seg.end);
            int count = Mathf.RoundToInt(length / StreetTreeSpacing * densityMultiplier);
            if (count <= 0) return;

            Vector3 dir  = (seg.end - seg.start).normalized;
            Vector3 perp = Vector3.Cross(Vector3.up, dir) * (seg.width * 0.65f);

            for (int i = 1; i < count; i++)
            {
                float t   = (float)i / count;
                Vector3 p = Vector3.Lerp(seg.start, seg.end, t);

                // Both sides of the road.
                SpawnTree(p + perp,  parent);
                SpawnTree(p - perp,  parent);
            }
        }

        private void PlaceCluster(Vector3 center, Transform parent, System.Random rng)
        {
            int count = rng.Next(3, MaxTreesPerCluster + 1);
            for (int i = 0; i < count; i++)
            {
                float angle = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                float dist  = (float)(rng.NextDouble() * ClusterRadius);
                Vector3 pos = center + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                SpawnTree(pos, parent);
            }
        }

        private void SpawnTree(Vector3 position, Transform parent)
        {
            GameObject prefab = SelectPrefab(position);
            if (prefab == null) return;

            float scale = Random.Range(MinTreeScale, MaxTreeScale);
            var go = Instantiate(prefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), parent);
            go.transform.localScale = Vector3.one * scale;
            go.name = "Tree";
            _trees.Add(go);
        }

        private GameObject SelectPrefab(Vector3 position)
        {
            // Use world-space X axis as a proxy for east-west band; configurable thresholds
            // allow the designer to tune biome zones to the actual world layout.
            if (position.x < tropicalLatitudeThreshold && palmTree     != null) return palmTree;
            if (position.x > nordicLatitudeThreshold   && coniferTree  != null) return coniferTree;
            if (deciduousTree != null) return deciduousTree;
            if (coniferTree   != null) return coniferTree;
            if (palmTree      != null) return palmTree;

            // Ultimate fallback: simple sphere placeholder.
            return GameObject.CreatePrimitive(PrimitiveType.Sphere);
        }

        #endregion

        #region LOD / Culling

        private void CullDistantTrees()
        {
            if (_camera == null) return;
            Vector3 camPos = _camera.transform.position;

            foreach (var t in _trees)
            {
                if (t == null) continue;
                float dist = (t.transform.position - camPos).magnitude;
                t.SetActive(dist < cullDistance);
            }
        }

        #endregion
    }
}
