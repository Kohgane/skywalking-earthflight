# Flight Training Academy & Skill Certification System

**Phase 84 · Namespace:** `SWEF.FlightAcademy`  
**Directory:** `Assets/SWEF/Scripts/FlightAcademy/`

## Overview

The Flight Training Academy adds a structured, progressive pilot certification system to SkyWalking EarthFlight. Players advance through six license grades — from Student Pilot all the way to Test Pilot — by completing training modules and practical exams that test real flight skills.

---

## License Progression Tree

```
Student Pilot  ──►  PPL  ──►  CPL  ──►  ATPL  ──►  Instructor Rating  ──►  Test Pilot
```

Each grade requires passing **5 training modules**. All modules in a grade must be passed before the corresponding license certificate is issued.

| Grade | Recommended Hours | Difficulty |
|-------|:-----------------:|:----------:|
| Student Pilot | 10 h | Bronze |
| PPL | 40 h | Silver |
| CPL | 150 h | Gold |
| ATPL | 500 h | Gold |
| Instructor Rating | 800 h | Platinum |
| Test Pilot | 1500 h | Platinum |

---

## Grading System

| Grade | Score Range | Description |
|-------|:-----------:|-------------|
| A+ | 97–100 | Outstanding |
| A  | 93–96  | Excellent |
| A- | 90–92  | Very Good |
| B+ | 87–89  | Good |
| B  | 83–86  | Above Average |
| B- | 80–82  | Satisfactory |
| C+ | 77–79  | Acceptable |
| C  | 73–76  | Adequate |
| C- | 70–72  | Minimum Competency |
| D  | 60–69  | Below Standard |
| F  | 0–59   | Fail |

**Passing thresholds by difficulty:**
- Bronze → 60 (D or above)
- Silver → 70 (C- or above)
- Gold → 80 (B- or above)
- Platinum → 90 (A- or above)

---

## Architecture

```
FlightAcademyManager (Singleton, DontDestroyOnLoad)
│   ├── Loads TrainingModule[] from Resources/Academy/
│   │   └── Falls back to FlightAcademyDefaultData (30 built-in modules)
│   ├── Persists to Application.persistentDataPath/academy_progress.json
│   └── Events: OnModuleStarted / OnExamStarted / OnExamCompleted
│               OnLicenseEarned / OnCertificateIssued
│
ExamController
│   ├── Real-time objective monitoring
│   ├── Penalty / bonus tracking
│   ├── Timer management
│   └── Delegates final scoring to ExamScoringEngine
│
ExamScoringEngine (Static)
│   ├── CalculateScore()          — weighted composite 0–100
│   ├── CalculateLandingScore()   — speed/centerline/descent/G-force/stability
│   ├── CalculateIFRScore()       — heading/altitude/speed/waypoint
│   ├── CalculateFormationScore() — position/heading/speed match
│   ├── GetLetterGrade()          — A+→F
│   └── GetPassStatus()           — per-difficulty threshold
│
CertificateGenerator (Static)
│   ├── GenerateCertificate()     — creates signed certificate (SHA-256)
│   ├── VerifyCertificate()       — authenticity check
│   └── FormatCertificateText()   — display string
│
TrainingModuleRunner
│   ├── Guided walkthroughs with InstructorDialogueController
│   ├── Unlimited practice retries
│   └── Bridges to SWEF.FlightSchool.FlightInstructor (null-safe)
│
InstructorDialogueController
│   ├── Priority-queued dialogue system
│   ├── Localized text via LocalizationManager (null-safe)
│   └── TTS via ScreenReaderBridge (null-safe)
│
FlightAcademyHUD
│   ├── Live objective checklist
│   ├── Countdown timer
│   ├── Score estimate
│   └── Penalty/bonus toast notifications
│
FlightAcademyUI
│   ├── License progression tree
│   ├── Module grid (locked/available/completed)
│   ├── Module detail card
│   └── Certificate gallery + statistics
│
ExamResultUI
│   ├── Animated score reveal
│   ├── Per-objective breakdown
│   └── Retry / share certificate buttons
│
FlightAcademyBridge
│   ├── → ProgressionManager.AddXP()      (null-safe)
│   ├── → AchievementManager              (null-safe)
│   ├── → SkillTreeManager.AddSkillPoint() (null-safe)
│   ├── → JournalManager                  (null-safe)
│   ├── → SocialActivityFeed              (null-safe)
│   └── → FlightSchoolManager             (null-safe)
│
FlightAcademyAnalytics
│   └── → TelemetryDispatcher (8 event types, null-safe)
│
FlightAcademyDefaultData (Static)
│   ├── 30 pre-configured TrainingModule instances
│   ├── 6 license grades × 5 modules each
│   └── Prerequisite chains per grade
```

---

## New Scripts (14 files)

| # | Script | Purpose |
|---|--------|---------|
| 1 | `FlightAcademyData.cs` | Enums, data classes, TrainingModule ScriptableObject |
| 2 | `FlightAcademyManager.cs` | Singleton orchestrator — progression, persistence |
| 3 | `ExamController.cs` | Active exam session — objectives, timer, penalties |
| 4 | `ExamScoringEngine.cs` | Static scoring utility |
| 5 | `TrainingModuleRunner.cs` | Pre-exam guided training mode |
| 6 | `CertificateGenerator.cs` | Static certificate creator/verifier |
| 7 | `CertificateShareController.cs` | Share/save certificate image |
| 8 | `FlightAcademyHUD.cs` | In-exam HUD overlay |
| 9 | `FlightAcademyUI.cs` | Full-screen academy panel |
| 10 | `ExamResultUI.cs` | Post-exam results screen |
| 11 | `FlightAcademyBridge.cs` | System integration bridge |
| 12 | `FlightAcademyAnalytics.cs` | Telemetry (8 event types) |
| 13 | `InstructorDialogueController.cs` | Priority-queued instructor dialogue |
| 14 | `FlightAcademyDefaultData.cs` | 30 default training modules |

---

## Landing Exam Scoring

| Factor | Weight | Scoring Bands |
|--------|:------:|---------------|
| Touchdown speed deviation | 25% | ≤10 kts=100%, ≤20=75%, ≤30=50%, >30=25% |
| Centerline deviation | 25% | <3 m=100%, <10=75%, <20=50%, ≥20=25% |
| Descent rate | 25% | <200 fpm=100%, <400=75%, <600=50%, ≥600=25% |
| G-force at touchdown | 15% | <1.2G=100%, <1.5=75%, <2.0=50%, ≥2.0=25% |
| Approach stability | 10% | Stable approach within final 500 ft |

---

## Persistence

`Application.persistentDataPath/academy_progress.json`  
Stores: current license, completed modules, best exam results, training hours, certificates.

---

## Localization

35 keys added to all 8 language files (`lang_en.json`, `lang_de.json`, `lang_es.json`, `lang_fr.json`, `lang_ja.json`, `lang_ko.json`, `lang_pt.json`, `lang_zh.json`).

**Key prefixes:** `academy_license_`, `academy_exam_`, `academy_diff_`, `academy_grade_`, `academy_` (UI)

---

## Tests

`Assets/Tests/EditMode/FlightAcademyTests.cs` — NUnit EditMode tests covering:

- `ExamScoringEngine.CalculateScore()` — weighted scoring with penalties/bonuses, clamping
- `ExamScoringEngine.CalculateLandingScore()` — all four scoring bands per factor
- `ExamScoringEngine.GetLetterGrade()` — all 11 grade boundaries
- `ExamScoringEngine.GetPassStatus()` — per-difficulty thresholds (Bronze/Silver/Gold/Platinum)
- `ExamScoringEngine.CalculateIFRScore()` / `CalculateFormationScore()`
- `CertificateGenerator` — creation, verification, tamper detection, null safety
- `FlightAcademyDefaultData` — 30 modules, 6 grades, unique IDs, prerequisite validity
- License progression — all-modules-passed and missing-module scenarios

---

## Integration Points

All external system calls are **null-safe** and guarded with `#if` compile symbols for optional systems.

| Script | System |
|--------|--------|
| `ExamController` | `SWEF.Flight.FlightController`, `SWEF.Landing.LandingDetector` |
| `FlightAcademyBridge` | `SWEF.Progression.ProgressionManager`, `SWEF.Achievement.AchievementManager` |
| `FlightAcademyBridge` | `SWEF.Progression.SkillTreeManager`, `SWEF.Journal.JournalManager` |
| `FlightAcademyBridge` | `SWEF.SocialHub.SocialActivityFeed`, `SWEF.FlightSchool.FlightSchoolManager` |
| `InstructorDialogueController` | `SWEF.Accessibility.ScreenReaderBridge`, `SWEF.Localization.LocalizationManager` |
| `FlightAcademyAnalytics` | `SWEF.Analytics.TelemetryDispatcher` |
| `CertificateShareController` | `SWEF.Social.ShareManager` |
