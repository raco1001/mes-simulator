import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { AssetsCanvasPage } from './AssetsCanvasPage'
import { getAssets } from '@/entities/asset'
import { getObjectTypeSchemas } from '@/entities/object-type-schema'
import { getRelationships } from '@/entities/relationship'
import { getLinkTypeSchemas } from '@/entities/link-type-schema'

vi.mock('@/entities/asset', () => ({
  getAssets: vi.fn(),
  createAsset: vi.fn(),
  getAssetById: vi.fn(),
  updateAsset: vi.fn(),
  deleteAsset: vi.fn(),
}))

vi.mock('@/entities/relationship', () => ({
  getRelationships: vi.fn(),
  createRelationship: vi.fn(),
  getRelationshipById: vi.fn(),
  updateRelationship: vi.fn(),
  deleteRelationship: vi.fn(),
}))

vi.mock('@/entities/object-type-schema', () => ({
  getObjectTypeSchemas: vi.fn(),
  createObjectTypeSchema: vi.fn(),
  updateObjectTypeSchema: vi.fn(),
  deleteObjectTypeSchema: vi.fn(),
}))

vi.mock('@/entities/link-type-schema', () => ({
  getLinkTypeSchemas: vi.fn(),
}))

vi.mock('@/entities/simulation', () => ({
  runSimulation: vi.fn(),
  startContinuousRun: vi.fn(),
  stopRun: vi.fn(),
  getRunEvents: vi.fn(),
  subscribeSimulationEvents: vi.fn(() => () => {}),
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
      { key: 'transfers', dataType: 'Array', simulationBehavior: 'Settable', mutability: 'Mutable', baseValue: [], constraints: {}, required: false },
      { key: 'ratio', dataType: 'Number', simulationBehavior: 'Settable', mutability: 'Mutable', baseValue: 1, constraints: { min: 0, max: 1 }, required: false },
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

describe('AssetsCanvasPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(getAssets).mockResolvedValue([
      {
        id: 'asset-1',
        type: 'freezer',
        connections: [],
        metadata: { capacity: '1000' },
        createdAt: '2026-02-18T10:00:00Z',
        updatedAt: '2026-02-18T10:00:00Z',
      },
    ])
    vi.mocked(getRelationships).mockResolvedValue([
      {
        id: 'rel-1',
        fromAssetId: 'asset-1',
        toAssetId: 'asset-2',
        relationshipType: 'Supplies',
        properties: {},
        createdAt: '2026-02-18T10:00:00Z',
        updatedAt: '2026-02-18T10:00:00Z',
      },
    ])
    vi.mocked(getObjectTypeSchemas).mockResolvedValue([
      {
        schemaVersion: 'v1',
        objectType: 'freezer',
        displayName: 'Freezer',
        traits: { persistence: 'Durable', dynamism: 'Dynamic', cardinality: 'Singular' },
        classifications: [],
        ownProperties: [],
        allowedLinks: [],
      },
    ])
    vi.mocked(getLinkTypeSchemas).mockResolvedValue(LINK_TYPE_SCHEMAS as never)
  })

  it('renders canvas and loads assets and relationships', async () => {
    vi.mocked(getRelationships).mockResolvedValue([])

    render(<AssetsCanvasPage />)

    await waitFor(() => {
      expect(getAssets).toHaveBeenCalledTimes(1)
      expect(getRelationships).toHaveBeenCalledTimes(1)
      expect(getLinkTypeSchemas).toHaveBeenCalledTimes(1)
    })

    await waitFor(() => {
      expect(screen.getByText('freezer')).toBeInTheDocument()
      expect(screen.getByText('에셋 추가')).toBeInTheDocument()
      expect(screen.getByText('관계 만들기')).toBeInTheDocument()
    })
  })

  it('shows loading then content', async () => {
    vi.mocked(getRelationships).mockResolvedValue([])

    render(<AssetsCanvasPage />)

    expect(screen.getByText('Loading...')).toBeInTheDocument()

    await waitFor(() => {
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument()
      expect(screen.getByText('freezer')).toBeInTheDocument()
    })
  })

  it('enters rel mode when clicking 관계 만들기', async () => {
    vi.mocked(getAssets).mockResolvedValue([
      { id: 'a1', type: 'pump', connections: [], metadata: {}, createdAt: '', updatedAt: '' },
      { id: 'a2', type: 'tank', connections: [], metadata: {}, createdAt: '', updatedAt: '' },
    ])
    vi.mocked(getRelationships).mockResolvedValue([])

    render(<AssetsCanvasPage />)

    await waitFor(() => expect(screen.getByText('pump')).toBeInTheDocument())

    await userEvent.click(screen.getByText('관계 만들기'))

    expect(screen.getByText('관계 만들기 취소')).toBeInTheDocument()
    expect(screen.getByText(/관계 편집 모드/)).toBeInTheDocument()
    expect(screen.getByText('관계 만들기', { selector: 'h3' })).toBeInTheDocument()
  })

  it('shows system info when toggle is clicked in asset side panel', async () => {
    vi.mocked(getRelationships).mockResolvedValue([])

    render(<AssetsCanvasPage />)

    await waitFor(() => {
      expect(screen.getByText('freezer')).toBeInTheDocument()
    })

    const node = screen.getByTestId('rf__node-asset-1')
    fireEvent.click(node)

    await waitFor(() => {
      expect(screen.getByText('에셋 편집')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: '시스템 정보' })).toBeInTheDocument()
    })

    expect(screen.queryByText('asset-1')).not.toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: '시스템 정보' }))

    await waitFor(() => {
      expect(screen.getByText('asset-1')).toBeInTheDocument()
    })
  })
})
