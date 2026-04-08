# Frontend

Factory MES의 React 프론트엔드입니다. Canvas 기반 에셋/관계 편집과 시뮬레이션 실행 UI를 제공합니다.  
React frontend for Factory MES with canvas-based asset/relationship editing and simulation controls.

## 기술 스택 / Tech Stack

- React 19 + TypeScript
- Vite
- Vitest + React Testing Library

## 개발 환경 설정 / Setup

### 1) 패키지 설치 / Install

```bash
npm install
```

### 2) 환경 변수 / Environment

`.env` (or `.env.local`)

```bash
VITE_API_BASE_URL=http://localhost:5000
```

### 3) 개발 서버 / Dev Server

```bash
npm run dev
```

Open: `http://localhost:5173`

## 빌드 및 테스트 / Build and Test

```bash
npm run build
npm test
npm run test:ui
npm run test:coverage
```

## 프로젝트 구조 요약 / Structure Snapshot

현재 구조는 Feature-Sliced Design 기반으로 `entities`, `features`, `pages`, `widgets`, `shared`를 사용합니다.  
Current structure follows feature-sliced design with `entities`, `features`, `pages`, `widgets`, and `shared`.

핵심 UI 영역 / Key UI areas:
- Canvas page and node/edge rendering
- Run simulation panel (single run + continuous run trigger selection)
- Asset/type edit panels and derived-property input helpers

## API 계약 동기화 / API Contract Sync

- Backend/OpenAPI source: `../../shared/api-schemas/openapi.json`
- Frontend simulation/asset DTO usage: `src/entities/**/api` + `src/entities/**/model`

계약 변경 시 권장 순서 / Recommended order:
1. Update `shared/api-schemas/openapi.json`
2. Sync backend DTO/controller behavior
3. Sync frontend API types/tests

## 현재 기능 범위 / Current Scope

- 캔버스에서 에셋/관계 편집 및 위치 저장
- 시뮬레이션 트리거 선택 및 실행 요청
- 결과 상태(live properties) 표시
- What-if/추천 연계 기능 확장 기반

진행중 포인트 / In-progress focus:
- Trigger UX and run-state clarity improvements (phase 22)
- Multi-seed interaction alignment with backend behavior (phase 23)

## 백엔드 연결 / Backend Dependency

프론트 실행 전에 백엔드가 실행 중이어야 합니다.  
Backend must be running before frontend requests.

```bash
cd ../backend
dotnet run --project DotnetEngine/DotnetEngine.csproj
```
