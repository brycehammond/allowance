import React, { createContext, useContext, useState, useEffect, useCallback, useRef } from 'react';
import type { ReactNode } from 'react';
import { authApi } from '../services/api';
import type { User, LoginRequest, RegisterRequest, AuthResponse } from '../types';

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: LoginRequest | AuthResponse) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => void;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

// eslint-disable-next-line react-refresh/only-export-components
export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

interface AuthProviderProps {
  children: ReactNode;
}

// Check if token will expire within the next hour
const isTokenExpiringSoon = (): boolean => {
  const expiryStr = localStorage.getItem('tokenExpiry');
  if (!expiryStr) return false;

  const expiry = new Date(expiryStr);
  const now = new Date();
  const oneHourFromNow = new Date(now.getTime() + 60 * 60 * 1000);

  return expiry <= oneHourFromNow;
};

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const refreshIntervalRef = useRef<number | null>(null);

  // Proactively refresh token if it's about to expire
  const proactiveRefresh = useCallback(async () => {
    const token = localStorage.getItem('token');
    if (!token) return;

    if (isTokenExpiringSoon()) {
      try {
        const response = await authApi.refreshToken();
        localStorage.setItem('token', response.token);
        localStorage.setItem('tokenExpiry', response.expiresAt);

        const refreshedUser: User = {
          id: response.userId,
          email: response.email,
          firstName: response.firstName,
          lastName: response.lastName,
          role: response.role,
          familyId: response.familyId || '',
        };
        localStorage.setItem('user', JSON.stringify(refreshedUser));
        setUser(refreshedUser);
      } catch {
        // Refresh failed silently - the interceptor will handle it on next API call
      }
    }
  }, []);

  // Initialize auth state from localStorage
  useEffect(() => {
    const initializeAuth = async () => {
      const token = localStorage.getItem('token');
      const storedUser = localStorage.getItem('user');

      if (token && storedUser) {
        try {
          // Check if token needs refresh first
          if (isTokenExpiringSoon()) {
            try {
              const response = await authApi.refreshToken();
              localStorage.setItem('token', response.token);
              localStorage.setItem('tokenExpiry', response.expiresAt);

              const refreshedUser: User = {
                id: response.userId,
                email: response.email,
                firstName: response.firstName,
                lastName: response.lastName,
                role: response.role,
                familyId: response.familyId || '',
              };
              localStorage.setItem('user', JSON.stringify(refreshedUser));
              setUser(refreshedUser);
              setIsLoading(false);
              return;
            } catch {
              // Refresh failed, try to get current user anyway
            }
          }

          // Verify token is still valid by fetching current user
          const currentUser = await authApi.getCurrentUser();
          setUser(currentUser);
          localStorage.setItem('user', JSON.stringify(currentUser));
        } catch {
          // Token is invalid, clear auth
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          localStorage.removeItem('tokenExpiry');
          setUser(null);
        }
      }
      setIsLoading(false);
    };

    initializeAuth();
  }, []);

  // Set up periodic token refresh check (every 30 minutes)
  useEffect(() => {
    if (user) {
      // Check immediately
      proactiveRefresh();

      // Set up interval for periodic checks
      refreshIntervalRef.current = window.setInterval(() => {
        proactiveRefresh();
      }, 30 * 60 * 1000); // 30 minutes
    }

    return () => {
      if (refreshIntervalRef.current) {
        clearInterval(refreshIntervalRef.current);
        refreshIntervalRef.current = null;
      }
    };
  }, [user, proactiveRefresh]);

  const login = async (credentials: LoginRequest | AuthResponse) => {
    // Check if this is already an AuthResponse (has token property)
    const response: AuthResponse = 'token' in credentials
      ? credentials
      : await authApi.login(credentials);

    const user: User = {
      id: response.userId,
      email: response.email,
      firstName: response.firstName,
      lastName: response.lastName,
      role: response.role,
      familyId: response.familyId || '',
    };
    localStorage.setItem('token', response.token);
    localStorage.setItem('tokenExpiry', response.expiresAt);
    localStorage.setItem('user', JSON.stringify(user));
    setUser(user);
  };

  const refreshUser = async () => {
    try {
      const currentUser = await authApi.getCurrentUser();
      setUser(currentUser);
      localStorage.setItem('user', JSON.stringify(currentUser));
    } catch {
      // Token is invalid, clear auth
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      localStorage.removeItem('tokenExpiry');
      setUser(null);
    }
  };

  const register = async (data: RegisterRequest) => {
    const response: AuthResponse = await authApi.register(data);
    const user: User = {
      id: response.userId,
      email: response.email,
      firstName: response.firstName,
      lastName: response.lastName,
      role: response.role,
      familyId: response.familyId || '',
    };
    localStorage.setItem('token', response.token);
    localStorage.setItem('tokenExpiry', response.expiresAt);
    localStorage.setItem('user', JSON.stringify(user));
    setUser(user);
  };

  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    localStorage.removeItem('tokenExpiry');
    setUser(null);
  };

  const value: AuthContextType = {
    user,
    isAuthenticated: !!user,
    isLoading,
    login,
    register,
    logout,
    refreshUser,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};
