// DamageModel.cs — SWEF Damage & Repair System (Phase 66)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Damage
{
    /// <summary>
    /// Master damage controller attached to the aircraft.
    ///
    /// <para>Maintains a <see cref="PartHealth"/> entry for every
    /// <see cref="AircraftPart"/>, handles collision detection, applies
    /// damage multipliers, and raises change notifications consumed by
    /// <see cref="DamageEffect"/> and <see cref="DamageIndicatorUI"/>.</para>
    ///
    /// <para>Attach to the root aircraft GameObject alongside a
    /// <see cref="Rigidbody"/> and one or more <see cref="Collider"/>s.</para>
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class DamageModel : MonoBehaviour
    {
        #region Inspector

        [Header("Damage Settings")]
        [Tooltip("Global multiplier applied to every incoming damage value. Use for difficulty scaling.")]
        /// <summary>Global multiplier applied to every incoming damage value.</summary>
        [Min(0f)]
        public float globalDamageMultiplier = 1f;

        [Tooltip("When true the aircraft cannot receive damage (god mode).")]
        /// <summary>When <c>true</c> the aircraft cannot receive damage.</summary>
        public bool isInvulnerable = false;

        [Tooltip("Scales relative collision velocity to a damage value.")]
        /// <summary>Scales relative collision velocity to a damage value.</summary>
        [Min(0f)]
        public float collisionDamageScale = DamageConfig.CollisionDamageScale;

        #endregion

        #region Public State

        /// <summary>Read-only view of per-part health records.</summary>
        public IReadOnlyDictionary<AircraftPart, PartHealth> Parts => _parts;

        #endregion

        #region Events

        /// <summary>Raised every time damage is successfully applied to any part.</summary>
        public event Action<DamageData> OnDamageReceived;

        /// <summary>Raised when a part transitions to a new <see cref="DamageLevel"/>.</summary>
        public event Action<AircraftPart, DamageLevel> OnPartDamageLevelChanged;

        /// <summary>Raised the first time the aircraft reaches a state where it can no longer fly.</summary>
        public event Action OnAircraftDestroyed;

        #endregion

        #region Private State

        private readonly Dictionary<AircraftPart, PartHealth> _parts =
            new Dictionary<AircraftPart, PartHealth>();

        private bool _destroyedFired;

        #endregion

        #region Unity

        private void Awake()
        {
            foreach (AircraftPart p in Enum.GetValues(typeof(AircraftPart)))
                _parts[p] = new PartHealth(p);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (isInvulnerable) return;

            float relativeVelocity = collision.relativeVelocity.magnitude;
            float rawDamage        = relativeVelocity * collisionDamageScale;

            if (rawDamage <= 0f) return;

            // Determine which part was hit from the contact point.
            AircraftPart hitPart = GetNearestPart(collision.GetContact(0).point);

            var data = new DamageData(
                source:       DamageSource.Collision,
                affectedPart: hitPart,
                damageAmount: rawDamage,
                impactPoint:  collision.GetContact(0).point,
                impactNormal: collision.GetContact(0).normal,
                impactForce:  collision.impulse.magnitude,
                timestamp:    Time.time,
                description:  $"Collision with {collision.gameObject.name} at {relativeVelocity:F1} m/s");

            ApplyDamage(data);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Main entry point for inflicting damage on the aircraft.
        ///
        /// <para>Applies <see cref="globalDamageMultiplier"/>, updates
        /// <see cref="PartHealth"/>, fires <see cref="OnDamageReceived"/>,
        /// <see cref="OnPartDamageLevelChanged"/>, and — when appropriate —
        /// <see cref="OnAircraftDestroyed"/>.</para>
        /// </summary>
        /// <param name="data">Fully-populated damage event descriptor.</param>
        public void ApplyDamage(DamageData data)
        {
            if (isInvulnerable || data == null) return;
            if (!_parts.TryGetValue(data.affectedPart, out PartHealth partHealth)) return;

            DamageLevel previousLevel = partHealth.damageLevel;
            float scaledAmount = data.damageAmount * globalDamageMultiplier;

            partHealth.ApplyDamage(scaledAmount, data);

            OnDamageReceived?.Invoke(data);

            if (partHealth.damageLevel != previousLevel)
                OnPartDamageLevelChanged?.Invoke(data.affectedPart, partHealth.damageLevel);

            if (!_destroyedFired && !IsFlightCapable())
            {
                _destroyedFired = true;
                OnAircraftDestroyed?.Invoke();
            }
        }

        /// <summary>
        /// Applies damage to all parts whose local-space attachment point lies
        /// within <paramref name="radius"/> of <paramref name="center"/>.
        ///
        /// <para>Damage falls off linearly from the centre to the edge of the
        /// blast radius.</para>
        /// </summary>
        /// <param name="center">World-space blast origin.</param>
        /// <param name="radius">Blast radius in metres.</param>
        /// <param name="damage">Maximum damage at the blast centre.</param>
        /// <param name="source">Damage source classification.</param>
        public void ApplyAreaDamage(Vector3 center, float radius, float damage, DamageSource source)
        {
            if (isInvulnerable) return;

            foreach (var kvp in _parts)
            {
                Vector3 partPos  = GetPartWorldPosition(kvp.Key);
                float   dist     = Vector3.Distance(center, partPos);
                if (dist > radius) continue;

                float falloff    = 1f - (dist / radius);
                float rawDamage  = damage * falloff;

                var data = new DamageData(
                    source:       source,
                    affectedPart: kvp.Key,
                    damageAmount: rawDamage,
                    impactPoint:  center,
                    impactNormal: (partPos - center).normalized,
                    impactForce:  rawDamage,
                    timestamp:    Time.time,
                    description:  $"Area damage ({source}) at {center}");

                ApplyDamage(data);
            }
        }

        /// <summary>
        /// Returns the weighted average health of all parts as a value in [0, 100].
        /// </summary>
        public float GetOverallHealth()
        {
            float totalWeight  = 0f;
            float weightedSum  = 0f;

            foreach (var kvp in _parts)
            {
                float w     = DamageConfig.GetPartWeight(kvp.Key);
                totalWeight += w;
                weightedSum += kvp.Value.currentHealth * w;
            }

            return totalWeight > 0f ? weightedSum / totalWeight : 100f;
        }

        /// <summary>Returns the <see cref="DamageLevel"/> that corresponds to <see cref="GetOverallHealth"/>.</summary>
        public DamageLevel GetOverallDamageLevel()
        {
            float hp = GetOverallHealth() / 100f;
            if (hp <= 0f)                            return DamageLevel.Destroyed;
            if (hp <= DamageConfig.CriticalThreshold) return DamageLevel.Critical;
            if (hp <= DamageConfig.SevereThreshold)   return DamageLevel.Severe;
            if (hp <= DamageConfig.ModerateThreshold) return DamageLevel.Moderate;
            if (hp <= DamageConfig.MinorThreshold)    return DamageLevel.Minor;
            return DamageLevel.None;
        }

        /// <summary>
        /// Returns the <see cref="PartHealth"/> record for <paramref name="part"/>,
        /// or <c>null</c> if the part is not tracked (should not occur in normal use).
        /// </summary>
        /// <param name="part">Part to query.</param>
        public PartHealth GetPartHealth(AircraftPart part)
        {
            return _parts.TryGetValue(part, out PartHealth ph) ? ph : null;
        }

        /// <summary>
        /// Returns <c>false</c> when the aircraft is no longer capable of controlled
        /// flight: the engine is destroyed or both wings are at
        /// <see cref="DamageLevel.Critical"/> or worse.
        /// </summary>
        public bool IsFlightCapable()
        {
            if (_parts[AircraftPart.Engine].isDestroyed)
                return false;

            bool leftWingCritical  = _parts[AircraftPart.LeftWing].damageLevel  >= DamageLevel.Critical;
            bool rightWingCritical = _parts[AircraftPart.RightWing].damageLevel >= DamageLevel.Critical;
            if (leftWingCritical && rightWingCritical)
                return false;

            return true;
        }

        // ── Performance Effect Queries ────────────────────────────────────────

        /// <summary>
        /// Returns a thrust multiplier in [0, 1] based on engine health.
        /// Below <see cref="DamageConfig.EngineFailureThreshold"/> the engine
        /// provides no thrust.
        /// </summary>
        public float GetEngineEfficiency()
        {
            float hp = _parts[AircraftPart.Engine].healthPercent;
            if (hp <= DamageConfig.EngineFailureThreshold) return 0f;
            return hp;
        }

        /// <summary>
        /// Returns a lift/maneuverability multiplier in [0, 1] based on the
        /// average health of the four wing/control surfaces.
        /// </summary>
        public float GetWingEfficiency()
        {
            float avg = (_parts[AircraftPart.LeftWing].healthPercent
                       + _parts[AircraftPart.RightWing].healthPercent
                       + _parts[AircraftPart.LeftAileron].healthPercent
                       + _parts[AircraftPart.RightAileron].healthPercent) * 0.25f;
            return avg;
        }

        /// <summary>
        /// Returns a stability multiplier in [0, 1] based on the average health
        /// of the tail, elevator, and rudder.
        /// </summary>
        public float GetStabilityEfficiency()
        {
            float avg = (_parts[AircraftPart.Tail].healthPercent
                       + _parts[AircraftPart.Elevator].healthPercent
                       + _parts[AircraftPart.Rudder].healthPercent) / 3f;
            return avg;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Returns the <see cref="AircraftPart"/> whose approximate local-space
        /// attachment point is closest to <paramref name="worldPos"/>.
        /// </summary>
        private AircraftPart GetNearestPart(Vector3 worldPos)
        {
            AircraftPart nearest     = AircraftPart.Fuselage;
            float        nearestDist = float.MaxValue;

            foreach (AircraftPart p in Enum.GetValues(typeof(AircraftPart)))
            {
                float dist = Vector3.Distance(worldPos, GetPartWorldPosition(p));
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest     = p;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Returns an approximate world-space position for <paramref name="part"/>
        /// relative to this transform.  Uses hardcoded local offsets that match
        /// a generic aircraft layout.
        /// </summary>
        private Vector3 GetPartWorldPosition(AircraftPart part)
        {
            Vector3 localOffset;
            switch (part)
            {
                case AircraftPart.Cockpit:       localOffset = new Vector3( 0f,  0.5f,  3f);  break;
                case AircraftPart.Engine:        localOffset = new Vector3( 0f,  0f,    1f);  break;
                case AircraftPart.LeftWing:      localOffset = new Vector3(-3f,  0f,    0f);  break;
                case AircraftPart.RightWing:     localOffset = new Vector3( 3f,  0f,    0f);  break;
                case AircraftPart.LeftAileron:   localOffset = new Vector3(-4f,  0f,   -0.5f);break;
                case AircraftPart.RightAileron:  localOffset = new Vector3( 4f,  0f,   -0.5f);break;
                case AircraftPart.Tail:          localOffset = new Vector3( 0f,  0.5f, -4f);  break;
                case AircraftPart.Elevator:      localOffset = new Vector3( 0f,  0f,   -5f);  break;
                case AircraftPart.Rudder:        localOffset = new Vector3( 0f,  1.5f, -4.5f);break;
                case AircraftPart.LandingGear:   localOffset = new Vector3( 0f, -1f,    0f);  break;
                case AircraftPart.Fuselage:
                default:                         localOffset = Vector3.zero;                   break;
            }
            return transform.TransformPoint(localOffset);
        }

        #endregion
    }
}
