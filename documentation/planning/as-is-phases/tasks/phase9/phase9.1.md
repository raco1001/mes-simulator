Phase 9.1 Alert SSE Backend 구현 계획

Goal

Kafka로 수신된 Alert를 GET /api/alerts/stream SSE 엔드포인트로 즉시 푸시하고, 기존 GET /api/alerts 이력 조회 동작을 유지합니다.

Constraints

Hexagonal 구조 유지: Consumer는 포트(IAlertNotifier)에만 의존

기존 저장 흐름(IAlertStore.Add) 및 API 계약 불변

단순/명확한 구현 우선: 연결별 Channel<AlertDto> 기반 fan-out

현재 테스트 스택(xUnit + Moq) 및 네임스페이스 컨벤션 준수

Acceptance Criteria

IAlertNotifier 포트가 추가되고 DI로 singleton 인프라 구현이 연결됨

KafkaAlertConsumerService가 Alert 저장 직후 notifier로 전달함

AlertController에 SSE 스트림 엔드포인트가 추가되어 text/event-stream으로 이벤트를 지속 전송함

연결 종료 시 구독 채널이 정리되고 예외 없이 종료됨

기존 GET /api/alerts 테스트가 계속 통과하고, SSE 채널/알림 호출 검증 테스트가 추가됨

Implementation Scope (files)

신규 포트: servers/backend/DotnetEngine/Application/Alert/Ports/Driven/IAlertNotifier.cs

신규 인프라: servers/backend/DotnetEngine/Infrastructure/Alert/SseAlertChannel.cs

Consumer 수정: servers/backend/DotnetEngine/Infrastructure/Kafka/KafkaAlertConsumerService.cs

Controller 수정: servers/backend/DotnetEngine/Presentation/Controllers/AlertController.cs

DI 등록: servers/backend/DotnetEngine/Program.cs

테스트(신규):

servers/backend/DotnetEngine.Tests/Infrastructure/Alert/SseAlertChannelTests.cs

servers/backend/DotnetEngine.Tests/Infrastructure/Kafka/KafkaAlertConsumerServiceTests.cs

Data flow

flowchart TD
kafkaAlert[KafkaAlertConsumerService] -->|store| alertStore[IAlertStore.Add]
kafkaAlert -->|notify| alertNotifier[IAlertNotifier.NotifyAsync]
alertNotifier --> sseChannel[SseAlertChannel]
sseChannel --> streamEndpoint[AlertController.StreamAlerts]
streamEndpoint --> eventSource[Frontend EventSource]

Step-by-step

포트 추가: NotifyAsync, SubscribeAsync 시그니처 확정

인프라 구현: 연결별 채널 등록/해제, NotifyAsync fan-out, 취소 토큰 기반 구독 종료

Consumer 주입 확장: 생성자에 IAlertNotifier 추가, \_alertStore.Add(alert) 뒤 await \_alertNotifier.NotifyAsync(alert, ct) 호출

Controller 확장: GET /api/alerts/stream 추가, SSE 헤더 설정 후 await foreach로 data: {json}\n\n 전송 + flush

DI 연결: Program.cs에 AddSingleton<IAlertNotifier, SseAlertChannel>() 등록

테스트 추가:

SseAlertChannel: 단일/다중 구독자 수신, 취소 시 정리

KafkaAlertConsumerService: Alert 처리 시 NotifyAsync 호출 검증

검증: 백엔드 테스트 실행 후 Alert REST 엔드포인트 회귀 여부 확인

Why this fits now

현재 규모에서는 SSE + in-memory 채널이 구현/운영 복잡도가 가장 낮고, 즉시 전달이라는 요구를 정확히 충족합니다. 이후 트래픽/인스턴스가 증가하면 Redis pub/sub 또는 메시지 브로커 기반 notifier로 IAlertNotifier 구현체만 교체해 확장할 수 있습니다.
