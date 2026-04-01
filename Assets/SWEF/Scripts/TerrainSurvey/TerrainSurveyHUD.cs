// TerrainSurveyHUD.cs — SWEF Terrain Scanning & Geological Survey System
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.TerrainSurvey
{
    /// <summary>
    /// HUD overlay panel showing real-time scan status, terrain classification,
    /// altitude/slope readouts, a 5-button survey mode selector, POI discovery
    /// toast notifications, and a cooldown progress bar.
    /// </summary>
    public class TerrainSurveyHUD : MonoBehaviour
    {
        // ── Inspector — panels ────────────────────────────────────────────────────
        [Header("Scan Indicator")]
        [SerializeField] private Image       scanIndicatorIcon;
        [SerializeField] private float       pulsePeriod = 1.2f;

        [Header("Classification Readout")]
        [SerializeField] private Text        featureLabel;
        [SerializeField] private Text        altitudeLabel;
        [SerializeField] private Text        slopeLabel;

        [Header("Scan Status")]
        [SerializeField] private Text        statusLabel;

        [Header("Survey Mode Selector")]
        [SerializeField] private Button[]    modeButtons;   // length 5, ordered by SurveyMode

        [Header("Cooldown")]
        [SerializeField] private Slider      cooldownBar;

        [Header("Discovery Counter")]
        [SerializeField] private Text        discoveryCountLabel;

        [Header("Toast")]
        [SerializeField] private GameObject  toastPanel;
        [SerializeField] private Text        toastLabel;
        [SerializeField] private float       toastDuration = 3f;

        // ── State ─────────────────────────────────────────────────────────────────
        private int       _totalDiscoveries;
        private Coroutine _toastRoutine;
        private bool      _pulseDirection = true;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void OnEnable()
        {
            SubscribeEvents();
            UpdateModeButtons(SurveyMode.Altitude);
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            UpdateCooldownBar();
            PulseScanIcon();
        }

        // ── Event bindings ────────────────────────────────────────────────────────

        private void SubscribeEvents()
        {
            if (TerrainScannerController.Instance != null)
            {
                TerrainScannerController.Instance.OnScanStarted   += OnScanStarted;
                TerrainScannerController.Instance.OnScanCompleted += OnScanCompleted;
                TerrainScannerController.Instance.OnScanPaused    += OnScanPaused;
            }

            if (SurveyPOIManager.Instance != null)
                SurveyPOIManager.Instance.OnPOIDiscovered += OnPOIDiscovered;
        }

        private void UnsubscribeEvents()
        {
            if (TerrainScannerController.Instance != null)
            {
                TerrainScannerController.Instance.OnScanStarted   -= OnScanStarted;
                TerrainScannerController.Instance.OnScanCompleted -= OnScanCompleted;
                TerrainScannerController.Instance.OnScanPaused    -= OnScanPaused;
            }

            if (SurveyPOIManager.Instance != null)
                SurveyPOIManager.Instance.OnPOIDiscovered -= OnPOIDiscovered;
        }

        // ── UI update methods ─────────────────────────────────────────────────────

        private void OnScanStarted()
        {
            if (statusLabel != null)
                statusLabel.text = "survey_hud_scanning"; // resolved by localization at runtime
        }

        private void OnScanCompleted(SurveySample[] samples)
        {
            if (statusLabel != null)
                statusLabel.text = "survey_hud_idle";

            if (samples == null || samples.Length == 0) return;

            // Show classification from the central (most representative) sample
            SurveySample centre = samples[samples.Length / 2];

            if (featureLabel != null)
                featureLabel.text = GeologicalClassifier.GetFeatureDisplayName(centre.featureType);

            if (altitudeLabel != null)
                altitudeLabel.text = $"{centre.altitude:F0} m";

            if (slopeLabel != null)
                slopeLabel.text = $"{centre.slope:F1}°";
        }

        private void OnScanPaused()
        {
            if (statusLabel != null)
                statusLabel.text = "survey_hud_idle";
        }

        private void OnPOIDiscovered(SurveyPOI poi)
        {
            _totalDiscoveries++;

            if (discoveryCountLabel != null)
                discoveryCountLabel.text = _totalDiscoveries.ToString();

            ShowToast(poi.nameLocKey);
        }

        private void UpdateCooldownBar()
        {
            if (cooldownBar == null || TerrainScannerController.Instance == null) return;

            float cfg     = TerrainScannerController.Instance.CooldownRemaining;
            float maxCd   = 5f; // fallback; ideally read from config
            cooldownBar.value = 1f - Mathf.Clamp01(cfg / maxCd);
        }

        private void PulseScanIcon()
        {
            if (scanIndicatorIcon == null) return;
            if (TerrainScannerController.Instance == null || !TerrainScannerController.Instance.IsScanning)
            {
                scanIndicatorIcon.color = Color.white;
                return;
            }

            float t = Mathf.PingPong(Time.time / pulsePeriod, 1f);
            scanIndicatorIcon.color = Color.Lerp(Color.white, Color.cyan, t);
        }

        // ── Mode selector ─────────────────────────────────────────────────────────

        /// <summary>
        /// Called by each survey-mode button in the Inspector.
        /// Index corresponds to <see cref="SurveyMode"/> cast from int.
        /// </summary>
        public void OnModeButtonPressed(int modeIndex)
        {
            SurveyMode mode = (SurveyMode)modeIndex;
            UpdateModeButtons(mode);

            HeatmapOverlayRenderer renderer = FindObjectOfType<HeatmapOverlayRenderer>();
            if (renderer != null)
                renderer.SetMode(mode);

            TerrainSurveyAnalytics.TrackModeChanged(mode);
        }

        private void UpdateModeButtons(SurveyMode active)
        {
            if (modeButtons == null) return;
            for (int i = 0; i < modeButtons.Length; i++)
            {
                if (modeButtons[i] == null) continue;
                var colors = modeButtons[i].colors;
                colors.normalColor = (i == (int)active) ? Color.cyan : Color.white;
                modeButtons[i].colors = colors;
            }
        }

        // ── Toast ─────────────────────────────────────────────────────────────────

        private void ShowToast(string locKey)
        {
            if (toastPanel == null) return;
            if (_toastRoutine != null) StopCoroutine(_toastRoutine);
            _toastRoutine = StartCoroutine(ToastRoutine(locKey));
        }

        private IEnumerator ToastRoutine(string locKey)
        {
            if (toastLabel != null)
                toastLabel.text = locKey; // resolved by localization system

            toastPanel.SetActive(true);
            yield return new WaitForSeconds(toastDuration);
            toastPanel.SetActive(false);
        }
    }
}
