using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.Core
{
    // ── Serialisable data classes ─────────────────────────────────────────────

    /// <summary>Generic key-value pair stored in the save file.</summary>
    [Serializable]
    public class KeyValueEntry
    {
        public string key;
        public string value;
    }

    /// <summary>One favourited location.</summary>
    [Serializable]
    public class FavoriteEntry
    {
        public string id;
        public string name;
        public double latitude;
        public double longitude;
        public double altitude;
        public string savedAt;
    }

    /// <summary>One recorded flight-journal entry.</summary>
    [Serializable]
    public class JournalEntry
    {
        public string id;
        public string startLocation;
        public double startLatitude;
        public double startLongitude;
        public float  maxAltitudeKm;
        public float  durationSec;
        public float  distanceKm;
        public string notes;
        public string recordedAt;
    }

    /// <summary>Root object serialised to disk.</summary>
    [Serializable]
    public class SaveData
    {
        public int    saveVersion        = 1;
        public string lastSavedAt;

        // generic key-value store
        public List<KeyValueEntry> keyValues = new List<KeyValueEntry>();

        // favourites
        public List<FavoriteEntry> favorites = new List<FavoriteEntry>();

        // flight journal
        public List<JournalEntry> journal = new List<JournalEntry>();

        // cumulative stats
        public int   totalFlights;
        public float totalFlightTimeSec;
        public float allTimeMaxAltitudeKm;
        public float totalDistanceKm;
    }

    // ── SaveManager ───────────────────────────────────────────────────────────

    /// <summary>
    /// Centralised JSON-based save/load system.
    /// Singleton with DontDestroyOnLoad.
    /// All game data is stored in a single JSON file at
    /// <see cref="Application.persistentDataPath"/>/swef_save.json.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        public static SaveManager Instance { get; private set; }

        // ── Events ───────────────────────────────────────────────────────────
        /// <summary>Raised when a save operation completes successfully.</summary>
        public event Action OnSaveCompleted;

        /// <summary>Raised when a load operation completes successfully.</summary>
        public event Action OnLoadCompleted;

        /// <summary>Raised when an error occurs during save/load.</summary>
        public event Action<string> OnSaveError;

        // ── Config ───────────────────────────────────────────────────────────
        [Header("Config")]
        [SerializeField] private string saveFileName = "swef_save.json";

        // ── Public state ─────────────────────────────────────────────────────
        /// <summary>In-memory copy of the current save data.</summary>
        public SaveData Data { get; private set; } = new SaveData();

        // ── Internal ─────────────────────────────────────────────────────────
        private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Load();
        }

        // ── File helpers ─────────────────────────────────────────────────────

        /// <summary>Returns <c>true</c> if a save file exists on disk.</summary>
        public bool HasSaveFile() => File.Exists(SavePath);

        /// <summary>Serialises <see cref="Data"/> to disk as JSON.</summary>
        public void Save()
        {
            try
            {
                Data.lastSavedAt = DateTime.UtcNow.ToString("o");
                string json = JsonUtility.ToJson(Data, prettyPrint: true);
                File.WriteAllText(SavePath, json);
                Debug.Log($"[SWEF] SaveManager: saved to {SavePath}");
                OnSaveCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] SaveManager: save failed — {ex.Message}");
                OnSaveError?.Invoke(ex.Message);
            }
        }

        /// <summary>Deserialises the save file from disk into <see cref="Data"/>.</summary>
        public void Load()
        {
            try
            {
                if (!HasSaveFile())
                {
                    Data = new SaveData();
                    Debug.Log("[SWEF] SaveManager: no save file found, using defaults.");
                    OnLoadCompleted?.Invoke();
                    return;
                }

                string json = File.ReadAllText(SavePath);
                Data = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
                Debug.Log($"[SWEF] SaveManager: loaded from {SavePath}");
                OnLoadCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] SaveManager: load failed — {ex.Message}");
                Data = new SaveData();
                OnSaveError?.Invoke(ex.Message);
            }
        }

        /// <summary>Deletes the save file from disk and resets in-memory data.</summary>
        public void DeleteSave()
        {
            try
            {
                if (HasSaveFile())
                    File.Delete(SavePath);
                Data = new SaveData();
                Debug.Log("[SWEF] SaveManager: save file deleted.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] SaveManager: delete failed — {ex.Message}");
                OnSaveError?.Invoke(ex.Message);
            }
        }

        // ── Key-value helpers ────────────────────────────────────────────────

        /// <summary>Gets a string value by key; returns <paramref name="defaultValue"/> when not found.</summary>
        public string GetString(string key, string defaultValue = "")
        {
            var entry = Data.keyValues.Find(e => e.key == key);
            return entry != null ? entry.value : defaultValue;
        }

        /// <summary>Stores a string value for <paramref name="key"/>.</summary>
        public void SetString(string key, string value)
        {
            var entry = Data.keyValues.Find(e => e.key == key);
            if (entry != null)
                entry.value = value;
            else
                Data.keyValues.Add(new KeyValueEntry { key = key, value = value });
        }

        /// <summary>Gets an integer value by key; returns <paramref name="defaultValue"/> when not found.</summary>
        public int GetInt(string key, int defaultValue = 0)
        {
            string raw = GetString(key);
            return string.IsNullOrEmpty(raw) ? defaultValue : (int.TryParse(raw, out int v) ? v : defaultValue);
        }

        /// <summary>Stores an integer value for <paramref name="key"/>.</summary>
        public void SetInt(string key, int value) => SetString(key, value.ToString());

        /// <summary>Gets a float value by key; returns <paramref name="defaultValue"/> when not found.</summary>
        public float GetFloat(string key, float defaultValue = 0f)
        {
            string raw = GetString(key);
            return string.IsNullOrEmpty(raw) ? defaultValue : (float.TryParse(raw, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : defaultValue);
        }

        /// <summary>Stores a float value for <paramref name="key"/>.</summary>
        public void SetFloat(string key, float value) =>
            SetString(key, value.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
    }
}
