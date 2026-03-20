# SWEF — SkywalkingEarthFlight Setup Guide

## Prerequisites
1. **Unity 2022.3 LTS+** with **URP** (Universal Render Pipeline)
2. **Cesium for Unity** v1.15.3+ (via Unity Package Manager → git URL or .tgz)
3. **Google Cloud Project** with:
   - Billing enabled
   - **Map Tiles API** activated
   - API key created (restrict to Map Tiles API)
4. **Cesium ion** account + access token

## Quick Start

### 1. Open project in Unity

### 2. Install Cesium for Unity package
- Package Manager → Add package from git URL
- Follow the official Cesium for Unity quickstart

### 3. Configure Scenes

#### Boot scene (`Assets/SWEF/Scenes/Boot.unity`)
1. Create new scene, save as `Boot.unity`
2. Create empty GameObject → attach `BootManager` script
3. Set `worldSceneName` = `"World"`

#### World scene (`Assets/SWEF/Scenes/World.unity`)
1. Create new scene, save as `World.unity`
2. Add **CesiumGeoreference** GameObject (from Cesium menu)
3. Add **Cesium3DTileset** → connect Google Photorealistic 3D Tiles
   - Follow the Cesium for Unity + Google 3D Tiles tutorial
4. Ensure **CesiumCreditSystem** is present
   - ⚠️ Attribution display is **mandatory** per Google Maps Platform TOS
5. Create **PlayerRig** GameObject:
   - Attach: `FlightController`, `TouchInputRouter`, `AltitudeController`
   - Add child: `Main Camera`
6. Create **Canvas** named `HUD`:
   - Add: `Slider` (Throttle, range 0–1)
   - Add: `Slider` (Altitude, range 0–120000)
   - Add: `Toggle` (Comfort)
   - Add: `Button` × 2 (RollLeft, RollRight) — attach `HoldButton` to each
   - Add: `Text` (AltitudeText)
7. Create **HudBinder** GameObject → wire all UI references
8. Create **WorldBootstrap** GameObject:
   - Assign `georeference` and `playerRig` fields

### 4. Build Settings
- File → Build Settings → add both scenes:
  - `Boot` at index 0
  - `World` at index 1
- **iOS**: Bundle ID = `com.kohgane.swef.earthflight`
- **Android**: applicationId = `com.kohgane.swef.earthflight`

### 5. Permissions
- **Location** (foreground only for MVP):
  - iOS: Add `NSLocationWhenInUseUsageDescription` to Info.plist
  - Android: `ACCESS_FINE_LOCATION` in AndroidManifest.xml

## ⚠️ Critical: Google Attribution
Google Photorealistic 3D Tiles **require** visible attribution on screen at all times.
The Cesium credit system handles this by default — **do NOT hide or remove it**.

## Architecture
```
Boot Scene
  └── BootManager: GPS fix → SWEFSession → Load World Scene

World Scene
  ├── WorldBootstrap: Sets CesiumGeoreference origin from SWEFSession
  ├── CesiumGeoreference + Cesium3DTileset (Google 3D Tiles)
  ├── CesiumCreditSystem (attribution — mandatory)
  ├── PlayerRig
  │   ├── FlightController (kinematic flight physics)
  │   ├── TouchInputRouter (touch/mouse → yaw/pitch/roll)
  │   ├── AltitudeController (slider → local Y)
  │   └── Main Camera
  └── HUD Canvas
      └── HudBinder (wires sliders/toggles to controllers)
```

---

## Altitude Staging (Ascent Experience)
| Range       | Visual Effect                                |
|-------------|----------------------------------------------|
| 0–2 km      | City detail, light haze                      |
| 2–20 km     | Fog reduces, sky color deepens               |
| 20–80 km    | Earth curvature visible, atmospheric scatter |
| 80–120 km   | Kármán line transition effect                |
| 120 km+     | Space skybox, low-detail Earth sphere        |

## Store Info
- **Title**: Skywalking: Earth Flight (SWEF)
- **Subtitle**: "Launch from your location. Climb to the edge of space."
- **iOS Bundle ID**: `com.kohgane.swef.earthflight`
- **Android applicationId**: `com.kohgane.swef.earthflight`

---

## Phase 2 — Atmosphere Visuals, Comfort Vignette, Teleport, Favorites

Phase 2 adds 6 new scripts across 3 new namespaces. All scripts are under `Assets/SWEF/Scripts/`.

### New Scripts

| Script | Namespace | Purpose |
|--------|-----------|---------|
| `Atmosphere/AtmosphereController.cs` | `SWEF.Atmosphere` | Altitude-based fog, sky color, skybox blend, sun intensity transitions |
| `Atmosphere/ComfortVignette.cs` | `SWEF.Atmosphere` | Motion-sickness vignette overlay during rapid rotation |
| `Teleport/TeleportController.cs` | `SWEF.Teleport` | Google Places Text Search API + fade teleport |
| `Teleport/TeleportUI.cs` | `SWEF.Teleport` | Search input panel with up to 5 result buttons |
| `Favorites/FavoriteManager.cs` | `SWEF.Favorites` | PlayerPrefs-based favorites storage (max 50) |
| `Favorites/FavoritesUI.cs` | `SWEF.Favorites` | Paged favorites list with save/delete/teleport |

### Setup in World Scene

#### 1. AtmosphereController
1. Create empty GameObject `AtmosphereController`
2. Attach `AtmosphereController` script
3. Assign:
   - `Altitude Source` → PlayerRig's `AltitudeController`
   - `Sun Light` → your Directional Light
   - `Skybox Material` → your skybox material (must have `_Blend` property)
4. Default layers are pre-configured; tweak in Inspector as needed

#### 2. ComfortVignette
1. On the HUD Canvas, create a full-screen **Image** (black, stretched to fill)
2. Add a **CanvasGroup** component to it
3. Create empty GameObject `ComfortVignette`
4. Attach `ComfortVignette` script
5. Assign:
   - `Flight` → PlayerRig's `FlightController`
   - `Vignette Overlay` → the CanvasGroup on the vignette Image
6. The vignette only activates when `FlightController.comfortMode == true`

#### 3. TeleportController
1. Create empty GameObject `TeleportController`
2. Attach `TeleportController` script
3. Assign:
   - `Api Key` → your Google Places API key
   - `Georeference` → the CesiumGeoreference GameObject
   - `Player Rig` → PlayerRig transform
   - `Fade Overlay` → a full-screen CanvasGroup for fade (can reuse vignette or create separate)
4. ⚠️ **Google Places API** must be enabled in your Google Cloud project

#### 4. TeleportUI
1. On the HUD Canvas, create a panel with:
   - `InputField` for search query
   - `Button` for search
   - 5 × `Button` for results (with `Text` children)
   - `Button` to toggle panel visibility
2. Create `TeleportUI` GameObject, attach script
3. Wire all UI references in Inspector

#### 5. FavoriteManager
1. Create empty GameObject `FavoriteManager`
2. Attach `FavoriteManager` script
3. No extra config needed — uses PlayerPrefs automatically

#### 6. FavoritesUI
1. On the HUD Canvas, create a panel with:
   - `Button` to save current location
   - 5 × `Button` for favorite items (with `Text` children)
   - 5 × `Button` for delete per item
   - `Button` Prev / `Button` Next for paging
   - `Text` for page indicator
2. Create `FavoritesUI` GameObject, attach script
3. Assign:
   - `Favorites` → FavoriteManager
   - `Teleport` → TeleportController
   - `Session` → SWEFSession (from Boot scene, DontDestroyOnLoad)
   - `Altitude Source` → PlayerRig's AltitudeController

### Updated Architecture
```
World Scene (Phase 2)
  ├── WorldBootstrap
  ├── CesiumGeoreference + Cesium3DTileset
  ├── CesiumCreditSystem
  ├── AtmosphereController          ← NEW
  ├── ComfortVignette                ← NEW
  ├── TeleportController             ← NEW
  ├── FavoriteManager                ← NEW
  ├── PlayerRig
  │   ├── FlightController
  │   ├── TouchInputRouter
  │   ├── AltitudeController
  │   └── Main Camera
  └── HUD Canvas
      ├── HudBinder
      ├── Vignette Overlay (Image + CanvasGroup)
      ├── Teleport Panel (TeleportUI)  ← NEW
      └── Favorites Panel (FavoritesUI) ← NEW
```

---

## Phase 3 — Settings, Audio Manager, Screenshot, Onboarding Tutorial

Phase 3 adds 7 new scripts across 4 new namespaces, plus minor additions to two existing scripts.

### New Scripts

| Script | Namespace | Purpose |
|--------|-----------|---------|
| `Settings/SettingsManager.cs` | `SWEF.Settings` | PlayerPrefs-based persistent settings (volume, sensitivity, speed, comfort mode) |
| `Settings/SettingsUI.cs` | `SWEF.Settings` | UI panel with sliders, toggle, close and reset buttons |
| `Audio/AudioManager.cs` | `SWEF.Audio` | Singleton BGM + SFX manager; survives scene loads |
| `Audio/AltitudeAudioTrigger.cs` | `SWEF.Audio` | Plays AltitudeWarning SFX when crossing 100 km / 120 km thresholds |
| `Screenshot/ScreenshotController.cs` | `SWEF.Screenshot` | Hides HUD, captures PNG to persistentDataPath/Screenshots/, fires event |
| `Screenshot/ScreenshotUI.cs` | `SWEF.Screenshot` | Capture button, white flash overlay, "Screenshot saved!" toast |
| `Tutorial/TutorialManager.cs` | `SWEF.Tutorial` | First-run 6-step onboarding overlay; persisted via "SWEF_TutorialCompleted" |

### Modified Scripts

| Script | Change |
|--------|--------|
| `Flight/FlightController.cs` | Added `SetMaxSpeed(float)` public setter |
| `Flight/TouchInputRouter.cs` | Added `SetSensitivity(float)` public setter |
| `Favorites/FavoritesUI.cs` | Fixed `SWEFSession` static-class access (`SWEFSession.Lat/Lon`) |

> **Note:** AudioClips (BGM and SFX) are user-provided assets and are **not** included in the repository. Assign them in the Inspector on the `AudioManager` GameObject.

### Setup in World Scene

#### 1. SettingsManager
1. Create empty GameObject `SettingsManager`
2. Attach `SettingsManager` script
3. Optionally assign `Flight Controller` and `Touch Input Router` fields (auto-found if left empty)

#### 2. SettingsUI
1. On the HUD Canvas create a settings panel with:
   - `Slider` MasterVolume (0–1)
   - `Slider` SfxVolume (0–1)
   - `Toggle` ComfortMode
   - `Slider` TouchSensitivity (0.5–3.0)
   - `Slider` MaxSpeed (50–500)
   - `Button` Close, `Button` Reset
2. Create `SettingsUI` GameObject, attach script
3. Assign `Open Button` in the HUD (e.g. a ⚙ icon button)
4. Assign `Settings Manager` → the SettingsManager GameObject above

#### 3. AudioManager
1. Create empty GameObject `AudioManager` (place in Boot scene or a persistent scene)
2. Attach `AudioManager` script — `DontDestroyOnLoad` is handled automatically
3. Assign:
   - `Bgm Clip` → your background music AudioClip
   - `Sfx Clips` (array of 5) → clips for ButtonClick, Teleport, Screenshot, FavoriteSave, AltitudeWarning
   - `Settings Manager` → SettingsManager (optional — auto-found)
4. BGM starts automatically on `Start()`

#### 4. AltitudeAudioTrigger
1. Create empty GameObject `AltitudeAudioTrigger` in World scene
2. Attach `AltitudeAudioTrigger` script
3. Assign `Altitude Source` → PlayerRig's `AltitudeController` (auto-found if left empty)
4. Default thresholds: 100,000 m (Kármán line) and 120,000 m; adjust in Inspector

#### 5. ScreenshotController
1. Create empty GameObject `ScreenshotController`
2. Attach `ScreenshotController` script
3. Assign `Hud Canvas` → the main HUD Canvas (auto-found if left empty)
4. Screenshots are saved to `Application.persistentDataPath/Screenshots/SWEF_<timestamp>.png`

#### 6. ScreenshotUI
1. On the HUD Canvas add a camera/screenshot button
2. Optionally add a full-screen white Image with CanvasGroup (flash overlay)
3. Add a `Text` element for the toast message
4. Create `ScreenshotUI` GameObject, attach script and wire all references
5. Assign `Controller` → the ScreenshotController above

#### 7. TutorialManager
1. Create a full-screen Canvas overlay for the tutorial
2. Add `Text` (message), `Button` (Next), `Button` (Skip) inside the panel
3. Create `TutorialManager` GameObject, attach script
4. Assign `Tutorial Panel`, `Message Text`, `Next Button`, `Skip Button` in Inspector
5. Tutorial shows automatically on first World scene load (once only)
6. To force re-show: call `PlayerPrefs.DeleteKey("SWEF_TutorialCompleted")` in the Unity Editor

### Updated Architecture
```
World Scene (Phase 3)
  ├── WorldBootstrap
  ├── CesiumGeoreference + Cesium3DTileset
  ├── CesiumCreditSystem
  ├── AtmosphereController
  ├── ComfortVignette
  ├── TeleportController
  ├── FavoriteManager
  ├── SettingsManager              ← NEW
  ├── AltitudeAudioTrigger         ← NEW
  ├── ScreenshotController         ← NEW
  ├── PlayerRig
  │   ├── FlightController  (+ SetMaxSpeed)
  │   ├── TouchInputRouter  (+ SetSensitivity)
  │   ├── AltitudeController
  │   └── Main Camera
  └── HUD Canvas
      ├── HudBinder
      ├── Vignette Overlay (Image + CanvasGroup)
      ├── Teleport Panel (TeleportUI)
      ├── Favorites Panel (FavoritesUI)
      ├── Settings Panel (SettingsUI)      ← NEW
      ├── Screenshot Button (ScreenshotUI) ← NEW
      └── Tutorial Overlay (TutorialManager) ← NEW

Boot Scene / DontDestroyOnLoad
  └── AudioManager                 ← NEW (singleton, persists across scenes)
```

---

## Phase 4 — Loading Screen, Error Handling, Speed/Compass HUD, Minimap

Phase 4 adds 5 new scripts across `SWEF.Core` and `SWEF.UI`, plus targeted modifications to two existing scripts.

### New Scripts

| Script | Namespace | Purpose |
|--------|-----------|---------|
| `Core/LoadingScreen.cs` | `SWEF.Core` | Progress bar + rotating tip texts while GPS initializes in the Boot scene |
| `Core/ErrorHandler.cs` | `SWEF.Core` | Per-scene singleton — displays GPS, Network and API errors with Retry/Dismiss |
| `UI/SpeedIndicator.cs` | `SWEF.UI` | HUD speed readout in km/h; Mach number above 343 m/s; "ORBITAL ⚡" above 7900 m/s |
| `UI/CompassHUD.cs` | `SWEF.UI` | HUD heading (000°) + 8-direction cardinal (N/NE/E/SE/S/SW/W/NW); optional needle |
| `UI/MiniMap.cs` | `SWEF.UI` | Lat/lon coordinates + adaptive altitude units + altitude-range emoji label |

### Modified Scripts

| Script | Change |
|--------|--------|
| `Flight/FlightController.cs` | Added `public float CurrentSpeedMps => _vel.magnitude;` property |
| `Core/BootManager.cs` | Integrated `LoadingScreen` (show/progress/status/hide) and `ErrorHandler` (GPS error presets) |

### Setup in Boot Scene

#### 1. LoadingScreen
1. Create a Canvas in the Boot scene (Screen Space – Overlay)
2. Add a full-screen panel (`GameObject` → Image, stretched to fill)
3. Inside the panel add:
   - `Slider` — progress bar (0–1)
   - `Text` — status text (e.g. "Acquiring GPS fix…")
   - `Text` — tip text (rotates every 3 s)
4. Attach a `CanvasGroup` component to the panel
5. Create `LoadingScreen` GameObject, attach the script
6. Wire: `Progress Bar`, `Status Text`, `Tip Text`, `Loading Panel`, `Canvas Group`
7. On the `BootManager` GameObject, assign `Loading Screen` → the LoadingScreen above

#### 2. ErrorHandler (Boot Scene)
1. Create an error panel (Canvas child): title Text, message Text, Retry Button, Dismiss Button
2. Create `ErrorHandler` GameObject, attach the script
3. Wire all four UI references in Inspector
4. The `BootManager` will call `ErrorHandler.Instance?.ShowGPSError()` / `ShowGPSTimeoutError()` automatically

### Setup in World Scene

#### 3. ErrorHandler (World Scene)
- Repeat the same steps as Boot Scene so errors in the World scene (e.g. network/API errors) can also be surfaced to the user
- Each scene maintains its own instance; `ErrorHandler.Instance` is updated per-scene

#### 4. SpeedIndicator
1. On the HUD Canvas, add a `Text` for speed (e.g. "SPD 0 km/h")
2. Optionally add a second `Text` for Mach / orbital info
3. Create `SpeedIndicator` GameObject, attach the script
4. Assign `Flight` → PlayerRig's `FlightController`; `Speed Text`; `Mach Text` (optional)

#### 5. CompassHUD
1. On the HUD Canvas, add a `Text` for the heading readout
2. Optionally add a `RectTransform` image as a compass needle
3. Create `CompassHUD` GameObject, attach the script
4. Assign `Player Rig` → the PlayerRig transform; `Heading Text`; `Compass Needle` (optional)

#### 6. MiniMap
1. On the HUD Canvas, create a small panel containing:
   - `Text` for coordinates
   - `Text` for altitude + range label (or omit to show both in coordinate text)
   - `Button` to toggle panel visibility (optional)
2. Create `MiniMap` GameObject, attach the script
3. Assign `Coord Text`, `Alt Range Text` (optional), `Mini Map Panel`, `Altitude Source`, `Toggle Button` (optional)

### Updated Architecture

```
Boot Scene (Phase 4)
  ├── BootManager (+ LoadingScreen integration)
  ├── LoadingScreen                     ← NEW
  │   ├── Progress Bar (Slider)
  │   ├── Status Text
  │   └── Tip Text (rotating)
  └── ErrorHandler                      ← NEW
      ├── Error Panel
      ├── Retry / Dismiss Buttons

World Scene (Phase 4)
  ├── WorldBootstrap
  ├── CesiumGeoreference + Cesium3DTileset
  ├── CesiumCreditSystem
  ├── AtmosphereController
  ├── ComfortVignette
  ├── TeleportController
  ├── FavoriteManager
  ├── SettingsManager
  ├── AudioManager
  ├── ErrorHandler                      ← NEW (per-scene instance)
  ├── PlayerRig
  │   ├── FlightController (+ CurrentSpeedMps)
  │   ├── TouchInputRouter
  │   ├── AltitudeController
  │   └── Main Camera
  └── HUD Canvas
      ├── HudBinder
      ├── SpeedIndicator                ← NEW
      ├── CompassHUD                    ← NEW
      ├── MiniMap                       ← NEW
      ├── Vignette Overlay
      ├── Teleport Panel
      ├── Favorites Panel
      ├── Settings Panel
      ├── Screenshot Button
      └── Tutorial Overlay
```

---

## Phase 5 — Pause System, Flight Recorder/Playback, Achievements, Stats Dashboard

Phase 5 adds 7 new scripts across 3 new namespaces (`SWEF.Core`, `SWEF.Recorder`, `SWEF.Achievement`) and extends `SWEF.UI`, plus targeted modifications to two existing scripts.

### New Scripts

| Script | Namespace | Purpose |
|--------|-----------|---------|
| `Core/PauseManager.cs` | `SWEF.Core` | Singleton pause/resume; sets `Time.timeScale` 0↔1; shows pause panel with Resume and Quit buttons |
| `Recorder/FlightRecorder.cs` | `SWEF.Recorder` | Records position, rotation, altitude and speed at configurable intervals (default 0.5 s, max 300 s) |
| `Recorder/FlightPlayback.cs` | `SWEF.Recorder` | Replays recorded frames by lerping a ghost Transform; adjustable speed 0.25×–4×; fires `OnPlaybackFinished` |
| `Recorder/RecorderUI.cs` | `SWEF.Recorder` | Record/Stop/Play/Clear buttons, progress slider and status text (🔴 REC / ▶ / Ready) |
| `Achievement/AchievementManager.cs` | `SWEF.Achievement` | Singleton — 8 achievements persisted in PlayerPrefs; auto-checks altitude and speed thresholds each frame |
| `Achievement/AchievementUI.cs` | `SWEF.Achievement` | Fade-in toast on unlock; achievement list panel (✅/🔒) toggled by a button |
| `UI/StatsDashboard.cs` | `SWEF.UI` | Tracks and displays flight time, max altitude, max speed, distance traveled, and achievement count |

### Modified Scripts

| Script | Change |
|--------|--------|
| `Teleport/TeleportController.cs` | Calls `AchievementManager.Instance?.TryUnlock("first_teleport")` after successful teleport |
| `Screenshot/ScreenshotController.cs` | Calls `AchievementManager.Instance?.TryUnlock("first_screenshot")` after successful capture |

### Achievements

| ID | Title | Trigger |
|----|-------|---------|
| `first_flight` | First Flight ✈️ | First Update frame in the World scene |
| `reach_10km` | Sky High 🌤️ | Altitude ≥ 10,000 m |
| `reach_karman` | Edge of Space 🌍 | Altitude ≥ 100,000 m (Kármán line) |
| `reach_120km` | Space Pioneer 🚀 | Altitude ≥ 120,000 m |
| `mach1` | Sound Barrier 💥 | Speed ≥ 343 m/s |
| `orbital_speed` | Orbital Velocity ⚡ | Speed ≥ 7,900 m/s |
| `first_teleport` | World Traveler 🗺️ | First successful teleport |
| `first_screenshot` | Photographer 📸 | First screenshot captured |

PlayerPrefs keys use the pattern `SWEF_ACH_{id}` (value `1` = unlocked).

### Setup in World Scene

#### 1. PauseManager
1. Create empty GameObject `PauseManager`
2. Attach `PauseManager` script
3. On the HUD Canvas create a pause panel containing:
   - A full-screen dark overlay with a **CanvasGroup** component (`Pause Overlay`)
   - `Button` Resume → wired automatically
   - `Button` Quit To Menu → wired automatically
4. Assign `Pause Panel`, `Resume Button`, `Quit Button`, `Pause Overlay` in Inspector
5. Call `PauseManager.Instance.TogglePause()` from a pause button (e.g. ☰ icon)

#### 2. FlightRecorder
1. Create empty GameObject `FlightRecorder` in World scene
2. Attach `FlightRecorder` script
3. Assign `Flight` → PlayerRig's `FlightController`; `Altitude` → `AltitudeController` (auto-found if left empty)
4. Adjust `Record Interval Sec` (default 0.5) and `Max Record Duration Sec` (default 300) if desired

#### 3. FlightPlayback
1. Create a simple ghost GameObject (e.g. a semi-transparent aircraft mesh or an empty marker)
2. Create `FlightPlayback` GameObject, attach script
3. Assign `Recorder` → the `FlightRecorder` above; `Ghost Object` → the ghost Transform
4. `Playback Speed` defaults to 1×

#### 4. RecorderUI
1. On the HUD Canvas create a toggleable panel containing:
   - `Button` Record, `Button` Stop, `Button` Play, `Button` Clear
   - `Slider` Progress (0–1, non-interactable for display)
   - `Text` Status
2. Create `RecorderUI` GameObject, attach script
3. Wire all button/slider/text references; assign `Recorder Panel` and `Toggle Button`

#### 5. AchievementManager
1. Create empty GameObject `AchievementManager` in World scene
2. Attach `AchievementManager` script
3. Assign `Altitude Source` → `AltitudeController`; `Flight` → `FlightController` (auto-found if left empty)

#### 6. AchievementUI
1. Create a toast panel on the HUD Canvas:
   - `Text` Toast Title, `Text` Toast Description
   - Attach a **CanvasGroup** to the panel (`Toast Canvas Group`)
2. Create an achievement list panel with 8 × `Text` elements (one per achievement)
3. Create `AchievementUI` GameObject, attach script
4. Wire all references; assign `Toggle Button` to show/hide the list panel

#### 7. StatsDashboard
1. On the HUD Canvas create a dashboard panel with `Text` elements for:
   - Flight Time, Max Altitude, Max Speed, Distance Traveled, Achievements
2. Create `StatsDashboard` GameObject, attach script
3. Assign all `Text` refs, `Dashboard Panel`, `Toggle Button`
4. Wire `Flight` → `FlightController`; `Altitude` → `AltitudeController` (auto-found if left empty)

### Updated Architecture

```
Boot Scene / World Scene (Phase 5)
  ├── PauseManager                      ← NEW
  ├── AchievementManager                ← NEW
  ├── FlightRecorder                    ← NEW
  ├── FlightPlayback                    ← NEW
  └── HUD Canvas additions:
      ├── Pause Panel (PauseManager)    ← NEW
      ├── Recorder Panel (RecorderUI)   ← NEW
      ├── Achievement Toast + List (AchievementUI) ← NEW
      └── Stats Dashboard (StatsDashboard) ← NEW
```

---

## Phase 6 — Visual & Environment Upgrade

Five new scripts enhance the visual flight experience from ground to space.

### New Scripts

| Script | Namespace | Role |
|--------|-----------|------|
| `Atmosphere/DayNightCycle.cs` | `SWEF.Atmosphere` | Real-time or accelerated day/night cycle (sun rotation, colour gradient, ambient intensity) |
| `Atmosphere/CloudLayer.cs` | `SWEF.Atmosphere` | Altitude-based cloud layer fade in/out (above vs below cloud opacity) |
| `Atmosphere/ReentryEffect.cs` | `SWEF.Atmosphere` | Atmospheric reentry particles + screen glow during fast descent below 120 km |
| `Flight/JetTrail.cs` | `SWEF.Flight` | Speed-proportional `TrailRenderer` (width, colour, emission) |
| `UI/AltitudeMilestone.cs` | `SWEF.UI` | Toast notifications at 1 km, 10 km, 20 km, 50 km, 100 km, 120 km |

### Modified Scripts

| Script | Change |
|--------|--------|
| `Atmosphere/AtmosphereController.cs` | Added optional `DayNightCycle` reference; when assigned and `IsNight`, sun intensity is multiplied by `nightIntensityFactor` (default 0.1) |

### Setup — DayNightCycle

1. Create an empty GameObject `DayNightCycle` in the World scene.
2. Attach the `DayNightCycle` script.
3. Assign `Sun Light` → your scene's directional light.
4. Set `Day Duration Minutes` (default 24 min = 1 day per 24 real minutes).
5. Configure `Sun Color Gradient` (warm sunrise → white noon → orange sunset → dark blue night).
6. Configure `Sun Intensity Curve` (0 at night, 1+ at noon).
7. Optionally enable `Use Real Time` to sync with device UTC clock.
8. Assign this GameObject to `AtmosphereController → Day Night Cycle` for night dimming.

### Setup — CloudLayer

1. Create cloud GameObjects (e.g. particle systems, semi-transparent quads) at the appropriate world heights. These are user-provided assets.
2. Create an empty GameObject `CloudLayer` and attach the script.
3. Populate `Cloud Layers` with your cloud GameObjects and `Cloud Altitudes` with matching altitude values in meters (e.g. 2000, 5000, 10000).
4. Assign `Altitude Source` → `AltitudeController` (auto-found if left empty).
5. Tune `Fade Range`, `Cloud Alpha Above`, and `Cloud Alpha Below` as desired.

### Setup — ReentryEffect

1. Create a ParticleSystem for fire/plasma (user-provided asset). Attach it to the player rig.
2. Create an empty GameObject `ReentryEffect` and attach the script.
3. Assign `Reentry Particles` → the ParticleSystem above.
4. (Optional) Create a full-screen `Image` on the HUD Canvas with an orange/red tint, add a **CanvasGroup**, and assign it to `Screen Glow`.
5. Assign `Altitude Source` and `Flight Source` (auto-found if left empty).
6. Tune `Activation Altitude` (default 120 000 m), `Min Descent Speed` (default 100 m/s), and `Max Glow Alpha` (default 0.3).

### Setup — JetTrail

1. Add a **TrailRenderer** component to the player's aircraft mesh or a child transform behind it.
2. Create an empty GameObject `JetTrail` (or add the script directly to the aircraft).
3. Assign `Trail` → the `TrailRenderer`.
4. Assign `Flight` → `FlightController` (auto-found if left empty).
5. Configure `Min Speed For Trail`, `Max Trail Speed`, `Trail Width Min/Max`, and `Trail Color Gradient`.

### Setup — AltitudeMilestone

1. On the HUD Canvas create a panel containing:
   - A `Text` element for the milestone message.
   - A **CanvasGroup** on the panel for fade control.
2. Create `AltitudeMilestone` GameObject, attach the script.
3. Assign `Milestone Text` and `Milestone Group`.
4. Assign `Altitude Source` → `AltitudeController` (auto-found if left empty).
5. Customise `Milestones` array or leave defaults (1 km → 120 km).

### Updated Architecture

```
World Scene (Phase 6)
  ├── DayNightCycle                         ← NEW
  ├── CloudLayer                            ← NEW
  ├── ReentryEffect                         ← NEW
  ├── PlayerRig
  │   ├── JetTrail (on aircraft mesh)       ← NEW
  │   └── (existing) FlightController, AltitudeController …
  └── HUD Canvas additions:
      └── Milestone Toast (AltitudeMilestone) ← NEW
```

---

## Phase 6 — Release Preparation: Performance, Analytics, Localization, Splash, Deep Link

Phase 6 adds 5 new scripts focused on release readiness and polishing.

### New Scripts

| Script | Namespace | Purpose |
|--------|-----------|---------|
| `Core/PerformanceManager.cs` | `SWEF.Core` | FPS monitoring + adaptive Cesium LOD quality switching (Low/Medium/High) |
| `Core/AnalyticsLogger.cs` | `SWEF.Core` | Local PlayerPrefs-based session analytics (flight time, max altitude, teleport/screenshot counts) |
| `UI/LocalizationManager.cs` | `SWEF.UI` | JSON-based multi-language support (en/ko/ja) with runtime switching |
| `UI/SplashScreen.cs` | `SWEF.UI` | App launch splash with logo fade-in/hold/fade-out → Boot scene transition |
| `Core/DeepLinkHandler.cs` | `SWEF.Core` | URL scheme handler (`swef://teleport?lat=...&lon=...&name=...`) for location sharing |

### Modified Scripts

| Script | Change |
|--------|--------|
| `Core/BootManager.cs` | Added optional `SplashScreen` serialized field |

### Setup

#### 1. PerformanceManager
1. Create empty GameObject `PerformanceManager` in World scene
2. Attach `PerformanceManager` script
3. Assign `Tileset` → your Cesium3DTileset component
4. Adjust target FPS and thresholds in Inspector as needed
5. Quality auto-switches between Low (SSE=24), Medium (SSE=8), High (SSE=2)

#### 2. AnalyticsLogger
1. Create empty GameObject `AnalyticsLogger` in World scene
2. Attach `AnalyticsLogger` script
3. Assign `Altitude Source` → PlayerRig's `AltitudeController` (auto-found if empty)
4. Call `RecordTeleport()` / `RecordScreenshot()` from respective controllers
5. Stats persist across sessions via PlayerPrefs

#### 3. LocalizationManager
1. Create empty GameObject `LocalizationManager` (place in Boot scene or persistent scene)
2. Attach `LocalizationManager` script — `DontDestroyOnLoad` is handled automatically
3. Create `Resources/Localization/` folder in Assets
4. Add JSON files: `en.json`, `ko.json`, `ja.json` with format:
   `{"keys": [{"k":"ui_start","v":"Start"}, {"k":"ui_settings","v":"Settings"}, ...]}`
5. System auto-detects device language on startup; call `SetLanguage()` to switch manually

#### 4. SplashScreen
1. Create a new scene `Splash.unity`
2. Add a Canvas (Screen Space – Overlay) with a logo Image
3. Add a `CanvasGroup` to the logo
4. Attach `SplashScreen` script, wire the `CanvasGroup`
5. Update Build Settings: `Splash` at index 0, `Boot` at index 1, `World` at index 2

#### 5. DeepLinkHandler
1. Create empty GameObject `DeepLinkHandler` (place in Boot scene or persistent scene)
2. Attach `DeepLinkHandler` script — `DontDestroyOnLoad` handled automatically
3. Assign `Teleport` → TeleportController (auto-found if empty)
4. Configure URL scheme in platform settings:
   - **iOS**: Add `swef` to URL Types in Xcode project
   - **Android**: Add intent-filter for `swef://` scheme in AndroidManifest.xml
5. Share links like: `swef://teleport?lat=35.6762&lon=139.6503&name=Tokyo`

### Updated Architecture
```
Splash Scene (Phase 6) ← NEW
  └── SplashScreen: Logo fade → Load Boot Scene

Boot Scene (Phase 6)
  ├── BootManager (+ optional SplashScreen ref)
  ├── LoadingScreen
  ├── ErrorHandler
  └── DeepLinkHandler (DontDestroyOnLoad)    ← NEW

World Scene (Phase 6)
  ├── WorldBootstrap
  ├── CesiumGeoreference + Cesium3DTileset
  ├── CesiumCreditSystem
  ├── PerformanceManager                      ← NEW
  ├── AnalyticsLogger                         ← NEW
  ├── AtmosphereController
  ├── ComfortVignette
  ├── TeleportController
  ├── FavoriteManager
  ├── SettingsManager
  ├── AltitudeAudioTrigger
  ├── ScreenshotController
  ├── PlayerRig
  │   ├── FlightController
  │   ├── TouchInputRouter
  │   ├── AltitudeController
  │   └── Main Camera
  └── HUD Canvas
      ├── HudBinder
      ├── SpeedIndicator
      ├── CompassHUD
      ├── MiniMap
      ├── AltitudeMilestone
      ├── StatsDashboard
      ├── Vignette Overlay
      ├── Teleport Panel
      ├── Favorites Panel
      ├── Settings Panel
      ├── Screenshot Button
      └── Tutorial Overlay

DontDestroyOnLoad (persistent singletons)
  ├── AudioManager
  ├── LocalizationManager                     ← NEW
  └── DeepLinkHandler                         ← NEW
```

---

## Phase 7 — Multiplayer Foundation & Social Features

Phase 7 adds multiplayer infrastructure and social sharing capabilities.

### New Scripts

| Script | Namespace | Purpose |
|--------|-----------|---------|
| `Multiplayer/PlayerAvatar.cs` | `SWEF.Multiplayer` | Remote player visual proxy with position/rotation interpolation and name label |
| `Multiplayer/MultiplayerManager.cs` | `SWEF.Multiplayer` | Core multiplayer orchestrator — state broadcasting, remote avatar lifecycle, stale player cleanup |
| `Social/ShareManager.cs` | `SWEF.Social` | Generates deep links & share text, native share sheet integration, clipboard fallback |
| `Social/LeaderboardManager.cs` | `SWEF.Social` | Local personal records — tracks top flights by altitude, duration, speed, score |
| `UI/LeaderboardUI.cs` | `SWEF.UI` | Scrollable leaderboard panel displaying personal best flights |

### Setup

#### 1. PlayerAvatar Prefab
1. Create a 3D object (capsule/sphere) to represent remote players
2. Attach `PlayerAvatar` script
3. Add `TextMesh` child for name label (optional)
4. Save as Prefab in `Assets/SWEF/Prefabs/PlayerAvatar.prefab`

#### 2. MultiplayerManager
1. Create empty GameObject `MultiplayerManager` in World scene
2. Attach `MultiplayerManager` script
3. Assign: `Local Flight` → PlayerRig's FlightController, `Local Altitude` → AltitudeController
4. Assign: `Avatar Prefab` → PlayerAvatar prefab, `Avatar Parent` → an empty transform for organization
5. Hook `OnLocalStateBroadcast` event to your network transport when ready

#### 3. ShareManager
1. Create empty GameObject `ShareManager` in World scene
2. Attach `ShareManager` script
3. Assign altitude source (auto-found if empty)
4. Call `ShareManager.Instance.ShareText(ShareManager.Instance.GenerateShareText())` from a share button

#### 4. LeaderboardManager
1. Create empty GameObject `LeaderboardManager` in World scene
2. Attach `LeaderboardManager` script
3. Assign flight & altitude controllers (auto-found if empty)
4. Session auto-submits on app pause/quit
5. Call `RecordSessionTeleport()` when teleporting

#### 5. LeaderboardUI
1. Create a Panel in HUD Canvas for the leaderboard
2. Add ScrollRect with content container
3. Create an entry prefab with Text elements: Rank, Date, Altitude, Duration, Speed, Score
4. Attach `LeaderboardUI`, wire all references
5. Add a toggle button to show/hide

### Updated Architecture
```
World Scene (Phase 7)
  ├── WorldBootstrap
  ├── CesiumGeoreference + Cesium3DTileset
  ├── CesiumCreditSystem
  ├── PerformanceManager
  ├── AnalyticsLogger
  ├── MultiplayerManager                     ← NEW
  │   └── RemoteAvatars (spawned PlayerAvatar instances)
  ├── ShareManager                            ← NEW
  ├── LeaderboardManager                      ← NEW
  ├── AtmosphereController
  ├── ComfortVignette
  ├── DayNightCycle
  ├── CloudLayer
  ├── ReentryEffect
  ├── TeleportController
  ├── FavoriteManager
  ├── SettingsManager
  ├── AltitudeAudioTrigger
  ├── ScreenshotController
  ├── PlayerRig
  │   ├── FlightController
  │   ├── TouchInputRouter
  │   ├── AltitudeController
  │   ├── JetTrail
  │   └── Main Camera
  └── HUD Canvas
      ├── HudBinder
      ├── SpeedIndicator
      ├── CompassHUD
      ├── MiniMap
      ├── AltitudeMilestone
      ├── StatsDashboard
      ├── LeaderboardUI                       ← NEW
      ├── Vignette Overlay
      ├── Teleport Panel
      ├── Favorites Panel
      ├── Settings Panel
      ├── Screenshot Button
      ├── Share Button                        ← NEW
      └── Tutorial Overlay
```

## Phase 8 — Polish & Optimization

Phase 8 adds 5 new scripts focused on app stability, performance presets, accessibility, and input remapping.

### New Scripts

| Script | Namespace | Purpose |
|--------|-----------|---------|
| `Core/MemoryManager.cs` | `SWEF.Core` | Runtime memory monitoring + auto GC/cache clear at configurable thresholds |
| `Core/QualityPresetManager.cs` | `SWEF.Core` | One-tap quality presets (Low/Medium/High/Ultra) controlling Cesium SSE, shadows, frame rate |
| `UI/AccessibilityManager.cs` | `SWEF.UI` | Font scaling, high-contrast mode, reduced-motion flag; persisted in PlayerPrefs |
| `Core/CrashReporter.cs` | `SWEF.Core` | Captures exceptions/errors to persistent log files; detects previous session crashes |
| `UI/InputRebinder.cs` | `SWEF.UI` | Keyboard/gamepad input remapping with PlayerPrefs persistence |

### Modified Scripts

| Script | Change |
|--------|--------|
| `Settings/SettingsUI.cs` | Added quality preset dropdown wired to QualityPresetManager |
| `Core/BootManager.cs` | Checks for previous crash on startup via CrashReporter |

### Setup

#### 1. MemoryManager
1. Create empty GameObject `MemoryManager` (Boot scene or persistent)
2. Attach `MemoryManager` script — DontDestroyOnLoad handled automatically
3. Adjust `Check Interval Sec`, `Memory Warning Threshold MB`, `Memory Critical Threshold MB` in Inspector

#### 2. QualityPresetManager
1. Create empty GameObject `QualityPresetManager` in World scene
2. Attach `QualityPresetManager` script
3. Optionally assign `Tileset` → Cesium3DTileset (auto-found if empty)
4. Default quality: Medium

#### 3. AccessibilityManager
1. Create empty GameObject `AccessibilityManager` (Boot scene or persistent)
2. Attach `AccessibilityManager` script — DontDestroyOnLoad handled automatically
3. Call SetFontScale/SetHighContrast/SetReducedMotion from Settings UI

#### 4. CrashReporter
1. Create empty GameObject `CrashReporter` (Boot scene or persistent)
2. Attach `CrashReporter` script — DontDestroyOnLoad handled automatically
3. Crash logs are written to `persistentDataPath/CrashLogs/`
4. On BootManager startup, previous crash is auto-detected and logged

#### 5. InputRebinder
1. Create empty GameObject `InputRebinder` in World scene
2. Attach `InputRebinder` script
3. Default bindings pre-populated; customize in Inspector
4. Useful for editor testing and desktop builds

### Updated Architecture
```
World Scene (Phase 8)
  ├── QualityPresetManager                   ← NEW
  ├── InputRebinder                          ← NEW
  ├── (all existing Phase 1–7 systems)
  └── HUD Canvas
      └── Settings Panel (+ quality dropdown) ← MODIFIED

Boot Scene / DontDestroyOnLoad (Phase 8)
  ├── MemoryManager                          ← NEW
  ├── AccessibilityManager                   ← NEW
  ├── CrashReporter                          ← NEW
  └── (all existing persistent singletons)
```

## Phase 9 — Advanced Camera, Weather System & Mini-Map

Phase 9 adds 6 new scripts providing multi-mode camera control, dynamic weather simulation, wind physics, and a mini-map overlay.

### New Scripts

| Script | Namespace | Purpose |
|--------|-----------|---------|
| `Flight/CameraController.cs` | `SWEF.Flight` | Four camera modes (FirstPerson, ThirdPerson, Orbit, Cinematic) with smooth transitions |
| `Atmosphere/WeatherController.cs` | `SWEF.Atmosphere` | Dynamic weather system (Clear/Cloudy/Rain/Storm/Snow) with altitude-based rules |
| `Atmosphere/WindController.cs` | `SWEF.Atmosphere` | Wind simulation with gusts, Perlin noise direction, weather-scaled strength |
| `UI/MiniMapController.cs` | `SWEF.UI` | Top-down mini-map with configurable zoom, position, and altitude-adaptive scaling |
| `UI/CameraUI.cs` | `SWEF.UI` | Camera mode switching buttons and mode display text |
| `UI/WeatherUI.cs` | `SWEF.UI` | Weather info display, wind indicator, manual weather override dropdown |

### Modified Scripts

| Script | Change |
|--------|--------|
| `UI/HudBinder.cs` | Added camera cycle button wiring |
| `Atmosphere/AtmosphereController.cs` | Defers fog control to WeatherController when weather is active |

### Setup

#### 1. CameraController
1. On the PlayerRig GameObject, attach `CameraController` script
2. Assign `Main Camera` reference (or leave empty for auto-find)
3. Adjust third-person offset, orbit distance, and cinematic speed in Inspector
4. Default mode: FirstPerson

#### 2. WeatherController
1. Create empty GameObject `WeatherController` in World scene
2. Attach `WeatherController` script
3. Create ParticleSystems for rain and snow; assign them
4. Assign `Sun Light` → your Directional Light
5. Auto weather changes every 120 seconds by default

#### 3. WindController
1. Create empty GameObject `WindController` in World scene
2. Attach `WindController` script
3. References to FlightController and WeatherController are auto-found
4. Adjust max wind force and gust parameters in Inspector

#### 4. MiniMapController
1. Create a secondary Camera named `MiniMapCamera` — set Clear Flags to Solid Color, Culling Mask as needed
2. Create a RenderTexture (256×256) and assign to the camera
3. On the HUD Canvas, add a `RawImage` for the mini-map display
4. Create `MiniMapController` GameObject, attach script, wire references

#### 5. CameraUI
1. On the HUD Canvas, add a Button for camera cycling and a Text for mode display
2. Optionally add 4 individual mode buttons
3. Create `CameraUI` GameObject, attach script, wire references

#### 6. WeatherUI
1. On the HUD Canvas, add Text elements for weather and wind info
2. Add an optional Image for weather icon and a Dropdown for manual override
3. Prepare 5 weather icon sprites (Clear/Cloudy/Rain/Storm/Snow)
4. Create `WeatherUI` GameObject, attach script, wire references

### Updated Architecture
```
World Scene (Phase 9)
  ├── WeatherController              ← NEW
  ├── WindController                 ← NEW
  ├── AtmosphereController           (modified — defers fog to WeatherController)
  ├── PlayerRig
  │   ├── FlightController
  │   ├── CameraController           ← NEW
  │   ├── TouchInputRouter
  │   ├── AltitudeController
  │   └── Main Camera
  ├── MiniMapCamera                  ← NEW (secondary camera)
  └── HUD Canvas
      ├── HudBinder                  (modified — camera cycle button)
      ├── MiniMapController + RawImage  ← NEW
      ├── CameraUI                   ← NEW
      └── WeatherUI                  ← NEW
```

## Phase 10 — Data Persistence, Cloud Save & Flight Journal

Phase 10 introduces a centralised JSON save system that replaces fragmented PlayerPrefs storage, adds optional cloud backup, a flight-session journal, and automated saving.

### New Scripts

| Script | Namespace | Purpose |
|--------|-----------|---------|
| `Core/SaveManager.cs` | `SWEF.Core` | JSON-based singleton save/load system (DontDestroyOnLoad). Central store for all game data. |
| `Core/CloudSaveController.cs` | `SWEF.Core` | Optional REST-API cloud backup — uploads (PUT) and downloads (GET) the save JSON. |
| `Core/FlightJournal.cs` | `SWEF.Core` | Records each flight session: start location, max altitude, duration, distance. |
| `UI/FlightJournalUI.cs` | `SWEF.UI` | Paged journal viewer (5 entries/page) with stats display and delete buttons. |
| `Core/DataMigrator.cs` | `SWEF.Core` | One-time migration of legacy PlayerPrefs keys to the JSON save file. |
| `Core/AutoSaveController.cs` | `SWEF.Core` | Periodic auto-save (default 60 s interval, minimum 10 s); also saves on pause/quit. |

### Modified Scripts

| Script | Change |
|--------|--------|
| `Core/BootManager.cs` | Before loading the World scene, logs a warning if `SaveManager` is absent from the scene. |
| `Settings/SettingsManager.cs` | In `Awake`, auto-finds `SaveManager`. `Load()` reads from both PlayerPrefs and SaveManager (SaveManager takes precedence when a save file exists). `Save()` writes to both. |

### Setup

#### 1. SaveManager
1. Create an empty GameObject in the **Boot scene** named `SaveManager`.
2. Attach the `SaveManager` script — `DontDestroyOnLoad` is applied automatically.
3. The save file is written to `Application.persistentDataPath/swef_save.json`.
4. All other Phase 10 scripts auto-find `SaveManager` via `FindFirstObjectByType`.

#### 2. CloudSaveController
1. Create an empty GameObject in the World scene (or Boot scene) named `CloudSaveController`.
2. Attach the `CloudSaveController` script.
3. Fill in **Cloud Endpoint Url** and optionally **Auth Token** in the Inspector.
4. Enable **Auto Sync On Save** to upload automatically after every local save.
5. `IsConfigured` returns `false` (and all operations are no-ops) until the URL is set.

#### 3. FlightJournal
1. Attach `FlightJournal` to the **PlayerRig** (or any active World-scene GameObject).
2. Recording starts automatically on `Start()` and stops on pause / destroy.
3. Set **Max Entries** (default 100) to control how many sessions are retained.
4. Distance tracking uses the world-space position of the host GameObject.

#### 4. FlightJournalUI
1. Create a panel GameObject in the **HUD Canvas** named `FlightJournalPanel`.
2. Attach `FlightJournalUI` to a controller GameObject in the same canvas.
3. Assign **Journal Panel**, **5 Entry Row** GameObjects, **Delete Buttons**, **Stats Labels**, and **Paging** controls in the Inspector.
4. Each entry row must contain at least 4 `Text` children: `[0]` date, `[1]` max altitude, `[2]` duration, `[3]` distance.
5. Call `Toggle()` from a HUD button to open/close the panel.

#### 5. DataMigrator
1. Create an empty GameObject in the **Boot scene** (after `SaveManager`) named `DataMigrator`.
2. Attach the `DataMigrator` script — migration runs on `Start()`.
3. Once migration completes, `SWEF_DataMigrated_v1 = 1` is written to PlayerPrefs so it never re-runs.

#### 6. AutoSaveController
1. Create an empty GameObject in the World scene named `AutoSaveController`.
2. Attach the `AutoSaveController` script.
3. Adjust **Auto Save Interval Sec** (minimum 10 s, default 60 s) in the Inspector.
4. Toggle **Enable Auto Save** at runtime via the public property.

### Updated Architecture
```
Boot Scene (Phase 10)
  ├── SaveManager (DontDestroyOnLoad)        ← NEW
  ├── DataMigrator                           ← NEW
  ├── BootManager  (checks SaveManager)      ← MODIFIED
  └── (all existing Boot-scene systems)

World Scene (Phase 10)
  ├── CloudSaveController                    ← NEW
  ├── FlightJournal  (on PlayerRig)          ← NEW
  ├── AutoSaveController                     ← NEW
  ├── SettingsManager (reads/writes SaveMgr) ← MODIFIED
  └── HUD Canvas
      └── FlightJournalUI                    ← NEW

DontDestroyOnLoad (persistent singletons)
  ├── SaveManager                            ← NEW
  ├── AudioManager
  ├── LocalizationManager
  └── DeepLinkHandler
```

### SaveData Schema

```
SaveData
  ├── saveVersion          (int)
  ├── lastSavedAt          (ISO-8601 string)
  ├── keyValues[]          ← generic key-value store (replaces scattered PlayerPrefs)
  │     └── { key, value }
  ├── favorites[]          ← FavoriteEntry { id, name, lat, lon, alt, savedAt }
  ├── journal[]            ← JournalEntry  { id, startLocation, lat, lon, maxAltKm,
  │                                          durationSec, distanceKm, notes, recordedAt }
  ├── totalFlights         (int)
  ├── totalFlightTimeSec   (float)
  ├── allTimeMaxAltitudeKm (float)
  └── totalDistanceKm      (float)
```

## Phase 11 — Editor Tools, Debugging & Testing Infrastructure

Phase 11 adds a comprehensive suite of Editor tools, an in-game debug overlay, gizmo helpers, test utilities, and a runtime performance profiler.

### New Scripts

| Script | Namespace | Purpose |
|--------|-----------|---------|
| `Editor/SWEFEditorWindow.cs` | `SWEF.Editor` | Custom Unity Editor window (SWEF → Dashboard). Shows script counts per folder, scene list, active build target, and quick-action buttons. |
| `Editor/SWEFSceneValidator.cs` | `SWEF.Editor` | Menu item (SWEF → Validate World Scene). Checks for all required components and logs pass/fail with `[SWEF Validator]` prefix. |
| `Editor/SWEFBuildPreprocessor.cs` | `SWEF.Editor` | `IPreprocessBuildWithReport` (callbackOrder = 0). Validates scenes in Build Settings, checks Boot scene for SaveManager, warns if DebugConsole is active in release builds. |
| `Core/DebugConsole.cs` | `SWEF.Core` | In-game overlay toggled by 3-finger tap or backtick. Scrollable log (last 50 messages), real-time FPS/altitude/position/memory stats, command input field. |
| `Core/DebugGizmoDrawer.cs` | `SWEF.Core` | Editor-only gizmos: altitude-coloured wire sphere at player position, line to georeference origin, altitude label via `Handles.Label`. |
| `Util/SWEFTestHelpers.cs` | `SWEF.Util` | Static testing utilities: `CreateMockSession`, `SimulateAltitudeChange` (coroutine), `CreateTestPlayerRig`, `ResetAllPlayerPrefs`, `GetSaveFilePath`. |
| `Util/PerformanceProfiler.cs` | `SWEF.Util` | Runtime frame-time circular buffer (default 300 frames). Properties: `AverageFPS`, `MinFPS`, `MaxFPS`, `AverageFrameTimeMs`, `FrameTimeP99`. Methods: `StartBenchmark(durationSec)`, `GetReport()`. Event: `OnBenchmarkComplete`. |

### Modified Scripts

| Script | Change |
|--------|--------|
| `Core/BootManager.cs` | Logs `[SWEF] Boot sequence started — Phase 11 debug infrastructure available` at the start of `Start()`. After `SceneManager.LoadScene`, logs `[SWEF] Scene load requested: {worldSceneName}`. |

### Setup

#### 1. SWEFEditorWindow
- Open via **SWEF → Dashboard** in the Unity menu bar.
- No scene or inspector setup required — the window reads the project on demand.
- **Quick Actions**: *Open Boot Scene*, *Open World Scene*, *Clear PlayerPrefs*, *Delete Save File*, *Refresh*.

#### 2. SWEFSceneValidator
- Open the World scene, then run **SWEF → Validate World Scene**.
- The Console will list each required component as ✓ (found) or ✗ (missing).
- Required: `CesiumGeoreference`, `Cesium3DTileset`, `FlightController`, `TouchInputRouter`, `AltitudeController`, `HudBinder`, `AtmosphereController`, `SaveManager`.

#### 3. SWEFBuildPreprocessor
- Runs automatically before every build (no manual trigger needed).
- Ensure both `Boot` and `World` scenes are added to Build Settings to avoid errors.
- `SaveManager` must exist in the Boot scene.

#### 4. DebugConsole
1. Add a `Canvas` to the World scene (set render mode to *Screen Space — Overlay*).
2. Attach `DebugConsole` to a persistent GameObject.
3. Assign **Debug Canvas**, **Log Scroll Rect**, **Log Text Prefab**, **Command Input**, and **Stats Text** in the Inspector.
4. Connect the InputField's `OnEndEdit` event to `DebugConsole.OnSubmitCommand`.

**Toggle:** 3-finger tap (mobile) or **backtick (`)** key (desktop).

**Commands:**
| Command | Effect |
|---------|--------|
| `clear` | Clears the on-screen log |
| `teleport lat,lon` | Teleports to coordinates via `TeleportController` |
| `save` | Triggers `SaveManager.Save()` |
| `load` | Triggers `SaveManager.Load()` |
| `fps` | Toggles the FPS counter |

#### 5. DebugGizmoDrawer
1. Attach `DebugGizmoDrawer` to the PlayerRig or any scene GameObject.
2. Assign **Player Rig**, **Altitude Controller**, and optionally **Georeference Origin** in the Inspector.
3. Gizmos are visible only in the Scene view (Unity Editor) and during play mode with Gizmos enabled.

#### 6. SWEFTestHelpers
- Pure static API — no GameObject attachment required.
- `SWEFTestHelpers.CreateMockSession(lat, lon, alt)` → `MockSession` struct.
- `SWEFTestHelpers.CreateTestPlayerRig()` → minimal player hierarchy.
- `SWEFTestHelpers.GetSaveFilePath()` → persistent data path for the save file.

#### 7. PerformanceProfiler
1. Attach `PerformanceProfiler` to any active GameObject.
2. Adjust **Frame Window** (default 300) in the Inspector.
3. Call `StartBenchmark(seconds)` from code or UI button.
4. Subscribe to `OnBenchmarkComplete` to receive the report string.
5. Read `AverageFPS`, `MinFPS`, `MaxFPS`, `AverageFrameTimeMs`, `FrameTimeP99` at any time.

### Editor Tools Usage Guide

#### Opening the Dashboard
```
Unity menu bar → SWEF → Dashboard
```
The window shows a live count of `.cs` files in each `Assets/SWEF/Scripts/<Folder>` directory, all scenes in Build Settings, the active build target, and quick-action buttons.

#### Running the Scene Validator
```
Unity menu bar → SWEF → Validate World Scene
```
Open the World scene first. The validator iterates all loaded scenes and DontDestroyOnLoad objects. Results appear in the Console with `[SWEF Validator]` prefix.

#### Build Preprocessor
The `SWEFBuildPreprocessor` runs automatically when you trigger a build. Inspect the Console for `[SWEF Build]` messages. Fix any `LogError` messages before shipping.

### Debug Console Commands Reference

| Command | Example | Description |
|---------|---------|-------------|
| `clear` | `clear` | Erases on-screen log entries |
| `teleport` | `teleport 48.8566,2.3522` | Teleports player to lat/lon (Paris) |
| `save` | `save` | Immediately writes save file to disk |
| `load` | `load` | Reloads save data from disk |
| `fps` | `fps` | Toggles FPS line in the stats panel |

### Updated Architecture

```
Boot Scene (Phase 11)
  ├── SaveManager (DontDestroyOnLoad)
  ├── DataMigrator
  ├── BootManager  (Phase 11 log added)     ← MODIFIED
  └── (all existing Boot-scene systems)

World Scene (Phase 11)
  ├── DebugConsole  (toggle: ` / 3-finger)  ← NEW
  ├── DebugGizmoDrawer  (on PlayerRig)      ← NEW
  ├── PerformanceProfiler                   ← NEW
  └── (all existing World-scene systems)

Editor Tools (Phase 11)  [Editor-only, #if UNITY_EDITOR]
  ├── SWEF → Dashboard         (SWEFEditorWindow)      ← NEW
  ├── SWEF → Validate World Scene (SWEFSceneValidator) ← NEW
  └── IPreprocessBuildWithReport  (SWEFBuildPreprocessor) ← NEW

Util (Phase 11)
  ├── SWEFTestHelpers  (static, no MonoBehaviour)      ← NEW
  └── PerformanceProfiler  (MonoBehaviour)             ← NEW
```

---

## Phase 12 — In-App Purchase (IAP), Monetization & Premium Features

### New Scripts

| Script | Namespace | Role |
|--------|-----------|------|
| `IAP/IAPProductCatalog.cs` | `SWEF.IAP` | Static catalog of product IDs and metadata |
| `IAP/IAPManager.cs` | `SWEF.IAP` | Unity IAP singleton wrapper (purchase / restore) |
| `IAP/IAPRestoreButton.cs` | `SWEF.IAP` | iOS restore-purchases UI button |
| `UI/StoreUI.cs` | `SWEF.UI` | In-app store panel with product list |
| `Core/AdManager.cs` | `SWEF.Core` | Banner / interstitial / rewarded ad stub |
| `Core/PremiumFeatureGate.cs` | `SWEF.Core` | Static utility for gating premium features |
| `UI/PremiumPromptUI.cs` | `SWEF.UI` | "Upgrade to Premium" modal dialog |

### Modified Scripts

| Script | Change |
|--------|--------|
| `Core/AnalyticsLogger.cs` | Added `Instance` singleton + static `LogEvent(eventName, value)` |
| `Favorites/FavoriteManager.cs` | Free-tier capped at 10 favorites; shows `PremiumPromptUI` on overflow |
| `Core/CloudSaveController.cs` | `Upload()` and `Download()` gated behind `PremiumFeature.CloudSave` |
| `Screenshot/ScreenshotController.cs` | 2× super-sampling when `PremiumFeature.HighResScreenshot` is unlocked |

### Product Catalog

| Product ID | Type | Default Price | Description |
|------------|------|---------------|-------------|
| `swef_premium` | Non-consumable | $4.99 | Unlock all premium features |
| `swef_remove_ads` | Non-consumable | $1.99 | Remove all ads permanently |
| `swef_donation_small` | Consumable | $0.99 | Small tip ☕ |
| `swef_donation_medium` | Consumable | $2.99 | Medium tip 🍕 |
| `swef_donation_large` | Consumable | $9.99 | Large tip 🚀 |

### Premium Feature Mapping

| PremiumFeature | Required product | Free-tier limit |
|----------------|-----------------|-----------------|
| `UnlimitedFavorites` | `swef_premium` | Max 10 favorites |
| `CloudSave` | `swef_premium` | Disabled |
| `AdvancedWeather` | `swef_premium` | Basic weather only |
| `CustomSkins` | `swef_premium` | Default skin only |
| `AdFree` | `swef_premium` **or** `swef_remove_ads` | Ads shown |
| `FlightJournalExport` | `swef_premium` | No export |
| `HighResScreenshot` | `swef_premium` | Native resolution |

### Setup Instructions

#### IAPManager
1. Add a persistent GameObject (e.g. in the Boot scene) and attach `IAPManager`.
2. Install **Unity In App Purchasing** via the Unity Package Manager (com.unity.purchasing).
3. The script uses `#if UNITY_PURCHASING` guards — it runs as a safe stub without the package.
4. Non-consumable purchases are persisted to `PlayerPrefs` under keys `swef_iap_{productId}`.

#### AdManager
1. Attach `AdManager` to the same persistent Boot-scene GameObject as `IAPManager`.
2. Set **Interstitial Cooldown Seconds** in the Inspector (default: 180 s).
3. Replace the `// TODO: replace with real SDK call` comments with your ad provider's API.
4. All Show methods are automatically no-ops when `IAPManager.IsAdFree` is `true`.

#### PremiumFeatureGate
- Pure static class — no GameObject needed.
- Call `PremiumFeatureGate.IsUnlocked(feature)` to check access.
- Call `PremiumFeatureGate.TryAccess(feature, onGranted, onDenied)` for guarded actions.

#### StoreUI
1. Create a Canvas panel for the store.
2. Attach `StoreUI` and wire: **Store Panel**, **Canvas Group**, **Content Parent**, **Product Item Prefab**, **Close Button**, **Restore Button**.
3. The prefab must have a `ProductItemUI` component with label and button references.
4. Call `StoreUI.Open()` / `StoreUI.Close()` from other scripts.

#### PremiumPromptUI
1. Create a modal Canvas panel and attach `PremiumPromptUI`.
2. Wire: **Prompt Panel**, **Canvas Group**, **Feature Name Label**, **Feature Description Label**, **Upgrade Button**, **Maybe Later Button**, **Watch Ad Button** (optional), **StoreUI** reference.
3. Call `PremiumPromptUI.Instance.Show(PremiumFeature.X)` from any gated code path.

#### IAPRestoreButton
1. Add a Button to your store UI Canvas.
2. Attach `IAPRestoreButton` — it auto-hides on non-iOS platforms.
3. Optionally wire a **Status Label** Text component for restore feedback.

### Updated Architecture

```
Boot Scene (Phase 12)
  ├── IAPManager     (DontDestroyOnLoad)            ← NEW
  ├── AdManager      (DontDestroyOnLoad)            ← NEW
  ├── AnalyticsLogger  (Instance + LogEvent added)  ← MODIFIED
  └── (all existing Boot-scene systems)

World Scene (Phase 12)
  ├── StoreUI        (Canvas panel, fade in/out)    ← NEW
  ├── PremiumPromptUI (modal dialog, singleton)     ← NEW
  └── (all existing World-scene systems)

IAP Layer (Phase 12)
  ├── IAPProductCatalog  (static, no MonoBehaviour) ← NEW
  ├── IAPManager         (Unity IAP wrapper)        ← NEW
  └── IAPRestoreButton   (Button component, iOS)    ← NEW

Premium Gates (Phase 12)
  ├── PremiumFeatureGate  (static utility)          ← NEW
  ├── FavoriteManager     (10-favorite free cap)    ← MODIFIED
  ├── CloudSaveController (sync gated)              ← MODIFIED
  └── ScreenshotController (high-res gated)         ← MODIFIED
```

---

## Phase 13 — Notification System, Rate Prompt & App Lifecycle Management

### New Scripts

| Script | Namespace | Role |
|--------|-----------|------|
| `Notification/NotificationManager.cs` | `SWEF.Notification` | Singleton — schedules/cancels local notifications; auto-schedules on pause |
| `Notification/NotificationSettings.cs` | `SWEF.Notification` | Channel config (IDs, icons) for Android/iOS |
| `Core/RatePromptManager.cs` | `SWEF.Core` | Singleton — evaluates rate-prompt conditions, triggers native review API |
| `Core/RatePromptUI.cs` | `SWEF.Core` | Optional fallback UI panel ("Rate Now / Later / Never") with fade |
| `Core/AppLifecycleManager.cs` | `SWEF.Core` | Singleton (DontDestroyOnLoad) — Active/Paused/Background/Quitting state machine, auto-save, session init |
| `Core/SessionTracker.cs` | `SWEF.Core` | Per-session metrics (altitude, speed, distance, teleports, screenshots) |

### Setup Instructions

#### NotificationManager
1. Add a persistent Boot-scene GameObject and attach `NotificationManager`.
2. Optionally attach a `NotificationSettings` component on the same object and reference it.
3. Install **Unity Mobile Notifications** via Package Manager (`com.unity.mobile.notifications`). The script compiles without it using `#if UNITY_ANDROID` / `#if UNITY_IOS` guards.
4. Call `NotificationManager.Instance.RequestPermission()` from `BootManager` (already wired in Phase 13).

#### NotificationSettings
- Component-based config; attach to the same GameObject as `NotificationManager`.
- Set `channelId`, `channelName`, `channelDescription`, `smallIconId`, `largeIconId` in the Inspector.
- Small/large icon IDs must match drawable resource names in the Android project.

#### RatePromptManager
1. Add to a World-scene persistent GameObject and attach `RatePromptManager`.
2. Fill **iOS App ID** and optionally override **Android Package Name** in the Inspector.
3. `CheckAndPrompt()` is called automatically by `AppLifecycleManager` on app resume.
4. For testing, call `RatePromptManager.Instance.ResetRateData()` from the debug console.

#### RatePromptUI
1. Create a Canvas panel (preferably on a separate "Overlay" canvas sorted above HUD).
2. Attach `RatePromptUI` and wire **Rate Panel**, **Canvas Group**, **Rate Button**, **Later Button**, **Never Button**.
3. Show via `RatePromptUI.Instance.Show()` when `RatePromptManager` determines native UI is unavailable.

#### AppLifecycleManager
1. Add to the Boot scene and attach `AppLifecycleManager` — it survives via `DontDestroyOnLoad`.
2. Set **Auto Save Interval Seconds** (default 60 s) in the Inspector.
3. Subscribe to `AppLifecycleManager.OnAppStateChanged` from any script that needs lifecycle events.
4. `InitSession()` is called automatically on `Awake`; `BootManager` also calls it for explicit ordering.

#### SessionTracker
1. Add to the World scene (a new `SessionTracker` GameObject).
2. No Inspector configuration required.
3. `StatsDashboard` feeds altitude/speed automatically (Phase 13).
4. Call `SessionTracker.Instance.IncrementTeleport()` from `TeleportController` and `IncrementScreenshot()` from `ScreenshotController` for full tracking.

### Modified Scripts

| Script | Change |
|--------|--------|
| `Settings/SettingsManager.cs` | Added `NotificationsEnabled` property, `KeyNotificationsEnabled` PlayerPrefs key, `OnNotificationSettingChanged` event, and `SetNotificationsEnabled()` setter |
| `Settings/SettingsUI.cs` | Added `notificationsToggle` field; wired to `SetNotificationsEnabled()` and `NotificationManager.CancelAll()` |
| `Core/BootManager.cs` | Calls `AppLifecycleManager.InitSession()` and `NotificationManager.RequestPermission()` on boot |
| `UI/StatsDashboard.cs` | Per-frame altitude, speed, and distance fed into `SessionTracker.Instance` |

### Updated Architecture

```
Boot Scene (Phase 13)
  ├── BootManager (+ AppLifecycleManager.InitSession, NotificationManager.RequestPermission)
  ├── LoadingScreen
  ├── ErrorHandler
  ├── AppLifecycleManager          ← NEW (DontDestroyOnLoad)
  ├── NotificationManager          ← NEW (DontDestroyOnLoad)
  └── AudioManager (DontDestroyOnLoad)

World Scene (Phase 13)
  ├── RatePromptManager            ← NEW
  ├── SessionTracker               ← NEW
  ├── SettingsManager (+ notificationsEnabled, OnNotificationSettingChanged)  ← MODIFIED
  └── HUD Canvas
      ├── Settings Panel (+ Notification Toggle)  ← MODIFIED
      ├── StatsDashboard (+ SessionTracker feed)  ← MODIFIED
      └── Rate Prompt Panel (RatePromptUI)        ← NEW
```

---

## Phase 15 — XR/VR Support Foundation

### New Scripts

| Script | Namespace | Role |
|--------|-----------|------|
| `XR/XRPlatformDetector.cs` | `SWEF.XR` | Static utility — detects XR platform at runtime (lazy, cached) |
| `XR/XRRigManager.cs` | `SWEF.XR` | Singleton — manages XR camera rig and mobile/VR mode switching |
| `XR/XRInputAdapter.cs` | `SWEF.XR` | MonoBehaviour — bridges XR controller input to FlightController |
| `XR/XRHandTracker.cs` | `SWEF.XR` | MonoBehaviour stub — future hand tracking support |
| `XR/XRComfortSettings.cs` | `SWEF.XR` | MonoBehaviour — VR comfort/anti-motion-sickness settings |
| `XR/XRUIAdapter.cs` | `SWEF.XR` | MonoBehaviour — converts Canvas UI to VR world-space rendering |
| `Settings/XRSettingsUI.cs` | `SWEF.Settings` | MonoBehaviour — XR settings panel (comfort, recenter, UI distance) |

### Modified Scripts

| Script | Change |
|--------|--------|
| `Core/BootManager.cs` | Added XR detection after boot sequence; logs device name when XR active |
| `Settings/SettingsUI.cs` | Added XR header with `xrSettingsButton` and `xrSettingsPanel`; button shown only when XR active |
| `Flight/FlightController.cs` | Added `SetInputFromXR(throttle, yaw, pitch, roll)` public API for XRInputAdapter |

### XR Platform Support Matrix

| Platform | Status | Notes |
|----------|--------|-------|
| Meta Quest (2/3/Pro) | Foundation ready | Detected via `"oculus"/"meta"/"quest"` device name |
| Apple Vision Pro | Planned | Detected via `"realitykit"/"visionos"/"polyspatial"` device name |
| PC VR (SteamVR / OpenVR) | Foundation ready | Detected via `"openvr"/"steamvr"/"windowsmr"` device name |
| Mobile (iOS/Android) | Unchanged | Default mode when XR not active |

### XR Comfort Level Presets

| Level | Snap Turning | Tunnel Vision | Max Turn Speed |
|-------|-------------|---------------|----------------|
| Low | Off | Off | 90 °/s |
| Medium *(default)* | Off | On | 60 °/s |
| High | On | On | 45 °/s |
| Custom | User-set | User-set | User-set |

Comfort settings are persisted to PlayerPrefs under keys:
- `SWEF_XR_ComfortLevel`
- `SWEF_XR_SnapTurning`
- `SWEF_XR_TunnelVision`

### XR Input Mapping

| Controller Input | Action |
|-----------------|--------|
| Left thumbstick Y | Throttle (`FlightController.SetThrottle`) |
| Left thumbstick X | Yaw |
| Right thumbstick X | Roll |
| Right thumbstick Y | Pitch |
| Right trigger | Altitude up (boost) |
| Left trigger | Altitude down (descend) |
| Left grip | Toggle comfort mode |
| Right grip | Screenshot |
| Menu button (left) | Pause |
| Secondary button (right) | Toggle HUD visibility |

### Setup Instructions

#### XRPlatformDetector
- Pure static class — no GameObject required.
- All calls guarded by `#if UNITY_XR_MANAGEMENT`; safe to call without the XR Management package.
- Returns `XRPlatformType.None` and `IsXRActive = false` when XR packages are absent.

#### XRRigManager
1. Add a persistent World-scene (or DontDestroyOnLoad) GameObject and attach `XRRigManager`.
2. Wire **Mobile Rig** (PlayerRig) and optionally **XR Rig** (XR Origin) in the Inspector.
3. On Awake, the manager auto-detects XR and switches mode if a headset is connected.
4. If `xrRig` is `null`, the manager gracefully falls back to mobile mode and logs a warning.
5. Call `XRRigManager.Instance.RecenterXR()` from a button to recenter the headset.

#### XRInputAdapter
1. Attach to the same GameObject as `XRRigManager` or the PlayerRig.
2. `FlightController` and `AltitudeController` are auto-found if not assigned.
3. Requires `#define UNITY_XR_MANAGEMENT` (set automatically by the XR Management package).
4. Without the package the component disables itself on Awake.

#### XRHandTracker
- Attach to the XR Rig if desired.
- `enableHandTracking` must be `true` **and** `XRPlatformDetector.IsHandTrackingAvailable` must return `true` before any processing occurs.
- All gesture logic is stubbed — implement with the **XR Hands** package when ready.

#### XRComfortSettings
1. Attach to the World-scene persistent manager GameObject.
2. `ComfortVignette` is auto-found if not assigned.
3. Call `SetComfortLevel(ComfortLevel.High)` programmatically to enforce a preset.

#### XRUIAdapter
1. Attach to a UI manager GameObject in the World scene.
2. Assign all HUD canvases in the `uiCanvases` array.
3. The adapter automatically listens to `XRRigManager.OnRigModeChanged` and converts canvases accordingly.
4. Adjust `worldSpaceDistance` (default 2 m) and `worldSpaceScale` (default 0.001) in the Inspector.

#### XRSettingsUI
1. Create a settings panel Canvas and attach `XRSettingsUI`.
2. Wire: **XR Settings Panel**, **Comfort Level Dropdown**, **Snap Turning Toggle**, **Tunnel Vision Toggle**, **UI Distance Slider** (1–5 m), **Follow Head Toggle**, **Recenter Button**, **Platform Info Text**.
3. The panel's visibility is automatically controlled by `XRPlatformDetector.IsXRActive`.

### Updated Architecture

```
Boot Scene (Phase 15)
  └── BootManager (+ XR detection on boot)  ← MODIFIED

World Scene (Phase 15)
  ├── XRRigManager    (singleton, rig switching)    ← NEW
  ├── XRInputAdapter  (controller → flight input)   ← NEW
  ├── XRHandTracker   (hand tracking stub)          ← NEW
  ├── XRComfortSettings (VR comfort presets)        ← NEW
  ├── XRUIAdapter     (Canvas → WorldSpace)         ← NEW
  └── HUD Canvas
      └── XR Settings Panel  (XRSettingsUI)        ← NEW

XR Module (Phase 15)
  ├── XRPlatformDetector  (static utility)           ← NEW
  ├── XRRigManager        (singleton MonoBehaviour)  ← NEW
  ├── XRInputAdapter      (XR → SWEF input bridge)   ← NEW
  ├── XRHandTracker       (hand tracking stub)       ← NEW
  ├── XRComfortSettings   (comfort presets + prefs)  ← NEW
  └── XRUIAdapter         (world-space canvas adapter) ← NEW

Flight Layer (Phase 15)
  └── FlightController (+ SetInputFromXR)  ← MODIFIED

Settings Layer (Phase 15)
  ├── SettingsUI    (+ xrSettingsButton / xrSettingsPanel)  ← MODIFIED
  └── XRSettingsUI  (XR comfort / recenter panel)           ← NEW
```

---

## Phase 16 — Accessibility Enhancement & Haptic Feedback System

### New Scripts

| Script | Namespace | Purpose |
|--------|-----------|---------|
| `Haptic/HapticManager.cs` | `SWEF.Haptic` | Singleton haptic/vibration hub — iOS Taptic, Android VibrationEffect, Editor stub |
| `Haptic/HapticPattern.cs` | `SWEF.Haptic` | Enum of all named haptic patterns |
| `Haptic/HapticTriggerZone.cs` | `SWEF.Haptic` | Bridges gameplay events (altitude, boost, stall, teleport, screenshot, achievements) to HapticManager |
| `UI/AccessibilityController.cs` | `SWEF.UI` | Colorblind modes, dynamic text scaling, screen-reader announcements, reduced-motion propagation |
| `UI/ColorblindMode.cs` | `SWEF.UI` | Enum: Normal, Protanopia, Deuteranopia, Tritanopia, Achromatopsia |
| `UI/OneHandedModeController.cs` | `SWEF.UI` | Repositions HUD elements to a single-hand reach zone (Left or Right) |
| `UI/VoiceCommandManager.cs` | `SWEF.UI` | Text-based voice-command stub; parses transcripts against keyword table |
| `UI/VoiceCommand.cs` | `SWEF.UI` | Enum of all recognised voice commands |
| `Settings/AccessibilitySettingsUI.cs` | `SWEF.Settings` | Full accessibility settings panel: colorblind, text scale, reduced motion, one-handed, haptics, voice, screen reader |

### Modified Scripts

| Script | Change |
|--------|--------|
| `Core/BootManager.cs` | Calls `AccessibilityController.ApplySavedSettings()` after XR detection |
| `Settings/SettingsUI.cs` | Added `[Header("Accessibility")]` — `accessibilitySettingsButton` + `accessibilitySettingsPanel` (nullable) |
| `Settings/SettingsManager.cs` | Added `HapticsEnabled`, `HapticIntensity` properties, `SetHapticsEnabled/Intensity` setters, `OnHapticsSettingChanged`/`OnHapticIntensityChanged` static events |
| `Flight/FlightController.cs` | Added `OnBoostStarted`, `OnBoostEnded`, `OnStallWarning` events; boost/stall detection in `SetThrottle`/`Step` |
| `UI/AccessibilityManager.cs` | Added `Controller` property (returns `FindFirstObjectByType<AccessibilityController>()`) and `Announce(string)` convenience wrapper |

### Haptic Pattern Reference

| Pattern | Platform Behaviour | Duration |
|---------|-------------------|---------|
| Light | iOS: UIImpactFeedbackStyleLight / Android: 10 ms amp 80 | 10 ms |
| Medium | iOS: UIImpactFeedbackStyleMedium / Android: 25 ms amp 128 | 25 ms |
| Heavy | iOS: UIImpactFeedbackStyleHeavy / Android: 50 ms amp 255 | 50 ms |
| Success | Double-tap (15 ms + pause) | 15 ms |
| Warning | Single medium pulse | 40 ms |
| Error | Long heavy pulse | 80 ms |
| AltitudeWarning | 3 × 20 ms pulses, 50 ms gap | 3 × 20 ms |
| TeleportComplete | Rising 3-step (10/20/30 ms) | 60 ms total |
| ScreenshotSnap | Single crisp pulse | 15 ms |
| AchievementUnlock | Rising 5-step celebration | 5 × 10 ms |
| Boost | Continuous light (repeating via coroutine) | Loop |
| Stall | Heavy double-pulse | 40 ms × 2 |

### Colorblind Modes

| Mode | Description |
|------|-------------|
| Normal | No correction |
| Protanopia | Red–green (red deficiency) |
| Deuteranopia | Red–green (green deficiency) |
| Tritanopia | Blue–yellow blindness |
| Achromatopsia | Total colour blindness (greyscale) |

### Accessibility Settings Reference

| Setting | PlayerPrefs Key | Default |
|---------|----------------|---------|
| Colorblind Mode | `SWEF_ColorblindMode` | 0 (Normal) |
| Text Scale | `SWEF_TextScale` | 1.0 |
| Reduced Motion | `SWEF_ReducedMotion` | false |
| One-Handed Mode | `SWEF_OneHandedMode` | false |
| Hand Preference | `SWEF_HandPreference` | 0 (Right) |
| Haptics Enabled | `SWEF_HapticsEnabled` | true |
| Haptic Intensity | `SWEF_HapticIntensity` | 1.0 |
| Voice Commands | — (VoiceCommandManager field) | false |
| Screen Reader | `SWEF_ScreenReaderEnabled` | false |

### Voice Command Keywords

| Phrase | Command |
|--------|---------|
| "screenshot", "take photo" | Screenshot |
| "teleport", "go to" | Teleport |
| "pause", "stop" | Pause |
| "resume", "continue" | Resume |
| "higher", "up" | AltitudeUp |
| "lower", "down" | AltitudeDown |
| "faster", "speed up" | SpeedUp |
| "slower", "slow down" | SlowDown |
| "hide hud", "show hud" | ToggleHUD |
| "recenter" | Recenter |

### One-Handed Mode Layout

- **Right-hand mode**: all `interactiveElements` are repositioned to the right 40% of the screen.
- **Left-hand mode**: all `interactiveElements` are repositioned to the left 40% of the screen.
- `reachZoneWidth` (default 0.4) controls the fraction of screen width used for the reach zone.
- Original positions are cached on `Awake` and fully restored when the mode is disabled.

### Setup Instructions

#### 1. HapticManager
1. Create empty GameObject `HapticManager` (Boot scene or persistent).
2. Attach `HapticManager` script — `DontDestroyOnLoad` handled automatically.
3. Adjust `Haptics Enabled` and `Haptic Intensity` in the Inspector (or leave defaults).

#### 2. HapticTriggerZone
1. Attach `HapticTriggerZone` to the player rig or a persistent World-scene object.
2. All managers are auto-found at `Start` — no Inspector wiring required.
3. Adjust altitude/speed thresholds and cooldowns as needed.

#### 3. AccessibilityController
1. Create empty GameObject `AccessibilityController` (Boot scene or persistent).
2. Attach `AccessibilityController` — `ApplySavedSettings()` is called on `Awake` and by `BootManager`.
3. No Inspector wiring required.

#### 4. OneHandedModeController
1. Attach `OneHandedModeController` to a persistent HUD/World object.
2. Assign the `Interactive Elements` array in the Inspector with the `RectTransform`s to reposition.
3. Call `SetOneHandedMode(true/false)` from `AccessibilitySettingsUI` or code.

#### 5. VoiceCommandManager
1. Attach `VoiceCommandManager` to any persistent object.
2. Subscribe to `OnVoiceCommandRecognized` from other systems (e.g., `ScreenshotController`, `PauseManager`).
3. Call `ProcessVoiceInput(transcript)` from your platform speech-recognition callback.

#### 6. AccessibilitySettingsUI
1. Add an `AccessibilitySettings` panel to your Settings Canvas.
2. Attach `AccessibilitySettingsUI` and wire all Dropdown/Slider/Toggle references in the Inspector.
3. Point `SettingsUI.accessibilitySettingsButton` at a Button that opens the panel.

### Updated Architecture

```
Boot Scene (Phase 16)
  └── BootManager (+ accessibility init after XR)  ← MODIFIED

World / Persistent (Phase 16)
  ├── HapticManager        (singleton, DontDestroyOnLoad)  ← NEW
  ├── HapticTriggerZone    (event bridge)                  ← NEW
  ├── AccessibilityController (colorblind, text scale, screen reader) ← NEW
  ├── OneHandedModeController (HUD repositioning)          ← NEW
  └── VoiceCommandManager  (voice command stub)            ← NEW

Settings Layer (Phase 16)
  ├── SettingsUI  (+ accessibilitySettingsButton/Panel)    ← MODIFIED
  ├── SettingsManager (+ HapticsEnabled, HapticIntensity)  ← MODIFIED
  └── AccessibilitySettingsUI (full accessibility panel)   ← NEW

Flight Layer (Phase 16)
  └── FlightController (+ OnBoostStarted/Ended/OnStallWarning) ← MODIFIED

Haptic Module (Phase 16)
  ├── HapticManager   (singleton, platform dispatch)       ← NEW
  ├── HapticPattern   (enum)                               ← NEW
  └── HapticTriggerZone (gameplay → haptic bridge)         ← NEW
```

---

## Phase 17 — Replay Sharing, Ghost Racing & Flight Path Visualization

### New Scripts (7)

| # | File | Namespace | Purpose |
|---|------|-----------|---------|
| 1 | `Replay/ReplayData.cs` | `SWEF.Replay` | Serializable replay file model; `FromFlightRecorder`, `ToJson`, `FromJson` |
| 2 | `Replay/ReplayFileManager.cs` | `SWEF.Replay` | Singleton file I/O — save / load / list / delete / export / import replays |
| 3 | `Replay/GhostRacer.cs` | `SWEF.Replay` | Binary-search interpolated ghost playback; live comparison stats |
| 4 | `Replay/FlightPathRenderer.cs` | `SWEF.Replay` | LineRenderer-based 3D path with altitude colour coding + Douglas–Peucker simplification |
| 5 | `Replay/ReplayShareManager.cs` | `SWEF.Replay` | Native share-sheet integration; deep-link encode/decode; clipboard import |
| 6 | `UI/ReplayBrowserUI.cs` | `SWEF.UI` | Paginated replay browser; sort, play, share, ghost-race, view path, delete |
| 7 | `UI/GhostRaceHUD.cs` | `SWEF.UI` | Race overlay HUD; time/altitude/speed delta; progress slider; pause/resume |

### Modified Scripts (5)

| File | Change |
|------|--------|
| `Recorder/FlightRecorder.cs` | Added `ExportToReplayData()`, `GetFrames()`, `GetRecordedDuration()` |
| `Recorder/RecorderUI.cs` | Added `saveReplayButton` and `openReplayBrowserButton` with handlers |
| `Core/DeepLinkHandler.cs` | Added path-based routing; `swef://replay?…` forwarded to `ReplayShareManager` |
| `Social/ShareManager.cs` | Added `ShareReplayText(string)` convenience wrapper |
| `Achievement/AchievementManager.cs` | Added `first_ghost_race` and `replay_shared` achievements |

### Replay File Format (.swefr)

Replay files are stored as UTF-8 JSON under `Application.persistentDataPath/Replays/`.
The extension is `.swefr`. Maximum file size is **10 MB**. Up to **50 replays** are
retained before the oldest are pruned by `CleanupOldReplays()`.

| Field | Type | Description |
|-------|------|-------------|
| `replayId` | string | GUID |
| `playerName` | string | Device display name |
| `version` | int | Format version (currently 1) |
| `createdAt` | string | ISO-8601 UTC timestamp |
| `totalDurationSec` | float | Total flight duration |
| `maxAltitudeM` | float | Peak altitude (metres) |
| `maxSpeedMps` | float | Peak speed (m/s) |
| `totalDistanceKm` | float | Flight distance (km) |
| `startLat` / `startLon` | double | Starting coordinates |
| `startLocationName` | string | Human-readable start location |
| `frames` | ReplayFrame[] | Per-frame flight state |

Each `ReplayFrame` captures: `time`, `px/py/pz` (position), `rx/ry/rz/rw` (quaternion), `altitude`, `speed`.

### Ghost Racing

1. Open Replay Browser → choose a saved replay → tap **Ghost Race**.
2. A semi-transparent ghost aircraft spawns at the starting position and flies
   the recorded path using binary-search frame interpolation (`Vector3.Lerp` /
   `Quaternion.Slerp`).
3. The **GhostRaceHUD** shows:
   - Time delta (ahead = green, behind = red, ≤0.5 s = white)
   - Altitude delta (↑ / ↓ metres)
   - Speed delta (km/h)
   - Progress slider + percentage
4. Completing a ghost race unlocks the **Ghost Hunter 👻** achievement.

### Flight Path Colour Legend

| Altitude Range | Colour |
|----------------|--------|
| 0 – 2,000 m | 🟢 Green |
| 2,000 – 20,000 m | 🟡 Yellow |
| 20,000 – 80,000 m | 🟠 Orange-Red |
| 80,000 – 120,000 m | 🔴 Red-Purple |
| 120,000 m+ | ⚪ Purple-White (space) |

Path simplification uses the **Douglas–Peucker** algorithm (default tolerance 10 m).
Maximum rendered points: **2,000** (configurable via `maxPoints`).

### Deep Link Format

| Scheme | Purpose |
|--------|---------|
| `swef://replay?id={replayId}` | Short link for cloud download (stub) |
| `swef://replay?id={id}&name={name}&alt={alt}&dur={dur}` | Metadata deep link |
| `swef://replay?data={base64}` | Inline encoded replay (≤ 10,000 chars) |

### Storage Management

- Max replays kept: **50** (call `ReplayFileManager.CleanupOldReplays()`)
- Max per-file size: **10 MB**
- Use `GetTotalReplaySizeBytes()` to display storage usage in `storageInfoText`

### Setup Instructions

#### 1. ReplayFileManager
1. Create empty GameObject `ReplayFileManager` (Boot scene).
2. Attach `ReplayFileManager` — `DontDestroyOnLoad` is handled automatically.

#### 2. GhostRacer
1. Attach `GhostRacer` to any World-scene object.
2. Assign `ghostPrefab` (semi-transparent aircraft model) in the Inspector.
3. Optionally wire `playerFlight` and `playerAltitude` for live comparison stats.

#### 3. FlightPathRenderer
1. Attach `FlightPathRenderer` to a World-scene GameObject.
2. A `LineRenderer` is added automatically if not present.
3. Optionally assign a custom `pathMaterial`.
4. Call `StartLiveTracking(playerRig.transform)` to enable real-time path display.

#### 4. ReplayShareManager
1. Attach `ReplayShareManager` to any World-scene object.
2. Wire `fileManager` and `shareManager` in the Inspector (or leave null for auto-find).

#### 5. ReplayBrowserUI
1. Add a Canvas panel for the replay browser.
2. Attach `ReplayBrowserUI` and assign all `[SerializeField]` references.
3. The `replayItemPrefab` must contain named children:
   `NameText`, `DateText`, `DurationText`, `AltitudeText`,
   `PlayButton`, `ShareButton`, `GhostRaceButton`, `DeleteButton`, `ViewPathButton`.

#### 6. GhostRaceHUD
1. Add a Canvas overlay for the race HUD.
2. Attach `GhostRaceHUD` and assign all `[SerializeField]` references.
3. The panel auto-shows on `GhostRacer.OnRaceStarted` and auto-hides 5 s after `OnRaceFinished`.

#### 7. RecorderUI (updated)
- Wire `saveReplayButton` → saves current recording as a `.swefr` file.
- Wire `openReplayBrowserButton` → opens `ReplayBrowserUI`.

### Updated Architecture

```
Boot Scene (Phase 17)
  └── ReplayFileManager (singleton, DontDestroyOnLoad)     ← NEW

World Scene (Phase 17)
  ├── GhostRacer        (replay playback + comparison)     ← NEW
  ├── FlightPathRenderer (LineRenderer path vis)           ← NEW
  ├── ReplayShareManager (share / import)                  ← NEW
  └── Canvas
      ├── ReplayBrowserUI (paginated list)                 ← NEW
      └── GhostRaceHUD   (race overlay)                    ← NEW

Recorder Layer (Phase 17)
  ├── FlightRecorder  (+ ExportToReplayData, GetFrames)    ← MODIFIED
  └── RecorderUI      (+ saveReplayButton, openBrowser)    ← MODIFIED

Core Layer (Phase 17)
  └── DeepLinkHandler (+ swef://replay routing)            ← MODIFIED

Social Layer (Phase 17)
  └── ShareManager    (+ ShareReplayText)                  ← MODIFIED

Achievement Layer (Phase 17)
  └── AchievementManager (+ first_ghost_race, replay_shared) ← MODIFIED

Replay Module (Phase 17)
  ├── ReplayData        (serializable model)               ← NEW
  ├── ReplayFileManager (file I/O, singleton)              ← NEW
  ├── GhostRacer        (ghost playback)                   ← NEW
  ├── FlightPathRenderer (3D path rendering)               ← NEW
  └── ReplayShareManager (share / import)                  ← NEW
```

---

## Phase 18 — Time-of-Day System, Photo Mode & Cinematic Camera

### New Scripts (6)

#### `Cinema/TimeOfDayController.cs` — namespace `SWEF.Cinema`
Controls sun/moon position and scene lighting based on a 0–24 h time-of-day value.

**Setup:**
1. Create a `TimeOfDayController` GameObject in the World scene.
2. Assign `sunLight` (main Directional Light) and optionally `moonLight`.
3. Assign `skyboxMaterial` (must expose `_Tint` or `_Color`).
4. Optionally assign `starParticles` for a particle-based star field.
5. Set `timeOfDay` (0–24, default 12 = noon) and `timeSpeed` (0 = paused).
6. Enable `useRealWorldTime` to sync to the device clock.

**Key API:**
- `SetTimeOfDay(float hour)` — clamp 0–24, update all lighting immediately.
- `SetTimeSpeed(float speed)` — clamp 0–100; 3600 = 1 game-hour per real second.
- `ToggleRealWorldTime(bool)` — enable/disable real-world sync.
- `GetTimeString()` → `"14:30"`.
- `IsDaytime` / `IsGoldenHour` / `IsNight` — period helpers.
- `OnTimeChanged(float)` / `OnDayNightTransition(bool)` — events.

**Sun position formula:** `sunAltitude = 90 − |timeOfDay − 12| × 15`

---

#### `Cinema/PhotoModeController.cs` — namespace `SWEF.Cinema`
Dedicated photo mode with free-camera movement, post-processing knobs, filter presets, and frame overlays.

**Setup:**
1. Add `PhotoModeController` component to any persistent GameObject.
2. Optionally assign `photoCamera`; falls back to `Camera.main`.
3. Populate `filterPresets` list with `FilterPreset` objects (name must match `PhotoFilter` enum).
4. Populate `frameSprites` list — one `Sprite` per `PhotoFrame` enum entry.
5. Assign `watermarkSprite` if watermark is desired.

**State machine:** `Inactive → Active → Capturing → Active`

**Key API:**
- `EnterPhotoMode()` / `ExitPhotoMode()` — toggle with PauseManager integration.
- `SetFilter(PhotoFilter)` / `SetFrame(PhotoFrame)`.
- `CapturePhoto()` — delegates to `ScreenshotController`.
- `CapturePhotoWithEffects()` — renders at `captureResolution`, saves via `ScreenshotController.SaveTextureToGallery`.
- `StartTimer(float seconds)` — countdown then auto-capture.

---

#### `Cinema/CinematicCameraPath.cs` — namespace `SWEF.Cinema`
Defines and plays back a spline-based cinematic camera path.

**Setup:**
1. Add `CinematicCameraPath` to a persistent GameObject.
2. Assign `cameraTarget` (the camera Transform to animate).
3. Add waypoints via `AddWaypoint()` while in play mode, or configure in Inspector.

**Key API:**
- `Play()` / `Pause()` / `Resume()` / `Stop()` / `Seek(float time)`.
- `AddWaypoint()` / `InsertWaypoint(int)` / `RemoveWaypoint(int)` / `UpdateWaypoint(int)`.
- `ToJson()` / `FromJson(string)` — serialise paths to/from JSON.
- `GetTotalDuration()` — returns path length in seconds.
- `loopMode` — `Once`, `Loop`, or `PingPong`.
- `useCatmullRom` — toggle Catmull-Rom vs linear interpolation.

**Camera path file format (JSON):**
```json
{
  "waypoints": [
    { "position": { "x": 0, "y": 100, "z": 0 }, "rotation": { "x": 0, "y": 0, "z": 0, "w": 1 }, "fov": 60, "timeAtWaypoint": 0, "holdDuration": 0 },
    { "position": { "x": 200, "y": 150, "z": 50 }, "rotation": { "x": 0, "y": 0.7, "z": 0, "w": 0.7 }, "fov": 45, "timeAtWaypoint": 5, "holdDuration": 1 }
  ]
}
```
Files are stored in `Application.persistentDataPath/CameraPaths/<name>.json`.

---

#### `Cinema/CinematicCameraUI.cs` — namespace `SWEF.Cinema`
Editor-style UI panel for building and controlling cinematic camera paths.

**Setup:**
1. Wire `cameraPath` reference and assign the scrollable `waypointListContent` transform.
2. Assign `waypointItemPrefab` — should contain a `Text`, a Delete `Button`, and an Update `Button`.
3. Wire playback control buttons and sliders.

---

#### `UI/PhotoModeUI.cs` — namespace `SWEF.UI`
Photo mode overlay UI with filter/frame scroll lists, composition grid, shutter flash, and info display.

**Setup:**
1. Place `PhotoModeUI` on the HUD canvas.
2. Assign `photoController` and optionally `timeController`.
3. Wire all sliders, buttons, scroll content transforms, and overlay images.
4. `filterItemPrefab` / `frameItemPrefab` must contain a `Text` label and a `Button`.

---

#### `UI/TimeOfDayUI.cs` — namespace `SWEF.UI`
Compact HUD panel exposing time-of-day controls: slider, quick-set buttons, period label, speed slider.

**Setup:**
1. Add `TimeOfDayUI` to the HUD canvas.
2. Assign `timeController`.
3. Wire all sliders, text labels, and quick-set buttons (sunrise=6h, noon=12h, sunset=18h, midnight=0h).

---

### Modified Scripts (6)

#### `Atmosphere/DayNightCycle.cs`
- Added `[Header("Phase 18 — Time of Day")]` with `timeOfDayController` field.
- `OnEnable` / `OnDisable` subscribe/unsubscribe to `TimeOfDayController.OnTimeChanged`.
- `HandleTimeChanged(float hour)` syncs the internal `_timeOfDay` (0–1 normalised) from the 0–24 h value.

#### `Screenshot/ScreenshotController.cs`
- Added `CaptureAtResolution(int width, int height) → Texture2D` — renders via `RenderTexture`, reads pixels.
- Added `SaveTextureToGallery(Texture2D tex, string filename = null)` — encodes PNG, writes to `persistentDataPath`.

#### `Flight/CameraController.cs`
- Added `_cinematicOverride` bool field.
- `EnableCinematicOverride()` / `DisableCinematicOverride()` — hand off / return camera control.
- `IsCinematicActive` property.
- `LateUpdate()` now returns early when `_cinematicOverride == true`.

#### `Core/PauseManager.cs`
- Added `PauseForPhotoMode()` — sets `Time.timeScale = 0` without showing the pause panel.
- Added `ResumeFromPhotoMode()` — restores `Time.timeScale = 1`.
- Added `IsPhotoModePaused` property.

#### `Settings/SettingsUI.cs`
- Added `[Header("Phase 18 — Cinema")]` with `defaultRealTimeToggle` and `defaultTimeOfDaySlider` fields.

#### `Achievement/AchievementManager.cs`
- Added four new achievements: `first_photo`, `golden_hour_photo`, `cinematic_path_created`, `night_flight`.
- Added `timeOfDayController` Inspector field; auto-resolved via `FindFirstObjectByType` in `Awake`.
- `Update()` accumulates `_nightFlightSeconds` when `IsNight && speed > 0`, unlocking `night_flight` after 300 s.

---

### Updated Architecture Diagram

```
Cinema Module (Phase 18)
  ├── TimeOfDayController  (sun/moon, sky, stars)           ← NEW
  ├── PhotoModeController  (photo mode state machine)       ← NEW
  ├── CinematicCameraPath  (spline path, playback)          ← NEW
  └── CinematicCameraUI    (path editing panel)             ← NEW

UI Layer (Phase 18)
  ├── PhotoModeUI   (filters, frames, sliders, flash)       ← NEW
  └── TimeOfDayUI   (quick-set, slider, period label)       ← NEW

Atmosphere Layer (Phase 18)
  └── DayNightCycle (+ TimeOfDayController sync)            ← MODIFIED

Screenshot Layer (Phase 18)
  └── ScreenshotController (+ CaptureAtResolution,
                              SaveTextureToGallery)         ← MODIFIED

Flight Layer (Phase 18)
  └── CameraController (+ EnableCinematicOverride,
                          DisableCinematicOverride,
                          IsCinematicActive)                ← MODIFIED

Core Layer (Phase 18)
  └── PauseManager (+ PauseForPhotoMode,
                      ResumeFromPhotoMode,
                      IsPhotoModePaused)                    ← MODIFIED

Settings Layer (Phase 18)
  └── SettingsUI (+ defaultRealTimeToggle,
                    defaultTimeOfDaySlider)                 ← MODIFIED

Achievement Layer (Phase 18)
  └── AchievementManager (+ first_photo, golden_hour_photo,
                            cinematic_path_created,
                            night_flight)                   ← MODIFIED
```

---

## Phase 19 — Weather System: Real-time Weather Integration & Environmental Flight Effects

### Overview
Phase 19 adds a comprehensive Weather System that fetches real-time weather data (via the Open-Meteo API), applies visual/physical environmental effects during flight, and integrates with achievements, analytics, and the flight journal.

### New Files

| File | Namespace | Role |
|------|-----------|------|
| `Weather/WeatherCondition.cs` | `SWEF.Weather` | `WeatherCondition` enum (13 conditions) + `WeatherData` class |
| `Weather/WeatherDataService.cs` | `SWEF.Weather` | Singleton — API fetch (Open-Meteo), polling, offline/procedural fallback |
| `Weather/WeatherStateManager.cs` | `SWEF.Weather` | Singleton — owns authoritative weather state, altitude zones, smooth transitions |
| `Weather/WeatherVFXController.cs` | `SWEF.Weather` | Particle system control — rain, snow, fog, lightning, sandstorm, hail |
| `Weather/WeatherFlightModifier.cs` | `SWEF.Weather` | Flight physics modifiers — wind force, turbulence shake, icing, thermals |
| `Weather/WeatherSkyboxController.cs` | `SWEF.Weather` | URP skybox/lighting adjustments per weather condition |
| `Weather/WeatherAudioController.cs` | `SWEF.Weather` | Ambient weather audio — rain loops, wind, thunder SFX, crossfades |
| `UI/WeatherHUD.cs` | `SWEF.UI` | Corner HUD widget — condition icon, temperature, wind, visibility |
| `Settings/WeatherSettings.cs` | `SWEF.Settings` | Persisted weather settings — quality, physics, audio, manual override |
| `Editor/WeatherDebugWindow.cs` | `SWEF.Editor` | Editor window (SWEF → Weather Debug) — force conditions, quick scenarios |

### Modified Files

| File | Changes |
|------|---------|
| `Achievement/AchievementManager.cs` | Added `storm_chaser`, `snowbird`, `clear_skies` achievements; weather state tracking |
| `Core/AnalyticsLogger.cs` | Added `RecordWeatherCondition()`, `WeatherEventCount`; subscribes to weather transitions |
| `Core/FlightJournal.cs` | Records weather conditions encountered; persists `weatherSummary` in `JournalEntry` |
| `Core/SaveManager.cs` | Added `weatherSummary` field to `JournalEntry` |

### Setup in Unity Editor

1. **WeatherDataService** — Add to a persistent GameObject (e.g. WorldBootstrap).
   - The default endpoint is the free [Open-Meteo API](https://open-meteo.com/) — no API key required.
   - To use a paid provider, enter the `apiBaseUrl` and `apiKey` in the Inspector.
   - **Never commit a real API key to source control.** Use Unity Cloud Config or environment variables.

2. **WeatherStateManager** — Add to the same persistent GameObject.

3. **WeatherVFXController** — Add to a GameObject in the World scene; assign particle system prefabs.

4. **WeatherFlightModifier** — Add alongside `FlightController`; call `ApplyToFlightController(fc)` each FixedUpdate/Update.

5. **WeatherSkyboxController** — Add to any persistent GameObject; assign the `Sun Light` directional light.

6. **WeatherAudioController** — Add to a persistent GameObject; assign audio clips in the Inspector.

7. **WeatherHUD** — Add to the HUD Canvas; assign `CanvasGroup`, `TextMeshProUGUI` labels, condition icon sprites (one per `WeatherCondition` enum value, in order).

8. **WeatherSettings** — Add to the settings persistent GameObject; integrates with `SettingsManager`.

9. **WeatherDebugWindow** — Open via **SWEF → Weather Debug** in the Unity menu bar (Editor only).

### Altitude Zones

| Zone | Altitude | Weather Behaviour |
|------|----------|-------------------|
| Ground | 0 – 2,000 m | Full weather effects |
| Cloud transition | 2,000 – 10,000 m | Precipitation fades, cloud coverage thins |
| Stratosphere | 10,000 – 30,000 m | Always clear above clouds; extreme cold (down to −60 °C) |
| Near-space | 30,000 m+ | No weather; temperature −80 °C |

### New Achievements

| ID | Title | Description |
|----|-------|-------------|
| `storm_chaser` | Storm Chaser ⛈️ | Fly through 10 thunderstorms |
| `snowbird` | Snowbird ❄️ | Fly in snow conditions |
| `clear_skies` | Clear Skies ☀️ | Complete a full flight in perfect clear weather |

### Architecture Diagram (Phase 19)

```
Weather Module (Phase 19)
  ├── WeatherCondition      (enum + WeatherData)            ← NEW
  ├── WeatherDataService    (Open-Meteo API + fallback)     ← NEW
  ├── WeatherStateManager   (state, altitude zones)         ← NEW
  ├── WeatherVFXController  (particles, lightning)          ← NEW
  ├── WeatherFlightModifier (wind, turbulence, icing)       ← NEW
  ├── WeatherSkyboxController (URP lighting/sky)            ← NEW
  └── WeatherAudioController  (ambient audio)               ← NEW

UI Layer (Phase 19)
  └── WeatherHUD (corner widget)                            ← NEW

Settings Layer (Phase 19)
  └── WeatherSettings (PlayerPrefs persistence)             ← NEW

Editor Layer (Phase 19)
  └── WeatherDebugWindow (SWEF → Weather Debug)             ← NEW

Achievement Layer (Phase 19)
  └── AchievementManager (+ storm_chaser, snowbird,
                            clear_skies)                    ← MODIFIED

Core Layer (Phase 19)
  ├── AnalyticsLogger (+ RecordWeatherCondition)            ← MODIFIED
  ├── FlightJournal   (+ weatherSummary tracking)           ← MODIFIED
  └── SaveManager     (+ JournalEntry.weatherSummary)       ← MODIFIED
```

---

## Phase 22 — Offline Mode, Data Caching & Tile Prefetch System

### Overview
Phase 22 adds an offline-first capability so users can pre-download 3D tile regions and fly without an active internet connection.  A new `Assets/SWEF/Scripts/Offline/` module handles connectivity detection, persistent tile caching, intelligent prefetching, and graceful service degradation.

---

### New Scripts

| Script | Namespace | Description |
|--------|-----------|-------------|
| `OfflineManager.cs` | `SWEF.Offline` | Singleton — monitors `Application.internetReachability` every 2 s, maintains `IsOffline` / `ConnectionType`, fires connectivity events, supports forced-offline via PlayerPrefs key `SWEF_ForceOffline` |
| `TileCacheManager.cs` | `SWEF.Offline` | Persists 3D tile region metadata as JSON in `Application.persistentDataPath/TileCache/regions.json`; simulates tile download with progress callback; LRU eviction when 2 GB limit is reached |
| `TilePrefetchController.cs` | `SWEF.Offline` | Predicts future flight position from heading/speed, prefetches tiles 30 s ahead at 5 km radius; WiFi-only; throttled to one request per 10 s |
| `OfflineFallbackController.cs` | `SWEF.Offline` | Disables weather API, multiplayer sync, and telemetry upload when offline; queues up to 1 000 deferred operations and flushes on reconnect |
| `OfflineHUD.cs` | `SWEF.Offline` | Top-right HUD widget — offline icon + label, expandable cache-usage panel with progress bar and time-since-online; 0.3 s alpha fade |
| `RegionDownloadUI.cs` | `SWEF.Offline` | Settings panel listing 10 predefined popular regions + "Cache Current Location"; per-region download/delete/progress rows inside a `ScrollRect` |

---

### Modified Scripts

| Script | Change |
|--------|--------|
| `Core/BootManager.cs` | Phase 22 block: finds `OfflineManager` after scene load and logs current online/offline state |
| `Settings/WeatherSettings.cs` | Added `ForceOfflineMode` property (get/set) — reads/writes `SWEF_ForceOffline` PlayerPrefs key and delegates to `OfflineManager.Instance` |
| `Weather/WeatherDataService.cs` | Added `SetFallbackMode(bool)` public method — switches `offlineMode` flag and triggers procedural weather generation when going offline |
| `Analytics/TelemetryDispatcher.cs` | Added `SetOfflineMode(bool)` and `FlushQueue()` public methods — suspends upload while offline and flushes accumulated events on reconnect |

---

### Setup Instructions

1. **OfflineManager** — attach `OfflineManager` to a persistent GameObject (e.g. the same object as `BootManager` or a dedicated `OfflineSystem` prefab).  Ensure `DontDestroyOnLoad` keeps it alive across scene transitions.

2. **TileCacheManager** — attach `TileCacheManager` to the same persistent GameObject.  Configure `maxCacheSizeBytes` in the Inspector (default 2 GB).

3. **TilePrefetchController** — attach to the persistent GameObject.  Assign `FlightController` in the Inspector or leave blank for auto-find.  Disable on cellular/offline via `Enable(false)`.

4. **OfflineFallbackController** — attach to the persistent GameObject.  Wire `OfflineHUD` in the Inspector or leave blank for auto-find.

5. **OfflineHUD** — add to the HUD Canvas (below the weather widget in the hierarchy).  Assign `CanvasGroup`, `Image`, `TextMeshProUGUI` labels, and `Slider` cacheBar in the Inspector.

6. **RegionDownloadUI** — add the panel to the Settings canvas.  Assign a region-row prefab containing child objects named `NameLabel`, `SizeLabel`, `StatusLabel`, `ProgressBar`, `DownloadButton`, `DeleteButton`, and place the panel inside a `ScrollRect`.  Wire the `cacheCurrentLocationButton`.

---

### Architecture Diagram (Phase 22)

```
Offline Module (Phase 22)
  ├── OfflineManager          (connectivity detection, force-offline)   ← NEW
  ├── TileCacheManager        (persistent tile region cache, LRU)       ← NEW
  ├── TilePrefetchController  (trajectory-based prefetch, WiFi only)    ← NEW
  ├── OfflineFallbackController (service degradation + deferred ops)    ← NEW
  ├── OfflineHUD              (status widget, cache bar)                ← NEW
  └── RegionDownloadUI        (manual download panel, 10 regions)       ← NEW

Core Layer (Phase 22)
  └── BootManager             (+ Phase 22 offline init block)           ← MODIFIED

Settings Layer (Phase 22)
  └── WeatherSettings         (+ ForceOfflineMode property)             ← MODIFIED

Weather Layer (Phase 22)
  └── WeatherDataService      (+ SetFallbackMode)                       ← MODIFIED

Analytics Layer (Phase 22)
  └── TelemetryDispatcher     (+ SetOfflineMode, FlushQueue)            ← MODIFIED
```

---

### Cache Storage Notes

| Parameter | Value |
|-----------|-------|
| Storage root | `Application.persistentDataPath/TileCache/` |
| Metadata file | `…/TileCache/regions.json` |
| Default max size | 2 GB (configurable in Inspector) |
| Eviction policy | LRU — least-recently-accessed region deleted first |
| Size estimate | π × r² × 5 MB/km² (placeholder; real Cesium payload sizes will differ) |
| Actual tile download | Stubbed — wire to Cesium offline API when available |

---

### Offline Behaviour Matrix

| Feature | Online | Offline |
|---------|--------|---------|
| 3D Tile rendering | Live Cesium stream | Cached regions (if pre-downloaded) |
| Weather data | Open-Meteo API poll | Procedural fallback (`SetFallbackMode(true)`) |
| Multiplayer sync | Active | Disabled (`RoomManager.enabled = false`) |
| Telemetry upload | Immediate dispatch | Queued locally, flushed on reconnect |
| Cloud save | Immediate | Deferred via `OfflineFallbackController` queue |
| HUD indicator | Hidden | Visible — shows connection type + cache usage |
| Tile prefetch | WiFi only, auto | N/A (prefetch disabled while offline) |

---

## Phase 23 — Onboarding Revamp & Interactive Tutorial 2.0

### New Scripts (`Assets/SWEF/Scripts/Tutorial/`)

| Script | Purpose |
|--------|---------|
| `TutorialStepData.cs` | Data class for tutorial step definitions with localization support |
| `TutorialHighlight.cs` | Full-screen spotlight/highlight overlay with cutout effect and pulse animation |
| `TutorialTooltip.cs` | Anchored tooltip with directional arrow for step instructions |
| `TutorialActionDetector.cs` | Detects player actions to auto-advance interactive tutorial steps |
| `InteractiveTutorialManager.cs` | Main tutorial controller — replaces legacy TutorialManager |
| `TutorialReplayButton.cs` | Settings panel button to replay the tutorial |

### Setup in World Scene

1. Create a full-screen Canvas (sort order above HUD) for the tutorial overlay
2. Add `TutorialHighlight` component with a dark overlay `Image` (semi-transparent black) and a `spotlightRect` child
3. Add `TutorialTooltip` component with instruction `Text`, arrow `Image`, prompt `Text`, and a `CanvasGroup`
4. Create `InteractiveTutorialManager` GameObject, attach script
5. Wire `TutorialHighlight`, `TutorialTooltip`, and `TutorialActionDetector` references in Inspector
6. Optionally assign the `hudRoot` Transform to limit spotlight searches to the HUD Canvas
7. Default tutorial steps (11 steps) are pre-configured in the script — customize in Inspector if needed
8. The tutorial auto-starts on first World scene load (respects legacy `SWEF_TutorialCompleted` key)

### Settings Integration

1. In the Settings panel, add a **"Replay Tutorial"** button
2. Attach `TutorialReplayButton` component, wire the button reference in Inspector
3. On click it calls `InteractiveTutorialManager.RestartTutorial()` from step 0

### Legacy Compatibility

- The old `TutorialManager.cs` is kept for reference; the new `InteractiveTutorialManager` takes priority
- On completion, both `"SWEF_Tutorial2_Completed"` and `"SWEF_TutorialCompleted"` PlayerPrefs keys are set
- If the legacy key is already set (returning player), the new tutorial won't auto-start unless replayed from Settings

### PlayerPrefs Keys

| Key | Type | Purpose |
|-----|------|---------|
| `SWEF_Tutorial2_Progress` | int | Last completed step index (for resume on re-launch) |
| `SWEF_Tutorial2_Completed` | int (0/1) | Set to 1 when tutorial finishes |
| `SWEF_TutorialCompleted` | int (0/1) | Legacy key — also set on completion for backward compatibility |

### Localization Keys (add to `Resources/Localization/*.json`)

| Key | Default English Text |
|-----|----------------------|
| `tutorial_welcome` | Welcome to Skywalking: Earth Flight! 🚀 |
| `tutorial_look_around` | Drag the screen to look around. |
| `tutorial_throttle` | Use the throttle slider to fly forward. |
| `tutorial_altitude` | Adjust the altitude slider to climb higher. |
| `tutorial_roll` | Tap the roll buttons to bank left and right. |
| `tutorial_comfort` | Toggle Comfort Mode for a smoother flight. |
| `tutorial_settings` | Open Settings to customize your experience. |
| `tutorial_screenshot` | Capture stunning views with the screenshot button. |
| `tutorial_teleport` | Tap Teleport to search for any place on Earth. |
| `tutorial_achievements` | Check your Achievements to discover goals. |
| `tutorial_complete` | You're ready! Enjoy exploring Earth from above. |

---

## Phase 24 — Advanced Flight Physics: Aerodynamics, Drag, Lift & Orbital Mechanics

### New Scripts

| Script | Namespace | Purpose |
|--------|-----------|---------|
| `Flight/AeroState.cs` | `SWEF.Flight` | Immutable struct — per-tick aerodynamic snapshot (density, Mach, AoA, dynamic pressure, etc.) |
| `Flight/AeroPhysicsModel.cs` | `SWEF.Flight` | Exponential atmosphere model, drag/lift/gravity/thrust calculations, Mach number |
| `Flight/OrbitState.cs` | `SWEF.Flight` | Enum: `Atmospheric`, `SubOrbital`, `LowOrbit`, `HighOrbit`, `Escape` |
| `Flight/OrbitalMechanics.cs` | `SWEF.Flight` | Orbital/escape velocity, 2-body gravity, apoapsis/periapsis estimation |
| `Flight/FlightPhysicsSnapshot.cs` | `SWEF.Flight` | Immutable struct for HUD/telemetry — G-force, net force, L/W ratio, orbit state |
| `Flight/FlightPhysicsIntegrator.cs` | `SWEF.Flight` | **Main integration point** — FixedUpdate physics loop connecting all systems |
| `Flight/StallWarningSystem.cs` | `SWEF.Flight` | Monitors AoA, dynamic pressure, G-force; fires stall/overspeed/G-force events |
| `UI/FlightPhysicsHUD.cs` | `SWEF.UI` | HUD MonoBehaviour — Mach, G-force, AoA, dynamic pressure, orbit state display |

### Modified Files

| File | Change |
|------|--------|
| `Flight/FlightController.cs` | + `Velocity` property, `ApplyExternalAcceleration()`, `Forward` property |

### Architecture

```
TouchInputRouter
    └── FlightController.Step()        ← original kinematic system (unchanged)
            ↑ ApplyExternalAcceleration()
FlightPhysicsIntegrator (FixedUpdate)
    ├── AltitudeController             ← reads current altitude
    ├── AeroPhysicsModel               ← drag, lift, gravity, Mach, air density
    ├── OrbitalMechanics               ← orbit state, orbital/escape velocity
    └── StallWarningSystem             ← stall / overspeed / G-force warnings
            └── OnPhysicsSnapshot ──→ FlightPhysicsHUD  (UI update)
```

### Setup in World Scene

1. On the **Player** GameObject, add the following components:
   - `AeroPhysicsModel`
   - `OrbitalMechanics`
   - `StallWarningSystem`
   - `FlightPhysicsIntegrator`
2. Wire `AltitudeController` and the three new components in the `FlightPhysicsIntegrator` Inspector fields (or let them auto-locate via `GetComponent`).
3. Add `FlightPhysicsHUD` to a HUD Canvas child and wire optional `Text`/`Slider` fields.

### Disabling Advanced Physics

Set `enableAdvancedPhysics = false` on `FlightPhysicsIntegrator` (or `physicsBlendFactor = 0`) to restore the exact original kinematic behaviour. All existing events (`OnSpeedChanged`, `OnComfortModeChanged`, `OnBoostStarted`, etc.) continue to fire unmodified.

### Physics Model Summary

| Parameter | Value |
|-----------|-------|
| Sea-level air density ρ₀ | 1.225 kg/m³ |
| Scale height H | 8,500 m |
| Drag coefficient Cd | 0.04 |
| Reference area A | 12 m² |
| Lift slope Cl/°AoA | 0.1 |
| Max lift coefficient Cl_max | 1.5 |
| Stall angle | 15° |
| Sea-level gravity g₀ | 9.81 m/s² |
| Earth radius R | 6,371,000 m |
| Earth GM | 3.986 × 10¹⁴ m³/s² |
| Rocket mode threshold | 25,000 m |
| Kármán line | 100,000 m |
| Max atmospheric speed | 8,000 m/s |
| Escape speed cap | 11,200 m/s |

---

## Phase 25 — Leaderboard & Global Rankings (비행 기록 순위 시스템)

### New Directory: `Assets/SWEF/Scripts/Leaderboard/`

| Script | Namespace | Purpose |
|--------|-----------|---------|
| `GlobalLeaderboardEntry.cs` | `SWEF.Leaderboard` | Serializable data class for global leaderboard entries; `CalculateScore()` static helper |
| `LeaderboardCategory.cs` | `SWEF.Leaderboard` | Enum: `HighestAltitude`, `FastestSpeed`, `LongestFlight`, `BestOverallScore`, `MostFlights`, `WeeklyChallenge`; `LeaderboardCategoryHelper.GetDisplayName()` |
| `LeaderboardTimeFilter.cs` | `SWEF.Leaderboard` | Enum: `AllTime`, `Monthly`, `Weekly`, `Daily` |
| `GlobalLeaderboardService.cs` | `SWEF.Leaderboard` | MonoBehaviour Singleton — REST API layer, offline queue, in-memory cache (60 s TTL), rate limiting (2 s), mock data generation |
| `LeaderboardUI.cs` | `SWEF.Leaderboard` | Full leaderboard HUD controller — category/time/region filters, pagination (20 per page), loading spinner, error display |
| `LeaderboardEntryUI.cs` | `SWEF.Leaderboard` | Single row component — gold/silver/bronze top-3 styling, current-player highlight |
| `WeeklyChallengeManager.cs` | `SWEF.Leaderboard` | MonoBehaviour Singleton — week-number-based mock challenge generation, `OnNewChallengeAvailable` event |
| `WeeklyChallengeUI.cs` | `SWEF.Leaderboard` | HUD banner — challenge title, description, progress slider, countdown timer, collapse toggle |

### New Social Scripts: `Assets/SWEF/Scripts/Social/`

| Script | Namespace | Purpose |
|--------|-----------|---------|
| `RegionHelper.cs` | `SWEF.Social` | Static utility — 39 country code→name map, `DetectRegion()`, `GetFlagEmoji()` |
| `PlayerProfileManager.cs` | `SWEF.Social` | MonoBehaviour Singleton — UUID generation, display name validation (2–20 chars), region auto-detection, `OnProfileUpdated` event |
| `PlayerProfileUI.cs` | `SWEF.Social` | Settings panel — name `InputField`, region `Dropdown`, save button with inline validation |

### Modified Files

| File | Change |
|------|--------|
| `Social/LeaderboardManager.cs` | `SubmitSession()` now also calls `GlobalLeaderboardService.SubmitScore()` after local save |
| `Core/BootManager.cs` | Phase 25 block: checks for `PlayerProfileManager` presence and logs a warning if absent |
| `UI/StatsDashboard.cs` | Added `globalRankText` field; `RefreshGlobalRank()` polls `GlobalLeaderboardService.FetchPlayerRank()` every 60 s |

### Scoring Formula

```
score = maxAltitude × 1.0 + maxSpeed × 0.5 + flightDuration × 0.3
```

### Architecture

```
LeaderboardManager.SubmitSession()
    ├── [local] PlayerPrefs JSON   (existing)
    └── GlobalLeaderboardService.SubmitScore()
            ├── Online  → POST /scores
            └── Offline → SWEF_PendingScores (PlayerPrefs queue, flushed on reconnect)

LeaderboardUI  ──→  GlobalLeaderboardService.FetchLeaderboard()
                         ├── Online  → GET /leaderboard
                         ├── Cache   → 60 s in-memory Dictionary
                         └── Mock    → GenerateMockPage() when apiBaseUrl empty or unreachable

StatsDashboard  ──→  GlobalLeaderboardService.FetchPlayerRank()  (every 60 s)
```

### Setup in World Scene

1. Add a persistent **Manager** GameObject with:
   - `GlobalLeaderboardService` — set `apiBaseUrl` to your REST endpoint (leave empty for mock/dev mode)
   - `PlayerProfileManager`
   - `WeeklyChallengeManager`

2. Add `LeaderboardUI` to your HUD Canvas and wire all `[SerializeField]` fields.

3. Add `WeeklyChallengeUI` to HUD Canvas → wire `LeaderboardUI` reference.

4. For each leaderboard row, create a prefab with `LeaderboardEntryUI` and wire `Text`/`Image` fields. Assign to `LeaderboardUI.entryPrefab`.

5. For the player profile settings panel, add `PlayerProfileUI` and wire fields.

6. Add a `globalRankText` (UI Text) to `StatsDashboard` in the Inspector to display the live global rank.

### Mock / Offline Mode

- Set `apiBaseUrl = ""` (empty) on `GlobalLeaderboardService` to run in full mock mode — no network calls are made.
- When the network is unavailable, submitted scores are queued in `PlayerPrefs` (`SWEF_PendingScores`) and automatically flushed the next time the service successfully connects.

### PlayerPrefs Keys

| Key | Type | Purpose |
|-----|------|---------|
| `SWEF_PlayerId` | string | Player UUID (auto-generated) |
| `SWEF_DisplayName` | string | Player display name |
| `SWEF_Region` | string | ISO 3166-1 alpha-2 region code |
| `SWEF_AvatarUrl` | string | Optional avatar URL |
| `SWEF_PendingScores` | string (JSON) | Offline-queued score submissions |

---

## Phase 26 — Performance Profiling & Memory Optimization 2.0

### New Directory: `Assets/SWEF/Scripts/Performance/`

| Script | Namespace | Purpose |
|--------|-----------|---------|
| `Performance/PerformanceProfiler.cs` | `SWEF.Performance` | Advanced frame-time profiler — rolling 60-frame window, 1%/0.1% lows, histogram, CSV export, 5-second snapshots |
| `Performance/MemoryPoolManager.cs` | `SWEF.Performance` | Generic `ObjectPool<T>` with pre-warm, Get/Return, Shrink; `MemoryPoolManager` singleton for monitoring all pools |
| `Performance/TextureMemoryOptimizer.cs` | `SWEF.Performance` | Scans all loaded `Texture2D` objects, runtime bilinear downsampling, `UnloadUnusedAssets`, auto-optimize above threshold |
| `Performance/DrawCallAnalyzer.cs` | `SWEF.Performance` | Draw call and batching stats via `FrameTimingManager`; heaviest-renderer list; batching efficiency ratio |
| `Performance/GarbageCollectionTracker.cs` | `SWEF.Performance` | 300-frame circular buffer of per-frame GC allocations; spike detection, `ForceCollect()`, high-alloc warning |
| `Performance/AssetLoadProfiler.cs` | `SWEF.Performance` | Records asset load events (time + memory), FIFO 500-cap, slowest/largest queries, formatted report |
| `Performance/RuntimeDiagnosticsHUD.cs` | `SWEF.Performance` | On-screen diagnostics overlay (dev builds only): FPS graph, memory bar, GC rate, draw calls, pool stats; F3 toggle |
| `Performance/SceneLoadProfiler.cs` | `SWEF.Performance` | Hooks `SceneManager` events, measures unload/load/activate phase timing, `LoadHistory`, average load time |
| `Performance/AdaptiveQualityController.cs` | `SWEF.Performance` | FPS-based dynamic quality adjustment with streak counters, cooldown hysteresis, `QualityAction` enum |

### New Editor Script: `Assets/SWEF/Scripts/Editor/`

| Script | Namespace | Purpose |
|--------|-----------|---------|
| `Editor/PerformanceProfilerWindow.cs` | `SWEF.Editor` | **SWEF → Performance Profiler** EditorWindow — live graphs (FPS/memory/GC), pool stats table, texture breakdown, draw-call analysis, Export/ForceGC/Optimize buttons |
## Phase 25 — Social Feed & Community (비행 스크린샷/영상 공유)

### New Scripts: `Assets/SWEF/Scripts/Social/`

| Script | Namespace | Purpose |
|--------|-----------|---------|
| `SocialPost.cs` | `SWEF.Social` | Serializable data class — `postId`, `authorName`, `imagePath`, `thumbnailPath`, `caption`, GPS coords, `timestamp` (ISO 8601), `likeCount`, `isLikedByMe`, `flightDataId`, `weatherCondition`, `tags`; `ToJson()` / `FromJson()`, `GetFormattedTimestamp()`, `GetLocationString()` |
| `SocialFeedManager.cs` | `SWEF.Social` | Singleton (DontDestroyOnLoad) — manages up to 200 local `SocialPost` entries; JSON files in `persistentDataPath/SocialFeed/`; `CreatePost()`, `DeletePost()`, `RefreshFeed()`, `ToggleLike()`, `GetPostsByAuthor()`; events `OnPostCreated`, `OnPostDeleted`, `OnFeedRefreshed` |
| `SocialFeedUI.cs` | `SWEF.Social` | ScrollRect feed viewer — spawns `SocialPostCard` prefabs, pull-to-refresh, scroll-to-bottom pagination (20 per page), `Open()` / `Close()` fade animations |
| `SocialPostCard.cs` | `SWEF.Social` | Single post card — loads thumbnail from file, like button with scale-punch animation, share button calls `SocialShareController`, delete button (own posts only), view-on-map teleport |
| `SocialShareController.cs` | `SWEF.Social` | Static utility — iOS `UIActivityViewController` stub, Android `Intent.ACTION_SEND` stub, Editor clipboard fallback; `ShareImage()`, `ShareFlightReplay()`, `CopyToClipboard()`; `OnShareCompleted` event |
| `CommunityProfileManager.cs` | `SWEF.Social` | Singleton (DontDestroyOnLoad) — local player profile (`displayName`, `avatarId`, stats) persisted in `PlayerPrefs` under `SWEF_Profile_Json`; `GetProfile()`, `UpdateProfile()`, `GetDisplayName()`, `IncrementStat()` |
| `PostComposerUI.cs` | `SWEF.Social` | Post creation dialog — screenshot preview, 280-char caption with live counter, auto-populated GPS location, optional FlightRecorder attachment via `includeFlightDataToggle`; `Open(screenshotPath)` |
| `SocialNotificationHandler.cs` | `SWEF.Social` | In-app toast notifications — slide-in animation, sequential queue for multiple alerts; subscribes to `SocialFeedManager.OnPostCreated`; `ShowNewPostToast(post)` |

### Modified Files

| File | Change |
|------|--------|
| `Core/PerformanceManager.cs` | Added `CurrentFps` alias, `FrameTimeMs` property; calls `PerformanceProfiler.Instance?.RecordFrame()` each frame |
| `Core/MemoryManager.cs` | Added `CurrentUsedMB`, `PeakUsedMB` properties; `OnMemoryWarning` upgraded to `Action<long>` firing at 80% system RAM |
| `Core/BootManager.cs` | Phase 26 block: finds `PerformanceProfiler` and `AdaptiveQualityController`, sets `AutoAdjustEnabled` from PlayerPrefs |
| `Settings/SettingsManager.cs` | Added `AdaptiveQuality` (default `true`) and `DiagnosticsHUD` (default `false`) settings with keys `SWEF_AdaptiveQuality`/`SWEF_DiagnosticsHUD`; `ApplyAll()` pushes value to `AdaptiveQualityController` |
| `Settings/SettingsUI.cs` | Added `adaptiveQualityToggle` and `diagnosticsToggle` `SerializeField`s; wired callbacks; `RefreshUI` syncs toggles |

### Setup Instructions

#### 1. PerformanceProfiler
1. Add a persistent **Performance** GameObject to the Boot scene.
2. Attach `PerformanceProfiler` — it calls `DontDestroyOnLoad` automatically.
3. Optionally attach `PerformanceManager` to the same GameObject so frame data is forwarded.

#### 2. MemoryPoolManager
1. Attach `MemoryPoolManager` to the same persistent GameObject.
2. In your runtime code, create pools and register them:
   ```csharp
   var pool = new ObjectPool<BulletComponent>(bulletPrefab, initialSize: 20, maxSize: 100, parent: transform);
   MemoryPoolManager.Instance.RegisterPool("Bullets", pool);
   ```

#### 3. TextureMemoryOptimizer
1. Attach `TextureMemoryOptimizer` to a scene GameObject (World scene recommended).
2. Set `autoOptimizeThresholdMB` (default 512 MB) and `defaultMaxResolution` (default 1024) in the Inspector.
3. Call `OptimizeTextures(maxRes)` manually or let the auto-threshold trigger it.

#### 4. DrawCallAnalyzer
1. Attach `DrawCallAnalyzer` to any persistent GameObject.
2. Subscribe to `OnStatsUpdated` or poll `GetCurrentStats()` from HUD code.

#### 5. GarbageCollectionTracker
1. Attach `GarbageCollectionTracker` to any persistent GameObject.
2. Subscribe to `OnAllocationSpike` to react to large per-frame allocations.

#### 6. AssetLoadProfiler
1. Attach `AssetLoadProfiler` to a persistent GameObject.
2. After each asset load, call:
   ```csharp
   AssetLoadProfiler.Instance?.RecordLoad(assetName, "Texture2D", elapsedMs, sizeBytes);
   ```

#### 7. RuntimeDiagnosticsHUD
> **Requirement:** Only compiles in `DEVELOPMENT_BUILD` or `UNITY_EDITOR`.

1. Create a Canvas in World scene for diagnostics.
2. Add `RuntimeDiagnosticsHUD` to the Canvas root.
3. Wire all `[SerializeField]` Text / RawImage / Slider / Button fields in the Inspector.
4. Press **F3** at runtime to toggle (or set the default state via `SWEF_DiagnosticsEnabled` PlayerPrefs).

#### 8. SceneLoadProfiler
1. Attach `SceneLoadProfiler` to a persistent GameObject.
2. Before loading a new scene, call `SceneLoadProfiler.Instance?.BeginSceneLoad(sceneName)`.
3. Subscribe to `OnSceneLoadComplete` to receive `SceneLoadEvent` records.

#### 9. AdaptiveQualityController
1. Attach `AdaptiveQualityController` to any persistent GameObject in the World scene.
2. Ensure `QualityPresetManager` is also present.
3. Enable/disable via `AutoAdjustEnabled` (persisted through `SWEF_AdaptiveQuality` in PlayerPrefs and Settings UI).

#### 10. PerformanceProfilerWindow (Editor)
1. Open via **SWEF → Performance Profiler** menu.
2. Enter Play mode to see live data.
3. Use **Export CSV Report** to save all snapshots to `persistentDataPath/Performance/`.

### DEVELOPMENT_BUILD Requirement for RuntimeDiagnosticsHUD

The `RuntimeDiagnosticsHUD` script is wrapped in `#if DEVELOPMENT_BUILD || UNITY_EDITOR`. To use it in a device build:

1. In **Build Settings**, check **Development Build**.
2. The script compiles and the canvas becomes active/toggleable.
3. In production (non-development) builds the entire overlay is stripped.
| `Screenshot/ScreenshotController.cs` | Added `LastScreenshotPath` property; added `OnScreenshotSaved` event; both are set/fired alongside existing `OnScreenshotCaptured` after every successful capture |
| `Screenshot/ScreenshotUI.cs` | Added `shareAfterCaptureButton` (revealed after capture) and `postComposer` reference; tapping Share opens `PostComposerUI` or falls back to `SocialShareController.ShareImage()` |

### Architecture

```
PerformanceManager.Update()
    └── PerformanceProfiler.RecordFrame()
            ├── Rolling frame-time window (60 frames)
            ├── Histogram (16 buckets, 2 ms wide)
            └── PerformanceSnapshot (every 5 s) → History (60 max)
                        └── OnSnapshotTaken → AdaptiveQualityController

MemoryManager.CheckMemory()
    └── OnMemoryWarning (Action<long>) at 80% system RAM

MemoryPoolManager
    └── ObjectPool<T> instances (registered by name)

TextureMemoryOptimizer ──→ Resources.FindObjectsOfTypeAll<Texture2D>()

GarbageCollectionTracker ──→ GC.GetTotalMemory() differential per frame

SceneLoadProfiler ──→ SceneManager.sceneLoaded / sceneUnloaded events

AdaptiveQualityController ──→ PerformanceProfiler.OnSnapshotTaken
                                    └── QualityPresetManager.SetQuality()

RuntimeDiagnosticsHUD (dev only) ──→ polls all Performance singletons every 0.5 s

Editor: PerformanceProfilerWindow ──→ EditorApplication.update (0.5 s)
```

### Performance Tuning Guide

#### Low-end devices (≤ 2 GB RAM, single/dual core GPU)
| Setting | Recommended value |
|---------|-------------------|
| `QualityPresetManager` default | `Low` |
| `AdaptiveQualityController.targetFps` | 30 |
| `AdaptiveQualityController.lowerSampleCount` | 2 (react faster) |
| `TextureMemoryOptimizer.defaultMaxResolution` | 512 |
| `TextureMemoryOptimizer.autoOptimizeThresholdMB` | 256 |
| `MemoryManager.memoryWarningThresholdMB` | 512 |

#### High-end devices (≥ 6 GB RAM, high-tier GPU)
| Setting | Recommended value |
|---------|-------------------|
| `QualityPresetManager` default | `Ultra` |
| `AdaptiveQualityController.targetFps` | 60 |
| `AdaptiveQualityController.raiseSampleCount` | 10 (raise conservatively) |
| `TextureMemoryOptimizer.defaultMaxResolution` | 2048 |
| `TextureMemoryOptimizer.autoOptimizeThresholdMB` | 1024 |
| `MemoryManager.memoryWarningThresholdMB` | 2048 |
ScreenshotController.CaptureScreenshot()
    └── OnScreenshotSaved / OnScreenshotCaptured  →  ScreenshotUI (shows Share button)
                                                   →  SocialFeedManager (optional auto-post)

ScreenshotUI [Share button]
    └── PostComposerUI.Open(path)
            └── SocialFeedManager.CreatePost(path, caption)
                    ├── saves  persistentDataPath/SocialFeed/<postId>.json
                    ├── fires  OnPostCreated  →  SocialNotificationHandler (toast)
                    └── fires  OnFeedRefreshed  →  SocialFeedUI (rebuilds cards)

SocialPostCard
    ├── Like   → SocialFeedManager.ToggleLike()
    ├── Share  → SocialShareController.ShareImage()
    ├── Delete → SocialFeedManager.DeletePost()
    └── Map    → TeleportController.TeleportTo(lat, lon)
```

### Setup in World Scene

1. **SocialFeedManager** — add a persistent GameObject with `SocialFeedManager` and `CommunityProfileManager` components. These are `DontDestroyOnLoad` singletons.

2. **SocialNotificationHandler** — add to your HUD Canvas; wire `toastPanel`, `toastText`, and `toastCanvasGroup` in the Inspector.

3. **SocialFeedUI** — add to a Canvas and wire:
   - `feedScrollRect`, `postPrefab` (prefab with `SocialPostCard`), `contentParent`
   - `refreshButton`, `createPostButton`, `closeFeedButton`
   - `feedPanel`, `feedCanvasGroup`, `emptyFeedText`

4. **PostComposerUI** — add to a Canvas; wire `composerPanel`, `composerCanvasGroup`, `previewImage`, `captionInput`, `locationLabel`, `altitudeLabel`, `characterCountText`, `postButton`, `cancelButton`, `includeFlightDataToggle`.

5. **ScreenshotUI** — wire the new `shareAfterCaptureButton` to a Button in the capture overlay, and optionally wire `postComposer` to the `PostComposerUI` instance.

### Native Sharing Plugins

`SocialShareController` includes stubs for iOS (`UIActivityViewController`) and Android (`Intent.ACTION_SEND`). To use real native sharing:

- **iOS**: Implement the `ShareViaIOSNative` block using a native plugin (e.g., UniShare, NativeShare from the Asset Store).
- **Android**: Implement the `ShareViaAndroidNative` block using `AndroidJavaObject` to construct an `Intent.ACTION_SEND`.
- **Editor/PC**: The clipboard fallback is active automatically — no changes needed.

### PlayerPrefs Keys

| Key | Type | Purpose |
|-----|------|---------|
| `SWEF_AdaptiveQuality` | int (0/1) | Whether adaptive quality is enabled |
| `SWEF_DiagnosticsHUD` | int (0/1) | Whether diagnostics HUD is shown by default |
| `SWEF_DiagnosticsEnabled` | int (0/1) | HUD visibility state persisted between sessions |
| `SWEF_Profile_Json` | string (JSON) | Serialised `CommunityProfileManager.PlayerProfile` |

### Feed Storage

Each post is stored as a single JSON file: `persistentDataPath/SocialFeed/<postId>.json`.  
The feed is capped at **200 posts**; oldest posts are deleted from disk automatically when the cap is exceeded.  
Images are referenced by file path only — actual image files (screenshots) remain in `persistentDataPath/Screenshots/`.

---

## Phase 27 — Procedural Terrain & LOD System

### New Scripts

| Script | Namespace | Role |
|--------|-----------|------|
| `Terrain/ProceduralTerrainGenerator.cs` | `SWEF.Terrain` | Singleton; multi-octave Perlin noise heightmap generation (async) + 5×5 chunk grid |
| `Terrain/TerrainChunk.cs` | `SWEF.Terrain` | Per-chunk MonoBehaviour — holds 4 LOD meshes, MeshFilter/Renderer/Collider |
| `Terrain/TerrainChunkPool.cs` | `SWEF.Terrain` | Object pool (default 50 chunks); `Get()` / `Return()` API |
| `Terrain/TerrainBiomeMapper.cs` | `SWEF.Terrain` | Static biome mapper — altitude + latitude + moisture → `BiomeType` + `Color` |
| `Terrain/CesiumTerrainBridge.cs` | `SWEF.Terrain` | Hides procedural terrain when Cesium tiles are loaded; shows fallback on network loss |
| `Terrain/TerrainTextureManager.cs` | `SWEF.Terrain` | Shared material/texture per biome; integrates with `TextureMemoryOptimizer` |
| `LOD/LODManager.cs` | `SWEF.LOD` | Singleton; altitude-aware LOD decisions every N frames; integrates with `AdaptiveQualityController` |
| `LOD/LODTransitionBlender.cs` | `SWEF.LOD` | Coroutine cross-fade between LOD levels to prevent popping |
| `LOD/OcclusionCullingHelper.cs` | `SWEF.LOD` | Frustum + distance + altitude culling via `GeometryUtility.TestPlanesAABB` |
| `Editor/TerrainDebugWindow.cs` | `SWEF.Editor` | **SWEF → Terrain Debug** EditorWindow — live stats, chunk grid, LOD sliders |

### Setup Instructions

#### 1. ProceduralTerrainGenerator
Add a persistent GameObject with `ProceduralTerrainGenerator` + `TerrainChunkPool`.
- Wire `playerTransform` to your camera/aircraft transform or leave null for `Camera.main` auto-detect.
- Tune `gridRadius` (default 2 → 5×5 grid), `heightScale`, `chunkSize`, and noise parameters.
- Subscribe to `OnChunkGenerated` if you need post-generation hooks.

#### 2. TerrainChunkPool
Add `TerrainChunkPool` to the same GameObject as `ProceduralTerrainGenerator`.
- Default `poolSize` is 50. Increase for larger grid radii.
- Set `chunkLayerName` to match your project's terrain layer (optional).

#### 3. LODManager
Add `LODManager` to a persistent GameObject.
- Default thresholds: Full < 500 m, Half < 2 000 m, Quarter < 8 000 m, Minimal < 20 000 m, Culled beyond.
- `updateIntervalFrames` (default 10) prevents per-frame LOD checks.
- Automatically scales thresholds with player altitude via `altitudeLODScaleBase`.

#### 4. LODTransitionBlender
Add `LODTransitionBlender` to any persistent GameObject.
- Call `StartTransition(chunk, from, to)` from your LOD change handler to cross-fade between levels.

#### 5. OcclusionCullingHelper
Add `OcclusionCullingHelper` to any persistent GameObject.
- Automatically used by `LODManager` if present in the scene.
- Call `ResetStats()` each frame if you need accurate per-frame culling counters.

#### 6. CesiumTerrainBridge
Add `CesiumTerrainBridge` to a persistent GameObject.
- Wire `terrainGenerator` to the `ProceduralTerrainGenerator` instance.
- Set `forceProceduralFallback = true` in the Inspector to test without Cesium.
- Call `OnNetworkLost()` / `OnNetworkRestored()` from your connectivity manager.

#### 7. TerrainTextureManager
Add `TerrainTextureManager` to any persistent GameObject.
- Uses 128×128 procedural gradient textures (one per biome).
- Wire `memBudgetMB` to control texture memory budget (default 32 MB).

#### 8. TerrainDebugWindow (Editor)
Open via **SWEF → Terrain Debug** from the Unity menu bar.
- Shows live chunk counts, LOD stats, and memory estimates.
- Provides LOD threshold sliders (apply at runtime in Play Mode).
- Toggle procedural terrain on/off without stopping Play Mode.

### Architecture Diagram

```
ProceduralTerrainGenerator  ──→  TerrainChunkPool (Queue<TerrainChunk>)
      │  async Task.Run()              └── TerrainChunk (MeshFilter + 4 LOD Meshes)
      │  GenerateHeightmap()
      │  BuildMesh() × 4 LODs
      └──→ fires OnChunkGenerated

LODManager  (every 10 frames)
      ├── OcclusionCullingHelper.IsVisible()  (frustum + distance + altitude)
      ├── GetLODLevel(distance, altitude)      (scaled thresholds)
      ├── TerrainChunk.UpdateLOD()
      └── fires OnLODChanged  →  LODTransitionBlender.StartTransition()

CesiumTerrainBridge
      ├── #if CESIUM_FOR_UNITY → IsCesiumRuntimeAvailable()
      ├── Cesium present → terrainGenerator.SetActive(false)
      └── Cesium absent  → terrainGenerator.SetActive(true)

TerrainBiomeMapper  (static)
      └── GetBiome(altitude, latitude, moisture)  →  BiomeType
          GetBiomeColor(biome)
          GetBiomeGradient(altitude)  →  vertex Color per mesh vertex

TerrainTextureManager
      └── GetTerrainMaterial(biome)  →  shared Material (instancing enabled)
          integrates with TextureMemoryOptimizer
```

### Terrain System Configuration Guide

#### Mobile-conservative defaults

| Parameter | Default | Notes |
|-----------|---------|-------|
| `chunkSize` | 256 | Vertex count per side |
| `gridRadius` | 2 | 5×5 = 25 chunks max |
| `poolSize` | 50 | Pre-warmed at startup |
| `heightScale` | 1000 m | Max terrain height |
| `noiseScale` | 0.002 | Zoom level of noise |
| `octaves` | 6 | Detail layers |
| `LOD thresholds` | 500/2 000/8 000/20 000 m | Distance breakpoints |

#### PC/high-end tuning suggestions

| Parameter | Suggested value |
|-----------|-----------------|
| `chunkSize` | 512 |
| `gridRadius` | 3 (7×7 grid) |
| `poolSize` | 100 |
| `LOD thresholds` | 1 000 / 5 000 / 15 000 / 40 000 m |

### PlayerPrefs Keys (Phase 27)

| Key | Type | Default | Purpose |
|-----|------|---------|---------|
| `SWEF_TerrainEnabled` | int (0/1) | 1 | Whether procedural terrain is on |
| `SWEF_TerrainLODQuality` | int (0–3) | 1 | LOD quality preset |
| `SWEF_TerrainRenderDistance` | float | 20 000 | Max render distance in metres |

---

## Phase 28 — Spatial Audio Engine & 3D Sound System

### New Scripts (12)

| Script | Namespace | Role |
|--------|-----------|------|
| `Audio/SpatialAudioManager.cs` | `SWEF.Audio` | Singleton pool of 32 3D AudioSources; priority-aware; integrates with SettingsManager & PerformanceManager |
| `Audio/AltitudeSoundscapeController.cs` | `SWEF.Audio` | 6-layer altitude-reactive ambient soundscape with ExpSmoothing crossfades |
| `Audio/DopplerEffectController.cs` | `SWEF.Audio` | Altitude-accurate Doppler pitch shift (speed of sound model); pitch clamped 0.5–2.0 |
| `Audio/EnvironmentReverbController.cs` | `SWEF.Audio` | AudioReverbFilter preset blending: Ground → MidAir → HighAlt → Space |
| `Audio/WindAudioGenerator.cs` | `SWEF.Audio` | Procedural wind synthesis — speed-driven lowpass + turbulence, integrates with WeatherStateManager |
| `Audio/SonicBoomController.cs` | `SWEF.Audio` | Mach 1 detection; one-shot boom + OnSonicBoom event; exposes CurrentMach for HUD |
| `Audio/AudioOcclusionSystem.cs` | `SWEF.Audio` | Raycast-based occlusion (top-8 sources); lowpass filter + volume reduction when blocked |
| `Audio/MusicLayerSystem.cs` | `SWEF.Audio` | Dynamic layered music (Base/Tension/Wonder/Triumph/Ambient); altitude-driven crossfades |
| `Audio/AudioMixerController.cs` | `SWEF.Audio` | Runtime AudioMixer wrapper; linear→dB conversion; snapshot transitions |
| `Audio/AudioEventTrigger.cs` | `SWEF.Audio` | Event-name → AudioClip mapping with per-event cooldowns; 2D and spatial variants |
| `Audio/AudioVisualizerData.cs` | `SWEF.Audio` | FFT spectrum + waveform data; bass/mid/treble levels; beat detection |
| `Editor/SpatialAudioDebugWindow.cs` | `SWEF.Editor` | EditorWindow: SWEF → Spatial Audio Debug; source pool stats, soundscape bars, Mach display, music layer sliders |

### Modified Files

| File | Change |
|------|--------|
| `Core/BootManager.cs` | Phase 28 init: finds SpatialAudioManager and logs if active |
| `Settings/SettingsManager.cs` | Added `SpatialAudioEnabled`, `DopplerEnabled`, `ReverbEnabled`, `AudioQuality` (0–2) settings |
| `UI/HudBinder.cs` | Added optional `machNumberText` + `SonicBoomController` ref; Update() displays `M {mach:F2}` |

### Setup Instructions

#### SpatialAudioManager
1. Add **SpatialAudioManager** to a persistent GameObject (e.g. `[AudioSystem]`).
2. Set `Pool Size` (default 32; use 8–16 for low-end mobile).
3. No AudioClips required — sources are created at runtime.

#### AltitudeSoundscapeController
1. Add to any GameObject in the World scene.
2. Assign one `AudioClip` per layer in the Inspector (6 slots):
   - 0–500 m: city ambience (traffic / crowds)
   - 500–5 000 m: light wind loop
   - 5 000–20 000 m: jet-stream wind
   - 20 000–80 000 m: thin atmosphere / eerie hum
   - 80 000–120 000 m: near-space hum
   - 120 000 m+: near-silence / cosmic background
3. Tune `maxVolume` and `fadeRange` per layer.

#### DopplerEffectController
1. Add to any persistent GameObject.
2. Set `Doppler Intensity` (0 = off, 1 = realistic, 2 = exaggerated).
3. Disable via **SettingsManager** `DopplerEnabled` or `sonicBoomController.IsEnabled = false`.

#### EnvironmentReverbController
1. Add to any GameObject with an **AudioListener** in the scene.
2. A `AudioReverbFilter` is auto-added to the AudioListener's GameObject.
3. Disable via **SettingsManager** `ReverbEnabled`.

#### WindAudioGenerator
1. Add to any persistent GameObject.
2. Assign a **white noise loop** AudioClip to `Wind Noise Clip`.
3. Tune `Base Wind Volume`, `Speed Multiplier`, `Turbulence Amount`.

#### SonicBoomController
1. Add to the player/aircraft GameObject.
2. Assign `Boom Clip` (a short transient boom WAV).
3. Listen to `OnSonicBoom` event for camera shake integration.
4. Optionally wire `machNumberText` in **HudBinder** for HUD display.

#### AudioOcclusionSystem
1. Add to any persistent GameObject.
2. Set `Occlusion Layers` to `Terrain` + `Default` layers.
3. Lower `Check Top N` (e.g. 4) on low-end devices.

#### MusicLayerSystem
1. Add to a persistent GameObject.
2. Assign one `AudioClip` per layer (Base, Tension, Wonder, Triumph, Ambient).
3. For teleport stingers assign `Teleport Stinger Clip`.

#### AudioMixerController
1. Create an **AudioMixer** asset in `Assets/SWEF/Audio/SWEFMixer.mixer`.
2. Expose parameters: `MasterVolume`, `MusicVolume`, `SFXVolume`, `AmbienceVolume`, `UIVolume`.
3. Create snapshots: `Default`, `Space`, `Paused`.
4. Assign the mixer to **AudioMixerController**.

#### AudioEventTrigger
1. Add to any manager GameObject.
2. Assign `AudioClip` per built-in event (Takeoff, Landing, SpeedBoost, etc.).
3. Call `TriggerEvent("Takeoff")` from flight code.

#### AudioVisualizerData
1. Add to any persistent GameObject.
2. Subscribe to `OnBeat` event to drive visual effects.

### Architecture Diagram (Phase 28 Audio Subsystem)

```
FlightController ──────┐
AltitudeController ────┼──► AltitudeSoundscapeController  (6-layer crossfade)
WeatherStateManager ───┤
                       ├──► DopplerEffectController        (Doppler pitch)
                       ├──► WindAudioGenerator             (procedural wind)
                       ├──► SonicBoomController            (Mach crossing)
                       └──► MusicLayerSystem               (layered BGM)

SpatialAudioManager ───────► AudioOcclusionSystem          (raycast occlusion)

AudioMixerController ──────► AudioMixer asset              (global mix/snapshots)

AudioEventTrigger ─────────► SpatialAudioManager           (event → 3D SFX)

AudioVisualizerData ───────► UI / VFX                      (FFT + beat events)

SettingsManager ───────────► DopplerEffectController.IsEnabled
                             EnvironmentReverbController.SetEnabled()
                             SpatialAudioManager.MaxActiveSourcesForQuality()
```

### Performance Tuning (Mobile)

| Setting | Low-end | Mid-range | High-end |
|---------|---------|-----------|----------|
| `poolSize` | 8 | 16 | 32 |
| `AudioQuality` | 0 | 1 | 2 |
| `DopplerEnabled` | false | true | true |
| `ReverbEnabled` | false | true | true |
| `checkTopN` (occlusion) | 4 | 8 | 8 |
| Soundscape layers | 2–3 active | all | all |

### PlayerPrefs Keys (Phase 28)

| Key | Type | Default | Purpose |
|-----|------|---------|---------|
| `SWEF_SpatialAudioEnabled` | int (0/1) | 1 | Master switch for spatial audio |
| `SWEF_DopplerEnabled` | int (0/1) | 1 | Doppler pitch shift on/off |
| `SWEF_ReverbEnabled` | int (0/1) | 1 | Environment reverb on/off |
| `SWEF_AudioQuality` | int (0–2) | 1 | Audio quality (source count cap) |

---

## Phase 29 — Cloud Rendering & Remote Streaming System

### New Scripts (12)

| Script | Namespace | Role |
|--------|-----------|------|
| `CloudRendering/CloudRenderingManager.cs` | `SWEF.CloudRendering` | Singleton pipeline manager; toggles local ↔ cloud mode; auto-detects low FPS and suggests cloud mode; exposes `ConnectionStatus` enum |
| `CloudRendering/StreamingClient.cs` | `SWEF.CloudRendering` | WebSocket client (System.Net.WebSockets); ring-buffer frame queue (3–10 frames); exponential-backoff reconnect (max 5 retries); latency measurement |
| `CloudRendering/InputStreamEncoder.cs` | `SWEF.CloudRendering` | Captures touch/gyro/button input at 60 Hz; serialises to compact binary; client-side prediction via `InputSnapshot` struct |
| `CloudRendering/FrameDecoder.cs` | `SWEF.CloudRendering` | Decodes raw/JPEG/PNG frame bytes into RenderTexture; CPU frame interpolation; color-space aware (linear/gamma) |
| `CloudRendering/LatencyCompensator.cs` | `SWEF.CloudRendering` | Rolling 60-sample latency average; client-side prediction; server-state reconciliation; jitter buffer sizing |
| `CloudRendering/AdaptiveBitrateController.cs` | `SWEF.CloudRendering` | 5-level quality (Minimum 480p … Ultra 4K); bandwidth estimation from frame arrivals; packet-loss-triggered degradation; 10 s hysteresis upgrade |
| `CloudRendering/CloudSessionManager.cs` | `SWEF.CloudRendering` | Session lifecycle: create, heartbeat (5 s), end; auth-token management; 30 min idle auto-disconnect; `CloudSessionConfig` inner class |
| `CloudRendering/HybridRenderingController.cs` | `SWEF.CloudRendering` | `RenderMode` enum (Local/Cloud/Hybrid); 1 s crossfade coroutine; disables world camera when streaming; fallback on disconnect |
| `CloudRendering/NetworkQualityMonitor.cs` | `SWEF.CloudRendering` | Composite score 0–100 (ping 40 pts + loss 40 pts + BW 20 pts); `NetworkQuality` enum (Good/Fair/Poor/Critical); 1 s update interval |
| `CloudRendering/CloudRenderingUI.cs` | `SWEF.CloudRendering` | Status dot (green/yellow/red); latency text; bandwidth display; quality dropdown; toggle button; latency warning toast (>100 ms) |
| `CloudRendering/ServerDiscoveryService.cs` | `SWEF.CloudRendering` | Pings 6 regional HTTP endpoints; selects lowest latency; caches in PlayerPrefs; `CloudServer` struct |
| `Editor/CloudRenderingDebugWindow.cs` | `SWEF.Editor` | EditorWindow: SWEF → Cloud Rendering Debug; real-time metric bars; network condition simulator; forced quality override |

### Modified Files

| File | Change |
|------|--------|
| `Core/BootManager.cs` | Boot log updated to Phase 29; adds `CloudRenderingManager.Initialize()` call before scene load |
| `Settings/SettingsManager.cs` | Added `CloudRenderingEnabled` (key `SWEF_CloudRendering`), `CloudQualityLevel` (key `SWEF_CloudQuality`), `CloudServerRegion` (key `SWEF_CloudRegion`) with Load/Save/Reset/Apply support |
| `UI/HudBinder.cs` | Added optional `cloudStatusIndicator`, `cloudLatencyBadge`, `cloudStatusDot` refs; `Update()` shows/hides cloud status and live latency badge based on current render mode |

### Setup Instructions

#### CloudRenderingManager
1. Create empty GameObject `[CloudRendering]` (Boot scene or persistent).
2. Attach `CloudRenderingManager` — `DontDestroyOnLoad` handled automatically.
3. Optionally enable **Enable On Start** in Inspector to activate cloud mode immediately.
4. Set **Auto Switch FPS Threshold** (default 25 FPS) — the manager logs a suggestion to switch when FPS drops below this value.
5. Wire `PerformanceManager`, `StreamingClient`, `CloudSessionManager` in Inspector or leave null for auto-find.

#### StreamingClient
1. Add `StreamingClient` to the `[CloudRendering]` GameObject.
2. Set **Frame Buffer Size** (3–10, default 5).
3. Set **Max Retries** (default 5) and **Initial Retry Delay Sec** (default 1 s; doubles each attempt up to 30 s).
4. `Connect(url)` is called automatically by `CloudSessionManager.CreateSession()`.

#### InputStreamEncoder
1. Add `InputStreamEncoder` to any persistent GameObject.
2. Adjust **Send Rate Hz** (default 60). Reduce to 30 on low-end devices.
3. Input is automatically forwarded to the cloud when `CloudRenderingManager.IsCloudMode` is true.

#### FrameDecoder
1. Add `FrameDecoder` to any persistent GameObject.
2. Assign a full-screen `RawImage` to **Display Image** in the Inspector.
3. Set **Texture Width / Height** to match your target streaming resolution.
4. Subscribe to `StreamingClient.OnFrameReceived` and call `FrameDecoder.DecodeFrame(data)`.

#### CloudSessionManager
1. Add `CloudSessionManager` to the `[CloudRendering]` GameObject.
2. Configure `CloudSessionConfig` in Inspector:
   - **Server URL** — WebSocket endpoint (e.g. `ws://render-us-east.swef.example.com/ws`).
   - **Region** — set to `"auto"` to use `ServerDiscoveryService`.
   - **Max Session Minutes** — idle timeout (default 30 min).
   - **Auth Token** — leave empty for unauthenticated dev servers.

#### HybridRenderingController
1. Add `HybridRenderingController` to any persistent GameObject.
2. Assign **World Camera** (your main scene camera) and **HUD Camera**.
3. Assign a `CanvasGroup` to **Crossfade Overlay** for the 1-second fade transition.
4. Assign the `RawImage` used by `FrameDecoder` to **Cloud Frame Display**.

#### ServerDiscoveryService
1. Add `ServerDiscoveryService` to the `[CloudRendering]` GameObject.
2. Override the default ping URLs in Inspector to point to your actual endpoints.
3. `DiscoverServers()` is called automatically when session region is `"auto"`.

#### CloudRenderingUI
1. Add `CloudRenderingUI` to your HUD Canvas.
2. Wire UI elements in the Inspector:
   - **Status Dot** — `Image` component coloured green/yellow/red.
   - **Status Text** — connection state label.
   - **Latency Text** — live ping display.
   - **Bandwidth Text** — estimated Mbps.
   - **Quality Dropdown** — Auto / Ultra / High / Medium / Low.
   - **Cloud Toggle Button** — enables/disables cloud mode.
   - **Latency Warning Toast** — shown when ping > 100 ms.

#### HudBinder — Cloud Status
1. Assign optional fields in Inspector:
   - **Cloud Status Indicator** — GameObject shown only in cloud mode.
   - **Cloud Latency Badge** — `Text` displaying live ping.
   - **Cloud Status Dot** — `Image` coloured by connection state.

### Architecture Diagram

```
CloudRenderingManager (singleton, DontDestroyOnLoad)
      ├── IsCloudMode toggle
      ├── OnCloudModeChanged  ──►  HybridRenderingController
      │                            ├── world camera on/off
      │                            └── 1 s crossfade coroutine
      ├── OnConnectionStatusChanged ──► CloudRenderingUI (status dot)
      │                                 └── HudBinder (cloud badge)
      └── Auto-switch: PerformanceManager.CurrentFps < threshold

CloudSessionManager
      ├── CreateSession()  ──► StreamingClient.Connect(url)
      ├── Heartbeat coroutine (every 5 s)
      └── Idle timeout (30 min) ──► EndSession()

ServerDiscoveryService
      ├── Ping 6 regional HTTP endpoints (async)
      ├── Select lowest-latency available server
      └── Cache in PlayerPrefs + push to CloudSessionManager.Config

StreamingClient (WebSocket)
      ├── Ring buffer (3–10 frames)
      ├── OnFrameReceived  ──► FrameDecoder.DecodeFrame()
      │                         └── Upload to RenderTexture ──► RawImage fullscreen
      ├── OnLatencyUpdated ──► LatencyCompensator.RecordLatencySample()
      │                         └── NetworkQualityMonitor.OnLatencyUpdated()
      └── Reconnect: exponential backoff (max 5 retries)

InputStreamEncoder  (60 Hz)
      ├── CaptureCurrentInput()  (touch, gyro, buttons, throttle, altitude)
      └── Encode()  ──► binary packet ──► server

LatencyCompensator
      ├── Rolling average (60 samples)
      ├── Client-side prediction
      └── Jitter buffer sizing (1 frame per 30 ms avg latency)

AdaptiveBitrateController
      ├── Bandwidth estimation from frame arrival intervals
      ├── Packet loss  > 2 %  → immediate quality decrease
      └── 10 s stability  → quality increase attempt

NetworkQualityMonitor (1 s update)
      ├── Score = ping(40) + loss(40) + bandwidth(20)
      └── OnNetworkQualityChanged  ──► CloudRenderingUI (status dot)

SettingsManager ──► CloudRenderingManager.Enable/DisableCloudRendering()
                    CloudSessionManager.Config.region
```

### Network Requirements

| Quality | Resolution | Min Bandwidth | Recommended Bandwidth |
|---------|------------|---------------|-----------------------|
| Minimum | 480p       | 1.5 Mbps      | 2 Mbps                |
| Low     | 720p       | 3 Mbps        | 5 Mbps                |
| Medium  | 1080p      | 6 Mbps        | 10 Mbps               |
| High    | 1440p      | 12 Mbps       | 20 Mbps               |
| Ultra   | 4K         | 25 Mbps       | 40+ Mbps              |

- Latency below 50 ms: optimal experience
- Latency 50–100 ms: good; jitter buffer compensates
- Latency 100–200 ms: fair; warning toast shown in HUD
- Latency > 200 ms: poor; consider switching to local rendering

### PlayerPrefs Keys (Phase 29)

| Key | Type | Default | Purpose |
|-----|------|---------|---------|
| `SWEF_CloudRendering` | int (0/1) | 0 | Whether cloud rendering is enabled |
| `SWEF_CloudQuality` | int (0–4) | 0 | Quality preset (0=Auto, 1=Ultra … 4=Low) |
| `SWEF_CloudRegion` | string | "auto" | Preferred cloud server region |
| `SWEF_CloudBestUrl` | string | "" | Cached best server URL (set by ServerDiscoveryService) |
| `SWEF_CloudBestRegion` | string | "" | Cached best server region name |

---

## Phase 30 — Localization & Multi-Language Support System

### Overview

Phase 30 adds a comprehensive localization system enabling the entire UI, tutorial, notifications, and error messages to be displayed in 8 languages with runtime switching.

**Supported Languages:** English · 한국어 · 日本語 · 简体中文 · Español · Français · Deutsch · Português

---

### New Scripts (9 scripts + 1 editor tool)

| Script | Namespace | Role |
|--------|-----------|------|
| `Localization/LocalizationManager.cs` | `SWEF.Localization` | Singleton managing all localization state — language detection, persistence, runtime switching, and `OnLanguageChanged` event |
| `Localization/LocalizedText.cs` | `SWEF.Localization` | Auto-updates `Text` / `TextMeshProUGUI` on language change; supports `SetFormatArgs` |
| `Localization/LocalizedImage.cs` | `SWEF.Localization` | Swaps `Image` sprite based on active language; fallback sprite |
| `Localization/LanguageDatabase.cs` | `SWEF.Localization` | Static JSON loader/cache for language files; `ClearCache()` for memory pressure |
| `Localization/LocalizationUI.cs` | `SWEF.Localization` | Language selection panel — native names, checkmark, hover preview |
| `Localization/PluralResolver.cs` | `SWEF.Localization` | Per-language plural form rules (CLDR: one/other for EN/DE/ES/FR/PT, other-only for CJK) |
| `Localization/RTLTextHandler.cs` | `SWEF.Localization` | RTL infrastructure (ready for Arabic/Hebrew); `ProcessRTL`, `IsRTLLanguage`, `AlignText` |
| `Localization/FontManager.cs` | `SWEF.Localization` | CJK / Korean / Latin font switching on language change; TMP_FontAsset support |
| `Editor/LocalizationEditorWindow.cs` | `SWEF.Editor` | `SWEF → Localization Editor` — table view, missing-key highlighting, Add/Remove key, JSON export, CSV import |

### New JSON Language Files (8 files)

All files are located in `Assets/SWEF/Resources/Localization/` (60+ keys each).

| File | Language |
|------|----------|
| `lang_en.json` | English (primary / fallback) |
| `lang_ko.json` | 한국어 (Korean) |
| `lang_ja.json` | 日本語 (Japanese) |
| `lang_zh.json` | 简体中文 (Simplified Chinese) |
| `lang_es.json` | Español (Spanish) |
| `lang_fr.json` | Français (French) |
| `lang_de.json` | Deutsch (German) |
| `lang_pt.json` | Português (Portuguese) |

### Modified Files (4 files)

| File | Change |
|------|--------|
| `Core/BootManager.cs` | Phase 30 log message + `LocalizationManager.Initialize()` call after Phase 29 cloud init |
| `Settings/SettingsManager.cs` | `SelectedLanguage` property (get/set with `LocalizationManager` sync) |
| `Settings/SettingsUI.cs` | Language selector button + `LocalizationUI` sub-panel integration; live label update on language change |
| `README_SWEF_SETUP.md` | This section |

---

### Setup Instructions

#### LocalizationManager
1. Add a persistent `[Localization]` GameObject to your Boot scene.
2. Attach `LocalizationManager` — it calls `DontDestroyOnLoad` automatically.
3. BootManager calls `Initialize()` automatically during the boot sequence.
4. First launch auto-detects `Application.systemLanguage`; subsequent launches restore from PlayerPrefs.

#### LocalizedText Usage
1. Add the `LocalizedText` component to any `Text` or `TextMeshProUGUI` GameObject.
2. Set `Localization Key` in the Inspector (e.g. `hud.altitude`).
3. The text updates automatically on language change and on `Start()`.
4. For format args (e.g. `"Altitude: {0}m"`), call `localizedText.SetFormatArgs(altitudeValue)` at runtime.

#### LocalizedImage Usage
1. Add the `LocalizedImage` component alongside a `Image` component.
2. Assign `Default Sprite` and any per-language overrides in `Language Sprites`.

#### Adding a New Language
1. Create `Assets/SWEF/Resources/Localization/lang_xx.json` with all keys from `lang_en.json`.
2. Add the language to `LocalizationManager.SupportedLanguages`.
3. Add a case to `LanguageDatabase.GetFileName(SystemLanguage)`.
4. Add a case to `LocalizationManager.GetNativeName(SystemLanguage)`.
5. Add a plural rule case to `PluralResolver.GetPluralForm(int, SystemLanguage)`.

#### Localization Editor
Open via **SWEF → Localization Editor** in the Unity menu bar.
- Table shows all keys as rows and all 8 languages as columns.
- **Red** cells indicate missing or empty translations.
- Use **Add** / **✕** to create or remove keys.
- **Save All** exports all changes to the JSON files.
- **Import CSV** accepts a CSV with header `key,en,ko,ja,zh,es,fr,de,pt`.

---

### JSON Format

```json
{
    "boot.loading": "Loading...",
    "boot.gps_check": "Checking location services...",
    "hud.altitude": "Altitude: {0}m",
    "tutorial.welcome": "Welcome to Skywalking: Earth Flight! 🚀\nYou're about to fly from your current location to the edge of space."
}
```

Keys use dot-separated namespaces (e.g. `boot.*`, `hud.*`, `settings.*`, `tutorial.*`, `teleport.*`, `fav.*`, `screenshot.*`, `cloud.*`, `general.*`, `pause.*`).
For pluralization append `_zero`, `_one`, `_few`, `_many`, or `_other` to the base key (e.g. `fav.page_one`, `fav.page_other`).

---

### Architecture

```
LocalizationManager (singleton, DontDestroyOnLoad)
      ├── CurrentLanguage (get/set)
      │       └── fires OnLanguageChanged(SystemLanguage)
      ├── GetText(key) / GetText(key, args)
      │       └── LanguageDatabase.LoadLanguage(lang) → Dictionary<key,value>
      │                └── Resources.Load<TextAsset>("Localization/lang_xx")
      └── Initialize() ─── called by BootManager

LocalizedText (per UI element)
      ├── OnEnable  → subscribe OnLanguageChanged
      ├── OnDisable → unsubscribe
      └── Refresh() → sets legacy Text.text or TMPro.text via reflection

LocalizationUI (sub-panel of SettingsUI)
      ├── Builds language button list from LocalizationManager.SupportedLanguages
      ├── Checkmark on active language
      ├── Hover → preview text in that language (tutorial.welcome key)
      └── Click → LocalizationManager.CurrentLanguage = selected

FontManager
      └── OnLanguageChanged → push Font / TMP_FontAsset to all registered LocalizedText

PluralResolver
      └── Resolve(key, count, lang) → key_one / key_other / key_other (CJK)

RTLTextHandler (future-ready)
      └── IsRTLLanguage(lang) → false for all current languages
```

### PlayerPrefs Keys (Phase 30)

| Key | Type | Default | Purpose |
|-----|------|---------|---------|
| `SWEF_Language` | string | `"English"` | Persisted `SystemLanguage` enum name |


---

## Phase 32 — Weather System & Dynamic Environment

### Overview

Phase 32 adds a complete real-time weather system with dynamic environment effects. It integrates with the existing flight simulation via **OpenWeatherMap API**, provides particle-based precipitation, environmental fog/wind systems, and weather-dependent flight physics modifiers.

**Key Features:**
- Live weather data from OpenWeatherMap (free tier) — no API key required for offline/procedural mode
- 15 weather types: Clear → Cloudy → Rain → Thunderstorm → Snow → Fog → Sandstorm → Hail, and more
- Smooth weather transitions (configurable blend duration)
- Particle-based rain, snow, and hail that follow the player camera
- Altitude-aware precipitation (stops above 15 km)
- Dynamic fog blended with the existing `AtmosphereController`
- Sun intensity and colour temperature adjusts to cloud cover
- Lightning flash coroutine during Thunderstorm
- Perlin-noise wind with jet-stream altitude model
- Icing warning event when temperature < 0 °C, humidity > 80 %, altitude 2–8 km
- HUD overlay with weather icon, temperature, wind, and visibility
- Ambient rain/wind/thunder audio (user-provided AudioClips)

---

### New Scripts (10 scripts)

| Script | Namespace | Role |
|--------|-----------|------|
| `Weather/WeatherData.cs` | `SWEF.Weather` | Data structs: `WeatherConditionData`, `WindData`, `WeatherForecast`, `WeatherType` enum |
| `Weather/WeatherAPIClient.cs` | `SWEF.Weather` | OpenWeatherMap fetch — singleton, polling, JSON parsing, fallback |
| `Weather/WeatherManager.cs` | `SWEF.Weather` | Central orchestrator — smooth transition, `ForceWeather`, `ResetToLive` |
| `Weather/PrecipitationSystem.cs` | `SWEF.Weather` | Particle-based rain / snow / hail, follows camera, altitude fade |
| `Weather/WindSystem.cs` | `SWEF.Weather` | Perlin-noise wind model, jet-stream altitude curve, `GetWindAtAltitude` |
| `Weather/WeatherFogController.cs` | `SWEF.Weather` | Dynamic fog: visibility → fog density, integrates with `AtmosphereController` |
| `Weather/WeatherLightingController.cs` | `SWEF.Weather` | Sun intensity / colour ramp, ambient, lightning flash coroutine |
| `Weather/WeatherUI.cs` | `SWEF.Weather` | HUD panel: weather icon, temperature, wind, visibility, icing warning flash |
| `Weather/WeatherSoundController.cs` | `SWEF.Weather` | Ambient rain / wind loops + thunder one-shots, altitude fade, SFX volume |

### Updated Script

| Script | Namespace | Change |
|--------|-----------|--------|
| `Weather/WeatherFlightModifier.cs` | `SWEF.Weather` | Phase 32 additions: `WeatherManager`/`WindSystem` integration, `OnIcingWarning` event, turbulence shake, visibility drag, `windForceMultiplier`, `turbulenceShakeStrength`, `reduceSpeedInLowVisibility` |

### Modified Scripts

| Script | Change |
|--------|--------|
| `Flight/FlightController.cs` | Added `ApplyExternalForce(Vector3)` (wind displacement) and `ExternalDragMultiplier` (visibility speed reduction) |
| `Atmosphere/AtmosphereController.cs` | Added `BaseFogDensity`, `BaseFogColor` properties and `SetWeatherOverride(float, Color)` / `ClearWeatherOverride()` methods |
| `Core/BootManager.cs` | Phase 32 init block — finds `WeatherManager` and `WeatherAPIClient`, logs status |
| `README_SWEF_SETUP.md` | This section |

---

### Setup Instructions

#### 1. WeatherManager GameObject
1. Create an empty GameObject named `[WeatherManager]` in the World scene (or persistent Boot scene).
2. Attach `WeatherManager`, `WeatherAPIClient`, `WeatherFlightModifier`.
3. In `WeatherAPIClient` Inspector: set your **OpenWeatherMap API key** (or leave blank for fallback).
   - ⚠️ **Never commit a real API key to source control.** Use Unity Cloud Config or a `.gitignored` config file.
4. In `WeatherManager` Inspector: assign references to `PrecipitationSystem`, `WindSystem`, `WeatherFogController`, `WeatherLightingController` (or leave null to auto-find).

#### 2. PrecipitationSystem GameObject
1. Create a GameObject named `[PrecipitationSystem]` and attach `PrecipitationSystem`.
2. Add three child GameObjects with a `ParticleSystem` component each:
   - `RainParticles` — set Shape to Box/Hemisphere, Simulation Space = World, gravity -9.8.
   - `SnowParticles` — slower fall speed, lateral drift, soft white particles.
   - `HailParticles` — larger spheres, fast fall, metallic grey colour.
3. Drag each child into the matching Inspector slot on `PrecipitationSystem`.
4. Set `Follow Target` to your main camera (or leave null for auto-find via `Camera.main`).

#### 3. WindSystem GameObject
1. Create an empty GameObject named `[WindSystem]` and attach `WindSystem`.
2. The `Altitude Wind Multiplier` curve is pre-configured; customise in Inspector if needed.

#### 4. WeatherFogController GameObject
1. Create an empty GameObject named `[WeatherFogController]` and attach `WeatherFogController`.
2. Assign `AtmosphereController` reference (or leave null to auto-find).

#### 5. WeatherLightingController GameObject
1. Create an empty GameObject named `[WeatherLightingController]` and attach `WeatherLightingController`.
2. Assign the scene's directional sun `Light` in the Inspector.
3. Customise `Clear To Overcast Gradient` in the Inspector.

#### 6. WeatherSoundController GameObject
1. Create an empty GameObject named `[WeatherSoundController]` and attach `WeatherSoundController`.
2. Add three child GameObjects each with an `AudioSource` component:
   - `RainAudio` — set to 3D off, loop, volume 0 (managed at runtime).
   - `WindAudio` — same as above.
   - `ThunderAudio` — no clip assigned (one-shots at runtime).
3. Assign your rain loop, wind loop, and thunder SFX clips in the Inspector.
   - **Note:** AudioClips are user-provided assets not included in this repository.

#### 7. WeatherUI (HUD Canvas)
1. Under your existing **HUD Canvas**, create a `Weather Panel` UI GameObject.
2. Add child Text/Image elements for weather name, wind, visibility, weather icon, and icing warning panel.
3. Attach `WeatherUI` to the panel and wire up the Inspector fields.
4. Add a `Sprite[]` array for `Weather Icons` — 15 entries indexed by `WeatherType` enum (0 = Clear, …, 14 = Mist).

#### 8. Testing
- Use the **Context Menu** on `WeatherManager` in Inspector (right-click → Force Rain/Snow/Thunderstorm/Clear).
- Call `WeatherManager.Instance.ForceWeather(WeatherType.Thunderstorm, 0.9f)` in code.
- Call `WeatherManager.Instance.ResetToLive()` to resume API-driven weather.

---

### Architecture (Phase 32)

```
World Scene (Phase 32)
  ├── (all existing GameObjects)
  ├── [WeatherManager]                    ← WeatherManager (orchestrator)
  │   ├── WeatherAPIClient               ← OpenWeatherMap fetch, fallback
  │   └── WeatherFlightModifier          ← physics: wind push, turbulence, icing
  ├── [PrecipitationSystem]              ← particle rain / snow / hail
  │   ├── RainParticles  (ParticleSystem)
  │   ├── SnowParticles  (ParticleSystem)
  │   └── HailParticles  (ParticleSystem)
  ├── [WindSystem]                       ← Perlin-noise wind, jet-stream model
  ├── [WeatherFogController]             ← fog density / colour → AtmosphereController
  ├── [WeatherLightingController]        ← sun intensity, ambient, lightning
  ├── [WeatherSoundController]           ← rain / wind loops + thunder one-shots
  └── HUD Canvas
      ├── (existing UI)
      └── Weather Panel (WeatherUI)      ← weather icon, temp, wind, visibility, icing

Data Flow:
  WeatherAPIClient ──OnWeatherUpdated──► WeatherManager
         │                                      │
  (fallback Clear)                    smooth Lerp each frame
                                               │
              ┌────────────────┬──────────────────┬────────────────┐
              │                │                  │                │
     PrecipitationSystem  WeatherFogController  WeatherLightingController  WeatherFlightModifier
              │                │                  │                │
     (particles)       AtmosphereController  (sun/lightning)   FlightController
                               │                                   │
                        RenderSettings.fog                ApplyExternalForce
                                                          ExternalDragMultiplier
```

---

### OpenWeatherMap API Key

| Item | Details |
|------|---------|
| Provider | [OpenWeatherMap](https://openweathermap.org/api) |
| Plan | Free tier (Current Weather API) |
| Rate limit | 60 calls/minute, 1,000,000 calls/month |
| Default poll interval | 300 s (5 minutes) — configurable in `WeatherAPIClient` |
| Fallback | If key is blank or request fails, `Clear` weather is emitted automatically |
| Key location | `WeatherAPIClient` → `apiKey` field in Inspector (never commit to git) |

### PlayerPrefs Keys (Phase 32)

_Phase 32 does not add new PlayerPrefs keys._ Weather state is ephemeral (re-fetched on session start).

### Notes

- **ParticleSystem prefabs** are user-provided. The `PrecipitationSystem` script expects three `ParticleSystem` references assigned in the Inspector.
- **AudioClips** (rain, wind, thunder) are user-provided. Assign them in `WeatherSoundController` Inspector.
- **Weather icons** (sprites indexed by `WeatherType`) are user-provided.
- Phase 9's `WeatherController`, `WindController`, and `WeatherStateManager` continue to function alongside Phase 32 systems. `WeatherFlightModifier` uses Phase 32 references when available and falls back to Phase 9 automatically.
