using System;
using System.Collections;
using System.IO;
using UnityEngine;
using SWEF.Core;

namespace SWEF.Screenshot
{
    /// <summary>
    /// Captures the screen to a PNG file inside Application.persistentDataPath/Screenshots/.
    /// Hides the HUD Canvas before the capture frame, then restores it.
    /// Plays SFX(2) via AudioManager if available.
    /// When the user owns <see cref="PremiumFeature.HighResScreenshot"/> the capture
    /// is taken at 2× supersampling via <see cref="ScreenCapture.CaptureScreenshot(string,int)"/>.
    /// </summary>
    public class ScreenshotController : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private Canvas hudCanvas;

        [Header("Timing")]
        [SerializeField] private float hideDelay = 0.1f;

        [Header("High-Res (Premium)")]
        [Tooltip("Supersampling multiplier used when the High-Res Screenshot premium feature is active.")]
        [SerializeField] private int highResSuperSize = 2;

        /// <summary>Raised after a screenshot is saved, passing the full file path.</summary>
        public event Action<string> OnScreenshotCaptured;

        private void Awake()
        {
            if (hudCanvas == null)
                hudCanvas = FindFirstObjectByType<Canvas>();
        }

        /// <summary>Initiates a screen capture. Call from a UI button or keybind.</summary>
        public void CaptureScreenshot()
        {
            StartCoroutine(CaptureCoroutine());
        }

        private IEnumerator CaptureCoroutine()
        {
            // Hide HUD
            bool wasEnabled = false;
            if (hudCanvas != null)
            {
                wasEnabled = hudCanvas.enabled;
                hudCanvas.enabled = false;
            }

            // Wait for the specified delay then one additional frame
            if (hideDelay > 0f)
                yield return new WaitForSeconds(hideDelay);
            yield return new WaitForEndOfFrame();

            // Build file path
            string dir  = Path.Combine(Application.persistentDataPath, "Screenshots");
            Directory.CreateDirectory(dir);
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filePath  = Path.Combine(dir, $"SWEF_{timestamp}.png");

            // Capture — use high-res supersampling if the premium feature is unlocked
            bool highRes = PremiumFeatureGate.IsUnlocked(PremiumFeature.HighResScreenshot);
            if (highRes)
                ScreenCapture.CaptureScreenshot(filePath, highResSuperSize);
            else
                ScreenCapture.CaptureScreenshot(filePath);

            // Restore HUD
            if (hudCanvas != null)
                hudCanvas.enabled = wasEnabled;

            // Play SFX
            if (Audio.AudioManager.Instance != null)
                Audio.AudioManager.Instance.PlaySFX(2); // Screenshot

            Debug.Log($"[SWEF] Screenshot{(highRes ? " (High-Res)" : "")} saved: {filePath}");
            OnScreenshotCaptured?.Invoke(filePath);

            // Achievement: first screenshot
            if (Achievement.AchievementManager.Instance != null)
                Achievement.AchievementManager.Instance.TryUnlock("first_screenshot");
        }
    }
}
