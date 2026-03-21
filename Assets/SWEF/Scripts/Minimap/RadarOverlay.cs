using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Minimap
{
    /// <summary>
    /// Alternative radar display with a classic rotating sweep line.
    /// Blips appear as dots that fade after the sweep passes them.
    /// Can toggle between minimap mode (hidden) and radar mode (shown).
    /// </summary>
    public class RadarOverlay : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────────
        [Header("References")]
        [Tooltip("Root panel that contains all radar UI (shown/hidden based on mode).")]
        [SerializeField] private GameObject radarRoot;

        [Tooltip("Canvas used to draw procedural radar elements (sweep, rings, dots).")]
        [SerializeField] private RectTransform radarCanvas;

        [Tooltip("Image component used to represent the sweep line (rotated each frame).")]
        [SerializeField] private RectTransform sweepLine;

        [Tooltip("Prefab for each blip dot on the radar.")]
        [SerializeField] private RectTransform dotPrefab;

        [Tooltip("Optional AudioSource for the ping sound when sweep crosses a new blip.")]
        [SerializeField] private AudioSource pingSoundSource;

        [Header("Sweep Settings")]
        [Tooltip("Revolutions per minute of the radar sweep.")]
        [SerializeField] private float sweepRPM = 6f;

        [Tooltip("Time in seconds before a blip dot fully fades after being swept.")]
        [SerializeField] private float blipPersistSeconds = 5f;

        [Header("Display")]
        [Tooltip("World-unit radius represented by the outermost radar ring.")]
        [SerializeField] private float radarRangeMeters = 5000f;

        [Tooltip("Intervals at which concentric range rings are drawn (world units).")]
        [SerializeField] private float[] ringIntervals = { 1000f, 2000f, 5000f };

        [Tooltip("Radius in pixels of the radar display circle.")]
        [SerializeField] private float radarRadiusPx = 150f;

        [Header("Style")]
        [Tooltip("Tint colour applied to blip dots (phosphor green style).")]
        [SerializeField] private Color blipDotColor = new Color(0f, 1f, 0.3f, 1f);

        // ── Private state ─────────────────────────────────────────────────────────
        private float _sweepAngleDeg;
        private bool  _radarActive;

        private class BlipDotState
        {
            public string         blipId;
            public RectTransform  rt;
            public Image          img;
            public float          spawnTime;
            public bool           swept;
        }

        private readonly List<BlipDotState>               _dots      = new List<BlipDotState>();
        private readonly Dictionary<string, BlipDotState> _dotById   = new Dictionary<string, BlipDotState>();
        private readonly List<RectTransform>               _dotPool   = new List<RectTransform>();
        private readonly HashSet<string>                   _newBlipIds = new HashSet<string>();

        // Sweep angle is in 0..360 clockwise from North
        private float _degreesPerSecond;

        // ── Unity callbacks ────────────────────────────────────────────────────────
        private void Awake()
        {
            _degreesPerSecond = sweepRPM * 360f / 60f;

            if (radarRoot != null)
                radarRoot.SetActive(false);

            // Pre-warm dot pool
            for (int i = 0; i < 32; i++)
                _dotPool.Add(CreateDotFromPool());
        }

        private void Update()
        {
            if (!_radarActive) return;

            float prev = _sweepAngleDeg;
            _sweepAngleDeg = (_sweepAngleDeg + _degreesPerSecond * Time.deltaTime) % 360f;

            // Rotate sweep line image
            if (sweepLine != null)
                sweepLine.localRotation = Quaternion.Euler(0f, 0f, -_sweepAngleDeg);

            // Update all active blip dots from MinimapManager
            RefreshDots(prev, _sweepAngleDeg);

            // Fade dots that have been swept
            FadeOldDots();
        }

        // ── Dot management ─────────────────────────────────────────────────────────
        private void RefreshDots(float prevAngle, float currentAngle)
        {
            if (MinimapManager.Instance == null) return;

            var blips = MinimapManager.Instance.GetActiveBlips();
            _newBlipIds.Clear();

            foreach (var blip in blips)
            {
                _newBlipIds.Add(blip.blipId);

                if (!_dotById.TryGetValue(blip.blipId, out BlipDotState dot))
                {
                    dot         = AllocateDot(blip.blipId);
                    dot.swept   = false;
                }

                // Position dot
                float normDist = Mathf.Clamp01(blip.distanceFromPlayer / radarRangeMeters);
                float rad      = blip.bearingDeg * Mathf.Deg2Rad;
                float px       = Mathf.Sin(rad) * normDist * radarRadiusPx;
                float py       = Mathf.Cos(rad) * normDist * radarRadiusPx;
                dot.rt.anchoredPosition = new Vector2(px, py);
                dot.rt.gameObject.SetActive(true);

                // Check if sweep just passed this blip's bearing
                if (AngleSweepPassed(prevAngle, currentAngle, blip.bearingDeg))
                {
                    dot.spawnTime = Time.time;
                    dot.swept     = true;
                    if (dot.img != null)
                    {
                        Color c  = blipDotColor;
                        c.a      = 1f;
                        dot.img.color = c;
                    }
                    PlayPing();
                }
            }

            // Remove dots for blips that no longer exist
            for (int i = _dots.Count - 1; i >= 0; i--)
            {
                if (!_newBlipIds.Contains(_dots[i].blipId))
                    ReturnDotToPool(_dots[i], i);
            }
        }

        private void FadeOldDots()
        {
            float now = Time.time;
            for (int i = 0; i < _dots.Count; i++)
            {
                var dot = _dots[i];
                if (!dot.swept) continue;

                float age   = now - dot.spawnTime;
                float alpha = Mathf.Clamp01(1f - age / blipPersistSeconds);

                if (dot.img != null)
                {
                    Color c = dot.img.color;
                    c.a     = alpha;
                    dot.img.color = c;
                }
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────────
        /// <summary>Returns true if the sweep crossed <paramref name="targetAngle"/> going from
        /// <paramref name="prev"/> to <paramref name="curr"/>.</summary>
        private static bool AngleSweepPassed(float prev, float curr, float targetAngle)
        {
            if (curr >= prev)
                return targetAngle >= prev && targetAngle < curr;

            // Wrapped around 360→0
            return targetAngle >= prev || targetAngle < curr;
        }

        private void PlayPing()
        {
            if (pingSoundSource != null && pingSoundSource.clip != null)
                pingSoundSource.PlayOneShot(pingSoundSource.clip);
        }

        // ── Pool helpers ──────────────────────────────────────────────────────────
        private BlipDotState AllocateDot(string blipId)
        {
            RectTransform rt = _dotPool.Count > 0
                ? _dotPool[_dotPool.Count - 1]
                : CreateDotFromPool();

            if (_dotPool.Count > 0) _dotPool.RemoveAt(_dotPool.Count - 1);

            var state = new BlipDotState
            {
                blipId    = blipId,
                rt        = rt,
                img       = rt.GetComponent<Image>(),
                spawnTime = Time.time,
                swept     = false
            };
            if (state.img != null)
            {
                Color c = blipDotColor;
                c.a     = 0f;
                state.img.color = c;
            }
            _dots.Add(state);
            _dotById[blipId] = state;
            return state;
        }

        private void ReturnDotToPool(BlipDotState dot, int listIndex)
        {
            dot.rt.gameObject.SetActive(false);
            _dotPool.Add(dot.rt);
            _dotById.Remove(dot.blipId);
            _dots.RemoveAt(listIndex);
        }

        private RectTransform CreateDotFromPool()
        {
            RectTransform rt;
            if (dotPrefab != null)
            {
                rt = Instantiate(dotPrefab, radarCanvas != null ? radarCanvas : transform as RectTransform);
            }
            else
            {
                var go = new GameObject("RadarDot", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(radarCanvas != null ? radarCanvas : transform, false);
                rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(6f, 6f);
                var img = go.GetComponent<Image>();
                if (img != null) img.color = blipDotColor;
            }
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.gameObject.SetActive(false);
            return rt;
        }

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Switches between radar sweep mode and minimap mode.
        /// <para>When <paramref name="radarMode"/> is <c>true</c> the radar panel is shown;
        /// when <c>false</c> it is hidden.</para>
        /// </summary>
        public void SetRadarMode(bool radarMode)
        {
            _radarActive = radarMode;
            if (radarRoot != null)
                radarRoot.SetActive(radarMode);
        }

        /// <summary>Returns whether the radar is currently active.</summary>
        public bool IsRadarActive => _radarActive;

        /// <summary>Sets the sweep speed in revolutions per minute.</summary>
        public void SetSweepRPM(float rpm)
        {
            sweepRPM          = Mathf.Max(0.5f, rpm);
            _degreesPerSecond = sweepRPM * 360f / 60f;
        }

        /// <summary>Sets the maximum world-unit range shown on the radar.</summary>
        public void SetRadarRange(float meters) => radarRangeMeters = Mathf.Max(100f, meters);
    }
}
