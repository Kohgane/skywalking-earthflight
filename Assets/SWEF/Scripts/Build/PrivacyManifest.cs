#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SWEF.Build
{
    /// <summary>
    /// Editor utility that generates platform privacy compliance files:
    /// <list type="bullet">
    ///   <item><description><b>iOS</b> — <c>Assets/Plugins/iOS/PrivacyInfo.xcprivacy</c> (Apple Privacy Manifest)</description></item>
    ///   <item><description><b>Android</b> — <c>Assets/Plugins/Android/data_safety.json</c> (Play Store Data Safety template)</description></item>
    /// </list>
    /// Access via <b>SWEF → Build → Generate Privacy Manifests</b>.
    /// </summary>
    public static class PrivacyManifest
    {
        private const string k_ConfigPath      = "Assets/SWEF/Config/SWEFBuildConfig.asset";
        private const string k_IOSPluginsDir   = "Assets/Plugins/iOS";
        private const string k_AndroidPluginsDir = "Assets/Plugins/Android";
        private const string k_IOSManifestPath = "Assets/Plugins/iOS/PrivacyInfo.xcprivacy";
        private const string k_AndroidSafetyPath = "Assets/Plugins/Android/data_safety.json";

        // ── Menu item ────────────────────────────────────────────────────────

        /// <summary>Generates both the iOS and Android privacy manifests from the active BuildConfig.</summary>
        [MenuItem("SWEF/Build/Generate Privacy Manifests")]
        public static void GenerateAll()
        {
            var config = AssetDatabase.LoadAssetAtPath<BuildConfig>(k_ConfigPath);
            if (config == null)
            {
                Debug.LogError($"[SWEF] PrivacyManifest: BuildConfig not found at {k_ConfigPath}.");
                return;
            }

            GenerateIOSPrivacyManifest(config);
            GenerateAndroidDataSafety(config);
            AssetDatabase.Refresh();
            Debug.Log("[SWEF] PrivacyManifest: privacy manifests generated successfully.");
        }

        // ── iOS ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Generates an Apple Privacy Manifest (<c>PrivacyInfo.xcprivacy</c>) based on
        /// the data collection flags in <paramref name="config"/>.
        /// </summary>
        public static void GenerateIOSPrivacyManifest(BuildConfig config)
        {
            Directory.CreateDirectory(k_IOSPluginsDir);

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">");
            sb.AppendLine("<plist version=\"1.0\">");
            sb.AppendLine("<dict>");

            // Tracking
            sb.AppendLine("\t<key>NSPrivacyTracking</key>");
            sb.AppendLine($"\t<{(config.usesTracking ? "true" : "false")}/>");

            // Tracking domains (empty unless tracking is enabled)
            sb.AppendLine("\t<key>NSPrivacyTrackingDomains</key>");
            sb.AppendLine("\t<array/>");

            // Collected data types
            sb.AppendLine("\t<key>NSPrivacyCollectedDataTypes</key>");
            sb.AppendLine("\t<array>");

            if (config.usesLocationServices)
            {
                AppendIOSDataType(sb, "NSPrivacyCollectedDataTypePreciseLocation",
                    linked: true, tracking: config.usesTracking, purposes: new[] { "NSPrivacyCollectedDataTypePurposeAppFunctionality" });
                AppendIOSDataType(sb, "NSPrivacyCollectedDataTypeCoarseLocation",
                    linked: false, tracking: false, purposes: new[] { "NSPrivacyCollectedDataTypePurposeAppFunctionality" });
            }

            if (config.usesPhotoLibrary)
            {
                AppendIOSDataType(sb, "NSPrivacyCollectedDataTypePhotosOrVideos",
                    linked: false, tracking: false, purposes: new[] { "NSPrivacyCollectedDataTypePurposeAppFunctionality" });
            }

            sb.AppendLine("\t</array>");

            // Accessed API types
            sb.AppendLine("\t<key>NSPrivacyAccessedAPITypes</key>");
            sb.AppendLine("\t<array>");

            // UserDefaults (Unity PlayerPrefs)
            AppendIOSAPIType(sb, "NSPrivacyAccessedAPICategoryUserDefaults",
                reasons: new[] { "CA92.1" }); // "Access info from the same app"

            // Disk space
            AppendIOSAPIType(sb, "NSPrivacyAccessedAPICategoryDiskSpace",
                reasons: new[] { "85F4.1" }); // "Display disk space info to user"

            // System boot time (Unity profiling / timing)
            AppendIOSAPIType(sb, "NSPrivacyAccessedAPICategorySystemBootTime",
                reasons: new[] { "35F9.1" }); // "Calculate time on-device"

            sb.AppendLine("\t</array>");
            sb.AppendLine("</dict>");
            sb.AppendLine("</plist>");

            File.WriteAllText(k_IOSManifestPath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[SWEF] PrivacyManifest: iOS manifest written to {k_IOSManifestPath}");
        }

        // ── Android ──────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a Play Store Data Safety JSON template based on the data
        /// collection flags in <paramref name="config"/>.
        /// </summary>
        public static void GenerateAndroidDataSafety(BuildConfig config)
        {
            Directory.CreateDirectory(k_AndroidPluginsDir);

            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"appName\": \"{EscapeJson(config.appName)}\",");
            sb.AppendLine($"  \"bundleId\": \"{EscapeJson(config.bundleId)}\",");
            sb.AppendLine($"  \"version\": \"{EscapeJson(config.version)}\",");
            sb.AppendLine($"  \"privacyPolicyUrl\": \"{EscapeJson(config.privacyPolicyUrl)}\",");
            sb.AppendLine("  \"dataCollected\": [");

            bool needsComma = false;

            if (config.usesLocationServices)
            {
                if (needsComma) sb.AppendLine("    ,");
                sb.AppendLine("    {");
                sb.AppendLine("      \"category\": \"Location\",");
                sb.AppendLine("      \"types\": [\"Approximate location\", \"Precise location\"],");
                sb.AppendLine("      \"purposes\": [\"App functionality\"],");
                sb.AppendLine("      \"isEncryptedInTransit\": true,");
                sb.AppendLine("      \"canUserRequestDeletion\": false");
                sb.AppendLine("    }");
                needsComma = true;
            }

            sb.AppendLine("    ,{");
            sb.AppendLine("      \"category\": \"App activity\",");
            sb.AppendLine("      \"types\": [\"App interactions\", \"In-app search history\"],");
            sb.AppendLine("      \"purposes\": [\"App functionality\", \"Analytics\"],");
            sb.AppendLine("      \"isEncryptedInTransit\": false,");
            sb.AppendLine("      \"canUserRequestDeletion\": true");
            sb.AppendLine("    }");

            if (config.usesTracking)
            {
                sb.AppendLine("    ,{");
                sb.AppendLine("      \"category\": \"Device or other IDs\",");
                sb.AppendLine("      \"types\": [\"Device or other IDs\"],");
                sb.AppendLine("      \"purposes\": [\"Advertising or marketing\"],");
                sb.AppendLine("      \"isEncryptedInTransit\": true,");
                sb.AppendLine("      \"canUserRequestDeletion\": true");
                sb.AppendLine("    }");
            }

            sb.AppendLine("  ],");
            sb.AppendLine("  \"sharesData\": false,");
            sb.AppendLine($"  \"usesTracking\": {(config.usesTracking ? "true" : "false")}");
            sb.AppendLine("}");

            File.WriteAllText(k_AndroidSafetyPath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[SWEF] PrivacyManifest: Android data safety written to {k_AndroidSafetyPath}");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static void AppendIOSDataType(StringBuilder sb, string dataType, bool linked, bool tracking, string[] purposes)
        {
            sb.AppendLine("\t\t<dict>");
            sb.AppendLine($"\t\t\t<key>NSPrivacyCollectedDataType</key>");
            sb.AppendLine($"\t\t\t<string>{dataType}</string>");
            sb.AppendLine($"\t\t\t<key>NSPrivacyCollectedDataTypeLinked</key>");
            sb.AppendLine($"\t\t\t<{(linked ? "true" : "false")}/>");
            sb.AppendLine($"\t\t\t<key>NSPrivacyCollectedDataTypeTracking</key>");
            sb.AppendLine($"\t\t\t<{(tracking ? "true" : "false")}/>");
            sb.AppendLine($"\t\t\t<key>NSPrivacyCollectedDataTypePurposes</key>");
            sb.AppendLine("\t\t\t<array>");
            foreach (var p in purposes)
                sb.AppendLine($"\t\t\t\t<string>{p}</string>");
            sb.AppendLine("\t\t\t</array>");
            sb.AppendLine("\t\t</dict>");
        }

        private static void AppendIOSAPIType(StringBuilder sb, string apiCategory, string[] reasons)
        {
            sb.AppendLine("\t\t<dict>");
            sb.AppendLine($"\t\t\t<key>NSPrivacyAccessedAPIType</key>");
            sb.AppendLine($"\t\t\t<string>{apiCategory}</string>");
            sb.AppendLine($"\t\t\t<key>NSPrivacyAccessedAPITypeReasons</key>");
            sb.AppendLine("\t\t\t<array>");
            foreach (var r in reasons)
                sb.AppendLine($"\t\t\t\t<string>{r}</string>");
            sb.AppendLine("\t\t\t</array>");
            sb.AppendLine("\t\t</dict>");
        }

        private static string EscapeJson(string s)
            => s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
    }
}
#endif
