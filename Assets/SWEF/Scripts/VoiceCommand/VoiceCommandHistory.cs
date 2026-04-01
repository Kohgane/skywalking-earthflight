// VoiceCommandHistory.cs — SWEF Voice Command & Cockpit Voice Assistant System
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.VoiceCommand
{
    /// <summary>Stores a single entry in the voice command history log.</summary>
    [Serializable]
    public class VoiceHistoryEntry
    {
        public string commandId;
        public string primaryPhrase;
        public CommandCategory category;
        public bool success;
        public string responseLocKey;
        public float confidence;
        public string timestamp;       // ISO-8601 UTC string for JSON serialisation
    }

    /// <summary>
    /// MonoBehaviour that maintains a circular buffer of the last N voice commands,
    /// with JSON persistence to <c>persistentDataPath/voice_history.json</c>.
    /// </summary>
    public class VoiceCommandHistory : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private VoiceAssistantConfig _config;

        // ── Constants ─────────────────────────────────────────────────────────────

        private const string FileName = "voice_history.json";

        // ── Runtime state ─────────────────────────────────────────────────────────

        private List<VoiceHistoryEntry> _buffer = new List<VoiceHistoryEntry>();
        private int _capacity = 100;
        private bool _dirty = false;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _capacity = _config != null ? _config.maxHistoryEntries : 100;
            LoadFromDisk();
        }

        private void OnDestroy()
        {
            if (_dirty) SaveToDisk();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Records a completed command execution to the history buffer.
        /// </summary>
        public void Record(VoiceCommandResult result, float confidence)
        {
            if (result.executedCommand == null) return;

            var entry = new VoiceHistoryEntry
            {
                commandId     = result.executedCommand.commandId,
                primaryPhrase = result.executedCommand.primaryPhrase,
                category      = result.executedCommand.category,
                success       = result.success,
                responseLocKey = result.responseLocKey,
                confidence    = confidence,
                timestamp     = result.timestamp.ToString("o")
            };

            _buffer.Add(entry);

            // Enforce capacity (circular buffer: remove oldest).
            while (_buffer.Count > _capacity)
                _buffer.RemoveAt(0);

            _dirty = true;
        }

        /// <summary>Returns the most recent <paramref name="count"/> entries (newest first).</summary>
        public List<VoiceHistoryEntry> GetRecent(int count)
        {
            int start  = Math.Max(0, _buffer.Count - count);
            var result = _buffer.GetRange(start, _buffer.Count - start);
            result.Reverse();
            return result;
        }

        /// <summary>Returns all entries for the given category, newest first.</summary>
        public List<VoiceHistoryEntry> GetByCategory(CommandCategory category)
        {
            var result = new List<VoiceHistoryEntry>();
            for (int i = _buffer.Count - 1; i >= 0; i--)
                if (_buffer[i].category == category) result.Add(_buffer[i]);
            return result;
        }

        /// <summary>Clears the in-memory buffer and removes the persisted file.</summary>
        public void ClearHistory()
        {
            _buffer.Clear();
            _dirty = false;
            try
            {
                string path = FilePath();
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VoiceCommandHistory] Could not delete history file: {ex.Message}");
            }
        }

        /// <summary>Returns the total number of recorded entries.</summary>
        public int Count => _buffer.Count;

        // ── Persistence ───────────────────────────────────────────────────────────

        private void SaveToDisk()
        {
            try
            {
                string json = JsonUtility.ToJson(new SerializableList<VoiceHistoryEntry>(_buffer), true);
                File.WriteAllText(FilePath(), json);
                _dirty = false;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VoiceCommandHistory] Save failed: {ex.Message}");
            }
        }

        private void LoadFromDisk()
        {
            try
            {
                string path = FilePath();
                if (!File.Exists(path)) return;

                string json    = File.ReadAllText(path);
                var    wrapper = JsonUtility.FromJson<SerializableList<VoiceHistoryEntry>>(json);
                if (wrapper?.items != null)
                    _buffer = wrapper.items;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VoiceCommandHistory] Load failed: {ex.Message}");
                _buffer = new List<VoiceHistoryEntry>();
            }
        }

        private static string FilePath() =>
            Path.Combine(Application.persistentDataPath, FileName);

        // ── Helper ────────────────────────────────────────────────────────────────

        /// <summary>JsonUtility-compatible list wrapper.</summary>
        [Serializable]
        private class SerializableList<T>
        {
            public List<T> items;
            public SerializableList(List<T> src) { items = src; }
        }
    }
}
