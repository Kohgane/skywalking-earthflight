# SWEF Phase Roadmap

> This file tracks the planned development phases for Skywalking: Earth Flight.
> Last updated: 2026-04-03 (Phase 96)

## Completed Phases

Phases 1–80 have been merged into `main`. See individual PR descriptions and `README.md` for details.

| Phase | Title | PR | Merged |
|-------|-------|----|--------|
| 81 | Terrain Scanning & Geological Survey System | [#98](https://github.com/Kohgane/skywalking-earthflight/pull/98) | 2026-04-01 |
| 82 | Passenger & Cargo Mission System | [#99](https://github.com/Kohgane/skywalking-earthflight/pull/99) | 2026-04-01 |
| 83 | Dynamic Soundtrack & Adaptive Music System | — | 2026-04-01 |
| 84 | Voice Command & Cockpit Voice Assistant System | [#104](https://github.com/Kohgane/skywalking-earthflight/pull/104) | 2026-04-01 |
| 85 | Space Station & Orbital Docking System | — | 2026-04-01 |
| 86 | Natural Disaster & Dynamic World Events | — | 2026-04-02 |
| 87 | Advanced Navigation & Flight Plan System | — | 2026-04-02 |
| 88 | Competitive Racing & Time Trial System | — | 2026-04-02 |
| 89 | Advanced Photography & Drone Camera System | — | 2026-04-02 |
| 90 | Aircraft Workshop & Part Customization | — | 2026-04-03 |
| 91 | 🌐 Multiplayer Expansion & Social Features | — | 2026-04-03 |
| 92 | 🛡️ Anti-Cheat & Security Hardening | — | 2026-04-03 |
| 93 | ♿ Accessibility & Platform Optimization | — | 2026-04-03 |
| 94 | 🏪 Community Content Marketplace | — | 2026-04-03 |
| 95 | 🔧 Platform Target Matrix & Build Pipeline | — | 2026-04-03 |
| 96 | 🧪 Integration Test & QA Framework | — | 2026-04-03 |

## Next Batch

Production-readiness phases — implement in order after Phase 96.

| Phase | Title | Priority | Description |
|-------|-------|----------|-------------|
| 97 | 📱 Tablet UI Optimization | **High** | iPad/Android tablet layouts, multitasking support |
| 98 | 🎮 PC Input & Controls Polish | **High** | WASD + mouse flight, gamepad profiles |
| 99 | 📅 Seasonal Live Events & Battle Pass | Medium | CrossSessionEvents extension |
| 100 | 🤖 AI Co-Pilot & Smart Assistant | Medium | VoiceCommand + Navigation synergy |

## Phase Selection Criteria

Phases are selected based on:
1. **Gap analysis** — identifying missing simulation/content areas
2. **System synergy** — leveraging existing integrations (Flight, Minimap, Journal, Achievement, etc.)
3. **Player experience variety** — alternating between simulation depth, content breadth, and quality-of-life
4. **Technical dependency** — ensuring prerequisite systems exist before dependent phases

## How to Update

When a phase is completed, move it to the "Completed Phases" section and add the next candidate.
When starting a new batch, discuss with @copilot to generate the next 4–5 candidates.

## Launch Briefing

> Last updated: 2026-04-03

### Target Timeline

| Milestone | Target Date | Notes |
|-----------|-------------|-------|
| Alpha (Internal Test) | 2026-05~06 | 94 feature phases done, Unity build integration + scene wiring remaining |
| Closed Beta | 2026-07~08 | Real-device testing, performance optimization, bug fixes |
| Open Beta / Soft Launch | 2026-09~10 | Regional App Store / Play Store / Steam soft launch |
| Official Launch | 2026-11~12 | Season 1 "Sky Pioneer" period (ends 2026-12-31) |

### Test Flight Timeline

| Milestone | Target Date | Notes |
|-----------|-------------|-------|
| Editor Play Mode Test Flight | 2026-04~05 | Unity Editor with Cesium tiles + flight |
| Real Device Build Test Flight | 2026-05~06 | PC (Win/Mac), iOS, Android builds |
| Multiplayer Test Flight | 2026-06~07 | Phase 33 multiplayer system live server test |

### Platform Requirements (CRITICAL)

**SWEF must be a cross-platform application. It must NOT be exclusive to any single device category.**

| Platform | Priority | Status |
|----------|----------|--------|
| Windows PC | **Primary** | Phase 93 PlatformOptimizer supports PC-primary |
| macOS | **Primary** | Unity cross-compile from same codebase |
| iOS (iPhone) | **Primary** | TouchInputRouter + GPS implemented |
| Android (Phone) | **Primary** | TouchInputRouter + GPS implemented |
| iPad | **High** | Needs tablet-specific UI layout (Phase 97) |
| Android Tablet | **High** | Needs tablet-specific UI layout (Phase 97) |
| Meta Quest / XR | Secondary (planned) | XR module exists (6 scripts) |
| Apple Vision Pro | Secondary (planned) | Requires visionOS adaptation |
