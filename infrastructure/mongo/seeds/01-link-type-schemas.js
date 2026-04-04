// LinkTypeSchema 기본 시드 (멱등: 컬렉션 전체 교체 후 삽입)
// 실행: ../run-seeds.sh 또는 Docker init의 zz-run-seeds.sh

db = db.getSiblingDB('factory_mes');

const linkTypeSeeds = [
  {
    _id: 'Supplies',
    linkType: 'Supplies',
    payloadJson: {
      schemaVersion: 'v1',
      linkType: 'Supplies',
      displayName: '공급',
      direction: 'Directed',
      temporality: 'Durable',
      fromConstraint: { requiredTraits: { dynamism: 'Dynamic' } },
      toConstraint: { requiredTraits: { dynamism: 'Dynamic' } },
      properties: [
        {
          key: 'transfers',
          dataType: 'Array',
          simulationBehavior: 'Settable',
          mutability: 'Mutable',
          baseValue: [],
          constraints: {},
          required: false
        },
        {
          key: 'ratio',
          dataType: 'Number',
          simulationBehavior: 'Settable',
          mutability: 'Mutable',
          baseValue: 1.0,
          constraints: { min: 0, max: 1 },
          required: false
        }
      ],
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    }
  },
  {
    _id: 'ConnectedTo',
    linkType: 'ConnectedTo',
    payloadJson: {
      schemaVersion: 'v1',
      linkType: 'ConnectedTo',
      displayName: '연결',
      direction: 'Bidirectional',
      temporality: 'Durable',
      fromConstraint: null,
      toConstraint: null,
      properties: [],
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    }
  },
  {
    _id: 'Contains',
    linkType: 'Contains',
    payloadJson: {
      schemaVersion: 'v1',
      linkType: 'Contains',
      displayName: '포함',
      direction: 'Hierarchical',
      temporality: 'Permanent',
      fromConstraint: null,
      toConstraint: null,
      properties: [],
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    }
  }
];

db.link_type_schemas.deleteMany({});
db.link_type_schemas.insertMany(linkTypeSeeds);

print('Seed 01-link-type-schemas: inserted ' + linkTypeSeeds.length + ' documents');
