using UnityEngine;
using SWEF.Flight;
using SWEF.Util;

namespace SWEF.Atmosphere
{
    /// <summary>
    /// Manages altitude-based cloud layer visibility.
    /// For each registered cloud layer the alpha is smoothly interpolated
    /// depending on how far the player is above or below the cloud altitude.
    /// </summary>
    public class CloudLayer : MonoBehaviour
    {
        [SerializeField] private AltitudeController altitudeSource;

        [Tooltip("Array of cloud GameObjects / particle-system holders at different altitudes.")]
        [SerializeField] private GameObject[] cloudLayers = new GameObject[0];

        [Tooltip("Matching altitude thresholds in meters (one per cloud layer).")]
        [SerializeField] private float[] cloudAltitudes = new float[] { 2000f, 5000f, 10000f };

        [Tooltip("Metres over which clouds fade in/out around the threshold.")]
        [SerializeField] private float fadeRange = 500f;

        [Tooltip("Alpha applied when the player is above the cloud layer (looking down).")]
        [SerializeField] private float cloudAlphaAbove = 0.15f;

        [Tooltip("Alpha applied when the player is below the cloud layer (looking up).")]
        [SerializeField] private float cloudAlphaBelow = 0.8f;

        // Per-layer smoothed alpha values
        private float[] _currentAlphas;
        private MaterialPropertyBlock[] _propBlocks;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            if (altitudeSource == null)
                altitudeSource = FindFirstObjectByType<AltitudeController>();

            _currentAlphas = new float[cloudLayers.Length];
            _propBlocks    = new MaterialPropertyBlock[cloudLayers.Length];
            for (int i = 0; i < _currentAlphas.Length; i++)
            {
                _currentAlphas[i] = cloudAlphaBelow;
                _propBlocks[i]    = new MaterialPropertyBlock();
            }
        }

        private void Update()
        {
            if (altitudeSource == null) return;

            float alt = altitudeSource.CurrentAltitudeMeters;
            float dt  = Time.deltaTime;

            for (int i = 0; i < cloudLayers.Length; i++)
            {
                if (cloudLayers[i] == null) continue;

                float cloudAlt = i < cloudAltitudes.Length ? cloudAltitudes[i] : 0f;
                float diff     = alt - cloudAlt; // positive = player above cloud

                float targetAlpha;
                if (diff > fadeRange)
                {
                    // Well above cloud
                    targetAlpha = cloudAlphaAbove;
                }
                else if (diff < -fadeRange)
                {
                    // Well below cloud
                    targetAlpha = cloudAlphaBelow;
                }
                else
                {
                    // Transitioning through the cloud band
                    float t = (diff + fadeRange) / (2f * fadeRange); // 0=below, 1=above
                    targetAlpha = Mathf.Lerp(cloudAlphaBelow, cloudAlphaAbove, t);
                }

                _currentAlphas[i] = ExpSmoothing.ExpLerp(_currentAlphas[i], targetAlpha, 4f, dt);

                // Enable/disable the layer based on visibility distance from threshold
                bool shouldBeActive = Mathf.Abs(diff) < fadeRange * 20f;
                if (cloudLayers[i].activeSelf != shouldBeActive)
                    cloudLayers[i].SetActive(shouldBeActive);

                // Apply alpha to each Renderer in the layer
                SetLayerAlpha(cloudLayers[i], _currentAlphas[i], _propBlocks[i]);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

        private static void SetLayerAlpha(GameObject layer, float alpha, MaterialPropertyBlock block)
        {
            // Try CanvasGroup first (UI overlays)
            var cg = layer.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = alpha;
                return;
            }

            // Use MaterialPropertyBlock to avoid per-frame material instance creation
            var renderers = layer.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.GetPropertyBlock(block);
                // Only set a colour override if the shader supports _Color
                Color baseColor = r.sharedMaterial != null ? r.sharedMaterial.color : Color.white;
                baseColor.a = alpha;
                block.SetColor(ColorPropertyId, baseColor);
                r.SetPropertyBlock(block);
            }
        }
    }
}
