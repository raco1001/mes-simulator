# Event Contract Scope (Public vs Internal)

`shared/event-schemas/`는 서비스 경계를 넘는 이벤트 계약(public)만 관리한다.
내부 구현 상세 이벤트(internal)는 각 서비스 코드베이스 내부에서 관리하며, 이 디렉토리에 추가하지 않는다.

---

## 분류 기준

| 분류 | 기준 | 관리 위치 |
| --- | --- | --- |
| `public` | 다른 서비스/프로젝트가 소비하거나, 거버넌스 대상으로 버전 관리해야 하는 이벤트 | `shared/event-schemas/schemas/*.json` |
| `internal` | 단일 서비스 내부 구현 세부(도메인 내부 전이/중간 상태)로만 쓰이는 이벤트 | 각 서비스 내부 문서/코드 |

---

## 현재 이벤트 분류

| eventType | 분류 | 근거 | 주요 소비자 |
| --- | --- | --- | --- |
| `asset.created` | public | 자산 생성 사실을 서비스 간 계약으로 공유 | pipeline, backend |
| `asset.health.updated` | public | 자산 상태 관측값 교환의 표준 이벤트 | pipeline, backend |
| `simulation.state.updated` | public | 시뮬레이션 전파 결과를 외부 파이프라인까지 전달 | pipeline, governance 검증 대상 |
| `simulation.tick.started` | public | 지속 시뮬 엔진 사이클 시작 봉투 | pipeline (선택적 배치 경계) |
| `simulation.tick.completed` | public | 지속 시뮬 엔진 사이클 완료 봉투 | pipeline (선택적 배치 경계) |
| `alert.generated` | public | 알림 생성 결과를 API/UI 경로로 전달 | backend alert consumer, REST API, frontend |
| `power_changed` | internal (예시) | 내부 구현 상세 관측 이벤트, 현재 shared 스키마 미등록 | 단일 서비스 내부 |
| `state_transitioned` | internal (예시) | 내부 상태 전이 추적 목적, 외부 계약 불필요 | 단일 서비스 내부 |

---

## 저장소 규칙

1. `shared/event-schemas/schemas/`에는 `public` 이벤트 스키마만 추가한다.
2. 신규 이벤트는 먼저 분류를 결정하고, `public`일 때만 shared 스키마/버전 매니페스트를 갱신한다.
3. `internal` 이벤트는 shared에 스키마를 추가하지 않으며 서비스 내부 문서로 관리한다.

---

## 런타임 영향

Phase 8.3은 분류 기준의 문서화 단계다.
따라서 코드/스키마 런타임 로직 변경은 요구하지 않는다.
