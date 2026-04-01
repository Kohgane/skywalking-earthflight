// VoiceCommandHUD.cs — SWEF Voice Command & Cockpit Voice Assistant System
using UnityEngine;

namespace SWEF.VoiceCommand
{
    /// <summary>
    /// MonoBehaviour that drives the cockpit voice-assistant HUD overlay.
    /// Displays: microphone state indicator, recognised-phrase text with confidence
    /// bar, response toast, audio-level meter, and wake-word indicator.
    /// Actual UI wiring (Canvas/Text/Image refs) is set up via inspector or code;
    /// all Unity Object references are guarded so the component runs without them.
    /// </summary>
    public class VoiceCommandHUD : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("HUD References (optional — assign in editor)")]
        [Tooltip("Root GameObject shown while in any active voice state.")]
        [SerializeField] private GameObject _hudRoot;

        [Tooltip("Text component that shows the recognised phrase.")]
        [SerializeField] private UnityEngine.UI.Text _phraseText;

        [Tooltip("Text component that shows the assistant response toast.")]
        [SerializeField] private UnityEngine.UI.Text _responseText;

        [Tooltip("Slider used as the confidence bar (0–1 value).")]
        [SerializeField] private UnityEngine.UI.Slider _confidenceBar;

        [Tooltip("Slider used as the audio-level meter (0–1 value).")]
        [SerializeField] private UnityEngine.UI.Slider _audioLevelMeter;

        [Tooltip("Image tinted per state (idle/listening/processing/confirmed/error).")]
        [SerializeField] private UnityEngine.UI.Image _stateIndicator;

        [Header("State Colours")]
        [SerializeField] private Color _colourIdle       = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        [SerializeField] private Color _colourListening  = new Color(0.2f, 0.8f, 0.2f, 0.9f);
        [SerializeField] private Color _colourProcessing = new Color(0.2f, 0.5f, 1.0f, 0.9f);
        [SerializeField] private Color _colourConfirmed  = new Color(1.0f, 0.8f, 0.1f, 0.9f);
        [SerializeField] private Color _colourError      = new Color(1.0f, 0.2f, 0.2f, 0.9f);

        [Header("Display")]
        [Tooltip("Seconds the response toast is visible before fading out.")]
        [SerializeField] private float _toastDuration = 3f;

        [Tooltip("When true, the HUD uses a compact layout (smaller, fewer elements).")]
        [SerializeField] private bool _compactMode = false;

        // ── Runtime state ─────────────────────────────────────────────────────────

        private float _toastTimer = 0f;
        private bool  _toastVisible = false;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (VoiceRecognitionController.Instance != null)
            {
                VoiceRecognitionController.Instance.OnStateChanged    += HandleStateChanged;
                VoiceRecognitionController.Instance.OnKeywordRecognized += HandlePhraseRecognised;
            }

            if (CommandExecutor.Instance != null)
                CommandExecutor.Instance.OnCommandExecuted += HandleCommandExecuted;
        }

        private void OnDisable()
        {
            if (VoiceRecognitionController.Instance != null)
            {
                VoiceRecognitionController.Instance.OnStateChanged    -= HandleStateChanged;
                VoiceRecognitionController.Instance.OnKeywordRecognized -= HandlePhraseRecognised;
            }

            if (CommandExecutor.Instance != null)
                CommandExecutor.Instance.OnCommandExecuted -= HandleCommandExecuted;
        }

        private void Update()
        {
            // Update audio level meter every frame.
            if (_audioLevelMeter != null && VoiceRecognitionController.Instance != null)
                _audioLevelMeter.value = VoiceRecognitionController.Instance.AudioLevel;

            // Toast countdown.
            if (_toastVisible)
            {
                _toastTimer -= Time.deltaTime;
                if (_toastTimer <= 0f)
                {
                    _toastVisible = false;
                    if (_responseText != null) _responseText.text = string.Empty;
                }
            }
        }

        // ── Event handlers ────────────────────────────────────────────────────────

        private void HandleStateChanged(ListeningState state)
        {
            SetIndicatorColour(state);

            bool hudVisible = state != ListeningState.Idle;
            if (_hudRoot != null && !_compactMode)
                _hudRoot.SetActive(hudVisible);
        }

        private void HandlePhraseRecognised(string phrase, float confidence)
        {
            if (_phraseText   != null) _phraseText.text = phrase;
            if (_confidenceBar != null) _confidenceBar.value = confidence;
        }

        private void HandleCommandExecuted(VoiceCommandResult result)
        {
            string response = VoiceResponseGenerator.GetShortResponse(result);
            ShowToast(response);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void SetIndicatorColour(ListeningState state)
        {
            if (_stateIndicator == null) return;

            switch (state)
            {
                case ListeningState.Idle:        _stateIndicator.color = _colourIdle;       break;
                case ListeningState.Listening:   _stateIndicator.color = _colourListening;  break;
                case ListeningState.Processing:  _stateIndicator.color = _colourProcessing; break;
                case ListeningState.Confirmed:   _stateIndicator.color = _colourConfirmed;  break;
                case ListeningState.Error:       _stateIndicator.color = _colourError;      break;
            }
        }

        private void ShowToast(string message)
        {
            if (_responseText == null) return;
            _responseText.text = message;
            _toastTimer  = _toastDuration;
            _toastVisible = true;
        }
    }
}
