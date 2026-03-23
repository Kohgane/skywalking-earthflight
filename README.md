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
├── Scenes/               # Boot.unity + World.unity (created in Unity Editor)
├── Scripts/
│   ├── Accessibility/    # AccessibilityManager, AdaptiveInputManager, ColorblindFilter, SubtitleSystem, UIScalingSystem, HapticAccessibility, CognitiveAssistSystem, ScreenReaderBridge
│   ├── Achievement/      # AchievementDefinition, AchievementState, AchievementManager, AchievementTracker, AchievementNotificationUI, AchievementPanelUI, AchievementCardUI, AchievementShareController, MilestoneDefinition, MilestoneTracker, AchievementData, AchievementUI
│   ├── AchievementNotification/ # AchievementNotificationData, NotificationQueueManager, ToastNotificationController, UnlockAnimationController, RewardDisplayManager, AchievementPopupUI, NotificationSoundController, AchievementNotificationAnalytics
│   ├── Aircraft/         # AircraftData, AircraftSkinRegistry, AircraftCustomizationManager, AircraftUnlockEvaluator, AircraftVisualController, AircraftTrailController, AircraftHangarUI, AircraftSkinCardUI, AircraftPreviewController, AircraftMultiplayerSync, AircraftAchievementBridge, AircraftSettingsBridge
│   ├── Analytics/        # TelemetryEvent, TelemetryDispatcher, FlightTelemetryCollector, PerformanceTelemetryCollector, UserBehaviorTracker, ABTestManager, PrivacyConsentManager, AnalyticsDashboardData
│   ├── Atmosphere/       # AtmosphereController, CloudLayer, DayNightCycle, WeatherController, WindController, ComfortVignette, ReentryEffect
│   ├── Audio/            # AudioManager, AudioMixerController, AudioEventTrigger, AltitudeAudioTrigger, AltitudeSoundscapeController, WindAudioGenerator, DopplerEffectController, SonicBoomController, EnvironmentReverbController, AudioOcclusionSystem, SpatialAudioManager, MusicLayerSystem, AudioVisualizerData
│   ├── Biome/            # BiomeData, BiomeClassifier, BiomeVisualEffects, BiomeAudioManager, VegetationPlacementHints, TerrainTextureBlender, BiomeTransitionZone, BiomeAnalytics
│   ├── Cinema/           # TimeOfDayController, PhotoModeController, CinematicCameraPath, CinematicCameraUI
│   ├── CockpitHUD/       # HUDDashboard, HUDInstrument, FlightData, FlightDataProvider, Altimeter, Speedometer, CompassHeading, AttitudeIndicator, VerticalSpeedIndicator, GForceIndicator, ThrottleFuelGauge, WarningSystem, CockpitHUDConfig
│   ├── CloudRendering/   # CloudRenderingManager, CloudSessionManager, StreamingClient, FrameDecoder, InputStreamEncoder, LatencyCompensator, NetworkQualityMonitor, AdaptiveBitrateController, HybridRenderingController, ServerDiscoveryService, CloudRenderingUI
│   ├── Core/             # BootManager, SWEFSession, WorldBootstrap, AppLifecycleManager, SaveManager, AutoSaveController, CloudSaveController, DataMigrator, PerformanceManager, MemoryManager, QualityPresetManager, LoadingScreen, PauseManager, ErrorHandler, CrashReporter, AnalyticsLogger, AdManager, PremiumFeatureGate, SessionTracker, DeepLinkHandler, DebugConsole, DebugGizmoDrawer, FlightJournal, RatePromptManager, RatePromptUI
│   ├── DailyChallenge/   # DailyChallengeDefinition, DailyChallengeDefaultData, DailyChallengeManager, DailyChallengeTracker, DailyChallengeHUD, ChallengeNotificationUI, ChallengeRewardController, WeeklyChallengeDefinition, WeeklyChallengeManager, SeasonDefinition, SeasonPassManager, SeasonPassUI
│   ├── DebugOverlay/     # DebugOverlayData, FPSCounter, MemoryProfiler, DrawCallMonitor, DebugOverlayController, PerformanceLogger, DebugConsole, DebugOverlayAnalytics
│   ├── Editor/           # SWEFEditorWindow, SWEFBuildPreprocessor, SWEFSceneValidator, AchievementEditorWindow, LocalizationEditorWindow, AnalyticsDebugWindow, CloudRenderingDebugWindow, MultiplayerDebugWindow, PerformanceProfilerWindow, SpatialAudioDebugWindow, TerrainDebugWindow, WeatherDebugWindow
│   ├── Events/           # WorldEventData, WorldEventInstance, EventScheduler, EventParticipationTracker, EventVisualController, EventNotificationUI, EventCalendarUI, EventRewardController
│   ├── Favorites/        # FavoriteManager, FavoritesUI
│   ├── Flight/           # FlightController, AltitudeController, TouchInputRouter, HoldButton, AeroPhysicsModel, AeroState, FlightPhysicsIntegrator, FlightPhysicsSnapshot, OrbitalMechanics, OrbitState, JetTrail, CameraController, StallWarningSystem
│   ├── FlightSchool/     # FlightSchoolData, FlightSchoolManager, FlightInstructor, FlightSchoolUI, FlightSchoolAnalyticsBridge
│   ├── GuidedTour/       # TourData, TourManager, WaypointNavigator, WaypointHUD, TourNarrationController, TourCatalogUI, TourProgressTracker, TourMinimapOverlay
│   ├── Haptic/           # HapticManager, HapticPattern, HapticTriggerZone
│   ├── HiddenGems/       # HiddenGemData, HiddenGemDatabase, HiddenGemManager, GemDiscoveryUI, GemCollectionUI, GemRadarUI, GemMinimapIntegration, GemTourGenerator, GemStatisticsTracker, Editor/HiddenGemEditorWindow
│   ├── IAP/              # IAPManager, IAPProductCatalog, IAPRestoreButton
│   ├── Journal/          # JournalData, JournalManager, JournalAutoRecorder, JournalPanelUI, JournalDetailUI, JournalStatisticsUI, JournalShareController, JournalSearchEngine, JournalTagManager, JournalComparisonUI
│   ├── LOD/              # LODManager, LODTransitionBlender, OcclusionCullingHelper
│   ├── Leaderboard/      # GlobalLeaderboardService, GlobalLeaderboardEntry, LeaderboardUI, LeaderboardEntryUI, LeaderboardCategory, LeaderboardTimeFilter, WeeklyChallengeManager, WeeklyChallengeUI
│   ├── Localization/     # LocalizationManager, LanguageDatabase, LocalizationUI, LocalizedText, LocalizedImage, FontManager, PluralResolver, RTLTextHandler
│   ├── Minimap/          # MinimapData, MinimapManager, MinimapRenderer, MinimapIconConfig, MinimapBlipProvider, MinimapCompass, MinimapSettingsUI, RadarOverlay
│   ├── Multiplayer/      # MultiplayerManager, NetworkManager2, PlayerSyncController, PlayerSyncSystem, FormationFlyingManager, CoopMissionSystem, MultiplayerWeatherSync, MultiplayerHUD, MultiplayerScoreboard, MultiplayerRace, RoomManager, PlayerAvatar, RemotePlayerRenderer, NetworkTransport, VoiceChatManager, ProximityChat
│   ├── MusicPlayer/      # MusicPlayerData, MusicPlayerManager, MusicPlaylistController, MusicPlayerUI, MusicLibraryUI, MusicFlightSync, MusicWeatherMixer, MusicVisualizerEffect, MusicMultiplayerSync, MusicEQController, MusicCrossfadeController, MusicSleepTimer, LrcParser, LyricsDatabase, KaraokeController, LyricsDisplayUI, LyricsEditorUI
│   ├── Narration/        # NarrationData, LandmarkDatabase, NarrationManager, NarrationAudioController, NarrationSubtitleUI, NarrationHudPanel, LandmarkDiscoveryTracker, LandmarkMinimapIntegration, NarrationSettingsUI, NarrationAnalytics, Editor/LandmarkDatabaseEditorWindow
│   ├── Notification/     # NotificationManager, NotificationSettings
│   ├── Offline/          # OfflineManager, OfflineFallbackController, OfflineHUD, TileCacheManager, TilePrefetchController, RegionDownloadUI
│   ├── OrbitalCamera/    # OrbitalCameraData, OrbitalCameraController, SpaceGroundTransition, OrbitalMechanicsSimulator, SatelliteViewRenderer, AltitudeEffectsManager, OrbitPathVisualizer, OrbitalCameraAnalytics
│   ├── Performance/      # PerformanceProfiler, AdaptiveQualityController, RuntimeDiagnosticsHUD, MemoryPoolManager, GarbageCollectionTracker, DrawCallAnalyzer, AssetLoadProfiler, SceneLoadProfiler, TextureMemoryOptimizer
│   ├── PhotoMode/        # PhotoModeData, PhotoModeUI, PhotoModeAnalytics, PhotoCameraController, PhotoCaptureManager, PhotoFilterSystem, PhotoFrameRenderer, PhotoGalleryManager, DroneCameraController, DroneVisualController
│   ├── Progression/      # PilotRankData, ProgressionManager, XPSourceConfig, XPTracker, SkillTreeData, SkillTreeManager, CosmeticUnlockManager, ProgressionHUD, ProgressionProfileUI, ProgressionDefaultData
│   ├── Radar/            # RadarEnums, RadarContact, RadarSystem, IFFTransponder, ThreatDetector, RadarDisplay, MissileWarningReceiver, RadarJammer, RadarConfig
│   ├── Racing/           # RacingData, BoostController, DriftController, BoostPadManager, SlipstreamController, StartBoostController, TrickBoostController, BoostVFXBridge, BoostAudioController, RacingAnalytics
│   ├── Recorder/         # FlightRecorder, FlightPlayback, RecorderUI
│   ├── Replay/           # ReplayData, ReplayFileManager, GhostRacer, FlightPathRenderer, ReplayShareManager
│   ├── ReplayTheater/    # ReplayTheaterManager, ReplayTheaterSettings, ReplayTheaterUI, ReplayTimeline, TimelineTrack, CameraKeyframe, CinematicCameraEditor, ReplayImporter, ReplayExporter, ReplayThumbnailGenerator
│   ├── RoutePlanner/     # RoutePlannerData, RoutePlannerManager, RouteBuilderController, RoutePathRenderer, RouteNavigationHUD, RouteStorageManager, RouteShareManager, RoutePlannerUI, RouteRecommendationEngine, RoutePlannerAnalytics
│   ├── SaveSystem/       # SaveData, SaveManager, SaveIntegrityChecker, SaveMigrationSystem, CloudSyncManager, SaveConflictResolver, SaveExportImport, SaveSystemUI
│   ├── Screenshot/       # ScreenshotController, ScreenshotUI
│   ├── Settings/         # SettingsManager, SettingsUI, AccessibilitySettingsUI, AnalyticsSettings, MultiplayerSettings, WeatherSettings, XRSettingsUI
│   ├── Social/           # SocialFeedManager, SocialFeedUI, SocialPost, SocialPostCard, PostComposerUI, ShareManager, SocialShareController, SocialNotificationHandler, PlayerProfileManager, PlayerProfileUI, LeaderboardManager, CommunityProfileManager, RegionHelper
│   ├── SocialHub/        # SocialHubController, FriendManager, FriendListUI, PlayerProfile, PlayerProfileManager, PlayerSearchUI, ProfileCardUI, ProfileCustomizationUI, ActivityFeedUI, SocialActivityFeed, SocialNotificationSystem
│   ├── Teleport/         # TeleportController, TeleportUI
│   ├── Terrain/          # CesiumTerrainBridge, ProceduralTerrainGenerator, TerrainChunk, TerrainChunkPool, TerrainBiomeMapper, TerrainTextureManager
│   ├── TimeCapsule/      # TimeCapsuleData, TimeCapsuleManager, TimeCapsuleAutoCapture, TimeCapsuleUI, TimeCapsuleMapOverlay, TimeCapsuleNotificationService
│   ├── TimeOfDay/        # TimeOfDayData, SolarCalculator, TimeOfDayManager, LightingController, SeasonalLightingProfile, GoldenHourEffect, NightSkyRenderer, TimeOfDayMultiplayerSync, TimeOfDayUI, TimeOfDayAnalytics
│   ├── Tutorial/         # TutorialManager, TutorialStepData, TutorialActionDetector, TutorialHighlight, TutorialTooltip, TutorialReplayButton, InteractiveTutorialManager
│   ├── UI/               # HudBinder, AccessibilityController, OneHandedModeController, VoiceCommandManager, HudBinder, FlightPhysicsHUD, CompassHUD, SpeedIndicator, AltitudeMilestone, WeatherHUD, WeatherUI, MiniMap, MiniMapController, MultiplayerHUD, GhostRaceHUD, SplashScreen, LoadingScreen (see Core), StatsDashboard, LeaderboardUI, FlightJournalUI, CameraUI, InputRebinder, LocalizationManager, PhotoModeUI, ReplayBrowserUI, StoreUI, TimeOfDayUI, PremiumPromptUI, VoiceCommand, ColorblindMode, AccessibilityManager
│   ├── Util/             # ExpSmoothing, PerformanceProfiler, SWEFTestHelpers
│   ├── VFX/              # VFXData, VFXPoolManager, VFXTriggerSystem, VFXLODController, EngineExhaustController, EnvironmentVFXController, SpeedVFXController, CelebrationVFXController, VFXCompositor, VFXAnalytics
│   ├── VoiceChat/        # VoiceChatData, VoiceChatManager, VoiceAudioProcessor, VoiceSpatialAudio, VoiceNetworkTransport, VoiceChannelManager, VoiceChatUI, VoiceChatSettings, VoiceRadioEffect, VoiceChatAnalytics
│   ├── Water/            # WaterData, WaterSurfaceManager, BuoyancyController, SplashEffectController, UnderwaterCameraTransition, WaterRippleSystem, WaterInteractionAnalytics
│   ├── Weather/          # WeatherData, WeatherCondition, WeatherManager, WeatherAPIClient, WeatherDataService, WeatherStateManager, WeatherLightingController, WeatherSkyboxController, WeatherFogController, WeatherVFXController, WeatherAudioController, WeatherSoundController, WeatherFlightModifier, PrecipitationSystem, WindSystem, WeatherUI
│   ├── WeatherChallenge/ # WeatherChallengeData, WeatherChallengeManager, DynamicRouteGenerator, WeatherChallengeUI, RouteVisualizationController, WeatherChallengeAnalyticsBridge
│   ├── Wildlife/         # WildlifeData, WildlifeManager, AnimalGroupController, BirdFlockController, MarineLifeController, AnimalAnimationController, WildlifeSpawnSystem, WildlifeAudioController, WildlifeJournalIntegration, WildlifeDebugOverlay
│   ├── WorldEvent/       # WorldEventType, WorldEventData, RewardData, WorldEventManager, ActiveWorldEvent, EventObjective, EventSpawnZone, QuestChain, EventNotificationUI, WorldEventConfig
│   └── XR/              # XRPlatformDetector, XRRigManager, XRInputAdapter, XRHandTracker, XRComfortSettings, XRUIAdapter
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


---

## Phase 39 — Player Progression & Pilot Rank System

### New Scripts (10 files in `Assets/SWEF/Scripts/Progression/`)

| # | File | Namespace | Role |
|---|------|-----------|------|
| 1 | `PilotRankData.cs` | `SWEF.Progression` | ScriptableObject — defines a single rank (rankLevel 1–50, requiredXP, tier, icon, colour, unlock rewards) |
| 2 | `ProgressionManager.cs` | `SWEF.Progression` | Singleton — central XP/rank tracker; `AddXP`, `GetCurrentRank`, `GetNextRank`, `GetProgressToNextRank01`, `UpdateFlightStats`; JSON persistence |
| 3 | `XPSourceConfig.cs` | `SWEF.Progression` | ScriptableObject — all XP reward amounts and multipliers (flight, achievements, events, tours, multiplayer, bonuses) |
| 4 | `XPTracker.cs` | `SWEF.Progression` | MonoBehaviour — auto-tracks per-frame flight/distance/formation XP; subscribes to AchievementManager, EventScheduler, TourManager; first-flight-of-day bonus |
| 5 | `SkillTreeData.cs` | `SWEF.Progression` | ScriptableObject — single skill node (category, tier, cost, prerequisites, effect type & value) |
| 6 | `SkillTreeManager.cs` | `SWEF.Progression` | Singleton — skill point allocation, prerequisite checks, cumulative effect lookup, reset; JSON persistence |
| 7 | `CosmeticUnlockManager.cs` | `SWEF.Progression` | Singleton — cosmetic catalog, rank-gated auto-unlock, equip-per-category, JSON persistence |
| 8 | `ProgressionHUD.cs` | `SWEF.Progression` | Always-visible HUD — animated XP bar, rank badge, level number, floating "+XP" popups, rank-up celebration animation |
| 9 | `ProgressionProfileUI.cs` | `SWEF.Progression` | Full-screen profile — rank card, flight stats, skill tree grid (tap-to-unlock), cosmetics gallery (tap-to-equip), XP history log |
| 10 | `ProgressionDefaultData.cs` | `SWEF.Progression` | Static helper — 50 ranks (exponential XP curve), 25 skills (5 categories × 5 tiers), default cosmetics, default XP config |

### Architecture

```
ProgressionManager (Singleton, DontDestroyOnLoad)
│   ├── Loads PilotRankData[] from Resources/Ranks/ (falls back to ProgressionDefaultData)
│   ├── Persists progression.json → persistentDataPath
│   ├── AddXP(amount, source) → CheckRankUps() → OnRankUp event
│   └── Events: OnXPGained / OnRankUp / OnStatsUpdated
│
XPTracker
│   ├── TrackFlightFrame(dt, km, inFormation)  — per-frame XP
│   ├── Subscribes → AchievementManager.OnAchievementUnlocked
│   ├── Subscribes → EventScheduler.OnEventExpired
│   ├── Subscribes → TourManager.OnTourCompleted
│   └── PlayerPrefs date key for first-flight-of-day bonus
│
SkillTreeManager (Singleton, DontDestroyOnLoad)
│   ├── Subscribes → ProgressionManager.OnRankUp → grants 1+ skill points
│   ├── UnlockSkill(id) — checks points & prerequisites
│   ├── GetSkillEffect(type) — cumulative % bonus across all unlocked skills
│   └── Persists skills.json
│
CosmeticUnlockManager (Singleton, DontDestroyOnLoad)
│   ├── Subscribes → ProgressionManager.OnRankUp → auto-unlocks rank-gated cosmetics
│   ├── EquipCosmetic(id, category) — one slot per category
│   └── Persists cosmetics.json
│
ProgressionHUD
│   ├── Subscribes → ProgressionManager.OnXPGained → floating popup + animated bar fill
│   └── Subscribes → ProgressionManager.OnRankUp → full-screen flash + badge celebration
│
ProgressionProfileUI
│   ├── RefreshAll() — rank card, stats, skill tree, cosmetics gallery, XP history
│   └── Open() / Close()
```

### XP Data Flow

```
Activities
│   ├── Flight time         ──→ XPTracker.TrackFlightFrame()
│   ├── Distance flown      ──→ XPTracker.TrackFlightFrame()
│   ├── Formation flight    ──→ XPTracker.TrackFlightFrame(inFormation=true)
│   ├── Achievement unlock  ──→ AchievementManager.OnAchievementUnlocked
│   ├── Event completion    ──→ EventParticipationTracker (direct AddXP)
│   ├── Tour completed      ──→ TourManager.OnTourCompleted
│   ├── Multiplayer session ──→ XPTracker.TrackMultiplayerSessionEnded()
│   ├── Photo taken         ──→ XPTracker.TrackPhotoTaken()
│   └── Replay shared       ──→ XPTracker.TrackReplayShared()
│
└──→ ProgressionManager.AddXP(amount, source)
         ├── Accumulates currentXP
         ├── Appends to XP history (capped at 200 entries)
         ├── Fires OnXPGained(amount, source)
         ├── CheckRankUps() → if currentXP ≥ nextRank.requiredXP
         │       ├── Fires OnRankUp(oldRank, newRank)
         │       │       ├── SkillTreeManager → grants skill points
         │       │       ├── CosmeticUnlockManager → unlocks rank cosmetics
         │       │       └── ProgressionHUD → plays rank-up celebration
         └── Saves progression.json
```

### Rank Tiers & XP Curve

| Tier | Levels | XP Formula | Colour |
|------|--------|-----------|--------|
| Trainee | 1–5 | `500 × level^1.5` | Grey |
| Cadet | 6–12 | `500 × level^1.5` | Blue |
| Pilot | 13–20 | `500 × level^1.5` | Green |
| Captain | 21–28 | `500 × level^1.5` | Gold |
| Commander | 29–36 | `500 × level^1.5` | Orange |
| Ace | 37–42 | `500 × level^1.5` | Red |
| Legend | 43–48 | `500 × level^1.5` | Purple |
| Skywalker | 49–50 | `500 × level^1.5` | Cyan |

### Skill Tree (25 nodes, 5 categories × 5 tiers)

| Category | Effect | Tiers |
|----------|--------|-------|
| FlightHandling | SpeedBoost (+5%/tier) | 1–5 |
| Exploration | EventRadius (+5%/tier) | 1–5 |
| Social | FormationBonus (+5%/tier) | 1–5 |
| Photography | CameraRange (+5%/tier) | 1–5 |
| Endurance | StaminaBoost (+5%/tier) | 1–5 |

### Integration Points

| Phase 39 Script | Integrates With |
|----------------|----------------|
| `XPTracker` | `SWEF.Achievement.AchievementManager` — `OnAchievementUnlocked` event |
| `XPTracker` | `SWEF.Events.EventScheduler` — `OnEventExpired` event |
| `XPTracker` | `SWEF.GuidedTour.TourManager` — `OnTourCompleted` event |
| `SkillTreeManager` | `SWEF.Progression.ProgressionManager` — `OnRankUp` for skill point grants |
| `CosmeticUnlockManager` | `SWEF.Progression.ProgressionManager` — `OnRankUp` for auto-unlock |
| `ProgressionHUD` | `SWEF.Progression.ProgressionManager` — `OnXPGained` / `OnRankUp` events |
| `ProgressionProfileUI` | All three manager singletons for data display |

## Phase 40 — Daily Challenge & Season Pass System

New directory: `Assets/SWEF/Scripts/DailyChallenge/` — 12 scripts, namespace `SWEF.DailyChallenge`.

### Scripts

| # | Script | Namespace | Description |
|---|--------|-----------|-------------|
| 1 | `DailyChallengeDefinition.cs` | `SWEF.DailyChallenge` | ScriptableObject — challenge template (10 types, 4 difficulty tiers) |
| 2 | `DailyChallengeManager.cs` | `SWEF.DailyChallenge` | Singleton — selects 3+1 challenges per UTC day using deterministic seed |
| 3 | `DailyChallengeTracker.cs` | `SWEF.DailyChallenge` | Auto-tracker — per-frame flight metrics + event-based activity tracking |
| 4 | `SeasonDefinition.cs` | `SWEF.DailyChallenge` | ScriptableObject — season pass definition (50 tiers, free + premium) |
| 5 | `SeasonPassManager.cs` | `SWEF.DailyChallenge` | Singleton — season points, tier advancement, reward claiming |
| 6 | `WeeklyChallengeDefinition.cs` | `SWEF.DailyChallenge` | ScriptableObject — weekly mega-challenge template |
| 7 | `WeeklyChallengeManager.cs` | `SWEF.DailyChallenge` | Singleton — weekly challenges reset every Monday UTC 00:00 |
| 8 | `ChallengeRewardController.cs` | `SWEF.DailyChallenge` | Reward distributor — XP, Sky Coins, season points, cosmetics, skill points |
| 9 | `DailyChallengeHUD.cs` | `SWEF.DailyChallenge` | Always-visible HUD — challenge cards, progress bars, streak flame, reset timer |
| 10 | `SeasonPassUI.cs` | `SWEF.DailyChallenge` | Full-screen season pass — tier track, reward preview, premium upsell |
| 11 | `ChallengeNotificationUI.cs` | `SWEF.DailyChallenge` | Toast notifications — completions, streak milestones, tier-ups, weekly alerts |
| 12 | `DailyChallengeDefaultData.cs` | `SWEF.DailyChallenge` | Static helper — 30+ daily defs, 10 weekly defs, Season 1 ("Sky Pioneer") |

### Daily Challenge Architecture

```
UTC midnight → DailyChallengeManager.RefreshIfNewDay()
                  │  seed = Year*10000 + Month*100 + Day
                  └─ selects: 1 Easy + 1 Medium + 1 Hard + 1 Elite (bonus)

Player activity → DailyChallengeTracker.Update()
                      ├── FlyDistance   (position delta, km)
                      ├── ReachAltitude (max altitude, m)
                      ├── FlyDuration   (seconds)
                      ├── AchieveSpeed  (km/h)
                      ├── TakePhotos    (ScreenshotController.OnScreenshotCaptured)
                      ├── CompleteTour  (TourManager.OnTourCompleted)
                      ├── CompleteFormation (FormationFlyingManager.OnFormationBroken)
                      └── PlayMultiplayer   (NetworkManager2.OnLobbyJoined)
                               │
                               └─▶ DailyChallengeManager.ReportProgress(type, amount)
                                       │
                                       └─▶ ActiveChallenge.currentProgress += amount
                                               │ if >= targetValue
                                               └─▶ OnChallengeCompleted event

Player claims → DailyChallengeManager.ClaimReward(id)
                    └─▶ ChallengeRewardController.GrantDailyChallengeReward(def, streak)
                              ├── ProgressionManager.AddXP(xp × streakMultiplier)
                              ├── ChallengeRewardController.AddCurrency(coins)
                              └── SeasonPassManager.AddSeasonPoints(sp)
```

### Season Pass Structure

```
SeasonPassManager
  ├── Active season loaded from Resources/Seasons/ (fallback: DailyChallengeDefaultData)
  ├── currentSeasonPoints → currentTier = points / pointsPerTier
  ├── Free track  — XP + currency every tier, cosmetics every 10 tiers
  └── Premium track — higher XP, currency every 3rd tier, skill points every 7th,
                      exclusive cosmetics every 10th, exclusive titles at T25 & T50
```

### Streak Bonus Mechanics

| Consecutive Days | XP Multiplier |
|-----------------|---------------|
| 1 | ×1.1 (+10%) |
| 2 | ×1.2 (+20%) |
| 5 | ×1.5 (+50%) |
| 10+ | ×2.0 (+100%, cap) |

Streak resets if a UTC day passes with no challenge completion.

### Virtual Currency (Sky Coins)

- Stored in `Application.persistentDataPath/currency.json`
- Granted by daily challenges (`baseCurrencyReward`) and season pass tiers
- `ChallengeRewardController.GetCurrencyBalance()`, `AddCurrency(int)`, `SpendCurrency(int)` → bool
- Display name localised as `currency_name` (e.g. "Sky Coins" / "스카이 코인")

### Reward Flow

```
Challenge completion
        │
        ▼
ChallengeRewardController
        ├──▶ ProgressionManager.AddXP(amount, source)       [XP & rank]
        ├──▶ SeasonPassManager.AddSeasonPoints(sp, source)  [season tier]
        ├──▶ CosmeticUnlockManager.UnlockCosmetic(id)       [exclusive cosmetics]
        ├──▶ SkillTreeManager.AddSkillPoint(count)          [skill points]
        └──▶ currency balance += amount                     [Sky Coins]
```

### Season 1 — "Sky Pioneer"

- **Duration**: 2026-01-01 → 2026-12-31 (UTC)
- **50 tiers** × 100 season points per tier
- **Free track**: XP every tier, currency every 5th, cosmetics every 10th
- **Premium track**: XP (2×), currency every 3rd tier, skill point every 7th,
  exclusive cosmetics every 10th, exclusive titles at tiers 25 & 50

### Integration Points

| Phase 40 Script | Integrates With |
|----------------|----------------|
| `DailyChallengeTracker` | `SWEF.Flight.FlightController` — position delta, speed |
| `DailyChallengeTracker` | `SWEF.Flight.AltitudeController` — `CurrentAltitudeMeters` |
| `DailyChallengeTracker` | `SWEF.Screenshot.ScreenshotController` — `OnScreenshotCaptured` |
| `DailyChallengeTracker` | `SWEF.GuidedTour.TourManager` — `OnTourCompleted` |
| `DailyChallengeTracker` | `SWEF.Multiplayer.FormationFlyingManager` — `OnFormationBroken` |
| `DailyChallengeTracker` | `SWEF.Multiplayer.NetworkManager2` — `OnLobbyJoined` |
| `ChallengeRewardController` | `SWEF.Progression.ProgressionManager` — `AddXP()` |
| `ChallengeRewardController` | `SWEF.Progression.CosmeticUnlockManager` — `UnlockCosmetic()` |
| `ChallengeRewardController` | `SWEF.Progression.SkillTreeManager` — `AddSkillPoint()` |
| `SeasonPassManager` | `ChallengeRewardController.GrantSeasonReward()` |

---

## Phase 41 — Social Hub & Player Profile System

New directory: `Assets/SWEF/Scripts/SocialHub/` — 10 scripts, namespace `SWEF.SocialHub`.

### Scripts

| # | Script | Namespace | Description |
|---|--------|-----------|-------------|
| 1 | `PlayerProfile.cs` | `SWEF.SocialHub` | Serializable data class — public profile snapshot (identity, rank, stats, achievements, cosmetics) |
| 2 | `PlayerProfileManager.cs` | `SWEF.SocialHub` | Singleton — builds local profile from live systems, caches remote profiles to JSON |
| 3 | `FriendManager.cs` | `SWEF.SocialHub` | Singleton — friend list / request management, persists to `friends.json` |
| 4 | `SocialHubController.cs` | `SWEF.SocialHub` | Central controller — opens/closes the Social Hub overlay, routes panel navigation |
| 5 | `ProfileCardUI.cs` | `SWEF.SocialHub` | Compact profile card — avatar, name, title, rank, stats summary, action button |
| 6 | `FriendListUI.cs` | `SWEF.SocialHub` | Friend list panel — confirmed friends, incoming/outgoing requests, add-friend form |
| 7 | `SocialActivityFeed.cs` | `SWEF.SocialHub` | Singleton — records social events, auto-hooks into Progression/Achievement/DailyChallenge/Multiplayer |
| 8 | `ProfileCustomizationUI.cs` | `SWEF.SocialHub` | Customization panel — avatar picker, title selector, live preview card |
| 9 | `PlayerSearchUI.cs` | `SWEF.SocialHub` | Search panel — searches cached remote profiles by name, shows add-friend actions |
| 10 | `SocialNotificationSystem.cs` | `SWEF.SocialHub` | Singleton — generates/persists social notifications; friend requests, activity, lobby joins |

### PlayerProfile Fields

| Field | Type | Description |
|-------|------|-------------|
| `playerId` | string | Unique player UUID |
| `displayName` | string | Player-chosen display name (2–20 chars) |
| `avatarId` | string | Selected avatar asset identifier |
| `titleId` | string | Equipped title (NameTag cosmetic id) |
| `pilotRankLevel` | int | Current pilot rank level (1–50) |
| `pilotRankName` | string | Human-readable rank name |
| `totalXP` | long | Total accumulated XP |
| `totalFlightTimeMinutes` | float | Total flight time in minutes |
| `totalDistanceKm` | float | Total distance flown in km |
| `maxAltitudeMeters` | float | Highest altitude ever reached (metres) |
| `maxSpeedKmh` | float | Top speed ever achieved (km/h) |
| `totalFlights` | int | Number of completed flights |
| `achievementsUnlocked` | int | Count of unlocked achievements |
| `achievementsTotal` | int | Total achievements available |
| `dailyStreak` | int | Current consecutive-day streak |
| `seasonTier` | int | Current season-pass tier |
| `isPremium` | bool | Premium season-pass status |
| `equippedCosmetics` | Dictionary<string,string> | Category name → equipped cosmetic id |

### Social Hub Architecture

```
SocialHubController.Open(panel)
  ├── SocialHubPanel.MyProfile        → PlayerProfileManager.GetLocalProfile()
  │                                       └─ ProgressionManager, AchievementManager,
  │                                          DailyChallengeManager, SeasonPassManager,
  │                                          CosmeticUnlockManager
  ├── SocialHubPanel.Friends          → FriendListUI.Refresh()
  │                                       └─ FriendManager: friends, incoming, outgoing
  ├── SocialHubPanel.ActivityFeed     → ActivityFeedUI.Refresh()
  │                                       └─ SocialActivityFeed.GetEntries()
  ├── SocialHubPanel.PlayerSearch     → PlayerSearchUI (searches cached remote profiles)
  │                                       └─ PlayerProfileManager.GetAllRemoteProfiles()
  └── SocialHubPanel.Customization    → ProfileCustomizationUI.Open()
                                          ├─ PlayerProfileManager.SetDisplayName()
                                          ├─ PlayerProfileManager.SetAvatarId()
                                          └─ CosmeticUnlockManager.EquipCosmetic(NameTag)
```

### Activity Feed Event Sources

| Event | Source |
|-------|--------|
| FlightCompleted | `SocialActivityFeed.NotifyFlightCompleted()` |
| AchievementUnlocked | `AchievementManager.OnAchievementUnlocked` |
| RankUp | `ProgressionManager.OnRankUp` |
| SeasonTierReached | `SeasonPassManager.OnTierAdvanced` |
| ChallengeCompleted | `DailyChallengeManager.OnChallengeCompleted` |
| BecameFriends | `FriendManager.OnFriendAdded` |
| JoinedMultiplayer | `NetworkManager2.OnLobbyJoined` |

### Persistence

| File | Contents |
|------|----------|
| `friends.json` | Friend entries (status, display name, timestamp) |
| `social_profiles.json` | Cached remote PlayerProfile objects |
| `social_activity.json` | Rolling activity feed (max 200 entries) |
| `social_notifications.json` | Social notifications (max 100, read/unread) |

### Integration Points

| Phase 41 Script | Integrates With |
|----------------|----------------|
| `PlayerProfileManager` | `SWEF.Progression.ProgressionManager` — XP, rank, stats |
| `PlayerProfileManager` | `SWEF.Progression.CosmeticUnlockManager` — equipped cosmetics |
| `PlayerProfileManager` | `SWEF.Achievement.AchievementManager` — achievement counts |
| `PlayerProfileManager` | `SWEF.DailyChallenge.DailyChallengeManager` — daily streak |
| `PlayerProfileManager` | `SWEF.DailyChallenge.SeasonPassManager` — season tier, premium |
| `SocialActivityFeed` | `SWEF.Progression.ProgressionManager.OnRankUp` |
| `SocialActivityFeed` | `SWEF.Achievement.AchievementManager.OnAchievementUnlocked` |
| `SocialActivityFeed` | `SWEF.DailyChallenge.DailyChallengeManager.OnChallengeCompleted` |
| `SocialActivityFeed` | `SWEF.DailyChallenge.SeasonPassManager.OnTierAdvanced` |
| `SocialActivityFeed` | `SWEF.Multiplayer.NetworkManager2.OnLobbyJoined` |
| `SocialNotificationSystem` | `FriendManager.OnFriendAdded`, `OnFriendListChanged` |
| `SocialNotificationSystem` | `SocialActivityFeed.OnActivityPosted` |
| `SocialNotificationSystem` | `SWEF.Multiplayer.NetworkManager2.OnPlayerConnected` |
| `ProfileCustomizationUI` | `SWEF.Progression.CosmeticUnlockManager.GetUnlockedCosmetics()` |

## Phase 42 — Mini-Map & Radar System

The Mini-Map & Radar system provides a comprehensive real-time navigation overlay with blip icons, a rotating radar sweep mode, a compass ring, and a full settings panel. All components are in `Assets/SWEF/Scripts/Minimap/` (namespace `SWEF.Minimap`).

### Scripts (8 total)

| # | Script | Role |
|---|--------|------|
| 1 | `MinimapData.cs` | Data layer — `MinimapIconType` enum (14 values) and `MinimapBlip` serializable class |
| 2 | `MinimapManager.cs` | Singleton — blip registry, per-frame distance & bearing calculation, range culling |
| 3 | `MinimapIconConfig.cs` | ScriptableObject — maps each `MinimapIconType` to `Sprite`, `Color`, scale, and label flag |
| 4 | `MinimapRenderer.cs` | Canvas UI renderer — object-pooled blip icons, circular/square shapes, pulsing animations, smooth zoom |
| 5 | `RadarOverlay.cs` | Radar sweep mode — rotating sweep line (default 6 RPM), phosphor-style fading blip dots, concentric range rings |
| 6 | `MinimapBlipProvider.cs` | Auto-bridge — scans game systems and registers/deregisters blips; updates moving entity positions each frame |
| 7 | `MinimapSettingsUI.cs` | Settings panel — toggle, shape, mode, zoom, opacity, icon size, category filters; full `PlayerPrefs` persistence |
| 8 | `MinimapCompass.cs` | Compass ring — cardinal/intercardinal labels, heading-relative rotation, bearing-to-target line, distance text |

### Architecture

```
                        ┌─────────────────────────────┐
                        │       MinimapManager         │
                        │  (singleton, DontDestroyOnLoad)│
                        │  List<MinimapBlip> registry  │
                        │  LateUpdate: dist + bearing  │
                        └──────────┬──────────────────┘
                                   │  GetActiveBlips()
              ┌────────────────────┼────────────────────┐
              ▼                    ▼                     ▼
    MinimapRenderer          RadarOverlay          MinimapCompass
    (blip icons, pool,    (sweep line, dots,    (cardinal labels,
     shape, zoom lerp)     range rings, ping)    bearing indicator)
              ▲
    MinimapBlipProvider
    (bridges game systems)
      ├─ FlightController      → Player blip
      ├─ WaypointNavigator     → Waypoint / WaypointNext / WaypointVisited
      ├─ PlayerSyncSystem      → OtherPlayer blips
      ├─ FormationFlyingManager→ FormationSlot blips
      ├─ GhostRacer            → GhostReplay blip
      ├─ EventScheduler        → WorldEvent blips
      ├─ WeatherManager        → WeatherZone blips
      └─ "SWEF_POI" tagged GOs → PointOfInterest blips

    MinimapSettingsUI
    (fires OnSettingsChanged → MinimapRenderer + RadarOverlay)
```

### Key Data Types

| Type | Description |
|------|-------------|
| `MinimapIconType` | 14-value enum: `Player`, `Waypoint`, `WaypointNext`, `WaypointVisited`, `OtherPlayer`, `FormationSlot`, `GhostReplay`, `WorldEvent`, `WeatherZone`, `PointOfInterest`, `Destination`, `TourPath`, `DangerZone`, `LandingZone` |
| `MinimapBlip` | Identity (`blipId`, `iconType`), world position, display (`label`, `color`), visibility flags (`isActive`, `isPulsing`), derived navigation (`distanceFromPlayer`, `bearingDeg`), metadata dictionary |
| `IconEntry` | Per-`MinimapIconType` config: `sprite`, `defaultColor`, `defaultScale`, `showLabel` |
| `MinimapShape` | `Circular` / `Square` — controls how `MinimapRenderer` clips blip positions |

### Settings Persistence Keys

| PlayerPrefs Key | Type | Default | Description |
|-----------------|------|---------|-------------|
| `SWEF_Minimap_Visible` | int (bool) | 1 | Whether the minimap is shown |
| `SWEF_Minimap_Shape` | int (enum) | 0 (Circular) | Minimap shape |
| `SWEF_Minimap_Mode` | int (bool) | 0 (Minimap) | 0 = minimap, 1 = radar |
| `SWEF_Minimap_Zoom` | float | 1000 | World-unit radius shown |
| `SWEF_Minimap_Opacity` | float | 1.0 | Blip layer opacity (0.3–1.0) |
| `SWEF_Minimap_IconSize` | float | 1.0 | Icon scale multiplier (0.5–2.0) |
| `SWEF_Minimap_ShowWeather` | int (bool) | 1 | Show weather zone blips |
| `SWEF_Minimap_ShowPOI` | int (bool) | 1 | Show point-of-interest blips |
| `SWEF_Minimap_ShowEvents` | int (bool) | 1 | Show world event blips |
| `SWEF_Minimap_ShowOtherPlayers` | int (bool) | 1 | Show other player blips |
| `SWEF_Minimap_ShowFormation` | int (bool) | 1 | Show formation slot blips |

### Integration Points

| Phase 42 Script | Integrates With |
|----------------|----------------|
| `MinimapManager` | `SWEF.Flight.FlightController` — auto-finds player transform |
| `MinimapBlipProvider` | `SWEF.Flight.FlightController` — player blip position |
| `MinimapBlipProvider` | `SWEF.GuidedTour.WaypointNavigator` — waypoint blips |
| `MinimapBlipProvider` | `SWEF.Multiplayer.PlayerSyncSystem` — remote player blips |
| `MinimapBlipProvider` | `SWEF.Multiplayer.FormationFlyingManager` — formation slot blips |
| `MinimapBlipProvider` | `SWEF.Replay.GhostRacer` — ghost replay blip |
| `MinimapBlipProvider` | `SWEF.Events.EventScheduler` — world event blips |
| `MinimapBlipProvider` | `SWEF.Weather.WeatherManager` — weather zone blips |
| `MinimapSettingsUI` | `MinimapRenderer` — shape, zoom, opacity, icon size |
| `MinimapSettingsUI` | `RadarOverlay` — radar mode toggle |
| `MinimapCompass` | `MinimapManager.PlayerTransform` — heading, nav target bearing |

## Phase 43 — Flight Journal & Logbook System

The Flight Journal & Logbook System is an automatic flight diary that records every flight session with rich metadata (route, altitude profile, distance, duration, weather, achievements, screenshots, tours). Players can browse, filter, search, and share their flight history. All components live in `Assets/SWEF/Scripts/Journal/` (namespace `SWEF.Journal`).

### Scripts (10 total)

| # | Script | Namespace | Purpose |
|---|--------|-----------|---------|
| 1 | `JournalData.cs` | `SWEF.Journal` | Pure data classes: `FlightLogEntry`, `JournalFilter`, `JournalStatistics`, `JournalSortBy` enum |
| 2 | `JournalManager.cs` | `SWEF.Journal` | Singleton MonoBehaviour — auto-logging, persistence, CRUD API, events |
| 3 | `JournalAutoRecorder.cs` | `SWEF.Journal` | Real-time data collection during flight (altitude, distance, speed, location) |
| 4 | `JournalPanelUI.cs` | `SWEF.Journal` | Full-screen journal browser with filter bar, sort dropdown, and search |
| 5 | `JournalDetailUI.cs` | `SWEF.Journal` | Single-flight detail view: notes, tags, screenshots, replay, share, delete |
| 6 | `JournalStatisticsUI.cs` | `SWEF.Journal` | Statistics dashboard with animated counter transitions |
| 7 | `JournalShareController.cs` | `SWEF.Journal` | Export & share flight logs (native share sheet / clipboard fallback) |
| 8 | `JournalSearchEngine.cs` | `SWEF.Journal` | Filter, search, and sort logic decoupled from UI |
| 9 | `JournalTagManager.cs` | `SWEF.Journal` | Global tag registry with auto-suggestion and usage statistics |
| 10 | `JournalComparisonUI.cs` | `SWEF.Journal` | Side-by-side comparison of two flights with delta indicators |

### Architecture

```
┌─ JournalPanelUI ─────────────────────────────────────────┐
│  ScrollView cards  │  Filter bar  │  Sort  │  Search      │
└─────────────────────────────────────────────────────────┘
         ↕                     ↕
   JournalManager ←── JournalAutoRecorder
   │  (Singleton, DDOL)    │  altitude samples (5 s)
   │  CRUD + events        │  distance accumulation
   │  flight_journal.json  │  speed tracking
   │                       └─ FlightController, AltitudeController
   ├─ JournalSearchEngine      (filter / sort)
   ├─ JournalTagManager        (tag registry / suggestions)
   │
   ├─ OnNewEntryAdded ──→ JournalPanelUI.Refresh()
   ├─ OnEntryUpdated  ──→ JournalPanelUI.Refresh()
   └─ OnEntryDeleted  ──→ JournalPanelUI.Refresh()

JournalDetailUI ←── JournalManager.GetEntry()
   ├─ notes / tags editor
   ├─ screenshot gallery
   ├─ Watch Replay → GhostRacer.StartRace()
   └─ Share → JournalShareController.Share()

JournalStatisticsUI ←── JournalManager.GetStatistics()
   └─ animated counter transitions

JournalComparisonUI ←── JournalManager.GetAllEntries()
   └─ delta row (green/red arrows)
```

### Journal Data Model — FlightLogEntry

| Field | Type | Description |
|-------|------|-------------|
| `logId` | string | GUID unique identifier |
| `flightDate` | string (ISO-8601) | Flight start timestamp (UTC) |
| `departureLocation` | string | GPS or landmark name |
| `arrivalLocation` | string | GPS or landmark name |
| `durationSeconds` | float | Total flight duration |
| `distanceKm` | float | Total distance flown |
| `maxAltitudeM` | float | Peak altitude (metres) |
| `avgSpeedKmh` | float | Average speed |
| `maxSpeedKmh` | float | Peak speed |
| `altitudeProfile` | float[] | Altitude sampled every 5 s |
| `weatherCondition` | string | WeatherManager.CurrentWeather.description at start |
| `atmosphereLayer` | string | Highest atmosphere layer reached |
| `tourName` | string | Completed guided tour name (or empty) |
| `achievementsUnlocked` | string[] | Achievement IDs earned during flight |
| `screenshotPaths` | string[] | Up to 5 screenshot file paths |
| `replayFileId` | string | Linked replay file ID |
| `pilotRankAtTime` | string | Pilot rank at time of flight |
| `xpEarned` | int | XP earned during flight |
| `tags` | string[] | User-defined tags |
| `notes` | string | Free-text notes (max 500 chars) |
| `isFavorite` | bool | Favourite flag |
| `flightPathHash` | string | Route comparison hash |

### Auto-Recording Flow

```
FlightController.IsFlying → true
        │
        └─► JournalManager.BeginEntry()
               └─► JournalAutoRecorder.BeginRecording()
                     ├── departure GPS recorded
                     ├── weather snapshot taken
                     ├── altitude sample coroutine started (5 s interval)
                     └── per-frame: distance Δ + speed tracking

FlightController.IsFlying → false
        │
        └─► JournalManager.EndEntry()
               ├─► JournalAutoRecorder.StopRecording()  ← fills entry fields
               ├── duration < 10 s? → discard
               ├── link replay file if FlightRecorder was active
               ├── commit to _entries list
               └── SaveJournal() + fire OnNewEntryAdded
```

### Filter & Search Capabilities (`JournalFilter`)

| Field | Type | Description |
|-------|------|-------------|
| `dateFrom` / `dateTo` | string (ISO) | Date range |
| `minDuration` / `maxDuration` | float | Duration range (seconds) |
| `minAltitude` / `maxAltitude` | float | Altitude range (metres) |
| `weatherFilter` | string | Partial match on weather condition |
| `tourFilter` | string | Partial match on tour name |
| `tagsFilter` | string[] | Any-match tag filter |
| `favoritesOnly` | bool | Only favourite entries |
| `searchQuery` | string | Full-text across notes, locations, tags, tour, weather |
| `sortBy` | JournalSortBy | Date / Duration / Distance / Altitude / Speed / XP |
| `sortDescending` | bool | Default true |

### Statistics Dashboard Fields (`JournalStatistics`)

| Field | Description |
|-------|-------------|
| `totalFlights` | Count of all recorded flights |
| `totalDistanceKm` | Cumulative distance |
| `totalDurationHours` | Cumulative flight time |
| `highestAltitudeEver` | All-time altitude record |
| `fastestSpeedEver` | All-time speed record |
| `longestFlightSeconds` | Longest single flight |
| `favoriteWeather` | Most common weather condition |
| `mostVisitedLocation` | Most frequent departure/arrival |
| `flightsThisWeek` / `flightsThisMonth` | Recency counts |
| `currentStreak` / `longestStreak` | Consecutive-day streaks |
| `averageFlightDuration` | Mean duration (seconds) |
| `averageAltitude` | Mean peak altitude (metres) |

### Persistence

| File | Location | Contents |
|------|----------|----------|
| `flight_journal.json` | `Application.persistentDataPath/` | All `FlightLogEntry` records |
| `journal_tags.json` | `Application.persistentDataPath/` | Tag registry with use counts |

### Localization Keys Added

| Key | Default (English) |
|-----|------------------|
| `journal_sort_date` | "Date" |
| `journal_sort_duration` | "Duration" |
| `journal_sort_distance` | "Distance" |
| `journal_sort_altitude` | "Altitude" |
| `journal_sort_speed` | "Speed" |
| `journal_sort_xp` | "XP" |
| `journal_stats_unknown` | "Unknown" |
| `journal_share_text` | "I flew {0:F1} km in {1:F0} min, reaching {2:F0} m!" |
| `journal_compare_same_route` | "Same Route" |

### Integration Points

| Journal Script | Integrates With |
|---------------|----------------|
| `JournalManager` | `SWEF.Flight.FlightController.IsFlying` — auto flight detection |
| `JournalManager` | `SWEF.Achievement.AchievementManager.OnAchievementUnlocked` |
| `JournalManager` | `SWEF.Screenshot.ScreenshotController.OnScreenshotCaptured` |
| `JournalManager` | `SWEF.GuidedTour.TourManager.OnTourCompleted` |
| `JournalManager` | `SWEF.Progression.ProgressionManager.GetCurrentRank()` |
| `JournalManager` | `SWEF.Weather.WeatherManager.CurrentWeather.description` |
| `JournalAutoRecorder` | `SWEF.Flight.FlightController` — position, speed |
| `JournalAutoRecorder` | `SWEF.Flight.AltitudeController` — current altitude |
| `JournalAutoRecorder` | `SWEF.Recorder.FlightRecorder` — IsRecording flag |
| `JournalDetailUI` | `SWEF.Replay.ReplayFileManager` — load replay by ID |
| `JournalDetailUI` | `SWEF.Replay.GhostRacer` — start ghost replay |
| `JournalTagManager` | `SWEF.Multiplayer.NetworkManager2` — multiplayer tag suggestion |
| `JournalShareController` | Follows `SWEF.Achievement.AchievementShareController` pattern |

---

## Phase 43 — Hidden Gems & Secret Locations Discovery System

A curated database of 55 real-world hidden locations across all continents that players can discover while flying. Features proximity detection, fog-of-war reveal, collectible cards, lore entries, and deep integration with existing minimap, achievement, tour, and progression systems.

### New Scripts (`Assets/SWEF/Scripts/HiddenGems/`)

| # | Script | Namespace | Purpose |
|---|--------|-----------|---------|
| 1 | `HiddenGemData.cs` | `SWEF.HiddenGems` | Pure data classes: `HiddenGemDefinition`, `HiddenGemState`, `GemDiscoveryEvent`; all enums |
| 2 | `HiddenGemDatabase.cs` | `SWEF.HiddenGems` | Static database of 55 real-world hidden gems with accurate GPS coordinates |
| 3 | `HiddenGemManager.cs` | `SWEF.HiddenGems` | Singleton manager: proximity detection with spatial hashing, persistence, events, minimap blips |
| 4 | `GemDiscoveryUI.cs` | `SWEF.HiddenGems` | Animated discovery popup with queue system, 8-second auto-dismiss, share/navigate buttons |
| 5 | `GemCollectionUI.cs` | `SWEF.HiddenGems` | Full-screen gallery with tabs, grid cards, detail view, sort/filter/search |
| 6 | `GemRadarUI.cs` | `SWEF.HiddenGems` | Compass-style HUD radar pointing to nearest undiscovered gem, with warmth-based pulsing |
| 7 | `GemMinimapIntegration.cs` | `SWEF.HiddenGems` | Bridges `HiddenGemManager` ↔ `MinimapManager`; registers blips, distance-based visibility |
| 8 | `GemTourGenerator.cs` | `SWEF.HiddenGems` | Static utility generating `TourData` from gem selections (continent/rarity/nearby/custom) |
| 9 | `GemStatisticsTracker.cs` | `SWEF.HiddenGems` | Analytics tracker: discovery rate, streaks, continent completion, rarity distribution |
| 10 | `Editor/HiddenGemEditorWindow.cs` | `SWEF.Editor` | Unity Editor tool: validation, GPS check, duplicate detection, charts, export/import JSON |

### Gem Rarities & Colours

| Rarity | Colour | XP Reward | Discovery Radius |
|--------|--------|-----------|-----------------|
| Common | `#AAAAAA` | 50 XP | 600 m |
| Uncommon | `#1EFF00` | 100 XP | 500 m |
| Rare | `#0070FF` | 200 XP | 400 m |
| Epic | `#A335EE` | 500 XP | 300 m |
| Legendary | `#FF8000` | 1000 XP | 200 m |

### Gem Categories (15)

`NaturalWonder`, `AncientRuin`, `SecretBeach`, `HiddenWaterfall`, `UndergroundCave`,
`AbandonedStructure`, `SacredSite`, `GeologicalFormation`, `HiddenVillage`,
`MysteriousLandmark`, `UnexploredIsland`, `ForgottenTemple`, `NaturalArch`,
`VolcanicFormation`, `IceFormation`

### Continental Gem Counts

| Continent | Count |
|-----------|-------|
| Asia | 10 |
| Europe | 10 |
| North America | 8 |
| South America | 8 |
| Africa | 8 |
| Oceania | 8 |
| Antarctica | 3 |
| **Total** | **55** |

### Architecture

```
HiddenGemManager (Singleton, DontDestroyOnLoad)
│   ├── Loads HiddenGemDefinition[] from HiddenGemDatabase.GetAllGems()
│   ├── Persists HiddenGemState[] → persistentDataPath/hidden_gems.json
│   ├── Spatial hash grid for proximity detection (10 km grid cells)
│   └── Events: OnGemDiscovered, OnGemFavorited
│
├── GemDiscoveryUI          → subscribes OnGemDiscovered, shows popup queue
├── GemCollectionUI         → full gallery with filters/sort/detail view
├── GemRadarUI              → HUD compass pointing to nearest undiscovered
├── GemMinimapIntegration   → bridges to MinimapManager blip system
├── GemTourGenerator        → creates TourData from gem selections
├── GemStatisticsTracker    → analytics + persistent stats
│
└── Editor/HiddenGemEditorWindow  → validation, preview, testing
```

### Persistence

| File | Location | Contents |
|------|----------|----------|
| `hidden_gems.json` | `Application.persistentDataPath` | All `HiddenGemState` records |
| `hidden_gems_stats.json` | `Application.persistentDataPath` | Discovery statistics |

### Localization Keys (Phase 43)

Added to all 8 language files (`lang_en.json` … `lang_pt.json`):

- `gem_panel_title`, `gem_filter_*`, `gem_total_progress`, `gem_discovery_popup_title`
- `gem_radar_cold`, `gem_radar_warm`, `gem_radar_hot`, `gem_radar_nearby`
- `gem_rarity_*` (5 rarities) — with UI colour coding
- `gem_continent_*` (7 continents)
- `gem_category_*` (15 categories)
- 55 gem name/description/fact keys (e.g. `gem_trolltunga_name`, `gem_trolltunga_desc`, `gem_trolltunga_fact`)
- `gem_undiscovered_hint`, `gem_locked_requirement`, `gem_navigate_button`, `gem_share_button`
- `gem_tour_continent`, `gem_tour_rarity`, `gem_tour_nearby`, `gem_tour_custom`
- `gem_stats_title`, `gem_stats_rate`, `gem_stats_streak`

### Integration Points

| HiddenGems Script | Integrates With |
|------------------|----------------|
| `HiddenGemManager` | `SWEF.Flight.FlightController` — player position, velocity |
| `HiddenGemManager` | `SWEF.Minimap.MinimapManager` — RegisterBlip/UnregisterBlip |
| `HiddenGemManager` | `SWEF.Progression.ProgressionManager.AddXP()` |
| `HiddenGemManager` | `SWEF.Achievement.AchievementManager.ReportProgress()` |
| `HiddenGemManager` | `SWEF.Analytics.UserBehaviorTracker.TrackFeatureDiscovery()` |
| `GemDiscoveryUI` | `SWEF.Localization.LocalizationManager` |
| `GemDiscoveryUI` | `SWEF.Social.ShareManager.ShareText()` |
| `GemDiscoveryUI` | `SWEF.GuidedTour.WaypointNavigator.SetManualTarget()` |
| `GemCollectionUI` | `SWEF.GuidedTour.TourManager.StartTour()` |
| `GemMinimapIntegration` | `SWEF.Minimap.MinimapManager` |
| `GemTourGenerator` | `SWEF.GuidedTour.TourData`, `TourManager` |
| `GemStatisticsTracker` | `SWEF.Progression.ProgressionManager.TotalFlightTimeSeconds` |
| `GemRadarUI` | `SWEF.Minimap.MinimapManager.GetBlip()` |

---

## Phase 44 — Custom Aircraft & Avatar System

### New Scripts (12 files) — `Assets/SWEF/Scripts/Aircraft/` — namespace `SWEF.Aircraft`

| # | Script | Purpose |
|---|--------|---------|
| 1 | `AircraftData.cs` | Pure data classes & enums: `AircraftSkinRarity`, `AircraftPartType`, `AircraftUnlockType`, `AircraftSkinDefinition`, `AircraftUnlockCondition`, `AircraftLoadout`, `AircraftCustomizationSaveData` |
| 2 | `AircraftSkinRegistry.cs` | Singleton skin registry — O(1) lookups, filter by part/rarity/unlock type |
| 3 | `AircraftCustomizationManager.cs` | Singleton manager — unlock, equip, loadout CRUD, favourites, JSON save/load |
| 4 | `AircraftUnlockEvaluator.cs` | Static utility — evaluates unlock conditions against all game systems |
| 5 | `AircraftVisualController.cs` | Applies materials, trail colours, particle/aura prefabs, decals at runtime |
| 6 | `AircraftTrailController.cs` | Speed-driven opacity, altitude-driven width via `ExpSmoothing.ExpLerp` |
| 7 | `AircraftHangarUI.cs` | Full-screen hangar with grid, part-filter, rarity filter, sort, loadout management |
| 8 | `AircraftSkinCardUI.cs` | Skin card: icon, rarity badge, lock/unlock, equip button, favourite star |
| 9 | `AircraftPreviewController.cs` | 3-D preview — drag orbit, pinch zoom, auto-rotate idle, skin preview |
| 10 | `AircraftMultiplayerSync.cs` | Serialises/broadcasts loadout; applies incoming remote loadouts |
| 11 | `AircraftAchievementBridge.cs` | Reports milestones (total skins, first Legendary, full set) to `AchievementManager` |
| 12 | `AircraftSettingsBridge.cs` | Persists trail/particle/aura/remote-skin settings via PlayerPrefs |

### PlayerPrefs Keys Added

| Key | Default |
|-----|---------|
| `SWEF_Aircraft_TrailEnabled` | `1` (true) |
| `SWEF_Aircraft_ParticleQuality` | `2` (Medium) |
| `SWEF_Aircraft_ShowOtherPlayerSkins` | `1` (true) |
| `SWEF_Aircraft_AuraEnabled` | `1` (true) |

### Save File

`aircraft_customization.json` in `Application.persistentDataPath` — serialised `AircraftCustomizationSaveData`.

---

## Phase 49 — Route Planner & Custom Flight Path

### New Scripts (10 files) — `Assets/SWEF/Scripts/RoutePlanner/` — namespace `SWEF.RoutePlanner`

| # | Script | Purpose |
|---|--------|---------|
| 1 | `RoutePlannerData.cs` | Pure data classes & enums: `RouteType`, `WaypointType`, `RouteVisibility`, `NavigationStyle`, `RouteStatus`, `RouteWaypoint`, `FlightRoute`, `RouteProgress`, `RoutePlannerConfig` |
| 2 | `RoutePlannerManager.cs` | Singleton manager — route CRUD, navigation state machine, waypoint triggering, ETA calculation, off-path detection |
| 3 | `RouteBuilderController.cs` | Interactive route builder — tap-to-place waypoints, undo/redo stack, landmark snapping, Catmull-Rom distance estimation, route validation |
| 4 | `RoutePathRenderer.cs` | 3D path line renderer — Catmull-Rom spline, altitude gradient colouring, animated flow, waypoint markers |
| 5 | `RouteNavigationHUD.cs` | In-flight navigation HUD — next-waypoint panel, progress bar, constraint hints (altitude/speed), off-path warning, completion screen |
| 6 | `RouteStorageManager.cs` | Persistent storage — JSON save/load to `Routes/MyRoutes/`, `Routes/Downloaded/`, `Routes/Drafts/`; import/export `.swefroute` files |
| 7 | `RouteShareManager.cs` | Social sharing — deep links, QR code generation, `.swefroute` file sharing, multiplayer broadcast, per-route ratings & leaderboard |
| 8 | `RoutePlannerUI.cs` | Full pre-flight planner UI — route list, detail view, builder mode, settings panel; delegates to `RouteBuilderController` |
| 9 | `RouteRecommendationEngine.cs` | Intelligent route suggestions — scoring by rating/downloads/freshness, route-of-the-day, nearby routes, contextual recommendations |
| 10 | `RoutePlannerAnalytics.cs` | Analytics tracking — route_created, route_started, route_completed, route_abandoned, waypoint_reached, off_path, route_shared, route_imported, route_rated, route_builder_used |

### Route Types

| Route Type | Description |
|-----------|-------------|
| `Scenic` | Relaxed sightseeing route designed for scenery |
| `Speed` | Optimised for minimum travel time |
| `Exploration` | Off-the-beaten-path discovery route |
| `Challenge` | Skill-testing route with obstacles or constraints |
| `Tour` | Guided tour converted into a reusable route |
| `Custom` | Player-created route with no predefined purpose |
| `Race` | Competitive route used in timed races |
| `Photography` | Route curated for optimal photo opportunities |

### Navigation Styles

| Style | Description |
|-------|-------------|
| `FreeFollow` | Player may deviate freely; waypoints are suggestions |
| `StrictPath` | Player must stay within the off-path threshold |
| `TimeAttack` | Route is scored on elapsed time |
| `Relaxed` | No constraints; enjoy the journey at any pace |

### Waypoint Types

| Type | Description |
|------|-------------|
| `Standard` | Regular navigation waypoint |
| `Landmark` | Co-located with a named landmark from `LandmarkDatabase` |
| `Photo` | Recommended photography vantage point with camera hint |
| `Checkpoint` | Timed or ranked checkpoint |
| `Start` | Route origin waypoint |
| `Finish` | Route destination waypoint |
| `RestStop` | Designated hover/pause location with `stayDuration` |
| `HiddenGem` | Secret location tied to the `HiddenGemManager` system |
| `Altitude` | Requires player to reach a minimum altitude |
| `SpeedGate` | Requires player to reach a minimum airspeed |

### Route Builder Features

- **Tap-to-place** waypoints on 3D terrain or minimap
- **Drag-to-reposition** with live distance/duration update
- **Snap to Landmark** — auto-link nearby `LandmarkData` entries
- **Snap to Favourite** — quick-add from `FavoritesManager`
- **Snap to Hidden Gem** — include `HiddenGemManager` locations
- **Undo / Redo** — full operation stack via `RouteBuilderController`
- **Auto-suggest waypoints** — interesting landmarks along the path
- **Catmull-Rom spline** preview — smooth 3D path estimation
- **Route validation** — warns about missing Start/Finish, unconfigured speed/altitude gates
- **Fast-forward preview** — animated path playback via `RoutePathRenderer`

### Route File Format (`.swefroute`)

```json
{
  "routeId": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Alpine Sunrise Loop",
  "description": "Dramatic peaks at golden hour",
  "routeType": "Scenic",
  "difficulty": 2,
  "waypoints": [
    {
      "waypointId": "abc123",
      "index": 0,
      "latitude": 46.85,
      "longitude": 9.53,
      "altitude": 2500.0,
      "waypointType": "Start",
      "name": "Chur Valley",
      "triggerRadius": 100.0,
      "isOptional": false
    }
  ],
  "estimatedDuration": 35.0,
  "estimatedDistance": 180.0,
  "maxAltitude": 4200.0,
  "minAltitude": 800.0,
  "isLoop": true,
  "visibility": "Public",
  "navigationStyle": "FreeFollow",
  "weatherRecommendation": "Best in clear weather",
  "timeOfDayRecommendation": "Best at sunrise",
  "version": 1
}
```

### Navigation HUD Layout

```
┌─────────────────────────────────────────────────────┐
│  [Next Waypoint]  Chur Valley  ◄──── 4.2 km  ETA 1:15 │
│  Progress ████████░░░░░░  6 / 10  waypoints          │
│                                                       │
│  Elapsed: 12:34  |  Dist: 87.3 km  |  Dev: 2         │
│                                                       │
│  [Altitude hint]  Climb to 3200 m                    │
│  [Turn-by-turn]   In 2.0 km, fly towards Silvaplana  │
└─────────────────────────────────────────────────────┘
                   [Off-path warning banner]
```

### Architecture

```
RoutePlannerManager (Singleton, DontDestroyOnLoad)
│   ├── Route CRUD (create, update, delete, getAll)
│   ├── Navigation state machine (Idle → Planning → InProgress → Completed/Abandoned)
│   ├── Waypoint proximity + off-path detection (coroutines)
│   └── Events: OnRouteCreated, OnNavigationStarted, OnWaypointReached, OnRouteCompleted,
│               OnOffPath, OnBackOnPath, OnNavigationPaused, OnNavigationResumed
│
├── RouteBuilderController  → interactive editing, undo/redo, validation
├── RoutePathRenderer       → Catmull-Rom spline LineRenderer, animated flow, markers
├── RouteNavigationHUD      → subscribes to manager events, refreshes every frame
├── RouteStorageManager     → JSON persistence to persistentDataPath subfolders
├── RouteShareManager       → deep links, QR, social share, multiplayer broadcast, ratings
├── RoutePlannerUI          → full pre-flight map UI, delegates builder to RouteBuilderController
├── RouteRecommendationEngine → scoring, nearby routes, route-of-the-day
└── RoutePlannerAnalytics   → forwards events to SWEF.Analytics.UserBehaviorTracker
```

### Integration Points

| RoutePlanner Script | Integrates With |
|--------------------|----------------|
| `RoutePlannerManager` | `SWEF.GuidedTour.WaypointNavigator.SetManualTarget()` |
| `RoutePlannerManager` | `SWEF.Narration.NarrationManager.PlayNarration()` |
| `RoutePlannerManager` | `SWEF.Flight.FlightController` — player position/speed |
| `RoutePathRenderer` | `SWEF.Replay.FlightPathRenderer` — LineRenderer patterns |
| `RoutePathRenderer` | `SWEF.Minimap.MinimapManager` — minimap path overlay |
| `RouteBuilderController` | `SWEF.Narration.LandmarkDatabase.GetNearestLandmark()` |
| `RouteBuilderController` | `SWEF.Favorites.FavoritesManager` — quick-add favourites |
| `RouteBuilderController` | `SWEF.HiddenGems.HiddenGemManager` — gem waypoint snapping |
| `RouteShareManager` | `SWEF.Social.ShareManager.ShareTextWithImage()` |
| `RouteShareManager` | `SWEF.Multiplayer.MultiplayerManager.BroadcastCustomData()` |
| `RoutePlannerAnalytics` | `SWEF.Analytics.UserBehaviorTracker.TrackFeatureDiscovery()` |
| `RouteStorageManager` | `Application.persistentDataPath` — JSON file I/O |

### Localization Keys (Phase 49)

Added to all 8 language files (`lang_en.json` … `lang_pt.json`):

- `route_planner_title`, `route_create`, `route_edit`, `route_delete`, `route_start`, `route_stop`
- `route_waypoint_add`, `route_waypoint_edit`, `route_waypoint_delete`, `route_waypoint_types_*` (10 types)
- `route_type_*` (8 route types)
- `route_nav_*` (4 navigation styles)
- `route_status_*` (5 status values)
- `route_visibility_*` (3 visibility options)
- `route_stats_*` (distance, duration, altitude, speed, waypoints, deviation)
- `route_builder_*` (undo, redo, preview, validate, snap_to_landmark, auto_suggest)
- `route_share_*` (share, import, export, qr_code, rate, leaderboard)
- `route_nav_hud_*` (next_waypoint, eta, off_path, climb, descend, speed_up, completed)

---

## Phase 53 — Weather Challenges & Dynamic Route System

### New Scripts (6 files) — `Assets/SWEF/Scripts/WeatherChallenge/` — namespace `SWEF.WeatherChallenge`

| # | Script | Purpose |
|---|--------|---------|
| 1 | `WeatherChallengeData.cs` | Pure data layer — `ChallengeWeatherType`, `ChallengeDifficulty`, `ChallengeStatus` enums; `RouteWaypoint` (haversine proximity, required action, optional flag); `WeatherChallenge` (waypoints, scoring, weather modifiers, expiry helpers) |
| 2 | `WeatherChallengeManager.cs` | Singleton manager — procedural challenge generation, lifecycle (start/complete/fail), per-frame waypoint proximity tracking, score calculation, JSON persistence, 5 events |
| 3 | `DynamicRouteGenerator.cs` | Procedural route generator — inverse-haversine waypoint placement, weather/difficulty adjustment, route validation, static math utilities (haversine, bearing, destination point) |
| 4 | `WeatherChallengeUI.cs` | UI controller — challenge browser list, detail panel, active HUD (timer/score/next waypoint), results summary; subscribes to manager events |
| 5 | `RouteVisualizationController.cs` | 3D route renderer — `LineRenderer` path, prefab waypoint markers, colour-coded progress (pending/active/completed), particle burst on waypoint reached |
| 6 | `WeatherChallengeAnalyticsBridge.cs` | Analytics bridge — tracks challenge_generated, challenge_started, challenge_completed, challenge_failed, waypoint_reached via `SWEF.Analytics.UserBehaviorTracker` |

### Architecture

```
WeatherChallengeManager (Singleton, DontDestroyOnLoad)
│   ├── GenerateChallenge → DynamicRouteGenerator.GenerateRoute()
│   ├── StartChallenge / UpdateActiveChallenge / CompleteChallenge / FailChallenge
│   ├── CalculateScore (time bonus + completion % + difficulty multiplier)
│   ├── JSON persistence → Application.persistentDataPath/weatherchallenges.json
│   └── Events: OnChallengeGenerated, OnChallengeStarted, OnWaypointReached,
│               OnChallengeCompleted, OnChallengeFailed
│
├── DynamicRouteGenerator   → haversine/inverse-haversine waypoint placement, weather & difficulty adjustment
├── WeatherChallengeUI      → browser, detail, HUD, results panels; subscribes to manager events
├── RouteVisualizationController → LineRenderer path, colour-coded progress, particle waypoint feedback
└── WeatherChallengeAnalyticsBridge → forwards events to SWEF.Analytics.UserBehaviorTracker
```

### Weather Scenarios

| Weather Type | Wind Multiplier | Visibility | Key Mechanic |
|-------------|----------------|-----------|-------------|
| `ClearSkies` | 1.0× | 100% | Precision flying |
| `Fog` | 0.8× | 15% | Instrument navigation |
| `Rain` | 1.4× | 55% | Race the storm front |
| `Thunderstorm` | 2.5× | 40% | Storm chasing, avoid cells |
| `Snow` | 1.5× | 50% | Icing risk, low altitude |
| `Turbulence` | 1.8× | 80% | Altitude/heading hold |
| `Crosswind` | 3.0× | 90% | Crab-angle correction |
| `Thermal` | 1.2× | 100% | Soar on rising air |
| `Icing` | 1.3× | 60% | Stay in safe altitude band |

### Integration Points

| Script | Integrates With |
|--------|----------------|
| `WeatherChallengeManager` | `SWEF.Flight.FlightController` — player lat/lon/alt |
| `WeatherChallengeManager` | `Application.persistentDataPath` — JSON persistence |
| `DynamicRouteGenerator` | Haversine math — no external dependency |
| `WeatherChallengeUI` | `UnityEngine.UI.Text`, `Button` — Unity UI |
| `RouteVisualizationController` | `UnityEngine.LineRenderer`, `ParticleSystem` — Unity rendering |
| `WeatherChallengeAnalyticsBridge` | `SWEF.Analytics.UserBehaviorTracker.TrackFeatureDiscovery()` |

---

## Phase 65 — Cockpit Instrument & HUD Dashboard System

### New Scripts (13 files) — `Assets/SWEF/Scripts/CockpitHUD/` — namespace `SWEF.CockpitHUD`

| # | File | Description |
|---|------|-------------|
| 1 | `CockpitHUDConfig.cs` | Static config — unit conversions (`MetersToFeet`, `MsToKnots`, `MsToKph`), default thresholds, shared `Color` constants (`SafeColor`, `CautionColor`, `WarningColor`, `CriticalColor`) |
| 2 | `FlightData.cs` | Data container — altitude ASL/AGL, speed (m/s / knots / Mach), vertical speed, heading, pitch, roll, yaw, G-force, throttle, fuel, velocity, position, stall/overspeed flags, OAT, wind |
| 3 | `FlightDataProvider.cs` | MonoBehaviour — reads aircraft Rigidbody each `FixedUpdate`; computes altitude from Y position, heading from XZ-projected forward, pitch/roll from Euler angles, G-force from velocity delta, Mach from speed/343; AGL via downward raycast; `event Action<FlightData> OnFlightDataUpdated` |
| 4 | `HUDInstrument.cs` | Abstract MonoBehaviour base class — `instrumentName`, `isVisible`, `minimumMode` (`HUDMode`), per-instrument `CanvasGroup` fade, `abstract UpdateInstrument(FlightData)`, virtual `Show()`/`Hide()`; auto-registers/unregisters with `HUDDashboard` on `OnEnable`/`OnDisable` |
| 5 | `HUDDashboard.cs` | Singleton MonoBehaviour — `HUDMode` enum (Minimal/Standard/Full/CinematicOff), mode cycling with `ToggleHUD()`, `SetMode()`, global opacity via `CanvasGroup`, auto-hide after idle, per-frame `FlightData` distribution to all registered instruments, `event Action<HUDMode> OnModeChanged` |
| 6 | `Altimeter.cs` | HUDInstrument — MSL & AGL readouts (meters or feet), scrolling tape, color coding (green/yellow/red by AGL threshold) |
| 7 | `Speedometer.cs` | HUDInstrument — speed readout (m/s / knots / km/h), Mach overlay above 0.8M, scrolling tape, trend arrow indicating acceleration/deceleration, color coding |
| 8 | `CompassHeading.cs` | HUDInstrument — numeric heading, 8-point cardinal direction, horizontal scrolling compass strip, heading bug for active waypoint; `GetCardinal(float)` helper |
| 9 | `AttitudeIndicator.cs` | HUDInstrument — artificial horizon; rotates `horizonBar` by −roll, shifts vertically by pitch × `pitchPixelsPerDegree`; pitch ladder and bank angle arc follow the same transform |
| 10 | `VerticalSpeedIndicator.cs` | HUDInstrument — smoothed numeric ±m/s readout, analog needle (±180° deflection), vertical bar graph; color: green (level ±2 m/s), blue (climb), orange (descent), red (>30 m/s descent) |
| 11 | `GForceIndicator.cs` | HUDInstrument — numeric G readout, filled arc meter, peak-G tracking with configurable decay rate, color transitions (white/yellow/red by threshold) |
| 12 | `ThrottleFuelGauge.cs` | HUDInstrument — vertical fill bars for throttle & fuel, percentage text, low-fuel blinking at configurable rate and threshold |
| 13 | `WarningSystem.cs` | MonoBehaviour — `WarningLevel` enum (Info/Caution/Warning/Critical), `WarningMessage` struct (code/message/level/timestamp), auto-evaluates stall/overspeed/low-altitude/high-G/low-fuel/high-descent from `FlightData`, `AddWarning`/`ClearWarning`/`AcknowledgeWarning`, audio clips per level, most-critical warning displayed on panel, `OnWarningTriggered`/`OnWarningCleared` events |

### Architecture

```
HUDDashboard (Singleton)
│   ├── SetMode(HUDMode) → show/hide instruments by minimumMode
│   ├── ToggleHUD() → cycles Minimal → Standard → Full → CinematicOff
│   ├── Auto-hide via idle timer + CanvasGroup alpha fade
│   └── Update: distributes FlightData to all registered HUDInstruments
│
├── FlightDataProvider (FixedUpdate)
│   ├── Computes altitude, speed, heading, pitch, roll, G-force, Mach
│   ├── AGL via Physics.Raycast downward
│   └── Fires OnFlightDataUpdated each physics step
│
├── HUDInstrument (abstract base)
│   ├── Auto-registers with HUDDashboard on OnEnable
│   ├── Show() / Hide() with CanvasGroup fade
│   └── abstract UpdateInstrument(FlightData)
│
├── Altimeter          → MSL + AGL text, tape scroll, green/yellow/red by AGL
├── Speedometer        → speed text, Mach overlay, tape, trend arrow
├── CompassHeading     → heading text, cardinal, compass strip, heading bug
├── AttitudeIndicator  → horizon rotation (roll) + shift (pitch), pitch ladder
├── VerticalSpeedIndicator → smoothed VS, needle/bargraph, color by rate
├── GForceIndicator    → G text, filled meter, peak G with decay
├── ThrottleFuelGauge  → throttle/fuel bars, low-fuel blink
└── WarningSystem      → stall/overspeed/altitude/G/fuel/descent checks, audio
```

### Integration Points

| Script | Integrates With |
|--------|----------------|
| `FlightDataProvider` | `UnityEngine.Rigidbody` — velocity, position |
| `FlightDataProvider` | `Physics.Raycast` — AGL calculation |
| `FlightDataProvider` | `SWEF.Flight.FlightController` — throttle/fuel (guarded by `#if SWEF_FLIGHTCONTROLLER_AVAILABLE`) |
| `HUDDashboard` | `FlightDataProvider.CurrentData` — pulls snapshot each frame |
| `WarningSystem` | `FlightDataProvider.OnFlightDataUpdated` — subscribes for real-time evaluation |
| All instruments | `TMPro.TextMeshProUGUI` — all text rendering |
| All instruments | `UnityEngine.UI.Image` — gauges, bars, tapes, meters |

---

## Phase 67 — Radar & Threat Detection System

### New Scripts (9 files) — `Assets/SWEF/Scripts/Radar/` — namespace `SWEF.Radar`

| # | File | Description |
|---|------|-------------|
| 1 | `RadarEnums.cs` | Enums — `ContactClassification` (Unknown/Friendly/Neutral/Hostile/Civilian/Landmark/Event), `ThreatLevel` (None/Low/Medium/High/Imminent), `RadarMode` (Off/Passive/Active/Search/Track), `BlipSize` (Small/Medium/Large/VeryLarge) |
| 2 | `RadarContact.cs` | Data class — unique `contactId`, `trackedTransform`, classification, threat, size, position, velocity, distance, bearing, elevation, signal strength, first/last detected times, `isLocked`, `displayName`, `contactIcon` |
| 3 | `RadarConfig.cs` | Static config — `DefaultRadarRange`, `ScanInterval`, `MaxContacts`, `ContactTimeout`, `SignalFalloffStart`, threat thresholds (`CloseRange`, `MediumRange`, `ClosingSpeedThreshold`), jammer defaults, zoom presets, display colors per classification with `GetClassificationColor()` helper |
| 4 | `RadarSystem.cs` | Singleton MonoBehaviour — `Physics.OverlapSphere` scan coroutine, IFF classification via `IFFTransponder`, signal-strength falloff, contact create/update/expire, `LockTarget`/`UnlockTarget`/`CycleTargets`, `GetNearestHostile`/`GetNearestContact`/`GetContactsByClassification`, events `OnContactDetected`/`OnContactLost`/`OnTargetLocked`/`OnTargetUnlocked` |
| 5 | `IFFTransponder.cs` | MonoBehaviour attached to detectable objects — `identity`, `transponderCode`, `displayName`, `radarSignature`, `radarIcon`, `isTransponderActive`, `signatureModifier`, `baseThreatLevel`; exposes `EffectiveIdentity` (returns Unknown when transponder is silent) |
| 6 | `ThreatDetector.cs` | MonoBehaviour — periodic `EvaluateThreat()` for each contact based on classification, distance, closing speed, heading-toward-player; `prioritizedThreats` sorted by level, `GetHighestThreat()`, `hostileCount`, `imminentThreatCount`; events `OnThreatLevelChanged`/`OnImminentThreat`; Phase 65 WarningSystem integration via `#if SWEF_WARNINGSYSTEM_AVAILABLE` |
| 7 | `RadarDisplay.cs` | MonoBehaviour UI — `RadarDisplayMode` (PlanPosition/BScope/ForwardLooking), rotating sweep line (PPI), blip pool keyed by contactId, per-classification color coding, locked-target blink ring, `CycleZoom()`/`SetDisplayMode()`, range rings label, heading indicator, north indicator |
| 8 | `MissileWarningReceiver.cs` | MonoBehaviour — `Physics.OverlapSphere` projectile detection at configurable Hz, closing-vector filter, direction indicators (`incomingBearing`/`incomingElevation`), looping audio tones for missile/lock warnings, `NotifyRadarLock()`/`ClearRadarLock()`, events `OnMissileDetected`/`OnMissileLockDetected`/`OnThreatCleared` |
| 9 | `RadarJammer.cs` | MonoBehaviour — `ToggleJammer()` with cooldown guard, `jamRange`/`jamEffectiveness`/`powerConsumption`, `DeployChaffs()`/`DeployFlares()` with particle effects, `chaffCount`/`flareCount` charges, `isOnCooldown`/`cooldownRemaining` state, events `OnJammerToggled`/`OnChaffDeployed`/`OnFlareDeployed` |

### Architecture

```
RadarSystem (Singleton)
│   ├── ScanRoutine (coroutine, every scanInterval)
│   │       └── Physics.OverlapSphere → IFFTransponder read → create/update RadarContact
│   ├── CleanupStaleContacts (Update, contactTimeout)
│   ├── LockTarget / UnlockTarget / CycleTargets
│   └── Events: OnContactDetected, OnContactLost, OnTargetLocked, OnTargetUnlocked
│
├── IFFTransponder   → attached to any detectable scene object
│       identity, transponderCode, signatureModifier, baseThreatLevel
│
├── ThreatDetector   → subscribes to RadarSystem contacts
│   ├── EvaluateThreat: classification + distance + closing speed + heading dot
│   ├── prioritizedThreats[] sorted by ThreatLevel descending
│   └── Events: OnThreatLevelChanged, OnImminentThreat
│           └── #if SWEF_WARNINGSYSTEM_AVAILABLE → WarningSystem.AddWarning("THREAT", ...)
│
├── RadarDisplay     → reads RadarSystem.contacts each frame
│   ├── PlanPosition: polar blip placement, rotating sweep line
│   ├── BScope: azimuth/range grid
│   ├── ForwardLooking: azimuth/elevation grid
│   ├── Blip color ← RadarConfig.GetClassificationColor(classification)
│   ├── Lock ring blink on lockedContact
│   └── CycleZoom (2 km / 5 km / 10 km presets)
│
├── MissileWarningReceiver  → OverlapSphere on missileLayers
│   ├── Closing-vector filter (dot product > 0.5)
│   ├── incomingBearing / incomingElevation direction indicators
│   └── Audio: missileWarningTone (loop) / lockWarningTone
│
└── RadarJammer
    ├── ToggleJammer → isJamming, cooldown after deactivation
    ├── DeployChaffs / DeployFlares (particle effects + counters)
    └── powerConsumption drain while active
```

### Integration Points

| Script | Integrates With |
|--------|----------------|
| `RadarSystem` | `Physics.OverlapSphere` — Unity physics for scan |
| `RadarSystem` | `IFFTransponder` — reads identity, signature, display name |
| `ThreatDetector` | `RadarSystem.contacts` — subscribes for periodic evaluation |
| `ThreatDetector` | `SWEF.CockpitHUD.WarningSystem.AddWarning` — guarded by `#if SWEF_WARNINGSYSTEM_AVAILABLE` (Phase 65) |
| `RadarDisplay` | `RadarSystem.contacts` — reads each frame for blip positions |
| `RadarDisplay` | `TMPro.TextMeshProUGUI` — range and heading labels |
| `RadarDisplay` | `UnityEngine.UI.Image` — blip and sweep-line rendering |
| `MissileWarningReceiver` | `Physics.OverlapSphere` — missile layer detection |
| `MissileWarningReceiver` | `Rigidbody.linearVelocity` — closing-speed calculation |
| `RadarJammer` | `ParticleSystem` — chaff/flare visual effects |
