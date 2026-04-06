import { useCallback, useEffect, useRef, useState } from 'react'
import type { AssetDto } from '@/entities/asset'
import {
  runSimulation,
  startContinuousRun,
  stopRun,
  getRunEvents,
  getRunningSimulationRuns,
  subscribeSimulationEvents,
  type EventDto,
} from '@/entities/simulation'
import { CanvasSidePanel } from '@/widgets/canvas-side-panel'
import './RunSimulationOnPanel.css'

export function RunSimulationOnPanel({
  assets,
  selectedAssetId,
  onClose,
  onAssetStateUpdate,
}: {
  assets: AssetDto[]
  selectedAssetId: string | null
  onClose: () => void
  onAssetStateUpdate: (assetId: string, properties: Record<string, unknown>, status: string) => void
}) {
  const [mode, setMode] = useState<'single' | 'continuous'>('single')
  const [triggerAssetId, setTriggerAssetId] = useState(selectedAssetId ?? '')
  const [maxDepth, setMaxDepth] = useState('100')
  const [engineTickIntervalMs, setEngineTickIntervalMs] = useState('1000')
  const [running, setRunning] = useState(false)
  const [activeRunId, setActiveRunId] = useState<string | null>(null)
  const [events, setEvents] = useState<EventDto[]>([])
  const [resultMessage, setResultMessage] = useState<string | null>(null)
  const [simError, setSimError] = useState<string | null>(null)
  const [sseCleanup, setSseCleanup] = useState<(() => void) | null>(null)
  const [tickCount, setTickCount] = useState(0)

  const onAssetStateUpdateRef = useRef(onAssetStateUpdate)
  onAssetStateUpdateRef.current = onAssetStateUpdate

  useEffect(() => {
    if (selectedAssetId) setTriggerAssetId(selectedAssetId)
  }, [selectedAssetId])

  useEffect(() => {
    return () => {
      sseCleanup?.()
    }
  }, [sseCleanup])

  const startSseSubscription = useCallback(() => {
    setSseCleanup((prev) => {
      prev?.()
      return subscribeSimulationEvents((tickEvent) => {
        onAssetStateUpdateRef.current(
          tickEvent.assetId,
          tickEvent.properties,
          tickEvent.status,
        )
        setTickCount((c) => c + 1)
      })
    })
  }, [])

  const syncRunningFromServer = useCallback(async (): Promise<boolean> => {
    try {
      const runs = await getRunningSimulationRuns()
      const active = runs.filter(
        (r) =>
          r.id &&
          (r.status === 'Running' || String(r.status).toLowerCase() === 'running'),
      )
      if (active.length === 0) return false
      const first = active[0]
      setMode('continuous')
      setActiveRunId(first.id!)
      setRunning(true)
      setSimError(null)
      setResultMessage(`실행 중인 런: ${first.id}`)
      startSseSubscription()
      return true
    } catch {
      return false
    }
  }, [startSseSubscription])

  useEffect(() => {
    let cancelled = false
    void (async () => {
      const recovered = await syncRunningFromServer()
      if (cancelled || !recovered) return
    })()
    return () => {
      cancelled = true
    }
  }, [syncRunningFromServer])

  const handleSingleRun = async () => {
    if (!triggerAssetId) return
    setSimError(null)
    setResultMessage(null)
    setEvents([])
    setRunning(true)
    try {
      const depth = parseInt(maxDepth, 10)
      const tickMs = Math.min(5000, Math.max(1, parseInt(engineTickIntervalMs, 10) || 1000))
      const result = await runSimulation({
        triggerAssetId,
        maxDepth: Number.isFinite(depth) ? depth : 100,
        engineTickIntervalMs: tickMs,
      })
      setResultMessage(result.message)
      if (result.runId) {
        const evts = await getRunEvents(result.runId)
        setEvents(evts)
      }
    } catch (err) {
      setSimError(err instanceof Error ? err.message : '시뮬레이션 실패')
    } finally {
      setRunning(false)
    }
  }

  const handleStartContinuous = async () => {
    if (!triggerAssetId) return
    setSimError(null)
    setResultMessage(null)
    setEvents([])
    setTickCount(0)
    setRunning(true)
    try {
      const depth = parseInt(maxDepth, 10)
      const tickMs = Math.min(5000, Math.max(1, parseInt(engineTickIntervalMs, 10) || 1000))
      const result = await startContinuousRun({
        triggerAssetId,
        maxDepth: Number.isFinite(depth) ? depth : 100,
        engineTickIntervalMs: tickMs,
      })
      if (result.success && result.runId) {
        setActiveRunId(result.runId)
        setResultMessage(`지속 실행 시작: ${result.runId}`)
        startSseSubscription()
      } else {
        const recovered = await syncRunningFromServer()
        if (!recovered) {
          setSimError(result.message ?? '시작 실패')
          setRunning(false)
        }
      }
    } catch (err) {
      setSimError(err instanceof Error ? err.message : '시뮬레이션 시작 실패')
      setRunning(false)
    }
  }

  const handleStop = async () => {
    if (!activeRunId) return
    try {
      await stopRun(activeRunId)
      sseCleanup?.()
      setSseCleanup(null)
      setResultMessage('시뮬레이션 중지됨')
    } catch (err) {
      setSimError(err instanceof Error ? err.message : '중지 실패')
    } finally {
      setRunning(false)
      setActiveRunId(null)
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
        <label>
          트리거 에셋
          <select
            value={triggerAssetId}
            onChange={(e) => setTriggerAssetId(e.target.value)}
            disabled={running}
          >
            <option value="">선택하세요...</option>
            {assets.map((a) => (
              <option key={a.id} value={a.id}>
                {a.type} ({a.id.slice(0, 8)}...)
              </option>
            ))}
          </select>
        </label>
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
      </div>

      <div className="sim-panel__actions">
        {!running ? (
          mode === 'single' ? (
            <button
              type="button"
              onClick={handleSingleRun}
              disabled={!triggerAssetId}
              className="sim-panel__run-btn"
            >
              실행
            </button>
          ) : (
            <button
              type="button"
              onClick={handleStartContinuous}
              disabled={!triggerAssetId}
              className="sim-panel__run-btn"
            >
              시작
            </button>
          )
        ) : (
          <button
            type="button"
            onClick={handleStop}
            className="sim-panel__stop-btn"
          >
            중지
          </button>
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
          <span className="sim-panel__events-title">이벤트 ({events.length}건)</span>
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
