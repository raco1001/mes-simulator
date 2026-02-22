# 이벤트 타입 분류 (Command / Observation)

Live 전환 시 "명령부만 교체"가 가능하도록 이벤트를 **입력(Command)** 과 **출력(Observation)** 으로 구분합니다.

---

## EventKind

| Kind | 의미 |
|------|------|
| **Command** | 시스템에 대한 명령(입력). 사용자·외부 시스템이 발생시킴. |
| **Observation** | 관측 결과(출력). 상태 변경·센서 값 등 엔진/시뮬레이터가 생성. |

- `eventType` 문자열 값으로 Command vs Observation을 구분한다.
- 동일한 EventDto/eventType 스키마를 유지하며, 값의 집합만 두 계열로 나눈다.

---

## Observation 이벤트 타입

시뮬레이터 또는 Live Adapter가 **생성·주입**하는 이벤트. 현재 시뮬레이터에서 사용하는 것만 나열하고, 확장용을 함께 적는다.

| eventType | 설명 |
|-----------|------|
| `simulation.state.updated` | 시뮬레이션 전파로 에셋 상태가 갱신됨. (현재 주로 사용.) |
| `power_changed` | 전력 값 변경. |
| `state_transitioned` | 상태 전이(예: normal → warning). |

- **코드 상수**: `EventTypes.SimulationStateUpdated`, `EventTypes.PowerChanged`, `EventTypes.StateTransitioned` 등.

---

## Command 이벤트 타입

시스템에 대한 **명령**. 시뮬레이터는 이 타입을 생성하지 않으며, Live 모드에서 Adapter·UI 등이 발생시킬 수 있다. 타입·문서만 정의.

| eventType | 설명 |
|-----------|------|
| `start_machine` | 기기/머신 시작 명령. |
| `stop_machine` | 기기/머신 정지 명령. |
| `change_speed` | 속도 변경 명령. |

- **코드 상수**: `EventTypes.StartMachine`, `EventTypes.StopMachine`, `EventTypes.ChangeSpeed` 등.

---

## 시뮬레이터 vs Live 계약

- **시뮬레이터**: **Observation**(및 필요 시 가짜 Command)만 생성한다. Command 이벤트는 현재 생성하지 않음.
- **Live 모드**: Adapter가 실제 소스(PLC, MQTT 등)에서 데이터를 받아 **Observation** 형태로 정규화하여 엔진에 주입한다. 명령은 Command 이벤트로 처리할 수 있다.

이를 통해 동일한 엔진(apply 경로)을 시뮬레이션과 Live에서 공유하고, "명령부만 교체"하여 전환할 수 있다.

---

## 참고

- [event-replay-contract.md](event-replay-contract.md) — runId, tick, 재생 순서 계약.
- [api-schemas.md](api-schemas.md) — REST API·Kafka 메시지 형식.
