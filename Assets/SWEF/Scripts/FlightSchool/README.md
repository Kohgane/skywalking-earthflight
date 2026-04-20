# Flight Training Academy & Skill Certification (Phase 84)

**Namespace:** `SWEF.FlightSchool`

Extends the Flight School foundation (Phases 1-83) with a multi-criteria grading engine,
flight-envelope constraint enforcement, certification practical tests, a post-flight debrief
screen, and a skill-tree progression system.

## Architecture

```
FlightSchoolManager (singleton, persistence)
    |
    |-- FlightInstructor (in-flight guidance)
    |       |-- FlightGradingSystem (multi-criteria scoring)
    |       |-- FlightConstraintEnforcer (envelope enforcement)
    |
    |-- CertificationExamController (practical-test flow)
    |-- SkillTreeController (DAG-based progression)
    |-- DebriefingController (post-flight analysis)
    |
    |-- FlightSchoolUI (curriculum/detail/certification panels)
    |       |-- FlightSchoolHUD (in-flight constraint + grade overlay)
    |
    |-- LessonReplayBridge -----> SWEF.Replay
    |-- FlightSchoolAchievementBridge -----> SWEF.Achievement
    |-- FlightSchoolJournalBridge -----> SWEF.Journal
    |-- FlightSchoolAnalyticsBridge -----> SWEF.Analytics
    |
    +-- FlightSchoolProfile (ScriptableObject config)
```

## Scripts

| Script | Type | Purpose |
|--------|------|---------|
| `FlightSchoolData.cs` | Data | Enums, data classes (FlightLesson, LessonObjective, PilotCertification, FlightConstraint, GradeCriteria, LessonGradeReport, CertificationExam, SkillNode, SkillTreeData) |
| `FlightSchoolManager.cs` | MonoBehaviour (singleton) | Curriculum management, progress persistence (JSON), XP, certification checks, skill-tree storage |
| `FlightInstructor.cs` | MonoBehaviour | In-flight guidance, objective tracking, hint generation, performance evaluation |
| `FlightGradingSystem.cs` | MonoBehaviour (singleton) | Multi-criteria grading: precision, smoothness, timing, safety, fuel efficiency |
| `FlightConstraintEnforcer.cs` | MonoBehaviour | Polls flight state, enforces altitude/speed/heading/bank/g-force/geofence constraints |
| `CertificationExamController.cs` | MonoBehaviour | Practical-test sessions — ordered lesson sequence with min-score requirement |
| `DebriefingController.cs` | MonoBehaviour | Post-flight debrief — grade breakdown, personal-best comparison, coaching tips |
| `SkillTreeController.cs` | MonoBehaviour | DAG-based skill tree — nodes unlock when parent lessons complete |
| `FlightSchoolHUD.cs` | MonoBehaviour | In-flight overlay — live score, constraint indicators, hint text |
| `FlightSchoolUI.cs` | MonoBehaviour | Menu panels — curriculum browser, lesson detail, certification, debrief, skill tree, exam |
| `FlightSchoolProfile.cs` | ScriptableObject | Designer-authored configuration — curriculum, grading weights, exam definitions |
| `LessonReplayBridge.cs` | MonoBehaviour | Auto-records flights during lessons (reflection-based) |
| `FlightSchoolAchievementBridge.cs` | MonoBehaviour | Unlocks achievements on milestones (reflection-based) |
| `FlightSchoolJournalBridge.cs` | MonoBehaviour | Tags journal entries with lesson metadata (reflection-based) |
| `FlightSchoolAnalyticsBridge.cs` | MonoBehaviour | Forwards events to SWEF.Analytics telemetry pipeline |

## Grading System

### Criteria & Default Weights

| Criterion | Weight | Description |
|-----------|--------|-------------|
| Precision | 2.0 | Heading and altitude tracking accuracy |
| Smoothness | 1.5 | Control-input jitter / over-correction |
| Timing | 1.0 | Lesson completion relative to estimate |
| Safety | 2.5 | Stall, overspeed, terrain warnings |
| Fuel | 1.0 | Fuel burn efficiency |

### Letter Grades

| Grade | Score Range |
|-------|------------|
| A | 90 - 100 |
| B | 80 - 89 |
| C | 70 - 79 |
| D | 60 - 69 |
| F | 0 - 59 |

## Constraint Types

| Type | Unit | Description |
|------|------|-------------|
| AltitudeRange | metres | Stay within min/max altitude |
| SpeedRange | knots | Maintain speed envelope |
| HeadingRange | degrees | Hold heading within range |
| BankAngleLimit | degrees | Limit bank angle |
| GForceLimit | g | Max positive/negative G |
| GeofenceRadius | metres | Stay within radius of a point |

Each constraint has a `warningMargin` (soft buffer before penalties) and a
`penaltyPerSecond` (deviation penalty rate fed to FlightInstructor).

## Certification Exam Flow

1. Player selects a certification tier (StudentPilot → MasterAviator)
2. `CertificationExamController.StartExam(certType)` begins the attempt
3. Lessons in `examLessonIds` execute sequentially
4. Each lesson must score >= `minimumPassScore` (default 70)
5. Failing any lesson → exam fails, attempt counter increments
6. Passing all lessons → certification awarded, `OnExamPassed` fired
7. Maximum attempts enforced (default 3, 0 = unlimited)

## Skill Tree

A directed acyclic graph where each node maps to a lesson:

```
[First Takeoff] → [Level Flight] → [Waypoint Nav] → [Aerobatic Loop] ─┐
                                  → [Crosswind]    → [Formation]     ──┼→ [Master Aviator]
                                  → [Engine-Out]   ─────────────────────┘
```

Completing a node's lesson unlocks its direct children.

## Localization

32 keys with prefix `flightschool_` across 8 languages:
en, fr, de, es, ja, ko, pt, zh

Located in `/Assets/SWEF/Resources/Localization/lang_*.json`.

## Tests

`/Assets/Tests/EditMode/FlightSchoolTests.cs` — 68 test methods covering:
- Data model (LessonObjective, FlightLesson, PilotCertification)
- Flight constraints (IsWithin, IsInWarningZone, boundary cases)
- Grading (GradeCriteria, LessonGradeReport, ComputeFinalScore, letter thresholds)
- Skill tree (FindNode, FindNodesByLesson, empty tree, multiple nodes per lesson)
- Certification exams (defaults, attempt limits)
- Debriefing (tip generation per criterion, severity levels)
- Enums (member counts, progression ordering)
- Integration (score-to-letter consistency, constraint independence)

## Setup

1. Create an empty GameObject named `FlightSchoolSystem`
2. Attach: `FlightSchoolManager`, `FlightInstructor`, `FlightGradingSystem`, `FlightConstraintEnforcer`
3. Attach: `CertificationExamController`, `DebriefingController`, `SkillTreeController`
4. Attach: `FlightSchoolUI`, `FlightSchoolHUD`
5. Attach bridges: `LessonReplayBridge`, `FlightSchoolAchievementBridge`, `FlightSchoolJournalBridge`, `FlightSchoolAnalyticsBridge`
6. (Optional) Create a `FlightSchoolProfile` asset via Create > SWEF > Flight School Profile and assign it
7. Wire Inspector references between components
