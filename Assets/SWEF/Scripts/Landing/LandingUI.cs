// LandingUI.cs — SWEF Landing & Airport System (Phase 68)
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Landing
{
    /// <summary>
    /// Phase 68 — UI controller for the landing system.
    ///
    /// <para>Displays ILS localizer and glide slope needles, PAPI lights,
    /// landing gear status, distance-to-runway, landing state, and an
    /// animated landing score popup after touchdown.</para>
    /// </summary>
    public class LandingUI : MonoBehaviour
    {
        #region Inspector

        [Header("ILS Display — Deviation Needles")]
        [Tooltip("Horizontal bar that moves left/right to show localizer deviation.")]
        [SerializeField] private RectTransform localizerIndicator;

        [Tooltip("Vertical bar that moves up/down to show glide slope deviation.")]
        [SerializeField] private RectTransform glideSlopeIndicator;

        [Tooltip("Combined crosshair showing both localizer and glide slope deviations.")]
        [SerializeField] private RectTransform approachCrosshair;

        [Tooltip("Maximum pixel offset for full-scale needle deflection.")]
        [SerializeField] private float needleMaxOffset = 80f;

        [Header("Text Readouts")]
        [Tooltip("Displays distance to the runway threshold.")]
        [SerializeField] private TextMeshProUGUI distanceToRunwayText;

        [Tooltip("Displays the current landing state.")]
        [SerializeField] private TextMeshProUGUI landingStateText;

        [Tooltip("Displays the current gear status string.")]
        [SerializeField] private TextMeshProUGUI gearStatusText;

        [Header("Gear Status")]
        [Tooltip("Icon image: green = deployed, red = retracted, yellow = in-transit.")]
        [SerializeField] private Image gearIcon;

        [Tooltip("Color when gear is fully deployed.")]
        [SerializeField] private Color gearDeployedColor  = new Color(0.20f, 0.85f, 0.20f, 1f);

        [Tooltip("Color when gear is fully retracted.")]
        [SerializeField] private Color gearRetractedColor = new Color(0.85f, 0.20f, 0.20f, 1f);

        [Tooltip("Color when gear is in transit or damaged.")]
        [SerializeField] private Color gearTransitColor   = new Color(1.00f, 0.85f, 0.00f, 1f);

        [Header("PAPI Lights")]
        [Tooltip("Four PAPI light images from left to right; red = below slope, white = above.")]
        [SerializeField] private Image[] papiLights = Array.Empty<Image>();

        [Tooltip("PAPI light color for 'above glide slope'.")]
        [SerializeField] private Color papiWhite = Color.white;

        [Tooltip("PAPI light color for 'below glide slope'.")]
        [SerializeField] private Color papiRed   = new Color(0.90f, 0.15f, 0.10f, 1f);

        [Header("Landing Score Popup")]
        [Tooltip("Root GameObject of the score popup panel; hidden when inactive.")]
        [SerializeField] private GameObject scorePopupRoot;

        [Tooltip("Text element showing the numeric landing score.")]
        [SerializeField] private TextMeshProUGUI scoreText;

        [Tooltip("Text element showing the landing grade.")]
        [SerializeField] private TextMeshProUGUI gradeText;

        [Tooltip("Duration in seconds the score popup remains visible.")]
        [SerializeField] private float scoreDisplayDuration = 5f;

        [Header("Auto-Land Indicator")]
        [Tooltip("Text element showing the current auto-land mode.")]
        [SerializeField] private TextMeshProUGUI autoLandModeText;

        [Header("Runway Overlay")]
        [Tooltip("Root GameObject of the runway overlay (optional); shown on approach.")]
        [SerializeField] private GameObject runwayOverlayRoot;

        [Tooltip("Text label inside the runway overlay for the runway ID.")]
        [SerializeField] private TextMeshProUGUI runwayOverlayLabel;

        [Header("Data Sources")]
        [Tooltip("ApproachGuidance to read ILS deviations from. Auto-resolved if null.")]
        [SerializeField] private ApproachGuidance approachGuidance;

        [Tooltip("LandingDetector to read state from. Auto-resolved if null.")]
        [SerializeField] private LandingDetector landingDetector;

        [Tooltip("LandingGearController to read gear state from. Auto-resolved if null.")]
        [SerializeField] private LandingGearController gearController;

        [Tooltip("AutoLandAssist to read mode from. Auto-resolved if null.")]
        [SerializeField] private AutoLandAssist autoLandAssist;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (approachGuidance == null) approachGuidance = FindFirstObjectByType<ApproachGuidance>();
            if (landingDetector  == null) landingDetector  = FindFirstObjectByType<LandingDetector>();
            if (gearController   == null) gearController   = FindFirstObjectByType<LandingGearController>();
            if (autoLandAssist   == null) autoLandAssist   = FindFirstObjectByType<AutoLandAssist>();

            if (scorePopupRoot != null) scorePopupRoot.SetActive(false);
        }

        private void Start()
        {
            if (landingDetector != null)
                landingDetector.OnLandingScored += ShowLandingScore;

            if (gearController != null)
                gearController.OnGearStateChanged += OnGearStateChanged;
        }

        private void OnDestroy()
        {
            if (landingDetector != null)
                landingDetector.OnLandingScored -= ShowLandingScore;

            if (gearController != null)
                gearController.OnGearStateChanged -= OnGearStateChanged;
        }

        private void Update()
        {
            UpdateILSDisplay();
            UpdateTextReadouts();
            UpdateAutoLandIndicator();
        }

        #endregion

        #region Public API

        /// <summary>Shows the animated landing score popup.</summary>
        /// <param name="score">Landing score 0–100.</param>
        /// <param name="grade">Grade string (Perfect, Good, Acceptable, Hard, Crash).</param>
        public void ShowLandingScore(float score, string grade)
        {
            if (scoreText  != null) scoreText.text  = $"{score:F1}";
            if (gradeText  != null) gradeText.text  = grade;
            if (scorePopupRoot != null)
                StartCoroutine(ShowScorePopup());
        }

        /// <summary>Shows the runway overlay highlighting the target runway.</summary>
        /// <param name="runway">The runway being approached.</param>
        public void ShowRunwayOverlay(RunwayData runway)
        {
            if (runwayOverlayRoot  != null) runwayOverlayRoot.SetActive(true);
            if (runwayOverlayLabel != null) runwayOverlayLabel.text = runway?.runwayId ?? "";
        }

        /// <summary>Hides the runway overlay.</summary>
        public void HideRunwayOverlay()
        {
            if (runwayOverlayRoot != null) runwayOverlayRoot.SetActive(false);
        }

        /// <summary>
        /// Updates the four PAPI lights based on glide slope deviation.
        /// Negative deviation = below slope (more lights red), positive = above (more white).
        /// </summary>
        /// <param name="glideSlopeDeviation">Glide slope deviation −1 to +1.</param>
        public void UpdatePAPI(float glideSlopeDeviation)
        {
            if (papiLights == null || papiLights.Length == 0) return;

            // Map −1…+1 to how many lights are white (0–4)
            // On slope (0): 2 red, 2 white  (standard PAPI convention)
            // Below (negative): more red
            // Above (positive): more white
            int whiteLights = Mathf.RoundToInt(Mathf.Clamp((glideSlopeDeviation + 1f) * 2f, 0f, 4f));

            for (int i = 0; i < papiLights.Length; i++)
            {
                if (papiLights[i] == null) continue;
                // Lights are ordered left-to-right; rightmost lights go white first
                papiLights[i].color = (i >= papiLights.Length - whiteLights) ? papiWhite : papiRed;
            }
        }

        #endregion

        #region ILS Display

        private void UpdateILSDisplay()
        {
            if (approachGuidance == null) return;

            float loc = approachGuidance.LocalizerDeviation;
            float gs  = approachGuidance.GlideSlopeDeviation;

            if (localizerIndicator  != null)
                localizerIndicator.anchoredPosition  = new Vector2(loc * needleMaxOffset, 0f);

            if (glideSlopeIndicator != null)
                glideSlopeIndicator.anchoredPosition = new Vector2(0f, gs * needleMaxOffset);

            if (approachCrosshair   != null)
                approachCrosshair.anchoredPosition   = new Vector2(loc * needleMaxOffset, gs * needleMaxOffset);

            UpdatePAPI(gs);
        }

        #endregion

        #region Text Readouts

        private void UpdateTextReadouts()
        {
            if (approachGuidance != null && distanceToRunwayText != null)
                distanceToRunwayText.text = $"{approachGuidance.DistanceToThreshold:F0} m";

            if (landingDetector != null && landingStateText != null)
                landingStateText.text = landingDetector.CurrentState.ToString();
        }

        private void UpdateAutoLandIndicator()
        {
            if (autoLandModeText == null || autoLandAssist == null) return;
            autoLandModeText.text = autoLandAssist.Mode == AutoLandAssist.AutoLandMode.Off
                ? ""
                : $"A/LAND: {autoLandAssist.Mode}";
        }

        #endregion

        #region Gear Status

        private void OnGearStateChanged(GearState state)
        {
            if (gearStatusText != null) gearStatusText.text = state.ToString();
            if (gearIcon == null) return;

            gearIcon.color = state switch
            {
                GearState.Deployed  => gearDeployedColor,
                GearState.Retracted => gearRetractedColor,
                _                   => gearTransitColor
            };
        }

        #endregion

        #region Score Popup Coroutine

        private IEnumerator ShowScorePopup()
        {
            scorePopupRoot.SetActive(true);
            yield return new WaitForSeconds(scoreDisplayDuration);
            scorePopupRoot.SetActive(false);
        }

        #endregion
    }
}
