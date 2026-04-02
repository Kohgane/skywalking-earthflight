# README — CompetitiveRacing System (Phase 88)

## Overview

The **Competitive Racing & Time Trial System** (`SWEF.CompetitiveRacing`) adds a
complete race course creation, management, and competition layer to SWEF.  It extends
the existing `SWEF.Racing` (Phase 62) boost/drift/slipstream mechanics with course
routing, checkpoint gates, a seasonal leaderboard, ghost racing, and full multiplayer
support.

---

## New Scripts (14 files in `Assets/SWEF/Scripts/CompetitiveRacing/`)

| # | Script | Purpose |
|---|--------|---------|
| 1 | `CompetitiveRacingEnums.cs` | `RaceMode` (6 types), `RaceStatus`, `CheckpointType`, `CourseEnvironment`, `CourseDifficulty`, `SeasonType`, `LeaderboardScope`, `RaceAlertType` |
| 2 | `CompetitiveRacingConfig.cs` | Static compile-time constants: countdown duration, max checkpoints, trigger radius, elimination interval, anti-cheat min lap time, season duration, leaderboard page size, ghost limit, course validation thresholds, medal multipliers |
| 3 | `RaceCourseData.cs` | `RaceCheckpoint` (serializable — position, gate shape, timing, bonus), `RaceCourse` (serializable — checkpoints, metadata, medals, community stats), `RaceCourseData` ScriptableObject (create via *Assets → Create → SWEF/CompetitiveRacing/Race Course Data*) |
| 4 | `RaceResultData.cs` | `CheckpointSplit` (index, elapsed, split, delta-to-best), `RaceResult` (total time, splits, PB flag, replay link), `SeasonEntry` (season window, featured courses) |
| 5 | `CourseEditorController.cs` | Interactive course builder: tap-to-place, drag-to-reposition, gate orientation, undo/redo stack, loop auto-detection, course validation, fly-through preview, medal time estimation |
| 6 | `CheckpointGateController.cs` | Runtime gate prefab: colour states (upcoming/active/captured/missed), proximity detection, wrong-way filtering, split-time floating text, VFX pulses, audio chimes |
| 7 | `RaceManager.cs` | Singleton (DontDestroyOnLoad): countdown, elapsed timer, checkpoint capture, lap management, wrong-way detection, elimination coroutine, PB tracking, leaderboard submission, flight recorder integration |
| 8 | `GhostRaceManager.cs` | Course-based ghost racing: up to 3 simultaneous ghosts (personal best / global best / friend best), per-checkpoint delta comparison, built on `SWEF.Replay.GhostRacer` |
| 9 | `CourseVisualizerRenderer.cs` | 3D world-space visualisation: Catmull-Rom spline via `LineRenderer`, gate prefab instances, direction arrows, distance labels, minimap blip registration |
| 10 | `SeasonalLeaderboardManager.cs` | Singleton: real-date season derivation (Spring/Summer/Autumn/Winter), featured courses per season, season end reward distribution, `GlobalLeaderboardService` integration |
| 11 | `CourseShareManager.cs` | Export/import `.swefcourse` JSON files, base64 share codes, `SWEF.Social.ShareManager` deep-link integration, community rating tracker |
| 12 | `RaceHUD.cs` | In-race HUD: timer, split delta, checkpoint progress slider, lap counter, speed/altitude readout, medal prediction, elimination countdown, alert banner, wrong-way warning, ghost comparison panel, compact/full toggle |
| 13 | `CompetitiveRacingUI.cs` | Pre-race menus: course browser with filters, detail card (preview, medal times, PB), mode selector, ghost picker, season overview, race results screen (splits, medal, replay save) |
| 14 | `CompetitiveRacingAnalytics.cs` | Static telemetry: `RecordRaceStart`, `RecordRaceFinish`, `RecordCheckpointSplit`, `RecordPersonalBest`, `RecordNewRecord`, `RecordCourseCreated`, `RecordCourseShared`, `RecordCourseRated` — guarded by `#if SWEF_ANALYTICS_AVAILABLE` |

---

## Architecture

```
RaceManager (Singleton, DontDestroyOnLoad)
│   ├── StartRace / PauseRace / ResumeRace / AbandonRace
│   ├── CaptureCheckpoint  →  split recording, lap advance
│   ├── FinishRace         →  PB check, leaderboard submit
│   ├── CountdownCoroutine
│   ├── EliminationCoroutine
│   └── Events: OnRaceStarted, OnCheckpointCaptured, OnLapCompleted,
│               OnRaceFinished, OnPersonalBest, OnNewRecord,
│               OnWrongWay, OnEliminated, OnRaceAlert
│
├── SeasonalLeaderboardManager  (Singleton, DontDestroyOnLoad)
│   ├── RefreshSeason  →  current SeasonEntry
│   ├── GetFeaturedCourses
│   ├── GetSeasonalLeaderboard
│   ├── AwardSeasonRewards
│   └── Events: OnSeasonChanged, OnSeasonalRewardEarned
│
├── CourseEditorController  (MonoBehaviour)
│   ├── CreateNewCourse / LoadCourse / SaveCourse / ValidateCourse
│   ├── HandleTap / BeginDrag / UpdateDrag / EndDrag
│   ├── Undo / Redo
│   ├── StartPreview / StopPreview
│   └── Events: OnCourseChanged, OnCheckpointAdded,
│               OnCheckpointRemoved, OnValidationResult
│
├── CheckpointGateController  (per-gate prefab)
│   ├── Initialise / SetAsActiveNext / MarkCaptured / MarkMissed / ResetGate
│   └── Event: OnCheckpointCaptured(checkpoint, splitTime)
│
├── GhostRaceManager  (MonoBehaviour)
│   ├── StartGhostRace(course, replays)  →  spawns GhostRacer instances
│   ├── StopAllGhosts
│   └── Events: OnGhostCheckpoint, OnGhostFinished
│
├── CourseVisualizerRenderer  (MonoBehaviour)
│   ├── BuildCourse  →  spline + gates + arrows + labels + minimap blips
│   ├── UpdateGateState
│   └── Clear
│
├── CourseShareManager  (MonoBehaviour)
│   ├── ExportCourse / ImportCourse / GenerateShareCode
│   ├── RateCourse / TrackPlay / ShareViaSocial
│   └── (integrates SWEF.Social.ShareManager)
│
├── RaceHUD  (MonoBehaviour)
│   ├── Timer, CheckpointProgress, MedalPrediction
│   ├── WrongWay banner, AlertBanner, EliminationCountdown
│   ├── GhostRaceHUD panel reference
│   └── ToggleCompactMode
│
└── CompetitiveRacingUI  (MonoBehaviour)
    ├── PopulateCourseList / ShowBrowser / ShowSeasonPanel
    ├── SelectCourse  →  ShowDetailPanel
    ├── ShowResultsScreen (splits table, medal)
    └── OnStartRacePressed  →  RaceManager.StartRace
```

---

## Integration Points

| System | Integration | Guard |
|--------|-------------|-------|
| `SWEF.Racing` (Phase 62) | Boost/drift/slipstream active during races; `BoostPadManager` can place pads on courses | Direct reference |
| `SWEF.Replay.GhostRacer` | `GhostRaceManager` spawns up to 3 `GhostRacer` instances | Direct reference |
| `SWEF.Recorder.FlightRecorder` | `RaceManager` calls `StartRecording()` / `StopRecording()` to capture ghost replay | Direct reference |
| `SWEF.Leaderboard.GlobalLeaderboardService` | `RaceManager.FinishRace()` submits score; `SeasonalLeaderboardManager` queries filtered boards | `#if SWEF_LEADERBOARD_AVAILABLE` |
| `SWEF.Minimap.MinimapManager` | `CourseVisualizerRenderer` registers/deregisters checkpoint blips | `#if SWEF_MINIMAP_AVAILABLE` |
| `SWEF.Achievement.AchievementManager` | Unlocks `first_race_finish`, `gold_medal`, `season_complete` | `#if SWEF_ACHIEVEMENT_AVAILABLE` |
| `SWEF.Analytics.TelemetryDispatcher` | `CompetitiveRacingAnalytics` dispatches all race/course events | `#if SWEF_ANALYTICS_AVAILABLE` |
| `SWEF.Social.ShareManager` | `CourseShareManager.ShareViaSocial` deep-links course codes | `#if SWEF_SOCIAL_AVAILABLE` |
| `SWEF.UI.GhostRaceHUD` | `RaceHUD` holds a `GhostRaceHUD` reference for ghost comparison panel | Direct reference |

---

## Setup Guide

1. **Create a RaceManager** — Add `RaceManager` to a persistent (DontDestroyOnLoad) scene object.
2. **Create a SeasonalLeaderboardManager** — Add to the same or another persistent object; set featured course IDs per season in the inspector.
3. **Course Editor** — Place `CourseEditorController` in your editor scene.  Set the terrain `LayerMask` and editor `Camera` reference.
4. **Gate Prefab** — Create a gate ring/arch prefab; add `CheckpointGateController`; assign `gateRenderer`, VFX particles, and `AudioSource`.  Reference this prefab in `CourseVisualizerRenderer._gatePrefab`.
5. **Course Visualizer** — Add `CourseVisualizerRenderer` to the race scene; assign `LineRenderer` and prefabs.
6. **RaceHUD** — Add `RaceHUD` to your HUD Canvas; wire up all Text/Slider/Button references.
7. **CompetitiveRacingUI** — Add `CompetitiveRacingUI` to your pre-race Canvas; call `PopulateCourseList(courses)` after loading course assets.
8. **Ghost Racing** — Add `GhostRaceManager` to the race scene; assign `_ghostRacerPrefab`.  Call `StartGhostRace(course, replays)` after `RaceManager.StartRace`.
9. **Sharing** — Add `CourseShareManager` anywhere in the scene.
10. **Compile Guards** — Define `SWEF_LEADERBOARD_AVAILABLE`, `SWEF_ANALYTICS_AVAILABLE`, `SWEF_MINIMAP_AVAILABLE`, `SWEF_ACHIEVEMENT_AVAILABLE`, and/or `SWEF_SOCIAL_AVAILABLE` in Player Settings → Scripting Define Symbols as required.

---

## Localization Keys

| Key | Default (en) |
|-----|-------------|
| `race_mode_timetrial` | Time Trial |
| `race_mode_sprint` | Sprint |
| `race_mode_circuit` | Circuit |
| `race_mode_endurance` | Endurance |
| `race_mode_relay` | Relay |
| `race_mode_elimination` | Elimination |
| `race_status_setup` | Setup |
| `race_status_countdown` | Countdown |
| `race_status_racing` | Racing |
| `race_status_paused` | Paused |
| `race_status_finished` | Finished |
| `race_status_abandoned` | Abandoned |
| `checkpoint_type_standard` | Checkpoint |
| `checkpoint_type_split` | Split |
| `checkpoint_type_sector` | Sector |
| `checkpoint_type_bonus` | Bonus |
| `checkpoint_type_penalty` | Penalty |
| `checkpoint_type_start` | Start |
| `checkpoint_type_finish` | Finish |
| `course_env_urban` | Urban |
| `course_env_mountain` | Mountain |
| `course_env_coastal` | Coastal |
| `course_env_desert` | Desert |
| `course_env_arctic` | Arctic |
| `course_env_canyon` | Canyon |
| `course_env_space` | Space |
| `course_env_mixed` | Mixed |
| `difficulty_beginner` | Beginner |
| `difficulty_intermediate` | Intermediate |
| `difficulty_advanced` | Advanced |
| `difficulty_expert` | Expert |
| `difficulty_extreme` | Extreme |
| `season_spring` | Spring |
| `season_summer` | Summer |
| `season_autumn` | Autumn |
| `season_winter` | Winter |
| `race_alert_checkpoint_missed` | Checkpoint Missed! |
| `race_alert_wrong_way` | Wrong Way! |
| `race_alert_new_pb` | New Personal Best! |
| `race_alert_new_record` | New Record! |
| `race_alert_lap_complete` | Lap Complete! |
| `race_alert_race_finished` | Race Finished! |
| `race_alert_elimination` | Eliminated! |
| `race_alert_bonus` | Bonus Checkpoint! |
| `race_hud_timer` | Time |
| `race_hud_lap` | Lap |
| `race_hud_checkpoints` | Checkpoints |
| `race_hud_speed` | Speed |
| `race_hud_altitude` | Altitude |
| `race_hud_medal_gold` | On pace for GOLD |
| `race_hud_medal_silver` | On pace for SILVER |
| `race_hud_medal_bronze` | On pace for BRONZE |
| `race_hud_no_medal` | No medal at current pace |
| `race_ui_start` | Start Race |
| `race_ui_editor` | Open Editor |
| `race_ui_save_replay` | Save Replay |
| `race_ui_season_overview` | Season Overview |
| `race_ui_featured_courses` | Featured Courses |
| `race_ui_personal_best` | Personal Best |
| `race_ui_leaderboard` | Leaderboard |

---

## Example Usage

```csharp
// ── Build and start a race ────────────────────────────────────────────────────

// 1. Load a course asset
RaceCourseData courseAsset = Resources.Load<RaceCourseData>("Courses/MountainLoop");

// 2. Start the race
RaceManager.Instance.StartRace(courseAsset.course, RaceMode.TimeTrial);

// 3. Subscribe to events
RaceManager.Instance.OnRaceFinished += result =>
{
    Debug.Log($"Finished in {result.totalTime:F2}s — PB: {result.isPersonalBest}");
};

// ── Create and share a course ─────────────────────────────────────────────────

var editor = FindFirstObjectByType<CourseEditorController>();
editor.CreateNewCourse();
// ... player places checkpoints interactively ...
editor.SaveCourse();

var shareManager = FindFirstObjectByType<CourseShareManager>();
string code = shareManager.GenerateShareCode(editor.editingCourse);

// On another device:
RaceCourse imported = shareManager.ImportCourse(code);
```
