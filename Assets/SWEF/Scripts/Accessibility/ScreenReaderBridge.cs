using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Accessibility
{
    // ── TTS interface ─────────────────────────────────────────────────────────────

    /// <summary>Abstraction over text-to-speech engines on different platforms.</summary>
    public interface ITTSEngine
    {
        /// <summary>Speaks the given text at the specified pitch and rate.</summary>
        void Speak(string text, float pitch, float rate);

        /// <summary>Stops any speech currently in progress.</summary>
        void Stop();

        /// <summary>Returns <c>true</c> when the engine is currently speaking.</summary>
        bool IsSpeaking { get; }
    }

    // ── Priority levels ──────────────────────────────────────────────────────────

    /// <summary>Priority level for a TTS announcement.</summary>
    public enum SpeechPriority
    {
        /// <summary>Highest — interrupts everything (e.g., collision warning).</summary>
        Critical = 0,
        /// <summary>Interrupts Medium and Low (e.g., mission objective).</summary>
        High = 1,
        /// <summary>Queued after High (e.g., navigation hint).</summary>
        Medium = 2,
        /// <summary>Only spoken when queue is empty (e.g., ambient descriptions).</summary>
        Low = 3
    }

    // ── Built-in TTS stub ─────────────────────────────────────────────────────────

    /// <summary>
    /// Console-logging TTS stub used when no native TTS engine is available.
    /// </summary>
    internal class ConsoleTTSEngine : ITTSEngine
    {
        private bool _speaking;
        public bool IsSpeaking => _speaking;

        public void Speak(string text, float pitch, float rate)
        {
            _speaking = true;
            Debug.Log($"[SWEF TTS] Speak (pitch={pitch:F2}, rate={rate:F0}): \"{text}\"");
            // In a real implementation this would schedule a callback; stub completes immediately.
            _speaking = false;
        }

        public void Stop()
        {
            _speaking = false;
            Debug.Log("[SWEF TTS] Stop");
        }
    }

    // ── Queue entry ──────────────────────────────────────────────────────────────

    internal class SpeechQueueEntry
    {
        public string         Text;
        public SpeechPriority Priority;
        public float          Pitch;
        public float          Rate;
    }

    /// <summary>
    /// Bridges the game's UI and flight systems to platform screen-reader / TTS APIs.
    /// Provides a priority queue, earcon support, and navigation announcements.
    /// </summary>
    public class ScreenReaderBridge : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static ScreenReaderBridge Instance { get; private set; }

        // ── Serialised fields ────────────────────────────────────────────────────
        [Header("Speech Settings")]
        [SerializeField] private bool  screenReaderEnabled = false;
        [SerializeField] [Range(80f, 400f)] private float wordsPerMinute = 180f;
        [SerializeField] [Range(0.5f, 2f)]  private float defaultPitch   = 1f;

        [Header("Earcons")]
        [SerializeField] private AudioClip earconButtonPress;
        [SerializeField] private AudioClip earconError;
        [SerializeField] private AudioClip earconSuccess;
        [SerializeField] private AudioClip earconNavigation;

        // ── Runtime state ─────────────────────────────────────────────────────────
        private ITTSEngine          _engine;
        private AudioSource         _audioSource;
        private readonly Queue<SpeechQueueEntry> _speechQueue = new Queue<SpeechQueueEntry>();
        private SpeechQueueEntry    _current;
        private Coroutine           _processRoutine;
        private string              _focusedElementLabel = string.Empty;

        /// <summary>Whether screen-reader / TTS announcements are enabled.</summary>
        public bool Enabled => screenReaderEnabled;

        /// <summary>Label of the UI element that currently has focus.</summary>
        public string FocusedElementLabel => _focusedElementLabel;

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Fired when a TTS utterance begins.</summary>
        public event Action<string> OnSpeechStarted;

        /// <summary>Fired when a TTS utterance finishes.</summary>
        public event Action<string> OnSpeechCompleted;

        /// <summary>Fired when the focused UI element changes.</summary>
        public event Action<string> OnFocusChanged;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0f; // 2D UI audio

            InitialiseTTSEngine();
        }

        private void InitialiseTTSEngine()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _engine = new IOSVoiceOverEngine();
#elif UNITY_ANDROID && !UNITY_EDITOR
            _engine = new AndroidTalkBackEngine();
#elif UNITY_STANDALONE_WIN && !UNITY_EDITOR
            _engine = new WindowsNarratorEngine();
#else
            _engine = new ConsoleTTSEngine();
#endif
            Debug.Log($"[SWEF ScreenReader] TTS engine: {_engine.GetType().Name}");
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Queues a text announcement with the given priority.
        /// Higher-priority entries interrupt lower-priority ones already speaking.
        /// </summary>
        public void Announce(string text, SpeechPriority priority = SpeechPriority.Medium)
        {
            if (!screenReaderEnabled || string.IsNullOrWhiteSpace(text)) return;

            var entry = new SpeechQueueEntry
            {
                Text     = text,
                Priority = priority,
                Pitch    = defaultPitch,
                Rate     = wordsPerMinute
            };

            if (_current != null && priority < _current.Priority)
            {
                // Interrupt lower-priority speech
                _engine.Stop();
                _current = null;
            }

            _speechQueue.Enqueue(entry);
            if (_processRoutine == null)
                _processRoutine = StartCoroutine(ProcessQueue());
        }

        /// <summary>
        /// Announces a screen or panel transition (e.g., "Settings opened").
        /// </summary>
        public void AnnounceNavigation(string screenName)
        {
            Announce(screenName, SpeechPriority.High);
            PlayEarcon(earconNavigation);
        }

        /// <summary>
        /// Notifies the bridge that a UI element has received focus.
        /// Reads the element's label, type, and state aloud.
        /// </summary>
        public void ReportFocus(string label, string type = "", string state = "")
        {
            _focusedElementLabel = label;
            string announcement = string.IsNullOrEmpty(type)
                ? label
                : $"{label}, {type}{(string.IsNullOrEmpty(state) ? string.Empty : ", " + state)}";
            Announce(announcement, SpeechPriority.High);
            OnFocusChanged?.Invoke(label);
        }

        /// <summary>Stops all speech and clears the queue.</summary>
        public void StopAll()
        {
            _speechQueue.Clear();
            _engine?.Stop();
            _current = null;
            if (_processRoutine != null)
            {
                StopCoroutine(_processRoutine);
                _processRoutine = null;
            }
        }

        /// <summary>Enables or disables the screen reader at runtime.</summary>
        public void SetEnabled(bool enabled)
        {
            screenReaderEnabled = enabled;
            if (!enabled) StopAll();
        }

        /// <summary>Sets words-per-minute speech rate (80–400).</summary>
        public void SetRate(float wpm) => wordsPerMinute = Mathf.Clamp(wpm, 80f, 400f);

        // ── Earcon helpers ───────────────────────────────────────────────────────

        /// <summary>Plays the button-press earcon.</summary>
        public void PlayButtonPress()  => PlayEarcon(earconButtonPress);

        /// <summary>Plays the error earcon.</summary>
        public void PlayError()        => PlayEarcon(earconError);

        /// <summary>Plays the success earcon.</summary>
        public void PlaySuccess()      => PlayEarcon(earconSuccess);

        private void PlayEarcon(AudioClip clip)
        {
            if (clip == null || _audioSource == null) return;
            _audioSource.PlayOneShot(clip);
        }

        // ── Queue processing ─────────────────────────────────────────────────────
        private IEnumerator ProcessQueue()
        {
            while (_speechQueue.Count > 0)
            {
                _current = _speechQueue.Dequeue();
                _engine.Speak(_current.Text, _current.Pitch, _current.Rate);
                OnSpeechStarted?.Invoke(_current.Text);

                // Estimate duration from word count and WPM
                int wordCount  = _current.Text.Split(new char[]{' ','\t','\n','\r'}, StringSplitOptions.RemoveEmptyEntries).Length;
                float duration = (wordCount / Mathf.Max(_current.Rate, 1f)) * 60f;
                duration = Mathf.Max(duration, 0.3f);

                yield return new WaitForSeconds(duration);

                OnSpeechCompleted?.Invoke(_current.Text);
                _current = null;
            }
            _processRoutine = null;
        }
    }

    // ── Platform-specific stubs ──────────────────────────────────────────────────

#if UNITY_IOS
    /// <summary>iOS VoiceOver integration stub.</summary>
    internal class IOSVoiceOverEngine : ITTSEngine
    {
        public bool IsSpeaking => false;
        public void Speak(string text, float pitch, float rate) => Debug.Log($"[VoiceOver] Speak: {text}");
        public void Stop() => Debug.Log("[VoiceOver] Stop");
    }
#endif

#if UNITY_ANDROID
    /// <summary>Android TalkBack integration stub.</summary>
    internal class AndroidTalkBackEngine : ITTSEngine
    {
        public bool IsSpeaking => false;
        public void Speak(string text, float pitch, float rate) => Debug.Log($"[TalkBack] Speak: {text}");
        public void Stop() => Debug.Log("[TalkBack] Stop");
    }
#endif

#if UNITY_STANDALONE_WIN
    /// <summary>Windows Narrator integration stub.</summary>
    internal class WindowsNarratorEngine : ITTSEngine
    {
        public bool IsSpeaking => false;
        public void Speak(string text, float pitch, float rate) => Debug.Log($"[Narrator] Speak: {text}");
        public void Stop() => Debug.Log("[Narrator] Stop");
    }
#endif
}
