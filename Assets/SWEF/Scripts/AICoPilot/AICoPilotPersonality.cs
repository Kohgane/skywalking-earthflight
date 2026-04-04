// Phase 100 — AI Co-Pilot & Smart Assistant
// Assets/SWEF/Scripts/AICoPilot/AICoPilotPersonality.cs
using System;
using UnityEngine;

namespace SWEF.AICoPilot
{
    /// <summary>
    /// Defines the available dialogue personalities for ARIA.
    /// </summary>
    public enum CoPilotPersonality
    {
        /// <summary>Formal, concise, aviation-standard phraseology.</summary>
        Professional,

        /// <summary>Warm, conversational, and encouraging.</summary>
        Friendly,

        /// <summary>Terse, disciplined, NATO-style brevity codes.</summary>
        Military,

        /// <summary>Light-hearted with occasional wit — never in emergencies.</summary>
        Humorous
    }

    /// <summary>
    /// Serialisable profile data for a single AI co-pilot personality.
    /// Drives how ARIA phrases advisories, greetings, and idle chatter.
    /// </summary>
    [Serializable]
    public class CoPilotPersonalityProfile
    {
        /// <summary>Personality type this profile represents.</summary>
        public CoPilotPersonality Personality;

        /// <summary>Display name for the AI when using this profile (default: "ARIA").</summary>
        public string DisplayName = "ARIA";

        /// <summary>Full expansion of the AI name acronym.</summary>
        public string DisplaySubtitle = "Aerial Intelligence & Routing Assistant";

        /// <summary>Phrases used when first greeting the pilot at session start.</summary>
        public string[] GreetingPhrases;

        /// <summary>Prefix applied to caution-level advisory text.</summary>
        public string CautionPrefix;

        /// <summary>Prefix applied to warning-level advisory text.</summary>
        public string WarningPrefix;

        /// <summary>Prefix applied to critical/emergency advisory text.</summary>
        public string CriticalPrefix;

        /// <summary>Phrases used to encourage the pilot after a successful manoeuvre.</summary>
        public string[] EncouragementPhrases;

        /// <summary>Phrases used during quiet moments (idle chatter).</summary>
        public string[] IdleChatterPhrases;
    }

    /// <summary>
    /// ScriptableObject that holds all <see cref="CoPilotPersonalityProfile"/> definitions
    /// and exposes the currently active profile.
    /// </summary>
    [CreateAssetMenu(fileName = "AICoPilotPersonality", menuName = "SWEF/AICoPilot/Personality Asset")]
    public class AICoPilotPersonality : ScriptableObject
    {
        #region Inspector

        [Tooltip("All available personality profiles.")]
        [SerializeField] private CoPilotPersonalityProfile[] _profiles;

        [Tooltip("Active personality. Can be overridden from AICoPilotSettings at runtime.")]
        [SerializeField] private CoPilotPersonality _activePersonality = CoPilotPersonality.Professional;

        #endregion

        #region Built-in Defaults

        /// <summary>
        /// Returns a built-in default personality library — used when no asset is assigned.
        /// </summary>
        public static AICoPilotPersonality CreateDefault()
        {
            var asset = CreateInstance<AICoPilotPersonality>();
            asset._profiles = BuildDefaultProfiles();
            return asset;
        }

        private static CoPilotPersonalityProfile[] BuildDefaultProfiles()
        {
            return new[]
            {
                new CoPilotPersonalityProfile
                {
                    Personality     = CoPilotPersonality.Professional,
                    DisplayName     = "ARIA",
                    DisplaySubtitle = "Aerial Intelligence & Routing Assistant",
                    GreetingPhrases = new[] { "ARIA online. All systems nominal.", "Co-pilot systems ready.", "ARIA standing by." },
                    CautionPrefix   = "CAUTION —",
                    WarningPrefix   = "WARNING —",
                    CriticalPrefix  = "CRITICAL —",
                    EncouragementPhrases = new[] { "Good correction.", "Smooth handling.", "Procedure complete." },
                    IdleChatterPhrases   = new[] { "Cruising altitude steady.", "Flight parameters nominal.", "On course." }
                },
                new CoPilotPersonalityProfile
                {
                    Personality     = CoPilotPersonality.Friendly,
                    DisplayName     = "ARIA",
                    DisplaySubtitle = "Aerial Intelligence & Routing Assistant",
                    GreetingPhrases = new[] { "Hey there! ARIA ready to help!", "Good to have you in the cockpit!", "ARIA here — let's fly!" },
                    CautionPrefix   = "Heads up —",
                    WarningPrefix   = "Watch out —",
                    CriticalPrefix  = "Emergency! —",
                    EncouragementPhrases = new[] { "Nice flying!", "You've got this!", "Great work up there!" },
                    IdleChatterPhrases   = new[] { "What a view!", "Beautiful skies today.", "I love this altitude." }
                },
                new CoPilotPersonalityProfile
                {
                    Personality     = CoPilotPersonality.Military,
                    DisplayName     = "ARIA",
                    DisplaySubtitle = "Aerial Intelligence & Routing Assistant",
                    GreetingPhrases = new[] { "ARIA online. Mission ready.", "Systems green. Awaiting orders.", "Co-pilot ready. All clear." },
                    CautionPrefix   = "ADVISORY —",
                    WarningPrefix   = "ALERT —",
                    CriticalPrefix  = "MAYDAY —",
                    EncouragementPhrases = new[] { "Copy, good work.", "Acknowledged.", "Manoeuvre successful." },
                    IdleChatterPhrases   = new[] { "Sector clear.", "No threats detected.", "All systems nominal." }
                },
                new CoPilotPersonalityProfile
                {
                    Personality     = CoPilotPersonality.Humorous,
                    DisplayName     = "ARIA",
                    DisplaySubtitle = "Aerial Intelligence & Routing Assistant",
                    GreetingPhrases = new[] { "ARIA online! Try not to crash this time!", "Your friendly neighbourhood AI co-pilot reporting!", "Ready to fly — no manual needed!" },
                    CautionPrefix   = "Uh oh —",
                    WarningPrefix   = "Yikes —",
                    CriticalPrefix  = "Oh no no no —",
                    EncouragementPhrases = new[] { "Not bad for a human!", "I'll pretend that was intentional.", "The passengers didn't notice. Probably." },
                    IdleChatterPhrases   = new[] { "Just a casual 30,000 feet up.", "Do you think the clouds get lonely?", "I'd rate this turbulence 2 out of 10." }
                }
            };
        }

        #endregion

        #region Public API

        /// <summary>Returns the profile for the given <paramref name="personality"/>.</summary>
        /// <param name="personality">Requested personality type.</param>
        /// <returns>
        /// The matching <see cref="CoPilotPersonalityProfile"/>, or the first profile if not found.
        /// </returns>
        public CoPilotPersonalityProfile GetProfile(CoPilotPersonality personality)
        {
            if (_profiles == null || _profiles.Length == 0) return null;
            foreach (var p in _profiles)
            {
                if (p.Personality == personality) return p;
            }
            return _profiles[0];
        }

        /// <summary>Returns the currently active profile.</summary>
        public CoPilotPersonalityProfile ActiveProfile => GetProfile(_activePersonality);

        /// <summary>Changes the active personality at runtime.</summary>
        /// <param name="personality">New personality to activate.</param>
        public void SetActivePersonality(CoPilotPersonality personality)
        {
            _activePersonality = personality;
        }

        #endregion
    }
}
