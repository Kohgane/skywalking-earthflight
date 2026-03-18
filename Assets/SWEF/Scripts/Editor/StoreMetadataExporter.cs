#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using SWEF.Build;

namespace SWEF.Editor
{
    /// <summary>
    /// Editor utility that exports a <c>StoreMetadata/</c> folder containing
    /// iOS and Android store-listing JSON files, a release notes template,
    /// and a screenshots placeholder README.
    ///
    /// Access via <b>SWEF → Build → Export Store Metadata</b>.
    /// </summary>
    public static class StoreMetadataExporter
    {
        private const string k_ConfigPath    = "Assets/SWEF/Config/SWEFBuildConfig.asset";
        private const string k_OutputDir     = "StoreMetadata";
        private const string k_ScreenshotDir = "StoreMetadata/screenshots";

        // ── Menu item ────────────────────────────────────────────────────────

        /// <summary>Generates the <c>StoreMetadata/</c> folder with all metadata files.</summary>
        [MenuItem("SWEF/Build/Export Store Metadata")]
        public static void ExportMetadata()
        {
            var config = AssetDatabase.LoadAssetAtPath<BuildConfig>(k_ConfigPath);
            if (config == null)
            {
                Debug.LogError($"[SWEF] StoreMetadataExporter: BuildConfig not found at {k_ConfigPath}.");
                return;
            }

            Directory.CreateDirectory(k_OutputDir);
            Directory.CreateDirectory(k_ScreenshotDir);

            WriteIOSMetadata(config);
            WriteAndroidMetadata(config);
            WriteReleaseNotes(config);
            WriteScreenshotsReadme();

            Debug.Log($"[SWEF] StoreMetadataExporter: metadata exported to {Path.GetFullPath(k_OutputDir)}");
            EditorUtility.RevealInFinder(k_OutputDir);
        }

        // ── iOS metadata ─────────────────────────────────────────────────────

        private static void WriteIOSMetadata(BuildConfig config)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"title\": \"Skywalking: Earth Flight\",");
            sb.AppendLine($"  \"subtitle\": \"Launch from your location. Climb to the edge of space.\",");
            sb.AppendLine($"  \"description\": \"Experience Earth from above like never before. Skywalking: Earth Flight places you at your exact GPS location and lets you soar upward through photorealistic 3D terrain, into the stratosphere, and beyond. Powered by Google 3D Tiles and Cesium for Unity.\\n\\n• Launch from your real-world location using GPS\\n• Fly through photorealistic 3D terrain\\n• Climb to the Karman Line and beyond\\n• Screenshot your favourite views\\n• Track your personal flight records\",");
            sb.AppendLine($"  \"keywords\": \"flight,earth,space,3D,explore,GPS,Karman,satellite,altitude,simulation\",");
            sb.AppendLine($"  \"supportUrl\": \"https://kohgane.com/swef/support\",");
            sb.AppendLine($"  \"privacyPolicyUrl\": \"{EscapeJson(config.privacyPolicyUrl)}\",");
            sb.AppendLine($"  \"marketingUrl\": \"{EscapeJson(config.iosAppStoreUrl)}\",");
            sb.AppendLine($"  \"category\": \"GAMES\",");
            sb.AppendLine($"  \"subcategory\": \"GAMES_SIMULATION\",");
            sb.AppendLine($"  \"contentRating\": \"4+\",");
            sb.AppendLine($"  \"price\": \"0\",");
            sb.AppendLine($"  \"version\": \"{EscapeJson(config.version)}\",");
            sb.AppendLine($"  \"bundleId\": \"{EscapeJson(config.bundleId)}\"");
            sb.AppendLine("}");

            WriteFile(Path.Combine(k_OutputDir, "ios_metadata.json"), sb.ToString());
        }

        // ── Android metadata ─────────────────────────────────────────────────

        private static void WriteAndroidMetadata(BuildConfig config)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"title\": \"Skywalking: Earth Flight\",");
            sb.AppendLine($"  \"shortDescription\": \"Launch from your GPS location and fly to the edge of space.\",");
            sb.AppendLine($"  \"fullDescription\": \"Experience Earth from above like never before. Skywalking: Earth Flight places you at your exact GPS location and lets you soar upward through photorealistic 3D terrain, into the stratosphere, and beyond. Powered by Google 3D Tiles and Cesium for Unity.\\n\\nFeatures:\\n• Launch from your real-world location using GPS\\n• Fly through photorealistic 3D terrain\\n• Climb to the Karman Line (100 km) and beyond\\n• Screenshot your favourite views and share them\\n• Track personal altitude, speed, and flight records\",");
            sb.AppendLine($"  \"category\": \"GAME_SIMULATION\",");
            sb.AppendLine($"  \"contentRating\": \"Everyone\",");
            sb.AppendLine($"  \"privacyPolicyUrl\": \"{EscapeJson(config.privacyPolicyUrl)}\",");
            sb.AppendLine($"  \"packageName\": \"{EscapeJson(config.bundleId)}\",");
            sb.AppendLine($"  \"version\": \"{EscapeJson(config.version)}\"");
            sb.AppendLine("}");

            WriteFile(Path.Combine(k_OutputDir, "android_metadata.json"), sb.ToString());
        }

        // ── Release notes ─────────────────────────────────────────────────────

        private static void WriteReleaseNotes(BuildConfig config)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Version {config.version} — {DateTime.Today:MMMM d, yyyy}");
            sb.AppendLine();
            sb.AppendLine("What's New");
            sb.AppendLine("----------");
            sb.AppendLine("• [Describe main new feature or improvement]");
            sb.AppendLine("• [Describe second new feature or fix]");
            sb.AppendLine("• Performance improvements and bug fixes");
            sb.AppendLine();
            sb.AppendLine("Bug Fixes");
            sb.AppendLine("---------");
            sb.AppendLine("• [Describe bug fix 1]");
            sb.AppendLine("• [Describe bug fix 2]");

            WriteFile(Path.Combine(k_OutputDir, "release_notes.txt"), sb.ToString());
        }

        // ── Screenshots README ────────────────────────────────────────────────

        private static void WriteScreenshotsReadme()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Screenshots Folder");
            sb.AppendLine("==================");
            sb.AppendLine();
            sb.AppendLine("Place your store screenshots here before uploading to App Store Connect / Play Console.");
            sb.AppendLine();
            sb.AppendLine("iOS Required Sizes");
            sb.AppendLine("------------------");
            sb.AppendLine("  6.7\"  iPhone 15 Pro Max  : 1290 × 2796 px  (required)");
            sb.AppendLine("  6.5\"  iPhone 14 Plus     : 1284 × 2778 px  (required)");
            sb.AppendLine("  5.5\"  iPhone 8 Plus      : 1242 × 2208 px  (required)");
            sb.AppendLine("  12.9\" iPad Pro           : 2048 × 2732 px  (if targeting iPad)");
            sb.AppendLine();
            sb.AppendLine("Android Required Sizes");
            sb.AppendLine("----------------------");
            sb.AppendLine("  Phone screenshots       : 1080 × 1920 px minimum (JPEG or PNG)");
            sb.AppendLine("  Feature graphic         : 1024 × 500 px  (required)");
            sb.AppendLine("  Hi-res icon             : 512 × 512 px   (required)");
            sb.AppendLine();
            sb.AppendLine("Recommended Count");
            sb.AppendLine("-----------------");
            sb.AppendLine("  iOS     : 3–10 screenshots per device class");
            sb.AppendLine("  Android : 2–8 screenshots");
            sb.AppendLine();
            sb.AppendLine("Naming Convention");
            sb.AppendLine("-----------------");
            sb.AppendLine("  ios_6.7_01_launch.png");
            sb.AppendLine("  ios_6.7_02_flight.png");
            sb.AppendLine("  android_phone_01_launch.png");

            WriteFile(Path.Combine(k_ScreenshotDir, "README.txt"), sb.ToString());
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static void WriteFile(string path, string content)
        {
            File.WriteAllText(path, content, Encoding.UTF8);
            Debug.Log($"[SWEF] StoreMetadataExporter: wrote {path}");
        }

        private static string EscapeJson(string s)
            => s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
    }
}
#endif
