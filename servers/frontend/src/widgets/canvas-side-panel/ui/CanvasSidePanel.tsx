import './CanvasSidePanel.css'

export function CanvasSidePanel({
  className,
  children,
}: {
  className?: string
  children: React.ReactNode
}) {
  return (
    <div
      className={['assets-canvas-page__side-panel', className].filter(Boolean).join(' ')}
    >
      {children}
    </div>
  )
}
