# MongoDB Infrastructure

Factory MES 시스템의 MongoDB 인프라 설정입니다.

## 빠른 시작

```bash
cd docker
docker-compose -f docker-compose.mongo.yml up -d
```

## 컬렉션 및 인덱스 초기화

컬렉션과 인덱스는 컨테이너가 처음 시작될 때 자동으로 생성됩니다.

**수동 실행 (필요시):**

```bash
# 방법 1: 스크립트 파일 직접 실행
docker exec -i mongodb mongosh -u admin -p admin123 --authenticationDatabase admin factory_mes < infrastructure/mongo/init-scripts/init-collections.js

# 방법 2: mongosh 내부에서 실행
docker exec -it mongodb mongosh -u admin -p admin123 --authenticationDatabase admin factory_mes
> load('init-scripts/init-collections.js')
```

## 데이터 모델

자세한 모델 설계는 [MODEL.md](./MODEL.md)를 참조하세요.

**주요 컬렉션:**
- `assets`: Asset 메타데이터
- `events`: Raw 이벤트 로그
- `states`: Asset 현재 상태 (핵심)

## 접속 정보

- **MongoDB**: `mongodb://admin:admin123@localhost:27017/factory_mes`
- **Mongo Express**: http://localhost:8081
  - Username: `admin`
  - Password: `admin123`

## 문제 해결

### 컬렉션이 생성되지 않음

MongoDB는 볼륨에 데이터가 이미 있으면 초기화 스크립트를 실행하지 않습니다:

```bash
# 볼륨 삭제 후 재시작 (주의: 데이터 삭제됨)
docker-compose -f docker-compose.mongo.yml down -v
docker-compose -f docker-compose.mongo.yml up -d
```

### 인덱스 확인

```bash
docker exec mongodb mongosh -u admin -p admin123 --authenticationDatabase admin factory_mes --eval "db.states.getIndexes()"
```

## 참고

- [MODEL.md](./MODEL.md) - 데이터 모델 상세 설계
- [init-scripts/init-collections.js](./init-scripts/init-collections.js) - 초기화 스크립트
