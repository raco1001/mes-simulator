// MongoDB 컬렉션 및 인덱스 초기화 스크립트
// docker-compose up 시 자동 실행됨 (데이터 볼륨이 비어 있을 때 1회)
//
// 온톨로지 컬렉션(object_type_schemas, link_type_schemas)의 payloadJson은
// JSON 문자열이 아니라 임베디드 BSON 객체(ObjectTypeSchemaDto / LinkTypeSchemaDto 직렬화)이다.
// 백엔드: MongoObjectTypeSchemaDocument.PayloadJson → BsonDocument
// 예전 validator가 payloadJson을 string으로 두면 삽입 시 Code 121이 난다.
// ensureOntologyCollection()은 컬렉션이 이미 있으면 collMod로 검증 규칙만 맞춘다.

// 데이터베이스 선택
db = db.getSiblingDB('factory_mes');

/**
 * 온톨로지 메타 컬렉션: 신규면 createCollection, 이미 있으면 validator만 collMod로 갱신
 */
function ensureOntologyCollection(collectionName, jsonSchema) {
  const validator = { $jsonSchema: jsonSchema };
  if (!db.getCollectionNames().includes(collectionName)) {
    db.createCollection(collectionName, { validator });
  } else {
    db.runCommand({
      collMod: collectionName,
      validator: validator,
      validationLevel: 'strict',
      validationAction: 'error'
    });
  }
}

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
        simulationRunId: {
          bsonType: ['string', 'null'],
          description: 'Simulation run ID when event is from a run (optional)'
        },
        relationshipId: {
          bsonType: ['string', 'null'],
          description: 'Relationship ID when event is about a relationship (optional)'
        },
        occurredAt: {
          bsonType: ['date', 'null'],
          description: 'Occurrence time (optional; use timestamp if not set)'
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
db.events.createIndex({ simulationRunId: 1 });
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
        properties: {
          bsonType: 'object',
          description: 'Dynamic state properties'
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

// ============================================
// 3-1. object_type_schemas 컬렉션 (온톨로지 메타모델)
// ============================================
const objectTypeSchemasJsonSchema = {
  bsonType: 'object',
  required: ['_id', 'objectType', 'payloadJson'],
  properties: {
    _id: { bsonType: 'string', description: '문서 _id (= objectType 문자열)' },
    objectType: { bsonType: 'string' },
    payloadJson: {
      bsonType: 'object',
      description: 'ObjectTypeSchemaDto를 BSON으로 직렬화한 문서 (문자열 아님)'
    }
  }
};
ensureOntologyCollection('object_type_schemas', objectTypeSchemasJsonSchema);
db.object_type_schemas.createIndex({ objectType: 1 }, { unique: true, name: "objectType_unique" });

// ============================================
// 3-2. link_type_schemas 컬렉션 (관계 스키마 메타모델)
// ============================================
const linkTypeSchemasJsonSchema = {
  bsonType: 'object',
  required: ['_id', 'linkType', 'payloadJson'],
  properties: {
    _id: { bsonType: 'string' },
    linkType: { bsonType: 'string' },
    payloadJson: {
      bsonType: 'object',
      description: 'LinkTypeSchemaDto를 BSON으로 직렬화한 문서 (문자열 아님)'
    }
  }
};
ensureOntologyCollection('link_type_schemas', linkTypeSchemasJsonSchema);
db.link_type_schemas.createIndex({ linkType: 1 }, { unique: true, name: "linkType_unique" });

// 시드 데이터는 infrastructure/mongo/seeds/*.js 에 두고,
// - 호스트: infrastructure/mongo/run-seeds.sh
// - Docker 최초 기동: init-scripts/zz-run-seeds.sh (compose에서 /mongo-seeds 마운트)
// 에서 실행한다.

// ============================================
// 4. relationships 컬렉션 (에셋 간 관계)
// ============================================
db.createCollection('relationships', {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: ['_id', 'fromAssetId', 'toAssetId', 'relationshipType', 'createdAt', 'updatedAt'],
      properties: {
        _id: {
          bsonType: 'string',
          description: 'Relationship ID (required)'
        },
        fromAssetId: {
          bsonType: 'string',
          description: '시작 에셋 ID (required)'
        },
        toAssetId: {
          bsonType: 'string',
          description: '대상 에셋 ID (required)'
        },
        relationshipType: {
          bsonType: 'string',
          description: '관계 종류 (required)'
        },
        properties: {
          bsonType: 'object',
          description: '관계 단위 속성 (optional)'
        },
        createdAt: {
          bsonType: 'date',
          description: 'Creation timestamp (required)'
        },
        updatedAt: {
          bsonType: 'date',
          description: 'Last update timestamp (required)'
        }
      }
    }
  }
});

// relationships 인덱스
db.relationships.createIndex({ fromAssetId: 1 });
db.relationships.createIndex({ toAssetId: 1 });
db.relationships.createIndex({ relationshipType: 1 });
db.relationships.createIndex({ updatedAt: -1 });
db.relationships.createIndex({ fromAssetId: 1, toAssetId: 1 });

// ============================================
// 5. simulation_runs 컬렉션 (시뮬레이션 런 세션)
// ============================================
db.createCollection('simulation_runs', {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: ['_id', 'startedAt', 'triggerAssetId', 'maxDepth'],
      properties: {
        _id: {
          bsonType: 'string',
          description: 'Run ID (required)'
        },
        startedAt: {
          bsonType: 'date',
          description: 'Run start timestamp (required)'
        },
        endedAt: {
          bsonType: ['date', 'null'],
          description: 'Run end timestamp (optional)'
        },
        triggerAssetId: {
          bsonType: 'string',
          description: 'Trigger asset ID (required)'
        },
        trigger: {
          bsonType: 'object',
          description: 'State patch applied at trigger (optional)'
        },
        maxDepth: {
          bsonType: 'int',
          description: 'Max BFS depth (required)'
        }
      }
    }
  }
});

db.simulation_runs.createIndex({ startedAt: -1 });
db.simulation_runs.createIndex({ triggerAssetId: 1 });

// ============================================
// 6. alerts 컬렉션 (알림 이력)
// ============================================
db.createCollection('alerts', {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: ['assetId', 'timestamp', 'severity', 'message'],
      properties: {
        assetId: {
          bsonType: 'string',
          description: 'Alert 대상 Asset ID (required)'
        },
        timestamp: {
          bsonType: 'date',
          description: 'Alert 발생 시각 (required)'
        },
        severity: {
          bsonType: 'string',
          enum: ['info', 'warning', 'error'],
          description: '심각도 (required)'
        },
        message: {
          bsonType: 'string',
          description: '표시 메시지 (required)'
        },
        runId: {
          bsonType: ['string', 'null'],
          description: '시뮬레이션 실행 ID (optional)'
        },
        metric: {
          bsonType: ['string', 'null'],
          description: '임계치 비교 metric 이름 (optional)'
        },
        current: {
          bsonType: ['number', 'null'],
          description: '현재값 (optional)'
        },
        threshold: {
          bsonType: ['number', 'null'],
          description: '임계값 (optional)'
        },
        code: {
          bsonType: ['string', 'null'],
          description: 'Alert 코드 (optional)'
        },
        metadata: {
          bsonType: 'object',
          description: '추가 메타데이터'
        }
      }
    }
  }
});

// alerts 인덱스
db.alerts.createIndex({ timestamp: -1 });
db.alerts.createIndex({ assetId: 1, timestamp: -1 });
db.alerts.createIndex({ severity: 1, timestamp: -1 });

// ============================================
// 7. recommendations 컬렉션 (추천 이력)
// ============================================
db.createCollection('recommendations', {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: ['recommendationId', 'objectId', 'severity', 'category', 'title', 'status', 'createdAt', 'updatedAt'],
      properties: {
        recommendationId: { bsonType: 'string' },
        objectId: { bsonType: 'string' },
        objectType: { bsonType: 'string' },
        severity: { bsonType: 'string', enum: ['info', 'warning', 'critical'] },
        category: { bsonType: 'string' },
        title: { bsonType: 'string' },
        description: { bsonType: 'string' },
        suggestedAction: { bsonType: 'object' },
        analysisBasis: { bsonType: 'object' },
        status: { bsonType: 'string', enum: ['pending', 'approved', 'rejected', 'applied'] },
        createdAt: { bsonType: 'date' },
        updatedAt: { bsonType: 'date' }
      }
    }
  }
});
db.recommendations.createIndex({ recommendationId: 1 }, { unique: true, name: 'recommendationId_unique' });
db.recommendations.createIndex({ status: 1, updatedAt: -1 });
db.recommendations.createIndex({ objectId: 1, updatedAt: -1 });

print('Collections and indexes created successfully!');
print('Collections: assets, events, states, object_type_schemas, link_type_schemas, relationships, simulation_runs, alerts, recommendations');
print('Run seed scripts: infrastructure/mongo/run-seeds.sh (or zz-run-seeds.sh on first container init)');
