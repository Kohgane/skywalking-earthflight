# 대화 기록 — 2026-04-09: Unity 6 컴파일 에러 수정

> **참여자**: @Kohgane, @copilot  
> **날짜**: 2026-04-09  
> **주제**: Unity 6000.3.12f1 Safe Mode 컴파일 에러 전면 해결

---

## 배경

- Unity 6000.3.12f1 (DX11)로 프로젝트를 열었더니 Safe Mode 진입
- 초기 **7개 에러 + 1개 경고** 발생
- 이전 대화에서 TMPro 관련 에러는 이미 해결 완료 상태

## 발생한 에러 목록

| # | 파일 | 에러 코드 | 내용 |
|---|------|----------|------|
| 1 | `RecorderUI.cs:140` | CS0234 | `SWEF` 네임스페이스에 `UI`가 없음 (assembly reference 누락) |
| 2 | `DebugConsole.cs:305` | CS0103 | `ScreenCapture`를 현재 컨텍스트에서 찾을 수 없음 |
| 3 | `AchievementUI.cs:59` | CS0426 | `AchievementDefinition`이 `AchievementManager` 타입에 없음 |
| 4 | `AchievementUI.cs:91` | CS0426 | 위와 동일 |
| 5 | `IAPManager.cs:213` | CS0103 | `AnalyticsLogger`를 현재 컨텍스트에서 찾을 수 없음 |
| 6 | `ReverbZoneManager.cs:94` | CS0117 | `AudioReverbPreset`에 `Carpetedhallway` 정의 없음 |
| 7 | `ReverbZoneManager.cs:131` | CS0117 | 위와 동일 |
| ⚠️ | `IAPManager.cs:54` | CS0067 | `OnPurchaseFailed` 이벤트 미사용 (경고) |

## 원인 분석 & 해결

### 1. RecorderUI — `SWEF.UI.ReplayBrowserUI` 참조 (순환 참조 문제)

**원인**: `RecorderUI.cs`가 `FindFirstObjectByType<SWEF.UI.ReplayBrowserUI>()`로 UI 타입을 직접 참조. `SWEF.Recorder` asmdef에 `SWEF.UI` 참조를 추가하면 **순환 참조** 발생 (`SWEF.UI → SWEF.IAP → ... → SWEF.Recorder → SWEF.UI`).

**해결**: 타입 직접 참조 대신 **리플렉션 + SendMessage** 패턴 사용

```csharp
// Before
var browser = FindFirstObjectByType<SWEF.UI.ReplayBrowserUI>();
browser.Refresh();

// After
var type = System.Type.GetType("SWEF.UI.ReplayBrowserUI, SWEF.UI");
if (type == null) { Debug.LogWarning("..."); return; }
var browser = FindFirstObjectByType(type) as MonoBehaviour;
browser.SendMessage("Refresh");
```

### 2. DebugConsole — `ScreenCapture` 미인식

**원인**: `#if DEVELOPMENT_BUILD || UNITY_EDITOR` 블록 내에서 `ScreenCapture.CaptureScreenshot()` 호출. Unity 6에서 모듈 분리로 단순 이름 인식 안 됨.

**해결**: 정규화된 이름 사용

```csharp
// Before
ScreenCapture.CaptureScreenshot(path);

// After
UnityEngine.ScreenCapture.CaptureScreenshot(path);
```

### 3. AchievementUI — `AchievementManager.AchievementDefinition` 참조 오류

**원인**: `AchievementDefinition`은 `AchievementManager`의 nested class가 아닌 **별도 ScriptableObject** 클래스 (`AchievementDefinition.cs:33`). 코드가 `AchievementManager.AchievementDefinition`으로 잘못 참조.

**해결**: 전체 치환

```csharp
// Before
AchievementManager.AchievementDefinition

// After  
AchievementDefinition
```

### 4. IAPManager — `AnalyticsLogger` 참조 (순환 참조 문제)

**원인**: `AnalyticsLogger`는 `SWEF.Core` asmdef에 속함. `SWEF.IAP`에 `SWEF.Core`를 추가하면 **순환 참조** 발생 (`SWEF.Core → SWEF.IAP → SWEF.Core`).

**해결**: 리플렉션으로 호출

```csharp
// Before
AnalyticsLogger.LogEvent("iap_purchase", productId);

// After
try {
    var loggerType = System.Type.GetType("SWEF.Core.AnalyticsLogger, SWEF.Core");
    if (loggerType != null) {
        var method = loggerType.GetMethod("LogEvent", new[] { typeof(string), typeof(string) });
        if (method != null) method.Invoke(null, new object[] { "iap_purchase", productId });
    }
} catch (System.Exception ex) {
    Debug.LogWarning($"[SWEF] IAPManager: AnalyticsLogger not available: {ex.Message}");
}
```

### 5. ReverbZoneManager — `AudioReverbPreset.Carpetedhallway` 오타

**원인**: Unity API의 정확한 enum 값은 `CarpetedHallway` (대문자 H).

**해결**: sed 일괄 치환

```bash
sed -i 's/AudioReverbPreset\.Carpetedhallway/AudioReverbPreset.CarpetedHallway/g' ReverbZoneManager.cs
```

## 수정된 파일 요약

| 파일 | 변경 내용 |
|------|----------|
| `Assets/SWEF/Scripts/Recorder/RecorderUI.cs` | SWEF.UI 직접 참조 → 리플렉션 + SendMessage |
| `Assets/SWEF/Scripts/Recorder/SWEF.Recorder.asmdef` | SWEF.UI 참조 추가 시도 → 순환 참조 → 원복 |
| `Assets/SWEF/Scripts/DebugOverlay/DebugConsole.cs` | `ScreenCapture` → `UnityEngine.ScreenCapture` |
| `Assets/SWEF/Scripts/Achievement/AchievementUI.cs` | `AchievementManager.AchievementDefinition` → `AchievementDefinition` |
| `Assets/SWEF/Scripts/IAP/SWEF.IAP.asmdef` | SWEF.Core 참조 추가 시도 → 순환 참조 → 원복 |
| `Assets/SWEF/Scripts/IAP/IAPManager.cs` | `AnalyticsLogger.LogEvent` → 리플렉션 호출 |
| `Assets/SWEF/Scripts/SpatialAudio/Propagation/ReverbZoneManager.cs` | `Carpetedhallway` → `CarpetedHallway` |

## 핵심 교훈

| 패턴 | 설명 |
|------|------|
| **순환 참조 회피** | asmdef 간 순환 의존 시 리플렉션 + SendMessage로 decoupling |
| **Unity 6 모듈 분리** | `ScreenCapture` 등이 별도 모듈로 분리됨 → 정규화된 이름 사용 |
| **Nested class 착각** | 별도 클래스를 nested class로 참조하는 실수 주의 |
| **enum 대소문자** | Unity API enum은 정확한 PascalCase 필요 |

## 현재 상태

- ⏳ Unity 재시작하여 에러 해소 확인 중
- 순환 참조 에러가 남아있을 가능성 있음 → 결과 확인 후 대응 예정

---

*다음 대화에서 Unity 재시작 결과 공유 예정*