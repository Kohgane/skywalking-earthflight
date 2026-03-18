using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Cinema
{
    /// <summary>
    /// UI panel for editing and playing cinematic camera paths.
    /// Provides waypoint list management, playback controls, seek bar, and path save/load.
    /// Phase 18 — Cinematic Camera UI.
    /// </summary>
    public class CinematicCameraUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Panel")]
        [SerializeField] private GameObject cinematicPanel;

        [Header("Path")]
        [SerializeField] private CinematicCameraPath cameraPath;

        [Header("Waypoint List")]
        [SerializeField] private Transform    waypointListContent;
        [SerializeField] private GameObject   waypointItemPrefab;

        [Header("Playback Controls")]
        [SerializeField] private Button   addWaypointButton;
        [SerializeField] private Button   playButton;
        [SerializeField] private Button   pauseButton;
        [SerializeField] private Button   stopButton;
        [SerializeField] private Slider   progressSlider;
        [SerializeField] private Text     timeText;
        [SerializeField] private Dropdown loopModeDropdown;
        [SerializeField] private Toggle   catmullRomToggle;
        [SerializeField] private Slider   speedSlider;

        [Header("Path Info")]
        [SerializeField] private Text waypointCountText;
        [SerializeField] private Text totalDurationText;

        [Header("Save / Load")]
        [SerializeField] private Button     savePathButton;
        [SerializeField] private Button     loadPathButton;
        [SerializeField] private InputField pathNameInput;

        // ── State ─────────────────────────────────────────────────────────────────
        private bool _ignoreSlider;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (cameraPath == null)
                cameraPath = FindFirstObjectByType<CinematicCameraPath>();

            if (addWaypointButton != null) addWaypointButton.onClick.AddListener(OnAddWaypoint);
            if (playButton        != null) playButton.onClick.AddListener(OnPlay);
            if (pauseButton       != null) pauseButton.onClick.AddListener(OnPause);
            if (stopButton        != null) stopButton.onClick.AddListener(OnStop);
            if (savePathButton    != null) savePathButton.onClick.AddListener(OnSavePath);
            if (loadPathButton    != null) loadPathButton.onClick.AddListener(OnLoadPath);

            if (progressSlider != null)
                progressSlider.onValueChanged.AddListener(OnSeek);

            if (cinematicPanel != null)
                cinematicPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (cameraPath == null) return;
            cameraPath.OnPlaybackStarted    += OnPlaybackStarted;
            cameraPath.OnPlaybackCompleted  += OnPlaybackCompleted;
            cameraPath.OnWaypointReached    += OnWaypointReached;
        }

        private void OnDisable()
        {
            if (cameraPath == null) return;
            cameraPath.OnPlaybackStarted    -= OnPlaybackStarted;
            cameraPath.OnPlaybackCompleted  -= OnPlaybackCompleted;
            cameraPath.OnWaypointReached    -= OnWaypointReached;
        }

        private void Update()
        {
            if (cameraPath == null) return;
            UpdateProgressUI();
        }

        // ── Public API ────────────────────────────────────────────────────────────
        /// <summary>Rebuilds the waypoint list UI.</summary>
        public void Refresh()
        {
            if (cameraPath == null) return;

            // Clear existing items
            if (waypointListContent != null)
            {
                foreach (Transform child in waypointListContent)
                    Destroy(child.gameObject);
            }

            // Rebuild
            for (int i = 0; i < cameraPath.WaypointCount; i++)
            {
                int capturedIndex = i;
                if (waypointItemPrefab != null && waypointListContent != null)
                {
                    var item   = Instantiate(waypointItemPrefab, waypointListContent);
                    var labels = item.GetComponentsInChildren<Text>();
                    if (labels.Length > 0) labels[0].text = $"WP {capturedIndex}";

                    var buttons = item.GetComponentsInChildren<Button>();
                    foreach (var btn in buttons)
                    {
                        if (btn.name.Contains("Delete") || btn.name.Contains("Remove"))
                            btn.onClick.AddListener(() => OnRemoveWaypoint(capturedIndex));
                        if (btn.name.Contains("Update") || btn.name.Contains("Edit"))
                            btn.onClick.AddListener(() => OnUpdateWaypoint(capturedIndex));
                    }
                }
            }

            UpdateInfoUI();
        }

        // ── Button handlers ───────────────────────────────────────────────────────
        private void OnAddWaypoint()
        {
            if (cameraPath == null) return;
            cameraPath.AddWaypoint();
            Refresh();

            // Achievement
            if (cameraPath.WaypointCount >= 2 &&
                Achievement.AchievementManager.Instance != null)
                Achievement.AchievementManager.Instance.TryUnlock("cinematic_path_created");
        }

        private void OnRemoveWaypoint(int index)
        {
            if (cameraPath == null) return;
            cameraPath.RemoveWaypoint(index);
            Refresh();
        }

        private void OnUpdateWaypoint(int index)
        {
            if (cameraPath == null) return;
            cameraPath.UpdateWaypoint(index);
            Refresh();
        }

        private void OnPlay()
        {
            if (cameraPath == null) return;
            if (cameraPath.CurrentState == CinematicCameraPath.PlaybackState.Paused)
                cameraPath.Resume();
            else
                cameraPath.Play();
        }

        private void OnPause() => cameraPath?.Pause();
        private void OnStop()  => cameraPath?.Stop();

        private void OnSeek(float value)
        {
            if (_ignoreSlider || cameraPath == null) return;
            cameraPath.Seek(value * cameraPath.GetTotalDuration());
        }

        private void OnSavePath()
        {
            if (cameraPath == null) return;
            string name = pathNameInput != null && !string.IsNullOrEmpty(pathNameInput.text)
                ? pathNameInput.text
                : $"Path_{DateTime.Now:yyyyMMdd_HHmmss}";

            string dir = Path.Combine(Application.persistentDataPath, "CameraPaths");
            Directory.CreateDirectory(dir);
            string filePath = Path.Combine(dir, $"{name}.json");
            File.WriteAllText(filePath, cameraPath.ToJson());
            Debug.Log($"[SWEF] CinematicCameraUI: Path saved → {filePath}");

            if (Achievement.AchievementManager.Instance != null)
                Achievement.AchievementManager.Instance.TryUnlock("cinematic_path_created");
        }

        private void OnLoadPath()
        {
            if (cameraPath == null || pathNameInput == null) return;
            string name = pathNameInput.text;
            if (string.IsNullOrEmpty(name)) return;

            string filePath = Path.Combine(Application.persistentDataPath, "CameraPaths", $"{name}.json");
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[SWEF] CinematicCameraUI: Path file not found — {filePath}");
                return;
            }

            string json     = File.ReadAllText(filePath);
            var    loaded   = CinematicCameraPath.FromJson(json);
            Debug.Log($"[SWEF] CinematicCameraUI: Path loaded — {filePath}");
            if (loaded != null) Refresh();
        }

        // ── Event callbacks ───────────────────────────────────────────────────────
        private void OnPlaybackStarted()   => UpdateInfoUI();
        private void OnPlaybackCompleted() => UpdateInfoUI();
        private void OnWaypointReached(int index) =>
            Debug.Log($"[SWEF] CinematicCameraUI: Reached waypoint {index}.");

        // ── UI helpers ────────────────────────────────────────────────────────────
        private void UpdateProgressUI()
        {
            if (cameraPath == null) return;
            float total   = cameraPath.GetTotalDuration();
            float current = cameraPath.CurrentPlaybackTime;
            if (total <= 0f) return;

            if (progressSlider != null && cameraPath.CurrentState == CinematicCameraPath.PlaybackState.Playing)
            {
                _ignoreSlider = true;
                progressSlider.SetValueWithoutNotify(total > 0f ? current / total : 0f);
                _ignoreSlider = false;
            }
            else if (cameraPath.CurrentState == CinematicCameraPath.PlaybackState.Stopped)
            {
                _ignoreSlider = true;
                if (progressSlider != null) progressSlider.SetValueWithoutNotify(0f);
                _ignoreSlider = false;
            }

            if (timeText != null)
            {
                int curSec   = Mathf.RoundToInt(current);
                int totalSec = Mathf.RoundToInt(total);
                timeText.text = $"{curSec / 60}:{curSec % 60:D2} / {totalSec / 60}:{totalSec % 60:D2}";
            }
        }

        private void UpdateInfoUI()
        {
            if (cameraPath == null) return;

            if (waypointCountText != null)
                waypointCountText.text = $"Waypoints: {cameraPath.WaypointCount}";

            float total = cameraPath.GetTotalDuration();
            if (totalDurationText != null)
            {
                int totalSec = Mathf.RoundToInt(total);
                totalDurationText.text = $"Duration: {totalSec / 60}:{totalSec % 60:D2}";
            }
        }
    }
}
