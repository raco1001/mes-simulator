import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { RecommendationsPage } from './RecommendationsPage'

vi.mock('@/entities/recommendation', () => ({
  getRecommendations: vi.fn(),
  applyRecommendation: vi.fn(),
}))

vi.mock('@/entities/simulation', () => ({
  runWhatIf: vi.fn(),
}))

import { applyRecommendation, getRecommendations, type RecommendationDto } from '@/entities/recommendation'
import { runWhatIf } from '@/entities/simulation'

const makePendingRec = (overrides: Partial<RecommendationDto> = {}): RecommendationDto => ({
  recommendationId: 'r1',
  objectId: 'battery-1',
  objectType: 'Battery',
  severity: 'warning',
  category: 'energy',
  title: 'Charge up',
  description: 'Increase charge rate',
  suggestedAction: { triggerAssetId: 'battery-1', patch: { properties: { chargeRate: 500 } } },
  analysisBasis: {},
  status: 'pending',
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  ...overrides,
})

describe('RecommendationsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(getRecommendations).mockResolvedValue([makePendingRec()])
    vi.mocked(runWhatIf).mockResolvedValue({
      runId: 'whatif-1',
      before: {},
      after: {},
      deltas: [{ objectId: 'battery-1', changes: [{ key: 'chargeRate', before: 100, after: 500, delta: 400 }] }],
      affectedObjects: ['battery-1'],
      propagationDepth: 1,
    })
    vi.mocked(applyRecommendation).mockResolvedValue({
      success: true,
      runId: 'run-1',
      recommendation: {} as never,
    })
  })

  it('renders list and shows detail on first load', async () => {
    render(<RecommendationsPage />)
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Recommendations' })).toBeInTheDocument())
    expect(screen.getAllByText('Increase charge rate').length).toBeGreaterThanOrEqual(1)
    expect(screen.getAllByText('pending').length).toBeGreaterThanOrEqual(1)
  })

  it('runs what-if and displays delta table', async () => {
    render(<RecommendationsPage />)
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Recommendations' })).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Run What-if' }))
    await waitFor(() => expect(screen.getByText('What-if Preview')).toBeInTheDocument())
    expect(screen.getByText('chargeRate')).toBeInTheDocument()
    expect(screen.getByText('100')).toBeInTheDocument()
    expect(screen.getByText('500')).toBeInTheDocument()
    expect(screen.getByText('400')).toBeInTheDocument()
  })

  it('applies recommendation after confirm and refreshes list', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    let callCount = 0
    vi.mocked(getRecommendations).mockImplementation(async () => {
      callCount++
      if (callCount <= 1) return [makePendingRec()]
      return [makePendingRec({ status: 'applied' })]
    })

    render(<RecommendationsPage />)
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Recommendations' })).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Apply' }))
    await waitFor(() => expect(applyRecommendation).toHaveBeenCalledWith('r1'))
    await waitFor(() => expect(screen.getByText('applied')).toBeInTheDocument())
  })

  it('does not apply when confirm is cancelled', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false)

    render(<RecommendationsPage />)
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Recommendations' })).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Apply' }))
    expect(applyRecommendation).not.toHaveBeenCalled()
  })

  it('filters by status', async () => {
    render(<RecommendationsPage />)
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Recommendations' })).toBeInTheDocument())

    fireEvent.change(screen.getByLabelText('status-filter'), { target: { value: 'applied' } })
    await waitFor(() => expect(getRecommendations).toHaveBeenCalledWith({ status: 'applied', severity: undefined }))
  })

  it('shows empty state when no recommendations', async () => {
    vi.mocked(getRecommendations).mockResolvedValue([])
    render(<RecommendationsPage />)
    await waitFor(() => expect(screen.getByText('No recommendations')).toBeInTheDocument())
  })
})
