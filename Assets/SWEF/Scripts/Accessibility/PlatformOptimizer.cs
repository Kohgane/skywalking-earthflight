// PlatformOptimizer.cs — SWEF Accessibility & Platform Optimization (Phase 93)
using System;
using UnityEngine;

namespace SWEF.Accessibility
{
    /// <summary>Overall quality tier, from maximum fidelity to minimum hardware requirement.</summary>
    public enum QualityTier
    {
        Ultra,
        High,
        Medium,
        Low,
        /// <summary>Lowest possible settings for very constrained hardware ("potato mode").</summary>
        Potato
    }

    /// <summary>
    /// Singleton that auto-detects the current platform, selects an appropriate
    /// <see cref="PlatformProfile"/>, applies it via Unity's QualitySettings, and
    /// exposes a quality-tier override for the user.
    ///
    /// <para>Integrates with <see cref="DynamicQualityScaler"/> for FPS-driven
    /// automatic quality adjustments.</para>
    /// </summary>
    public class PlatformOptimizer : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static PlatformOptimizer Instance { get; private set; }

        // ── Serialised fields ─────────────────────────────────────────────────────
        [Header("Quality Tier Profiles")]
        [SerializeField] private PlatformProfile ultraProfile  = CreateUltra();
        [SerializeField] private PlatformProfile highProfile   = CreateHigh();
        [SerializeField] private PlatformProfile mediumProfile = CreateMedium();
        [SerializeField] private PlatformProfile lowProfile    = CreateLow();
        [SerializeField] private PlatformProfile potatoProfile = CreatePotato();

        [Header("Runtime")]
        [SerializeField, Tooltip("Allow DynamicQualityScaler to auto-adjust tier.")]
        private bool allowDynamicScaling = true;

        // ── Runtime state ─────────────────────────────────────────────────────────
        private QualityTier     _activeTier;
        private PlatformProfile _activeProfile;

        /// <summary>Currently active platform profile.</summary>
        public PlatformProfile ActiveProfile => _activeProfile;

        /// <summary>Currently active quality tier.</summary>
        public QualityTier ActiveTier => _activeTier;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired whenever the quality tier changes.</summary>
        public event Action<QualityTier> OnTierChanged;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            QualityTier detected = DetectPlatformTier();
            ApplyTier(detected);
            Debug.Log($"[SWEF] Accessibility: PlatformOptimizer auto-selected tier '{detected}' for {SystemInfo.deviceModel}.");
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Applies the given quality tier, updating Unity's QualitySettings.
        /// </summary>
        public void ApplyTier(QualityTier tier)
        {
            _activeTier    = tier;
            _activeProfile = ProfileForTier(tier);
            ApplyProfile(_activeProfile);
            OnTierChanged?.Invoke(tier);
        }

        /// <summary>
        /// Applies an explicit <see cref="PlatformProfile"/> without changing the tier label.
        /// </summary>
        public void ApplyProfile(PlatformProfile profile)
        {
            _activeProfile = profile;

            // Frame rate
            Application.targetFrameRate = profile.targetFrameRate;

            // LOD
            QualitySettings.lodBias = profile.lodBias;

            // Shadows
            switch (profile.shadowQuality)
            {
                case ShadowQuality.Off:    QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;   break;
                case ShadowQuality.Low:    QualitySettings.shadows = UnityEngine.ShadowQuality.HardOnly;  break;
                case ShadowQuality.Medium: QualitySettings.shadows = UnityEngine.ShadowQuality.All;       break;
                case ShadowQuality.High:   QualitySettings.shadows = UnityEngine.ShadowQuality.All;       break;
            }

            // Texture resolution
            switch (profile.textureQuality)
            {
                case TextureQuality.Quarter: QualitySettings.masterTextureLimit = 2; break;
                case TextureQuality.Half:    QualitySettings.masterTextureLimit = 1; break;
                case TextureQuality.Full:    QualitySettings.masterTextureLimit = 0; break;
            }

            // Anti-aliasing
            switch (profile.antiAliasing)
            {
                case AntiAliasingMode.None:   QualitySettings.antiAliasing = 0; break;
                case AntiAliasingMode.MSAA2x: QualitySettings.antiAliasing = 2; break;
                case AntiAliasingMode.MSAA4x: QualitySettings.antiAliasing = 4; break;
                default:                      QualitySettings.antiAliasing = 0; break; // FXAA/SMAA handled by post-processing volume
            }

            Debug.Log($"[SWEF] Accessibility: Applied platform profile — FPS:{profile.targetFrameRate} " +
                      $"Shadow:{profile.shadowQuality} AA:{profile.antiAliasing}");
        }

        // ── Platform auto-detection ───────────────────────────────────────────────

        private QualityTier DetectPlatformTier()
        {
#if UNITY_ANDROID || UNITY_IOS
            int ram = SystemInfo.systemMemorySize;
            int vram = SystemInfo.graphicsMemorySize;
            if (ram >= 8000 && vram >= 4000) return QualityTier.High;
            if (ram >= 4000 && vram >= 2000) return QualityTier.Medium;
            if (ram >= 2000)                 return QualityTier.Low;
            return QualityTier.Potato;
#else
            int vram = SystemInfo.graphicsMemorySize;
            if (vram >= 8000) return QualityTier.Ultra;
            if (vram >= 4000) return QualityTier.High;
            if (vram >= 2000) return QualityTier.Medium;
            if (vram >= 1000) return QualityTier.Low;
            return QualityTier.Potato;
#endif
        }

        private PlatformProfile ProfileForTier(QualityTier tier) => tier switch
        {
            QualityTier.Ultra  => ultraProfile,
            QualityTier.High   => highProfile,
            QualityTier.Medium => mediumProfile,
            QualityTier.Low    => lowProfile,
            _                  => potatoProfile
        };

        // ── Default profile factories ─────────────────────────────────────────────

        private static PlatformProfile CreateUltra() => new PlatformProfile
        {
            targetFrameRate    = 120,
            renderScale        = 1.5f,
            lodBias            = 2f,
            shadowQuality      = ShadowQuality.High,
            textureQuality     = TextureQuality.Full,
            antiAliasing       = AntiAliasingMode.MSAA4x,
            oceanQuality       = OceanQuality.High,
            weatherParticleLimit = 50000,
            viewDistance       = 200000f,
            cesiumTileCacheSize = 2048,
            enablePostProcessing = true,
            enableVolumetrics  = true
        };

        private static PlatformProfile CreateHigh() => new PlatformProfile
        {
            targetFrameRate    = 60,
            renderScale        = 1f,
            lodBias            = 1.5f,
            shadowQuality      = ShadowQuality.High,
            textureQuality     = TextureQuality.Full,
            antiAliasing       = AntiAliasingMode.SMAA,
            oceanQuality       = OceanQuality.High,
            weatherParticleLimit = 20000,
            viewDistance       = 150000f,
            cesiumTileCacheSize = 1024,
            enablePostProcessing = true,
            enableVolumetrics  = true
        };

        private static PlatformProfile CreateMedium() => new PlatformProfile
        {
            targetFrameRate    = 60,
            renderScale        = 1f,
            lodBias            = 1f,
            shadowQuality      = ShadowQuality.Medium,
            textureQuality     = TextureQuality.Full,
            antiAliasing       = AntiAliasingMode.FXAA,
            oceanQuality       = OceanQuality.Medium,
            weatherParticleLimit = 10000,
            viewDistance       = 100000f,
            cesiumTileCacheSize = 512,
            enablePostProcessing = true,
            enableVolumetrics  = false
        };

        private static PlatformProfile CreateLow() => new PlatformProfile
        {
            targetFrameRate    = 30,
            renderScale        = 0.75f,
            lodBias            = 0.5f,
            shadowQuality      = ShadowQuality.Low,
            textureQuality     = TextureQuality.Half,
            antiAliasing       = AntiAliasingMode.None,
            oceanQuality       = OceanQuality.Low,
            weatherParticleLimit = 2000,
            viewDistance       = 50000f,
            cesiumTileCacheSize = 256,
            enablePostProcessing = false,
            enableVolumetrics  = false
        };

        private static PlatformProfile CreatePotato() => new PlatformProfile
        {
            targetFrameRate    = 30,
            renderScale        = 0.5f,
            lodBias            = 0.25f,
            shadowQuality      = ShadowQuality.Off,
            textureQuality     = TextureQuality.Quarter,
            antiAliasing       = AntiAliasingMode.None,
            oceanQuality       = OceanQuality.Low,
            weatherParticleLimit = 500,
            viewDistance       = 30000f,
            cesiumTileCacheSize = 128,
            enablePostProcessing = false,
            enableVolumetrics  = false
        };
    }
}
