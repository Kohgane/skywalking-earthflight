// Phase 100 — AI Co-Pilot & Smart Assistant
// Assets/SWEF/Scripts/AICoPilot/AICoPilotDialogueManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.AICoPilot
{
    /// <summary>
    /// Priority tier for the message queue — lower numeric value = higher priority.
    /// </summary>
    public enum MessagePriority
    {
        /// <summary>Emergency — interrupts all other messages.</summary>
        Emergency = 0,

        /// <summary>Warning-level advisory.</summary>
        Warning = 1,

        /// <summary>Informational advisory or navigation callout.</summary>
        Info = 2,

        /// <summary>Idle chatter — low priority ambient commentary.</summary>
        Chatter = 3
    }

    /// <summary>
    /// A single queued message entry.
    /// </summary>
    [Serializable]
    public class DialogueMessage
    {
        /// <summary>Source category (e.g. "Flight", "Navigation", "Emergency").</summary>
        public string Category;

        /// <summary>Message text content.</summary>
        public string Text;

        /// <summary>Display priority.</summary>
        public MessagePriority Priority;

        /// <summary>UTC time the message was enqueued.</summary>
        public float EnqueuedTime;
    }

    /// <summary>
    /// Manages the priority-based message queue for ARIA dialogue.
    /// Provides a text-to-speech-ready interface (TTS placeholder) and drives
    /// the subtitle/HUD display via events. Also runs the idle chatter system.
    /// </summary>
    [DefaultExecutionOrder(-55)]
    [DisallowMultipleComponent]
    public class AICoPilotDialogueManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static AICoPilotDialogueManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        #endregion

        #region Events

        /// <summary>
        /// Fired when ARIA starts displaying a message.
        /// Subscribe to update HUD/subtitle elements.
        /// </summary>
        public event Action<DialogueMessage> OnMessageDisplayed;

        /// <summary>Fired when the current message finishes displaying.</summary>
        public event Action OnMessageCleared;

        #endregion

        #region Inspector

        [Header("Display Timing")]
        [Tooltip("Base display duration in seconds for a short message.")]
        [SerializeField] private float _baseDurationSeconds = 4f;

        [Tooltip("Additional seconds added per character for longer messages.")]
        [SerializeField] private float _secondsPerCharacter = 0.04f;

        [Header("Idle Chatter")]
        [Tooltip("Minimum seconds of silence before idle chatter triggers.")]
        [SerializeField] private float _idleChatterThresholdSeconds = 60f;

        [Tooltip("Maximum idle chatter interval (seconds).")]
        [SerializeField] private float _maxIdleIntervalSeconds = 120f;

        [Header("Settings Reference")]
        [SerializeField] private AICoPilotPersonality _personalityAsset;

        #endregion

        #region Private State

        private readonly List<DialogueMessage> _queue = new List<DialogueMessage>();
        private DialogueMessage _current;
        private Coroutine _displayCoroutine;
        private Coroutine _idleChatterCoroutine;

        private float _lastMessageTime;
        private bool _silenced;

        private AICoPilotSettings _settings;

        #endregion

        #region Public State

        /// <summary>All messages displayed in the current session (newest last).</summary>
        public IReadOnlyList<DialogueMessage> MessageLog => _log;
        private readonly List<DialogueMessage> _log = new List<DialogueMessage>();
        private const int MaxLogSize = 50;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _settings = AICoPilotSettings.Instance;
            _lastMessageTime = Time.time;
            _idleChatterCoroutine = StartCoroutine(IdleChatterLoop());
        }

        #endregion

        #region Public API

        /// <summary>
        /// Adds a message to the priority queue.
        /// Emergency messages clear the queue and are displayed immediately.
        /// </summary>
        /// <param name="category">Source category label.</param>
        /// <param name="text">Message text.</param>
        /// <param name="priority">Queue priority tier.</param>
        public void EnqueueMessage(string category, string text,
                                   MessagePriority priority = MessagePriority.Info)
        {
            if (_silenced && priority != MessagePriority.Emergency) return;
            if (string.IsNullOrEmpty(text)) return;

            var msg = new DialogueMessage
            {
                Category     = category,
                Text         = text,
                Priority     = priority,
                EnqueuedTime = Time.time
            };

            if (priority == MessagePriority.Emergency)
            {
                _queue.Clear();
                if (_displayCoroutine != null) StopCoroutine(_displayCoroutine);
                _current = null;
            }

            InsertSorted(msg);

            if (_current == null)
                _displayCoroutine = StartCoroutine(ProcessQueue());
        }

        /// <summary>Silences all non-emergency messages.</summary>
        public void Silence() => _silenced = true;

        /// <summary>Resumes message delivery.</summary>
        public void Resume() => _silenced = false;

        /// <summary>Repeats the last displayed message.</summary>
        public void RepeatLast()
        {
            if (_log.Count == 0) return;
            var last = _log[_log.Count - 1];
            EnqueueMessage(last.Category, last.Text, last.Priority);
        }

        /// <summary>
        /// Text-to-speech placeholder. Override or subscribe to add real TTS integration.
        /// </summary>
        /// <param name="message">Message to be spoken.</param>
        protected virtual void SpeakMessage(DialogueMessage message)
        {
            // TTS integration point — no implementation required.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ARIA TTS] [{message.Priority}] {message.Category}: {message.Text}");
#endif
        }

        #endregion

        #region Queue Processing

        private IEnumerator ProcessQueue()
        {
            while (_queue.Count > 0)
            {
                _current = _queue[0];
                _queue.RemoveAt(0);

                AppendToLog(_current);
                _lastMessageTime = Time.time;

                OnMessageDisplayed?.Invoke(_current);
                SpeakMessage(_current);

                float duration = ComputeDuration(_current);
                if (_settings != null)
                    duration *= _settings.MessageDurationMultiplier;

                yield return new WaitForSeconds(duration);

                _current = null;
                OnMessageCleared?.Invoke();
            }
        }

        private float ComputeDuration(DialogueMessage msg)
        {
            return _baseDurationSeconds + msg.Text.Length * _secondsPerCharacter;
        }

        private void InsertSorted(DialogueMessage msg)
        {
            for (int i = 0; i < _queue.Count; i++)
            {
                if ((int)msg.Priority < (int)_queue[i].Priority)
                {
                    _queue.Insert(i, msg);
                    return;
                }
            }
            _queue.Add(msg);
        }

        private void AppendToLog(DialogueMessage msg)
        {
            _log.Add(msg);
            if (_log.Count > MaxLogSize)
                _log.RemoveAt(0);
        }

        #endregion

        #region Idle Chatter

        private IEnumerator IdleChatterLoop()
        {
            while (true)
            {
                float wait = UnityEngine.Random.Range(_idleChatterThresholdSeconds, _maxIdleIntervalSeconds);
                yield return new WaitForSeconds(wait);

                if (_settings != null && !_settings.IdleChatterEnabled) continue;
                if (_silenced) continue;
                if (Time.time - _lastMessageTime < _idleChatterThresholdSeconds) continue;
                if (_queue.Count > 0) continue;

                string chatter = GetRandomIdleChatter();
                if (!string.IsNullOrEmpty(chatter))
                    EnqueueMessage("ARIA", chatter, MessagePriority.Chatter);
            }
        }

        private string GetRandomIdleChatter()
        {
            CoPilotPersonalityProfile profile = GetActiveProfile();
            if (profile?.IdleChatterPhrases == null || profile.IdleChatterPhrases.Length == 0)
                return null;

            int index = UnityEngine.Random.Range(0, profile.IdleChatterPhrases.Length);
            return profile.IdleChatterPhrases[index];
        }

        private CoPilotPersonalityProfile GetActiveProfile()
        {
            if (_personalityAsset != null) return _personalityAsset.ActiveProfile;
            return AICoPilotPersonality.CreateDefault().ActiveProfile;
        }

        #endregion
    }
}
