using System;
using System.Collections;
using UnityEngine;

namespace SWEF.Emergency
{
    /// <summary>
    /// Phase 76 — PAN-PAN / MAYDAY radio distress call flow, squawk code assignment,
    /// and ATC response simulation. Integrates with SWEF.Audio (null-safe).
    /// </summary>
    [DisallowMultipleComponent]
    public class DistressCallSystem : MonoBehaviour
    {
        #region Inspector

        [Header("Audio Keys")]
        [Tooltip("Audio clip key for PAN-PAN transmission.")]
        [SerializeField] private string panPanClipKey = "sfx_radio_panpan";

        [Tooltip("Audio clip key for MAYDAY transmission.")]
        [SerializeField] private string maydayClipKey = "sfx_radio_mayday";

        [Tooltip("Audio clip key for ATC acknowledgement.")]
        [SerializeField] private string atcAckClipKey = "sfx_radio_atc_ack";

        [Tooltip("Audio clip key for radio filter effect.")]
        [SerializeField] private string radioFilterKey = "sfx_radio_filter";

        [Header("Timing")]
        [Tooltip("Seconds after transmission before ATC responds.")]
        [SerializeField] private float atcResponseDelay = 4f;

        [Tooltip("Duration of the radio filter audio effect.")]
        [SerializeField] private float radioFilterDuration = 2f;

        #endregion

        #region Events

        /// <summary>Fired immediately when a distress transmission begins.</summary>
        public event Action<DistressCallType> OnTransmissionStarted;

        /// <summary>Fired when ATC acknowledges the call.</summary>
        public event Action<DistressCallType> OnATCAcknowledged;

        /// <summary>Fired when squawk code is set.</summary>
        public event Action<int> OnSquawkSet;

        #endregion

        #region Private State

        private bool _callInProgress;
        private Coroutine _callCoroutine;

        #endregion

        #region Public API

        /// <summary>Initiate a distress call for the given emergency.</summary>
        /// <param name="emergency">Active emergency context.</param>
        /// <param name="callType">Type of call to make.</param>
        public void MakeDistressCall(ActiveEmergency emergency, DistressCallType callType)
        {
            if (_callInProgress || callType == DistressCallType.None) return;
            _callCoroutine = StartCoroutine(DistressCallFlow(emergency, callType));
        }

        /// <summary>Set the transponder squawk code.</summary>
        /// <param name="code">Squawk code (e.g. 7700, 7600, 7500).</param>
        public void SetSquawk(int code)
        {
            OnSquawkSet?.Invoke(code);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[DistressCallSystem] Squawk set to {code}");
#endif
        }

        /// <summary>Returns true when a distress call is currently in progress.</summary>
        public bool IsCallInProgress => _callInProgress;

        #endregion

        #region Private Helpers

        private IEnumerator DistressCallFlow(ActiveEmergency emergency, DistressCallType callType)
        {
            _callInProgress = true;
            OnTransmissionStarted?.Invoke(callType);

            // Apply radio filter effect
            PlayAudio(radioFilterKey);
            yield return new WaitForSeconds(radioFilterDuration);

            // Play appropriate call clip
            string clipKey = callType == DistressCallType.Mayday ? maydayClipKey : panPanClipKey;
            PlayAudio(clipKey);

            // Set squawk code
            int squawk = SquawkCodeFor(callType);
            if (squawk > 0)
                SetSquawk(squawk);

            // Register with manager
            EmergencyManager.Instance?.MakeDistressCall(emergency, callType);

            // Wait for ATC response
            yield return new WaitForSeconds(atcResponseDelay);

            PlayAudio(atcAckClipKey);
            OnATCAcknowledged?.Invoke(callType);

            _callInProgress = false;
        }

        private static int SquawkCodeFor(DistressCallType callType)
        {
            return callType switch
            {
                DistressCallType.Mayday      => 7700,
                DistressCallType.PanPan      => 7700,
                DistressCallType.Squawk7700  => 7700,
                DistressCallType.Squawk7600  => 7600,
                DistressCallType.Squawk7500  => 7500,
                _                            => 0
            };
        }

        private void PlayAudio(string clipKey)
        {
#if SWEF_AUDIO_AVAILABLE
            SWEF.Audio.AudioManager.Instance?.PlayClip(clipKey);
#endif
        }

        #endregion
    }
}
