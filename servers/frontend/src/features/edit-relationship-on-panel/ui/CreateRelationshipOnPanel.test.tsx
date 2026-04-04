import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { Node } from '@xyflow/react'
import { CreateRelationshipOnPanel } from './CreateRelationshipOnPanel'
import {
  createRelationship,
  updateRelationship,
  deleteRelationship,
} from '@/entities/relationship'
import type { CreateRelationshipFlowNodeData } from './CreateRelationshipOnPanel'

vi.mock('@/entities/relationship', () => ({
  getRelationships: vi.fn(),
  createRelationship: vi.fn(),
  getRelationshipById: vi.fn(),
  updateRelationship: vi.fn(),
  deleteRelationship: vi.fn(),
}))

const LINK_TYPE_SCHEMAS = [
  {
    schemaVersion: 'v1',
    linkType: 'Supplies',
    displayName: '공급',
    direction: 'Directed',
    temporality: 'Durable',
    fromConstraint: null,
    toConstraint: null,
    properties: [
      {
        key: 'transfers',
        dataType: 'Array',
        simulationBehavior: 'Settable',
        mutability: 'Mutable',
        baseValue: [],
        constraints: {},
        required: false,
      },
      {
        key: 'ratio',
        dataType: 'Number',
        simulationBehavior: 'Settable',
        mutability: 'Mutable',
        baseValue: 1,
        constraints: { min: 0, max: 1 },
        required: false,
      },
    ],
  },
  {
    schemaVersion: 'v1',
    linkType: 'ConnectedTo',
    displayName: '연결',
    direction: 'Bidirectional',
    temporality: 'Durable',
    fromConstraint: null,
    toConstraint: null,
    properties: [],
  },
]

const MOCK_NODES = [
  {
    id: 'asset-1',
    data: {
      asset: {
        id: 'asset-1',
        type: 'freezer',
        connections: [],
        metadata: {},
        createdAt: '',
        updatedAt: '',
      },
      liveStatus: 'online',
      liveProperties: {},
    },
    position: { x: 0, y: 0 },
  },
  {
    id: 'asset-2',
    data: {
      asset: {
        id: 'asset-2',
        type: 'tank',
        connections: [],
        metadata: {},
        createdAt: '',
        updatedAt: '',
      },
    },
  },
] as Node<CreateRelationshipFlowNodeData>[]

const defaultHandlers = {
  onSetSource: vi.fn(),
  onSetTarget: vi.fn(),
  onSwap: vi.fn(),
  onClose: vi.fn(),
}

describe('CreateRelationshipOnPanel (edit mode)', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders unified panel title and fixed From/To when editing', () => {
    const rel = {
      id: 'rel-1',
      fromAssetId: 'asset-1',
      toAssetId: 'asset-2',
      relationshipType: 'Supplies',
      properties: {},
      createdAt: '2026-02-18T10:00:00Z',
      updatedAt: '2026-02-18T10:00:00Z',
    }
    render(
      <CreateRelationshipOnPanel
        editingRelationship={rel}
        sourceId="asset-1"
        targetId="asset-2"
        nodes={MOCK_NODES}
        linkTypeSchemas={LINK_TYPE_SCHEMAS as never}
        objectTypeSchemas={[]}
        {...defaultHandlers}
        onSaved={vi.fn()}
        onDeleted={vi.fn()}
      />,
    )
    expect(
      screen.getByRole('heading', { name: '관계 설정' }),
    ).toBeInTheDocument()
    expect(screen.getByText(/이 관계의 From\/To는 고정/)).toBeInTheDocument()
    expect(screen.getByRole('combobox', { name: '관계 타입' })).toHaveValue(
      'Supplies',
    )
  })

  it('calls updateRelationship when saving in edit mode', async () => {
    vi.mocked(updateRelationship).mockResolvedValue({
      id: 'rel-1',
      fromAssetId: 'asset-1',
      toAssetId: 'asset-2',
      relationshipType: 'ConnectedTo',
      properties: {},
      createdAt: '2026-02-18T10:00:00Z',
      updatedAt: '2026-02-18T10:00:00Z',
    })
    const rel = {
      id: 'rel-1',
      fromAssetId: 'asset-1',
      toAssetId: 'asset-2',
      relationshipType: 'Supplies',
      properties: { capacity: 100 },
      createdAt: '2026-02-18T10:00:00Z',
      updatedAt: '2026-02-18T10:00:00Z',
    }
    render(
      <CreateRelationshipOnPanel
        editingRelationship={rel}
        sourceId="asset-1"
        targetId="asset-2"
        nodes={MOCK_NODES}
        linkTypeSchemas={LINK_TYPE_SCHEMAS as never}
        objectTypeSchemas={[]}
        {...defaultHandlers}
        onSaved={vi.fn()}
        onDeleted={vi.fn()}
      />,
    )

    const typeSelect = screen.getByRole('combobox', { name: '관계 타입' })
    await userEvent.selectOptions(typeSelect, 'ConnectedTo')

    await userEvent.click(screen.getByRole('button', { name: /^저장$/ }))

    await waitFor(() => {
      expect(updateRelationship).toHaveBeenCalledWith(
        'rel-1',
        expect.objectContaining({
          relationshipType: 'ConnectedTo',
          properties: expect.objectContaining({ capacity: 100 }),
        }),
      )
    })
  })

  it('calls deleteRelationship when deleting in edit mode', async () => {
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    const rel = {
      id: 'rel-1',
      fromAssetId: 'asset-1',
      toAssetId: 'asset-2',
      relationshipType: 'Supplies',
      properties: {},
      createdAt: '2026-02-18T10:00:00Z',
      updatedAt: '2026-02-18T10:00:00Z',
    }
    render(
      <CreateRelationshipOnPanel
        editingRelationship={rel}
        sourceId="asset-1"
        targetId="asset-2"
        nodes={MOCK_NODES}
        linkTypeSchemas={LINK_TYPE_SCHEMAS as never}
        objectTypeSchemas={[]}
        {...defaultHandlers}
        onSaved={vi.fn()}
        onDeleted={vi.fn()}
      />,
    )

    await userEvent.click(screen.getByRole('button', { name: /^삭제$/ }))

    await waitFor(() => {
      expect(deleteRelationship).toHaveBeenCalledWith('rel-1')
    })
    confirmSpy.mockRestore()
  })
})

describe('CreateRelationshipOnPanel (create mode)', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('submits createRelationship when source, target, and type are set', async () => {
    vi.mocked(createRelationship).mockResolvedValue({
      id: 'new-rel',
      fromAssetId: 'asset-1',
      toAssetId: 'asset-2',
      relationshipType: 'ConnectedTo',
      properties: {},
      createdAt: '',
      updatedAt: '',
    })

    render(
      <CreateRelationshipOnPanel
        sourceId="asset-1"
        targetId="asset-2"
        nodes={MOCK_NODES}
        linkTypeSchemas={LINK_TYPE_SCHEMAS as never}
        objectTypeSchemas={[]}
        {...defaultHandlers}
        onCreated={vi.fn()}
      />,
    )

    await userEvent.selectOptions(
      screen.getByRole('combobox', { name: '관계 타입' }),
      'ConnectedTo',
    )
    await userEvent.click(screen.getByRole('button', { name: '관계 생성' }))

    await waitFor(() => {
      expect(createRelationship).toHaveBeenCalledWith(
        expect.objectContaining({
          fromAssetId: 'asset-1',
          toAssetId: 'asset-2',
          relationshipType: 'ConnectedTo',
        }),
      )
    })
  })
})
