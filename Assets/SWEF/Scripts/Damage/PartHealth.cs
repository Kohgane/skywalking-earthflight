// PartHealth.cs — SWEF Damage & Repair System (Phase 66)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Damage
{
    /// <summary>
    /// Serializable health tracker for a single <see cref="AircraftPart"/>.
    ///
    /// <para>Instances are owned by <see cref="DamageModel"/> which manages one
    /// <see cref="PartHealth"/> entry per part in its internal dictionary.</para>
    /// </summary>
    [Serializable]
    public class PartHealth
    {
        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>The aircraft part this instance represents.</summary>
        public AircraftPart part;

        // ── Health Values ─────────────────────────────────────────────────────

        /// <summary>Maximum possible health for this part.</summary>
        public float maxHealth = 100f;

        /// <summary>Current health; clamped to [0, <see cref="maxHealth"/>].</summary>
        public float currentHealth = 100f;

        /// <summary>
        /// Current damage severity derived from <see cref="healthPercent"/>.
        /// Updated automatically by <see cref="ApplyDamage"/> and <see cref="Repair"/>.
        /// </summary>
        public DamageLevel damageLevel;

        // ── Repair ────────────────────────────────────────────────────────────

        /// <summary>Health restored per second while this part is being repaired.</summary>
        public float repairRate = 5f;

        // ── Computed Properties ───────────────────────────────────────────────

        /// <summary>Current health expressed as a fraction of <see cref="maxHealth"/> (0–1).</summary>
        public float healthPercent => maxHealth > 0f ? currentHealth / maxHealth : 0f;

        /// <summary><c>true</c> when <see cref="currentHealth"/> has reached zero.</summary>
        public bool isDestroyed => currentHealth <= 0f;

        /// <summary>
        /// <c>true</c> while the part is still usable
        /// (damage level is below <see cref="DamageLevel.Critical"/>).
        /// </summary>
        public bool isFunctional => damageLevel < DamageLevel.Critical;

        // ── History ───────────────────────────────────────────────────────────

        /// <summary>Ordered log of every <see cref="DamageData"/> event applied to this part.</summary>
        public List<DamageData> damageHistory = new List<DamageData>();

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>Creates a fully-healthy <see cref="PartHealth"/> for <paramref name="part"/>.</summary>
        /// <param name="part">The aircraft part to track.</param>
        public PartHealth(AircraftPart part)
        {
            this.part      = part;
            currentHealth  = maxHealth;
            damageLevel    = DamageLevel.None;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reduces <see cref="currentHealth"/> by <paramref name="amount"/>, clamps
        /// to zero, records the <paramref name="data"/> event in <see cref="damageHistory"/>,
        /// and updates <see cref="damageLevel"/>.
        /// </summary>
        /// <param name="amount">Raw health points to remove (≥ 0).</param>
        /// <param name="data">
        /// Optional <see cref="DamageData"/> to append to <see cref="damageHistory"/>.
        /// Pass <c>null</c> to skip history recording.
        /// </param>
        public void ApplyDamage(float amount, DamageData data = null)
        {
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            damageLevel   = CalculateDamageLevel();

            if (data != null)
                damageHistory.Add(data);
        }

        /// <summary>
        /// Restores <see cref="currentHealth"/> by <paramref name="amount"/>, clamps
        /// to <see cref="maxHealth"/>, and updates <see cref="damageLevel"/>.
        /// </summary>
        /// <param name="amount">Health points to restore (≥ 0).</param>
        public void Repair(float amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            damageLevel   = CalculateDamageLevel();
        }

        /// <summary>
        /// Derives the appropriate <see cref="DamageLevel"/> from the current
        /// <see cref="healthPercent"/>.
        ///
        /// <list type="table">
        ///   <listheader><term>Range</term><description>Level</description></listheader>
        ///   <item><term>&gt; 90 %</term><description><see cref="DamageLevel.None"/></description></item>
        ///   <item><term>&gt; 70 %</term><description><see cref="DamageLevel.Minor"/></description></item>
        ///   <item><term>&gt; 50 %</term><description><see cref="DamageLevel.Moderate"/></description></item>
        ///   <item><term>&gt; 25 %</term><description><see cref="DamageLevel.Severe"/></description></item>
        ///   <item><term>&gt;  0 %</term><description><see cref="DamageLevel.Critical"/></description></item>
        ///   <item><term>= 0 %</term><description><see cref="DamageLevel.Destroyed"/></description></item>
        /// </list>
        /// </summary>
        /// <returns>The computed <see cref="DamageLevel"/>.</returns>
        public DamageLevel CalculateDamageLevel()
        {
            float hp = healthPercent;
            if (hp <= 0f)                            return DamageLevel.Destroyed;
            if (hp <= DamageConfig.CriticalThreshold) return DamageLevel.Critical;
            if (hp <= DamageConfig.SevereThreshold)   return DamageLevel.Severe;
            if (hp <= DamageConfig.ModerateThreshold) return DamageLevel.Moderate;
            if (hp <= DamageConfig.MinorThreshold)    return DamageLevel.Minor;
            return DamageLevel.None;
        }
    }
}
