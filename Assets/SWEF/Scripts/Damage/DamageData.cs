// DamageData.cs — SWEF Damage & Repair System (Phase 66)
using System;
using UnityEngine;

namespace SWEF.Damage
{
    /// <summary>
    /// Serializable data container that describes a single damage event.
    ///
    /// <para>Instances are created by the system that detects the damage
    /// (e.g. <see cref="DamageModel.OnCollisionEnter"/>) and are then passed
    /// to <see cref="DamageModel.ApplyDamage"/> as well as stored in
    /// <see cref="PartHealth.damageHistory"/>.</para>
    /// </summary>
    [Serializable]
    public class DamageData
    {
        /// <summary>Origin of this damage event.</summary>
        public DamageSource source;

        /// <summary>Aircraft part that received the damage.</summary>
        public AircraftPart affectedPart;

        /// <summary>Raw damage value in the range [0, 100].</summary>
        public float damageAmount;

        /// <summary>World-space position where the damage was inflicted.</summary>
        public Vector3 impactPoint;

        /// <summary>World-space surface normal at the impact point.</summary>
        public Vector3 impactNormal;

        /// <summary>Magnitude of the impact force (Newtons or arbitrary units).</summary>
        public float impactForce;

        /// <summary>Value of <c>Time.time</c> when the damage occurred.</summary>
        public float timestamp;

        /// <summary>Human-readable description of the damage event.</summary>
        public string description;

        /// <summary>
        /// Initialises a new <see cref="DamageData"/> with all fields explicitly set.
        /// </summary>
        /// <param name="source">Origin of the damage.</param>
        /// <param name="affectedPart">Part that was damaged.</param>
        /// <param name="damageAmount">Raw damage value (0–100).</param>
        /// <param name="impactPoint">World-space impact position.</param>
        /// <param name="impactNormal">World-space surface normal at impact.</param>
        /// <param name="impactForce">Magnitude of the impact force.</param>
        /// <param name="timestamp">Game time of the event (<c>Time.time</c>).</param>
        /// <param name="description">Optional human-readable description.</param>
        public DamageData(
            DamageSource source,
            AircraftPart affectedPart,
            float        damageAmount,
            Vector3      impactPoint,
            Vector3      impactNormal,
            float        impactForce,
            float        timestamp,
            string       description = "")
        {
            this.source       = source;
            this.affectedPart = affectedPart;
            this.damageAmount = damageAmount;
            this.impactPoint  = impactPoint;
            this.impactNormal = impactNormal;
            this.impactForce  = impactForce;
            this.timestamp    = timestamp;
            this.description  = description;
        }
    }
}
