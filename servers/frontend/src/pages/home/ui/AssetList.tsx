import { useEffect, useState } from 'react'
import { getAssets, type AssetDto } from '@/entities/asset'
import { getStates, type StateDto } from '@/entities/state'
import './AssetList.css'

interface AssetWithState extends AssetDto {
  state?: StateDto
}

function summarizeProperties(properties: Record<string, unknown> | undefined): string {
  if (!properties || Object.keys(properties).length === 0) return 'N/A'
  return Object.entries(properties)
    .map(([k, v]) => `${k}: ${String(v)}`)
    .join(', ')
}

export function AssetList() {
  const [assets, setAssets] = useState<AssetWithState[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true)
        const [assetsData, statesData] = await Promise.all([
          getAssets(),
          getStates(),
        ])

        // Asset과 State를 결합
        const assetsWithState: AssetWithState[] = assetsData.map((asset) => {
          const state = statesData.find((s) => s.assetId === asset.id)
          return { ...asset, state }
        })

        setAssets(assetsWithState)
        setError(null)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to fetch assets')
      } finally {
        setLoading(false)
      }
    }

    fetchData()
  }, [])

  const getStatusColor = (status?: string) => {
    switch (status) {
      case 'normal':
        return 'status-normal'
      case 'warning':
        return 'status-warning'
      case 'error':
        return 'status-error'
      default:
        return 'status-unknown'
    }
  }

  const getStatusEmoji = (status?: string) => {
    switch (status) {
      case 'normal':
        return '🟢'
      case 'warning':
        return '🟡'
      case 'error':
        return '🔴'
      default:
        return '⚪'
    }
  }

  if (loading) {
    return <div className="asset-list-loading">Loading assets...</div>
  }

  if (error) {
    return <div className="asset-list-error">Error: {error}</div>
  }

  return (
    <div className="asset-list">
      <h1>Factory MES - Asset List</h1>
      <div className="asset-list-container">
        {assets.length === 0 ? (
          <div className="asset-list-empty">No assets found</div>
        ) : (
          <table className="asset-table">
            <thead>
              <tr>
                <th>ID</th>
                <th>Type</th>
                <th>Status</th>
                <th>Properties</th>
                <th>Updated At</th>
              </tr>
            </thead>
            <tbody>
              {assets.map((asset) => (
                <tr key={asset.id}>
                  <td>{asset.id}</td>
                  <td>{asset.type}</td>
                  <td>
                    <span className={`status-badge ${getStatusColor(asset.state?.status)}`}>
                      {getStatusEmoji(asset.state?.status)} {asset.state?.status || 'N/A'}
                    </span>
                  </td>
                  <td>{summarizeProperties(asset.state?.properties)}</td>
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
      </div>
    </div>
  )
}
