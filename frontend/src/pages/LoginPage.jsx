import { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { login } from '../services/authService';
import './AuthPages.css';

function LoginPage() {
  const [formData, setFormData] = useState({ username: '', password: '' });
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();

  function handleChange(event) {
    const { name, value } = event.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  }

  async function handleSubmit(event) {
    event.preventDefault();

    if (!formData.username.trim() || !formData.password) {
      setError('Username/email and password are required.');
      return;
    }

    setError('');
    setIsSubmitting(true);
    try {
      await login(formData.username.trim(), formData.password);
      const redirectTo = location.state?.from ?? '/';
      navigate(redirectTo, { replace: true });
    } catch (err) {
      setError('Invalid username/email or password.');
      console.error(err);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="auth-page">
      <form className="auth-card" onSubmit={handleSubmit}>
        <h2>Login</h2>
        <label htmlFor="login-username">Username or Email</label>
        <input
          id="login-username"
          name="username"
          type="text"
          value={formData.username}
          onChange={handleChange}
          autoComplete="username"
        />

        <label htmlFor="login-password">Password</label>
        <input
          id="login-password"
          name="password"
          type="password"
          value={formData.password}
          onChange={handleChange}
          autoComplete="current-password"
        />

        {error && <p className="form-error">{error}</p>}

        <button type="submit" className="btn btn--primary" disabled={isSubmitting}>
          {isSubmitting ? 'Logging in...' : 'Login'}
        </button>

        <p className="auth-card__footer">
          Don&apos;t have an account? <Link to="/register">Register</Link>
        </p>
      </form>
    </div>
  );
}

export default LoginPage;
