// AdaptiveMusicHUD.cs — SWEF Dynamic Soundtrack & Adaptive Music System (Phase 83)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SWEF.AdaptiveMusic
{
    /// <summary>
    /// Minimal HUD widget that displays:
    /// <list type="bullet">
    ///   <item>Current mood label and icon.</item>
    ///   <item>Intensity bar with gradient colour.</item>
    ///   <item>Per-layer active stem indicators (coloured dots).</item>
    /// </list>
    ///
    /// <para>Expands into a panel with mood history timeline, manual override slider,
    /// and per-layer solo/mute toggles when opened by the player.</para>
    /// </summary>
    public class AdaptiveMusicHUD : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("References")]
        [SerializeField] private AdaptiveMusicManager adaptiveManager;

        [Header("Compact HUD Elements")]
        [SerializeField] private TextMeshProUGUI  moodLabel;
        [SerializeField] private Image            moodIcon;
        [SerializeField] private Image            intensityBar;
        [SerializeField] private Transform        stemIndicatorRoot;
        [SerializeField] private GameObject       stemIndicatorPrefab;

        [Header("Expanded Panel")]
        [SerializeField] private GameObject       expandedPanel;
        [SerializeField] private Slider           intensityOverrideSlider;
        [SerializeField] private TextMeshProUGUI  overrideLabel;

        [Header("Intensity Gradient")]
        [SerializeField] private Gradient         intensityGradient = new Gradient();

        // ── State ─────────────────────────────────────────────────────────────────
        private readonly Dictionary<MusicLayer, Image> _stemDots = new Dictionary<MusicLayer, Image>();
        private bool _expanded;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (adaptiveManager == null)
                adaptiveManager = FindFirstObjectByType<AdaptiveMusicManager>();

            BuildStemIndicators();

            if (expandedPanel != null)
                expandedPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (adaptiveManager == null) return;
            adaptiveManager.OnMoodChanged     += HandleMoodChanged;
            adaptiveManager.OnIntensityChanged += HandleIntensityChanged;
            adaptiveManager.OnStemActivated   += HandleStemActivated;
            adaptiveManager.OnStemDeactivated += HandleStemDeactivated;
        }

        private void OnDisable()
        {
            if (adaptiveManager == null) return;
            adaptiveManager.OnMoodChanged     -= HandleMoodChanged;
            adaptiveManager.OnIntensityChanged -= HandleIntensityChanged;
            adaptiveManager.OnStemActivated   -= HandleStemActivated;
            adaptiveManager.OnStemDeactivated -= HandleStemDeactivated;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Toggles the expanded HUD panel open/closed.</summary>
        public void ToggleExpanded()
        {
            _expanded = !_expanded;
            if (expandedPanel != null)
                expandedPanel.SetActive(_expanded);
        }

        // ── Event Handlers ────────────────────────────────────────────────────────

        private void HandleMoodChanged(MusicMood prev, MusicMood next)
        {
            if (moodLabel != null)
                moodLabel.text = FormatMoodName(next);
        }

        private void HandleIntensityChanged(float intensity)
        {
            if (intensityBar != null)
            {
                intensityBar.fillAmount = intensity;
                intensityBar.color      = intensityGradient.Evaluate(intensity);
            }
        }

        private void HandleStemActivated(MusicLayer layer)
        {
            if (_stemDots.TryGetValue(layer, out Image dot) && dot != null)
                dot.color = Color.green;
        }

        private void HandleStemDeactivated(MusicLayer layer)
        {
            if (_stemDots.TryGetValue(layer, out Image dot) && dot != null)
                dot.color = Color.gray;
        }

        // ── Internals ─────────────────────────────────────────────────────────────

        private void BuildStemIndicators()
        {
            if (stemIndicatorRoot == null || stemIndicatorPrefab == null)
                return;

            foreach (MusicLayer layer in System.Enum.GetValues(typeof(MusicLayer)))
            {
                GameObject go  = Instantiate(stemIndicatorPrefab, stemIndicatorRoot);
                go.name        = $"Dot_{layer}";
                Image img      = go.GetComponentInChildren<Image>();
                if (img != null)
                {
                    img.color = Color.gray;
                    _stemDots[layer] = img;
                }

                TextMeshProUGUI lbl = go.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl != null)
                    lbl.text = layer.ToString();
            }
        }

        private static string FormatMoodName(MusicMood mood)
        {
            return mood.ToString();
        }
    }
}
