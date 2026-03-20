using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using SWEF.Replay;

namespace SWEF.ReplayTheater
{
    /// <summary>
    /// Imports shared replays and camera paths from the filesystem.
    /// Supports <c>.swef-replay</c> package files and standalone camera-path JSON files.
    /// Validates replay data integrity before returning results.
    /// </summary>
    public class ReplayImporter : MonoBehaviour
    {
        #region Inspector

        [Header("Settings")]
        [SerializeField] private ReplayTheaterSettings settings;

        #endregion

        #region Events

        /// <summary>Fired when a package import succeeds.</summary>
        public event Action<ReplayData> OnImportSucceeded;

        /// <summary>Fired when an import fails.  Parameter is the error message.</summary>
        public event Action<string>     OnImportFailed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (settings == null)
                settings = Resources.Load<ReplayTheaterSettings>("ReplayTheaterSettings");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Loads a <c>.swef-replay</c> package from <paramref name="filePath"/>,
        /// validates it, and returns the embedded <see cref="ReplayData"/>.
        /// </summary>
        /// <param name="filePath">Absolute path to the <c>.swef-replay</c> file.</param>
        /// <returns>
        /// A valid <see cref="ReplayData"/> on success, or <c>null</c> on failure.
        /// </returns>
        public ReplayData ImportReplayPackage(string filePath)
        {
            if (!ValidateFilePath(filePath, ".swef-replay")) return null;

            try
            {
                string json = File.ReadAllText(filePath, Encoding.UTF8);
                var    pkg  = JsonUtility.FromJson<ReplayPackage>(json);

                if (pkg == null)
                {
                    Fail(filePath, "Failed to deserialise package.");
                    return null;
                }

                var data = pkg.replayData;
                if (!ValidateReplayData(data, filePath)) return null;

                // Ensure replayId is populated
                if (string.IsNullOrEmpty(data.replayId))
                    data.replayId = Guid.NewGuid().ToString();

                Debug.Log($"[SWEF] ReplayImporter: Package imported — '{data.replayId}' ({data.frames?.Count ?? 0} frames).");
                OnImportSucceeded?.Invoke(data);
                return data;
            }
            catch (Exception ex)
            {
                Fail(filePath, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Loads a camera path JSON file and returns the list of <see cref="CameraKeyframe"/> objects.
        /// </summary>
        /// <param name="filePath">Absolute path to the JSON file.</param>
        /// <returns>
        /// A list of keyframes on success, or <c>null</c> on failure.
        /// </returns>
        public List<CameraKeyframe> ImportCameraPath(string filePath)
        {
            if (!ValidateFilePath(filePath, ".json")) return null;

            try
            {
                string json = File.ReadAllText(filePath, Encoding.UTF8);
                var    data = JsonUtility.FromJson<CameraPathImportData>(json);

                if (data?.keyframes == null || data.keyframes.Count == 0)
                {
                    Fail(filePath, "No keyframes found in camera path file.");
                    return null;
                }

                // Sort by time
                data.keyframes.Sort((a, b) => a.time.CompareTo(b.time));

                Debug.Log($"[SWEF] ReplayImporter: Camera path imported — {data.keyframes.Count} keyframes.");
                return data.keyframes;
            }
            catch (Exception ex)
            {
                Fail(filePath, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Attempts to load a plain-JSON <see cref="ReplayData"/> file (legacy format).
        /// </summary>
        /// <param name="filePath">Absolute path to the JSON file.</param>
        /// <returns>A <see cref="ReplayData"/> on success, or <c>null</c>.</returns>
        public ReplayData ImportLegacyReplayJson(string filePath)
        {
            if (!ValidateFilePath(filePath, ".json")) return null;

            try
            {
                string json = File.ReadAllText(filePath, Encoding.UTF8);
                var    data = ReplayData.FromJson(json);

                if (!ValidateReplayData(data, filePath)) return null;

                Debug.Log($"[SWEF] ReplayImporter: Legacy JSON replay imported — '{data.replayId}'.");
                OnImportSucceeded?.Invoke(data);
                return data;
            }
            catch (Exception ex)
            {
                Fail(filePath, ex.Message);
                return null;
            }
        }

        #endregion

        #region Validation

        private bool ValidateFilePath(string filePath, string expectedExtension)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Fail(filePath, "File path is null or empty.");
                return false;
            }
            if (!File.Exists(filePath))
            {
                Fail(filePath, $"File not found: {filePath}");
                return false;
            }
            string ext = Path.GetExtension(filePath);
            if (!string.Equals(ext, expectedExtension, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[SWEF] ReplayImporter: Expected extension '{expectedExtension}', got '{ext}'. Attempting anyway.");
            }
            return true;
        }

        private bool ValidateReplayData(ReplayData data, string source)
        {
            if (data == null)
            {
                Fail(source, "Replay data is null after deserialisation.");
                return false;
            }
            if (data.frames == null || data.frames.Count < 2)
            {
                Fail(source, $"Replay data has insufficient frames ({data.frames?.Count ?? 0}).");
                return false;
            }
            return true;
        }

        private void Fail(string source, string reason)
        {
            string msg = $"Import failed [{source}]: {reason}";
            Debug.LogError($"[SWEF] ReplayImporter: {msg}");
            OnImportFailed?.Invoke(msg);
        }

        #endregion

        #region Serialisation Helpers

        [Serializable]
        private class CameraPathImportData
        {
            public List<CameraKeyframe> keyframes;
        }

        [Serializable]
        private class ReplayPackage
        {
            public int                  schemaVersion;
            public ReplayData           replayData;
            public CameraPathImportData cameraPath;
        }

        #endregion
    }
}
