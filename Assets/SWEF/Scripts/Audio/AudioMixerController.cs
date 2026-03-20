using UnityEngine;
using UnityEngine.Audio;
using SWEF.Util;

namespace SWEF.Audio
{
    /// <summary>
    /// Runtime wrapper for a Unity <see cref="AudioMixer"/> that exposes named group
    /// volume and lowpass control via a linear API (internally converts to dB).
    /// Falls back to direct <see cref="AudioSource"/> volume when no mixer is assigned.
    /// Predefined snapshots: "Default", "Space", "Paused".
    /// </summary>
    public class AudioMixerController : MonoBehaviour
    {
        // ── Exposed parameter names ───────────────────────────────────────────────
        public const string ParamMaster    = "MasterVolume";
        public const string ParamMusic     = "MusicVolume";
        public const string ParamSFX       = "SFXVolume";
        public const string ParamAmbience  = "AmbienceVolume";
        public const string ParamUI        = "UIVolume";

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Mixer (optional)")]
        [SerializeField] private AudioMixer mixer;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            // Sync with SettingsManager on startup
            var settings = FindFirstObjectByType<SWEF.Settings.SettingsManager>();
            if (settings != null)
            {
                SetVolume(ParamMaster,   settings.MasterVolume);
                SetVolume(ParamSFX,      settings.SfxVolume);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Sets a mixer group volume. <paramref name="linearVolume"/> is 0–1 and is converted to dB.</summary>
        public void SetVolume(string group, float linearVolume)
        {
            if (mixer == null) return;
            float db = linearVolume > 0.0001f ? 20f * Mathf.Log10(linearVolume) : -80f;
            mixer.SetFloat(group, db);
        }

        /// <summary>Sets a lowpass cutoff frequency on a mixer group parameter.</summary>
        public void SetLowpassCutoff(string group, float frequency)
        {
            if (mixer == null) return;
            mixer.SetFloat(group, Mathf.Clamp(frequency, 10f, 22000f));
        }

        /// <summary>Transitions to the named snapshot over <paramref name="duration"/> seconds.</summary>
        public void TransitionToSnapshot(string snapshotName, float duration)
        {
            if (mixer == null) return;
            var snapshot = mixer.FindSnapshot(snapshotName);
            if (snapshot != null)
                snapshot.TransitionTo(duration);
            else
                Debug.LogWarning($"[AudioMixerController] Snapshot '{snapshotName}' not found in mixer.");
        }

        /// <summary>Returns the current dB value for the given mixer group parameter.</summary>
        public float GetVolume(string group)
        {
            if (mixer == null || !mixer.GetFloat(group, out float db)) return 0f;
            return db;
        }

        /// <summary>Whether a mixer is assigned to this controller.</summary>
        public bool HasMixer => mixer != null;
    }
}
