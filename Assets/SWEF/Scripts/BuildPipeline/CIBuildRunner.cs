// CIBuildRunner.cs — SWEF Phase 95: Platform Target Matrix & Build Pipeline
// Command-line CI/CD build runner for all primary SWEF platforms.
//
// Usage (from shell / Unity batch mode):
//   unity -batchmode -executeMethod SWEF.BuildPipeline.CIBuildRunner.BuildWindows
//   unity -batchmode -executeMethod SWEF.BuildPipeline.CIBuildRunner.BuildMacOS
//   unity -batchmode -executeMethod SWEF.BuildPipeline.CIBuildRunner.BuildiOS
//   unity -batchmode -executeMethod SWEF.BuildPipeline.CIBuildRunner.BuildAndroid
//   unity -batchmode -executeMethod SWEF.BuildPipeline.CIBuildRunner.BuildAll
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SWEF.BuildPipeline
{
    /// <summary>
    /// Static class invoked from CI/CD pipelines via Unity's batch-mode
    /// <c>-executeMethod</c> argument.
    ///
    /// <para>Each method loads the matching <see cref="BuildProfileConfig"/>
    /// from <c>Resources/BuildPipeline/</c> (falling back to a generated
    /// default), configures Unity's <see cref="PlayerSettings"/> and
    /// <see cref="BuildPlayerOptions"/>, then performs the build.</para>
    /// </summary>
    public static class CIBuildRunner
    {
        // ── Output root (overridable via env-var BUILD_OUTPUT_PATH) ──────────────
        private static string OutputRoot =>
            Environment.GetEnvironmentVariable("BUILD_OUTPUT_PATH")
            ?? Path.Combine(Directory.GetCurrentDirectory(), "Builds");

        // ── Public entry points ───────────────────────────────────────────────────

        /// <summary>Build for Windows (x86-64).</summary>
        public static void BuildWindows() =>
            RunBuild(PlatformTarget.WindowsPC,
                     BuildTarget.StandaloneWindows64,
                     Path.Combine(OutputRoot, "Windows", "SWEF.exe"));

        /// <summary>Build for macOS (Universal).</summary>
        public static void BuildMacOS() =>
            RunBuild(PlatformTarget.macOS,
                     BuildTarget.StandaloneOSX,
                     Path.Combine(OutputRoot, "macOS", "SWEF.app"));

        /// <summary>Build for iOS (Xcode project output).</summary>
        public static void BuildiOS() =>
            RunBuild(PlatformTarget.iOS,
                     BuildTarget.iOS,
                     Path.Combine(OutputRoot, "iOS"));

        /// <summary>Build for Android (APK or AAB).</summary>
        public static void BuildAndroid() =>
            RunBuild(PlatformTarget.Android,
                     BuildTarget.Android,
                     Path.Combine(OutputRoot, "Android", "SWEF.apk"));

        /// <summary>Sequentially build all four primary platforms.</summary>
        public static void BuildAll()
        {
            BuildWindows();
            BuildMacOS();
            BuildiOS();
            BuildAndroid();
        }

        // ── Core build logic ──────────────────────────────────────────────────────

        private static void RunBuild(PlatformTarget swefTarget,
                                     BuildTarget unityTarget,
                                     string outputPath)
        {
            BuildProfileConfig profile = LoadProfile(swefTarget);
            ApplyPlayerSettings(profile);

            // Ensure output directory exists
            string dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var options = new BuildPlayerOptions
            {
                scenes      = GetEnabledScenes(),
                locationPathName = outputPath,
                target      = unityTarget,
                options     = profile.buildOptions
            };

            DateTime start = DateTime.UtcNow;
            Debug.Log($"[CIBuildRunner] Starting {swefTarget} build → {outputPath}");

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            TimeSpan elapsed = DateTime.UtcNow - start;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[CIBuildRunner] ✅ {swefTarget} build SUCCEEDED in {elapsed.TotalSeconds:F1}s " +
                          $"| Size: {summary.totalSize / 1024 / 1024} MB | Path: {outputPath}");
            }
            else
            {
                Debug.LogError($"[CIBuildRunner] ❌ {swefTarget} build FAILED after {elapsed.TotalSeconds:F1}s " +
                               $"| Errors: {summary.totalErrors} | Warnings: {summary.totalWarnings}");
                // In CI (batch mode), exit with non-zero code so the workflow step fails
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads the <see cref="BuildProfileConfig"/> for <paramref name="target"/>
        /// from <c>Resources/BuildPipeline/{target}Profile</c>, or creates a
        /// programmatic default when the asset is absent.
        /// </summary>
        private static BuildProfileConfig LoadProfile(PlatformTarget target)
        {
            string resourcePath = $"BuildPipeline/{target}Profile";
            var profile = Resources.Load<BuildProfileConfig>(resourcePath);
            if (profile == null)
            {
                Debug.LogWarning($"[CIBuildRunner] No BuildProfileConfig found at Resources/{resourcePath}. " +
                                 "Using generated defaults.");
                profile = BuildProfileConfig.CreateDefault(target);
            }
            return profile;
        }

        /// <summary>Applies <see cref="PlayerSettings"/> from the profile.</summary>
        private static void ApplyPlayerSettings(BuildProfileConfig profile)
        {
            PlayerSettings.applicationIdentifier = profile.bundleIdentifier;
            PlayerSettings.productName           = profile.productName;

            if (profile.targetFrameRate > 0)
                Application.targetFrameRate = profile.targetFrameRate;

            if (profile.maxTextureResolution > 0)
                PlayerSettings.SetGraphicsAPIs(
                    EditorUserBuildSettings.activeBuildTarget,
                    PlayerSettings.GetGraphicsAPIs(EditorUserBuildSettings.activeBuildTarget));
        }

        /// <summary>Returns all scenes currently enabled in Build Settings.</summary>
        private static string[] GetEnabledScenes()
        {
            var scenes = EditorBuildSettings.scenes;
            var paths  = new System.Collections.Generic.List<string>(scenes.Length);
            foreach (var s in scenes)
                if (s.enabled)
                    paths.Add(s.path);
            return paths.ToArray();
        }
    }
}
#endif
