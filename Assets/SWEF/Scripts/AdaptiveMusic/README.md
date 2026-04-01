# Adaptive Music System — Phase 83

**Namespace:** `SWEF.AdaptiveMusic`  
**Directory:** `Assets/SWEF/Scripts/AdaptiveMusic/`

## Overview

The Adaptive Music System dynamically mixes audio stems in real-time based on flight state. The music reacts to altitude, speed, weather, time of day, danger level, biome, and mission context — creating a unique soundtrack for every flight.

The system supports stem-based layering (Drums, Bass, Melody, Pads, Strings, Percussion, Choir, Synth), smooth crossfades between mood states, intensity scaling, and integration with the existing MusicPlayer system for user playlists alongside the adaptive soundtrack.

## Scripts

| File | Class | Purpose |
|------|-------|---------|
| `AdaptiveMusicData.cs` | Various | Pure data layer: `MusicMood` / `MusicLayer` enums, `StemDefinition`, `AdaptiveMusicProfile` ScriptableObject, `FlightMusicContext` struct |
| `AdaptiveMusicManager.cs` | `AdaptiveMusicManager` | Singleton orchestrator — polls context every 0.5s, resolves mood/intensity, manages transitions |
| `FlightContextAnalyzer.cs` | `FlightContextAnalyzer` | Samples flight systems each tick; builds `FlightMusicContext` |
| `MoodResolver.cs` | `MoodResolver` (static) | Priority-based mood resolution; intensity scaling |
| `StemMixer.cs` | `StemMixer` | AudioSource pool; fade-in / fade-out / crossfade / ducking |
| `MusicTransitionController.cs` | `MusicTransitionController` | Mood-to-mood transitions with anti-flicker hold time |
| `IntensityController.cs` | `IntensityController` | Intensity → active layers mapping with smooth volume interpolation |
| `BeatSyncClock.cs` | `BeatSyncClock` | Master BPM clock; beat/bar events; bar-quantised scheduling |
| `AdaptiveMusicHUD.cs` | `AdaptiveMusicHUD` | HUD widget: mood label, intensity bar, stem dots |
| `AdaptiveMusicUI.cs` | `AdaptiveMusicUI` | Full settings panel: enable, volume, mode, profile, preview |
| `MusicPlayerBridge.cs` | `MusicPlayerBridge` | Bridges adaptive music with `SWEF.MusicPlayer` (3 modes) |
| `AdaptiveMusicAnalytics.cs` | `AdaptiveMusicAnalytics` | Telemetry: 6 event types + session summary |

## Architecture

```
AdaptiveMusicManager (Singleton, DontDestroyOnLoad)
│   ├── Polls FlightContextAnalyzer every 0.5 s
│   ├── MoodResolver.ResolveMood(ctx)      → target MusicMood
│   ├── MoodResolver.ResolveIntensity(ctx) → target intensity (0–1)
│   ├── MusicTransitionController.RequestMood(mood)
│   ├── IntensityController.SetIntensity(intensity)
│   └── Events: OnMoodChanged / OnIntensityChanged / OnStemActivated / OnStemDeactivated
│
FlightContextAnalyzer
│   ├── FlightController      (speed, isFlying) [null-safe]
│   ├── AltitudeController    (altitude)        [null-safe]
│   ├── WeatherManager        (intensity, storm)[null-safe]
│   ├── TimeOfDayManager      (hour, sunAlt)    [null-safe]
│   ├── BiomeClassifier       (biome type)      [null-safe]
│   ├── EmergencyManager      (active emergencies) [null-safe]
│   └── DamageModel           (overall health)  [null-safe]
│
MoodResolver (static)
│   ├── Priority 1: Emergency OR damage > 60%  → Danger
│   ├── Priority 2: G-force > 3.0 / stall     → Tense
│   ├── Priority 3: Storm (weather > 0.7)      → Tense
│   ├── Priority 4: Altitude > 100 km          → Epic
│   ├── Priority 5: Mission just completed     → Triumphant
│   ├── Priority 6: Golden hour (sun 0–6°)     → Serene
│   ├── Priority 7: Night + clear weather      → Mysterious
│   ├── Priority 8: Speed > Mach 0.8           → Adventurous
│   ├── Priority 9: Smooth cruise              → Cruising
│   └── Priority 10: Low alt + calm + slow     → Peaceful
│
MusicTransitionController
│   ├── Minimum mood hold time (8 s anti-flicker)
│   ├── Per-pair crossfade durations (from AdaptiveMusicProfile)
│   ├── Optional stinger clips on specific transitions
│   └── Bar-quantised timing via BeatSyncClock
│
IntensityController
│   ├── 0.0–0.2  → Pads
│   ├── 0.2–0.4  → + Strings
│   ├── 0.4–0.6  → + Melody + Bass
│   ├── 0.6–0.8  → + Drums + Percussion
│   └── 0.8–1.0  → + Choir + Synth (all layers)
│
StemMixer
│   ├── AudioSource pool (one per layer)
│   ├── fade-in / fade-out / crossfade coroutines
│   └── Duck() / Unduck() for narration / voice chat
│
BeatSyncClock
│   ├── DSP-clock-based beat tracking
│   ├── Events: OnBeat / OnBar / OnDownbeat
│   └── GetNextBarTime() → double (for scheduling)
│
AdaptiveMusicHUD ─→ AdaptiveMusicManager (events)
AdaptiveMusicUI  ─→ AdaptiveMusicManager (settings)
MusicPlayerBridge ─→ AdaptiveMusicManager + MusicPlayerManager
AdaptiveMusicAnalytics ─→ TelemetryDispatcher
```

## Mood Resolution Priority

| Priority | Condition | Mood |
|----------|-----------|------|
| 1 | Active emergency OR damage > 60% | Danger |
| 2 | G-force > 3.0 OR stall warning | Tense |
| 3 | Storm (weather intensity > 0.7) | Tense |
| 4 | Altitude > 100,000 m (space) | Epic |
| 5 | Mission just completed (10 s window) | Triumphant |
| 6 | Golden hour (sun altitude 0–6°) | Serene |
| 7 | Night + clear weather | Mysterious |
| 8 | Speed > Mach 0.8 | Adventurous |
| 9 | Smooth cruise (stable altitude, moderate speed) | Cruising |
| 10 | Low altitude + calm weather + low speed | Peaceful |

## Intensity → Layer Mapping

| Intensity Range | Active Layers |
|----------------|---------------|
| 0.0 – 0.2 | Pads |
| 0.2 – 0.4 | Pads, Strings |
| 0.4 – 0.6 | Pads, Strings, Melody, Bass |
| 0.6 – 0.8 | Pads, Strings, Melody, Bass, Drums, Percussion |
| 0.8 – 1.0 | All layers (+ Choir, Synth) |

## Persistence (PlayerPrefs)

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `SWEF_Music_AdaptiveEnabled` | int (bool) | 1 | Adaptive music on/off |
| `SWEF_Music_Mode` | int | 0 | 0=Adaptive, 1=Playlist, 2=Hybrid |
| `SWEF_Music_MasterVolume` | float | 1.0 | Master volume 0–1 |
| `SWEF_Music_CrossfadeSpeed` | float | 1.0 | Speed multiplier 0.5–2.0 |
| `SWEF_Music_MoodSensitivity` | float | 1.0 | Sensitivity multiplier 0.5–2.0 |
| `SWEF_Music_DisabledLayers` | string | "" | Comma-separated disabled layers |

## Localization Keys (25 total)

**Mood names (9):** `music_mood_peaceful`, `music_mood_cruising`, `music_mood_adventurous`, `music_mood_tense`, `music_mood_danger`, `music_mood_epic`, `music_mood_serene`, `music_mood_mysterious`, `music_mood_triumphant`

**Layer names (8):** `music_layer_drums`, `music_layer_bass`, `music_layer_melody`, `music_layer_pads`, `music_layer_strings`, `music_layer_percussion`, `music_layer_choir`, `music_layer_synth`

**UI keys (8):** `music_adaptive_title`, `music_adaptive_enabled`, `music_mode_adaptive`, `music_mode_playlist`, `music_mode_hybrid`, `music_intensity_label`, `music_preview_mode`, `music_crossfade_speed`

## Integration Points

| Script | Integrates With |
|--------|----------------|
| `FlightContextAnalyzer` | `SWEF.Flight.FlightController` (null-safe) |
| `FlightContextAnalyzer` | `SWEF.Flight.AltitudeController` (null-safe) |
| `FlightContextAnalyzer` | `SWEF.Weather.WeatherManager` (null-safe) |
| `FlightContextAnalyzer` | `SWEF.TimeOfDay.TimeOfDayManager` (null-safe) |
| `FlightContextAnalyzer` | `SWEF.Biome.BiomeClassifier` (null-safe) |
| `FlightContextAnalyzer` | `SWEF.Emergency.EmergencyManager` (null-safe) |
| `FlightContextAnalyzer` | `SWEF.Damage.DamageModel` (null-safe) |
| `MusicPlayerBridge` | `SWEF.MusicPlayer.MusicPlayerManager` (null-safe) |
| `AdaptiveMusicAnalytics` | `SWEF.Analytics.TelemetryDispatcher` (null-safe) |

## Tests

`Assets/Tests/EditMode/AdaptiveMusicTests.cs` covers:
- `MoodResolver.ResolveMood()` — all 10 priority rules
- `MoodResolver.ResolveIntensity()` — range validation for each mood
- `IntensityController.GetActiveLayersForIntensity()` — boundary values 0, 0.2, 0.4, 0.6, 0.8, 1.0
- `BeatSyncClock` — initial state, BPM set/clamp
- `FlightMusicContext.Default()` — all default values
- `MusicTransitionController` — initial mood, same-mood no-op
- Enum completeness: `MusicMood` (9), `MusicLayer` (8)
