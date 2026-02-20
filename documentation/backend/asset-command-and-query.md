# Asset Command & Query 확장

모델(Asset) 정의·저장을 위한 백엔드 확장 작업의 기획, 결과, 실무 관점의 확장 요소를 정리한 문서입니다.

---

## 기획

### 목표

- **모델 = Asset**으로 간주하고, “모델을 정의하고 저장할 수 있는 화면”을 위한 API를 백엔드에 추가한다.
- 통신 규약은 `shared/api-schemas/`에 정의된 스키마를 기준으로 하며, 백엔드는 해당 계약에 맞춰 구현한다.

### 전제

- 기존 Asset API는 **GET(목록/단건 조회)** 만 존재했다.
- 생성·수정을 위해 **API 스키마 보강** 후, Domain → DTO/Port → 테스트 → 비즈니스 로직 순으로 진행한다.

### 작업 순서(계획)

| 단계 | 내용 |
|------|------|
| 0 | `shared/api-schemas/assets.json`에 POST/PUT 스키마 추가 (CreateAssetRequest, UpdateAssetRequest, paths) |
| 1 | Domain: Asset/AssetState를 ValueObjects → Entities로 전환, record → class, 엔티티 메서드 추가 및 참조 수정 |
| 2 | DTO(Create/Update Request), Port(ICreateAssetCommand, IUpdateAssetCommand), IAssetRepository 확장(AddAsync, UpdateAsync) 및 Mongo 구현 |
| 3 | Domain/Application/Presentation 테스트 선행 추가·수정 |
| 4 | Create/Update Handler, AssetController POST/PUT, DI 등록 |
| 5 | 빌드·테스트 실행 및 오류 수정 |

---

## 결과

### 1. API 스키마 (shared/api-schemas/assets.json)

- **정의 추가**
  - `CreateAssetRequest`: `type`(필수), `connections`, `metadata` (선택)
  - `UpdateAssetRequest`: `type`, `connections`, `metadata` (모두 선택, 부분 업데이트)
- **Paths 추가**
  - `POST /api/assets` — 201 + AssetDto, 400
  - `PUT /api/assets/{id}` — 200 + AssetDto, 400, 404

### 2. Domain 레이어

- **위치 변경**: `Domain/Asset/ValueObjects/` → `Domain/Asset/Entities/`
- **Asset (class)**
  - `Create(id, type, connections?, metadata?)`, `Restore(...)` 정적 팩토리
  - `UpdateType`, `UpdateConnections`, `UpdateMetadata`, `TouchUpdatedAt`
- **AssetState (class)**
  - `Restore(...)` 정적 팩토리
  - `UpdateStatus`, `UpdateTemp`, `UpdatePower`, `TouchUpdatedAt`
- Health 도메인은 기존 ValueObjects 유지.

### 3. Application 레이어

- **DTO**: `CreateAssetRequest`, `UpdateAssetRequest` (Application/Asset/Dto)
- **Port**: `ICreateAssetCommand`, `IUpdateAssetCommand` (Application/Asset/Ports)
- **Handlers**
  - `CreateAssetCommandHandler`: ID 생성(Guid) → Asset.Create → AddAsync → AssetDto 반환
  - `UpdateAssetCommandHandler`: GetByIdAsync → 엔티티 수정 메서드 호출 → UpdateAsync → AssetDto 반환
- **Repository**
  - `IAssetRepository`: `AddAsync(Asset)`, `UpdateAsync(id, Asset)` 추가
  - `MongoAssetRepository`: BsonDocument 직렬화 후 InsertOne / ReplaceOne 구현

### 4. Presentation 레이어

- **AssetController**
  - `POST /api/assets`: CreateAssetRequest 바인딩, 유효성( null / Type 공백) 검사, 201 + CreatedAtAction
  - `PUT /api/assets/{id}`: UpdateAssetRequest 바인딩, 200/404 처리
- **Program.cs**: `ICreateAssetCommand`/`IUpdateAssetCommand` 및 각 Handler Scoped 등록

### 5. 테스트

- **Domain**: `AssetTests.cs` — Create, Restore, UpdateType, UpdateConnections, UpdateMetadata, TouchUpdatedAt, null 인자 예외
- **Application**: `CreateAssetCommandHandlerTests`, `UpdateAssetCommandHandlerTests` — Mock Repository, DTO 검증
- **Presentation**: `AssetControllerTests` — POST 201 + body, PUT 200/404, 기존 GET/State 테스트 유지 및 Entity 참조 수정
- 네임스페이스 충돌(Application.Asset vs Domain.Asset.Entities.Asset) 해결을 위해 테스트/Handler에서 `DomainAsset` 등 alias 사용

### 6. 빌드·테스트

- `dotnet build --no-restore` 성공
- `dotnet test --no-build` 38개 테스트 통과

---

## 작업 하진 않았지만 실무 기준 사실상 추가/확장해야 하는 요소

아래는 현재 구현 범위에는 없으나, 실무에서는 보통 도입하거나 확장하는 항목들입니다.

### API·계약

- **DELETE /api/assets/{id}**: 스키마 및 Controller/Command/Repository 삭제 플로우 (204 또는 404).
- **요청/응답 검증 강화**: `type` 허용 값 열거, `connections` 내 ID 존재 여부 검증, `metadata` 크기/키 제한 등. DataAnnotations 또는 FluentValidation 도입 검토.
- **에러 응답 스키마 통일**: 400/404/409 시 공통 ErrorDto(코드, 메시지, 상세) 및 `shared/api-schemas`에 정의.
- **버저닝**: `/api/v1/assets` 등 경로 또는 헤더 기반 API 버전 관리.

### 보안·운영

- **인증/인가**: 생성·수정·삭제에 대한 역할 기반 접근 제어(RBAC), JWT/API Key 등.
- **Rate limiting**: POST/PUT 남용 방지.
- **감사 로그**: Asset 생성/수정 시 누가/언제 변경했는지 기록(감사 테이블 또는 이벤트 저장).

### 도메인·비즈니스

- **낙관적/비관적 동시성 제어**: `updatedAt` 또는 버전 필드로 PUT 시 충돌 감지(409 Conflict).
- **연결 유효성**: `connections`에 포함된 assetId가 실제 존재하는지 검증 후 저장.
- **소프트 삭제**: 삭제 시 플래그만 변경하고 조회에서 제외하는 방식 검토.

### 인프라·품질

- **MongoDB 인덱스**: `_id`, `type`, `createdAt` 등 조회 패턴에 맞는 인덱스 정의.
- **통합 테스트**: 실제 MongoDB(또는 Testcontainers)를 사용한 Repository/API 통합 테스트.
- **OpenAPI(Swagger)**: 스키마 보강 후 `shared/api-schemas`와 Swagger 문서 동기화 또는 코드에서 스키마 참조.

이 문서는 위 확장을 진행할 때 체크리스트로 활용할 수 있습니다.
