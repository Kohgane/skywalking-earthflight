# Phase 46 — Dynamic Time-of-Day & Seasonal Lighting System

**Namespace:** `SWEF.TimeOfDay`  
**Directory:** `Assets/SWEF/Scripts/TimeOfDay/`

---

## Overview

A comprehensive real-time sky simulation that makes every flight feel unique based on *when* and *where* you fly. The system models the sun and moon with astronomical precision, drives Unity's rendering pipeline with smooth lighting transitions, and integrates with weather, multiplayer, localization, analytics, and screenshot systems.

---

## Architecture

```
TimeOfDayManager (singleton)
    │
    ├─ SolarCalculator          ← Pure-math astronomical algorithms
    ├─ LightingController       ← Applies snapshots to Unity scene
    ├─ SeasonalLightingProfile  ← ScriptableObject: per-season curves/gradients
    ├─ GoldenHourEffect         ← Golden/blue hour special rendering
    ├─ NightSkyRenderer         ← Stars, moon, aurora, Milky Way
    ├─ TimeOfDayUI              ← HUD clock, expanded panel, photo mode
    ├─ TimeOfDayMultiplayerSync ← Multiplayer time authority
    └─ TimeOfDayAnalytics       ← Telemetry event tracking
```

---

## Scripts

### 1. `TimeOfDayData.cs`
Pure data classes and enums — no MonoBehaviour dependencies.

| Type | Description |
|------|-------------|
| `DayPhase` | Night → AstronomicalTwilight → NauticalTwilight → CivilTwilight → GoldenHour → Day → SolarNoon |
| `Season` | Spring / Summer / Autumn / Winter (hemisphere-aware) |
| `MoonPhase` | 8-phase lunar cycle |
| `SunMoonState` | Positional snapshot: altitudes, azimuths, world-space direction vectors, phase flags |
| `LightingSnapshot` | Scene parameters: sun/moon color+intensity, ambient trilight, fog, skybox, shadows, stars |
| `TimeOfDayConfig` | Runtime config: time scale, real-world sync, lat/lon, season overrides |
| `SeasonalProfile` | Per-season modifiers: day length, color tints, fog multipliers |

---

### 2. `SolarCalculator.cs`
Static utility class. No Unity lifecycle — thread-safe.

Key methods:
- `CalculateSunPosition(utcTime, lat, lon)` → `(altitude, azimuth)` in degrees
- `CalculateSunrise / CalculateSunset / CalculateDayLength`
- `GetDayPhase(sunAltitude)` → `DayPhase`
- `CalculateMoonPosition`, `GetMoonPhase`, `GetMoonIllumination`
- `GetSeason(date, latitude)` — hemisphere-aware seasonal mapping
- `SolarNoonTime(date, longitude)` — fractional UTC hour

**Algorithm:** NOAA Solar Calculator (Jean Meeus) — accuracy ±0.1° for dates within ±100 years of J2000.0. Handles polar night and midnight sun (returns −1 and 25 respectively for sunrise/sunset).

---

### 3. `TimeOfDayManager.cs`
Central singleton. `DontDestroyOnLoad`.

**Responsibilities:**
- Advance simulated time (`timeScale`) or sync to device UTC
- Convert player world-position to geographic lat/lon
- Recalculate sun/moon on configurable interval (`lightingUpdateInterval`)
- Fire `OnDayPhaseChanged`, `OnSunrise`, `OnSunset`, `OnSeasonChanged`, `OnHourChanged`
- Persist time preferences to PlayerPrefs (`SWEF_TOD_*`)

**Public API:**
```csharp
TimeOfDayManager.Instance.SetTime(float hour);
TimeOfDayManager.Instance.SetDate(DateTime utc);
TimeOfDayManager.Instance.SetTimeScale(float scale);
TimeOfDayManager.Instance.FastForward(float hours);
TimeOfDayManager.Instance.Rewind(float hours);
TimeOfDayManager.Instance.PauseTime() / ResumeTime();
TimeOfDayManager.Instance.GetSunMoonState() → SunMoonState
TimeOfDayManager.Instance.GetCurrentLighting() → LightingSnapshot
```

---

### 4. `LightingController.cs`
Applies `LightingSnapshot` to the active scene.

- Smoothly lerps sun/moon directional lights (color, intensity, rotation)
- Updates `RenderSettings` ambient trilight, fog, skybox
- Controls star particle system fade
- Quality-tier aware: skips expensive `RenderSettings` updates on low-end (`QualitySettings.GetQualityLevel() ≤ threshold`)

---

### 5. `SeasonalLightingProfile.cs`
`[CreateAssetMenu]` ScriptableObject.

- Designer-authored `Gradient` and `AnimationCurve` assets per season
- `Evaluate(sunAltitude, hourOfDay)` → `LightingSnapshot`
- Built-in Kelvin → RGB color temperature conversion
- `CreateDefault(Season)` factory for runtime fallbacks

**Create assets:** `Assets → Create → SWEF → TimeOfDay → SeasonalLightingProfile`

---

### 6. `GoldenHourEffect.cs`
Special rendering during golden hour (±6° of horizon) and blue hour (civil/nautical twilight).

- Subscribes to `TimeOfDayManager.OnDayPhaseChanged`
- Drives `LensFlare` brightness via `AnimationCurve`
- Notifies `ScreenshotManager` of photo-worthy moments
- Events: `OnGoldenHourStart/End`, `OnBlueHourStart/End`

---

### 7. `NightSkyRenderer.cs`
Manages all nighttime sky elements.

- **Stars:** particle emission rate driven by `LightingSnapshot.starVisibility`; external cloud coverage occlusion via `SetCloudCoverage(float)`
- **Moon:** updates phase material property and glow; positions moon mesh toward `moonDirection`
- **Aurora:** enabled when `|latitude| > 60°` and sun altitude < −12°; procedural curtain animation; intensity inversely proportional to moon illumination
- **Milky Way:** rotation driven by hour of day; visibility gated by moon phase and cloud coverage

---

### 8. `TimeOfDayUI.cs`
HUD and expanded panel for time display.

- **Mini clock widget:** time (12h/24h), day phase label, season, sun/moon icon, golden-hour badge
- **Expanded panel:** sunrise/sunset times, day-length bar, moon phase, time-lapse controls
- **Photo mode:** time scrubber slider (0–24 h)
- All text localized via `LocalizationManager.GetText(key)` with `tod_*` key prefix
- `ToggleExpandedPanel()` and `SetPhotoMode(bool)` for external control

---

### 9. `TimeOfDayMultiplayerSync.cs`
Keeps time synchronized in multiplayer sessions.

- Room host is the authoritative time source
- Non-host clients apply gentle drift correction (`maxDriftCorrectionSpeed` hours/second)
- Syncs every `syncIntervalSeconds` (default 5 s)
- Handles host migration: new host immediately broadcasts
- Wire `ReceiveSyncPacket(hour, timeScale, seasonOverride)` to your RPC layer

---

### 10. `TimeOfDayAnalytics.cs`
Tracks time-of-day related telemetry.

| Event | Trigger |
|-------|---------|
| `tod_sunrise_witnessed` | Player flying during sunrise |
| `tod_sunset_witnessed` | Player flying during sunset |
| `tod_golden_hour_screenshot` | Screenshot taken during golden hour |
| `tod_night_flight_duration` | Flushed on session end |
| `tod_season_distribution` | Per-season seconds flushed on session end |
| `tod_time_scale_usage` | User changes time scale |
| `tod_aurora_witnessed` | Player near pole during aurora |
| `tod_favorite_time` | Most-flown hour flushed on session end |

---

## Integration Points

| System | How connected |
|--------|---------------|
| `FlightController` | Player world position → lat/lon conversion; `IsFlying` gate for analytics |
| `AtmosphereController` | Existing fog/sky — `LightingController` uses `RenderSettings` directly |
| `SettingsManager` | Time preferences persisted under `SWEF_TOD_*` PlayerPrefs keys |
| `LocalizationManager` | All UI text via `GetText("tod_*")` |
| `ScreenshotController` | Golden-hour photo-worthy notifications |
| `NetworkManager2` | Multiplayer time authority, host migration |
| `TelemetryDispatcher` | Analytics events via `EnqueueEvent` |
| `WeatherManager` | Call `NightSkyRenderer.SetCloudCoverage(float)` from weather system |

---

## Setup

1. Add `TimeOfDayManager` to your bootstrap/persistent scene object.
2. Add `LightingController` to the same or a child GameObject; wire the sun/moon `Light` references.
3. Add `NightSkyRenderer` and wire the star `ParticleSystem`, moon `MeshRenderer`, aurora root, and Milky Way renderer.
4. Add `GoldenHourEffect` if you have a `LensFlare` source.
5. Add `TimeOfDayUI` and wire the uGUI elements.
6. (Multiplayer) Add `TimeOfDayMultiplayerSync` and wire it to your RPC dispatcher.
7. (Analytics) Add `TimeOfDayAnalytics` to any persistent object.
8. Create `SeasonalLightingProfile` ScriptableObject assets (one per season) if you want designer-authored curves; otherwise, runtime defaults are used automatically.
