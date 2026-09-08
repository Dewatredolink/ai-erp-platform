import apiClient from './companyService';

const TOKEN_KEY = 'authToken';
const USER_KEY = 'authUser';

/**
 * Store the JWT auth token in localStorage.
 * @param {string} token
 */
export function setToken(token) {
  if (token) {
    localStorage.setItem(TOKEN_KEY, token);
  }
}

/**
 * Retrieve the JWT auth token from localStorage.
 * @returns {string|null}
 */
export function getToken() {
  return localStorage.getItem(TOKEN_KEY);
}

/**
 * Store the authenticated user's basic profile info in localStorage.
 * @param {Object} user
 */
export function setUser(user) {
  if (user) {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  }
}

/**
 * Retrieve the authenticated user's basic profile info from localStorage.
 * @returns {Object|null}
 */
export function getUser() {
  const raw = localStorage.getItem(USER_KEY);
  return raw ? JSON.parse(raw) : null;
}

/**
 * Check whether the current user is authenticated (has a stored token).
 * @returns {boolean}
 */
export function isAuthenticated() {
  return Boolean(getToken());
}

/**
 * Log in with a username/email and password. On success, stores the JWT
 * token and user profile in localStorage.
 * @param {string} username - username or email
 * @param {string} password
 * @returns {Promise<Object>} the auth response containing the token and user info
 */
export async function login(username, password) {
  const response = await apiClient.post('/auth/login', {
    usernameOrEmail: username,
    password,
  });

  const { token, userId, email, username: returnedUsername } = response.data;
  setToken(token);
  setUser({ userId, username: returnedUsername, email });
  return response.data;
}

/**
 * Register a new user account.
 * @param {string} username
 * @param {string} email
 * @param {string} password
 * @returns {Promise<Object>} the created user info
 */
export async function register(username, email, password) {
  const response = await apiClient.post('/auth/register', {
    username,
    email,
    password,
  });
  return response.data;
}

/**
 * Log out the current user, clearing local auth state.
 * @returns {Promise<void>}
 */
export async function logout() {
  try {
    await apiClient.post('/auth/logout');
  } finally {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  }
}

// Attach the stored JWT token to every outgoing request, if present.
apiClient.interceptors.request.use((config) => {
  const token = getToken();
  if (token) {
    const authScheme = 'Bearer';
    config.headers.Authorization = `${authScheme} ${token}`;
  }
  return config;
});

export default {
  login,
  register,
  logout,
  getToken,
  setToken,
  isAuthenticated,
  getUser,
  setUser,
};
