using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SWEF.Flight;
using SWEF.Teleport;

namespace SWEF.Core
{
    /// <summary>
    /// In-game debug overlay. Toggle with a 3-finger tap (touch) or the backtick key (keyboard).
    /// Displays a scrollable log of the last 50 Unity log messages and real-time stats
    /// (FPS, altitude, player position, memory).
    /// Supports commands typed in the input field: <c>clear</c>, <c>teleport lat,lon</c>,
    /// <c>save</c>, <c>load</c>, <c>fps</c>.
    /// </summary>
    public class DebugConsole : MonoBehaviour
    {
        // ── Inspector refs ────────────────────────────────────────────────────

        [Header("UI Refs")]
        [SerializeField] private Canvas         debugCanvas;
        [SerializeField] private ScrollRect     logScrollRect;
        [SerializeField] private Text           logTextPrefab;   // prefab for each log entry
        [SerializeField] private InputField     commandInput;
        [SerializeField] private Text           statsText;

        [Header("Settings")]
        [SerializeField] private int            maxLogMessages  = 50;
        [SerializeField] private int            requiredFingers = 3;

        // ── Private state ─────────────────────────────────────────────────────

        private readonly Queue<string> _logQueue  = new Queue<string>();
        private readonly List<Text>    _logItems  = new List<Text>();
        private bool                   _visible   = false;
        private bool                   _showFps   = true;

        // cached component refs
        private AltitudeController  _altitude;
        private TeleportController  _teleport;
        private SaveManager         _saveManager;
        private Transform           _playerRig;

        // FPS tracking
        private float _fpsAccum;
        private int   _fpsFrames;
        private float _currentFps;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (debugCanvas != null)
                debugCanvas.gameObject.SetActive(false);

            Application.logMessageReceived += OnLogMessage;
        }

        private void Start()
        {
            _altitude    = FindFirstObjectByType<AltitudeController>();
            _teleport    = FindFirstObjectByType<TeleportController>();
            _saveManager = FindFirstObjectByType<SaveManager>();

            if (_altitude != null)
                _playerRig = _altitude.transform;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLogMessage;
        }

        private void Update()
        {
            HandleToggleInput();
            TrackFps();

            if (_visible)
                RefreshStats();
        }

        // ── Toggle ────────────────────────────────────────────────────────────

        private void HandleToggleInput()
        {
            // Keyboard: backtick
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                Toggle();
                return;
            }

            // Touch: 3-finger tap
            if (Input.touchCount == requiredFingers)
            {
                bool allBegan = true;
                for (int i = 0; i < requiredFingers; i++)
                {
                    if (Input.GetTouch(i).phase != TouchPhase.Began)
                    {
                        allBegan = false;
                        break;
                    }
                }
                if (allBegan)
                    Toggle();
            }
        }

        /// <summary>Shows or hides the debug console overlay.</summary>
        public void Toggle()
        {
            _visible = !_visible;
            if (debugCanvas != null)
                debugCanvas.gameObject.SetActive(_visible);

            if (_visible && commandInput != null)
                commandInput.ActivateInputField();
        }

        // ── Log capture ───────────────────────────────────────────────────────

        private void OnLogMessage(string message, string stackTrace, LogType type)
        {
            string prefix = type switch
            {
                LogType.Error   => "<color=red>[E]</color> ",
                LogType.Warning => "<color=yellow>[W]</color> ",
                _               => "[I] ",
            };

            _logQueue.Enqueue(prefix + message);
            while (_logQueue.Count > maxLogMessages)
                _logQueue.Dequeue();

            if (_visible)
                RebuildLogUI();
        }

        private void RebuildLogUI()
        {
            if (logScrollRect == null || logTextPrefab == null) return;

            Transform content = logScrollRect.content;
            if (content == null) return;

            // Clear existing items beyond what we need
            while (_logItems.Count > _logQueue.Count)
            {
                int last = _logItems.Count - 1;
                Destroy(_logItems[last].gameObject);
                _logItems.RemoveAt(last);
            }

            string[] messages = _logQueue.ToArray();

            // Create or update items
            for (int i = 0; i < messages.Length; i++)
            {
                if (i < _logItems.Count)
                {
                    _logItems[i].text = messages[i];
                }
                else
                {
                    Text item = Instantiate(logTextPrefab, content);
                    item.text = messages[i];
                    _logItems.Add(item);
                }
            }

            // Scroll to bottom
            Canvas.ForceUpdateCanvases();
            logScrollRect.verticalNormalizedPosition = 0f;
        }

        // ── Stats ─────────────────────────────────────────────────────────────

        private void TrackFps()
        {
            _fpsAccum  += Time.unscaledDeltaTime;
            _fpsFrames += 1;

            if (_fpsAccum >= 0.5f)
            {
                _currentFps = _fpsFrames / _fpsAccum;
                _fpsAccum   = 0f;
                _fpsFrames  = 0;
            }
        }

        private void RefreshStats()
        {
            if (statsText == null) return;

            float altM  = _altitude != null ? _altitude.CurrentAltitudeMeters : 0f;
            Vector3 pos = _playerRig != null ? _playerRig.position : Vector3.zero;
            float memMb = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);

            string fpsLine = _showFps ? $"FPS: {_currentFps:F1}\n" : "";
            statsText.text =
                $"{fpsLine}" +
                $"Alt: {altM:F0} m  ({altM / 1000f:F2} km)\n" +
                $"Pos: ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})\n" +
                $"Mem: {memMb:F1} MB";
        }

        // ── Command handling ──────────────────────────────────────────────────

        /// <summary>
        /// Called by the InputField's <c>OnEndEdit</c> event (or via Enter/Return).
        /// </summary>
        public void OnSubmitCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;

            commandInput?.SetTextWithoutNotify(string.Empty);
            ProcessCommand(input.Trim().ToLowerInvariant());
            commandInput?.ActivateInputField();
        }

        private void ProcessCommand(string cmd)
        {
            if (cmd == "clear")
            {
                _logQueue.Clear();
                foreach (var item in _logItems)
                    Destroy(item.gameObject);
                _logItems.Clear();
                Debug.Log("[SWEF] DebugConsole: log cleared.");
                return;
            }

            const string teleportCmd = "teleport ";
            if (cmd.StartsWith(teleportCmd))
            {
                string[] parts = cmd.Substring(teleportCmd.Length).Split(',');
                if (parts.Length >= 2 &&
                    double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double lon))
                {
                    if (_teleport != null)
                    {
                        _teleport.TeleportTo(lat, lon, $"Debug ({lat:F4},{lon:F4})");
                        Debug.Log($"[SWEF] DebugConsole: teleporting to {lat},{lon}");
                    }
                    else
                    {
                        Debug.LogWarning("[SWEF] DebugConsole: TeleportController not found.");
                    }
                }
                else
                {
                    Debug.LogWarning("[SWEF] DebugConsole: usage — teleport <lat>,<lon>");
                }
                return;
            }

            if (cmd == "save")
            {
                if (_saveManager != null)
                    _saveManager.Save();
                else
                    Debug.LogWarning("[SWEF] DebugConsole: SaveManager not found.");
                return;
            }

            if (cmd == "load")
            {
                if (_saveManager != null)
                    _saveManager.Load();
                else
                    Debug.LogWarning("[SWEF] DebugConsole: SaveManager not found.");
                return;
            }

            if (cmd == "fps")
            {
                _showFps = !_showFps;
                Debug.Log($"[SWEF] DebugConsole: FPS counter {(_showFps ? "on" : "off")}.");
                return;
            }

            Debug.LogWarning($"[SWEF] DebugConsole: unknown command '{cmd}'. " +
                             "Commands: clear | teleport lat,lon | save | load | fps");
        }
    }
}
