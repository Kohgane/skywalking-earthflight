using UnityEngine;
using UnityEngine.UI;

namespace SWEF.GPWS
{
    /// <summary>
    /// Phase 121 — HUD overlay for GPWS status, active alerts, terrain clearance,
    /// and windshear escape guidance.
    /// </summary>
    [DisallowMultipleComponent]
    public class GPWSHUD : MonoBehaviour
    {
        #region Inspector

        [Header("Status")]
        [Tooltip("Text showing GPWS system status (ARMED / OFF).")]
        [SerializeField] private Text gpwsStatusText;

        [Header("Alert Display")]
        [Tooltip("Text for the highest-priority active alert message.")]
        [SerializeField] private Text alertText;

        [Tooltip("Panel background behind the alert text.")]
        [SerializeField] private Image alertPanel;

        [Header("Terrain Clearance")]
        [Tooltip("Text showing radar altitude / terrain clearance in feet.")]
        [SerializeField] private Text terrainClearanceText;

        [Header("Windshear Escape")]
        [Tooltip("Root object for the windshear escape guidance overlay.")]
        [SerializeField] private GameObject windshearEscapePanel;

        [Tooltip("Text showing escape pitch target.")]
        [SerializeField] private Text escapePitchText;

        [Header("TAWS")]
        [Tooltip("RawImage displaying the TAWS terrain scan texture.")]
        [SerializeField] private RawImage tawsDisplay;

        #endregion

        #region Private State

        private GPWSManager _mgr;
        private WindshearDetector _windshear;
        private TerrainAwarenessDisplay _taws;

        private static readonly Color ColorPullUp  = Color.red;
        private static readonly Color ColorWarning = new Color(1f, 0.65f, 0f);
        private static readonly Color ColorCaution = Color.yellow;
        private static readonly Color ColorNormal  = new Color(0f, 0.8f, 0f);
        private static readonly Color ColorOff     = Color.gray;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _mgr = GPWSManager.Instance;
            _windshear = FindFirstObjectByType<WindshearDetector>();
            _taws = FindFirstObjectByType<TerrainAwarenessDisplay>();

            if (windshearEscapePanel != null)
                windshearEscapePanel.SetActive(false);
        }

        private void Update()
        {
            UpdateStatus();
            UpdateAlertDisplay();
            UpdateTerrainClearance();
            UpdateWindshearEscape();
            UpdateTAWSDisplay();
        }

        #endregion

        #region Display Updates

        private void UpdateStatus()
        {
            if (gpwsStatusText == null) return;
            if (_mgr == null || !_mgr.IsEnabled)
            {
                gpwsStatusText.text = "GPWS OFF";
                gpwsStatusText.color = ColorOff;
                return;
            }
            gpwsStatusText.text = "GPWS";
            gpwsStatusText.color = ColorNormal;
        }

        private void UpdateAlertDisplay()
        {
            if (alertText == null) return;
            if (_mgr == null || _mgr.ActiveAlerts.Count == 0)
            {
                alertText.text = string.Empty;
                if (alertPanel != null) alertPanel.enabled = false;
                return;
            }

            GPWSAlertLevel highest = GPWSAlertLevel.None;
            string msg = string.Empty;
            foreach (var a in _mgr.ActiveAlerts)
            {
                if (a.level > highest)
                {
                    highest = a.level;
                    msg = a.messageKey;
                }
            }

            alertText.text = msg.Replace("gpws_", "").Replace("_", " ").ToUpperInvariant();
            alertText.color = highest switch
            {
                GPWSAlertLevel.PullUp  => ColorPullUp,
                GPWSAlertLevel.Warning => ColorWarning,
                GPWSAlertLevel.Caution => ColorCaution,
                _                      => ColorNormal
            };

            if (alertPanel != null)
            {
                alertPanel.enabled = true;
                alertPanel.color = new Color(alertText.color.r, alertText.color.g, alertText.color.b, 0.25f);
            }
        }

        private void UpdateTerrainClearance()
        {
            if (terrainClearanceText == null || _mgr == null) return;
            float aglFt = _mgr.AircraftAltitudeAglFt;
            if (aglFt < 2500f)
            {
                terrainClearanceText.text = $"RA {Mathf.RoundToInt(aglFt)}";
                terrainClearanceText.color = aglFt < 500f ? ColorWarning : ColorNormal;
            }
            else
            {
                terrainClearanceText.text = string.Empty;
            }
        }

        private void UpdateWindshearEscape()
        {
            if (windshearEscapePanel == null) return;
            bool active = _windshear != null && _windshear.WindshearActive;
            windshearEscapePanel.SetActive(active);

            if (active && escapePitchText != null)
                escapePitchText.text = $"PITCH {_windshear.EscapePitchTarget:F0} UP";
        }

        private void UpdateTAWSDisplay()
        {
            if (tawsDisplay == null || _taws == null) return;
            tawsDisplay.enabled = _taws.DisplayMode != TAWSDisplayMode.Off;
        }

        #endregion
    }
}
