import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { AssetsCanvasPage, RelationshipEditPanel } from './AssetsCanvasPage'
import { getAssets } from '@/entities/asset'
import {
  getRelationships,
  updateRelationship,
  deleteRelationship,
} from '@/entities/relationship'

vi.mock('@/entities/asset', () => ({
  getAssets: vi.fn(),
  createAsset: vi.fn(),
  getAssetById: vi.fn(),
  updateAsset: vi.fn(),
}))

vi.mock('@/entities/relationship', () => ({
  getRelationships: vi.fn(),
  createRelationship: vi.fn(),
  getRelationshipById: vi.fn(),
  updateRelationship: vi.fn(),
  deleteRelationship: vi.fn(),
  RELATIONSHIP_TYPE_OPTIONS: ['feeds_into', 'contains', 'located_in'],
}))

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
        relationshipType: 'feeds_into',
        properties: {},
        createdAt: '2026-02-18T10:00:00Z',
        updatedAt: '2026-02-18T10:00:00Z',
      },
    ])
  })

  it('renders canvas and loads assets and relationships', async () => {
    vi.mocked(getRelationships).mockResolvedValue([])

    render(<AssetsCanvasPage />)

    await waitFor(() => {
      expect(getAssets).toHaveBeenCalledTimes(1)
      expect(getRelationships).toHaveBeenCalledTimes(1)
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

    expect(screen.getByText('로딩 중...')).toBeInTheDocument()

    await waitFor(() => {
      expect(screen.queryByText('로딩 중...')).not.toBeInTheDocument()
      expect(screen.getByText('freezer')).toBeInTheDocument()
    })
  })

  it('renders relationship edit panel with from/to and relationship type', () => {
    const rel = {
      id: 'rel-1',
      fromAssetId: 'asset-1',
      toAssetId: 'asset-2',
      relationshipType: 'feeds_into',
      properties: {},
      createdAt: '2026-02-18T10:00:00Z',
      updatedAt: '2026-02-18T10:00:00Z',
    }
    render(
      <RelationshipEditPanel
        relationship={rel}
        onClose={vi.fn()}
        onSaved={vi.fn()}
        onDeleted={vi.fn()}
      />
    )
    expect(screen.getByText('관계 편집')).toBeInTheDocument()
    expect(screen.getByText(/From: asset-1 → To: asset-2/)).toBeInTheDocument()
    expect(screen.getByDisplayValue('feeds_into')).toBeInTheDocument()
  })

  it('calls updateRelationship when saving from relationship edit panel', async () => {
    vi.mocked(updateRelationship).mockResolvedValue({
      id: 'rel-1',
      fromAssetId: 'asset-1',
      toAssetId: 'asset-2',
      relationshipType: 'contains',
      properties: { capacity: 100 },
      createdAt: '2026-02-18T10:00:00Z',
      updatedAt: '2026-02-18T10:00:00Z',
    })
    const rel = {
      id: 'rel-1',
      fromAssetId: 'asset-1',
      toAssetId: 'asset-2',
      relationshipType: 'feeds_into',
      properties: { capacity: 100 },
      createdAt: '2026-02-18T10:00:00Z',
      updatedAt: '2026-02-18T10:00:00Z',
    }
    render(
      <RelationshipEditPanel
        relationship={rel}
        onClose={vi.fn()}
        onSaved={vi.fn()}
        onDeleted={vi.fn()}
      />
    )

    const typeSelect = screen.getByRole('combobox', { name: /관계 타입/ })
    await userEvent.selectOptions(typeSelect, 'contains')

    const saveBtn = screen.getByRole('button', { name: /^저장$/ })
    await userEvent.click(saveBtn)

    await waitFor(() => {
      expect(updateRelationship).toHaveBeenCalledWith(
        'rel-1',
        expect.objectContaining({
          relationshipType: 'contains',
          properties: { capacity: 100 },
        })
      )
    })
  })

  it('calls deleteRelationship when deleting from relationship edit panel', async () => {
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    const rel = {
      id: 'rel-1',
      fromAssetId: 'asset-1',
      toAssetId: 'asset-2',
      relationshipType: 'feeds_into',
      properties: {},
      createdAt: '2026-02-18T10:00:00Z',
      updatedAt: '2026-02-18T10:00:00Z',
    }
    render(
      <RelationshipEditPanel
        relationship={rel}
        onClose={vi.fn()}
        onSaved={vi.fn()}
        onDeleted={vi.fn()}
      />
    )

    const deleteBtn = screen.getByRole('button', { name: /^삭제$/ })
    await userEvent.click(deleteBtn)

    await waitFor(() => {
      expect(deleteRelationship).toHaveBeenCalledWith('rel-1')
    })
    confirmSpy.mockRestore()
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
