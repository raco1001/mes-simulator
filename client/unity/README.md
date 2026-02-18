# Unity Client

Unity 기반 3D 렌더링 클라이언트. Backend API·시뮬레이션 엔진과 연동하여 공장 MES 시뮬레이션 뷰를 제공할 예정입니다.

## 요구 사항

- **Unity Editor**: 2022.3 LTS 이상 (권장). 더 최신 LTS(예: 6000.x)로 열면 프로젝트가 자동 업그레이드될 수 있습니다.
- **실행 환경**: Unity Editor는 Windows에 설치하고, 프로젝트는 WSL 경로에서 열어 사용합니다.

## 프로젝트 열기 (Windows에서 Unity Editor)

1. Unity Hub 또는 Unity Editor 실행.
2. **Open** → 프로젝트 폴더 선택.
3. WSL에 있는 경우 Windows 경로로 열기:
   - `\\wsl$\Ubuntu\home\orca\devs\projects\shadow-boxing\Scenario4\servers\unity`
   - 또는 WSL 디스크가 마운트된 드라이브 경로(예: `Z:\home\orca\devs\projects\shadow-boxing\Scenario4\servers\unity`).

## 재생(Play) 확인

초기화 후 클라이언트가 정상 기동하는지 확인하려면:

1. Unity Editor에서 프로젝트 열기(위 경로).
2. **File → New Scene**으로 빈 씬 생성.
3. Hierarchy에서 **Create Empty**로 GameObject 생성.
4. Inspector에서 **Add Component** → `Bootstrap` 스크립트 추가.
5. **File → Save As** → `Assets/Scenes/Main.unity`로 저장.
6. **Play** 버튼 클릭.
7. Console에 `Unity client started.` 로그가 출력되면 정상 동작입니다.

## 구조

- [structure.md](structure.md) — 디렉터리 및 클린 아키텍처 개요.
- [documentation/unity/](../documentation/unity/) — 설정·운영 문서.
