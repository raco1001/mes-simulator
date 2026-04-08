import { Outlet, useLocation } from 'react-router-dom'
import './AppLayout.css'
import { useCallback, useEffect, useState } from 'react'
import { AlertToast } from '@/entities/alert/ui/AlertToast'
import { subscribeAlerts, type AlertDto } from '@/entities/alert'
import { AppHeader } from '@/widgets/app-header'

interface AlertToastItem {
  id: string
  alert: AlertDto
}

const MAX_RECENT_ALERTS = 20

export function AppLayout() {
  const [toastQueue, setToastQueue] = useState<AlertToastItem[]>([])
  const [recentAlerts, setRecentAlerts] = useState<AlertDto[]>([])
  const location = useLocation()

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

  const isCanvasHome = location.pathname === '/'

  return (
    <div className="app-layout">
      <AppHeader />
      <main className={`app-main ${isCanvasHome ? '' : 'app-main--padded'}`}>
        <Outlet />
        {!isCanvasHome && recentAlerts.length > 0 && (
          <section className="alert-recent-panel" aria-label="최근 알림">
            <h3>최근 알림</h3>
            <ul>
              {recentAlerts.slice(0, 5).map((alert, index) => (
                <li key={`${alert.assetId}-${alert.timestamp}-${index}`}>
                  [{alert.severity}] {alert.assetId} - {alert.message}
                </li>
              ))}
            </ul>
          </section>
        )}
      </main>
      <div className="alert-toast-stack" aria-live="polite">
        {toastQueue.map((item) => (
          <AlertToast key={item.id} alert={item.alert} onClose={() => dismissToast(item.id)} />
        ))}
      </div>
    </div>
  )
}
