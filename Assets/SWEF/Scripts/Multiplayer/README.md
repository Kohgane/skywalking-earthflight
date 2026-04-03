# Phase 91 — Multiplayer Expansion & Social Features

> **Namespace:** `SWEF.Multiplayer`  
> **Directory:** `Assets/SWEF/Scripts/Multiplayer/`  
> **Phase:** 91 (Final confirmed phase — Feature Freeze for v1.0)  
> **Completed:** 2026-04-03

---

## Architecture

```
MultiplayerSessionManager (Singleton, DontDestroyOnLoad)
│   ├── CreateSession / JoinSession / LeaveSession
│   ├── DiscoverPublicSessions  →  list of FlightSessionData
│   ├── StartCurrentSession  →  Lobby → InProgress
│   ├── MigrateHost  (auto host failover)
│   ├── PositionSyncLoop  (coroutine, 2 s interval)
│   └── Persistence: multiplayer_sessions.json (last 50 sessions)
│
PlayerProfileManager (Singleton, DontDestroyOnLoad)
│   ├── GetLocalProfile / UpdateLocalProfile / UpdateLocalPosition
│   ├── GetRemoteProfile / CacheProfile  (LRU cache, 100 entries)
│   ├── SyncProgressionData  →  ProgressionManager (rank, flight hours)
│   └── Persistence: player_profile.json
│
FriendSystemController (Singleton, DontDestroyOnLoad)
│   ├── AddFriend / RemoveFriend / IsFriend
│   ├── GetFriendList / GetOnlineFriends
│   ├── InviteToFlight / AcceptInvite / DeclineInvite / ReceiveInvite
│   ├── IncrementMutualFlightCount  (called at end of shared session)
│   └── Persistence: friends_list.json
│
CrossSessionEventManager (Singleton, DontDestroyOnLoad)
│   ├── GetActiveEvents / JoinEvent / LeaveEvent
│   ├── GetEventLeaderboard / CompleteCurrentEvent
│   ├── EventCheckLoop  (coroutine, 60 s interval)
│   └── Persistence: cross_session_events.json
│
EventScheduler (static)
│   ├── GetActiveEventTemplates(DateTime now)
│   ├── GetSeason / IsWeekend
│   └── Template builders: DailySpeedRun, WeekendFormation, WeeklyRally, SeasonalFestival
│
SharedWaypointManager (Singleton, DontDestroyOnLoad)
│   ├── ShareWaypoint / ImportWaypoint / GetWaypointById
│   ├── GetNearbySharedWaypoints  (Haversine distance)
│   ├── LikeWaypoint / GetPopularWaypoints / GetFriendWaypoints
│   ├── Deep link: swef://waypoint?id=xxx
│   └── Persistence: shared_waypoints.json
│
CollaborativeFlightPlanner (MonoBehaviour)
│   ├── AddWaypoint / RemoveWaypoint / ReorderWaypoint / ClearPlan
│   ├── SetLocalRole  (Planner / Follower)
│   ├── ExportPlanAsJson / ImportPlanFromJson
│   └── SendToNavigationSystem  →  SWEF.Navigation.FlightPlanManager
│
FriendFlightController (MonoBehaviour)
│   ├── FormationCheckLoop  (coroutine, 3 s interval)
│   ├── CheckFormation  →  AwardFormationXP / OnFormationFormed / OnFormationBroken
│   ├── UpdateHudMarkers  (instantiate/destroy per online friend)
│   └── StartFollowMode / StopFollowMode  (Follow Me autopilot)
│
MultiplayerChatController (MonoBehaviour)
│   ├── SendChatMessage  (profanity filter placeholder, 256 char limit)
│   ├── SendEmote  (wave / salute / barrel_roll / thumbs_up / …)
│   ├── SendLocationPing  →  lat,lon,alt as Ping message
│   ├── SendSystemAlert
│   └── Persistence: chat_history.json (last 100 messages)
│
MultiplayerBridge  (static)
│   ├── RegisterDeepLinks  →  DeepLinkHandler ("waypoint", "session")
│   ├── OnSessionCreated / OnSessionJoined  →  AddXP + achievement + social + telemetry
│   ├── OnFriendAdded  →  social_butterfly achievement + social feed
│   ├── OnEventJoined / OnEventCompleted  →  event_champion achievement + AddXP
│   ├── OnWaypointShared / OnWaypointVisited  →  waypoint_explorer achievement
│   ├── OnFormationFormed / OnFormationBroken  →  formation_master achievement
│   └── OnFlightPlanShared  →  collaborative_planner achievement
│
MultiplayerAnalytics  (static)
│   └── 14 telemetry event methods  →  TelemetryDispatcher.EnqueueEvent
```

---

## Session Lifecycle Flowchart

```
[Host]                                    [Participant]
  │                                            │
  ├─ CreateSession(type, isPublic) ──────────► │
  │      status = Lobby                        │
  │      participants = [hostId]               │
  │                                            │
  │                            DiscoverPublicSessions()
  │                                            │
  │                            JoinSession(sessionId)
  │                              participants.Add(local)
  │                                            │
  ├─ StartCurrentSession() ─────────────────► │
  │      status = InProgress                   │
  │      startTime = now                       │
  │                                            │
  │    PositionSyncLoop (every 2 s)            │
  │◄────────────────────────────────────────── │
  │                                            │
  ├─ LeaveSession() ────────────────────────► │
  │      host migrated if needed               │
  │      status = Completed if empty           │
  │      AddToHistory + SaveSessionHistory     │
  └────────────────────────────────────────── ┘
```

---

## Friend System Overview

| Operation | Method | Effect |
|-----------|--------|--------|
| Add friend | `FriendSystemController.AddFriend(profile)` | Creates FriendData, saves, awards achievement progress, posts to social feed |
| Remove friend | `RemoveFriend(friendId)` | Removes from list, saves |
| Get online | `GetOnlineFriends()` | Returns friends with status Online or InFlight |
| Invite | `InviteToFlight(friendId)` | Sends session invite (requires active session) |
| Accept invite | `AcceptInvite(sessionId)` | Calls `MultiplayerSessionManager.JoinSession` |
| Mutual flight | `IncrementMutualFlightCount(friendId)` | Increments counter on FriendData, saves |
| Profile update | `UpdateFriendProfile(profile)` | Updates cache, fires `OnFriendOnline`/`OnFriendOffline` |

---

## Cross-Session Event Types & Schedule

| Type | Recurrence | Duration | Example |
|------|-----------|----------|---------|
| `SpeedRun` | Daily (resets midnight UTC) | 24 h | Daily Speed Run Challenge |
| `FormationChallenge` | Weekend (Fri 18:00 – Sun 23:59 UTC) | ~54 h | Weekend Formation Challenge |
| `ExplorationRally` | Weekly (Fri – Sun) | 72 h | Weekly Exploration Rally |
| `SeasonalFestival` | First 7 days of Mar/Jun/Sep/Dec | 7 d | Spring/Summer/Autumn/Winter Festival |
| `AirShow` | Manual/special event | Variable | — |
| `CommunityMission` | Manual/special event | Variable | — |
| `WeatherEvent` | Triggered by NaturalDisaster system | Variable | — |

---

## Shared Waypoint Categories

| Category | Use Case |
|----------|----------|
| `Scenic` | Visually stunning real-world vista |
| `Airport` | Real or fictional airfield |
| `Challenge` | Skill-based flying challenge |
| `Custom` | Generic player-defined POI |
| `Event` | Tied to an active cross-session event |

---

## Formation Flight Rules

- **Detection radius:** 500 m (configurable in inspector)
- **Check interval:** every 3 seconds
- **XP reward:** 10 XP per check interval per friend in formation
- **Formation formed** when ≥ 1 friend is within radius
- **Formation broken** when all friends leave radius
- **Follow Me mode** — autopilot trails a target friend at a configurable speed (default 80 m/s)

---

## Communication System

### Message Types

| Type | Trigger | Payload |
|------|---------|---------|
| `Chat` | `SendChatMessage(text)` | Filtered plain text (max 256 chars) |
| `Emote` | `SendEmote(name)` | Emote key: wave, salute, barrel_roll, thumbs_up, thumbs_down, shrug, point, heart |
| `Ping` | `SendLocationPing()` | `"lat,lon,alt"` string |
| `SystemAlert` | `SendSystemAlert(text)` | System-generated notification |
| `FlightInvite` | `FriendSystemController.InviteToFlight` | Session ID |
| `WaypointShare` | Inline from SharedWaypointManager | Waypoint ID |

### Profanity Filter
A placeholder filter exists in `MultiplayerChatController.ApplyProfanityFilter`.
Replace with a word-list loaded from `Resources/Localization/profanity_*.txt` before release.

---

## Persistence Files

| File | Contents | Max Size |
|------|----------|----------|
| `player_profile.json` | Local player profile (single record) | ~1 KB |
| `friends_list.json` | All friends + cached profiles | ~100 KB |
| `multiplayer_sessions.json` | Session history (last 50) | ~50 KB |
| `cross_session_events.json` | Community events | ~20 KB |
| `shared_waypoints.json` | All imported/shared waypoints | ~500 KB |
| `chat_history.json` | Last 100 messages | ~50 KB |

All files are written to `Application.persistentDataPath` using `JsonUtility`.

---

## Integration Points

| Dependency | Integration | Guard |
|------------|-------------|-------|
| `SWEF.Progression.ProgressionManager` | `AddXP` for sessions, events, formations, sharing | `#if SWEF_PROGRESSION_AVAILABLE` |
| `SWEF.Achievement.AchievementManager` | `ReportProgress` for 7 achievements (see below) | `#if SWEF_ACHIEVEMENT_AVAILABLE` |
| `SWEF.SocialHub.SocialActivityFeed` | `PostActivity` for sessions, events, waypoints, friends | `#if SWEF_SOCIAL_AVAILABLE` |
| `SWEF.Analytics.TelemetryDispatcher` | 14 event types via `MultiplayerAnalytics` | `#if SWEF_ANALYTICS_AVAILABLE` |
| `SWEF.Core.DeepLinkHandler` | Routes for `waypoint` and `session` | `#if SWEF_DEEPLINK_AVAILABLE` |
| `SWEF.Navigation.FlightPlanManager` | `AddWaypointFromMultiplayer` from CollaborativeFlightPlanner | `#if SWEF_NAVIGATION_AVAILABLE` |

### Achievement Keys

| Key | Trigger |
|-----|---------|
| `first_multiplayer_flight` | First session created or joined |
| `formation_master` | Formation formed (cumulative) |
| `social_butterfly` | 10 friends added (cumulative) |
| `event_champion` | Cross-session event completed |
| `waypoint_explorer` | 50 shared waypoints visited/imported |
| `collaborative_planner` | Collaborative flight plan shared |
| `chat_veteran` | Messages sent (cumulative) |

---

## Deep Link Routes Added

| Route | Format | Handler |
|-------|--------|---------|
| `swef://waypoint?id=<guid>` | GUID of a shared waypoint | `SharedWaypointManager` |
| `swef://session?id=<guid>` | GUID of a public session | `MultiplayerSessionManager.JoinSession` |

---

## Localization Key Prefix

All UI strings should use the prefix `multiplayer_`. Example keys:

```
multiplayer_session_created
multiplayer_session_joined
multiplayer_session_left
multiplayer_friend_added
multiplayer_friend_removed
multiplayer_event_joined
multiplayer_event_completed
multiplayer_waypoint_shared
multiplayer_waypoint_liked
multiplayer_formation_formed
multiplayer_formation_broken
multiplayer_chat_placeholder
multiplayer_emote_wave
multiplayer_emote_salute
multiplayer_emote_barrel_roll
multiplayer_emote_thumbs_up
multiplayer_emote_thumbs_down
multiplayer_emote_shrug
multiplayer_emote_point
multiplayer_emote_heart
multiplayer_status_online
multiplayer_status_inflight
multiplayer_status_inworkshop
multiplayer_status_offline
multiplayer_role_planner
multiplayer_role_follower
multiplayer_invite_received
multiplayer_invite_accepted
multiplayer_invite_declined
multiplayer_session_full
multiplayer_session_not_found
```
