using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.FlightSchool
{
    /// <summary>
    /// Enhanced in-flight HUD overlay for Flight School lessons (Phase 84).
    /// Complements <see cref="FlightSchoolUI"/> by surfacing constraint status,
    /// a live grade preview, and the most recent instructor hint. Pure
    /// presentation layer — pulls data from the other Phase 84 components.
    /// </summary>
    public class FlightSchoolHUD : MonoBehaviour
    {
        // ── Inspector — dependencies ─────────────────────────────────────────────

        [Header("Dependencies")]
        [SerializeField] private FlightInstructor          instructor;
        [SerializeField] private FlightGradingSystem        grading;
        [SerializeField] private FlightConstraintEnforcer   constraintEnforcer;

        // ── Inspector — UI elements ─────────────────────────────────────────────

        [Header("Live Grade")]
        [SerializeField] private Text   liveScoreLabel;
        [SerializeField] private Text   liveLetterLabel;
        [SerializeField] private Slider liveScoreBar;

        [Header("Constraint Indicators")]
        [SerializeField] private Transform constraintListRoot;
        [SerializeField] private GameObject constraintItemPrefab;

        [Header("Hints")]
        [SerializeField] private Text hintText;
        [SerializeField] private float hintDisplaySeconds = 5f;

        [Header("Colors")]
        [SerializeField] private Color okColor      = Color.green;
        [SerializeField] private Color warnColor    = Color.yellow;
        [SerializeField] private Color violateColor = Color.red;

        // ── Internal state ───────────────────────────────────────────────────────

        private float _hintShownAt;
        private readonly StringBuilder _sb = new StringBuilder(64);

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (instructor != null)
                instructor.OnInstructionStep += HandleInstructionStep;

            if (constraintEnforcer != null)
            {
                constraintEnforcer.OnConstraintViolated += HandleConstraintViolated;
                constraintEnforcer.OnConstraintWarning  += HandleConstraintWarning;
                constraintEnforcer.OnConstraintRestored += HandleConstraintRestored;
            }
        }

        private void OnDisable()
        {
            if (instructor != null)
                instructor.OnInstructionStep -= HandleInstructionStep;

            if (constraintEnforcer != null)
            {
                constraintEnforcer.OnConstraintViolated -= HandleConstraintViolated;
                constraintEnforcer.OnConstraintWarning  -= HandleConstraintWarning;
                constraintEnforcer.OnConstraintRestored -= HandleConstraintRestored;
            }
        }

        private void Update()
        {
            UpdateLiveScore();
            UpdateHintFade();
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Rebuilds the constraint list UI from the active enforcer.</summary>
        public void RebuildConstraintList()
        {
            if (constraintListRoot == null) return;

            foreach (Transform child in constraintListRoot)
                Destroy(child.gameObject);

            if (constraintEnforcer == null || constraintItemPrefab == null) return;

            foreach (var c in constraintEnforcer.ActiveConstraints)
                SpawnConstraintItem(c);
        }

        // ── Internal helpers ─────────────────────────────────────────────────────

        private void UpdateLiveScore()
        {
            if (grading == null) return;

            float score = grading.GetLiveAggregateScore();
            if (liveScoreLabel != null) liveScoreLabel.text = $"{score:F0}/100";
            if (liveLetterLabel != null) liveLetterLabel.text = LessonGradeReport.ScoreToLetter(score);
            if (liveScoreBar != null) liveScoreBar.value = Mathf.Clamp01(score / 100f);
        }

        private void UpdateHintFade()
        {
            if (hintText == null || _hintShownAt <= 0f) return;

            float elapsed = Time.time - _hintShownAt;
            if (elapsed > hintDisplaySeconds)
            {
                hintText.text = string.Empty;
                _hintShownAt  = 0f;
            }
        }

        private void SpawnConstraintItem(FlightConstraint c)
        {
            var go = Instantiate(constraintItemPrefab, constraintListRoot);

            SetChildText(go, "LabelText", FormatConstraintLabel(c));

            var image = go.GetComponent<Image>();
            if (image == null) image = go.GetComponentInChildren<Image>();

            if (image != null)
            {
                float value = constraintEnforcer.GetCurrentValueFor(c);
                if (c.IsWithin(value))              image.color = okColor;
                else if (c.IsInWarningZone(value))  image.color = warnColor;
                else                                image.color = violateColor;
            }
        }

        private string FormatConstraintLabel(FlightConstraint c)
        {
            _sb.Length = 0;
            _sb.Append(c.type).Append(": ");
            _sb.AppendFormat("{0:F0}–{1:F0}", c.minValue, c.maxValue);
            if (!string.IsNullOrEmpty(c.description))
                _sb.Append(" · ").Append(c.description);
            return _sb.ToString();
        }

        // ── Event handlers ───────────────────────────────────────────────────────

        private void HandleInstructionStep(string message)
        {
            if (hintText != null) hintText.text = message;
            _hintShownAt = Time.time;
        }

        private void HandleConstraintViolated(FlightConstraint c, float penalty)
        {
            if (grading != null)
            {
                // Scale penalty into a safety score reduction (up to 10 points per call).
                float current = grading.GetCriterionScore("safety");
                grading.SetCriterionScore("safety", Mathf.Max(0f, current - penalty * 0.5f));
            }
            RebuildConstraintList();
        }

        private void HandleConstraintWarning(FlightConstraint c)    => RebuildConstraintList();
        private void HandleConstraintRestored(FlightConstraint c)   => RebuildConstraintList();

        // ── Static utilities ─────────────────────────────────────────────────────

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
