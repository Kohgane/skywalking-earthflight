# SWEF Live Flight Tracking & Real-World Data Overlay

**Phase 103** — Post-launch feature for [Skywalking: Earth Flight](../../../../README.md).

Namespace: `SWEF.LiveFlight`  
Assembly: `SWEF.LiveFlight.asmdef`

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────┐
│                  LiveFlightAPIClient                    │
│  (Singleton MonoBehaviour — polls REST API / mock)      │
│   ┌──────────────┐  ┌──────────────┐  ┌─────────────┐  │
│   │   OpenSky    │  │ ADS-B Exch.  │  │    Mock     │  │
│   └──────────────┘  └──────────────┘  └─────────────┘  │
│         └──────────────────┬──────────────────┘         │
│                OnAircraftDataReceived event              │
└───────────────────────┬─────────────────────────────────┘
                        │
          ┌─────────────┼──────────────────┐
          ▼             ▼                  ▼
 LiveAircraftRenderer  LiveFlightHUD  LiveFlightMinimapOverlay
  (Object pool /        (Filters,      (Minimap blips,
   smooth lerp,          info popup,    heading indicator)
   LOD labels)           Follow btn)
          │
          ▼
 LiveFlightFollowController   FlightRouteRenderer
  (Camera follow,              (Great-circle arc,
   HUD info panel,             dashed predicted /
   FlightController pause)     solid traveled)
          │
          ▼
   LiveFlightAnalytics  ──►  TelemetryDispatcher
   (static, #if guard)        (#if SWEF_ANALYTICS_AVAILABLE)
```

---

## API Provider Setup

### OpenSky Network (free)

1. Register at <https://opensky-network.org/> for higher rate limits (anonymous
   access allows ~100 requests/day).
2. In **Project Settings → Player → Scripting Define Symbols** no extra symbol
   is needed; just select `OpenSky` as the `apiProvider` in `LiveFlightConfig`.
3. Set `apiUrl` to `https://opensky-network.org/api`.
4. Leave `apiKey` empty for anonymous, or enter your credentials as
   `username:password` (Basic Auth — handled in `LiveFlightAPIClient`).

### ADS-B Exchange (commercial)

1. Obtain an API key from <https://www.adsbexchange.com/data/>.
2. Select `ADS_B_Exchange` as `apiProvider`.
3. Set `apiUrl` to `https://adsbexchange.com/api/aircraft`.
4. Paste your API key into `apiKey`.

---

## Mock Mode (Editor Testing)

No API key or internet connection is required.

1. Create a `LiveFlightConfig` asset (*Assets → Create → SWEF/LiveFlight/Config*).
2. Set `apiProvider` to **Mock**.
3. Add a `LiveFlightAPIClient` MonoBehaviour to your scene and assign the config.
4. Press **Play** — synthetic aircraft will be generated immediately.

---

## Configuration via ScriptableObject

All tunable parameters live in `LiveFlightConfig`:

| Field | Default | Description |
|-------|---------|-------------|
| `apiProvider` | `Mock` | Data source |
| `apiUrl` | OpenSky URL | REST endpoint |
| `apiKey` | `""` | Bearer token or Basic-Auth |
| `pollIntervalSeconds` | `10` | Seconds between fetches |
| `maxAircraftDisplayed` | `100` | Object-pool cap |
| `displayRadiusKm` | `500` | Filter radius around player |
| `showRouteLines` | `true` | Draw great-circle arcs |
| `showLabels` | `true` | Callsign / alt / speed labels |
| `altitudeColorGradient` | white→cyan | Altitude colour ramp |
| `iconScale` | `1` | Marker scale multiplier |

---

## Integration Points

| Script | Integrates With | Guard |
|--------|-----------------|-------|
| `LiveAircraftRenderer` | `SWEF.Terrain.CesiumTerrainBridge` | `#if SWEF_TERRAIN_AVAILABLE` |
| `LiveFlightFollowController` | `SWEF.Flight.FlightController` | `#if SWEF_FLIGHT_AVAILABLE` |
| `LiveFlightHUD` | `SWEF.Minimap.MinimapManager` | `#if SWEF_MINIMAP_AVAILABLE` |
| `LiveFlightAnalytics` | `SWEF.Analytics.TelemetryDispatcher` | `#if SWEF_ANALYTICS_AVAILABLE` |
| `LiveFlightPanelUI` | `SWEF.Localization.LocalizationManager` | `#if SWEF_LOCALIZATION_AVAILABLE` |

Enable a guard by adding its symbol to **Project Settings → Player →
Scripting Define Symbols**.

---

## Quick-Start Scene Setup

```
[Scene Hierarchy]
LiveFlight/
  ├── LiveFlightAPIClient       (LiveFlightAPIClient.cs, LiveFlightConfig asset)
  ├── LiveAircraftRenderer      (LiveAircraftRenderer.cs, marker prefab optional)
  ├── FlightRouteRenderer       (FlightRouteRenderer.cs + LineRenderer)
  ├── LiveFlightFollowController
  ├── LiveFlightHUD             (Canvas child)
  ├── LiveFlightPanelUI         (Canvas child, full-screen)
  └── LiveFlightMinimapOverlay  (Minimap canvas child)
```

---

## Tests

Edit-mode tests live in `Assets/Tests/EditMode/LiveFlightTests.cs`.

Run via **Window → General → Test Runner → EditMode**.
