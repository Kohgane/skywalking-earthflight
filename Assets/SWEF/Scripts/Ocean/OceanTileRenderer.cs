using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Ocean
{
    /// <summary>
    /// Phase 50 — Renders a virtually infinite ocean surface by tiling a configurable
    /// NxN quad-mesh grid that follows the camera in world space.
    ///
    /// <para>Features:
    /// <list type="bullet">
    ///   <item>Grid snaps to a <see cref="tileSize"/> grid to prevent jitter.</item>
    ///   <item>Per-tile LOD — tiles farther from the camera use fewer vertices.</item>
    ///   <item>Frustum culling — invisible tiles are skipped.</item>
    ///   <item>Gerstner wave parameters are forwarded to the material each frame
    ///         via <see cref="MaterialPropertyBlock"/> to avoid material instancing.</item>
    /// </list>
    /// </para>
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public class OceanTileRenderer : MonoBehaviour
    {
        #region Constants

        private const int   MinTileResolution  = 4;
        private const int   MaxTileResolution  = 64;
        private const float FrustumCullPadding = 1.2f;  // slightly widen the frustum check

        // Shader property IDs (pre-hashed for performance).
        private static readonly int ShaderPropWaveAmplitude  = Shader.PropertyToID("_WaveAmplitude");
        private static readonly int ShaderPropWaveFrequency  = Shader.PropertyToID("_WaveFrequency");
        private static readonly int ShaderPropWaveSpeed      = Shader.PropertyToID("_WaveSpeed");
        private static readonly int ShaderPropWaveDirection  = Shader.PropertyToID("_WaveDirection");
        private static readonly int ShaderPropWaveSteepness  = Shader.PropertyToID("_WaveSteepness");
        private static readonly int ShaderPropWaveTime       = Shader.PropertyToID("_WaveTime");
        private static readonly int ShaderPropShallowColor   = Shader.PropertyToID("_ShallowColor");
        private static readonly int ShaderPropDeepColor      = Shader.PropertyToID("_DeepColor");
        private static readonly int ShaderPropFoamColor      = Shader.PropertyToID("_FoamColor");
        private static readonly int ShaderPropFresnelPower   = Shader.PropertyToID("_FresnelPower");

        #endregion

        #region Inspector

        [Header("Grid")]
        [Tooltip("Number of tiles along each axis of the camera-centred grid (e.g. 5 → 5×5 grid).")]
        [SerializeField, Range(3, 13)] private int gridRadius = 5;

        [Tooltip("World-space side length of each tile in metres.")]
        [SerializeField] private float tileSize = 256f;

        [Header("Mesh Resolution")]
        [Tooltip("Vertex count per tile edge at the highest LOD (closest tiles).")]
        [SerializeField, Range(MinTileResolution, MaxTileResolution)] private int tileResolutionHigh = 32;

        [Tooltip("Vertex count per tile edge at medium LOD.")]
        [SerializeField, Range(MinTileResolution, MaxTileResolution)] private int tileResolutionMed  = 16;

        [Tooltip("Vertex count per tile edge at low LOD.")]
        [SerializeField, Range(MinTileResolution, MaxTileResolution)] private int tileResolutionLow  = 8;

        [Tooltip("Camera-distance (metres) at which medium LOD kicks in.")]
        [SerializeField] private float lodMediumDistance = 512f;

        [Tooltip("Camera-distance (metres) at which low LOD kicks in.")]
        [SerializeField] private float lodLowDistance    = 1024f;

        [Header("Rendering")]
        [Tooltip("Material applied to every ocean tile.")]
        [SerializeField] private Material oceanMaterial;

        [Tooltip("Rendering layer mask used by ocean tile renderers.")]
        [SerializeField] private int renderLayer = 4;  // Water layer (default Unity convention)

        [Header("References")]
        [Tooltip("Camera used to centre the tile grid. Defaults to Camera.main.")]
        [SerializeField] private Camera trackedCamera;

        [Tooltip("OceanManager reference — resolved at runtime if null.")]
        [SerializeField] private OceanManager oceanManager;

        #endregion

        #region Private State

        private Camera        _cam;
        private Plane[]       _frustumPlanes = new Plane[6];
        private OceanManager  _mgr;

        // Each active tile is a child GameObject with a MeshRenderer + MeshFilter.
        private readonly Dictionary<Vector2Int, TileEntry> _tiles = new Dictionary<Vector2Int, TileEntry>();

        // Pre-built meshes keyed by vertex-per-edge count to avoid redundant rebuilds.
        private readonly Dictionary<int, Mesh> _tileMeshCache = new Dictionary<int, Mesh>();

        private readonly MaterialPropertyBlock _propBlock = new MaterialPropertyBlock();
        private Vector2Int _lastGridOrigin = new Vector2Int(int.MinValue, int.MinValue);

        #endregion

        #region Inner Types

        private class TileEntry
        {
            public GameObject  go;
            public MeshFilter  filter;
            public MeshRenderer renderer;
            public Vector2Int  coord;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _cam = trackedCamera != null ? trackedCamera : Camera.main;
            _mgr = oceanManager  != null ? oceanManager  : FindFirstObjectByType<OceanManager>();
        }

        private void Start()
        {
            if (_cam == null)
                Debug.LogWarning("[SWEF.Ocean] OceanTileRenderer: no camera found — rendering disabled.");

            RebuildAllTiles();
        }

        private void LateUpdate()
        {
            if (_cam == null) return;

            GeometryUtility.CalculateFrustumPlanes(_cam, _frustumPlanes);
            UpdateTileGrid();
            UploadShaderProperties();
        }

        private void OnDestroy()
        {
            foreach (var m in _tileMeshCache.Values)
                Destroy(m);
            _tileMeshCache.Clear();
        }

        #endregion

        #region Grid Update

        private void UpdateTileGrid()
        {
            Vector3    camPos   = _cam.transform.position;
            Vector2Int origin   = WorldToGridCoord(camPos);

            if (origin == _lastGridOrigin) return;
            _lastGridOrigin = origin;

            int half = gridRadius / 2;

            // Mark all existing tiles as unused.
            var toRemove = new List<Vector2Int>();
            foreach (var key in _tiles.Keys)
            {
                int dx = Mathf.Abs(key.x - origin.x);
                int dz = Mathf.Abs(key.y - origin.y);
                if (dx > half || dz > half)
                    toRemove.Add(key);
            }
            foreach (var key in toRemove)
            {
                Destroy(_tiles[key].go);
                _tiles.Remove(key);
            }

            // Ensure all required tiles exist.
            for (int gz = origin.y - half; gz <= origin.y + half; gz++)
            for (int gx = origin.x - half; gx <= origin.x + half; gx++)
            {
                var coord = new Vector2Int(gx, gz);
                if (!_tiles.ContainsKey(coord))
                    CreateTile(coord);
            }
        }

        private void RebuildAllTiles()
        {
            foreach (var t in _tiles.Values)
                Destroy(t.go);
            _tiles.Clear();

            if (_cam != null)
                UpdateTileGrid();
        }

        #endregion

        #region Tile Creation

        private void CreateTile(Vector2Int coord)
        {
            Vector3 tileWorldPos = GridCoordToWorld(coord);

            // Frustum-cull before creating a new tile.
            var bounds = new Bounds(tileWorldPos + new Vector3(0f, 0f, 0f),
                                    new Vector3(tileSize, 50f, tileSize));
            if (!GeometryUtility.TestPlanesAABB(_frustumPlanes, bounds))
                return;

            float dist        = Vector3.Distance(_cam.transform.position, tileWorldPos);
            int   resolution  = ResolutionForDistance(dist);
            Mesh  mesh        = GetOrBuildTileMesh(resolution);

            var go = new GameObject($"OceanTile_{coord.x}_{coord.y}");
            go.layer = renderLayer;
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.position = tileWorldPos;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = oceanMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows    = false;

            _tiles[coord] = new TileEntry { go = go, filter = mf, renderer = mr, coord = coord };
        }

        #endregion

        #region Mesh Cache

        private Mesh GetOrBuildTileMesh(int resolution)
        {
            if (_tileMeshCache.TryGetValue(resolution, out Mesh cached))
                return cached;

            Mesh mesh = BuildQuadMesh(resolution);
            mesh.name = $"OceanTileMesh_{resolution}";
            _tileMeshCache[resolution] = mesh;
            return mesh;
        }

        private Mesh BuildQuadMesh(int resolution)
        {
            int   verts   = resolution + 1;
            float step    = tileSize / resolution;
            float halfSz  = tileSize * 0.5f;

            Vector3[] positions  = new Vector3[verts * verts];
            Vector2[] uvs        = new Vector2[verts * verts];
            int[]     triangles  = new int[resolution * resolution * 6];

            for (int z = 0; z <= resolution; z++)
            for (int x = 0; x <= resolution; x++)
            {
                int idx = z * verts + x;
                positions[idx] = new Vector3(x * step - halfSz, 0f, z * step - halfSz);
                uvs[idx]       = new Vector2((float)x / resolution, (float)z / resolution);
            }

            int tri = 0;
            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                int bl = z * verts + x;
                int br = bl + 1;
                int tl = bl + verts;
                int tr = tl + 1;

                triangles[tri++] = bl; triangles[tri++] = tl; triangles[tri++] = tr;
                triangles[tri++] = bl; triangles[tri++] = tr; triangles[tri++] = br;
            }

            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(positions);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        #endregion

        #region Shader Property Upload

        private void UploadShaderProperties()
        {
            if (oceanMaterial == null || _mgr == null) return;

            var settings = GetActiveOceanSettings();
            if (settings == null) return;

            var wave = settings.waves;
            var app  = settings.appearance;

            _propBlock.SetFloat(ShaderPropWaveAmplitude, wave.amplitude);
            _propBlock.SetFloat(ShaderPropWaveFrequency, wave.frequency);
            _propBlock.SetFloat(ShaderPropWaveSpeed,     wave.speed);
            _propBlock.SetVector(ShaderPropWaveDirection, new Vector4(wave.direction.x, wave.direction.y, 0f, 0f));
            _propBlock.SetFloat(ShaderPropWaveSteepness, wave.steepness);
            _propBlock.SetFloat(ShaderPropWaveTime,      _mgr.WaveTime);
            _propBlock.SetColor(ShaderPropShallowColor,  app.shallowColor);
            _propBlock.SetColor(ShaderPropDeepColor,     app.deepColor);
            _propBlock.SetColor(ShaderPropFoamColor,     app.foamColor);
            _propBlock.SetFloat(ShaderPropFresnelPower,  app.fresnelPower);

            foreach (var t in _tiles.Values)
            {
                if (t.renderer != null)
                    t.renderer.SetPropertyBlock(_propBlock);
            }
        }

        private OceanSettings GetActiveOceanSettings()
        {
            if (_mgr.WaterBodies.Count == 0) return null;
            var body = _mgr.GetNearestWaterBody(_cam.transform.position);
            return body?.oceanSettings;
        }

        #endregion

        #region Helpers

        private int ResolutionForDistance(float dist)
        {
            if (dist < lodMediumDistance) return Mathf.Clamp(tileResolutionHigh, MinTileResolution, MaxTileResolution);
            if (dist < lodLowDistance)    return Mathf.Clamp(tileResolutionMed,  MinTileResolution, MaxTileResolution);
            return Mathf.Clamp(tileResolutionLow, MinTileResolution, MaxTileResolution);
        }

        private Vector2Int WorldToGridCoord(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x / tileSize),
                Mathf.FloorToInt(worldPos.z / tileSize));
        }

        private Vector3 GridCoordToWorld(Vector2Int coord)
        {
            float waterY = _mgr != null ? _mgr.GetNearestWaterBody(_cam.transform.position)?.worldPosition.y ?? 0f : 0f;
            return new Vector3(
                (coord.x + 0.5f) * tileSize,
                waterY,
                (coord.y + 0.5f) * tileSize);
        }

        #endregion
    }
}
