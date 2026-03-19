using System.Collections.Generic;
using UnityEngine;
using SWEF.Performance;

namespace SWEF.Terrain
{
    /// <summary>
    /// Manages a runtime texture pool for terrain chunks.
    /// Provides one shared <see cref="Material"/> per <see cref="BiomeType"/> to
    /// minimise draw calls, and integrates with <see cref="TextureMemoryOptimizer"/>
    /// for memory budgeting.
    /// </summary>
    public class TerrainTextureManager : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Texture Settings")]
        [SerializeField] private int   textureSize    = 128;
        [SerializeField] private float memBudgetMB    = 32f;

        // ── Internal state ───────────────────────────────────────────────────────
        private readonly Dictionary<BiomeType, Material>  _materials = new Dictionary<BiomeType, Material>();
        private readonly Dictionary<BiomeType, Texture2D> _textures  = new Dictionary<BiomeType, Texture2D>();

        private TextureMemoryOptimizer _texOptimizer;
        private float                  _usedMemMB;

        // ── Unity lifecycle ──────────────────────────────────────────────────────
        private void Awake()
        {
            _texOptimizer = FindFirstObjectByType<TextureMemoryOptimizer>();
            if (_texOptimizer == null)
                Debug.Log("[SWEF] TerrainTextureManager: TextureMemoryOptimizer not found — terrain texture memory will not be tracked.");
            PreBuildBiomeMaterials();
        }

        // ── Public API ───────────────────────────────────────────────────────────
        /// <summary>
        /// Returns the shared terrain <see cref="Material"/> for the given <see cref="BiomeType"/>.
        /// Falls back to a plain vertex-color material if the biome material is not available.
        /// </summary>
        public Material GetTerrainMaterial(BiomeType biome)
        {
            if (_materials.TryGetValue(biome, out var mat)) return mat;
            return GetOrCreateMaterial(biome);
        }

        /// <summary>Estimated terrain texture memory in megabytes.</summary>
        public float UsedMemoryMB => _usedMemMB;

        // ── Internal helpers ─────────────────────────────────────────────────────
        private void PreBuildBiomeMaterials()
        {
            foreach (BiomeType biome in System.Enum.GetValues(typeof(BiomeType)))
                GetOrCreateMaterial(biome);

            RecalcMemory();

            if (_texOptimizer != null)
                Debug.Log($"[SWEF] TerrainTextureManager: {_usedMemMB:F1} MB terrain textures (budget {memBudgetMB} MB)");
        }

        private Material GetOrCreateMaterial(BiomeType biome)
        {
            if (_materials.TryGetValue(biome, out var existing)) return existing;

            Texture2D tex = GetOrCreateTexture(biome);
            Shader    sh  = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(sh) { name = $"Terrain_{biome}", mainTexture = tex };
            mat.enableInstancing = true;

            _materials[biome] = mat;
            return mat;
        }

        private Texture2D GetOrCreateTexture(BiomeType biome)
        {
            if (_textures.TryGetValue(biome, out var existing)) return existing;

            Color primary   = TerrainBiomeMapper.GetBiomeColor(biome);
            Color secondary = primary * 0.85f;
            secondary.a     = 1f;

            var tex = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, mipChain: true)
            {
                name        = $"TerrainTex_{biome}",
                filterMode  = FilterMode.Bilinear,
                wrapMode    = TextureWrapMode.Repeat
            };

            var pixels = new Color[textureSize * textureSize];
            for (int y = 0; y < textureSize; y++)
            for (int x = 0; x < textureSize; x++)
            {
                float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                pixels[y * textureSize + x] = Color.Lerp(secondary, primary, noise);
            }

            tex.SetPixels(pixels);
            tex.Apply(updateMipmaps: true, makeNoLongerReadable: true);

            _textures[biome] = tex;
            return tex;
        }

        private void RecalcMemory()
        {
            _usedMemMB = 0f;
            foreach (var kv in _textures)
            {
                if (kv.Value != null)
                    _usedMemMB += (textureSize * textureSize * 3) / (1024f * 1024f); // RGB24
            }
        }

        private void OnDestroy()
        {
            foreach (var mat in _materials.Values) if (mat != null) Destroy(mat);
            foreach (var tex in _textures.Values)  if (tex != null) Destroy(tex);
            _materials.Clear();
            _textures.Clear();
        }
    }
}
