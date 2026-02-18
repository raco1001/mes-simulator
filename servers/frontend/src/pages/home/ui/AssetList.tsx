import { useEffect, useState } from 'react'
import { apiClient, type AssetDto, type StateDto } from '@/shared/api'
import './AssetList.css'

interface AssetWithState extends AssetDto {
  state?: StateDto
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
          apiClient.getAssets(),
          apiClient.getStates(),
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
                <th>Temperature</th>
                <th>Power</th>
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
                  <td>
                    {asset.state?.currentTemp !== null && asset.state?.currentTemp !== undefined
                      ? `${asset.state.currentTemp}°C`
                      : 'N/A'}
                  </td>
                  <td>
                    {asset.state?.currentPower !== null && asset.state?.currentPower !== undefined
                      ? `${asset.state.currentPower}W`
                      : 'N/A'}
                  </td>
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
