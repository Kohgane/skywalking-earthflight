// DebugConsole.cs — SWEF Performance Profiler & Debug Overlay
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.DebugOverlay
{
    /// <summary>
    /// MonoBehaviour providing an in-game debug command console with command
    /// registration, history recall, auto-complete suggestions, and Debug.Log capture.
    /// Built-in commands: fps, memory, drawcalls, gc_collect, timescale, screenshot.
    /// </summary>
    public class DebugConsole : MonoBehaviour
    {
        #region Command Record

        /// <summary>Metadata for a registered console command.</summary>
        private struct CommandEntry
        {
            public string Name;
            public string Description;
            public Action<string[]> Handler;
        }

        #endregion

        #region Inspector Fields

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        [Header("Debug Console Configuration")]
        [Tooltip("Reference to the DebugOverlayController (auto-found if null).")]
        [SerializeField] private DebugOverlayController overlayController;

        [Tooltip("Maximum number of log lines stored in the console output buffer.")]
        [SerializeField] private int maxLogLines = 200;

        [Tooltip("Maximum number of commands kept in history.")]
        [SerializeField] private int maxHistoryEntries = 50;

        [Tooltip("Keyboard key that toggles the console window.")]
        [SerializeField] private KeyCode consoleToggleKey = KeyCode.BackQuote;
#endif

        #endregion

        #region Public Properties

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        /// <summary>Whether the console UI is currently open.</summary>
        public bool IsOpen { get; private set; }
#endif

        #endregion

        #region Private State

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private readonly Dictionary<string, CommandEntry> _commands = new Dictionary<string, CommandEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string>  _history     = new List<string>();
        private readonly List<string>  _logBuffer   = new List<string>();
        private int _historyIndex = -1;

        private DebugOverlayController _controller;
#endif

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _controller = overlayController != null
                ? overlayController
                : FindFirstObjectByType<DebugOverlayController>();
            RegisterBuiltinCommands();
            Application.logMessageReceived += OnLogReceived;
#endif
        }

        private void OnDestroy()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Application.logMessageReceived -= OnLogReceived;
#endif
        }

        private void Update()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (Input.GetKeyDown(consoleToggleKey))
                IsOpen = !IsOpen;
#endif
        }

        #endregion

        #region Public API

        /// <summary>
        /// Registers a new console command.
        /// </summary>
        /// <param name="name">Command name (case-insensitive).</param>
        /// <param name="description">Short description shown by the <c>help</c> command.</param>
        /// <param name="handler">
        /// Callback invoked with the tokenised arguments when the command is executed.
        /// </param>
        public void RegisterCommand(string name, string description, Action<string[]> handler)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(name) || handler == null) return;
            _commands[name.Trim()] = new CommandEntry
            {
                Name        = name.Trim(),
                Description = description ?? string.Empty,
                Handler     = handler
            };
#endif
        }

        /// <summary>Parses and executes a raw input string.</summary>
        /// <param name="input">Raw command line text from the user.</param>
        public void ExecuteCommand(string input)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(input)) return;

            // History
            if (_history.Count == 0 || _history[_history.Count - 1] != input)
            {
                _history.Add(input);
                if (_history.Count > maxHistoryEntries)
                    _history.RemoveAt(0);
            }
            _historyIndex = -1;

            AppendLog($"> {input}");

            string[] parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            string cmdName = parts[0];
            string[] args  = new string[parts.Length - 1];
            Array.Copy(parts, 1, args, 0, args.Length);

            if (_commands.TryGetValue(cmdName, out CommandEntry entry))
            {
                try   { entry.Handler.Invoke(args); }
                catch (Exception ex) { AppendLog($"[ERROR] {ex.Message}"); }
            }
            else
            {
                AppendLog($"Unknown command: '{cmdName}'. Type 'help' for a list.");
            }
#endif
        }

        /// <summary>Returns a copy of the command history list (oldest first).</summary>
        public List<string> GetCommandHistory()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            return new List<string>(_history);
#else
            return new List<string>();
#endif
        }

        /// <summary>Returns all command names that start with <paramref name="prefix"/>.</summary>
        /// <param name="prefix">Input prefix to match against.</param>
        public List<string> GetAutoCompleteSuggestions(string prefix)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            var suggestions = new List<string>();
            if (string.IsNullOrEmpty(prefix)) return suggestions;
            foreach (var key in _commands.Keys)
                if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    suggestions.Add(key);
            suggestions.Sort();
            return suggestions;
#else
            return new List<string>();
#endif
        }

        /// <summary>Returns all buffered log lines.</summary>
        public List<string> GetLogBuffer()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            return new List<string>(_logBuffer);
#else
            return new List<string>();
#endif
        }

        /// <summary>
        /// Recalls a previous command from history using up/down navigation.
        /// </summary>
        /// <param name="direction">Positive = older, negative = newer.</param>
        /// <returns>The recalled command string, or empty string at the head.</returns>
        public string RecallHistory(int direction)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (_history.Count == 0) return string.Empty;
            _historyIndex = Mathf.Clamp(_historyIndex + direction, -1, _history.Count - 1);
            return _historyIndex < 0 ? string.Empty : _history[_history.Count - 1 - _historyIndex];
#else
            return string.Empty;
#endif
        }

        #endregion

        #region Private Helpers

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private void AppendLog(string line)
        {
            _logBuffer.Add($"[{DateTime.UtcNow:HH:mm:ss}] {line}");
            if (_logBuffer.Count > maxLogLines)
                _logBuffer.RemoveAt(0);
        }

        private void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            string prefix = type switch
            {
                LogType.Warning => "[WARN] ",
                LogType.Error   => "[ERR]  ",
                LogType.Exception => "[EXC]  ",
                LogType.Assert  => "[ASSERT] ",
                _               => ""
            };
            AppendLog(prefix + condition);
        }

        private void RegisterBuiltinCommands()
        {
            RegisterCommand("help", "List all registered commands.", args =>
            {
                AppendLog("--- Registered Commands ---");
                var sorted = new List<string>(_commands.Keys);
                sorted.Sort();
                foreach (var k in sorted)
                    AppendLog($"  {k,-16} — {_commands[k].Description}");
            });

            RegisterCommand("fps", "Show current FPS stats.", args =>
            {
                if (_controller == null) { AppendLog("DebugOverlayController not found."); return; }
                var snap = _controller.GetFullSnapshot();
                AppendLog($"FPS  cur={snap.currentFPS:F1}  avg={snap.averageFPS:F1}  " +
                          $"min={snap.minFPS:F1}  max={snap.maxFPS:F1}  " +
                          $"ft={snap.frameTimeMs:F2}ms");
            });

            RegisterCommand("memory", "Show current memory stats.", args =>
            {
                if (_controller == null) { AppendLog("DebugOverlayController not found."); return; }
                var snap = _controller.GetFullSnapshot();
                AppendLog($"Memory  alloc={snap.memory.allocatedManagedMB:F1}MB  " +
                          $"reserved={snap.memory.reservedManagedMB:F1}MB  " +
                          $"totalUsed={snap.memory.totalUsedMB:F1}MB");
            });

            RegisterCommand("drawcalls", "Show current rendering stats.", args =>
            {
                if (_controller == null) { AppendLog("DebugOverlayController not found."); return; }
                var snap = _controller.GetFullSnapshot();
                AppendLog($"Rendering  drawCalls={snap.rendering.drawCalls}  " +
                          $"batches={snap.rendering.batches}  " +
                          $"tris={snap.rendering.triangles}  " +
                          $"verts={snap.rendering.vertices}");
            });

            RegisterCommand("gc_collect", "Force a full GC collection.", args =>
            {
                GC.Collect();
                AppendLog("GC.Collect() invoked.");
            });

            RegisterCommand("timescale", "Get or set Time.timeScale. Usage: timescale [value]", args =>
            {
                if (args.Length == 0)
                {
                    AppendLog($"Time.timeScale = {Time.timeScale}");
                }
                else if (float.TryParse(args[0], System.Globalization.NumberStyles.Float,
                             System.Globalization.CultureInfo.InvariantCulture, out float ts))
                {
                    Time.timeScale = Mathf.Clamp(ts, 0f, 100f);
                    AppendLog($"Time.timeScale set to {Time.timeScale}");
                }
                else
                {
                    AppendLog($"Invalid value: '{args[0]}'");
                }
            });

            RegisterCommand("screenshot", "Capture a screenshot to the persistent data path.", args =>
            {
                string path = Path.Combine(
                    Application.persistentDataPath,
                    $"SWEF_Screenshot_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png");
                ScreenCapture.CaptureScreenshot(path);
                AppendLog($"Screenshot saved: {path}");
            });
        }
#endif

        #endregion
    }
}
