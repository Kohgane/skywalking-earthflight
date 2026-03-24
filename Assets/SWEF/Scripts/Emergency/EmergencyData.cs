using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Emergency
{
    // ─── Enumerations ────────────────────────────────────────────────────────────

    /// <summary>Category of in-flight emergency that can be triggered.</summary>
    public enum EmergencyType
    {
        EngineFailure,
        DualEngineFailure,
        FuelStarvation,
        FuelLeak,
        BirdStrike,
        StructuralDamage,
        IcingCritical,
        ElectricalFailure,
        HydraulicFailure,
        FireOnboard,
        Depressurization,
        NavigationFailure,
        CommunicationFailure,
        ControlSurfaceJam,
        LandingGearMalfunction
    }

    /// <summary>Urgency level of an active emergency.</summary>
    public enum EmergencySeverity
    {
        Caution,
        Warning,
        Emergency,
        Mayday
    }

    /// <summary>Lifecycle phase of an active emergency.</summary>
    public enum EmergencyPhase
    {
        Detected,
        Acknowledged,
        ChecklistActive,
        ExecutingProcedure,
        Diverting,
        OnApproach,
        Landed,
        Crashed,
        Rescued,
        Resolved
    }

    /// <summary>Type of distress call made by the crew.</summary>
    public enum DistressCallType
    {
        None,
        PanPan,
        Mayday,
        Squawk7700,
        Squawk7600,
        Squawk7500
    }

    /// <summary>Category of rescue unit dispatched to an incident site.</summary>
    public enum RescueUnitType
    {
        FireTruck,
        Ambulance,
        Helicopter,
        CoastGuard,
        MountainRescue,
        MilitaryJet
    }

    // ─── Data Classes ─────────────────────────────────────────────────────────────

    /// <summary>Design-time definition of a single emergency scenario.</summary>
    [Serializable]
    public class EmergencyScenario
    {
        [Tooltip("Unique identifier, e.g. \"engine_failure\".")]
        public string scenarioId = string.Empty;

        [Tooltip("Localization key for display name.")]
        public string displayNameKey = string.Empty;

        [Tooltip("Localization key for scenario description.")]
        public string descriptionKey = string.Empty;

        [Tooltip("Emergency category this scenario represents.")]
        public EmergencyType type = EmergencyType.EngineFailure;

        [Tooltip("Initial severity when triggered.")]
        public EmergencySeverity defaultSeverity = EmergencySeverity.Warning;

        [Tooltip("Seconds before severity escalates one level.")]
        public float escalationTime = 60f;

        [Tooltip("Maximum seconds allowed to resolve before crash.")]
        public float resolutionTimeLimit = 300f;

        [Tooltip("Whether this emergency can be resolved without landing.")]
        public bool canResolveInFlight = false;

        [Tooltip("Whether this emergency mandates an emergency landing.")]
        public bool requiresEmergencyLanding = true;

        [Tooltip("Localization keys for checklist procedure steps.")]
        public string[] checklistStepKeys = Array.Empty<string>();

        [Tooltip("Difficulty rating 1-5.")]
        [Range(1, 5)]
        public int difficultyRating = 3;

        [Tooltip("The distress call required for this scenario.")]
        public DistressCallType requiredCall = DistressCallType.PanPan;
    }

    /// <summary>Runtime state of a currently active emergency.</summary>
    [Serializable]
    public class ActiveEmergency
    {
        [Tooltip("Unique runtime identifier.")]
        public string emergencyId = string.Empty;

        [Tooltip("Source scenario definition.")]
        public EmergencyScenario scenario;

        [Tooltip("Current severity level.")]
        public EmergencySeverity currentSeverity;

        [Tooltip("Current lifecycle phase.")]
        public EmergencyPhase currentPhase;

        [Tooltip("Game time when this emergency was triggered.")]
        public float triggerTime;

        [Tooltip("Seconds elapsed since trigger.")]
        public float elapsedTime;

        [Tooltip("Number of checklist steps completed.")]
        public int checklistProgress;

        [Tooltip("Total number of checklist steps.")]
        public int totalChecklistSteps;

        [Tooltip("Whether a distress call has been made.")]
        public bool distressCallMade;

        [Tooltip("Whether severity is currently escalating.")]
        public bool isEscalating;

        [Tooltip("World position where the emergency was triggered.")]
        public Vector3 triggerPosition;

        [Tooltip("Altitude in metres at trigger time.")]
        public float triggerAltitude;

        [Tooltip("ID of divert airport selected, if any.")]
        public string divertAirportId = string.Empty;

        [Tooltip("Damage applied to aircraft each second while active.")]
        public float damagePerSecond;
    }

    /// <summary>A candidate site for an emergency landing.</summary>
    [Serializable]
    public class EmergencyLandingSite
    {
        [Tooltip("Unique identifier for this landing site.")]
        public string siteId = string.Empty;

        [Tooltip("Localization key for display name.")]
        public string displayNameKey = string.Empty;

        [Tooltip("World position of the touchdown zone.")]
        public Vector3 position;

        [Tooltip("Available runway length in metres.")]
        public float runwayLength;

        [Tooltip("Runway magnetic heading in degrees.")]
        public float runwayHeading;

        [Tooltip("Whether emergency services are on standby.")]
        public bool hasEmergencyServices;

        [Tooltip("Water ditching site (no runway).")]
        public bool isWaterLanding;

        [Tooltip("Off-airport field landing.")]
        public bool isFieldLanding;

        [Tooltip("Rescue unit types available at this site.")]
        public List<RescueUnitType> availableRescue = new List<RescueUnitType>();
    }

    /// <summary>A single step in an emergency procedure checklist.</summary>
    [Serializable]
    public class EmergencyChecklistItem
    {
        [Tooltip("Localization key for the step label.")]
        public string stepKey = string.Empty;

        [Tooltip("Localization key for the action description.")]
        public string actionKey = string.Empty;

        [Tooltip("Whether this step completes automatically (no player input needed).")]
        public bool isAutomatic;

        [Tooltip("Whether skipping this step incurs a severity penalty.")]
        public bool isCritical;

        [Tooltip("Maximum seconds the player has to complete this step; 0 = unlimited.")]
        public float timeLimit;
    }

    /// <summary>Runtime state of a single rescue unit en route to an incident.</summary>
    [Serializable]
    public class RescueUnit
    {
        [Tooltip("Unique identifier.")]
        public string unitId = string.Empty;

        [Tooltip("Category of this rescue unit.")]
        public RescueUnitType type;

        [Tooltip("Current world position.")]
        public Vector3 position;

        [Tooltip("Destination world position.")]
        public Vector3 targetPosition;

        [Tooltip("Travel speed in metres per second.")]
        public float speed;

        [Tooltip("Estimated seconds until arrival at target.")]
        public float arrivalTime;

        [Tooltip("Whether this unit has reached its destination.")]
        public bool hasArrived;

        [Tooltip("Localization key for unit display name.")]
        public string displayNameKey = string.Empty;
    }

    /// <summary>Global configuration for the Emergency system.</summary>
    [Serializable]
    public class EmergencyConfig
    {
        [Tooltip("Whether random emergencies can occur during free flight.")]
        public bool enableRandomEmergencies = false;

        [Tooltip("Average seconds between random emergency events.")]
        public float randomEmergencyInterval = 1200f;

        [Tooltip("Probability (0-1) of an emergency occurring each interval.")]
        [Range(0f, 1f)]
        public float randomEmergencyChance = 0.1f;

        [Tooltip("Seconds per checklist step before auto-timeout.")]
        public float checklistStepTimeout = 30f;

        [Tooltip("Multiplier applied to severity escalation speed.")]
        public float emergencyEscalationRate = 1f;

        [Tooltip("Seconds after landing before rescue units are dispatched.")]
        public float rescueDispatchDelay = 5f;

        [Tooltip("Base seconds for a rescue unit to travel to the site.")]
        public float rescueArrivalBaseTime = 120f;

        [Tooltip("Maximum number of simultaneous active emergencies.")]
        [Range(1, 4)]
        public int maxSimultaneousEmergencies = 2;

        [Tooltip("Whether crash consequences (game over / damage) are applied.")]
        public bool enableCrashConsequences = true;

        [Tooltip("Whether tutorial emergencies are available in Flight School.")]
        public bool enableTutorialEmergencies = true;

        [Tooltip("Extra assistance level for cautious players (0 = none, 2 = maximum).")]
        [Range(0, 2)]
        public int cautiousPlayerAssistLevel = 0;
    }

    /// <summary>Outcome record written when an emergency concludes.</summary>
    [Serializable]
    public class EmergencyResolution
    {
        [Tooltip("ID of the resolved emergency.")]
        public string emergencyId = string.Empty;

        [Tooltip("Emergency type that was resolved.")]
        public EmergencyType type;

        [Tooltip("Phase the emergency was in when it ended.")]
        public EmergencyPhase finalPhase;

        [Tooltip("Total seconds the emergency was active.")]
        public float totalTime;

        [Tooltip("Number of checklist steps completed.")]
        public int checklistStepsCompleted;

        [Tooltip("Total checklist steps in the scenario.")]
        public int checklistStepsTotal;

        [Tooltip("Whether a distress call was made.")]
        public bool distressCallMade;

        [Tooltip("Landing distance in metres from the planned threshold.")]
        public float landingDistance;

        [Tooltip("Touchdown speed in m/s.")]
        public float landingSpeed;

        [Tooltip("Whether the emergency was handled successfully.")]
        public bool wasSuccessful;

        [Tooltip("Composite score 0-100.")]
        [Range(0f, 100f)]
        public float score;
    }
}
