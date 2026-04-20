using System;
using UnityEngine;

namespace SWEF.FlightSchool
{
    /// <summary>
    /// Tags auto-created journal entries with Flight School metadata after
    /// lessons complete (Phase 84). The <c>SWEF.Journal.JournalManager</c>
    /// owns entry creation, so this bridge only annotates the most recent
    /// entry via the manager's public Update* APIs when a lesson finishes.
    /// Uses reflection to avoid a hard dependency on the Journal assembly.
    /// </summary>
    public class FlightSchoolJournalBridge : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────

        [SerializeField] private FlightSchoolManager schoolManager;

        [Tooltip("Tag applied to every lesson flight entry.")]
        [SerializeField] private string lessonTag = "flightschool";

        [Tooltip("Tag applied when a certification is earned during the same flight.")]
        [SerializeField] private string certificationTag = "certification";

        // ── Internal state ───────────────────────────────────────────────────────

        private static Type _journalType;
        private static bool _resolveAttempted;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (schoolManager == null) schoolManager = FlightSchoolManager.Instance;
        }

        private void OnEnable()
        {
            if (schoolManager == null) return;
            schoolManager.OnLessonCompleted     += HandleLessonCompleted;
            schoolManager.OnCertificationEarned += HandleCertificationEarned;
        }

        private void OnDisable()
        {
            if (schoolManager == null) return;
            schoolManager.OnLessonCompleted     -= HandleLessonCompleted;
            schoolManager.OnCertificationEarned -= HandleCertificationEarned;
        }

        // ── Event handlers ───────────────────────────────────────────────────────

        private void HandleLessonCompleted(FlightLesson lesson)
        {
            if (lesson == null) return;
            AnnotateMostRecentEntry(new[] { lessonTag, SanitizeTag($"lesson_{lesson.lessonId}") },
                                    $"Flight School lesson completed: {lesson.title} — best score {lesson.bestScore:F0}/100");
        }

        private void HandleCertificationEarned(PilotCertification cert)
        {
            if (cert == null) return;
            AnnotateMostRecentEntry(new[] { lessonTag, certificationTag, SanitizeTag($"cert_{cert.certType}") },
                                    $"Certification earned: {cert.displayName}");
        }

        // ── Internal helpers ─────────────────────────────────────────────────────

        private static string SanitizeTag(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            return raw.ToLowerInvariant().Replace(' ', '_');
        }

        private void AnnotateMostRecentEntry(string[] tags, string note)
        {
            var instance = GetJournalInstance();
            if (instance == null) return;

            // Discover most recent entry id through GetRecentEntries(1)[0].logId.
            string logId = GetMostRecentLogId(instance);
            if (string.IsNullOrEmpty(logId)) return;

            InvokeJournalUpdate(instance, "UpdateEntryTags", new object[] { logId, tags });
            InvokeJournalUpdate(instance, "UpdateEntryNotes", new object[] { logId, note });
        }

        private static MonoBehaviour GetJournalInstance()
        {
            if (!_resolveAttempted)
            {
                _resolveAttempted = true;
                _journalType      = Type.GetType("SWEF.Journal.JournalManager, Assembly-CSharp");
            }
            if (_journalType == null) return null;
            var prop = _journalType.GetProperty("Instance");
            return prop?.GetValue(null) as MonoBehaviour;
        }

        private static string GetMostRecentLogId(MonoBehaviour journalInstance)
        {
            try
            {
                var method = journalInstance.GetType().GetMethod("GetRecentEntries", new[] { typeof(int) });
                var result = method?.Invoke(journalInstance, new object[] { 1 }) as System.Collections.IList;
                if (result == null || result.Count == 0) return null;

                var entry    = result[0];
                var logIdFld = entry?.GetType().GetField("logId");
                return logIdFld?.GetValue(entry) as string;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FlightSchool] JournalBridge GetMostRecentLogId failed — {ex.Message}");
                return null;
            }
        }

        private static void InvokeJournalUpdate(MonoBehaviour journalInstance, string methodName, object[] args)
        {
            try
            {
                var method = FindMethodByName(journalInstance.GetType(), methodName);
                method?.Invoke(journalInstance, args);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FlightSchool] JournalBridge {methodName} failed — {ex.Message}");
            }
        }

        private static System.Reflection.MethodInfo FindMethodByName(Type t, string name)
        {
            foreach (var m in t.GetMethods())
                if (m.Name == name) return m;
            return null;
        }
    }
}
