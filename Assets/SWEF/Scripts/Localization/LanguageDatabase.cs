using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Localization
{
    /// <summary>
    /// Static utility class that loads and caches JSON language files from
    /// <c>Assets/SWEF/Resources/Localization/</c>.
    /// Files are named <c>lang_xx.json</c> and contain a flat key-value object.
    /// </summary>
    public static class LanguageDatabase
    {
        // ── Cache ────────────────────────────────────────────────────────────────
        private static readonly Dictionary<SystemLanguage, Dictionary<string, string>> _cache
            = new Dictionary<SystemLanguage, Dictionary<string, string>>();

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the localization dictionary for <paramref name="lang"/>.
        /// Lazy-loads and caches the JSON file on first access.
        /// Returns <c>null</c> if the file cannot be found.
        /// </summary>
        /// <param name="lang">Target language.</param>
        /// <returns>Key-value dictionary or <c>null</c>.</returns>
        public static Dictionary<string, string> LoadLanguage(SystemLanguage lang)
        {
            if (_cache.TryGetValue(lang, out var cached))
                return cached;

            string fileName = GetFileName(lang);
            var asset = Resources.Load<TextAsset>($"Localization/{fileName}");
            if (asset == null)
            {
                Debug.LogWarning($"[SWEF] Language file not found: Resources/Localization/{fileName}.json");
                _cache[lang] = null;
                return null;
            }

            var dict = ParseJson(asset.text);
            _cache[lang] = dict;
            Debug.Log($"[SWEF] Loaded language file: {fileName} ({dict.Count} keys)");
            return dict;
        }

        /// <summary>
        /// Clears all cached language dictionaries to free memory.
        /// Subsequent calls to <see cref="LoadLanguage"/> will reload from disk.
        /// </summary>
        public static void ClearCache()
        {
            _cache.Clear();
            Debug.Log("[SWEF] Language cache cleared");
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        /// <summary>Returns the resource file name (without extension) for <paramref name="lang"/>.</summary>
        public static string GetFileName(SystemLanguage lang)
        {
            switch (lang)
            {
                case SystemLanguage.English:           return "lang_en";
                case SystemLanguage.Korean:            return "lang_ko";
                case SystemLanguage.Japanese:          return "lang_ja";
                case SystemLanguage.ChineseSimplified: return "lang_zh";
                case SystemLanguage.Spanish:           return "lang_es";
                case SystemLanguage.French:            return "lang_fr";
                case SystemLanguage.German:            return "lang_de";
                case SystemLanguage.Portuguese:        return "lang_pt";
                default:                               return "lang_en";
            }
        }

        // ── JSON parsing ─────────────────────────────────────────────────────────
        // Manual flat-JSON parser — no Newtonsoft or third-party packages required.
        private static Dictionary<string, string> ParseJson(string json)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(json)) return result;

            // Strip surrounding braces
            json = json.Trim();
            if (json.StartsWith("{")) json = json.Substring(1);
            if (json.EndsWith("}"))  json = json.Substring(0, json.Length - 1);

            int i = 0;
            while (i < json.Length)
            {
                // Skip whitespace
                while (i < json.Length && (json[i] == ' ' || json[i] == '\t' || json[i] == '\r' || json[i] == '\n' || json[i] == ','))
                    i++;

                if (i >= json.Length) break;
                if (json[i] != '"') { i++; continue; }

                // Read key
                string key = ReadJsonString(json, ref i);

                // Skip ':'
                while (i < json.Length && json[i] != ':') i++;
                i++; // consume ':'

                // Skip whitespace
                while (i < json.Length && (json[i] == ' ' || json[i] == '\t')) i++;

                if (i >= json.Length) break;

                // Read value
                string value = (json[i] == '"') ? ReadJsonString(json, ref i) : ReadJsonPrimitive(json, ref i);
                result[key] = value;
            }

            return result;
        }

        private static string ReadJsonString(string json, ref int i)
        {
            i++; // skip opening '"'
            var sb = new System.Text.StringBuilder();
            while (i < json.Length && json[i] != '"')
            {
                if (json[i] == '\\' && i + 1 < json.Length)
                {
                    i++;
                    switch (json[i])
                    {
                        case '"':  sb.Append('"');  break;
                        case '\\': sb.Append('\\'); break;
                        case '/':  sb.Append('/');  break;
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
                {
                    sb.Append(json[i]);
                }
                i++;
            }
            if (i < json.Length) i++; // skip closing '"'
            return sb.ToString();
        }

        private static string ReadJsonPrimitive(string json, ref int i)
        {
            int start = i;
            while (i < json.Length && json[i] != ',' && json[i] != '}' && json[i] != '\n' && json[i] != '\r')
                i++;
            return json.Substring(start, i - start).Trim();
        }
    }
}
