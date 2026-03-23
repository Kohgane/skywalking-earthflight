# Phase 57 — Input Rebinding & Controller Support System

**Namespace:** `SWEF.InputSystem`  
**Directory:** `Assets/SWEF/Scripts/InputSystem/`

---

## Overview

A complete Input Rebinding & Controller Support System that handles custom keybinding,
gamepad profiles, touch controls, and input device management for the skywalking-earthflight
flight simulator.  All cross-system calls are guarded by `#if SWEF_*_AVAILABLE` defines so
this module compiles cleanly when other systems are absent.

---

## Architecture

```
InputBindingManager (singleton)
    │
    ├── InputSystemData          ← Enums, BindingEntry, GamepadProfile, TouchControlLayout,
    │                               InputPreset, InputSystemProfile (ScriptableObject)
    │
    ├── GamepadInputHandler      ← Dead-zone, sensitivity-curve, axis-inversion, connection detection
    ├── TouchInputHandler        ← Virtual joystick, swipe/tap/pinch gestures
    ├── InputDeviceDetector      ← Runtime Keyboard / Gamepad / Touch switching
    │
    ├── InputRebindingUI         ← Panel UI, per-category rows, listening overlay
    ├── InputPresetManager       ← Save / load / delete named presets (PlayerPrefs JSON)
    ├── InputHapticFeedback      ← Rumble motor control, Light / Medium / Heavy presets
    └── InputAnalytics           ← Telemetry: rebind, device-switch, preset-applied events
```

---

## Data Layer (`InputSystemData.cs`)

| Type | Description |
|------|-------------|
| `InputDeviceType` | Keyboard / Gamepad / Touch / VR / HOTAS |
| `InputActionCategory` | Flight / Camera / UI / Social / PhotoMode / MusicPlayer |
| `BindingEntry` | actionName, category, primaryKey, secondaryKey, gamepadButton, isRebindable |
| `GamepadProfile` | profileName, deadzoneInner/Outer, sensitivityCurvePoints, invertPitch/Yaw/Roll, vibrationEnabled |
| `TouchControlLayout` | joystickPosition, joystickSize, buttonPositions, gestureSensitivity |
| `InputPreset` | presetName, description, List\<BindingEntry\> |
| `InputSystemProfile` | ScriptableObject — combines all above + persistBindings, allowRebinding, rebindTimeoutSeconds |

---

## Script Reference

| File | Role |
|------|------|
| `InputSystemData.cs` | Pure data layer — enums, structs, ScriptableObject |
| `InputBindingManager.cs` | Singleton — binding map, GetAction*, SetKey, StartRebind, ApplyPreset |
| `GamepadInputHandler.cs` | Gamepad axis/button processing with dead-zone & curve shaping |
| `TouchInputHandler.cs` | Virtual joystick, tap, swipe, pinch gesture recognition |
| `InputDeviceDetector.cs` | Runtime device-type auto-detection & switching |
| `InputRebindingUI.cs` | Rebinding panel UI — rows, category filter, listening overlay, preset buttons |
| `InputPresetManager.cs` | Preset catalogue, save/load to PlayerPrefs, activate preset |
| `InputHapticFeedback.cs` | Gamepad rumble — Light/Medium/Heavy/Asymmetric, respects GamepadProfile |
| `InputAnalytics.cs` | Batched telemetry for rebind, device-switch, and preset events |

---

## Default Bindings

| Action | Primary Key | Gamepad | Category |
|--------|-------------|---------|----------|
| ThrottleUp | W | Right Trigger | Flight |
| ThrottleDown | S | Left Trigger | Flight |
| YawLeft | A | Horizontal | Flight |
| YawRight | D | Horizontal | Flight |
| PitchUp | Up Arrow | Vertical | Flight |
| PitchDown | Down Arrow | Vertical | Flight |
| RollLeft | Q | LB (button 4) | Flight |
| RollRight | E | RB (button 5) | Flight |
| Boost | Left Shift | A (button 0) | Flight |
| Brake | Space | B (button 1) | Flight |
| CameraSwitch | C | Back (button 8) | Camera |
| CameraZoomIn | Numpad + | Right Stick Y | Camera |
| CameraZoomOut | Numpad - | Right Stick Y | Camera |
| Pause | Escape | Start (button 7) | UI |
| Confirm | Enter | A (button 0) | UI |
| Back | Backspace | B (button 1) | UI |
| ChatToggle | T | — | Social |
| VoiceToggle | V | RS (button 9) | Social |
| PhotoCapture | F12 | X (button 2) | PhotoMode |
| PhotoFilter | F | Y (button 3) | PhotoMode |
| MusicPlay | M | — | MusicPlayer |
| MusicNext | . | — | MusicPlayer |
| MusicPrev | , | — | MusicPlayer |

---

## Setup

1. Create an **Input System Profile** asset:  
   *Assets → Create → SWEF → InputSystem → Input System Profile*

2. Add a **persistent bootstrap GameObject** to your boot scene and attach:
   - `InputBindingManager`
   - `GamepadInputHandler`
   - `TouchInputHandler`
   - `InputDeviceDetector`
   - `InputPresetManager`
   - `InputHapticFeedback`
   - `InputAnalytics`

3. Wire the `InputSystemProfile` asset to the `InputBindingManager` inspector field.

4. Attach `InputRebindingUI` to your settings/pause-menu canvas and wire the panel,
   row template, container, and buttons in the inspector.

---

## Integration Points

| System | How InputSystem Connects |
|--------|--------------------------|
| `Flight.FlightController` | Reads `InputBindingManager.GetActionHeld("ThrottleUp")` etc. |
| `PhotoMode.PhotoCaptureManager` | Listens for `PhotoCapture` action |
| `MusicPlayer.MusicPlayerManager` | Listens for `MusicPlay / MusicNext / MusicPrev` actions |
| `Social.VoiceChatManager` | Listens for `VoiceToggle` action |
| `Analytics.AnalyticsManager` | Receives flushed telemetry (guarded by `#if SWEF_ANALYTICS_AVAILABLE`) |
| `Settings.SettingsManager` | Stores/restores `masterIntensity` and `persistBindings` |

All cross-system calls are guarded by `#if SWEF_*_AVAILABLE` defines.

---

## PlayerPrefs Keys

| Key | Value |
|-----|-------|
| `SWEF_Bind_{action}_primary` | Primary key name |
| `SWEF_Bind_{action}_secondary` | Secondary key name |
| `SWEF_InputPresets_Names` | Comma-separated list of custom preset names |
| `SWEF_InputPreset_{name}` | JSON-serialised `PresetWrapper` |
| `SWEF_InputPreset_Active` | Name of the last-activated preset |
