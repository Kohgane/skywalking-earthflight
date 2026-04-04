# 버그/이슈 트래킹 셋업 가이드 — Bug Tracking Guide

> SWEF의 버그 및 이슈를 체계적으로 추적하기 위한 가이드입니다.

---

## Bug Report Template (버그 리포트 템플릿)

새 이슈를 열 때 아래 형식을 사용하세요:

```
**제목 형식**: [Platform] [Phase/System] 짧은 설명
예시: [Android] [Flight] 고도 5000m 이상에서 FlightController NullReferenceException 발생
```

```markdown
## 버그 요약
<!-- 한 문장으로 버그를 설명하세요 -->

## 재현 단계 (Steps to Reproduce)
1. 
2. 
3. 

## 예상 동작 (Expected Behavior)
<!-- 어떻게 동작해야 하는지 설명하세요 -->

## 실제 동작 (Actual Behavior)
<!-- 실제로 어떻게 동작하는지 설명하세요 -->

## 플랫폼 / 기기 정보 (Platform / Device Info)
- **플랫폼**: (Windows / macOS / iOS / Android / iPad / Android Tablet)
- **OS 버전**: (예: iOS 17.2, Android 14, Windows 11)
- **기기 모델**: (예: iPhone 15 Pro, Samsung Galaxy S24, MacBook Pro M3)
- **SWEF 버전**: (예: v1.0.0-rc1, 빌드 번호)
- **Unity 버전**: (에디터 재현 시)

## 스크린샷 / 로그 (Screenshots / Logs)
<!-- 스크린샷, Unity Console 로그, 크래시 스택 트레이스를 첨부하세요 -->

## 추가 정보
<!-- 재현 빈도, 특정 조건, 관련 PR/이슈 등 -->
```

---

## Issue Labels (이슈 라벨)

GitHub 이슈에 아래 라벨들을 사용하세요:

### 유형 라벨

| 라벨 | 색상 | 설명 |
|------|------|------|
| `bug` | 🔴 `#d73a4a` | 버그 / 의도치 않은 동작 |
| `critical` | 🟣 `#b60205` | 크리티컬 이슈 (크래시, 데이터 손실) |
| `performance` | 🟠 `#e4e669` | 성능 관련 이슈 (FPS, 메모리) |
| `enhancement` | 🟢 `#a2eeef` | 기능 개선 요청 |
| `question` | 🔵 `#d876e3` | 질문 또는 명확화 필요 |
| `test-flight` | 🟡 `#fbca04` | 테스트 비행 중 발견된 이슈 |

### 플랫폼 라벨

| 라벨 | 설명 |
|------|------|
| `platform:windows` | Windows PC |
| `platform:mac` | macOS |
| `platform:ios` | iPhone |
| `platform:android` | Android Phone |
| `platform:ipad` | iPad |
| `platform:android-tablet` | Android Tablet |

### 페이즈 라벨

| 라벨 | 대상 Phase |
|------|-----------|
| `phase:103` | Phase 103 — Live Flight Tracking |
| `phase:104` | Phase 104 — Flight Academy |
| `phase:105` | Phase 105 — Dynamic Terrain Events |
| `phase:106` | Phase 106 — Historical & Sci-Fi Flight |
| `phase:107` | Phase 107 — Live Streaming |
| `phase:108` | Phase 108 — UGC Editor |
| `phase:109` | Phase 109 — Clan/Squadron System |
| `phase:110` | Phase 110 — Dynamic NPC & Air Traffic |

---

## Priority Levels (우선순위)

| 레벨 | 이름 | 기준 | 목표 해결 시간 |
|------|------|------|--------------|
| **P0** | Critical | 크래시, 데이터 손실, 진행 불가 | 24시간 이내 |
| **P1** | Major | 핵심 기능 작동 불가 (비행 불가, HUD 표시 안 됨 등) | 3일 이내 |
| **P2** | Minor | 부분적 버그, 우회 가능 | 1주일 이내 |
| **P3** | Polish | UI 오타, 색상 어긋남, QoL 개선 | 여유 시간에 처리 |

---

## Bug Lifecycle (버그 라이프사이클)

```
New → Triaged → In Progress → Fixed → Verified → Closed
 ↑                                        ↓
 └─────────────── Reopened ───────────────┘
```

| 상태 | 설명 | 담당자 |
|------|------|--------|
| **New** | 이슈 생성됨, 아직 검토 안 됨 | 리포터 |
| **Triaged** | 우선순위/라벨 지정, 담당자 배정 | 팀 리드 |
| **In Progress** | 수정 작업 중 | 담당 개발자 |
| **Fixed** | 수정 완료, PR 머지됨 | 담당 개발자 |
| **Verified** | QA/테스터가 수정 확인 완료 | QA |
| **Closed** | 검증 완료, 이슈 닫힘 | QA / 팀 리드 |
| **Reopened** | 수정이 불완전하거나 회귀 발생 | 테스터 |

---

## Test Flight Checklist (테스트 비행 체크리스트)

각 테스트 비행 세션에서 아래 항목들을 확인하세요. Phase 102 `FinalQAChecklist`의 20개 시스템 영역을 기준으로 합니다.

### 🛫 비행 핵심 (Flight Core)
- [ ] **FP-001** — 앱 실행 후 GPS 위치 기반 이륙 정상 동작
- [ ] **FP-002** — 비행 물리 (중력, 양력, 항력) 정상 적용
- [ ] **FP-003** — 고도 변화에 따른 대기권 레이어 전환
- [ ] **FP-004** — 카르만 라인(100km) 도달 및 우주 진입 이벤트
- [ ] **FP-005** — 착륙/재진입 시 열 효과 및 물리 정상 동작

### 🕹️ 조작 (Controls)
- [ ] **CT-001** — PC: WASD + 마우스 입력 반응 정상
- [ ] **CT-002** — 모바일: 터치 조이스틱 및 제스처 정상 동작
- [ ] **CT-003** — 게임패드 연결 시 입력 정상 (Phase 98)
- [ ] **CT-004** — 키 리매핑 적용 정상 (Phase 98)
- [ ] **CT-005** — 한 손 모드 정상 동작 (Phase 34/93)

### 🌍 Cesium 타일 (Cesium Tiles)
- [ ] **CES-001** — 3D 타일 스트리밍 및 로드 정상
- [ ] **CES-002** — LOD 전환 시 팝핑 없음
- [ ] **CES-003** — 고속 이동 시 타일 로드 추적 정상
- [ ] **CES-004** — 타일 로드 실패 시 폴백 동작

### 📡 GPS
- [ ] **GPS-001** — 실제 기기에서 GPS 위치 정상 획득
- [ ] **GPS-002** — 권한 거부 시 기본 위치로 폴백
- [ ] **GPS-003** — 비행 중 GPS 신호 손실 처리

### 🌤️ 날씨 / 시간 (Weather / Day-Night)
- [ ] **WX-001** — 날씨 시스템 정상 동작 (구름, 비, 안개)
- [ ] **DN-001** — 낮/밤 사이클 정상 전환
- [ ] **DN-002** — 태양/달 위치 현실 반영

### 🖥️ HUD / UI
- [ ] **HUD-001** — 고도계, 속도계, 방위계 정상 표시
- [ ] **HUD-002** — 미니맵 정상 동작
- [ ] **HUD-003** — 일시 정지 메뉴 정상 동작
- [ ] **HUD-004** — 모바일 터치 버튼 반응 정상

### 🏆 업적 / 저장 (Achievement / Save)
- [ ] **ACH-001** — 업적 트리거 정상 동작
- [ ] **ACH-002** — 업적 알림 UI 표시 정상
- [ ] **JN-001** — 저장 슬롯 저장/불러오기 정상
- [ ] **JN-002** — 클라우드 동기화 정상 (네트워크 있을 때)

### 🌐 멀티플레이어 (Multiplayer)
- [ ] **MP-001** — 멀티플레이어 세션 생성/참가 정상
- [ ] **MP-002** — 편대 비행 동기화 정상
- [ ] **MP-003** — 음성 채팅 연결 정상

### 🤖 ARIA AI 코파일럿 (AI Co-Pilot)
- [ ] **ARIA-001** — ARIA 활성화 정상
- [ ] **ARIA-002** — 긴급 조언 트리거 정상
- [ ] **ARIA-003** — 오토파일럿 핸드오프 정상
- [ ] **ARIA-004** — ARIA 설정 저장/복원 정상

### 🔊 오디오 / 카메라 / 성능 (Audio / Camera / Performance)
- [ ] **AU-001** — 적응형 음악 상태 전환 정상 (Phase 83)
- [ ] **CAM-001** — 드론/시네마 카메라 정상 동작
- [ ] **PERF-001** — PC에서 60fps 이상 달성
- [ ] **PERF-002** — 모바일에서 30fps 이상 달성
- [ ] **PERF-003** — 메모리 예산 초과 없음

---

## Performance Targets (성능 기준)

Phase 102 `PerformanceBenchmarkConfig` 기준:

| 플랫폼 | 목표 FPS | 최저 FPS | RAM 예산 | 활성 타일 수 | 최소 대역폭 |
|--------|---------|---------|----------|------------|------------|
| Windows PC | 60 fps | 45 fps | 4,096 MB | 512 | 10 Mbps |
| macOS | 60 fps | 45 fps | 3,072 MB | 512 | 10 Mbps |
| iOS | 30 fps | 28 fps | 1,536 MB | 128 | 5 Mbps |
| Android | 30 fps | 28 fps | 1,536 MB | 128 | 5 Mbps |
| iPad | 60 fps | 45 fps | 2,048 MB | 256 | 5 Mbps |
| Android Tablet | 30 fps | 28 fps | 2,048 MB | 192 | 5 Mbps |

> 성능이 최저 FPS에 미달하면 P1 버그로 처리하세요.

---

## How to File a Bug (버그 신고 방법)

1. **GitHub 이슈 페이지** 이동: [https://github.com/Kohgane/skywalking-earthflight/issues](https://github.com/Kohgane/skywalking-earthflight/issues)
2. **New issue** 버튼 클릭
3. [Bug Report Template](#bug-report-template-버그-리포트-템플릿) 형식으로 내용 작성
4. 적절한 **라벨** 추가 (예: `bug`, `platform:ios`, `critical`)
5. **우선순위**에 따라 `P0`~`P3` 라벨 추가
6. 관련 **Phase** 라벨 추가 (예: `phase:103`)
7. 담당자(Assignee) 지정
8. **Submit new issue** 클릭

> **팁**: 재현 가능한 버그라면 Unity Console 로그와 스크린샷을 반드시 첨부해주세요. 재현 불가능한 버그는 `P3`로 처리하고 추가 정보를 모읍니다.

---

*이 문서는 SWEF v1.0.0-rc1 기준입니다. 자세한 QA 기준은 `Assets/SWEF/Scripts/QA/FinalQAChecklist.cs`를 참조하세요.*
