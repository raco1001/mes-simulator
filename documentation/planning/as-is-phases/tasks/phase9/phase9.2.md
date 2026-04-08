Phase 9.2 Alert SSE Frontend 구현 계획

Goal

백엔드 GET /api/alerts/stream SSE를 프론트에서 구독해 Alert 발생 시 즉시 토스트를 표시하고, 기존 REST 이력 조회(getAlerts)를 함께 제공해 실시간 + 이력 UX를 완성합니다.

Constraints

기존 구조(FSD-ish): entities/\*, app/layout, shared/api 패턴 준수

별도 토스트 라이브러리 도입 없이 현재 스택(React + Vite + Vitest)으로 구현

앱 전역 동작: 페이지 이동과 무관하게 수신되도록 AppLayout 레벨에서 구독

단순성 우선: 최소 상태(Toast 큐 + 최근 Alert 목록)로 시작

Acceptance Criteria

entities/alert에 types, alertStream, alertApi가 추가됨

subscribeAlerts(onAlert)가 EventSource 기반으로 동작하고 cleanup 반환

앱 레벨에서 SSE 구독이 연결되어 Alert 수신 시 토스트가 즉시 표시됨

severity(warning/error)별 표시와 자동 닫힘(warning 5초, error 10초)이 동작함

테스트(alertStream, AlertToast)가 추가되고 프론트 테스트가 통과함

Target Files

신규 엔티티

/home/orca/devs/projects/shadow-boxing/Scenario4/servers/frontend/src/entities/alert/model/types.ts

/home/orca/devs/projects/shadow-boxing/Scenario4/servers/frontend/src/entities/alert/api/alertStream.ts

/home/orca/devs/projects/shadow-boxing/Scenario4/servers/frontend/src/entities/alert/api/alertApi.ts

/home/orca/devs/projects/shadow-boxing/Scenario4/servers/frontend/src/entities/alert/index.ts

UI/연결

/home/orca/devs/projects/shadow-boxing/Scenario4/servers/frontend/src/entities/alert/ui/AlertToast.tsx

/home/orca/devs/projects/shadow-boxing/Scenario4/servers/frontend/src/entities/alert/ui/AlertToast.css

/home/orca/devs/projects/shadow-boxing/Scenario4/servers/frontend/src/app/layout/AppLayout.tsx

테스트

/home/orca/devs/projects/shadow-boxing/Scenario4/servers/frontend/src/entities/alert/api/alertStream.test.ts

/home/orca/devs/projects/shadow-boxing/Scenario4/servers/frontend/src/entities/alert/ui/AlertToast.test.tsx

(선택) alertApi 테스트: /home/orca/devs/projects/shadow-boxing/Scenario4/servers/frontend/src/entities/alert/api/alertApi.test.ts

Flow

flowchart TD
appLayout[AppLayout] --> streamSub[subscribeAlerts]
streamSub --> eventSource[EventSource /api/alerts/stream]
eventSource --> onAlertCb[onAlert callback]
onAlertCb --> toastQueue[toastQueue state]
toastQueue --> alertToast[AlertToast render]
onAlertCb --> recentAlerts[recentAlerts state]

Implementation Steps

types.ts: 백엔드 AlertDto와 정합되는 타입 정의

alertStream.ts: VITE_API_BASE_URL 기반 stream URL 생성, EventSource 메시지 파싱, cleanup(close) 반환

alertApi.ts: 기존 entity API 패턴처럼 httpClient.request<AlertDto[]>('/api/alerts?limit=...') 구현

AlertToast.tsx: severity 스타일 분기 + 자동 닫힘 타이머 + 수동 닫기 버튼

AppLayout.tsx: useEffect로 mount 시 구독/cleanup, 수신시 toast 큐 및 최근 N개(예: 20) 상태 갱신

테스트:

alertStream.test.ts: mock EventSource로 message 이벤트 시 콜백 호출 검증

AlertToast.test.tsx: severity 렌더링 + vi.useFakeTimers()로 auto-dismiss 검증

(선택) alertApi.test.ts: endpoint/limit 호출 검증

검증: servers/frontend에서 테스트 실행 후 기존 레이아웃/라우팅 테스트 회귀 확인

Why this fits now

현 구조에는 전역 알림 시스템이 없으므로 AppLayout에 가벼운 구독/토스트 호스트를 두는 방식이 가장 단순하고 영향 범위가 명확합니다. 이후 규모가 커지면 전용 AlertProvider로 분리하고, 동일 entities/alert API를 유지한 채 상태관리(store)만 교체해 확장할 수 있습니다.
