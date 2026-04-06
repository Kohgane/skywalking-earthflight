// DebrisRadarHUD.cs — Phase 114: Satellite & Space Debris Tracking
// In-flight debris radar: proximity warnings, collision countdown, avoidance guidance.
// Namespace: SWEF.SatelliteTracking

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// In-flight heads-up display showing nearby debris threats on a radar circle,
    /// proximity warning indicators, time-to-closest-approach countdown, and
    /// avoidance maneuver guidance vector.
    /// </summary>
    public class DebrisRadarHUD : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Radar Display")]
        [SerializeField] private RectTransform radarCircle;
        [SerializeField] private GameObject debrisBlipPrefab;
        [SerializeField] private float radarRangeKm = 50f;

        [Header("Warning Indicators")]
        [SerializeField] private GameObject yellowWarningIndicator;
        [SerializeField] private GameObject redWarningIndicator;
        [SerializeField] private Text warningText;
        [SerializeField] private Text tcaCountdownText;

        [Header("Avoidance Guidance")]
        [SerializeField] private RectTransform avoidanceArrow;
        [SerializeField] private Text avoidanceDeltaVText;

        [Header("Player Aircraft")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private float kmPerWorldUnit = 10f;

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly List<GameObject> _blips = new List<GameObject>();
        private CollisionWarningSystem _warningSystem;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            _warningSystem = FindObjectOfType<CollisionWarningSystem>();
            if (_warningSystem != null)
                _warningSystem.OnConjunctionDetected += HandleConjunction;
        }

        private void OnDestroy()
        {
            if (_warningSystem != null)
                _warningSystem.OnConjunctionDetected -= HandleConjunction;
        }

        private void Update()
        {
            UpdateRadarBlips();
            UpdateWarningDisplay();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void UpdateRadarBlips()
        {
            // Clear old blips
            foreach (var b in _blips) Destroy(b);
            _blips.Clear();

            if (radarCircle == null || debrisBlipPrefab == null || playerTransform == null) return;

            var debrisMgr = SpaceDebrisManager.Instance;
            if (debrisMgr == null) return;

            var playerPosKm = playerTransform.position * kmPerWorldUnit;

            foreach (var debris in debrisMgr.GetAllDebris())
            {
                var debrisPos = debris.positionECI;
                float distKm  = Vector3.Distance(playerPosKm, debrisPos);
                if (distKm > radarRangeKm) continue;

                var blip = Instantiate(debrisBlipPrefab, radarCircle);
                float radarSize = radarCircle.sizeDelta.x * 0.5f;
                Vector2 offset  = new Vector2(
                    (debrisPos.x - playerPosKm.x) / radarRangeKm * radarSize,
                    (debrisPos.z - playerPosKm.z) / radarRangeKm * radarSize);

                blip.GetComponent<RectTransform>().anchoredPosition = offset;
                _blips.Add(blip);
            }
        }

        private void UpdateWarningDisplay()
        {
            if (_warningSystem == null) return;

            var conjunctions = _warningSystem.ActiveConjunctions;
            bool hasYellow = false, hasRed = false;
            ConjunctionData worst = null;

            foreach (var c in conjunctions)
            {
                if (c.urgencyLevel >= 3) { hasRed = true; worst = c; break; }
                if (c.urgencyLevel >= 2) { hasYellow = true; if (worst == null) worst = c; }
            }

            if (yellowWarningIndicator != null) yellowWarningIndicator.SetActive(hasYellow && !hasRed);
            if (redWarningIndicator    != null) redWarningIndicator.SetActive(hasRed);

            if (worst != null)
            {
                if (warningText != null)
                    warningText.text = $"CONJUNCTION: {worst.missDistanceKm:F1} km";

                var timeToTCA = (worst.tcaUtc - System.DateTime.UtcNow).TotalSeconds;
                if (tcaCountdownText != null)
                    tcaCountdownText.text = $"TCA: {timeToTCA:F0}s";

                if (avoidanceDeltaVText != null)
                    avoidanceDeltaVText.text = $"ΔV: {worst.avoidanceDeltaVms:F1} m/s";
            }
            else
            {
                if (warningText      != null) warningText.text      = "ALL CLEAR";
                if (tcaCountdownText != null) tcaCountdownText.text = string.Empty;
            }
        }

        private void HandleConjunction(ConjunctionData conj)
        {
            Debug.LogWarning($"[DebrisRadarHUD] Conjunction alert: " +
                             $"NORAD {conj.primaryNoradId} vs {conj.secondaryNoradId}, " +
                             $"miss dist {conj.missDistanceKm:F2} km, P={conj.collisionProbability:E2}");
        }
    }
}
