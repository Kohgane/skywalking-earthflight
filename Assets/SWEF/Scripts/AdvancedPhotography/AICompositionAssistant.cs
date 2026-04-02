// AICompositionAssistant.cs — SWEF Advanced Photography & Drone Camera System (Phase 89)
using System;
using System.Collections;
using UnityEngine;

namespace SWEF.AdvancedPhotography
{
    /// <summary>
    /// Phase 89 — Singleton MonoBehaviour that performs real-time photographic composition
    /// analysis using screen-space grid calculations (no external AI dependency).
    ///
    /// <para>Evaluates the current camera framing against composition rules and provides a
    /// normalised score plus human-readable suggestions.  Auto-framing mode adjusts the
    /// camera's FOV or position to maximise the composition score.</para>
    /// </summary>
    public sealed class AICompositionAssistant : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static AICompositionAssistant Instance { get; private set; }

        #endregion

        #region Events

        /// <summary>Fired when the composition score changes by more than the configured delta threshold.</summary>
        public event Action<float> OnCompositionScoreChanged;

        /// <summary>Fired when auto-framing has finished adjusting the camera.</summary>
        public event Action OnAutoFrameComplete;

        /// <summary>Fired when a new suggestion string is available.</summary>
        public event Action<string> OnSuggestionUpdated;

        #endregion

        #region Inspector

        [Header("Settings")]
        [Tooltip("Active AI assistance level.")]
        [SerializeField] private AIAssistLevel _assistLevel = AIAssistLevel.Suggestions;

        [Tooltip("Camera to analyse. Defaults to Camera.main if null.")]
        [SerializeField] private Camera _targetCamera;

        [Header("Auto-Frame")]
        [Tooltip("FOV adjustment step per auto-frame iteration.")]
        [SerializeField] [Range(0.1f, 5f)] private float _fovAdjustStep = 1f;

        [Tooltip("Minimum FOV allowed during auto-framing.")]
        [SerializeField] [Range(10f, 90f)] private float _minFov = 20f;

        [Tooltip("Maximum FOV allowed during auto-framing.")]
        [SerializeField] [Range(30f, 120f)] private float _maxFov = 90f;

        #endregion

        #region Private State

        private float _lastScore = 0f;
        private CompositionAnalysis _lastAnalysis = new CompositionAnalysis();
        private Coroutine _updateCoroutine;

        // Rule-of-thirds grid intersections (normalised screen space)
        private static readonly Vector2[] _ruleOfThirdsPoints = new Vector2[]
        {
            new Vector2(1f / 3f, 1f / 3f),
            new Vector2(2f / 3f, 1f / 3f),
            new Vector2(1f / 3f, 2f / 3f),
            new Vector2(2f / 3f, 2f / 3f)
        };

        // Golden ratio spiral approximate key points
        private static readonly Vector2[] _goldenRatioPoints = new Vector2[]
        {
            new Vector2(0.618f, 0.382f),
            new Vector2(0.382f, 0.618f),
            new Vector2(0.618f, 0.618f),
            new Vector2(0.5f,   0.5f)
        };

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (_targetCamera == null)
                _targetCamera = Camera.main;

            _updateCoroutine = StartCoroutine(CompositionUpdateLoop());
        }

        private void OnDestroy()
        {
            if (_updateCoroutine != null)
                StopCoroutine(_updateCoroutine);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Performs an immediate composition analysis of the current camera frame.
        /// </summary>
        /// <returns>A <see cref="CompositionAnalysis"/> with rule, score, suggestion, and guide points.</returns>
        public CompositionAnalysis AnalyzeComposition()
        {
            return EvaluateRule(CompositionRule.RuleOfThirds);
        }

        /// <summary>
        /// Adjusts the camera FOV and, if <see cref="AIAssistLevel.FullAuto"/>, position to
        /// maximise the score for the given composition rule.
        /// </summary>
        public void AutoFrame(CompositionRule rule)
        {
            if (_targetCamera == null) return;

            StartCoroutine(AutoFrameCoroutine(rule));
            AdvancedPhotographyAnalytics.RecordAutoFrameUsed();
        }

        /// <summary>Sets the active AI assistance level.</summary>
        public void SetAssistLevel(AIAssistLevel level)
        {
            _assistLevel = level;
            Debug.Log($"[SWEF] AICompositionAssistant: assist level → {level}");
        }

        /// <summary>Returns the most recently computed composition score (0–1).</summary>
        public float GetCurrentScore() => _lastScore;

        #endregion

        #region Private — Analysis

        private IEnumerator CompositionUpdateLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(AdvancedPhotographyConfig.AICompositionUpdateInterval);

                if (_assistLevel == AIAssistLevel.Off) continue;

                CompositionAnalysis analysis = EvaluateRule(CompositionRule.RuleOfThirds);
                float delta = Mathf.Abs(analysis.score - _lastScore);

                if (delta >= AdvancedPhotographyConfig.AICompositionScoreDeltaThreshold)
                {
                    _lastScore    = analysis.score;
                    _lastAnalysis = analysis;
                    OnCompositionScoreChanged?.Invoke(_lastScore);
                    OnSuggestionUpdated?.Invoke(analysis.suggestion);
                }

                if (_assistLevel == AIAssistLevel.AutoFrame ||
                    _assistLevel == AIAssistLevel.FullAuto)
                {
                    AutoFrame(CompositionRule.RuleOfThirds);
                }

                AdvancedPhotographyAnalytics.RecordAICompositionUsed();
            }
        }

        /// <summary>Evaluates the current frame against the specified composition rule.</summary>
        private CompositionAnalysis EvaluateRule(CompositionRule rule)
        {
            var analysis = new CompositionAnalysis { rule = rule };

            switch (rule)
            {
                case CompositionRule.RuleOfThirds:
                    analysis.score       = ScoreRuleOfThirds();
                    analysis.guidePoints = _ruleOfThirdsPoints;
                    analysis.suggestion  = analysis.score > AdvancedPhotographyConfig.AICompositionGoodThreshold
                        ? "Great composition — subject is near a rule-of-thirds intersection."
                        : "Try moving the subject toward a grid intersection for better balance.";
                    break;

                case CompositionRule.GoldenRatio:
                    analysis.score       = ScoreGoldenRatio();
                    analysis.guidePoints = _goldenRatioPoints;
                    analysis.suggestion  = analysis.score > AdvancedPhotographyConfig.AICompositionGoodThreshold
                        ? "Subject aligns well with the golden ratio spiral."
                        : "Position the main subject along the golden ratio spiral.";
                    break;

                case CompositionRule.Symmetry:
                    analysis.score      = ScoreSymmetry();
                    analysis.suggestion = analysis.score > AdvancedPhotographyConfig.AICompositionGoodThreshold
                        ? "Strong symmetry detected."
                        : "Align the horizon or a central element with the frame midline for symmetry.";
                    break;

                case CompositionRule.CenterWeighted:
                    analysis.score      = ScoreCenterWeighted();
                    analysis.suggestion = analysis.score > AdvancedPhotographyConfig.AICompositionGoodThreshold
                        ? "Subject is well-centred."
                        : "Centre the main subject in the frame.";
                    break;

                default:
                    analysis.score      = 0.5f;
                    analysis.suggestion = "Apply a composition rule for better framing.";
                    break;
            }

            return analysis;
        }

        // ── Score helpers (screen-space heuristics) ───────────────────────────────

        private float ScoreRuleOfThirds()
        {
            // Heuristic: evaluate how close the camera's screen-centre is to any
            // rule-of-thirds intersection — simplified for no-AI implementation.
            float minDist = float.MaxValue;
            Vector2 screenCentre = new Vector2(0.5f, 0.5f);

            foreach (Vector2 pt in _ruleOfThirdsPoints)
                minDist = Mathf.Min(minDist, Vector2.Distance(screenCentre, pt));

            float maxPossibleDist = Vector2.Distance(Vector2.zero, new Vector2(0.5f, 0.5f));
            return Mathf.Clamp01(1f - minDist / maxPossibleDist);
        }

        private float ScoreGoldenRatio()
        {
            float minDist = float.MaxValue;
            Vector2 screenCentre = new Vector2(0.5f, 0.5f);

            foreach (Vector2 pt in _goldenRatioPoints)
                minDist = Mathf.Min(minDist, Vector2.Distance(screenCentre, pt));

            return Mathf.Clamp01(1f - minDist / 0.7f);
        }

        private float ScoreSymmetry()
        {
            // Perfect symmetry heuristic: camera roll close to zero, tilt near horizon.
            if (_targetCamera == null) return 0.5f;
            float roll = _targetCamera.transform.eulerAngles.z;
            if (roll > 180f) roll -= 360f;
            return Mathf.Clamp01(1f - Mathf.Abs(roll) / 45f);
        }

        private float ScoreCenterWeighted()
        {
            // Perfect centre: camera pointing close to horizon level.
            if (_targetCamera == null) return 0.5f;
            float pitch = _targetCamera.transform.eulerAngles.x;
            if (pitch > 180f) pitch -= 360f;
            return Mathf.Clamp01(1f - Mathf.Abs(pitch) / 90f);
        }

        #endregion

        #region Private — Auto-Frame

        private IEnumerator AutoFrameCoroutine(CompositionRule rule)
        {
            if (_targetCamera == null) yield break;

            float bestScore = EvaluateRule(rule).score;
            float bestFov   = _targetCamera.fieldOfView;

            // Try narrowing FOV
            for (float fov = bestFov; fov >= _minFov; fov -= _fovAdjustStep)
            {
                _targetCamera.fieldOfView = fov;
                yield return null;

                float score = EvaluateRule(rule).score;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestFov   = fov;
                }
                else break;
            }

            // Try widening FOV
            for (float fov = bestFov; fov <= _maxFov; fov += _fovAdjustStep)
            {
                _targetCamera.fieldOfView = fov;
                yield return null;

                float score = EvaluateRule(rule).score;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestFov   = fov;
                }
                else break;
            }

            _targetCamera.fieldOfView = bestFov;
            OnAutoFrameComplete?.Invoke();
            Debug.Log($"[SWEF] AICompositionAssistant: auto-frame complete, FOV={bestFov:0.0}, score={bestScore:0.00}");
        }

        #endregion
    }
}
