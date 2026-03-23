# Phase 48 — Replay & Flight Recording System

**Namespace:** `SWEF.Replay`  
**Directory:** `Assets/SWEF/Scripts/Replay/`

---

## Overview

A complete Replay & Flight Recording System that allows players to record their flight paths, replay them with cinematic camera angles, and share recordings with other players.

---

## Architecture

```
FlightRecorderManager (singleton)
    │
    ├─ FlightRecorderData       ← Enums, FlightFrame, FlightRecording, RecordingSettings, RecordingSerializer
    ├─ FlightPlaybackController ← Play/Pause/Stop/Seek, speed control, ghost spawning, frame interpolation
    ├─ ReplayCameraController   ← Follow/Cockpit/Chase/Orbit/Free/Cinematic camera modes
    ├─ RecordingStorageManager  ← JSON save/load, quota management, recording index
    ├─ RecordingSharingManager  ← Export/import .swefr files, share codes, clipboard
    ├─ ReplayTimelineUI         ← Timeline scrubber, transport controls, keyboard shortcuts
    ├─ FlightPathRenderer       ← 3D LineRenderer path with altitude/speed colour gradient
    ├─ ReplayGhostAircraft      ← Transparent ghost aircraft with particle trail
    └─ FlightAnalyticsOverlay   ← Speed/altitude graphs, G-force, control inputs, stats panel
```

---

## Scripts

### 1. `FlightRecorderData.cs`
Pure data classes and enums — no MonoBehaviour dependencies.

| Type | Description |
|------|-------------|
| `RecordingState` | Idle / Recording / Paused / Stopped |
| `PlaybackSpeed` | Quarter (0.25×) / Half / Normal / Double / Quadruple (4×) |
| `CameraAngle` | FollowCam / CockpitCam / ChaseCam / OrbitCam / FreeCam / CinematicCam |
| `FlightFrame` | Per-frame snapshot: position, rotation, velocity, altitude, timestamp, throttle, pitch/roll/yaw inputs, speed |
| `FlightRecording` | Full recording: metadata + `List<FlightFrame>` + thumbnail + derived stats |
| `RecordingSettings` | captureRate, maxDuration, qualityLevel, autoSave |
| `RecordingSerializer` | `ToJson`, `FromJson`, `BuildFileName` static helpers |

---

### 2. `FlightRecorderManager.cs`
Central singleton. `DontDestroyOnLoad`.

**Responsibilities:**
- Ring-buffer frame capture at configurable fps (default 30 fps, max 18 000 frames)
- Auto-trim old frames to respect `RecordingSettings.maxDuration`
- Resolves `FlightController` / `AltitudeController` at startup via `FindFirstObjectByType`
- Triggers auto-save via `RecordingStorageManager` when `autoSave = true`

**Public API:**
```csharp
FlightRecorderManager.Instance.StartRecording();
FlightRecorderManager.Instance.PauseRecording();
FlightRecorderManager.Instance.ResumeRecording();
FlightRecorderManager.Instance.StopRecording();          // → FlightRecording
FlightRecorderManager.Instance.Settings                  // RecordingSettings
```

**Events:** `OnRecordingStarted`, `OnRecordingStopped`, `OnPlaybackStarted`, `OnPlaybackFinished`, `OnFrameCaptured`

---

### 3. `FlightPlaybackController.cs`
Controls playback of a loaded `FlightRecording`.

- Lerp (position) + Slerp (rotation) frame interpolation for smooth sub-frame playback
- Coroutine-based playback loop driven by `Time.deltaTime × speedMultiplier`
- Ghost aircraft spawning: instantiates `ghostAircraftPrefab`, disables physics, calls `ReplayGhostAircraft.Initialise`

**Public API:**
```csharp
controller.LoadRecording(recording);
controller.Play();
controller.Pause();
controller.StopPlayback();
controller.Seek(float seconds);
controller.SeekNormalised(float t);      // t ∈ [0, 1]
controller.SetSpeed(PlaybackSpeed);
controller.LoopMode = true;
```

**Events:** `OnPlaybackTimeChanged(float progress)`, `OnPlaybackSpeedChanged(PlaybackSpeed)`

---

### 4. `ReplayCameraController.cs`
Manages six camera angle modes with smooth Lerp-based transitions.

| Mode | Behaviour |
|------|-----------|
| FollowCam | Trails behind at configurable distance/height |
| CockpitCam | Locks to cockpit position/rotation |
| ChaseCam | Dynamic distance scales with aircraft speed |
| OrbitCam | Continuously orbits; configurable radius/speed |
| FreeCam | WASD + mouse, fully independent |
| CinematicCam | Auto-switches between Follow/Chase/Orbit on a timer |

Additional: camera shake impulse (`AddShake(amount)`), DOF hint properties (`DofFocusDistance`, `DofAperture`).

---

### 5. `RecordingStorageManager.cs`
Saves/loads recordings to `Application.persistentDataPath/Recordings/`.

- JSON serialisation via `RecordingSerializer`
- Maintains a lightweight `RecordingIndex` (metadata only) for fast listing
- Hard quota: 100 MB; warning: 50 MB — fires `OnStorageWarning`
- File naming: `SWEF_Recording_{timestamp}_{aircraftType}.json`

**Public API:**
```csharp
RecordingStorageManager.Instance.SaveRecording(recording);
RecordingStorageManager.Instance.LoadRecording(id);        // → FlightRecording
RecordingStorageManager.Instance.GetAllRecordings();       // → IReadOnlyList<RecordingMeta>
RecordingStorageManager.Instance.DeleteRecording(id);
RecordingStorageManager.Instance.GetUsedBytes();           // → long
```

---

### 6. `ReplayTimelineUI.cs`
uGUI-based replay timeline interface.

- Timeline `Slider` scrubber with pointer-drag detection (no feedback loop)
- Play/Pause/Stop, speed buttons (0.25× … 4×), loop `Toggle`
- Current time / total duration formatted as `mm:ss.ff`
- Camera angle `Dropdown` wired to `ReplayCameraController`
- Mini-map position indicator
- Keyboard shortcuts: **Space** = play/pause, **←/→** = ±5 s, **+/-** = speed step

---

### 7. `FlightPathRenderer.cs` *(pre-existing)*
Renders the recorded path as a coloured `LineRenderer` in world space with altitude/speed gradient.

---

### 8. `ReplayGhostAircraft.cs`
Transparent ghost aircraft driven by the playback controller.

- Instantiates material instances (no shared-material mutation) and sets URP transparency
- Configurable opacity, tint colour, particle trail (`ParticleSystem`)
- Optional control-surface animation: aileron / elevator / rudder rotations mirrored from `FlightFrame` inputs
- `SetVisible(bool)`, `SetColor(Color)`, `SetOpacity(float)` runtime API

---

### 9. `RecordingSharingManager.cs`
Export/import recordings in a compressed `.swefr` format.

- **Export:** JSON → UTF-8 → `DeflateStream` compression → binary file in `Exports/`
- **Import:** reverse pipeline with version check (`version > CurrentVersion` → reject) and basic validation
- **Share codes:** 8-character alphanumeric code deterministically generated from recording ID; auto-copied to clipboard via `GUIUtility.systemCopyBuffer`
- Max import size: 10 MB

**Events:** `OnRecordingExported`, `OnRecordingImported`, `OnShareCodeGenerated`

---

### 10. `FlightAnalyticsOverlay.cs`
Real-time HUD overlay rendered during replay.

| Panel | Contents |
|-------|----------|
| Speed Graph | Scrolling `Texture2D` line graph, colour-coded (green / yellow / red) |
| Altitude Graph | Same layout, thresholds at 8 000 m / 12 000 m |
| G-Force | Derived from velocity-Y delta; colour indicator |
| Controls | Joystick stick indicator, throttle slider, rudder bar |
| Statistics | Max speed, max altitude, total distance, average speed, flight time |

All panels independently toggleable via `SetPanelVisible(name, bool)` or the master `ToggleOverlay()`.

---

## Integration Points

| System | How connected |
|--------|---------------|
| `FlightController` | Player aircraft transform, speed, inputs resolved at startup |
| `AltitudeController` | Altitude value per frame |
| `RecordingStorageManager` | Auto-save on `FlightRecorderManager.StopRecording()` when `autoSave = true` |
| `ReplayGhostAircraft` | Spawned by `FlightPlaybackController` from `ghostAircraftPrefab` |
| `ReplayCameraController` | Controlled via `ReplayTimelineUI` dropdown |
| `FlightAnalyticsOverlay` | Subscribes to `FlightPlaybackController.CurrentTime` each `Update` |

---

## Setup

1. Add `FlightRecorderManager` to your bootstrap/persistent scene object.
2. Add `RecordingStorageManager` and `RecordingSharingManager` to the same object.
3. Add `FlightPlaybackController` to the replay scene, wire `ghostAircraftPrefab`.
4. Add `ReplayCameraController` to the main `Camera` (or a camera-controller wrapper).
5. Add `ReplayTimelineUI` and wire all uGUI references (Slider, Buttons, Text, Dropdown, Toggle).
6. Add `FlightAnalyticsOverlay` and wire its graph `RawImage`, text labels, and control widgets.
7. Add `FlightPathRenderer` and `ReplayGhostAircraft` components as needed.
8. Configure `RecordingSettings` in the `FlightRecorderManager` inspector:
   - `captureRate` — frames per second (default 30)
   - `maxDuration` — ring-buffer cap in seconds (default 300)
   - `autoSave` — save on stop (default true)
