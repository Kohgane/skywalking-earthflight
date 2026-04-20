using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.FlightSchool
{
    /// <summary>
    /// UI controller for the Training &amp; Flight School system.
    /// Manages the curriculum browser, lesson detail panel, certification panel,
    /// and the in-flight HUD overlay.
    /// Wire up serialised panel references in the Inspector, then call
    /// <see cref="RefreshUI"/> whenever the underlying data changes.
    /// </summary>
    public class FlightSchoolUI : MonoBehaviour
    {
        // ── Inspector — panels ───────────────────────────────────────────────────

        [Header("Panels")]
        [SerializeField] private GameObject curriculumPanel;
        [SerializeField] private GameObject lessonDetailPanel;
        [SerializeField] private GameObject certificationPanel;
        [SerializeField] private GameObject inFlightHudOverlay;

        [Header("Phase 84 — Debrief Panel")]
        [SerializeField] private GameObject debriefPanel;
        [SerializeField] private Text   debriefLessonTitle;
        [SerializeField] private Text   debriefFinalScore;
        [SerializeField] private Text   debriefLetterGrade;
        [SerializeField] private Text   debriefDuration;
        [SerializeField] private Text   debriefPersonalBest;
        [SerializeField] private Transform debriefCriteriaRoot;
        [SerializeField] private GameObject debriefCriteriaItemPrefab;
        [SerializeField] private Transform debriefTipsRoot;
        [SerializeField] private GameObject debriefTipItemPrefab;
        [SerializeField] private Button debriefCloseButton;

        [Header("Phase 84 — Skill Tree Panel")]
        [SerializeField] private GameObject skillTreePanel;
        [SerializeField] private Transform skillTreeNodeRoot;
        [SerializeField] private GameObject skillTreeNodePrefab;
        [SerializeField] private Text   skillTreeProgressText;

        [Header("Phase 84 — Exam UI")]
        [SerializeField] private GameObject examPanel;
        [SerializeField] private Text   examTitleText;
        [SerializeField] private Text   examProgressText;
        [SerializeField] private Button examStartButton;

        // ── Inspector — curriculum panel ──────────────────────────────────────────

        [Header("Curriculum Panel")]
        [SerializeField] private Transform categoryListRoot;
        [SerializeField] private GameObject categoryItemPrefab;

        // ── Inspector — lesson detail panel ──────────────────────────────────────

        [Header("Lesson Detail Panel")]
        [SerializeField] private Text   lessonTitleText;
        [SerializeField] private Text   lessonDescriptionText;
        [SerializeField] private Text   lessonDifficultyText;
        [SerializeField] private Text   lessonEstimatedTimeText;
        [SerializeField] private Text   lessonXpRewardText;
        [SerializeField] private Text   lessonBestScoreText;
        [SerializeField] private Text   lessonBriefingText;
        [SerializeField] private Transform objectiveListRoot;
        [SerializeField] private GameObject objectiveItemPrefab;
        [SerializeField] private Button startLessonButton;
        [SerializeField] private Button replayLessonButton;

        // ── Inspector — certification panel ─────────────────────────────────────

        [Header("Certification Panel")]
        [SerializeField] private Transform certificationListRoot;
        [SerializeField] private GameObject certificationItemPrefab;

        // ── Inspector — in-flight HUD overlay ───────────────────────────────────

        [Header("In-Flight HUD Overlay")]
        [SerializeField] private Text   hudLessonTitleText;
        [SerializeField] private Text   hudCurrentObjectiveText;
        [SerializeField] private Slider hudObjectiveProgressSlider;
        [SerializeField] private Text   hudHintText;
        [SerializeField] private Text   hudOverallProgressText;

        // ── Dependencies ─────────────────────────────────────────────────────────

        [Header("Dependencies")]
        [SerializeField] private FlightSchoolManager schoolManager;
        [SerializeField] private FlightInstructor    instructor;
        [SerializeField] private DebriefingController debriefingController;
        [SerializeField] private SkillTreeController  skillTreeController;
        [SerializeField] private CertificationExamController examController;

        // ── Internal ─────────────────────────────────────────────────────────────

        private string _selectedLessonId;
        private Coroutine _progressBarCoroutine;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (schoolManager == null)
                schoolManager = FlightSchoolManager.Instance;
        }

        private void OnEnable()
        {
            if (schoolManager != null)
            {
                schoolManager.OnLessonCompleted    += HandleLessonCompleted;
                schoolManager.OnCertificationEarned += HandleCertificationEarned;
            }
            if (debriefingController != null)
                debriefingController.OnDebriefReady += HandleDebriefReady;
            if (examController != null)
            {
                examController.OnExamPassed += HandleExamPassed;
                examController.OnExamFailed += HandleExamFailed;
            }
        }

        private void OnDisable()
        {
            if (schoolManager != null)
            {
                schoolManager.OnLessonCompleted    -= HandleLessonCompleted;
                schoolManager.OnCertificationEarned -= HandleCertificationEarned;
            }
            if (debriefingController != null)
                debriefingController.OnDebriefReady -= HandleDebriefReady;
            if (examController != null)
            {
                examController.OnExamPassed -= HandleExamPassed;
                examController.OnExamFailed -= HandleExamFailed;
            }
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Shows the curriculum overview panel with all categories, lesson counts, and
        /// aggregate progress bars.
        /// </summary>
        public void ShowCurriculum()
        {
            SetPanelActive(curriculumPanel, true);
            SetPanelActive(lessonDetailPanel, false);
            SetPanelActive(certificationPanel, false);

            if (categoryListRoot == null) return;

            // Clear existing entries
            foreach (Transform child in categoryListRoot)
                Destroy(child.gameObject);

            foreach (LessonCategory category in Enum.GetValues(typeof(LessonCategory)))
                SpawnCategoryItem(category);
        }

        /// <summary>
        /// Shows all lessons belonging to <paramref name="category"/> in the curriculum panel.
        /// </summary>
        /// <param name="category">Category to filter by.</param>
        public void ShowCategory(LessonCategory category)
        {
            SetPanelActive(curriculumPanel, true);
            SetPanelActive(lessonDetailPanel, false);

            if (categoryListRoot == null || schoolManager == null) return;

            foreach (Transform child in categoryListRoot)
                Destroy(child.gameObject);

            var lessons = schoolManager.GetLessonsByCategory(category);
            foreach (var lesson in lessons)
                SpawnLessonItem(lesson);
        }

        /// <summary>
        /// Opens the lesson detail panel for <paramref name="lessonId"/>, showing full
        /// lesson info, objectives, best score, and action buttons.
        /// </summary>
        /// <param name="lessonId">ID of the lesson to display.</param>
        public void ShowLessonDetail(string lessonId)
        {
            if (schoolManager == null) return;

            FlightLesson lesson = null;
            foreach (var l in schoolManager.allLessons)
                if (l.lessonId == lessonId) { lesson = l; break; }

            if (lesson == null) return;

            _selectedLessonId = lessonId;

            SetPanelActive(lessonDetailPanel, true);

            SafeSetText(lessonTitleText,         lesson.title);
            SafeSetText(lessonDescriptionText,   lesson.description);
            SafeSetText(lessonDifficultyText,     lesson.difficulty.ToString());
            SafeSetText(lessonEstimatedTimeText,  $"{lesson.estimatedMinutes} min");
            SafeSetText(lessonXpRewardText,       $"+{lesson.xpReward} XP");
            SafeSetText(lessonBestScoreText,      lesson.completionCount > 0 ? $"Best: {lesson.bestScore:F0}" : "Not yet completed");
            SafeSetText(lessonBriefingText,       lesson.briefingText);

            // Objectives list
            if (objectiveListRoot != null)
            {
                foreach (Transform child in objectiveListRoot)
                    Destroy(child.gameObject);

                if (lesson.objectives != null)
                    foreach (var obj in lesson.objectives)
                        SpawnObjectiveItem(obj);
            }

            // Buttons
            bool canStart = lesson.status == LessonStatus.Available;
            bool canReplay = lesson.status == LessonStatus.Completed
                          || lesson.status == LessonStatus.Mastered;

            if (startLessonButton != null)
            {
                startLessonButton.gameObject.SetActive(canStart);
                startLessonButton.onClick.RemoveAllListeners();
                startLessonButton.onClick.AddListener(() => OnStartLessonClicked(lessonId));
            }

            if (replayLessonButton != null)
            {
                replayLessonButton.gameObject.SetActive(canReplay);
                replayLessonButton.onClick.RemoveAllListeners();
                replayLessonButton.onClick.AddListener(() => OnReplayLessonClicked(lessonId));
            }
        }

        /// <summary>
        /// Shows the certification panel with all certifications and their progress.
        /// </summary>
        public void ShowCertifications()
        {
            SetPanelActive(certificationPanel, true);
            SetPanelActive(curriculumPanel, false);
            SetPanelActive(lessonDetailPanel, false);

            if (certificationListRoot == null || schoolManager == null) return;

            foreach (Transform child in certificationListRoot)
                Destroy(child.gameObject);

            var completed = new List<string>();
            foreach (var l in schoolManager.allLessons)
                if (l.status == LessonStatus.Completed || l.status == LessonStatus.Mastered)
                    completed.Add(l.lessonId);

            foreach (var cert in schoolManager.certifications)
                SpawnCertificationItem(cert, completed);
        }

        /// <summary>
        /// Activates the in-flight HUD overlay for <paramref name="lesson"/>,
        /// showing active objectives and hint messages.
        /// </summary>
        /// <param name="lesson">The lesson currently in progress.</param>
        public void ShowInFlightOverlay(FlightLesson lesson)
        {
            if (lesson == null) return;
            SetPanelActive(inFlightHudOverlay, true);

            SafeSetText(hudLessonTitleText, lesson.title);
            UpdateInFlightOverlay(lesson, 0);
        }

        /// <summary>Hides the in-flight HUD overlay.</summary>
        public void HideInFlightOverlay()
        {
            SetPanelActive(inFlightHudOverlay, false);
        }

        /// <summary>
        /// Called when the player taps the Start button in the lesson detail panel.
        /// Delegates to <see cref="FlightSchoolManager.StartLesson"/>.
        /// </summary>
        /// <param name="lessonId">Lesson to start.</param>
        public void OnStartLessonClicked(string lessonId)
        {
            schoolManager?.StartLesson(lessonId);

            FlightLesson lesson = null;
            if (schoolManager != null)
                foreach (var l in schoolManager.allLessons)
                    if (l.lessonId == lessonId) { lesson = l; break; }

            if (lesson != null)
                ShowInFlightOverlay(lesson);
        }

        /// <summary>
        /// Called when the player taps the Replay button in the lesson detail panel.
        /// Resets progress and starts the lesson again.
        /// </summary>
        /// <param name="lessonId">Lesson to replay.</param>
        public void OnReplayLessonClicked(string lessonId)
        {
            schoolManager?.ResetLesson(lessonId);
            OnStartLessonClicked(lessonId);
        }

        /// <summary>Rebuilds all visible panels to reflect the latest data.</summary>
        public void RefreshUI()
        {
            ShowCurriculum();
            if (!string.IsNullOrEmpty(_selectedLessonId))
                ShowLessonDetail(_selectedLessonId);
        }

        /// <summary>
        /// Animates the progress bar for <paramref name="lessonId"/> to display
        /// <paramref name="progress"/> (0–1).
        /// </summary>
        /// <param name="lessonId">Lesson whose progress bar to update.</param>
        /// <param name="progress">Target progress value in [0, 1].</param>
        public void UpdateProgressBar(string lessonId, float progress)
        {
            if (_progressBarCoroutine != null)
                StopCoroutine(_progressBarCoroutine);

            _progressBarCoroutine = StartCoroutine(AnimateProgressBar(progress));

            if (hudObjectiveProgressSlider != null
                && instructor != null
                && instructor.activeLesson?.lessonId == lessonId)
            {
                hudObjectiveProgressSlider.value = progress;
            }
        }

        // ── Internal helpers ─────────────────────────────────────────────────────

        private void UpdateInFlightOverlay(FlightLesson lesson, int objectiveIndex)
        {
            if (lesson.objectives == null || lesson.objectives.Count == 0)
            {
                SafeSetText(hudCurrentObjectiveText, "No objectives.");
                return;
            }

            if (objectiveIndex < lesson.objectives.Count)
            {
                var obj = lesson.objectives[objectiveIndex];
                SafeSetText(hudCurrentObjectiveText, obj.description);

                if (hudObjectiveProgressSlider != null)
                    hudObjectiveProgressSlider.value = obj.Progress01();
            }

            float overall = lesson.OverallProgress();
            SafeSetText(hudOverallProgressText, $"Overall: {overall * 100f:F0}%");
        }

        private void SpawnCategoryItem(LessonCategory category)
        {
            if (categoryItemPrefab == null || categoryListRoot == null) return;

            var lessons = schoolManager?.GetLessonsByCategory(category) ?? new List<FlightLesson>();
            int completed = 0;
            foreach (var l in lessons)
                if (l.status == LessonStatus.Completed || l.status == LessonStatus.Mastered) completed++;

            var go = Instantiate(categoryItemPrefab, categoryListRoot);

            // Populate common label fields by name (UI prefabs may vary)
            SetChildText(go, "TitleText",    category.ToString());
            SetChildText(go, "CountText",    $"{completed}/{lessons.Count} completed");
            SetChildText(go, "ProgressText", lessons.Count > 0 ? $"{(float)completed / lessons.Count * 100f:F0}%" : "0%");

            var btn = go.GetComponentInChildren<Button>();
            if (btn != null)
            {
                var cap = category;
                btn.onClick.AddListener(() => ShowCategory(cap));
            }
        }

        private void SpawnLessonItem(FlightLesson lesson)
        {
            if (categoryItemPrefab == null || categoryListRoot == null) return;

            var go = Instantiate(categoryItemPrefab, categoryListRoot);
            SetChildText(go, "TitleText",  lesson.title);
            SetChildText(go, "CountText",  lesson.status.ToString());

            var btn = go.GetComponentInChildren<Button>();
            if (btn != null)
            {
                var id = lesson.lessonId;
                btn.onClick.AddListener(() => ShowLessonDetail(id));
            }
        }

        private void SpawnObjectiveItem(LessonObjective obj)
        {
            if (objectiveItemPrefab == null || objectiveListRoot == null) return;

            var go = Instantiate(objectiveItemPrefab, objectiveListRoot);
            SetChildText(go, "DescriptionText", obj.description);
            SetChildText(go, "ProgressText",    $"{obj.Progress01() * 100f:F0}%");

            var toggle = go.GetComponentInChildren<Toggle>();
            if (toggle != null) toggle.isOn = obj.isCompleted;
        }

        private void SpawnCertificationItem(PilotCertification cert, List<string> completedIds)
        {
            if (certificationItemPrefab == null || certificationListRoot == null) return;

            var go = Instantiate(certificationItemPrefab, certificationListRoot);
            SetChildText(go, "TitleText",    cert.displayName);
            SetChildText(go, "StatusText",   cert.isEarned ? "Earned" : "In Progress");
            SetChildText(go, "ProgressText", $"{cert.Progress(completedIds) * 100f:F0}%");

            var slider = go.GetComponentInChildren<Slider>();
            if (slider != null) slider.value = cert.Progress(completedIds);
        }

        private IEnumerator AnimateProgressBar(float target)
        {
            if (hudObjectiveProgressSlider == null) yield break;

            float start   = hudObjectiveProgressSlider.value;
            float elapsed = 0f;
            const float duration = 0.4f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                hudObjectiveProgressSlider.value = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }

            hudObjectiveProgressSlider.value = target;
        }

        private void HandleLessonCompleted(FlightLesson lesson)
        {
            HideInFlightOverlay();
            if (!string.IsNullOrEmpty(_selectedLessonId) && _selectedLessonId == lesson.lessonId)
                ShowLessonDetail(_selectedLessonId);
        }

        private void HandleCertificationEarned(PilotCertification cert)
        {
            Debug.Log($"[FlightSchoolUI] Certification earned: {cert.displayName}");
        }

        // ── Phase 84 — Debrief ──────────────────────────────────────────────────

        private void HandleDebriefReady(DebriefingController.DebriefPayload payload)
        {
            if (payload == null) return;
            ShowDebriefPanel(payload);
        }

        /// <summary>Populates and displays the debrief panel from <paramref name="payload"/>.</summary>
        public void ShowDebriefPanel(DebriefingController.DebriefPayload payload)
        {
            if (payload == null) return;

            SetPanelActive(debriefPanel, true);
            SetPanelActive(inFlightHudOverlay, false);

            SafeSetText(debriefLessonTitle,  payload.lesson?.title);
            SafeSetText(debriefFinalScore,   $"{payload.report.finalScore:F0}/100");
            SafeSetText(debriefLetterGrade,  payload.report.letterGrade);
            SafeSetText(debriefDuration,     $"{payload.report.durationSeconds / 60f:F1} min");
            SafeSetText(debriefPersonalBest, payload.isNewPersonalBest
                ? $"NEW BEST! (prev: {payload.previousBestScore:F0})"
                : $"Best: {payload.previousBestScore:F0}");

            // Criteria breakdown
            if (debriefCriteriaRoot != null)
            {
                foreach (Transform child in debriefCriteriaRoot) Destroy(child.gameObject);
                if (debriefCriteriaItemPrefab != null)
                    foreach (var c in payload.report.criteria)
                        SpawnDebriefCriteriaItem(c);
            }

            // Tips
            if (debriefTipsRoot != null)
            {
                foreach (Transform child in debriefTipsRoot) Destroy(child.gameObject);
                if (debriefTipItemPrefab != null)
                    foreach (var tip in payload.tips)
                        SpawnDebriefTipItem(tip);
            }

            if (debriefCloseButton != null)
            {
                debriefCloseButton.onClick.RemoveAllListeners();
                debriefCloseButton.onClick.AddListener(CloseDebriefPanel);
            }
        }

        /// <summary>Hides the debrief panel and notifies the controller.</summary>
        public void CloseDebriefPanel()
        {
            SetPanelActive(debriefPanel, false);
            debriefingController?.CloseDebrief();
        }

        private void SpawnDebriefCriteriaItem(GradeCriteria c)
        {
            var go = Instantiate(debriefCriteriaItemPrefab, debriefCriteriaRoot);
            SetChildText(go, "TitleText", c.displayName);
            SetChildText(go, "ScoreText", $"{c.score:F0}");
            var slider = go.GetComponentInChildren<Slider>();
            if (slider != null) slider.value = c.score / 100f;
        }

        private void SpawnDebriefTipItem(DebriefingController.DebriefTip tip)
        {
            var go = Instantiate(debriefTipItemPrefab, debriefTipsRoot);
            SetChildText(go, "TipText", tip.message);
        }

        // ── Phase 84 — Skill Tree ───────────────────────────────────────────────

        /// <summary>Shows the skill-tree visualisation panel.</summary>
        public void ShowSkillTree()
        {
            SetPanelActive(skillTreePanel, true);
            SetPanelActive(curriculumPanel, false);
            SetPanelActive(lessonDetailPanel, false);
            SetPanelActive(certificationPanel, false);

            if (skillTreeController == null || skillTreeNodeRoot == null) return;

            foreach (Transform child in skillTreeNodeRoot) Destroy(child.gameObject);

            if (skillTreeController.Tree?.nodes == null) return;

            foreach (var node in skillTreeController.Tree.nodes)
                SpawnSkillTreeNode(node);

            SafeSetText(skillTreeProgressText,
                $"Progress: {skillTreeController.ComputeUnlockProgress() * 100f:F0}%");
        }

        private void SpawnSkillTreeNode(SkillNode node)
        {
            if (skillTreeNodePrefab == null || skillTreeNodeRoot == null) return;
            var go = Instantiate(skillTreeNodePrefab, skillTreeNodeRoot);
            SetChildText(go, "TitleText",  node.displayName);
            SetChildText(go, "StatusText", node.isUnlocked ? "Unlocked" : "Locked");

            var btn = go.GetComponentInChildren<Button>();
            if (btn != null && node.isUnlocked && !string.IsNullOrEmpty(node.lessonId))
            {
                string lessonId = node.lessonId;
                btn.onClick.AddListener(() => ShowLessonDetail(lessonId));
            }
        }

        // ── Phase 84 — Exam ─────────────────────────────────────────────────────

        /// <summary>Shows the exam panel for <paramref name="certType"/>.</summary>
        public void ShowExamPanel(CertificationType certType)
        {
            if (examController == null) return;
            var exam = examController.GetExamFor(certType);
            if (exam == null) return;

            SetPanelActive(examPanel, true);
            SafeSetText(examTitleText, exam.displayName ?? certType.ToString());
            SafeSetText(examProgressText, $"Attempts: {exam.attemptsUsed}/{exam.maxAttempts}");

            if (examStartButton != null)
            {
                bool canStart = exam.maxAttempts <= 0 || exam.attemptsUsed < exam.maxAttempts;
                examStartButton.gameObject.SetActive(canStart);
                examStartButton.onClick.RemoveAllListeners();
                var ct = certType;
                examStartButton.onClick.AddListener(() => examController.StartExam(ct));
            }
        }

        private void HandleExamPassed(CertificationExam exam)
        {
            SetPanelActive(examPanel, false);
            Debug.Log($"[FlightSchoolUI] Exam passed: {exam.displayName}");
        }

        private void HandleExamFailed(CertificationExam exam, string reason)
        {
            SafeSetText(examProgressText, $"Failed: {reason} — Attempts: {exam.attemptsUsed}/{exam.maxAttempts}");
        }

        // ── Static utilities ─────────────────────────────────────────────────────

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }

        private static void SafeSetText(Text label, string value)
        {
            if (label != null) label.text = value ?? string.Empty;
        }

        private static void SetChildText(GameObject parent, string childName, string value)
        {
            if (parent == null) return;
            var child = parent.transform.Find(childName);
            if (child == null) return;
            var lbl = child.GetComponent<Text>();
            if (lbl != null) lbl.text = value ?? string.Empty;
        }
    }
}
