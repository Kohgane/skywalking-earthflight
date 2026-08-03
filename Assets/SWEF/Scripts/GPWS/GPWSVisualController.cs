using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if SWEF_COCKPITHUD_AVAILABLE
using SWEF.CockpitHUD;
#endif

namespace SWEF.GPWS
{
    /// <summary>
    /// Phase 121 — Visual warning controller for GPWS annunciations.
    ///
    /// <para>Manages the master warning light flash, GPWS annunciator panel text,
    /// terrain display coloring, and pull-up bar on the PFD. Integrates with
    /// <c>SWEF.CockpitHUD.WarningSystem</c> when available.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class GPWSVisualController : MonoBehaviour
    {
        #region Inspector

        [Header("Master Warning")]
        [Tooltip("Image that flashes as the master warning light.")]
        [SerializeField] private Image masterWarningLight;
        [SerializeField] private float flashRate = 2f;

        [Header("Annunciator Panel")]
        [Tooltip("Text label showing the active GPWS annunciation.")]
        [SerializeField] private Text annunciatorText;

        [Header("Pull-Up Bar")]
        [Tooltip("GameObject for the PFD pull-up command bar.")]
        [SerializeField] private GameObject pullUpBar;

        [Header("GPWS Status")]
        [Tooltip("Text label showing overall GPWS system status.")]
        [SerializeField] private Text statusText;

        #endregion

        #region Private State

        private GPWSManager _mgr;
        private bool _flashing;
        private float _flashTimer;
        private GPWSAlertLevel _currentHighestLevel;

        private static readonly Color ColorCaution  = new Color(1f, 0.65f, 0f);
        private static readonly Color ColorWarning  = Color.red;
        private static readonly Color ColorNormal   = new Color(0f, 0.8f, 0f);

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _mgr = GPWSManager.Instance;
            if (_mgr == null) return;

            _mgr.OnGPWSAlert        += HandleAlert;
            _mgr.OnGPWSAlertCleared  += HandleAlertCleared;

            SetPullUpBar(false);
            SetMasterWarning(false);
            UpdateStatusText();
        }

        private void Update()
        {
            if (!_flashing) return;

            _flashTimer += Time.deltaTime * flashRate;
            bool on = Mathf.Sin(_flashTimer * Mathf.PI * 2f) > 0f;
            if (masterWarningLight != null)
                masterWarningLight.enabled = on;
        }

        private void OnDestroy()
        {
            if (_mgr != null)
            {
                _mgr.OnGPWSAlert        -= HandleAlert;
                _mgr.OnGPWSAlertCleared  -= HandleAlertCleared;
            }
        }

        #endregion

        #region Event Handlers

        private void HandleAlert(GPWSAlert alert)
        {
            RefreshDisplay();
            PushToWarningSystem(alert);
        }

        private void HandleAlertCleared(GPWSAlert alert)
        {
            RefreshDisplay();
            ClearFromWarningSystem(alert);
        }

        #endregion

        #region Display

        private void RefreshDisplay()
        {
            if (_mgr == null) return;

            var alerts = _mgr.ActiveAlerts;
            _currentHighestLevel = GPWSAlertLevel.None;
            string annunciation = string.Empty;

            foreach (var a in alerts)
            {
                if (a.level > _currentHighestLevel)
                {
                    _currentHighestLevel = a.level;
                    annunciation = a.messageKey;
                }
            }

            bool hasPullUp = _currentHighestLevel == GPWSAlertLevel.PullUp;
            SetPullUpBar(hasPullUp);
            SetMasterWarning(_currentHighestLevel >= GPWSAlertLevel.Warning);

            if (annunciatorText != null)
            {
                annunciatorText.text = _currentHighestLevel == GPWSAlertLevel.None
                    ? string.Empty
                    : annunciation.Replace("gpws_", "").Replace("_", " ").ToUpperInvariant();

                annunciatorText.color = _currentHighestLevel >= GPWSAlertLevel.Warning
                    ? ColorWarning
                    : ColorCaution;
            }

            UpdateStatusText();
        }

        private void SetPullUpBar(bool active)
        {
            if (pullUpBar != null)
                pullUpBar.SetActive(active);
        }

        private void SetMasterWarning(bool active)
        {
            _flashing = active;
            if (!active && masterWarningLight != null)
                masterWarningLight.enabled = false;
        }

        private void UpdateStatusText()
        {
            if (statusText == null) return;
            if (_mgr == null || !_mgr.IsEnabled)
            {
                statusText.text = "GPWS OFF";
                statusText.color = ColorWarning;
                return;
            }
            statusText.text = _currentHighestLevel == GPWSAlertLevel.None ? "GPWS" : "GPWS WARN";
            statusText.color = _currentHighestLevel == GPWSAlertLevel.None ? ColorNormal : ColorWarning;
        }

        #endregion

        #region WarningSystem Integration

#if SWEF_COCKPITHUD_AVAILABLE
        private void PushToWarningSystem(GPWSAlert alert)
        {
            var ws = FindFirstObjectByType<WarningSystem>();
            if (ws == null) return;

            var wl = alert.level switch
            {
                GPWSAlertLevel.PullUp  => WarningLevel.Critical,
                GPWSAlertLevel.Warning => WarningLevel.Warning,
                GPWSAlertLevel.Caution => WarningLevel.Caution,
                _                      => WarningLevel.Info
            };
            ws.AddWarning($"GPWS_{alert.mode}", alert.messageKey, wl);
        }

        private void ClearFromWarningSystem(GPWSAlert alert)
        {
            var ws = FindFirstObjectByType<WarningSystem>();
            ws?.ClearWarning($"GPWS_{alert.mode}");
        }
#else
        private void PushToWarningSystem(GPWSAlert alert) { }
        private void ClearFromWarningSystem(GPWSAlert alert) { }
#endif

        #endregion
    }
}
