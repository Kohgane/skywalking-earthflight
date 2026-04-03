// PlatformProfile.cs — SWEF Accessibility & Platform Optimization (Phase 93)
using System;
using UnityEngine;

namespace SWEF.Accessibility
{
    /// <summary>Shadow quality tier.</summary>
    public enum ShadowQuality
    {
        Off,
        Low,
        Medium,
        High
    }

    /// <summary>Texture resolution tier.</summary>
    public enum TextureQuality
    {
        /// <summary>Quarter-resolution textures (lowest VRAM).</summary>
        Quarter,
        /// <summary>Half-resolution textures.</summary>
        Half,
        /// <summary>Full-resolution textures.</summary>
        Full
    }

    /// <summary>Anti-aliasing technique.</summary>
    public enum AntiAliasingMode
    {
        None,
        FXAA,
        SMAA,
        MSAA2x,
        MSAA4x
    }

    /// <summary>Ocean rendering detail level.</summary>
    public enum OceanQuality
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Serializable platform-specific quality settings. Applied by <see cref="PlatformOptimizer"/>.
    /// </summary>
    [Serializable]
    public class PlatformProfile
    {
        // ── Display ──────────────────────────────────────────────────────────────
        [Tooltip("Target frame rate (e.g. 30, 60, 120). -1 = uncapped.")]
        public int targetFrameRate = 60;

        [Tooltip("Internal render resolution relative to native (0.5 = half, 1.0 = native, 2.0 = super-sample).")]
        [Range(0.5f, 2f)]
        public float renderScale = 1f;

        // ── Geometry ─────────────────────────────────────────────────────────────
        [Tooltip("Unity QualitySettings.lodBias value.")]
        public float lodBias = 1f;

        [Tooltip("Shadow map quality tier.")]
        public ShadowQuality shadowQuality = ShadowQuality.Medium;

        [Tooltip("Texture streaming mip-map resolution.")]
        public TextureQuality textureQuality = TextureQuality.Full;

        // ── Post-processing ───────────────────────────────────────────────────────
        [Tooltip("Anti-aliasing technique.")]
        public AntiAliasingMode antiAliasing = AntiAliasingMode.FXAA;

        [Tooltip("Enable full post-processing stack (bloom, AO, etc.).")]
        public bool enablePostProcessing = true;

        [Tooltip("Enable volumetric fog and lighting (GPU intensive).")]
        public bool enableVolumetrics = true;

        // ── Environment ───────────────────────────────────────────────────────────
        [Tooltip("Ocean surface and wave simulation detail.")]
        public OceanQuality oceanQuality = OceanQuality.Medium;

        [Tooltip("Maximum simultaneous weather particles.")]
        public int weatherParticleLimit = 10000;

        [Tooltip("Camera far-clip view distance in metres.")]
        public float viewDistance = 100000f;

        // ── Cesium ────────────────────────────────────────────────────────────────
        [Tooltip("Maximum memory (MB) the Cesium 3D tile cache may occupy.")]
        public int cesiumTileCacheSize = 512;
    }
}
