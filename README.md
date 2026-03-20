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

---

## Phase 33 — Multiplayer Co-op Flight & Formation Flying System

### New Scripts (8 files — all in `Assets/SWEF/Scripts/Multiplayer/`)

| # | Script | Namespace | Purpose |
|---|--------|-----------|---------|
| 1 | `Multiplayer/NetworkManager2.cs` | `SWEF.Multiplayer` | Advanced lobby manager — 6-char room codes, automatic host migration (by latency rank), UDP NAT punch-through with relay fallback, per-player RTT/jitter/packet-loss metrics, `ConnectionQuality` enum |
| 2 | `Multiplayer/PlayerSyncSystem.cs` | `SWEF.Multiplayer` | 20 Hz tick-rate state sync — interpolation buffer (≥ 3 snapshots), dead-reckoning with velocity extrapolation, delta compression via `PlayerSnapshot.flags` bitfield, bandwidth tracking |
| 3 | `Multiplayer/FormationFlyingManager.cs` | `SWEF.Multiplayer` | 7 formation types (`V_Formation`, `Diamond`, `Echelon_Left/Right`, `Line_Abreast`, `Trail`, `Finger_Four`), PID-based auto slot-keeping, per-wingman scoring (distance + heading + speed), ghost slot markers |
| 4 | `Multiplayer/VoiceChatManager.cs` | `SWEF.Multiplayer` | Proximity spatial audio (default 500 m falloff), team channel (full volume), push-to-talk & open-mic, VAD amplitude gate, noise gate + normalisation, codec bitrate simulation, per-player mute |
| 5 | `Multiplayer/CoopMissionSystem.cs` | `SWEF.Multiplayer` | 6 mission types (`Escort`, `Relay`, `FormationChallenge`, `SearchAndRescue`, `Recon`, `TimeAttack_Coop`), `NotStarted → Briefing → InProgress → Completed/Failed` lifecycle, role assignment (Lead/Wingman/Support/Scout), difficulty scaling by player count, formation XP bonus |
| 6 | `Multiplayer/MultiplayerWeatherSync.cs` | `SWEF.Multiplayer` | Host-authoritative weather sync — broadcasts `WeatherStatePacket` every 30 s (or on significant change), clients interpolate over configurable duration, stale-data fallback (> 5 min), integrates Phase 32 `WeatherManager` & `WeatherDataService` |
| 7 | `Multiplayer/MultiplayerHUD.cs` | `SWEF.Multiplayer` | World-space floating name tags with distance (km/mi), minimap blips colour-coded by role/team, formation slot arrow indicator, 50-message text chat (all/team channels), voice-speaking mic icon, connection-quality ping colour, toast notification feed (5 s auto-dismiss) |
| 8 | `Multiplayer/MultiplayerScoreboard.cs` | `SWEF.Multiplayer` | Live sortable scoreboard (Score / Formation % / Distance / Objectives / Ping), 5 s stat broadcast from host, `PlayerSessionStats` tracking, end-of-session summary with `SessionAward` enum (`MVP`, `BestWingman`, `MostObjectives`, `IronPilot`, `SharpShooter`) |

### Key Data Types

| Type | File | Description |
|------|------|-------------|
| `ConnectionQuality` | `NetworkManager2.cs` | `Excellent` / `Good` / `Fair` / `Poor` — derived from RTT and packet loss |
| `PlayerSnapshot` | `PlayerSyncSystem.cs` | `{ tick, position, rotation, velocity, throttle, flaps, flags }` |
| `FormationType` | `FormationFlyingManager.cs` | 7-value enum of aviation formation patterns |
| `VoiceChannel` | `VoiceChatManager.cs` | `Proximity` / `Team` / `Global` |
| `CoopMissionData` | `CoopMissionSystem.cs` | Mission definition (objectives, time limit, player count, XP reward) |
| `WeatherStatePacket` | `MultiplayerWeatherSync.cs` | Serialisable weather snapshot sent host → clients |
| `PlayerSessionStats` | `MultiplayerScoreboard.cs` | Per-player session stats including formation score, distance, ping |
| `SessionAward` | `MultiplayerScoreboard.cs` | 5 end-of-session award categories |

### Architecture

```
NetworkManager2 (Singleton)
│   ├── Lobby creation/join — 6-char room codes
│   ├── NAT punch-through → relay fallback
│   └── Host migration (latency-ranked promotion)
│
PlayerSyncSystem
│   ├── 20 Hz tick: CaptureLocalSnapshot → DeltaCompress → BroadcastSnapshot
│   ├── RemotePlayerSyncState: interpolation buffer + dead-reckoning
│   └── Bandwidth tracking (bytes/sec TX and RX)
│
FormationFlyingManager (Singleton)
│   ├── CreateFormation / JoinFormation / BreakFormation / ReformFormation
│   ├── Slot offsets calculated per FormationType
│   ├── PID steering correction for wingmen
│   └── Per-slot score: position deviation + heading + speed match
│
VoiceChatManager (Singleton)
│   ├── OpenMicrophone → ProcessMicrophoneInput (PTT / OpenMic + VAD)
│   ├── CompressAudio / DecompressAudio (codec simulation)
│   └── UpdateProximityVolumes — distance attenuation per AudioSource
│
CoopMissionSystem (Singleton)
│   ├── RegisterDefaultMissions (Escort, FormationChallenge, SAR)
│   ├── StartMission → BriefingPhase coroutine → InProgress
│   ├── ReportObjectiveProgress → CompleteObjective → CompleteMission
│   └── ScaleDifficulty — radius / time-limit scaling by player count
│
MultiplayerWeatherSync (Singleton)
│   ├── Host: polls WeatherManager, broadcasts WeatherStatePacket every 30 s
│   └── Client: ReceiveWeatherPacket → interpolated ForceWeather over 8 s
│
MultiplayerHUD (Singleton)
│   ├── AddPlayer / RemovePlayer — name tag + minimap blip lifecycle
│   ├── LateUpdate: UpdateNameTags + UpdateMinimapBlips + UpdateFormationIndicator
│   ├── ReceiveChatMessage / SendChatMessage (50-msg history)
│   └── ShowNotification (coroutine, 5 s auto-dismiss)
│
MultiplayerScoreboard (Singleton)
│   ├── RegisterPlayer / AddScore / AddFlightDistance / UpdateConnectionMetrics
│   ├── Host: BroadcastStats every 5 s
│   ├── Client: ReceiveStatsBroadcast → RefreshUI (sorted rows)
│   └── EndSession → BuildSummary → assign SessionAwards → OnSessionSummaryReady
```

### Integration Points

| Phase 33 Script | Integrates With |
|----------------|----------------|
| `NetworkManager2` | `NetworkTransport.cs` (Phase 20) — low-level send/receive |
| `PlayerSyncSystem` | `MultiplayerManager.BroadcastSyncData` (Phase 20) |
| `FormationFlyingManager` | `MultiplayerScoreboard` — formation score updates |
| `VoiceChatManager` | `Audio/` spatial audio system (Phase 28); `MultiplayerHUD` speaking indicators |
| `CoopMissionSystem` | `MultiplayerScoreboard` — objective completion score events |
| `MultiplayerWeatherSync` | `WeatherManager` + `WeatherDataService` (Phase 32) |
| `MultiplayerHUD` | `NetworkManager2`, `VoiceChatManager`, `FormationFlyingManager` |
| `MultiplayerScoreboard` | `FormationFlyingManager.OnSlotScoreUpdated`, `CoopMissionSystem.OnObjectiveCompleted` |
