import { createBrowserRouter, Navigate } from 'react-router-dom'
import { AppLayout } from '@/app/layout/AppLayout'
import { AssetList } from '@/pages/home/ui/AssetList'
import { AssetsPage } from '@/pages/assets'
import { RelationshipsPage } from '@/pages/relationships'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <AssetList /> },
      { path: 'assets', element: <AssetsPage /> },
      { path: 'relationships', element: <RelationshipsPage /> },
    ],
  },
  // 추후 로그인 추가 시 예: { path: 'login', element: <LoginPage /> }
  { path: '*', element: <Navigate to="/" replace /> },
])
