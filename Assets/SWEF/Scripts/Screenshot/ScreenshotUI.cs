using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Screenshot
{
    /// <summary>
    /// Minimal screenshot UI: a capture button, an optional white flash overlay,
    /// and a short toast message confirming the save.
    /// </summary>
    public class ScreenshotUI : MonoBehaviour
    {
        [Header("Controls")]
        [SerializeField] private Button              captureButton;
        [SerializeField] private ScreenshotController controller;

        [Header("Flash overlay (optional)")]
        /// <summary>Full-screen white CanvasGroup that fades from 1→0 on capture.</summary>
        [SerializeField] private CanvasGroup flashOverlay;

        [Header("Toast")]
        [SerializeField] private Text  toastText;
        [SerializeField] private float toastDuration  = 2f;
        [SerializeField] private float flashDuration  = 0.3f;

        private Coroutine _toastCoroutine;

        private void Awake()
        {
            if (controller == null)
                controller = FindFirstObjectByType<ScreenshotController>();

            if (captureButton != null)
                captureButton.onClick.AddListener(OnCapture);

            // Start hidden
            if (flashOverlay != null) flashOverlay.alpha = 0f;
            if (toastText    != null) toastText.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (controller != null)
                controller.OnScreenshotCaptured += HandleCaptured;
        }

        private void OnDisable()
        {
            if (controller != null)
                controller.OnScreenshotCaptured -= HandleCaptured;
        }

        private void OnCapture()
        {
            if (controller != null)
                controller.CaptureScreenshot();
        }

        private void HandleCaptured(string filePath)
        {
            if (flashOverlay != null)
                StartCoroutine(FlashCoroutine());

            if (toastText != null)
            {
                if (_toastCoroutine != null) StopCoroutine(_toastCoroutine);
                _toastCoroutine = StartCoroutine(ToastCoroutine());
            }
        }

        private IEnumerator FlashCoroutine()
        {
            if (flashOverlay == null) yield break;
            flashOverlay.alpha = 1f;
            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                flashOverlay.alpha = Mathf.Lerp(1f, 0f, elapsed / flashDuration);
                yield return null;
            }
            flashOverlay.alpha = 0f;
        }

        private IEnumerator ToastCoroutine()
        {
            toastText.text = "Screenshot saved!";
            toastText.gameObject.SetActive(true);
            yield return new WaitForSeconds(toastDuration);
            toastText.gameObject.SetActive(false);
            _toastCoroutine = null;
        }
    }
}
