# Phase 74 — Water Interaction & Buoyancy System

**Namespace:** `SWEF.Water`  
**Directory:** `Assets/SWEF/Scripts/Water/`

---

## Overview

A comprehensive water interaction system that handles realistic wave simulation, physics-based buoyancy for water landings and ditching, splash visual and audio effects, underwater camera transitions with zone-based fog and lighting, and surface ripple generation from aircraft proximity and impacts.

---

## Architecture

```
WaterSurfaceManager (Singleton, DontDestroyOnLoad)
    │
    ├─ WaterConfig              ← Serializable configuration (wave, buoyancy, fog, ripple params)
    ├─ Gerstner wave model      ← Multi-octave, wind-driven amplitude
    ├─ IsOverWater / GetWaterHeight / GetSurfaceNormal / DetectWaterBodyType
    └─ Events: OnWaterDetected / OnWaterLost / OnWaveStateChanged
    │
BuoyancyController (per-aircraft)
    │
    ├─ BuoyancyState            ← Live contact state machine snapshot
    ├─ Archimedes buoyancy + water drag + wave rocking torque + angular damping
    ├─ InitiateDitching()       ← Controlled water landing sequence
    ├─ Damage integration       ← Impact + ingress via DamageModel (null-safe)
    └─ Events: OnWaterContact / OnStateChanged / OnDitchingComplete / OnSinking
    │
SplashEffectController
    │
    ├─ Pooled ParticleSystem    ← One pool queue per SplashType
    ├─ TrailRenderer wake       ← Width proportional to speed
    ├─ AudioManager bridge      ← Splash clips + continuous water rush (null-safe)
    └─ Events: OnSplashTriggered / OnWakeStarted / OnWakeStopped
    │
UnderwaterCameraTransition
    │
    ├─ Per-frame camera Y check ← vs WaterSurfaceManager.GetWaterHeight
    ├─ UnderwaterZone by depth  ← Surface / Shallow / Mid / Deep / Abyss
    ├─ Smooth fog + lighting    ← RenderSettings transitioned per zone
    ├─ Caustics overlay         ← Shown in Surface + Shallow zones only
    ├─ Bubble particles         ← Active while submerged
    └─ Events: OnSubmerged / OnSurfaced / OnZoneChanged
    │
WaterRippleSystem
    │
    ├─ Pooled RippleInstance    ← Max 20 simultaneous rings
    ├─ LineRenderer rings       ← Expanding + fading per ripple
    ├─ Sources: flyover, splash contact, floating idle
    └─ SpawnRipple / ClearAllRipples
    │
WaterInteractionAnalytics → SWEF.Analytics.UserBehaviorTracker
    ├─ water_contact / water_ditching / water_skim_duration
    ├─ water_submersion / water_floating_duration
    ├─ water_body_type_distribution / water_splash_count
    └─ water_photo_underwater (call TrackUnderwaterPhoto())
```

---

## Scripts

### 1. `WaterData.cs`
Pure data layer — no MonoBehaviour dependencies.

| Type | Kind | Description |
|------|------|-------------|
| `WaterBodyType` | enum | Ocean / Sea / Lake / River / Pond / Reservoir / Unknown |
| `WaterContactState` | enum | Airborne / Skimming / Touching / Floating / Sinking / Submerged / Ditching |
| `SplashType` | enum | LightSpray / MediumSplash / HeavySplash / Touchdown / Skip / DiveEntry / BellyFlop / WakeTrail |
| `UnderwaterZone` | enum | Surface / Shallow / Mid / Deep / Abyss |
| `WaterConfig` | serializable class | 19 tuneable fields covering waves, buoyancy, ripples, fog, depth colours |
| `WaterSurfaceState` | serializable class | Per-frame water surface snapshot at a sampled world position |
| `BuoyancyState` | serializable class | Live buoyancy/contact state including depth, forces, time counters |
| `SplashEvent` | serializable class | Immutable event payload: type, position, velocity, impact force, timestamp |

---

### 2. `WaterSurfaceManager.cs`
Central singleton. `DontDestroyOnLoad`. `[DisallowMultipleComponent]`.

**Responsibilities:**
- Advance wave phase each frame (`_time += deltaTime`)
- Multi-octave Gerstner wave height / normal sampling
- Per-frame water detection at player position (null-safe `FlightController`)
- Wind-driven wave amplitude multiplier (null-safe `WeatherManager`)
- Fire `OnWaterDetected` / `OnWaterLost` on area transitions

**Public API:**
```csharp
WaterSurfaceManager.Instance.IsOverWater(Vector3 worldPos) → bool
WaterSurfaceManager.Instance.GetWaterHeight(Vector3 worldPos) → float
WaterSurfaceManager.Instance.GetSurfaceNormal(Vector3 worldPos) → Vector3
WaterSurfaceManager.Instance.DetectWaterBodyType(Vector3 worldPos) → WaterBodyType
WaterSurfaceManager.Instance.Config → WaterConfig
WaterSurfaceManager.Instance.CurrentState → WaterSurfaceState
```

---

### 3. `BuoyancyController.cs`
Per-aircraft `[RequireComponent(typeof(Rigidbody))]`. `[DisallowMultipleComponent]`.

**State machine transitions:**
```
Airborne ──(altitude < skimThreshold)──→ Skimming
Skimming ──(depth > 0)──────────────────→ Touching
Touching ──(slow + shallow angle)────────→ Floating
Touching ──(fast or steep)───────────────→ Sinking
Floating ──(CanFloat() = false)──────────→ Sinking
Sinking ──(depth > halfLength×2)─────────→ Submerged
Ditching ──(speed < threshold, near surf)→ Floating + OnDitchingComplete
```

**Forces applied in `FixedUpdate`:**
- Archimedes upward buoyancy ∝ submersion fraction × density
- Water drag opposes velocity ∝ submersion fraction
- Wave rocking torque from front/back normal difference
- Angular damping via `angularVelocity` lerp
- Passive sink force when `Sinking`

**Public API:**
```csharp
controller.InitiateDitching()
controller.GetSubmersionPercent() → float   // 0–1
controller.CanFloat() → bool
controller.State → BuoyancyState
```

---

### 4. `SplashEffectController.cs`
Subscribes to `BuoyancyController.OnWaterContact` and `OnStateChanged`.

**Features:**
- Pre-allocated `Queue<ParticleSystem>` pool per `SplashType`
- Velocity-aligned spray direction, force-scaled particle size
- `TrailRenderer` wake — width `Lerp(min, max, speed/maxSpeed)`
- `AudioManager` calls: `PlayOneShot("SplashMedium", pos)` / `PlayLoop("WaterRush")`
- Coroutine-based camera shake using `Camera.main`
- Splash cooldown respects `WaterConfig.splashCooldown`

---

### 5. `UnderwaterCameraTransition.cs`
`[DisallowMultipleComponent]`. Per-frame camera Y vs water height.

**Zone thresholds (configurable):**
| Zone | Default Depth |
|------|--------------|
| Surface | 0 m (blend zone ±0.5 m) |
| Shallow | 0–10 m |
| Mid | 10–50 m |
| Deep | 50–200 m |
| Abyss | 200 m+ |

**Fog per zone:** Colours and densities are independently configurable in the Inspector. Transitions are smoothed with `Mathf.Lerp` over `transitionDuration` seconds.

**Properties:**
```csharp
bool IsUnderwater { get; }
float CurrentDepth { get; }
UnderwaterZone CurrentZone { get; }
```

---

### 6. `WaterRippleSystem.cs`
Pooled `List<RippleInstance>` with companion `List<LineRenderer>` ring renderers.

**Ripple sources:**
| Source | Condition | Spawn rate |
|--------|-----------|------------|
| Flyover | `Skimming` + alt < skimThreshold | `flyoverSpawnInterval` (0.5 s default) |
| Splash contact | `OnWaterContact` event | Immediate, one per event |
| Floating idle | `Floating` state | `floatRippleInterval` (1.5 s default) |

**Per-frame update per ripple:**
```
radius += expansionSpeed × deltaTime
intensity = 1 − elapsed / lifetime
→ disabled when elapsed ≥ lifetime OR radius ≥ rippleMaxRadius
```

**Public API:**
```csharp
rippleSystem.SpawnRipple(Vector3 position, float intensity, float expansionSpeed)
rippleSystem.ClearAllRipples()
rippleSystem.ActiveRippleCount → int
```

---

### 7. `WaterInteractionAnalytics.cs`
`[DisallowMultipleComponent]`. Null-safe `UserBehaviorTracker` bridge.

**Events tracked:**

| Event key | Trigger | Key parameters |
|-----------|---------|----------------|
| `water_contact` | First water touch per session | `splash_type`, `speed`, `impact_force`, position |
| `water_ditching` | `OnDitchingComplete` | `success`, `speed` |
| `water_sinking` | `Sinking` state transition | `submersion_depth`, `time_in_water` |
| `water_submersion` | `OnSurfaced` after submerged | `max_depth`, `duration` |
| `water_skim_duration` | Session end (OnDestroy) | `total_seconds` |
| `water_floating_duration` | Session end | `total_seconds` |
| `water_body_type_distribution` | Session end | time per body type |
| `water_splash_count` | Session end | count per `SplashType` |
| `water_photo_underwater` | `TrackUnderwaterPhoto()` | `depth`, `zone` |

---

## Integration Points

| Script | Integrates With |
|--------|----------------|
| `WaterSurfaceManager` | `SWEF.Flight.FlightController` — player world position (null-safe) |
| `WaterSurfaceManager` | `SWEF.Weather.WeatherManager` — wind speed → wave amplitude multiplier (null-safe) |
| `BuoyancyController` | `SWEF.Damage.DamageModel` — `ApplyDamage()` on impact and water ingress (null-safe) |
| `BuoyancyController` | `SWEF.Landing.LandingDetector` — referenced for ditching touchdown confirmation (null-safe) |
| `SplashEffectController` | `SWEF.Audio.AudioManager` — splash one-shots + water rush loop (null-safe) |
| `SplashEffectController` | `SWEF.Contrail.ContrailManager` — wake trail rendering pipeline (null-safe) |
| `UnderwaterCameraTransition` | `SWEF.Audio.AudioManager` — 800 Hz low-pass filter when submerged (null-safe) |
| `WaterRippleSystem` | `BuoyancyController` — subscribes to `OnWaterContact` for contact ripples |
| `WaterRippleSystem` | `WaterSurfaceManager` — subscribes to `OnWaterDetected` for area ripples |
| `WaterInteractionAnalytics` | `SWEF.Analytics.UserBehaviorTracker` — all telemetry events (null-safe) |

---

## Localization Keys (31 keys added in Phase 74)

| Key | English |
|-----|---------|
| `water_body_ocean` … `water_body_unknown` | Ocean, Sea, Lake, River, Pond, Reservoir, Unknown Water |
| `water_state_airborne` … `water_state_ditching` | Airborne, Skimming, Touching, Floating, Sinking, Submerged, Ditching |
| `water_splash_light` … `water_splash_wake` | Light Spray, Splash, Heavy Splash, Touchdown, Skip, Dive Entry, Belly Flop, Wake Trail |
| `water_zone_surface` … `water_zone_abyss` | Surface, Shallow, Mid Water, Deep Water, Abyss |
| `water_hud_depth` | Depth |
| `water_hud_buoyancy` | Buoyancy |
| `water_hud_warning_sinking` | WARNING: SINKING |
| `water_hud_ditching` | DITCHING |

All 31 keys are present in all 8 language files: `lang_en`, `lang_de`, `lang_es`, `lang_fr`, `lang_ja`, `lang_ko`, `lang_pt`, `lang_zh`.
