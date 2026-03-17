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
    }
}
