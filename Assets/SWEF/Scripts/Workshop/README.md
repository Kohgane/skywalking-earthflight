# Phase 90 — Aircraft Workshop & Part Customization

**Namespace:** `SWEF.Workshop`  
**Directory:** `Assets/SWEF/Scripts/Workshop/`

---

## Overview

A comprehensive aircraft customisation system.  Players browse an unlock tree of
engine, wing, fuselage, and other component parts, equip them into named build
presets, and see the performance impact in real time via the simulation layer.
A paint/livery editor and decal placement tool let players personalise the visual
appearance, and a share system lets them export/import builds as Base-64 strings
to share with friends.

---

## Scripts

| # | File | Purpose |
|---|------|---------|
| 1 | `AircraftPartType.cs` | Enum: Engine, Wing, Fuselage, Tail, LandingGear, Aileron, Rudder, Elevator, Cockpit, Propeller, Intake, Exhaust, FuelTank |
| 2 | `PartTier.cs` | Enum: Common/Uncommon/Rare/Epic/Legendary with `ToColorHex()` + `ToLocKey()` extension methods |
| 3 | `AircraftPartData.cs` | `[Serializable]` data record — partId, partName, partType, tier, weight, dragCoefficient, liftModifier, thrustModifier, durability, description, iconPath, unlockRequirement, isUnlocked |
| 4 | `AircraftBuildData.cs` | `[Serializable]` complete build/loadout — equippedPartIds, paintScheme, decals (max 10), cached stat fields |
| 5 | `PaintSchemeData.cs` | `[Serializable]` livery data — primaryColor, secondaryColor, accentColor, metallic, roughness, pattern, customPatternPath |
| 6 | `DecalData.cs` | `[Serializable]` decal placement — texturePath, uvPosition, rotation, scale, layerIndex |
| 7 | `WorkshopManager.cs` | Singleton (DontDestroyOnLoad): OpenWorkshop, CloseWorkshop, SaveBuild, LoadBuildById, EquipPart, UnequipPartByType, ApplyActiveBuild, JSON persistence to `workshop_builds.json` |
| 8 | `PartInventoryController.cs` | Singleton (DontDestroyOnLoad): AddPart, RemovePart, HasPart, GetPartById, GetPartsByType, GetPartsByTier, GetAllParts, JSON persistence to `workshop_inventory.json` |
| 9 | `PartUnlockTree.cs` | Singleton (DontDestroyOnLoad): CanUnlock, UnlockPart, GetUnlockProgress, GetNextUnlockable — level / currency / achievement / mission / prerequisite gates |
| 10 | `PerformanceSimulator.cs` | Static utility — ComputeMaxSpeed, ComputeClimbRate, ComputeManeuverability, ComputeFuelEfficiency, ComputeStructuralIntegrity, ComputeWeightBalance, CompareBuilds |
| 11 | `PaintEditorController.cs` | MonoBehaviour — SetPrimaryColor, SetSecondaryColor, SetAccentColor, SetMetallic, SetRoughness, SetPattern, ApplyPaintScheme, ResetToDefault, SaveScheme, LoadScheme |
| 12 | `DecalEditorController.cs` | MonoBehaviour — AddDecal, RemoveDecal, MoveDecal, RotateDecal, ScaleDecal, SelectDecal, SetDecalLayer; max 10 decals per build |
| 13 | `AircraftShareManager.cs` | Static — ExportBuild → Base-64 string, ImportBuild(string) → AircraftBuildData, ShareBuild (clipboard + social), ValidateImportedBuild |
| 14 | `WorkshopBridge.cs` | MonoBehaviour integration bridge → ProgressionManager, AchievementManager, SocialActivityFeed |
| 15 | `WorkshopAnalytics.cs` | Static telemetry helper — 11 event types guarded by `#if SWEF_ANALYTICS_AVAILABLE` |

---

## Architecture

```
WorkshopManager (Singleton, DontDestroyOnLoad)
│   ├── OpenWorkshop / CloseWorkshop
│   ├── EquipPart / UnequipPartByType
│   ├── SaveBuild / LoadBuildById / GetAllBuilds
│   ├── ApplyActiveBuild   →  RefreshCachedStats
│   │                             ↓
│   │                      PerformanceSimulator (static)
│   │                             ↓
│   │                      AircraftBuildData.cached*
│   └── JSON persistence: workshop_builds.json
│
PartInventoryController (Singleton, DontDestroyOnLoad)
│   ├── AddPart / RemovePart / HasPart / GetPartById
│   ├── GetPartsByType / GetPartsByTier / GetAllParts
│   └── JSON persistence: workshop_inventory.json
│
PartUnlockTree (Singleton, DontDestroyOnLoad)
│   ├── CanUnlock  (level + currency + achievements + missions + prerequisites)
│   ├── UnlockPart →  part.isUnlocked = true  →  PartInventoryController.AddPart
│   ├── GetUnlockProgress   →  fraction [0, 1] for progress bars
│   └── GetNextUnlockable   →  list of currently available unlocks
│
PerformanceSimulator (static)
│   ├── ComputeMaxSpeed           →  BaseSpeed × (thrust/drag) × weightPenalty
│   ├── ComputeClimbRate          →  BaseClimbRate × (thrust/weight) × lift
│   ├── ComputeManeuverability    →  wing+aileron+rudder+elevator / weight
│   ├── ComputeFuelEfficiency     →  tankBonus / thrustRatio
│   ├── ComputeStructuralIntegrity →  Σ durability
│   ├── ComputeWeightBalance      →  nose/tail mass ratio → score [0, 1]
│   └── CompareBuilds             →  BuildComparison (buildB − buildA deltas)
│
PaintEditorController (MonoBehaviour)
│   ├── Live material preview via Renderer.material shader properties
│   └── ApplyPaintScheme  →  AircraftBuildData.paintScheme
│
DecalEditorController (MonoBehaviour)
│   ├── UV-space placement with position / rotation / scale / layer handles
│   ├── Max 10 decals enforced (MaxDecals constant)
│   └── SyncToActiveBuild  →  AircraftBuildData.decals
│
AircraftShareManager (static)
│   ├── ExportBuild  →  JsonUtility.ToJson  →  Base-64 string
│   ├── ImportBuild  →  Base-64 decode  →  JsonUtility.FromJson  →  Validate
│   └── ShareBuild   →  clipboard copy  +  SocialActivityFeed.PostActivity
│
WorkshopBridge (MonoBehaviour)
│   ├── → ProgressionManager.AddXP  (part equip / build save / share)
│   ├── → AchievementManager.ReportProgress  (first_custom_build, legendary_collector, …)
│   └── → SocialActivityFeed.PostActivity  (build shared)
│
WorkshopAnalytics (static)
│   └── → TelemetryDispatcher.EnqueueEvent  (11 event types)
```

---

## Part Type Catalogue

| Type | Slot | Affects |
|------|------|---------|
| Engine | 1 | thrustModifier → max speed, climb rate, fuel efficiency |
| Wing | 1 | liftModifier → climb rate, maneuverability |
| Fuselage | 1 | weight, drag → all stats |
| Tail | 1 | weight balance, structural integrity |
| LandingGear | 1 | weight → ground handling (visual only in current sim) |
| Aileron | 1 | liftModifier, drag → maneuverability (roll) |
| Rudder | 1 | liftModifier, drag → maneuverability (yaw) |
| Elevator | 1 | liftModifier, drag → maneuverability (pitch) |
| Cockpit | 1 | weight → CG balance |
| Propeller | 1 | thrustModifier → speed (piston/turboprop) |
| Intake | 1 | dragCoefficient → engine efficiency |
| Exhaust | 1 | drag, weight → CG balance |
| FuelTank | 1 | fuel efficiency bonus (+20 % per extra tank) |

Each slot accepts exactly one part.  Equipping a new part of the same type
automatically replaces the previous occupant.

---

## Tier System

| Tier | Colour | Hex | Typical Stat Gain vs. Common |
|------|--------|-----|------------------------------|
| Common | Grey | `#9E9E9E` | Baseline |
| Uncommon | Green | `#4CAF50` | +10–20 % |
| Rare | Blue | `#2196F3` | +30–50 % |
| Epic | Purple | `#9C27B0` | +60–90 % |
| Legendary | Gold/Orange | `#FF9800` | +100–150 % |

Colour hex values are accessible at runtime via `tier.ToColorHex()`.
Localisation keys are returned by `tier.ToLocKey()` (prefix: `workshop_tier_`).

---

## Performance Simulation Formulas

All formulas are arcade approximations for relative build comparison.

### Max Speed (km/h)
```
maxSpeed = BaseSpeed(200) × (totalThrust / totalDrag) × (BaseWeight(500) / totalWeight)
```
- `totalThrust = BaseThrust(800) × Π(engine.thrustModifier)`
- `totalDrag   = BaseDrag(0.3) + Σ(part.dragCoefficient)`
- `totalWeight = BaseWeight(500) + Σ(part.weight)`

### Climb Rate (m/s)
```
climbRate = BaseClimbRate(5) × (thrust / totalWeight) × Π(wing.liftModifier)
```

### Maneuverability [0, 1]
```
controlScore = Σ(wing|aileron|rudder|elevator: liftModifier × (1 − dragCoeff))
maneuverability = clamp(controlScore / (totalWeight / BaseWeight) × 0.5, 0, 1)
```

### Fuel Efficiency [0, 1]
```
tankBonus  = 1 + 0.2 × (fuelTankCount)
thrustRatio = Π(engine.thrustModifier)
fuelEfficiency = clamp(tankBonus / thrustRatio, 0, 1)
```

### Structural Integrity (durability units)
```
integrity = Σ(part.durability)
```

### Weight Balance [0, 1]
```
noseWeight = Σ(Engine + Cockpit + Intake + Propeller: weight)
tailWeight = Σ(Tail + Elevator + Rudder + Exhaust: weight)
ratio      = noseWeight / (noseWeight + tailWeight)   [ideal ≈ 0.5]
balance    = 1 − clamp(|ratio − 0.5| × 4, 0, 1)
```

---

## Paint & Decal System

### Paint Schemes
- Three colour channels: Primary, Secondary, Accent.
- PBR Metallic [0, 1] and Roughness [0, 1] sliders.
- 10 built-in pattern overlays (Solid, RacingStripe, Camo, Digital,
  Checkerboard, DiagonalStripes, Flames, CarbonFibre, RetroAirline, Custom).
- Live preview via `PaintEditorController`, which writes shader properties
  (`_PrimaryColor`, `_SecondaryColor`, `_AccentColor`, `_Metallic`, `_Roughness`)
  directly onto the preview Renderer's material.

### Decals
- Up to **10 decals** per aircraft (`DecalEditorController.MaxDecals`).
- Placed in UV space (`DecalData.uvPosition`).
- Supports per-decal rotation (degrees), uniform scale, and layer ordering.
- Position/rotation snap helpers configurable in the Inspector.

---

## Persistence Files

| File | Location | Contents |
|------|----------|----------|
| `workshop_inventory.json` | `Application.persistentDataPath` | Player's unlocked part collection |
| `workshop_builds.json` | `Application.persistentDataPath` | All saved build presets |

---

## Integration Points

| System | Integration | Guard |
|--------|-------------|-------|
| `SWEF.Progression.ProgressionManager` | `AddXP` on part equip / build save / build share | `#if SWEF_PROGRESSION_AVAILABLE` |
| `SWEF.Achievement.AchievementManager` | `ReportProgress` for `first_custom_build`, `all_parts_unlocked`, `legendary_collector`, `shared_build` | `#if SWEF_ACHIEVEMENT_AVAILABLE` |
| `SWEF.SocialHub.SocialActivityFeed` | `PostActivity("workshop_build_shared", …)` | `#if SWEF_SOCIAL_AVAILABLE` |
| `SWEF.Analytics.TelemetryDispatcher` | 11 telemetry event types via `WorkshopAnalytics` | `#if SWEF_ANALYTICS_AVAILABLE` |
| `SWEF.Damage.AircraftPart` | Enum overlap (Fuselage/Engine/etc.) — damage system reads part health | none (struct alignment) |

---

## Localization Key Prefix: `workshop_`

| Key | Usage |
|-----|-------|
| `workshop_tier_common` | Tier label — Common |
| `workshop_tier_uncommon` | Tier label — Uncommon |
| `workshop_tier_rare` | Tier label — Rare |
| `workshop_tier_epic` | Tier label — Epic |
| `workshop_tier_legendary` | Tier label — Legendary |
| `workshop_part_type_engine` | Part type label |
| `workshop_part_type_wing` | Part type label |
| `workshop_part_type_fuselage` | Part type label |
| `workshop_part_type_tail` | Part type label |
| `workshop_part_type_landinggear` | Part type label |
| `workshop_part_type_aileron` | Part type label |
| `workshop_part_type_rudder` | Part type label |
| `workshop_part_type_elevator` | Part type label |
| `workshop_part_type_cockpit` | Part type label |
| `workshop_part_type_propeller` | Part type label |
| `workshop_part_type_intake` | Part type label |
| `workshop_part_type_exhaust` | Part type label |
| `workshop_part_type_fueltank` | Part type label |
| `workshop_pattern_solid` | Paint pattern label |
| `workshop_pattern_racing_stripe` | Paint pattern label |
| `workshop_pattern_camo` | Paint pattern label |
| `workshop_pattern_digital` | Paint pattern label |
| `workshop_pattern_checkerboard` | Paint pattern label |
| `workshop_pattern_diagonal_stripes` | Paint pattern label |
| `workshop_pattern_flames` | Paint pattern label |
| `workshop_pattern_carbon_fibre` | Paint pattern label |
| `workshop_pattern_retro_airline` | Paint pattern label |
| `workshop_pattern_custom` | Paint pattern label |
| `workshop_build_saved` | Toast notification |
| `workshop_build_loaded` | Toast notification |
| `workshop_build_shared` | Toast notification |
| `workshop_build_imported` | Toast notification |
| `workshop_part_equipped` | Toast notification |
| `workshop_part_unlocked` | Toast notification |
| `workshop_decal_limit_reached` | Warning toast |
| `workshop_share_copied` | Clipboard confirmation toast |

---

## Example Usage

```csharp
// Open workshop with a specific saved build
WorkshopManager.Instance.OpenWorkshop(myBuildId);

// Equip a part
var turbofan = inventory.GetPartById("engine_turbofan_epic");
WorkshopManager.Instance.EquipPart(turbofan);

// Compute stats for the active build
float speed = PerformanceSimulator.ComputeMaxSpeed(
    WorkshopManager.Instance.ActiveBuild,
    PartInventoryController.Instance);

// Compare two builds
var delta = PerformanceSimulator.CompareBuilds(buildA, buildB, inventory);
Debug.Log($"Speed delta: {delta.maxSpeedDelta:+0.0;-0.0} km/h");

// Apply paint changes
var paint = FindFirstObjectByType<PaintEditorController>();
paint.SetPrimaryColor(Color.red);
paint.SetPattern(PaintPattern.RacingStripe);
paint.ApplyPaintScheme();

// Export and share a build
string code = AircraftShareManager.ExportBuild(WorkshopManager.Instance.ActiveBuild);
AircraftShareManager.ShareBuild();

// Import a shared code
var imported = AircraftShareManager.ImportBuild(shareCode);
if (imported != null)
    WorkshopManager.Instance.SaveBuild(imported);

// Unlock a part via the tech tree
if (PartUnlockTree.Instance.CanUnlock("wing_composite_rare", level, currency, missions, achievements, inventory))
    PartUnlockTree.Instance.UnlockPart("wing_composite_rare", inventory);
```
