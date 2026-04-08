# 검증: Orchestration 전력 미유입 (시드 vs Supplies vs 매핑)

## 가설 (코드 인사이트)

1. **시드 한정 BFS**: `RunOnePropagationAsync` 큐 초기값은 `ResolveTriggerAssetIds()`뿐. `SimulationParticipation`으로 참여 목록에 공급자가 있어도 **공급자 노드를 dequeue하기 전까지** `Supplies`(From→To) 규칙이 실행되지 않음.
2. **지속 시뮬 단일 시드 틱**: due가 전원이 아니면 tick마다 `TriggerAssetId` 하나로만 전파가 돌 수 있어, 특정 tick에는 공급자가 시드가 아님.
3. **매핑 실패**: `FromProperty`가 상태 키와 불일치하면 패치가 비고, `power`→`powerOut` 폴백은 `FromProperty` 정규화가 `power`일 때만 동작.

## 자동 검증 (xUnit)

| ID | 내용 |
|----|------|
| S1 | 시드만 Orchestrator일 때 upstream Supplier→Orchestrator Supplies가 실행되지 않아 target에 `powerIn` 패치 없음 |
| S2 | 동일 그래프에서 시드를 Supplier로 두면 target에 `powerIn` 생김 |
| M1 | `FromProperty`가 `power_in` 등일 때 소스에 `powerOut`만 있어도 매핑 결과 비어 있음 (`power` 폴백 미적용) |

## 수동 검증 (선택)

- 개발 로그에서 `Supplies` + 기대 매핑인데 `OutgoingPatch` 비는 경고 확인.
- 실제 run의 `triggerAssetIds` / 연속 run due 목록에 Center·Last 공급자 포함 여부 확인.
