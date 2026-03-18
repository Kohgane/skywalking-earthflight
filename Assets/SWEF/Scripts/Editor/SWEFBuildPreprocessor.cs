#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SWEF.Editor
{
    /// <summary>
    /// Pre-build validation step that runs before every Unity build.
    /// Checks that both Boot and World scenes are present in Build Settings,
    /// that <c>SaveManager</c> exists in the Boot scene, and warns if the
    /// debug console is active in a Release build.
    /// All output is prefixed with <c>[SWEF Build]</c>.
    /// </summary>
    public class SWEFBuildPreprocessor : IPreprocessBuildWithReport
    {
        private const string k_BootSceneName  = "Boot";
        private const string k_WorldSceneName = "World";

        /// <inheritdoc/>
        public int callbackOrder => 0;

        /// <inheritdoc/>
        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("[SWEF Build] ─── Pre-build validation started ───");

            bool pass = true;
            pass &= CheckScenesInBuildSettings();
            CheckSaveManagerInBootScene();
            CheckDebugConsoleInRelease(report);

            if (pass)
                Debug.Log("[SWEF Build] ─── Pre-build validation passed. ───");
            else
                Debug.LogError("[SWEF Build] ─── Pre-build validation FAILED — see warnings above. ───");
        }

        // ── Checks ───────────────────────────────────────────────────────────

        private static bool CheckScenesInBuildSettings()
        {
            bool bootFound  = false;
            bool worldFound = false;

            foreach (var scene in EditorBuildSettings.scenes)
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(scene.path);
                if (name == k_BootSceneName)  bootFound  = true;
                if (name == k_WorldSceneName) worldFound = true;
            }

            if (!bootFound)
                Debug.LogError($"[SWEF Build] Scene '{k_BootSceneName}' is missing from Build Settings.");
            else
                Debug.Log($"[SWEF Build] ✓ '{k_BootSceneName}' scene found in Build Settings.");

            if (!worldFound)
                Debug.LogError($"[SWEF Build] Scene '{k_WorldSceneName}' is missing from Build Settings.");
            else
                Debug.Log($"[SWEF Build] ✓ '{k_WorldSceneName}' scene found in Build Settings.");

            return bootFound && worldFound;
        }

        private static void CheckSaveManagerInBootScene()
        {
            // Find Boot scene path
            string bootPath = null;
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (System.IO.Path.GetFileNameWithoutExtension(scene.path) == k_BootSceneName)
                {
                    bootPath = scene.path;
                    break;
                }
            }

            if (bootPath == null)
                return; // already logged above

            // Open Boot scene additively in editor
            var bootScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                bootPath, UnityEditor.SceneManagement.OpenSceneMode.Additive);

            bool saveManagerFound = false;
            foreach (var root in bootScene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<SWEF.Core.SaveManager>() != null)
                {
                    saveManagerFound = true;
                    break;
                }
            }

            if (saveManagerFound)
                Debug.Log("[SWEF Build] ✓ SaveManager found in Boot scene.");
            else
                Debug.LogWarning("[SWEF Build] SaveManager not found in Boot scene — save/load may not function correctly.");

            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(bootScene, true);
        }

        private static void CheckDebugConsoleInRelease(BuildReport report)
        {
            bool isRelease = !report.summary.options.HasFlag(BuildOptions.Development);
            if (!isRelease)
                return;

            // Check whether a DebugConsole is active in the loaded scenes
            var debugConsoles = Object.FindObjectsByType<SWEF.Core.DebugConsole>(FindObjectsSortMode.None);
            if (debugConsoles.Length > 0)
            {
                Debug.LogWarning("[SWEF Build] DebugConsole component is active in a Release build — " +
                                 "consider disabling it before shipping.");
            }
        }
    }
}
#endif
