using UnityEngine;

// Optional integration compile guard
#if SWEF_ANALYTICS_AVAILABLE
using SWEF.Analytics;
#endif

namespace SWEF.FlightAcademy
{
    /// <summary>
    /// Telemetry bridge for the Flight Training Academy.
    /// Emits analytics events via <see cref="TelemetryDispatcher"/> (null-safe).
    /// Events: academy_module_started, academy_exam_started, academy_exam_completed,
    /// academy_exam_failed, academy_license_earned, academy_certificate_shared,
    /// academy_training_hours, academy_session_summary.
    /// </summary>
    public class FlightAcademyAnalytics : MonoBehaviour
    {
        // ── Session accumulators ──────────────────────────────────────────────────
        private int   _sessionExams;
        private int   _sessionPassed;
        private float _sessionScoreSum;
        private int   _sessionLicenses;
        private float _sessionTrainingHours;

        // ── Unity ─────────────────────────────────────────────────────────────────
        private void OnEnable()
        {
            var manager = FlightAcademyManager.Instance;
            if (manager == null) return;

            manager.OnModuleStarted     += HandleModuleStarted;
            manager.OnExamStarted       += HandleExamStarted;
            manager.OnExamCompleted     += HandleExamCompleted;
            manager.OnLicenseEarned     += HandleLicenseEarned;
            manager.OnCertificateIssued += HandleCertificateIssued;
        }

        private void OnDisable()
        {
            var manager = FlightAcademyManager.Instance;
            if (manager == null) return;

            manager.OnModuleStarted     -= HandleModuleStarted;
            manager.OnExamStarted       -= HandleExamStarted;
            manager.OnExamCompleted     -= HandleExamCompleted;
            manager.OnLicenseEarned     -= HandleLicenseEarned;
            manager.OnCertificateIssued -= HandleCertificateIssued;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) FlushSessionSummary();
        }

        private void OnApplicationQuit() => FlushSessionSummary();

        // ── Handlers ─────────────────────────────────────────────────────────────
        private void HandleModuleStarted(TrainingModule module)
        {
            Dispatch("academy_module_started", new TelemetryEventData
            {
                { "module_id",    module.moduleId },
                { "grade",        module.licenseGrade.ToString() },
                { "exam_type",    module.examType.ToString() },
                { "difficulty",   module.examDifficulty.ToString() }
            });
        }

        private void HandleExamStarted(TrainingModule module)
        {
            _sessionExams++;
            Dispatch("academy_exam_started", new TelemetryEventData
            {
                { "module_id",  module.moduleId },
                { "difficulty", module.examDifficulty.ToString() }
            });
        }

        private void HandleExamCompleted(TrainingModule module, ExamResult result)
        {
            if (result.passed)
            {
                _sessionPassed++;
                _sessionScoreSum += result.score;
                Dispatch("academy_exam_completed", new TelemetryEventData
                {
                    { "module_id", module.moduleId },
                    { "score",     result.score.ToString("F1") },
                    { "grade",     result.grade },
                    { "time",      result.totalTime.ToString("F1") }
                });
            }
            else
            {
                Dispatch("academy_exam_failed", new TelemetryEventData
                {
                    { "module_id", module.moduleId },
                    { "score",     result.score.ToString("F1") }
                });
            }
        }

        private void HandleLicenseEarned(LicenseGrade grade)
        {
            _sessionLicenses++;
            Dispatch("academy_license_earned", new TelemetryEventData
            {
                { "grade", grade.ToString() }
            });
        }

        private void HandleCertificateIssued(Certificate certificate)
        {
            // Certificate issued event is implicitly covered by license_earned;
            // additional share event fired from ShareController.
        }

        /// <summary>Called externally when the player shares a certificate.</summary>
        public void TrackCertificateShared(LicenseGrade grade)
        {
            Dispatch("academy_certificate_shared", new TelemetryEventData
            {
                { "grade", grade.ToString() }
            });
        }

        /// <summary>Adds training hours to the session accumulator and dispatches an event.</summary>
        public void TrackTrainingHours(float hours)
        {
            _sessionTrainingHours += hours;
            Dispatch("academy_training_hours", new TelemetryEventData
            {
                { "hours", hours.ToString("F2") }
            });
        }

        // ── Session summary ───────────────────────────────────────────────────────
        private void FlushSessionSummary()
        {
            if (_sessionExams == 0) return;
            float passRate = _sessionExams > 0 ? (float)_sessionPassed / _sessionExams : 0f;
            float avgScore = _sessionPassed > 0 ? _sessionScoreSum / _sessionPassed : 0f;
            Dispatch("academy_session_summary", new TelemetryEventData
            {
                { "exams_taken",    _sessionExams.ToString() },
                { "pass_rate",      passRate.ToString("F2") },
                { "avg_score",      avgScore.ToString("F1") },
                { "licenses_earned",_sessionLicenses.ToString() },
                { "training_hours", _sessionTrainingHours.ToString("F2") }
            });
            ResetSessionCounters();
        }

        private void ResetSessionCounters()
        {
            _sessionExams = 0;
            _sessionPassed = 0;
            _sessionScoreSum = 0f;
            _sessionLicenses = 0;
            _sessionTrainingHours = 0f;
        }

        // ── Dispatch helper ───────────────────────────────────────────────────────
        private static void Dispatch(string eventName, TelemetryEventData data)
        {
#if SWEF_ANALYTICS_AVAILABLE
            TelemetryDispatcher.Instance?.Dispatch(eventName, data);
#else
            Debug.Log($"[FlightAcademyAnalytics] {eventName}: {data}");
#endif
        }
    }

    /// <summary>Lightweight dictionary alias for telemetry event parameters.</summary>
    internal class TelemetryEventData : System.Collections.Generic.Dictionary<string, string>
    {
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder("{");
            foreach (var kvp in this)
                sb.Append($" {kvp.Key}={kvp.Value},");
            sb.Append("}");
            return sb.ToString();
        }
    }
}
