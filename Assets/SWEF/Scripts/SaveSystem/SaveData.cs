using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SaveSystem
{
    // ── Interface ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Implement this interface on any MonoBehaviour to participate in the
    /// Phase 35 save/load pipeline.  <see cref="SaveManager"/> auto-discovers
    /// all ISaveable components present in the scene at load time.
    /// </summary>
    public interface ISaveable
    {
        /// <summary>Unique key identifying this component's data in the save payload.</summary>
        string SaveKey { get; }

        /// <summary>Returns a JSON-serialisable snapshot of the component's current state.</summary>
        object CaptureState();

        /// <summary>
        /// Restores the component's state from a snapshot.
        /// The <paramref name="state"/> argument is the JSON string previously stored
        /// by <see cref="CaptureState"/>; call <c>JsonUtility.FromJson&lt;T&gt;(state as string)</c>
        /// to re-hydrate it.
        /// </summary>
        void RestoreState(object state);
    }

    // ── Enums ──────────────────────────────────────────────────────────────────

    /// <summary>Cloud synchronisation status for a save slot.</summary>
    public enum CloudSyncStatus
    {
        /// <summary>Cloud sync is not configured or not available.</summary>
        NotConfigured,
        /// <summary>Local and cloud data are in sync.</summary>
        Synced,
        /// <summary>Local data is newer than the last cloud upload.</summary>
        LocalAhead,
        /// <summary>Cloud data is newer than the local copy.</summary>
        CloudAhead,
        /// <summary>Both local and cloud have independent changes requiring resolution.</summary>
        Conflict,
        /// <summary>A sync operation is currently running.</summary>
        Syncing,
        /// <summary>The last sync operation failed.</summary>
        Error
    }

    // ── Save slot metadata ─────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight metadata stored in a sidecar file alongside each save slot.
    /// Persisting separately avoids full deserialization just to render the slot list.
    /// </summary>
    [Serializable]
    public class SaveSlotInfo
    {
        /// <summary>Slot index: 0–2 = manual, 3 = auto-save, 4 = quicksave.</summary>
        public int    slotIndex;

        /// <summary>User-facing display name (e.g. "Save 1" or "Auto Save").</summary>
        public string displayName;

        /// <summary>UTC ISO-8601 timestamp of the most recent write to this slot.</summary>
        public string timestamp;

        /// <summary>Total play time in seconds at the moment of saving.</summary>
        public float  playTimeSec;

        /// <summary>Relative path (under persistentDataPath) for a thumbnail screenshot.</summary>
        public string thumbnailPath;

        /// <summary>Save-format version stored in this slot.</summary>
        public int    saveVersion;

        /// <summary>SHA-256 hex checksum of the raw (possibly compressed+encrypted) save bytes.</summary>
        public string checksum;

        /// <summary>UTC ticks when this slot was first created.</summary>
        public long   creationTimestampTicks;

        /// <summary>Last known cloud sync state for this slot.</summary>
        public CloudSyncStatus cloudSyncStatus;

        /// <summary><c>true</c> when the slot has never been written to.</summary>
        public bool   isEmpty = true;
    }

    // ── File header ────────────────────────────────────────────────────────────

    /// <summary>
    /// Metadata header at the root of every save file.
    /// Written before the payload so quick-validation does not require full parse.
    /// </summary>
    [Serializable]
    public class SaveFileHeader
    {
        /// <summary>4-character magic string; always "SWEF".</summary>
        public string magic               = "SWEF";

        /// <summary>Save-format version; incremented whenever the schema changes.</summary>
        public int    formatVersion       = SaveSystemConstants.CurrentSaveVersion;

        /// <summary>UTC ticks when this slot was first created.</summary>
        public long   creationTimestamp;

        /// <summary>UTC ticks of the most recent save.</summary>
        public long   lastModifiedTimestamp;

        /// <summary>Total in-game play time in seconds at save time.</summary>
        public float  totalPlayTimeSec;

        /// <summary>Application version string at save time (e.g. "1.4.2").</summary>
        public string gameVersion;

        /// <summary>Runtime platform identifier (iOS, Android, WindowsPlayer, …).</summary>
        public string platform;
    }

    // ── Save payload ───────────────────────────────────────────────────────────

    /// <summary>
    /// Main save payload.  Each <see cref="ISaveable"/> contributes one entry:
    /// <c>key</c> = <see cref="ISaveable.SaveKey"/>,
    /// <c>value</c> = JSON-serialised state snapshot.
    /// Parallel lists are used instead of <c>Dictionary</c> for
    /// <see cref="JsonUtility"/> compatibility.
    /// </summary>
    [Serializable]
    public class SavePayload
    {
        public List<string> keys   = new List<string>();
        public List<string> values = new List<string>();

        /// <summary>Upserts a JSON value for <paramref name="key"/>.</summary>
        public void Set(string key, string json)
        {
            int idx = keys.IndexOf(key);
            if (idx >= 0)
                values[idx] = json;
            else
            {
                keys.Add(key);
                values.Add(json);
            }
        }

        /// <summary>Returns the stored JSON for <paramref name="key"/>, or <c>null</c>.</summary>
        public string Get(string key)
        {
            int idx = keys.IndexOf(key);
            return idx >= 0 ? values[idx] : null;
        }

        /// <summary>Returns <c>true</c> if <paramref name="key"/> is present.</summary>
        public bool Contains(string key) => keys.Contains(key);

        /// <summary>Number of stored key-value pairs.</summary>
        public int Count => keys.Count;
    }

    // ── Player progress snapshot ───────────────────────────────────────────────

    /// <summary>
    /// Structured snapshot of a player's progression.
    /// Stored as a top-level object in the save file for easy reporting and migration.
    /// </summary>
    [Serializable]
    public class PlayerProgressData
    {
        // ── Flight statistics ─────────────────────────────────────────────────
        public int   flightsCompleted;
        public float totalFlightTimeSec;
        public float totalDistanceKm;
        public float furthestAltitudeKm;

        // ── World exploration ─────────────────────────────────────────────────
        public List<string> unlockedRegions     = new List<string>();
        public List<string> unlockedAircraft    = new List<string>();
        public List<string> discoveredLocations = new List<string>();
        public List<string> completedMissions   = new List<string>();
        public List<string> customRoutes        = new List<string>();

        // ── Economy ───────────────────────────────────────────────────────────
        public int currencyBalance;
        public int prestigeLevel;

        // ── Last known position ───────────────────────────────────────────────
        public string lastRegion;
        public double lastLatitude;
        public double lastLongitude;
        public double lastAltitude;

        // ── Timestamps ────────────────────────────────────────────────────────
        public string firstPlayDate;
        public string lastPlayDate;
    }

    // ── Full save file ─────────────────────────────────────────────────────────

    /// <summary>
    /// Complete serialisable save file that is JSON-encoded, optionally compressed,
    /// and optionally encrypted before being written to disk.
    /// </summary>
    [Serializable]
    public class SaveFile
    {
        public SaveFileHeader     header   = new SaveFileHeader();
        public SavePayload        payload  = new SavePayload();
        public PlayerProgressData progress = new PlayerProgressData();
    }

    // ── Constants ──────────────────────────────────────────────────────────────

    /// <summary>Shared constants for the Phase 35 save system.</summary>
    public static class SaveSystemConstants
    {
        /// <summary>Current save-format version. Increment on breaking schema changes.</summary>
        public const int CurrentSaveVersion = 1;

        /// <summary>Total number of save slots (indices 0–4).</summary>
        public const int TotalSlots = 5;

        /// <summary>Number of user-accessible manual save slots (indices 0–2).</summary>
        public const int ManualSlotCount = 3;

        /// <summary>Slot index reserved for automatic saves.</summary>
        public const int AutoSaveSlot = 3;

        /// <summary>Slot index reserved for quicksaves.</summary>
        public const int QuickSaveSlot = 4;

        /// <summary>Sub-directory under <c>Application.persistentDataPath</c>.</summary>
        public const string SaveDirectory = "Saves";

        /// <summary>File extension for compressed/encrypted save blobs.</summary>
        public const string SaveExtension = ".swsave";

        /// <summary>File extension for slot-metadata sidecar files.</summary>
        public const string MetaExtension = ".swmeta";

        /// <summary>Payload key prefix for built-in subsystem data.</summary>
        public const string SubsystemKeyPrefix = "subsystem.";
    }
}
