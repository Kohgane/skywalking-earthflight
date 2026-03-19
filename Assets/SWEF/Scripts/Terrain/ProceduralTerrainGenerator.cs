using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SWEF.Terrain
{
    /// <summary>
    /// MonoBehaviour singleton that generates procedural terrain chunks around the player.
    /// Uses multi-octave fractal Perlin noise for heightmap generation, performed
    /// off the main thread via <see cref="Task.Run"/>.
    /// Chunks are recycled through <see cref="TerrainChunkPool"/> to minimise GC pressure.
    /// </summary>
    public class ProceduralTerrainGenerator : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static ProceduralTerrainGenerator Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Chunk Settings")]
        [SerializeField] private int   chunkSize     = 256;
        [SerializeField] private float heightScale   = 1000f;
        [SerializeField] private float noiseScale    = 0.002f;
        [SerializeField] private int   gridRadius    = 2;          // 5×5 grid = radius 2

        [Header("Noise (fractal Perlin)")]
        [SerializeField] private int   octaves       = 6;
        [SerializeField] private float persistence   = 0.5f;
        [SerializeField] private float lacunarity    = 2.0f;
        [SerializeField] private int   noiseSeed     = 42;

        [Header("Refs")]
        [SerializeField] private Transform         playerTransform;
        [SerializeField] private TerrainChunkPool  chunkPool;

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Fired on the main thread after a chunk has been generated and is ready.</summary>
        public event Action<Vector2Int> OnChunkGenerated;

        // ── Internal state ───────────────────────────────────────────────────────
        private readonly Dictionary<Vector2Int, TerrainChunk> _activeChunks = new Dictionary<Vector2Int, TerrainChunk>();
        private readonly HashSet<Vector2Int> _pendingGeneration              = new HashSet<Vector2Int>();
        private Vector2Int _lastPlayerChunk;

        // ── Unity lifecycle ──────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (chunkPool == null)
                chunkPool = FindFirstObjectByType<TerrainChunkPool>();
        }

        private void Start()
        {
            if (playerTransform == null)
                playerTransform = Camera.main?.transform;

            if (chunkPool == null)
                Debug.LogWarning("[SWEF] ProceduralTerrainGenerator: TerrainChunkPool not assigned — chunks will be created on demand without pooling.");

            if (playerTransform != null)
                UpdateChunks();
        }

        private void Update()
        {
            if (playerTransform == null) return;

            Vector2Int current = WorldToChunkCoord(playerTransform.position);
            if (current != _lastPlayerChunk)
            {
                _lastPlayerChunk = current;
                UpdateChunks();
            }
        }

        // ── Public API ───────────────────────────────────────────────────────────
        /// <summary>Returns the world-space centre of the chunk at <paramref name="coord"/>.</summary>
        public Vector3 ChunkCentreWorld(Vector2Int coord)
        {
            float worldSize = chunkSize;
            return new Vector3(coord.x * worldSize + worldSize * 0.5f, 0f, coord.y * worldSize + worldSize * 0.5f);
        }

        /// <summary>
        /// Generates a normalised heightmap for <paramref name="chunkCoord"/> off-thread.
        /// Values are in [0, 1] — multiply by <see cref="heightScale"/> for world heights.
        /// </summary>
        public float[,] GenerateHeightmap(Vector2Int chunkCoord)
        {
            int res    = chunkSize + 1;
            var map    = new float[res, res];
            float originX = chunkCoord.x * chunkSize;
            float originZ = chunkCoord.y * chunkSize;

            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    float sampleX = (originX + x) * noiseScale + noiseSeed;
                    float sampleZ = (originZ + z) * noiseScale + noiseSeed;
                    map[x, z] = FractalPerlin(sampleX, sampleZ);
                }
            }
            return map;
        }

        /// <summary>
        /// Builds an optimised <see cref="Mesh"/> from a heightmap at the given resolution divisor.
        /// <paramref name="lodDivisor"/> 1 = full, 2 = half, 4 = quarter, 8 = minimal.
        /// Vertices carry vertex colors derived from altitude biome.
        /// NOTE: Must be called on the main thread (Unity Mesh API).
        /// </summary>
        public Mesh BuildMesh(float[,] heightmap, int lodDivisor = 1)
        {
            int fullRes = chunkSize + 1;
            int res     = Mathf.Max(2, fullRes / lodDivisor);
            float step  = (float)chunkSize / (res - 1);

            var vertices  = new Vector3[res * res];
            var uvs       = new Vector2[res * res];
            var colors    = new Color[res * res];
            int triCount  = (res - 1) * (res - 1) * 6;
            var triangles = new int[triCount];

            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    int   idx     = z * res + x;
                    int   srcX    = Mathf.RoundToInt((float)x / (res - 1) * (fullRes - 1));
                    int   srcZ    = Mathf.RoundToInt((float)z / (res - 1) * (fullRes - 1));
                    float h       = heightmap[srcX, srcZ] * heightScale;

                    vertices[idx] = new Vector3(x * step, h, z * step);
                    uvs[idx]      = new Vector2((float)x / (res - 1), (float)z / (res - 1));
                    colors[idx]   = TerrainBiomeMapper.GetBiomeGradient(h);
                }
            }

            int tri = 0;
            for (int z = 0; z < res - 1; z++)
            {
                for (int x = 0; x < res - 1; x++)
                {
                    int bl = z * res + x;
                    int br = bl + 1;
                    int tl = bl + res;
                    int tr = tl + 1;

                    triangles[tri++] = bl; triangles[tri++] = tl; triangles[tri++] = tr;
                    triangles[tri++] = bl; triangles[tri++] = tr; triangles[tri++] = br;
                }
            }

            var mesh = new Mesh { name = $"TerrainMesh_LOD{lodDivisor}" };
            if (res * res > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.vertices  = vertices;
            mesh.uv        = uvs;
            mesh.colors    = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ── Chunk management ─────────────────────────────────────────────────────
        private void UpdateChunks()
        {
            var needed = new HashSet<Vector2Int>();
            Vector2Int center = _lastPlayerChunk;

            for (int z = -gridRadius; z <= gridRadius; z++)
            for (int x = -gridRadius; x <= gridRadius; x++)
                needed.Add(new Vector2Int(center.x + x, center.y + z));

            // Recycle chunks no longer needed
            var toRemove = new List<Vector2Int>();
            foreach (var kv in _activeChunks)
            {
                if (!needed.Contains(kv.Key))
                {
                    chunkPool?.Return(kv.Value);
                    toRemove.Add(kv.Key);
                }
            }
            foreach (var k in toRemove) _activeChunks.Remove(k);

            // Spawn/generate missing chunks
            foreach (var coord in needed)
            {
                if (_activeChunks.ContainsKey(coord)) continue;
                if (_pendingGeneration.Contains(coord)) continue;
                _pendingGeneration.Add(coord);
                GenerateChunkAsync(coord);
            }
        }

        private async void GenerateChunkAsync(Vector2Int coord)
        {
            // Heavy noise work off the main thread
            float[,] heightmap = await Task.Run(() => GenerateHeightmap(coord));

            // Back on main thread — build meshes and configure chunk
            if (this == null || !isActiveAndEnabled) return; // destroyed or disabled while awaiting

            TerrainChunk chunk = chunkPool != null ? chunkPool.Get() : CreateFallbackChunk(coord);

            chunk.ChunkCoord = coord;
            chunk.HeightData  = FlattenHeightmap(heightmap);

            // Build all four LOD meshes
            chunk.SetLODMesh(LOD.TerrainLODLevel.Full,     BuildMesh(heightmap, 1));
            chunk.SetLODMesh(LOD.TerrainLODLevel.Half,     BuildMesh(heightmap, 2));
            chunk.SetLODMesh(LOD.TerrainLODLevel.Quarter,  BuildMesh(heightmap, 4));
            chunk.SetLODMesh(LOD.TerrainLODLevel.Minimal,  BuildMesh(heightmap, 8));

            // Position in world
            float worldSize = chunkSize;
            chunk.transform.position = new Vector3(coord.x * worldSize, 0f, coord.y * worldSize);

            // Apply default material if renderer has none
            var mr = chunk.GetComponent<MeshRenderer>();
            if (mr != null && mr.sharedMaterial == null)
                mr.sharedMaterial = GetDefaultMaterial();

            chunk.UpdateLOD(LOD.TerrainLODLevel.Full);
            chunk.SetVisible(true);

            _activeChunks[coord] = chunk;
            _pendingGeneration.Remove(coord);

            OnChunkGenerated?.Invoke(coord);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────
        private Vector2Int WorldToChunkCoord(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x / chunkSize),
                Mathf.FloorToInt(worldPos.z / chunkSize));
        }

        private float FractalPerlin(float x, float z)
        {
            float value     = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float maxValue  = 0f;

            for (int i = 0; i < octaves; i++)
            {
                value    += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            return value / maxValue;
        }

        private static float[] FlattenHeightmap(float[,] map)
        {
            int w   = map.GetLength(0);
            int h   = map.GetLength(1);
            var arr = new float[w * h];
            for (int z = 0; z < h; z++)
            for (int x = 0; x < w; x++)
                arr[z * w + x] = map[x, z];
            return arr;
        }

        private TerrainChunk CreateFallbackChunk(Vector2Int coord)
        {
            var go    = new GameObject($"TerrainChunk_{coord.x}_{coord.y}");
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            return go.AddComponent<TerrainChunk>();
        }

        private Material _defaultMaterial;
        private Material GetDefaultMaterial()
        {
            if (_defaultMaterial == null)
                _defaultMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                {
                    name = "TerrainDefault"
                };
            return _defaultMaterial;
        }

        // ── Editor-visible stats ─────────────────────────────────────────────────
        /// <summary>Number of currently active terrain chunks.</summary>
        public int ActiveChunkCount  => _activeChunks.Count;

        /// <summary>Number of chunks currently being generated asynchronously.</summary>
        public int PendingChunkCount => _pendingGeneration.Count;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
