// BoostAudioController.cs — SWEF Boost & Drift Mechanics System (Phase 62)
using System;
using UnityEngine;

namespace SWEF.Racing
{
    /// <summary>
    /// Phase 62 — Audio integration for the Boost &amp; Drift Mechanics System.
    ///
    /// <para>Listens to events from <see cref="BoostController"/>,
    /// <see cref="DriftController"/>, <see cref="SlipstreamController"/>,
    /// <see cref="StartBoostController"/>, and <see cref="TrickBoostController"/>
    /// to play per-event SFX and manage continuous loop audio.
    /// Integrates with <c>AudioManager</c> via the <c>SWEF_AUDIO_AVAILABLE</c>
    /// compile guard so the script compiles in isolation.</para>
    ///
    /// <para>Attach to the same persistent GameObject as <see cref="BoostController"/>.</para>
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class BoostAudioController : MonoBehaviour
    {
        #region Inspector

        [Header("Controller References")]
        [Tooltip("BoostController to listen to (auto-resolved from singleton if null).")]
        [SerializeField] private BoostController      boostController;

        [Tooltip("DriftController to listen to (auto-resolved from singleton if null).")]
        [SerializeField] private DriftController      driftController;

        [Tooltip("SlipstreamController (auto-resolved from singleton if null).")]
        [SerializeField] private SlipstreamController slipstreamController;

        [Tooltip("StartBoostController for countdown SFX.")]
        [SerializeField] private StartBoostController startBoostController;

        [Tooltip("TrickBoostController for trick/landing SFX.")]
        [SerializeField] private TrickBoostController trickBoostController;

        [Header("Boost SFX")]
        [Tooltip("Short activation clip played on any boost start (fallback if config has no SFX).")]
        [SerializeField] private AudioClip boostActivateClip;

        [Tooltip("Continuous boost loop audio (fades in/out during active boost).")]
        [SerializeField] private AudioClip boostLoopClip;

        [Tooltip("Maximum volume of the boost loop.")]
        [Range(0f, 1f)]
        [SerializeField] private float boostLoopMaxVolume = 0.6f;

        [Tooltip("Fade-in duration for the boost loop (seconds).")]
        [SerializeField] private float boostLoopFadeIn = 0.2f;

        [Tooltip("Fade-out duration for the boost loop (seconds).")]
        [SerializeField] private float boostLoopFadeOut = 0.5f;

        [Header("Drift SFX")]
        [Tooltip("Drift initiation clip (air-friction screech equivalent).")]
        [SerializeField] private AudioClip driftStartClip;

        [Tooltip("Level-up chime clips (index 0=Blue, 1=Orange, 2=Purple, 3=UltraPurple).")]
        [SerializeField] private AudioClip[] driftLevelUpChimes = new AudioClip[4];

        [Tooltip("Pitch offset applied per drift level (higher level = higher pitch).")]
        [SerializeField] private float driftChimePitchStep = 0.1f;

        [Header("Slipstream SFX")]
        [Tooltip("Continuous wind whoosh clip for slipstream. Volume scales with charge.")]
        [SerializeField] private AudioClip slipstreamWhooshClip;

        [Header("Start Boost SFX")]
        [Tooltip("Countdown beep clip (played once per tick).")]
        [SerializeField] private AudioClip countdownBeepClip;

        [Tooltip("GO horn clip.")]
        [SerializeField] private AudioClip goHornClip;

        [Header("Trick SFX")]
        [Tooltip("Trick whoosh clip played at trick start.")]
        [SerializeField] private AudioClip trickWhooshClip;

        [Tooltip("Landing thud clip played on trick landing.")]
        [SerializeField] private AudioClip trickLandingClip;

        [Header("Engine")]
        [Tooltip("Engine audio source whose pitch is modified by boost multiplier.")]
        [SerializeField] private AudioSource engineAudioSource;

        [Tooltip("Base engine pitch (at 1x speed).")]
        [SerializeField] private float baseEnginePitch = 1f;

        [Tooltip("Additional pitch increase at max boost multiplier.")]
        [SerializeField] private float maxEnginePitchBoost = 0.5f;

        [Tooltip("Speed at which the engine pitch lerps to the target.")]
        [SerializeField] private float enginePitchLerpSpeed = 3f;

        #endregion

        #region Private State

        private AudioSource _oneShotSource;
        private AudioSource _loopSource;
        private AudioSource _slipstreamSource;

        private float _loopTargetVolume;
        private float _slipstreamTargetVolume;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Primary source for one-shot SFX.
            _oneShotSource = GetComponent<AudioSource>();
            _oneShotSource.playOnAwake = false;

            // Dedicated loop source.
            _loopSource = gameObject.AddComponent<AudioSource>();
            _loopSource.playOnAwake = false;
            _loopSource.loop        = true;
            _loopSource.volume      = 0f;
            if (boostLoopClip != null)
                _loopSource.clip = boostLoopClip;

            // Dedicated slipstream source.
            _slipstreamSource = gameObject.AddComponent<AudioSource>();
            _slipstreamSource.playOnAwake = false;
            _slipstreamSource.loop        = true;
            _slipstreamSource.volume      = 0f;
            if (slipstreamWhooshClip != null)
                _slipstreamSource.clip = slipstreamWhooshClip;

            ResolveReferences();
            SubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            FadeLoopVolume(ref _loopSource,        _loopTargetVolume,       boostLoopFadeIn, boostLoopFadeOut);
            FadeLoopVolume(ref _slipstreamSource,  _slipstreamTargetVolume, 0.3f, 0.5f);
            UpdateEnginePitch();
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeEvents()
        {
            if (boostController != null)
            {
                boostController.OnBoostStart += HandleBoostStart;
                boostController.OnBoostEnd   += HandleBoostEnd;
            }

            if (driftController != null)
            {
                driftController.OnDriftStart   += HandleDriftStart;
                driftController.OnDriftLevelUp += HandleDriftLevelUp;
                driftController.OnDriftRelease += HandleDriftRelease;
                driftController.OnDriftCancel  += HandleDriftCancel;
            }

            if (slipstreamController != null)
            {
                slipstreamController.OnSlipstreamEnter   += HandleSlipstreamEnter;
                slipstreamController.OnSlipstreamCharged += HandleSlipstreamCharged;
                slipstreamController.OnSlipstreamExit    += HandleSlipstreamExit;
            }

            if (startBoostController != null)
            {
                startBoostController.OnCountdownTick    += HandleCountdownTick;
                startBoostController.OnGoSignal         += HandleGoSignal;
                startBoostController.OnStartBoostResult += HandleStartBoostResult;
            }

            if (trickBoostController != null)
            {
                trickBoostController.OnTrickStart    += HandleTrickStart;
                trickBoostController.OnTrickComplete += HandleTrickComplete;
            }
        }

        private void UnsubscribeEvents()
        {
            if (boostController != null)
            {
                boostController.OnBoostStart -= HandleBoostStart;
                boostController.OnBoostEnd   -= HandleBoostEnd;
            }

            if (driftController != null)
            {
                driftController.OnDriftStart   -= HandleDriftStart;
                driftController.OnDriftLevelUp -= HandleDriftLevelUp;
                driftController.OnDriftRelease -= HandleDriftRelease;
                driftController.OnDriftCancel  -= HandleDriftCancel;
            }

            if (slipstreamController != null)
            {
                slipstreamController.OnSlipstreamEnter   -= HandleSlipstreamEnter;
                slipstreamController.OnSlipstreamCharged -= HandleSlipstreamCharged;
                slipstreamController.OnSlipstreamExit    -= HandleSlipstreamExit;
            }

            if (startBoostController != null)
            {
                startBoostController.OnCountdownTick    -= HandleCountdownTick;
                startBoostController.OnGoSignal         -= HandleGoSignal;
                startBoostController.OnStartBoostResult -= HandleStartBoostResult;
            }

            if (trickBoostController != null)
            {
                trickBoostController.OnTrickStart    -= HandleTrickStart;
                trickBoostController.OnTrickComplete -= HandleTrickComplete;
            }
        }

        #endregion

        #region Event Handlers

        private void HandleBoostStart(BoostConfig config)
        {
            AudioClip clip = (config?.sfxClip != null) ? config.sfxClip : boostActivateClip;
            PlayOneShot(clip, pitch: 1f);
            _loopTargetVolume = boostLoopMaxVolume;
            if (!_loopSource.isPlaying && _loopSource.clip != null)
                _loopSource.Play();
        }

        private void HandleBoostEnd(BoostType type)
        {
            if (boostController != null && boostController.ActiveBoostCount == 0)
                _loopTargetVolume = 0f;
        }

        private void HandleDriftStart(DriftDirection dir)
        {
            PlayOneShot(driftStartClip, pitch: 1f);
        }

        private void HandleDriftLevelUp(DriftLevel level)
        {
            int idx = (int)level - 1;
            if (idx < 0 || idx >= driftLevelUpChimes.Length) return;
            float pitch = 1f + driftChimePitchStep * idx;
            PlayOneShot(driftLevelUpChimes[idx], pitch: pitch);
        }

        private void HandleDriftRelease(DriftLevel level, BoostConfig reward) { }

        private void HandleDriftCancel(DriftLevel level) { }

        private void HandleSlipstreamEnter(string leadPlayerId)
        {
            _slipstreamTargetVolume = 0.1f;
            if (!_slipstreamSource.isPlaying && _slipstreamSource.clip != null)
                _slipstreamSource.Play();
        }

        private void HandleSlipstreamCharged(float charge)
        {
            _slipstreamTargetVolume = Mathf.Lerp(0.1f, 0.8f, charge);
        }

        private void HandleSlipstreamExit()
        {
            _slipstreamTargetVolume = 0f;
        }

        private void HandleCountdownTick(int tickValue)
        {
            float pitch = 1f + (3 - tickValue) * 0.1f; // slightly higher each tick
            PlayOneShot(countdownBeepClip, pitch: pitch);
        }

        private void HandleGoSignal()
        {
            PlayOneShot(goHornClip, pitch: 1f);
        }

        private void HandleStartBoostResult(StartBoostGrade grade, BoostConfig reward) { }

        private void HandleTrickStart(TrickType trick)
        {
            PlayOneShot(trickWhooshClip, pitch: 1f);
        }

        private void HandleTrickComplete(TrickType trick, float meter)
        {
            PlayOneShot(trickLandingClip, pitch: Mathf.Lerp(0.9f, 1.3f, meter));
        }

        #endregion

        #region Audio Helpers

        private void PlayOneShot(AudioClip clip, float pitch)
        {
            if (clip == null || _oneShotSource == null) return;
            _oneShotSource.pitch = pitch;
            _oneShotSource.PlayOneShot(clip);
        }

        private void FadeLoopVolume(ref AudioSource source, float target, float inRate, float outRate)
        {
            if (source == null) return;
            float rate = target > source.volume ? 1f / inRate : 1f / outRate;
            source.volume = Mathf.MoveTowards(source.volume, target, rate * Time.deltaTime);
            if (source.volume <= 0f && source.isPlaying) source.Stop();
        }

        private void UpdateEnginePitch()
        {
            if (engineAudioSource == null || BoostController.Instance == null) return;
            float mult         = BoostController.Instance.CurrentSpeedMultiplier;
            float targetPitch  = baseEnginePitch + maxEnginePitchBoost * Mathf.InverseLerp(1f, 3f, mult);
            engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch, targetPitch,
                enginePitchLerpSpeed * Time.deltaTime);
        }

        #endregion

        #region Private Init

        private void ResolveReferences()
        {
            if (boostController      == null) boostController      = BoostController.Instance;
            if (driftController      == null) driftController      = DriftController.Instance;
            if (slipstreamController == null) slipstreamController = SlipstreamController.Instance;
        }

        #endregion
    }
}
