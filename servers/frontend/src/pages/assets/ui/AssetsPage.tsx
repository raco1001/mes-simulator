import { useEffect, useState } from 'react'
import { apiClient, type AssetDto, type CreateAssetRequest } from '@/shared/api'
import './AssetsPage.css'

export function AssetsPage() {
  const [assets, setAssets] = useState<AssetDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [formType, setFormType] = useState('')
  const [formConnections, setFormConnections] = useState('')
  const [createError, setCreateError] = useState<string | null>(null)
  const [simulationRequested, setSimulationRequested] = useState(false)

  const loadAssets = async () => {
    try {
      setLoading(true)
      const data = await apiClient.getAssets()
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

    const body: CreateAssetRequest = {
      type,
      connections,
      metadata: {},
    }
    try {
      await apiClient.createAsset(body)
      setFormType('')
      setFormConnections('')
      await loadAssets()
    } catch (err) {
      setCreateError(
        err instanceof Error ? err.message : 'Failed to create asset',
      )
    }
  }

  const handleSimulationClick = () => {
    setSimulationRequested(true)
    // 백엔드 시뮬레이션 API 연동 시 여기서 호출
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
                <th>Created</th>
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
                  <td>{new Date(asset.createdAt).toLocaleString()}</td>
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
          {createError && (
            <div className="assets-page-error">{createError}</div>
          )}
          <button type="submit">생성</button>
        </form>
      </section>

      <section className="assets-page-section">
        <h2>시뮬레이션</h2>
        <button
          type="button"
          onClick={handleSimulationClick}
          className="assets-simulation-btn"
        >
          시뮬레이션 실행
        </button>
        {simulationRequested && (
          <p className="assets-simulation-status">
            시뮬레이션 요청됨 (기능은 추후 연동)
          </p>
        )}
      </section>
    </div>
  )
}
