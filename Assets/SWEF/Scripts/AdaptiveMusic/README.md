# SWEF Adaptive Music System — Phase 83

**Namespace:** `SWEF.AdaptiveMusic`  
**Directory:** `Assets/SWEF/Scripts/AdaptiveMusic/`

## Overview

The Adaptive Music System dynamically mixes audio stems in real-time based on flight state. Music reacts to altitude, speed, weather, time of day, danger level, biome, and mission context — creating a unique soundtrack for every flight.

## Architecture

```
FlightContextAnalyzer  →  MoodResolver  →  AdaptiveMusicManager
         ↓                                        ↓
  FlightMusicContext             MusicTransitionController
                                         ↓
                               StemMixer (AudioSource pool)
                                         ↓
                               IntensityController
                                         ↓
                               BeatSyncClock (BPM clock)
```

## Scripts

| Script | Purpose |
|--------|---------|
| `AdaptiveMusicData.cs` | Data layer — enums, structs, `StemDefinition`, `AdaptiveMusicProfile` ScriptableObject, `FlightMusicContext` struct |
| `AdaptiveMusicManager.cs` | Singleton orchestrator — polls context, resolves mood/intensity, manages events, PlayerPrefs persistence |
| `FlightContextAnalyzer.cs` | Builds `FlightMusicContext` each tick by null-safely sampling flight/weather/mission systems |
| `MoodResolver.cs` | Static utility — `ResolveMood()` and `ResolveIntensity()` using 10-priority rule set |
| `StemMixer.cs` | AudioSource pool — activate/deactivate/crossfade stems, ducking, master volume |
| `MusicTransitionController.cs` | Mood transitions — configurable crossfade durations, minimum mood duration queue, bar-quantized timing, stingers |
| `IntensityController.cs` | Maps intensity (0–1) to active stem layers, per-layer volume curves |
| `BeatSyncClock.cs` | Master BPM clock — beat/bar/downbeat events, `GetNextBarTime()` for DSP scheduling |
| `AdaptiveMusicHUD.cs` | HUD panel — mood label, intensity gradient bar, per-layer dots, override slider |
| `AdaptiveMusicUI.cs` | Settings panel — enable toggle, volume/crossfade/sensitivity sliders, mode dropdown |
| `MusicPlayerBridge.cs` | Bridges with existing `MusicPlayer` subsystem via reflection (no compile-time dependency) |
| `AdaptiveMusicAnalytics.cs` | Telemetry events to `TelemetryDispatcher` (null-safe) |

## Mood Priority Rules

| Priority | Condition | Mood |
|----------|-----------|------|
| 1 | `isInCombatZone` OR `dangerLevel ≥ 1` OR `damageLevel ≥ 0.6` | Danger |
| 2 | `gForce ≥ 3.0` OR `stallWarning` | Tense |
| 3 | `weatherIntensity ≥ 0.7` | Tense |
| 4 | `altitude ≥ 100 km` OR `isInSpace` | Epic |
| 5 | `missionJustCompleted` | Triumphant |
| 6 | `sunAltitudeDeg` in 0–6° | Serene |
| 7 | Night (≥20:00 or <05:00) + clear | Mysterious |
| 8 | `speed ≥ ~Mach 0.8` (272 m/s) | Adventurous |
| 9 | Stable cruise (alt ≥ 500m, speed ≥ 50 m/s, stable G) | Cruising |
| 10 | Default | Peaceful |

## Intensity → Layer Mapping

| Intensity Range | Active Layers |
|----------------|---------------|
| 0.0–0.2 | Pads |
| 0.2–0.4 | + Strings |
| 0.4–0.6 | + Melody, Bass |
| 0.6–0.8 | + Drums, Percussion |
| 0.8–1.0 | + Choir, Synth (all 8 layers) |

## MusicPlayerBridge Modes

| Mode | Behaviour |
|------|-----------|
| `AdaptiveOnly` | Only adaptive stems play; user playlist is paused |
| `PlaylistOnly` | Only user's playlist plays; adaptive music is paused |
| `Hybrid` | Both play; adaptive is ducked when user playlist is active |

## Integration Points

All external integrations are guarded by `#if` preprocessor directives and null checks, so the system compiles and runs even when optional subsystems are absent.

| Dependency | Guard | Notes |
|-----------|-------|-------|
| `FlightController`, `AltitudeController`, `FlightPhysicsIntegrator` | `SWEF_FLIGHT_AVAILABLE` | Speed, altitude, G-force, stall |
| `WeatherManager` | `SWEF_WEATHER_AVAILABLE` | Weather intensity |
| `TimeOfDayManager` | `SWEF_TIMEOFDAY_AVAILABLE` | Hour of day, sun altitude |
| `BiomeClassifier` | `SWEF_BIOME_AVAILABLE` | Current biome ID |
| `TransportMissionManager` | `SWEF_PASSENGERCARGO_AVAILABLE` | Mission active/completed |
| `DamageModel` | `SWEF_DAMAGE_AVAILABLE` | Hull damage level |
| `EmergencyManager` | `SWEF_EMERGENCY_AVAILABLE` | Active emergencies, combat zone |
| `TelemetryDispatcher` | `SWEF_ANALYTICS_AVAILABLE` | Telemetry events |
| `MusicPlayerManager` | Reflection (no flag needed) | User playlist interop |

## Setup

1. Create an `AdaptiveMusicProfile` asset (`Assets → Create → SWEF → Adaptive Music Profile`).
2. Add the `AdaptiveMusicManager` prefab to your scene and wire up references in the Inspector.
3. Add stem `AudioClip` assets under `Resources/Music/Stems/` and register them in the profile.
4. (Optional) Add `AdaptiveMusicHUD` to your flight HUD canvas.

## Localization Keys

All 25 keys have the prefix `music_` and are defined in all 8 language files under `Assets/SWEF/Resources/Localization/`.
