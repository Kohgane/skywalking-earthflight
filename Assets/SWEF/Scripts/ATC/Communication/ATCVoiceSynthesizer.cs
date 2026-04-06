// ATCVoiceSynthesizer.cs — Phase 119: Advanced AI Traffic Control
// AI voice generation: multiple controller voices, accent variation, urgency modulation.
// Namespace: SWEF.ATC

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — AI voice synthesizer that selects controller voice profiles
    /// and applies urgency modulation to ATC speech output.
    /// </summary>
    public class ATCVoiceSynthesizer : MonoBehaviour
    {
        // ── Voice Profile ─────────────────────────────────────────────────────────

        /// <summary>A controller voice profile.</summary>
        [System.Serializable]
        public class VoiceProfile
        {
            public string profileId;
            public string displayName;
            public float pitch;       // 0.5 – 2.0
            public float rate;        // 0.5 – 2.0
            public string accent;     // "American", "British", "Australian" …
        }

        [Header("Voices")]
        [SerializeField] private List<VoiceProfile> voiceProfiles = new List<VoiceProfile>();

        [Header("Volume")]
        [SerializeField] [Range(0f, 1f)] private float voiceVolume = 0.8f;

        private readonly List<VoiceProfile> _defaultProfiles = new List<VoiceProfile>
        {
            new VoiceProfile { profileId = "V1", displayName = "Tower Alpha",   pitch = 1.0f, rate = 1.0f, accent = "American" },
            new VoiceProfile { profileId = "V2", displayName = "Approach Bravo", pitch = 0.9f, rate = 1.1f, accent = "American" },
            new VoiceProfile { profileId = "V3", displayName = "Center Charlie", pitch = 1.1f, rate = 0.95f, accent = "British" }
        };

        private void Awake()
        {
            if (voiceProfiles.Count == 0)
                voiceProfiles.AddRange(_defaultProfiles);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Speaks ATC text using the given voice profile, scaled by urgency (0–1).
        /// In the editor / non-TTS builds this logs the phrase.
        /// </summary>
        public void Speak(string profileId, string text, float urgency = 0f)
        {
            var profile = GetProfile(profileId) ?? voiceProfiles[0];
            float effectivePitch = profile.pitch + urgency * 0.3f;
            float effectiveRate  = profile.rate  + urgency * 0.2f;

            // Platform TTS would be invoked here; we log for simulation purposes.
            Debug.Log($"[ATC Voice | {profile.displayName} | P:{effectivePitch:F2} R:{effectiveRate:F2}] {text}");
        }

        /// <summary>Returns the voice profile with the given ID, or null.</summary>
        public VoiceProfile GetProfile(string profileId)
            => voiceProfiles.Find(v => v.profileId == profileId);

        /// <summary>Number of registered voice profiles.</summary>
        public int ProfileCount => voiceProfiles.Count;

        /// <summary>Current voice volume (0–1).</summary>
        public float Volume
        {
            get => voiceVolume;
            set => voiceVolume = Mathf.Clamp01(value);
        }
    }
}
