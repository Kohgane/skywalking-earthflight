// AcademyUIController.cs — SWEF Flight Academy & Certification System (Phase 104)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR || SWEF_TMPRO_AVAILABLE
using TMPro;
#endif

namespace SWEF.Academy
{
    /// <summary>
    /// UI controller for the Academy menu: handles panel navigation, lesson selection,
    /// progress display, and the certificate gallery.
    /// Requires a <see cref="FlightAcademyManager"/> singleton to be present in the scene.
    /// </summary>
    public class AcademyUIController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject curriculumListPanel;
        [SerializeField] private GameObject lessonListPanel;
        [SerializeField] private GameObject theoryQuizPanel;
        [SerializeField] private GameObject practicalBriefingPanel;
        [SerializeField] private GameObject debriefPanel;
        [SerializeField] private GameObject certificateGalleryPanel;

        [Header("Curriculum List")]
        [SerializeField] private Transform curriculumListContainer;
        [SerializeField] private GameObject curriculumItemPrefab;

        [Header("Lesson List")]
        [SerializeField] private Transform lessonListContainer;
        [SerializeField] private GameObject lessonItemPrefab;

#if UNITY_EDITOR || SWEF_TMPRO_AVAILABLE
        [Header("Debrief Labels")]
        [SerializeField] private TextMeshProUGUI debriefTitleLabel;
        [SerializeField] private TextMeshProUGUI debriefTheoryScoreLabel;
        [SerializeField] private TextMeshProUGUI debriefPracticalScoreLabel;
        [SerializeField] private TextMeshProUGUI debriefOverallLabel;
        [SerializeField] private TextMeshProUGUI debriefXpLabel;

        [Header("Certificate Gallery")]
        [SerializeField] private Transform certificateGalleryContainer;
        [SerializeField] private GameObject certificateItemPrefab;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI pilotNameLabel;
        [SerializeField] private TextMeshProUGUI highestTierLabel;
        [SerializeField] private TextMeshProUGUI totalXpLabel;
#endif

        // ── State ──────────────────────────────────────────────────────────────
        private FlightAcademyManager _manager;
        private string               _selectedCurriculumId;

        // ── Unity lifecycle ────────────────────────────────────────────────────
        private void Awake()
        {
            _manager = FlightAcademyManager.Instance;
            if (_manager == null)
                Debug.LogError("[AcademyUI] FlightAcademyManager not found in scene.");
        }

        private void OnEnable()
        {
            if (_manager != null)
            {
                _manager.OnSessionCompleted  += OnSessionCompleted;
                _manager.OnCertificateIssued += OnCertificateIssued;
            }
            ShowPanel(mainMenuPanel);
        }

        private void OnDisable()
        {
            if (_manager != null)
            {
                _manager.OnSessionCompleted  -= OnSessionCompleted;
                _manager.OnCertificateIssued -= OnCertificateIssued;
            }
        }

        // ── Navigation ─────────────────────────────────────────────────────────

        /// <summary>Opens the curriculum selection panel.</summary>
        public void ShowCurriculaList()
        {
            ShowPanel(curriculumListPanel);
            PopulateCurriculumList();
        }

        /// <summary>Opens the lesson list for the selected curriculum.</summary>
        public void ShowLessonList(string curriculumId)
        {
            _selectedCurriculumId = curriculumId;
            ShowPanel(lessonListPanel);
            PopulateLessonList(curriculumId);
        }

        /// <summary>Opens the certificate gallery panel.</summary>
        public void ShowCertificateGallery()
        {
            ShowPanel(certificateGalleryPanel);
            PopulateCertificateGallery();
        }

        /// <summary>Returns to the main menu panel.</summary>
        public void ShowMainMenu()
        {
            ShowPanel(mainMenuPanel);
            RefreshHeader();
        }

        /// <summary>Requests the academy manager to start the selected lesson.</summary>
        public void RequestStartLesson(string lessonId)
        {
            if (_manager == null || string.IsNullOrEmpty(_selectedCurriculumId)) return;
            _manager.StartLesson(_selectedCurriculumId, lessonId);
        }

        /// <summary>Requests enrolment in the given curriculum then refreshes the list.</summary>
        public void RequestEnrolment(string curriculumId)
        {
            _manager?.EnrollInCurriculum(curriculumId);
            PopulateLessonList(curriculumId);
        }

        // ── Private UI helpers ─────────────────────────────────────────────────

        private void ShowPanel(GameObject target)
        {
            foreach (var p in new[] {
                mainMenuPanel, curriculumListPanel, lessonListPanel,
                theoryQuizPanel, practicalBriefingPanel, debriefPanel, certificateGalleryPanel })
            {
                if (p != null) p.SetActive(p == target);
            }
        }

        private void RefreshHeader()
        {
            if (_manager == null) return;
#if UNITY_EDITOR || SWEF_TMPRO_AVAILABLE
            var data = _manager.ProgressTracker?.Data;
            if (data == null) return;
            if (pilotNameLabel)    pilotNameLabel.text    = data.pilotName;
            if (highestTierLabel)  highestTierLabel.text  = data.highestTier.ToString();
            if (totalXpLabel)      totalXpLabel.text      = $"{data.totalXpEarned} XP";
#endif
        }

        private void PopulateCurriculumList()
        {
            if (curriculumListContainer == null || curriculumItemPrefab == null) return;
            ClearChildren(curriculumListContainer);

            if (_manager == null) return;
            foreach (var curriculum in _manager.GetAvailableCurricula())
            {
                var item = Instantiate(curriculumItemPrefab, curriculumListContainer);
                // Wire up button if a standard Button component is present
                var btn = item.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    string id = curriculum.curriculumId;  // capture for closure
                    btn.onClick.AddListener(() => ShowLessonList(id));
                }
#if UNITY_EDITOR || SWEF_TMPRO_AVAILABLE
                var lbl = item.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl != null) lbl.text = curriculum.curriculumName;
#endif
            }
        }

        private void PopulateLessonList(string curriculumId)
        {
            if (lessonListContainer == null || lessonItemPrefab == null || _manager == null) return;
            ClearChildren(lessonListContainer);

            var curriculum = _manager.Config?.curricula;
            if (curriculum == null) return;
            foreach (var c in curriculum)
            {
                if (c == null || c.curriculumId != curriculumId) continue;
                foreach (var lesson in c.lessons)
                {
                    var item = Instantiate(lessonItemPrefab, lessonListContainer);
                    var btn  = item.GetComponentInChildren<Button>();
                    if (btn != null)
                    {
                        string lid = lesson.lessonId;
                        btn.onClick.AddListener(() => RequestStartLesson(lid));

                        var status = _manager.ProgressTracker.GetLessonStatus(curriculumId, lid);
                        btn.interactable = status != LessonStatus.Locked;
                    }
#if UNITY_EDITOR || SWEF_TMPRO_AVAILABLE
                    var lbl = item.GetComponentInChildren<TextMeshProUGUI>();
                    if (lbl != null) lbl.text = lesson.title;
#endif
                }
            }
        }

        private void PopulateCertificateGallery()
        {
            if (certificateGalleryContainer == null || _manager == null) return;
            ClearChildren(certificateGalleryContainer);

            foreach (var cert in _manager.CertificationManager.GetAllCertificates())
            {
                if (certificateItemPrefab == null) break;
                var item = Instantiate(certificateItemPrefab, certificateGalleryContainer);
#if UNITY_EDITOR || SWEF_TMPRO_AVAILABLE
                var lbl = item.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl != null) lbl.text = cert.certificateName;
#endif
                // Load badge sprite at runtime if resource path is set
                if (!string.IsNullOrEmpty(cert.badgeResourcePath))
                {
                    var sprite = Resources.Load<Sprite>(cert.badgeResourcePath);
                    if (sprite != null)
                    {
                        var img = item.GetComponentInChildren<Image>();
                        if (img != null) img.sprite = sprite;
                    }
                }
            }
        }

        private void OnSessionCompleted(SessionResult result)
        {
#if UNITY_EDITOR || SWEF_TMPRO_AVAILABLE
            if (debriefTitleLabel)       debriefTitleLabel.text       = result.lessonCompleted ? "Lesson Passed!" : "Lesson Failed";
            if (debriefTheoryScoreLabel) debriefTheoryScoreLabel.text = $"Theory: {result.theoryScore:F1}%";
            if (debriefPracticalScoreLabel) debriefPracticalScoreLabel.text = $"Practical: {result.practicalScore:F1}%";
            if (debriefXpLabel)          debriefXpLabel.text          = result.lessonCompleted ? $"+{result.xpAwarded} XP" : "—";
#endif
            ShowPanel(debriefPanel);
        }

        private void OnCertificateIssued(CertificateData cert)
        {
            Debug.Log($"[AcademyUI] New certificate issued: {cert.certificateName}");
            // The certificate gallery will refresh next time the player navigates to it.
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }
    }
}
