# SWEF Security System — Phase 92

**Namespace:** `SWEF.Security`  
**Directory:** `Assets/SWEF/Scripts/Security/`

---

## Architecture

```
Security/
├── Data Models
│   ├── SecurityEventData.cs      — Serializable event record (type, severity, player, action)
│   ├── SecurityConfig.cs         — All configurable thresholds and limits
│   └── ValidationResult.cs       — Struct: isValid, violations[], correctedValue
│
├── Save File Integrity
│   ├── SaveFileValidator.cs      — SHA-256 checksum + HMAC signing, backup/restore
│   └── SaveFileEncryptor.cs      — AES-256-CBC encryption / decryption with PBKDF2 key derivation
│
├── Runtime Cheat Detection
│   ├── CheatDetectionManager.cs  — Singleton coordinator; periodic integrity sweeps
│   ├── SpeedHackDetector.cs      — Time-scale and physics-step anomaly detection
│   ├── PositionValidator.cs      — Ring-buffer position history; teleport jump detection
│   └── CurrencyValidator.cs      — Transaction log; balance reconciliation
│
├── Multiplayer Security
│   ├── MultiplayerSecurityController.cs — Packet validation, rate limiting, replay prevention
│   └── RateLimiter.cs            — Sliding-window rate limiter with backoff
│
├── Data Validation
│   ├── InputSanitizer.cs         — Display names, chat, waypoints, coordinates, build data
│   └── ProfanityFilter.cs        — Word-list filter with leet-speak normalisation
│
├── Logging & Reporting
│   ├── SecurityLogger.cs         — Rolling JSON log (max 1 000 entries)
│   └── SecurityAnalytics.cs      — Telemetry wrapper (8 event types)
│
├── Integration
│   └── SecurityBridge.cs         — Static bridge to Multiplayer, Achievement, Social, Telemetry
│
└── README.md                     ← You are here
```

---

## Threat Model

| Threat | Covered By | Severity |
|--------|-----------|----------|
| Save file editing outside the game | `SaveFileValidator` HMAC | Critical |
| Time-scale / speed hack | `SpeedHackDetector` | High |
| Teleport / position injection | `PositionValidator` | High |
| Currency injection | `CurrencyValidator` + `CheatDetectionManager` | Critical |
| XP farming / injection | `CheatDetectionManager.ReportXpGain` | High |
| Part duplication | `InputSanitizer.ValidateBuildData` + `SecurityBridge` | Medium |
| Chat spam | `RateLimiter` (5 msg/s) | Low |
| Position packet flood | `RateLimiter` (30 pkt/s) | Medium |
| Action packet flood | `RateLimiter` (10 pkt/s) | Medium |
| Replay attacks | `MultiplayerSecurityController` sequence + timestamp window | High |
| Malformed packets | `MultiplayerSecurityController.ValidatePacket` | Medium |
| Profanity / HTML injection | `InputSanitizer` + `ProfanityFilter` | Low |

---

## Save File Integrity Flow

```
WRITE:
  SecurityBridge.OnBeforeSave(path)   → SaveFileValidator.CreateBackup
  [write JSON payload]
  SecurityBridge.OnAfterSave(path)    → SaveFileValidator.SignSaveFile (appends HMAC)

READ:
  SecurityBridge.OnBeforeLoad(path)   → SaveFileValidator.DetectTampering
    ├── OK  → proceed
    └── FAIL → RestoreFromBackup → SecurityAnalytics.RecordBackupRestored
                                 → SecurityLogger.LogEvent (SaveTamper/Critical)
```

The HMAC is appended as an HTML comment footer so it is ignored by `JsonUtility.FromJson`:

```
{ ...json payload... }
<!-- SWEF_HMAC:a3f9c2... -->
```

---

## Encryption

`SaveFileEncryptor` uses **AES-256-CBC** with a key derived by PBKDF2 (10 000 iterations, SHA-1):

| Input | Value |
|-------|-------|
| Password | `SystemInfo.deviceUniqueIdentifier + "SWEF_AES_APP_SECRET_v1"` |
| Salt | SHA-256 of `"SWEF_SALT:" + password` (first 16 bytes) |
| PBKDF2 hash | SHA-256 (explicitly specified via `HashAlgorithmName.SHA256`) |
| Key size | 256-bit |
| IV | Randomly generated per call, prepended to cipher-text |
| Output format | Base-64 (`[IV(16)] + [cipher-text]`) |

If decryption fails the caller receives `null` and should call `SaveFileValidator.RestoreFromBackup`.

---

## Cheat Detection Thresholds

| Detection | Default Threshold | Config Field |
|-----------|-------------------|-------------|
| Max aircraft speed | 3 000 m/s | `maxSpeedThreshold` |
| Speed tolerance multiplier | 1.5× | `speedToleranceMultiplier` |
| Time-scale anomaly delta | 0.05 | `timeScaleAnomalyThreshold` |
| Max teleport per tick | 5 000 m | `maxTeleportDistancePerTick` |
| Teleport tolerance multiplier | 2.0× | `teleportToleranceMultiplier` |
| Max XP per minute | 5 000 | `maxXpGainPerMinute` |
| Max currency per minute | 10 000 | `maxCurrencyGainPerMinute` |
| Integrity check interval | 30 s | `integrityCheckIntervalSeconds` |

---

## Rate Limiting Config

| Action | Default Limit | Window | Config Field |
|--------|--------------|--------|-------------|
| Chat messages | 5 / s | 1 s | `chatRateLimitPerSecond` |
| Position updates | 30 / s | 1 s | `positionRateLimitPerSecond` |
| Generic actions | 10 / s | 1 s | `actionRateLimitPerSecond` |
| Backoff escalation | 3 violations → 2× window | — | `RateLimiter.OffenderThreshold` |
| Max backoff multiplier | 8× | — | `RateLimiter.MaxBackoffMultiplier` |

---

## Multiplayer Security Measures

1. **Packet validation** — every packet must have a non-empty sender ID, message type, and payload.
2. **Timestamp window** — packets older or newer than ±30 s are rejected.
3. **Sequence numbers** — each unique sequence number is recorded per player; duplicates are rejected as replay attacks.
4. **Rate limiting** — per-player sliding-window quotas on chat, position, and action packets.
5. **Player state plausibility** — reported speed and altitude checked against configured limits.
6. **Kick / Ban** — `MultiplayerSecurityController.KickPlayer` / `BanPlayer` with tiered ban durations.

---

## Persistence Files Monitored

| File | Contents | Critical |
|------|----------|---------|
| `player_profile.json` | Local player profile | ✅ |
| `friends_list.json` | Friend list | ✅ |
| `multiplayer_sessions.json` | Session history | ✅ |
| `cross_session_events.json` | Community events | ✅ |
| `shared_waypoints.json` | Shared waypoints | ✅ |
| `chat_history.json` | Recent chat | — |
| `workshop_builds.json` | Aircraft build presets | ✅ |
| `workshop_inventory.json` | Unlocked parts | ✅ |
| `progression_data.json` | XP / rank / currency | ✅ |
| `achievement_data.json` | Achievement state | ✅ |
| `flight_journal.json` | Flight records | — |
| `settings.json` | App settings | — |
| `daily_challenge.json` | Challenge state | — |
| `race_leaderboards.json` | Race scores | ✅ |
| `photo_contest.json` | Photo contest entries | — |

---

## Integration Points

| System | Integration | Guard |
|--------|-------------|-------|
| `SWEF.Multiplayer.MultiplayerSessionManager` | Kick/ban participants | `#if SWEF_MULTIPLAYER_AVAILABLE` |
| `SWEF.Achievement.AchievementManager` | `ReportProgress("clean_record", n)` | `#if SWEF_ACHIEVEMENT_AVAILABLE` |
| `SWEF.SocialHub.SocialActivityFeed` | Admin notifications | `#if SWEF_SOCIAL_AVAILABLE` |
| `SWEF.Analytics.TelemetryDispatcher` | 8 telemetry events via `SecurityAnalytics` | `#if SWEF_ANALYTICS_AVAILABLE` |
| `SWEF.Workshop.WorkshopManager` | `ValidateBuildData` via `SecurityBridge` | `#if SWEF_WORKSHOP_AVAILABLE` |
| `SWEF.Progression.ProgressionManager` | `OnXpGain`, `OnCurrencyChange` via `SecurityBridge` | `#if SWEF_PROGRESSION_AVAILABLE` |
| `SWEF.Multiplayer.FriendSystemController` | `SanitizeDisplayName` via `SecurityBridge` | — |

---

## Localization Prefix

All user-visible security strings use the prefix `security_`.

| Key | Usage |
|-----|-------|
| `security_save_tamper_warning` | Toast shown when save is restored from backup |
| `security_rate_limit_warning` | In-game message when chat/action is rate-limited |
| `security_cheat_warning` | Generic cheat-detected notification |
| `security_kicked` | Kick notification |
| `security_banned` | Ban notification |

---

## Usage Example

```csharp
// Before saving any JSON file:
SecurityBridge.OnBeforeSave(savePath);
File.WriteAllText(savePath, json);
SecurityBridge.OnAfterSave(savePath);

// Before loading any JSON file:
if (!SecurityBridge.OnBeforeLoad(savePath))
    json = File.ReadAllText(savePath); // re-read the restored backup

// When awarding XP:
SecurityBridge.OnXpGain(xpAmount);

// When modifying currency:
float validated = SecurityBridge.OnCurrencyChange(delta, "mission_reward");
```
