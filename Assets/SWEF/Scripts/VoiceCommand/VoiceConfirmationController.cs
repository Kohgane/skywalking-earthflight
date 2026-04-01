// VoiceConfirmationController.cs — SWEF Voice Command & Cockpit Voice Assistant System
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.VoiceCommand
{
    /// <summary>
    /// Manages "Are you sure?" confirmation dialogs for critical voice commands.
    /// Commands are queued and presented one at a time; each has a configurable
    /// timeout after which the pending command is auto-cancelled.
    /// </summary>
    public class VoiceConfirmationController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private VoiceAssistantConfig _config;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Fired when the player confirms the pending critical command.</summary>
        public event Action<VoiceCommandDefinition> OnConfirmed;

        /// <summary>Fired when the player cancels or the timeout elapses.</summary>
        public event Action<VoiceCommandDefinition> OnCancelled;

        /// <summary>Fired when a new command enters the confirmation queue.</summary>
        public event Action<VoiceCommandDefinition> OnConfirmationRequested;

        // ── Runtime state ─────────────────────────────────────────────────────────

        private readonly Queue<VoiceCommandDefinition> _queue =
            new Queue<VoiceCommandDefinition>();

        private VoiceCommandDefinition _current = null;
        private bool _isWaiting = false;
        private Coroutine _timeoutCoroutine;

        // ── Properties ────────────────────────────────────────────────────────────

        /// <summary>The command currently awaiting confirmation, or null.</summary>
        public VoiceCommandDefinition PendingCommand => _current;

        /// <summary>True while a confirmation dialog is active.</summary>
        public bool IsWaiting => _isWaiting;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Queues a critical command for confirmation.</summary>
        public void RequestConfirmation(VoiceCommandDefinition command)
        {
            if (command == null) return;
            _queue.Enqueue(command);
            OnConfirmationRequested?.Invoke(command);
            if (!_isWaiting)
                StartNext();
        }

        /// <summary>Confirms the currently pending command (voice "Confirm" or touch).</summary>
        public void Confirm()
        {
            if (!_isWaiting || _current == null) return;
            CancelTimeout();
            var cmd = _current;
            FinishCurrent();
            OnConfirmed?.Invoke(cmd);
        }

        /// <summary>Cancels the currently pending command (voice "Cancel" or touch).</summary>
        public void Cancel()
        {
            if (!_isWaiting || _current == null) return;
            CancelTimeout();
            var cmd = _current;
            FinishCurrent();
            OnCancelled?.Invoke(cmd);
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void StartNext()
        {
            if (_queue.Count == 0)
            {
                _isWaiting = false;
                _current   = null;
                return;
            }

            _current   = _queue.Dequeue();
            _isWaiting = true;

            float timeout = _config != null ? _config.confirmationTimeoutSeconds : 10f;
            _timeoutCoroutine = StartCoroutine(TimeoutRoutine(timeout));
        }

        private void FinishCurrent()
        {
            _current   = null;
            _isWaiting = false;
            StartNext();
        }

        private void CancelTimeout()
        {
            if (_timeoutCoroutine != null)
            {
                StopCoroutine(_timeoutCoroutine);
                _timeoutCoroutine = null;
            }
        }

        private IEnumerator TimeoutRoutine(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (_isWaiting && _current != null)
            {
                var cmd = _current;
                FinishCurrent();
                OnCancelled?.Invoke(cmd);
            }
        }
    }
}
