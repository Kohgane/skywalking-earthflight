# Phase 79 — Flight Replay Theater Enhancement

Extends the Phase 48 Replay system with a full non-linear editing suite, cinematic camera tooling, music mixing, post-process effects, multi-platform export, and social sharing — all surfaced through a redesigned `ReplayTheaterUI`.

---

## Architecture Overview

The ten Phase 79 scripts layer on top of the Phase 48 `ReplayData` / `ReplayFileManager` foundation.  A central **`ReplayEditorManager`** owns the editing session and coordinates four specialist subsystems (clip editing, transitions, effects, music), two pipeline stages (export, sharing), a persistent data model (`ReplayTheaterData`), an analytics bridge, and the UI shell.

```
ReplayEditorManager  (session owner, undo/redo stack)
│
├── ReplayTheaterData       — serialisable project model
├── ReplayClipEditor        — cut / trim / split / duplicate
├── ReplayTransitionSystem  — fade / dissolve / wipe / zoom / slide
├── ReplayEffectsProcessor  — speed ramp, colour grade, vignette, bloom, grain, PiP
├── ReplayMusicMixer        — beat-synced music track with fade envelope
│
├── ReplayExportManager     — render pipeline → file (MP4 / WebM / GIF)
├── ReplaySharingHub        — link generation and platform dispatch
│
├── ReplayTheaterUI         — editor canvas, timeline scrubber, clip inspector
└── ReplayTheaterAnalytics  — view / like / share telemetry
```

---

## Scripts

| # | Script | Purpose | Key Public API |
|---|--------|---------|----------------|
| 1 | `ReplayTheaterData.cs` | Serialisable project model — holds the ordered clip list, per-clip metadata (in/out points, speed, colour grade, transition), music strip, export settings, and sharing metadata | `ReplayTheaterProject` (serialisable class); `ReplayClipData`, `ReplayMusicData`, `ReplayExportSettings`, `ReplaySharingMetadata` nested types |
| 2 | `ReplayEditorManager.cs` | Session owner and undo/redo coordinator — opens / saves / closes projects, drives playback preview, dispatches commands to subsystems, maintains a `CommandHistory` stack | `OpenProject(string path)`, `SaveProject()`, `CloseProject()`, `Undo()`, `Redo()`, `Preview(float time)` |
| 3 | `ReplayClipEditor.cs` | Non-linear clip operations — add, remove, split at playhead, trim in/out handles, duplicate, copy/paste with clipboard | `AddClip(ReplayClipData)`, `RemoveClip(int idx)`, `SplitClip(int idx, float time)`, `TrimClip(int idx, float inPt, float outPt)`, `DuplicateClip(int idx)`, `CopyClip(int idx)`, `PasteClip(int afterIdx)` |
| 4 | `ReplayTransitionSystem.cs` | Inter-clip transition rendering — Fade, Cross Dissolve, Wipe (left/right/up/down), Zoom, Slide; configurable duration and easing | `SetTransition(int clipIdx, TransitionType type, float duration)`, `RemoveTransition(int clipIdx)`, `RenderTransition(RenderTexture a, RenderTexture b, float t, TransitionType type)` |
| 5 | `ReplayEffectsProcessor.cs` | Per-clip and global post-process effects — slow motion / fast forward speed ramp, cinematic / vintage / dramatic / vivid / monochrome colour grades, vignette, bloom, film grain, picture-in-picture overlay | `SetSpeed(int clipIdx, float multiplier)`, `SetColorGrade(int clipIdx, ColorGradePreset preset)`, `SetVignette(int clipIdx, float intensity)`, `SetBloom(float threshold, float intensity)`, `SetGrain(float intensity)`, `SetPiP(int clipIdx, Rect rect)` |
| 6 | `ReplayMusicMixer.cs` | Beat-synced background music strip — loads AudioClip, places beat markers, applies fade-in / fade-out envelopes, mixes with flight audio at configurable volume | `LoadTrack(AudioClip clip)`, `SetVolume(float volume)`, `SetFadeIn(float duration)`, `SetFadeOut(float duration)`, `AddBeatMarker(float time)`, `RemoveBeatMarker(int idx)`, `GetMixedAudio(float time)` |
| 7 | `ReplayExportManager.cs` | Render-to-file pipeline — frame-accurate capture via `ScreenCapture`, format selection (MP4 / WebM / GIF), quality / framerate / watermark / HUD-inclusion settings, async export coroutine with progress reporting | `StartExport(ReplayExportSettings settings)`, `CancelExport()`, `OnProgress` (event, 0–1), `OnComplete` (event), `OnFailed` (event) |
| 8 | `ReplaySharingHub.cs` | Link generation and platform dispatch — produces shareable URLs, routes to Direct Link / Social Media / In-Game / Cloud Save platforms, enforces privacy levels (Public / Friends Only / Private) | `GenerateLink(ReplayTheaterProject project)`, `Share(string link, SharePlatform platform, PrivacyLevel privacy)`, `GetShareMetadata(string link)` |
| 9 | `ReplayTheaterUI.cs` | Full editor canvas — timeline scrubber with per-track lanes (Video / Audio / Effects / Music), clip inspector panel, transition picker, effects stack sidebar, export/share dialogs, undo/redo toolbar | `Open(ReplayTheaterProject project)`, `Close()`, `RefreshTimeline()`, `ShowExportDialog()`, `ShowShareDialog()` |
| 10 | `ReplayTheaterAnalytics.cs` | Telemetry bridge — records view count, like count, share count per project; integrates with `SWEF.Analytics.TelemetryDispatcher` (null-safe, `#define SWEF_ANALYTICS_AVAILABLE`) | `RecordView(string projectId)`, `RecordLike(string projectId)`, `RecordShare(string projectId, SharePlatform platform)`, `GetStats(string projectId) → ReplayAnalyticsStats` |

---

## Key Data Types

| Type | Kind | Purpose |
|------|------|---------|
| `ReplayTheaterProject` | class | Top-level serialisable container for a Replay Theater editing session |
| `ReplayClipData` | class | Per-clip in/out points, speed multiplier, colour grade, transition reference |
| `TransitionType` | enum | None / Fade / CrossDissolve / Wipe / Zoom / Slide |
| `ColorGradePreset` | enum | None / Cinematic / Vintage / Dramatic / Vivid / Monochrome |
| `ReplayMusicData` | class | AudioClip reference, volume, fade envelope, beat marker list |
| `ReplayExportSettings` | class | Format (MP4/WebM/GIF), quality level, framerate, watermark flag, HUD-inclusion flag |
| `ReplaySharingMetadata` | class | Link URL, platform, privacy level, timestamp |
| `SharePlatform` | enum | DirectLink / SocialMedia / InGame / CloudSave |
| `PrivacyLevel` | enum | Public / FriendsOnly / Private |
| `ReplayAnalyticsStats` | class | Views, likes, shares counts for a project |
| `CommandHistory` | class | Undo/redo stack of `IReplayCommand` operations |

---

## Integration with Phase 48 Replay System

Phase 79 is a consumer of Phase 48 — it never modifies source replay files.

| Phase 48 Type | How Phase 79 Uses It |
|--------------|----------------------|
| `ReplayData` | Read-only source for each `ReplayClipData` in-point / out-point range |
| `ReplayFileManager` | `LoadReplay(string path)` called by `ReplayEditorManager.OpenProject()` to hydrate clip sources |
| `GhostRacer` | Optional PiP source — `ReplayEffectsProcessor.SetPiP()` can render a ghost overlay |
| `FlightPathRenderer` | Can be embedded in the timeline preview window as a map layer |
| `ReplayShareManager` | Phase 48 share primitives extended by `ReplaySharingHub` for new platform targets |

Guard pattern used throughout:

```csharp
#if SWEF_REPLAY_AVAILABLE
    var data = ReplayFileManager.Instance?.LoadReplay(path);
#endif
```

---

## Setup Instructions

1. **Import scripts** — copy all ten `.cs` files into `Assets/SWEF/Scripts/ReplayTheater/`.
2. **Scene setup** — add `ReplayEditorManager` as a `DontDestroyOnLoad` singleton via `ReplayEditorManager.Instance`.
3. **UI prefab** — attach `ReplayTheaterUI` to the editor canvas prefab under `Assets/SWEF/Prefabs/ReplayTheater/`.
4. **Define guards** — in *Project Settings → Player → Scripting Define Symbols* add `SWEF_REPLAY_AVAILABLE` (Phase 48) and optionally `SWEF_ANALYTICS_AVAILABLE` (Phase 29).
5. **Localization** — 63 `replay_theater_*` keys are pre-loaded into all 8 language JSON files under `Assets/SWEF/Resources/Localization/`.
6. **Export prerequisites** — `ReplayExportManager` requires write access to `Application.persistentDataPath`; no third-party encoder is needed for GIF output (built-in).  MP4 / WebM encoding delegates to Unity's `MovieTexture` API or a drop-in encoder plugin.
7. **Test** — open the *Replay Theater* demo scene at `Assets/SWEF/Scenes/ReplayTheaterDemo.unity`, press Play, and use **File → Load Project** to import a Phase 48 `.replay` file.
