import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { AssetList } from './AssetList'
import { getAssets } from '@/entities/asset'
import { getStates } from '@/entities/state'
import type { AssetDto } from '@/entities/asset'
import type { StateDto } from '@/entities/state'

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

describe('AssetList', () => {
  const mockAssets: AssetDto[] = [
    {
      id: 'freezer-1',
      type: 'freezer',
      connections: ['conveyor-1'],
      metadata: {},
      createdAt: '2026-02-18T10:00:00Z',
      updatedAt: '2026-02-18T10:00:00Z',
    },
    {
      id: 'conveyor-1',
      type: 'conveyor',
      connections: [],
      metadata: {},
      createdAt: '2026-02-18T10:00:00Z',
      updatedAt: '2026-02-18T10:00:00Z',
    },
  ]

  const mockStates: StateDto[] = [
    {
      assetId: 'freezer-1',
      currentTemp: -5.0,
      currentPower: 120.0,
      status: 'normal',
      lastEventType: 'asset.health.updated',
      updatedAt: '2026-02-18T10:00:00Z',
      metadata: {},
    },
    {
      assetId: 'conveyor-1',
      currentTemp: null,
      currentPower: null,
      status: 'warning',
      lastEventType: 'asset.health.updated',
      updatedAt: '2026-02-18T10:00:00Z',
      metadata: {},
    },
  ]

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders loading state initially', () => {
    vi.mocked(getAssets).mockImplementation(
      () => new Promise(() => {}), // Never resolves
    )
    vi.mocked(getStates).mockImplementation(
      () => new Promise(() => {}), // Never resolves
    )

    render(<AssetList />)
    expect(screen.getByText('Loading assets...')).toBeInTheDocument()
  })

  it('renders assets with states', async () => {
    vi.mocked(getAssets).mockResolvedValue(mockAssets)
    vi.mocked(getStates).mockResolvedValue(mockStates)

    render(<AssetList />)

    await waitFor(() => {
      expect(screen.getByText('freezer-1')).toBeInTheDocument()
      expect(screen.getByText('conveyor-1')).toBeInTheDocument()
    })

    expect(screen.getByText('freezer')).toBeInTheDocument()
    expect(screen.getByText('conveyor')).toBeInTheDocument()
    // Status는 이모지와 함께 표시되므로 정규식으로 검색
    expect(screen.getByText(/normal/i)).toBeInTheDocument()
    expect(screen.getByText(/warning/i)).toBeInTheDocument()
    expect(screen.getByText('-5°C')).toBeInTheDocument()
    expect(screen.getByText('120W')).toBeInTheDocument()
  })

  it('renders error message on API failure', async () => {
    vi.mocked(getAssets).mockRejectedValue(new Error('API Error'))
    vi.mocked(getStates).mockRejectedValue(new Error('API Error'))

    render(<AssetList />)

    await waitFor(() => {
      expect(screen.getByText(/Error:/)).toBeInTheDocument()
    })
  })

  it('renders empty state when no assets', async () => {
    vi.mocked(getAssets).mockResolvedValue([])
    vi.mocked(getStates).mockResolvedValue([])

    render(<AssetList />)

    await waitFor(() => {
      expect(screen.getByText('No assets found')).toBeInTheDocument()
    })
  })

  it('displays N/A for assets without state', async () => {
    vi.mocked(getAssets).mockResolvedValue(mockAssets)
    vi.mocked(getStates).mockResolvedValue([])

    render(<AssetList />)

    await waitFor(() => {
      expect(screen.getByText('freezer-1')).toBeInTheDocument()
    })

    // Check for N/A values
    const naElements = screen.getAllByText('N/A')
    expect(naElements.length).toBeGreaterThan(0)
  })
})
