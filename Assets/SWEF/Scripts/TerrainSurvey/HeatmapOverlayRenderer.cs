// HeatmapOverlayRenderer.cs — SWEF Terrain Scanning & Geological Survey System
using System;
using UnityEngine;

namespace SWEF.TerrainSurvey
{
    /// <summary>
    /// Renders a procedural quad mesh below the aircraft colored by the active
    /// <see cref="SurveyMode"/>.  Subscribes to <see cref="TerrainScannerController.OnScanCompleted"/>
    /// and rebuilds the mesh vertices/colors after every scan cycle.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class HeatmapOverlayRenderer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Configuration")]
        [SerializeField] private TerrainSurveyConfig config;
        [SerializeField] private SurveyMode          activeMode = SurveyMode.Altitude;

        [Header("Appearance")]
        [SerializeField, Range(0f, 1f)] private float opacity     = 0.7f;
        [SerializeField]                private float yOffset      = 2f;

        [Header("Altitude Range (for gradient mapping)")]
        [SerializeField] private float altitudeMin = 0f;
        [SerializeField] private float altitudeMax = 8000f;

        // ── State ─────────────────────────────────────────────────────────────────
        /// <summary>Currently active visualization mode.</summary>
        public SurveyMode ActiveMode => activeMode;

        /// <summary>Whether the heatmap overlay is currently visible.</summary>
        public bool IsVisible { get; private set; } = true;

        private MeshFilter   _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh         _mesh;
        private Material     _material;
        private SurveySample[] _lastSamples;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _meshFilter   = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();

            _mesh = new Mesh { name = "HeatmapOverlayMesh" };
            _meshFilter.mesh = _mesh;

            // Create a simple vertex-color unlit material at runtime
            _material = new Material(Shader.Find("Particles/Standard Unlit") ??
                                     Shader.Find("Unlit/Color") ??
                                     Shader.Find("Hidden/InternalErrorShader"));
            if (_material != null)
                _material.SetFloat("_Mode", 3); // Transparent blending if available
            _meshRenderer.material = _material;
        }

        private void OnEnable()
        {
            if (TerrainScannerController.Instance != null)
                TerrainScannerController.Instance.OnScanCompleted += OnScanCompleted;
        }

        private void OnDisable()
        {
            if (TerrainScannerController.Instance != null)
                TerrainScannerController.Instance.OnScanCompleted -= OnScanCompleted;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Sets the active visualization mode and rebuilds the mesh.</summary>
        public void SetMode(SurveyMode mode)
        {
            activeMode = mode;
            if (_lastSamples != null)
                RebuildMesh(_lastSamples);
        }

        /// <summary>Sets overlay opacity in [0, 1] and updates material color.</summary>
        public void SetOpacity(float value)
        {
            opacity = Mathf.Clamp01(value);
            ApplyOpacity();
        }

        /// <summary>Toggles heatmap visibility.</summary>
        public void SetVisible(bool visible)
        {
            IsVisible = visible;
            _meshRenderer.enabled = visible;
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void OnScanCompleted(SurveySample[] samples)
        {
            _lastSamples = samples;
            if (IsVisible)
                RebuildMesh(samples);
        }

        private void RebuildMesh(SurveySample[] samples)
        {
            if (samples == null || samples.Length == 0) return;

            int   count    = samples.Length;
            var   vertices = new Vector3[count];
            var   colors   = new Color32[count];
            var   indices  = new int[(count - 1) * 6]; // approximate — filled below
            int   triIdx   = 0;

            int resolution = config != null ? config.scanResolution : 10;

            for (int i = 0; i < count; i++)
            {
                SurveySample s = samples[i];
                vertices[i] = new Vector3(s.position.x, s.position.y + yOffset, s.position.z)
                              - transform.position; // local space

                Color c = SampleColor(s);
                c.a    = opacity;
                colors[i] = (Color32)c;
            }

            // Build quads from adjacent grid points
            int rowLen = resolution;
            triIdx = 0;
            var triList = new System.Collections.Generic.List<int>();
            for (int r = 0; r < resolution - 1; r++)
            {
                for (int col = 0; col < rowLen - 1; col++)
                {
                    int a = r * rowLen + col;
                    int b = a + 1;
                    int c2 = a + rowLen;
                    int d = c2 + 1;
                    triList.Add(a); triList.Add(c2); triList.Add(b);
                    triList.Add(b); triList.Add(c2); triList.Add(d);
                }
            }

            _mesh.Clear();
            _mesh.vertices  = vertices;
            _mesh.colors32  = colors;
            _mesh.triangles = triList.ToArray();
            _mesh.RecalculateNormals();
        }

        private Color SampleColor(SurveySample s)
        {
            switch (activeMode)
            {
                case SurveyMode.Altitude:
                    if (config != null && config.altitudeGradient != null)
                    {
                        float t = Mathf.InverseLerp(altitudeMin, altitudeMax, s.altitude);
                        return config.altitudeGradient.Evaluate(t);
                    }
                    return Color.Lerp(Color.blue, Color.red,
                        Mathf.InverseLerp(altitudeMin, altitudeMax, s.altitude));

                case SurveyMode.Slope:
                    if (config != null && config.slopeGradient != null)
                    {
                        float t = Mathf.InverseLerp(0f, 90f, s.slope);
                        return config.slopeGradient.Evaluate(t);
                    }
                    return Color.Lerp(Color.green, Color.yellow,
                        Mathf.InverseLerp(0f, 90f, s.slope));

                case SurveyMode.Biome:
                    return GeologicalClassifier.GetFeatureColor(s.featureType);

                case SurveyMode.Temperature:
                    // Estimate from altitude — cold=blue, hot=red
                    float tmpEstimate = 15f - (s.altitude / 1000f) * 6.5f;
                    return Color.Lerp(Color.blue, Color.red,
                        Mathf.InverseLerp(-30f, 50f, tmpEstimate));

                case SurveyMode.Mineral:
                    // Placeholder: slope + altitude proxy for mineral probability
                    float mineralProb = Mathf.Clamp01(s.slope / 90f * 0.6f
                                                    + Mathf.InverseLerp(0f, 8000f, s.altitude) * 0.4f);
                    return Color.Lerp(Color.gray, new Color(0.8f, 0.6f, 0.1f), mineralProb);

                default:
                    return Color.white;
            }
        }

        private void ApplyOpacity()
        {
            if (_material == null) return;
            Color c = _material.color;
            c.a = opacity;
            _material.color = c;
        }
    }
}
