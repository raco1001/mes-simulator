# 시뮬레이션 이벤트 Replay 계약

runId와 tick(seq)만으로 이벤트 스트림을 재구성할 수 있도록 하는 계약입니다. 실제 재생 구현은 7c.1에서 다룹니다.

---

## 필수 필드

모든 시뮬레이션 이벤트에는 다음이 포함되어야 합니다.

| 필드 | 설명 | 위치 |
|------|------|------|
| **runId** | 시뮬레이션 Run 식별자 | `simulationRunId` (EventDto), Kafka 메시지 `runId` |
| **tick** | Run 전역 tick 번호 (해당 전파 스텝) | `payload.tick` |
| **occurredAt** | 이벤트 발생 시각 | `occurredAt` |
| **eventType** | 이벤트 타입 (Command 또는 Observation 계열) | `eventType` |
| **payload** | 이벤트별 데이터 (tick 포함) | `payload` |

- DB(events 컬렉션): EventDto 기준으로 `SimulationRunId`, `OccurredAt`, `EventType`, `Payload`(내부에 `tick`) 저장.
- Kafka(factory.asset.events): `runId`, `eventType`, `assetId`, `timestamp`, `payload`(내부에 `tick`) 발행.
- `eventType`은 Command 또는 Observation 계열이며, 상세 분류는 [event-types.md](event-types.md)를 참고하세요.

---

## 재생 순서

**runId + tick 순서로 같은 이벤트 스트림 재생 가능**이 보장됩니다.

- 정렬 키: **(runId, tick, occurredAt)**.
- 동일 runId·동일 tick 내 여러 이벤트는 `occurredAt` 순으로 재생.
- 단건 실행(run 1회 전파 후 완료)은 tick=0으로 1회만 전파되므로 tick=0 이벤트만 존재.

---

## 에셋별 tick 확장 시

에셋별 tick을 사용할 경우 payload에 다음을 포함할 수 있습니다.

- **assetId**: 이벤트가 발생한 에셋 (이미 최상위 `assetId`로 있음).
- **assetTickIndex**: 해당 에셋의 tick 횟수.

재생 순서 규칙:

- **(runId, runTick)** 로 먼저 묶고, 동일 runTick 내에서는 **(occurredAt, assetId, assetTickIndex)** 순으로 재생 가능.
- 또는 **(runId, occurredAt, assetId, assetTickIndex)** 순으로 재생해도 "머신 A의 N번째 tick" 단위 분석 가능.

---

## Event Envelope (참고)

Replay·파이프라인 소비 시 공통으로 기대할 수 있는 봉투 형태:

- **eventId** (선택): 이벤트 고유 ID. 현재 DB는 ObjectId, Kafka는 미설정.
- **sourceId** / **assetId**: 이벤트가 발생한 에셋.
- **runId**: 시뮬레이션 Run ID.
- **occurredAt** / **timestamp**: 발생 시각.
- **seq** / **tick**: Run 전역 tick (payload.tick과 동일).
- **type** / **eventType**: 이벤트 타입.
- **payload**: 이벤트별 데이터.

API 스키마·Kafka 메시지 형식은 [api-schemas.md](api-schemas.md) 및 백엔드 KafkaEventPublisher를 참고하세요.
