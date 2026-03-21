using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SWEF.Multiplayer;

namespace SWEF.Journal
{
    /// <summary>
    /// Maintains a global registry of all tags ever used across journal entries.
    /// Persists the registry to <c>Application.persistentDataPath/journal_tags.json</c>.
    /// Also provides smart auto-suggestion based on flight characteristics.
    /// </summary>
    public class JournalTagManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static JournalTagManager Instance { get; private set; }

        // ── Internal state ────────────────────────────────────────────────────────
        private readonly Dictionary<string, int> _tagUsage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private static readonly string SaveFileName = "journal_tags.json";
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        // ── Serialisation wrapper ─────────────────────────────────────────────────
        [Serializable]
        private class TagSaveData
        {
            public List<TagEntry> tags = new List<TagEntry>();
        }

        [Serializable]
        private class TagEntry
        {
            public string tag;
            public int    useCount;
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadTags();
        }

        private void OnApplicationQuit() => SaveTags();
        private void OnApplicationPause(bool pause) { if (pause) SaveTags(); }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns all known tags sorted alphabetically.</summary>
        public List<string> GetAllTags()
        {
            var list = new List<string>(_tagUsage.Keys);
            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }

        /// <summary>
        /// Returns auto-suggested tags based on the characteristics of the given flight entry.
        /// </summary>
        public List<string> GetSuggestedTags(FlightLogEntry entry)
        {
            var suggestions = new List<string>();
            if (entry == null) return suggestions;

            // Night flight: approximate by checking current time of day.
            int hour = DateTime.Now.Hour;
            if (hour < 6 || hour >= 20)
                suggestions.Add("night");

            // Scenic: screenshots were taken.
            if (entry.screenshotPaths != null && entry.screenshotPaths.Length > 0)
                suggestions.Add("scenic");

            // Record: altitude or speed is noteworthy (placeholder — JournalManager can refine).
            if (entry.maxAltitudeM > 10000f)
                suggestions.Add("high-altitude");

            // Tour completed.
            if (!string.IsNullOrEmpty(entry.tourName))
                suggestions.Add("tour");

            // Multiplayer: check if multiplayer system is active.
            var net = FindFirstObjectByType<NetworkManager2>();
            if (net != null && net.CurrentLobby != null)
                suggestions.Add("multiplayer");

            // Weather-based tags.
            if (!string.IsNullOrEmpty(entry.weatherCondition))
            {
                string w = entry.weatherCondition.ToLowerInvariant();
                if (w.Contains("storm") || w.Contains("thunder")) suggestions.Add("stormy");
                else if (w.Contains("fog") || w.Contains("mist"))  suggestions.Add("foggy");
                else if (w.Contains("clear") || w.Contains("sun")) suggestions.Add("clear");
                else if (w.Contains("rain") || w.Contains("drizzle")) suggestions.Add("rainy");
                else if (w.Contains("snow"))                         suggestions.Add("snowy");
            }

            return suggestions;
        }

        /// <summary>Returns the most frequently used tags.</summary>
        /// <param name="count">Maximum number of tags to return.</param>
        public List<string> GetPopularTags(int count)
        {
            var list = new List<KeyValuePair<string, int>>(_tagUsage);
            list.Sort((a, b) => b.Value.CompareTo(a.Value));
            var result = new List<string>(Mathf.Min(count, list.Count));
            for (int i = 0; i < result.Capacity; i++)
                result.Add(list[i].Key);
            return result;
        }

        /// <summary>Registers a custom tag in the global registry.</summary>
        public void AddCustomTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;
            tag = tag.Trim().ToLowerInvariant();
            _tagUsage.TryGetValue(tag, out int count);
            _tagUsage[tag] = count; // ensure present even with 0 uses
            SaveTags();
        }

        /// <summary>
        /// Records usage of the given tags (called when a journal entry is saved).
        /// </summary>
        public void RecordTagUsage(string[] tags)
        {
            if (tags == null) return;
            foreach (var t in tags)
            {
                if (string.IsNullOrWhiteSpace(t)) continue;
                string key = t.Trim().ToLowerInvariant();
                _tagUsage.TryGetValue(key, out int c);
                _tagUsage[key] = c + 1;
            }
        }

        // ── Persistence ───────────────────────────────────────────────────────────
        private void SaveTags()
        {
            try
            {
                var data = new TagSaveData();
                foreach (var kv in _tagUsage)
                    data.tags.Add(new TagEntry { tag = kv.Key, useCount = kv.Value });
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] JournalTagManager: Save failed — {ex.Message}");
            }
        }

        private void LoadTags()
        {
            if (!File.Exists(SavePath)) return;
            try
            {
                var data = JsonUtility.FromJson<TagSaveData>(File.ReadAllText(SavePath));
                if (data?.tags == null) return;
                foreach (var te in data.tags)
                    if (!string.IsNullOrWhiteSpace(te.tag))
                        _tagUsage[te.tag] = te.useCount;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] JournalTagManager: Load failed — {ex.Message}");
            }
        }
    }
}
