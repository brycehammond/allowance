import axios, { AxiosError } from 'axios';
import type { InternalAxiosRequestConfig } from 'axios';
import {
  TimePeriod,
  type User,
  type LoginRequest,
  type RegisterRequest,
  type AuthResponse,
  type Child,
  type CreateChildRequest,
  type UpdateChildSettingsRequest,
  type Transaction,
  type CreateTransactionRequest,
  type WishListItem,
  type CreateWishListItemRequest,
  type ApiError,
  type BalancePoint,
  type IncomeSpendingSummary,
  type TrendData,
  type MonthlyComparison,
  type CategoryBreakdown,
  type SavingsAccountSummary,
  type SavingsTransaction,
  type DepositToSavingsRequest,
  type WithdrawFromSavingsRequest,
  type UpdateSavingsConfigRequest,
  type CategoryInfo,
} from '../types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'https://localhost:7071';

// Create axios instance
const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true,
});

// Request interceptor to add auth token
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem('token');
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor for error handling
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ApiError>) => {
    if (error.response?.status === 401) {
      // Token expired or invalid - clear auth and redirect to login
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

// Auth API
export const authApi = {
  login: async (data: LoginRequest): Promise<AuthResponse> => {
    const response = await apiClient.post<AuthResponse>('/api/v1/auth/login', data);
    return response.data;
  },

  register: async (data: RegisterRequest): Promise<AuthResponse> => {
    const response = await apiClient.post<AuthResponse>('/api/v1/auth/register', data);
    return response.data;
  },

  getCurrentUser: async (): Promise<User> => {
    const response = await apiClient.get<User>('/api/v1/auth/me');
    return response.data;
  },
};

// Children API
export const childrenApi = {
  getAll: async (): Promise<Child[]> => {
    const response = await apiClient.get<Child[]>('/api/v1/children');
    return response.data;
  },

  getById: async (id: string): Promise<Child> => {
    const response = await apiClient.get<Child>(`/api/v1/children/${id}`);
    return response.data;
  },

  create: async (data: CreateChildRequest): Promise<Child> => {
    const response = await apiClient.post<Child>('/api/v1/children', data);
    return response.data;
  },

  updateSettings: async (id: string, data: UpdateChildSettingsRequest): Promise<Child> => {
    const response = await apiClient.put<Child>(`/api/v1/children/${id}/settings`, data);
    return response.data;
  },

  getBalance: async (id: string): Promise<{ balance: number }> => {
    const response = await apiClient.get<{ balance: number }>(`/api/v1/children/${id}/balance`);
    return response.data;
  },
};

// Transactions API
export const transactionsApi = {
  getByChild: async (childId: string): Promise<Transaction[]> => {
    const response = await apiClient.get<Transaction[]>('/api/v1/transactions', {
      params: { childId },
    });
    return response.data;
  },

  create: async (data: CreateTransactionRequest): Promise<Transaction> => {
    const response = await apiClient.post<Transaction>('/api/v1/transactions', data);
    return response.data;
  },

  getById: async (id: string): Promise<Transaction> => {
    const response = await apiClient.get<Transaction>(`/api/v1/transactions/${id}`);
    return response.data;
  },
};

// Wish List API
export const wishListApi = {
  getByChild: async (childId: string): Promise<WishListItem[]> => {
    const response = await apiClient.get<WishListItem[]>('/api/v1/wishlist', {
      params: { childId },
    });
    return response.data;
  },

  create: async (data: CreateWishListItemRequest): Promise<WishListItem> => {
    const response = await apiClient.post<WishListItem>('/api/v1/wishlist', data);
    return response.data;
  },

  markAsPurchased: async (id: string): Promise<void> => {
    await apiClient.post(`/api/v1/wishlist/${id}/purchase`);
  },

  markAsUnpurchased: async (id: string): Promise<void> => {
    await apiClient.post(`/api/v1/wishlist/${id}/unpurchase`);
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/v1/wishlist/${id}`);
  },
};

// Analytics API
export const analyticsApi = {
  getBalanceHistory: async (childId: string, days: number = 30): Promise<BalancePoint[]> => {
    const response = await apiClient.get<BalancePoint[]>(`/api/v1/analytics/children/${childId}/balance-history`, {
      params: { days },
    });
    return response.data;
  },

  getIncomeVsSpending: async (
    childId: string,
    startDate?: string,
    endDate?: string
  ): Promise<IncomeSpendingSummary> => {
    const response = await apiClient.get<IncomeSpendingSummary>(
      `/api/v1/analytics/children/${childId}/income-spending`,
      { params: { startDate, endDate } }
    );
    return response.data;
  },

  getSpendingTrend: async (childId: string, period: TimePeriod = TimePeriod.Week): Promise<TrendData> => {
    const response = await apiClient.get<TrendData>(`/api/v1/analytics/children/${childId}/spending-trend`, {
      params: { period },
    });
    return response.data;
  },

  getSavingsRate: async (childId: string, period: TimePeriod = TimePeriod.Month): Promise<number> => {
    const response = await apiClient.get<{ savingsRate: number }>(
      `/api/v1/analytics/children/${childId}/savings-rate`,
      { params: { period } }
    );
    return response.data.savingsRate;
  },

  getMonthlyComparison: async (childId: string, months: number = 6): Promise<MonthlyComparison[]> => {
    const response = await apiClient.get<MonthlyComparison[]>(
      `/api/v1/analytics/children/${childId}/monthly-comparison`,
      { params: { months } }
    );
    return response.data;
  },

  getSpendingBreakdown: async (
    childId: string,
    startDate?: string,
    endDate?: string
  ): Promise<CategoryBreakdown[]> => {
    const response = await apiClient.get<CategoryBreakdown[]>(
      `/api/v1/analytics/children/${childId}/spending-breakdown`,
      { params: { startDate, endDate } }
    );
    return response.data;
  },
};

// Savings Account API
export const savingsApi = {
  getSummary: async (childId: string): Promise<SavingsAccountSummary> => {
    const response = await apiClient.get<SavingsAccountSummary>(`/api/v1/savings/${childId}/summary`);
    return response.data;
  },

  getBalance: async (childId: string): Promise<number> => {
    const response = await apiClient.get<number>(`/api/v1/savings/${childId}/balance`);
    return response.data;
  },

  getHistory: async (childId: string, limit: number = 50): Promise<SavingsTransaction[]> => {
    const response = await apiClient.get<SavingsTransaction[]>(`/api/v1/savings/${childId}/history`, {
      params: { limit },
    });
    return response.data;
  },

  deposit: async (data: DepositToSavingsRequest): Promise<SavingsTransaction> => {
    const response = await apiClient.post<SavingsTransaction>('/api/v1/savings/deposit', data);
    return response.data;
  },

  withdraw: async (data: WithdrawFromSavingsRequest): Promise<SavingsTransaction> => {
    const response = await apiClient.post<SavingsTransaction>('/api/v1/savings/withdraw', data);
    return response.data;
  },

  updateConfig: async (data: UpdateSavingsConfigRequest): Promise<void> => {
    await apiClient.put('/api/v1/savings/config', data);
  },

  disable: async (childId: string): Promise<void> => {
    await apiClient.post(`/api/v1/savings/${childId}/disable`);
  },
};

// Categories API
export const categoriesApi = {
  getAll: async (): Promise<CategoryInfo[]> => {
    const response = await apiClient.get<CategoryInfo[]>('/api/v1/categories/all');
    return response.data;
  },

  getByType: async (type: 'Credit' | 'Debit'): Promise<CategoryInfo[]> => {
    const response = await apiClient.get<CategoryInfo[]>('/api/v1/categories', {
      params: { type },
    });
    return response.data;
  },
};

export default apiClient;
