using System.Collections;
using UnityEngine;
using SWEF.Audio;

namespace SWEF.Narration
{
    /// <summary>
    /// Manages narration AudioSource playback and integrates with
    /// <see cref="AudioManager"/> for BGM ducking when narration is active.
    /// Attach to the same GameObject as <see cref="NarrationManager"/>.
    /// </summary>
    [RequireComponent(typeof(NarrationManager))]
    public class NarrationAudioController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("AudioSource")]
        [Tooltip("Dedicated AudioSource for narration. Created automatically if null.")]
        [SerializeField] private AudioSource narrationSource;

        // ── State ─────────────────────────────────────────────────────────────────
        private AudioManager _audioManager;
        private Coroutine    _duckCoroutine;
        private float        _originalBgmVolume = 1f;
        private bool         _isDucked;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (narrationSource == null)
            {
                narrationSource            = gameObject.AddComponent<AudioSource>();
                narrationSource.playOnAwake = false;
                narrationSource.loop        = false;
            }
        }

        private void Start()
        {
            _audioManager = AudioManager.Instance;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads and plays the narration clip at <paramref name="resourcePath"/> from Resources.
        /// Ducks the BGM if config requires it.
        /// </summary>
        public void PlayNarration(string resourcePath, float volume)
        {
            if (string.IsNullOrEmpty(resourcePath)) return;

            var clip = Resources.Load<AudioClip>(resourcePath);
            if (clip == null)
            {
                Debug.LogWarning($"[SWEF] NarrationAudioController: clip not found at '{resourcePath}'.");
                return;
            }

            narrationSource.clip   = clip;
            narrationSource.volume = Mathf.Clamp01(volume);
            narrationSource.Play();

            var mgr = NarrationManager.Instance;
            if (mgr != null && mgr.Config.duckMusicDuringNarration)
                StartDucking(mgr.Config.duckAmount);
        }

        /// <summary>Stop narration audio and restore BGM volume.</summary>
        public void Stop()
        {
            if (narrationSource.isPlaying)
                narrationSource.Stop();

            StopDucking();
        }

        /// <summary>Pause or resume narration audio playback.</summary>
        public void SetPaused(bool paused)
        {
            if (paused) narrationSource.Pause();
            else        narrationSource.UnPause();
        }

        /// <summary>Set the volume of the narration AudioSource.</summary>
        public void SetVolume(float volume)
        {
            narrationSource.volume = Mathf.Clamp01(volume);
        }

        /// <summary>True while the narration AudioSource is playing.</summary>
        public bool IsPlaying => narrationSource != null && narrationSource.isPlaying;

        // ── BGM ducking ───────────────────────────────────────────────────────────

        private void StartDucking(float duckAmount)
        {
            if (_audioManager == null) return;
            if (_isDucked) return;

            // Capture the current BGM volume from SettingsManager, falling back to 0.7.
            var settings = FindFirstObjectByType<SWEF.Settings.SettingsManager>();
            _originalBgmVolume = settings != null ? settings.MasterVolume : 0.7f;
            float targetVolume  = _originalBgmVolume * (1f - Mathf.Clamp01(duckAmount));

            if (_duckCoroutine != null) StopCoroutine(_duckCoroutine);
            _duckCoroutine = StartCoroutine(FadeBgm(_originalBgmVolume, targetVolume, 0.5f));
            _isDucked = true;
        }

        private void StopDucking()
        {
            if (!_isDucked || _audioManager == null) return;
            if (_duckCoroutine != null) StopCoroutine(_duckCoroutine);
            _duckCoroutine = StartCoroutine(FadeBgm(_originalBgmVolume * 0.5f, _originalBgmVolume, 0.8f));
            _isDucked = false;
        }

        private IEnumerator FadeBgm(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float v = Mathf.Lerp(from, to, elapsed / duration);
                _audioManager.SetBGMVolume(v);
                yield return null;
            }
            _audioManager.SetBGMVolume(to);
        }
    }
}
