Phase 9.3 MongoAlertStore 구현 계획

Goal

IAlertStore의 구현체를 InMemoryAlertStore에서 MongoAlertStore로 전환해 서버 재시작 후에도 Alert 이력이 유지되도록 합니다.

Constraints

Hexagonal 구조 유지: IAlertStore 인터페이스 변경 없이 구현체만 교체

기존 Alert 조회 계약(GetLatest(maxCount))과 반환 정렬 의미(최신 우선) 유지

Mongo 인프라 기존 패턴(Mongo*Repository, Mongo*Document, camelCase BsonElement) 준수

테스트는 현재 스택(xUnit + Moq)으로 작성

Acceptance Criteria

MongoAlertStore가 alerts 컬렉션에 Alert를 저장하고 최신순 조회를 제공함

Program.cs에서 IAlertStore가 MongoAlertStore로 등록됨(Scoped)

infrastructure/mongo/MODEL.md에 alerts 컬렉션 스키마가 기존 문서 형식으로 추가됨

MongoAlertStoreTests가 추가되고 테스트가 통과함

기존 GET /api/alerts 동작이 유지됨

Target Files

신규 구현

servers/backend/DotnetEngine/Infrastructure/Alert/MongoAlertStore.cs

servers/backend/DotnetEngine/Infrastructure/Mongo/MongoAlertDocument.cs

DI 전환

servers/backend/DotnetEngine/Program.cs

문서 업데이트

infrastructure/mongo/MODEL.md

테스트

servers/backend/DotnetEngine.Tests/Infrastructure/Mongo/MongoAlertStoreTests.cs

Data Flow

flowchart TD
kafkaConsumer[KafkaAlertConsumerService] --> alertStorePort[IAlertStore]
alertStorePort --> mongoStore[MongoAlertStore]
mongoStore --> alertsCollection[(Mongo alerts)]
getAlertsQuery[GetAlertsQueryHandler] --> alertStorePort
alertStorePort --> latestAlerts[Latest alerts sorted desc]

Implementation Steps

MongoAlertDocument 정의

필드: assetId, timestamp, severity, message, runId, metric, current, threshold, code, metadata

기존 Mongo 문서 스타일대로 [BsonElement("...")]/[BsonId] 적용

MongoAlertStore 구현

생성자에서 IMongoDatabase 주입, alerts 컬렉션 획득

Add(AlertDto alert): DTO -> 문서 매핑 후 insert

GetLatest(int maxCount): timestamp 내림차순 정렬 + limit 적용 후 DTO 매핑 반환

메타데이터는 MetadataBsonConverter를 재사용해 BSON <-> dictionary 변환

DI 교체

Program.cs에서 IAlertStore 등록을 AddScoped<IAlertStore, MongoAlertStore>()로 변경

기존 InMemoryAlertStore 코드는 유지(필요 시 한 줄로 롤백 가능)

문서 업데이트

MODEL.md의 ## 컬렉션 구조에 ### 5. alerts 섹션 추가

기존 패턴(스키마/예시/인덱스/제약사항/구분선) 동일하게 적용

테스트 추가

MongoAlertStoreTests에서 핵심 시나리오 검증:

Add 시 insert 호출

GetLatest 최신순 반환

maxCount 제한 준수

optional 필드/metadata 매핑

Why this fits now

현재 코드베이스는 포트-어댑터 분리가 이미 되어 있어 IAlertStore 구현체 교체만으로 영속성 요구를 충족할 수 있습니다. 규모가 커지면 인덱스 최적화(예: timestamp desc, assetId+timestamp)와 TTL/아카이빙 정책을 MongoAlertStore 내부에서 확장해도 상위 애플리케이션 레이어는 변경 없이 유지됩니다.

Optional strategy (portfolio-focused)

포트폴리오 목적상 어댑터 교체 가능성을 더 명확히 보여주기 위해 다음 확장 전략을 제안합니다.

- InMemoryAlertStore: 개발 초기/테스트용 기본값 (빠르고 단순, 비영속)
- FileAlertStore: 로컬 파일 기반 영속 저장소 (재시작 복원 가능, 데모 친화적)
- MongoAlertStore: 운영 유사 저장소 (조회/인덱싱/확장성 대응)

권장 전환 방식:

- 설정값 기반 Provider 선택
  - 예: `AlertStore:Provider = InMemory | File | Mongo`
  - DI에서 설정값으로 `IAlertStore` 구현체만 분기 등록

FileAlertStore trade-off:

- 장점: 별도 인프라 없이 영속성 확보, 로컬 복원 시나리오 시연 용이
- 한계: 파일 동시성/손상 복구, 대량 조회 성능, 멀티 인스턴스 부적합

핵심 메시지:

- “도메인/애플리케이션 코드는 고정하고, 인프라 어댑터만 교체해 요구사항 변화에 대응한다.”
