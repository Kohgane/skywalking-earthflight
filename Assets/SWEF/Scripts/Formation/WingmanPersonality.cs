// WingmanPersonality.cs — SWEF Flight Formation & Wingman AI System (Phase 63)
using UnityEngine;

namespace SWEF.Formation
{
    /// <summary>
    /// ScriptableObject that describes a wingman's personality traits.
    /// <para>
    /// Traits directly influence the runtime behaviour of
    /// <see cref="WingmanAI"/>: aggression drives break-off thresholds,
    /// discipline tightens formation tolerance, and skill level scales
    /// flight precision and reaction time.
    /// </para>
    /// <para>
    /// Create assets via <em>Assets → Create → SWEF → Formation → Wingman Personality</em>.
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        menuName = "SWEF/Formation/Wingman Personality",
        fileName = "WP_New",
        order    = 1)]
    public sealed class WingmanPersonality : ScriptableObject
    {
        #region Inspector — Identity

        [Header("Identity")]
        [Tooltip("Full display name of this wingman (e.g. \"Alex Mercer\").")]
        [SerializeField] private string wingmanName = "Wingman";

        [Tooltip("Radio call-sign used in comms (e.g. \"Eagle-2\", \"Ghost-1\").")]
        [SerializeField] private string callSign = "Eagle-2";

        [Tooltip("Portrait sprite shown in the radio-comms HUD panel.")]
        [SerializeField] private Sprite portrait;

        #endregion

        #region Inspector — Traits

        [Header("Personality Traits")]
        [Tooltip("How eagerly this wingman breaks formation to attack a target. " +
                 "0 = very passive, 1 = immediately engages on any threat.")]
        [Range(0f, 1f)]
        [SerializeField] private float aggressiveness = 0.5f;

        [Tooltip("How tightly this wingman holds its assigned formation slot. " +
                 "0 = loose / casual, 1 = drill-perfect.")]
        [Range(0f, 1f)]
        [SerializeField] private float discipline = 0.7f;

        [Tooltip("Flight precision and reaction time. " +
                 "0 = rookie, 1 = ace pilot.")]
        [Range(0f, 1f)]
        [SerializeField] private float skillLevel = 0.6f;

        #endregion

        #region Inspector — Presentation

        [Header("Presentation")]
        [Tooltip("Radio voice-line audio clips for this wingman. " +
                 "Played via WingmanRadioComms when the wingman responds to a command.")]
        [SerializeField] private AudioClip[] voiceLines = System.Array.Empty<AudioClip>();

        [Tooltip("Unique engine-trail colour used to distinguish this wingman in-flight.")]
        [SerializeField] private Color trailColor = Color.cyan;

        #endregion

        #region Public Properties

        /// <summary>Full display name of this wingman.</summary>
        public string WingmanName => wingmanName;

        /// <summary>Radio call-sign (e.g. "Eagle-2").</summary>
        public string CallSign => callSign;

        /// <summary>Portrait sprite used in the HUD comms panel.</summary>
        public Sprite Portrait => portrait;

        /// <summary>Aggression level (0–1). Higher values cause earlier attack breaks.</summary>
        public float Aggressiveness => aggressiveness;

        /// <summary>Discipline level (0–1). Higher values yield tighter formation holding.</summary>
        public float Discipline => discipline;

        /// <summary>Skill level (0–1). Scales flight precision and reaction time.</summary>
        public float SkillLevel => skillLevel;

        /// <summary>Array of radio voice-line clips.</summary>
        public AudioClip[] VoiceLines => voiceLines;

        /// <summary>Unique trail colour for this wingman.</summary>
        public Color TrailColor => trailColor;

        #endregion

        #region Derived Helpers

        /// <summary>
        /// Effective formation tolerance in metres derived from
        /// <see cref="Discipline"/> and the base tolerance of
        /// <see cref="WingmanAI.formationTolerance"/>.
        /// </summary>
        /// <param name="baseTolerance">
        /// The unmodified <see cref="WingmanAI.formationTolerance"/> value.
        /// </param>
        /// <returns>
        /// Tolerance scaled inversely by discipline: a fully disciplined wingman
        /// uses half the base tolerance; a fully undisciplined one uses double.
        /// </returns>
        public float GetEffectiveTolerance(float baseTolerance)
        {
            // discipline 1 → 0.5× tolerance; discipline 0 → 2× tolerance
            float multiplier = Mathf.Lerp(2f, 0.5f, discipline);
            return baseTolerance * multiplier;
        }

        /// <summary>
        /// Returns a random voice-line clip, or <see langword="null"/> when
        /// the <see cref="VoiceLines"/> array is empty.
        /// </summary>
        public AudioClip GetRandomVoiceLine()
        {
            if (voiceLines == null || voiceLines.Length == 0)
                return null;
            return voiceLines[Random.Range(0, voiceLines.Length)];
        }

        #endregion

        #region Unity Lifecycle

        private void OnValidate()
        {
            aggressiveness = Mathf.Clamp01(aggressiveness);
            discipline     = Mathf.Clamp01(discipline);
            skillLevel     = Mathf.Clamp01(skillLevel);
        }

        #endregion
    }
}
