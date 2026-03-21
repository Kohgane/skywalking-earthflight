#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SWEF.HiddenGems;

namespace SWEF.Editor
{
    /// <summary>
    /// Unity Editor tool for the Hidden Gems system.
    /// Open via <c>SWEF → Hidden Gem Editor</c>.
    /// </summary>
    public class HiddenGemEditorWindow : EditorWindow
    {
        // ── Menu item ─────────────────────────────────────────────────────────────
        [MenuItem("SWEF/Hidden Gem Editor")]
        public static void Open() => GetWindow<HiddenGemEditorWindow>("Hidden Gem Editor");

        // ── State ─────────────────────────────────────────────────────────────────
        private List<HiddenGemDefinition> _gems;
        private Vector2  _scrollPos;
        private string   _filter    = "";
        private string   _exportPath = "";
        private string   _statusMsg  = "";

        // ── Styles ────────────────────────────────────────────────────────────────
        private GUIStyle _headerStyle;
        private GUIStyle _errorStyle;

        // ── Lifecycle ─────────────────────────────────────────────────────────────
        private void OnEnable()
        {
            _gems = HiddenGemDatabase.GetAllGems();
        }

        private void OnGUI()
        {
            EnsureStyles();

            GUILayout.Label("SWEF — Hidden Gem Editor", _headerStyle);
            EditorGUILayout.Space(4);

            DrawToolbar();
            EditorGUILayout.Space(4);

            DrawStats();
            EditorGUILayout.Space(4);

            DrawGemList();
            EditorGUILayout.Space(4);

            DrawExportImport();

            if (!string.IsNullOrEmpty(_statusMsg))
                EditorGUILayout.HelpBox(_statusMsg, MessageType.Info);
        }

        // ── Toolbar ───────────────────────────────────────────────────────────────

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Validate All", EditorStyles.toolbarButton, GUILayout.Width(90)))
                ValidateAll();

            if (GUILayout.Button("Check Duplicates", EditorStyles.toolbarButton, GUILayout.Width(110)))
                CheckDuplicates();

            GUILayout.FlexibleSpace();

            _filter = GUILayout.TextField(_filter, EditorStyles.toolbarSearchField, GUILayout.Width(180));

            EditorGUILayout.EndHorizontal();
        }

        // ── Stats panel ───────────────────────────────────────────────────────────

        private void DrawStats()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label($"Total gems: {_gems.Count}", EditorStyles.boldLabel);

            // Continent distribution
            GUILayout.Label("Continent distribution:");
            foreach (GemContinent c in System.Enum.GetValues(typeof(GemContinent)))
            {
                int cnt = _gems.Count(g => g.continent == c);
                int barW = cnt * 8;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(c.ToString(), GUILayout.Width(120));
                GUILayout.Label($"{cnt}", GUILayout.Width(28));
                Rect r = GUILayoutUtility.GetRect(barW, 12, GUILayout.ExpandWidth(false));
                EditorGUI.DrawRect(r, GetContinentColor(c));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);

            // Rarity distribution
            GUILayout.Label("Rarity distribution:");
            foreach (GemRarity r in System.Enum.GetValues(typeof(GemRarity)))
            {
                int cnt = _gems.Count(g => g.rarity == r);
                ColorUtility.TryParseHtmlString(HiddenGemDefinition.RarityColor(r), out Color rc);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(r.ToString(), GUILayout.Width(90));
                GUILayout.Label($"{cnt}", GUILayout.Width(28));
                Rect rect = GUILayoutUtility.GetRect(cnt * 12, 12, GUILayout.ExpandWidth(false));
                EditorGUI.DrawRect(rect, rc);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        // ── Gem list ──────────────────────────────────────────────────────────────

        private void DrawGemList()
        {
            EditorGUILayout.LabelField("All Gems", EditorStyles.boldLabel);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(280));

            string filter = _filter.ToLowerInvariant();

            foreach (var gem in _gems)
            {
                if (!string.IsNullOrEmpty(filter) &&
                    !gem.gemId.ToLowerInvariant().Contains(filter) &&
                    !gem.country.ToLowerInvariant().Contains(filter))
                    continue;

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                ColorUtility.TryParseHtmlString(HiddenGemDefinition.RarityColor(gem.rarity), out Color rc);
                var oldColor = GUI.color;
                GUI.color = rc;
                GUILayout.Label("■", GUILayout.Width(14));
                GUI.color = oldColor;

                GUILayout.Label(gem.gemId,          GUILayout.Width(220));
                GUILayout.Label(gem.country,        GUILayout.Width(110));
                GUILayout.Label(gem.rarity.ToString(),   GUILayout.Width(70));
                GUILayout.Label(gem.continent.ToString(), GUILayout.Width(90));
                GUILayout.Label($"({gem.latitude:F4}, {gem.longitude:F4})", GUILayout.Width(160));

                bool coordsValid = ValidateCoordinates(gem);
                if (!coordsValid)
                {
                    var es = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } };
                    GUILayout.Label("⚠ GPS", es, GUILayout.Width(42));
                }

                if (GUILayout.Button("Test", GUILayout.Width(44)))
                    TestDiscovery(gem);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        // ── Export / Import ───────────────────────────────────────────────────────

        private void DrawExportImport()
        {
            EditorGUILayout.BeginHorizontal();
            _exportPath = EditorGUILayout.TextField("JSON path:", _exportPath, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
                _exportPath = EditorUtility.SaveFilePanel("Export gems", "", "hidden_gems_export", "json");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export to JSON"))
                ExportToJson();
            if (GUILayout.Button("Import from JSON"))
                ImportFromJson();
            EditorGUILayout.EndHorizontal();
        }

        // ── Validation ────────────────────────────────────────────────────────────

        private void ValidateAll()
        {
            int errors = 0;
            foreach (var gem in _gems)
            {
                if (!ValidateCoordinates(gem))
                {
                    Debug.LogError($"[SWEF] Gem '{gem.gemId}': invalid GPS ({gem.latitude}, {gem.longitude})");
                    errors++;
                }
                if (string.IsNullOrEmpty(gem.gemId))
                {
                    Debug.LogError("[SWEF] Gem with empty gemId found.");
                    errors++;
                }
            }
            _statusMsg = errors == 0
                ? $"Validation passed — {_gems.Count} gems OK."
                : $"Validation found {errors} error(s). Check Console.";
        }

        private void CheckDuplicates()
        {
            var seen  = new HashSet<string>();
            int dupes = 0;
            foreach (var gem in _gems)
            {
                if (!seen.Add(gem.gemId))
                {
                    Debug.LogError($"[SWEF] Duplicate gemId: '{gem.gemId}'");
                    dupes++;
                }
            }
            _statusMsg = dupes == 0 ? "No duplicate IDs found." : $"{dupes} duplicate ID(s) found!";
        }

        private static bool ValidateCoordinates(HiddenGemDefinition gem)
            => gem.latitude  >= -90.0  && gem.latitude  <= 90.0
            && gem.longitude >= -180.0 && gem.longitude <= 180.0;

        // ── Test discovery ────────────────────────────────────────────────────────

        private void TestDiscovery(HiddenGemDefinition gem)
        {
            if (!Application.isPlaying)
            {
                _statusMsg = "Test Discovery requires Play mode.";
                return;
            }
            HiddenGemManager.Instance?.ForceDiscover(gem.gemId);
            _statusMsg = $"Force-discovered: {gem.gemId}";
        }

        // ── JSON Export / Import ──────────────────────────────────────────────────

        private void ExportToJson()
        {
            if (string.IsNullOrEmpty(_exportPath))
            {
                _statusMsg = "Set an export path first.";
                return;
            }
            try
            {
                var wrapper = new GemListWrapper { gems = _gems };
                File.WriteAllText(_exportPath, JsonUtility.ToJson(wrapper, true));
                _statusMsg = $"Exported {_gems.Count} gems to {_exportPath}";
            }
            catch (System.Exception e)
            {
                _statusMsg = $"Export error: {e.Message}";
            }
        }

        private void ImportFromJson()
        {
            string path = EditorUtility.OpenFilePanel("Import gems JSON", "", "json");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                string json = File.ReadAllText(path);
                var wrapper = JsonUtility.FromJson<GemListWrapper>(json);
                if (wrapper?.gems != null)
                {
                    _statusMsg = $"Imported {wrapper.gems.Count} gems from {path} (display only — not saved to database).";
                    Debug.Log($"[SWEF] Imported gems:\n{string.Join("\n", wrapper.gems.Select(g => g.gemId))}");
                }
            }
            catch (System.Exception e)
            {
                _statusMsg = $"Import error: {e.Message}";
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void EnsureStyles()
        {
            if (_headerStyle == null)
                _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            if (_errorStyle == null)
                _errorStyle  = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } };
        }

        private static Color GetContinentColor(GemContinent c) => c switch
        {
            GemContinent.Asia          => new Color(1f,  0.6f, 0f),
            GemContinent.Europe        => new Color(0.2f,0.6f, 1f),
            GemContinent.NorthAmerica  => new Color(0.2f,0.8f, 0.2f),
            GemContinent.SouthAmerica  => new Color(0.8f,0.8f, 0f),
            GemContinent.Africa        => new Color(1f,  0.4f, 0.4f),
            GemContinent.Oceania       => new Color(0f,  0.8f, 0.8f),
            GemContinent.Antarctica    => new Color(0.8f,0.8f, 0.8f),
            _                          => Color.grey
        };

        // ── Serialization wrapper ─────────────────────────────────────────────────
        [System.Serializable]
        private class GemListWrapper
        {
            public List<HiddenGemDefinition> gems;
        }
    }
}
#endif
