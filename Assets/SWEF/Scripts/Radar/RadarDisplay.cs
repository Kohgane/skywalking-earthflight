// RadarDisplay.cs — SWEF Radar & Threat Detection System (Phase 67)
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Radar
{
    /// <summary>
    /// UI MonoBehaviour that renders the radar scope on screen.
    /// <para>
    /// Supports three display modes — Plan Position Indicator (PPI),
    /// B-Scope (range/azimuth), and Forward-Looking (elevation/azimuth).
    /// Updates blip positions every frame from <see cref="RadarSystem"/> contacts.
    /// </para>
    /// </summary>
    public class RadarDisplay : MonoBehaviour
    {
        /// <summary>Visual layout mode of the radar scope.</summary>
        public enum RadarDisplayMode
        {
            /// <summary>Standard top-down plan-position indicator with a rotating sweep line.</summary>
            PlanPosition,
            /// <summary>B-Scope — azimuth on X-axis, range on Y-axis.</summary>
            BScope,
            /// <summary>Forward-looking — azimuth on X-axis, elevation on Y-axis.</summary>
            ForwardLooking
        }

        #region Inspector

        [Header("Radar Display — Mode")]
        [Tooltip("Current display layout.")]
        /// <summary>Current display layout mode.</summary>
        public RadarDisplayMode displayMode = RadarDisplayMode.PlanPosition;

        [Header("Radar Display — Panels")]
        [Tooltip("Root RectTransform of the radar scope panel.")]
        /// <summary>Root panel of the radar scope.</summary>
        public RectTransform radarPanel;

        [Tooltip("The rotating sweep line image (PPI mode only).")]
        /// <summary>Rotating sweep-line transform (PPI mode only).</summary>
        public RectTransform sweepLine;

        [Header("Radar Display — Sweep")]
        [Tooltip("Degrees per second the sweep line rotates.")]
        [Min(1f)]
        /// <summary>Degrees per second the sweep-line rotates in PPI mode.</summary>
        public float sweepSpeed = 60f;

        [Header("Radar Display — Zoom")]
        [Tooltip("Currently active display range in metres.")]
        /// <summary>Currently active display range (zoom level) in metres.</summary>
        public float displayRange = RadarConfig.ZoomPresets[1];

        [Tooltip("Available zoom-level presets in metres.")]
        /// <summary>Preset display-range zoom levels in metres.</summary>
        public float[] zoomLevels = { 2000f, 5000f, 10000f };

        [Tooltip("Index into zoomLevels currently active.")]
        /// <summary>Index of the currently active zoom level within <see cref="zoomLevels"/>.</summary>
        public int currentZoomIndex = 1;

        [Header("Radar Display — Blips")]
        [Tooltip("Prefab instantiated for each contact blip.  Must have a RectTransform.")]
        /// <summary>Prefab used to spawn contact blips on the display.</summary>
        public GameObject blipPrefab;

        [Header("Radar Display — HUD Labels")]
        [Tooltip("Text element showing the current display range.")]
        /// <summary>Text element displaying the active display range.</summary>
        public TextMeshProUGUI rangeLabel;

        [Tooltip("Text element showing the player heading.")]
        /// <summary>Text element displaying the current heading.</summary>
        public TextMeshProUGUI headingLabel;

        [Tooltip("North indicator RectTransform (rotated to stay north-up or track-up).")]
        /// <summary>North-indicator transform.</summary>
        public RectTransform northIndicator;

        [Header("Radar Display — Locked-Target Blink")]
        [Tooltip("Rate in Hz at which the locked-target ring blinks.")]
        [Min(0.5f)]
        /// <summary>Hz rate at which the lock ring blinks.</summary>
        public float lockBlinkRate = 2f;

        [Header("Radar Display — Player Reference")]
        [Tooltip("Player transform used for bearing calculations.  Auto-resolved on Start if null.")]
        /// <summary>Player transform used for relative bearing and heading calculations.</summary>
        [SerializeField] private Transform _playerTransform;

        #endregion

        #region Runtime State

        /// <summary>
        /// Active blip RectTransforms keyed by <see cref="RadarContact.contactId"/>.
        /// </summary>
        public Dictionary<string, RectTransform> blipInstances { get; } =
            new Dictionary<string, RectTransform>();

        private RadarSystem _radar;
        private float       _sweepAngle;
        private float       _blinkTimer;
        private bool        _blinkState;

        // Track which contacts have blips so we can remove stale ones.
        private readonly HashSet<string> _activeContactIds = new HashSet<string>();

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _radar = RadarSystem.Instance != null ? RadarSystem.Instance : FindFirstObjectByType<RadarSystem>();
            if (_playerTransform == null) _playerTransform = transform;

            if (_radar != null)
            {
                _radar.OnContactLost += HandleContactLost;
            }

            ApplyZoom();
        }

        private void OnDestroy()
        {
            if (_radar != null) _radar.OnContactLost -= HandleContactLost;
        }

        private void Update()
        {
            UpdateSweepLine();
            UpdateBlips();
            UpdateLabels();
            UpdateBlinkState();
        }

        #endregion

        #region Public API

        /// <summary>Advances to the next zoom level preset, wrapping around.</summary>
        public void CycleZoom()
        {
            if (zoomLevels == null || zoomLevels.Length == 0) return;
            currentZoomIndex = (currentZoomIndex + 1) % zoomLevels.Length;
            ApplyZoom();
        }

        /// <summary>Sets the radar display to the specified layout mode.</summary>
        /// <param name="mode">New display mode.</param>
        public void SetDisplayMode(RadarDisplayMode mode)
        {
            displayMode = mode;

            if (sweepLine != null)
                sweepLine.gameObject.SetActive(mode == RadarDisplayMode.PlanPosition);
        }

        #endregion

        #region Private — Sweep Line

        private void UpdateSweepLine()
        {
            if (displayMode != RadarDisplayMode.PlanPosition) return;
            if (sweepLine == null) return;

            _sweepAngle -= sweepSpeed * Time.deltaTime;
            if (_sweepAngle < -360f) _sweepAngle += 360f;
            sweepLine.localRotation = Quaternion.Euler(0f, 0f, _sweepAngle);
        }

        #endregion

        #region Private — Blips

        private void UpdateBlips()
        {
            if (_radar == null || radarPanel == null || blipPrefab == null) return;

            _activeContactIds.Clear();
            foreach (RadarContact c in _radar.contacts)
                _activeContactIds.Add(c.contactId);

            // Remove blips for lost contacts.
            var toRemove = new List<string>();
            foreach (var kv in blipInstances)
            {
                if (!_activeContactIds.Contains(kv.Key))
                    toRemove.Add(kv.Key);
            }
            foreach (string id in toRemove) RemoveBlip(id);

            // Create / update blips.
            foreach (RadarContact c in _radar.contacts)
            {
                if (!blipInstances.TryGetValue(c.contactId, out RectTransform blipRT))
                    blipRT = CreateBlip(c);

                PositionBlip(blipRT, c);
                ColorBlip(blipRT, c);
                ApplyLockEffect(blipRT, c);
            }
        }

        private RectTransform CreateBlip(RadarContact contact)
        {
            GameObject go = Instantiate(blipPrefab, radarPanel);
            go.name = $"Blip_{contact.contactId}";
            RectTransform rt = go.GetComponent<RectTransform>();
            blipInstances[contact.contactId] = rt;
            return rt;
        }

        private void RemoveBlip(string id)
        {
            if (blipInstances.TryGetValue(id, out RectTransform rt))
            {
                if (rt != null) Destroy(rt.gameObject);
                blipInstances.Remove(id);
            }
        }

        private void PositionBlip(RectTransform blipRT, RadarContact contact)
        {
            if (radarPanel == null) return;
            float panelRadius = radarPanel.rect.width * 0.5f;

            Vector2 pos;
            switch (displayMode)
            {
                case RadarDisplayMode.BScope:
                {
                    // X = bearing (−180 to +180 mapped to −panelRadius..+panelRadius)
                    // Y = range (0 at top, max at bottom)
                    float bearingNorm = (contact.bearing > 180f ? contact.bearing - 360f : contact.bearing) / 180f;
                    float rangeFrac   = 1f - Mathf.Clamp01(contact.distance / displayRange);
                    pos = new Vector2(bearingNorm * panelRadius,
                                      (rangeFrac - 0.5f) * radarPanel.rect.height);
                    break;
                }
                case RadarDisplayMode.ForwardLooking:
                {
                    float bearingNorm = (contact.bearing > 180f ? contact.bearing - 360f : contact.bearing) / 180f;
                    float elevNorm    = Mathf.Clamp(contact.elevation, -60f, 60f) / 60f;
                    pos = new Vector2(bearingNorm * panelRadius, elevNorm * panelRadius);
                    break;
                }
                default: // PlanPosition
                {
                    float bearingRad  = contact.bearing * Mathf.Deg2Rad;
                    float rangeFrac   = Mathf.Clamp01(contact.distance / displayRange);
                    pos = new Vector2(
                        Mathf.Sin(bearingRad) * rangeFrac * panelRadius,
                        Mathf.Cos(bearingRad) * rangeFrac * panelRadius);
                    break;
                }
            }

            blipRT.anchoredPosition = pos;
        }

        private void ColorBlip(RectTransform blipRT, RadarContact contact)
        {
            Image img = blipRT.GetComponent<Image>();
            if (img == null) return;
            img.color = RadarConfig.GetClassificationColor(contact.classification);
        }

        private void ApplyLockEffect(RectTransform blipRT, RadarContact contact)
        {
            // Find or create the lock-ring child image.
            Transform lockRing = blipRT.Find("LockRing");
            if (lockRing == null) return;

            lockRing.gameObject.SetActive(contact.isLocked);

            if (contact.isLocked)
            {
                Image ringImg = lockRing.GetComponent<Image>();
                if (ringImg != null)
                    ringImg.enabled = _blinkState;
            }
        }

        private void HandleContactLost(RadarContact c)
        {
            RemoveBlip(c.contactId);
        }

        #endregion

        #region Private — Labels

        private void UpdateLabels()
        {
            if (rangeLabel != null)
                rangeLabel.text = $"{displayRange * 0.001f:F0} km";

            if (headingLabel != null && _playerTransform != null)
            {
                float hdg = _playerTransform.eulerAngles.y;
                headingLabel.text = $"{hdg:F0}°";

                if (northIndicator != null)
                    northIndicator.localRotation = Quaternion.Euler(0f, 0f, hdg);
            }
        }

        #endregion

        #region Private — Helpers

        private void ApplyZoom()
        {
            if (zoomLevels != null && zoomLevels.Length > 0)
                displayRange = zoomLevels[Mathf.Clamp(currentZoomIndex, 0, zoomLevels.Length - 1)];
        }

        private void UpdateBlinkState()
        {
            _blinkTimer += Time.deltaTime;
            if (_blinkTimer >= 1f / lockBlinkRate)
            {
                _blinkTimer = 0f;
                _blinkState = !_blinkState;
            }
        }

        #endregion
    }
}
