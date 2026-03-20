#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using SWEF.CloudRendering;

namespace SWEF.Editor
{
    /// <summary>
    /// Custom EditorWindow for debugging the Phase 29 Cloud Rendering &amp; Remote
    /// Streaming system.
    /// Open via <b>SWEF → Cloud Rendering Debug</b>.
    /// </summary>
    public class CloudRenderingDebugWindow : EditorWindow
    {
        // ── Menu ─────────────────────────────────────────────────────────────────
        [MenuItem("SWEF/Cloud Rendering Debug")]
        public static void ShowWindow() =>
            GetWindow<CloudRenderingDebugWindow>("SWEF Cloud Rendering Debug");

        // ── State ────────────────────────────────────────────────────────────────
        private Vector2 _scroll;

        // Network simulation
        private float _simLatencyMs    = 0f;
        private float _simPacketLoss   = 0f;
        private float _simBandwidthMbps = 50f;
        private bool  _simEnabled      = false;

        // Forced quality override
        private int   _forcedQualityIndex = 0;
        private static readonly string[] QualityNames =
            { "None (Auto)", "Ultra (4K)", "High (1440p)", "Medium (1080p)", "Low (720p)", "Min (480p)" };

        // Foldouts
        private bool _showSession   = true;
        private bool _showMetrics   = true;
        private bool _showSimulate  = true;
        private bool _showQuality   = true;

        // ── Cached scene refs ─────────────────────────────────────────────────────
        private CloudRenderingManager   _manager;
        private StreamingClient         _client;
        private NetworkQualityMonitor   _netMonitor;
        private AdaptiveBitrateController _bitrateCtrl;
        private LatencyCompensator      _latComp;
        private CloudSessionManager     _sessionMgr;
        private ServerDiscoveryService  _discovery;

        private void OnEnable() => RefreshRefs();

        private void RefreshRefs()
        {
            _manager     = FindFirstObjectByType<CloudRenderingManager>();
            _client      = FindFirstObjectByType<StreamingClient>();
            _netMonitor  = FindFirstObjectByType<NetworkQualityMonitor>();
            _bitrateCtrl = FindFirstObjectByType<AdaptiveBitrateController>();
            _latComp     = FindFirstObjectByType<LatencyCompensator>();
            _sessionMgr  = FindFirstObjectByType<CloudSessionManager>();
            _discovery   = FindFirstObjectByType<ServerDiscoveryService>();
        }

        // ── GUI ───────────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawTitle();
            EditorGUILayout.Space(4);
            DrawSessionInfo();
            EditorGUILayout.Space(4);
            DrawRealtimeMetrics();
            EditorGUILayout.Space(4);
            DrawNetworkSimulation();
            EditorGUILayout.Space(4);
            DrawQualityOverride();
            EditorGUILayout.Space(8);
            DrawActions();

            EditorGUILayout.EndScrollView();
        }

        private void DrawTitle()
        {
            EditorGUILayout.LabelField("SWEF — Cloud Rendering Debug", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Phase 29: Cloud Rendering & Remote Streaming System",
                EditorStyles.miniLabel);
            EditorGUILayout.Space(2);
            if (GUILayout.Button("Refresh Scene References", GUILayout.Height(22)))
                RefreshRefs();
        }

        private void DrawSessionInfo()
        {
            _showSession = EditorGUILayout.Foldout(_showSession, "Session Info");
            if (!_showSession) return;

            if (_manager == null)
            {
                EditorGUILayout.HelpBox("CloudRenderingManager not found in scene.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Cloud Mode:   {_manager.IsCloudMode}");
            EditorGUILayout.LabelField($"Connection:   {_manager.CurrentConnectionStatus}");

            if (_sessionMgr != null)
            {
                EditorGUILayout.LabelField($"Session ID:   {(_sessionMgr.SessionId.Length > 0 ? _sessionMgr.SessionId : "(none)")}");
                EditorGUILayout.LabelField($"Active:       {_sessionMgr.IsSessionActive}");
                EditorGUILayout.LabelField($"Server URL:   {_sessionMgr.Config.serverUrl}");
                EditorGUILayout.LabelField($"Region:       {_sessionMgr.Config.region}");
            }

            if (_discovery != null)
                EditorGUILayout.LabelField($"Best Server:  {_discovery.BestServer.region} ({_discovery.BestServer.pingMs:F0} ms)");
        }

        private void DrawRealtimeMetrics()
        {
            _showMetrics = EditorGUILayout.Foldout(_showMetrics, "Real-time Metrics");
            if (!_showMetrics) return;

            // Latency
            EditorGUILayout.LabelField("Latency", EditorStyles.boldLabel);
            if (_client != null)
            {
                float ping = _client.LatencyMs;
                Color pingColor = ping < 50f ? Color.green : (ping < 100f ? Color.yellow : Color.red);
                var prev = GUI.color;
                GUI.color = pingColor;
                EditorGUILayout.LabelField($"Ping: {ping:F0} ms");
                GUI.color = prev;
            }
            if (_latComp != null)
            {
                EditorGUILayout.LabelField($"Avg Latency (60 samples): {_latComp.CurrentLatencyMs:F1} ms");
                EditorGUILayout.LabelField($"Prediction Horizon:       {_latComp.PredictionHorizonMs:F0} ms");
                EditorGUILayout.LabelField($"Jitter Buffer:            {_latComp.JitterBufferFrames} frames");
            }

            EditorGUILayout.Space(2);

            // Network quality
            EditorGUILayout.LabelField("Network Quality", EditorStyles.boldLabel);
            if (_netMonitor != null)
            {
                DrawBar("Score", _netMonitor.NetworkScore / 100f, Color.cyan);
                EditorGUILayout.LabelField($"Category:  {_netMonitor.CurrentQuality}");
                EditorGUILayout.LabelField($"Ping:      {_netMonitor.PingMs:F0} ms");
                EditorGUILayout.LabelField($"Loss:      {_netMonitor.PacketLossPercent:F1} %");
                EditorGUILayout.LabelField($"Bandwidth: {_netMonitor.BandwidthMbps:F2} Mbps");
            }

            EditorGUILayout.Space(2);

            // Bitrate
            EditorGUILayout.LabelField("Adaptive Bitrate", EditorStyles.boldLabel);
            if (_bitrateCtrl != null)
            {
                EditorGUILayout.LabelField($"Quality:    {_bitrateCtrl.CurrentQuality}");
                EditorGUILayout.LabelField($"Bandwidth:  {_bitrateCtrl.EstimatedBandwidthMbps:F2} Mbps");
                DrawBar("Quality Level",
                    (int)_bitrateCtrl.CurrentQuality / 4f, Color.green);
            }
        }

        private void DrawNetworkSimulation()
        {
            _showSimulate = EditorGUILayout.Foldout(_showSimulate, "Simulate Network Conditions");
            if (!_showSimulate) return;

            EditorGUILayout.HelpBox(
                "Applies simulated conditions to the LatencyCompensator and NetworkQualityMonitor " +
                "for testing. Only active in Play Mode.",
                MessageType.None);

            _simEnabled = EditorGUILayout.Toggle("Enable Simulation", _simEnabled);

            using (new EditorGUI.DisabledGroupScope(!_simEnabled))
            {
                _simLatencyMs     = EditorGUILayout.Slider("Latency (ms)",        _simLatencyMs,    0f,   500f);
                _simPacketLoss    = EditorGUILayout.Slider("Packet Loss (%)",     _simPacketLoss,   0f,   20f);
                _simBandwidthMbps = EditorGUILayout.Slider("Bandwidth (Mbps)",    _simBandwidthMbps, 0.5f, 100f);
            }

            if (_simEnabled && Application.isPlaying)
            {
                _latComp?.RecordLatencySample(_simLatencyMs);
                _bitrateCtrl?.ReportPacketLoss(_simPacketLoss / 100f);
            }
        }

        private void DrawQualityOverride()
        {
            _showQuality = EditorGUILayout.Foldout(_showQuality, "Quality Override");
            if (!_showQuality) return;

            _forcedQualityIndex = EditorGUILayout.Popup("Force Quality", _forcedQualityIndex, QualityNames);

            using (new EditorGUI.DisabledGroupScope(!Application.isPlaying || _bitrateCtrl == null))
            {
                if (GUILayout.Button("Apply Override") && _forcedQualityIndex > 0 && _bitrateCtrl != null)
                {
                    var quality = (AdaptiveBitrateController.StreamQuality)(5 - _forcedQualityIndex);
                    _bitrateCtrl.ForceQuality(quality);
                    Debug.Log($"[SWEF] CloudRenderingDebug: forced quality → {quality}");
                }
            }
        }

        private void DrawActions()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledGroupScope(!Application.isPlaying || _manager == null))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Enable Cloud"))
                    _manager?.EnableCloudRendering();
                if (GUILayout.Button("Disable Cloud"))
                    _manager?.DisableCloudRendering();
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Discover Servers"))
                    _discovery?.DiscoverServers();
            }

            EditorGUILayout.HelpBox(
                "Enable Play Mode to interact with live cloud rendering components.",
                MessageType.None);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static void DrawBar(string label, float value, Color color)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(160));
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(16));
            EditorGUI.DrawRect(new Rect(r.x, r.y + 2, r.width * value, r.height - 4), color);
            EditorGUI.DrawRect(new Rect(r.x + r.width * value, r.y + 2,
                r.width * (1f - value), r.height - 4), new Color(0.2f, 0.2f, 0.2f));
            EditorGUILayout.LabelField($"{value:P0}", GUILayout.Width(45));
            EditorGUILayout.EndHorizontal();
        }

        // Repaint continuously in play mode
        private void OnInspectorUpdate()
        {
            if (Application.isPlaying) Repaint();
        }
    }
}
#endif
