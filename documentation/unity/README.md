# Unity 클라이언트 문서

Unity 클라이언트(3D 렌더링·시뮬레이션 뷰) 관련 문서 디렉터리입니다.

| 문서 | 설명 |
|------|------|
| [servers/unity/README.md](../../servers/unity/README.md) | 프로젝트 개요, 열기·재생 확인 방법 |
| [servers/unity/structure.md](../../servers/unity/structure.md) | 클린 아키텍처 디렉터리 구조, asmdef 의존성 |

## 요약

- **위치**: `servers/unity/`
- **역할**: Backend·시뮬레이션 엔진과 연동하여 3D 뷰 제공 (WebGL 빌드 예정).
- **구조**: Domain → Application → Infrastructure / Presentation (Assembly Definition으로 레이어 분리).
