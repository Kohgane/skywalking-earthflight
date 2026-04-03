# SWEF Project Briefing

> Persistent briefing document for Skywalking: Earth Flight development status.
> Last updated: 2026-04-03

## 🚀 App Launch Estimate

- **Target window**: After all core phases (up to ~Phase 100) are complete + platform testing
- **Realistic estimate**: ~2–3 weeks from now (mid-to-late April 2026), assuming current velocity of ~4 phases/day
- **Blockers**: CI/CD pipeline validation, platform-specific testing, store submission process
- **Note**: This is the earliest possible date for an MVP/Early Access launch, not a full 1.0 release

## 🛫 Test Flight Estimate

- **Internal test flight**: Can begin as soon as Phase 98 (PC Input & Controls Polish) is merged
- **Realistic estimate**: ~1 week from now (around April 10, 2026)
- **Scope**: Basic flight loop with location → takeoff → cruise → edge-of-space on PC/tablet/mobile
- **Note**: Test flight should validate core flight mechanics across all platforms before additional content phases

## 🖥️ Platform Requirements (CRITICAL)

The app and all test flights MUST support ALL of the following platforms equally — **no platform-exclusive builds**:

| Platform | Status | Notes |
|----------|--------|-------|
| **PC (Windows/Mac/Linux)** | ✅ Required | Primary dev platform, WASD + mouse + gamepad |
| **Mobile (iOS/Android)** | ✅ Required | Touch controls, gyroscope input |
| **Tablet (iPad/Android tablet)** | ✅ Required | Optimized layouts, multitasking |
| **XR (Google Glass / Apple Vision Pro)** | 🔧 Supported but NOT exclusive | XR module exists but app must NOT be XR-only |

### ⚠️ Hard Rule
> Google Glass나 Apple VR 안경 전용이면 안 됨. 모바일 전용이어서도 안 됨.
> PC로도, 태블릿으로도, 모바일로도 전부 가능해야 함.
> XR은 추가 지원이지 필수 플랫폼이 아님.

## 📊 Current Progress

- **Phases completed**: 1–96 (merged to main)
- **Current phase**: 97 (Tablet UI Optimization)
- **Modules in codebase**: 70+ script directories under Assets/SWEF/Scripts/
- **Known CI issues**: ReplayTheater assembly references, Unity UI resolve in CI environment
