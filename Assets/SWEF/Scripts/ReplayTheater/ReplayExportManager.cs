using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ReplayTheater
{
    /// <summary>
    /// Singleton MonoBehaviour that manages the export pipeline for
    /// <see cref="ReplayProject"/> instances.  Supports direct export,
    /// a bounded export queue, share-code generation, and frame-sequence output.
    /// </summary>
    public class ReplayExportManager : MonoBehaviour
    {
        #region Singleton

        private static ReplayExportManager _instance;

        /// <summary>Global singleton instance.</summary>
        public static ReplayExportManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<ReplayExportManager>();
                return _instance;
            }
        }

        #endregion

        #region Inspector

        [Header("Export Settings")]
        [SerializeField] private string defaultExportPath = "Replays/Export";

        [Header("Queue Settings")]
        [SerializeField] private int maxQueueSize = 5;

        #endregion

        #region State

        private Queue<(ReplayProject project, ExportSettings settings)> _exportQueue
            = new Queue<(ReplayProject, ExportSettings)>();

        private bool      _isExporting;
        private bool      _cancelRequested;
        private Coroutine _exportCoroutine;

        private const string ShareCodeChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int    ShareCodeLength = 8;

        #endregion

        #region Events

        /// <summary>Fired periodically during an export.  Parameter is progress in [0, 1].</summary>
        public event Action<float> OnExportProgress;

        /// <summary>Fired when an export completes successfully.  Parameter is the output file path.</summary>
        public event Action<string> OnExportCompleted;

        /// <summary>Fired when an export fails.  Parameter is the error message.</summary>
        public event Action<string> OnExportFailed;

        /// <summary>Fired when the queue size changes.  Parameter is the new count.</summary>
        public event Action<int> OnQueueCountChanged;

        #endregion

        #region Properties

        /// <summary>Number of exports currently waiting in the queue.</summary>
        public int QueueCount => _exportQueue.Count;

        /// <summary>Whether an export is currently in progress.</summary>
        public bool IsExporting => _isExporting;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Immediately begins exporting <paramref name="project"/> using <paramref name="settings"/>.
        /// If an export is already running the request is dropped; use <see cref="QueueExport"/> instead.
        /// </summary>
        /// <param name="project">Project to export.</param>
        /// <param name="settings">Export parameters.</param>
        public void ExportProject(ReplayProject project, ExportSettings settings)
        {
            if (project == null || settings == null) return;

            if (_isExporting)
            {
                Debug.LogWarning("[SWEF] ReplayExportManager: Export already in progress.");
                return;
            }

            _cancelRequested = false;
            _exportCoroutine = StartCoroutine(ExportCoroutine(project, settings));
        }

        /// <summary>
        /// Adds the project/settings pair to the export queue.
        /// The queue is processed automatically, one export at a time.
        /// </summary>
        /// <param name="project">Project to export.</param>
        /// <param name="settings">Export parameters.</param>
        public void QueueExport(ReplayProject project, ExportSettings settings)
        {
            if (project == null || settings == null) return;

            if (_exportQueue.Count >= maxQueueSize)
            {
                Debug.LogWarning("[SWEF] ReplayExportManager: Export queue is full.");
                return;
            }

            _exportQueue.Enqueue((project, settings));
            Debug.Log($"[SWEF] ReplayExportManager: Queued export for '{project.title}'. Queue size: {_exportQueue.Count}.");
            OnQueueCountChanged?.Invoke(_exportQueue.Count);

            if (!_isExporting)
                StartCoroutine(ProcessQueueCoroutine());
        }

        /// <summary>
        /// Generates a short, unique share code for a project.
        /// </summary>
        /// <param name="project">The project to generate a code for.</param>
        /// <returns>An alphanumeric share code.</returns>
        public string GenerateShareCode(ReplayProject project)
        {
            var rng  = new System.Random(project?.projectId?.GetHashCode() ?? Environment.TickCount);
            var code = new System.Text.StringBuilder(ShareCodeLength);

            for (int i = 0; i < ShareCodeLength; i++)
                code.Append(ShareCodeChars[rng.Next(ShareCodeChars.Length)]);

            string result = code.ToString();
            Debug.Log($"[SWEF] ReplayExportManager: Share code '{result}' generated for '{project?.title}'.");
            return result;
        }

        /// <summary>
        /// Exports individual frames of the project as a sequence to <paramref name="outputDir"/>.
        /// </summary>
        /// <param name="project">Project to export.</param>
        /// <param name="outputDir">Directory path for the frame images.</param>
        /// <param name="progress">Optional progress callback in [0, 1].</param>
        public void ExportFrameSequence(ReplayProject project, string outputDir, Action<float> progress)
        {
            if (project == null) return;
            StartCoroutine(FrameSequenceCoroutine(project, outputDir, progress));
        }

        /// <summary>Requests cancellation of the currently running export.</summary>
        public void CancelExport()
        {
            if (!_isExporting) return;
            _cancelRequested = true;
            Debug.Log("[SWEF] ReplayExportManager: Cancel requested.");
        }

        #endregion

        #region Internals

        private IEnumerator ExportCoroutine(ReplayProject project, ExportSettings settings)
        {
            _isExporting = true;
            Debug.Log($"[SWEF] ReplayExportManager: Exporting '{project.title}' as {settings.format}…");

            string outputPath = string.IsNullOrEmpty(settings.outputPath)
                ? $"{defaultExportPath}/{project.projectId}.swefr"
                : settings.outputPath;

            // Simulate export with progress ticks
            int   steps   = 20;
            float stepTime = Mathf.Max(0.05f, project.totalDuration / steps);

            for (int i = 0; i <= steps; i++)
            {
                if (_cancelRequested)
                {
                    _isExporting     = false;
                    _cancelRequested = false;
                    Debug.Log("[SWEF] ReplayExportManager: Export cancelled.");
                    OnExportFailed?.Invoke("Export cancelled by user.");
                    yield break;
                }

                float t = (float)i / steps;
                OnExportProgress?.Invoke(t);
                yield return new WaitForSecondsRealtime(stepTime);
            }

            _isExporting = false;
            Debug.Log($"[SWEF] ReplayExportManager: Export complete → '{outputPath}'.");
            OnExportCompleted?.Invoke(outputPath);
        }

        private IEnumerator ProcessQueueCoroutine()
        {
            while (_exportQueue.Count > 0)
            {
                var (project, settings) = _exportQueue.Dequeue();
                OnQueueCountChanged?.Invoke(_exportQueue.Count);
                yield return ExportCoroutine(project, settings);
            }
        }

        private IEnumerator FrameSequenceCoroutine(ReplayProject project, string outputDir, Action<float> progress)
        {
            Debug.Log($"[SWEF] ReplayExportManager: Frame sequence export to '{outputDir}' started.");
            int totalFrames = Mathf.Max(1, Mathf.RoundToInt(project.totalDuration * 30f));

            for (int f = 0; f < totalFrames; f++)
            {
                float t = (float)f / totalFrames;
                progress?.Invoke(t);
                // Frame capture hook — integrate RenderTexture readback here
                yield return null;
            }

            progress?.Invoke(1f);
            Debug.Log("[SWEF] ReplayExportManager: Frame sequence export complete.");
        }

        #endregion
    }
}
