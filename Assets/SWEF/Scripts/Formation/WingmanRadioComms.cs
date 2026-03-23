// WingmanRadioComms.cs — SWEF Flight Formation & Wingman AI System (Phase 63)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Formation
{
    /// <summary>
    /// Commands that the player can broadcast to all wingmen via radio.
    /// </summary>
    public enum RadioCommand
    {
        /// <summary>Order all wingmen to return to formation.</summary>
        FormUp,

        /// <summary>Order all wingmen to break formation and fly freely.</summary>
        BreakFormation,

        /// <summary>Order all wingmen to attack the current target.</summary>
        AttackTarget,

        /// <summary>Order all wingmen to return to base / rally point.</summary>
        ReturnToBase,

        /// <summary>Request an acknowledgement chirp from all wingmen.</summary>
        Acknowledge,

        /// <summary>Request a status report from all wingmen.</summary>
        RequestStatus,
    }

    /// <summary>
    /// MonoBehaviour that handles in-flight radio communication between the
    /// player and all active wingmen.
    /// <para>
    /// Commands are queued and dispatched with a short <see cref="commsDelay"/>
    /// to simulate realistic radio latency.  Each wingman responds with a
    /// voice-line clip (from <see cref="WingmanPersonality.VoiceLines"/>) and
    /// a subtitle string broadcast via <see cref="OnRadioMessage"/>.
    /// </para>
    /// </summary>
    public sealed class WingmanRadioComms : MonoBehaviour
    {
        #region Events

        /// <summary>
        /// Raised whenever a wingman sends a radio message.
        /// Parameters: (callSign, messageText).
        /// </summary>
        public event Action<string, string> OnRadioMessage;

        #endregion

        #region Inspector

        [Header("Timing")]
        [Tooltip("Simulated radio delay in seconds between sending a command and " +
                 "wingmen starting to respond.")]
        [SerializeField] private float commsDelay = 0.5f;

        [Tooltip("Additional stagger delay between each wingman's response (seconds).")]
        [SerializeField] private float responseStagger = 0.3f;

        [Header("Audio")]
        [Tooltip("Optional AudioSource used to play wingman voice-line clips.  " +
                 "If null, audio playback is skipped.")]
        [SerializeField] private AudioSource audioSource;

        #endregion

        #region Runtime State

        private readonly Queue<(RadioCommand command, Transform attackTarget)> _commandQueue
            = new Queue<(RadioCommand, Transform)>();

        private bool _isProcessing;

        #endregion

        #region Public API

        /// <summary>
        /// Enqueues a command to be broadcast to all wingmen after
        /// <see cref="commsDelay"/> seconds.
        /// </summary>
        /// <param name="command">The <see cref="RadioCommand"/> to send.</param>
        public void SendCommand(RadioCommand command)
        {
            SendCommandWithTarget(command, null);
        }

        /// <summary>
        /// Enqueues an <see cref="RadioCommand.AttackTarget"/> command with a
        /// specific target transform.
        /// </summary>
        /// <param name="command">The <see cref="RadioCommand"/> to send.</param>
        /// <param name="target">Target transform (used for AttackTarget).</param>
        public void SendCommandWithTarget(RadioCommand command, Transform target)
        {
            _commandQueue.Enqueue((command, target));
            if (!_isProcessing)
                StartCoroutine(ProcessQueue());
        }

        #endregion

        #region Private — Queue Processing

        private IEnumerator ProcessQueue()
        {
            _isProcessing = true;

            while (_commandQueue.Count > 0)
            {
                var (cmd, target) = _commandQueue.Dequeue();
                yield return new WaitForSeconds(commsDelay);
                ExecuteCommand(cmd, target);
                yield return BroadcastResponses(cmd);
            }

            _isProcessing = false;
        }

        private void ExecuteCommand(RadioCommand command, Transform target)
        {
            FormationManager mgr = FormationManager.Instance;
            if (mgr == null) return;

            switch (command)
            {
                case RadioCommand.FormUp:
                    mgr.RecallAll();
                    break;

                case RadioCommand.BreakFormation:
                    mgr.BreakFormation();
                    break;

                case RadioCommand.AttackTarget:
                    if (target != null)
                        foreach (WingmanAI w in mgr.wingmen)
                            w.CommandAttack(target);
                    break;

                case RadioCommand.ReturnToBase:
                    foreach (WingmanAI w in mgr.wingmen)
                        w.CommandReturn();
                    break;

                case RadioCommand.Acknowledge:
                case RadioCommand.RequestStatus:
                    // Responses only — no state change.
                    break;
            }
        }

        private IEnumerator BroadcastResponses(RadioCommand command)
        {
            FormationManager mgr = FormationManager.Instance;
            if (mgr == null) yield break;

            foreach (WingmanAI w in mgr.wingmen)
            {
                yield return new WaitForSeconds(responseStagger);

                string callSign = w.Personality != null
                    ? w.Personality.CallSign
                    : $"Wingman-{w.assignedSlot + 1}";

                string message = BuildResponseMessage(command, w);
                OnRadioMessage?.Invoke(callSign, message);

                // Play voice line if available.
                if (audioSource != null && w.Personality != null)
                {
                    AudioClip clip = w.Personality.GetRandomVoiceLine();
                    if (clip != null)
                        audioSource.PlayOneShot(clip);
                }
            }
        }

        private static string BuildResponseMessage(RadioCommand command, WingmanAI w)
        {
            string slot = $"Slot {w.assignedSlot + 1}";
            switch (command)
            {
                case RadioCommand.FormUp:          return $"{slot}, forming up.";
                case RadioCommand.BreakFormation:  return $"{slot}, breaking formation.";
                case RadioCommand.AttackTarget:    return $"{slot}, engaging target.";
                case RadioCommand.ReturnToBase:    return $"{slot}, returning to base.";
                case RadioCommand.Acknowledge:     return $"{slot}, copy that.";
                case RadioCommand.RequestStatus:
                    string state = w.currentState.ToString();
                    bool   inPos = w.IsInFormation;
                    return $"{slot}, status: {state}{(inPos ? ", in position" : ", repositioning")}.";
                default:                           return $"{slot}, acknowledged.";
            }
        }

        #endregion
    }
}
