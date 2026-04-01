using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.FlightAcademy
{
    /// <summary>
    /// Post-exam results screen: animated score reveal, letter grade,
    /// per-objective breakdown, penalty/bonus itemisation, and reward display.
    /// </summary>
    public class ExamResultUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Score")]
        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _gradeText;
        [SerializeField] private float _scoreRevealDuration = 1.5f;

        [Header("Verdict")]
        [SerializeField] private Text _verdictText;
        [SerializeField] private GameObject _passBanner;
        [SerializeField] private GameObject _failBanner;

        [Header("Objectives")]
        [SerializeField] private Transform _objectiveContainer;
        [SerializeField] private GameObject _objectiveRowPrefab;

        [Header("Penalties / Bonuses")]
        [SerializeField] private Text _penaltyText;
        [SerializeField] private Text _bonusText;

        [Header("Rewards")]
        [SerializeField] private Text _xpRewardText;
        [SerializeField] private Text _skillPointsText;

        [Header("Buttons")]
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _shareCertificateButton;

        [Header("Celebration")]
        [SerializeField] private ParticleSystem _confettiVFX;

        // ── State ─────────────────────────────────────────────────────────────────
        private TrainingModule _module;
        private ExamResult _result;

        // ── Unity ─────────────────────────────────────────────────────────────────
        private void Awake()
        {
            _retryButton?.onClick.AddListener(OnRetry);
            _continueButton?.onClick.AddListener(OnContinue);
            _shareCertificateButton?.onClick.AddListener(OnShareCertificate);

            var manager = FlightAcademyManager.Instance;
            if (manager != null)
                manager.OnExamCompleted += Show;
        }

        private void OnDestroy()
        {
            var manager = FlightAcademyManager.Instance;
            if (manager != null)
                manager.OnExamCompleted -= Show;
        }

        // ── Public API ─────────────────────────────────────────────────────────────
        /// <summary>Populates and shows the results screen for the completed exam.</summary>
        public void Show(TrainingModule module, ExamResult result)
        {
            _module = module;
            _result = result;
            gameObject.SetActive(true);

            BuildObjectiveBreakdown(result);
            ApplyVerdictVisuals(result);
            PopulateRewards(module, result);
            ShowShareButtonIfCertificateEarned();

            StartCoroutine(AnimateScore(result.score));
        }

        // ── Animation ─────────────────────────────────────────────────────────────
        private IEnumerator AnimateScore(float targetScore)
        {
            float elapsed = 0f;
            while (elapsed < _scoreRevealDuration)
            {
                elapsed += Time.deltaTime;
                float displayed = Mathf.Lerp(0f, targetScore, elapsed / _scoreRevealDuration);
                if (_scoreText != null)
                    _scoreText.text = $"{displayed:F0}";
                yield return null;
            }
            if (_scoreText != null)
                _scoreText.text = $"{targetScore:F0}";
            if (_gradeText != null)
                _gradeText.text = _result.grade;

            // Confetti for A grades
            if (_result.grade.StartsWith("A") && _confettiVFX != null)
                _confettiVFX.Play();
        }

        // ── UI helpers ────────────────────────────────────────────────────────────
        private void BuildObjectiveBreakdown(ExamResult result)
        {
            if (_objectiveContainer == null) return;
            foreach (Transform child in _objectiveContainer) Destroy(child.gameObject);

            foreach (var obj in result.objectiveScores)
            {
                if (_objectiveRowPrefab == null) break;
                var row   = Instantiate(_objectiveRowPrefab, _objectiveContainer);
                var texts = row.GetComponentsInChildren<Text>();
                if (texts.Length >= 2)
                {
                    texts[0].text = obj.objectiveType;
                    texts[1].text = $"{obj.score:F0}";
                }
                var slider = row.GetComponentInChildren<Slider>();
                if (slider != null)
                    slider.value = obj.score / 100f;
            }
        }

        private void ApplyVerdictVisuals(ExamResult result)
        {
            if (_passBanner != null) _passBanner.SetActive(result.passed);
            if (_failBanner  != null) _failBanner.SetActive(!result.passed);
            if (_verdictText != null)
                _verdictText.text = result.passed ? "PASS" : "FAIL";

            if (_penaltyText != null)
                _penaltyText.text = $"-{result.penaltyPoints:F0}";
            if (_bonusText != null)
                _bonusText.text = $"+{result.bonusPoints:F0}";
        }

        private void PopulateRewards(TrainingModule module, ExamResult result)
        {
            if (!result.passed || module == null) return;
            if (_xpRewardText != null)
                _xpRewardText.text = $"+{module.rewardXP} XP";
            if (_skillPointsText != null)
                _skillPointsText.text = $"+{module.rewardSkillPoints} SP";
        }

        private void ShowShareButtonIfCertificateEarned()
        {
            if (_shareCertificateButton == null) return;
            var manager = FlightAcademyManager.Instance;
            bool hasCert = manager != null && manager.GetAcademyProgress().certificates.Count > 0;
            _shareCertificateButton.gameObject.SetActive(hasCert);
        }

        // ── Button handlers ───────────────────────────────────────────────────────
        private void OnRetry()
        {
            gameObject.SetActive(false);
            if (_module != null && FlightAcademyManager.Instance != null)
                FlightAcademyManager.Instance.StartExam(_module);
        }

        private void OnContinue() => gameObject.SetActive(false);

        private void OnShareCertificate()
        {
            var manager = FlightAcademyManager.Instance;
            if (manager == null) return;
            var certs = manager.GetAcademyProgress().certificates;
            if (certs.Count == 0) return;

            var shareCtrl = FindObjectOfType<CertificateShareController>();
            shareCtrl?.ShareCertificate(certs[certs.Count - 1]);
        }
    }
}
