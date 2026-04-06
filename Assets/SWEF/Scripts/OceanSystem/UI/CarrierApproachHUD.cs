// CarrierApproachHUD.cs — Phase 117: Advanced Ocean & Maritime System
// Carrier landing HUD: meatball, lineup, glideslope, AOA indexer, LSO calls.
// Namespace: SWEF.OceanSystem

using UnityEngine;
using UnityEngine.UI;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Carrier Controlled Approach (CCA) HUD overlay.
    /// Displays meatball glidepath indicator, lineup deviation bar,
    /// glideslope needle, angle-of-attack indexer, and LSO call text.
    /// </summary>
    public class CarrierApproachHUD : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("HUD Root")]
        [SerializeField] private GameObject hudRoot;

        [Header("Meatball")]
        [SerializeField] private Image  meatballImage;
        [SerializeField] private Color  meatballOnGlidepath = Color.yellow;
        [SerializeField] private Color  meatballHigh        = Color.green;
        [SerializeField] private Color  meatballLow         = Color.red;

        [Header("Lineup")]
        [SerializeField] private RectTransform lineupBar;
        [SerializeField] private float         lineupBarMaxOffset = 60f;

        [Header("Glideslope")]
        [SerializeField] private RectTransform glideslopeNeedle;
        [SerializeField] private float         glideslopeMaxOffset = 50f;

        [Header("AOA Indexer")]
        [SerializeField] private Image aoaIndicator;
        [SerializeField] private Color aoaOnSpeedColour   = Color.green;
        [SerializeField] private Color aoaFastColour      = Color.yellow;
        [SerializeField] private Color aoaSlowColour      = Color.red;

        [Header("LSO")]
        [SerializeField] private Text lsoCallText;
        [SerializeField] private float lsoCallDisplayTime = 4f;

        // ── Private state ─────────────────────────────────────────────────────────

        private float _lsoCallTimer;
        private CarrierNavigationSystem.ApproachGuidance _lastGuidance;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Updates the HUD with fresh approach guidance data.</summary>
        public void UpdateGuidance(CarrierNavigationSystem.ApproachGuidance guidance)
        {
            _lastGuidance = guidance;
            UpdateMeatball(guidance.glidepathState);
            UpdateLineup(guidance.lineupDeviation);
            UpdateGlideslope(guidance.glideslopeDeviation);
        }

        /// <summary>Displays an LSO call for a short duration.</summary>
        public void ShowLSOCall(string call)
        {
            if (lsoCallText == null) return;
            lsoCallText.text = call;
            _lsoCallTimer    = lsoCallDisplayTime;
        }

        /// <summary>Updates the AOA indexer colour.</summary>
        public void UpdateAOA(float aoaDeg, float targetAoa, float tolerance = 1f)
        {
            if (aoaIndicator == null) return;
            float delta = aoaDeg - targetAoa;
            if (Mathf.Abs(delta) <= tolerance)      aoaIndicator.color = aoaOnSpeedColour;
            else if (delta >  tolerance)             aoaIndicator.color = aoaFastColour;
            else                                     aoaIndicator.color = aoaSlowColour;
        }

        /// <summary>Shows or hides the carrier approach HUD.</summary>
        public void SetVisible(bool visible)
        {
            if (hudRoot != null) hudRoot.SetActive(visible);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Update()
        {
            if (_lsoCallTimer > 0f)
            {
                _lsoCallTimer -= Time.deltaTime;
                if (_lsoCallTimer <= 0f && lsoCallText != null)
                    lsoCallText.text = string.Empty;
            }
        }

        // ── Private Helpers ───────────────────────────────────────────────────────

        private void UpdateMeatball(GlidepathState state)
        {
            if (meatballImage == null) return;
            meatballImage.color = state switch
            {
                GlidepathState.OnGlidepath  => meatballOnGlidepath,
                GlidepathState.SlightlyHigh => meatballHigh,
                GlidepathState.High         => meatballHigh,
                GlidepathState.SlightlyLow  => meatballLow,
                GlidepathState.Low          => meatballLow,
                _ => meatballOnGlidepath
            };
        }

        private void UpdateLineup(float deviationDeg)
        {
            if (lineupBar == null) return;
            float offset = Mathf.Clamp(deviationDeg / 10f, -1f, 1f) * lineupBarMaxOffset;
            lineupBar.anchoredPosition = new Vector2(offset, lineupBar.anchoredPosition.y);
        }

        private void UpdateGlideslope(float deviationDeg)
        {
            if (glideslopeNeedle == null) return;
            float offset = Mathf.Clamp(-deviationDeg / 3f, -1f, 1f) * glideslopeMaxOffset;
            glideslopeNeedle.anchoredPosition = new Vector2(glideslopeNeedle.anchoredPosition.x, offset);
        }
    }
}
