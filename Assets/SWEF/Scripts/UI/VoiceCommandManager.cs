using System;
using UnityEngine;

namespace SWEF.UI
{
    /// <summary>
    /// Stub MonoBehaviour for future voice-command control.
    /// Parses plain-text transcripts against a keyword table and fires
    /// <see cref="OnVoiceCommandRecognized"/> when a match is found.
    /// Actual platform speech-recognition integration is left as a TODO.
    /// </summary>
    public class VoiceCommandManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [SerializeField] private bool voiceCommandsEnabled = false;

        // ── State ─────────────────────────────────────────────────────────────────
        private bool _isListening;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raises when a recognized voice command is detected.</summary>
        public event Action<VoiceCommand> OnVoiceCommandRecognized;

        // ── Properties ───────────────────────────────────────────────────────────
        /// <summary>Whether voice-command processing is globally enabled.</summary>
        public bool VoiceCommandsEnabled => voiceCommandsEnabled;

        /// <summary>Whether the manager is actively listening for speech input.</summary>
        public bool IsListening => _isListening;

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Starts the speech-recognition session.
        /// Platform SDK integration is stubbed here — see TODO below.
        /// </summary>
        public void StartListening()
        {
            if (!voiceCommandsEnabled) return;
            _isListening = true;
            Debug.Log("[SWEF] VoiceCommandManager: StartListening");
            // TODO: Integrate platform speech-to-text SDK (iOS SFSpeechRecognizer / Android SpeechRecognizer)
        }

        /// <summary>Stops the speech-recognition session.</summary>
        public void StopListening()
        {
            _isListening = false;
            Debug.Log("[SWEF] VoiceCommandManager: StopListening");
            // TODO: Integrate platform speech-to-text SDK — stop recognition session
        }

        /// <summary>
        /// Parses a plain-text transcript and fires <see cref="OnVoiceCommandRecognized"/>
        /// when a known keyword is detected.
        /// </summary>
        /// <param name="transcript">Lower-cased, trimmed speech transcript.</param>
        public void ProcessVoiceInput(string transcript)
        {
            if (!voiceCommandsEnabled || string.IsNullOrEmpty(transcript)) return;

            transcript = transcript.ToLowerInvariant().Trim();

            VoiceCommand? cmd = null;

            if (transcript.Contains("screenshot") || transcript.Contains("take photo"))
                cmd = VoiceCommand.Screenshot;
            else if (transcript.Contains("teleport") || transcript.Contains("go to"))
                cmd = VoiceCommand.Teleport;
            else if (transcript.Contains("pause") || transcript.Contains("stop"))
                cmd = VoiceCommand.Pause;
            else if (transcript.Contains("resume") || transcript.Contains("continue"))
                cmd = VoiceCommand.Resume;
            else if (transcript.Contains("higher") || transcript.Contains(" up"))
                cmd = VoiceCommand.AltitudeUp;
            else if (transcript.Contains("lower") || transcript.Contains(" down"))
                cmd = VoiceCommand.AltitudeDown;
            else if (transcript.Contains("faster") || transcript.Contains("speed up"))
                cmd = VoiceCommand.SpeedUp;
            else if (transcript.Contains("slower") || transcript.Contains("slow down"))
                cmd = VoiceCommand.SlowDown;
            else if (transcript.Contains("hide hud") || transcript.Contains("show hud"))
                cmd = VoiceCommand.ToggleHUD;
            else if (transcript.Contains("recenter"))
                cmd = VoiceCommand.Recenter;

            if (cmd.HasValue)
            {
                Debug.Log($"[SWEF] Voice command recognized: {cmd.Value}");
                OnVoiceCommandRecognized?.Invoke(cmd.Value);
            }
        }

        /// <summary>Enables or disables voice-command processing.</summary>
        public void SetVoiceCommandsEnabled(bool enabled)
        {
            voiceCommandsEnabled = enabled;
            if (!enabled && _isListening)
                StopListening();
        }
    }
}
