Phase 8.3 공개/내부 이벤트 분류

Goal

서비스 간 계약으로 관리할 이벤트(public)와 서비스 내부 구현 이벤트(internal)를 명확히 분리합니다.

거버넌스 대상 범위를 문서 계약으로 고정해 shared/의 역할을 명확히 합니다.

Constraints

런타임 동작 변경 없이 문서/계약 정합성 중심으로 진행합니다.

shared/event-schemas/schemas/에는 public 이벤트만 포함한다는 원칙을 유지합니다.

기존 스키마 파일명/이벤트 타입 네이밍 규칙(domain.entity.action)을 유지합니다.

Acceptance Criteria

[shared/event-schemas/CONTRACT.md](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/CONTRACT.md) 신규 생성

이벤트별 public/internal 분류와 근거가 표 형태로 정의됨

shared/event-schemas/schemas/의 현재 파일들이 public 범주임이 CONTRACT에 명시됨

[documentation/shared/event-types.md](/home/orca/devs/projects/shadow-boxing/Scenario4/documentation/shared/event-types.md)에 분류 기준과 CONTRACT 참조 연결

스키마/코드 런타임 변경이 필요 없음을 문서에 명확히 기록

Implementation Steps

분류 기준 확정 및 현재 이벤트 매핑

기준: 외부 소비자(다른 서비스/프로젝트)가 의존하면 public, 내부 구현 세부 전파/상태 변경용이면 internal

현재 shared 스키마 이벤트는 public로 선언:

asset.created

asset.health.updated

simulation.state.updated

alert.generated

CONTRACT 문서 작성

파일: [shared/event-schemas/CONTRACT.md](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/CONTRACT.md)

내용:

분류 정책(정의/판단 기준)

이벤트 분류표(이벤트명, 분류, 근거, 주요 소비자)

저장소 규칙: shared/event-schemas/schemas에는 public만 배치

internal 이벤트 예시(power_changed, state_transitioned 등) 및 관리 위치 원칙

event-types 문서 연동

파일: [documentation/shared/event-types.md](/home/orca/devs/projects/shadow-boxing/Scenario4/documentation/shared/event-types.md)

내용:

public/internal 개념 추가

CONTRACT 링크 추가

현재 목록에 분류 컬럼(또는 섹션) 반영

스키마 디렉토리 정합성 확인

점검 파일:

[shared/event-schemas/schemas/asset.created.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/schemas/asset.created.json)

[shared/event-schemas/schemas/asset.health.updated.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/schemas/asset.health.updated.json)

[shared/event-schemas/schemas/simulation.state.updated.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/schemas/simulation.state.updated.json)

[shared/event-schemas/schemas/alert.generated.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/schemas/alert.generated.json)

구조 변경 없이 문서 정의와 실제 파일 목록이 일치하는지 확인

Why this fits now

현재 단계에서는 분류 기준을 문서 계약으로 고정하는 것이 가장 저비용/고효율입니다.

이후 Phase 10/11(검증/호환성/CI)에서 이 분류를 기준으로 검증 범위(public 우선)를 자동화하기 쉬워집니다.
