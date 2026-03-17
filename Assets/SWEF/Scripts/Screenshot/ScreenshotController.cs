using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SWEF.Screenshot
{
    /// <summary>
    /// Captures the screen to a PNG file inside Application.persistentDataPath/Screenshots/.
    /// Hides the HUD Canvas before the capture frame, then restores it.
    /// Plays SFX(2) via AudioManager if available.
    /// </summary>
    public class ScreenshotController : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private Canvas hudCanvas;

        [Header("Timing")]
        [SerializeField] private float hideDelay = 0.1f;

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

            // Capture
            ScreenCapture.CaptureScreenshot(filePath);

            // Restore HUD
            if (hudCanvas != null)
                hudCanvas.enabled = wasEnabled;

            // Play SFX
            if (Audio.AudioManager.Instance != null)
                Audio.AudioManager.Instance.PlaySFX(2); // Screenshot

            Debug.Log($"[SWEF] Screenshot saved: {filePath}");
            OnScreenshotCaptured?.Invoke(filePath);

            // Achievement: first screenshot
            if (Achievement.AchievementManager.Instance != null)
                Achievement.AchievementManager.Instance.TryUnlock("first_screenshot");
        }
    }
}
