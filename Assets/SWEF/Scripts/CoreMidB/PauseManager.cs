using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SWEF.Core
{
    /// <summary>
    /// Manages game pause/resume state. Sets Time.timeScale to 0 when paused.
    /// Shows pause panel with Resume and Quit buttons.
    /// </summary>
    public class PauseManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static PauseManager Instance { get; private set; }

        // ── Inspector refs ───────────────────────────────────────────────────────
        [Header("Pause Panel")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private CanvasGroup pauseOverlay;

        // ── State ────────────────────────────────────────────────────────────────
        /// <summary>Whether the game is currently paused.</summary>
        public bool IsPaused { get; private set; }

        /// <summary>Fired whenever pause state changes. Argument is the new IsPaused value.</summary>
        public event System.Action<bool> OnPauseChanged;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (resumeButton != null)
                resumeButton.onClick.AddListener(Resume);
            if (quitButton != null)
                quitButton.onClick.AddListener(QuitToMenu);

            // Start unpaused
            SetPauseVisuals(false);
        }

        private void OnDestroy()
        {
            // Safety: always restore timeScale when this object is destroyed
            Time.timeScale = 1f;
        }

        // ── Public API ────────────────────────────────────────────────────────────
        /// <summary>Toggles between paused and unpaused states.</summary>
        public void TogglePause()
        {
            // In multiplayer rooms, do not freeze time — use overlay only
            if (IsInMultiplayerRoom)
            {
                if (!_multiplayerPause)
                    ShowMultiplayerPauseOverlay();
                else
                    HideMultiplayerPauseOverlay();
                return;
            }

            IsPaused = !IsPaused;
            Time.timeScale = IsPaused ? 0f : 1f;
            SetPauseVisuals(IsPaused);
            OnPauseChanged?.Invoke(IsPaused);
            Debug.Log($"[SWEF] PauseManager: {(IsPaused ? "Paused" : "Resumed")}");
        }

        /// <summary>Forces the game to resume (un-pauses).</summary>
        public void Resume()
        {
            if (!IsPaused) return;
            TogglePause();
        }

        /// <summary>Restores timeScale then loads the Boot scene.</summary>
        public void QuitToMenu()
        {
            // Ensure timeScale is restored before loading
            if (IsPaused)
            {
                IsPaused = false;
                Time.timeScale = 1f;
                SetPauseVisuals(false);
                OnPauseChanged?.Invoke(false);
            }
            Debug.Log("[SWEF] PauseManager: Quitting to Boot scene.");
            SceneManager.LoadScene("Boot");
        }

        // ── Internal ─────────────────────────────────────────────────────────────
        private void SetPauseVisuals(bool paused)
        {
            if (pausePanel != null)
                pausePanel.SetActive(paused);

            if (pauseOverlay != null)
            {
                pauseOverlay.alpha = paused ? 1f : 0f;
                pauseOverlay.interactable = paused;
                pauseOverlay.blocksRaycasts = paused;
            }
        }

        // ── Phase 18 — Photo Mode pause ───────────────────────────────────────────
        [Header("Phase 18 — Photo Mode")]
        private bool _photoModePause = false;

        /// <summary>Freezes time for photo mode without showing the pause panel.</summary>
        public void PauseForPhotoMode()
        {
            _photoModePause = true;
            Time.timeScale = 0f;
            Debug.Log("[SWEF] Game paused for Photo Mode");
        }

        /// <summary>Restores normal time after photo mode is exited.</summary>
        public void ResumeFromPhotoMode()
        {
            _photoModePause = false;
            Time.timeScale = 1f;
            Debug.Log("[SWEF] Game resumed from Photo Mode");
        }

        /// <summary>Whether the game is currently paused specifically for photo mode.</summary>
        public bool IsPhotoModePaused => _photoModePause;

        // ── Phase 20 — Multiplayer pause ─────────────────────────────────────────

        [Header("Phase 20 — Multiplayer")]
        [SerializeField] private CanvasGroup multiplayerPauseOverlay;

        private bool _multiplayerPause;

        // Reflection cache — avoids hard compile-time dependency on SWEF.Multiplayer
        // (a direct reference would create a cyclic assembly chain CoreMidB → Multiplayer → … → CoreMidB).
        private static System.Type                  _multiplayerManagerType;
        private static System.Reflection.PropertyInfo _isInRoomProp;

        private static void EnsureMultiplayerReflection()
        {
            if (_multiplayerManagerType != null) return;
            _multiplayerManagerType = System.Type.GetType("SWEF.Multiplayer.MultiplayerManager, SWEF.Multiplayer");
            if (_multiplayerManagerType != null)
                _isInRoomProp = _multiplayerManagerType.GetProperty("IsInRoom");
        }

        /// <summary>
        /// Whether the game is currently in multiplayer mode.
        /// When true, <see cref="TogglePause"/> dims the screen instead of freezing time.
        /// Uses reflection to avoid a hard compile-time dependency on SWEF.Multiplayer
        /// (which would create a cyclic assembly reference CoreMidB → Multiplayer → … → CoreMidB).
        /// </summary>
        public bool IsInMultiplayerRoom
        {
            get
            {
                EnsureMultiplayerReflection();
                if (_multiplayerManagerType == null) return false;
                var obj = FindFirstObjectByType(_multiplayerManagerType) as MonoBehaviour;
                if (obj == null) return false;
                return _isInRoomProp != null && (bool)_isInRoomProp.GetValue(obj);
            }
        }

        /// <summary>
        /// Shows a dim overlay for multiplayer without stopping <see cref="Time.timeScale"/>,
        /// since freezing time in a multiplayer session would desync other players.
        /// </summary>
        public void ShowMultiplayerPauseOverlay()
        {
            _multiplayerPause = true;
            if (multiplayerPauseOverlay != null)
            {
                multiplayerPauseOverlay.alpha          = 0.6f;
                multiplayerPauseOverlay.interactable   = true;
                multiplayerPauseOverlay.blocksRaycasts = true;
            }
            Debug.Log("[SWEF][PauseManager] Multiplayer pause overlay shown (time not frozen).");
        }

        /// <summary>Hides the multiplayer pause overlay.</summary>
        public void HideMultiplayerPauseOverlay()
        {
            _multiplayerPause = false;
            if (multiplayerPauseOverlay != null)
            {
                multiplayerPauseOverlay.alpha          = 0f;
                multiplayerPauseOverlay.interactable   = false;
                multiplayerPauseOverlay.blocksRaycasts = false;
            }
            Debug.Log("[SWEF][PauseManager] Multiplayer pause overlay hidden.");
        }

        /// <summary>Whether the multiplayer pause overlay is currently visible.</summary>
        public bool IsMultiplayerPaused => _multiplayerPause;
    }
}
