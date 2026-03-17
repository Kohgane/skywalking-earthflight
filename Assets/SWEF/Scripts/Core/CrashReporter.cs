using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SWEF.Core
{
    /// <summary>
    /// Singleton crash reporter. Survives scene loads via DontDestroyOnLoad.
    /// Listens to Unity's log message callback and writes exception/error logs
    /// to the device's persistent storage. On startup it checks for crash logs
    /// left by the previous session and fires <see cref="OnPreviousCrashDetected"/>.
    /// </summary>
    public class CrashReporter : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static CrashReporter Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Log Retention")]
        [SerializeField] private int maxLogFiles = 20;

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>
        /// Fired during <c>Start()</c> if crash logs from a previous session exist.
        /// The string argument is the file path of the most recent log.
        /// </summary>
        public event Action<string> OnPreviousCrashDetected;

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
        }

        private void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void Start()
        {
            string[] existing = GetCrashLogPaths();
            if (existing.Length > 0)
            {
                string latest = existing.OrderByDescending(p => p).First();
                Debug.Log($"[SWEF] Previous crash detected: {latest}");
                OnPreviousCrashDetected?.Invoke(latest);
            }
        }

        // ── Public API ───────────────────────────────────────────────────────────
        /// <summary>Returns all saved crash log file paths, sorted by name descending (newest first).</summary>
        public string[] GetCrashLogPaths()
        {
            string dir = CrashLogDirectory();
            if (!Directory.Exists(dir)) return Array.Empty<string>();
            return Directory.GetFiles(dir, "crash_*.txt")
                            .OrderByDescending(p => p)
                            .ToArray();
        }

        /// <summary>Deletes all crash log files.</summary>
        public void ClearCrashLogs()
        {
            foreach (string path in GetCrashLogPaths())
            {
                try { File.Delete(path); }
                catch (Exception ex) { Debug.LogWarning($"[SWEF] CrashReporter: failed to delete {path}: {ex.Message}"); }
            }
        }

        // ── Internal ─────────────────────────────────────────────────────────────
        private void HandleLog(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error) return;

            WriteCrashLog(message, stackTrace);
        }

        private void WriteCrashLog(string message, string stackTrace)
        {
            try
            {
                string dir = CrashLogDirectory();
                Directory.CreateDirectory(dir);

                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff");
                string path      = Path.Combine(dir, $"crash_{timestamp}.txt");

                float memMB = MemoryManager.Instance != null
                    ? MemoryManager.Instance.CurrentMemoryMB
                    : -1f;

                string content = string.Join(Environment.NewLine,
                    $"Timestamp  : {DateTime.UtcNow:O}",
                    $"Device     : {SystemInfo.deviceModel}",
                    $"OS         : {SystemInfo.operatingSystem}",
                    $"Memory(MB) : {(memMB >= 0 ? memMB.ToString("F1") : "unavailable")}",
                    string.Empty,
                    "=== Message ===",
                    message,
                    string.Empty,
                    "=== Stack Trace ===",
                    stackTrace
                );

                File.WriteAllText(path, content);
                EnforceLogFileLimit(dir);
            }
            catch (Exception ex)
            {
                // Avoid infinite recursion — use raw console output
                Debug.LogWarning($"[SWEF] CrashReporter: could not write log: {ex.Message}");
            }
        }

        private void EnforceLogFileLimit(string dir)
        {
            string[] files = Directory.GetFiles(dir, "crash_*.txt")
                                      .OrderBy(p => p)
                                      .ToArray();

            while (files.Length > maxLogFiles)
            {
                try { File.Delete(files[0]); }
                catch { /* best-effort */ }
                files = files.Skip(1).ToArray();
            }
        }

        private static string CrashLogDirectory()
            => Path.Combine(Application.persistentDataPath, "CrashLogs");
    }
}
