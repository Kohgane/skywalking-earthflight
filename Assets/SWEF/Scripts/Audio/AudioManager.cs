using UnityEngine;
using SWEF.Settings;

namespace SWEF.Audio
{
    /// <summary>
    /// Singleton audio manager. Survives scene loads via DontDestroyOnLoad.
    /// Manages one BGM AudioSource (looping) and one SFX AudioSource.
    /// SFX index convention: 0=ButtonClick, 1=Teleport, 2=Screenshot,
    /// 3=FavoriteSave, 4=AltitudeWarning.
    /// Responds to SettingsManager volume changes automatically.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static AudioManager Instance { get; private set; }

        // ── AudioSources ─────────────────────────────────────────────────────────
        [Header("Sources (auto-created if null)")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        // ── Clips ─────────────────────────────────────────────────────────────────
        [Header("Clips")]
        [SerializeField] private AudioClip   bgmClip;
        /// <summary>
        /// SFX clips indexed by convention:
        /// 0=ButtonClick, 1=Teleport, 2=Screenshot, 3=FavoriteSave, 4=AltitudeWarning.
        /// Assign in Inspector. Null entries are safely skipped.
        /// </summary>
        [SerializeField] private AudioClip[] sfxClips = new AudioClip[5];

        [Header("Ref (optional)")]
        [SerializeField] private SettingsManager settingsManager;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureSources();

            if (settingsManager == null)
                settingsManager = FindFirstObjectByType<SettingsManager>();
        }

        private void Start()
        {
            if (settingsManager != null)
            {
                ApplyVolumes();
                settingsManager.OnSettingsChanged += ApplyVolumes;
            }

            PlayBGM();
        }

        private void OnDestroy()
        {
            if (settingsManager != null)
                settingsManager.OnSettingsChanged -= ApplyVolumes;
        }

        // ── BGM ───────────────────────────────────────────────────────────────────
        /// <summary>Plays the BGM clip from the beginning (or resumes if already set).</summary>
        public void PlayBGM()
        {
            if (bgmSource == null || bgmClip == null) return;
            if (bgmSource.isPlaying) return;
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        /// <summary>Stops BGM playback.</summary>
        public void StopBGM()
        {
            if (bgmSource != null) bgmSource.Stop();
        }

        /// <summary>Sets BGM volume (0–1).</summary>
        public void SetBGMVolume(float volume)
        {
            if (bgmSource != null) bgmSource.volume = Mathf.Clamp01(volume);
        }

        // ── SFX ───────────────────────────────────────────────────────────────────
        /// <summary>Plays the SFX clip at the given index (0–4). Silently skipped if out of range or null.</summary>
        public void PlaySFX(int index)
        {
            if (sfxSource == null) return;
            if (index < 0 || index >= sfxClips.Length) return;
            AudioClip clip = sfxClips[index];
            if (clip == null) return;
            sfxSource.PlayOneShot(clip);
        }

        /// <summary>Sets SFX volume (0–1).</summary>
        public void SetSFXVolume(float volume)
        {
            if (sfxSource != null) sfxSource.volume = Mathf.Clamp01(volume);
        }

        // ── Internal ─────────────────────────────────────────────────────────────
        private void EnsureSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.playOnAwake = false;
                bgmSource.loop        = true;
            }
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.loop        = false;
            }
        }

        private void ApplyVolumes()
        {
            if (settingsManager == null) return;
            float master = settingsManager.MasterVolume;
            SetBGMVolume(master);
            SetSFXVolume(settingsManager.SfxVolume * master);
        }
    }
}
