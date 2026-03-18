#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SWEF.Editor
{
    /// <summary>
    /// Custom Unity Editor window providing a SWEF project dashboard.
    /// Open via menu <b>SWEF → Dashboard</b>.
    /// Displays script counts per folder, scene list, current build target, and quick-action buttons.
    /// </summary>
    public class SWEFEditorWindow : EditorWindow
    {
        private const string k_ScriptsRoot = "Assets/SWEF/Scripts";
        private const string k_BootScene   = "Assets/SWEF/Scenes/Boot.unity";
        private const string k_WorldScene  = "Assets/SWEF/Scenes/World.unity";

        private Vector2 _scroll;

        // ── Menu item ────────────────────────────────────────────────────────

        /// <summary>Opens the SWEF Dashboard editor window.</summary>
        [MenuItem("SWEF/Dashboard")]
        public static void ShowWindow()
        {
            var win = GetWindow<SWEFEditorWindow>("SWEF Dashboard");
            win.minSize = new Vector2(360f, 480f);
        }

        // ── GUI ──────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("SWEF — Project Dashboard", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Build Target: {EditorUserBuildSettings.activeBuildTarget}", EditorStyles.miniLabel);
            EditorGUILayout.Space(4f);

            DrawScriptCounts();
            EditorGUILayout.Space(8f);
            DrawSceneList();
            EditorGUILayout.Space(8f);
            DrawQuickActions();
        }

        // ── Sections ─────────────────────────────────────────────────────────

        private void DrawScriptCounts()
        {
            EditorGUILayout.LabelField("Scripts per Folder", EditorStyles.boldLabel);

            if (!Directory.Exists(k_ScriptsRoot))
            {
                EditorGUILayout.HelpBox($"Scripts root not found: {k_ScriptsRoot}", MessageType.Warning);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(180f));

            var dirs = Directory.GetDirectories(k_ScriptsRoot, "*", SearchOption.TopDirectoryOnly)
                                .OrderBy(d => d);

            foreach (string dir in dirs)
            {
                int count = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories).Length;
                string folderName = Path.GetFileName(dir);
                EditorGUILayout.LabelField($"  {folderName}", $"{count} script{(count == 1 ? "" : "s")}",
                    EditorStyles.miniLabel);
            }

            // Also count files directly in root
            int rootCount = Directory.GetFiles(k_ScriptsRoot, "*.cs", SearchOption.TopDirectoryOnly).Length;
            if (rootCount > 0)
                EditorGUILayout.LabelField("  (root)", $"{rootCount} script{(rootCount == 1 ? "" : "s")}",
                    EditorStyles.miniLabel);

            EditorGUILayout.EndScrollView();
        }

        private void DrawSceneList()
        {
            EditorGUILayout.LabelField("Build Scenes", EditorStyles.boldLabel);

            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length == 0)
            {
                EditorGUILayout.HelpBox("No scenes in Build Settings.", MessageType.Info);
                return;
            }

            foreach (var scene in scenes)
            {
                string name = Path.GetFileNameWithoutExtension(scene.path);
                string status = scene.enabled ? "✓" : "✗";
                EditorGUILayout.LabelField($"  {status}  {name}", scene.path, EditorStyles.miniLabel);
            }
        }

        private void DrawQuickActions()
        {
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Boot Scene"))
                OpenScene(k_BootScene);
            if (GUILayout.Button("Open World Scene"))
                OpenScene(k_WorldScene);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear PlayerPrefs"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.Log("[SWEF] Dashboard: all PlayerPrefs cleared.");
            }
            if (GUILayout.Button("Delete Save File"))
                DeleteSaveFile();
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Refresh"))
                Repaint();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static void OpenScene(string scenePath)
        {
            if (!File.Exists(scenePath))
            {
                Debug.LogWarning($"[SWEF] Dashboard: scene not found at {scenePath}");
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(scenePath);
        }

        private static void DeleteSaveFile()
        {
            string path = System.IO.Path.Combine(Application.persistentDataPath, "swef_save.json");
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[SWEF] Dashboard: save file deleted ({path}).");
            }
            else
            {
                Debug.Log($"[SWEF] Dashboard: no save file found at {path}.");
            }
        }
    }
}
#endif
