import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { EditRelationshipOnPanel } from './EditRelationshipOnPanel'
import { updateRelationship, deleteRelationship } from '@/entities/relationship'

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

describe('EditRelationshipOnPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders relationship edit panel with from/to and relationship type', () => {
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
      <EditRelationshipOnPanel
        relationship={rel}
        linkTypeSchemas={LINK_TYPE_SCHEMAS as never}
        onClose={vi.fn()}
        onSaved={vi.fn()}
        onDeleted={vi.fn()}
      />,
    )
    expect(screen.getByText('관계 편집')).toBeInTheDocument()
    expect(screen.getByText(/From: asset-1 → To: asset-2/)).toBeInTheDocument()
    expect(screen.getByDisplayValue('Supplies')).toBeInTheDocument()
  })

  it('calls updateRelationship when saving from relationship edit panel', async () => {
    vi.mocked(updateRelationship).mockResolvedValue({
      id: 'rel-1',
      fromAssetId: 'asset-1',
      toAssetId: 'asset-2',
      relationshipType: 'ConnectedTo',
      properties: { capacity: 100 },
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
      <EditRelationshipOnPanel
        relationship={rel}
        linkTypeSchemas={LINK_TYPE_SCHEMAS as never}
        onClose={vi.fn()}
        onSaved={vi.fn()}
        onDeleted={vi.fn()}
      />,
    )

    const typeSelect = screen.getByRole('combobox', { name: /관계 타입/ })
    await userEvent.selectOptions(typeSelect, 'ConnectedTo')

    const saveBtn = screen.getByRole('button', { name: /^저장$/ })
    await userEvent.click(saveBtn)

    await waitFor(() => {
      expect(updateRelationship).toHaveBeenCalledWith(
        'rel-1',
        expect.objectContaining({
          relationshipType: 'ConnectedTo',
          properties: { capacity: 100 },
        }),
      )
    })
  })

  it('calls deleteRelationship when deleting from relationship edit panel', async () => {
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
      <EditRelationshipOnPanel
        relationship={rel}
        linkTypeSchemas={LINK_TYPE_SCHEMAS as never}
        onClose={vi.fn()}
        onSaved={vi.fn()}
        onDeleted={vi.fn()}
      />,
    )

    const deleteBtn = screen.getByRole('button', { name: /^삭제$/ })
    await userEvent.click(deleteBtn)

    await waitFor(() => {
      expect(deleteRelationship).toHaveBeenCalledWith('rel-1')
    })
    confirmSpy.mockRestore()
  })
})
