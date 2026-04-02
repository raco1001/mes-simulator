import { useCallback, useEffect, useState } from 'react'
import { getAssets, type AssetDto } from '@/entities/asset'
import { getStates, type StateDto } from '@/entities/state'
import { getAlerts, type AlertDto } from '@/entities/alert'
import './MonitoringPage.css'

const POLL_INTERVAL_MS = 30_000

interface AssetWithState extends AssetDto {
  state?: StateDto
}

function renderProperties(properties: Record<string, unknown> | undefined): string {
  if (!properties || Object.keys(properties).length === 0) return 'N/A'
  return Object.entries(properties)
    .map(([k, v]) => `${k}: ${String(v)}`)
    .join(', ')
}

export function MonitoringPage() {
  const [assets, setAssets] = useState<AssetWithState[]>([])
  const [alerts, setAlerts] = useState<AlertDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const fetchData = useCallback(async (showLoading = false) => {
    try {
      if (showLoading) setLoading(true)
      const [assetsData, statesData, alertsData] = await Promise.all([
        getAssets(),
        getStates(),
        getAlerts(20),
      ])
      const merged: AssetWithState[] = assetsData.map((asset) => {
        const state = statesData.find((s) => s.assetId === asset.id)
        return { ...asset, state }
      })
      setAssets(merged)
      setAlerts(alertsData)
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch data')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    fetchData(true)
    const timer = setInterval(() => fetchData(false), POLL_INTERVAL_MS)
    return () => clearInterval(timer)
  }, [fetchData])

  const normalCount = assets.filter((a) => a.state?.status === 'normal').length
  const warningCount = assets.filter((a) => a.state?.status === 'warning').length
  const errorCount = assets.filter((a) => a.state?.status === 'error').length

  if (loading) {
    return <div className="monitoring-loading">Loading...</div>
  }

  return (
    <div className="monitoring-page">
      <div className="monitoring-header">
        <h1>모니터링</h1>
        <div>
          <button type="button" className="monitoring-refresh-btn" onClick={() => fetchData(false)}>
            새로고침
          </button>
          <span className="monitoring-auto-hint"> (30초 자동 갱신)</span>
        </div>
      </div>

      <div className="monitoring-summary">
        <div className="monitoring-card monitoring-card--total">
          <div className="monitoring-card__count">{assets.length}</div>
          <div className="monitoring-card__label">전체 에셋</div>
        </div>
        <div className="monitoring-card monitoring-card--normal">
          <div className="monitoring-card__count">{normalCount}</div>
          <div className="monitoring-card__label">정상</div>
        </div>
        <div className="monitoring-card monitoring-card--warning">
          <div className="monitoring-card__count">{warningCount}</div>
          <div className="monitoring-card__label">경고</div>
        </div>
        <div className="monitoring-card monitoring-card--error">
          <div className="monitoring-card__count">{errorCount}</div>
          <div className="monitoring-card__label">에러</div>
        </div>
      </div>

      {error && <div className="monitoring-error">{error}</div>}

      <section className="monitoring-section">
        <h2>에셋 상태</h2>
        {assets.length === 0 ? (
          <div className="monitoring-empty">등록된 에셋이 없습니다.</div>
        ) : (
          <table className="monitoring-table">
            <thead>
              <tr>
                <th>ID</th>
                <th>Type</th>
                <th>Status</th>
                <th>Properties</th>
                <th>Updated</th>
              </tr>
            </thead>
            <tbody>
              {assets.map((asset) => (
                <tr key={asset.id}>
                  <td>{asset.id}</td>
                  <td>{asset.type}</td>
                  <td>
                    <span className={`status-badge status-${asset.state?.status ?? 'unknown'}`}>
                      {asset.state?.status ?? 'N/A'}
                    </span>
                  </td>
                  <td>{renderProperties(asset.state?.properties)}</td>
                  <td>
                    {asset.state?.updatedAt
                      ? new Date(asset.state.updatedAt).toLocaleString()
                      : 'N/A'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      <section className="monitoring-section">
        <h2>최근 알림 이력</h2>
        {alerts.length === 0 ? (
          <div className="monitoring-empty">알림 이력이 없습니다.</div>
        ) : (
          <table className="monitoring-table">
            <thead>
              <tr>
                <th>Severity</th>
                <th>Asset</th>
                <th>Message</th>
                <th>Time</th>
              </tr>
            </thead>
            <tbody>
              {alerts.map((alert, i) => (
                <tr key={`${alert.assetId}-${alert.timestamp}-${i}`}>
                  <td>
                    <span className={`alert-severity--${alert.severity}`}>
                      {alert.severity}
                    </span>
                  </td>
                  <td>{alert.assetId}</td>
                  <td>{alert.message}</td>
                  <td>{new Date(alert.timestamp).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </div>
  )
}
