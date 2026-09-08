import { NavLink, useNavigate } from 'react-router-dom';
import { isAuthenticated, logout } from '../services/authService';
import './Navigation.css';

function Navigation() {
  const navigate = useNavigate();
  const authenticated = isAuthenticated();

  async function handleLogout() {
    await logout();
    navigate('/login', { replace: true });
  }

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
        <NavLink
          to="/invoices"
          className={({ isActive }) => (isActive ? 'nav-link nav-link--active' : 'nav-link')}
        >
          Invoices
        </NavLink>
        {authenticated && (
          <button type="button" className="nav-link nav-link--button" onClick={handleLogout}>
            Logout
          </button>
        )}
      </nav>
    </header>
  );
}

export default Navigation;
