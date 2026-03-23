using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.Replay
{
    /// <summary>
    /// Phase 48 — Persists <see cref="FlightRecording"/> objects to and from
    /// <see cref="Application.persistentDataPath"/> using JSON serialisation.
    /// Supports auto-save, storage quota management, and a lightweight metadata
    /// index so the UI can list recordings without loading full frame data.
    /// </summary>
    public class RecordingStorageManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static RecordingStorageManager Instance { get; private set; }

        #endregion

        #region Constants

        private const string RecordingsFolder   = "Recordings";
        private const string IndexFileName      = "recording_index.json";
        private const long   QuotaWarnBytes     = 50L * 1024 * 1024;  // 50 MB
        private const long   QuotaHardBytes     = 100L * 1024 * 1024; // 100 MB

        #endregion

        #region Events

        /// <summary>Fired after a recording is successfully saved.</summary>
        public event Action<FlightRecording> OnRecordingSaved;

        /// <summary>Fired after a recording is deleted; carries the recording ID.</summary>
        public event Action<string> OnRecordingDeleted;

        /// <summary>Fired when storage usage approaches or exceeds the warning quota.</summary>
        public event Action<long> OnStorageWarning;

        #endregion

        #region Private State

        private string _rootPath;
        private RecordingIndex _index = new RecordingIndex();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _rootPath = Path.Combine(Application.persistentDataPath, RecordingsFolder);
            EnsureDirectory(_rootPath);
            LoadIndex();
        }

        private void OnEnable()
        {
            if (FlightRecorderManager.Instance != null)
                FlightRecorderManager.Instance.OnRecordingStopped += OnRecordingStopped;
        }

        private void OnDisable()
        {
            if (FlightRecorderManager.Instance != null)
                FlightRecorderManager.Instance.OnRecordingStopped -= OnRecordingStopped;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Saves <paramref name="recording"/> to persistent storage.
        /// Overwrites any existing file with the same <c>recordingId</c>.
        /// </summary>
        public void SaveRecording(FlightRecording recording)
        {
            if (recording == null) return;
            if (string.IsNullOrEmpty(recording.recordingId))
                recording.recordingId = Guid.NewGuid().ToString();

            if (GetUsedBytes() + EstimateBytes(recording) > QuotaHardBytes)
            {
                Debug.LogWarning("[SWEF] RecordingStorageManager: Hard quota reached — cannot save.");
                return;
            }

            string fileName = RecordingSerializer.BuildFileName(recording);
            string path     = Path.Combine(_rootPath, fileName);
            string json     = RecordingSerializer.ToJson(recording);

            try
            {
                File.WriteAllText(path, json);
                _index.AddOrUpdate(new RecordingMeta(recording, fileName));
                SaveIndex();
                OnRecordingSaved?.Invoke(recording);
                CheckQuota();
                Debug.Log($"[SWEF] RecordingStorageManager: Saved '{fileName}'.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] RecordingStorageManager: Failed to save — {ex.Message}");
            }
        }

        /// <summary>Loads and returns the recording with the given <paramref name="recordingId"/>.</summary>
        public FlightRecording LoadRecording(string recordingId)
        {
            if (string.IsNullOrEmpty(recordingId)) return null;

            var meta = _index.Find(recordingId);
            if (meta == null)
            {
                Debug.LogWarning($"[SWEF] RecordingStorageManager: ID '{recordingId}' not in index.");
                return null;
            }

            string path = Path.Combine(_rootPath, meta.fileName);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SWEF] RecordingStorageManager: File not found — '{path}'.");
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                return RecordingSerializer.FromJson(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] RecordingStorageManager: Failed to load '{recordingId}' — {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Returns lightweight metadata for all saved recordings without loading
        /// full frame data.
        /// </summary>
        public IReadOnlyList<RecordingMeta> GetAllRecordings() => _index.Entries;

        /// <summary>Deletes the recording with the given <paramref name="recordingId"/> from disk and index.</summary>
        public void DeleteRecording(string recordingId)
        {
            if (string.IsNullOrEmpty(recordingId)) return;

            var meta = _index.Find(recordingId);
            if (meta != null)
            {
                string path = Path.Combine(_rootPath, meta.fileName);
                if (File.Exists(path))
                {
                    try { File.Delete(path); }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[SWEF] RecordingStorageManager: Delete failed — {ex.Message}");
                        return;
                    }
                }
                _index.Remove(recordingId);
                SaveIndex();
            }

            OnRecordingDeleted?.Invoke(recordingId);
        }

        /// <summary>Returns total bytes used by saved recordings.</summary>
        public long GetUsedBytes()
        {
            long total = 0L;
            if (!Directory.Exists(_rootPath)) return 0L;
            foreach (string f in Directory.GetFiles(_rootPath, "*.json"))
            {
                try { total += new FileInfo(f).Length; }
                catch { /* ignore */ }
            }
            return total;
        }

        #endregion

        #region Private — Helpers

        private void OnRecordingStopped(FlightRecording recording)
        {
            // Auto-save is handled inside FlightRecorderManager; nothing extra needed here.
        }

        private void CheckQuota()
        {
            long used = GetUsedBytes();
            if (used >= QuotaWarnBytes) OnStorageWarning?.Invoke(used);
        }

        private long EstimateBytes(FlightRecording recording)
        {
            // Rough estimate: ~120 bytes per frame.
            return (recording.frames?.Count ?? 0) * 120L + 512L;
        }

        private void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        private void LoadIndex()
        {
            string path = Path.Combine(_rootPath, IndexFileName);
            if (!File.Exists(path)) return;
            try
            {
                string json = File.ReadAllText(path);
                _index = JsonUtility.FromJson<RecordingIndex>(json) ?? new RecordingIndex();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] RecordingStorageManager: Could not load index — {ex.Message}");
                _index = new RecordingIndex();
            }
        }

        private void SaveIndex()
        {
            string path = Path.Combine(_rootPath, IndexFileName);
            try { File.WriteAllText(path, JsonUtility.ToJson(_index, prettyPrint: true)); }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] RecordingStorageManager: Could not save index — {ex.Message}");
            }
        }

        #endregion
    }

    // ── Supporting types ──────────────────────────────────────────────────────────

    /// <summary>Lightweight metadata entry stored in the recording index.</summary>
    [System.Serializable]
    public class RecordingMeta
    {
        public string recordingId;
        public string pilotName;
        public string aircraftType;
        public string date;
        public float  duration;
        public string routeName;
        public float  maxAltitude;
        public float  maxSpeed;
        public float  totalDistanceKm;
        public string fileName;

        public RecordingMeta() { }

        public RecordingMeta(FlightRecording r, string fileName)
        {
            recordingId      = r.recordingId;
            pilotName        = r.pilotName;
            aircraftType     = r.aircraftType;
            date             = r.date;
            duration         = r.duration;
            routeName        = r.routeName;
            maxAltitude      = r.maxAltitude;
            maxSpeed         = r.maxSpeed;
            totalDistanceKm  = r.totalDistanceKm;
            this.fileName    = fileName;
        }
    }

    /// <summary>Serialisable index of all saved recording metadata entries.</summary>
    [System.Serializable]
    public class RecordingIndex
    {
        public List<RecordingMeta> Entries = new List<RecordingMeta>();

        public RecordingMeta Find(string id)
        {
            foreach (var e in Entries)
                if (e.recordingId == id) return e;
            return null;
        }

        public void AddOrUpdate(RecordingMeta meta)
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].recordingId == meta.recordingId)
                {
                    Entries[i] = meta;
                    return;
                }
            }
            Entries.Add(meta);
        }

        public void Remove(string id)
        {
            Entries.RemoveAll(e => e.recordingId == id);
        }
    }
}
