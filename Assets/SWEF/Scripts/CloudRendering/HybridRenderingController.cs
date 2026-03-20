using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.CloudRendering
{
    /// <summary>
    /// Blends local and cloud rendering for the optimal player experience.
    /// The world camera is disabled when streaming; the HUD camera remains active
    /// at all times.  Transitions between modes use a 1-second crossfade.
    /// Falls back to local rendering automatically on connection loss.
    /// </summary>
    public class HybridRenderingController : MonoBehaviour
    {
        // ── Mode enum ─────────────────────────────────────────────────────────────
        /// <summary>The active rendering mode.</summary>
        public enum RenderMode
        {
            /// <summary>All rendering is performed locally.</summary>
            Local,
            /// <summary>World rendering is streamed from the cloud; HUD is rendered locally.</summary>
            Cloud,
            /// <summary>World rendered locally while cloud frames overlay selectively.</summary>
            Hybrid,
        }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Cameras")]
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Camera hudCamera;

        [Header("Crossfade")]
        [SerializeField] private CanvasGroup crossfadeOverlay;
        [SerializeField] private float crossfadeDurationSec = 1f;

        [Header("Cloud Output")]
        [SerializeField] private RawImage cloudFrameDisplay;

        // ── Internal state ────────────────────────────────────────────────────────
        private RenderMode _currentMode = RenderMode.Local;
        private Coroutine  _crossfadeCoroutine;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>The currently active rendering mode.</summary>
        public RenderMode CurrentMode => _currentMode;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Start()
        {
            var cloudMgr = FindFirstObjectByType<CloudRenderingManager>();
            if (cloudMgr != null)
            {
                cloudMgr.OnCloudModeChanged         += OnCloudModeChanged;
                cloudMgr.OnConnectionStatusChanged  += OnConnectionStatusChanged;
            }

            // Ensure cloud display is hidden by default
            if (cloudFrameDisplay != null)
                cloudFrameDisplay.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            var cloudMgr = FindFirstObjectByType<CloudRenderingManager>();
            if (cloudMgr != null)
            {
                cloudMgr.OnCloudModeChanged        -= OnCloudModeChanged;
                cloudMgr.OnConnectionStatusChanged -= OnConnectionStatusChanged;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Immediately sets the render mode without a crossfade transition.</summary>
        public void SetModeImmediate(RenderMode mode)
        {
            _currentMode = mode;
            ApplyMode(mode);
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void OnCloudModeChanged(bool cloudEnabled)
        {
            var targetMode = cloudEnabled ? RenderMode.Cloud : RenderMode.Local;
            if (_crossfadeCoroutine != null) StopCoroutine(_crossfadeCoroutine);
            _crossfadeCoroutine = StartCoroutine(CrossfadeToMode(targetMode));
        }

        private void OnConnectionStatusChanged(CloudRenderingManager.ConnectionStatus status)
        {
            if (status == CloudRenderingManager.ConnectionStatus.Error ||
                status == CloudRenderingManager.ConnectionStatus.Disconnected)
            {
                // Fallback to local rendering on connection loss
                if (_currentMode != RenderMode.Local)
                {
                    Debug.Log("[SWEF] HybridRenderingController: connection lost — falling back to local rendering");
                    if (_crossfadeCoroutine != null) StopCoroutine(_crossfadeCoroutine);
                    _crossfadeCoroutine = StartCoroutine(CrossfadeToMode(RenderMode.Local));
                }
            }
        }

        private IEnumerator CrossfadeToMode(RenderMode targetMode)
        {
            // Fade out
            yield return StartCoroutine(FadeOverlay(0f, 1f));

            _currentMode = targetMode;
            ApplyMode(targetMode);

            // Fade in
            yield return StartCoroutine(FadeOverlay(1f, 0f));
            _crossfadeCoroutine = null;
        }

        private IEnumerator FadeOverlay(float from, float to)
        {
            if (crossfadeOverlay == null)
            {
                yield break;
            }

            float elapsed      = 0f;
            float halfDuration = crossfadeDurationSec * 0.5f; // each half of the crossfade (fade-out + fade-in = full duration)
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                crossfadeOverlay.alpha = Mathf.Lerp(from, to, elapsed / halfDuration);
                yield return null;
            }
            crossfadeOverlay.alpha = to;
        }

        private void ApplyMode(RenderMode mode)
        {
            bool isCloud = mode == RenderMode.Cloud || mode == RenderMode.Hybrid;

            // Disable world camera when streaming to save GPU resources
            if (worldCamera != null)
                worldCamera.enabled = !isCloud || mode == RenderMode.Hybrid;

            // HUD camera always active
            if (hudCamera != null)
                hudCamera.enabled = true;

            // Show/hide cloud frame display
            if (cloudFrameDisplay != null)
                cloudFrameDisplay.gameObject.SetActive(isCloud);

            Debug.Log($"[SWEF] HybridRenderingController: mode → {mode}");
        }
    }
}
