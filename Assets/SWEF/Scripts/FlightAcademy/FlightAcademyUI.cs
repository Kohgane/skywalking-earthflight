using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.FlightAcademy
{
    /// <summary>
    /// Full-screen academy panel: license progression tree, per-license module grid,
    /// module detail card, certificate gallery, and overall statistics.
    /// </summary>
    public class FlightAcademyUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Navigation")]
        [SerializeField] private Button _closeButton;

        [Header("License Tree")]
        [SerializeField] private Transform _licenseTreeContainer;
        [SerializeField] private GameObject _licenseNodePrefab;

        [Header("Module Grid")]
        [SerializeField] private Transform _moduleGridContainer;
        [SerializeField] private GameObject _moduleCardPrefab;

        [Header("Module Detail")]
        [SerializeField] private GameObject _moduleDetailPanel;
        [SerializeField] private Text _moduleDetailTitle;
        [SerializeField] private Text _moduleDetailDescription;
        [SerializeField] private Text _moduleDetailBestScore;
        [SerializeField] private Button _startTrainingButton;
        [SerializeField] private Button _startExamButton;

        [Header("Certificates")]
        [SerializeField] private Transform _certificateGalleryContainer;
        [SerializeField] private GameObject _certificatePrefab;

        [Header("Statistics")]
        [SerializeField] private Text _flightHoursText;
        [SerializeField] private Text _examPassedText;
        [SerializeField] private Text _avgScoreText;

        // ── State ─────────────────────────────────────────────────────────────────
        private TrainingModule _selectedModule;
        private LicenseGrade _selectedGrade = LicenseGrade.StudentPilot;

        // ── Unity ─────────────────────────────────────────────────────────────────
        private void Awake()
        {
            _closeButton?.onClick.AddListener(Hide);
            _startTrainingButton?.onClick.AddListener(OnStartTraining);
            _startExamButton?.onClick.AddListener(OnStartExam);
        }

        private void OnEnable()
        {
            var manager = FlightAcademyManager.Instance;
            if (manager != null)
            {
                manager.OnExamCompleted  += OnExamCompleted;
                manager.OnLicenseEarned  += OnLicenseEarned;
            }
            Refresh();
        }

        private void OnDisable()
        {
            var manager = FlightAcademyManager.Instance;
            if (manager != null)
            {
                manager.OnExamCompleted  -= OnExamCompleted;
                manager.OnLicenseEarned  -= OnLicenseEarned;
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────────
        /// <summary>Shows and refreshes the academy panel.</summary>
        public void Show()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        /// <summary>Hides the academy panel.</summary>
        public void Hide() => gameObject.SetActive(false);

        /// <summary>Selects the grade tab and redraws the module grid.</summary>
        public void SelectGrade(LicenseGrade grade)
        {
            _selectedGrade = grade;
            RefreshModuleGrid();
        }

        // ── Refresh ───────────────────────────────────────────────────────────────
        private void Refresh()
        {
            RefreshLicenseTree();
            RefreshModuleGrid();
            RefreshCertificateGallery();
            RefreshStatistics();
        }

        private void RefreshLicenseTree()
        {
            if (_licenseTreeContainer == null) return;
            foreach (Transform child in _licenseTreeContainer) Destroy(child.gameObject);

            var manager = FlightAcademyManager.Instance;
            if (manager == null) return;

            LicenseGrade current = manager.GetCurrentLicense();
            foreach (LicenseGrade grade in System.Enum.GetValues(typeof(LicenseGrade)))
            {
                if (_licenseNodePrefab == null) break;
                var node = Instantiate(_licenseNodePrefab, _licenseTreeContainer);
                var label = node.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = grade.ToString();
                var btn = node.GetComponent<Button>();
                if (btn != null)
                {
                    LicenseGrade capturedGrade = grade;
                    btn.onClick.AddListener(() => SelectGrade(capturedGrade));
                }
                // Visual state
                bool earned = grade <= current;
                var img = node.GetComponent<Image>();
                if (img != null)
                    img.color = earned ? Color.white : Color.gray;
            }
        }

        private void RefreshModuleGrid()
        {
            if (_moduleGridContainer == null) return;
            foreach (Transform child in _moduleGridContainer) Destroy(child.gameObject);

            var manager = FlightAcademyManager.Instance;
            if (manager == null) return;

            var progress = manager.GetAcademyProgress();
            var modules  = manager.GetAvailableModules(_selectedGrade);

            foreach (var module in modules)
            {
                if (_moduleCardPrefab == null) break;
                var card  = Instantiate(_moduleCardPrefab, _moduleGridContainer);
                var label = card.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = module.titleLocKey;

                bool completed = progress.completedModules.Contains(module.moduleId);
                bool unlocked  = manager.IsModuleUnlocked(module);

                var img = card.GetComponent<Image>();
                if (img != null)
                    img.color = !unlocked ? Color.gray : completed ? Color.green : Color.white;

                var btn = card.GetComponent<Button>();
                if (btn != null && unlocked)
                {
                    TrainingModule captured = module;
                    btn.onClick.AddListener(() => ShowModuleDetail(captured));
                }
            }
        }

        private void RefreshCertificateGallery()
        {
            if (_certificateGalleryContainer == null) return;
            foreach (Transform child in _certificateGalleryContainer) Destroy(child.gameObject);

            var manager = FlightAcademyManager.Instance;
            if (manager == null) return;

            foreach (var cert in manager.GetAcademyProgress().certificates)
            {
                if (_certificatePrefab == null) break;
                var item  = Instantiate(_certificatePrefab, _certificateGalleryContainer);
                var label = item.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = $"{cert.licenseGrade}\n{cert.issueDate}";
            }
        }

        private void RefreshStatistics()
        {
            var manager = FlightAcademyManager.Instance;
            if (manager == null) return;

            var progress = manager.GetAcademyProgress();
            if (_flightHoursText != null)
                _flightHoursText.text = $"{progress.totalTrainingHours:F1} h";

            int passed = 0;
            float scoreSum = 0f;
            foreach (var kvp in progress.examResults)
            {
                if (kvp.Value.passed) passed++;
                scoreSum += kvp.Value.score;
            }

            int total = progress.examResults.Count;
            if (_examPassedText != null)
                _examPassedText.text = $"{passed} / {total}";
            if (_avgScoreText != null)
                _avgScoreText.text = total > 0 ? $"{scoreSum / total:F1}" : "—";
        }

        // ── Detail Panel ──────────────────────────────────────────────────────────
        private void ShowModuleDetail(TrainingModule module)
        {
            _selectedModule = module;
            if (_moduleDetailPanel != null)
                _moduleDetailPanel.SetActive(true);
            if (_moduleDetailTitle != null)
                _moduleDetailTitle.text = module.titleLocKey;
            if (_moduleDetailDescription != null)
                _moduleDetailDescription.text = module.descriptionLocKey;

            var manager = FlightAcademyManager.Instance;
            if (_moduleDetailBestScore != null && manager != null)
            {
                var progress = manager.GetAcademyProgress();
                string best = progress.examResults.TryGetValue(module.moduleId, out var r)
                    ? r.score.ToString("F1") : "—";
                _moduleDetailBestScore.text = best;
            }
        }

        // ── Button handlers ───────────────────────────────────────────────────────
        private void OnStartTraining()
        {
            if (_selectedModule == null) return;
            var runner = FindObjectOfType<TrainingModuleRunner>();
            runner?.StartTraining(_selectedModule);
        }

        private void OnStartExam()
        {
            if (_selectedModule == null || FlightAcademyManager.Instance == null) return;
            FlightAcademyManager.Instance.StartExam(_selectedModule);
            Hide();
        }

        // ── Event handlers ────────────────────────────────────────────────────────
        private void OnExamCompleted(TrainingModule module, ExamResult result) => Refresh();
        private void OnLicenseEarned(LicenseGrade grade) => Refresh();
    }
}
