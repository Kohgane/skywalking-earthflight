using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Analytics
{
    /// <summary>
    /// ScriptableObject that configures a single A/B test.
    /// Create via Assets → Create → SWEF → ABTestConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/ABTestConfig", fileName = "ABTestConfig")]
    public class ABTestConfig : ScriptableObject
    {
        public string   testId;
        public string   testName;
        public string[] variants = { "control", "variant_a" };
        public bool     enabled = true;
    }

    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>Runtime state of a single A/B test.</summary>
    [Serializable]
    public class ABTest
    {
        public string   testId;
        public string   testName;
        public string[] variants;
        public string   assignedVariant;
        public bool     isActive;
    }

    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Singleton MonoBehaviour that manages A/B test variant assignment.
    /// Assignments are deterministic (hash of userId + testId) and persisted
    /// in PlayerPrefs so each user always sees the same variant.
    /// </summary>
    public class ABTestManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static ABTestManager Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────────
        [SerializeField] private ABTestConfig[] activeTests = new ABTestConfig[0];

        // ── State ────────────────────────────────────────────────────────────────
        private readonly Dictionary<string, ABTest> _tests = new Dictionary<string, ABTest>();
        private string _userId = "";

        private const string PrefsPrefix = "SWEF_ABTest_";

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // userId may be set after Awake; re-resolve from PrivacyConsentManager if available
            var pcm = PrivacyConsentManager.Instance;
            if (pcm != null) _userId = pcm.GetAnonymizedUserId();

            InitializeTests();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Sets the anonymized user ID used for variant assignment.</summary>
        public void SetUserId(string uid)
        {
            _userId = uid ?? "";
            InitializeTests();
        }

        /// <summary>Returns the assigned variant string for the given test, or empty string.</summary>
        public string GetVariant(string testId)
        {
            if (_tests.TryGetValue(testId, out var test) && test.isActive)
                return test.assignedVariant;
            return string.Empty;
        }

        /// <summary>Returns true when the user is assigned to a specific variant.</summary>
        public bool IsInVariant(string testId, string variant) =>
            string.Equals(GetVariant(testId), variant, StringComparison.Ordinal);

        /// <summary>Fire an exposure event — call once when the user first sees the tested UI.</summary>
        public void LogExposure(string testId)
        {
            if (!_tests.TryGetValue(testId, out var test) || !test.isActive) return;

            var dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            var evt = TelemetryEventBuilder.Create(AnalyticsEvents.AbTestExposure)
                .WithCategory("ui")
                .WithProperty("testId",          testId)
                .WithProperty("testName",         test.testName)
                .WithProperty("assignedVariant",  test.assignedVariant)
                .Build();
            dispatcher.EnqueueEvent(evt);
        }

        /// <summary>Fire a conversion event for the given test.</summary>
        public void LogConversion(string testId, string conversionEvent)
        {
            if (!_tests.TryGetValue(testId, out var test) || !test.isActive) return;

            var dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            var evt = TelemetryEventBuilder.Create(AnalyticsEvents.AbTestConversion)
                .WithCategory("ui")
                .WithProperty("testId",          testId)
                .WithProperty("testName",         test.testName)
                .WithProperty("assignedVariant",  test.assignedVariant)
                .WithProperty("conversionEvent",  conversionEvent)
                .Build();
            dispatcher.EnqueueEvent(evt);
        }

        /// <summary>Returns a snapshot of all active tests (for the debug window).</summary>
        public IEnumerable<ABTest> GetAllTests() => _tests.Values;

        /// <summary>Override a variant for testing purposes (Editor only).</summary>
        public void OverrideVariant(string testId, string variant)
        {
            if (_tests.TryGetValue(testId, out var test))
            {
                test.assignedVariant = variant;
                PlayerPrefs.SetString(PrefsPrefix + testId, variant);
                PlayerPrefs.Save();
            }
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void InitializeTests()
        {
            _tests.Clear();
            if (activeTests == null) return;

            foreach (var cfg in activeTests)
            {
                if (cfg == null || !cfg.enabled) continue;

                string savedVariant = PlayerPrefs.GetString(PrefsPrefix + cfg.testId, "");
                string assigned     = string.IsNullOrEmpty(savedVariant)
                    ? AssignVariant(cfg)
                    : savedVariant;

                _tests[cfg.testId] = new ABTest
                {
                    testId          = cfg.testId,
                    testName        = cfg.testName,
                    variants        = cfg.variants,
                    assignedVariant = assigned,
                    isActive        = true,
                };
            }
        }

        private string AssignVariant(ABTestConfig cfg)
        {
            if (cfg.variants == null || cfg.variants.Length == 0) return string.Empty;

            // Deterministic hash of userId + testId; use unchecked to avoid overflow exceptions.
            // Clamp to non-negative by masking the sign bit (handles int.MinValue edge case).
            string seed = _userId + cfg.testId;
            int hash = 5381;
            unchecked
            {
                foreach (char c in seed)
                    hash = hash * 31 + c;
            }
            hash = hash & 0x7FFFFFFF; // always non-negative

            string variant = cfg.variants[hash % cfg.variants.Length];
            PlayerPrefs.SetString(PrefsPrefix + cfg.testId, variant);
            PlayerPrefs.Save();
            return variant;
        }
    }
}
