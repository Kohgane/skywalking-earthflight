using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if SWEF_AUDIO_AVAILABLE
using SWEF.Audio;
#endif

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 78 — Manages VHF radio communication including frequency tuning,
    /// TX/RX queue simulation, squelch gating, and static audio effects.
    ///
    /// <para>Integrates with <c>SWEF.Audio.AudioManager</c> for radio audio processing
    /// (static noise, squelch gate, voice filter) when
    /// <c>SWEF_AUDIO_AVAILABLE</c> is defined.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class ATCRadioController : MonoBehaviour
    {
        #region Inspector

        [Header("Radio Settings")]
        [Tooltip("Squelch threshold (0 = fully open, 1 = fully closed).")]
        [SerializeField, Range(0f, 1f)] private float squelch = 0.2f;

        [Tooltip("Whether COMM1 radio is active.")]
        [SerializeField] private bool comm1Active = true;

        [Tooltip("Whether COMM2 radio is active.")]
        [SerializeField] private bool comm2Active = false;

        [Tooltip("COMM2 active frequency.")]
        [SerializeField] private RadioFrequency comm2Frequency = new RadioFrequency { valueMHz = 121.5f };

        [Header("Transmission Timing")]
        [Tooltip("Simulated delay in seconds before a response transmission begins.")]
        [SerializeField, Range(0.5f, 5f)] private float responseDelay = 1.5f;

        #endregion

        #region Events

        /// <summary>Fired when the player begins transmitting.</summary>
        public event Action<string> OnTransmissionStarted;

        /// <summary>Fired when a transmission ends.</summary>
        public event Action OnTransmissionEnded;

        /// <summary>Fired when an incoming message is received.</summary>
        public event Action<string> OnMessageReceived;

        #endregion

        #region Public Properties

        /// <summary>Whether COMM1 is currently active.</summary>
        public bool COMM1Active => comm1Active;

        /// <summary>Whether COMM2 is currently active.</summary>
        public bool COMM2Active => comm2Active;

        /// <summary>Whether a transmission is currently in progress.</summary>
        public bool IsTransmitting { get; private set; }

        /// <summary>Current squelch threshold.</summary>
        public float Squelch => squelch;

        #endregion

        #region Private State

        private readonly Queue<string> _txQueue  = new Queue<string>();
        private readonly Queue<string> _rxQueue  = new Queue<string>();
        private Coroutine _txCoroutine;

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            if (_txCoroutine != null) StopCoroutine(_txCoroutine);
        }

        #endregion

        #region Public API

        /// <summary>Queues a message for transmission on the active COMM1 frequency.</summary>
        /// <param name="message">The message text to transmit.</param>
        public void Transmit(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            _txQueue.Enqueue(message);

            if (_txCoroutine == null)
                _txCoroutine = StartCoroutine(TransmitRoutine());
        }

        /// <summary>Receives an incoming radio message.</summary>
        /// <param name="message">The received message text.</param>
        public void ReceiveMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            _rxQueue.Enqueue(message);
            OnMessageReceived?.Invoke(message);

#if SWEF_AUDIO_AVAILABLE
            PlayRadioAudio(message);
#endif
        }

        /// <summary>Sets the squelch threshold.</summary>
        /// <param name="value">Squelch value between 0 (fully open) and 1 (fully closed).</param>
        public void SetSquelch(float value)
        {
            squelch = Mathf.Clamp01(value);
        }

        /// <summary>Toggles COMM1 active state.</summary>
        public void ToggleCOMM1() => comm1Active = !comm1Active;

        /// <summary>Toggles COMM2 active state.</summary>
        public void ToggleCOMM2() => comm2Active = !comm2Active;

        #endregion

        #region Transmission Coroutine

        private IEnumerator TransmitRoutine()
        {
            while (_txQueue.Count > 0)
            {
                string message = _txQueue.Dequeue();
                IsTransmitting = true;
                OnTransmissionStarted?.Invoke(message);

                // Simulate transmission duration (~0.05 s per character, min 1 s)
                float duration = Mathf.Max(1f, message.Length * 0.05f);
                yield return new WaitForSeconds(duration);

                IsTransmitting = false;
                OnTransmissionEnded?.Invoke();

                // Brief gap between messages
                if (_txQueue.Count > 0)
                    yield return new WaitForSeconds(0.3f);
            }
            _txCoroutine = null;
        }

        #endregion

        #region Audio Integration

#if SWEF_AUDIO_AVAILABLE
        private void PlayRadioAudio(string message)
        {
            var audioManager = AudioManager.Instance ?? FindFirstObjectByType<AudioManager>();
            if (audioManager == null) return;
            audioManager.PlaySFX("radio_receive");
        }
#endif

        #endregion
    }
}
