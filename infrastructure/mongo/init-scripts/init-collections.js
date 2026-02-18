// MongoDB 컬렉션 및 인덱스 초기화 스크립트
// docker-compose up 시 자동 실행됨

// 데이터베이스 선택
db = db.getSiblingDB('factory_mes');

// ============================================
// 1. assets 컬렉션
// ============================================
db.createCollection('assets', {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: ['_id', 'type', 'createdAt'],
      properties: {
        _id: {
          bsonType: 'string',
          description: 'Asset ID (required)'
        },
        type: {
          bsonType: 'string',
          description: 'Asset type (required)'
        },
        connections: {
          bsonType: 'array',
          items: {
            bsonType: 'string'
          },
          description: 'Connected asset IDs'
        },
        metadata: {
          bsonType: 'object',
          description: 'Additional metadata'
        },
        createdAt: {
          bsonType: 'date',
          description: 'Creation timestamp'
        },
        updatedAt: {
          bsonType: 'date',
          description: 'Last update timestamp'
        }
      }
    }
  }
});

// assets 인덱스
// _id는 자동으로 unique 인덱스가 생성되므로 별도로 생성하지 않음
db.assets.createIndex({ type: 1 });
db.assets.createIndex({ updatedAt: -1 });

// ============================================
// 2. events 컬렉션 (Raw 이벤트 로그)
// ============================================
db.createCollection('events', {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: ['assetId', 'eventType', 'timestamp'],
      properties: {
        assetId: {
          bsonType: 'string',
          description: 'Asset ID (required)'
        },
        eventType: {
          bsonType: 'string',
          description: 'Event type (required)'
        },
        timestamp: {
          bsonType: 'date',
          description: 'Event timestamp (required)'
        },
        payload: {
          bsonType: 'object',
          description: 'Event payload'
        }
      }
    }
  }
});

// events 인덱스
db.events.createIndex({ assetId: 1, timestamp: -1 });
db.events.createIndex({ eventType: 1 });
db.events.createIndex({ timestamp: -1 });
// TTL 인덱스: 90일 후 자동 삭제 (선택사항)
// db.events.createIndex({ timestamp: 1 }, { expireAfterSeconds: 7776000 });

// ============================================
// 3. states 컬렉션 (핵심 - 현재 상태)
// ============================================
db.createCollection('states', {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: ['assetId', 'updatedAt'],
      properties: {
        assetId: {
          bsonType: 'string',
          description: 'Asset ID (required)'
        },
        currentTemp: {
          bsonType: ['number', 'null'],
          description: 'Current temperature'
        },
        currentPower: {
          bsonType: ['number', 'null'],
          description: 'Current power consumption'
        },
        status: {
          bsonType: 'string',
          enum: ['normal', 'warning', 'error'],
          description: 'Current status'
        },
        lastEventType: {
          bsonType: 'string',
          description: 'Last event type that updated this state'
        },
        updatedAt: {
          bsonType: 'date',
          description: 'Last update timestamp (required)'
        },
        metadata: {
          bsonType: 'object',
          description: 'Additional state metadata'
        }
      }
    }
  }
});

// states 인덱스
db.states.createIndex({ assetId: 1 }, { unique: true, name: "assetId_unique" });
db.states.createIndex({ status: 1 });
db.states.createIndex({ updatedAt: -1 });

print('Collections and indexes created successfully!');
print('Collections: assets, events, states');
