# Field Mapping (Kafka ↔ REST)

Kafka 이벤트 계약과 REST API 계약 사이에는 의도적으로 다른 필드명이 존재한다.
이 문서는 동일한 비즈니스 개념이 어떤 이름으로 노출되는지와 설계 근거를 정리한다.

## 매핑 테이블

| 개념 | Kafka 필드명 | REST API 필드명 | 설명 |
| --- | --- | --- | --- |
| 이벤트 발생 시각 | `timestamp` | `occurredAt` | Kafka는 스트리밍 메시지 관점의 이벤트 시각을 사용한다. REST의 `EventDto`는 조회/저장 맥락에서 발생 시각 필드를 `occurredAt`으로 노출한다. |
| 시뮬레이션 실행 ID | `runId` | `simulationRunId` | Kafka는 메시지 크기/가독성을 고려해 간결한 키(`runId`)를 사용한다. REST는 도메인 의미를 명확히 하기 위해 `simulationRunId`를 사용한다. |
| 이벤트 타입 | `eventType` | `eventType` | 동일한 도메인 식별자. 이벤트 라우팅과 분류의 기준으로 양쪽에서 동일하게 유지한다. |
| 대상 에셋 | `assetId` | `assetId` | 동일한 엔티티 식별자. 데이터 흐름 전체에서 추적 키를 일관되게 유지한다. |

## 설계 근거

- **계약 계층 분리**: Kafka는 이벤트 전송(transport) 중심, REST는 조회/응답(contract) 중심 네이밍을 허용한다.
- **도메인 가독성**: REST에서는 약어보다 명시적 이름(`simulationRunId`)을 우선해 API 소비자의 의미 해석 비용을 줄인다.
- **호환성 유지**: 기존 운영 경로에서 이미 사용 중인 필드명을 유지하면서 문서로 차이를 고정해 drift 오해를 방지한다.

## 기준 소스

- Kafka 엔벨로프: `shared/event-schemas/schemas/event-envelope.json`
- REST EventDto: `shared/api-schemas/openapi.json` (`components.schemas.EventDto`)
