using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.PassengerCargo
{
    // ── Enumerations ──────────────────────────────────────────────────────────

    /// <summary>Types of transport mission available to the player.</summary>
    public enum MissionType
    {
        PassengerStandard,
        PassengerVIP,
        PassengerCharter,
        CargoStandard,
        CargoFragile,
        CargoHazardous,
        CargoOversized,
        EmergencyMedical
    }

    /// <summary>Passenger comfort quality levels mapped from the 0–100 score.</summary>
    public enum ComfortLevel
    {
        Excellent,  // ≥ 90
        Good,       // 70–89
        Fair,       // 50–69
        Poor,       // 30–49
        Critical    // < 30
    }

    /// <summary>Category of cargo being transported.</summary>
    public enum CargoCategory
    {
        General,
        Perishable,
        Fragile,
        Hazardous,
        Livestock,
        Oversized,
        Medical
    }

    /// <summary>Phases of the delivery countdown timer.</summary>
    public enum TimerPhase
    {
        Green,    // > 50 % time remaining
        Yellow,   // 25–50 %
        Red,      // < 25 %
        Overtime  // 0 % — mission still completable with penalty
    }

    /// <summary>Lifecycle states for an active transport mission.</summary>
    public enum MissionState
    {
        Idle,
        Accepted,
        Loading,
        InFlight,
        Approaching,
        Delivered,
        Completed,
        Failed,
        Abandoned
    }

    // ── Data Classes ──────────────────────────────────────────────────────────

    /// <summary>
    /// Describes the passenger group carried on a transport mission.
    /// </summary>
    [Serializable]
    public class PassengerProfile
    {
        [Tooltip("Number of passengers on board.")]
        public int   passengerCount        = 1;

        [Tooltip("0 = economy, 1 = business, 2 = first class / VIP.")]
        public int   vipLevel              = 0;

        [Range(0f, 1f)]
        [Tooltip("How sensitive passengers are to discomfort (0 = tolerant, 1 = demanding).")]
        public float comfortSensitivity    = 0.5f;

        [Tooltip("Preferred cruising altitude in metres.")]
        public float preferredAltitude     = 3000f;

        [Tooltip("G-force threshold above which passengers become motion-sick.")]
        public float motionSicknessThreshold = 2.0f;
    }

    /// <summary>
    /// Describes the cargo loaded for a transport mission.
    /// </summary>
    [Serializable]
    public class CargoManifest
    {
        [Tooltip("Total cargo weight in kilograms.")]
        public float         weight             = 500f;

        [Tooltip("Volume in cubic metres (affects CG shift calculations).")]
        public float         volume             = 2f;

        public CargoCategory category           = CargoCategory.General;

        [Range(0f, 1f)]
        [Tooltip("0 = robust, 1 = extremely fragile.")]
        public float         fragilityRating    = 0f;

        [Tooltip("Minimum acceptable temperature in °C (−273 if unrestricted).")]
        public float         minTemperature     = -273f;

        [Tooltip("Maximum acceptable temperature in °C.")]
        public float         maxTemperature     = 60f;

        [Tooltip("Hard time limit for delivery in seconds (0 = unlimited).")]
        public float         timeLimit          = 0f;

        [Tooltip("Free-text special handling notes shown to the player.")]
        public string        specialHandling    = "";
    }

    /// <summary>
    /// Result of a completed (or failed) delivery mission.
    /// </summary>
    [Serializable]
    public class DeliveryResult
    {
        public bool   success;
        public float  comfortScore;       // 0–100
        public float  timeBonus;          // additive fraction, e.g. 0.3 = +30 %
        public float  damagePercentage;   // 0–100
        public long   totalXP;
        public long   totalCoins;
        public int    starRating;         // 1–5
        public string contractId;
    }

    // ── ScriptableObject ──────────────────────────────────────────────────────

    /// <summary>
    /// Defines a single transport contract that the player can accept.
    /// Create instances via the Project window → Create → SWEF → Transport Contract.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTransportContract",
                     menuName  = "SWEF/Transport Contract",
                     order     = 60)]
    public class TransportContract : ScriptableObject
    {
        [Header("Identity")]
        public string      contractId       = "";
        public MissionType missionType      = MissionType.PassengerStandard;

        [Header("Route")]
        [Tooltip("ICAO or display name of the origin airport.")]
        public string      origin           = "";

        [Tooltip("ICAO or display name of the destination airport.")]
        public string      destination      = "";

        [Header("Payload")]
        public PassengerProfile passengerProfile = new PassengerProfile();
        public CargoManifest    cargoManifest    = new CargoManifest();

        [Header("Rewards")]
        public long  baseReward            = 200;
        public long  bonusReward           = 50;
        public long  baseXP                = 150;

        [Header("Requirements")]
        [Tooltip("Time limit for this contract in seconds (0 = unlimited).")]
        public float timeLimitSeconds      = 0f;

        [Tooltip("Minimum pilot rank level required to accept this contract.")]
        public int   requiredRank          = 0;

        [Header("Localisation")]
        [Tooltip("Key into the localisation table for the contract description.")]
        public string descriptionLocKey    = "";
    }
}
