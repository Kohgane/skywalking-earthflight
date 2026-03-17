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
