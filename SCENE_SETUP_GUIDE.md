# 씬 셋업 가이드 — SWEF Scene Setup Guide

> **목표**: Unity Editor에서 SWEF를 열고 첫 테스트 비행까지 진행하는 단계별 가이드입니다.

---

## Prerequisites (사전 요구사항)

| 항목 | 버전 / 참고 |
|------|-------------|
| **Unity** | 2022.3 LTS (URP 렌더 파이프라인 포함) |
| **Cesium for Unity** | 최신 버전 ([패키지 페이지](https://cesium.com/platform/cesium-for-unity/)) |
| **Google Map Tiles API Key** | [Google Cloud Console](https://console.cloud.google.com/) — `Map Tiles API` 활성화 필요 |
| **Git** | 2.x 이상 |
| **OS** | Windows 10+, macOS 12+ |

---

## Step 1: Project Setup (프로젝트 셋업)

### 1-1. 레포지토리 클론

```bash
git clone https://github.com/Kohgane/skywalking-earthflight.git
cd skywalking-earthflight
```

### 1-2. Unity Hub에서 열기

1. Unity Hub 실행 → **Add** → 클론된 폴더 선택
2. Unity **2022.3 LTS** 에디터가 설치되어 있는지 확인
3. 에디터가 자동으로 `Assets/` 를 임포트합니다 (최초 임포트는 수 분 소요)

### 1-3. Cesium for Unity 패키지 설치

1. Unity 메뉴 → **Window → Package Manager**
2. 좌상단 **+** → **Add package by name**
3. `com.cesium.unity` 입력 후 **Add**
4. 또는 [Cesium for Unity GitHub](https://github.com/CesiumGS/cesium-unity) 에서 `.tgz` 파일 직접 설치

### 1-4. Google Map Tiles API 키 설정

1. `Assets/SWEF/Resources/` 폴더에 `SwefConfig.asset` ScriptableObject 생성 (또는 기존 파일 열기)
2. `Google Map Tiles Api Key` 필드에 API 키 입력
3. API 키는 절대 Git에 커밋하지 마세요. `.gitignore`에 `SwefConfig.asset`을 추가하거나 환경 변수를 사용하세요.

---

## Step 2: Scene Structure (씬 구조)

SWEF는 **두 개의 씬**으로 구성됩니다:

| 씬 파일 | 경로 | 역할 |
|---------|------|------|
| `Boot.unity` | `Assets/SWEF/Scenes/Boot.unity` | 앱 초기화, 싱글턴 매니저 로드, 로딩 화면 |
| `World.unity` | `Assets/SWEF/Scenes/World.unity` | Cesium 지형, 플라이트 컨트롤러, 게임플레이 |

**씬 로드 순서**: `Boot.unity` → (초기화 완료) → `World.unity` (Additive 또는 Single 로드)

- `Boot.unity`는 앱 시작 시 **첫 번째로 로드**되어야 합니다. **Build Settings**에서 `Boot.unity`가 인덱스 0인지 확인하세요.
- `World.unity`에는 Cesium Tileset, 카메라, 항공기 프리팹이 배치됩니다.

---

## Step 3: Core Manager Setup (핵심 매니저 셋업)

아래 싱글턴 매니저들을 씬의 **빈 GameObject**로 배치해야 합니다. 각 매니저는 `DontDestroyOnLoad`를 사용합니다.

### Boot.unity에 배치할 매니저 (필수)

| 매니저 클래스 | 네임스페이스 / 폴더 | GameObject 이름 |
|--------------|---------------------|-----------------|
| `BootManager` | `Core/` | `[BootManager]` |
| `SWEFSession` | `Core/` | `[SWEFSession]` |
| `SettingsManager` | `Settings/` | `[SettingsManager]` |
| `SaveManager` | `SaveSystem/` | `[SaveManager]` |
| `AudioManager` | `Audio/` | `[AudioManager]` |
| `AccessibilityManager` | `Accessibility/` | `[AccessibilityManager]` |

### World.unity에 배치할 매니저

| 매니저 클래스 | 네임스페이스 / 폴더 | GameObject 이름 |
|--------------|---------------------|-----------------|
| `FlightController` | `Flight/` | `[FlightController]` |
| `AltitudeController` | `Flight/` | `[AltitudeController]` |
| `CesiumTerrainBridge` | `Terrain/` | `[CesiumTerrainBridge]` |
| `WeatherManager` | `Weather/` | `[WeatherManager]` |
| `TimeOfDayManager` | `TimeOfDay/` | `[TimeOfDayManager]` |
| `HUDDashboard` | `CockpitHUD/` | `[HUDDashboard]` |

> **팁**: 각 매니저를 빈 GameObject에 Add Component하고, Inspector에서 SerializedField 참조를 연결해주세요.

---

## Step 4: Cesium Configuration (Cesium 설정)

### 4-1. CesiumGeoreference 설정

1. `World.unity`에 빈 GameObject 생성 → 이름: `CesiumGeoreference`
2. **Add Component** → `CesiumGeoreference`
3. 위도/경도를 테스트 위치로 설정 (예: 서울 37.5665° N, 126.9780° E)

### 4-2. Cesium3DTileset 추가

1. `CesiumGeoreference` 하위에 빈 GameObject → 이름: `GooglePhotorealistic3DTileset`
2. **Add Component** → `Cesium3DTileset`
3. **Ion Asset ID** 또는 **URL** 설정:
   - Google Photorealistic 3D Tiles 사용 시: `https://tile.googleapis.com/v1/3dtiles/root.json?key=YOUR_API_KEY`
   - Cesium ion 사용 시: ion 토큰과 에셋 ID 입력

### 4-3. ion 토큰 설정

1. Unity 메뉴 → **Cesium → Cesium**
2. **ion Access Token** 필드에 Cesium ion 토큰 입력
3. Google Map Tiles 전용 사용 시 이 단계는 생략 가능

---

## Step 5: First Test Flight (첫 테스트 비행)

### 5-1. Play Mode 진입

1. `Boot.unity`가 열린 상태에서 Unity 에디터 상단 ▶ **Play** 버튼 클릭
2. 콘솔(Console) 창에서 에러가 없는지 확인
3. `BootManager`가 초기화 완료 후 `World.unity`로 전환되는지 확인

### 5-2. 예상 동작

- Cesium 3D Tiles가 카메라 주변으로 스트리밍되어 표시됩니다
- GPS는 Editor에서 사용 불가 → 기본 위치(위도/경도 하드코딩)로 시작합니다
- HUD 대시보드가 화면에 표시됩니다
- 고도계, 속도계, 방위계가 정상 작동합니다

### 5-3. 기본 조작 (PC)

| 키 | 동작 |
|----|------|
| `W` / `S` | 피치 (앞/뒤 기울기) |
| `A` / `D` | 롤 (좌/우 기울기) |
| `Q` / `E` | 요 (좌/우 회전) |
| `Space` | 스로틀 증가 |
| `Shift` | 스로틀 감소 |
| 마우스 이동 | 카메라 방향 |
| `Escape` | 일시 정지 메뉴 |

### 5-4. 모바일 조작

| 제스처 | 동작 |
|--------|------|
| 조이스틱 (왼쪽) | 방향 제어 |
| 조이스틱 (오른쪽) | 카메라 |
| 싱글 탭 | 스로틀 토글 |
| 더블 탭 | 오토파일럿 |
| 핀치 인/아웃 | 고도 제어 |

---

## Step 6: Troubleshooting (트러블슈팅)

### 타일이 로드되지 않을 때

- **원인**: API 키 미설정 또는 인터넷 연결 없음
- **해결**: `SwefConfig.asset`에 API 키가 올바르게 입력되었는지 확인. Console에서 `403` 또는 `401` 에러 검색.

### GPS가 Editor에서 작동하지 않을 때

- **원인**: GPS는 실제 기기에서만 사용 가능 (Unity Editor는 지원 안 함)
- **해결**: `SWEFSession`의 `UseHardcodedGPSInEditor` 옵션을 활성화하고 테스트 좌표를 설정하세요.

### Assembly 에러 / 컴파일 실패

- **원인**: `.asmdef` 참조 누락 또는 Cesium 패키지 미설치
- **해결**:
  1. Package Manager에서 Cesium for Unity 설치 확인
  2. Unity 메뉴 → **Assets → Reimport All**
  3. `.github/workflows/validate-assembly-refs.yml` 워크플로우 참고

### NullReferenceException — 매니저를 찾을 수 없음

- **원인**: 씬에 필수 매니저 GameObject가 없음
- **해결**: [Step 3](#step-3-core-manager-setup-핵심-매니저-셋업) 체크리스트 재확인

### 성능이 너무 낮을 때 (Editor)

- Editor에서는 실제 기기보다 성능이 낮을 수 있습니다
- Cesium 타일 LOD 조정: `Cesium3DTileset` → `Maximum Screen Space Error` 값을 높이면 타일 해상도 감소 → 성능 향상

---

## Platform Support Matrix (플랫폼 지원 현황)

> 자세한 내용은 `README.md`의 Platform Support Matrix 섹션을 참조하세요.

| 플랫폼 | 우선순위 | 상태 |
|--------|----------|------|
| Windows PC | Primary | ✅ 지원 |
| macOS | Primary | ✅ 지원 |
| iOS (iPhone) | Primary | ✅ 지원 |
| Android (Phone) | Primary | ✅ 지원 |
| iPad | High | ✅ 지원 (Phase 97) |
| Android Tablet | High | ✅ 지원 (Phase 97) |
| Meta Quest / XR | Secondary | 계획 중 |
| Apple Vision Pro | Secondary | 계획 중 |

---

*이 문서는 SWEF v1.0.0-rc1 기준입니다. 문제가 있으면 [이슈](https://github.com/Kohgane/skywalking-earthflight/issues)를 열어주세요.*
