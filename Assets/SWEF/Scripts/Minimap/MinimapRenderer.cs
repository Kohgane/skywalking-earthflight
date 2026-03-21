using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Minimap
{
    /// <summary>Minimap display shapes.</summary>
    public enum MinimapShape { Circular, Square }

    /// <summary>
    /// UI MonoBehaviour that renders <see cref="MinimapBlip"/> icons on a Canvas-based minimap
    /// panel. Uses object-pooled <see cref="RectTransform"/> icon prefabs so no allocation occurs
    /// per frame. Supports circular and square shapes, pulsing animations, and zoom levels.
    /// </summary>
    public class MinimapRenderer : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────────
        [Header("References")]
        [Tooltip("Root RectTransform that defines the minimap panel area.")]
        [SerializeField] private RectTransform mapPanel;

        [Tooltip("Prefab used for each blip icon. Must have an Image component.")]
        [SerializeField] private RectTransform blipIconPrefab;

        [Tooltip("Icon configuration ScriptableObject mapping MinimapIconType to sprite/colour.")]
        [SerializeField] private MinimapIconConfig iconConfig;

        [Header("Shape & Zoom")]
        [Tooltip("Circular clips icons to a round mask; Square allows the full rectangle.")]
        [SerializeField] private MinimapShape shape = MinimapShape.Circular;

        [Tooltip("Current world-unit radius shown by the minimap.")]
        [SerializeField] private float zoomRange = 1000f;

        [Tooltip("Smoothing speed when transitioning between zoom levels (units/sec).")]
        [SerializeField] private float zoomLerpSpeed = 3f;

        [Header("Display")]
        [Tooltip("Overall opacity of the blip layer.")]
        [Range(0.3f, 1f)]
        [SerializeField] private float opacity = 1f;

        [Tooltip("Uniform scale multiplier applied to all blip icons.")]
        [Range(0.5f, 2f)]
        [SerializeField] private float iconSizeMultiplier = 1f;

        [Tooltip("Radius (in pixels) of the circular map panel. Only used when shape = Circular.")]
        [SerializeField] private float mapRadiusPx = 100f;

        [Tooltip("Transform of the player icon that sits at the center of the minimap.")]
        [SerializeField] private RectTransform playerIcon;

        [Header("Pulse Animation")]
        [Tooltip("Speed of the sinusoidal pulse cycle in Hz.")]
        [SerializeField] private float pulseFrequency = 2f;

        [Tooltip("Amplitude of scale oscillation added on top of the normal scale.")]
        [SerializeField] private float pulseAmplitude = 0.25f;

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly List<RectTransform> _pool       = new List<RectTransform>();
        private readonly List<RectTransform> _active     = new List<RectTransform>();
        private float                        _targetZoom;
        private float                        _currentZoom;

        // ── Unity callbacks ────────────────────────────────────────────────────────
        private void Awake()
        {
            _targetZoom  = zoomRange;
            _currentZoom = zoomRange;

            if (mapPanel == null)
                mapPanel = GetComponent<RectTransform>();

            // Pre-warm pool
            for (int i = 0; i < 32; i++)
                _pool.Add(CreatePooledIcon());
        }

        private void LateUpdate()
        {
            // Smooth zoom transition
            _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, Time.deltaTime * zoomLerpSpeed);

            ReturnAllToPool();

            if (MinimapManager.Instance == null) return;

            var blips = MinimapManager.Instance.GetActiveBlips();

            // Rotate player icon to match player heading
            if (playerIcon != null && MinimapManager.Instance.PlayerTransform != null)
            {
                float heading = MinimapManager.Instance.PlayerTransform.eulerAngles.y;
                playerIcon.localRotation = Quaternion.Euler(0f, 0f, -heading);
            }

            for (int i = 0; i < blips.Count; i++)
            {
                MinimapBlip blip = blips[i];
                if (blip.iconType == MinimapIconType.Player) continue; // drawn by playerIcon

                PlaceBlip(blip);
            }
        }

        // ── Blip placement ─────────────────────────────────────────────────────────
        private void PlaceBlip(MinimapBlip blip)
        {
            // Normalised distance (0 = centre, 1 = edge)
            float normDist = Mathf.Clamp01(blip.distanceFromPlayer / _currentZoom);

            // Convert bearing + normalised distance to 2-D map position
            float bearingRad = blip.bearingDeg * Mathf.Deg2Rad;
            float px         = Mathf.Sin(bearingRad) * normDist;
            float py         = Mathf.Cos(bearingRad) * normDist;

            bool clamped = false;

            if (shape == MinimapShape.Circular)
            {
                // Clamp to circle edge
                Vector2 dir = new Vector2(px, py);
                if (dir.magnitude > 1f)
                {
                    dir.Normalize();
                    px      = dir.x;
                    py      = dir.y;
                    clamped = true;
                }
            }

            RectTransform rt    = GetFromPool();
            Image          img  = rt.GetComponent<Image>();

            if (iconConfig != null)
            {
                IconEntry entry = iconConfig.GetEntry(blip.iconType);
                if (img != null)
                {
                    if (entry.sprite != null) img.sprite = entry.sprite;
                    Color c = string.IsNullOrEmpty(blip.customIconId) ? entry.defaultColor : blip.color;
                    c.a    = opacity;
                    img.color = c;
                }

                float scale = entry.defaultScale * iconSizeMultiplier;
                if (blip.isPulsing)
                {
                    float pulse = 1f + Mathf.Sin(Time.time * pulseFrequency * Mathf.PI * 2f) * pulseAmplitude;
                    scale      *= pulse;
                }
                rt.localScale = Vector3.one * scale;
            }

            // Anchor at map centre, then offset by panel half-size
            float halfW = mapPanel.rect.width  * 0.5f;
            float halfH = mapPanel.rect.height * 0.5f;

            if (shape == MinimapShape.Circular)
            {
                rt.anchoredPosition = new Vector2(px * mapRadiusPx, py * mapRadiusPx);
            }
            else
            {
                rt.anchoredPosition = new Vector2(px * halfW, py * halfH);
            }

            rt.gameObject.SetActive(true);
            _active.Add(rt);
        }

        // ── Object pool helpers ────────────────────────────────────────────────────
        private RectTransform GetFromPool()
        {
            if (_pool.Count > 0)
            {
                var rt = _pool[_pool.Count - 1];
                _pool.RemoveAt(_pool.Count - 1);
                return rt;
            }
            return CreatePooledIcon();
        }

        private void ReturnAllToPool()
        {
            foreach (var rt in _active)
            {
                rt.gameObject.SetActive(false);
                _pool.Add(rt);
            }
            _active.Clear();
        }

        private RectTransform CreatePooledIcon()
        {
            RectTransform rt;
            if (blipIconPrefab != null)
            {
                rt = Instantiate(blipIconPrefab, mapPanel);
            }
            else
            {
                var go  = new GameObject("BlipIcon", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(mapPanel, false);
                rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(16f, 16f);
            }
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.gameObject.SetActive(false);
            return rt;
        }

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>Sets the target world-unit zoom range. Transitions smoothly via lerp.</summary>
        public void SetZoom(float range) => _targetZoom = Mathf.Max(50f, range);

        /// <summary>Changes the minimap shape at runtime.</summary>
        public void SetShape(MinimapShape s) => shape = s;

        /// <summary>Sets the blip layer opacity (0.3 – 1.0).</summary>
        public void SetOpacity(float o) => opacity = Mathf.Clamp(o, 0.3f, 1f);

        /// <summary>Sets the icon size multiplier (0.5 – 2.0).</summary>
        public void SetIconSizeMultiplier(float m) => iconSizeMultiplier = Mathf.Clamp(m, 0.5f, 2f);

        /// <summary>Assigns a new icon config asset at runtime.</summary>
        public void SetIconConfig(MinimapIconConfig cfg) => iconConfig = cfg;
    }
}
