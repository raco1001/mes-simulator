import { createBrowserRouter, Navigate } from 'react-router-dom'
import { AppLayout } from '@/app/layout/AppLayout'
import { CanvasPage } from '@/pages/canvas'
import { MonitoringPage } from '@/pages/monitoring'
import { RecommendationsPage } from '@/pages/recommendations'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <CanvasPage /> },
      { path: 'monitoring', element: <MonitoringPage /> },
      { path: 'recommendations', element: <RecommendationsPage /> },
    ],
  },
  { path: '*', element: <Navigate to="/" replace /> },
])
