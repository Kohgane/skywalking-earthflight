using System;
using UnityEngine;

namespace SWEF.FlightSchool
{
    /// <summary>
    /// Bridges Flight School lessons with the SWEF.Replay recording system
    /// (Phase 84). Automatically starts a recording when a lesson begins and
    /// stops/saves it when the lesson ends.
    /// Resolves <c>SWEF.Replay.FlightRecorderManager</c> reflectively so this
    /// bridge compiles even when the Replay assembly isn't available.
    /// </summary>
    public class LessonReplayBridge : MonoBehaviour
    {
        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Fired when a recording has been started for <see cref="ActiveLessonId"/>.</summary>
        public event Action<string> OnRecordingStarted;

        /// <summary>Fired when a recording has been stopped and linked to the finished lesson.</summary>
        public event Action<string> OnRecordingStopped;

        // ── Inspector ────────────────────────────────────────────────────────────

        [SerializeField] private FlightSchoolManager schoolManager;

        [Tooltip("If false, recordings are not automatically triggered on lesson start/stop.")]
        [SerializeField] private bool autoRecordLessons = true;

        // ── Public state ─────────────────────────────────────────────────────────

        /// <summary>Lesson the current recording is associated with, or null when idle.</summary>
        public string ActiveLessonId { get; private set; }

        /// <summary>Whether a lesson recording is currently in progress.</summary>
        public bool IsRecording { get; private set; }

        // ── Internal state ───────────────────────────────────────────────────────

        private static Type _recorderType;
        private static bool _resolveAttempted;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (schoolManager == null) schoolManager = FlightSchoolManager.Instance;
        }

        private void OnEnable()
        {
            if (!autoRecordLessons || schoolManager == null) return;
            schoolManager.OnLessonStarted   += HandleLessonStarted;
            schoolManager.OnLessonCompleted += HandleLessonCompleted;
        }

        private void OnDisable()
        {
            if (schoolManager == null) return;
            schoolManager.OnLessonStarted   -= HandleLessonStarted;
            schoolManager.OnLessonCompleted -= HandleLessonCompleted;
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Begins a recording for <paramref name="lessonId"/>. Returns <c>false</c>
        /// when the Replay subsystem is unavailable or a recording is already in progress.
        /// </summary>
        public bool StartRecording(string lessonId)
        {
            if (IsRecording || string.IsNullOrEmpty(lessonId)) return false;

            var recorder = GetRecorderInstance();
            if (recorder == null) return false;

            TryInvoke(recorder, "StartRecording");

            ActiveLessonId = lessonId;
            IsRecording    = true;
            OnRecordingStarted?.Invoke(lessonId);
            return true;
        }

        /// <summary>
        /// Stops the active recording. Returns <c>false</c> when no recording is in progress.
        /// </summary>
        public bool StopAndSave()
        {
            if (!IsRecording) return false;

            var recorder = GetRecorderInstance();
            if (recorder != null)
                TryInvoke(recorder, "StopRecording");

            string lessonId = ActiveLessonId;
            IsRecording     = false;
            ActiveLessonId  = null;
            OnRecordingStopped?.Invoke(lessonId);
            return true;
        }

        // ── Event handlers ───────────────────────────────────────────────────────

        private void HandleLessonStarted(FlightLesson lesson)
        {
            if (lesson == null) return;
            StartRecording(lesson.lessonId);
        }

        private void HandleLessonCompleted(FlightLesson lesson)
        {
            if (!IsRecording) return;
            if (lesson != null && ActiveLessonId != null && ActiveLessonId != lesson.lessonId) return;
            StopAndSave();
        }

        // ── Reflection helpers ───────────────────────────────────────────────────

        private static Type ResolveRecorderType()
        {
            if (_resolveAttempted) return _recorderType;
            _resolveAttempted = true;
            _recorderType     = Type.GetType("SWEF.Replay.FlightRecorderManager, Assembly-CSharp");
            return _recorderType;
        }

        private static MonoBehaviour GetRecorderInstance()
        {
            var t = ResolveRecorderType();
            if (t == null) return null;
            var prop = t.GetProperty("Instance");
            return prop?.GetValue(null) as MonoBehaviour;
        }

        private static void TryInvoke(MonoBehaviour instance, string methodName)
        {
            if (instance == null || string.IsNullOrEmpty(methodName)) return;
            var method = instance.GetType().GetMethod(methodName, Type.EmptyTypes);
            method?.Invoke(instance, null);
        }
    }
}
