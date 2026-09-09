import { Navigate, useLocation } from 'react-router-dom';
import { hasPermission, isAuthenticated } from '../services/authService';

function ProtectedRoute({ children, requiredPermission }) {
  const location = useLocation();

  if (!isAuthenticated()) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  if (requiredPermission && !hasPermission(requiredPermission)) {
    return <Navigate to="/" replace />;
  }

  return children;
}

export default ProtectedRoute;
