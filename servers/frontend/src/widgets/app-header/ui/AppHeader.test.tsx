import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { AppHeader } from './AppHeader'

describe('AppHeader', () => {
  it('renders nav links and app title aligned for access', () => {
    render(
      <MemoryRouter>
        <AppHeader />
      </MemoryRouter>,
    )

    expect(screen.getByRole('link', { name: '홈' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: '모니터링' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: '추천' })).toBeInTheDocument()
    expect(screen.getByText('Ontology Simulator')).toBeInTheDocument()
  })
})
