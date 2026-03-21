using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SWEF.Localization;

namespace SWEF.HiddenGems
{
    /// <summary>
    /// Compact HUD radar that points toward the nearest undiscovered hidden gem.
    /// The indicator pulses faster as the player approaches (cold → warm → hot → discovery!).
    /// Can be toggled via <see cref="SettingsManager"/>.
    /// </summary>
    public class GemRadarUI : MonoBehaviour
    {
        // ── Settings key ──────────────────────────────────────────────────────────
        private const string SettingKey = "SWEF_GemRadar_Enabled";

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("References")]
        [SerializeField] private GameObject radarRoot;
        [SerializeField] private RectTransform directionArrow;
        [SerializeField] private Image         radarIcon;
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private TextMeshProUGUI warmthText;

        [Header("Thresholds (world units)")]
        [SerializeField] private float coldThreshold = 8000f;
        [SerializeField] private float warmThreshold = 4000f;
        [SerializeField] private float hotThreshold  = 1500f;

        [Header("Pulse")]
        [SerializeField] private float coldPulseRate = 0.5f;
        [SerializeField] private float warmPulseRate = 1.5f;
        [SerializeField] private float hotPulseRate  = 3.0f;

        [Header("Sonar Ping")]
        [SerializeField] private float sonarInterval = 4f;

        // ── State ─────────────────────────────────────────────────────────────────
        private bool    _enabled   = true;
        private float   _pulseTime;
        private float   _sonarTimer;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Start()
        {
            LoadSetting();
            if (radarRoot != null) radarRoot.SetActive(_enabled);
        }

        private void Update()
        {
            if (!_enabled) return;
            if (HiddenGemManager.Instance == null) return;

            var (gem, dist) = HiddenGemManager.Instance.GetNearestUndiscoveredGem();
            if (gem == null)
            {
                if (radarRoot != null) radarRoot.SetActive(false);
                return;
            }
            if (radarRoot != null) radarRoot.SetActive(true);

            // Distance text
            UpdateDistanceText(dist);

            // Warmth category
            var (warmthKey, pulseRate, rarityColor) = ClassifyDistance(dist, gem.rarity);
            UpdateWarmthText(warmthKey);
            UpdatePulse(pulseRate);
            UpdateIconColor(rarityColor);

            // Arrow direction
            UpdateArrow(gem);

            // Sonar ping
            _sonarTimer += Time.deltaTime;
            if (_sonarTimer >= sonarInterval)
            {
                _sonarTimer = 0f;
                TriggerSonarPing();
            }
        }

        // ── Radar logic ───────────────────────────────────────────────────────────

        private void UpdateDistanceText(float dist)
        {
            if (distanceText == null) return;
            if (dist >= 1000f)
                distanceText.text = $"{dist / 1000f:F1} km";
            else
                distanceText.text = $"{dist:F0} m";
        }

        private (string key, float pulseRate, Color color) ClassifyDistance(float dist, GemRarity rarity)
        {
            string key;
            float  rate;
            if (dist > coldThreshold)      { key = "gem_radar_cold"; rate = coldPulseRate; }
            else if (dist > warmThreshold) { key = "gem_radar_warm"; rate = warmPulseRate; }
            else if (dist > hotThreshold)  { key = "gem_radar_warm"; rate = warmPulseRate * 2f; }
            else                           { key = "gem_radar_hot";  rate = hotPulseRate; }

            ColorUtility.TryParseHtmlString(HiddenGemDefinition.RarityColor(rarity), out Color c);
            return (key, rate, c);
        }

        private void UpdateWarmthText(string locKey)
        {
            if (warmthText == null) return;
            var lm = LocalizationManager.Instance;
            warmthText.text = lm != null ? lm.GetText(locKey) : locKey;
        }

        private void UpdatePulse(float rate)
        {
            if (radarIcon == null) return;
            _pulseTime += Time.deltaTime * rate;
            float alpha = Mathf.Abs(Mathf.Sin(_pulseTime * Mathf.PI));
            var c = radarIcon.color;
            c.a = Mathf.Clamp(alpha, 0.3f, 1.0f);
            radarIcon.color = c;
        }

        private void UpdateIconColor(Color color)
        {
            if (radarIcon == null) return;
            var c   = radarIcon.color;
            color.a = c.a;
            radarIcon.color = color;
        }

        private void UpdateArrow(HiddenGemDefinition gem)
        {
            if (directionArrow == null || HiddenGemManager.Instance == null) return;
            var mgr = HiddenGemManager.Instance;
            // Use minimap bearing if available
            var mm = SWEF.Minimap.MinimapManager.Instance;
            string blipId = "hiddengem_" + gem.gemId;
            var blip = mm?.GetBlip(blipId);
            if (blip != null)
            {
                directionArrow.localEulerAngles = new Vector3(0, 0, -blip.bearingDeg);
            }
        }

        private void TriggerSonarPing()
        {
            // A simple scale-pop animation to indicate a sonar ping
            if (radarIcon == null) return;
            StopAllCoroutines();
            StartCoroutine(SonarPingAnim());
        }

        private System.Collections.IEnumerator SonarPingAnim()
        {
            if (directionArrow == null) yield break;
            Vector3 original = directionArrow.localScale;
            directionArrow.localScale = original * 1.3f;
            yield return new UnityEngine.WaitForSeconds(0.1f);
            directionArrow.localScale = original;
        }

        // ── Settings ──────────────────────────────────────────────────────────────

        private void LoadSetting()
        {
            _enabled = PlayerPrefs.GetInt(SettingKey, 1) != 0;
        }

        /// <summary>Toggle the radar on/off and persist the preference.</summary>
        public void SetEnabled(bool value)
        {
            _enabled = value;
            PlayerPrefs.SetInt(SettingKey, value ? 1 : 0);
            PlayerPrefs.Save();
            if (radarRoot != null) radarRoot.SetActive(value);
        }
    }
}
