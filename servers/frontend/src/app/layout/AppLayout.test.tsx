import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen } from '@testing-library/react'
import { renderAppAtRoute } from '@/test/utils'
import { apiClient } from '@/shared/api'
import type { AssetDto, StateDto } from '@/shared/api'

vi.mock('@/shared/api', () => ({
  apiClient: {
    getAssets: vi.fn(),
    getStates: vi.fn(),
  },
}))

describe('AppLayout and routes', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(apiClient.getAssets).mockResolvedValue([])
    vi.mocked(apiClient.getStates).mockResolvedValue([])
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
    vi.mocked(apiClient.getAssets).mockResolvedValue(mockAssets)
    vi.mocked(apiClient.getStates).mockResolvedValue(mockStates)

    renderAppAtRoute('/')

    const heading = await screen.findByRole('heading', { name: /Factory MES - Asset List/i })
    expect(heading).toBeInTheDocument()
  })

  it('shows assets settings page at /assets', async () => {
    vi.mocked(apiClient.getAssets).mockResolvedValue([])

    renderAppAtRoute('/assets')

    const heading = await screen.findByRole('heading', { name: '에셋 설정' })
    expect(heading).toBeInTheDocument()
    expect(screen.getByRole('button', { name: '시뮬레이션 실행' })).toBeInTheDocument()
  })
})
