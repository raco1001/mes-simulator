# 시뮬레이션 엔진 Tick 규칙 (에셋별 tick 스키마·엔진 규칙)

에셋별 tick 주기 스키마와 "due 에셋만 처리" 엔진 규칙을 정의합니다. Phase 6.4에서 전역 tick 또는 due 기반 구현 선택 시 이 문서를 기준으로 합니다.

---

## 1. 에셋 tick 스키마

에셋 단위 tick 설정은 **AssetDto.Metadata**에 키로 저장합니다. (Application: [AssetDto](../../servers/backend/DotnetEngine/Application/Asset/Dto/AssetDto.cs) — `IReadOnlyDictionary<string, object> Metadata`)

### 정의

| 키 | 타입 | 필수 | 의미 | 기본 동작 |
|----|------|------|------|------------|
| **tickIntervalMs** | number (정수) | 아니오 | 에셋 tick 주기(밀리초). 해당 에셋이 몇 ms마다 한 번씩 "tick이 due"로 간주되는지. | **0 또는 미설정**이면 Run 기본 주기(전역 tick) 사용. 즉, Run의 한 tick에 모든 참여 에셋이 함께 처리되는 모드. |
| **tickPhaseMs** | number (정수) | 아니오 | 오프셋(밀리초). 같은 주기 내에서 phase 조정용. | 미설정 시 0으로 간주. |

### Run 전역 tick만 사용

모든 에셋에서 `tickIntervalMs`를 넣지 않거나 0으로 두면, **Run 전역 tick**만 사용하는 모드가 됩니다. 이때 "Run 내 참여 에셋 전체가 한 tick에 한 번에 처리"되며, 기존 BFS 1회 = 한 tick과 동일합니다.

### 스키마 계약

- API 스키마·metadata 규약: [shared/api-schemas.md](../shared/api-schemas.md) — AssetDto metadata에 시뮬레이션 tick용 `tickIntervalMs`, `tickPhaseMs` (선택) 사용.

---

## 2. 엔진 규칙: due 에셋만 처리

### 스텝(또는 Run tick) 한 번의 동작

매 스텝(또는 Run tick 1회)에서:

1. **이번에 tick이 due인 에셋**만 수집한다.
2. **그 due 에셋에 대해서만** update·전파·이벤트를 수행한다.

즉, 루프 구조는 "한 번에 한 BFS"가 아니라 **"due 에셋 수집 → due 에셋만 전파/이벤트"**로 확장 가능하게 설계합니다.

### due 판단

- **입력**: 현재 시각(또는 Run tick 번호), 에셋의 `tickIntervalMs`, 해당 에셋의 **lastTick** 시점(또는 lastTickIndex).
- **의미**: 에셋 A의 다음 tick 시각 = `lastTick(A) + tickIntervalMs` (phase는 `tickPhaseMs`로 오프셋 적용).
- **due 조건**: 현재 Run tick 시각(또는 시뮬 시각) ≥ 에셋의 "다음 tick 시각"이면 해당 에셋은 이번 스텝에서 due.

(에셋별 tick을 쓰지 않을 때는 lastTick을 "Run 시작 시각"으로 두고, tickIntervalMs = 0 또는 미설정이면 항상 due로 간주.)

### 전역 tick 모드

**due = "Run에 참여하는 모든 에셋"**으로 정의하면, 기존 "BFS 1회 = 한 tick에 전체 처리"와 동일합니다. 즉, 전역 tick 모드에서는 매 스텝에서 due 에셋 집합을 "참여 에셋 전체"로 두면 됩니다.

---

## 3. 6.4 설계 방향

- **루프 구조**: "due 에셋 수집 → due 에셋만 전파/이벤트". 첫 구현은 **due = 전체(전역 tick)**로 두어 기존 동작을 유지할 수 있음.
- **확장**: 나중에 에셋별 `tickIntervalMs`를 읽어 due 집합을 제한하면, "due 에셋만 처리"하는 동일 루프로 확장 가능.

---

## 참고

- [simulation-api.md](simulation-api.md) — 시뮬레이션 API·Run·전파 진입점
- [temp.phases.md](../../temp.phases.md) — Phase 6.2.5·6.4 로드맵
