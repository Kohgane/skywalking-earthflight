// DisasterTrackerUI.cs — SWEF Natural Disaster & Dynamic World Events (Phase 86)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.NaturalDisaster
{
    /// <summary>
    /// Phase 86 — In-flight HUD overlay that displays a compact card for each active
    /// disaster.  Each card shows the disaster icon, name, phase indicator, distance,
    /// and a severity bar.  A directional hazard arrow points toward the nearest
    /// hazard zone.  Tapping a card expands a detail panel.
    ///
    /// <para>Colour-coding by severity: green (Minor) → yellow (Moderate) →
    /// orange (Severe) → red (Catastrophic) → purple (Apocalyptic).</para>
    /// </summary>
    public class DisasterTrackerUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Card Container")]
        [Tooltip("Parent RectTransform that holds all disaster cards.")]
        [SerializeField] private RectTransform _cardContainer;

        [Tooltip("Template card GameObject (deactivated). Cloned per active disaster.")]
        [SerializeField] private GameObject _cardTemplate;

        [Header("Directional Arrow")]
        [Tooltip("RectTransform of the directional hazard arrow.")]
        [SerializeField] private RectTransform _hazardArrow;

        [Header("Detail Panel")]
        [Tooltip("Root of the expandable detail panel.")]
        [SerializeField] private GameObject _detailPanel;

        [Tooltip("Text label for the selected disaster's name in the detail panel.")]
        [SerializeField] private Text _detailNameText;

        [Tooltip("Text label for the selected disaster's phase.")]
        [SerializeField] private Text _detailPhaseText;

        [Tooltip("Text label for the selected disaster's severity.")]
        [SerializeField] private Text _detailSeverityText;

        [Tooltip("Text label for the selected disaster's distance.")]
        [SerializeField] private Text _detailDistanceText;

        [Header("Refresh")]
        [Tooltip("Interval in seconds between card refresh cycles.")]
        [SerializeField] [Min(0.05f)] private float _refreshInterval = 0.25f;

        // ── Private State ─────────────────────────────────────────────────────────

        private Transform _playerTransform;
        private float     _refreshTimer;
        private ActiveDisaster _selectedDisaster;

        private readonly List<DisasterCard> _cards = new List<DisasterCard>();

        // ── Card Data Class ───────────────────────────────────────────────────────

        private class DisasterCard
        {
            public ActiveDisaster disaster;
            public GameObject     root;
            public Image          icon;
            public Text           nameText;
            public Text           phaseText;
            public Text           distanceText;
            public Image          severityBar;
            public Button         expandButton;
        }

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            var fc = FindFirstObjectByType<Flight.FlightController>();
            if (fc != null) _playerTransform = fc.transform;

            if (_detailPanel != null) _detailPanel.SetActive(false);
            if (_cardTemplate != null) _cardTemplate.SetActive(false);

            if (DisasterManager.Instance != null)
            {
                DisasterManager.Instance.OnDisasterSpawned += OnDisasterSpawned;
                DisasterManager.Instance.OnDisasterEnded   += OnDisasterEnded;
            }
        }

        private void OnDestroy()
        {
            if (DisasterManager.Instance != null)
            {
                DisasterManager.Instance.OnDisasterSpawned -= OnDisasterSpawned;
                DisasterManager.Instance.OnDisasterEnded   -= OnDisasterEnded;
            }
        }

        private void Update()
        {
            _refreshTimer -= Time.deltaTime;
            if (_refreshTimer <= 0f)
            {
                _refreshTimer = _refreshInterval;
                RefreshCards();
                UpdateHazardArrow();
            }
        }

        // ── Event Handlers ────────────────────────────────────────────────────────

        private void OnDisasterSpawned(ActiveDisaster disaster)
        {
            AddCard(disaster);
        }

        private void OnDisasterEnded(ActiveDisaster disaster)
        {
            RemoveCard(disaster);
            if (_selectedDisaster == disaster)
                CloseDetailPanel();
        }

        // ── Card Management ───────────────────────────────────────────────────────

        private void AddCard(ActiveDisaster disaster)
        {
            if (_cardTemplate == null || _cardContainer == null) return;

            GameObject cardRoot = Instantiate(_cardTemplate, _cardContainer);
            cardRoot.SetActive(true);

            DisasterCard card = new DisasterCard
            {
                disaster      = disaster,
                root          = cardRoot,
                icon          = cardRoot.transform.Find("Icon")?.GetComponent<Image>(),
                nameText      = cardRoot.transform.Find("Name")?.GetComponent<Text>(),
                phaseText     = cardRoot.transform.Find("Phase")?.GetComponent<Text>(),
                distanceText  = cardRoot.transform.Find("Distance")?.GetComponent<Text>(),
                severityBar   = cardRoot.transform.Find("SeverityBar")?.GetComponent<Image>(),
                expandButton  = cardRoot.GetComponent<Button>()
            };

            if (card.icon != null && disaster.data?.disasterIcon != null)
                card.icon.sprite = disaster.data.disasterIcon;

            if (card.expandButton != null)
            {
                ActiveDisaster captured = disaster;
                card.expandButton.onClick.AddListener(() => ToggleDetailPanel(captured));
            }

            _cards.Add(card);
        }

        private void RemoveCard(ActiveDisaster disaster)
        {
            for (int i = _cards.Count - 1; i >= 0; i--)
            {
                if (_cards[i].disaster == disaster)
                {
                    Destroy(_cards[i].root);
                    _cards.RemoveAt(i);
                }
            }
        }

        // ── Refresh ───────────────────────────────────────────────────────────────

        private void RefreshCards()
        {
            foreach (DisasterCard card in _cards)
            {
                if (card.disaster == null) continue;

                ActiveDisaster d   = card.disaster;
                Color          col = GetSeverityColor(d.currentSeverity);

                if (card.nameText != null)
                    card.nameText.text = d.data != null ? d.data.disasterName : "Unknown";

                if (card.phaseText != null)
                    card.phaseText.text = d.currentPhase.ToString();

                if (card.distanceText != null && _playerTransform != null)
                {
                    float dist = Vector3.Distance(_playerTransform.position, d.epicenter);
                    card.distanceText.text = $"{dist / 1000f:F1} km";
                }

                if (card.severityBar != null)
                {
                    card.severityBar.color = col;
                    card.severityBar.fillAmount = ((int)d.currentSeverity + 1) / 5f;
                }
            }

            // Refresh open detail panel
            if (_selectedDisaster != null && _detailPanel != null && _detailPanel.activeSelf)
                PopulateDetailPanel(_selectedDisaster);
        }

        // ── Hazard Arrow ──────────────────────────────────────────────────────────

        private void UpdateHazardArrow()
        {
            if (_hazardArrow == null || _playerTransform == null) return;

            // Find nearest active hazard zone across all disasters
            Vector3 nearest    = Vector3.zero;
            float   nearestDist = float.MaxValue;
            bool    found       = false;

            if (DisasterManager.Instance != null)
            {
                foreach (ActiveDisaster d in DisasterManager.Instance.activeDisasters)
                {
                    foreach (HazardZone z in d.hazardZones)
                    {
                        if (!z.isActive) continue;
                        float dist = Vector3.Distance(_playerTransform.position, z.center);
                        if (dist < nearestDist)
                        {
                            nearestDist = dist;
                            nearest     = z.center;
                            found       = true;
                        }
                    }
                }
            }

            _hazardArrow.gameObject.SetActive(found);

            if (found)
            {
                Vector3 dir    = nearest - _playerTransform.position;
                float   angle  = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                _hazardArrow.rotation = Quaternion.Euler(0f, 0f, -angle);
            }
        }

        // ── Detail Panel ──────────────────────────────────────────────────────────

        private void ToggleDetailPanel(ActiveDisaster disaster)
        {
            if (_selectedDisaster == disaster && _detailPanel != null && _detailPanel.activeSelf)
            {
                CloseDetailPanel();
            }
            else
            {
                OpenDetailPanel(disaster);
            }
        }

        private void OpenDetailPanel(ActiveDisaster disaster)
        {
            _selectedDisaster = disaster;
            if (_detailPanel != null) _detailPanel.SetActive(true);
            PopulateDetailPanel(disaster);
        }

        private void CloseDetailPanel()
        {
            _selectedDisaster = null;
            if (_detailPanel != null) _detailPanel.SetActive(false);
        }

        private void PopulateDetailPanel(ActiveDisaster d)
        {
            if (d == null || d.data == null) return;

            if (_detailNameText != null)
                _detailNameText.text = d.data.disasterName;

            if (_detailPhaseText != null)
                _detailPhaseText.text = d.currentPhase.ToString();

            if (_detailSeverityText != null)
                _detailSeverityText.text = d.currentSeverity.ToString();

            if (_detailDistanceText != null && _playerTransform != null)
            {
                float dist = Vector3.Distance(_playerTransform.position, d.epicenter);
                _detailDistanceText.text = $"{dist / 1000f:F1} km";
            }
        }

        // ── Colour Helpers ────────────────────────────────────────────────────────

        private static Color GetSeverityColor(DisasterSeverity severity)
        {
            switch (severity)
            {
                case DisasterSeverity.Minor:        return Color.green;
                case DisasterSeverity.Moderate:     return Color.yellow;
                case DisasterSeverity.Severe:       return new Color(1f, 0.5f, 0f);
                case DisasterSeverity.Catastrophic: return Color.red;
                case DisasterSeverity.Apocalyptic:  return new Color(0.6f, 0f, 1f);
                default:                            return Color.white;
            }
        }
    }
}
