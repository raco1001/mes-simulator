import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { AssetsPage } from './AssetsPage'
import { getAssets, createAsset } from '@/entities/asset'
import type { AssetDto } from '@/entities/asset'
import { runSimulation, startContinuousRun, stopRun } from '@/entities/simulation'

vi.mock('@/entities/asset', () => ({
  getAssets: vi.fn(),
  createAsset: vi.fn(),
  getAssetById: vi.fn(),
  updateAsset: vi.fn(),
}))

vi.mock('@/entities/simulation', () => ({
  runSimulation: vi.fn(),
  getRunEvents: vi.fn(),
  startContinuousRun: vi.fn(),
  stopRun: vi.fn(),
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

  it('calls createAsset with metadata when metadata rows are filled', async () => {
    const user = userEvent.setup()
    vi.mocked(createAsset).mockResolvedValue({
      ...mockAssets[0],
      id: 'new-1',
      type: 'sensor',
      connections: [],
      metadata: { location: 'floor-a' },
    })
    vi.mocked(getAssets).mockResolvedValue(mockAssets)

    render(<AssetsPage />)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: '항목 추가' })).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: '항목 추가' }))
    const keyInputs = screen.getAllByPlaceholderText('key')
    const valueInputs = screen.getAllByPlaceholderText('value')
    await user.type(keyInputs[0], 'location')
    await user.type(valueInputs[0], 'floor-a')
    await user.type(screen.getByLabelText(/Type \(필수\)/), 'sensor')
    await user.click(screen.getByRole('button', { name: '생성' }))

    await waitFor(() => {
      expect(createAsset).toHaveBeenCalledTimes(1)
      expect(createAsset).toHaveBeenCalledWith({
        type: 'sensor',
        connections: [],
        metadata: { location: 'floor-a' },
      })
    })
  })

  it('simulation button calls runSimulation and shows result', async () => {
    const user = userEvent.setup()
    vi.mocked(runSimulation).mockResolvedValue({
      success: true,
      runId: 'run-123',
      message: 'Simulation run completed',
    })

    render(<AssetsPage />)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: '시뮬레이션 실행' })).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: '시뮬레이션 실행' }))

    await waitFor(() => {
      expect(runSimulation).toHaveBeenCalledTimes(1)
      expect(runSimulation).toHaveBeenCalledWith({
        triggerAssetId: 'freezer-1',
        maxDepth: 3,
      })
    })
    expect(screen.getByText(/Simulation run completed/)).toBeInTheDocument()
    expect(screen.getByText(/runId: run-123/)).toBeInTheDocument()
  })

  it('지속 실행 시작 button calls startContinuousRun and shows result', async () => {
    const user = userEvent.setup()
    vi.mocked(startContinuousRun).mockResolvedValue({
      success: true,
      runId: 'continuous-run-1',
    })

    render(<AssetsPage />)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: '지속 실행 시작' })).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: '지속 실행 시작' }))

    await waitFor(() => {
      expect(startContinuousRun).toHaveBeenCalledTimes(1)
      expect(startContinuousRun).toHaveBeenCalledWith({
        triggerAssetId: 'freezer-1',
        maxDepth: 3,
      })
    })
    expect(screen.getByText(/지속 시뮬레이션 시작됨/)).toBeInTheDocument()
    expect(screen.getByText(/runId: continuous-run-1/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: '중단' })).toBeInTheDocument()
  })

  it('중단 button calls stopRun when continuous run is active', async () => {
    const user = userEvent.setup()
    vi.mocked(startContinuousRun).mockResolvedValue({
      success: true,
      runId: 'continuous-run-1',
    })
    vi.mocked(stopRun).mockResolvedValue({ success: true })

    render(<AssetsPage />)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: '지속 실행 시작' })).toBeInTheDocument()
    })
    await user.click(screen.getByRole('button', { name: '지속 실행 시작' }))

    await waitFor(() => {
      expect(screen.getByRole('button', { name: '중단' })).toBeInTheDocument()
    })
    await user.click(screen.getByRole('button', { name: '중단' }))

    await waitFor(() => {
      expect(stopRun).toHaveBeenCalledTimes(1)
      expect(stopRun).toHaveBeenCalledWith('continuous-run-1')
    })
  })
})
