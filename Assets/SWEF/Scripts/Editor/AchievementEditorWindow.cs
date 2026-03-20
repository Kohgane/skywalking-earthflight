#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using SWEF.Achievement;

namespace SWEF.Editor
{
    /// <summary>
    /// Unity Editor window for managing SWEF Achievement definitions.
    /// Open via <c>SWEF > Achievement Editor</c>.
    /// </summary>
    public class AchievementEditorWindow : EditorWindow
    {
        private Vector2 _scroll;
        private List<AchievementDefinition> _defs = new List<AchievementDefinition>();
        private string _outputFolder = "Assets/SWEF/Resources/Achievements";

        [MenuItem("SWEF/Achievement Editor")]
        public static void Open()
        {
            GetWindow<AchievementEditorWindow>("Achievement Editor").Show();
        }

        private void OnEnable()  => RefreshList();
        private void OnFocus()   => RefreshList();

        // ── GUI ───────────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            EditorGUILayout.LabelField("SWEF — Achievement Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _outputFolder = EditorGUILayout.TextField("ScriptableObject Folder", _outputFolder);
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh List"))                          RefreshList();
            if (GUILayout.Button("Bulk Create Defaults"))                  BulkCreateDefaults();
            if (GUILayout.Button("Validate All"))                          ValidateAll();
            if (GUILayout.Button("Reset Player Progress (Editor only)"))   ResetProgress();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            DrawTable();
        }

        // ── Table ─────────────────────────────────────────────────────────────────
        private void DrawTable()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // Header row.
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("ID",          GUILayout.Width(180));
            GUILayout.Label("Category",    GUILayout.Width(90));
            GUILayout.Label("Tier",        GUILayout.Width(70));
            GUILayout.Label("Target",      GUILayout.Width(60));
            GUILayout.Label("XP",          GUILayout.Width(50));
            GUILayout.Label("Has Icon",    GUILayout.Width(60));
            GUILayout.Label("Title Key",   GUILayout.Width(180));
            GUILayout.Label("Desc Key",    GUILayout.Width(180));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            foreach (var def in _defs)
            {
                if (def == null) continue;

                Color bg = GUI.backgroundColor;
                bool hasIssue = string.IsNullOrEmpty(def.id)
                             || def.icon == null
                             || string.IsNullOrEmpty(def.titleKey)
                             || string.IsNullOrEmpty(def.descriptionKey);
                if (hasIssue) GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.Label(def.id,                          GUILayout.Width(180));
                GUILayout.Label(def.category.ToString(),         GUILayout.Width(90));
                DrawTierLabel(def.tier);
                GUILayout.Label(def.targetValue.ToString("F0"), GUILayout.Width(60));
                GUILayout.Label(def.xpReward.ToString(),         GUILayout.Width(50));
                GUILayout.Label(def.icon != null ? "✓" : "✗",   GUILayout.Width(60));
                GUILayout.Label(def.titleKey,                    GUILayout.Width(180));
                GUILayout.Label(def.descriptionKey,              GUILayout.Width(180));

                if (GUILayout.Button("Select", GUILayout.Width(54)))
                    Selection.activeObject = def;

                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = bg;
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawTierLabel(AchievementTier tier)
        {
            Color c = tier switch
            {
                AchievementTier.Silver   => new Color(0.75f, 0.75f, 0.75f),
                AchievementTier.Gold     => new Color(1f, 0.85f, 0f),
                AchievementTier.Platinum => new Color(0.9f, 0.9f, 0.88f),
                AchievementTier.Diamond  => new Color(0.73f, 0.95f, 1f),
                _                        => new Color(0.8f, 0.5f, 0.2f) // Bronze
            };

            Color prev = GUI.contentColor;
            GUI.contentColor = c;
            GUILayout.Label(tier.ToString(), GUILayout.Width(70));
            GUI.contentColor = prev;
        }

        // ── Actions ───────────────────────────────────────────────────────────────
        private void RefreshList()
        {
            _defs.Clear();
            var guids = AssetDatabase.FindAssets("t:AchievementDefinition");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var def     = AssetDatabase.LoadAssetAtPath<AchievementDefinition>(path);
                if (def != null) _defs.Add(def);
            }
            Repaint();
        }

        private void BulkCreateDefaults()
        {
            if (!AssetDatabase.IsValidFolder(_outputFolder))
            {
                // Create intermediate folders.
                string[] parts = _outputFolder.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }

            var defaults = AchievementData.GetDefaults();
            int created  = 0;

            foreach (var info in defaults)
            {
                string assetPath = $"{_outputFolder}/{info.id}.asset";
                if (File.Exists(assetPath))
                {
                    Debug.Log($"[SWEF] Skipped existing: {assetPath}");
                    continue;
                }

                var def = CreateInstance<AchievementDefinition>();
                def.id             = info.id;
                def.titleKey       = info.titleKey;
                def.descriptionKey = info.descriptionKey;
                def.category       = info.category;
                def.tier           = info.tier;
                def.targetValue    = info.targetValue;
                def.xpReward       = info.xpReward;
                def.isHidden       = info.isHidden;

                AssetDatabase.CreateAsset(def, assetPath);
                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshList();
            Debug.Log($"[SWEF] AchievementEditorWindow: Created {created} achievement ScriptableObjects.");
        }

        private void ValidateAll()
        {
            var idSet      = new HashSet<string>();
            int errorCount = 0;

            foreach (var def in _defs)
            {
                if (def == null) continue;

                if (string.IsNullOrEmpty(def.id))
                { Debug.LogError($"[SWEF] Achievement '{def.name}' is missing an id."); errorCount++; }
                else if (!idSet.Add(def.id))
                { Debug.LogError($"[SWEF] Duplicate achievement id: '{def.id}'."); errorCount++; }

                if (def.icon == null)
                { Debug.LogWarning($"[SWEF] Achievement '{def.id}' is missing an icon."); errorCount++; }

                if (string.IsNullOrEmpty(def.titleKey))
                { Debug.LogError($"[SWEF] Achievement '{def.id}' is missing a titleKey."); errorCount++; }

                if (string.IsNullOrEmpty(def.descriptionKey))
                { Debug.LogError($"[SWEF] Achievement '{def.id}' is missing a descriptionKey."); errorCount++; }
            }

            if (errorCount == 0)
                Debug.Log("[SWEF] Achievement validation passed — no issues found.");
            else
                Debug.LogWarning($"[SWEF] Achievement validation found {errorCount} issue(s).");
        }

        private static void ResetProgress()
        {
            if (!EditorUtility.DisplayDialog("Reset Achievement Progress",
                "This will delete all achievement progress in the editor play session. Continue?",
                "Reset", "Cancel")) return;

            AchievementManager.Instance?.ResetAll();
            Debug.Log("[SWEF] AchievementEditorWindow: Progress reset requested.");
        }
    }
}
#endif
