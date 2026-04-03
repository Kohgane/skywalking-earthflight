// AircraftPartData.cs — SWEF Aircraft Workshop & Part Customization (Phase 90)
using System;
using UnityEngine;

namespace SWEF.Workshop
{
    /// <summary>
    /// Serialisable data record describing a single aircraft part that can be
    /// equipped in the Workshop system.  Instances are typically stored inside
    /// <see cref="PartInventoryController"/> and referenced by <see cref="AircraftBuildData"/>.
    /// </summary>
    [Serializable]
    public class AircraftPartData
    {
        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>Unique string identifier for this part (e.g. <c>"engine_turbojet_mk2"</c>).</summary>
        [Tooltip("Unique string identifier for this part.")]
        public string partId = string.Empty;

        /// <summary>Human-readable display name shown in the Workshop UI.</summary>
        [Tooltip("Human-readable display name shown in the Workshop UI.")]
        public string partName = string.Empty;

        /// <summary>Category of this part (Engine, Wing, Fuselage, etc.).</summary>
        [Tooltip("Category of this part.")]
        public AircraftPartType partType = AircraftPartType.Engine;

        /// <summary>Quality tier that determines stat bonuses and unlock requirements.</summary>
        [Tooltip("Quality tier that determines stat bonuses and unlock requirements.")]
        public PartTier tier = PartTier.Common;

        // ── Physical Characteristics ──────────────────────────────────────────

        /// <summary>Mass of the part in kilograms, added to total aircraft weight.</summary>
        [Tooltip("Mass of the part in kilograms.")]
        public float weight = 50f;

        /// <summary>
        /// Aerodynamic drag coefficient contribution.  A lower value reduces total
        /// aircraft drag and raises top speed.
        /// </summary>
        [Tooltip("Aerodynamic drag coefficient contribution (lower = faster).")]
        public float dragCoefficient = 0.05f;

        /// <summary>
        /// Multiplier applied to the base lift force produced by the airframe
        /// (1.0 = neutral; &gt;1.0 increases climb capability).
        /// </summary>
        [Tooltip("Lift force multiplier (1 = neutral).")]
        public float liftModifier = 1f;

        /// <summary>
        /// Multiplier applied to the engine thrust output
        /// (1.0 = neutral; &gt;1.0 increases maximum thrust).
        /// </summary>
        [Tooltip("Engine thrust multiplier (1 = neutral). Only meaningful for Engine parts.")]
        public float thrustModifier = 1f;

        /// <summary>Maximum hit-points for this part before it reaches a destroyed state.</summary>
        [Tooltip("Maximum hit-points (durability) for this part.")]
        public float durability = 100f;

        // ── Presentation ──────────────────────────────────────────────────────

        /// <summary>Short description shown in the Workshop detail panel.</summary>
        [Tooltip("Short description shown in the Workshop detail panel.")]
        public string description = string.Empty;

        /// <summary>
        /// Resource path (relative to a <c>Resources/</c> folder) for the part's
        /// icon sprite.
        /// </summary>
        [Tooltip("Resources/ path for the part icon sprite.")]
        public string iconPath = string.Empty;

        // ── Unlock ────────────────────────────────────────────────────────────

        /// <summary>
        /// Human-readable description of what the player must do to unlock this
        /// part (e.g. <c>"Reach Pilot Level 10"</c>).
        /// </summary>
        [Tooltip("Human-readable unlock requirement description.")]
        public string unlockRequirement = string.Empty;

        /// <summary>Whether the player has already unlocked this part.</summary>
        [Tooltip("Whether the player has already unlocked this part.")]
        public bool isUnlocked = false;
    }
}
