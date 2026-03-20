using System;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.CloudRendering
{
    /// <summary>
    /// UI overlay that shows streaming status, quality indicator, latency, and
    /// bandwidth usage.  Displays a latency warning toast when ping exceeds 100 ms.
    /// </summary>
    public class CloudRenderingUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Status Indicator")]
        [SerializeField] private Image   statusDot;          // green/yellow/red
        [SerializeField] private Text    statusText;

        [Header("Metrics")]
        [SerializeField] private Text    latencyText;
        [SerializeField] private Text    bandwidthText;

        [Header("Quality Selector")]
        [SerializeField] private Dropdown qualityDropdown;

        [Header("Controls")]
        [SerializeField] private Button  cloudToggleButton;
        [SerializeField] private Text    cloudToggleLabel;

        [Header("Toast")]
        [SerializeField] private GameObject latencyWarningToast;
        [SerializeField] private float      toastDurationSec = 3f;
        [SerializeField] private float      latencyWarningThresholdMs = 100f;

        // ── Internal state ────────────────────────────────────────────────────────
        private CloudRenderingManager   _manager;
        private StreamingClient         _client;
        private NetworkQualityMonitor   _netMonitor;
        private AdaptiveBitrateController _bitrateCtrl;
        private float _toastTimer;
        private float _totalMbReceived;

        // ── Colour constants ──────────────────────────────────────────────────────
        private static readonly Color ColGreen  = new Color(0.18f, 0.80f, 0.44f);
        private static readonly Color ColYellow = new Color(0.95f, 0.77f, 0.06f);
        private static readonly Color ColRed    = new Color(0.91f, 0.30f, 0.24f);
        private static readonly Color ColGray   = new Color(0.50f, 0.50f, 0.50f);

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Start()
        {
            _manager     = FindFirstObjectByType<CloudRenderingManager>();
            _client      = FindFirstObjectByType<StreamingClient>();
            _netMonitor  = FindFirstObjectByType<NetworkQualityMonitor>();
            _bitrateCtrl = FindFirstObjectByType<AdaptiveBitrateController>();

            if (_manager != null)
            {
                _manager.OnConnectionStatusChanged += OnConnectionStatusChanged;
                _manager.OnCloudModeChanged        += OnCloudModeChanged;
            }
            if (_client != null)
            {
                _client.OnFrameReceived  += OnFrameReceived;
                _client.OnLatencyUpdated += OnLatencyUpdated;
            }

            cloudToggleButton?.onClick.AddListener(OnToggleCloud);
            qualityDropdown?.onValueChanged.AddListener(OnQualityDropdownChanged);

            SetupQualityDropdown();
            RefreshToggleLabel();

            if (latencyWarningToast != null)
                latencyWarningToast.SetActive(false);
        }

        private void Update()
        {
            // Update bandwidth display (rolling usage in MB/min)
            if (bandwidthText != null)
                bandwidthText.text = $"BW {_netMonitor?.BandwidthMbps ?? 0f:F1} Mbps";

            // Toast countdown
            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.deltaTime;
                if (_toastTimer <= 0f && latencyWarningToast != null)
                    latencyWarningToast.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.OnConnectionStatusChanged -= OnConnectionStatusChanged;
                _manager.OnCloudModeChanged        -= OnCloudModeChanged;
            }
            if (_client != null)
            {
                _client.OnFrameReceived  -= OnFrameReceived;
                _client.OnLatencyUpdated -= OnLatencyUpdated;
            }
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void SetupQualityDropdown()
        {
            if (qualityDropdown == null) return;
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new System.Collections.Generic.List<string>
                { "Auto", "Ultra (4K)", "High (1440p)", "Medium (1080p)", "Low (720p)", "Min (480p)" });
        }

        private void OnConnectionStatusChanged(CloudRenderingManager.ConnectionStatus status)
        {
            if (statusText != null)
                statusText.text = status.ToString();

            if (statusDot != null)
            {
                statusDot.color = status switch
                {
                    CloudRenderingManager.ConnectionStatus.Connected  => ColGreen,
                    CloudRenderingManager.ConnectionStatus.Streaming  => ColGreen,
                    CloudRenderingManager.ConnectionStatus.Connecting => ColYellow,
                    CloudRenderingManager.ConnectionStatus.Error      => ColRed,
                    _                                                  => ColGray,
                };
            }
        }

        private void OnCloudModeChanged(bool enabled)
        {
            RefreshToggleLabel();
        }

        private void OnFrameReceived(byte[] data)
        {
            _totalMbReceived += data.Length / (1024f * 1024f);
        }

        private void OnLatencyUpdated(float ms)
        {
            if (latencyText != null)
                latencyText.text = $"Ping {ms:F0} ms";

            if (ms > latencyWarningThresholdMs)
                ShowLatencyWarning();
        }

        private void ShowLatencyWarning()
        {
            if (latencyWarningToast == null) return;
            latencyWarningToast.SetActive(true);
            _toastTimer = toastDurationSec;
        }

        private void OnToggleCloud()
        {
            if (_manager == null) return;
            if (_manager.IsCloudMode)
                _manager.DisableCloudRendering();
            else
                _manager.EnableCloudRendering();
        }

        private void OnQualityDropdownChanged(int index)
        {
            if (_bitrateCtrl == null) return;
            // index 0 = Auto (no override); indices 1–5 map to Ultra…Minimum
            if (index > 0)
            {
                var quality = (AdaptiveBitrateController.StreamQuality)(5 - index); // reverse mapping
                _bitrateCtrl.ForceQuality(quality);
            }
        }

        private void RefreshToggleLabel()
        {
            if (cloudToggleLabel == null) return;
            bool isCloud = _manager != null && _manager.IsCloudMode;
            cloudToggleLabel.text = isCloud ? "Disable Cloud" : "Enable Cloud";
        }
    }
}
