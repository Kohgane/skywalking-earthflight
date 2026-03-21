using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Minimap
{
    /// <summary>
    /// Renders a compass rose ring around the minimap edge with cardinal and intercardinal
    /// direction labels. The ring rotates with the player's heading so that "N" always points
    /// to true world north. Also shows a bearing indicator line towards the current navigation
    /// target and a distance-to-target text label.
    /// </summary>
    public class MinimapCompass : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────────
        [Header("UI References")]
        [Tooltip("RectTransform that rotates to represent the compass rose ring.")]
        [SerializeField] private RectTransform compassRing;

        [Tooltip("RectTransform of the bearing indicator line pointing to the nav target.")]
        [SerializeField] private RectTransform bearingLine;

        [Tooltip("Text label showing distance to the current navigation target.")]
        [SerializeField] private Text distanceText;

        [Header("Cardinal Labels")]
        [Tooltip("Text label for North.")]  [SerializeField] private Text labelN;
        [Tooltip("Text label for South.")] [SerializeField] private Text labelS;
        [Tooltip("Text label for East.")]  [SerializeField] private Text labelE;
        [Tooltip("Text label for West.")]  [SerializeField] private Text labelW;
        [Tooltip("Text label for NE.")]    [SerializeField] private Text labelNE;
        [Tooltip("Text label for SE.")]    [SerializeField] private Text labelSE;
        [Tooltip("Text label for SW.")]    [SerializeField] private Text labelSW;
        [Tooltip("Text label for NW.")]    [SerializeField] private Text labelNW;

        [Header("Settings")]
        [Tooltip("Radius in pixels at which cardinal labels are positioned around the ring.")]
        [SerializeField] private float labelOrbitRadiusPx = 120f;

        [Tooltip("If true, intercardinal labels (NE, SE, SW, NW) are shown.")]
        [SerializeField] private bool showIntercardinals = true;

        // ── Private state ─────────────────────────────────────────────────────────
        private RectTransform[] _cardinalRects;   // [N, NE, E, SE, S, SW, W, NW]
        private float[]         _cardinalAngles = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };

        // ── Unity callbacks ────────────────────────────────────────────────────────
        private void Awake()
        {
            _cardinalRects = new RectTransform[]
            {
                labelN  != null ? labelN.rectTransform  : null,
                labelNE != null ? labelNE.rectTransform : null,
                labelE  != null ? labelE.rectTransform  : null,
                labelSE != null ? labelSE.rectTransform : null,
                labelS  != null ? labelS.rectTransform  : null,
                labelSW != null ? labelSW.rectTransform : null,
                labelW  != null ? labelW.rectTransform  : null,
                labelNW != null ? labelNW.rectTransform : null,
            };

            SetIntercardinalVisibility(showIntercardinals);
        }

        private void LateUpdate()
        {
            if (MinimapManager.Instance == null) return;

            Transform player = MinimapManager.Instance.PlayerTransform;
            if (player == null) return;

            float heading = player.eulerAngles.y; // 0 = North, 90 = East

            // Rotate compass ring counter-clockwise by heading so N stays at top
            if (compassRing != null)
                compassRing.localRotation = Quaternion.Euler(0f, 0f, heading);

            // Orbit cardinal label positions around the ring
            UpdateCardinalPositions(heading);

            // Update bearing indicator and distance text for active nav target
            UpdateBearingIndicator();
        }

        // ── Cardinal labels ────────────────────────────────────────────────────────
        private void UpdateCardinalPositions(float playerHeading)
        {
            // Each label stays at its world-compass angle; we counter-rotate labels
            // so that the text remains readable (not spinning with the ring).
            for (int i = 0; i < _cardinalRects.Length; i++)
            {
                RectTransform rt = _cardinalRects[i];
                if (rt == null) continue;

                float worldAngleDeg = _cardinalAngles[i];
                float uiAngleDeg    = worldAngleDeg - playerHeading; // compass ring rotation offsets
                float rad           = uiAngleDeg * Mathf.Deg2Rad;

                rt.anchoredPosition = new Vector2(
                    Mathf.Sin(rad) * labelOrbitRadiusPx,
                    Mathf.Cos(rad) * labelOrbitRadiusPx);

                // Keep text upright
                rt.localRotation = Quaternion.Euler(0f, 0f, -uiAngleDeg);
            }
        }

        private void SetIntercardinalVisibility(bool show)
        {
            // Indices 1, 3, 5, 7 are NE, SE, SW, NW
            int[] intercardinalIdx = { 1, 3, 5, 7 };
            foreach (int i in intercardinalIdx)
            {
                if (i < _cardinalRects.Length && _cardinalRects[i] != null)
                    _cardinalRects[i].gameObject.SetActive(show);
            }
        }

        // ── Bearing indicator ──────────────────────────────────────────────────────
        private void UpdateBearingIndicator()
        {
            if (MinimapManager.Instance == null) return;

            // Look for the nearest Destination or WaypointNext blip
            MinimapBlip target = null;
            float        minDist = float.MaxValue;

            var blips = MinimapManager.Instance.GetActiveBlips();
            foreach (var blip in blips)
            {
                if (blip.iconType != MinimapIconType.Destination &&
                    blip.iconType != MinimapIconType.WaypointNext)
                    continue;

                if (blip.distanceFromPlayer < minDist)
                {
                    minDist = blip.distanceFromPlayer;
                    target  = blip;
                }
            }

            // Show/hide bearing line
            if (bearingLine != null)
            {
                bearingLine.gameObject.SetActive(target != null);
                if (target != null)
                    bearingLine.localRotation = Quaternion.Euler(0f, 0f, -target.bearingDeg);
            }

            // Update distance text
            if (distanceText != null)
            {
                if (target != null)
                {
                    distanceText.gameObject.SetActive(true);
                    distanceText.text = FormatDistance(target.distanceFromPlayer);
                }
                else
                {
                    distanceText.gameObject.SetActive(false);
                }
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────────
        private static string FormatDistance(float meters)
        {
            if (meters < 1000f)
                return $"{meters:0} m";
            return $"{meters / 1000f:0.0} km";
        }

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>Shows or hides the intercardinal (NE, SE, SW, NW) labels.</summary>
        public void SetShowIntercardinals(bool show)
        {
            showIntercardinals = show;
            SetIntercardinalVisibility(show);
        }

        /// <summary>Sets the radius at which cardinal labels orbit the compass ring.</summary>
        public void SetLabelOrbitRadius(float radiusPx) => labelOrbitRadiusPx = Mathf.Max(20f, radiusPx);
    }
}
