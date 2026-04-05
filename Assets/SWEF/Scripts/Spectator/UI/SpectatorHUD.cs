// SpectatorHUD.cs — SWEF Phase 107: Live Streaming & Spectator Mode
using System.Collections.Generic;
using UnityEngine;

#if SWEF_TMPRO_AVAILABLE
using TMPro;
#endif

namespace SWEF.Spectator
{
    /// <summary>
    /// Phase 107 — Head-Up Display for Spectator Mode.
    ///
    /// <para>Displays:</para>
    /// <list type="bullet">
    ///   <item>Target aircraft info card (name, altitude, speed, heading).</item>
    ///   <item>Active camera mode indicator.</item>
    ///   <item>Participant list sidebar.</item>
    ///   <item>Event notification feed.</item>
    ///   <item>Stream info panel (viewer count, uptime).</item>
    /// </list>
    ///
    /// <para>Bind UI element references via the Inspector. All text fields accept
    /// Unity UI <c>Text</c> or TMPro <c>TextMeshProUGUI</c> components depending
    /// on the <c>SWEF_TMPRO_AVAILABLE</c> compile symbol.</para>
    /// </summary>
    public sealed class SpectatorHUD : MonoBehaviour
    {
        // ── Inspector — target info card ──────────────────────────────────────
        [Header("Target Info Card")]
        [SerializeField] private GameObject targetInfoCard;

#if SWEF_TMPRO_AVAILABLE
        [SerializeField] private TextMeshProUGUI targetNameLabel;
        [SerializeField] private TextMeshProUGUI altitudeLabel;
        [SerializeField] private TextMeshProUGUI speedLabel;
        [SerializeField] private TextMeshProUGUI headingLabel;
        // ── camera mode indicator
        [Header("Camera Mode")]
        [SerializeField] private TextMeshProUGUI cameraModeLabel;
        // ── stream info
        [Header("Stream Info")]
        [SerializeField] private TextMeshProUGUI viewerCountLabel;
        [SerializeField] private TextMeshProUGUI uptimeLabel;
        // ── event feed
        [Header("Event Feed")]
        [SerializeField] private TextMeshProUGUI eventFeedLabel;
#else
        [SerializeField] private UnityEngine.UI.Text targetNameLabel;
        [SerializeField] private UnityEngine.UI.Text altitudeLabel;
        [SerializeField] private UnityEngine.UI.Text speedLabel;
        [SerializeField] private UnityEngine.UI.Text headingLabel;
        [Header("Camera Mode")]
        [SerializeField] private UnityEngine.UI.Text cameraModeLabel;
        [Header("Stream Info")]
        [SerializeField] private UnityEngine.UI.Text viewerCountLabel;
        [SerializeField] private UnityEngine.UI.Text uptimeLabel;
        [Header("Event Feed")]
        [SerializeField] private UnityEngine.UI.Text eventFeedLabel;
#endif

        [Header("Participant Sidebar")]
        [SerializeField] private GameObject participantListRoot;
        [SerializeField] private GameObject participantEntryPrefab;

        // ── Inspector — event feed settings ──────────────────────────────────
        [Header("Event Feed")]
        [SerializeField] private int maxEventFeedLines = 6;

        // ── Internal state ─────────────────────────────────────────────────────
        private readonly Queue<string> _eventLines = new Queue<string>();

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            RefreshTargetCard();
            RefreshStreamInfo();
        }

        // ── Subscription helpers ───────────────────────────────────────────────

        private void SubscribeToEvents()
        {
            var spectator = SpectatorModeController.Instance;
            if (spectator != null)
            {
                spectator.OnCameraModeChanged += HandleCameraModeChanged;
                spectator.OnTargetChanged     += HandleTargetChanged;
                spectator.OnSpectatorModeEntered += HandleSpectatorEntered;
                spectator.OnSpectatorModeExited  += HandleSpectatorExited;
            }

            var commentator = CommentatorController.Instance;
            if (commentator != null)
                commentator.OnEventMarked += HandleEventMarked;
        }

        private void UnsubscribeFromEvents()
        {
            var spectator = SpectatorModeController.Instance;
            if (spectator != null)
            {
                spectator.OnCameraModeChanged -= HandleCameraModeChanged;
                spectator.OnTargetChanged     -= HandleTargetChanged;
                spectator.OnSpectatorModeEntered -= HandleSpectatorEntered;
                spectator.OnSpectatorModeExited  -= HandleSpectatorExited;
            }

            var commentator = CommentatorController.Instance;
            if (commentator != null)
                commentator.OnEventMarked -= HandleEventMarked;
        }

        // ── Per-frame refresh ──────────────────────────────────────────────────

        private void RefreshTargetCard()
        {
            var spectator = SpectatorModeController.Instance;
            if (spectator == null || spectator.CurrentTarget == null)
            {
                if (targetInfoCard != null) targetInfoCard.SetActive(false);
                return;
            }

            if (targetInfoCard != null) targetInfoCard.SetActive(true);

#if SWEF_FLIGHT_AVAILABLE
            var fc = spectator.CurrentTarget.GetComponent<SWEF.Flight.FlightController>();
            if (fc != null)
            {
                SetText(altitudeLabel, $"{fc.CurrentAltitudeMetres:F0} m");
                SetText(speedLabel,    $"{fc.CurrentSpeedKph:F0} kph");
                SetText(headingLabel,  $"{fc.CurrentHeadingDegrees:F0}°");
            }
#endif
            SetText(targetNameLabel, spectator.CurrentTarget.name);
        }

        private void RefreshStreamInfo()
        {
            var streaming = StreamingIntegrationManager.Instance;
            if (streaming == null) return;

            if (viewerCountLabel != null)
                SetText(viewerCountLabel, $"👁 {streaming.ViewerCount:N0}");

            if (uptimeLabel != null)
                SetText(uptimeLabel, FormatDuration(streaming.StreamUptime));
        }

        /// <summary>Formats a duration in seconds as <c>HH:MM:SS</c>.</summary>
        private static string FormatDuration(float totalSeconds)
        {
            int s = Mathf.FloorToInt(totalSeconds);
            return $"{s / 3600:D2}:{(s % 3600) / 60:D2}:{s % 60:D2}";
        }

        // ── Event handlers ─────────────────────────────────────────────────────

        private void HandleCameraModeChanged(SpectatorCameraMode mode)
        {
            SetText(cameraModeLabel, mode.ToString());
        }

        private void HandleTargetChanged(UnityEngine.Transform target)
        {
            SetText(targetNameLabel, target != null ? target.name : "—");
        }

        private void HandleSpectatorEntered()
        {
            gameObject.SetActive(true);
        }

        private void HandleSpectatorExited()
        {
            gameObject.SetActive(false);
        }

        private void HandleEventMarked(FlightEventMarker marker)
        {
            string line = $"[{marker.eventType}] {marker.description}";
            _eventLines.Enqueue(line);
            while (_eventLines.Count > maxEventFeedLines) _eventLines.Dequeue();

            if (eventFeedLabel != null)
                SetText(eventFeedLabel, string.Join("\n", _eventLines));
        }

        // ── Utility ────────────────────────────────────────────────────────────

#if SWEF_TMPRO_AVAILABLE
        private static void SetText(TextMeshProUGUI label, string text)
        {
            if (label != null) label.text = text;
        }
#else
        private static void SetText(UnityEngine.UI.Text label, string text)
        {
            if (label != null) label.text = text;
        }
#endif
    }
}
