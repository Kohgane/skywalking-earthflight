using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Optional integration compile guards
#if SWEF_ACCESSIBILITY_AVAILABLE
using SWEF.Accessibility;
#endif
#if SWEF_LOCALIZATION_AVAILABLE
using SWEF.Localization;
#endif
#if SWEF_NARRATION_AVAILABLE
using SWEF.Narration;
#endif

namespace SWEF.FlightAcademy
{
    /// <summary>Priority levels for instructor dialogue entries.</summary>
    public enum InstructorDialoguePriority
    {
        Low     = 0,
        Normal  = 1,
        High    = 2,
        Urgent  = 3
    }

    /// <summary>
    /// Manages instructor voice / text during training and exams.
    /// Maintains a priority queue so that urgent warnings always preempt
    /// lower-priority commentary. Integrates with ScreenReaderBridge (TTS)
    /// and LocalizationManager for translated text.
    /// </summary>
    public class InstructorDialogueController : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired whenever a new dialogue line begins. Parameter: localised text.</summary>
        public event System.Action<string> OnDialogueLineStarted;

        /// <summary>Fired when the queue is empty.</summary>
        public event System.Action OnDialogueQueueEmpty;

        // ── Inspector ─────────────────────────────────────────────────────────────
        [SerializeField, Range(0.5f, 2f)] private float _voiceSpeed  = 1f;
        [SerializeField, Range(0f,   1f)] private float _voiceVolume = 1f;
        [SerializeField] private float _lineDuration = 4f;

        // ── Queue ─────────────────────────────────────────────────────────────────
        private readonly SortedList<int, Queue<string>> _queue
            = new SortedList<int, Queue<string>>(new DescendingComparer());

        private bool _speaking;

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Enqueues a dialogue line identified by <paramref name="locKey"/>
        /// at the given <paramref name="priority"/>.
        /// </summary>
        public void QueueDialogue(string locKey, InstructorDialoguePriority priority)
        {
            int p = (int)priority;
            if (!_queue.ContainsKey(p))
                _queue[p] = new Queue<string>();
            _queue[p].Enqueue(locKey);

            if (!_speaking)
                StartCoroutine(ProcessQueue());
        }

        /// <summary>Immediately clears all queued dialogue.</summary>
        public void ClearQueue()
        {
            StopAllCoroutines();
            _queue.Clear();
            _speaking = false;
        }

        // ── Processing ────────────────────────────────────────────────────────────
        private IEnumerator ProcessQueue()
        {
            _speaking = true;
            while (HasPendingDialogue())
            {
                string locKey = DequeueNext();
                string text   = Localise(locKey);

                OnDialogueLineStarted?.Invoke(text);
                SpeakText(text);
                PlayNarration(locKey);

                yield return new WaitForSeconds(_lineDuration / _voiceSpeed);
            }
            _speaking = false;
            OnDialogueQueueEmpty?.Invoke();
        }

        private bool HasPendingDialogue()
        {
            foreach (var kvp in _queue)
                if (kvp.Value.Count > 0) return true;
            return false;
        }

        private string DequeueNext()
        {
            foreach (var kvp in _queue)
            {
                if (kvp.Value.Count > 0)
                    return kvp.Value.Dequeue();
            }
            return string.Empty;
        }

        // ── Localisation & Accessibility helpers ──────────────────────────────────
        private static string Localise(string locKey)
        {
#if SWEF_LOCALIZATION_AVAILABLE
            if (LocalizationManager.Instance != null)
                return LocalizationManager.Instance.GetString(locKey);
#endif
            return locKey; // fallback: return key as-is
        }

        private void SpeakText(string text)
        {
#if SWEF_ACCESSIBILITY_AVAILABLE
            ScreenReaderBridge.Speak(text);
#endif
        }

        private static void PlayNarration(string locKey)
        {
#if SWEF_NARRATION_AVAILABLE
            NarrationManager.Instance?.PlayNarration(locKey);
#endif
        }

        // ── Comparer for descending priority ──────────────────────────────────────
        private class DescendingComparer : IComparer<int>
        {
            public int Compare(int x, int y) => y.CompareTo(x);
        }
    }
}
