using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.FlightAcademy
{
    /// <summary>
    /// In-exam HUD displaying the active objective checklist, countdown timer,
    /// score estimate, and penalty/bonus toast notifications.
    /// </summary>
    public class FlightAcademyHUD : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Objective List")]
        [SerializeField] private Transform _objectiveContainer;
        [SerializeField] private GameObject _objectiveItemPrefab;

        [Header("Timer")]
        [SerializeField] private Text _timerText;
        [SerializeField] private Image _timerFill;

        [Header("Score")]
        [SerializeField] private Text _scoreText;
        [SerializeField] private Slider _scoreSlider;

        [Header("Toasts")]
        [SerializeField] private Text _toastText;
        [SerializeField] private float _toastDuration = 2f;

        [Header("Instructor")]
        [SerializeField] private Text _instructorCommentaryText;

        // ── State ─────────────────────────────────────────────────────────────────
        private float _timeLimit;
        private float _elapsed;
        private bool _running;
        private float _toastTimer;

        private ExamController _examController;

        // ── Unity ─────────────────────────────────────────────────────────────────
        private void OnEnable()
        {
            var manager = FlightAcademyManager.Instance;
            if (manager != null)
            {
                manager.OnExamStarted    += HandleExamStarted;
                manager.OnExamCompleted  += HandleExamCompleted;
            }
        }

        private void OnDisable()
        {
            var manager = FlightAcademyManager.Instance;
            if (manager != null)
            {
                manager.OnExamStarted    -= HandleExamStarted;
                manager.OnExamCompleted  -= HandleExamCompleted;
            }

            if (_examController != null)
            {
                _examController.OnScoreUpdated   -= UpdateScore;
                _examController.OnPenaltyApplied -= ShowPenaltyToast;
                _examController.OnBonusApplied   -= ShowBonusToast;
            }
        }

        private void Update()
        {
            if (!_running) return;

            _elapsed += Time.deltaTime;
            float remaining = Mathf.Max(0f, _timeLimit - _elapsed);

            if (_timerText != null)
                _timerText.text = FormatTime(remaining);

            if (_timerFill != null && _timeLimit > 0f)
                _timerFill.fillAmount = remaining / _timeLimit;

            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.deltaTime;
                if (_toastTimer <= 0f && _toastText != null)
                    _toastText.enabled = false;
            }
        }

        // ── Handlers ─────────────────────────────────────────────────────────────
        private void HandleExamStarted(TrainingModule module)
        {
            _timeLimit = module.timeLimit;
            _elapsed = 0f;
            _running = true;

            BuildObjectiveList(module);

            _examController = FindObjectOfType<ExamController>();
            if (_examController != null)
            {
                _examController.OnScoreUpdated   += UpdateScore;
                _examController.OnPenaltyApplied += ShowPenaltyToast;
                _examController.OnBonusApplied   += ShowBonusToast;
            }
        }

        private void HandleExamCompleted(TrainingModule module, ExamResult result)
        {
            _running = false;
        }

        // ── UI helpers ────────────────────────────────────────────────────────────
        private void BuildObjectiveList(TrainingModule module)
        {
            if (_objectiveContainer == null || module.objectives == null) return;

            foreach (Transform child in _objectiveContainer)
                Destroy(child.gameObject);

            foreach (var obj in module.objectives)
            {
                if (_objectiveItemPrefab == null) break;
                var item = Instantiate(_objectiveItemPrefab, _objectiveContainer);
                var label = item.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = obj.descriptionLocKey;
            }
        }

        private void UpdateScore(float score)
        {
            if (_scoreText != null)
                _scoreText.text = $"{score:F0}";
            if (_scoreSlider != null)
                _scoreSlider.value = score / 100f;
        }

        private void ShowPenaltyToast(string reason, float amount)
            => ShowToast($"-{amount:F0}  {reason}", Color.red);

        private void ShowBonusToast(string reason, float amount)
            => ShowToast($"+{amount:F0}  {reason}", Color.green);

        private void ShowToast(string message, Color color)
        {
            if (_toastText == null) return;
            _toastText.text    = message;
            _toastText.color   = color;
            _toastText.enabled = true;
            _toastTimer = _toastDuration;
        }

        /// <summary>Displays instructor commentary text.</summary>
        public void ShowInstructorComment(string comment)
        {
            if (_instructorCommentaryText != null)
                _instructorCommentaryText.text = comment;
        }

        private static string FormatTime(float seconds)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return $"{m:D2}:{s:D2}";
        }
    }
}
