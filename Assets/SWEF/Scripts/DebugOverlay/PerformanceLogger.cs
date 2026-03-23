// PerformanceLogger.cs — SWEF Performance Profiler & Debug Overlay
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace SWEF.DebugOverlay
{
    /// <summary>
    /// MonoBehaviour that periodically writes performance data to a CSV log file.
    /// Supports configurable log intervals, automatic file rotation, and session
    /// summary generation on application quit.
    /// </summary>
    public class PerformanceLogger : MonoBehaviour
    {
        #region Inspector Fields

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        [Header("Logger Configuration")]
        [Tooltip("Reference to the DebugOverlayController (auto-found if null).")]
        [SerializeField] private DebugOverlayController overlayController;

        [Tooltip("How often (seconds) a performance row is written to the log.")]
        [SerializeField] private float logInterval = 1f;

        [Tooltip("Maximum log file size in kilobytes before rotation.")]
        [SerializeField] private int maxFileSizeKB = 10240; // 10 MB

        [Tooltip("Start logging automatically on Awake.")]
        [SerializeField] private bool autoStart = true;

        [Tooltip("Default log file path (relative to Application.persistentDataPath).")]
        [SerializeField] private string defaultFileName = "SWEF_PerformanceLog.csv";
#endif

        #endregion

        #region Private State

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private bool   _isLogging;
        private string _logFilePath;
        private float  _logTimer;

        private DebugOverlayController _controller;

        // Session accumulators for summary
        private long  _frameCount;
        private float _fpsSumSession;
        private float _minFpsSession = float.MaxValue;
        private float _maxFpsSession = float.MinValue;
        private float _peakMemoryMB;
        private DateTime _sessionStart;

        private const string CsvHeader = "Timestamp,FPS,AvgFPS,FrameTimeMs,AllocMemMB,TotalUsedMB,DrawCalls,Triangles";
#endif

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (_controller == null) _controller = overlayController != null
                ? overlayController
                : FindFirstObjectByType<DebugOverlayController>();
            _sessionStart = DateTime.UtcNow;

            if (autoStart)
            {
                string path = Path.Combine(Application.persistentDataPath, defaultFileName);
                StartLogging(path);
            }
#endif
        }

        private void Update()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (!_isLogging) return;

            _logTimer += Time.unscaledDeltaTime;
            if (_logTimer >= logInterval)
            {
                _logTimer = 0f;
                WriteRow();
            }
#endif
        }

        private void OnApplicationQuit()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (_isLogging)
            {
                StopLogging();
                string summaryPath = Path.Combine(
                    Application.persistentDataPath, "SWEF_SessionSummary.txt");
                ExportSessionSummary(summaryPath);
            }
#endif
        }

        #endregion

        #region Public API

        /// <summary>Begins logging performance data to the specified file path.</summary>
        /// <param name="filePath">Absolute path for the CSV log file.</param>
        public void StartLogging(string filePath)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (string.IsNullOrEmpty(filePath)) return;
            _logFilePath = filePath;
            _isLogging   = true;
            _logTimer    = 0f;

            // Write header if file does not exist
            if (!File.Exists(_logFilePath))
                File.WriteAllText(_logFilePath, CsvHeader + Environment.NewLine);
#endif
        }

        /// <summary>Stops the active logging session.</summary>
        public void StopLogging()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _isLogging = false;
#endif
        }

        /// <summary>
        /// Writes a plain-text session summary to <paramref name="filePath"/>.
        /// </summary>
        /// <param name="filePath">Absolute path for the summary file.</param>
        public void ExportSessionSummary(string filePath)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (string.IsNullOrEmpty(filePath)) return;
            TimeSpan duration = DateTime.UtcNow - _sessionStart;
            float avgFps = _frameCount > 0 ? _fpsSumSession / _frameCount : 0f;

            var sb = new StringBuilder();
            sb.AppendLine("=== SWEF Performance Session Summary ===");
            sb.AppendLine($"Session Start : {_sessionStart:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"Duration      : {duration:hh\\:mm\\:ss}");
            sb.AppendLine($"Total Frames  : {_frameCount}");
            sb.AppendLine($"Avg FPS       : {avgFps:F1}");
            sb.AppendLine($"Min FPS       : {(_minFpsSession == float.MaxValue ? 0 : _minFpsSession):F1}");
            sb.AppendLine($"Max FPS       : {(_maxFpsSession == float.MinValue ? 0 : _maxFpsSession):F1}");
            sb.AppendLine($"Peak Mem (MB) : {_peakMemoryMB:F1}");

            try { File.WriteAllText(filePath, sb.ToString()); }
            catch (Exception ex) { Debug.LogWarning($"[PerformanceLogger] Could not write summary: {ex.Message}"); }
#endif
        }

        #endregion

        #region Private Helpers

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private void WriteRow()
        {
            if (_controller == null) return;

            // Rotate file if too large
            try
            {
                var fi = new FileInfo(_logFilePath);
                if (fi.Exists && fi.Length / 1024 >= maxFileSizeKB)
                    RotateLogFile();
            }
            catch { /* IO errors should not crash the game */ }

            DebugOverlaySnapshot snap = _controller.GetFullSnapshot();

            // Session accumulators
            _frameCount++;
            _fpsSumSession += snap.currentFPS;
            if (snap.currentFPS > 0f && snap.currentFPS < _minFpsSession) _minFpsSession = snap.currentFPS;
            if (snap.currentFPS > _maxFpsSession) _maxFpsSession = snap.currentFPS;
            if (snap.memory.allocatedManagedMB > _peakMemoryMB) _peakMemoryMB = snap.memory.allocatedManagedMB;

            string row = string.Format("{0},{1:F1},{2:F1},{3:F2},{4:F1},{5:F1},{6},{7}",
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                snap.currentFPS,
                snap.averageFPS,
                snap.frameTimeMs,
                snap.memory.allocatedManagedMB,
                snap.memory.totalUsedMB,
                snap.rendering.drawCalls,
                snap.rendering.triangles);

            try { File.AppendAllText(_logFilePath, row + Environment.NewLine); }
            catch (Exception ex) { Debug.LogWarning($"[PerformanceLogger] Write failed: {ex.Message}"); }
        }

        private void RotateLogFile()
        {
            string dir  = Path.GetDirectoryName(_logFilePath) ?? Application.persistentDataPath;
            string name = Path.GetFileNameWithoutExtension(_logFilePath);
            string ext  = Path.GetExtension(_logFilePath);
            string rotated = Path.Combine(dir,
                $"{name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{ext}");
            try
            {
                File.Move(_logFilePath, rotated);
                File.WriteAllText(_logFilePath, CsvHeader + Environment.NewLine);
            }
            catch (Exception ex) { Debug.LogWarning($"[PerformanceLogger] Rotate failed: {ex.Message}"); }
        }
#endif

        #endregion
    }
}
