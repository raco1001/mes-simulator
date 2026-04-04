import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getAssets, type AssetDto } from '@/entities/asset'
import {
  runSimulation,
  startContinuousRun,
  stopRun,
  getRunEvents,
  subscribeSimulationEvents,
  type EventDto,
} from '@/entities/simulation'
import './SimulationPage.css'

export function SimulationPage() {
  const [assets, setAssets] = useState<AssetDto[]>([])
  const [loading, setLoading] = useState(true)
  const [triggerAssetId, setTriggerAssetId] = useState('')
  const [maxDepth, setMaxDepth] = useState(3)

  const [simLoading, setSimLoading] = useState(false)
  const [simResult, setSimResult] = useState<{ runId: string; message: string } | null>(null)
  const [simError, setSimError] = useState<string | null>(null)

  const [continuousRunId, setContinuousRunId] = useState<string | null>(null)
  const [continuousStartLoading, setContinuousStartLoading] = useState(false)
  const [stopLoading, setStopLoading] = useState(false)

  const [runEvents, setRunEvents] = useState<EventDto[] | null>(null)
  const [runEventsLoading, setRunEventsLoading] = useState(false)
  const [runEventsError, setRunEventsError] = useState<string | null>(null)

  const [liveStates, setLiveStates] = useState<Record<string, Record<string, unknown>>>({})

  useEffect(() => {
    const load = async () => {
      try {
        const data = await getAssets()
        setAssets(data)
        if (data.length > 0) setTriggerAssetId(data[0].id)
      } catch {
        // non-fatal
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [])

  useEffect(() => {
    if (!continuousRunId) {
      setLiveStates({})
      return
    }
    const unsubscribe = subscribeSimulationEvents((tickEvent) => {
      setLiveStates((prev) => ({
        ...prev,
        [tickEvent.assetId]: {
          ...(prev[tickEvent.assetId] ?? {}),
          ...tickEvent.properties,
        },
      }))
    })
    return unsubscribe
  }, [continuousRunId])

  const handleRun = async () => {
    if (!triggerAssetId) return
    setSimError(null)
    setSimResult(null)
    setRunEvents(null)
    setRunEventsError(null)
    setSimLoading(true)
    try {
      const result = await runSimulation({ triggerAssetId, maxDepth })
      setSimResult({ runId: result.runId, message: result.message })
    } catch (err) {
      setSimError(err instanceof Error ? err.message : 'Simulation failed')
    } finally {
      setSimLoading(false)
    }
  }

  const handleStartContinuous = async () => {
    if (!triggerAssetId) return
    setSimError(null)
    setContinuousStartLoading(true)
    try {
      const result = await startContinuousRun({ triggerAssetId, maxDepth })
      if (result.success) {
        setContinuousRunId(result.runId)
        setSimResult({ runId: result.runId, message: '지속 시뮬레이션 시작됨' })
        setRunEvents(null)
        setRunEventsError(null)
      } else {
        setSimError(result.message ?? '지속 실행을 시작할 수 없습니다.')
      }
    } catch (err) {
      setSimError(err instanceof Error ? err.message : 'Start failed')
    } finally {
      setContinuousStartLoading(false)
    }
  }

  const handleStop = async () => {
    if (!continuousRunId) return
    setSimError(null)
    setStopLoading(true)
    try {
      await stopRun(continuousRunId)
      setContinuousRunId(null)
      setSimResult(null)
      setRunEvents(null)
    } catch (err) {
      setSimError(err instanceof Error ? err.message : 'Stop failed')
    } finally {
      setStopLoading(false)
    }
  }

  const handleShowEvents = async () => {
    if (!simResult?.runId) return
    setRunEventsError(null)
    setRunEventsLoading(true)
    try {
      const list = await getRunEvents(simResult.runId)
      setRunEvents(list)
    } catch (err) {
      setRunEventsError(err instanceof Error ? err.message : 'Failed to load events')
    } finally {
      setRunEventsLoading(false)
    }
  }

  if (loading) {
    return <div className="simulation-loading">Loading...</div>
  }

  if (assets.length === 0) {
    return (
      <div className="simulation-empty">
        <p>시뮬레이션을 실행하려면 먼저 에셋을 추가하세요.</p>
        <Link to="/">홈에서 에셋 추가하기</Link>
      </div>
    )
  }

  return (
    <div className="simulation-page">
      <h1>시뮬레이션</h1>

      <section className="simulation-section">
        <h2>실행 설정</h2>
        <div className="simulation-controls">
          <div className="simulation-field">
            <label htmlFor="trigger-asset">트리거 에셋</label>
            <select
              id="trigger-asset"
              value={triggerAssetId}
              onChange={(e) => setTriggerAssetId(e.target.value)}
            >
              {assets.map((a) => (
                <option key={a.id} value={a.id}>
                  {a.id} ({a.type})
                </option>
              ))}
            </select>
          </div>
          <div className="simulation-field">
            <label htmlFor="max-depth">최대 깊이</label>
            <input
              id="max-depth"
              type="number"
              min={1}
              max={10}
              value={maxDepth}
              onChange={(e) => setMaxDepth(Number(e.target.value) || 3)}
            />
          </div>
        </div>

        <div className="simulation-actions">
          <button type="button" onClick={handleRun} disabled={simLoading || !triggerAssetId}>
            {simLoading ? '실행 중...' : '1회 실행'}
          </button>
          <button
            type="button"
            onClick={handleStartContinuous}
            disabled={continuousStartLoading || !triggerAssetId}
          >
            {continuousStartLoading ? '시작 중...' : '지속 실행'}
          </button>
          {continuousRunId && (
            <button type="button" onClick={handleStop} disabled={stopLoading}>
              {stopLoading ? '중단 중...' : '중단'}
            </button>
          )}
        </div>

        {simError && <p className="simulation-status simulation-error">{simError}</p>}

        {simResult && (
          <div>
            <p className="simulation-status">
              {simResult.message} (runId: {simResult.runId})
            </p>
            <button type="button" onClick={handleShowEvents} disabled={runEventsLoading}>
              {runEventsLoading ? '불러오는 중...' : '이벤트 보기'}
            </button>
          </div>
        )}
      </section>

      {Object.keys(liveStates).length > 0 && (
        <section className="simulation-section simulation-live-states">
          <h2>실시간 Asset 상태</h2>
          <div className="simulation-live-grid">
            {Object.entries(liveStates).map(([assetId, props]) => (
              <div key={assetId} className="simulation-live-card">
                <div className="simulation-live-card-title">
                  {assets.find((a) => a.id === assetId)?.id ?? assetId}
                </div>
                {Object.entries(props).map(([k, v]) => (
                  <div key={k} className="simulation-live-prop">
                    <span className="simulation-live-prop-key">{k}:</span>{' '}
                    <span className="simulation-live-prop-val">{String(v)}</span>
                  </div>
                ))}
              </div>
            ))}
          </div>
        </section>
      )}

      {runEventsError && <p className="simulation-status simulation-error">{runEventsError}</p>}

      {runEvents && (
        <section className="simulation-section">
          <h2>실행 결과 이벤트</h2>
          {runEvents.length === 0 ? (
            <p className="simulation-empty">이벤트 없음</p>
          ) : (
            <table className="simulation-table">
              <thead>
                <tr>
                  <th>Asset ID</th>
                  <th>Event Type</th>
                  <th>Occurred At</th>
                  <th>Payload</th>
                </tr>
              </thead>
              <tbody>
                {runEvents.map((evt, i) => (
                  <tr key={`${evt.assetId}-${evt.occurredAt}-${i}`}>
                    <td>{evt.assetId}</td>
                    <td>{evt.eventType}</td>
                    <td>{new Date(evt.occurredAt).toLocaleString()}</td>
                    <td className="simulation-payload">
                      {evt.payload && Object.keys(evt.payload).length > 0
                        ? JSON.stringify(evt.payload)
                        : '-'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>
      )}
    </div>
  )
}
