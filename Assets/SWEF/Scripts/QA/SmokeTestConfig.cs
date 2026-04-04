// SmokeTestConfig.cs — SWEF Phase 102: Final QA & Release Candidate Prep
// Per-platform smoke test configuration that specifies which QA checklist
// items are required to pass before a build can be promoted to RC status.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.QA
{
    // ── Enums ────────────────────────────────────────────────────────────────────

    /// <summary>Identifies a smoke-test tier/gate for a specific platform build.</summary>
    public enum SmokePlatform
    {
        WindowsPC,
        macOS,
        iOS,
        Android,
        iPad,
        AndroidTablet
    }

    // ── Config ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Smoke test configuration for a single target platform.
    ///
    /// <para>A smoke test is a minimal required-pass subset of the full QA checklist.
    /// All <see cref="RequiredItemIds"/> must yield <see cref="QAResult.Pass"/>
    /// before the build is considered promotable for that platform.</para>
    /// </summary>
    [Serializable]
    public sealed class PlatformSmokeTestConfig
    {
        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>Target platform for this smoke test config.</summary>
        public SmokePlatform Platform;

        /// <summary>Human-readable platform display name.</summary>
        public string DisplayName;

        // ── Gate ─────────────────────────────────────────────────────────────

        /// <summary>
        /// IDs of <see cref="QAChecklistItem"/>s from <see cref="FinalQAChecklist"/>
        /// that must pass to clear the smoke gate for this platform.
        /// </summary>
        public string[] RequiredItemIds;

        /// <summary>
        /// IDs of items that should be run but are informational only (won't block promotion).
        /// </summary>
        public string[] InformationalItemIds;

        // ── Build ID ──────────────────────────────────────────────────────────

        /// <summary>Unity build target string used in CI (e.g. "StandaloneWindows64").</summary>
        public string UnityBuildTarget;

        /// <summary>Minimum target FPS for this platform to pass the smoke gate.</summary>
        public int MinTargetFps;

        // ── Evaluation ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> if every required item in <paramref name="checklist"/>
        /// has a <see cref="QAResult.Pass"/> result.
        /// </summary>
        public bool Evaluate(FinalQAChecklist checklist)
        {
            if (checklist == null) throw new ArgumentNullException(nameof(checklist));
            foreach (var id in RequiredItemIds)
            {
                var item = checklist.GetById(id);
                if (item == null || item.Result != QAResult.Pass)
                    return false;
            }
            return true;
        }
    }

    // ── Registry ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Static registry of smoke-test configurations for all SWEF target platforms.
    /// </summary>
    public static class SmokeTestConfig
    {
        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Returns all six per-platform smoke configs.</summary>
        public static IReadOnlyList<PlatformSmokeTestConfig> All => s_all;

        /// <summary>Returns the config for the given platform.</summary>
        public static PlatformSmokeTestConfig Get(SmokePlatform platform)
        {
            foreach (var cfg in s_all)
                if (cfg.Platform == platform) return cfg;
            return null;
        }

        // ── Data ──────────────────────────────────────────────────────────────

        private static readonly PlatformSmokeTestConfig[] s_all =
        {
            // ── Windows PC ──────────────────────────────────────────────────────
            new PlatformSmokeTestConfig
            {
                Platform          = SmokePlatform.WindowsPC,
                DisplayName       = "Windows PC (x64)",
                UnityBuildTarget  = "StandaloneWindows64",
                MinTargetFps      = 60,
                RequiredItemIds   = new[]
                {
                    "FP-001", "FP-002", "FP-003",       // Core flight physics
                    "CT-002", "CT-003", "CT-004",        // WASD + gamepad + keybind
                    "CES-001", "CES-002",                // Cesium tile streaming
                    "HUD-001", "HUD-003",                // HUD instruments + warnings
                    "PERF-001",                          // 60 fps on PC spec
                    "PLT-001"                            // Standalone build boots
                },
                InformationalItemIds = new[]
                {
                    "ARIA-001", "ARIA-002", "AU-001", "MP-001"
                }
            },

            // ── macOS ───────────────────────────────────────────────────────────
            new PlatformSmokeTestConfig
            {
                Platform          = SmokePlatform.macOS,
                DisplayName       = "macOS (Universal)",
                UnityBuildTarget  = "StandaloneOSX",
                MinTargetFps      = 60,
                RequiredItemIds   = new[]
                {
                    "FP-001", "FP-002",
                    "CT-002", "CT-003",
                    "CES-001",
                    "HUD-001",
                    "PERF-001",
                    "PLT-002"                            // Notarisation / Gatekeeper
                },
                InformationalItemIds = new[]
                {
                    "ARIA-001", "AU-001"
                }
            },

            // ── iOS ─────────────────────────────────────────────────────────────
            new PlatformSmokeTestConfig
            {
                Platform          = SmokePlatform.iOS,
                DisplayName       = "iOS (ARM64)",
                UnityBuildTarget  = "iOS",
                MinTargetFps      = 30,
                RequiredItemIds   = new[]
                {
                    "FP-001", "FP-002",
                    "CT-001",                            // Touch controls
                    "CES-001", "CES-004",               // Cesium + offline fallback
                    "GPS-001", "GPS-002",                // GPS permission + accuracy
                    "HUD-001", "HUD-002",                // HUD + phone layout
                    "PERF-002", "PERF-003",              // 30 fps + memory
                    "PLT-003"                            // TestFlight install
                },
                InformationalItemIds = new[]
                {
                    "ARIA-001", "WX-001", "AU-001"
                }
            },

            // ── Android ─────────────────────────────────────────────────────────
            new PlatformSmokeTestConfig
            {
                Platform          = SmokePlatform.Android,
                DisplayName       = "Android (ARM64 + ARMv7)",
                UnityBuildTarget  = "Android",
                MinTargetFps      = 30,
                RequiredItemIds   = new[]
                {
                    "FP-001", "FP-002",
                    "CT-001",
                    "CES-001", "CES-004",
                    "GPS-001", "GPS-002",
                    "HUD-001", "HUD-002",
                    "PERF-002", "PERF-003",
                    "PLT-004"                            // APK sideload
                },
                InformationalItemIds = new[]
                {
                    "ARIA-001", "WX-001", "AU-001"
                }
            },

            // ── iPad ────────────────────────────────────────────────────────────
            new PlatformSmokeTestConfig
            {
                Platform          = SmokePlatform.iPad,
                DisplayName       = "iPad (iPadOS / ARM64)",
                UnityBuildTarget  = "iOS",
                MinTargetFps      = 60,
                RequiredItemIds   = new[]
                {
                    "FP-001",
                    "CT-001", "CT-005",                 // Touch + dual-stick tablet
                    "CES-001",
                    "GPS-001",
                    "HUD-002",                           // Tablet HUD layout
                    "PERF-002",
                    "PLT-005"                            // Tablet layout active
                },
                InformationalItemIds = new[]
                {
                    "ARIA-001", "AU-001"
                }
            },

            // ── Android Tablet ───────────────────────────────────────────────────
            new PlatformSmokeTestConfig
            {
                Platform          = SmokePlatform.AndroidTablet,
                DisplayName       = "Android Tablet (ARM64)",
                UnityBuildTarget  = "Android",
                MinTargetFps      = 30,
                RequiredItemIds   = new[]
                {
                    "FP-001",
                    "CT-001", "CT-005",
                    "CES-001",
                    "GPS-001",
                    "HUD-002",
                    "PERF-002",
                    "PLT-006"                            // Tablet layout active
                },
                InformationalItemIds = new[]
                {
                    "ARIA-001", "AU-001"
                }
            }
        };
    }
}
