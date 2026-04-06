# 대화 기록 — 2026-04-06: Unity 셋업 가이드 & 다음 단계 정리

> **참여자**: @Kohgane, @copilot  
> **날짜**: 2026-04-06  
> **주제**: 이전 대화 복구, Unity 프로젝트 셋업 가이드, 다음 단계 정의

---

## 배경

- 이전 대화 내용이 소실됨
- 마지막 대화는 Unity 설정법 설명 중이었고, 코드적으로 할 건 다 해서 컴파일 에러를 잡자는 얘기가 있었음

## 레포 현황 분석 결과

### ✅ 완료된 것
- Phase 1~120 전부 코드 작성 완료 (총 120개 Phase, C# 스크립트 80개+ 모듈)
- 88개+ `.asmdef` (Assembly Definition) 파일 생성 + 크로스 레퍼런스 정리 (Phase 101)
- CI/CD 워크플로우 — `.asmdef` 유효성 검증, 빌드 매트릭스
- NUnit 테스트 — EditMode / Integration 테스트
- Release Candidate v1.0.0-rc1 문서 완료
- Post-launch Phase 103~120 전부 완료 (v3.0 Roadmap Complete)

### ❌ 아직 안 된 것
- Unity Editor에서 씬 와이어링 (Boot.unity, World.unity에 매니저 배치)
- Cesium for Unity 패키지 설치
- 추가 패키지 확인 (TextMeshPro, Input System, Addressables)
- 컴파일 에러 확인 및 해결
- Google Map Tiles API 키 설정
- 첫 테스트 플레이

## 다음 단계 (우선순위 순)

| 순서 | 할 일 | 상태 |
|------|-------|------|
| 1 | Unity 2022.3 LTS로 프로젝트 열기 | ⬜ |
| 2 | Cesium for Unity 패키지 설치 (`com.cesium.unity`) | ⬜ |
| 3 | TextMeshPro, Input System 패키지 확인/설치 | ⬜ |
| 4 | Console에서 컴파일 에러 확인 | ⬜ |
| 5 | 에러 메시지 공유 → Copilot과 해결 | ⬜ |
| 6 | Boot.unity에 매니저 배치 (BootManager, SWEFSession, SettingsManager, SaveManager, AudioManager, AccessibilityManager) | ⬜ |
| 7 | World.unity에 매니저 배치 (FlightController, AltitudeController, CesiumTerrainBridge, WeatherManager, TimeOfDayManager, HUDDashboard) | ⬜ |
| 8 | Cesium 오브젝트 추가 (CesiumGeoreference + Cesium3DTileset) | ⬜ |
| 9 | Build Settings에 씬 추가 (Boot=0, World=1) | ⬜ |
| 10 | Google Map Tiles API 키 설정 | ⬜ |
| 11 | 첫 테스트 플레이 | ⬜ |

## 참고 문서
- `SCENE_SETUP_GUIDE.md` — 씬 셋업 전체 가이드
- `RELEASE_NOTES_v1.0.0-rc1.md` — RC1 릴리즈 노트
- `PHASE_ROADMAP.md` — 전체 Phase 로드맵
- `Assets/SWEF/README_SWEF_SETUP.md` — 상세 셋업 가이드

## 핵심 트러블슈팅 메모

| 에러 유형 | 원인 | 해결 |
|-----------|------|------|
| `CesiumGeoreference could not be found` | Cesium 패키지 미설치 | Package Manager에서 `com.cesium.unity` 설치 |
| `TMP_Text could not be found` | TextMeshPro 미설치 | Package Manager에서 TextMeshPro 설치 |
| `Assembly 'SWEF.XXX' has reference to 'SWEF.YYY'` | .asmdef 참조 깨짐 | 해당 폴더에 .asmdef 파일 확인 |
| `Multiple assemblies with name` | .asmdef 중복 | 중복 파일 삭제 |

---

*다음 대화에서는 Unity 열고 나온 컴파일 에러를 공유할 예정*
