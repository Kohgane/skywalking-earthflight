// PerformanceBenchmarkConfig.cs — SWEF Phase 102: Final QA & Release Candidate Prep
// Defines minimum performance targets per platform that QA must verify before
// the build is promoted to Release Candidate status.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.QA
{
    // ── Memory budget record ──────────────────────────────────────────────────────

    /// <summary>Memory budget limits for a single platform.</summary>
    [Serializable]
    public sealed class MemoryBudget
    {
        /// <summary>Maximum total RAM usage in megabytes.</summary>
        public int TotalRamMB;

        /// <summary>Maximum GPU memory / VRAM usage in megabytes.</summary>
        public int GpuVramMB;

        /// <summary>Maximum managed C# heap growth over a 15-minute session (MB).</summary>
        public int ManagedHeapGrowthMB;

        /// <summary>Maximum single texture atlas size in megabytes.</summary>
        public int MaxTextureAtlasMB;
    }

    // ── Network budget record ─────────────────────────────────────────────────────

    /// <summary>Network bandwidth and latency requirements.</summary>
    [Serializable]
    public sealed class NetworkBudget
    {
        /// <summary>Minimum recommended download bandwidth in Mbps for full-quality tiles.</summary>
        public float RecommendedBandwidthMbps;

        /// <summary>Minimum download speed in Mbps for a degraded-but-functional experience.</summary>
        public float MinimumBandwidthMbps;

        /// <summary>Maximum acceptable round-trip multiplayer latency in milliseconds.</summary>
        public int MaxMultiplayerLatencyMs;
    }

    // ── Tile streaming budget ─────────────────────────────────────────────────────

    /// <summary>Cesium 3D Tile streaming budget per platform.</summary>
    [Serializable]
    public sealed class TileStreamingBudget
    {
        /// <summary>Maximum number of simultaneously active tile objects.</summary>
        public int MaxActiveTiles;

        /// <summary>Maximum tile GPU memory budget in megabytes.</summary>
        public int GpuTileMemoryMB;

        /// <summary>Maximum tiles loaded per second (to stay within bandwidth budget).</summary>
        public float MaxTilesPerSecond;
    }

    // ── Per-platform benchmark config ─────────────────────────────────────────────

    /// <summary>
    /// Minimum performance targets for a single target platform.
    /// </summary>
    [Serializable]
    public sealed class PlatformBenchmarkConfig
    {
        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>Target platform name (e.g. "Windows PC", "iOS").</summary>
        public string PlatformName;

        // ── FPS targets ───────────────────────────────────────────────────────

        /// <summary>Target sustained frames per second (must be met on reference hardware).</summary>
        public int TargetFps;

        /// <summary>Minimum acceptable average FPS; below this the build fails the benchmark.</summary>
        public int MinAcceptableFps;

        /// <summary>Maximum allowed single-frame spike duration in milliseconds.</summary>
        public float MaxFrameSpikeMs;

        // ── Memory budget ─────────────────────────────────────────────────────

        /// <summary>Memory budgets for this platform.</summary>
        public MemoryBudget Memory;

        // ── Tile streaming budget ─────────────────────────────────────────────

        /// <summary>Cesium tile streaming budgets for this platform.</summary>
        public TileStreamingBudget Tiles;

        // ── Network requirements ──────────────────────────────────────────────

        /// <summary>Network bandwidth requirements for this platform.</summary>
        public NetworkBudget Network;

        // ── Reference scenario ────────────────────────────────────────────────

        /// <summary>Description of the reference scenario used to measure these targets.</summary>
        public string ReferenceScenario;

        /// <summary>Reference hardware specification used for the benchmark.</summary>
        public string ReferenceHardware;
    }

    // ── Registry ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Static registry of per-platform performance benchmark targets for SWEF v1.0.0-rc1.
    ///
    /// <para>These targets must be validated by QA before the build is promoted
    /// to Release Candidate status. See RELEASE_NOTES_v1.0.0-rc1.md for rationale.</para>
    /// </summary>
    public static class PerformanceBenchmarkConfig
    {
        // ── Public accessors ──────────────────────────────────────────────────

        /// <summary>Returns all platform benchmark configs.</summary>
        public static IReadOnlyList<PlatformBenchmarkConfig> All => s_all;

        /// <summary>Returns the config whose <see cref="PlatformBenchmarkConfig.PlatformName"/>
        /// starts with <paramref name="platformNamePrefix"/> (case-insensitive).</summary>
        public static PlatformBenchmarkConfig Get(string platformNamePrefix)
        {
            foreach (var cfg in s_all)
                if (cfg.PlatformName.StartsWith(platformNamePrefix, StringComparison.OrdinalIgnoreCase))
                    return cfg;
            return null;
        }

        /// <summary>Convenience accessor — Windows PC config.</summary>
        public static PlatformBenchmarkConfig WindowsPC  => s_all[0];

        /// <summary>Convenience accessor — macOS config.</summary>
        public static PlatformBenchmarkConfig macOS       => s_all[1];

        /// <summary>Convenience accessor — iOS config.</summary>
        public static PlatformBenchmarkConfig iOS         => s_all[2];

        /// <summary>Convenience accessor — Android config.</summary>
        public static PlatformBenchmarkConfig Android     => s_all[3];

        /// <summary>Convenience accessor — iPad config.</summary>
        public static PlatformBenchmarkConfig iPad        => s_all[4];

        /// <summary>Convenience accessor — Android Tablet config.</summary>
        public static PlatformBenchmarkConfig AndroidTablet => s_all[5];

        // ── Data ──────────────────────────────────────────────────────────────

        private static readonly PlatformBenchmarkConfig[] s_all =
        {
            // ── Windows PC (x64) ──────────────────────────────────────────────
            new PlatformBenchmarkConfig
            {
                PlatformName      = "Windows PC",
                TargetFps         = 60,
                MinAcceptableFps  = 45,
                MaxFrameSpikeMs   = 33f,
                ReferenceHardware = "NVIDIA GTX 1060 6GB / AMD Ryzen 5 3600 / 16 GB RAM",
                ReferenceScenario = "Cruise at 1 000 m AGL over an urban area; camera panning; weather: Overcast",
                Memory = new MemoryBudget
                {
                    TotalRamMB            = 4096,
                    GpuVramMB             = 3072,
                    ManagedHeapGrowthMB   = 50,
                    MaxTextureAtlasMB     = 512
                },
                Tiles = new TileStreamingBudget
                {
                    MaxActiveTiles    = 512,
                    GpuTileMemoryMB   = 1024,
                    MaxTilesPerSecond = 20f
                },
                Network = new NetworkBudget
                {
                    RecommendedBandwidthMbps = 50f,
                    MinimumBandwidthMbps     = 10f,
                    MaxMultiplayerLatencyMs  = 200
                }
            },

            // ── macOS (Universal) ─────────────────────────────────────────────
            new PlatformBenchmarkConfig
            {
                PlatformName      = "macOS",
                TargetFps         = 60,
                MinAcceptableFps  = 45,
                MaxFrameSpikeMs   = 33f,
                ReferenceHardware = "Apple M1 / 8 GB Unified Memory",
                ReferenceScenario = "Cruise at 1 000 m AGL over an urban area; camera panning; weather: Clear",
                Memory = new MemoryBudget
                {
                    TotalRamMB            = 3072,
                    GpuVramMB             = 0,    // Unified memory — tracked via TotalRam
                    ManagedHeapGrowthMB   = 50,
                    MaxTextureAtlasMB     = 512
                },
                Tiles = new TileStreamingBudget
                {
                    MaxActiveTiles    = 512,
                    GpuTileMemoryMB   = 1024,
                    MaxTilesPerSecond = 20f
                },
                Network = new NetworkBudget
                {
                    RecommendedBandwidthMbps = 50f,
                    MinimumBandwidthMbps     = 10f,
                    MaxMultiplayerLatencyMs  = 200
                }
            },

            // ── iOS (ARM64) ───────────────────────────────────────────────────
            new PlatformBenchmarkConfig
            {
                PlatformName      = "iOS",
                TargetFps         = 30,
                MinAcceptableFps  = 28,
                MaxFrameSpikeMs   = 50f,
                ReferenceHardware = "iPhone 12 (A14 Bionic) / 4 GB RAM",
                ReferenceScenario = "Cruise at 500 m AGL over suburbs; touch controls active; Clear weather",
                Memory = new MemoryBudget
                {
                    TotalRamMB            = 1536,
                    GpuVramMB             = 0,    // Unified memory
                    ManagedHeapGrowthMB   = 30,
                    MaxTextureAtlasMB     = 256
                },
                Tiles = new TileStreamingBudget
                {
                    MaxActiveTiles    = 128,
                    GpuTileMemoryMB   = 384,
                    MaxTilesPerSecond = 8f
                },
                Network = new NetworkBudget
                {
                    RecommendedBandwidthMbps = 20f,
                    MinimumBandwidthMbps     = 5f,
                    MaxMultiplayerLatencyMs  = 300
                }
            },

            // ── Android (ARM64 + ARMv7) ───────────────────────────────────────
            new PlatformBenchmarkConfig
            {
                PlatformName      = "Android",
                TargetFps         = 30,
                MinAcceptableFps  = 28,
                MaxFrameSpikeMs   = 50f,
                ReferenceHardware = "Google Pixel 7 (Tensor G2) / 8 GB RAM",
                ReferenceScenario = "Cruise at 500 m AGL over suburbs; touch controls active; Clear weather",
                Memory = new MemoryBudget
                {
                    TotalRamMB            = 1536,
                    GpuVramMB             = 512,
                    ManagedHeapGrowthMB   = 30,
                    MaxTextureAtlasMB     = 256
                },
                Tiles = new TileStreamingBudget
                {
                    MaxActiveTiles    = 128,
                    GpuTileMemoryMB   = 384,
                    MaxTilesPerSecond = 8f
                },
                Network = new NetworkBudget
                {
                    RecommendedBandwidthMbps = 20f,
                    MinimumBandwidthMbps     = 5f,
                    MaxMultiplayerLatencyMs  = 300
                }
            },

            // ── iPad (iPadOS ARM64) ───────────────────────────────────────────
            new PlatformBenchmarkConfig
            {
                PlatformName      = "iPad",
                TargetFps         = 60,
                MinAcceptableFps  = 45,
                MaxFrameSpikeMs   = 33f,
                ReferenceHardware = "iPad Air 5 (M1) / 8 GB RAM",
                ReferenceScenario = "Cruise at 1 000 m AGL; tablet split-panel HUD active; dual-stick touch",
                Memory = new MemoryBudget
                {
                    TotalRamMB            = 2048,
                    GpuVramMB             = 0,    // Unified memory
                    ManagedHeapGrowthMB   = 40,
                    MaxTextureAtlasMB     = 512
                },
                Tiles = new TileStreamingBudget
                {
                    MaxActiveTiles    = 256,
                    GpuTileMemoryMB   = 512,
                    MaxTilesPerSecond = 12f
                },
                Network = new NetworkBudget
                {
                    RecommendedBandwidthMbps = 20f,
                    MinimumBandwidthMbps     = 5f,
                    MaxMultiplayerLatencyMs  = 250
                }
            },

            // ── Android Tablet (ARM64) ────────────────────────────────────────
            new PlatformBenchmarkConfig
            {
                PlatformName      = "Android Tablet",
                TargetFps         = 30,
                MinAcceptableFps  = 28,
                MaxFrameSpikeMs   = 50f,
                ReferenceHardware = "Samsung Galaxy Tab S8 (Snapdragon 8 Gen 1) / 8 GB RAM",
                ReferenceScenario = "Cruise at 1 000 m AGL; tablet split-panel HUD active; dual-stick touch",
                Memory = new MemoryBudget
                {
                    TotalRamMB            = 2048,
                    GpuVramMB             = 512,
                    ManagedHeapGrowthMB   = 40,
                    MaxTextureAtlasMB     = 512
                },
                Tiles = new TileStreamingBudget
                {
                    MaxActiveTiles    = 192,
                    GpuTileMemoryMB   = 512,
                    MaxTilesPerSecond = 10f
                },
                Network = new NetworkBudget
                {
                    RecommendedBandwidthMbps = 20f,
                    MinimumBandwidthMbps     = 5f,
                    MaxMultiplayerLatencyMs  = 300
                }
            }
        };
    }
}
