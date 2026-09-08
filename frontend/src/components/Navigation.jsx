import { NavLink } from 'react-router-dom';
import './Navigation.css';

function Navigation() {
  return (
    <header className="app-header">
      <div className="app-header__title">AI ERP Platform</div>
      <nav className="app-header__nav">
        <NavLink
          to="/"
          end
          className={({ isActive }) => (isActive ? 'nav-link nav-link--active' : 'nav-link')}
        >
          Companies
        </NavLink>
      </nav>
    </header>
  );
}

export default Navigation;
