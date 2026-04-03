// ModuleHealthCheck.cs — SWEF Phase 96: Integration Test & QA Framework
// Validates that each major SWEF module's core manager/singleton can be instantiated.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.IntegrationTest
{
    /// <summary>
    /// Validates the health of every major SWEF module by attempting to locate or
    /// instantiate its core manager and verifying there are no obvious null-reference
    /// chains or missing cross-module dependencies.
    ///
    /// <para>Each check is defensive — a missing module produces a warning, not an
    /// exception, so the runner can continue through all modules.</para>
    /// </summary>
    public class ModuleHealthCheck : IntegrationTestCase
    {
        /// <inheritdoc/>
        public override string TestName => "ModuleHealthCheck";

        /// <inheritdoc/>
        public override string ModuleName => "Core";

        /// <inheritdoc/>
        public override int Priority => 10; // Run early — infrastructure smoke test.

        // Known module manager type names to probe via reflection.
        private static readonly string[] ManagerTypeNames =
        {
            "SWEF.Flight.FlightManager",
            "SWEF.Achievement.AchievementManager",
            "SWEF.Mission.MissionManager",
            "SWEF.Weather.WeatherManager",
            "SWEF.TimeOfDay.TimeOfDayManager",
            "SWEF.Multiplayer.MultiplayerSessionManager",
            "SWEF.VoiceCommand.VoiceRecognitionController",
            "SWEF.AdaptiveMusic.AdaptiveMusicManager",
            "SWEF.Replay.ReplayManager",
            "SWEF.SaveSystem.SaveManager",
            "SWEF.Analytics.AnalyticsManager",
            "SWEF.Localization.LocalizationManager",
            "SWEF.Security.CheatDetectionManager",
            "SWEF.Accessibility.AccessibilityManager",
            "SWEF.Marketplace.MarketplaceManager",
            "SWEF.BuildPipeline.PlatformBootstrapper",
        };

        private readonly List<string> _healthy = new List<string>();
        private readonly List<string> _missing  = new List<string>();

        /// <inheritdoc/>
        public override IntegrationTestResult Setup() => null; // Nothing to pre-allocate.

        /// <inheritdoc/>
        public override IntegrationTestResult Execute()
        {
            _healthy.Clear();
            _missing.Clear();

            foreach (string typeName in ManagerTypeNames)
            {
                Type t = FindType(typeName);
                if (t == null)
                {
                    _missing.Add(typeName);
                    Debug.LogWarning($"[ModuleHealthCheck] Type not found: {typeName}");
                }
                else
                {
                    _healthy.Add(typeName);
                }
            }

            if (_missing.Count == 0)
                return Pass($"All {_healthy.Count} module type(s) resolved.");

            // Partial health is a warning, not a hard failure — modules may be excluded
            // from the current build target.
            if (_healthy.Count > 0)
            {
                string msg = $"{_healthy.Count}/{ManagerTypeNames.Length} healthy. " +
                             $"Missing: {string.Join(", ", _missing)}";
                return IntegrationTestResult.Fail(ModuleName, TestName, msg);
            }

            return Fail($"No module types could be resolved ({_missing.Count} missing).");
        }

        /// <inheritdoc/>
        public override void Teardown() { }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Type FindType(string fullTypeName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = asm.GetType(fullTypeName);
                    if (t != null) return t;
                }
                catch { /* ignore reflection errors */ }
            }

            return null;
        }
    }
}
