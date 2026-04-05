// UGCEditorUI.cs — SWEF Phase 108: User-Generated Content (UGC) Editor
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.UGC
{
    /// <summary>
    /// Phase 108 — Full-screen editor panel that exposes project settings, the placed-object
    /// browser, trigger logic connections, test results, validation feedback, and
    /// the step-by-step publishing wizard.
    ///
    /// <para>All <see cref="SerializeField"/> references are null-safe.</para>
    /// </summary>
    public sealed class UGCEditorUI : MonoBehaviour
    {
        // ── Inspector — Panels ─────────────────────────────────────────────────

        [Header("Root")]
        [SerializeField] private GameObject _panelRoot;

        [Header("Project Settings Panel")]
        [SerializeField] private InputField _inputTitle;
        [SerializeField] private InputField _inputDescription;
        [SerializeField] private Dropdown   _dropdownType;
        [SerializeField] private Dropdown   _dropdownDifficulty;
        [SerializeField] private Dropdown   _dropdownCategory;
        [SerializeField] private InputField _inputTags;
        [SerializeField] private Button     _btnApplySettings;

        [Header("Content Browser Panel")]
        [SerializeField] private Transform  _waypointListContainer;
        [SerializeField] private Transform  _triggerListContainer;
        [SerializeField] private Transform  _zoneListContainer;
        [SerializeField] private GameObject _listItemPrefab;

        [Header("Test Results Panel")]
        [SerializeField] private GameObject _testResultsPanel;
        [SerializeField] private Text       _lblTestStatus;
        [SerializeField] private Text       _lblTestIssues;
        [SerializeField] private Text       _lblTestDifficulty;

        [Header("Validation Results Panel")]
        [SerializeField] private GameObject _validationPanel;
        [SerializeField] private Transform  _validationListContainer;
        [SerializeField] private Text       _lblQualityScore;

        [Header("Publishing Wizard Panel")]
        [SerializeField] private GameObject _publishWizardPanel;
        [SerializeField] private Text       _lblPublishStatus;
        [SerializeField] private Button     _btnConfirmPublish;
        [SerializeField] private Button     _btnCancelPublish;

        [Header("Navigation Tabs")]
        [SerializeField] private Button _tabSettings;
        [SerializeField] private Button _tabBrowser;
        [SerializeField] private Button _tabValidation;
        [SerializeField] private Button _tabPublish;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Start()
        {
            WireButtons();
            HideAll();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Shows or hides the full editor panel.</summary>
        public void SetVisible(bool visible)
        {
            if (_panelRoot != null) _panelRoot.SetActive(visible);
        }

        /// <summary>Populates the project-settings panel with the current project data.</summary>
        public void RefreshSettings()
        {
            var content = UGCEditorManager.Instance?.CurrentProject;
            if (content == null) return;

            if (_inputTitle       != null) _inputTitle.text       = content.title;
            if (_inputDescription != null) _inputDescription.text = content.description;

            // Tags as comma-separated
            if (_inputTags != null) _inputTags.text = string.Join(", ", content.tags);
        }

        /// <summary>Refreshes the validation results panel for the current project.</summary>
        public void RefreshValidation()
        {
            var content = UGCEditorManager.Instance?.CurrentProject;
            if (content == null) return;

            var result = UGCValidator.ValidateContent(content);
            if (_lblQualityScore != null)
                _lblQualityScore.text = $"Quality Score: {result.QualityScore}/100";

            PopulateValidationList(result.Issues);
        }

        /// <summary>Updates the test-results panel from the given result.</summary>
        public void ShowTestResult(TestPlayResult result)
        {
            if (_testResultsPanel != null) _testResultsPanel.SetActive(true);
            if (_lblTestStatus    != null) _lblTestStatus.text    = result.passed ? "✓ PASSED" : "✗ FAILED";
            if (_lblTestDifficulty != null) _lblTestDifficulty.text = $"Estimated: {result.estimatedDifficulty}";
            if (_lblTestIssues    != null) _lblTestIssues.text    = result.issues.Count > 0
                ? string.Join("\n", result.issues)
                : "No issues found.";
        }

        // ── Private setup ──────────────────────────────────────────────────────

        private void WireButtons()
        {
            _btnApplySettings?.onClick.AddListener(ApplySettings);
            _btnConfirmPublish?.onClick.AddListener(OnConfirmPublish);
            _btnCancelPublish?.onClick.AddListener(OnCancelPublish);

            _tabSettings?.onClick.AddListener(() => ShowTab(Tab.Settings));
            _tabBrowser?.onClick.AddListener(()   => ShowTab(Tab.Browser));
            _tabValidation?.onClick.AddListener(() => ShowTab(Tab.Validation));
            _tabPublish?.onClick.AddListener(()   => ShowTab(Tab.Publish));
        }

        private void ApplySettings()
        {
            var content = UGCEditorManager.Instance?.CurrentProject;
            if (content == null) return;

            if (_inputTitle != null)       content.title       = _inputTitle.text.Trim();
            if (_inputDescription != null) content.description = _inputDescription.text.Trim();

            if (_inputTags != null)
            {
                content.tags.Clear();
                foreach (var tag in _inputTags.text.Split(','))
                {
                    string t = tag.Trim();
                    if (!string.IsNullOrEmpty(t) && t.Length <= UGCConfig.MaxTagLength)
                        content.tags.Add(t);
                    if (content.tags.Count >= UGCConfig.MaxTagCount) break;
                }
            }

            if (UGCEditorManager.Instance != null)
                UGCEditorManager.Instance.HasUnsavedChanges = true;
        }

        private void PopulateValidationList(List<ValidationIssue> issues)
        {
            if (_validationListContainer == null || _listItemPrefab == null) return;

            foreach (Transform child in _validationListContainer)
                Destroy(child.gameObject);

            foreach (var issue in issues)
            {
                var item  = Instantiate(_listItemPrefab, _validationListContainer);
                var label = item.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = $"[{issue.severity}] {issue.message}";
            }
        }

        private void OnConfirmPublish()
        {
            var mgr    = UGCEditorManager.Instance;
            var pubMgr = UGCPublishManager.Instance;
            if (mgr?.CurrentProject == null || pubMgr == null) return;

            bool submitted = pubMgr.SubmitForReview(mgr.CurrentProject);
            if (_lblPublishStatus != null)
                _lblPublishStatus.text = submitted ? "Submitted for review!" : "Submission failed — fix errors first.";
        }

        private void OnCancelPublish()
        {
            if (_publishWizardPanel != null) _publishWizardPanel.SetActive(false);
        }

        private void HideAll()
        {
            if (_testResultsPanel   != null) _testResultsPanel.SetActive(false);
            if (_validationPanel    != null) _validationPanel.SetActive(false);
            if (_publishWizardPanel != null) _publishWizardPanel.SetActive(false);
        }

        // ── Tab navigation ─────────────────────────────────────────────────────

        private enum Tab { Settings, Browser, Validation, Publish }

        private void ShowTab(Tab tab)
        {
            HideAll();
            switch (tab)
            {
                case Tab.Settings:
                    RefreshSettings();
                    break;
                case Tab.Browser:
                    RefreshBrowser();
                    break;
                case Tab.Validation:
                    if (_validationPanel != null) _validationPanel.SetActive(true);
                    RefreshValidation();
                    break;
                case Tab.Publish:
                    if (_publishWizardPanel != null) _publishWizardPanel.SetActive(true);
                    break;
            }
        }

        private void RefreshBrowser()
        {
            var content = UGCEditorManager.Instance?.CurrentProject;
            if (content == null) return;

            PopulateObjectList(_waypointListContainer, content.waypoints.Count, "WP");
            PopulateObjectList(_triggerListContainer,  content.triggers.Count,  "TR");
            PopulateObjectList(_zoneListContainer,     content.zones.Count,     "ZN");
        }

        private void PopulateObjectList(Transform container, int count, string prefix)
        {
            if (container == null || _listItemPrefab == null) return;
            foreach (Transform child in container) Destroy(child.gameObject);

            for (int i = 0; i < count; i++)
            {
                var item  = Instantiate(_listItemPrefab, container);
                var label = item.GetComponentInChildren<Text>();
                if (label != null) label.text = $"{prefix} #{i + 1}";
            }
        }
    }
}
