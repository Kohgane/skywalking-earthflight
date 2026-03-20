using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using SWEF.Replay;

namespace SWEF.ReplayTheater
{
    /// <summary>
    /// Exports Replay Theater cinematics in multiple formats:
    /// PNG frame sequences, camera path JSON, and bundled <c>.swef-replay</c> packages.
    /// </summary>
    public class ReplayExporter : MonoBehaviour
    {
        #region Inspector

        [Header("Settings")]
        [SerializeField] private ReplayTheaterSettings settings;

        [Header("Scene")]
        [SerializeField] private Camera captureCamera;

        #endregion

        #region Events

        /// <summary>Fired when an export operation completes.  Parameter is the output path.</summary>
        public event Action<string> OnExportCompleted;

        /// <summary>Fired when an export operation fails.  Parameter is an error message.</summary>
        public event Action<string> OnExportFailed;

        /// <summary>Fired every frame during a video export.  Parameter is 0–1 progress.</summary>
        public event Action<float>  OnExportProgress;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (settings == null)
                settings = Resources.Load<ReplayTheaterSettings>("ReplayTheaterSettings");
            if (captureCamera == null)
                captureCamera = Camera.main;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Exports the replay as a PNG frame sequence to <paramref name="outputPath"/>.
        /// Each frame is rendered from <see cref="captureCamera"/> at the given resolution and FPS.
        /// </summary>
        /// <param name="outputPath">Destination directory.</param>
        /// <param name="width">Frame width in pixels.</param>
        /// <param name="height">Frame height in pixels.</param>
        /// <param name="fps">Frames per second.</param>
        /// <param name="timeline">Timeline to scrub during capture.</param>
        public void ExportVideo(string outputPath, int width, int height, int fps, ReplayTimeline timeline = null)
        {
            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = Path.Combine(settings?.ExportOutputPath ?? Application.persistentDataPath,
                                          $"Export_{DateTime.Now:yyyyMMdd_HHmmss}");
            }
            StartCoroutine(ExportVideoCoroutine(outputPath, width, height, fps, timeline));
        }

        /// <summary>
        /// Exports the current camera keyframe path as JSON to <paramref name="outputPath"/>.
        /// </summary>
        /// <param name="outputPath">Destination file path (.json).</param>
        /// <param name="editor">The camera editor holding the keyframes.</param>
        public void ExportCameraPath(string outputPath, CinematicCameraEditor editor)
        {
            if (editor == null)
            {
                OnExportFailed?.Invoke("No camera editor provided.");
                return;
            }
            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = Path.Combine(settings?.ExportOutputPath ?? Application.persistentDataPath,
                                          $"CameraPath_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            }

            try
            {
                EnsureDirectory(Path.GetDirectoryName(outputPath));

                var wrapper = new CameraPathExportData();
                wrapper.keyframes = new List<CameraKeyframe>(editor.Keyframes);
                string json = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(outputPath, json, Encoding.UTF8);

                Debug.Log($"[SWEF] ReplayExporter: Camera path exported → {outputPath}");
                OnExportCompleted?.Invoke(outputPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] ReplayExporter: Camera path export failed — {ex.Message}");
                OnExportFailed?.Invoke(ex.Message);
            }
        }

        /// <summary>
        /// Bundles <paramref name="data"/> and the current camera path into a single
        /// <c>.swef-replay</c> JSON package at <paramref name="outputPath"/>.
        /// </summary>
        /// <param name="outputPath">Destination file path (.swef-replay).</param>
        /// <param name="data">Replay data to bundle.</param>
        /// <param name="editor">Camera editor (may be null).</param>
        public void ExportReplayPackage(string outputPath, ReplayData data, CinematicCameraEditor editor = null)
        {
            if (data == null)
            {
                OnExportFailed?.Invoke("No replay data to export.");
                return;
            }
            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = Path.Combine(settings?.ExportOutputPath ?? Application.persistentDataPath,
                                          $"Replay_{data.replayId}_{DateTime.Now:yyyyMMdd_HHmmss}.swef-replay");
            }

            try
            {
                EnsureDirectory(Path.GetDirectoryName(outputPath));

                var pkg = new ReplayPackage
                {
                    schemaVersion = 1,
                    replayData    = data,
                };

                if (editor != null)
                {
                    var camPath = new CameraPathExportData();
                    camPath.keyframes = new List<CameraKeyframe>(editor.Keyframes);
                    pkg.cameraPath = camPath;
                }

                string json = JsonUtility.ToJson(pkg, true);
                File.WriteAllText(outputPath, json, Encoding.UTF8);

                Debug.Log($"[SWEF] ReplayExporter: Package exported → {outputPath}");
                OnExportCompleted?.Invoke(outputPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] ReplayExporter: Package export failed — {ex.Message}");
                OnExportFailed?.Invoke(ex.Message);
            }
        }

        /// <summary>
        /// Captures a screenshot of the current frame and saves it as PNG.
        /// </summary>
        /// <param name="outputPath">Destination file path (.png).</param>
        /// <param name="width">Capture width.</param>
        /// <param name="height">Capture height.</param>
        /// <returns>The captured <see cref="Texture2D"/>, or <c>null</c> on failure.</returns>
        public Texture2D CaptureScreenshot(string outputPath, int width = 0, int height = 0)
        {
            if (width  <= 0) width  = settings?.DefaultExportWidth  ?? Screen.width;
            if (height <= 0) height = settings?.DefaultExportHeight ?? Screen.height;

            try
            {
                var rt  = new RenderTexture(width, height, 24);
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);

                var cam = captureCamera != null ? captureCamera : Camera.main;
                if (cam == null) return null;

                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                cam.targetTexture = null;
                RenderTexture.active = null;
                Destroy(rt);

                if (!string.IsNullOrEmpty(outputPath))
                {
                    EnsureDirectory(Path.GetDirectoryName(outputPath));
                    File.WriteAllBytes(outputPath, tex.EncodeToPNG());
                    Debug.Log($"[SWEF] ReplayExporter: Screenshot saved → {outputPath}");
                }

                return tex;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] ReplayExporter: Screenshot failed — {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Internals

        private IEnumerator ExportVideoCoroutine(string outputDir, int width, int height, int fps,
                                                 ReplayTimeline timeline)
        {
            EnsureDirectory(outputDir);

            float duration    = timeline?.TotalDuration ?? 0f;
            if (duration <= 0f)
            {
                Debug.LogWarning("[SWEF] ReplayExporter: Replay duration is zero — cannot export video.");
                OnExportFailed?.Invoke("Replay duration is zero.");
                yield break;
            }
            int   totalFrames = Mathf.Max(1, Mathf.CeilToInt(duration * fps));
            var   rt        = new RenderTexture(width, height, 24);
            var   cam       = captureCamera != null ? captureCamera : Camera.main;

            if (cam == null)
            {
                Debug.LogError("[SWEF] ReplayExporter: No capture camera found.");
                OnExportFailed?.Invoke("No capture camera.");
                yield break;
            }

            Debug.Log($"[SWEF] ReplayExporter: Starting video export — {totalFrames} frames → {outputDir}");

            for (int frame = 0; frame < totalFrames; frame++)
            {
                float frameTime = (float)frame / fps;
                timeline?.SeekTo(frameTime);

                yield return new WaitForEndOfFrame();

                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;

                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                cam.targetTexture = null;
                RenderTexture.active = null;

                string filePath = Path.Combine(outputDir, $"frame_{frame:D6}.png");
                File.WriteAllBytes(filePath, tex.EncodeToPNG());
                Destroy(tex);

                OnExportProgress?.Invoke((float)frame / totalFrames);
            }

            Destroy(rt);
            Debug.Log($"[SWEF] ReplayExporter: Video export complete → {outputDir}");
            OnExportCompleted?.Invoke(outputDir);
        }

        private static void EnsureDirectory(string dir)
        {
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        #endregion

        #region Serialisation Helpers

        [Serializable]
        private class CameraPathExportData
        {
            public List<CameraKeyframe> keyframes;
        }

        [Serializable]
        private class ReplayPackage
        {
            public int                  schemaVersion;
            public ReplayData           replayData;
            public CameraPathExportData cameraPath;
        }

        #endregion
    }
}
