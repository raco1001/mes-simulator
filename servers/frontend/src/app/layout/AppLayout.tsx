import { Outlet, NavLink } from 'react-router-dom'
import './AppLayout.css'

export function AppLayout() {
  return (
    <div className="app-layout">
      <nav className="app-nav">
        <NavLink to="/" className={({ isActive }) => (isActive ? 'app-nav-link active' : 'app-nav-link')}>
          메인
        </NavLink>
        <NavLink to="/assets" className={({ isActive }) => (isActive ? 'app-nav-link active' : 'app-nav-link')}>
          에셋 설정
        </NavLink>
        <NavLink to="/relationships" className={({ isActive }) => (isActive ? 'app-nav-link active' : 'app-nav-link')}>
          관계
        </NavLink>
        <NavLink to="/canvas" className={({ isActive }) => (isActive ? 'app-nav-link active' : 'app-nav-link')}>
          캔버스
        </NavLink>
      </nav>
      <main className="app-main">
        <Outlet />
      </main>
    </div>
  )
}
