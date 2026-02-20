import type { ReactElement } from 'react'
import { render } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { AppLayout } from '@/app/layout/AppLayout'
import { AssetList } from '@/pages/home/ui/AssetList'
import { AssetsPage } from '@/pages/assets'

function AppWithRoutes() {
  return (
    <Routes>
      <Route path="/" element={<AppLayout />}>
        <Route index element={<AssetList />} />
        <Route path="assets" element={<AssetsPage />} />
      </Route>
    </Routes>
  )
}

/**
 * Renders the full app (layout + routes) inside MemoryRouter at the given route.
 */
export function renderAppAtRoute(initialRoute = '/') {
  return render(
    <MemoryRouter initialEntries={[initialRoute]}>
      <AppWithRoutes />
    </MemoryRouter>,
  )
}

/**
 * Renders a component wrapped in MemoryRouter (e.g. for components that need route context).
 */
export function renderWithRouter(ui: ReactElement, initialRoute = '/') {
  return render(
    <MemoryRouter initialEntries={[initialRoute]}>{ui}</MemoryRouter>,
  )
}
