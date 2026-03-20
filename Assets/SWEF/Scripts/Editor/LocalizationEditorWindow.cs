#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SWEF.Editor
{
    /// <summary>
    /// Unity Editor window for managing SWEF localization strings.
    /// Open via <b>SWEF → Localization Editor</b>.
    /// Provides a table view (rows = keys, columns = languages), highlights
    /// missing translations in red, and supports add/remove, JSON export, and CSV import.
    /// </summary>
    public class LocalizationEditorWindow : EditorWindow
    {
        // ── Menu item ────────────────────────────────────────────────────────────
        [MenuItem("SWEF/Localization Editor")]
        public static void Open()
        {
            var window = GetWindow<LocalizationEditorWindow>("Localization Editor");
            window.minSize = new Vector2(900, 500);
            window.Show();
        }

        // ── Column definitions ───────────────────────────────────────────────────
        private static readonly (SystemLanguage lang, string code, string label)[] Columns =
        {
            (SystemLanguage.English,           "en", "English"),
            (SystemLanguage.Korean,            "ko", "한국어"),
            (SystemLanguage.Japanese,          "ja", "日本語"),
            (SystemLanguage.ChineseSimplified, "zh", "简体中文"),
            (SystemLanguage.Spanish,           "es", "Español"),
            (SystemLanguage.French,            "fr", "Français"),
            (SystemLanguage.German,            "de", "Deutsch"),
            (SystemLanguage.Portuguese,        "pt", "Português"),
        };

        private const string ResourcesPath = "Assets/SWEF/Resources/Localization";

        // ── State ────────────────────────────────────────────────────────────────
        private Dictionary<string, Dictionary<string, string>> _data; // [langCode][key] = value
        private List<string> _keys = new List<string>();
        private Vector2 _scrollPos;
        private string _searchFilter = string.Empty;
        private string _newKeyInput  = string.Empty;
        private bool   _dirty        = false;
        private static readonly Color MissingColor = new Color(1f, 0.3f, 0.3f, 0.4f);
        private static readonly Color AltRowColor  = new Color(0f, 0f, 0f, 0.08f);

        private const float KeyColWidth   = 240f;
        private const float LangColWidth  = 170f;
        private const float RowHeight     = 20f;

        // ── Unity EditorWindow lifecycle ─────────────────────────────────────────
        private void OnEnable()
        {
            LoadAll();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawSearchBar();
            DrawTable();
            DrawAddKeyRow();
        }

        // ── Toolbar ──────────────────────────────────────────────────────────────
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(70)))
                LoadAll();

            if (GUILayout.Button("Save All", EditorStyles.toolbarButton, GUILayout.Width(70)))
                SaveAll();

            if (GUILayout.Button("Export JSON", EditorStyles.toolbarButton, GUILayout.Width(90)))
                SaveAll();

            if (GUILayout.Button("Import CSV", EditorStyles.toolbarButton, GUILayout.Width(90)))
                ImportCSV();

            GUILayout.FlexibleSpace();

            if (_dirty)
            {
                var prev = GUI.color;
                GUI.color = Color.yellow;
                GUILayout.Label("● Unsaved changes", EditorStyles.toolbarButton);
                GUI.color = prev;
            }

            EditorGUILayout.EndHorizontal();
        }

        // ── Search bar ───────────────────────────────────────────────────────────
        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(55));
            _searchFilter = EditorGUILayout.TextField(_searchFilter, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("✕", GUILayout.Width(22)))
                _searchFilter = string.Empty;
            EditorGUILayout.EndHorizontal();
        }

        // ── Table ────────────────────────────────────────────────────────────────
        private void DrawTable()
        {
            // Header row
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Key", EditorStyles.boldLabel, GUILayout.Width(KeyColWidth));
            foreach (var col in Columns)
                GUILayout.Label(col.label, EditorStyles.boldLabel, GUILayout.Width(LangColWidth));
            GUILayout.Label("", GUILayout.Width(26)); // delete button column
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            var filtered = string.IsNullOrEmpty(_searchFilter)
                ? _keys
                : _keys.FindAll(k => k.IndexOf(_searchFilter, System.StringComparison.OrdinalIgnoreCase) >= 0);

            for (int rowIdx = 0; rowIdx < filtered.Count; rowIdx++)
            {
                string key = filtered[rowIdx];

                if (rowIdx % 2 == 1)
                {
                    var prev = GUI.backgroundColor;
                    GUI.backgroundColor = AltRowColor;
                    EditorGUILayout.BeginHorizontal(GUI.skin.box);
                    GUI.backgroundColor = prev;
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                }

                GUILayout.Label(key, GUILayout.Width(KeyColWidth));

                foreach (var col in Columns)
                {
                    if (!_data.TryGetValue(col.code, out var langDict))
                        langDict = new Dictionary<string, string>();

                    bool missing = !langDict.ContainsKey(key) || string.IsNullOrWhiteSpace(langDict[key]);

                    if (missing)
                    {
                        var prev = GUI.backgroundColor;
                        GUI.backgroundColor = MissingColor;
                        string newVal = EditorGUILayout.TextField(
                            missing ? "(missing)" : langDict[key],
                            GUILayout.Width(LangColWidth), GUILayout.Height(RowHeight));
                        GUI.backgroundColor = prev;

                        if (newVal != "(missing)" && newVal != langDict.GetValueOrDefault(key, string.Empty))
                        {
                            if (!_data.ContainsKey(col.code))
                                _data[col.code] = new Dictionary<string, string>();
                            _data[col.code][key] = newVal;
                            _dirty = true;
                        }
                    }
                    else
                    {
                        string current = langDict[key];
                        string newVal  = EditorGUILayout.TextField(current,
                            GUILayout.Width(LangColWidth), GUILayout.Height(RowHeight));
                        if (newVal != current)
                        {
                            _data[col.code][key] = newVal;
                            _dirty = true;
                        }
                    }
                }

                // Delete key button
                if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(RowHeight)))
                {
                    if (EditorUtility.DisplayDialog("Remove Key", $"Remove key \"{key}\" from all languages?", "Remove", "Cancel"))
                    {
                        _keys.Remove(key);
                        foreach (var d in _data.Values)
                            d.Remove(key);
                        _dirty = true;
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        // ── Add new key ──────────────────────────────────────────────────────────
        private void DrawAddKeyRow()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("New key:", GUILayout.Width(60));
            _newKeyInput = EditorGUILayout.TextField(_newKeyInput, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Add", GUILayout.Width(60)))
            {
                string k = _newKeyInput.Trim();
                if (!string.IsNullOrEmpty(k) && !_keys.Contains(k))
                {
                    _keys.Add(k);
                    _keys.Sort();
                    _dirty      = true;
                    _newKeyInput = string.Empty;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        // ── Load all language files ──────────────────────────────────────────────
        private void LoadAll()
        {
            _data = new Dictionary<string, Dictionary<string, string>>();
            _keys.Clear();

            var allKeys = new HashSet<string>();

            foreach (var col in Columns)
            {
                string path = Path.Combine(ResourcesPath, $"lang_{col.code}.json");
                if (!File.Exists(path)) continue;

                string json = File.ReadAllText(path, Encoding.UTF8);
                var dict    = ParseJson(json);
                _data[col.code] = dict;
                foreach (var k in dict.Keys)
                    allKeys.Add(k);
            }

            _keys = new List<string>(allKeys);
            _keys.Sort();
            _dirty = false;
        }

        // ── Save all language files ──────────────────────────────────────────────
        private void SaveAll()
        {
            Directory.CreateDirectory(ResourcesPath);

            foreach (var col in Columns)
            {
                if (!_data.TryGetValue(col.code, out var dict))
                    dict = new Dictionary<string, string>();

                string path = Path.Combine(ResourcesPath, $"lang_{col.code}.json");
                string json = SerializeJson(dict);
                File.WriteAllText(path, json, Encoding.UTF8);
            }

            AssetDatabase.Refresh();
            _dirty = false;
            Debug.Log("[SWEF] Localization files saved.");
        }

        // ── CSV import ───────────────────────────────────────────────────────────
        private void ImportCSV()
        {
            string path = EditorUtility.OpenFilePanel("Import CSV", Application.dataPath, "csv");
            if (string.IsNullOrEmpty(path)) return;

            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length == 0) return;

            // First row = header: key, en, ko, ja, zh, es, fr, de, pt
            string[] header = SplitCSVLine(lines[0]);

            for (int i = 1; i < lines.Length; i++)
            {
                string[] cols = SplitCSVLine(lines[i]);
                if (cols.Length == 0) continue;
                string key = cols[0].Trim();
                if (string.IsNullOrEmpty(key)) continue;

                if (!_keys.Contains(key))
                {
                    _keys.Add(key);
                    _keys.Sort();
                }

                for (int c = 1; c < header.Length && c < cols.Length; c++)
                {
                    string code = header[c].Trim().ToLower();
                    if (!_data.ContainsKey(code))
                        _data[code] = new Dictionary<string, string>();
                    _data[code][key] = cols[c].Trim();
                }
            }

            _dirty = true;
            Debug.Log("[SWEF] CSV imported.");
        }

        // ── Helpers ──────────────────────────────────────────────────────────────
        private static Dictionary<string, string> ParseJson(string json)
        {
            // Reuse LanguageDatabase's logic via a temporary TextAsset
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(json)) return result;

            json = json.Trim();
            if (json.StartsWith("{")) json = json.Substring(1);
            if (json.EndsWith("}"))  json = json.Substring(0, json.Length - 1);

            int i = 0;
            while (i < json.Length)
            {
                while (i < json.Length && json[i] != '"') i++;
                if (i >= json.Length) break;

                string key = ReadString(json, ref i);

                while (i < json.Length && json[i] != ':') i++;
                i++;
                while (i < json.Length && (json[i] == ' ' || json[i] == '\t')) i++;
                if (i >= json.Length) break;

                string value = (json[i] == '"') ? ReadString(json, ref i) : ReadPrimitive(json, ref i);
                result[key] = value;
            }
            return result;
        }

        private static string ReadString(string json, ref int i)
        {
            i++; // skip "
            var sb = new StringBuilder();
            while (i < json.Length && json[i] != '"')
            {
                if (json[i] == '\\' && i + 1 < json.Length)
                {
                    i++;
                    switch (json[i])
                    {
                        case '"':  sb.Append('"');  break;
                        case '\\': sb.Append('\\'); break;
                        case 'n':  sb.Append('\n'); break;
                        case 'r':  sb.Append('\r'); break;
                        case 't':  sb.Append('\t'); break;
                        case 'u':
                            if (i + 4 < json.Length)
                            {
                                string hex = json.Substring(i + 1, 4);
                                if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int code))
                                    sb.Append((char)code);
                                i += 4;
                            }
                            break;
                        default: sb.Append(json[i]); break;
                    }
                }
                else
                    sb.Append(json[i]);
                i++;
            }
            if (i < json.Length) i++;
            return sb.ToString();
        }

        private static string ReadPrimitive(string json, ref int i)
        {
            int start = i;
            while (i < json.Length && json[i] != ',' && json[i] != '}' && json[i] != '\n')
                i++;
            return json.Substring(start, i - start).Trim();
        }

        private static string SerializeJson(Dictionary<string, string> dict)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            int count = 0;
            foreach (var kv in dict)
            {
                count++;
                string escaped = kv.Value
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
                sb.Append($"    \"{kv.Key}\": \"{escaped}\"");
                if (count < dict.Count) sb.Append(",");
                sb.AppendLine();
            }
            sb.Append("}");
            return sb.ToString();
        }

        private static string[] SplitCSVLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current  = new StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')       { inQuotes = !inQuotes; }
                else if (c == ',' && !inQuotes) { result.Add(current.ToString()); current.Clear(); }
                else                { current.Append(c); }
            }
            result.Add(current.ToString());
            return result.ToArray();
        }
    }
}
#endif
