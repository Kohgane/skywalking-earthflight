using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace SWEF.Replay
{
    /// <summary>
    /// Phase 48 — Handles exporting and importing <see cref="FlightRecording"/>
    /// objects in a shareable compressed-JSON format and generating human-readable
    /// share codes.
    /// </summary>
    public class RecordingSharingManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static RecordingSharingManager Instance { get; private set; }

        #endregion

        #region Constants

        private const int    ShareCodeLength  = 8;
        private const string ShareCodeChars   = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private const int    CurrentVersion   = 1;
        private const string ExportExtension  = ".swefr";
        private const long   MaxImportBytes   = 10L * 1024 * 1024; // 10 MB

        #endregion

        #region Events

        /// <summary>Fired after a recording is successfully exported.</summary>
        public event Action<FlightRecording, string> OnRecordingExported;

        /// <summary>Fired after a recording is successfully imported.</summary>
        public event Action<FlightRecording> OnRecordingImported;

        /// <summary>Fired when a share code is generated; carries the code string.</summary>
        public event Action<string> OnShareCodeGenerated;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region Public API — Export

        /// <summary>
        /// Exports <paramref name="recording"/> to a <c>.swefr</c> file in
        /// <see cref="Application.persistentDataPath"/>/Exports/ and fires
        /// <see cref="OnRecordingExported"/> with the resulting file path.
        /// </summary>
        /// <returns>The absolute export path, or <c>null</c> on failure.</returns>
        public string ExportRecording(FlightRecording recording)
        {
            if (recording == null) return null;

            string folder = Path.Combine(Application.persistentDataPath, "Exports");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string fileName = $"SWEF_{recording.aircraftType}_{DateTime.UtcNow:yyyyMMddHHmmss}{ExportExtension}";
            string path     = Path.Combine(folder, fileName);

            var payload = new SharePayload
            {
                version   = CurrentVersion,
                recording = recording
            };

            try
            {
                string json      = JsonUtility.ToJson(payload, prettyPrint: false);
                byte[] bytes     = Encoding.UTF8.GetBytes(json);
                byte[] compressed = CompressBytes(bytes);
                File.WriteAllBytes(path, compressed);
                OnRecordingExported?.Invoke(recording, path);
                Debug.Log($"[SWEF] RecordingSharingManager: Exported to '{path}'.");
                return path;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] RecordingSharingManager: Export failed — {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Returns an estimated export size in bytes for <paramref name="recording"/>
        /// without writing any file.
        /// </summary>
        public long EstimateExportBytes(FlightRecording recording)
        {
            if (recording == null) return 0L;
            string json = JsonUtility.ToJson(recording, prettyPrint: false);
            // Compressed output is typically ~40% of raw JSON for numeric data.
            return (long)(Encoding.UTF8.GetByteCount(json) * 0.4f);
        }

        #endregion

        #region Public API — Import

        /// <summary>
        /// Imports a <see cref="FlightRecording"/> from the <c>.swefr</c> file
        /// at <paramref name="filePath"/>.
        /// Validates the version field and data integrity before accepting.
        /// </summary>
        /// <returns>The imported recording, or <c>null</c> on failure.</returns>
        public FlightRecording ImportRecording(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[SWEF] RecordingSharingManager: File not found — '{filePath}'.");
                return null;
            }

            FileInfo fi = new FileInfo(filePath);
            if (fi.Length > MaxImportBytes)
            {
                Debug.LogWarning($"[SWEF] RecordingSharingManager: File exceeds size limit ({fi.Length} bytes).");
                return null;
            }

            try
            {
                byte[]  compressed = File.ReadAllBytes(filePath);
                byte[]  bytes      = DecompressBytes(compressed);
                string  json       = Encoding.UTF8.GetString(bytes);
                var     payload    = JsonUtility.FromJson<SharePayload>(json);

                if (payload == null || payload.recording == null)
                {
                    Debug.LogWarning("[SWEF] RecordingSharingManager: Null payload after deserialization.");
                    return null;
                }

                if (payload.version > CurrentVersion)
                {
                    Debug.LogWarning($"[SWEF] RecordingSharingManager: Unsupported version {payload.version}.");
                    return null;
                }

                if (!ValidateRecording(payload.recording))
                {
                    Debug.LogWarning("[SWEF] RecordingSharingManager: Validation failed.");
                    return null;
                }

                OnRecordingImported?.Invoke(payload.recording);
                Debug.Log($"[SWEF] RecordingSharingManager: Imported '{payload.recording.pilotName}'.");
                return payload.recording;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] RecordingSharingManager: Import failed — {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Returns a <see cref="RecordingMeta"/> preview of the recording stored at
        /// <paramref name="filePath"/> without fully deserialising frame data.
        /// </summary>
        public RecordingMeta PreviewImport(string filePath)
        {
            var recording = ImportRecording(filePath);
            if (recording == null) return null;
            return new RecordingMeta(recording, Path.GetFileName(filePath));
        }

        #endregion

        #region Public API — Share Code

        /// <summary>
        /// Generates a short unique share code for <paramref name="recording"/> and
        /// copies it to the system clipboard.
        /// </summary>
        public string GenerateShareCode(FlightRecording recording)
        {
            if (recording == null) return string.Empty;

            // Build a deterministic seed from recording metadata.
            int seed  = (recording.recordingId + recording.date).GetHashCode();
            var rng   = new System.Random(seed);
            var sb    = new StringBuilder(ShareCodeLength);
            for (int i = 0; i < ShareCodeLength; i++)
                sb.Append(ShareCodeChars[rng.Next(ShareCodeChars.Length)]);

            string code = sb.ToString();
            GUIUtility.systemCopyBuffer = code;
            OnShareCodeGenerated?.Invoke(code);
            Debug.Log($"[SWEF] RecordingSharingManager: Share code '{code}' copied to clipboard.");
            return code;
        }

        #endregion

        #region Private — Compression (Base-64 + GZip substitute using zlib via .NET DeflateStream)

        private static byte[] CompressBytes(byte[] data)
        {
            using var ms  = new MemoryStream();
            using var ds  = new System.IO.Compression.DeflateStream(ms,
                            System.IO.Compression.CompressionLevel.Optimal);
            ds.Write(data, 0, data.Length);
            ds.Close();
            return ms.ToArray();
        }

        private static byte[] DecompressBytes(byte[] data)
        {
            using var input  = new MemoryStream(data);
            using var ds     = new System.IO.Compression.DeflateStream(input,
                               System.IO.Compression.CompressionMode.Decompress);
            using var output = new MemoryStream();
            ds.CopyTo(output);
            return output.ToArray();
        }

        #endregion

        #region Private — Validation

        private static bool ValidateRecording(FlightRecording recording)
        {
            if (string.IsNullOrEmpty(recording.recordingId)) return false;
            if (recording.duration <= 0f)                    return false;
            if (recording.frames == null)                    return false;
            return true;
        }

        #endregion

        #region Private — Supporting Types

        [System.Serializable]
        private class SharePayload
        {
            public int             version;
            public FlightRecording recording;
        }

        #endregion
    }
}
