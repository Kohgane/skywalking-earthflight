using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Replay
{
    /// <summary>
    /// Phase 48 — HUD overlay that renders real-time flight analytics during
    /// replay: speed/altitude line graphs, G-force display, control-input
    /// visualisation, and a statistics panel.
    /// All UI panels are independently toggleable.
    /// </summary>
    public class FlightAnalyticsOverlay : MonoBehaviour
    {
        #region Constants

        private const int   GraphResolution   = 256;    // horizontal data points
        private const float GraphUpdateHz     = 20f;
        private const float GForceGravity     = 9.81f;

        // Colour thresholds
        private const float SpeedCautionKts   = 250f;
        private const float SpeedDangerKts    = 400f;
        private const float AltCautionM       = 8000f;
        private const float AltDangerM        = 12000f;
        private const float GCautionForce     = 4f;
        private const float GDangerForce      = 6f;

        private static readonly Color ColorNormal  = new Color(0.2f, 0.9f, 0.3f);
        private static readonly Color ColorCaution = new Color(1f,   0.8f, 0.0f);
        private static readonly Color ColorDanger  = new Color(0.9f, 0.2f, 0.1f);

        #endregion

        #region Inspector — Panels

        [Header("Panel Roots (toggle visibility)")]
        [SerializeField] private GameObject speedGraphPanel;
        [SerializeField] private GameObject altGraphPanel;
        [SerializeField] private GameObject gForcePanel;
        [SerializeField] private GameObject controlsPanel;
        [SerializeField] private GameObject statsPanel;

        [Header("Toggle Button")]
        [SerializeField] private Button overlayToggleButton;

        #endregion

        #region Inspector — Speed Graph

        [Header("Speed Graph")]
        [SerializeField] private RawImage speedGraphImage;
        [SerializeField] private Text     speedValueText;

        #endregion

        #region Inspector — Altitude Graph

        [Header("Altitude Graph")]
        [SerializeField] private RawImage altGraphImage;
        [SerializeField] private Text     altValueText;

        #endregion

        #region Inspector — G-Force

        [Header("G-Force")]
        [SerializeField] private Text  gForceValueText;
        [SerializeField] private Image gForceIndicator;

        #endregion

        #region Inspector — Controls

        [Header("Control Inputs")]
        [SerializeField] private RectTransform stickIndicator;   // child of a joystick background
        [SerializeField] private Slider        throttleSlider;
        [SerializeField] private Image         rudderBar;

        #endregion

        #region Inspector — Statistics

        [Header("Statistics Panel")]
        [SerializeField] private Text maxSpeedText;
        [SerializeField] private Text maxAltText;
        [SerializeField] private Text totalDistText;
        [SerializeField] private Text avgSpeedText;
        [SerializeField] private Text flightTimeText;

        #endregion

        #region Private State

        private FlightPlaybackController _playback;
        private bool                     _overlayVisible = true;

        // Scrolling graph buffers
        private float[] _speedHistory   = new float[GraphResolution];
        private float[] _altHistory     = new float[GraphResolution];
        private int     _historyHead;

        private Texture2D _speedTex;
        private Texture2D _altTex;

        private float _lastGraphUpdate;
        private float _prevVelocityY;
        private float _currentGForce;

        // Sticks range for normalised display
        private RectTransform _stickBounds;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _playback = FindFirstObjectByType<FlightPlaybackController>();

            _speedTex = CreateGraphTexture();
            _altTex   = CreateGraphTexture();

            if (speedGraphImage != null) speedGraphImage.texture = _speedTex;
            if (altGraphImage   != null) altGraphImage.texture   = _altTex;

            if (stickIndicator != null)
                _stickBounds = stickIndicator.parent as RectTransform;
        }

        private void OnEnable()
        {
            if (overlayToggleButton != null)
                overlayToggleButton.onClick.AddListener(ToggleOverlay);
        }

        private void OnDisable()
        {
            if (overlayToggleButton != null)
                overlayToggleButton.onClick.RemoveListener(ToggleOverlay);
        }

        private void Update()
        {
            if (_playback == null || !_overlayVisible) return;

            FlightFrame frame = GetCurrentFrame();
            if (frame == null) return;

            UpdateGraphs(frame);
            UpdateGForce(frame);
            UpdateControls(frame);
        }

        private void OnDestroy()
        {
            if (_speedTex != null) Destroy(_speedTex);
            if (_altTex   != null) Destroy(_altTex);
        }

        #endregion

        #region Public API

        /// <summary>Populates the statistics panel from a loaded recording.</summary>
        public void DisplayStatistics(FlightRecording recording)
        {
            if (recording == null) return;

            SetText(maxSpeedText,   $"{recording.maxSpeed * 1.944f:F0} kts");
            SetText(maxAltText,     $"{recording.maxAltitude:F0} m");
            SetText(totalDistText,  $"{recording.totalDistanceKm:F1} km");
            SetText(flightTimeText, FormatSeconds(recording.duration));

            // Compute average speed from distance / duration.
            float avgMps = recording.duration > 0f
                         ? (recording.totalDistanceKm * 1000f) / recording.duration
                         : 0f;
            SetText(avgSpeedText, $"{avgMps * 1.944f:F0} kts");
        }

        /// <summary>Shows or hides the entire overlay.</summary>
        public void ToggleOverlay()
        {
            _overlayVisible = !_overlayVisible;
            SetPanelsActive(_overlayVisible);
        }

        /// <summary>
        /// Individually controls visibility of a named panel.
        /// Valid names: "Speed", "Altitude", "GForce", "Controls", "Stats".
        /// </summary>
        public void SetPanelVisible(string panelName, bool visible)
        {
            switch (panelName)
            {
                case "Speed":    if (speedGraphPanel != null) speedGraphPanel.SetActive(visible); break;
                case "Altitude": if (altGraphPanel   != null) altGraphPanel.SetActive(visible);   break;
                case "GForce":   if (gForcePanel     != null) gForcePanel.SetActive(visible);     break;
                case "Controls": if (controlsPanel   != null) controlsPanel.SetActive(visible);   break;
                case "Stats":    if (statsPanel      != null) statsPanel.SetActive(visible);     break;
            }
        }

        #endregion

        #region Private — Frame Retrieval

        private FlightFrame GetCurrentFrame()
        {
            if (_playback?.ActiveRecording == null) return null;
            int idx = _playback.ActiveRecording.FindFrameIndex(_playback.CurrentTime);
            if (idx < 0 || idx >= _playback.ActiveRecording.FrameCount) return null;
            return _playback.ActiveRecording.frames[idx];
        }

        #endregion

        #region Private — Graph Updates

        private void UpdateGraphs(FlightFrame frame)
        {
            if (Time.time - _lastGraphUpdate < 1f / GraphUpdateHz) return;
            _lastGraphUpdate = Time.time;

            float speedKts = frame.speed * 1.944f;
            float altM     = frame.altitude;

            _speedHistory[_historyHead % GraphResolution] = speedKts;
            _altHistory[  _historyHead % GraphResolution] = altM;
            _historyHead++;

            RedrawGraph(_speedTex, _speedHistory, 0f, SpeedDangerKts,
                        SpeedCautionKts, SpeedDangerKts);
            RedrawGraph(_altTex,   _altHistory,   0f, AltDangerM,
                        AltCautionM, AltDangerM);

            SetText(speedValueText, $"{speedKts:F0} kts", GetRangeColor(speedKts, SpeedCautionKts, SpeedDangerKts));
            SetText(altValueText,   $"{altM:F0} m",       GetRangeColor(altM, AltCautionM, AltDangerM));
        }

        private void RedrawGraph(Texture2D tex, float[] history, float minVal, float maxVal,
                                 float cautionThreshold, float dangerThreshold)
        {
            if (tex == null) return;

            int w = tex.width, h = tex.height;
            Color[] pixels = new Color[w * h];
            Color bg = new Color(0f, 0f, 0f, 0.4f);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;

            float range = Mathf.Max(maxVal - minVal, 1f);

            for (int x = 0; x < w; x++)
            {
                int dataIdx = (_historyHead - w + x + GraphResolution * 4) % GraphResolution;
                float val   = history[dataIdx];
                float norm  = Mathf.Clamp01((val - minVal) / range);
                int   py    = Mathf.RoundToInt(norm * (h - 1));
                Color col   = GetRangeColor(val, cautionThreshold, dangerThreshold);

                for (int dy = -1; dy <= 1; dy++)
                {
                    int row = Mathf.Clamp(py + dy, 0, h - 1);
                    pixels[row * w + x] = col;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
        }

        #endregion

        #region Private — G-Force

        private void UpdateGForce(FlightFrame frame)
        {
            float accel  = (frame.velocity.y - _prevVelocityY) / Mathf.Max(Time.deltaTime, 0.001f);
            _currentGForce = (GForceGravity + accel) / GForceGravity;
            _prevVelocityY = frame.velocity.y;

            Color gCol = GetRangeColor(Mathf.Abs(_currentGForce), GCautionForce, GDangerForce);
            SetText(gForceValueText, $"{_currentGForce:F1} G", gCol);
            if (gForceIndicator != null) gForceIndicator.color = gCol;
        }

        #endregion

        #region Private — Control Visualisation

        private void UpdateControls(FlightFrame frame)
        {
            if (stickIndicator != null && _stickBounds != null)
            {
                float hw = _stickBounds.rect.width  * 0.5f;
                float hh = _stickBounds.rect.height * 0.5f;
                stickIndicator.anchoredPosition = new Vector2(frame.rollInput * hw, frame.pitchInput * hh);
            }

            if (throttleSlider != null)
                throttleSlider.SetValueWithoutNotify(frame.throttle);

            if (rudderBar != null)
            {
                var rt = rudderBar.rectTransform;
                if (rt != null)
                    rt.anchoredPosition = new Vector2(frame.yawInput * 50f, rt.anchoredPosition.y);
            }
        }

        #endregion

        #region Private — Helpers

        private void SetPanelsActive(bool active)
        {
            if (speedGraphPanel != null) speedGraphPanel.SetActive(active);
            if (altGraphPanel   != null) altGraphPanel.SetActive(active);
            if (gForcePanel     != null) gForcePanel.SetActive(active);
            if (controlsPanel   != null) controlsPanel.SetActive(active);
            if (statsPanel      != null) statsPanel.SetActive(active);
        }

        private static Texture2D CreateGraphTexture()
        {
            var tex = new Texture2D(GraphResolution, 64, TextureFormat.RGBA32, mipChain: false);
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        private static Color GetRangeColor(float value, float caution, float danger)
        {
            if (value >= danger)  return ColorDanger;
            if (value >= caution) return ColorCaution;
            return ColorNormal;
        }

        private static void SetText(Text label, string text, Color? color = null)
        {
            if (label == null) return;
            label.text  = text;
            if (color.HasValue) label.color = color.Value;
        }

        private static string FormatSeconds(float s)
        {
            int m = (int)(s / 60f);
            int sec = (int)(s % 60f);
            return $"{m:00}:{sec:00}";
        }

        #endregion
    }
}
