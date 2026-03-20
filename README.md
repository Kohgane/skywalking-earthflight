# SkywalkingEarthFlight (SWEF)

🚀 **Fly from your exact location to the edge of space.**

A mobile flight-experience app powered by Google Photorealistic 3D Tiles via Cesium for Unity.

## Features (MVP)
- **Launch** — Start from your GPS location on real 3D terrain
- **Flight** — Free-fly with touch controls + Comfort mode (anti-motion-sickness)
- **Ascent** — Rise through atmosphere layers to the Kármán line and beyond
- **XR/VR** — VR headset support with comfort options + hand tracking (planned)
- **Accessibility** — Colorblind modes, dynamic text scaling, one-handed mode, screen reader support, haptic feedback
- **Cinema System** — Time-of-day control, photo mode with filters/frames, and cinematic camera paths
- **Replay System** — Save, share, and race against ghost replays with 3D flight path visualization

## Tech Stack
| Layer | Technology |
|-------|-----------|
| Engine | Unity 2022.3 LTS + URP |
| Earth Data | Google Photorealistic 3D Tiles (Map Tiles API) |
| Tile Renderer | Cesium for Unity |
| Location | GPS (foreground) |
| Platforms | iOS, Android, XR (Meta Quest, Vision Pro planned) |

## Project Structure
```
Assets/SWEF/
├── Scenes/          # Boot.unity + World.unity (created in Unity Editor)
├── Scripts/
│   ├── Cinema/      # TimeOfDayController, PhotoModeController, CinematicCameraPath, CinematicCameraUI
│   ├── Core/        # BootManager, SWEFSession, WorldBootstrap
│   ├── Flight/      # FlightController, TouchInputRouter, AltitudeController, HoldButton
│   ├── Haptic/      # HapticManager, HapticTriggerZone
│   ├── Replay/      # ReplayData, ReplayFileManager, GhostRacer, FlightPathRenderer, ReplayShareManager
│   ├── UI/          # HudBinder, AccessibilityController, OneHandedModeController, VoiceCommandManager
│   ├── XR/          # XRPlatformDetector, XRRigManager, XRInputAdapter, XRHandTracker, XRComfortSettings, XRUIAdapter
│   └── Util/        # ExpSmoothing
└── README_SWEF_SETUP.md
```

## Setup
See [`Assets/SWEF/README_SWEF_SETUP.md`](Assets/SWEF/README_SWEF_SETUP.md) for detailed setup instructions.

## Store
- **App Store / Play Store Title**: Skywalking: Earth Flight (SWEF)
- **iOS Bundle ID**: `com.kohgane.swef.earthflight`
- **Android applicationId**: `com.kohgane.swef.earthflight`

## License
TBD

## Attribution
This app uses Google Photorealistic 3D Tiles. All required attributions are displayed in-app as mandated by Google Maps Platform Terms of Service.

---

## Phase 31 — Achievement System 2.0: Badges, Milestones & Social Sharing

### New Scripts (12 files)

| # | Script | Namespace | Purpose |
|---|--------|-----------|---------|
| 1 | `Achievement/AchievementDefinition.cs` | `SWEF.Achievement` | ScriptableObject defining a single achievement (tier, category, target value, XP reward) |
| 2 | `Achievement/AchievementState.cs` | `SWEF.Achievement` | Serializable per-achievement runtime state (progress, unlock date) |
| 3 | `Achievement/AchievementManager.cs` | `SWEF.Achievement` | Singleton — JSON persistence, `ReportProgress`, `SetProgress`, events |
| 4 | `Achievement/AchievementTracker.cs` | `SWEF.Achievement` | Auto-tracks 8 metric categories (flight time, altitude, speed, distance, etc.) |
| 5 | `Achievement/AchievementNotificationUI.cs` | `SWEF.Achievement` | Slide-in popup queue for newly unlocked achievements |
| 6 | `Achievement/AchievementPanelUI.cs` | `SWEF.Achievement` | Full-screen scrollable gallery with category filters and sort options |
| 7 | `Achievement/AchievementCardUI.cs` | `SWEF.Achievement` | Individual gallery card (grayscale when locked, tap to expand, share button) |
| 8 | `Achievement/AchievementShareController.cs` | `SWEF.Achievement` | Native share sheet / clipboard fallback with achievement image capture |
| 9 | `Achievement/MilestoneDefinition.cs` | `SWEF.Achievement` | ScriptableObject for meta-achievements requiring multiple unlocks |
| 10 | `Achievement/MilestoneTracker.cs` | `SWEF.Achievement` | Listens for achievement events and completes milestones + awards bonus XP |
| 11 | `Achievement/AchievementData.cs` | `SWEF.Achievement` | Static helper returning 30 default achievement definitions |
| 12 | `Editor/AchievementEditorWindow.cs` | `SWEF.Editor` | `SWEF > Achievement Editor` — validate, bulk-create, preview |

### Achievement Tiers & Colours

| Tier | Colour |
|------|--------|
| 🥉 Bronze | `#CD7F32` |
| 🥈 Silver | `#C0C0C0` |
| 🥇 Gold | `#FFD700` |
| 💠 Platinum | `#E5E4E2` |
| 💎 Diamond | `#B9F2FF` |

### Achievement Categories
`Flight` · `Altitude` · `Speed` · `Exploration` · `Social` · `Collection` · `Challenge` · `Special`

### ScriptableObject Workflow

1. Open **SWEF > Achievement Editor** in the Unity menu bar.
2. Click **Bulk Create Defaults** to generate all 30 default `AchievementDefinition` assets in `Assets/SWEF/Resources/Achievements/`.
3. Click **Validate All** to check for duplicate IDs, missing icons, or missing localization keys.
4. Similarly, create `MilestoneDefinition` assets under `Assets/SWEF/Resources/Milestones/` for meta-achievements.

### Architecture

```
AchievementManager (Singleton, DontDestroyOnLoad)
│   ├── Loads AchievementDefinition[] from Resources/Achievements/
│   ├── Persists AchievementState[] → persistentDataPath/achievements.json
│   └── Event: OnAchievementUnlocked
│
├── AchievementTracker  →  polls FlightController / AltitudeController each frame
│                          calls ReportProgress() / SetProgress()
│
├── AchievementNotificationUI  →  subscribes OnAchievementUnlocked, shows queue
├── AchievementPanelUI         →  gallery with filter/sort
│   └── AchievementCardUI[]    →  per-card view + AchievementShareController
│
└── MilestoneTracker   →  loads MilestoneDefinition[], checks on every unlock
                          fires OnMilestoneCompleted, awards bonus XP
```

### Milestone Configuration

Create a `MilestoneDefinition` ScriptableObject and fill in:
- `id` — unique identifier
- `titleKey` / `descriptionKey` — localization keys
- `requiredAchievementIds` — array of achievement IDs that must all be unlocked
- `bonusXP` — extra XP awarded on completion
- `tier` — visual tier for the milestone badge

### Localization Keys (Phase 31 additions)

All 8 language JSON files (`lang_en.json` … `lang_pt.json`) have been extended with:
- `achievement_panel_title`, `achievement_filter_*`, `achievement_total_xp`, `achievement_progress`
- `achievement_unlocked`, `achievement_hidden`, `achievement_share_text`, `milestone_completed`
- 60 achievement title/description keys (`ach_*_title` / `ach_*_desc`)

### Persistence

| File | Location | Contents |
|------|----------|----------|
| `achievements.json` | `Application.persistentDataPath` | All `AchievementState` records |
| `milestones.json` | `Application.persistentDataPath` | Completed milestone ID list |
