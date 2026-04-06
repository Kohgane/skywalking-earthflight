// RoadNetworkGenerator.cs — Phase 113: Procedural City & Airport Generation
// Road grid generation: main roads, highways, side streets, intersections, roundabouts.
// Namespace: SWEF.ProceduralWorld

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Generates and renders the road network for a procedural city.
    /// Supports grid-based main roads, side streets, highway connectors, and roundabouts.
    /// </summary>
    public class RoadNetworkGenerator : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Road Materials")]
        [SerializeField] private Material highwayMaterial;
        [SerializeField] private Material mainRoadMaterial;
        [SerializeField] private Material sideStreetMaterial;

        [Header("Road Widths (metres)")]
        [SerializeField] private float highwayWidth = 20f;
        [SerializeField] private float mainRoadWidth = 12f;
        [SerializeField] private float sideStreetWidth = 6f;

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly List<GameObject> _roadObjects = new List<GameObject>();

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Generates road meshes from the segments defined in <paramref name="layout"/>.</summary>
        public void GenerateRoads(CityLayout layout, Transform parent)
        {
            ClearRoads();
            foreach (var seg in layout.roadSegments)
                CreateRoadSegment(seg, parent);
        }

        /// <summary>Removes all generated road objects.</summary>
        public void ClearRoads()
        {
            foreach (var go in _roadObjects)
                if (go != null) Destroy(go);
            _roadObjects.Clear();
        }

        /// <summary>Generates roundabout geometry at the given world position.</summary>
        public GameObject GenerateRoundabout(Vector3 position, float radius, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Roundabout";
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);
            go.GetComponent<Renderer>().sharedMaterial = mainRoadMaterial;
            _roadObjects.Add(go);
            return go;
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void CreateRoadSegment(RoadSegmentData seg, Transform parent)
        {
            float width = seg.roadType switch
            {
                RoadType.Highway   => highwayWidth,
                RoadType.MainRoad  => mainRoadWidth,
                _                  => sideStreetWidth
            };

            var mat = seg.roadType switch
            {
                RoadType.Highway  => highwayMaterial,
                RoadType.MainRoad => mainRoadMaterial,
                _                 => sideStreetMaterial
            };

            Vector3 dir = (seg.end - seg.start).normalized;
            float len = seg.lengthMetres;
            if (len < 0.01f) return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Road_{seg.roadType}";
            go.transform.SetParent(parent, false);
            go.transform.position = (seg.start + seg.end) * 0.5f;
            go.transform.rotation = Quaternion.LookRotation(dir);
            go.transform.localScale = new Vector3(width, 0.1f, len);

            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            _roadObjects.Add(go);
        }
    }
}
