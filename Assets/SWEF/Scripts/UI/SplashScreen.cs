using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SWEF.UI
{
    /// <summary>
    /// App launch splash screen controller.
    /// Fades a logo <see cref="CanvasGroup"/> in, holds for a moment, fades it out,
    /// then loads the next scene (typically "Boot").
    /// Place this component in a "Splash" scene at build index 0.
    /// </summary>
    public class SplashScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup logoGroup;
        [SerializeField] private float fadeInDuration  = 1.0f;
        [SerializeField] private float holdDuration    = 2.0f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private string nextSceneName  = "Boot";

        private void Start()
        {
            StartCoroutine(SplashSequence());
        }

        /// <summary>
        /// Plays the full splash sequence:
        /// fade in → hold → fade out → load next scene.
        /// </summary>
        private IEnumerator SplashSequence()
        {
            if (logoGroup == null)
            {
                Debug.LogWarning("[SWEF] SplashScreen: logoGroup is not assigned. Skipping splash.");
                SceneManager.LoadScene(nextSceneName);
                yield break;
            }

            // 1. Start fully transparent
            logoGroup.alpha = 0f;

            // 2. Fade in
            yield return StartCoroutine(FadeAlpha(logoGroup, 0f, 1f, fadeInDuration));

            // 3. Hold
            yield return new WaitForSeconds(holdDuration);

            // 4. Fade out
            yield return StartCoroutine(FadeAlpha(logoGroup, 1f, 0f, fadeOutDuration));

            // 5. Load next scene
            SceneManager.LoadScene(nextSceneName);
        }

        private IEnumerator FadeAlpha(CanvasGroup group, float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                group.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed    += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            group.alpha = to;
        }
    }
}
