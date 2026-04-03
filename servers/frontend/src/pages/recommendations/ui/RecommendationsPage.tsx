import { useEffect, useMemo, useState } from 'react'
import {
  applyRecommendation,
  getRecommendations,
  type RecommendationDto,
  type RecommendationSeverity,
  type RecommendationStatus,
} from '@/entities/recommendation'
import { runWhatIf, type WhatIfResultDto } from '@/entities/simulation'
import './RecommendationsPage.css'

const PAGE_SIZE = 8

export function RecommendationsPage() {
  const [items, setItems] = useState<RecommendationDto[]>([])
  const [statusFilter, setStatusFilter] = useState<RecommendationStatus | 'all'>('all')
  const [severityFilter, setSeverityFilter] = useState<RecommendationSeverity | 'all'>('all')
  const [selected, setSelected] = useState<RecommendationDto | null>(null)
  const [whatIfResult, setWhatIfResult] = useState<WhatIfResultDto | null>(null)
  const [whatIfLoading, setWhatIfLoading] = useState(false)
  const [applyLoading, setApplyLoading] = useState(false)
  const [page, setPage] = useState(1)
  const [error, setError] = useState<string | null>(null)

  async function fetchRecommendations() {
    try {
      const data = await getRecommendations({
        status: statusFilter === 'all' ? undefined : statusFilter,
        severity: severityFilter === 'all' ? undefined : severityFilter,
      })
      setItems(data)
      setError(null)
      if (data.length === 0) {
        setSelected(null)
      } else if (!selected || !data.find((x) => x.recommendationId === selected.recommendationId)) {
        setSelected(data[0])
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch recommendations')
    }
  }

  useEffect(() => {
    setPage(1)
    void fetchRecommendations()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [statusFilter, severityFilter])

  const pagedItems = useMemo(() => {
    const start = (page - 1) * PAGE_SIZE
    return items.slice(start, start + PAGE_SIZE)
  }, [items, page])
  const totalPages = Math.max(1, Math.ceil(items.length / PAGE_SIZE))

  const onWhatIf = async () => {
    if (!selected) return
    setWhatIfLoading(true)
    setError(null)
    try {
      const action = selected.suggestedAction as Record<string, unknown>
      const triggerAssetId = String(action.triggerAssetId ?? selected.objectId)
      const patch = (action.patch ?? { properties: {} }) as Record<string, unknown>
      const result = await runWhatIf({
        triggerAssetId,
        patch: patch as { properties?: Record<string, unknown | null> },
        maxDepth: 3,
      })
      setWhatIfResult(result)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'What-if failed')
    } finally {
      setWhatIfLoading(false)
    }
  }

  const onApply = async () => {
    if (!selected) return
    const confirmed = window.confirm('이 추천을 실제로 적용하시겠습니까?')
    if (!confirmed) return

    setApplyLoading(true)
    setError(null)
    try {
      await applyRecommendation(selected.recommendationId)
      await fetchRecommendations()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Apply failed')
    } finally {
      setApplyLoading(false)
    }
  }

  return (
    <div className="recommendations-page">
      <div className="recommendations-header">
        <h1>Recommendations</h1>
        <div className="recommendations-filters">
          <select
            aria-label="status-filter"
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as RecommendationStatus | 'all')}
          >
            <option value="all">All Status</option>
            <option value="pending">pending</option>
            <option value="approved">approved</option>
            <option value="rejected">rejected</option>
            <option value="applied">applied</option>
          </select>
          <select
            aria-label="severity-filter"
            value={severityFilter}
            onChange={(e) => setSeverityFilter(e.target.value as RecommendationSeverity | 'all')}
          >
            <option value="all">All Severity</option>
            <option value="info">info</option>
            <option value="warning">warning</option>
            <option value="critical">critical</option>
          </select>
        </div>
      </div>

      {error && <p className="recommendations-error">{error}</p>}

      <div className="recommendations-layout">
        <section className="recommendations-list">
          {pagedItems.map((item) => (
            <button
              type="button"
              key={item.recommendationId}
              className={`recommendation-card ${selected?.recommendationId === item.recommendationId ? 'active' : ''}`}
              onClick={() => {
                setSelected(item)
                setWhatIfResult(null)
              }}
            >
              <span className={`severity severity-${item.severity}`}>{item.severity}</span>
              <strong>{item.title}</strong>
              <p>{item.description}</p>
              <small>{item.objectId}</small>
            </button>
          ))}
          {pagedItems.length === 0 && <div className="recommendations-empty">No recommendations</div>}
          <div className="recommendations-pagination">
            <button type="button" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
              Prev
            </button>
            <span>
              {page} / {totalPages}
            </span>
            <button type="button" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>
              Next
            </button>
          </div>
        </section>

        <section className="recommendations-detail">
          {!selected ? (
            <div className="recommendations-empty">Select one recommendation</div>
          ) : (
            <>
              <h2>{selected.title}</h2>
              <p>{selected.description}</p>
              <p>
                <b>Status:</b> {selected.status}
              </p>
              <p>
                <b>Category:</b> {selected.category}
              </p>
              <div className="recommendations-actions">
                <button type="button" onClick={onWhatIf} disabled={whatIfLoading}>
                  {whatIfLoading ? 'Running what-if...' : 'Run What-if'}
                </button>
                <button type="button" onClick={onApply} disabled={applyLoading}>
                  {applyLoading ? 'Applying...' : 'Apply'}
                </button>
              </div>

              {whatIfResult && (
                <div className="whatif-panel">
                  <h3>What-if Preview</h3>
                  <p>
                    affected: {whatIfResult.affectedObjects.length}, depth: {whatIfResult.propagationDepth}
                  </p>
                  <table>
                    <thead>
                      <tr>
                        <th>Object</th>
                        <th>Key</th>
                        <th>Before</th>
                        <th>After</th>
                        <th>Delta</th>
                      </tr>
                    </thead>
                    <tbody>
                      {whatIfResult.deltas.flatMap((obj) =>
                        obj.changes.map((c) => (
                          <tr key={`${obj.objectId}-${c.key}`}>
                            <td>{obj.objectId}</td>
                            <td>{c.key}</td>
                            <td>{String(c.before ?? '-')}</td>
                            <td>{String(c.after ?? '-')}</td>
                            <td>{String(c.delta ?? '-')}</td>
                          </tr>
                        )),
                      )}
                    </tbody>
                  </table>
                </div>
              )}
            </>
          )}
        </section>
      </div>
    </div>
  )
}
