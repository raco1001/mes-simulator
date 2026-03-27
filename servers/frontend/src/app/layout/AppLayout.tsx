import { Outlet, NavLink } from 'react-router-dom'
import './AppLayout.css'
import { useCallback, useEffect, useState } from 'react'
import { AlertToast } from '@/entities/alert/ui/AlertToast'
import { subscribeAlerts, type AlertDto } from '@/entities/alert'

interface AlertToastItem {
  id: string
  alert: AlertDto
}

const MAX_RECENT_ALERTS = 20

export function AppLayout() {
  const [toastQueue, setToastQueue] = useState<AlertToastItem[]>([])
  const [recentAlerts, setRecentAlerts] = useState<AlertDto[]>([])

  const dismissToast = useCallback((id: string) => {
    setToastQueue((current) => current.filter((item) => item.id !== id))
  }, [])

  useEffect(() => {
    const unsubscribe = subscribeAlerts((alert) => {
      const id = `${alert.assetId}-${alert.timestamp}-${Math.random().toString(36).slice(2, 7)}`
      setToastQueue((current) => [...current, { id, alert }])
      setRecentAlerts((current) => [alert, ...current].slice(0, MAX_RECENT_ALERTS))
    })

    return () => {
      unsubscribe()
    }
  }, [])

  return (
    <div className="app-layout">
      <nav className="app-nav">
        <NavLink to="/" className={({ isActive }) => (isActive ? 'app-nav-link active' : 'app-nav-link')}>
          메인
        </NavLink>
        <NavLink to="/assets" className={({ isActive }) => (isActive ? 'app-nav-link active' : 'app-nav-link')}>
          에셋 설정
        </NavLink>
        <NavLink to="/relationships" className={({ isActive }) => (isActive ? 'app-nav-link active' : 'app-nav-link')}>
          관계
        </NavLink>
        <NavLink to="/canvas" className={({ isActive }) => (isActive ? 'app-nav-link active' : 'app-nav-link')}>
          캔버스
        </NavLink>
      </nav>
      <main className="app-main">
        <Outlet />
        <section className="alert-recent-panel" aria-label="최근 알림">
          <h3>최근 알림</h3>
          {recentAlerts.length === 0 ? (
            <p>최근 알림이 없습니다.</p>
          ) : (
            <ul>
              {recentAlerts.slice(0, 5).map((alert, index) => (
                <li key={`${alert.assetId}-${alert.timestamp}-${index}`}>
                  [{alert.severity}] {alert.assetId} - {alert.message}
                </li>
              ))}
            </ul>
          )}
        </section>
      </main>
      <div className="alert-toast-stack" aria-live="polite">
        {toastQueue.map((item) => (
          <AlertToast key={item.id} alert={item.alert} onClose={() => dismissToast(item.id)} />
        ))}
      </div>
    </div>
  )
}
