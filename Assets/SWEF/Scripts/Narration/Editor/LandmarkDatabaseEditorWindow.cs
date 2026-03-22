#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SWEF.Editor
{
    /// <summary>
    /// Unity Editor window for inspecting, validating, and testing the
    /// <see cref="SWEF.Narration.LandmarkDatabase"/>.
    /// Open via <c>SWEF / Narration / Landmark Database Editor</c>.
    /// </summary>
    public class LandmarkDatabaseEditorWindow : EditorWindow
    {
        // ── State ─────────────────────────────────────────────────────────────────
        private SWEF.Narration.LandmarkDatabase _database;
        private Vector2 _scrollPos;
        private string  _searchQuery = string.Empty;
        private SWEF.Narration.LandmarkCategory _filterCategory;
        private bool    _filterByCategory;
        private bool    _showValidation;

        // Validation results
        private readonly List<string> _warnings = new List<string>();
        private readonly List<string> _errors   = new List<string>();

        // ── Menu item ─────────────────────────────────────────────────────────────
        [MenuItem("SWEF/Narration/Landmark Database Editor")]
        public static void Open()
        {
            var win = GetWindow<LandmarkDatabaseEditorWindow>("Landmark DB Editor");
            win.minSize = new Vector2(520f, 400f);
        }

        // ── GUI ───────────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            GUILayout.Label("SWEF — Landmark Database Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Database field.
            _database = (SWEF.Narration.LandmarkDatabase)EditorGUILayout.ObjectField(
                "Database Asset", _database, typeof(SWEF.Narration.LandmarkDatabase), false);

            if (_database == null)
            {
                EditorGUILayout.HelpBox("Assign a LandmarkDatabase ScriptableObject asset.", MessageType.Info);
                if (GUILayout.Button("Find in Project"))
                    TryAutoFind();
                return;
            }

            EditorGUILayout.Space();

            // Statistics bar.
            EditorGUILayout.LabelField($"Total Landmarks: {_database.TotalLandmarks}", EditorStyles.miniLabel);
            var catDict = _database.LandmarksByCategory;
            string catSummary = string.Join("  ", catDict.Select(kv => $"{kv.Key}:{kv.Value}"));
            EditorGUILayout.LabelField(catSummary, EditorStyles.miniLabel);

            EditorGUILayout.Space();

            // Filters.
            using (new EditorGUILayout.HorizontalScope())
            {
                _searchQuery     = EditorGUILayout.TextField("Search", _searchQuery);
                _filterByCategory = EditorGUILayout.ToggleLeft("Filter Category", _filterByCategory, GUILayout.Width(120));
                if (_filterByCategory)
                    _filterCategory = (SWEF.Narration.LandmarkCategory)EditorGUILayout.EnumPopup(_filterCategory, GUILayout.Width(130));
            }

            EditorGUILayout.Space();

            // Action buttons.
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate"))
                    RunValidation();
                if (GUILayout.Button("Export JSON"))
                    ExportJson();
                if (GUILayout.Button("Refresh"))
                    Repaint();
            }

            EditorGUILayout.Space();

            // Validation results.
            if (_showValidation)
                DrawValidationResults();

            // Landmark list.
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            DrawLandmarkList();
            EditorGUILayout.EndScrollView();
        }

        // ── Landmark list ─────────────────────────────────────────────────────────

        private void DrawLandmarkList()
        {
            // Collect all landmarks across all categories.
            var landmarks = new List<SWEF.Narration.LandmarkData>();
            foreach (SWEF.Narration.LandmarkCategory cat in System.Enum.GetValues(typeof(SWEF.Narration.LandmarkCategory)))
                landmarks.AddRange(_database.GetLandmarksByCategory(cat));

            // Remove duplicates.
            var seen = new HashSet<string>();
            var unique = new List<SWEF.Narration.LandmarkData>();
            foreach (var lm in landmarks)
            {
                if (seen.Add(lm.landmarkId)) unique.Add(lm);
            }

            // Apply filters.
            if (_filterByCategory)
                unique = unique.Where(l => l.category == _filterCategory).ToList();
            if (!string.IsNullOrEmpty(_searchQuery))
            {
                string q = _searchQuery.ToLower();
                unique = unique.Where(l =>
                    l.name.ToLower().Contains(q) ||
                    l.landmarkId.ToLower().Contains(q) ||
                    l.country.ToLower().Contains(q)).ToList();
            }

            if (unique.Count == 0)
            {
                EditorGUILayout.LabelField("No landmarks match the current filter.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            // Header.
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("ID",            GUILayout.Width(200));
                GUILayout.Label("Name",          GUILayout.Width(160));
                GUILayout.Label("Category",      GUILayout.Width(100));
                GUILayout.Label("Country",       GUILayout.Width(80));
                GUILayout.Label("Lat / Lon",     GUILayout.Width(120));
                GUILayout.Label("Radius (m)",    GUILayout.Width(80));
                GUILayout.Label("UNESCO",        GUILayout.Width(50));
            }

            foreach (var lm in unique)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(lm.landmarkId,       GUILayout.Width(200));
                    EditorGUILayout.LabelField(lm.name,             GUILayout.Width(160));
                    EditorGUILayout.LabelField(lm.category.ToString(), GUILayout.Width(100));
                    EditorGUILayout.LabelField(lm.country,          GUILayout.Width(80));
                    EditorGUILayout.LabelField($"{lm.latitude:F4}, {lm.longitude:F4}", GUILayout.Width(120));
                    EditorGUILayout.LabelField(lm.triggerRadius.ToString("F0"), GUILayout.Width(80));
                    EditorGUILayout.LabelField(lm.unescoWorldHeritage ? "✓" : "-", GUILayout.Width(50));
                }
            }
        }

        // ── Validation ────────────────────────────────────────────────────────────

        private void RunValidation()
        {
            _warnings.Clear();
            _errors.Clear();

            var allLandmarks = new List<SWEF.Narration.LandmarkData>();
            foreach (SWEF.Narration.LandmarkCategory cat in System.Enum.GetValues(typeof(SWEF.Narration.LandmarkCategory)))
                allLandmarks.AddRange(_database.GetLandmarksByCategory(cat));

            var idsSeen = new HashSet<string>();
            foreach (var lm in allLandmarks)
            {
                // Duplicate IDs.
                if (!idsSeen.Add(lm.landmarkId))
                    _errors.Add($"Duplicate ID: '{lm.landmarkId}'");

                // Empty required fields.
                if (string.IsNullOrEmpty(lm.landmarkId)) _errors.Add("Landmark with empty landmarkId found.");
                if (string.IsNullOrEmpty(lm.name))       _warnings.Add($"'{lm.landmarkId}' has no name.");

                // GPS sanity.
                if (lm.latitude < -90 || lm.latitude > 90)
                    _errors.Add($"'{lm.landmarkId}': latitude {lm.latitude} out of range [-90, 90].");
                if (lm.longitude < -180 || lm.longitude > 180)
                    _errors.Add($"'{lm.landmarkId}': longitude {lm.longitude} out of range [-180, 180].");

                // Trigger radius.
                if (lm.triggerRadius <= 0)
                    _warnings.Add($"'{lm.landmarkId}': triggerRadius is {lm.triggerRadius} (should be > 0).");
            }

            _showValidation = true;
            Debug.Log($"[SWEF] LandmarkDatabaseEditorWindow: validation done — {_errors.Count} errors, {_warnings.Count} warnings.");
        }

        private void DrawValidationResults()
        {
            if (_errors.Count == 0 && _warnings.Count == 0)
            {
                EditorGUILayout.HelpBox("✓ Validation passed — no issues found.", MessageType.Info);
            }
            else
            {
                foreach (var e in _errors)
                    EditorGUILayout.HelpBox(e, MessageType.Error);
                foreach (var w in _warnings)
                    EditorGUILayout.HelpBox(w, MessageType.Warning);
            }
            EditorGUILayout.Space();
        }

        // ── Export ────────────────────────────────────────────────────────────────

        private void ExportJson()
        {
            string path = EditorUtility.SaveFilePanel(
                "Export Landmark Database JSON", Application.dataPath, "landmark_database", "json");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                // Build a simple JSON structure.
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("  \"landmarks\": [");

                var allLandmarks = new List<SWEF.Narration.LandmarkData>();
                var seen = new HashSet<string>();
                foreach (SWEF.Narration.LandmarkCategory cat in System.Enum.GetValues(typeof(SWEF.Narration.LandmarkCategory)))
                    foreach (var lm in _database.GetLandmarksByCategory(cat))
                        if (seen.Add(lm.landmarkId)) allLandmarks.Add(lm);

                for (int i = 0; i < allLandmarks.Count; i++)
                {
                    string json = JsonUtility.ToJson(allLandmarks[i], true);
                    sb.Append("    ").Append(json);
                    if (i < allLandmarks.Count - 1) sb.AppendLine(",");
                    else sb.AppendLine();
                }
                sb.AppendLine("  ]");
                sb.AppendLine("}");

                System.IO.File.WriteAllText(path, sb.ToString());
                Debug.Log($"[SWEF] LandmarkDatabaseEditorWindow: exported {allLandmarks.Count} landmarks to {path}.");
                EditorUtility.RevealInFinder(path);
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Export Failed", ex.Message, "OK");
            }
        }

        // ── Auto-find ─────────────────────────────────────────────────────────────

        private void TryAutoFind()
        {
            string[] guids = AssetDatabase.FindAssets("t:LandmarkDatabase");
            if (guids.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                _database = AssetDatabase.LoadAssetAtPath<SWEF.Narration.LandmarkDatabase>(assetPath);
                if (_database != null)
                    Debug.Log($"[SWEF] Auto-found LandmarkDatabase at '{assetPath}'.");
            }
            else
            {
                EditorUtility.DisplayDialog("Not Found",
                    "No LandmarkDatabase asset found in the project. Create one via Assets > Create > SWEF > Narration > Landmark Database.",
                    "OK");
            }
        }
    }
}
#endif
