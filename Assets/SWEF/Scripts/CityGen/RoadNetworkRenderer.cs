using System.Collections.Generic;
using UnityEngine;

namespace SWEF.CityGen
{
    /// <summary>
    /// Phase 52 — Renders road networks for generated cities.
    ///
    /// <para>Generates flat quad meshes for each <see cref="RoadSegment"/>, applies
    /// road-type materials (asphalt / cobblestone), and draws lane markings.
    /// At medium distance individual road meshes are merged per-chunk; at far
    /// distance roads are hidden entirely.</para>
    ///
    /// <para>Attach to the same GameObject as <see cref="CityManager"/> or any
    /// persistent manager object.</para>
    /// </summary>
    public class RoadNetworkRenderer : MonoBehaviour
    {
        #region Constants

        private const float DefaultSegmentHeight = 0.02f; // slight Y lift above terrain
        private const float MarkingUVTile        = 4f;    // UV tiling for lane markings
        private const int   MaxSegmentsPerMesh   = 512;   // Unity 16-bit index limit guard

        #endregion

        #region Inspector

        [Header("Materials")]
        [Tooltip("Material for standard asphalt roads.")]
        [SerializeField] private Material asphaltMaterial;

        [Tooltip("Material for cobblestone historic roads.")]
        [SerializeField] private Material cobblestoneMaterial;

        [Tooltip("Material for highway / freeway surfaces.")]
        [SerializeField] private Material highwayMaterial;

        [Header("LOD")]
        [Tooltip("Distance at which road meshes are hidden entirely.")]
        [SerializeField] private float cullDistance = 1500f;

        [Tooltip("Distance at which roads switch to simplified (no markings) mesh.")]
        [SerializeField] private float simplifyDistance = 600f;

        #endregion

        #region Private State

        private Camera _camera;
        private readonly List<GameObject> _roadObjects = new List<GameObject>();

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _camera = Camera.main;
        }

        private void Update()
        {
            if (_camera == null) return;
            Vector3 camPos = _camera.transform.position;
            foreach (var go in _roadObjects)
            {
                if (go == null) continue;
                float dist = (go.transform.position - camPos).magnitude;
                go.SetActive(dist < cullDistance);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Generates GameObjects for every segment in <paramref name="network"/> and
        /// parents them under <paramref name="parent"/>.
        /// </summary>
        public void RenderNetwork(RoadNetwork network, Transform parent)
        {
            if (network == null) return;

            int   batchIndex   = 0;
            var   batchVerts   = new List<Vector3>();
            var   batchTris    = new List<int>();
            var   batchUVs     = new List<Vector2>();
            Material batchMat  = GetMaterial(RoadType.Street);

            foreach (var seg in network.segments)
            {
                Material mat = GetMaterial(seg.roadType);

                // Flush batch on material change or size limit.
                if ((batchVerts.Count > 0 && mat != batchMat) ||
                     batchVerts.Count >= MaxSegmentsPerMesh * 4)
                {
                    FlushBatch(batchVerts, batchTris, batchUVs, batchMat, parent, ref batchIndex);
                    batchMat = mat;
                }

                AddSegmentToBuffer(seg, batchVerts, batchTris, batchUVs);
            }

            if (batchVerts.Count > 0)
                FlushBatch(batchVerts, batchTris, batchUVs, batchMat, parent, ref batchIndex);
        }

        #endregion

        #region Batch Helpers

        private static void AddSegmentToBuffer(
            RoadSegment seg,
            List<Vector3> verts, List<int> tris, List<Vector2> uvs)
        {
            Vector3 dir     = (seg.end - seg.start).normalized;
            Vector3 right   = Vector3.Cross(Vector3.up, dir).normalized;
            float   hw      = seg.width * 0.5f;
            float   length  = Vector3.Distance(seg.start, seg.end);
            float   lift    = DefaultSegmentHeight;

            Vector3 p0 = seg.start + right * -hw + Vector3.up * lift;
            Vector3 p1 = seg.start + right *  hw + Vector3.up * lift;
            Vector3 p2 = seg.end   + right *  hw + Vector3.up * lift;
            Vector3 p3 = seg.end   + right * -hw + Vector3.up * lift;

            int b = verts.Count;
            verts.Add(p0); verts.Add(p1); verts.Add(p2); verts.Add(p3);

            float uvLen = length / seg.width * MarkingUVTile;
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(1f, uvLen));
            uvs.Add(new Vector2(0f, uvLen));

            tris.AddRange(new[] { b, b+1, b+2,  b, b+2, b+3 });
        }

        private void FlushBatch(
            List<Vector3> verts, List<int> tris, List<Vector2> uvs,
            Material mat, Transform parent, ref int batchIndex)
        {
            var mesh = new Mesh { name = $"RoadBatch_{batchIndex++}" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go  = new GameObject($"RoadMesh_{batchIndex}");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh    = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat ?? GetMaterial(RoadType.Street);

            _roadObjects.Add(go);

            verts.Clear();
            tris.Clear();
            uvs.Clear();
        }

        private Material GetMaterial(RoadType roadType)
        {
            return roadType switch
            {
                RoadType.Highway  => highwayMaterial    ?? CreateFallbackMaterial(new Color(0.25f, 0.25f, 0.3f)),
                RoadType.Street   => asphaltMaterial    ?? CreateFallbackMaterial(new Color(0.3f,  0.3f,  0.3f)),
                RoadType.Alley    => cobblestoneMaterial ?? CreateFallbackMaterial(new Color(0.4f,  0.38f, 0.35f)),
                RoadType.Pedestrian => cobblestoneMaterial ?? CreateFallbackMaterial(new Color(0.6f, 0.58f, 0.5f)),
                _                 => asphaltMaterial    ?? CreateFallbackMaterial(new Color(0.3f,  0.3f,  0.3f))
            };
        }

        private static Material CreateFallbackMaterial(Color color)
        {
            var m = new Material(Shader.Find("Standard")) { color = color };
            return m;
        }

        #endregion
    }
}
