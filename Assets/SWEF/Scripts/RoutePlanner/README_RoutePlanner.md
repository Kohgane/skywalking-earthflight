# Route Planner & Navigation System

**Namespace:** `SWEF.RoutePlanner`  
**Directory:** `Assets/SWEF/Scripts/RoutePlanner/`  
**Phase:** 49 — Dynamic Route Planning & Navigation System

---

## Overview

The Route Planner & Navigation System lets players design multi-waypoint flight routes, fly them with real-time HUD guidance, visualise the path in 3D, and receive smart route recommendations. The system is split into focused, independently reusable components.

---

## Scripts

| File | Class | Purpose |
|------|-------|---------|
| `RoutePlannerData.cs` | (data only) | All enums and serialisable data classes shared by the system |
| `RoutePlannerManager.cs` | `RoutePlannerManager` | Singleton — route CRUD, navigation state machine, waypoint triggering, ETA, off-path detection |
| `NavigationController.cs` | `NavigationController` | Real-time navigation: arrival detection, auto-advance, distance/bearing/ETA updates, loop support |
| `NavigationHUDController.cs` | `NavigationHUDController` | Screen-space HUD: direction arrow, distance, ETA, compass strip, altitude guide, progress bar, off-route warning |
| `RouteVisualizerRenderer.cs` | `RouteVisualizerRenderer` | 3D LineRenderer path with colour coding, waypoint markers, labels, distance labels, flow animation, distance fade |
| `RoutePlannerUI.cs` | `RoutePlannerUI` | In-game UI panel for creating, editing, saving, and loading routes |
| `AutoRouteRecommender.cs` | `AutoRouteRecommender` | Scoring-based recommendation engine: proximity, difficulty, time-of-day, weather, unexplored, popularity |
| `WaypointInteractionHandler.cs` | `WaypointInteractionHandler` | Proximity glow, arrival celebration, checkpoint save, info popup, missed-waypoint detection, breadcrumbs |
| `RouteShareManager.cs` | `RouteShareManager` | Export/import routes as compressed JSON, share codes, clipboard, rating, download tracking |
| `NavigationAudioManager.cs` | `NavigationAudioManager` | Voice/chime cues, spatial audio, cooldown, mute/unmute, priority system |
| `RouteBuilderController.cs` | `RouteBuilderController` | Interactive builder: tap-to-place, undo/redo, landmark snapping, route validation |
| `RoutePathRenderer.cs` | `RoutePathRenderer` | Spline-based path preview used during route building |
| `RouteNavigationHUD.cs` | `RouteNavigationHUD` | Legacy HUD widget integrated with `RoutePlannerManager` |
| `RouteRecommendationEngine.cs` | `RouteRecommendationEngine` | Legacy recommendation engine used by `RoutePlannerManager` |
| `RouteShareManager.cs` | `RouteShareManager` | Sharing utilities: export/import, share codes |
| `RouteStorageManager.cs` | `RouteStorageManager` | JSON persistence: save/load routes to `persistentDataPath/Routes/` |
| `RoutePlannerAnalytics.cs` | `RoutePlannerAnalytics` | Analytics events for route creation, navigation, and abandonment |

---

## Key Data Types (`RoutePlannerData.cs`)

### Enums

| Enum | Values |
|------|--------|
| `WaypointType` | Standard, Landmark, Photo, Checkpoint, Start, Finish, RestStop, HiddenGem, Altitude, SpeedGate |
| `RouteStatus` | Draft, Ready, InProgress, Completed, Abandoned |
| `RouteType` | Scenic, Speed, Exploration, Challenge, Tour, Custom, Race, Photography |
| `NavigationStyle` | FreeFollow, StrictPath, TimeAttack, Relaxed |
| `RouteVisibility` | Private, FriendsOnly, Public |
| `NavigationMode` | FreeRoam, GuidedRoute, AutoPilotAssist, RacingMode |
| `RouteCategory` | Scenic, Speed, Challenge, Exploration, Training, Custom |
| `RouteDifficulty` | Beginner, Intermediate, Advanced, Expert |

### Serialisable Classes

| Class | Purpose |
|-------|---------|
| `RouteWaypoint` | Lat/lon waypoint with trigger radius, optional conditions, narration link |
| `FlightRoute` | Complete route: waypoints, metadata, community stats, navigation style |
| `RouteProgress` | Live navigation session state |
| `RoutePlannerConfig` | Persistent user settings |
| `Waypoint` | World-space (Vector3) waypoint used by NavigationController and visualiser |
| `NavigationSettings` | HUD toggles, arrival radius, voice guidance, off-route threshold |
| `RouteRecommendation` | Scored route suggestion with reason and tags |

---

## Architecture

```
RoutePlannerManager (singleton)
    │
    ├── RouteStorageManager      — JSON save/load
    ├── RouteRecommendationEngine — legacy recommender
    ├── RoutePlannerAnalytics     — analytics events
    │
NavigationController              — frame-level guidance logic
    │
    ├── NavigationHUDController  — drives all HUD elements
    ├── RouteVisualizerRenderer  — 3D world-space path
    ├── WaypointInteractionHandler — proximity/celebration/breadcrumbs
    └── NavigationAudioManager    — voice/chime/spatial audio

AutoRouteRecommender              — standalone scoring engine
RoutePlannerUI                    — builder & route browser UI
RouteBuilderController            — interactive waypoint placement
RouteShareManager                 — export/import/share codes
```

---

## Integration Points

| System | Integration |
|--------|-------------|
| `SWEF.GuidedTour` | `WaypointNavigator.SetManualTarget` can be pointed at a route waypoint |
| `SWEF.Minimap` | `RoutePlannerManager` notifies minimap to render the active route |
| `SWEF.Narration` | `RouteWaypoint.narrationId` triggers a narration clip on arrival |
| `SWEF.HiddenGems` | Waypoints of type `HiddenGem` link to the HiddenGem discovery system |
| `SWEF.SaveSystem` | Checkpoint waypoints call `PlayerPrefs` save; full progress can be hooked to `SaveManager` |

---

## Quick Start

1. Add **RoutePlannerManager** to your bootstrap scene's persistent GameObject.
2. Add **NavigationController**, **NavigationHUDController**, **RouteVisualizerRenderer**, **WaypointInteractionHandler**, and **NavigationAudioManager** to scene GameObjects.
3. Wire inspector references (or let them auto-find via `FindFirstObjectByType`).
4. Create a route via `RoutePlannerManager.Instance.CreateRoute("My Route")`, add `RouteWaypoint` objects, then call `StartNavigation(route)`.
5. The HUD, visualiser, audio, and interaction handlers respond to `NavigationController` events automatically.

---

## No Third-Party Dependencies

All scripts use only Unity built-in APIs (`UnityEngine`, `UnityEngine.UI`, `System`). No external packages are required.
