# VoiceCommand — Phase 84

Cockpit Voice Assistant System for SWEF.

## Architecture

```
VoiceAssistantConfig (ScriptableObject)
│
├── VoiceRecognitionController  [Singleton MonoBehaviour]
│   ├── Activation modes: PushToTalk | WakeWord ("Hey Pilot") | AlwaysListening
│   ├── Events: OnKeywordRecognized(phrase, confidence), OnStateChanged(state)
│   └── SimulateRecognition() — for UI test-mode & unit tests
│
├── CommandRegistry  [Singleton MonoBehaviour]
│   ├── Register / Unregister / GetByCategory / GetAll
│   └── 40+ built-in commands registered on Awake
│
├── CommandParser  [Static utility]
│   ├── Parse(rawPhrase, registry) → (VoiceCommandDefinition, parameters)
│   ├── Levenshtein fuzzy matching (configurable threshold, default = 3)
│   ├── Parameter extraction ("set altitude to 30000" → altitude=30000)
│   └── GetSuggestions(partial, maxResults) — autocomplete
│
├── CommandExecutor  [Singleton MonoBehaviour]
│   ├── ExecuteCommand(definition, parameters) → VoiceCommandResult
│   ├── Per-command cooldown guard
│   ├── Category-enabled guard (uses VoiceAssistantConfig)
│   └── Null-safe subsystem dispatch stubs for all 40+ commands
│
├── VoiceConfirmationController  [MonoBehaviour]
│   ├── RequestConfirmation(command) — queues critical commands
│   ├── Confirm() / Cancel() — voice or touch
│   ├── Timeout auto-cancel (configurable via VoiceAssistantConfig)
│   └── Events: OnConfirmed, OnCancelled, OnConfirmationRequested
│
├── VoiceResponseGenerator  [Static utility]
│   ├── GetShortResponse(result, parameters) — for HUD toast
│   ├── GetDetailedResponse(result, parameters) — for history log
│   └── GetConfirmationPrompt(command) — "Are you sure?" text
│
├── VoiceCommandHistory  [MonoBehaviour]
│   ├── Circular buffer (capacity set by VoiceAssistantConfig.maxHistoryEntries)
│   ├── Record / GetRecent / GetByCategory / ClearHistory
│   └── JSON persistence → persistentDataPath/voice_history.json
│
├── VoiceCommandHUD  [MonoBehaviour]
│   ├── State indicator (Idle/Listening/Processing/Confirmed/Error)
│   ├── Recognised-phrase display with confidence bar
│   ├── Response toast (auto-fades after configurable duration)
│   └── Audio-level meter
│
├── VoiceCommandUI  [MonoBehaviour]
│   ├── Enable/disable toggle
│   ├── Activation-mode selector (dropdown)
│   ├── Confidence-threshold slider
│   ├── Searchable command reference list
│   ├── Command history view (last 20 entries)
│   └── Test-mode button (SimulateRecognition)
│
├── VoiceATCBridge  [MonoBehaviour]
│   ├── Translates natural-language phrases to ATC protocol strings
│   ├── Callsign + runway + flight-level formatting
│   └── `#if SWEF_ATC_AVAILABLE` compile guard
│
└── VoiceCommandAnalytics  [MonoBehaviour]
    ├── Events: voice_command_recognized, voice_command_executed,
    │          voice_command_failed, voice_activation_mode_changed
    └── `#if SWEF_ANALYTICS_AVAILABLE` compile guard
```

## Built-in Commands (40+)

| Category | Examples |
|----------|---------|
| **Flight** | increase/decrease throttle, set altitude [N], bank left/right, level wings, nose up/down, engage/disengage autopilot, flaps up/down, landing gear up/down *(confirmation required)*, emergency landing *(confirmation required)* |
| **Navigation** | set waypoint [name], next waypoint, show route, distance to destination, ETA, heading [N] degrees |
| **Instruments** | show altimeter, show speed, calibrate instruments |
| **Weather** | weather report, turbulence level, wind direction, visibility check |
| **Music** | play/pause music, next track, volume up/down |
| **Camera** | photo mode, take screenshot, cinematic view, cockpit view, chase view |
| **System** | pause/resume game, save flight, show map, toggle HUD, toggle minimap |

## Setup Guide

1. Add `CommandRegistry`, `CommandExecutor`, `VoiceRecognitionController`, and
   `VoiceCommandHistory` MonoBehaviours to a persistent scene GameObject.
2. Create a `VoiceAssistantConfig` asset via
   **Assets → Create → SWEF → Voice Assistant Config** and assign it to each component.
3. Add `VoiceCommandHUD` to your HUD Canvas.
4. Optionally add `VoiceCommandUI` to a settings screen.
5. Optionally add `VoiceATCBridge` and `VoiceCommandAnalytics`.

## Integration Map

| SWEF System | Integration point |
|-------------|------------------|
| Flight (FlightController) | `CommandExecutor.FlightCmd` stubs |
| Autopilot | `cmd_engage_autopilot` / `cmd_disengage_autopilot` |
| Navigation (RoutePlanner) | `cmd_set_waypoint`, `cmd_next_waypoint`, `cmd_heading` |
| Weather | `cmd_weather_report`, `cmd_turbulence_level`, `cmd_wind_direction` |
| CockpitHUD | `cmd_show_altimeter`, `cmd_show_speed`, `cmd_calibrate_instruments` |
| AdaptiveMusic / MusicPlayer | `cmd_play_music`, `cmd_pause_music`, `cmd_next_track`, `cmd_volume_*` |
| Camera / PhotoMode | `cmd_photo_mode`, `cmd_take_screenshot`, `cmd_*_view` |
| ATC | `VoiceATCBridge` + `#if SWEF_ATC_AVAILABLE` |
| Analytics | `VoiceCommandAnalytics` + `#if SWEF_ANALYTICS_AVAILABLE` |

## Localization Keys

33 keys added to all 8 language files:
- `voice_category_*` (9) — category names
- `voice_response_*` (10) — response templates with `{param}` substitution
- `voice_title`, `voice_mode_*`, `voice_activation_*`, `voice_confirm_*`,
  `voice_listening`, `voice_processing`, `voice_error_*` (14) — UI strings
