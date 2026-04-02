// CourseShareManager.cs — SWEF Competitive Racing & Time Trial System (Phase 88)
using System;
using System.Text;
using UnityEngine;

#if SWEF_SOCIAL_AVAILABLE
using SWEF.Social;
#endif

namespace SWEF.CompetitiveRacing
{
    /// <summary>
    /// Phase 88 — MonoBehaviour that handles exporting and importing race courses
    /// as <c>.swefcourse</c> JSON files or share-code strings (base64-encoded JSON).
    /// Also tracks per-course ratings and play counts.
    ///
    /// <para>Integrates with <c>SWEF.Social.ShareManager</c> for deep-link sharing
    /// when the <c>SWEF_SOCIAL_AVAILABLE</c> symbol is defined.</para>
    /// </summary>
    public class CourseShareManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("File I/O")]
        [Tooltip("File extension used when saving courses to disk.")]
        [SerializeField] private string _fileExtension = ".swefcourse";

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Serialises <paramref name="course"/> to JSON and writes it to the persistent
        /// data directory with a <c>.swefcourse</c> extension.
        /// </summary>
        public void ExportCourse(RaceCourse course)
        {
            if (course == null)
            {
                Debug.LogWarning("[SWEF] CourseShareManager: ExportCourse called with null course.");
                return;
            }

            string json     = JsonUtility.ToJson(course, prettyPrint: true);
            string fileName = SanitiseFileName(course.courseName) + _fileExtension;
            string path     = System.IO.Path.Combine(Application.persistentDataPath, fileName);

            try
            {
                System.IO.File.WriteAllText(path, json, Encoding.UTF8);
                Debug.Log($"[SWEF] CourseShareManager: Course exported → {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] CourseShareManager: Export failed — {ex.Message}");
            }

            CompetitiveRacingAnalytics.RecordCourseShared(course.courseId);
        }

        /// <summary>
        /// Deserialises a <see cref="RaceCourse"/> from a base64-encoded share-code
        /// string.  Returns <c>null</c> if decoding fails.
        /// </summary>
        public RaceCourse ImportCourse(string shareCode)
        {
            if (string.IsNullOrWhiteSpace(shareCode))
            {
                Debug.LogWarning("[SWEF] CourseShareManager: ImportCourse called with empty share code.");
                return null;
            }

            try
            {
                byte[] bytes = Convert.FromBase64String(shareCode);
                string json  = Encoding.UTF8.GetString(bytes);
                var course   = JsonUtility.FromJson<RaceCourse>(json);

                if (course != null)
                {
                    // Give the imported course a fresh ID to avoid collisions
                    course.courseId      = Guid.NewGuid().ToString();
                    course.createdDate   = DateTime.UtcNow;
                    course.lastModifiedDate = DateTime.UtcNow;
                    Debug.Log($"[SWEF] CourseShareManager: Imported course '{course.courseName}'.");
                }
                return course;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] CourseShareManager: Import failed — {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generates a base64 share code for <paramref name="course"/> and stores it
        /// on the course object.
        /// </summary>
        public string GenerateShareCode(RaceCourse course)
        {
            if (course == null) return string.Empty;
            string json    = JsonUtility.ToJson(course);
            string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            course.shareCode = encoded;
            return encoded;
        }

        /// <summary>
        /// Records a 1–5 star rating for <paramref name="courseId"/> and updates the
        /// running average on the supplied <paramref name="course"/> object.
        /// </summary>
        public void RateCourse(RaceCourse course, int rating)
        {
            if (course == null) return;
            rating = Mathf.Clamp(rating, 1, 5);

            float total = course.averageRating * course.ratingCount + rating;
            course.ratingCount++;
            course.averageRating = total / course.ratingCount;

            CompetitiveRacingAnalytics.RecordCourseRated(course.courseId, rating);
            Debug.Log($"[SWEF] CourseShareManager: Rated '{course.courseName}' {rating}★ → avg {course.averageRating:F1}");
        }

        /// <summary>
        /// Increments the play count of <paramref name="course"/> and optionally shares
        /// via the social system.
        /// </summary>
        public void TrackPlay(RaceCourse course)
        {
            if (course == null) return;
            course.playCount++;
        }

        /// <summary>
        /// Shares the course deep-link via <c>SWEF.Social.ShareManager</c> when available.
        /// </summary>
        public void ShareViaSocial(RaceCourse course)
        {
            if (course == null) return;
            string code = string.IsNullOrEmpty(course.shareCode)
                ? GenerateShareCode(course) : course.shareCode;

#if SWEF_SOCIAL_AVAILABLE
            ShareManager.Instance?.ShareText(
                $"Race '{course.courseName}' in SWEF! Code: {code}",
                "SWEF Race Course");
#else
            Debug.Log($"[SWEF] CourseShareManager: Share code for '{course.courseName}': {code}");
#endif
            CompetitiveRacingAnalytics.RecordCourseShared(course.courseId);
        }

        // ── Private Helpers ───────────────────────────────────────────────────────

        private static string SanitiseFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "unnamed_course";
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
