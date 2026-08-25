# skywalking-earthflight

> **Unity 트랙 전용 저장소** — 2026-08 웹 피벗 이후 이 저장소는 Unity 클라이언트 개발만 담당합니다.

---

## 요약

| 항목 | 내용 |
|------|------|
| 저장소 역할 | Unity 3D 지구 비행 뷰어 (Unity 트랙) |
| 웹 앱 본체 | [`Kohgane/LinkLynk`](https://github.com/Kohgane/LinkLynk) › `static/fly/` |
| 웹 앱 배포 URL | <https://linklynk.onrender.com/fly/> |
| 현재 씬 | `MainScene` |
| 요구 Unity 버전 | Unity 6.3 LTS |

---

## 현재 상태

2026년 8월 웹 피벗 이후, **웹 앱(`/fly/`)은 `Kohgane/LinkLynk` 저장소**에서 개발·배포됩니다.  
이 저장소(`skywalking-earthflight`)는 Unity 기반 3D 클라이언트의 지속 개발을 위해 유지됩니다.

---

## 웹 앱 위치

- **소스**: [`Kohgane/LinkLynk`](https://github.com/Kohgane/LinkLynk) → `static/fly/`
- **배포**: <https://linklynk.onrender.com/fly/>

Unity 빌드 없이 즉시 사용할 수 있는 웹 버전은 위 URL을 이용하세요.

---

## Unity 트랙 — 실행 방법

### 요구 환경

- **Unity 6.3 LTS**
- **Cesium for Unity** 패키지 (Package Manager에서 설치)
- **Cesium ion 토큰** (환경 변수 또는 에디터 내 설정)

### 실행 절차

1. 이 저장소를 클론합니다.
2. Unity Hub에서 **Unity 6.3 LTS**로 프로젝트를 엽니다.
3. Package Manager → Cesium for Unity 패키지를 설치합니다.
4. Cesium ion 토큰을 `Cesium → Cesium ion` 패널에 입력합니다.
5. `Assets/Scenes/MainScene`을 열고 Play합니다.

---

## 현재 씬 구성

### 씬: `MainScene`

| 구성요소 | 역할 |
|----------|------|
| `CesiumGeoreference` | 지구 좌표계 기준 설정 |
| `Cesium3DTileset` | 3D 타일 지구 렌더링 |
| `DynamicCamera` | 비행 카메라 제어 |

### 주요 스크립트

| 스크립트 | 기능 |
|----------|------|
| `LandmarkMenu.cs` | 명소 텔레포트 — 세계 명소 목록에서 선택해 즉시 이동 |
| `SunDial.cs` | 시간대별 대기·조명 — 시각에 따른 하늘색·태양 위치 연동 |

### 렌더링 / 후처리

- **URP** (Universal Render Pipeline)
- **Global Volume**: Bloom, ACES 톤맵핑, Vignette

---

## `_Parked/` 디렉터리 주의사항

> ⚠️ **`_Parked/`의 파일을 현재 빌드 범위로 복원하지 마세요.**

`_Parked/`는 **구 아키텍처 유산**입니다.

- 이 디렉터리에는 **1,411개의 C# 스크립트**가 포함되어 있었습니다.
- `.meta` 파일 부재로 인해 **asmdef GUID가 손상**되어 있습니다.
- 현재 **빌드에서 제외된 상태**이며, 의도적으로 분리되어 있습니다.
- 실수로 `Assets/` 하위 활성 경로로 이동하면 수십 개의 asmdef 참조 오류가 발생합니다.

이 폴더는 히스토리 보존 목적으로만 유지됩니다.

---

## 관련 이슈 메모

| 이슈 | 상태 | 메모 |
|------|------|------|
| [#125](../../issues/125) | 아카이브 대상 | 웹 피벗 이전 기획 — 현재 방향과 무관 |
| [#133](../../issues/133) | 아카이브 대상 | 웹 피벗 이전 기획 — 현재 방향과 무관 |

이슈 #125, #133은 2026-08 웹 피벗 이전에 작성된 기획으로, 현재 Unity 트랙 또는 웹 앱 방향과 맞지 않습니다. 향후 아카이브 처리 예정입니다.

---

## 관련 링크

- 웹 앱 저장소: [Kohgane/LinkLynk](https://github.com/Kohgane/LinkLynk)
- 웹 앱 배포: <https://linklynk.onrender.com/fly/>
