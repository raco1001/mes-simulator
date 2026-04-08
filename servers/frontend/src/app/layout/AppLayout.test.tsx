import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import { renderAppAtRoute } from '@/test/utils'
import { getAssets } from '@/entities/asset'
import { getStates } from '@/entities/state'
import { getAlerts } from '@/entities/alert'
import { getRelationships } from '@/entities/relationship'

vi.mock('@/entities/asset', () => ({
  getAssets: vi.fn(),
  getAssetById: vi.fn(),
  createAsset: vi.fn(),
  updateAsset: vi.fn(),
}))
vi.mock('@/entities/state', () => ({
  getStates: vi.fn(),
  getStateByAssetId: vi.fn(),
}))
vi.mock('@/entities/alert', () => ({
  getAlerts: vi.fn(),
  subscribeAlerts: vi.fn(() => () => {}),
}))
vi.mock('@/entities/relationship', () => ({
  getRelationships: vi.fn(),
  createRelationship: vi.fn(),
  getRelationshipById: vi.fn(),
  updateRelationship: vi.fn(),
  deleteRelationship: vi.fn(),
}))
vi.mock('@/entities/link-type-schema', () => ({
  getLinkTypeSchemas: vi.fn().mockResolvedValue([]),
}))
vi.mock('@/entities/simulation', () => ({
  runSimulation: vi.fn(),
  startContinuousRun: vi.fn(),
  stopRun: vi.fn(),
  getRunEvents: vi.fn(),
  getRunningSimulationRuns: vi.fn().mockResolvedValue([]),
  subscribeSimulationEvents: vi.fn(() => () => {}),
}))

describe('AppLayout and routes', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(getAssets).mockResolvedValue([])
    vi.mocked(getStates).mockResolvedValue([])
    vi.mocked(getAlerts).mockResolvedValue([])
    vi.mocked(getRelationships).mockResolvedValue([])
  })

  it('shows nav links 홈, 모니터링, 추천 and app title', async () => {
    renderAppAtRoute('/')
    expect(screen.getByRole('link', { name: '홈' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: '모니터링' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: '추천' })).toBeInTheDocument()
    expect(screen.getByText('Ontology Simulator')).toBeInTheDocument()

    await waitFor(() => {
      expect(getAssets).toHaveBeenCalled()
    })
  })

  it('renders canvas page at / (home)', async () => {
    vi.mocked(getAssets).mockResolvedValue([])
    vi.mocked(getRelationships).mockResolvedValue([])

    renderAppAtRoute('/')

    await waitFor(() => {
      expect(screen.getByText('에셋 추가')).toBeInTheDocument()
    })
  })

  it('renders monitoring page at /monitoring', async () => {
    renderAppAtRoute('/monitoring')

    const heading = await screen.findByRole('heading', { name: /모니터링/i })
    expect(heading).toBeInTheDocument()
  })
})
