using UnityEngine;

namespace SWEF.Core
{
    /// <summary>
    /// Periodic auto-save controller.
    /// Saves on a configurable interval (default 60 s, minimum 10 s) using
    /// <see cref="Time.unscaledDeltaTime"/> so the timer is not affected by pausing.
    /// Also triggers a save on application pause and quit.
    /// Requires a <see cref="SaveManager"/> in the scene.
    /// </summary>
    public class AutoSaveController : MonoBehaviour
    {
        private const float MinAutoSaveInterval = 10f;

        // ── Config ───────────────────────────────────────────────────────────
        [Header("Config")]
        [Tooltip("Enable or disable periodic auto-saving.")]
        [SerializeField] private bool  enableAutoSave       = true;
        [Tooltip("Interval in seconds between automatic saves (minimum 10 s).")]
        [SerializeField] private float autoSaveIntervalSec  = 60f;

        // ── Public properties ────────────────────────────────────────────────
        /// <summary>Gets or sets whether auto-saving is enabled.</summary>
        public bool EnableAutoSave
        {
            get => enableAutoSave;
            set => enableAutoSave = value;
        }

        /// <summary>Gets or sets the auto-save interval in seconds (minimum 10).</summary>
        public float AutoSaveIntervalSec
        {
            get => autoSaveIntervalSec;
            set => autoSaveIntervalSec = Mathf.Max(MinAutoSaveInterval, value);
        }

        /// <summary>Number of auto-saves performed since startup.</summary>
        public int SaveCount { get; private set; }

        // ── Internal ─────────────────────────────────────────────────────────
        private SaveManager _save;
        private float       _timer;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            _save = FindFirstObjectByType<SaveManager>();
            if (_save == null)
                Debug.LogWarning("[SWEF] AutoSaveController: SaveManager not found — auto-save will have no effect.");

            // Enforce minimum interval at startup
            autoSaveIntervalSec = Mathf.Max(MinAutoSaveInterval, autoSaveIntervalSec);
        }

        private void Update()
        {
            if (!enableAutoSave || _save == null) return;

            _timer += Time.unscaledDeltaTime;
            if (_timer >= autoSaveIntervalSec)
            {
                _timer = 0f;
                PerformSave("auto-save (interval)");
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _save != null)
                PerformSave("auto-save (pause)");
        }

        private void OnApplicationQuit()
        {
            if (_save != null)
                PerformSave("auto-save (quit)");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void PerformSave(string reason)
        {
            _save.Save();
            SaveCount++;
            Debug.Log($"[SWEF] AutoSaveController: {reason} #{SaveCount}.");
        }
    }
}
