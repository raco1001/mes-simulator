import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen } from '@testing-library/react'
import { renderAppAtRoute } from '@/test/utils'
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

describe('AppLayout and routes', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(getAssets).mockResolvedValue([])
    vi.mocked(getStates).mockResolvedValue([])
  })

  it('shows nav links 메인 and 에셋 설정', async () => {
    renderAppAtRoute('/')
    expect(screen.getByRole('link', { name: '메인' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: '에셋 설정' })).toBeInTheDocument()
    // Wait for AssetList to finish loading (avoids act warning)
    await screen.findByText('No assets found')
  })

  it('shows main page content at /', async () => {
    const mockAssets: AssetDto[] = [
      {
        id: 'freezer-1',
        type: 'freezer',
        connections: [],
        metadata: {},
        createdAt: '2026-02-18T10:00:00Z',
        updatedAt: '2026-02-18T10:00:00Z',
      },
    ]
    const mockStates: StateDto[] = []
    vi.mocked(getAssets).mockResolvedValue(mockAssets)
    vi.mocked(getStates).mockResolvedValue(mockStates)

    renderAppAtRoute('/')

    const heading = await screen.findByRole('heading', { name: /Factory MES - Asset List/i })
    expect(heading).toBeInTheDocument()
  })

  it('shows assets settings page at /assets', async () => {
    vi.mocked(getAssets).mockResolvedValue([])

    renderAppAtRoute('/assets')

    const heading = await screen.findByRole('heading', { name: '에셋 설정' })
    expect(heading).toBeInTheDocument()
    expect(screen.getByRole('button', { name: '시뮬레이션 실행' })).toBeInTheDocument()
  })
})
