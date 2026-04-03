# Phase 11 Backend-first Handoff

## 완료된 백엔드 범위

- `StateDto`/`StatePatchDto` 동적 `properties` 전환
- `RunSimulationCommandHandler` 병합 로직 및 이벤트 payload를 `properties` 중심으로 전환
- `SuppliesRule`/`ContainsRule` 전파 패치가 `properties`를 전달하도록 변경
- `ReplayRunCommandHandler`가 이벤트 payload의 `properties`를 복원하도록 변경
- `CreateAssetCommandHandler`에서 ObjectTypeSchema 기반 baseValue 초기화 추가
- ObjectTypeSchema CRUD (port/handler/controller/repository) 1차 연결
- `openapi.json`에 `/api/object-type-schemas` 엔드포인트 반영

## 잔여 작업 반영 결과 (Phase 11 완료)

1. Frontend (완료)
   - Home/Monitoring 목록을 `state.properties` 요약 렌더링으로 전환
   - `AssetsCanvasPage`에서 ObjectTypeSchema 조회 기반 생성/편집 UX 연결

2. Pipeline (완료)
   - `calculate_state()`를 `payload.properties` 전용 처리로 전환 (legacy fallback 제거)
   - `build_alert_event()`를 multi-metric payload(`metrics[]`) 구조로 확장하고 worker 연동 완료

3. 검증
   - Frontend: 대상 Vitest 스위트 통과
   - Pipeline: 대상 pytest 스위트 통과
   - Backend: `dotnet test` 전체 실행은 여전히 NuGet 프록시 403으로 대기, 네트워크 가능한 CI/로컬 환경에서 회귀 테스트 필요

## 참고

- 메타계층 계약 소스: `shared/ontology-schemas/`
- 런타임 API 계약: `shared/api-schemas/openapi.json`
