using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Core
{
    /// <summary>
    /// Global error handler. Displays user-friendly error messages with retry/dismiss actions.
    /// Centralizes GPS, API, and network error handling.
    /// </summary>
    public class ErrorHandler : MonoBehaviour
    {
        /// <summary>Per-scene singleton instance.</summary>
        public static ErrorHandler Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject errorPanel;
        [SerializeField] private Text errorTitleText;
        [SerializeField] private Text errorMessageText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button dismissButton;

        private System.Action _onRetry;
        private System.Action _onDismiss;

        private void Awake()
        {
            Instance = this;

            if (errorPanel != null)
                errorPanel.SetActive(false);

            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryClicked);

            if (dismissButton != null)
                dismissButton.onClick.AddListener(HideError);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Show an error panel with a title, message, and optional retry/dismiss callbacks.
        /// </summary>
        public void ShowError(string title, string message,
            System.Action onRetry = null, System.Action onDismiss = null)
        {
            _onRetry   = onRetry;
            _onDismiss = onDismiss;

            if (errorTitleText != null)
                errorTitleText.text = title;

            if (errorMessageText != null)
                errorMessageText.text = message;

            // Show retry button only when a retry action is provided
            if (retryButton != null)
                retryButton.gameObject.SetActive(onRetry != null);

            if (errorPanel != null)
                errorPanel.SetActive(true);

            Debug.LogWarning($"[SWEF] Error shown — {title}: {message}");
        }

        /// <summary>Hide the error panel.</summary>
        public void HideError()
        {
            if (errorPanel != null)
                errorPanel.SetActive(false);

            _onDismiss?.Invoke();
            _onRetry   = null;
            _onDismiss = null;
        }

        // --- Static preset helpers ---

        /// <summary>Show a GPS-disabled error.</summary>
        public static void ShowGPSError()
        {
            if (Instance == null)
            {
                Debug.LogError("[SWEF] ErrorHandler.ShowGPSError — no Instance in scene.");
                return;
            }
            Instance.ShowError(
                "GPS Unavailable",
                "Please enable location services and restart the app.");
        }

        /// <summary>Show a GPS-timeout error with a retry callback.</summary>
        public static void ShowGPSTimeoutError(System.Action onRetry = null)
        {
            if (Instance == null)
            {
                Debug.LogError("[SWEF] ErrorHandler.ShowGPSTimeoutError — no Instance in scene.");
                return;
            }
            Instance.ShowError(
                "GPS Timeout",
                "Could not get location fix. Check if you're in an area with GPS signal.",
                onRetry);
        }

        /// <summary>Show a network error with additional detail.</summary>
        public static void ShowNetworkError(string detail)
        {
            if (Instance == null)
            {
                Debug.LogError("[SWEF] ErrorHandler.ShowNetworkError — no Instance in scene.");
                return;
            }
            Instance.ShowError("Network Error", detail);
        }

        /// <summary>Show an API error with additional detail.</summary>
        public static void ShowAPIError(string detail)
        {
            if (Instance == null)
            {
                Debug.LogError("[SWEF] ErrorHandler.ShowAPIError — no Instance in scene.");
                return;
            }
            Instance.ShowError("API Error", detail);
        }

        private void OnRetryClicked()
        {
            var retry = _onRetry;
            HideError();
            retry?.Invoke();
        }
    }
}
