# Photo Mode & Screenshot System — Enhanced Features

## Overview

Phase 51 adds seven new scripts that extend the existing **PhotoMode** and **Screenshot** systems in SkyWalking Earthflight with advanced capabilities:

| Script | Namespace | Location |
|--------|-----------|----------|
| `PanoramaCaptureController.cs` | `SWEF.PhotoMode` | `Assets/SWEF/Scripts/PhotoMode/` |
| `TimelapseController.cs` | `SWEF.PhotoMode` | `Assets/SWEF/Scripts/PhotoMode/` |
| `PhotoSharingManager.cs` | `SWEF.PhotoMode` | `Assets/SWEF/Scripts/PhotoMode/` |
| `PhotoChallengeManager.cs` | `SWEF.PhotoMode` | `Assets/SWEF/Scripts/PhotoMode/` |
| `AdvancedPostProcessing.cs` | `SWEF.PhotoMode` | `Assets/SWEF/Scripts/PhotoMode/` |
| `ScreenshotFormatManager.cs` | `SWEF.Screenshot` | `Assets/SWEF/Scripts/Screenshot/` |
| `ScreenshotWatermarkRenderer.cs` | `SWEF.Screenshot` | `Assets/SWEF/Scripts/Screenshot/` |

---

## Enhanced System Architecture

```
PhotoMode/
├── [existing] PhotoModeData.cs         ← shared enums & data classes
├── [existing] PhotoCaptureManager.cs   ← core single/burst/HDR capture
├── [existing] PhotoCameraController.cs ← virtual camera controls
├── [existing] PhotoFilterSystem.cs     ← filter/effect pipeline
├── [existing] PhotoFrameRenderer.cs    ← frame overlays
├── [existing] PhotoGalleryManager.cs   ← gallery index & storage
├── [existing] PhotoModeUI.cs           ← HUD / UI controls
├── [existing] PhotoModeAnalytics.cs    ← analytics events
├── [existing] DroneCameraController.cs ← drone flight control
├── [existing] DroneVisualController.cs ← drone mesh & VFX
│
├── [NEW] PanoramaCaptureController.cs  ← 360° cube-face → equirectangular
├── [NEW] TimelapseController.cs        ← interval/distance timelapse
├── [NEW] PhotoSharingManager.cs        ← social share, branding card, clipboard
├── [NEW] PhotoChallengeManager.cs      ← challenge missions, criteria, scoring
└── [NEW] AdvancedPostProcessing.cs     ← DOF, tilt-shift, grain, CA, LUT, flare

Screenshot/
├── [existing] ScreenshotController.cs  ← basic capture + HUD hide
├── [existing] ScreenshotUI.cs          ← flash, toast, share button
│
├── [NEW] ScreenshotFormatManager.cs    ← PNG/JPG/BMP/EXR, super-res, metadata
└── [NEW] ScreenshotWatermarkRenderer.cs← watermark, timestamp, location, flight info
```

---

## New Features Overview

### PanoramaCaptureController

- Captures **6 cube faces** (right, left, up, down, forward, back) by rotating the capture camera 90° between frames.
- **Stitches** cube faces into an equirectangular texture using spherical UV projection.
- `CaptureType` enum: `Spherical360` (4:2 aspect), `Cylindrical180` (2:1), `WidePanorama` (3:1).
- **Pauses `Time.timeScale`** during capture to prevent scene changes between faces.
- Configurable `_faceResolution` (1024 / 2048 / 4096 px per face).
- Anti-aliasing per face via RenderTexture.antiAliasing.
- Saves to `<persistentDataPath>/Panoramas/` as PNG or JPG.
- Fires `OnPanoramaCaptureStarted`, `OnPanoramaCaptureProgress(float 0-1)`, `OnPanoramaCaptureCompleted(string path)`.
- `ShowPreview()` / `HidePreview()` — projects last panorama onto an optional sphere mesh for in-UI viewing.

### TimelapseController

- **Time-based mode**: captures a frame every `_captureInterval` seconds.
- **Distance-based mode**: captures every `_distanceInterval` metres of aircraft travel.
- Configurable `_maxFrameCount` and/or `_maxDuration` stop conditions.
- **Pause / Resume** support at any time without losing the frame sequence.
- Frames saved to `<persistentDataPath>/Timelapse/<session>/frame_NNNNN.png|jpg`.
- Fires `OnTimelapseStarted`, `OnTimelapseFrameCaptured(int frame, int total)`, `OnTimelapseCompleted(IReadOnlyList<string> paths)`.

### PhotoSharingManager

- `Share(SharePayload)` — composites optional branding border, then calls the platform-native share sheet.
- `ShareLastPhoto(path, caption)` — convenience method with default hashtags.
- `CopyToClipboard(path)` — writes the file path to `GUIUtility.systemCopyBuffer` for plugin use.
- **Rate limiting** via `_rateLimitSeconds` (default 5 s).
- **Privacy**: option to strip file metadata before sharing (`stripMetadata` in `SharePayload`).
- Fires `OnShareInitiated`, `OnShareCompleted`, `OnShareFailed(string reason)`.

### PhotoChallengeManager

- Challenge definitions stored in inspector list (`List<PhotoChallengeData>`).
- `PhotoChallengeState` lifecycle: `Locked → Available → Active → Submitted / Completed / Failed`.
- `ChallengeCriteria` checks: altitude range, required `PhotoFilter`, weather keyword, location tag, landmark name.
- **Scoring**: 0–100 based on how many criteria are satisfied; ≥ 50 = pass.
- Progress persisted as JSON to `<persistentDataPath>/photo_challenges.json`.
- `ActivateChallenge`, `EvaluateSubmission`, `ClaimReward`, `UnlockChallenge` public API.
- Fires `OnChallengeActivated`, `OnChallengeCompleted`, `OnChallengeRewardClaimed`.

### AdvancedPostProcessing

Effect types in the compositing stack:

| Effect | Key Parameters |
|--------|----------------|
| `DepthOfField` / `TiltShift` | focus Y, band half-width, blur radius |
| `Vignette` | colour, power, intensity |
| `FilmGrain` | grain seed, intensity |
| `ChromaticAberration` | lateral offset, intensity |
| `LensFlare` | brightness threshold, intensity |
| `ColorLUT` | LUT texture (1D strip), blend amount |

- **Effect stack**: effects are applied in list order; each step operates on the result of the previous.
- **Before/After comparison**: `GenerateComparisonTexture()` blends original (left) and processed (right) at a configurable split position.
- **Presets**: `SavePreset(name)` / `LoadPreset(name)` persisted to `<persistentDataPath>/PPPresets/`.
- Live preview via optional `_previewTarget` RawImage.

### ScreenshotFormatManager

- Output formats: `PNG`, `JPG` (quality 1–100), `BMP`, `EXR` (HDR, ZIP-compressed).
- Super-resolution: `Native`, `X2`, `X4` — renders at higher resolution then downscales for AA.
- Auto-naming: `SWEF_yyyy-MM-dd_HH-mm-ss.ext`.
- **Thumbnail** (default 256 px, configurable) saved alongside each screenshot.
- **Metadata sidecar** JSON: timestamp, game version, platform, resolution, super-res setting.
- **Async save**: encoding and file write happen off the main thread to avoid frame hitches.
- **Storage cleanup**: when `_maxStoredScreenshots` is exceeded, oldest files (+ sidecars + thumbnails) are deleted.
- `EstimateFileSizeBytes()` — pre-save file size estimate.
- Fires `OnScreenshotSaved(string path)`, `OnScreenshotFailed(string reason)`, `OnStorageLimitReached`.

### ScreenshotWatermarkRenderer

- `Apply(Texture2D source)` — returns a new texture with all enabled overlays composited.
- Watermark image anchored to `BottomRight`, `BottomLeft`, `TopRight`, `TopLeft`, `Center`, or `Custom` normalised position.
- Auto-scales watermark to `_relativeSizeFraction` of screenshot width.
- Per-pixel alpha blend respects `_opacity` (0–1).
- Text overlays: **timestamp**, **location name**, **flight info** (altitude / speed / heading).
- Master `_watermarkEnabled` toggle; when `false`, `Apply()` returns the source unchanged.
- `SetFlightInfo(alt, speed, heading)` and `SetLocationName(name)` for runtime injection.

---

## Integration with Existing Scripts

### With PhotoCaptureManager

`PanoramaCaptureController` and `TimelapseController` use the same `Camera` reference pattern as `PhotoCaptureManager` (SerializeField + Camera.main fallback). Call `_galleryManager?.RefreshGallery()` after capture to keep the gallery index current.

### With PhotoGalleryManager

Both new capture controllers call `RefreshGallery()` on completion so new panoramas and timelapse frames appear in the gallery automatically.

### With ScreenshotController

`ScreenshotFormatManager` and `ScreenshotWatermarkRenderer` are standalone MonoBehaviours. To integrate:

```csharp
// In your capture pipeline:
var watermarked = watermarkRenderer.Apply(rawTexture);
formatManager.CaptureScreenshot(); // or pass the composited texture directly
```

---

## Photo Challenge System Guide

### Defining Challenges in the Inspector

1. Add `PhotoChallengeManager` component to a manager GameObject.
2. Expand `_challenges` list and add `PhotoChallengeData` entries:
   ```
   challengeId  : "daily_sunset_001"
   title        : "Golden Hour"
   description  : "Capture a photo at sunset below 200 m altitude."
   criteria:
     minAltitude  : 0
     maxAltitude  : 200
     requiredWeather : "clear"
   rewardPoints : 150
   difficulty   : 2
   isDaily      : true
   ```
3. Set initial `State` via `UnlockChallenge("daily_sunset_001")` or start locked.

### Player Flow

```
UnlockChallenge → ActivateChallenge → [player captures photo] → EvaluateSubmission → ClaimReward
```

### Criteria Evaluated

| Criterion | How Checked |
|-----------|-------------|
| Altitude range | `PhotoMetadata.altitude` (double → metres) vs min/max |
| Filter | `PhotoMetadata.cameraSettings.filter` enum match |
| Weather | `PhotoMetadata.weatherCondition` contains keyword |
| Location | `PhotoMetadata.tags` joined string contains keyword |
| Landmark | `PhotoMetadata.tags` joined string contains landmark name |

---

## Panorama Capture Workflow

```
1. Call PanoramaCaptureController.BeginCapture()
2. Time is paused (Time.timeScale = 0)
3. Camera rotates through 6 directions, one frame each
4. OnPanoramaCaptureProgress fires at 1/6, 2/6, … 6/6
5. Cube faces are stitched into equirectangular texture
6. Image saved to <persistentDataPath>/Panoramas/
7. Time.timeScale restored
8. OnPanoramaCaptureCompleted fires with file path
9. Optional: call ShowPreview() to view in the sphere viewer UI
```

---

## Coding Conventions

All new scripts follow the existing PhotoMode style:
- Namespace `SWEF.PhotoMode` or `SWEF.Screenshot`
- `[SerializeField]` + `FindObjectOfType<T>()` fallback in `Awake`
- `#region` blocks (Constants, Events, Inspector, Private State, Public API, Helpers)
- `event Action<T>` with null-conditional invoke (`?.Invoke(...)`)
- Coroutines for async operations; `WaitForEndOfFrame` before texture reads
- `try/catch` with `Debug.LogWarning` on IO operations
- No third-party dependencies
