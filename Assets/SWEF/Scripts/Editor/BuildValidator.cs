#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using SWEF.Build;

namespace SWEF.Editor
{
    /// <summary>
    /// Editor window that validates the SWEF project is ready for iOS and Android
    /// store submission. Displays a ✅ / ❌ checklist and provides "Fix All" and
    /// "Export Report" actions.
    ///
    /// Open via <b>SWEF → Build → Validate Submission Readiness</b>.
    /// Called from CI via the static <see cref="ValidateFromCLI"/> entry point.
    /// </summary>
    public class BuildValidator : EditorWindow
    {
        // ── Internal result type ─────────────────────────────────────────────
        private struct ValidationResult
        {
            public bool   Passed;
            public string Label;
            public string Detail;
            public bool   AutoFixable;
            public Action FixAction;

            public ValidationResult(bool passed, string label, string detail = "",
                bool autoFixable = false, Action fixAction = null)
            {
                Passed       = passed;
                Label        = label;
                Detail       = detail;
                AutoFixable  = autoFixable && !passed;
                FixAction    = fixAction;
            }
        }

        // ── State ────────────────────────────────────────────────────────────
        private List<ValidationResult> _results = new List<ValidationResult>();
        private Vector2 _scroll;
        private BuildConfig _config;

        // ── Menu item ────────────────────────────────────────────────────────

        /// <summary>Opens the Build Validator editor window.</summary>
        [MenuItem("SWEF/Build/Validate Submission Readiness")]
        public static void ShowWindow()
        {
            var win = GetWindow<BuildValidator>("SWEF — Build Validator");
            win.minSize = new Vector2(480f, 600f);
            win.RunValidation();
        }

        // ── CI entry point ───────────────────────────────────────────────────

        /// <summary>
        /// Headless validation entry point for game-ci/unity-builder.
        /// Exits with code 1 if any check fails; code 0 on full pass.
        /// </summary>
        public static void ValidateFromCLI()
        {
            var validator = CreateInstance<BuildValidator>();
            validator.RunValidation();

            int failures = 0;
            foreach (var r in validator._results)
            {
                string icon = r.Passed ? "PASS" : "FAIL";
                Debug.Log($"[SWEF] BuildValidator [{icon}] {r.Label}" +
                          (string.IsNullOrEmpty(r.Detail) ? "" : $" — {r.Detail}"));
                if (!r.Passed) failures++;
            }

            Debug.Log($"[SWEF] BuildValidator: {validator._results.Count - failures}/{validator._results.Count} checks passed.");

            if (failures > 0)
            {
                Debug.LogError($"[SWEF] BuildValidator: {failures} check(s) failed. Aborting build.");
                EditorApplication.Exit(1);
            }
        }

        // ── GUI ──────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("SWEF — Submission Readiness Validator", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Re-validate"))
                RunValidation();

            using (new EditorGUI.DisabledScope(!HasAutoFixes()))
            {
                if (GUILayout.Button("Fix All"))
                    ApplyAllFixes();
            }

            if (GUILayout.Button("Export Report"))
                ExportReport();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8f);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (var r in _results)
                DrawResult(r);

            EditorGUILayout.EndScrollView();
        }

        private void DrawResult(ValidationResult r)
        {
            string icon = r.Passed ? "✅" : "❌";
            Color  col  = r.Passed ? Color.green : Color.red;

            EditorGUILayout.BeginHorizontal();

            var prevColor = GUI.contentColor;
            GUI.contentColor = col;
            GUILayout.Label($"{icon} {r.Label}", GUILayout.Width(320f));
            GUI.contentColor = prevColor;

            if (!string.IsNullOrEmpty(r.Detail))
                EditorGUILayout.LabelField(r.Detail, EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }

        // ── Validation logic ─────────────────────────────────────────────────

        private void RunValidation()
        {
            _results.Clear();

            const string configPath = "Assets/SWEF/Config/SWEFBuildConfig.asset";
            _config = AssetDatabase.LoadAssetAtPath<BuildConfig>(configPath);

            CheckBundleId();
            CheckVersion();
            CheckBuildNumber();
            CheckScenes();
            CheckIOSTeamId();
            CheckAndroidKeystore();
            CheckPrivacyManifests();
            CheckIcons();
            CheckSplashScreen();
            CheckScriptingBackend();
            CheckDevelopmentBuild();
            CheckStripEngineCode();
            CheckMinOsVersions();

            Repaint();
        }

        private void CheckBundleId()
        {
            bool ok = _config != null &&
                      PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS)     == _config.bundleId &&
                      PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android) == _config.bundleId;

            _results.Add(new ValidationResult(
                ok, "Bundle identifier matches BuildConfig",
                _config != null ? _config.bundleId : "BuildConfig missing",
                autoFixable: _config != null,
                fixAction: () =>
                {
                    PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS,     _config.bundleId);
                    PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, _config.bundleId);
                }));
        }

        private void CheckVersion()
        {
            bool ok = _config != null && !string.IsNullOrEmpty(_config.version) &&
                      PlayerSettings.bundleVersion == _config.version;

            _results.Add(new ValidationResult(
                ok, "Version string is set",
                _config?.version ?? "not set",
                autoFixable: _config != null,
                fixAction: () => PlayerSettings.bundleVersion = _config.version));
        }

        private void CheckBuildNumber()
        {
            bool ok = _config != null && _config.buildNumber > 0;
            _results.Add(new ValidationResult(ok, "Build number is greater than 0",
                _config != null ? _config.buildNumber.ToString() : "BuildConfig missing"));
        }

        private void CheckScenes()
        {
            var scenes = EditorBuildSettings.scenes;
            bool hasTwo = scenes.Length >= 2;
            bool bootFirst  = hasTwo && Path.GetFileNameWithoutExtension(scenes[0].path)
                .Equals("Boot", StringComparison.OrdinalIgnoreCase);
            bool worldSecond = hasTwo && Path.GetFileNameWithoutExtension(scenes[1].path)
                .Equals("World", StringComparison.OrdinalIgnoreCase);

            _results.Add(new ValidationResult(bootFirst,
                "Boot scene at Build Settings index 0",
                hasTwo ? scenes[0].path : "fewer than 2 scenes"));
            _results.Add(new ValidationResult(worldSecond,
                "World scene at Build Settings index 1",
                hasTwo && scenes.Length > 1 ? scenes[1].path : "missing"));
        }

        private void CheckIOSTeamId()
        {
            bool ok = _config == null || !string.IsNullOrWhiteSpace(_config.iosTeamId);
            _results.Add(new ValidationResult(ok, "iOS Team ID configured",
                ok ? (_config?.iosTeamId ?? "N/A") : "empty — required for iOS builds"));
        }

        private void CheckAndroidKeystore()
        {
            bool pathOk = _config == null || !string.IsNullOrWhiteSpace(_config.androidKeystorePath);
            bool passOk = _config == null || !string.IsNullOrWhiteSpace(_config.androidKeystorePass);

            _results.Add(new ValidationResult(pathOk, "Android keystore path set",
                pathOk ? (_config?.androidKeystorePath ?? "N/A") : "empty"));
            _results.Add(new ValidationResult(passOk, "Android keystore password set",
                passOk ? "****" : "empty — required for release builds"));
        }

        private void CheckPrivacyManifests()
        {
            bool iosOk     = File.Exists("Assets/Plugins/iOS/PrivacyInfo.xcprivacy");
            bool androidOk = File.Exists("Assets/Plugins/Android/data_safety.json");

            _results.Add(new ValidationResult(iosOk, "iOS PrivacyInfo.xcprivacy exists",
                iosOk ? "" : "Run SWEF → Build → Generate Privacy Manifests",
                autoFixable: _config != null,
                fixAction: () => PrivacyManifest.GenerateIOSPrivacyManifest(_config)));

            _results.Add(new ValidationResult(androidOk, "Android data_safety.json exists",
                androidOk ? "" : "Run SWEF → Build → Generate Privacy Manifests",
                autoFixable: _config != null,
                fixAction: () => PrivacyManifest.GenerateAndroidDataSafety(_config)));
        }

        private void CheckIcons()
        {
            // Verify that at least one platform icon is set (non-default)
            var icons = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Unknown);
            bool ok   = icons != null && icons.Length > 0 && icons[0] != null;
            _results.Add(new ValidationResult(ok, "App icons set (128×128 minimum)",
                ok ? $"{icons.Length} icon(s) found" : "no icons configured"));
        }

        private void CheckSplashScreen()
        {
            bool ok = PlayerSettings.SplashScreen.show;
            _results.Add(new ValidationResult(ok, "Splash screen configured",
                ok ? "enabled" : "disabled — enable via Project Settings → Player → Splash Image",
                autoFixable: true,
                fixAction: () =>
                {
                    PlayerSettings.SplashScreen.show = true;
                    PlayerSettings.SplashScreen.showUnityLogo = false;
                }));
        }

        private void CheckScriptingBackend()
        {
            bool iosOk     = PlayerSettings.GetScriptingBackend(BuildTargetGroup.iOS)     == ScriptingImplementation.IL2CPP;
            bool androidOk = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) == ScriptingImplementation.IL2CPP;
            bool ok        = iosOk && androidOk;

            _results.Add(new ValidationResult(ok, "IL2CPP scripting backend selected",
                ok ? "IL2CPP" : $"iOS={PlayerSettings.GetScriptingBackend(BuildTargetGroup.iOS)} Android={PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android)}",
                autoFixable: true,
                fixAction: () =>
                {
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS,     ScriptingImplementation.IL2CPP);
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                }));
        }

        private void CheckDevelopmentBuild()
        {
            bool ok = !EditorUserBuildSettings.development;
            _results.Add(new ValidationResult(ok, "Development Build flag is OFF",
                ok ? "off (release mode)" : "ON — must be disabled for store submission",
                autoFixable: true,
                fixAction: () => EditorUserBuildSettings.development = false));
        }

        private void CheckStripEngineCode()
        {
            bool ok = PlayerSettings.stripEngineCode;
            _results.Add(new ValidationResult(ok, "Strip Engine Code enabled",
                ok ? "enabled" : "disabled",
                autoFixable: true,
                fixAction: () => PlayerSettings.stripEngineCode = true));
        }

        private void CheckMinOsVersions()
        {
            string iosMin  = PlayerSettings.iOS.targetOSVersionString;
            bool   iosOk   = !string.IsNullOrEmpty(iosMin);

            _results.Add(new ValidationResult(iosOk, "iOS minimum OS version set",
                iosOk ? iosMin : "empty — set via Project Settings → Player → iOS → Other Settings",
                autoFixable: _config != null,
                fixAction: () => PlayerSettings.iOS.targetOSVersionString = _config?.iosMinVersion ?? "15.0"));

            bool androidOk = (int)PlayerSettings.Android.minSdkVersion >= 26;
            _results.Add(new ValidationResult(androidOk, "Android minimum SDK ≥ 26 (Oreo)",
                $"minSdk={PlayerSettings.Android.minSdkVersion}",
                autoFixable: true,
                fixAction: () => PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)26));
        }

        // ── Fix All ──────────────────────────────────────────────────────────

        private bool HasAutoFixes() => _results.Exists(r => r.AutoFixable);

        private void ApplyAllFixes()
        {
            foreach (var r in _results)
            {
                if (r.AutoFixable && r.FixAction != null)
                {
                    try { r.FixAction(); }
                    catch (Exception e)
                    { Debug.LogError($"[SWEF] BuildValidator: fix failed for '{r.Label}': {e.Message}"); }
                }
            }

            AssetDatabase.SaveAssets();
            RunValidation();
            Debug.Log("[SWEF] BuildValidator: auto-fixes applied.");
        }

        // ── Export Report ────────────────────────────────────────────────────

        private void ExportReport()
        {
            string path = EditorUtility.SaveFilePanel("Save Validation Report",
                Application.dataPath, "BuildValidation_Report", "txt");

            if (string.IsNullOrEmpty(path)) return;

            var sb = new StringBuilder();
            sb.AppendLine("SWEF — Build Validation Report");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Unity Version: {Application.unityVersion}");
            sb.AppendLine(new string('-', 60));

            foreach (var r in _results)
            {
                string icon = r.Passed ? "[PASS]" : "[FAIL]";
                sb.AppendLine($"{icon} {r.Label}");
                if (!string.IsNullOrEmpty(r.Detail))
                    sb.AppendLine($"       {r.Detail}");
            }

            int passed   = _results.FindAll(r =>  r.Passed).Count;
            int failures = _results.FindAll(r => !r.Passed).Count;
            sb.AppendLine(new string('-', 60));
            sb.AppendLine($"Result: {passed}/{_results.Count} passed, {failures} failed.");

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[SWEF] BuildValidator: report exported to {path}");
        }
    }
}
#endif
