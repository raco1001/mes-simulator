# Unity Client 구조 (Clean Architecture)

```
unity/
├── Assets/
│   ├── Scripts/
│   │   ├── Domain/                    # 도메인: 엔티티, Value Object, 상수 (Unity 미의존)
│   │   │   ├── UnityClient.Domain.asmdef
│   │   │   └── ...
│   │   ├── Application/               # Use Case, Port(인터페이스), Handler
│   │   │   ├── UnityClient.Application.asmdef
│   │   │   └── ...
│   │   ├── Infrastructure/            # 외부 연동 구현 (예: Backend API)
│   │   │   ├── UnityClient.Infrastructure.asmdef
│   │   │   └── ...
│   │   └── Presentation/              # MonoBehaviour, UI, 진입점
│   │       ├── UnityClient.Presentation.asmdef
│   │       ├── Bootstrap.cs
│   │       └── ...
│   └── Scenes/
├── Packages/
│   └── manifest.json
├── ProjectSettings/
│   └── ProjectVersion.txt
├── structure.md
└── README.md
```

## 레이어별 역할

| 레이어 | 역할 | Unity 의존 |
|--------|------|------------|
| **Domain** | 엔티티, Value Object, 도메인 상수. 순수 C#. | 없음 |
| **Application** | Use Case, Port(인터페이스), Handler. | 없음 |
| **Infrastructure** | Port 구현(HTTP, 저장소 등). | 필요 시 |
| **Presentation** | MonoBehaviour, 씬/UI, 진입점(Bootstrap). | UnityEngine |

## 의존성 방향

```
Presentation ──► Application ──► Domain
      │                │
      │                └──────────────► Domain
      │
      └──► UnityEngine

Infrastructure ──► Application ──► Domain
```

- **Domain**: 어떤 레이어도 참조하지 않음.
- **Application**: Domain만 참조.
- **Infrastructure**: Application, Domain 참조.
- **Presentation**: Application, Domain, UnityEngine 참조.

Domain·Application은 Unity 엔진에 의존하지 않으므로, 유닛 테스트나 다른 런타임에서도 재사용하기 쉽습니다.

## Backend/Pipeline과의 대비

- **Backend** ([servers/backend/structure.md](../backend/structure.md)): Domain → Application(Ports/Handlers) → Presentation(Controllers). Hexagonal.
- **Pipeline** ([servers/pipeline/structure.md](../pipeline/structure.md)): domains → pipelines → workers.
- **Unity Client**: Domain → Application → Infrastructure / Presentation. 동일하게 “도메인/유스케이스는 프레임워크 비의존” 원칙을 따릅니다.
