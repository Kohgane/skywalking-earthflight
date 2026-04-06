// OceanSurfaceRenderer.cs — Phase 117: Advanced Ocean & Maritime System
// Ocean surface rendering: displacement, normal mapping, foam, SSS, caustics.
// Namespace: SWEF.OceanSystem

using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Drives ocean surface visual rendering parameters.
    /// Feeds displacement data to the material, manages foam generation,
    /// subsurface scattering colour, and caustic projection.
    /// </summary>
    public class OceanSurfaceRenderer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Configuration")]
        [SerializeField] private OceanSystemConfig config;

        [Header("Material")]
        [SerializeField] private Material oceanMaterial;

        [Header("Foam")]
        [SerializeField] private float foamThreshold = 0.4f;
        [SerializeField] private Texture2D foamTexture;

        [Header("Caustics")]
        [SerializeField] private Projector causticsProjector;
        [SerializeField] private Texture2D[] causticFrames;
        [SerializeField] private float causticsFrameRate = 24f;

        // ── Private state ─────────────────────────────────────────────────────────

        private int    _causticsFrame;
        private float  _causticsTimer;

        // Shader property IDs
        private static readonly int PropFoamDensity      = Shader.PropertyToID("_FoamDensity");
        private static readonly int PropShallowColour     = Shader.PropertyToID("_ShallowColour");
        private static readonly int PropDeepColour        = Shader.PropertyToID("_DeepColour");
        private static readonly int PropTransparencyDepth = Shader.PropertyToID("_TransparencyDepth");

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Start()
        {
            ApplyConfig();
        }

        private void Update()
        {
            TickCaustics();
        }

        // ── Initialisation ────────────────────────────────────────────────────────

        private void ApplyConfig()
        {
            if (config == null || oceanMaterial == null) return;

            oceanMaterial.SetFloat(PropFoamDensity,       config.foamDensity);
            oceanMaterial.SetColor(PropShallowColour,     config.subsurfaceColour);
            oceanMaterial.SetColor(PropDeepColour,        config.deepWaterColour);
            oceanMaterial.SetFloat(PropTransparencyDepth, config.transparencyDepth);

            if (causticsProjector != null)
                causticsProjector.enabled = config.enableCaustics;
        }

        // ── Caustics Animation ────────────────────────────────────────────────────

        private void TickCaustics()
        {
            if (causticFrames == null || causticFrames.Length == 0) return;
            if (causticsProjector == null || !causticsProjector.enabled) return;

            _causticsTimer += Time.deltaTime;
            if (_causticsTimer >= 1f / causticsFrameRate)
            {
                _causticsTimer = 0f;
                _causticsFrame = (_causticsFrame + 1) % causticFrames.Length;
                causticsProjector.material.mainTexture = causticFrames[_causticsFrame];
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Updates foam intensity based on current sea state.</summary>
        public void SetSeaState(SeaState state)
        {
            if (oceanMaterial == null) return;
            float foam = state switch
            {
                SeaState.Calm      => 0.05f,
                SeaState.Slight    => 0.15f,
                SeaState.Moderate  => 0.35f,
                SeaState.Rough     => 0.60f,
                SeaState.VeryRough => 0.80f,
                SeaState.HighSeas  => 1.00f,
                _ => 0.1f
            };
            oceanMaterial.SetFloat(PropFoamDensity, foam * (config != null ? config.foamDensity : 1f));
        }

        /// <summary>Enables or disables caustic projection.</summary>
        public void SetCausticsEnabled(bool enabled)
        {
            if (causticsProjector != null) causticsProjector.enabled = enabled;
        }
    }
}
