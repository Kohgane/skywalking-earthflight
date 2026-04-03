# SWEF Accessibility & Platform Optimization

**Phase 93** | Namespace: `SWEF.Accessibility` | Directory: `Assets/SWEF/Scripts/Accessibility/`

This module makes Skywalking: Earth Flight accessible to all players and optimizes it for multi-platform launch (PC primary, mobile secondary).

---

## New Scripts (Phase 93)

### Data Models

| File | Type | Purpose |
|------|------|---------|
| `AccessibilityProfile.cs` | Data | Serializable: all accessibility settings in one profile |
| `PlatformProfile.cs` | Data | Serializable: platform-specific quality settings |
| `InputRemapData.cs` | Data | Serializable: custom input mappings + `RemappableActions` registry |
| `PerformanceMetrics.cs` | Struct | FPS, frame time, memory, draw calls, tile cache hit rate |

### Accessibility Systems

| File | Type | Purpose |
|------|------|---------|
| `AccessibilityManager.cs` | Singleton MB | Central manager: load/save `accessibility_settings.json`, presets, events |
| `ColorblindFilter.cs` | Singleton MB | Daltonization (Protanopia / Deuteranopia / Tritanopia / Achromatopsia), UI recolour |
| `SubtitleController.cs` | Singleton MB | Subtitles for voice, ATC, assistant, multiplayer; queue, fade, speaker colours |
| `ScreenReaderBridge.cs` | Static | TTS interface, priority queue (Interrupt / High / Normal / Low) |
| `MotorAccessibilityController.cs` | MB | One-handed mode, auto-hover assist, dwell click, input smoothing |
| `InputRemapController.cs` | Singleton MB | Full keyboard/gamepad/touch remap, conflict detection, `input_remap.json` |

### Platform Optimization

| File | Type | Purpose |
|------|------|---------|
| `PlatformOptimizer.cs` | Singleton MB | Auto-detect platform, apply `PlatformProfile`, quality tiers Ultra→Potato |
| `DynamicQualityScaler.cs` | MB | FPS monitoring, auto-adjust quality tier with hysteresis |
| `MemoryBudgetController.cs` | Singleton MB | Memory budget enforcement, cache cleanup, GC |
| `PerformanceMonitor.cs` | Singleton MB | FPS counter, frame time, memory tracking, optional debug overlay |
| `LoadingOptimizer.cs` | Singleton MB | Async loading, tile prefetch scheduling, concurrent-load limit |

### Visual Accessibility

| File | Type | Purpose |
|------|------|---------|
| `HighContrastMode.cs` | MB | High-contrast palette for all UI Text / Image elements |
| `FlashWarningController.cs` | Singleton MB | Replaces strobe effects with a static warning icon |
| `HUDScaleController.cs` | Singleton MB | Global HUD scale (0.5–2×) and text scale (0.75–2×) |

### Audio Accessibility

| File | Type | Purpose |
|------|------|---------|
| `AudioAccessibilityController.cs` | Singleton MB | Per-channel volume, mono audio, audio descriptions, visual sound indicator |

### Integration & Analytics

| File | Type | Purpose |
|------|------|---------|
| `AccessibilityBridge.cs` | Static | Cross-system bridge: CockpitHUD scale, VoiceCommand subtitles, Security save-validate |
| `AccessibilityAnalytics.cs` | Static | 8 telemetry event methods → TelemetryDispatcher |

### Pre-existing Scripts (updated)

| File | Notes |
|------|-------|
| `AccessibilityManager.cs` | Migrated persistence to `accessibility_settings.json`; updated to new profile fields; added `ApplyProfile`, `GetActiveProfile`, `ResetToDefault`, `OnColorBlindModeChanged`, `OnSubtitleSettingsChanged` |
| `SubtitleSystem.cs` | Unchanged (superset; `SubtitleController.cs` provides the Phase 93 API) |
| `ColorblindFilter.cs` | Unchanged (provides `ColorblindMode` and daltonization; AccessibilityProfile introduces parallel `ColorBlindMode` for profile serialization) |
| `ScreenReaderBridge.cs` | Unchanged (provides full TTS engine abstraction) |
| `AdaptiveInputManager.cs` | Unchanged (provides gyro / dead-zone / sequential input helpers) |

---

## Preset Profiles

| Preset | What it enables |
|--------|----------------|
| `Default` | All features off |
| `Low Vision` | Screen reader, high contrast, reduced motion, HUD/text scale 1.5× |
| `Color Blind Friendly` | Deuteranopia daltonization at full intensity |
| `Motor Impaired` | One-handed mode, auto-hover assist, HUD scale 1.25× |
| `Hearing Impaired` | Subtitles on, audio descriptions on |
| `Full Assist` | All of the above combined |

---

## Quality Tiers

| Tier | Target FPS | Shadow | AA | Post-FX | Volumetrics |
|------|-----------|--------|----|---------|-------------|
| Ultra | 120 | High | MSAA 4× | ✓ | ✓ |
| High | 60 | High | SMAA | ✓ | ✓ |
| Medium | 60 | Medium | FXAA | ✓ | ✗ |
| Low | 30 | Low | None | ✗ | ✗ |
| Potato | 30 | Off | None | ✗ | ✗ |

`DynamicQualityScaler` steps down after 3 s of sustained low FPS and steps up after 10 s of sustained high FPS (5 s cooldown between changes).

---

## Persistence

| File | Contents |
|------|----------|
| `accessibility_settings.json` | Full `AccessibilityProfile` (persistent data path) |
| `input_remap.json` | All custom input bindings (persistent data path) |

---

## Integration Points

| System | Integration | Guard |
|--------|-------------|-------|
| `SWEF.CockpitHUD.HUDController` | `SetScale(hudScale)` | `#if SWEF_COCKPITHUD_AVAILABLE` |
| `SWEF.Achievement.AchievementManager` | `ReportProgress("accessibility_enabled", 1)` | `#if SWEF_ACHIEVEMENT_AVAILABLE` |
| `SWEF.Security.SaveFileValidator` | Validate `accessibility_settings.json` on save | `#if SWEF_SECURITY_AVAILABLE` |
| `SWEF.Analytics.TelemetryDispatcher` | 8 events via `AccessibilityAnalytics` | `#if SWEF_ANALYTICS_AVAILABLE` |
| `SubtitleController` | Voice transcript feed via `AccessibilityBridge.FeedVoiceSubtitle` | Always |
