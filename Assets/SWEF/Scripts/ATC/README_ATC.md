# Phase 78 — Air Traffic Control (ATC) System

**Namespace:** `SWEF.ATC`  
**Directory:** `Assets/SWEF/Scripts/ATC/`

---

## System Architecture

```
ATCManager (Singleton, DontDestroyOnLoad)
│   ├── ATC facility registry — loads AirspaceZone definitions
│   ├── Active frequency tracking (COMM1 / COMM2)
│   ├── Player zone detection — streams ATC zones based on position
│   ├── Clearance lifecycle: Request → Issue → Acknowledge → Monitor → Expire
│   └── Events: OnClearanceReceived / OnHandoff / OnEmergencyDeclared
│
├── ATCRadioController
│   ├── Frequency tuning (118.000–136.975 MHz range, 25 kHz spacing)
│   ├── TX/RX queue with realistic timing delays
│   ├── Audio processing: static noise, squelch gate, voice filter
│   └── COMM1 + COMM2 dual-radio support
│
├── ATCPhraseGenerator
│   ├── ICAO standard phraseology generation
│   ├── NATO phonetic alphabet for callsigns
│   ├── Localization-aware phrase construction
│   └── Simplified mode for casual players
│
├── TrafficSimulator
│   ├── AI traffic spawning around airports (configurable max)
│   ├── Flight path generation (SID/STAR patterns)
│   ├── Separation enforcement (3nm / 1000ft)
│   └── Distance-based update LOD (full → reduced → minimal)
│
├── RunwayManager
│   ├── Wind-based active runway selection
│   ├── ILS approach data provision
│   ├── Runway status management
│   └── Weather integration (null-safe)
│
├── ApproachController
│   ├── Standard approach procedure generation (downwind → base → final)
│   ├── SID departure procedure generation
│   ├── Glidepath tracking and centerline deviation
│   └── Landing/ApproachGuidance integration (null-safe)
│
├── AirspaceController
│   ├── Zone entry/exit detection (per-frame position check)
│   ├── Controlled vs uncontrolled airspace classification
│   ├── Entry clearance requirement enforcement
│   └── Procedural airspace generation around airports
│
├── ATCHUD
│   ├── Radio frequency display (active + standby)
│   ├── Clearance card with timer
│   ├── Mini traffic radar scope
│   ├── Communication log (50 messages)
│   └── TX/RX transmission indicator
│
└── ATCAnalytics
    ├── Clearance compliance tracking
    ├── Approach accuracy metrics
    ├── Go-around statistics
    └── TelemetryDispatcher integration (null-safe)
```

---

## ATC Communication Flow

```
Player            ATCManager          ATCRadioController     ATCPhraseGenerator
  │                   │                       │                      │
  │ RequestClearance()│                       │                      │
  │──────────────────>│                       │                      │
  │                   │ BuildInstruction()    │                      │
  │                   │────────────────────── │                      │
  │                   │ IssueClearance()      │                      │
  │                   │ OnClearanceReceived   │                      │
  │<──────────────────│                       │                      │
  │                   │                       │ GenerateClearance()  │
  │                   │──────────────────────────────────────────────>
  │                   │                       │  phrase string       │
  │                   │<──────────────────────────────────────────────
  │                   │ Transmit(phrase)      │                      │
  │                   │──────────────────────>│                      │
  │                   │                       │ OnTransmissionStarted│
  │                   │                       │─────────────────────>│
  │ AcknowledgeClearance()                    │                      │
  │──────────────────>│                       │                      │
  │                   │ CancelExpiryTimer()   │                      │
  │                   │────────────────────── │                      │
```

---

## Scripts

### 1. `ATCData.cs`

Pure data layer for the ATC system.

**Enumerations:**

| Enum | Values |
|------|--------|
| `ATCFacilityType` | Tower / Approach / Center / Ground / Departure / ATIS |
| `FlightPhase` | Parked / Taxi / Takeoff / Departure / Cruise / Approach / Landing / GoAround / Emergency |
| `Clearance` | Taxi / Takeoff / Landing / Approach / Altitude / Speed / Heading / Hold / GoAround |
| `RunwayStatus` | Active / Closed / Maintenance |

**Data Classes:**

| Class | Description |
|-------|-------------|
| `RadioFrequency` | Frequency value in MHz, human name, facility type |
| `ATCInstruction` | Full clearance with assigned runway, altitude, heading, speed, holding flag, expiration |
| `AirspaceZone` | Cylindrical airspace with floor/ceiling, facility type, primary frequency |
| `TrafficContact` | Simulated AI aircraft with position, speed, heading, threat level |
| `RunwayInfo` | Physical runway data with ILS availability and operational status |
| `ATCSettings` | Runtime config: max traffic, comms range, radio volume, phraseology mode |

---

### 2. `ATCManager.cs`

Singleton MonoBehaviour that manages the full ATC lifecycle.

**Public API:**

| Method | Description |
|--------|-------------|
| `RequestClearance(Clearance)` | Issues a new clearance of the requested type |
| `AcknowledgeClearance()` | Acknowledges the current clearance and resets the expiry timer |
| `TuneFrequency(RadioFrequency)` | Tunes COMM1 to the specified frequency |
| `SwapFrequency()` | Swaps active and standby COMM1 frequencies |
| `ContactFacility(ATCFacilityType)` | Auto-tunes to the specified facility type in the current zone |
| `DeclareEmergency()` | Declares a MAYDAY (squawk 7700) |
| `CancelEmergency()` | Cancels an active emergency declaration |

**Events:** `OnClearanceReceived`, `OnClearanceExpired`, `OnFrequencyChanged`, `OnHandoff`, `OnEmergencyDeclared`, `OnEmergencyCancelled`

---

### 3. `ATCRadioController.cs`

Simulates VHF radio communications.

**Public API:**

| Method | Description |
|--------|-------------|
| `Transmit(message)` | Queues a message for TX on COMM1 |
| `ReceiveMessage(message)` | Delivers an incoming message to the receive queue |
| `SetSquelch(float)` | Adjusts the squelch threshold (0–1) |
| `ToggleCOMM1()` | Toggles COMM1 active state |
| `ToggleCOMM2()` | Toggles COMM2 active state |

**Events:** `OnTransmissionStarted(message)`, `OnTransmissionEnded`, `OnMessageReceived(message)`

---

### 4. `ATCPhraseGenerator.cs`

Generates ICAO-standard ATC phraseology.

**Public API:**

| Method | Description |
|--------|-------------|
| `GenerateClearance(ATCInstruction)` | Returns a localised ATC clearance phrase |
| `GenerateReadback(ATCInstruction)` | Returns the pilot readback phrase |
| `GenerateATIS(airport, weather)` | Generates an ATIS broadcast string |
| `SpellCallsign(callsign)` | Converts a callsign to NATO phonetic spelling |

---

### 5. `TrafficSimulator.cs`

Manages AI traffic contacts around active airports.

**Public API:**

| Method | Description |
|--------|-------------|
| `SpawnTraffic(nearPosition)` | Spawns a new AI contact near a position |
| `DespawnTraffic(contact)` | Removes a contact from the simulation |
| `GetNearbyTraffic(position, range)` | Returns contacts within range metres |
| `GetTrafficOnFrequency(frequency)` | Returns contacts on the specified frequency |

Separation: 3 nm lateral (5,556 m) / 1,000 ft vertical.

---

### 6. `RunwayManager.cs`

Manages runway assignments and operational status.

**Public API:**

| Method | Description |
|--------|-------------|
| `AssignRunway(FlightPhase)` | Returns the best runway for the given phase (wind-aware) |
| `GetActiveRunways()` | Returns all runways with Active status |
| `SetRunwayStatus(name, RunwayStatus)` | Updates a runway's operational status |
| `GetILSData(RunwayInfo)` | Returns ILS approach parameters string |

**Events:** `OnRunwayAssigned`, `OnRunwayStatusChanged`

---

### 7. `ApproachController.cs`

Generates approach and departure procedures.

**Public API:**

| Method | Description |
|--------|-------------|
| `InitiateApproach(runway)` | Generates downwind → base → final → threshold waypoints |
| `InitiateDeparture(runway)` | Generates SID departure waypoints |
| `GetApproachProgress01()` | Returns 0–1 normalised approach progress |
| `IsOnGlidepath()` | True if within ±200 ft altitude and ±1° lateral |
| `GetDeviationFromCenterline()` | Returns degrees deviation from runway centreline |

---

### 8. `AirspaceController.cs`

Per-frame airspace zone detection and entry enforcement.

**Public API:**

| Method | Description |
|--------|-------------|
| `GetCurrentZone(position)` | Returns the innermost zone containing position |
| `IsInControlledAirspace(position, altitudeFt)` | True if inside any controlled zone or Class A |
| `GetZoneFacility(zone)` | Returns the facility type managing the zone |
| `RequestEntry(zone)` | Checks clearance requirement; fires `OnUnauthorizedEntry` if needed |
| `AddZone(zone)` | Registers a new zone at runtime |

**Events:** `OnZoneEntered`, `OnZoneExited`, `OnUnauthorizedEntry`

---

### 9. `ATCHUD.cs`

UGUI overlay for ATC information.

**UI References:**

| Field | Description |
|-------|-------------|
| `frequencyText` | Active COMM1 frequency label |
| `standbyFrequencyText` | Standby COMM1 frequency label |
| `clearancePanel` | Clearance card root panel |
| `clearanceTypeText` | Clearance type label |
| `clearanceTimerText` | Countdown timer label |
| `trafficScope` | RectTransform for radar blip rendering |
| `blipPrefab` | Traffic contact blip prefab |
| `messageLog` | ScrollRect for communication log |
| `messageLogText` | Text element inside the scroll rect |
| `transmitIndicator` | GameObject shown while transmitting |
| `receiveIndicator` | GameObject shown while receiving |
| `atisPanel` | ATIS information root panel |
| `atisText` | ATIS text label |

---

### 10. `ATCAnalytics.cs`

Telemetry for ATC interactions.

**Public API:**

| Method | Description |
|--------|-------------|
| `RecordClearanceCompliance(bool)` | Records whether the player followed a clearance |
| `RecordApproachAccuracy(deviation)` | Records a centreline deviation sample (degrees) |
| `RecordGoAround()` | Increments the go-around counter |
| `GetSessionSummary()` | Returns a formatted metrics summary string |

---

## Radio Frequency Ranges

| Band | Range | Spacing |
|------|-------|---------|
| VHF Aviation | 118.000–136.975 MHz | 25 kHz |
| Emergency guard | 121.500 MHz | — |
| Military UHF guard | 243.000 MHz | — |

---

## Integration Points

| ATC Script | Integrates With | Guard |
|-----------|-----------------|-------|
| `ATCManager` | `SWEF.Landing.AirportRegistry` — airport-based facility generation | `#define SWEF_LANDING_AVAILABLE` |
| `ATCRadioController` | `SWEF.Audio.AudioManager` — radio SFX (static, squelch, voice) | `#define SWEF_AUDIO_AVAILABLE` |
| `ATCPhraseGenerator` | `SWEF.Localization.LocalizationManager` — localised phrases | `#define SWEF_LOCALIZATION_AVAILABLE` |
| `RunwayManager` | `SWEF.Weather.WeatherManager` — wind for runway selection | `#define SWEF_WEATHER_AVAILABLE` |
| `ApproachController` | `SWEF.Landing.ApproachGuidance` — ILS overlay | `#define SWEF_LANDING_AVAILABLE` |
| `AirspaceController` | `SWEF.Flight.FlightController` — player position | null-safe |
| `ATCAnalytics` | `SWEF.Analytics.TelemetryDispatcher` — telemetry events | `#define SWEF_ANALYTICS_AVAILABLE` |
| `TrafficSimulator` | `SWEF.CityGen.CityManager` — traffic density scaling | null-safe |

---

## Localization Keys (40 keys)

### ATC Facility Types
| Key | English |
|-----|---------|
| `atc_facility_tower` | Tower |
| `atc_facility_approach` | Approach |
| `atc_facility_center` | Center |
| `atc_facility_ground` | Ground |
| `atc_facility_departure` | Departure |
| `atc_facility_atis` | ATIS |

### Flight Phases
| Key | English |
|-----|---------|
| `atc_phase_parked` | Parked |
| `atc_phase_taxi` | Taxi |
| `atc_phase_takeoff` | Takeoff |
| `atc_phase_departure` | Departure |
| `atc_phase_cruise` | Cruise |
| `atc_phase_approach` | Approach |
| `atc_phase_landing` | Landing |
| `atc_phase_goaround` | Go Around |
| `atc_phase_emergency` | Emergency |

### Clearance Types
| Key | English |
|-----|---------|
| `atc_clearance_taxi` | Taxi Clearance |
| `atc_clearance_takeoff` | Takeoff Clearance |
| `atc_clearance_landing` | Landing Clearance |
| `atc_clearance_approach` | Approach Clearance |
| `atc_clearance_altitude` | Altitude Assignment |
| `atc_clearance_speed` | Speed Assignment |
| `atc_clearance_heading` | Heading Assignment |
| `atc_clearance_hold` | Holding Pattern |
| `atc_clearance_goaround` | Go Around |

### HUD Labels
| Key | English |
|-----|---------|
| `atc_hud_frequency` | Frequency |
| `atc_hud_active` | Active |
| `atc_hud_standby` | Standby |
| `atc_hud_clearance` | Current Clearance |
| `atc_hud_traffic` | Traffic |
| `atc_hud_comlog` | Communications |
| `atc_hud_transmit` | Transmitting |
| `atc_hud_receive` | Receiving |
| `atc_hud_atis` | ATIS Information |
| `atc_hud_no_clearance` | No Active Clearance |
| `atc_hud_emergency` | EMERGENCY |

### Radio Phrases
| Key | English |
|-----|---------|
| `atc_radio_roger` | Roger |
| `atc_radio_wilco` | Wilco |
| `atc_radio_unable` | Unable |
| `atc_radio_sayagain` | Say Again |
| `atc_radio_standby` | Standby |
| `atc_radio_affirm` | Affirm |
| `atc_radio_negative` | Negative |

---

## File Layout

```
Assets/SWEF/Scripts/ATC/
├── ATCData.cs              # Enums + data classes
├── ATCManager.cs           # Singleton manager
├── ATCRadioController.cs   # Radio TX/RX simulation
├── ATCPhraseGenerator.cs   # ICAO phraseology
├── TrafficSimulator.cs     # AI traffic contacts
├── RunwayManager.cs        # Runway assignment
├── ApproachController.cs   # Approach/departure procedures
├── AirspaceController.cs   # Airspace zone detection
├── ATCHUD.cs               # HUD overlay
├── ATCAnalytics.cs         # Telemetry bridge
└── README_ATC.md           # This file
```
