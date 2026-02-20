import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { AssetsPage } from './AssetsPage'
import { getAssets, createAsset } from '@/entities/asset'
import type { AssetDto } from '@/entities/asset'

vi.mock('@/entities/asset', () => ({
  getAssets: vi.fn(),
  createAsset: vi.fn(),
  getAssetById: vi.fn(),
  updateAsset: vi.fn(),
}))

describe('AssetsPage', () => {
  const mockAssets: AssetDto[] = [
    {
      id: 'freezer-1',
      type: 'freezer',
      connections: ['conveyor-1'],
      metadata: {},
      createdAt: '2026-02-18T10:00:00Z',
      updatedAt: '2026-02-18T10:00:00Z',
    },
  ]

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(getAssets).mockResolvedValue(mockAssets)
  })

  it('renders asset list from getAssets', async () => {
    render(<AssetsPage />)

    await waitFor(() => {
      expect(screen.getByText('freezer-1')).toBeInTheDocument()
      expect(screen.getByText('freezer')).toBeInTheDocument()
    })
  })

  it('calls createAsset on form submit with type and connections', async () => {
    const user = userEvent.setup()
    vi.mocked(createAsset).mockResolvedValue({
      ...mockAssets[0],
      id: 'new-1',
      type: 'conveyor',
      connections: ['freezer-1'],
    })
    vi.mocked(getAssets).mockResolvedValue(mockAssets)

    render(<AssetsPage />)

    await waitFor(() => {
      expect(screen.getByLabelText(/Type/)).toBeInTheDocument()
    })

    await user.type(screen.getByLabelText(/Type \(필수\)/), 'conveyor')
    await user.type(screen.getByLabelText(/Connections/), 'freezer-1')
    await user.click(screen.getByRole('button', { name: '생성' }))

    await waitFor(() => {
      expect(createAsset).toHaveBeenCalledTimes(1)
      expect(createAsset).toHaveBeenCalledWith({
        type: 'conveyor',
        connections: ['freezer-1'],
        metadata: {},
      })
    })
  })

  it('simulation button does not call API and shows status', async () => {
    const user = userEvent.setup()
    render(<AssetsPage />)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: '시뮬레이션 실행' })).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: '시뮬레이션 실행' }))

    expect(createAsset).not.toHaveBeenCalled()
    expect(getAssets).toHaveBeenCalledTimes(1)
    expect(screen.getByText(/시뮬레이션 요청됨/)).toBeInTheDocument()
  })
})
