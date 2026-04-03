import { useEffect } from 'react'
import type { AlertDto } from '../model/types'
import './AlertToast.css'

interface AlertToastProps {
  alert: AlertDto
  onClose: () => void
}

function getAutoCloseMs(severity: string): number {
  return severity === 'error' ? 10_000 : 5_000
}

export function AlertToast({ alert, onClose }: AlertToastProps) {
  useEffect(() => {
    const timerId = window.setTimeout(() => {
      onClose()
    }, getAutoCloseMs(alert.severity))

    return () => {
      window.clearTimeout(timerId)
    }
  }, [alert.severity, onClose])

  return (
    <article className={`alert-toast alert-toast--${alert.severity}`} role="alert" aria-live="assertive">
      <div className="alert-toast__header">
        <strong>{alert.severity.toUpperCase()}</strong>
        <button className="alert-toast__close" type="button" aria-label="알림 닫기" onClick={onClose}>
          ×
        </button>
      </div>
      <p className="alert-toast__message">{alert.message}</p>
      <p className="alert-toast__meta">asset: {alert.assetId}</p>
    </article>
  )
}
