// ActiveDisaster.cs — SWEF Natural Disaster & Dynamic World Events (Phase 86)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if SWEF_MINIMAP_AVAILABLE
using SWEF.Minimap;
#endif

#if SWEF_VFX_AVAILABLE
using SWEF.VFX;
#endif

namespace SWEF.NaturalDisaster
{
    /// <summary>
    /// Phase 86 — MonoBehaviour that represents a single live disaster instance in the world.
    /// Manages phase transitions, hazard zone lifecycle, screen-shake coroutines, atmospheric
    /// VFX integration, and minimap blip registration.
    ///
    /// <para>Spawned and owned by <see cref="DisasterManager"/>. Do not add to scene manually.</para>
    /// </summary>
    public class ActiveDisaster : MonoBehaviour
    {
        // ── Data ──────────────────────────────────────────────────────────────────

        /// <summary>ScriptableObject template that defines this disaster's properties.</summary>
        public DisasterData data { get; private set; }

        // ── Runtime State ─────────────────────────────────────────────────────────

        /// <summary>Current lifecycle phase.</summary>
        public DisasterPhase currentPhase { get; private set; } = DisasterPhase.Dormant;

        /// <summary>Current severity (may escalate up to <see cref="DisasterData.maxSeverity"/>).</summary>
        public DisasterSeverity currentSeverity { get; private set; } = DisasterSeverity.Minor;

        /// <summary>World-space position of the disaster epicentre.</summary>
        public Vector3 epicenter { get; private set; }

        /// <summary>Total elapsed time since the disaster was spawned (seconds).</summary>
        public float elapsedTime { get; private set; }

        /// <summary>Elapsed time within the current phase (seconds).</summary>
        public float phaseElapsedTime { get; private set; }

        /// <summary>Active hazard zones this disaster produces.</summary>
        public List<HazardZone> hazardZones { get; private set; } = new List<HazardZone>();

        /// <summary>Overall disaster influence radius (metres).</summary>
        public float currentRadius { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised whenever the disaster transitions to a new phase.</summary>
        public event Action<DisasterPhase> OnPhaseChanged;

        /// <summary>Raised when the disaster fully ends (Aftermath complete).</summary>
        public event Action<ActiveDisaster> OnDisasterEnded;

        // ── Private ───────────────────────────────────────────────────────────────

        private bool _initialised;
        private string _minimapBlipId;

        // ── Initialisation ────────────────────────────────────────────────────────

        /// <summary>
        /// Called by <see cref="DisasterManager"/> immediately after instantiation.
        /// </summary>
        public void Initialise(DisasterData disasterData, Vector3 position, DisasterSeverity severity)
        {
            data            = disasterData;
            epicenter       = position;
            currentSeverity = (DisasterSeverity)Mathf.Min((int)severity, (int)disasterData.maxSeverity);
            transform.position = position;
            currentRadius   = disasterData.hazardRadius;
            _initialised    = true;

            BuildHazardZones();
            TransitionToPhase(DisasterPhase.Warning);
            RegisterMinimapBlip();
        }

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Update()
        {
            if (!_initialised) return;

            float dt = Time.deltaTime;
            elapsedTime      += dt;
            phaseElapsedTime += dt;

            UpdatePhaseTransition();
            UpdateHazardZones(dt);
        }

        private void OnDestroy()
        {
            UnregisterMinimapBlip();
        }

        // ── Phase Transition ──────────────────────────────────────────────────────

        private void UpdatePhaseTransition()
        {
            float phaseDuration = GetCurrentPhaseDuration();
            if (phaseElapsedTime < phaseDuration) return;

            switch (currentPhase)
            {
                case DisasterPhase.Warning:
                    TransitionToPhase(DisasterPhase.Onset);
                    break;
                case DisasterPhase.Onset:
                    TransitionToPhase(DisasterPhase.Peak);
                    break;
                case DisasterPhase.Peak:
                    TransitionToPhase(DisasterPhase.Declining);
                    break;
                case DisasterPhase.Declining:
                    TransitionToPhase(DisasterPhase.Aftermath);
                    break;
                case DisasterPhase.Aftermath:
                    EndDisaster();
                    break;
            }
        }

        private void TransitionToPhase(DisasterPhase newPhase)
        {
            currentPhase     = newPhase;
            phaseElapsedTime = 0f;

            OnPhaseChanged?.Invoke(newPhase);
            DisasterManager.Instance?.NotifyPhaseChanged(this);

            switch (newPhase)
            {
                case DisasterPhase.Peak:
                    TriggerAtmosphericEffects(true);
                    if (data.type == DisasterType.Earthquake || data.type == DisasterType.Volcano)
                        StartCoroutine(ScreenShakeCoroutine());
                    break;
                case DisasterPhase.Declining:
                    TriggerAtmosphericEffects(false);
                    break;
            }
        }

        private float GetCurrentPhaseDuration()
        {
            switch (currentPhase)
            {
                case DisasterPhase.Warning:   return data.warningDuration;
                case DisasterPhase.Onset:
                {
                    float totalActive = data.baseDuration - data.warningDuration
                                        - data.peakDuration - data.aftermathDuration;
                    return Mathf.Max(5f, totalActive * 0.25f);
                }
                case DisasterPhase.Peak:      return data.peakDuration;
                case DisasterPhase.Declining:
                {
                    float totalActive = data.baseDuration - data.warningDuration
                                        - data.peakDuration - data.aftermathDuration;
                    return Mathf.Max(5f, totalActive * 0.75f);
                }
                case DisasterPhase.Aftermath: return data.aftermathDuration;
                default:                      return float.MaxValue;
            }
        }

        // ── Hazard Zones ──────────────────────────────────────────────────────────

        private void BuildHazardZones()
        {
            hazardZones.Clear();
            int idx = 0;
            foreach (HazardZoneType hzt in data.hazardTypes)
            {
                float baseRadius = GetDefaultRadius(hzt);
                hazardZones.Add(new HazardZone
                {
                    zoneId          = $"{data.disasterId}_{idx++}_{hzt}",
                    type            = hzt,
                    center          = epicenter,
                    radius          = baseRadius * 0.3f,   // start small
                    maxRadius       = baseRadius,
                    intensity       = 0.3f,
                    altitudeFloor   = data.altitudeRange.x,
                    altitudeCeiling = data.altitudeRange.y,
                    isActive        = true
                });
            }
        }

        private void UpdateHazardZones(float dt)
        {
            bool growing    = currentPhase == DisasterPhase.Onset || currentPhase == DisasterPhase.Peak;
            bool contracting = currentPhase == DisasterPhase.Declining || currentPhase == DisasterPhase.Aftermath;
            float targetIntensity = GetPhaseIntensity();

            foreach (HazardZone zone in hazardZones)
            {
                zone.center = epicenter;
                zone.intensity = Mathf.MoveTowards(zone.intensity, targetIntensity, dt * 0.2f);

                if (growing)    zone.Expand(dt, data.expansionRate);
                if (contracting) zone.Contract(dt, data.expansionRate * 0.5f);
            }

            // Overall radius follows the largest zone
            float maxR = 0f;
            foreach (HazardZone z in hazardZones)
                if (z.radius > maxR) maxR = z.radius;
            currentRadius = Mathf.Max(maxR, data.hazardRadius);
        }

        private float GetPhaseIntensity()
        {
            switch (currentPhase)
            {
                case DisasterPhase.Warning:   return 0.2f;
                case DisasterPhase.Onset:     return 0.6f;
                case DisasterPhase.Peak:      return 1.0f;
                case DisasterPhase.Declining: return 0.5f;
                case DisasterPhase.Aftermath: return 0.2f;
                default:                      return 0f;
            }
        }

        private static float GetDefaultRadius(HazardZoneType hzt)
        {
            switch (hzt)
            {
                case HazardZoneType.NoFlyZone:         return DisasterConfig.RadiusNoFlyZone;
                case HazardZoneType.Turbulence:        return DisasterConfig.RadiusTurbulence;
                case HazardZoneType.ReducedVisibility: return DisasterConfig.RadiusReducedVisibility;
                case HazardZoneType.ThermalUpDraft:    return DisasterConfig.RadiusThermalUpDraft;
                case HazardZoneType.AshCloud:          return DisasterConfig.RadiusAshCloud;
                case HazardZoneType.DebrisField:       return DisasterConfig.RadiusDebrisField;
                case HazardZoneType.FloodZone:         return DisasterConfig.RadiusFloodZone;
                case HazardZoneType.FireZone:          return DisasterConfig.RadiusFireZone;
                default:                               return 3000f;
            }
        }

        // ── Screen Shake ──────────────────────────────────────────────────────────

        private IEnumerator ScreenShakeCoroutine()
        {
            float shakeIntensity = GetShakeIntensity();
            Camera mainCam = Camera.main;
            if (mainCam == null) yield break;

            while (currentPhase == DisasterPhase.Peak)
            {
                float interval = UnityEngine.Random.Range(2f, 6f);
                yield return new WaitForSeconds(interval);

                if (currentPhase != DisasterPhase.Peak) break;

                Vector3 originalPos = mainCam.transform.localPosition;
                float elapsed = 0f;
                while (elapsed < DisasterConfig.ShakeDuration)
                {
                    elapsed += Time.deltaTime;
                    float x = UnityEngine.Random.Range(-shakeIntensity, shakeIntensity);
                    float y = UnityEngine.Random.Range(-shakeIntensity, shakeIntensity);
                    mainCam.transform.localPosition = originalPos + new Vector3(x, y, 0f);
                    yield return null;
                }
                mainCam.transform.localPosition = originalPos;
            }
        }

        private float GetShakeIntensity()
        {
            switch (currentSeverity)
            {
                case DisasterSeverity.Minor:        return DisasterConfig.ShakeMinorIntensity;
                case DisasterSeverity.Moderate:     return DisasterConfig.ShakeModerateIntensity;
                case DisasterSeverity.Severe:       return DisasterConfig.ShakeSevereIntensity;
                case DisasterSeverity.Catastrophic: return DisasterConfig.ShakeCatastrophicIntensity;
                case DisasterSeverity.Apocalyptic:  return DisasterConfig.ShakeApocalypticIntensity;
                default:                            return DisasterConfig.ShakeMinorIntensity;
            }
        }

        // ── Atmospheric VFX ───────────────────────────────────────────────────────

        private void TriggerAtmosphericEffects(bool active)
        {
#if SWEF_VFX_AVAILABLE
            var vfx = EnvironmentVFXController.Instance;
            if (vfx == null) return;
            foreach (string effectId in data.atmosphericEffects)
                vfx.SetEffectActive(effectId, active);
#endif
        }

        // ── Minimap Blip ──────────────────────────────────────────────────────────

        private void RegisterMinimapBlip()
        {
#if SWEF_MINIMAP_AVAILABLE
            if (MinimapManager.Instance == null) return;
            _minimapBlipId = $"disaster_{data.disasterId}_{GetInstanceID()}";
            var blip = new MinimapBlip
            {
                blipId        = _minimapBlipId,
                iconType      = MinimapIconType.DangerZone,
                worldPosition = epicenter,
                label         = data.disasterName,
                color         = GetSeverityColor(),
                isActive      = true,
                isPulsing     = true
            };
            MinimapManager.Instance.RegisterBlip(blip);
#endif
        }

        private void UnregisterMinimapBlip()
        {
#if SWEF_MINIMAP_AVAILABLE
            if (MinimapManager.Instance == null || string.IsNullOrEmpty(_minimapBlipId)) return;
            MinimapManager.Instance.UnregisterBlip(_minimapBlipId);
#endif
        }

        private Color GetSeverityColor()
        {
            switch (currentSeverity)
            {
                case DisasterSeverity.Minor:        return Color.green;
                case DisasterSeverity.Moderate:     return Color.yellow;
                case DisasterSeverity.Severe:       return new Color(1f, 0.5f, 0f);  // orange
                case DisasterSeverity.Catastrophic: return Color.red;
                case DisasterSeverity.Apocalyptic:  return new Color(0.6f, 0f, 1f);  // purple
                default:                            return Color.white;
            }
        }

        // ── End ───────────────────────────────────────────────────────────────────

        private void EndDisaster()
        {
            foreach (HazardZone zone in hazardZones)
                zone.isActive = false;

            OnDisasterEnded?.Invoke(this);
            DisasterManager.Instance?.EndDisaster(this);
        }
    }
}
