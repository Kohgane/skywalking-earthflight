using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.PhotoMode
{
    /// <summary>
    /// Enhanced post-processing effects stack for photo mode.
    /// Composites multiple effects in-order on a source texture and supports
    /// before/after comparison, presets, and real-time preview.
    /// </summary>
    public class AdvancedPostProcessing : MonoBehaviour
    {
        #region Enums

        /// <summary>Available effect types that can be added to the stack.</summary>
        public enum EffectType
        {
            DepthOfField,
            TiltShift,
            Vignette,
            FilmGrain,
            ChromaticAberration,
            LensFlare,
            ColorLUT
        }

        #endregion

        #region Inner Types

        /// <summary>A single effect entry in the compositing stack.</summary>
        [Serializable]
        public class EffectEntry
        {
            public EffectType type;
            public bool       enabled = true;

            // Shared intensity / strength
            [Range(0f, 1f)] public float intensity = 0.5f;

            // DepthOfField / TiltShift
            [Range(0f, 1f)] public float focusNormalizedY = 0.5f;   // 0 = bottom, 1 = top
            [Range(0f, 0.5f)] public float focusBandHalf  = 0.15f;
            [Range(0f, 0.5f)] public float blurRadius     = 0.04f;

            // Vignette
            public Color vignetteColour = Color.black;
            [Range(1f, 5f)] public float vignettePower = 2f;

            // Film Grain
            [Range(0f, 1f)] public float grainSeed = 0f;

            // Chromatic Aberration
            [Range(0f, 0.02f)] public float caOffset = 0.005f;

            // Lens Flare
            [Range(0f, 1f)] public float flareThreshold = 0.8f;

            // Color LUT
            public Texture2D lutTexture;
            [Range(0f, 1f)] public float lutBlend = 1f;
        }

        /// <summary>A named preset containing a serializable effect stack.</summary>
        [Serializable]
        public class PostProcessPreset
        {
            public string            name;
            public List<EffectEntry> effects = new List<EffectEntry>();
        }

        #endregion

        #region Inspector

        [Header("Effect Stack")]
        [SerializeField]
        private List<EffectEntry> _effects = new List<EffectEntry>();

        [Header("Before/After Comparison")]
        [SerializeField, Tooltip("Normalised X position of the split divider (0=left, 1=right).")]
        [Range(0f, 1f)]
        private float _splitPosition = 0.5f;

        [SerializeField]
        private bool _showComparison;

        [Header("Preview")]
        [SerializeField, Tooltip("Raw Image used to display the live post-process preview.")]
        private UnityEngine.UI.RawImage _previewTarget;

        [Header("Presets")]
        [SerializeField]
        private List<PostProcessPreset> _presets = new List<PostProcessPreset>();

        [SerializeField, Tooltip("Folder name inside persistentDataPath for user presets.")]
        private string _presetFolder = "PPPresets";

        #endregion

        #region Private State

        private Camera _camera;
        private Texture2D _lastProcessed;

        #endregion

        #region Public Properties

        /// <summary>Live access to the effect stack for runtime editing.</summary>
        public IList<EffectEntry> Effects => _effects;

        /// <summary>Whether the before/after split is currently displayed.</summary>
        public bool ShowComparison
        {
            get => _showComparison;
            set => _showComparison = value;
        }

        /// <summary>Normalised X position of the comparison split divider.</summary>
        public float SplitPosition
        {
            get => _splitPosition;
            set => _splitPosition = Mathf.Clamp01(value);
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
                _camera = Camera.main;
        }

        #endregion

        #region Public API

        /// <summary>Apply all enabled effects to <paramref name="source"/> and return the result.
        /// The source texture is not modified.</summary>
        public Texture2D Process(Texture2D source)
        {
            if (source == null) return null;

            Texture2D current = DuplicateTexture(source);

            foreach (var effect in _effects)
            {
                if (!effect.enabled || effect.intensity <= 0f) continue;

                current = ApplyEffect(current, effect);
            }

            _lastProcessed = current;
            return current;
        }

        /// <summary>Process and update the preview RawImage if assigned.</summary>
        public void UpdatePreview(Texture2D source)
        {
            if (_previewTarget == null) return;
            var result = Process(source);
            if (result != null)
                _previewTarget.texture = result;
        }

        /// <summary>Produce a side-by-side comparison texture: original left, processed right.</summary>
        public Texture2D GenerateComparisonTexture(Texture2D original)
        {
            var processed = Process(original);
            return BlendSplit(original, processed, _splitPosition);
        }

        /// <summary>Add an effect to the end of the stack.</summary>
        public void AddEffect(EffectEntry entry) => _effects.Add(entry);

        /// <summary>Remove an effect at the given index.</summary>
        public void RemoveEffect(int index)
        {
            if (index >= 0 && index < _effects.Count)
                _effects.RemoveAt(index);
        }

        /// <summary>Load a named preset onto the stack (replaces current effects).</summary>
        public bool LoadPreset(string presetName)
        {
            foreach (var p in _presets)
            {
                if (p.name == presetName)
                {
                    _effects = new List<EffectEntry>(p.effects);
                    return true;
                }
            }
            return false;
        }

        /// <summary>Save the current effect stack as a user preset.</summary>
        public void SavePreset(string presetName)
        {
            var preset = new PostProcessPreset
            {
                name    = presetName,
                effects = new List<EffectEntry>(_effects)
            };

            string folder = Path.Combine(Application.persistentDataPath, _presetFolder);
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, presetName + ".json");

            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(preset, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AdvancedPostProcessing] Preset save failed: {ex.Message}");
            }
        }

        #endregion

        #region Effect Implementations

        private Texture2D ApplyEffect(Texture2D src, EffectEntry e)
        {
            switch (e.type)
            {
                case EffectType.Vignette:          return ApplyVignette(src, e);
                case EffectType.FilmGrain:         return ApplyFilmGrain(src, e);
                case EffectType.ChromaticAberration: return ApplyCA(src, e);
                case EffectType.TiltShift:
                case EffectType.DepthOfField:      return ApplyTiltShiftDof(src, e);
                case EffectType.ColorLUT:          return ApplyLUT(src, e);
                case EffectType.LensFlare:         return ApplyLensFlare(src, e);
                default:                            return src;
            }
        }

        private Texture2D ApplyVignette(Texture2D src, EffectEntry e)
        {
            var result = DuplicateTexture(src);
            int w = result.width, h = result.height;
            Color[] pixels = result.GetPixels();
            Color vc = e.vignetteColour;

            for (int i = 0; i < pixels.Length; i++)
            {
                float x = (i % w) / (float)w - 0.5f;
                float y = (i / w) / (float)h - 0.5f;
                float d = Mathf.Pow(x * x + y * y, e.vignettePower);
                float t = Mathf.Clamp01(d * 2f) * e.intensity;
                pixels[i] = Color.Lerp(pixels[i], vc, t);
            }

            result.SetPixels(pixels);
            result.Apply();
            Destroy(src);
            return result;
        }

        private Texture2D ApplyFilmGrain(Texture2D src, EffectEntry e)
        {
            var result = DuplicateTexture(src);
            Color[] pixels = result.GetPixels();
            float seed = e.grainSeed;

            for (int i = 0; i < pixels.Length; i++)
            {
                float noise = Mathf.PerlinNoise(i * 0.01f + seed, seed) - 0.5f;
                float g = noise * e.intensity * 0.3f;
                pixels[i] = new Color(
                    Mathf.Clamp01(pixels[i].r + g),
                    Mathf.Clamp01(pixels[i].g + g),
                    Mathf.Clamp01(pixels[i].b + g),
                    pixels[i].a);
            }

            result.SetPixels(pixels);
            result.Apply();
            Destroy(src);
            return result;
        }

        private Texture2D ApplyCA(Texture2D src, EffectEntry e)
        {
            var result = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            int w = src.width, h = src.height;
            float offset = e.caOffset * e.intensity;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)w;
                    float v = y / (float)h;

                    Color r = src.GetPixelBilinear(u + offset, v);
                    Color g = src.GetPixelBilinear(u, v);
                    Color b = src.GetPixelBilinear(u - offset, v);

                    result.SetPixel(x, y, new Color(r.r, g.g, b.b, g.a));
                }
            }

            result.Apply();
            Destroy(src);
            return result;
        }

        private Texture2D ApplyTiltShiftDof(Texture2D src, EffectEntry e)
        {
            int w = src.width, h = src.height;
            Color[] orig = src.GetPixels();

            // Compute per-row blur radius
            int[] rowRadius = new int[h];
            float focusY = e.focusNormalizedY;
            float band   = e.focusBandHalf;
            int   maxR   = Mathf.Max(1, Mathf.RoundToInt(e.blurRadius * h));

            for (int py = 0; py < h; py++)
            {
                float nv   = py / (float)h;
                float dist = Mathf.Abs(nv - focusY);
                float blur = Mathf.Clamp01((dist - band) / Mathf.Max(1f - band, 0.001f)) * e.intensity;
                rowRadius[py] = blur < 0.01f ? 0 : Mathf.RoundToInt(blur * maxR);
            }

            // Horizontal pass → temp buffer
            Color[] hPass = new Color[w * h];
            for (int py = 0; py < h; py++)
            {
                int r = rowRadius[py];
                if (r == 0)
                {
                    for (int px = 0; px < w; px++)
                        hPass[py * w + px] = orig[py * w + px];
                    continue;
                }
                int diam = r * 2 + 1;
                for (int px = 0; px < w; px++)
                {
                    Color acc = Color.clear;
                    for (int dx = -r; dx <= r; dx++)
                        acc += orig[py * w + Mathf.Clamp(px + dx, 0, w - 1)];
                    hPass[py * w + px] = acc / diam;
                }
            }

            // Vertical pass → output buffer
            Color[] vPass = new Color[w * h];
            for (int px = 0; px < w; px++)
            {
                for (int py = 0; py < h; py++)
                {
                    int r = rowRadius[py];
                    if (r == 0) { vPass[py * w + px] = hPass[py * w + px]; continue; }
                    int   diam = r * 2 + 1;
                    Color acc  = Color.clear;
                    for (int dy = -r; dy <= r; dy++)
                        acc += hPass[Mathf.Clamp(py + dy, 0, h - 1) * w + px];
                    vPass[py * w + px] = acc / diam;
                }
            }

            var result = new Texture2D(w, h, TextureFormat.RGBA32, false);
            result.SetPixels(vPass);
            result.Apply();
            Destroy(src);
            return result;
        }

        private Texture2D ApplyLUT(Texture2D src, EffectEntry e)
        {
            if (e.lutTexture == null) return src;

            // Cache the LUT pixels once to avoid repeated GetPixelBilinear GPU round-trips
            int   lutW     = e.lutTexture.width;
            Color[] lutPix = e.lutTexture.GetPixels();

            var result = DuplicateTexture(src);
            Color[] pixels = result.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                Color input = pixels[i];
                // Sample LUT as a 1D strip (N×1 horizontal) using nearest-index lookup
                int ri = Mathf.Clamp(Mathf.RoundToInt(input.r * (lutW - 1)), 0, lutW - 1);
                int gi = Mathf.Clamp(Mathf.RoundToInt(input.g * (lutW - 1)), 0, lutW - 1);
                int bi = Mathf.Clamp(Mathf.RoundToInt(input.b * (lutW - 1)), 0, lutW - 1);

                Color lutOut = new Color(lutPix[ri].r, lutPix[gi].g, lutPix[bi].b, input.a);
                pixels[i] = Color.Lerp(input, lutOut, e.lutBlend * e.intensity);
            }

            result.SetPixels(pixels);
            result.Apply();
            Destroy(src);
            return result;
        }

        private Texture2D ApplyLensFlare(Texture2D src, EffectEntry e)
        {
            var result = DuplicateTexture(src);
            int w = result.width, h = result.height;
            Color[] pixels = result.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                float brightness = pixels[i].r * 0.299f + pixels[i].g * 0.587f + pixels[i].b * 0.114f;
                if (brightness > e.flareThreshold)
                {
                    float flare = (brightness - e.flareThreshold) / (1f - e.flareThreshold);
                    pixels[i] = Color.Lerp(pixels[i], Color.white, flare * e.intensity * 0.5f);
                }
            }

            result.SetPixels(pixels);
            result.Apply();
            Destroy(src);
            return result;
        }

        #endregion

        #region Helpers

        private Texture2D DuplicateTexture(Texture2D source)
        {
            var copy = new Texture2D(source.width, source.height, source.format, false);
            copy.SetPixels(source.GetPixels());
            copy.Apply();
            return copy;
        }

        private Texture2D BlendSplit(Texture2D original, Texture2D processed, float splitX)
        {
            int w = original.width, h = original.height;
            int splitPx = Mathf.RoundToInt(splitX * w);

            var result  = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] lo  = original.GetPixels();
            Color[] lp  = processed.GetPixels();
            Color[] dst = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx  = y * w + x;
                    dst[idx] = x < splitPx ? lo[idx] : lp[idx];
                }
            }

            result.SetPixels(dst);
            result.Apply();
            return result;
        }

        #endregion
    }
}
