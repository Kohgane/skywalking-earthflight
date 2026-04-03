// AdvancedPhotographyUI.cs — SWEF Advanced Photography & Drone Camera System (Phase 89)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.AdvancedPhotography
{
    /// <summary>
    /// Phase 89 — MonoBehaviour providing the full-screen photography menu UI.
    ///
    /// <para>Contains five panels:
    /// <list type="bullet">
    ///   <item>Gallery Browser — grid of captured photos with metadata.</item>
    ///   <item>Challenge List — active, upcoming and completed challenges.</item>
    ///   <item>Contest Panel — submit, vote and view leaderboard.</item>
    ///   <item>Filter Editor — custom filter with tint/saturation/contrast/vignette/exposure sliders.</item>
    ///   <item>Settings Panel — AI assist level, guide overlay toggle, auto-save toggle.</item>
    /// </list>
    /// All manager references are null-safe.</para>
    /// </summary>
    public sealed class AdvancedPhotographyUI : MonoBehaviour
    {
        // ── Panel Roots ───────────────────────────────────────────────────────────

        [Header("Panel Roots")]
        [SerializeField] private GameObject _galleryPanel;
        [SerializeField] private GameObject _challengePanel;
        [SerializeField] private GameObject _contestPanel;
        [SerializeField] private GameObject _filterEditorPanel;
        [SerializeField] private GameObject _settingsPanel;

        // ── Gallery ───────────────────────────────────────────────────────────────

        [Header("Gallery Browser")]
        [Tooltip("Prefab instantiated per photo entry in the gallery grid.")]
        [SerializeField] private GameObject _galleryItemPrefab;

        [Tooltip("Content transform of the gallery ScrollRect.")]
        [SerializeField] private RectTransform _galleryContent;

        // ── Challenge List ────────────────────────────────────────────────────────

        [Header("Challenge List")]
        [SerializeField] private GameObject _challengeItemPrefab;
        [SerializeField] private RectTransform _challengeListContent;

        // ── Contest Panel ─────────────────────────────────────────────────────────

        [Header("Contest Panel")]
        [SerializeField] private Text _contestTitleText;
        [SerializeField] private Button _submitButton;
        [SerializeField] private Button _voteButton;
        [SerializeField] private RectTransform _leaderboardContent;
        [SerializeField] private GameObject _leaderboardItemPrefab;

        // ── Filter Editor ─────────────────────────────────────────────────────────

        [Header("Filter Editor")]
        [SerializeField] private Slider _tintSlider;
        [SerializeField] private Slider _saturationSlider;
        [SerializeField] private Slider _contrastSlider;
        [SerializeField] private Slider _vignetteSlider;
        [SerializeField] private Slider _exposureSlider;
        [SerializeField] private Button _applyFilterButton;

        // ── Settings Panel ────────────────────────────────────────────────────────

        [Header("Settings Panel")]
        [SerializeField] private Dropdown _aiAssistDropdown;
        [SerializeField] private Toggle _guideOverlayToggle;
        [SerializeField] private Toggle _autoSaveToggle;

        // ── Navigation Buttons ────────────────────────────────────────────────────

        [Header("Navigation")]
        [SerializeField] private Button _galleryTabButton;
        [SerializeField] private Button _challengeTabButton;
        [SerializeField] private Button _contestTabButton;
        [SerializeField] private Button _filterTabButton;
        [SerializeField] private Button _settingsTabButton;
        [SerializeField] private Button _closeButton;

        // ── Private State ─────────────────────────────────────────────────────────

        private PhotoContestManager _contestManager;
        private AICompositionAssistant _aiAssistant;
        private string _selectedContestId = "";

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            _contestManager = PhotoContestManager.Instance;
            _aiAssistant    = AICompositionAssistant.Instance;

            BindButtons();
            ShowPanel(_galleryPanel);
        }

        // ── Tab Navigation ────────────────────────────────────────────────────────

        private void BindButtons()
        {
            _galleryTabButton?.onClick.AddListener(() => { ShowPanel(_galleryPanel);      RefreshGallery(); });
            _challengeTabButton?.onClick.AddListener(() => { ShowPanel(_challengePanel);  RefreshChallenges(); });
            _contestTabButton?.onClick.AddListener(() => { ShowPanel(_contestPanel);      RefreshContests(); });
            _filterTabButton?.onClick.AddListener(() => ShowPanel(_filterEditorPanel));
            _settingsTabButton?.onClick.AddListener(() => ShowPanel(_settingsPanel));
            _closeButton?.onClick.AddListener(() => gameObject.SetActive(false));

            _submitButton?.onClick.AddListener(OnSubmitButtonClicked);
            _voteButton?.onClick.AddListener(OnVoteButtonClicked);
            _applyFilterButton?.onClick.AddListener(OnApplyFilterClicked);

            _aiAssistDropdown?.onValueChanged.AddListener(OnAIAssistLevelChanged);
        }

        private void ShowPanel(GameObject panel)
        {
            GameObject[] all =
            {
                _galleryPanel, _challengePanel, _contestPanel,
                _filterEditorPanel, _settingsPanel
            };

            foreach (var p in all)
                if (p != null) p.SetActive(p == panel);
        }

        // ── Gallery ───────────────────────────────────────────────────────────────

        private void RefreshGallery()
        {
            if (_galleryContent == null || _galleryItemPrefab == null) return;

            foreach (Transform child in _galleryContent)
                Destroy(child.gameObject);

            // Gallery data would be retrieved from PhotoGalleryManager when available.
            // Placeholder: create a few dummy entries.
            for (int i = 0; i < 9; i++)
            {
                var item = Instantiate(_galleryItemPrefab, _galleryContent);
                var label = item.GetComponentInChildren<Text>();
                if (label != null) label.text = $"Photo {i + 1}";
            }
        }

        // ── Challenges ────────────────────────────────────────────────────────────

        private void RefreshChallenges()
        {
            if (_challengeListContent == null || _challengeItemPrefab == null) return;

            foreach (Transform child in _challengeListContent)
                Destroy(child.gameObject);

            if (_contestManager == null) return;

            foreach (var contest in _contestManager.GetActiveContests())
            {
                var item  = Instantiate(_challengeItemPrefab, _challengeListContent);
                var label = item.GetComponentInChildren<Text>();
                if (label != null) label.text = contest.challenge.title;
            }
        }

        // ── Contest ───────────────────────────────────────────────────────────────

        private void RefreshContests()
        {
            if (_contestManager == null) return;

            var active = _contestManager.GetActiveContests();
            if (active.Count == 0) return;

            _selectedContestId = active[0].challenge.challengeId;

            if (_contestTitleText != null)
                _contestTitleText.text = active[0].challenge.title;

            RefreshLeaderboard(_selectedContestId);
        }

        private void RefreshLeaderboard(string contestId)
        {
            if (_leaderboardContent == null || _leaderboardItemPrefab == null) return;

            foreach (Transform child in _leaderboardContent)
                Destroy(child.gameObject);

            if (_contestManager == null) return;

            var entries = _contestManager.GetLeaderboard(contestId);
            for (int i = 0; i < Mathf.Min(entries.Count, AdvancedPhotographyConfig.ContestLeaderboardPageSize); i++)
            {
                var item  = Instantiate(_leaderboardItemPrefab, _leaderboardContent);
                var label = item.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = $"#{i + 1}  Score: {entries[i].finalScore:0.00}";
            }
        }

        private void OnSubmitButtonClicked()
        {
            // In a real implementation, the current photo and metadata would be retrieved.
            Debug.Log("[SWEF] AdvancedPhotographyUI: submit button clicked.");
        }

        private void OnVoteButtonClicked()
        {
            Debug.Log("[SWEF] AdvancedPhotographyUI: vote button clicked.");
        }

        // ── Filter Editor ─────────────────────────────────────────────────────────

        private void OnApplyFilterClicked()
        {
            float tint       = _tintSlider       != null ? _tintSlider.value       : 0f;
            float saturation = _saturationSlider != null ? _saturationSlider.value : 1f;
            float contrast   = _contrastSlider   != null ? _contrastSlider.value   : 1f;
            float vignette   = _vignetteSlider   != null ? _vignetteSlider.value   : 0f;
            float exposure   = _exposureSlider   != null ? _exposureSlider.value   : 0f;

            Debug.Log($"[SWEF] AdvancedPhotographyUI: apply filter — " +
                      $"tint={tint:0.00} sat={saturation:0.00} con={contrast:0.00} " +
                      $"vig={vignette:0.00} exp={exposure:0.00}");
        }

        // ── Settings ──────────────────────────────────────────────────────────────

        private void OnAIAssistLevelChanged(int index)
        {
            if (_aiAssistant == null) return;

            AIAssistLevel level = (AIAssistLevel)index;
            _aiAssistant.SetAssistLevel(level);
            Debug.Log($"[SWEF] AdvancedPhotographyUI: AI assist level → {level}");
        }
    }
}
