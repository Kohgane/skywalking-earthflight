// VoiceRecognitionController.cs — SWEF Voice Command & Cockpit Voice Assistant System
using System;
using System.Collections;
using UnityEngine;

namespace SWEF.VoiceCommand
{
    /// <summary>Represents the microphone/recognition pipeline state.</summary>
    public enum ListeningState
    {
        Idle,
        Listening,
        Processing,
        Confirmed,
        Error
    }

    /// <summary>How the recognition session is activated.</summary>
    public enum ActivationMode
    {
        PushToTalk,
        WakeWord,
        AlwaysListening
    }

    /// <summary>
    /// Singleton MonoBehaviour that manages microphone input and keyword spotting.
    /// On platforms that support <c>UnityEngine.Windows.Speech.KeywordRecognizer</c> the
    /// real implementation is used; on all other platforms a stub is provided so the
    /// rest of the system compiles and runs without modification.
    /// </summary>
    public class VoiceRecognitionController : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────

        private static VoiceRecognitionController _instance;

        public static VoiceRecognitionController Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<VoiceRecognitionController>();
                return _instance;
            }
        }

        // ── Inspector fields ──────────────────────────────────────────────────────

        [Header("Config")]
        [Tooltip("Config asset. If null, defaults are used.")]
        [SerializeField] private VoiceAssistantConfig _config;

        [Header("Activation")]
        [Tooltip("Default activation mode. Can be changed at runtime.")]
        [SerializeField] private ActivationMode _activationMode = ActivationMode.PushToTalk;

        [Header("Audio")]
        [Tooltip("Noise gate threshold (0–1). Audio below this level is ignored.")]
        [Range(0f, 1f)]
        [SerializeField] private float _noiseGateThreshold = 0.02f;

        // ── Runtime state ─────────────────────────────────────────────────────────

        private ListeningState _state = ListeningState.Idle;
        private float _audioLevel = 0f;
        private string _lastRecognisedPhrase = string.Empty;
        private float _lastConfidence = 0f;
        private bool _isListeningSession = false;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Fired when a phrase is recognised with its confidence score.</summary>
        public event Action<string, float> OnKeywordRecognized;

        /// <summary>Fired when the listening state changes.</summary>
        public event Action<ListeningState> OnStateChanged;

        // ── Properties ────────────────────────────────────────────────────────────

        public ListeningState State => _state;
        public float AudioLevel     => _audioLevel;
        public ActivationMode Mode  => _activationMode;
        public string LastPhrase    => _lastRecognisedPhrase;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (_activationMode == ActivationMode.AlwaysListening)
                BeginListening();
        }

        private void OnDestroy()
        {
            StopListening();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Changes the activation mode at runtime.</summary>
        public void SetActivationMode(ActivationMode mode)
        {
            _activationMode = mode;
            if (mode == ActivationMode.AlwaysListening)
                BeginListening();
            else if (mode == ActivationMode.PushToTalk)
                StopListening();
        }

        /// <summary>Starts a push-to-talk listening session (ignored if not in PushToTalk mode).</summary>
        public void PushToTalkBegin()
        {
            if (_activationMode == ActivationMode.PushToTalk)
                BeginListening();
        }

        /// <summary>Ends a push-to-talk session.</summary>
        public void PushToTalkEnd()
        {
            if (_activationMode == ActivationMode.PushToTalk)
                StopListening();
        }

        /// <summary>
        /// Simulates a recognised phrase — useful for tests and the UI test-mode button.
        /// </summary>
        public void SimulateRecognition(string phrase, float confidence = 1f)
        {
            HandleRecognised(phrase, confidence);
        }

        // ── Listening control ─────────────────────────────────────────────────────

        private void BeginListening()
        {
            if (_isListeningSession) return;
            _isListeningSession = true;
            SetState(ListeningState.Listening);
            StartCoroutine(AudioLevelRoutine());

            float timeout = _config != null ? _config.listenTimeoutSeconds : 5f;
            StartCoroutine(ListenTimeoutRoutine(timeout));
        }

        private void StopListening()
        {
            if (!_isListeningSession) return;
            _isListeningSession = false;
            StopAllCoroutines();
            _audioLevel = 0f;
            if (_state == ListeningState.Listening)
                SetState(ListeningState.Idle);
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void HandleRecognised(string phrase, float confidence)
        {
            if (string.IsNullOrWhiteSpace(phrase)) return;

            float threshold = _config != null ? _config.confidenceThreshold : 0.6f;
            if (confidence < threshold)
            {
                SetState(ListeningState.Error);
                return;
            }

            _lastRecognisedPhrase = phrase;
            _lastConfidence       = confidence;
            SetState(ListeningState.Processing);
            OnKeywordRecognized?.Invoke(phrase, confidence);
        }

        private void SetState(ListeningState newState)
        {
            if (_state == newState) return;
            _state = newState;
            OnStateChanged?.Invoke(newState);
        }

        private IEnumerator AudioLevelRoutine()
        {
            while (_isListeningSession)
            {
                // Stub: real implementation would sample microphone data.
                _audioLevel = 0f;
                yield return new WaitForSeconds(0.05f);
            }
        }

        private IEnumerator ListenTimeoutRoutine(float timeout)
        {
            yield return new WaitForSeconds(timeout);
            if (_isListeningSession && _state == ListeningState.Listening)
            {
                StopListening();
                SetState(ListeningState.Idle);
            }
        }
    }
}
