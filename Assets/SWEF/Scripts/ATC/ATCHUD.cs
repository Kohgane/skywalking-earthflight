using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 78 — ATC-specific HUD overlay.
    ///
    /// <para>Displays:
    /// <list type="bullet">
    ///   <item>Active and standby radio frequencies (COMM1).</item>
    ///   <item>Current clearance with countdown timer.</item>
    ///   <item>Mini traffic radar scope showing nearby contacts as blips.</item>
    ///   <item>Scrollable communication log (maximum 50 messages).</item>
    ///   <item>TX/RX transmission indicator icons.</item>
    ///   <item>ATIS information panel.</item>
    /// </list>
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class ATCHUD : MonoBehaviour
    {
        #region Constants

        private const int MaxLogMessages = 50;

        #endregion

        #region Inspector — UI References

        [Header("Frequency Display")]
        [Tooltip("Label showing the active COMM1 frequency.")]
        [SerializeField] private Text frequencyText;

        [Tooltip("Label showing the standby COMM1 frequency.")]
        [SerializeField] private Text standbyFrequencyText;

        [Header("Clearance Panel")]
        [Tooltip("Root panel that shows the current clearance card.")]
        [SerializeField] private GameObject clearancePanel;

        [Tooltip("Label showing the clearance type.")]
        [SerializeField] private Text clearanceTypeText;

        [Tooltip("Label showing the countdown until clearance expiry.")]
        [SerializeField] private Text clearanceTimerText;

        [Header("Traffic Radar Scope")]
        [Tooltip("RectTransform of the radar scope canvas area.")]
        [SerializeField] private RectTransform trafficScope;

        [Tooltip("Prefab used to represent each traffic contact blip.")]
        [SerializeField] private GameObject blipPrefab;

        [Header("Communication Log")]
        [Tooltip("ScrollRect containing the communication log text.")]
        [SerializeField] private ScrollRect messageLog;

        [Tooltip("Text component inside the scroll rect for log content.")]
        [SerializeField] private Text messageLogText;

        [Header("Transmission Indicator")]
        [Tooltip("GameObject shown when the player is transmitting.")]
        [SerializeField] private GameObject transmitIndicator;

        [Tooltip("GameObject shown when a message is being received.")]
        [SerializeField] private GameObject receiveIndicator;

        [Header("ATIS Panel")]
        [Tooltip("Root panel for ATIS information.")]
        [SerializeField] private GameObject atisPanel;

        [Tooltip("Label showing the ATIS information string.")]
        [SerializeField] private Text atisText;

        [Header("Radar Settings")]
        [Tooltip("Display radius of the radar scope in metres (world).")]
        [SerializeField] private float radarRangeM = 37040f;  // 20 nm

        #endregion

        #region Private State

        private readonly List<string> _logMessages = new List<string>();
        private readonly List<GameObject> _blips = new List<GameObject>();
        private ATCManager _atcManager;
        private ATCRadioController _radio;
        private TrafficSimulator _traffic;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _atcManager = ATCManager.Instance ?? FindFirstObjectByType<ATCManager>();
            _radio      = FindFirstObjectByType<ATCRadioController>();
            _traffic    = FindFirstObjectByType<TrafficSimulator>();

            SubscribeEvents();
            RefreshFrequencyDisplay();

            if (clearancePanel != null)
                clearancePanel.SetActive(false);
        }

        private void Update()
        {
            UpdateClearanceTimer();
            UpdateTrafficScope();
            UpdateTransmitIndicator();
        }

        private void OnDestroy() => UnsubscribeEvents();

        #endregion

        #region Public API

        /// <summary>Appends a message to the communication log.</summary>
        /// <param name="message">Message text to log.</param>
        public void LogMessage(string message)
        {
            _logMessages.Add($"[{System.DateTime.Now:HH:mm:ss}] {message}");
            if (_logMessages.Count > MaxLogMessages)
                _logMessages.RemoveAt(0);

            if (messageLogText != null)
                messageLogText.text = string.Join("\n", _logMessages);

            if (messageLog != null)
            {
                Canvas.ForceUpdateCanvases();
                messageLog.verticalNormalizedPosition = 0f;  // scroll to bottom
            }
        }

        /// <summary>Sets the ATIS information displayed in the ATIS panel.</summary>
        /// <param name="info">ATIS broadcast text.</param>
        public void SetATISInfo(string info)
        {
            if (atisText != null)   atisText.text = info;
            if (atisPanel != null)  atisPanel.SetActive(!string.IsNullOrEmpty(info));
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeEvents()
        {
            if (_atcManager == null) return;
            _atcManager.OnClearanceReceived  += HandleClearanceReceived;
            _atcManager.OnClearanceExpired   += HandleClearanceExpired;
            _atcManager.OnFrequencyChanged   += HandleFrequencyChanged;

            if (_radio != null)
            {
                _radio.OnMessageReceived     += LogMessage;
                _radio.OnTransmissionStarted += HandleTransmitStarted;
                _radio.OnTransmissionEnded   += HandleTransmitEnded;
            }
        }

        private void UnsubscribeEvents()
        {
            if (_atcManager != null)
            {
                _atcManager.OnClearanceReceived  -= HandleClearanceReceived;
                _atcManager.OnClearanceExpired   -= HandleClearanceExpired;
                _atcManager.OnFrequencyChanged   -= HandleFrequencyChanged;
            }

            if (_radio != null)
            {
                _radio.OnMessageReceived     -= LogMessage;
                _radio.OnTransmissionStarted -= HandleTransmitStarted;
                _radio.OnTransmissionEnded   -= HandleTransmitEnded;
            }
        }

        #endregion

        #region Event Handlers

        private void HandleClearanceReceived(ATCInstruction instr)
        {
            if (clearancePanel != null) clearancePanel.SetActive(true);
            if (clearanceTypeText != null)
                clearanceTypeText.text = instr.clearanceType.ToString();
            LogMessage($"Clearance: {instr.clearanceType}");
        }

        private void HandleClearanceExpired(ATCInstruction instr)
        {
            if (clearancePanel != null) clearancePanel.SetActive(false);
        }

        private void HandleFrequencyChanged(RadioFrequency freq)
        {
            RefreshFrequencyDisplay();
        }

        private void HandleTransmitStarted(string message)
        {
            if (transmitIndicator != null) transmitIndicator.SetActive(true);
        }

        private void HandleTransmitEnded()
        {
            if (transmitIndicator != null) transmitIndicator.SetActive(false);
        }

        #endregion

        #region Update Helpers

        private void RefreshFrequencyDisplay()
        {
            if (_atcManager == null) return;
            if (frequencyText != null && _atcManager.ActiveFrequency != null)
                frequencyText.text = _atcManager.ActiveFrequency.ToString();
            if (standbyFrequencyText != null && _atcManager.StandbyFrequency != null)
                standbyFrequencyText.text = _atcManager.StandbyFrequency.ToString();
        }

        private void UpdateClearanceTimer()
        {
            if (_atcManager == null || clearanceTimerText == null) return;
            var instr = _atcManager.CurrentClearance;
            if (instr == null || instr.expirationTime <= 0f)
            {
                clearanceTimerText.text = string.Empty;
                return;
            }
            float remaining = instr.expirationTime - Time.time;
            if (remaining <= 0f)
            {
                clearanceTimerText.text = "EXPIRED";
                return;
            }
            clearanceTimerText.text = $"{Mathf.CeilToInt(remaining):D3}s";
        }

        private void UpdateTrafficScope()
        {
            if (trafficScope == null || _traffic == null) return;

            // Remove old blips
            foreach (var b in _blips)
                if (b != null) Destroy(b);
            _blips.Clear();

            if (blipPrefab == null) return;

            Vector3 playerPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            var nearby = _traffic.GetNearbyTraffic(playerPos, radarRangeM);
            float scopeRadius = trafficScope.rect.width * 0.5f;

            foreach (var contact in nearby)
            {
                Vector3 delta = contact.position - playerPos;
                float normX = delta.x / radarRangeM;
                float normZ = delta.z / radarRangeM;

                var blip = Instantiate(blipPrefab, trafficScope);
                var rt   = blip.GetComponent<RectTransform>();
                if (rt != null)
                    rt.anchoredPosition = new Vector2(normX * scopeRadius, normZ * scopeRadius);
                _blips.Add(blip);
            }
        }

        private void UpdateTransmitIndicator()
        {
            if (receiveIndicator == null || _radio == null) return;
            receiveIndicator.SetActive(!_radio.IsTransmitting && _radio.COMM1Active);
        }

        #endregion
    }
}
