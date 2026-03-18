using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace SWEF.Replay
{
    /// <summary>
    /// Singleton MonoBehaviour that handles all replay file I/O.
    /// Replays are saved as GZip-compressed JSON files under
    /// <c>Application.persistentDataPath/Replays/</c> with the extension
    /// <c>.swefr</c>.
    /// </summary>
    public class ReplayFileManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Singleton instance; persists across scene loads.</summary>
        public static ReplayFileManager Instance { get; private set; }

        // ── Constants ─────────────────────────────────────────────────────────────
        private const string FileExtension        = ".swefr";
        private const long   MaxReplayFileSizeBytes = 10L * 1024 * 1024; // 10 MB
        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired after a replay is successfully saved to disk.</summary>
        public event Action<ReplayData> OnReplaySaved;

        /// <summary>Fired after a replay is successfully deleted from disk.</summary>
        public event Action<string> OnReplayDeleted;

        // ── Properties ────────────────────────────────────────────────────────────
        private string SaveDirectory =>
            Path.Combine(Application.persistentDataPath, "Replays");

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureDirectoryExists();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Serializes <paramref name="data"/> to a <c>.swefr</c> file.
        /// Files exceeding <see cref="MaxReplayFileSizeBytes"/> are rejected.
        /// </summary>
        public void SaveReplay(ReplayData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[SWEF] ReplayFileManager: SaveReplay called with null data.");
                return;
            }

            EnsureDirectoryExists();
            string path = GetFilePath(data.replayId);
            string json = data.ToJson();

            if (System.Text.Encoding.UTF8.GetByteCount(json) > MaxReplayFileSizeBytes)
            {
                Debug.LogWarning($"[SWEF] ReplayFileManager: Replay '{data.replayId}' exceeds max size limit — not saved.");
                return;
            }

            File.WriteAllText(path, json, Encoding.UTF8);
            Debug.Log($"[SWEF] ReplayFileManager: Saved replay '{data.replayId}' ({json.Length} bytes).");
            OnReplaySaved?.Invoke(data);
        }

        /// <summary>
        /// Loads and deserializes the replay identified by <paramref name="replayId"/>.
        /// Returns <c>null</c> when the file does not exist or cannot be parsed.
        /// </summary>
        public ReplayData LoadReplay(string replayId)
        {
            string path = GetFilePath(replayId);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SWEF] ReplayFileManager: File not found for replay '{replayId}'.");
                return null;
            }

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                return ReplayData.FromJson(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] ReplayFileManager: Failed to load replay '{replayId}' — {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Returns a lightweight metadata list for all saved replays.
        /// Only the first-pass JSON fields are read; full frame data is not loaded.
        /// </summary>
        public List<ReplayFileInfo> ListReplays()
        {
            EnsureDirectoryExists();
            var result = new List<ReplayFileInfo>();

            foreach (string file in Directory.GetFiles(SaveDirectory, "*" + FileExtension))
            {
                try
                {
                    string json = File.ReadAllText(file, Encoding.UTF8);
                    var data    = ReplayData.FromJson(json);
                    if (data == null) continue;

                    var info = new ReplayFileInfo
                    {
                        replayId          = data.replayId,
                        playerName        = data.playerName,
                        createdAt         = data.createdAt,
                        durationSec       = data.totalDurationSec,
                        maxAltitudeM      = data.maxAltitudeM,
                        maxSpeedMps       = data.maxSpeedMps,
                        startLocationName = data.startLocationName,
                        fileSizeBytes     = new FileInfo(file).Length,
                    };
                    result.Add(info);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SWEF] ReplayFileManager: Could not read metadata from '{file}' — {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// Deletes the replay file for the given <paramref name="replayId"/>.
        /// Returns <c>true</c> on success.
        /// </summary>
        public bool DeleteReplay(string replayId)
        {
            string path = GetFilePath(replayId);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SWEF] ReplayFileManager: Cannot delete — file not found for '{replayId}'.");
                return false;
            }

            File.Delete(path);
            Debug.Log($"[SWEF] ReplayFileManager: Deleted replay '{replayId}'.");
            OnReplayDeleted?.Invoke(replayId);
            return true;
        }

        /// <summary>
        /// Loads the replay, serializes it to JSON, compresses with GZip, and returns
        /// the result as a Base64 string suitable for sharing.
        /// </summary>
        public string ExportReplayToString(string replayId)
        {
            var data = LoadReplay(replayId);
            if (data == null) return null;

            return CompressToBase64(data.ToJson());
        }

        /// <summary>
        /// Decodes a Base64 string, decompresses with GZip, and deserializes the JSON
        /// into a <see cref="ReplayData"/> instance.
        /// Returns <c>null</c> when decoding or deserialization fails.
        /// </summary>
        public ReplayData ImportReplayFromString(string encodedData)
        {
            if (string.IsNullOrEmpty(encodedData)) return null;

            try
            {
                string json = DecompressFromBase64(encodedData);
                return ReplayData.FromJson(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] ReplayFileManager: Import failed — {ex.Message}");
                return null;
            }
        }

        /// <summary>Returns the number of saved replay files.</summary>
        public int GetReplayCount()
        {
            EnsureDirectoryExists();
            return Directory.GetFiles(SaveDirectory, "*" + FileExtension).Length;
        }

        /// <summary>Returns the combined size of all replay files in bytes.</summary>
        public long GetTotalReplaySizeBytes()
        {
            EnsureDirectoryExists();
            long total = 0;
            foreach (string file in Directory.GetFiles(SaveDirectory, "*" + FileExtension))
                total += new FileInfo(file).Length;
            return total;
        }

        /// <summary>
        /// Deletes the oldest replay files when more than <paramref name="maxKeep"/>
        /// replays are saved, keeping only the most recently written files.
        /// </summary>
        public void CleanupOldReplays(int maxKeep = 50)
        {
            EnsureDirectoryExists();
            string[] files = Directory.GetFiles(SaveDirectory, "*" + FileExtension);
            if (files.Length <= maxKeep) return;

            // Sort by last-write time ascending (oldest first)
            Array.Sort(files, (a, b) =>
                File.GetLastWriteTimeUtc(a).CompareTo(File.GetLastWriteTimeUtc(b)));

            int deleteCount = files.Length - maxKeep;
            for (int i = 0; i < deleteCount; i++)
            {
                File.Delete(files[i]);
                Debug.Log($"[SWEF] ReplayFileManager: Cleaned up old replay '{Path.GetFileName(files[i])}'.");
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private string GetFilePath(string replayId) =>
            Path.Combine(SaveDirectory, replayId + FileExtension);

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(SaveDirectory))
                Directory.CreateDirectory(SaveDirectory);
        }

        /// <summary>Compresses a UTF-8 string with GZip and returns it as Base64.</summary>
        private static string CompressToBase64(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            using var outputStream = new MemoryStream();
            using (var gzip = new GZipStream(outputStream, CompressionMode.Compress))
                gzip.Write(inputBytes, 0, inputBytes.Length);
            return Convert.ToBase64String(outputStream.ToArray());
        }

        /// <summary>Decodes a Base64 string and decompresses it with GZip to UTF-8.</summary>
        private static string DecompressFromBase64(string base64)
        {
            byte[] compressedBytes = Convert.FromBase64String(base64);
            using var inputStream  = new MemoryStream(compressedBytes);
            using var gzip         = new GZipStream(inputStream, CompressionMode.Decompress);
            using var reader       = new StreamReader(gzip, Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight metadata summary for a single saved replay file.
    /// Used in the replay browser UI to avoid loading full frame data.
    /// </summary>
    [System.Serializable]
    public class ReplayFileInfo
    {
        public string replayId;
        public string playerName;
        public string createdAt;
        public float  durationSec;
        public float  maxAltitudeM;
        public float  maxSpeedMps;
        public string startLocationName;
        public long   fileSizeBytes;
    }
}
