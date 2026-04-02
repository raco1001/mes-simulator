import { createBrowserRouter, Navigate } from 'react-router-dom'
import { AppLayout } from '@/app/layout/AppLayout'
import { AssetsCanvasPage } from '@/pages/canvas'
import { MonitoringPage } from '@/pages/monitoring'
import { RecommendationsPage } from '@/pages/recommendations'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <AssetsCanvasPage /> },
      { path: 'monitoring', element: <MonitoringPage /> },
      { path: 'recommendations', element: <RecommendationsPage /> },
    ],
  },
  { path: '*', element: <Navigate to="/" replace /> },
])
