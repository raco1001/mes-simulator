import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { CanvasSidePanel } from './CanvasSidePanel'

describe('CanvasSidePanel', () => {
  it('renders children inside the side panel shell', () => {
    render(
      <CanvasSidePanel>
        <p>Panel body</p>
      </CanvasSidePanel>,
    )
    const panel = document.querySelector('.assets-canvas-page__side-panel')
    expect(panel).toBeInTheDocument()
    expect(screen.getByText('Panel body')).toBeInTheDocument()
  })

  it('merges optional className onto the shell', () => {
    render(
      <CanvasSidePanel className="assets-canvas-page__object-type-panel">
        <span>inner</span>
      </CanvasSidePanel>,
    )
    const panel = document.querySelector('.assets-canvas-page__side-panel')
    expect(panel?.classList.contains('assets-canvas-page__object-type-panel')).toBe(true)
  })
})
