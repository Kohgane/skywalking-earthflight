// Phase 100 — AI Co-Pilot & Smart Assistant
// Assets/SWEF/Scripts/AICoPilot/AICoPilotSettings.cs
using UnityEngine;

namespace SWEF.AICoPilot
{
    /// <summary>
    /// Controls how much the AI co-pilot speaks during flight.
    /// </summary>
    public enum VerbosityLevel
    {
        /// <summary>Only critical and emergency messages.</summary>
        Minimal,

        /// <summary>Balanced mix of advisories and navigation callouts.</summary>
        Normal,

        /// <summary>All messages including idle chatter and detailed callouts.</summary>
        Chatty
    }

    /// <summary>
    /// Runtime settings for the AI Co-Pilot. Persisted to <see cref="PlayerPrefs"/>
    /// so user preferences survive between sessions.
    /// </summary>
    [DefaultExecutionOrder(-70)]
    [DisallowMultipleComponent]
    public class AICoPilotSettings : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static AICoPilotSettings Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        #endregion

        #region PlayerPrefs Keys (private constants)

        private const string KeyAssistanceLevel        = "ARIA_AssistanceLevel";
        private const string KeyPersonality            = "ARIA_Personality";
        private const string KeyVerbosity              = "ARIA_Verbosity";
        private const string KeyNavigationEnabled      = "ARIA_NavEnabled";
        private const string KeyFlightEnabled          = "ARIA_FlightEnabled";
        private const string KeyEmergencyEnabled       = "ARIA_EmergencyEnabled";
        private const string KeyIdleChatterEnabled     = "ARIA_IdleChatter";
        private const string KeyVoiceVolume            = "ARIA_VoiceVolume";
        private const string KeyMsgDurationMultiplier  = "ARIA_MsgDuration";

        #endregion

        #region Settings Properties

        /// <summary>Controls how actively ARIA assists during flight.</summary>
        public AssistanceLevel AssistanceLevel
        {
            get => _assistanceLevel;
            set { _assistanceLevel = value; Save(); }
        }
        private AssistanceLevel _assistanceLevel = AssistanceLevel.Active;

        /// <summary>AI dialogue personality profile.</summary>
        public CoPilotPersonality Personality
        {
            get => _personality;
            set { _personality = value; Save(); }
        }
        private CoPilotPersonality _personality = CoPilotPersonality.Professional;

        /// <summary>Controls how frequently ARIA speaks.</summary>
        public VerbosityLevel Verbosity
        {
            get => _verbosity;
            set { _verbosity = value; Save(); }
        }
        private VerbosityLevel _verbosity = VerbosityLevel.Normal;

        /// <summary>Whether navigation callouts are enabled.</summary>
        public bool NavigationEnabled
        {
            get => _navigationEnabled;
            set { _navigationEnabled = value; }
        }
        private bool _navigationEnabled = true;

        /// <summary>Whether flight parameter advisories are enabled.</summary>
        public bool FlightAdvisoryEnabled
        {
            get => _flightEnabled;
            set { _flightEnabled = value; }
        }
        private bool _flightEnabled = true;

        /// <summary>Whether emergency advisories are enabled (always recommended ON).</summary>
        public bool EmergencyAdvisoryEnabled
        {
            get => _emergencyEnabled;
            set { _emergencyEnabled = value; }
        }
        private bool _emergencyEnabled = true;

        /// <summary>Whether idle chatter is enabled during quiet flight segments.</summary>
        public bool IdleChatterEnabled
        {
            get => _idleChatterEnabled;
            set { _idleChatterEnabled = value; }
        }
        private bool _idleChatterEnabled = true;

        /// <summary>Voice volume for future TTS integration (0–1).</summary>
        public float VoiceVolume
        {
            get => _voiceVolume;
            set { _voiceVolume = Mathf.Clamp01(value); }
        }
        private float _voiceVolume = 0.85f;

        /// <summary>
        /// Multiplier applied to message display duration.
        /// Values &lt; 1 shorten messages; values &gt; 1 lengthen them.
        /// </summary>
        public float MessageDurationMultiplier
        {
            get => _msgDurationMultiplier;
            set { _msgDurationMultiplier = Mathf.Clamp(value, 0.5f, 3f); }
        }
        private float _msgDurationMultiplier = 1f;

        #endregion

        #region Persistence

        /// <summary>Saves all settings to <see cref="PlayerPrefs"/>.</summary>
        public void Save()
        {
            PlayerPrefs.SetInt(KeyAssistanceLevel,       (int)_assistanceLevel);
            PlayerPrefs.SetInt(KeyPersonality,           (int)_personality);
            PlayerPrefs.SetInt(KeyVerbosity,             (int)_verbosity);
            PlayerPrefs.SetInt(KeyNavigationEnabled,     _navigationEnabled  ? 1 : 0);
            PlayerPrefs.SetInt(KeyFlightEnabled,         _flightEnabled      ? 1 : 0);
            PlayerPrefs.SetInt(KeyEmergencyEnabled,      _emergencyEnabled   ? 1 : 0);
            PlayerPrefs.SetInt(KeyIdleChatterEnabled,    _idleChatterEnabled ? 1 : 0);
            PlayerPrefs.SetFloat(KeyVoiceVolume,         _voiceVolume);
            PlayerPrefs.SetFloat(KeyMsgDurationMultiplier, _msgDurationMultiplier);
            PlayerPrefs.Save();
        }

        /// <summary>Loads all settings from <see cref="PlayerPrefs"/>, applying defaults for missing keys.</summary>
        public void Load()
        {
            _assistanceLevel    = (AssistanceLevel)PlayerPrefs.GetInt(KeyAssistanceLevel, (int)AssistanceLevel.Active);
            _personality        = (CoPilotPersonality)PlayerPrefs.GetInt(KeyPersonality,  (int)CoPilotPersonality.Professional);
            _verbosity          = (VerbosityLevel)PlayerPrefs.GetInt(KeyVerbosity,        (int)VerbosityLevel.Normal);
            _navigationEnabled  = PlayerPrefs.GetInt(KeyNavigationEnabled,  1) == 1;
            _flightEnabled      = PlayerPrefs.GetInt(KeyFlightEnabled,       1) == 1;
            _emergencyEnabled   = PlayerPrefs.GetInt(KeyEmergencyEnabled,    1) == 1;
            _idleChatterEnabled = PlayerPrefs.GetInt(KeyIdleChatterEnabled,  1) == 1;
            _voiceVolume        = PlayerPrefs.GetFloat(KeyVoiceVolume,              0.85f);
            _msgDurationMultiplier = PlayerPrefs.GetFloat(KeyMsgDurationMultiplier, 1f);
        }

        /// <summary>Resets all settings to their defaults and saves.</summary>
        public void ResetToDefaults()
        {
            _assistanceLevel    = AssistanceLevel.Active;
            _personality        = CoPilotPersonality.Professional;
            _verbosity          = VerbosityLevel.Normal;
            _navigationEnabled  = true;
            _flightEnabled      = true;
            _emergencyEnabled   = true;
            _idleChatterEnabled = true;
            _voiceVolume        = 0.85f;
            _msgDurationMultiplier = 1f;
            Save();
        }

        #endregion
    }
}
