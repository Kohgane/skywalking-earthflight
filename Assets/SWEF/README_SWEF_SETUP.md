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
