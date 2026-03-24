// Phase 73 — Flight Formation Display & Airshow System
// Assets/SWEF/Scripts/Airshow/AirshowRoutineData.cs
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Airshow
{
    /// <summary>A single timed step within an act, describing one maneuver for one performer.</summary>
    [System.Serializable]
    public class ManeuverStep
    {
        public ManeuverType type;
        public float startTimeOffset;       // seconds from act start
        public float duration;              // seconds
        public Vector3 relativePosition;    // relative to venue center
        public Vector3 targetDirection;
        public SmokeColor smokeColor;
        public bool smokeEnabled;
        public int assignedSlot;            // which performer (0 = lead)
        public string annotation;           // designer notes
    }

    /// <summary>An ordered sequence of maneuvers comprising one act of a routine.</summary>
    [System.Serializable]
    public class ManeuverSequence
    {
        public string actName;
        public List<ManeuverStep> steps = new List<ManeuverStep>();
    }

    /// <summary>
    /// ScriptableObject defining a complete choreographed airshow routine.
    /// Create via <c>SWEF/Airshow/AirshowRoutineData</c> asset menu.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/Airshow/AirshowRoutineData", fileName = "NewAirshowRoutine")]
    public class AirshowRoutineData : ScriptableObject
    {
        /// <summary>Unique identifier for this routine.</summary>
        public string routineId;

        /// <summary>Display name (localization key).</summary>
        public string routineName;

        /// <summary>Show type classification.</summary>
        public AirshowType showType;

        /// <summary>Minimum number of performers required.</summary>
        public int requiredPerformers = 1;

        /// <summary>Maximum number of performers allowed.</summary>
        public int maxPerformers = 4;

        /// <summary>Estimated show duration in minutes.</summary>
        public float estimatedDurationMinutes = 5f;

        /// <summary>Difficulty rating from 1 (easiest) to 5 (hardest).</summary>
        [Range(1, 5)]
        public int difficulty = 1;

        /// <summary>Ordered list of acts; each act contains a list of maneuver steps.</summary>
        public List<ManeuverSequence> acts = new List<ManeuverSequence>();

        /// <summary>World position of the airshow venue center.</summary>
        public Vector3 venueCenter;

        /// <summary>Base altitude (metres) for the show.</summary>
        public float venueAltitude = 300f;

        /// <summary>Achievement or rank prerequisites required to unlock this routine.</summary>
        public string[] unlockRequirements = System.Array.Empty<string>();

        // ── Factory ─────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a default <see cref="AirshowRoutineData"/> asset with a basic 3-act routine.
        /// </summary>
        public static AirshowRoutineData CreateDefault()
        {
            var data = CreateInstance<AirshowRoutineData>();
            data.routineId = "default_routine";
            data.routineName = "airshow_default_routine";
            data.showType = AirshowType.FormationDisplay;
            data.requiredPerformers = 2;
            data.maxPerformers = 4;
            data.estimatedDurationMinutes = 5f;
            data.difficulty = 2;
            data.venueCenter = Vector3.zero;
            data.venueAltitude = 300f;

            // Act 1 — Opening Pass
            var act1 = new ManeuverSequence { actName = "Opening Pass" };
            act1.steps.Add(new ManeuverStep
            {
                type = ManeuverType.StraightAndLevel,
                startTimeOffset = 0f, duration = 10f,
                relativePosition = new Vector3(-500f, 0f, 0f),
                targetDirection = Vector3.right,
                smokeEnabled = true, smokeColor = SmokeColor.White, assignedSlot = 0
            });
            act1.steps.Add(new ManeuverStep
            {
                type = ManeuverType.StraightAndLevel,
                startTimeOffset = 0f, duration = 10f,
                relativePosition = new Vector3(-500f, 0f, 50f),
                targetDirection = Vector3.right,
                smokeEnabled = true, smokeColor = SmokeColor.Red, assignedSlot = 1
            });

            // Act 2 — Formation Maneuvers
            var act2 = new ManeuverSequence { actName = "Formation Maneuvers" };
            act2.steps.Add(new ManeuverStep
            {
                type = ManeuverType.BarrelRoll,
                startTimeOffset = 5f, duration = 8f,
                relativePosition = new Vector3(0f, 50f, 0f),
                targetDirection = Vector3.forward,
                smokeEnabled = true, smokeColor = SmokeColor.Blue, assignedSlot = 0
            });
            act2.steps.Add(new ManeuverStep
            {
                type = ManeuverType.DiamondRoll,
                startTimeOffset = 5f, duration = 8f,
                relativePosition = new Vector3(0f, 50f, 0f),
                targetDirection = Vector3.forward,
                smokeEnabled = true, smokeColor = SmokeColor.Red, assignedSlot = 1
            });

            // Act 3 — Finale
            var act3 = new ManeuverSequence { actName = "Finale" };
            act3.steps.Add(new ManeuverStep
            {
                type = ManeuverType.BombBurst,
                startTimeOffset = 0f, duration = 15f,
                relativePosition = Vector3.zero,
                targetDirection = Vector3.up,
                smokeEnabled = true, smokeColor = SmokeColor.Custom, assignedSlot = 0
            });

            data.acts = new List<ManeuverSequence> { act1, act2, act3 };
            return data;
        }
    }
}
