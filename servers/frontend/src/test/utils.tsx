import type { ReactElement } from 'react'
import { render } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { AppLayout } from '@/app/layout/AppLayout'
import { CanvasPage } from '@/pages/canvas'
import { MonitoringPage } from '@/pages/monitoring'
import { RecommendationsPage } from '@/pages/recommendations'

function AppWithRoutes() {
  return (
    <Routes>
      <Route path="/" element={<AppLayout />}>
        <Route index element={<CanvasPage />} />
        <Route path="monitoring" element={<MonitoringPage />} />
        <Route path="recommendations" element={<RecommendationsPage />} />
      </Route>
    </Routes>
  )
}

export function renderAppAtRoute(initialRoute = '/') {
  return render(
    <MemoryRouter initialEntries={[initialRoute]}>
      <AppWithRoutes />
    </MemoryRouter>,
  )
}

export function renderWithRouter(ui: ReactElement, initialRoute = '/') {
  return render(
    <MemoryRouter initialEntries={[initialRoute]}>{ui}</MemoryRouter>,
  )
}
