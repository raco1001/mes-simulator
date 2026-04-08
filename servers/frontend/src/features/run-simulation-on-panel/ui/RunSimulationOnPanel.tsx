import { useCallback, useEffect, useMemo, useState } from 'react'
import type { AssetDto } from '@/entities/asset'
import type { ObjectTypeSchemaDto } from '@/entities/object-type-schema'
import {
  runSimulation,
  startContinuousRun,
  stopRun,
  getRunEvents,
  type EventDto,
} from '@/entities/simulation'
import { mergeEligibleMappingProperties } from '@/shared/lib/canvasMetadata'
import type { SimCanvasPhase } from '@/pages/canvas/lib/useCanvasSimulationSync'
import { CanvasSidePanel } from '@/widgets/canvas-side-panel'
import './RunSimulationOnPanel.css'

export type CanvasSimulationControl = {
  simCanvasPhase: SimCanvasPhase
  running: boolean
  setRunning: (v: boolean) => void
  activeRunId: string | null
  setActiveRunId: (id: string | null) => void
  tickCount: number
  setTickCount: React.Dispatch<React.SetStateAction<number>>
  startSseSubscription: () => void
  stopSseSubscription: () => void
  syncRunningFromServer: () => Promise<boolean>
  onContinuousStarted: () => void
  onContinuousStopped: () => void
  onSingleRunStarted: () => void
  onSingleRunEnded: () => void
}

export function isTriggerCandidateAsset(
  asset: AssetDto,
  objectTypeSchemas: ObjectTypeSchemaDto[],
): boolean {
  const schema = objectTypeSchemas.find((s) => s.objectType === asset.type)
  const merged = mergeEligibleMappingProperties(
    schema ?? null,
    asset.metadata,
  )
  return merged.length > 0
}

export function RunSimulationOnPanel({
  assets,
  selectedAssetId,
  onClose,
  simulation,
  objectTypeSchemas,
}: {
  assets: AssetDto[]
  selectedAssetId: string | null
  onClose: () => void
  simulation: CanvasSimulationControl
  objectTypeSchemas: ObjectTypeSchemaDto[]
}) {
  const [mode, setMode] = useState<'single' | 'continuous'>('single')
  const [triggerAssetIds, setTriggerAssetIds] = useState<string[]>([])
  const [maxDepth, setMaxDepth] = useState('100')
  const [engineTickIntervalMs, setEngineTickIntervalMs] = useState('1000')
  const [events, setEvents] = useState<EventDto[]>([])
  const [resultMessage, setResultMessage] = useState<string | null>(null)
  const [simError, setSimError] = useState<string | null>(null)
  const [resetState, setResetState] = useState(false)

  const {
    running,
    activeRunId,
    setActiveRunId,
    tickCount,
    setTickCount,
    startSseSubscription,
    syncRunningFromServer,
    onContinuousStarted,
    onContinuousStopped,
    onSingleRunStarted,
    onSingleRunEnded,
  } = simulation

  const triggerCandidates = useMemo(
    () => assets.filter((a) => isTriggerCandidateAsset(a, objectTypeSchemas)),
    [assets, objectTypeSchemas],
  )

  useEffect(() => {
    if (running && activeRunId) setMode('continuous')
  }, [running, activeRunId])

  useEffect(() => {
    if (!selectedAssetId) return
    if (!triggerCandidates.some((a) => a.id === selectedAssetId)) return
    setTriggerAssetIds((prev) => {
      if (prev.includes(selectedAssetId)) return prev
      return [selectedAssetId, ...prev]
    })
  }, [selectedAssetId, triggerCandidates])

  useEffect(() => {
    setTriggerAssetIds((prev) => {
      const ids = triggerCandidates.map((a) => a.id)
      if (ids.length === 0) return []
      const next = prev.filter((id) => ids.includes(id))
      if (next.length > 0) return next
      return ids
    })
  }, [triggerCandidates])

  const toggleTriggerId = useCallback((id: string, checked: boolean) => {
    setTriggerAssetIds((prev) => {
      if (checked) return prev.includes(id) ? prev : [...prev, id]
      return prev.filter((x) => x !== id)
    })
  }, [])

  const selectAllTriggers = useCallback(() => {
    setTriggerAssetIds(triggerCandidates.map((a) => a.id))
  }, [triggerCandidates])

  const clearAllTriggers = useCallback(() => {
    setTriggerAssetIds([])
  }, [])

  const resolvedTriggerIds = (): string[] => triggerAssetIds

  const handleSingleRun = async () => {
    const ids = resolvedTriggerIds()
    if (ids.length === 0) return
    setSimError(null)
    setResultMessage(null)
    setEvents([])
    onSingleRunStarted()
    try {
      const depth = parseInt(maxDepth, 10)
      const tickMs = Math.min(
        5000,
        Math.max(1, parseInt(engineTickIntervalMs, 10) || 1000),
      )
      const base = {
        maxDepth: Number.isFinite(depth) ? depth : 100,
        engineTickIntervalMs: tickMs,
        ...(resetState ? { resetState: true } : {}),
      }
      const body =
        ids.length === 1
          ? { triggerAssetId: ids[0], ...base }
          : { triggerAssetIds: ids, ...base }
      const result = await runSimulation(body)
      setResultMessage(result.message)
      if (result.runId) {
        const evts = await getRunEvents(result.runId)
        setEvents(evts)
      }
    } catch (err) {
      setSimError(err instanceof Error ? err.message : '시뮬레이션 실패')
    } finally {
      onSingleRunEnded()
    }
  }

  const handleStartContinuous = async () => {
    const ids = resolvedTriggerIds()
    if (ids.length === 0) return
    setSimError(null)
    setResultMessage(null)
    setEvents([])
    setTickCount(0)
    onContinuousStarted()
    try {
      const depth = parseInt(maxDepth, 10)
      const tickMs = Math.min(
        5000,
        Math.max(1, parseInt(engineTickIntervalMs, 10) || 1000),
      )
      const base = {
        maxDepth: Number.isFinite(depth) ? depth : 100,
        engineTickIntervalMs: tickMs,
        ...(resetState ? { resetState: true } : {}),
      }
      const body =
        ids.length === 1
          ? { triggerAssetId: ids[0], ...base }
          : { triggerAssetIds: ids, ...base }
      const result = await startContinuousRun(body)
      if (result.success && result.runId) {
        setActiveRunId(result.runId)
        setResultMessage(`지속 실행 시작: ${result.runId}`)
        startSseSubscription()
      } else {
        const recovered = await syncRunningFromServer()
        if (!recovered) {
          setSimError(result.message ?? '시작 실패')
          onSingleRunEnded()
        }
      }
    } catch (err) {
      setSimError(err instanceof Error ? err.message : '시뮬레이션 시작 실패')
      onSingleRunEnded()
    }
  }

  const handleStop = async () => {
    if (!activeRunId) return
    try {
      await stopRun(activeRunId)
      setResultMessage('시뮬레이션 중지됨')
      onContinuousStopped()
    } catch (err) {
      setSimError(err instanceof Error ? err.message : '중지 실패')
    }
  }

  return (
    <CanvasSidePanel className="assets-canvas-page__sim-panel">
      <div className="assets-canvas-page__side-panel-header">
        <h3>시뮬레이션</h3>
        <button type="button" onClick={onClose} aria-label="닫기">
          ×
        </button>
      </div>

      <div className="sim-panel__mode-toggle">
        <button
          type="button"
          className={mode === 'single' ? 'active' : ''}
          onClick={() => setMode('single')}
          disabled={running}
        >
          1회 실행
        </button>
        <button
          type="button"
          className={mode === 'continuous' ? 'active' : ''}
          onClick={() => setMode('continuous')}
          disabled={running}
        >
          지속 실행
        </button>
      </div>

      <div className="sim-panel__section">
        <div className="sim-panel__trigger-list">
          <div className="sim-panel__trigger-list-header">
            <span>트리거 후보 에셋</span>
            <button
              type="button"
              className="sim-panel__link-btn"
              onClick={selectAllTriggers}
              disabled={running || triggerCandidates.length === 0}
            >
              전체 선택
            </button>
            <button
              type="button"
              className="sim-panel__link-btn"
              onClick={clearAllTriggers}
              disabled={running}
            >
              전체 해제
            </button>
          </div>
          <ul className="sim-panel__trigger-checkboxes">
            {triggerCandidates.map((a) => (
              <li key={a.id}>
                <label>
                  <input
                    type="checkbox"
                    checked={triggerAssetIds.includes(a.id)}
                    onChange={(e) => toggleTriggerId(a.id, e.target.checked)}
                    disabled={running}
                  />
                  <span>
                    {a.type} ({a.id.slice(0, 8)}…)
                  </span>
                </label>
              </li>
            ))}
          </ul>
        </div>
        <label>
          최대 전파 깊이 (리프까지 권장: 100+)
          <input
            type="number"
            min="1"
            max="2000"
            value={maxDepth}
            onChange={(e) => setMaxDepth(e.target.value)}
            disabled={running}
          />
        </label>
        <label title="연속 시뮬 엔진이 깨어나는 주기(ms). 단발 실행에도 Rate/Accumulator 델타에 사용됩니다.">
          엔진 tick (ms)
          <input
            type="number"
            min={1}
            max={5000}
            value={engineTickIntervalMs}
            onChange={(e) => setEngineTickIntervalMs(e.target.value)}
            disabled={running}
          />
        </label>
        <label className="sim-panel__reset-state">
          <input
            type="checkbox"
            checked={resetState}
            onChange={(e) => setResetState(e.target.checked)}
            disabled={running}
          />
          <span>
            참여 에셋 state 초기화 후 시작 (Mongo states 삭제, 에셋·스키마 기준 재시드. 이벤트
            이력은 유지)
          </span>
        </label>
      </div>

      <div className="sim-panel__actions">
        {!running ? (
          mode === 'single' ? (
            <button
              type="button"
              onClick={handleSingleRun}
              disabled={triggerAssetIds.length === 0}
              className="sim-panel__run-btn"
            >
              실행
            </button>
          ) : (
            <button
              type="button"
              onClick={handleStartContinuous}
              disabled={resolvedTriggerIds().length === 0}
              className="sim-panel__run-btn"
            >
              시작
            </button>
          )
        ) : activeRunId ? (
          <button
            type="button"
            onClick={handleStop}
            className="sim-panel__stop-btn"
          >
            중지
          </button>
        ) : (
          <p className="sim-panel__running-hint">1회 실행 중…</p>
        )}
      </div>

      {simError && <p className="assets-canvas-page__error">{simError}</p>}
      {resultMessage && <p className="sim-panel__result">{resultMessage}</p>}

      {running && activeRunId && (
        <div className="sim-panel__live-indicator">
          <span className="sim-panel__live-dot" />
          LIVE — tick 수신: {tickCount}
        </div>
      )}

      {events.length > 0 && (
        <div className="sim-panel__events">
          <span className="sim-panel__events-title">
            이벤트 ({events.length}건)
          </span>
          <ul className="sim-panel__events-list">
            {events.map((evt, i) => (
              <li key={i} className="sim-panel__event-item">
                <span className="sim-panel__event-type">{evt.eventType}</span>
                <span className="sim-panel__event-asset">{evt.assetId}</span>
                <time>{new Date(evt.occurredAt).toLocaleTimeString()}</time>
              </li>
            ))}
          </ul>
        </div>
      )}
    </CanvasSidePanel>
  )
}
