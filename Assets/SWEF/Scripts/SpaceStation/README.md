# SWEF Space Station & Orbital Docking System — Phase 85

**Namespace:** `SWEF.SpaceStation`  
**Directory:** `Assets/SWEF/Scripts/SpaceStation/`

## Overview

Phase 85 adds a fully featured space station and orbital docking system.  
Players can fly to the upper atmosphere, locate stations on Keplerian orbits, execute a 6-phase docking maneuver using RCS thrusters, and explore station interiors in zero-gravity.

## Architecture

```
OrbitalMechanicsController   ←── Keplerian orbit propagation (2-body)
         │
         ▼
StationSpawnManager          ←── LOD-based spawn (icon / low-poly / full)
         │
         ▼
DockingController            ←── 6-phase docking state machine
    ├── RCSController        ←── 6-DOF reaction control
    └── DockingGuidanceHUD   ←── Real-time approach HUD

         ▼ (phase == Docked)
StationInteriorController    ←── Zero-G interior movement
         │
         ▼
StationModuleGenerator       ←── Procedural station layout graph

  ─── Cross-cutting ────────────────────────────────
  SpaceStationUI          — Station info, port list, dock/undock buttons
  SpaceStationMinimap     — Orbital icons + interior map (#if SWEF_MINIMAP_AVAILABLE)
  SpaceStationAchievements— Achievement triggers      (#if SWEF_ACHIEVEMENT_AVAILABLE)
  SpaceStationAnalytics   — Telemetry events          (#if SWEF_ANALYTICS_AVAILABLE)
```

## Scripts

| Script | Purpose |
|--------|---------|
| `SpaceStationData.cs` | `OrbitalBody` (6), `DockingPortState` (4), `StationSegmentType` (8), `DockingApproachPhase` (6) enums; `OrbitalParameters` struct; `StationDefinition`, `DockingPortDefinition` classes; `SpaceStationConfig` ScriptableObject |
| `OrbitalMechanicsController.cs` | Singleton — Keplerian orbit propagation; `GetStationPosition()`, `GetOrbitalVelocity()`, `GetRelativeVelocity()`; `OnStationInRange` event |
| `StationSpawnManager.cs` | Singleton — altitude-based LOD spawn/despawn; `GetNearestStation()`, `GetStationsInRange()` |
| `DockingController.cs` | Singleton — 6-phase docking state machine; `BeginDockingApproach()`, `Tick()`, `Abort()`, `Undock()`; events |
| `RCSController.cs` | 6-DOF RCS; `Translate()`, `Rotate()`, `ProcessInput()`; fuel consumption via `#if SWEF_FUEL_AVAILABLE` |
| `DockingGuidanceHUD.cs` | Approach HUD — crosshair, distance, closing speed, deviation indicators, corridor colour |
| `StationInteriorController.cs` | Interior mode — zero-G movement, push-off, hatch navigation; `EnterStation()`, `ExitStation()` |
| `StationModuleGenerator.cs` | Static — `GenerateLayout()` → `StationLayout` (graph of `StationSegmentNode`); deterministic seed |
| `SpaceStationUI.cs` | Station panel — info, port list, approach/undock buttons, catalogue |
| `SpaceStationMinimap.cs` | Orbital icons + approach corridor + interior map |
| `SpaceStationAchievements.cs` | Achievements: first dock, speed dock, zero-damage, visit all, redock, max altitude |
| `SpaceStationAnalytics.cs` | Telemetry: approach started, phase changed, docking complete/aborted, interior enter/exit, RCS fuel, session summary |

## Docking Sequence

```
FreeApproach (>1000 m)
    │  distance ≤ 1000 m
    ▼
InitialAlignment (200–1000 m)  ← abort: speed > 50 m/s
    │  distance ≤ 200 m  AND  alignment ≤ 15°
    ▼
FinalApproach (10–200 m)       ← abort: speed > 10 m/s OR alignment > 15°
    │  distance ≤ 10 m  AND  speed ≤ 5 m/s  AND  alignment ≤ 5°
    ▼
SoftCapture (<10 m)            ← abort: collision speed > 2 m/s
    │  distance ≤ 1 m  AND  speed ≤ 0.5 m/s  AND  alignment ≤ 2°
    ▼
HardDock (<1 m)  [2-second auto-lock timer]
    │  timer ≥ 2 s
    ▼
Docked  →  EnterStation()  →  StationInteriorController
```

## RCS Control Mapping

| Input | Action |
|-------|--------|
| Translate X+ / X− | Strafe right / left |
| Translate Y+ / Y− | Translate up / down |
| Translate Z+ / Z− | Translate forward / backward |
| Rotate X (pitch) | Pitch up / down |
| Rotate Y (yaw) | Yaw left / right |
| Rotate Z (roll) | Roll clockwise / counter-clockwise |

## Integration Table

| Script | Integrates With |
|--------|----------------|
| `RCSController` | `FuelManager` — fuel consumption (`#if SWEF_FUEL_AVAILABLE`) |
| `SpaceStationMinimap` | `MinimapManager` — orbital markers (`#if SWEF_MINIMAP_AVAILABLE`) |
| `SpaceStationAchievements` | `AchievementManager` (`#if SWEF_ACHIEVEMENT_AVAILABLE`) |
| `SpaceStationAnalytics` | `TelemetryDispatcher` (`#if SWEF_ANALYTICS_AVAILABLE`) |
| `StationInteriorController` | `DockingController` — undock on exit |
| `DockingGuidanceHUD` | `DockingController` — phase/abort events |

## Localization

40 keys across 8 languages (`station_body_*` × 6, `station_segment_*` × 8, `station_dock_phase_*` × 6, `station_port_*` × 4, UI/HUD keys × 16).
