#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SWEF.Build
{
    /// <summary>
    /// Static build pipeline providing one-click iOS and Android builds from the
    /// Unity Editor menu and from CI via static entry-point methods.
    ///
    /// Access via <b>SWEF → Build → Build iOS</b>, <b>Build Android</b>, or <b>Build Both</b>.
    /// CI uses <see cref="BuildiOSCLI"/> and <see cref="BuildAndroidCLI"/> as the
    /// <c>buildMethod</c> argument for game-ci/unity-builder.
    /// </summary>
    public static class BuildPipeline
    {
        // ── Config paths ─────────────────────────────────────────────────────
        private const string k_ConfigPath    = "Assets/SWEF/Config/SWEFBuildConfig.asset";
        private const string k_BuildRootIOS  = "Builds/iOS";
        private const string k_BuildRootAndroid = "Builds/Android";
        private const string k_BootScene     = "Assets/SWEF/Scenes/Boot.unity";
        private const string k_WorldScene    = "Assets/SWEF/Scenes/World.unity";

        // ── Menu items ───────────────────────────────────────────────────────

        /// <summary>Configures PlayerSettings for iOS and produces an Xcode project.</summary>
        [MenuItem("SWEF/Build/Build iOS")]
        public static void BuildiOS()
        {
            var config = LoadConfig();
            if (config == null) return;
            if (!ValidateScenes()) return;

            ApplyBuildConfig(config);
            config.IncrementBuildNumber();

            Directory.CreateDirectory(k_BuildRootIOS);
            var options = new BuildPlayerOptions
            {
                scenes         = GetScenePaths(),
                locationPathName = k_BuildRootIOS,
                target         = BuildTarget.iOS,
                options        = config.developmentBuild
                    ? BuildOptions.Development | BuildOptions.AllowDebugging
                    : BuildOptions.None
            };

            LogBuildReport(UnityEditor.BuildPipeline.BuildPlayer(options), "iOS");
        }

        /// <summary>Configures PlayerSettings for Android and produces an AAB.</summary>
        [MenuItem("SWEF/Build/Build Android")]
        public static void BuildAndroid()
        {
            var config = LoadConfig();
            if (config == null) return;
            if (!ValidateScenes()) return;

            ApplyBuildConfig(config);
            config.IncrementBuildNumber();

            Directory.CreateDirectory(k_BuildRootAndroid);
            string aabPath = Path.Combine(k_BuildRootAndroid, $"swef-{config.version}-{config.buildNumber}.aab");

            EditorUserBuildSettings.buildAppBundle = true;

            var options = new BuildPlayerOptions
            {
                scenes           = GetScenePaths(),
                locationPathName = aabPath,
                target           = BuildTarget.Android,
                options          = config.developmentBuild
                    ? BuildOptions.Development | BuildOptions.AllowDebugging
                    : BuildOptions.None
            };

            LogBuildReport(UnityEditor.BuildPipeline.BuildPlayer(options), "Android");
        }

        /// <summary>Builds iOS then Android in sequence.</summary>
        [MenuItem("SWEF/Build/Build Both")]
        public static void BuildBoth()
        {
            BuildiOS();
            BuildAndroid();
        }

        // ── CI entry points ──────────────────────────────────────────────────

        /// <summary>
        /// CLI entry point for game-ci/unity-builder iOS builds.
        /// Reads keystore/signing values from environment variables injected by CI.
        /// </summary>
        public static void BuildiOSCLI()
        {
            InjectCIEnvironmentConfig();
            BuildiOS();
        }

        /// <summary>
        /// CLI entry point for game-ci/unity-builder Android builds.
        /// Reads keystore/signing values from environment variables injected by CI.
        /// </summary>
        public static void BuildAndroidCLI()
        {
            InjectCIEnvironmentConfig();
            BuildAndroid();
        }

        // ── Shared configuration ─────────────────────────────────────────────

        /// <summary>
        /// Applies all settings from <paramref name="config"/> to Unity PlayerSettings.
        /// Called automatically before each platform build.
        /// </summary>
        public static void ApplyBuildConfig(BuildConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            PlayerSettings.companyName                = config.companyName;
            PlayerSettings.productName                = config.appName;
            PlayerSettings.bundleVersion              = config.version;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS,     config.scriptingBackend);
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, config.scriptingBackend);

            // iOS
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, config.bundleId);
            PlayerSettings.iOS.buildNumber            = config.buildNumber.ToString();
            PlayerSettings.iOS.appleDeveloperTeamID   = config.iosTeamId;
            PlayerSettings.iOS.iOSManualProvisioningProfileID = config.iosProvisioningProfile;
            PlayerSettings.iOS.appleEnableAutomaticSigning    = config.iosAutoSign;
            PlayerSettings.iOS.targetOSVersionString  = config.iosMinVersion;

            // Android
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, config.bundleId);
            PlayerSettings.Android.bundleVersionCode  = config.buildNumber;
            PlayerSettings.Android.keystoreName       = config.androidKeystorePath;
            PlayerSettings.Android.keystorePass       = config.androidKeystorePass;
            PlayerSettings.Android.keyaliasName       = config.androidKeyAlias;
            PlayerSettings.Android.keyaliasPass       = config.androidKeyPass;
            PlayerSettings.Android.minSdkVersion      = (AndroidSdkVersions)config.androidMinSdk;
            PlayerSettings.Android.targetSdkVersion   = (AndroidSdkVersions)config.androidTargetSdk;

            // Build quality flags
            PlayerSettings.stripEngineCode            = config.stripEngineCode;
            EditorUserBuildSettings.development       = config.developmentBuild;

            // iOS capabilities
            PlayerSettings.iOS.locationUsageDescription = "SWEF uses your GPS location as your flight start position.";

            Debug.Log("[SWEF] BuildPipeline: PlayerSettings applied from BuildConfig.");
        }

        // ── Internal helpers ─────────────────────────────────────────────────

        private static BuildConfig LoadConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<BuildConfig>(k_ConfigPath);
            if (config == null)
                Debug.LogError($"[SWEF] BuildPipeline: BuildConfig not found at {k_ConfigPath}. " +
                               "Create one via Assets → Create → SWEF → Build Config.");
            return config;
        }

        private static bool ValidateScenes()
        {
            var scenes = EditorBuildSettings.scenes;

            if (scenes.Length < 2)
            {
                Debug.LogError("[SWEF] BuildPipeline: fewer than 2 scenes in Build Settings. " +
                               "Add Boot at index 0 and World at index 1.");
                return false;
            }

            string boot  = Path.GetFileNameWithoutExtension(scenes[0].path);
            string world = Path.GetFileNameWithoutExtension(scenes.Length > 1 ? scenes[1].path : "");

            if (!boot.Equals("Boot", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError($"[SWEF] BuildPipeline: scene at index 0 should be 'Boot', found '{boot}'.");
                return false;
            }

            if (!world.Equals("World", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError($"[SWEF] BuildPipeline: scene at index 1 should be 'World', found '{world}'.");
                return false;
            }

            return true;
        }

        private static string[] GetScenePaths()
            => EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

        private static void LogBuildReport(BuildReport report, string platform)
        {
            var summary = report.summary;
            Debug.Log($"[SWEF] BuildPipeline [{platform}]: result={summary.result} " +
                      $"size={summary.totalSize / 1024 / 1024:F1}MB " +
                      $"time={summary.totalTime.TotalSeconds:F1}s " +
                      $"errors={summary.totalErrors} warnings={summary.totalWarnings}");

            if (summary.result != BuildResult.Succeeded)
                Debug.LogError($"[SWEF] BuildPipeline [{platform}]: build FAILED — check the Build Report window.");
        }

        private static void InjectCIEnvironmentConfig()
        {
            // game-ci/unity-builder injects keystore values as environment variables.
            // Read them and write to the BuildConfig if it exists.
            var config = AssetDatabase.LoadAssetAtPath<BuildConfig>(k_ConfigPath);
            if (config == null) return;

            string keystorePass = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASS");
            string keyaliasPass = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_PASS");

            if (!string.IsNullOrEmpty(keystorePass)) config.androidKeystorePass = keystorePass;
            if (!string.IsNullOrEmpty(keyaliasPass)) config.androidKeyPass      = keyaliasPass;

            string teamId = Environment.GetEnvironmentVariable("IOS_TEAM_ID");
            if (!string.IsNullOrEmpty(teamId)) config.iosTeamId = teamId;
        }
    }
}
#endif
