// VoiceCommandAnalytics.cs — SWEF Voice Command & Cockpit Voice Assistant System
using UnityEngine;

namespace SWEF.VoiceCommand
{
    /// <summary>
    /// Emits telemetry events for the Voice Command system via
    /// <c>TelemetryDispatcher.EnqueueEvent()</c>.
    /// Compiled only when <c>SWEF_ANALYTICS_AVAILABLE</c> is defined; on other
    /// platforms the class is a no-op stub that does not create compile errors.
    /// </summary>
    public class VoiceCommandAnalytics : MonoBehaviour
    {
        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (VoiceRecognitionController.Instance != null)
            {
                VoiceRecognitionController.Instance.OnKeywordRecognized += OnRecognized;
                VoiceRecognitionController.Instance.OnStateChanged      += OnStateChanged;
            }

            if (CommandExecutor.Instance != null)
                CommandExecutor.Instance.OnCommandExecuted += OnCommandExecuted;
        }

        private void OnDisable()
        {
            if (VoiceRecognitionController.Instance != null)
            {
                VoiceRecognitionController.Instance.OnKeywordRecognized -= OnRecognized;
                VoiceRecognitionController.Instance.OnStateChanged      -= OnStateChanged;
            }

            if (CommandExecutor.Instance != null)
                CommandExecutor.Instance.OnCommandExecuted -= OnCommandExecuted;
        }

        // ── Event handlers ────────────────────────────────────────────────────────

        private void OnRecognized(string phrase, float confidence)
        {
            Emit("voice_command_recognized", new System.Collections.Generic.Dictionary<string, object>
            {
                { "phrase",     phrase },
                { "confidence", confidence }
            });
        }

        private void OnStateChanged(ListeningState state)
        {
            // Only emit telemetry for terminal states (confirmed/error) to reduce noise.
            if (state != ListeningState.Confirmed && state != ListeningState.Error)
                return;

            Emit("voice_activation_mode_changed", new System.Collections.Generic.Dictionary<string, object>
            {
                { "state", state.ToString() }
            });
        }

        private void OnCommandExecuted(VoiceCommandResult result)
        {
            string eventName = result.success ? "voice_command_executed" : "voice_command_failed";

            Emit(eventName, new System.Collections.Generic.Dictionary<string, object>
            {
                { "command_id", result.executedCommand?.commandId ?? "unknown" },
                { "category",   result.executedCommand?.category.ToString() ?? "unknown" },
                { "success",    result.success }
            });
        }

        // ── Telemetry dispatch ────────────────────────────────────────────────────

        private void Emit(string eventName,
            System.Collections.Generic.Dictionary<string, object> properties)
        {
#if SWEF_ANALYTICS_AVAILABLE
            SWEF.Analytics.TelemetryDispatcher.EnqueueEvent(
                new SWEF.Analytics.TelemetryEvent(eventName, properties));
#else
            // Stub: log in development builds.
            if (Debug.isDebugBuild)
                Debug.Log($"[VoiceCommandAnalytics] Event: {eventName}");
#endif
        }
    }
}
