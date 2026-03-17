using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Core
{
    /// <summary>
    /// Loading screen controller for Boot→World scene transition.
    /// Shows progress bar and rotating tip texts while GPS initializes.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private Text statusText;
        [SerializeField] private Text tipText;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Tips")]
        [SerializeField] private string[] tips = new string[]
        {
            "Did you know? The Kármán line at 100km marks the edge of space.",
            "Tip: Toggle Comfort Mode for a smoother flight experience.",
            "You can teleport to any place on Earth using the Search feature!",
            "Save your favorite locations with the ⭐ Favorites button.",
            "Take screenshots to capture your journey to space! 📸",
            "The atmosphere changes as you climb — watch the sky transform!"
        };

        private Coroutine _fadeCoroutine;
        private Coroutine _tipCoroutine;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            if (loadingPanel != null)
                loadingPanel.SetActive(false);
        }

        /// <summary>Fade the loading screen in (alpha 0→1 over 0.5 s).</summary>
        public void Show()
        {
            if (loadingPanel != null)
                loadingPanel.SetActive(true);

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeCoroutine(1f));

            if (_tipCoroutine != null)
                StopCoroutine(_tipCoroutine);
            _tipCoroutine = StartCoroutine(RotateTipsCoroutine());

            Debug.Log("[SWEF] LoadingScreen shown.");
        }

        /// <summary>Fade the loading screen out (alpha 1→0 over 0.5 s).</summary>
        public void Hide()
        {
            if (_tipCoroutine != null)
            {
                StopCoroutine(_tipCoroutine);
                _tipCoroutine = null;
            }

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(HideCoroutine());

            Debug.Log("[SWEF] LoadingScreen hiding.");
        }

        /// <summary>Update the progress bar (0–1).</summary>
        public void SetProgress(float value)
        {
            if (progressBar != null)
                progressBar.value = Mathf.Clamp01(value);
        }

        /// <summary>Update the status text label.</summary>
        public void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        private IEnumerator FadeCoroutine(float targetAlpha)
        {
            if (canvasGroup == null) yield break;

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;
            const float duration = 0.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        private IEnumerator HideCoroutine()
        {
            yield return FadeCoroutine(0f);

            if (loadingPanel != null)
                loadingPanel.SetActive(false);
        }

        private IEnumerator RotateTipsCoroutine()
        {
            if (tips == null || tips.Length == 0) yield break;

            int index = 0;
            while (true)
            {
                if (tipText != null)
                    tipText.text = tips[index % tips.Length];

                index++;
                yield return new WaitForSeconds(3f);
            }
        }
    }
}
