import { useEffect, useState } from 'react'
import {
  getAssets,
  createAsset,
  updateAsset,
  type AssetDto,
  type CreateAssetRequest,
  type UpdateAssetRequest,
} from '@/entities/asset'
import {
  getRunEvents,
  runSimulation,
  startContinuousRun,
  stopRun,
  type EventDto,
} from '@/entities/simulation'
import './AssetsPage.css'

export function AssetsPage() {
  const [assets, setAssets] = useState<AssetDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [formType, setFormType] = useState('')
  const [formConnections, setFormConnections] = useState('')
  const [formMetadata, setFormMetadata] = useState<Array<{ key: string; value: string }>>([])
  const [createError, setCreateError] = useState<string | null>(null)
  const [simulationLoading, setSimulationLoading] = useState(false)
  const [simulationResult, setSimulationResult] = useState<{
    runId: string
    message: string
  } | null>(null)
  const [simulationError, setSimulationError] = useState<string | null>(null)
  const [continuousRunId, setContinuousRunId] = useState<string | null>(null)
  const [continuousStartLoading, setContinuousStartLoading] = useState(false)
  const [stopLoading, setStopLoading] = useState(false)
  const [runEvents, setRunEvents] = useState<EventDto[] | null>(null)
  const [runEventsLoading, setRunEventsLoading] = useState(false)
  const [runEventsError, setRunEventsError] = useState<string | null>(null)
  const [editingAsset, setEditingAsset] = useState<AssetDto | null>(null)
  const [editType, setEditType] = useState('')
  const [editConnections, setEditConnections] = useState('')
  const [editMetadata, setEditMetadata] = useState<Array<{ key: string; value: string }>>([])
  const [editError, setEditError] = useState<string | null>(null)

  const recordToMetadataRows = (meta: Record<string, unknown>): Array<{ key: string; value: string }> =>
    Object.entries(meta ?? {}).map(([key, value]) => ({
      key,
      value: typeof value === 'string' ? value : JSON.stringify(value),
    }))

  const formMetadataToRecord = (rows: Array<{ key: string; value: string }>): Record<string, unknown> => {
    const out: Record<string, unknown> = {}
    for (const row of rows) {
      const k = row.key.trim()
      if (k) out[k] = row.value
    }
    return out
  }

  const addMetadataRow = () => setFormMetadata((prev) => [...prev, { key: '', value: '' }])
  const removeMetadataRow = (index: number) =>
    setFormMetadata((prev) => prev.filter((_, i) => i !== index))
  const setMetadataRow = (index: number, field: 'key' | 'value', value: string) =>
    setFormMetadata((prev) =>
      prev.map((row, i) => (i === index ? { ...row, [field]: value } : row)),
    )

  const addEditMetadataRow = () => setEditMetadata((prev) => [...prev, { key: '', value: '' }])
  const removeEditMetadataRow = (index: number) =>
    setEditMetadata((prev) => prev.filter((_, i) => i !== index))
  const setEditMetadataRow = (index: number, field: 'key' | 'value', value: string) =>
    setEditMetadata((prev) =>
      prev.map((row, i) => (i === index ? { ...row, [field]: value } : row)),
    )

  const openEdit = (asset: AssetDto) => {
    setEditingAsset(asset)
    setEditType(asset.type)
    setEditConnections(asset.connections?.length ? asset.connections.join(', ') : '')
    setEditMetadata(recordToMetadataRows(asset.metadata ?? {}))
    setEditError(null)
  }

  const closeEdit = () => {
    setEditingAsset(null)
    setEditError(null)
  }

  const handleEditSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!editingAsset) return
    setEditError(null)
    const connections = editConnections
      .split(',')
      .map((s) => s.trim())
      .filter(Boolean)
    const metadata = formMetadataToRecord(editMetadata)
    const body: UpdateAssetRequest = {
      type: editType.trim() || undefined,
      connections,
      metadata: Object.keys(metadata).length ? metadata : undefined,
    }
    try {
      await updateAsset(editingAsset.id, body)
      closeEdit()
      await loadAssets()
    } catch (err) {
      setEditError(err instanceof Error ? err.message : 'Failed to update asset')
    }
  }

  const metadataSummary = (meta: Record<string, unknown> | undefined) => {
    if (!meta || Object.keys(meta).length === 0) return '-'
    return Object.entries(meta)
      .slice(0, 2)
      .map(([k, v]) => `${k}: ${v}`)
      .join(', ')
  }

  const loadAssets = async () => {
    try {
      setLoading(true)
      const data = await getAssets()
      setAssets(data)
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch assets')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadAssets()
  }, [])

  const handleCreateSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setCreateError(null)
    const type = formType.trim()
    if (!type) {
      setCreateError('Type is required')
      return
    }
    const connections = formConnections
      .split(',')
      .map((s) => s.trim())
      .filter(Boolean)

    const metadata = formMetadataToRecord(formMetadata)
    const body: CreateAssetRequest = {
      type,
      connections,
      metadata,
    }
    try {
      await createAsset(body)
      setFormType('')
      setFormConnections('')
      setFormMetadata([])
      await loadAssets()
    } catch (err) {
      setCreateError(
        err instanceof Error ? err.message : 'Failed to create asset',
      )
    }
  }

  const handleSimulationClick = async () => {
    setSimulationError(null)
    setSimulationResult(null)
    setRunEvents(null)
    setRunEventsError(null)
    setSimulationLoading(true)
    try {
      const triggerAssetId = assets[0]?.id ?? ''
      if (!triggerAssetId) {
        setSimulationError('트리거로 사용할 에셋이 없습니다. 에셋을 먼저 추가하세요.')
        return
      }
      const result = await runSimulation({
        triggerAssetId,
        maxDepth: 3,
      })
      setSimulationResult({
        runId: result.runId,
        message: result.message,
      })
    } catch (err) {
      setSimulationError(err instanceof Error ? err.message : 'Simulation request failed')
    } finally {
      setSimulationLoading(false)
    }
  }

  const handleShowEvents = async () => {
    if (!simulationResult?.runId) return
    setRunEventsError(null)
    setRunEventsLoading(true)
    try {
      const list = await getRunEvents(simulationResult.runId)
      setRunEvents(list)
    } catch (err) {
      setRunEventsError(err instanceof Error ? err.message : 'Failed to load events')
    } finally {
      setRunEventsLoading(false)
    }
  }

  const handleStartContinuousClick = async () => {
    setSimulationError(null)
    setContinuousStartLoading(true)
    try {
      const triggerAssetId = assets[0]?.id ?? ''
      if (!triggerAssetId) {
        setSimulationError('트리거로 사용할 에셋이 없습니다. 에셋을 먼저 추가하세요.')
        return
      }
      const result = await startContinuousRun({ triggerAssetId, maxDepth: 3 })
      if (result.success) {
        setContinuousRunId(result.runId)
        setSimulationResult({ runId: result.runId, message: '지속 시뮬레이션 시작됨' })
        setRunEvents(null)
        setRunEventsError(null)
      } else {
        setSimulationError(result.message ?? '지속 실행을 시작할 수 없습니다.')
      }
    } catch (err) {
      setSimulationError(err instanceof Error ? err.message : 'Start continuous run failed')
    } finally {
      setContinuousStartLoading(false)
    }
  }

  const handleStopClick = async () => {
    if (!continuousRunId) return
    setSimulationError(null)
    setStopLoading(true)
    try {
      await stopRun(continuousRunId)
      setContinuousRunId(null)
      setSimulationResult(null)
      setRunEvents(null)
      setRunEventsError(null)
    } catch (err) {
      setSimulationError(err instanceof Error ? err.message : 'Stop run failed')
    } finally {
      setStopLoading(false)
    }
  }

  const payloadSummary = (p?: Record<string, unknown>) => {
    if (!p || Object.keys(p).length === 0) return '-'
    return JSON.stringify(p)
  }

  if (loading) {
    return <div className="assets-page-loading">Loading assets...</div>
  }

  return (
    <div className="assets-page">
      <h1>에셋 설정</h1>

      <section className="assets-page-section">
        <h2>에셋 목록</h2>
        {error && <div className="assets-page-error">{error}</div>}
        {assets.length === 0 ? (
          <div className="assets-page-empty">No assets found</div>
        ) : (
          <table className="assets-table">
            <thead>
              <tr>
                <th>ID</th>
                <th>Type</th>
                <th>Connections</th>
                <th>Metadata</th>
                <th>Created</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {assets.map((asset) => (
                <tr key={asset.id}>
                  <td>{asset.id}</td>
                  <td>{asset.type}</td>
                  <td>
                    {asset.connections?.length
                      ? asset.connections.join(', ')
                      : '-'}
                  </td>
                  <td>{metadataSummary(asset.metadata)}</td>
                  <td>{new Date(asset.createdAt).toLocaleString()}</td>
                  <td>
                    <button type="button" onClick={() => openEdit(asset)} className="assets-edit-btn">
                      수정
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      <section className="assets-page-section">
        <h2>에셋 생성</h2>
        <form onSubmit={handleCreateSubmit} className="assets-create-form">
          <div className="form-group">
            <label htmlFor="asset-type">Type (필수)</label>
            <input
              id="asset-type"
              type="text"
              value={formType}
              onChange={(e) => setFormType(e.target.value)}
              placeholder="e.g. freezer, conveyor"
            />
          </div>
          <div className="form-group">
            <label htmlFor="asset-connections">Connections (쉼표 구분)</label>
            <input
              id="asset-connections"
              type="text"
              value={formConnections}
              onChange={(e) => setFormConnections(e.target.value)}
              placeholder="id1, id2"
            />
          </div>
          <div className="form-group">
            <span className="form-label">Metadata (선택)</span>
            {formMetadata.map((row, index) => (
              <div key={index} className="metadata-row">
                <input
                  type="text"
                  value={row.key}
                  onChange={(e) => setMetadataRow(index, 'key', e.target.value)}
                  placeholder="key"
                  className="metadata-key"
                />
                <input
                  type="text"
                  value={row.value}
                  onChange={(e) => setMetadataRow(index, 'value', e.target.value)}
                  placeholder="value"
                  className="metadata-value"
                />
                <button
                  type="button"
                  onClick={() => removeMetadataRow(index)}
                  className="metadata-remove"
                  aria-label="Remove row"
                >
                  삭제
                </button>
              </div>
            ))}
            <button type="button" onClick={addMetadataRow} className="metadata-add">
              항목 추가
            </button>
          </div>
          {createError && (
            <div className="assets-page-error">{createError}</div>
          )}
          <button type="submit">생성</button>
        </form>
      </section>

      {editingAsset && (
        <section className="assets-page-section assets-edit-modal">
          <h2>에셋 수정 — {editingAsset.id}</h2>
          <form onSubmit={handleEditSubmit} className="assets-create-form">
            <div className="form-group">
              <label htmlFor="edit-type">Type</label>
              <input
                id="edit-type"
                type="text"
                value={editType}
                onChange={(e) => setEditType(e.target.value)}
                placeholder="e.g. freezer, conveyor"
              />
            </div>
            <div className="form-group">
              <label htmlFor="edit-connections">Connections (쉼표 구분)</label>
              <input
                id="edit-connections"
                type="text"
                value={editConnections}
                onChange={(e) => setEditConnections(e.target.value)}
                placeholder="id1, id2"
              />
            </div>
            <div className="form-group">
              <span className="form-label">Metadata (선택)</span>
              {editMetadata.map((row, index) => (
                <div key={index} className="metadata-row">
                  <input
                    type="text"
                    value={row.key}
                    onChange={(e) => setEditMetadataRow(index, 'key', e.target.value)}
                    placeholder="key"
                    className="metadata-key"
                  />
                  <input
                    type="text"
                    value={row.value}
                    onChange={(e) => setEditMetadataRow(index, 'value', e.target.value)}
                    placeholder="value"
                    className="metadata-value"
                  />
                  <button
                    type="button"
                    onClick={() => removeEditMetadataRow(index)}
                    className="metadata-remove"
                    aria-label="Remove row"
                  >
                    삭제
                  </button>
                </div>
              ))}
              <button type="button" onClick={addEditMetadataRow} className="metadata-add">
                항목 추가
              </button>
            </div>
            {editError && <div className="assets-page-error">{editError}</div>}
            <div className="assets-edit-actions">
              <button type="submit">저장</button>
              <button type="button" onClick={closeEdit}>
                취소
              </button>
            </div>
          </form>
        </section>
      )}

      <section className="assets-page-section">
        <h2>시뮬레이션</h2>
        <button
          type="button"
          onClick={handleSimulationClick}
          className="assets-simulation-btn"
          disabled={simulationLoading}
        >
          {simulationLoading ? '실행 중…' : '시뮬레이션 실행'}
        </button>
        <button
          type="button"
          onClick={handleStartContinuousClick}
          className="assets-simulation-btn"
          disabled={continuousStartLoading || !assets[0]?.id}
        >
          {continuousStartLoading ? '시작 중…' : '지속 실행 시작'}
        </button>
        {continuousRunId && (
          <button
            type="button"
            onClick={handleStopClick}
            className="assets-simulation-btn"
            disabled={stopLoading}
          >
            {stopLoading ? '중단 중…' : '중단'}
          </button>
        )}
        {simulationError && (
          <p className="assets-simulation-status assets-simulation-error">{simulationError}</p>
        )}
        {simulationResult && (
          <>
            <p className="assets-simulation-status">
              {simulationResult.message} (runId: {simulationResult.runId})
            </p>
            <button
              type="button"
              onClick={handleShowEvents}
              className="assets-events-btn"
              disabled={runEventsLoading}
            >
              {runEventsLoading ? '불러오는 중…' : '이벤트 보기'}
            </button>
            {runEventsError && (
              <p className="assets-simulation-status assets-simulation-error">{runEventsError}</p>
            )}
            {runEvents && (
              <div className="assets-run-events">
                <h3>실행 결과 이벤트</h3>
                {runEvents.length === 0 ? (
                  <p className="assets-page-empty">이벤트 없음</p>
                ) : (
                  <table className="assets-table">
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
                          <td className="assets-event-payload">{payloadSummary(evt.payload)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>
            )}
          </>
        )}
      </section>
    </div>
  )
}
