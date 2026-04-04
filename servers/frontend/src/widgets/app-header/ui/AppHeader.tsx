import { NavLink } from 'react-router-dom'
import './AppHeader.css'

export function AppHeader() {
  return (
    <header className="app-header">
      <span className="app-header__title">Ontology Simulator</span>
      <nav className="app-header__nav" aria-label="주 메뉴">
        <NavLink
          to="/"
          end
          className={({ isActive }) =>
            isActive ? 'app-header__link active' : 'app-header__link'
          }
        >
          홈
        </NavLink>
        <NavLink
          to="/monitoring"
          className={({ isActive }) =>
            isActive ? 'app-header__link active' : 'app-header__link'
          }
        >
          모니터링
        </NavLink>
        <NavLink
          to="/recommendations"
          className={({ isActive }) =>
            isActive ? 'app-header__link active' : 'app-header__link'
          }
        >
          추천
        </NavLink>
      </nav>
    </header>
  )
}
