# Phase 50 — Ocean & Water Rendering System

Namespace: `SWEF.Ocean`  
Directory: `Assets/SWEF/Scripts/Ocean/`

---

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        SWEF.Ocean  (Phase 50)                           │
│                                                                         │
│   ┌──────────────────┐      registers       ┌─────────────────────┐    │
│   │  OceanData.cs    │ ──────────────────► │  OceanManager.cs    │    │
│   │  (Data classes,  │                      │  (Singleton, wave   │    │
│   │   enums, structs)│                      │   time, registry,   │    │
│   └──────────────────┘                      │   height queries)   │    │
│                                             └──────┬──────────────┘    │
│                          ┌──────────────────────────┤                  │
│                          │                          │                  │
│              ┌───────────▼──────┐    ┌──────────────▼──────────┐      │
│              │ WaveSimulator.cs │    │ OceanTileRenderer.cs    │      │
│              │ (Gerstner, 8     │    │ (NxN tiled grid mesh,   │      │
│              │  octaves, wind → │    │  LOD, frustum cull,     │      │
│              │  Beaufort)       │    │  shader upload)         │      │
│              └────────┬─────────┘    └─────────────────────────┘      │
│                       │                                                │
│       ┌───────────────┼──────────────────────────────────────┐        │
│       │               │                                      │        │
│  ┌────▼──────┐  ┌──────▼──────────────┐  ┌──────────────────▼──┐     │
│  │Shoreline  │  │ WaterReflection     │  │ Underwater          │     │
│  │Renderer   │  │ Controller.cs       │  │ EffectController.cs │     │
│  │.cs        │  │ (Planar/CubeMap)    │  │ (Fog, caustics,     │     │
│  │(Foam,     │  │                     │  │  bubble, transition)│     │
│  │ wet sand) │  └─────────────────────┘  └─────────────────────┘     │
│  └───────────┘                                                         │
│                                                                         │
│  ┌──────────────────┐    ┌─────────────────────┐                       │
│  │ RiverFlow        │    │ WaterInteraction    │                       │
│  │ Controller.cs    │    │ Handler.cs          │                       │
│  │ (Spline mesh,    │    │ (Splash, wake,      │                       │
│  │  UV scroll,      │    │  ripple, buoyancy,  │                       │
│  │  waterfall)      │    │  aircraft landing)  │                       │
│  └──────────────────┘    └─────────────────────┘                       │
│                                                                         │
│  ┌────────────────────────────────────────────────────┐                │
│  │           OceanWeatherIntegrator.cs                │                │
│  │  (Weather→waves, rain ripple, fog, lightning,      │                │
│  │   time-of-day tint, ice blend, sea-state lerp)     │                │
│  └────────────────────────────────────────────────────┘                │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Dependencies on Other SWEF Systems

| System | Integration |
|--------|-------------|
| `SWEF.Weather` | `OceanWeatherIntegrator` reads `WeatherManager.CurrentWeather` and `CurrentWind` — guarded by `#if SWEF_WEATHER_AVAILABLE` |
| `SWEF.Terrain` | `ShorelineRenderer` can reference `Renderer[]` components on terrain chunks for wet-sand darkening |
| `SWEF.TimeOfDay` | `WaterReflectionController` and `OceanWeatherIntegrator` consume time-of-day data (falls back to `Time.time` if unavailable) |
| `SWEF.Atmosphere` | Fog density can optionally be read from `SWEF.Atmosphere` for underwater visibility |
| `SWEF.Performance` | Quality managed through `OceanManager.SetQuality()` — feed from `PerformanceProfiler` |

---

## Required GameObjects & Component Configuration

### Minimal Setup (Ocean)

1. **OceanSystem** _(persistent, `DontDestroyOnLoad`)_
   - `OceanManager` — wire `trackedCamera` (defaults to `Camera.main`)
   - `WaveSimulator` — configure base `WaveParameters`

2. **OceanSurface** _(scene root)_
   - `OceanTileRenderer` — assign `oceanMaterial`, set `gridRadius` and `tileSize`
   - `WaterReflectionController` — assign `waterMaterial`, set `reflectionTextureSize`

3. **OceanEffects** _(scene root)_
   - `UnderwaterEffectController` — assign `bubbleParticles` and optional `globalUnderwaterMaterial`
   - `WaterInteractionHandler` — assign `splashPrefab`, `oceanSurfaceMaterial`

4. **OceanWeatherIntegrator** _(scene root or OceanSystem child)_
   - Assign `oceanMaterial`, wire `OceanManager` and `WaveSimulator`
   - Define `seaStates` array or leave defaults

### River Setup

1. Create a **RiverBody** GameObject with `RiverFlowController`.
2. Add at least 2 child Transforms as `controlPoints`.
3. Assign `riverMaterial` and call `RebuildMesh()` at Start or via context-menu.
4. Register with `OceanManager.RegisterWaterBody()` using a `WaterBodyDefinition` with `bodyType = River`.

### Shoreline Setup

1. Add `ShorelineRenderer` to your ocean-body GameObject or a dedicated shoreline ring.
2. Assign `shorelineMaterial` and optionally `terrainRenderers` (terrain mesh renderers nearby).
3. Wire `beachSplashPrefab` for optional splash particles.

---

## Performance Tuning Guide

### Wave Quality Levels

| Level | Octaves | Best For |
|-------|---------|----------|
| `Low` | 1 | Mobile / very low-end |
| `Medium` | 2 | Mobile mid-range |
| `High` | 4 | PC / console (default) |
| `Ultra` | 8 | High-end PC cinematics |

Change at runtime:
```csharp
OceanManager.Instance.SetQuality(WaveQuality.Medium, ReflectionMode.None);
```

### Reflection Modes

| Mode | Cost | Quality |
|------|------|---------|
| `None` | Free | No reflection |
| `PlanarSimple` | Low (256×256) | Good for most views |
| `PlanarHQ` | Medium (2048×2048) | Cinema quality |
| `ScreenSpace` | GPU-dependent | Requires SSR pipeline |
| `CubeMap` | Near-free | Fallback for low-end |

### Tile Grid Size

- `gridRadius = 3` (3×3 = 9 tiles) — mobile
- `gridRadius = 5` (5×5 = 25 tiles) — default
- `gridRadius = 7` (7×7 = 49 tiles) — very wide views

### Reflection Update Frequency

Set `updateEveryNFrames` on `WaterReflectionController`:
- `0` = every frame (expensive)
- `2` = every other frame (good balance)
- `4` = quarter-rate (mobile)

---

## Integration Points

### Weather System

Enable `SWEF_WEATHER_AVAILABLE` scripting define to activate the full weather bridge:

```
Project Settings → Player → Scripting Define Symbols → add SWEF_WEATHER_AVAILABLE
```

Without the define, the integrator runs in standalone mode (no weather reading, no sea-state changes).

### TimeOfDay System

Wire `timeOfDayWaterTint` gradient on `OceanWeatherIntegrator` and `timeOfDayTint` on
`WaterReflectionController`. Replace the `Time.time % 86400` fallback with
`SWEF.TimeOfDay.TimeOfDayManager.Instance.NormalisedHour` when available.

### Audio System

`UnderwaterEffectController.AudioMuffleActive` is `true` when the camera is submerged.
Poll this flag in your Audio Manager to reduce high-frequency content.

`RiverFlowController.CurrentTurbulence` (0–1) can drive river audio mix (calm ↔ rapids).

### Aircraft / Flight System

```csharp
bool gearOnWater = waterInteractionHandler.IsAircraftGearOnWater(gearTipWorldPos);
if (gearOnWater)
    TriggerWaterLanding();
```
