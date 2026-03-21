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

---

## Phase 34 — Accessibility & Adaptive Input System

### New Scripts (8 files — all in `Assets/SWEF/Scripts/Accessibility/`)

| # | Script | Namespace | Purpose |
|---|--------|-----------|---------|
| 1 | `Accessibility/AccessibilityManager.cs` | `SWEF.Accessibility` | Central singleton — serialisable `AccessibilityProfile`, JSON persistence via `PlayerPrefs`, 6 preset profiles (`Default`, `LowVision`, `Colorblind`, `MotorImpaired`, `HearingImpaired`, `FullAssist`), string-keyed feature flag dictionary, OS hint auto-detection |
| 2 | `Accessibility/AdaptiveInputManager.cs` | `SWEF.Accessibility` | Full key/button remapping (`Dictionary<InputAction, KeyCode>`), one-handed left/right layouts, gyroscope steering, sequential scanning mode, hold-vs-toggle per action, per-axis dead-zone + sensitivity curves (Linear/Exponential/S-Curve), turbo auto-repeat |
| 3 | `Accessibility/ScreenReaderBridge.cs` | `SWEF.Accessibility` | `ITTSEngine` interface + console stub, platform hooks for iOS VoiceOver / Android TalkBack / Windows Narrator, priority queue (`Critical`→`Low`), UI focus tracking, earcon audio cues, configurable WPM speech rate |
| 4 | `Accessibility/ColorblindFilter.cs` | `SWEF.Accessibility` | 5 colorblind modes (`None`→`Achromatopsia`), scientifically-based 3×3 colour-matrix post-processing, simulate vs. correct toggle, custom palette override, UI element recolouring, high-contrast mode, 0–100% intensity blend |
| 5 | `Accessibility/SubtitleSystem.cs` | `SWEF.Accessibility` | FIFO subtitle queue, closed-captions sound descriptions, colour-coded speaker names, configurable position/font-size/background opacity, WCAG-aligned reading-speed auto-duration (21 cps), Phase 30 localization integration |
| 6 | `Accessibility/UIScalingSystem.cs` | `SWEF.Accessibility` | Global 0.5×–3.0× canvas scale, DPI-aware suggestion, 5-level large-text mode (+0–100%), spacing multiplier, pulsing focus highlight (Outline component), reduced-motion propagation to `SWEF.UI.AccessibilityManager`, simplified-UI element hiding |
| 7 | `Accessibility/HapticAccessibility.cs` | `SWEF.Accessibility` | Visual-to-haptic substitution, 9 built-in patterns (`Waypoint_Near`, `Stall_Warning`, `Altitude_Low`, `Formation_Drift`, `Mission_Complete`, `Collision_Warning`, `Turbulence`, `Landing_Gear`, `Rhythm_Formation`), audio-to-haptic conversion, 0–200% global intensity multiplier |
| 8 | `Accessibility/CognitiveAssistSystem.cs` | `SWEF.Accessibility` | Simplified-flight auto-management (altitude+speed), 4-step game-speed control (0.25×–1.0×), 3-level HUD density (`Full`/`Reduced`/`Minimal`), cooldown-aware reminder system, force-pause anywhere, auto-difficulty adjustment from death/retry telemetry |

### Key Data Types

| Type | File | Description |
|------|------|-------------|
| `AccessibilityProfile` | `AccessibilityManager.cs` | Serialisable container for all accessibility preferences; saved as JSON in `PlayerPrefs` |
| `AccessibilityPreset` | `AccessibilityManager.cs` | 6-value enum for quick-apply preset profiles |
| `InputAction` | `AdaptiveInputManager.cs` | Abstract game actions decoupled from physical inputs (Throttle, Pitch, Roll, Yaw, …) |
| `AxisSettings` | `AdaptiveInputManager.cs` | Per-axis dead-zone, sensitivity, and curve-shape settings |
| `ITTSEngine` | `ScreenReaderBridge.cs` | `Speak / Stop / IsSpeaking` interface for platform TTS backends |
| `SpeechPriority` | `ScreenReaderBridge.cs` | 4-level priority queue (`Critical`, `High`, `Medium`, `Low`) |
| `ColorblindMode` | `ColorblindFilter.cs` | `None` / `Protanopia` / `Deuteranopia` / `Tritanopia` / `Achromatopsia` |
| `SubtitleEntry` | `SubtitleSystem.cs` | Single subtitle record (text, speaker, colour, duration, localization key) |
| `HapticStep` / `HapticPattern` | `HapticAccessibility.cs` | `(intensity, duration, pause)` tuple sequence for named haptic patterns |
| `HUDInfoLevel` | `CognitiveAssistSystem.cs` | `Full` / `Reduced` / `Minimal` information density |

### Architecture

```
AccessibilityManager (Singleton, DontDestroyOnLoad)
│   ├── AccessibilityProfile — JSON persistence via PlayerPrefs
│   ├── ApplyPreset(AccessibilityPreset) — quick-apply 6 presets
│   ├── SetFeature(key, bool) — runtime feature flag toggle
│   └── OnProfileChanged / OnFeatureToggled / OnPresetApplied events
│
AdaptiveInputManager (Singleton)
│   ├── GetKey(InputAction) — abstracted key lookup
│   ├── Remap(action, KeyCode) — runtime remapping + persistence
│   ├── SetInputMode(InputMode) — Standard / OneHandedLeft / OneHandedRight / Sequential
│   ├── GetGyroInput() — device-tilt pitch+roll for one-handed play
│   ├── ProcessPitch/Roll/Yaw(raw) — dead-zone + curve shaping
│   └── ProcessBoost/Brake(down, held) — hold-vs-toggle logic
│
ScreenReaderBridge (Singleton)
│   ├── Announce(text, SpeechPriority) — priority queue, interrupts lower-priority
│   ├── ReportFocus(label, type, state) — UI focus tracking + earcon
│   ├── AnnounceNavigation(screenName) — screen transition announcements
│   └── Platform stubs: ConsoleTTSEngine / IOSVoiceOverEngine / AndroidTalkBackEngine / WindowsNarratorEngine
│
ColorblindFilter (Singleton)
│   ├── SetMode(ColorblindMode) — updates post-processing shader matrix
│   ├── SetFilterMode(Simulate|Correct) — testing vs. assistance toggle
│   ├── SetIntensity(0–1) — blend between original and corrected colours
│   ├── ResolveColor(name, original) — custom palette + default swap rules
│   └── RecolourUI(root) — recolours all Graphic components under a transform
│
SubtitleSystem (Singleton)
│   ├── ShowSubtitle(entry) — FIFO queue with auto-duration calculation
│   ├── ShowSoundDescription(text) — closed-caption ambient descriptions
│   ├── SetPosition(Top|Center|Bottom) — repositions panel anchors
│   └── Localization integration via SWEF.Localization.LocalizationManager
│
UIScalingSystem (Singleton)
│   ├── SetGlobalScale(0.5–3.0) — applies to all CanvasScaler instances
│   ├── SuggestScaleForDPI() — DPI-aware recommendation
│   ├── SetTextSizeLevel(0–4) — +0/25/50/75/100% text enlargement
│   ├── SetFocus(target) — pulsing Outline focus highlight
│   └── SetReducedMotion → propagates to SWEF.UI.AccessibilityManager
│
HapticAccessibility (Singleton)
│   ├── Play(patternName) — plays registered pattern by name
│   ├── RegisterPattern(HapticPattern) — custom pattern registration
│   ├── OnAudioEvent(name, amplitude) — audio-to-haptic conversion
│   └── 9 built-in patterns in pattern library
│
CognitiveAssistSystem (Singleton)
│   ├── UpdateSimplifiedFlight(altitude, speed) → (throttleDelta, pitchDelta)
│   ├── SetGameSpeed(0.25–1.0) — snaps to allowed values, sets Time.timeScale
│   ├── SetInfoLevel(Full|Reduced|Minimal) — shows/hides HUD element groups
│   ├── TriggerReminder(key, message) — cooldown-gated reminders via ScreenReaderBridge
│   ├── TryForcePause() / ForceResume() — pause-anywhere support
│   └── RecordDeath/Retry → EvaluateAutoDifficulty → OnDifficultyAdjusted
```

### Integration Points

| Phase 34 Script | Integrates With |
|----------------|----------------|
| `AccessibilityManager` | All 7 other Accessibility scripts — broadcasts `OnProfileChanged` |
| `AdaptiveInputManager` | `Flight/FlightController.cs` — wraps flight input |
| `ScreenReaderBridge` | `Audio/` spatial audio (Phase 28); `UI/` canvas elements |
| `ColorblindFilter` | Camera post-processing pipeline; `UI/` Graphic components |
| `SubtitleSystem` | `Localization/LocalizationManager` (Phase 30) — localised subtitle text |
| `UIScalingSystem` | `UI/AccessibilityManager` (Phase 16) — reduced-motion propagation; all `CanvasScaler` instances |
| `HapticAccessibility` | `Haptic/HapticManager` (Phase 18) — extends existing haptic system |
| `CognitiveAssistSystem` | `ScreenReaderBridge` — reminder announcements; `Flight/` flight controller |

### WCAG 2.1 AA Alignment

| Feature | WCAG Criterion |
|---------|----------------|
| Subtitle auto-duration (21 cps) | 1.2.2 Captions (Prerecorded) |
| Background opacity ≥ 60% | 1.4.3 Contrast (Minimum) |
| Focus highlight (thick outline + pulse) | 2.4.7 Focus Visible |
| Reduced motion mode | 2.3.3 Animation from Interactions |
| Large text mode (+25–100%) | 1.4.4 Resize Text |
| Minimum touch target 88 px | 2.5.5 Target Size |
| DPI-aware UI scaling | 1.4.10 Reflow |

---

## Phase 35 — Save System & Cloud Sync (세이브 시스템 & 클라우드 동기화)

### New Scripts

| # | File | Namespace | Summary |
|---|------|-----------|---------|
| 1 | `SaveSystem/SaveData.cs` | `SWEF.SaveSystem` | Core data types: `ISaveable` interface, `CloudSyncStatus` enum, `SaveSlotInfo`, `SaveFileHeader`, `SavePayload`, `PlayerProgressData`, `SaveFile`, `SaveSystemConstants` |
| 2 | `SaveSystem/SaveManager.cs` | `SWEF.SaveSystem` | Central singleton manager: 5 save slots (0–2 manual, 3 auto-save, 4 quicksave), ISaveable auto-discovery, GZip compression, AES-256 encryption, SHA-256 checksum, 5-minute auto-save, scene-transition auto-save, full save/load pipeline with pre/post hooks |
| 3 | `SaveSystem/SaveIntegrityChecker.cs` | `SWEF.SaveSystem` | SHA-256 checksum generation & verification, full-scan of all slots, per-slot corruption quarantine, health-report generator |
| 4 | `SaveSystem/SaveMigrationSystem.cs` | `SWEF.SaveSystem` | Version-based save-format migration: step registry, chained forward-only upgrades, built-in v1→v2 step, custom step registration API |
| 5 | `SaveSystem/CloudSyncManager.cs` | `SWEF.SaveSystem` | REST-API cloud sync: per-slot upload/download, all-slots SyncAll, auto-upload on save, auto-check on start, conflict detection via `SaveConflictResolver`, cloud metadata polling |
| 6 | `SaveSystem/SaveConflictResolver.cs` | `SWEF.SaveSystem` | Conflict detection (timestamp + sync-status), three resolution policies (`UseLocal`, `UseCloud`, `Merge`), pending-blob storage, best-effort merge of divergent payloads |
| 7 | `SaveSystem/SaveExportImport.cs` | `SWEF.SaveSystem` | Portable export envelope (Base64 + SHA-256 checksum, no device encryption), pre-import validation, slot import with metadata rebuild |
| 8 | `SaveSystem/SaveSystemUI.cs` | `SWEF.SaveSystem` | Save-slot panel controller + `SaveSlotCard` helper: slot selection, Save/Load/Delete/Export/Import buttons, conflict-resolution prompt, status messages |

### Key Data Types

| Type | File | Description |
|------|------|-------------|
| `ISaveable` | `SaveData.cs` | `SaveKey / CaptureState() / RestoreState()` — any MonoBehaviour implements this to join the save pipeline |
| `SaveSlotInfo` | `SaveData.cs` | Per-slot sidecar metadata: index, display name, ISO-8601 timestamp, play time, thumbnail path, format version, SHA-256 checksum, creation ticks, `CloudSyncStatus`, `isEmpty` flag |
| `SaveFileHeader` | `SaveData.cs` | `"SWEF"` magic, format version, creation & last-modified ticks, play time, game version, platform |
| `SavePayload` | `SaveData.cs` | Parallel-list key→JSON map (JsonUtility-compatible); `Set / Get / Contains / Count` |
| `PlayerProgressData` | `SaveData.cs` | Flights, flight time, distance, altitude, regions, aircraft, locations, missions, routes, currency, prestige, last position & dates |
| `SaveFile` | `SaveData.cs` | Root serialisable container: `SaveFileHeader` + `SavePayload` + `PlayerProgressData` |
| `CloudSyncStatus` | `SaveData.cs` | `NotConfigured / Synced / LocalAhead / CloudAhead / Conflict / Syncing / Error` |
| `ConflictResolution` | `SaveConflictResolver.cs` | `None / UseLocal / UseCloud / Merge` |

### Architecture

```
SaveManager (Singleton, DontDestroyOnLoad)
│   ├── 5 slots — ISaveable auto-discovery on SceneLoaded
│   ├── Save(slot) / Load(slot) / Delete(slot) / QuickSave() / QuickLoad()
│   ├── Auto-save timer (default 300 s) + OnApplicationPause/Quit
│   ├── SuspendAutoSave() / ResumeAutoSave() — disable during cutscenes
│   ├── Save pipeline: Gather ISaveables → GatherSubsystems → Build SaveFile
│   │   → JsonUtility.ToJson → GZip compress → AES-256 encrypt → Write
│   ├── Load pipeline: Read → SHA-256 verify → AES decrypt → GZip decompress
│   │   → JsonUtility.FromJson → Migrate → DistributeISaveables → DistributeSubsystems
│   └── Events: OnSaveStarted / OnSaveCompleted / OnLoadStarted / OnLoadCompleted
│              OnAutoSaveTriggered / OnSlotDeleted
│
SaveIntegrityChecker (Singleton)
│   ├── ComputeChecksum(byte[]) — SHA-256 static utility
│   ├── VerifySlot(index, info) — compare stored vs actual checksum
│   ├── ScanAllSlots() — fires OnCorruptionDetected per bad slot
│   ├── QuarantineIfCorrupted(index) — deletes corrupt save blob
│   └── GetHealthReport() — human-readable integrity summary
│
SaveMigrationSystem (Singleton)
│   ├── Migrate(SaveFile, from, to) — chains registered steps
│   ├── RegisterStep(fromVersion, Action<SaveFile>) — custom step API
│   └── Built-in: MigrateV1ToV2 — ensures PlayerProgressData exists
│
CloudSyncManager (Singleton)
│   ├── UploadSlot(index) / DownloadSlot(index) / SyncAll()
│   ├── CheckSlot(index) — polls cloud metadata, updates CloudSyncStatus
│   ├── AutoUploadOnSave / AutoCheckOnStart flags
│   └── Events: OnSyncStarted / OnSyncCompleted / OnSyncError / OnConflictDetected
│
SaveConflictResolver (Singleton)
│   ├── DetectConflict(index, cloudBlob) — timestamp + status comparison
│   ├── StoreCloudBlob(index, bytes) — holds pending cloud data
│   ├── ResolveUseLocal / ResolveUseCloud / ResolveMerge
│   └── Events: OnConflictDetected / OnConflictResolved
│
SaveExportImport (Singleton)
│   ├── ExportSlot(index, path?) — writes .swefsave envelope (Base64 + checksum)
│   ├── ValidateExportFile(path) — magic + checksum validation, returns error string
│   ├── ImportToSlot(path, targetSlot) — validates then writes to slot
│   └── Events: OnExportCompleted / OnImportCompleted / OnExportImportError
│
SaveSystemUI (Singleton)
│   ├── OpenSaveMode() / OpenLoadMode() / Toggle() / Close()
│   ├── SelectSlot(index) — highlights card, enables action buttons
│   ├── Save / Load / Delete / Export / Import button handlers
│   ├── Conflict-resolution panel (Use Local / Use Cloud / Merge)
│   └── SaveSlotCard — per-slot display: name, timestamp, play time, sync status
```

### Integration Points

| Phase 35 Script | Integrates With |
|----------------|----------------|
| `SaveManager` | `Achievement/AchievementManager` — captures/restores achievement states |
| `SaveManager` | `Settings/SettingsManager` — persists master/SFX volume |
| `SaveManager` | `Accessibility/AccessibilityManager` — captures/restores full profile |
| `SaveManager` | `Localization/LocalizationManager` — persists active language |
| `SaveManager` | `IAP/IAPManager` — records premium status |
| `SaveManager` | `Core/SaveManager` — syncs flight stats to PlayerProgressData |
| `CloudSyncManager` | `SaveConflictResolver` — delegates conflict detection on download |
| `SaveSystemUI` | `SaveConflictResolver` — shows resolution prompt on `OnConflictDetected` |
| `SaveIntegrityChecker` | `BootManager` — auto-scan on boot, quarantine corrupted slots |

---

## Phase 37 — Guided Tour & Waypoint Navigation System

### New Scripts (8 files in `Assets/SWEF/Scripts/GuidedTour/`)

| # | Script | Namespace | Purpose |
|---|--------|-----------|---------|
| 1 | `TourData.cs` | `SWEF.GuidedTour` | ScriptableObject — defines a tour with ordered `WaypointData` list, difficulty, estimated duration, and localization key |
| 2 | `TourManager.cs` | `SWEF.GuidedTour` | Singleton MonoBehaviour — manages tour lifecycle (start/pause/resume/cancel/complete), coroutine-driven auto-advance, events |
| 3 | `WaypointNavigator.cs` | `SWEF.GuidedTour` | Navigation assistance and optional auto-pilot via `FlightController.Step()`; calculates bearing & distance to next waypoint |
| 4 | `WaypointHUD.cs` | `SWEF.GuidedTour` | HUD overlay — on-screen waypoint markers, distance labels, off-screen direction arrows, progress bar, waypoint counter |
| 5 | `TourNarrationController.cs` | `SWEF.GuidedTour` | Queue-based audio + subtitle narration with `LocalizationManager` integration; skip/volume controls |
| 6 | `TourCatalogUI.cs` | `SWEF.GuidedTour` | Scrollable tour list with difficulty/status/region filters, search bar, per-entry start button wired to `TourManager.StartTour()` |
| 7 | `TourProgressTracker.cs` | `SWEF.GuidedTour` | Singleton — JSON-persisted completion data, 1–3 star rating, `AchievementManager` integration on milestones |
| 8 | `TourMinimapOverlay.cs` | `SWEF.GuidedTour` | `LineRenderer`-based minimap path; visited (green) vs. remaining (white) segments; player marker; toggle visibility |

### Key Data Types

| Type | File | Description |
|------|------|-------------|
| `TourDifficulty` | `TourData.cs` | Enum: `Easy / Medium / Hard` |
| `TourData.WaypointData` | `TourData.cs` | Struct: `position`, `lookAtTarget`, `waypointName`, `narrationKey`, `stayDurationSeconds`, `triggerRadius`, optional `cameraAngleOverride` |
| `TourProgressTracker.TourResult` | `TourProgressTracker.cs` | Struct: `completionTime`, `waypointsVisited`, `starsEarned`, `completedDate` (ISO-8601) |

### Architecture

```
TourManager (Singleton, coroutine-driven)
│   ├── StartTour(TourData) / PauseTour() / ResumeTour() / CancelTour() / SkipToWaypoint(int)
│   ├── Coroutine polls WaypointNavigator.DistanceToNextWaypoint vs triggerRadius
│   ├── Dwells for stayDurationSeconds then auto-advances
│   └── Events: OnTourStarted / OnWaypointReached / OnTourCompleted / OnTourCancelled
│
WaypointNavigator  →  FlightController.Step(yaw, pitch, 0) for autopilot steering
│   ├── DistanceToNextWaypoint / BearingToNextWaypoint (read-only properties)
│   ├── EnableAutoPilot() / DisableAutoPilot() / SetAutoPilotSpeed(float)
│   └── Subscribes to TourManager.OnWaypointReached to advance target position
│
WaypointHUD  →  Camera.WorldToScreenPoint per waypoint
│   ├── Spawns marker prefabs into a Canvas RectTransform container
│   ├── Clamps off-screen waypoints to a direction arrow
│   └── Drives Slider progress bar and "N/M" counter Text
│
TourNarrationController  →  LocalizationManager.Instance.GetText(narrationKey)
│   ├── Queue<NarrationRequest> — never overlaps AudioSource playback
│   ├── PlayNarration(key, clip) / SkipNarration() / SetNarrationVolume(float)
│   └── Fades subtitle CanvasGroup out when queue empties
│
TourCatalogUI
│   ├── Instantiates tourEntryPrefab per matching TourData in contentRoot
│   ├── Filters: difficulty dropdown / status dropdown / region dropdown / search InputField
│   └── Each entry's Button calls TourManager.StartTour() then hides catalog
│
TourProgressTracker (Singleton)
│   ├── Persists to persistentDataPath/tour_progress.json (JsonUtility)
│   ├── GetTourProgress(tourId) / SaveTourResult(tourId, result) / GetCompletedTourCount()
│   └── TriggerAchievements → AchievementManager.TryUnlock / ReportProgress
│
TourMinimapOverlay
│   ├── pathLineRenderer (all waypoints) + visitedLineRenderer (visited segment)
│   ├── Colours: remaining = white, visited = green, player marker = yellow
│   └── Show() / Hide() / Toggle()
```

### Integration Points

| Phase 37 Script | Integrates With |
|----------------|----------------|
| `WaypointNavigator` | `SWEF.Flight.FlightController` — calls `SetThrottle` + `Step()` for autopilot |
| `TourNarrationController` | `SWEF.Localization.LocalizationManager` — `GetText(narrationKey)` for subtitles |
| `TourProgressTracker` | `SWEF.Achievement.AchievementManager` — `TryUnlock` + `ReportProgress` on milestones |
| `TourManager` | `WaypointNavigator` — polls `DistanceToNextWaypoint` in trigger-check loop |
| `TourCatalogUI` | `TourProgressTracker` — reads completion/star status per entry |
| `TourMinimapOverlay` | `SWEF.Flight.FlightController` — reads player transform for marker position |


---

## Phase 38 — Dynamic Event System & World Events

### New Scripts (8 files in `Assets/SWEF/Scripts/Events/`)

| # | File | Namespace | Role |
|---|------|-----------|------|
| 1 | `WorldEventData.cs` | `SWEF.Events` | ScriptableObject — event template with type, duration, spawn region, probability, rewards, recurrence, seasonal constraint |
| 2 | `WorldEventInstance.cs` | `SWEF.Events` | Plain C# class — live runtime instance; state machine (`Pending → Active → Expiring → Ended`); `RemainingTime`, `Progress01`, `IsActive` |
| 3 | `EventScheduler.cs` | `SWEF.Events` | Singleton MonoBehaviour — loads `Resources/Events/`, coroutine-based evaluation loop, considers probability / cooldown / season / weather; `ForceSpawnEvent`, `GetActiveEvents`, `GetUpcomingEvents` |
| 4 | `EventParticipationTracker.cs` | `SWEF.Events` | MonoBehaviour — distance-based participation detection, tracks time in region, completion threshold, JSON persistence, `AchievementManager` grant on completion |
| 5 | `EventVisualController.cs` | `SWEF.Events` | MonoBehaviour — spawns prefabs from Resources, scale-in coroutine, particle management, fade-out on expiry; `SpawnVisual`, `DespawnVisual`, `SetVisualIntensity` |
| 6 | `EventNotificationUI.cs` | `SWEF.Events` | MonoBehaviour — slide-in toast (name, distance, countdown, Navigate button), persistent HUD widget with countdown slider and participation progress |
| 7 | `EventCalendarUI.cs` | `SWEF.Events` | MonoBehaviour — full-screen calendar with `Active Now / Upcoming / History` tabs, per-entry Navigate action via `WaypointNavigator.SetManualTarget` |
| 8 | `EventRewardController.cs` | `SWEF.Events` | MonoBehaviour — `GrantRewards` + `ShowRewardPopup`; slide-up card with per-reward rows; `AchievementManager` unlock for achievement rewards |

### Updated Scripts
| File | Change |
|------|--------|
| `GuidedTour/WaypointNavigator.cs` | Added `SetManualTarget(Vector3)` public method for event-based navigation |

### Architecture Overview

```
EventScheduler (Singleton, DontDestroyOnLoad)
│   ├── Loads WorldEventData[] from Resources/Events/
│   ├── Coroutine: EvaluateSpawns every evaluationIntervalSeconds
│   │       ├── Checks: season, cooldown, concurrent cap, time-of-day, weather (null-safe)
│   │       └── SpawnEvent() → WorldEventInstance (Pending → Activate())
│   ├── TickActiveEvents(): Expire() when RemainingTime ≤ 0, clean Ended instances
│   └── Events: OnEventSpawned / OnEventExpired
│
EventParticipationTracker
│   ├── Subscribes to EventScheduler.OnEventSpawned / OnEventExpired
│   ├── Update(): distance check → builds EventParticipation records
│   ├── Completion threshold → GrantRewardsForEvent → AchievementManager + EventRewardController
│   └── JSON persistence to Application.persistentDataPath/event_participation.json
│
EventVisualController
│   ├── Subscribes to EventScheduler.OnEventSpawned / OnEventExpired
│   ├── SpawnVisual → Instantiate prefab → ScaleIn coroutine → particle Play
│   └── DespawnVisual → FadeOutAndDestroy coroutine
│
EventNotificationUI
│   ├── ShowEventNotification → slide-in toast → Navigate button → WaypointNavigator.SetManualTarget
│   └── Update(): HUD widget countdown + participation progress
│
EventCalendarUI
│   ├── Tabs: Active Now (EventScheduler.GetActiveEvents), Upcoming (GetUpcomingEvents), History (tracker)
│   └── Navigate entry → WaypointNavigator.SetManualTarget + EnableAutoPilot
│
EventRewardController
│   ├── GrantRewards(instance, participation) → AchievementManager.TryUnlock
│   └── ShowRewardPopup(rewards) → slide-up animated card
```

### Integration Points

| Phase 38 Script | Integrates With |
|----------------|----------------|
| `EventScheduler` | `SWEF.Weather.WeatherManager` — weather-gated aurora / rare-weather spawning (null-safe) |
| `EventParticipationTracker` | `SWEF.Achievement.AchievementManager` — `TryUnlock(achievementId)` on completion |
| `EventParticipationTracker` | `SWEF.Flight.FlightController` — player transform for distance checks |
| `EventNotificationUI` | `SWEF.GuidedTour.WaypointNavigator` — `SetManualTarget` + `EnableAutoPilot` on Navigate |
| `EventCalendarUI` | `SWEF.GuidedTour.WaypointNavigator` — same Navigate-to-event flow |
| `EventRewardController` | `SWEF.Achievement.AchievementManager` — `TryUnlock` for achievement rewards |
