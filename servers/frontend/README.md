# Frontend

Factory MES 시스템의 React 프론트엔드입니다.

## 기술 스택

- **React 19** + **TypeScript**
- **Vite** (빌드 도구)
- **Vitest** + **React Testing Library** (테스트)

## 개발 환경 설정

### 1. 패키지 설치

```bash
npm install
```

### 2. 환경 변수 설정

`.env` 파일을 생성하고 다음 내용을 추가하세요:

```bash
VITE_API_BASE_URL=http://localhost:5000
```

또는 `.env.example`을 참고하세요.

### 3. 개발 서버 실행

```bash
npm run dev
```

브라우저에서 `http://localhost:5173` 접속

## 빌드 및 테스트

### 빌드

```bash
npm run build
```

### 테스트 실행

```bash
# 모든 테스트 실행
npm test

# UI 모드로 실행
npm run test:ui

# 커버리지 포함
npm run test:coverage
```

## 프로젝트 구조

```
src/
├── pages/
│   └── home/
│       └── ui/
│           ├── AssetList.tsx      # Asset 목록 컴포넌트
│           └── AssetList.test.tsx # 테스트
├── shared/
│   └── api/
│       ├── apiClient.ts           # API 클라이언트
│       ├── apiClient.test.ts      # API 클라이언트 테스트
│       ├── types.ts                # API 타입 정의
│       └── index.ts
└── test/
    └── setup.ts                   # 테스트 설정
```

## API 엔드포인트

백엔드 API는 `shared/api-schemas/assets.json`에 정의된 스키마를 따릅니다:

- `GET /api/assets` - 모든 asset 목록 조회
- `GET /api/assets/{id}` - 특정 asset 정보 조회
- `GET /api/states` - 모든 asset의 현재 상태 조회
- `GET /api/states/{assetId}` - 특정 asset의 현재 상태 조회

## 백엔드 연결

프론트엔드를 실행하기 전에 백엔드가 실행 중이어야 합니다:

```bash
# 백엔드 실행 (별도 터미널)
cd ../backend
dotnet run --project DotnetEngine/DotnetEngine.csproj
```

백엔드는 기본적으로 `http://localhost:5000`에서 실행됩니다.
