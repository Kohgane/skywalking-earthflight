// CompetitiveRacingUI.cs — SWEF Competitive Racing & Time Trial System (Phase 88)
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.CompetitiveRacing
{
    /// <summary>
    /// Phase 88 — Pre-race menu and course browser UI.  Allows the player to browse,
    /// filter, and launch race courses; choose a race mode and ghost opponent; view
    /// seasonal highlights; and review race results after finishing.
    ///
    /// <para>Manages three main sub-panels:
    /// <list type="bullet">
    ///   <item>Course browser (list + filters + detail card)</item>
    ///   <item>Season overview (current season, featured courses, rewards)</item>
    ///   <item>Race results (splits, medal, replay save prompt)</item>
    /// </list>
    /// </para>
    /// </summary>
    public class CompetitiveRacingUI : MonoBehaviour
    {
        // ── Inspector — Navigation ────────────────────────────────────────────────

        [Header("Main Panels")]
        [SerializeField] private GameObject _browserPanel;
        [SerializeField] private GameObject _detailPanel;
        [SerializeField] private GameObject _seasonPanel;
        [SerializeField] private GameObject _resultsPanel;

        // ── Inspector — Browser ───────────────────────────────────────────────────

        [Header("Course Browser")]
        [SerializeField] private Transform     _courseListContainer;
        [SerializeField] private GameObject    _courseListItemPrefab;

        [Header("Filters")]
        [SerializeField] private Dropdown _modeFilterDropdown;
        [SerializeField] private Dropdown _difficultyFilterDropdown;
        [SerializeField] private Dropdown _environmentFilterDropdown;

        // ── Inspector — Detail View ───────────────────────────────────────────────

        [Header("Detail View")]
        [SerializeField] private Image  _coursePreviewImage;
        [SerializeField] private Text   _courseNameText;
        [SerializeField] private Text   _courseDescriptionText;
        [SerializeField] private Text   _medalTimesText;
        [SerializeField] private Text   _personalBestText;
        [SerializeField] private Text   _leaderboardPreviewText;
        [SerializeField] private Dropdown _raceModeDropdown;
        [SerializeField] private Dropdown _ghostSelectionDropdown;
        [SerializeField] private Button   _startRaceButton;
        [SerializeField] private Button   _openEditorButton;

        // ── Inspector — Season Panel ──────────────────────────────────────────────

        [Header("Season Overview")]
        [SerializeField] private Text      _seasonNameText;
        [SerializeField] private Text      _seasonDatesText;
        [SerializeField] private Transform _featuredCoursesContainer;
        [SerializeField] private GameObject _featuredCourseItemPrefab;

        // ── Inspector — Results Panel ─────────────────────────────────────────────

        [Header("Race Results")]
        [SerializeField] private Text   _resultsTotalTimeText;
        [SerializeField] private Text   _resultsMedalText;
        [SerializeField] private Text   _resultsPBText;
        [SerializeField] private Transform _splitsContainer;
        [SerializeField] private GameObject _splitRowPrefab;
        [SerializeField] private Button   _saveReplayButton;
        [SerializeField] private Button   _returnToMenuButton;

        // ── Data ──────────────────────────────────────────────────────────────────

        private List<RaceCourseData> _allCourses = new List<RaceCourseData>();
        private RaceCourseData       _selectedCourse;
        private RaceMode             _selectedMode   = RaceMode.TimeTrial;
        private CourseEditorController _editorController;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _editorController = FindFirstObjectByType<CourseEditorController>();
            SetupButtons();
            SetupDropdowns();
        }

        private void OnEnable()
        {
            if (RaceManager.Instance != null)
                RaceManager.Instance.OnRaceFinished += ShowResultsScreen;

            if (SeasonalLeaderboardManager.Instance != null)
                SeasonalLeaderboardManager.Instance.OnSeasonChanged += RefreshSeasonPanel;
        }

        private void OnDisable()
        {
            if (RaceManager.Instance != null)
                RaceManager.Instance.OnRaceFinished -= ShowResultsScreen;

            if (SeasonalLeaderboardManager.Instance != null)
                SeasonalLeaderboardManager.Instance.OnSeasonChanged -= RefreshSeasonPanel;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Populates the browser with all available <paramref name="courses"/>.</summary>
        public void PopulateCourseList(List<RaceCourseData> courses)
        {
            _allCourses = courses ?? new List<RaceCourseData>();
            RefreshBrowserList();
        }

        /// <summary>Navigates to the browser panel.</summary>
        public void ShowBrowser() => NavigateTo(_browserPanel);

        /// <summary>Navigates to the season overview panel.</summary>
        public void ShowSeasonPanel()
        {
            NavigateTo(_seasonPanel);
            RefreshSeasonPanel(SeasonalLeaderboardManager.Instance?.currentSeason);
        }

        // ── Private Helpers ───────────────────────────────────────────────────────

        private void SetupButtons()
        {
            _startRaceButton?.onClick.AddListener(OnStartRacePressed);
            _openEditorButton?.onClick.AddListener(OnOpenEditorPressed);
            _saveReplayButton?.onClick.AddListener(OnSaveReplayPressed);
            _returnToMenuButton?.onClick.AddListener(() => NavigateTo(_browserPanel));
        }

        private void SetupDropdowns()
        {
            if (_modeFilterDropdown != null)
            {
                _modeFilterDropdown.ClearOptions();
                _modeFilterDropdown.AddOptions(new List<string>(Enum.GetNames(typeof(RaceMode))));
                _modeFilterDropdown.onValueChanged.AddListener(_ => RefreshBrowserList());
            }

            if (_difficultyFilterDropdown != null)
            {
                _difficultyFilterDropdown.ClearOptions();
                _difficultyFilterDropdown.AddOptions(new List<string>(Enum.GetNames(typeof(CourseDifficulty))));
                _difficultyFilterDropdown.onValueChanged.AddListener(_ => RefreshBrowserList());
            }

            if (_environmentFilterDropdown != null)
            {
                _environmentFilterDropdown.ClearOptions();
                _environmentFilterDropdown.AddOptions(new List<string>(Enum.GetNames(typeof(CourseEnvironment))));
                _environmentFilterDropdown.onValueChanged.AddListener(_ => RefreshBrowserList());
            }

            if (_raceModeDropdown != null)
            {
                _raceModeDropdown.ClearOptions();
                _raceModeDropdown.AddOptions(new List<string>(Enum.GetNames(typeof(RaceMode))));
                _raceModeDropdown.onValueChanged.AddListener(idx =>
                    _selectedMode = (RaceMode)idx);
            }
        }

        private void RefreshBrowserList()
        {
            if (_courseListContainer == null || _courseListItemPrefab == null) return;

            // Clear existing items
            foreach (Transform child in _courseListContainer)
                Destroy(child.gameObject);

            foreach (var courseData in _allCourses)
            {
                var go   = Instantiate(_courseListItemPrefab, _courseListContainer);
                var nameText = go.GetComponentInChildren<Text>();
                if (nameText != null)
                    nameText.text = courseData.course.courseName;

                var btn = go.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    var captured = courseData;
                    btn.onClick.AddListener(() => SelectCourse(captured));
                }
            }
        }

        private void SelectCourse(RaceCourseData courseData)
        {
            _selectedCourse = courseData;
            ShowDetailPanel(courseData);
        }

        private void ShowDetailPanel(RaceCourseData courseData)
        {
            if (courseData == null) return;
            NavigateTo(_detailPanel);

            var c = courseData.course;
            if (_coursePreviewImage != null)
                _coursePreviewImage.sprite = courseData.coursePreview;
            if (_courseNameText        != null) _courseNameText.text        = c.courseName;
            if (_courseDescriptionText != null) _courseDescriptionText.text = c.description;
            if (_medalTimesText        != null)
                _medalTimesText.text = $"🥇 {FormatTime(c.goldTime)}  🥈 {FormatTime(c.silverTime)}  🥉 {FormatTime(c.bronzeTime)}";
        }

        private void ShowResultsScreen(RaceResult result)
        {
            NavigateTo(_resultsPanel);

            if (_resultsTotalTimeText != null)
                _resultsTotalTimeText.text = FormatTime(result.totalTime);

            if (_resultsMedalText != null)
            {
                if (_selectedCourse != null)
                {
                    float t = result.totalTime;
                    var c   = _selectedCourse.course;
                    if (t <= c.goldTime)        _resultsMedalText.text = "🥇 GOLD";
                    else if (t <= c.silverTime) _resultsMedalText.text = "🥈 SILVER";
                    else if (t <= c.bronzeTime) _resultsMedalText.text = "🥉 BRONZE";
                    else                        _resultsMedalText.text = "No Medal";
                }
            }

            if (_resultsPBText != null)
                _resultsPBText.text = result.isPersonalBest ? "★ NEW PERSONAL BEST!" : "";

            PopulateSplitsPanel(result);
        }

        private void PopulateSplitsPanel(RaceResult result)
        {
            if (_splitsContainer == null || _splitRowPrefab == null) return;

            foreach (Transform child in _splitsContainer)
                Destroy(child.gameObject);

            foreach (var split in result.splits)
            {
                var row  = Instantiate(_splitRowPrefab, _splitsContainer);
                var text = row.GetComponentInChildren<Text>();
                if (text != null)
                {
                    string delta = split.deltaToBest < 0
                        ? $"<color=green>-{Mathf.Abs(split.deltaToBest):F2}s</color>"
                        : $"<color=red>+{split.deltaToBest:F2}s</color>";
                    text.text = $"CP {split.checkpointIndex + 1}  {FormatTime(split.elapsedTime)}  {delta}";
                }
            }
        }

        private void RefreshSeasonPanel(SeasonEntry season)
        {
            if (season == null) return;
            if (_seasonNameText  != null) _seasonNameText.text  = $"Season: {season.season} {season.year}";
            if (_seasonDatesText != null)
                _seasonDatesText.text = $"{season.startDate:MMM d} – {season.endDate:MMM d, yyyy}";
        }

        private void OnStartRacePressed()
        {
            if (_selectedCourse == null || RaceManager.Instance == null) return;
            RaceManager.Instance.StartRace(_selectedCourse.course, _selectedMode);
            gameObject.SetActive(false);

            CompetitiveRacingAnalytics.RecordRaceStart(_selectedCourse.course.courseId, _selectedMode);
        }

        private void OnOpenEditorPressed()
        {
            if (_editorController == null) return;

            if (_selectedCourse != null)
                _editorController.LoadCourse(_selectedCourse.course);
            else
                _editorController.CreateNewCourse();

            CompetitiveRacingAnalytics.RecordCourseCreated("new");
        }

        private void OnSaveReplayPressed()
        {
            Debug.Log("[SWEF] CompetitiveRacingUI: Save replay requested.");
        }

        private void NavigateTo(GameObject panel)
        {
            if (_browserPanel  != null) _browserPanel.SetActive(panel == _browserPanel);
            if (_detailPanel   != null) _detailPanel.SetActive(panel == _detailPanel);
            if (_seasonPanel   != null) _seasonPanel.SetActive(panel == _seasonPanel);
            if (_resultsPanel  != null) _resultsPanel.SetActive(panel == _resultsPanel);
        }

        private static string FormatTime(float seconds)
        {
            int m  = (int)seconds / 60;
            int s  = (int)seconds % 60;
            int ms = (int)((seconds % 1f) * 100);
            return $"{m}:{s:00}.{ms:00}";
        }
    }
}
