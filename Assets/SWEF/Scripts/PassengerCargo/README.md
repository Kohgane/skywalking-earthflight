# Phase 82 — Passenger & Cargo Mission System

**Namespace:** `SWEF.PassengerCargo`  
**Directory:** `Assets/SWEF/Scripts/PassengerCargo/`

---

## Overview

A comprehensive passenger and cargo transport mission system. Players accept
transport contracts (passenger flights, cargo deliveries, VIP escorts), manage
payload weight that affects flight physics, keep passengers comfortable during
flight, and earn rewards on successful delivery.

---

## Scripts

| # | File | Purpose |
|---|------|---------|
| 1 | `PassengerCargoData.cs` | Enums, data classes, `TransportContract` ScriptableObject, `DeliveryResult` |
| 2 | `PassengerComfortSystem.cs` | Singleton — real-time comfort scoring (G-force, turbulence, altitude, pressure, noise) |
| 3 | `CargoPhysicsController.cs` | Singleton — cargo weight tracking, damage monitoring |
| 4 | `TransportMissionManager.cs` | Singleton — full mission lifecycle with JSON persistence |
| 5 | `TransportContractGenerator.cs` | Static utility — procedural contract generation |
| 6 | `PassengerBehaviorController.cs` | Passenger reaction state machine |
| 7 | `DeliveryTimerController.cs` | Singleton — countdown timer with Green/Yellow/Red/Overtime phases |
| 8 | `TransportRewardCalculator.cs` | Static utility — reward and star-rating calculation |
| 9 | `TransportMissionHUD.cs` | In-flight HUD overlay |
| 10 | `TransportMissionUI.cs` | Full-screen contract board |
| 11 | `TransportMissionBridge.cs` | Integration bridge to Progression, Achievement, Social systems |
| 12 | `TransportAnalytics.cs` | Telemetry event dispatch (9 event types) |

---

## Architecture

```
TransportMissionManager (Singleton, DontDestroyOnLoad)
│   ├── AcceptContract(TransportContract)  → Accepted
│   ├── BeginMission()                     → InFlight
│   ├── CompleteMission()                  → Completed + DeliveryResult
│   ├── AbandonMission()                   → Idle (with penalty)
│   └── JSON persistence: transport_active.json
│
PassengerComfortSystem (Singleton)
│   ├── G-Force (30 %) — via FlightPhysicsIntegrator.OnPhysicsSnapshot
│   ├── Turbulence (25 %) — via WeatherFlightModifier.TurbulenceIntensity
│   ├── Bank Angle Rate (15 %) — derived from FlightController transform
│   ├── Altitude Rate (15 %) — derived from AltitudeController
│   ├── Cabin Pressure (10 %) — altitude-dependent
│   └── Noise (5 %) — speed-based estimate
│
CargoPhysicsController (Singleton)
│   ├── TotalMassKg, FuelMultiplier, CGShiftMetres (exposed as properties)
│   └── Fragile cargo damage tracking (G-force + turbulence)
│
TransportContractGenerator (Static)
│   ├── GenerateContracts(count, playerPos, pilotRank)
│   ├── Rank-gated difficulty (VIP at rank 5+, hazardous at rank 3+)
│   └── Storm-triggered emergency medical missions
│
TransportRewardCalculator (Static)
│   └── CalculateResult → XP, Coins, star rating (1–5)
│
DeliveryTimerController (Singleton)
│   └── Green → Yellow → Red → Overtime phase transitions
│
TransportMissionBridge
│   ├── → ProgressionManager.AddXP
│   ├── → AchievementManager.ReportProgress
│   └── → SocialActivityFeed.PostActivity
│
TransportAnalytics
│   └── → TelemetryDispatcher.EnqueueEvent (9 event types)
```

---

## Mission Lifecycle

```
Idle → Accepted → Loading → InFlight → Approaching → Delivered
                                                       ↓
                                                  Completed ← CompleteMission()
                                                  Failed    ← FailMission()
Any → Abandoned ← AbandonMission()
```

---

## Comfort Scoring

| Factor | Weight | Full-Penalty Threshold |
|--------|--------|----------------------|
| G-Force | 30 % | > 2.5 G |
| Turbulence | 25 % | Intensity 1.0 |
| Bank Angle | 15 % | 45 ° instantaneous |
| Altitude Rate | 15 % | 20 m/s |
| Cabin Pressure | 10 % | > 12 000 m |
| Noise | 5 % | 350 m/s |

Recovery: **+2 comfort/s** during smooth flight.  
Decay: **−5 to −20 comfort/s** depending on severity.

---

## Reward Multipliers

| Factor | Multiplier |
|--------|-----------|
| Comfort Excellent (≥ 90) | × 2.0 |
| Comfort Good (70–89) | × 1.5 |
| Comfort Fair (50–69) | × 1.0 |
| Comfort Poor (30–49) | × 0.5 |
| Comfort Critical (< 30) | × 0.25 |
| Early delivery | up to +50 % |
| Overtime | −25 % |
| Per 10 % cargo damage | −10 % |
| VIP mission | × 1.5 |
| Streak ≥ 5 | × 1.25 |

---

## Star Rating

| Stars | Composite Score |
|-------|----------------|
| ⭐⭐⭐⭐⭐ | ≥ 90 % |
| ⭐⭐⭐⭐ | 75–89 % |
| ⭐⭐⭐ | 55–74 % |
| ⭐⭐ | 35–54 % |
| ⭐ | < 35 % |

---

## Persistence

| File | Contents |
|------|---------|
| `transport_active.json` | Active contract metadata |
| `transport_history.json` | Completed delivery history |
| `transport_stats.json` | Cumulative statistics (deliveries, average rating, streak) |

---

## Localization Keys (30)

All keys use the prefix `transport_`. See `lang_en.json` for English values.

**Mission Types (8):** `transport_type_passenger_standard`, `_vip`, `_charter`,
`transport_type_cargo_standard`, `_fragile`, `_hazardous`, `_oversized`,
`transport_type_emergency_medical`

**Comfort Levels (5):** `transport_comfort_excellent/good/fair/poor/critical`

**Cargo Categories (7):** `transport_cargo_general/perishable/fragile/hazardous/livestock/oversized/medical`

**UI (10):** `transport_board_title`, `transport_accept_contract`,
`transport_decline_contract`, `transport_mission_active`,
`transport_delivery_complete`, `transport_mission_failed`,
`transport_time_bonus`, `transport_comfort_bonus`,
`transport_history_title`, `transport_stats_title`

---

## Integration Points

| This System | External Class | How |
|-------------|---------------|-----|
| `PassengerComfortSystem` | `FlightPhysicsIntegrator` | Subscribe to `OnPhysicsSnapshot` |
| `PassengerComfortSystem` | `WeatherFlightModifier` | `TurbulenceIntensity` |
| `PassengerComfortSystem` | `AltitudeController` | `CurrentAltitudeMeters` |
| `CargoPhysicsController` | `FlightPhysicsIntegrator` | Subscribe to `OnPhysicsSnapshot` |
| `TransportMissionManager` | `LandingDetector` | Subscribe to `OnTouchdown` |
| `TransportMissionManager` | `AirportRegistry` | `GetNearestAirport`, `GetAirportById` |
| `TransportMissionManager` | `RoutePlannerManager` | `CreateRoute`, `StartNavigation` |
| `TransportContractGenerator` | `AirportRegistry` | `GetAirportsInRange` |
| `TransportMissionBridge` | `ProgressionManager` | `AddXP` |
| `TransportMissionBridge` | `AchievementManager` | `ReportProgress` |
| `TransportMissionBridge` | `SocialActivityFeed` | `PostActivity(ActivityType.Custom, ...)` |
| `TransportAnalytics` | `TelemetryDispatcher` | `EnqueueEvent` |
