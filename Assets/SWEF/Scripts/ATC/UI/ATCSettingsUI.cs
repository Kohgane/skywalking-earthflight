// ATCSettingsUI.cs — Phase 119: Advanced AI Traffic Control
// ATC settings: AI difficulty, communication speed, realism level, voice volume.
// Namespace: SWEF.ATC

using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Persists and applies ATC settings: AI difficulty, communication
    /// speed, realism and voice volume.
    /// </summary>
    public class ATCSettingsUI : MonoBehaviour
    {
        // ── PlayerPrefs Keys ──────────────────────────────────────────────────────

        private const string KEY_DIFFICULTY   = "ATC_Difficulty";
        private const string KEY_COMM_SPEED   = "ATC_CommSpeed";
        private const string KEY_REALISM      = "ATC_Realism";
        private const string KEY_VOICE_VOL    = "ATC_VoiceVolume";

        // ── Cached Values ─────────────────────────────────────────────────────────

        private float _difficulty;
        private float _commSpeed;
        private float _realism;
        private float _voiceVolume;

        // ── Properties ────────────────────────────────────────────────────────────

        /// <summary>AI difficulty (0 = arcade, 1 = realistic).</summary>
        public float Difficulty
        {
            get => _difficulty;
            set { _difficulty = Mathf.Clamp01(value); PlayerPrefs.SetFloat(KEY_DIFFICULTY, _difficulty); }
        }

        /// <summary>Communication speed multiplier (0.5 = slow, 2 = fast).</summary>
        public float CommunicationSpeed
        {
            get => _commSpeed;
            set { _commSpeed = Mathf.Clamp(value, 0.5f, 2f); PlayerPrefs.SetFloat(KEY_COMM_SPEED, _commSpeed); }
        }

        /// <summary>Realism level (0 = simplified, 1 = fully realistic).</summary>
        public float RealismLevel
        {
            get => _realism;
            set { _realism = Mathf.Clamp01(value); PlayerPrefs.SetFloat(KEY_REALISM, _realism); }
        }

        /// <summary>ATC voice volume (0–1).</summary>
        public float VoiceVolume
        {
            get => _voiceVolume;
            set { _voiceVolume = Mathf.Clamp01(value); PlayerPrefs.SetFloat(KEY_VOICE_VOL, _voiceVolume); }
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            Load();
        }

        /// <summary>Loads settings from PlayerPrefs.</summary>
        public void Load()
        {
            _difficulty  = PlayerPrefs.GetFloat(KEY_DIFFICULTY,  0.8f);
            _commSpeed   = PlayerPrefs.GetFloat(KEY_COMM_SPEED,  1.0f);
            _realism     = PlayerPrefs.GetFloat(KEY_REALISM,     0.8f);
            _voiceVolume = PlayerPrefs.GetFloat(KEY_VOICE_VOL,   0.8f);
        }

        /// <summary>Resets all settings to defaults.</summary>
        public void ResetToDefaults()
        {
            Difficulty         = 0.8f;
            CommunicationSpeed = 1.0f;
            RealismLevel       = 0.8f;
            VoiceVolume        = 0.8f;
            PlayerPrefs.Save();
        }
    }
}
