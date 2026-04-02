# README — AdvancedPhotography System (Phase 89)

## Overview

The **Advanced Photography & Drone Camera System** (`SWEF.AdvancedPhotography`) extends
the existing `PhotoMode/` and `Cinema/` systems with an independent autonomous drone
camera, AI-powered composition guidance, multi-mode panorama capture, advanced timelapse,
community photo contests, scenic spot discovery, and a full UGUI HUD/menu layer.

---

## New Scripts (13 files in `Assets/SWEF/Scripts/AdvancedPhotography/`)

| # | Script | Purpose |
|---|--------|---------|
| 1 | `AdvancedPhotographyEnums.cs` | `DroneFlightMode` (8), `CompositionRule` (7), `PhotoSubject` (10), `ChallengeCategory` (5), `PhotoRating` (5), `PanoramaType` (4), `TimelapseMode` (5), `AIAssistLevel` (4) |
| 2 | `AdvancedPhotographyConfig.cs` | Static compile-time constants: drone max range 500 m, max altitude 200 m, battery 300 s, low-battery threshold 10 %, orbit defaults, AI score thresholds, challenge durations (daily 24 h / weekly 168 h), panorama face resolution 2048 px / 30 % overlap, timelapse min/max intervals, photo-spot discovery radius 5000 m |
| 3 | `AdvancedPhotographyData.cs` | `DroneWaypoint` (position, rotation, speed, holdTime, lookAtTarget), `DroneFlightPath` (waypoints, loop, totalDuration), `CompositionAnalysis` (rule, score, suggestion, guidePoints), `PhotoMetadata` (timestamp, GPS, biome, weather, filter, frame, camera settings, compositionScore, subjects list), `PhotoChallenge` ScriptableObject, `PhotoSpot` (spotId, position, subjects, time/weather/season hints, difficulty, discovered) |
| 4 | `DroneAutonomyController.cs` | Singleton (DontDestroyOnLoad): Orbit/Flyby/Follow/Waypoint/Tracking/Cinematic/FreeRoam/ReturnHome modes, battery drain + low-battery auto-return, collision avoidance raycasts, waypoint coroutine with hold-time, `#if SWEF_PHOTOMODE_AVAILABLE` integration |
| 5 | `AICompositionAssistant.cs` | Singleton (DontDestroyOnLoad): screen-space RuleOfThirds/GoldenRatio/Symmetry/CenterWeighted scoring, auto-frame via FOV sweep, coroutine-based update loop, `OnCompositionScoreChanged` / `OnAutoFrameComplete` / `OnSuggestionUpdated` events |
| 6 | `AdvancedPanoramaController.cs` | Horizontal/Vertical strip capture, Full360 cubemap→equirectangular, LittlePlanet stereographic projection, configurable face resolution, progress events, gallery save via `#if SWEF_PHOTOMODE_AVAILABLE` |
| 7 | `AdvancedTimelapseController.cs` | TimeInterval / DistanceInterval / SunTracking / WeatherChange / DayNightCycle modes, configurable frame buffer (max 600), pause/resume, `OnTimelapseComplete(Texture2D[])`, `#if SWEF_WEATHER_AVAILABLE` |
| 8 | `PhotoContestManager.cs` | Singleton (DontDestroyOnLoad): Upcoming→Active→Judging→Complete lifecycle, AI scoring (composition 40 % + subject 30 % + biome 20 % + creativity 10 %), community vote simulation, JSON persistence to `persistentDataPath/PhotoContests/`, `#if SWEF_ACHIEVEMENT_AVAILABLE` |
| 9 | `PhotoSpotDiscovery.cs` | Singleton (DontDestroyOnLoad): `PhotoSpot` registry, proximity-trigger discovery, scored recommendation engine (proximity 25 % + undiscovered 20 % + time-of-day 20 % + weather 20 % + difficulty 10 % + biome 5 %), `#if SWEF_BIOME_AVAILABLE`, `#if SWEF_WEATHER_AVAILABLE`, `#if SWEF_NARRATION_AVAILABLE`, `#if SWEF_MINIMAP_AVAILABLE` |
| 10 | `DronePathEditor.cs` | Tap-to-place waypoints, drag-to-reposition, undo/redo stack, per-waypoint speed & hold time, Catmull-Rom spline `LineRenderer`, path validation (range + altitude), animated preview coroutine |
| 11 | `AdvancedPhotographyHUD.cs` | UGUI HUD overlay: composition guide image, score `Slider`, AI suggestion `Text`, drone status panel (battery, mode, altitude, distance), challenge progress, photo spot direction arrow + distance label, histogram placeholder |
| 12 | `AdvancedPhotographyUI.cs` | Full-screen five-panel menu: Gallery Browser, Challenge List, Contest Panel (submit/vote/leaderboard), Filter Editor (tint/saturation/contrast/vignette/exposure sliders), Settings Panel (AI assist dropdown, guide overlay toggle, auto-save toggle) |
| 13 | `AdvancedPhotographyAnalytics.cs` | Static: `RecordDroneFlightStarted`, `RecordDroneFlightEnded`, `RecordDroneBatteryDepleted`, `RecordAICompositionUsed`, `RecordAutoFrameUsed`, `RecordPanoramaCaptured`, `RecordTimelapseCompleted`, `RecordPhotoSubmittedToContest`, `RecordPhotoSpotDiscovered`, `RecordDronePathCreated`, `RecordChallengeCompleted` — all guarded by `#if SWEF_ANALYTICS_AVAILABLE` |

---

## Architecture

```
DroneAutonomyController (Singleton, DontDestroyOnLoad)
│   ├── SetFlightMode / SetOrbitTarget / StartWaypointPath / ReturnToPlayer
│   ├── WaypointFlightCoroutine  →  OnWaypointReached
│   ├── Battery drain  →  OnBatteryLow / OnBatteryDepleted / ReturnToPlayer
│   ├── Collision avoidance raycasts  →  OnCollisionAvoided
│   └── Events: OnFlightModeChanged, OnBatteryLow, OnBatteryDepleted,
│               OnWaypointReached, OnCollisionAvoided

AICompositionAssistant (Singleton, DontDestroyOnLoad)
│   ├── AnalyzeComposition  →  CompositionAnalysis
│   ├── AutoFrame (FOV sweep coroutine)  →  OnAutoFrameComplete
│   ├── CompositionUpdateLoop coroutine
│   └── Events: OnCompositionScoreChanged, OnAutoFrameComplete,
│               OnSuggestionUpdated

AdvancedPanoramaController
│   ├── StartCapture / CancelCapture / SetFaceResolution
│   ├── Full360 cubemap → equirectangular  (RenderToCubemap)
│   ├── LittlePlanet stereographic projection
│   └── Events: OnPanoramaCaptureStarted, OnPanoramaCaptureProgress,
│               OnPanoramaCaptureComplete, OnPanoramaCaptureFailed

AdvancedTimelapseController
│   ├── StartTimelapse / StopTimelapse / PauseTimelapse / ResumeTimelapse
│   ├── TimelapseLoop coroutine (5 modes)
│   └── Events: OnTimelapseStarted, OnFrameCaptured,
│               OnTimelapseComplete, OnTimelapseCancelled

PhotoContestManager (Singleton, DontDestroyOnLoad)
│   ├── GetActiveContests / SubmitPhoto / GetLeaderboard
│   ├── VoteForPhoto / GetContestResults
│   ├── ScoreSubmission (AI scoring formula)
│   └── Events: OnContestStarted, OnPhotoSubmitted,
│               OnContestEnded, OnResultsAvailable

PhotoSpotDiscovery (Singleton, DontDestroyOnLoad)
│   ├── RegisterSpot / DiscoverSpot / GetNearbySpots
│   ├── GetRecommendedSpots (scored recommendation)
│   └── Events: OnPhotoSpotDiscovered, OnPhotoSpotRecommended

DronePathEditor (per-scene MonoBehaviour)
│   ├── AddWaypoint / RemoveWaypoint / MoveWaypoint
│   ├── Undo / Redo  →  undo/redo stack
│   ├── ValidatePath (range + altitude checks)
│   ├── PreviewPath coroutine + Catmull-Rom LineRenderer
│   └── GetFlightPath  →  DroneFlightPath

AdvancedPhotographyHUD  →  subscribes to all manager events
AdvancedPhotographyUI   →  full-screen panel switcher, delegates to managers
AdvancedPhotographyAnalytics  →  TelemetryDispatcher events (static)
```

---

## Data Types

| Type | Kind | Description |
|------|------|-------------|
| `DroneFlightMode` | enum | 8 autonomous flight modes |
| `CompositionRule` | enum | 7 photographic composition rules |
| `PhotoSubject` | enum | 10 subject categories |
| `ChallengeCategory` | enum | 5 challenge time-window types |
| `PhotoRating` | enum | 1–5 star rating |
| `PanoramaType` | enum | 4 panorama output formats |
| `TimelapseMode` | enum | 5 timelapse trigger modes |
| `AIAssistLevel` | enum | 4 assistance levels |
| `DroneWaypoint` | Serializable class | One point in a drone path |
| `DroneFlightPath` | Serializable class | Ordered list of waypoints |
| `CompositionAnalysis` | Serializable class | Score + suggestion + guide points |
| `PhotoMetadata` | Serializable class | Full photo metadata record |
| `PhotoChallenge` | ScriptableObject | Challenge mission definition |
| `PhotoSpot` | Serializable class | Scenic spot registry entry |
| `TimelapseConfig` | Serializable class | Per-session timelapse settings |
| `ContestSubmission` | Serializable class | Player contest submission record |
| `ActiveContest` | Serializable class | Runtime contest wrapper |

---

## Integration Points

| System | Integration | Guard Symbol |
|--------|-------------|--------------|
| `SWEF.PhotoMode.DroneCameraController` | Base drone camera referenced by `DroneAutonomyController` | `#if SWEF_PHOTOMODE_AVAILABLE` |
| `SWEF.PhotoMode.PhotoGalleryManager` | Panorama result saved to gallery | `#if SWEF_PHOTOMODE_AVAILABLE` |
| `SWEF.Weather.WeatherManager` | Weather condition for timelapse trigger & spot scoring | `#if SWEF_WEATHER_AVAILABLE` |
| `SWEF.Biome.BiomeClassifier` | Biome data used in spot scoring | `#if SWEF_BIOME_AVAILABLE` |
| `SWEF.Narration.LandmarkDatabase` | Landmark lookup for photo spot seeding | `#if SWEF_NARRATION_AVAILABLE` |
| `SWEF.Minimap.MinimapManager` | `RegisterMarker` for photo spots | `#if SWEF_MINIMAP_AVAILABLE` |
| `SWEF.Achievement.AchievementManager` | `TryUnlock("photo_contest_winner")` | `#if SWEF_ACHIEVEMENT_AVAILABLE` |
| `SWEF.Analytics.TelemetryDispatcher` | All 11 telemetry events | `#if SWEF_ANALYTICS_AVAILABLE` |

---

## Localization Keys

43 keys added to all 8 language files:

| Prefix | Count | Examples |
|--------|-------|---------|
| `drone_mode_*` | 8 | `drone_mode_orbit`, `drone_mode_follow` |
| `composition_rule_*` | 7 | `composition_rule_ruleofthirds`, `composition_rule_goldenratio` |
| `photo_subject_*` | 10 | `photo_subject_landscape`, `photo_subject_wildlife` |
| `challenge_cat_*` | 5 | `challenge_cat_daily`, `challenge_cat_weekly` |
| `panorama_type_*` | 4 | `panorama_type_full360`, `panorama_type_littleplanet` |
| `timelapse_mode_*` | 5 | `timelapse_mode_timeinterval`, `timelapse_mode_suntracking` |
| `photo_hud_*` | 4 | `photo_hud_composition_score`, `photo_hud_battery` |

---

## File Layout

```
Assets/SWEF/Scripts/AdvancedPhotography/
├── AdvancedPhotographyEnums.cs
├── AdvancedPhotographyConfig.cs
├── AdvancedPhotographyData.cs
├── DroneAutonomyController.cs
├── AICompositionAssistant.cs
├── AdvancedPanoramaController.cs
├── AdvancedTimelapseController.cs
├── PhotoContestManager.cs
├── PhotoSpotDiscovery.cs
├── DronePathEditor.cs
├── AdvancedPhotographyHUD.cs
├── AdvancedPhotographyUI.cs
├── AdvancedPhotographyAnalytics.cs
└── README_AdvancedPhotography.md

Assets/Tests/EditMode/
└── AdvancedPhotographyTests.cs
```

---

## Quick Start

```csharp
// 1 — Enter drone orbit mode around a landmark
DroneAutonomyController drone = DroneAutonomyController.Instance;
drone.SetOrbitTarget(landmarkPosition, radius: 80f);
drone.SetFlightMode(DroneFlightMode.Orbit);

// 2 — Enable AI composition suggestions
AICompositionAssistant.Instance.SetAssistLevel(AIAssistLevel.Suggestions);

// Subscribe to suggestions
AICompositionAssistant.Instance.OnSuggestionUpdated += txt => hudSuggestionLabel.text = txt;

// 3 — Capture a Full360 panorama
var panorama = FindFirstObjectByType<AdvancedPanoramaController>();
panorama.OnPanoramaCaptureComplete += tex => Debug.Log($"Panorama ready: {tex.width}×{tex.height}");
panorama.StartCapture(PanoramaType.Full360);

// 4 — Start a 5-second timelapse
var timelapse = FindFirstObjectByType<AdvancedTimelapseController>();
timelapse.StartTimelapse(new TimelapseConfig { mode = TimelapseMode.TimeInterval, timeInterval = 5f });

// 5 — Build a drone path
DronePathEditor editor = FindFirstObjectByType<DronePathEditor>();
editor.AddWaypoint(new Vector3(100, 50, 0));
editor.AddWaypoint(new Vector3(200, 80, 100));
if (editor.ValidatePath())
{
    DroneFlightPath path = editor.GetFlightPath();
    drone.StartWaypointPath(path);
    drone.SetFlightMode(DroneFlightMode.Waypoint);
}
```
