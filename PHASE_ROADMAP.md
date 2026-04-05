# SWEF Phase Roadmap

> This file tracks the planned development phases for Skywalking: Earth Flight.
> Last updated: 2026-04-05 (Phase 107 — Live Streaming & Spectator Mode)

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
| 97 | 📱 Tablet UI Optimization | — | 2026-04-03 |
| 98 | 🎮 PC Input & Controls Polish | — | 2026-04-04 |
| 99 | 📅 Seasonal Live Events & Battle Pass | — | 2026-04-04 |
| 100 | 🤖 AI Co-Pilot & Smart Assistant (ARIA) | [#122](https://github.com/Kohgane/skywalking-earthflight/pull/122) | 2026-04-04 |
| 101 | 🔧 CI/CD Pipeline Fix & Assembly Reference Cleanup | — | 2026-04-04 |
| 102 | 🎯 Final QA & Release Candidate Prep | — | 2026-04-04 |
| 103 | 🌍 Live Flight Tracking & Real-World Data Overlay | — | 2026-04-04 |
| 104 | 🎓 Flight Academy & Certification System | — | 2026-04-04 |
| 105 | 🌋 Dynamic Terrain Events & Geological Phenomena | — | 2026-04-04 |
| 106 | 🛸 Historical & Sci-Fi Flight Mode | — | 2026-04-04 |
| 107 | 📺 Live Streaming & Spectator Mode | — | 2026-04-05 |

## Current Phase

**Phase 107 — 📺 Live Streaming & Spectator Mode** (Closes [#130](https://github.com/Kohgane/skywalking-earthflight/issues/130))

## Next Phase

**Phase 108 — 🏗️ User-Generated Content (UGC) Editor** ([#131](https://github.com/Kohgane/skywalking-earthflight/issues/131))

## Post-Launch Phases (v2.0+)

> Master roadmap issue: [#125 — 🗺️ Post-Launch Feature Roadmap: Phase 103+ Overview](https://github.com/Kohgane/skywalking-earthflight/issues/125)

| Phase | Title | Category | Issue | Status |
|-------|-------|----------|-------|--------|
| 108 | 🏗️ User-Generated Content (UGC) Editor | UGC | [#131](https://github.com/Kohgane/skywalking-earthflight/issues/131) | Planned |
| 109 | 🤝 Clan/Squadron System | Social | [#132](https://github.com/Kohgane/skywalking-earthflight/issues/132) | Planned |
| 110 | 🎭 Dynamic NPC & Air Traffic Ecosystem | AI | [#133](https://github.com/Kohgane/skywalking-earthflight/issues/133) | Planned |

## 🏁 Launch Preparation

All base development phases are complete. Post-launch phases (103+) are tracked above.

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

> Last updated: 2026-04-04

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
