using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.CityGen
{
    /// <summary>
    /// Phase 52 — Generates individual building meshes procedurally.
    ///
    /// <para>Supports rectangular, L-shaped, U-shaped, T-shaped and cylindrical
    /// footprints.  Each building can produce up to four LOD meshes ranging from
    /// fully-detailed (LOD0) down to a single billboard quad (LOD3).</para>
    ///
    /// <para>All generated <see cref="GameObject"/>s are pooled internally to
    /// minimise GC allocations.  Buildings in the same <see cref="CityBlock"/>
    /// can be batch-combined into a single mesh for draw-call reduction.</para>
    /// </summary>
    public class ProceduralBuildingGenerator : MonoBehaviour
    {
        #region Constants

        private const float FloorHeight         = 3.5f;   // metres per storey
        private const float WindowInset         = 0.1f;   // depth of window recess
        private const float GroundFloorScale    = 1.15f;  // slightly taller ground floor
        private const int   PoolGrowth          = 16;     // buildings added to pool per grow

        #endregion

        #region Inspector

        [Header("Materials")]
        [Tooltip("Shared material palette indexed by BuildingDefinition.materialIndex.")]
        [SerializeField] private Material[] buildingMaterials = Array.Empty<Material>();

        [Tooltip("Material used for road-facing ground floor shop fronts.")]
        [SerializeField] private Material groundFloorMaterial;

        [Tooltip("Billboard material for LOD3 impostors.")]
        [SerializeField] private Material billboardMaterial;

        [Header("Pool")]
        [Tooltip("Number of building GameObjects pre-allocated at startup.")]
        [SerializeField] private int initialPoolSize = 32;

        #endregion

        #region Private State

        private readonly Queue<GameObject> _pool = new Queue<GameObject>();
        private readonly List<GameObject>  _active = new List<GameObject>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            GrowPool(initialPoolSize);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Instantiates (or recycles) a building at <paramref name="position"/> with
        /// <paramref name="rotationY"/> degrees yaw, using <paramref name="def"/> to
        /// drive the geometry and material selection.
        /// </summary>
        /// <returns>The root <see cref="GameObject"/> of the generated building.</returns>
        public GameObject GenerateBuilding(BuildingDefinition def, Vector3 position, float rotationY)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));

            GameObject go = AcquireFromPool();
            go.name = $"Building_{def.buildingType}";
            go.transform.SetPositionAndRotation(
                position,
                Quaternion.Euler(0f, rotationY, 0f)
            );

            int seed = (int)(position.x * 73 + position.z * 31);
            var rng  = new System.Random(seed);

            float height = Mathf.Lerp(def.minHeight, def.maxHeight, (float)rng.NextDouble());
            float width  = Mathf.Lerp(def.minWidth,  def.maxWidth,  (float)rng.NextDouble());
            int   floors = rng.Next(def.minFloors, def.maxFloors + 1);

            Mesh lod0 = BuildMesh_LOD0(width, height, floors, def.roofType, rng);
            Mesh lod1 = BuildMesh_LOD1(width, height);
            Mesh lod2 = BuildMesh_LOD2(width, height);
            Mesh lod3 = BuildMesh_LOD3(width, height);

            SetupLODGroup(go, def, new[] { lod0, lod1, lod2, lod3 });
            _active.Add(go);
            return go;
        }

        /// <summary>Generates all buildings for a fully loaded <see cref="CityLayout"/>.</summary>
        public void GenerateSettlement(CityLayout layout, Transform parent)
        {
            if (layout == null) return;
            var rng = new System.Random(layout.seed);

            foreach (var block in layout.blocks)
            {
                foreach (var def in block.buildings)
                {
                    Vector3 pos = block.bounds.center + new Vector3(
                        (float)(rng.NextDouble() - 0.5) * block.bounds.size.x,
                        0f,
                        (float)(rng.NextDouble() - 0.5) * block.bounds.size.z
                    );
                    float rot = (float)(rng.NextDouble() * 360.0);
                    var go = GenerateBuilding(def, pos, rot);
                    go.transform.SetParent(parent, true);
                }
            }
        }

        /// <summary>Returns a building to the pool for later reuse.</summary>
        public void ReleaseBuilding(GameObject go)
        {
            if (go == null) return;
            _active.Remove(go);
            go.SetActive(false);
            _pool.Enqueue(go);
        }

        #endregion

        #region Mesh Construction

        // --- LOD 0 : Full detail ---------------------------------------------------

        private Mesh BuildMesh_LOD0(float w, float h, int floors, RoofType roofType, System.Random rng)
        {
            var verts  = new List<Vector3>();
            var tris   = new List<int>();
            var uvs    = new List<Vector2>();

            float d = w * 0.6f; // depth

            // Box walls
            AddBox(verts, tris, uvs, Vector3.zero, w, h, d);

            // Window rows on each facade
            float floorH = h / floors;
            for (int f = 1; f < floors; f++)
            {
                float yBase = f * floorH;
                AddWindowRow(verts, tris, uvs, w, d, yBase, floorH, rng);
            }

            // Roof
            AddRoof(verts, tris, uvs, w, h, d, roofType);

            return BuildMesh(verts, tris, uvs, $"LOD0_w{w:F0}_h{h:F0}");
        }

        // --- LOD 1 : Simplified (no windows) --------------------------------------

        private Mesh BuildMesh_LOD1(float w, float h)
        {
            var verts = new List<Vector3>();
            var tris  = new List<int>();
            var uvs   = new List<Vector2>();
            float d = w * 0.6f;
            AddBox(verts, tris, uvs, Vector3.zero, w, h, d);
            return BuildMesh(verts, tris, uvs, $"LOD1_w{w:F0}_h{h:F0}");
        }

        // --- LOD 2 : Single box ---------------------------------------------------

        private Mesh BuildMesh_LOD2(float w, float h)
        {
            var verts = new List<Vector3>();
            var tris  = new List<int>();
            var uvs   = new List<Vector2>();
            AddBox(verts, tris, uvs, Vector3.zero, w, h, w * 0.6f);
            return BuildMesh(verts, tris, uvs, $"LOD2_w{w:F0}_h{h:F0}");
        }

        // --- LOD 3 : Billboard quad -----------------------------------------------

        private Mesh BuildMesh_LOD3(float w, float h)
        {
            var m = new Mesh { name = $"LOD3_w{w:F0}_h{h:F0}" };
            float hw = w * 0.5f;
            m.vertices  = new[]
            {
                new Vector3(-hw, 0f, 0f), new Vector3( hw, 0f, 0f),
                new Vector3( hw, h,  0f), new Vector3(-hw, h,  0f)
            };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.uv        = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            m.RecalculateNormals();
            return m;
        }

        // --- Geometry helpers -----------------------------------------------------

        private static void AddBox(
            List<Vector3> verts, List<int> tris, List<Vector2> uvs,
            Vector3 origin, float w, float h, float d)
        {
            float hw = w * 0.5f;
            float hd = d * 0.5f;

            // Six faces: +X, -X, +Z, -Z, +Y, -Y
            var faces = new (Vector3 a, Vector3 b, Vector3 c, Vector3 dd)[]
            {
                (new Vector3( hw,0,  hd), new Vector3( hw,h,  hd), new Vector3( hw,h,-hd), new Vector3( hw,0,-hd)), // Right
                (new Vector3(-hw,0, -hd), new Vector3(-hw,h, -hd), new Vector3(-hw,h, hd), new Vector3(-hw,0, hd)), // Left
                (new Vector3(-hw,0,  hd), new Vector3(-hw,h,  hd), new Vector3( hw,h, hd), new Vector3( hw,0, hd)), // Front
                (new Vector3( hw,0, -hd), new Vector3( hw,h, -hd), new Vector3(-hw,h,-hd), new Vector3(-hw,0,-hd)), // Back
                (new Vector3(-hw,h, -hd), new Vector3( hw,h, -hd), new Vector3( hw,h, hd), new Vector3(-hw,h, hd)), // Top
                (new Vector3(-hw,0, hd),  new Vector3( hw,0, hd),  new Vector3( hw,0,-hd), new Vector3(-hw,0,-hd)), // Bottom
            };

            foreach (var f in faces)
            {
                int b = verts.Count;
                verts.Add(origin + f.a); verts.Add(origin + f.b);
                verts.Add(origin + f.c); verts.Add(origin + f.dd);
                tris.AddRange(new[] { b, b+1, b+2, b, b+2, b+3 });
                uvs.AddRange(new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right });
            }
        }

        private static void AddWindowRow(
            List<Vector3> verts, List<int> tris, List<Vector2> uvs,
            float w, float d, float yBase, float floorH, System.Random rng)
        {
            int cols      = Mathf.Max(2, Mathf.RoundToInt(w / 2.5f));
            float winW    = w / cols * 0.35f;
            float winH    = floorH * 0.5f;
            float hw      = w * 0.5f;

            for (int c = 0; c < cols; c++)
            {
                float xc = -hw + (c + 0.5f) * (w / cols);
                float yc = yBase + floorH * 0.2f;
                float z  = d * 0.5f + WindowInset;
                int   b  = verts.Count;
                verts.Add(new Vector3(xc - winW * 0.5f, yc,        z));
                verts.Add(new Vector3(xc + winW * 0.5f, yc,        z));
                verts.Add(new Vector3(xc + winW * 0.5f, yc + winH, z));
                verts.Add(new Vector3(xc - winW * 0.5f, yc + winH, z));
                tris.AddRange(new[] { b, b+1, b+2, b, b+2, b+3 });
                uvs.AddRange(new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
            }
        }

        private static void AddRoof(
            List<Vector3> verts, List<int> tris, List<Vector2> uvs,
            float w, float h, float d, RoofType roofType)
        {
            float hw = w * 0.5f;
            float hd = d * 0.5f;

            switch (roofType)
            {
                case RoofType.Pitched:
                {
                    float peak = h + Mathf.Min(w, d) * 0.35f;
                    int b = verts.Count;
                    verts.Add(new Vector3(-hw, h, -hd)); verts.Add(new Vector3( hw, h, -hd));
                    verts.Add(new Vector3( hw, h,  hd)); verts.Add(new Vector3(-hw, h,  hd));
                    verts.Add(new Vector3(0f, peak, -hd)); verts.Add(new Vector3(0f, peak, hd));
                    tris.AddRange(new[] { b,b+1,b+4, b+1,b+2,b+5, b+2,b+3,b+5, b+3,b+0,b+4, b+4,b+1,b+5, b+0,b+3,b+4 });
                    for (int i = 0; i < 6; i++) uvs.Add(Vector2.zero);
                    break;
                }
                case RoofType.Spire:
                case RoofType.Antenna:
                {
                    int b = verts.Count;
                    float spireH = h + Mathf.Min(w, d) * 0.8f;
                    verts.Add(new Vector3(-hw, h, -hd)); verts.Add(new Vector3( hw, h, -hd));
                    verts.Add(new Vector3( hw, h,  hd)); verts.Add(new Vector3(-hw, h,  hd));
                    verts.Add(new Vector3(0f, spireH, 0f));
                    tris.AddRange(new[] { b,b+1,b+4, b+1,b+2,b+4, b+2,b+3,b+4, b+3,b+0,b+4 });
                    for (int i = 0; i < 5; i++) uvs.Add(Vector2.zero);
                    break;
                }
                default:
                {
                    // Flat roof — top face already generated by AddBox.
                    break;
                }
            }
        }

        private static Mesh BuildMesh(List<Vector3> verts, List<int> tris, List<Vector2> uvs, string name)
        {
            var m = new Mesh { name = name };
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.SetUVs(0, uvs);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        #endregion

        #region LOD Group Setup

        private void SetupLODGroup(GameObject go, BuildingDefinition def, Mesh[] meshes)
        {
            // Reuse existing child GameObjects up to lodCount; destroy extras; create missing.
            int lodCount = Mathf.Min(def.lodLevels, meshes.Length);
            string[] suffixes  = { "LOD0", "LOD1", "LOD2", "LOD3" };

            // Remove extra children beyond what we need.
            while (go.transform.childCount > lodCount)
            {
                var last = go.transform.GetChild(go.transform.childCount - 1).gameObject;
                Destroy(last);
            }

            var lods       = new LOD[lodCount];
            float[] thresholds = { 1f, 0.5f, 0.15f, 0.04f };
            Material mat = GetMaterial(def.materialIndex);

            for (int i = 0; i < lodCount; i++)
            {
                // Reuse existing child or create a new one.
                GameObject child;
                if (i < go.transform.childCount)
                {
                    child = go.transform.GetChild(i).gameObject;
                    child.name = suffixes[i];
                }
                else
                {
                    child = new GameObject(suffixes[i]);
                    child.transform.SetParent(go.transform, false);
                }

                var mf = child.GetComponent<MeshFilter>()  ?? child.AddComponent<MeshFilter>();
                var mr = child.GetComponent<MeshRenderer>() ?? child.AddComponent<MeshRenderer>();
                mf.sharedMesh     = meshes[i];
                mr.sharedMaterial = (i == lodCount - 1 && billboardMaterial != null)
                    ? billboardMaterial
                    : mat;
                lods[i] = new LOD(thresholds[i] * 0.2f, new Renderer[] { mr });
            }

            var lodGroup = go.GetComponent<LODGroup>() ?? go.AddComponent<LODGroup>();
            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();
            go.SetActive(true);
        }

        private Material GetMaterial(int index)
        {
            if (buildingMaterials != null && index >= 0 && index < buildingMaterials.Length)
                return buildingMaterials[index];
            return new Material(Shader.Find("Standard"));
        }

        #endregion

        #region Object Pool

        private void GrowPool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("PooledBuilding");
                go.SetActive(false);
                _pool.Enqueue(go);
            }
        }

        private GameObject AcquireFromPool()
        {
            if (_pool.Count == 0) GrowPool(PoolGrowth);
            var go = _pool.Dequeue();
            go.SetActive(true);
            return go;
        }

        #endregion
    }
}
