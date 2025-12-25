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
  type SendParentInviteRequest,
  type ParentInviteResponse,
  type ValidateInviteResponse,
  type AcceptInviteRequest,
  type AcceptJoinRequest,
  type AcceptJoinResponse,
  type PendingInvite,
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

// Flag to prevent infinite refresh loops
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (token: string) => void;
  reject: (error: unknown) => void;
}> = [];

const processQueue = (error: unknown, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token!);
    }
  });
  failedQueue = [];
};

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

// Response interceptor for error handling with token refresh
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiError>) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    // If 401 and not already retrying, attempt token refresh
    if (error.response?.status === 401 && !originalRequest._retry) {
      // Don't try to refresh if this was the refresh request itself
      if (originalRequest.url?.includes('/auth/refresh')) {
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        localStorage.removeItem('tokenExpiry');
        window.location.href = '/login';
        return Promise.reject(error);
      }

      if (isRefreshing) {
        // Queue this request to retry after refresh completes
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            if (originalRequest.headers) {
              originalRequest.headers.Authorization = `Bearer ${token}`;
            }
            return apiClient(originalRequest);
          })
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        // Attempt to refresh the token
        const response = await apiClient.post<AuthResponse>('/api/v1/auth/refresh');
        const { token, expiresAt } = response.data;

        // Store new token
        localStorage.setItem('token', token);
        localStorage.setItem('tokenExpiry', expiresAt);

        // Update user data if needed
        const user: User = {
          id: response.data.userId,
          email: response.data.email,
          firstName: response.data.firstName,
          lastName: response.data.lastName,
          role: response.data.role,
          familyId: response.data.familyId || '',
        };
        localStorage.setItem('user', JSON.stringify(user));

        processQueue(null, token);

        // Retry the original request with new token
        if (originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${token}`;
        }
        return apiClient(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError, null);
        // Refresh failed - clear auth and redirect to login
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        localStorage.removeItem('tokenExpiry');
        window.location.href = '/login';
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
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
    // Remove confirmPassword as backend doesn't expect it
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const { confirmPassword: _confirmPassword, ...backendData } = data;
    const response = await apiClient.post<AuthResponse>('/api/v1/auth/register/parent', backendData);
    return response.data;
  },

  registerAdditionalParent: async (data: { email: string; password: string; firstName: string; lastName: string }): Promise<User> => {
    const response = await apiClient.post<User>('/api/v1/auth/register/parent/additional', data);
    return response.data;
  },

  getCurrentUser: async (): Promise<User> => {
    const response = await apiClient.get<User>('/api/v1/auth/me');
    return response.data;
  },

  changePassword: async (data: { currentPassword: string; newPassword: string }): Promise<{ message: string }> => {
    const response = await apiClient.post<{ message: string }>('/api/v1/auth/change-password', data);
    return response.data;
  },

  forgotPassword: async (email: string): Promise<{ message: string }> => {
    const response = await apiClient.post<{ message: string }>('/api/v1/auth/forgot-password', { email });
    return response.data;
  },

  resetPassword: async (data: { email: string; resetToken: string; newPassword: string }): Promise<{ message: string }> => {
    const response = await apiClient.post<{ message: string }>('/api/v1/auth/reset-password', data);
    return response.data;
  },

  refreshToken: async (): Promise<AuthResponse> => {
    const response = await apiClient.post<AuthResponse>('/api/v1/auth/refresh');
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
    // Remove confirmPassword as backend doesn't expect it
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const { confirmPassword: _confirmPassword, ...backendData } = data;
    const response = await apiClient.post<Child>('/api/v1/auth/register/child', backendData);
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
    const response = await apiClient.get<Transaction[]>(`/api/v1/children/${childId}/transactions`);
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
    const response = await apiClient.get<WishListItem[]>(`/api/v1/children/${childId}/wishlist`);
    return response.data;
  },

  create: async (data: CreateWishListItemRequest): Promise<WishListItem> => {
    // Map frontend property names to backend DTO names
    const backendData = {
      name: data.itemName,
      price: data.targetAmount,
    };
    const response = await apiClient.post<WishListItem>(`/api/v1/children/${data.childId}/wishlist`, backendData);
    return response.data;
  },

  markAsPurchased: async (childId: string, id: string): Promise<void> => {
    await apiClient.post(`/api/v1/children/${childId}/wishlist/${id}/purchase`);
  },

  markAsUnpurchased: async (childId: string, id: string): Promise<void> => {
    await apiClient.post(`/api/v1/children/${childId}/wishlist/${id}/unpurchase`);
  },

  delete: async (childId: string, id: string): Promise<void> => {
    await apiClient.delete(`/api/v1/children/${childId}/wishlist/${id}`);
  },
};

// Analytics API
export const analyticsApi = {
  getBalanceHistory: async (childId: string, days: number = 30): Promise<BalancePoint[]> => {
    const response = await apiClient.get<BalancePoint[]>(`/api/v1/children/${childId}/analytics/balance-history`, {
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
      `/api/v1/children/${childId}/analytics/income-spending`,
      { params: { startDate, endDate } }
    );
    return response.data;
  },

  getSpendingTrend: async (childId: string, period: TimePeriod = TimePeriod.Week): Promise<TrendData> => {
    const response = await apiClient.get<TrendData>(`/api/v1/children/${childId}/analytics/spending-trend`, {
      params: { period },
    });
    return response.data;
  },

  getSavingsRate: async (childId: string, period: TimePeriod = TimePeriod.Month): Promise<number> => {
    const response = await apiClient.get<{ savingsRate: number }>(
      `/api/v1/children/${childId}/analytics/savings-rate`,
      { params: { period } }
    );
    return response.data.savingsRate;
  },

  getMonthlyComparison: async (childId: string, months: number = 6): Promise<MonthlyComparison[]> => {
    const response = await apiClient.get<MonthlyComparison[]>(
      `/api/v1/children/${childId}/analytics/monthly-comparison`,
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
      `/api/v1/children/${childId}/analytics/spending-breakdown`,
      { params: { startDate, endDate } }
    );
    return response.data;
  },
};

// Savings Account API
export const savingsApi = {
  getSummary: async (childId: string): Promise<SavingsAccountSummary> => {
    const response = await apiClient.get<SavingsAccountSummary>(`/api/v1/children/${childId}/savings/summary`);
    return response.data;
  },

  getBalance: async (childId: string): Promise<number> => {
    const response = await apiClient.get<number>(`/api/v1/children/${childId}/savings/balance`);
    return response.data;
  },

  getHistory: async (childId: string, limit: number = 50): Promise<SavingsTransaction[]> => {
    const response = await apiClient.get<SavingsTransaction[]>(`/api/v1/children/${childId}/savings/history`, {
      params: { limit },
    });
    return response.data;
  },

  deposit: async (data: DepositToSavingsRequest): Promise<SavingsTransaction> => {
    const response = await apiClient.post<SavingsTransaction>(`/api/v1/children/${data.childId}/savings/deposit`, {
      amount: data.amount,
      description: data.description,
    });
    return response.data;
  },

  withdraw: async (data: WithdrawFromSavingsRequest): Promise<SavingsTransaction> => {
    const response = await apiClient.post<SavingsTransaction>(`/api/v1/children/${data.childId}/savings/withdraw`, {
      amount: data.amount,
      description: data.description,
    });
    return response.data;
  },

  updateConfig: async (data: UpdateSavingsConfigRequest): Promise<void> => {
    await apiClient.put(`/api/v1/children/${data.childId}/savings/config`, data);
  },

  disable: async (childId: string): Promise<void> => {
    await apiClient.post(`/api/v1/children/${childId}/savings/disable`);
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

// Parent Invites API
export const invitesApi = {
  sendInvite: async (data: SendParentInviteRequest): Promise<ParentInviteResponse> => {
    const response = await apiClient.post<ParentInviteResponse>('/api/v1/invites/parent', data);
    return response.data;
  },

  validateToken: async (token: string, email: string): Promise<ValidateInviteResponse> => {
    const response = await apiClient.get<ValidateInviteResponse>('/api/v1/invites/validate', {
      params: { token, email },
    });
    return response.data;
  },

  acceptInvite: async (data: AcceptInviteRequest): Promise<AuthResponse> => {
    const response = await apiClient.post<AuthResponse>('/api/v1/invites/accept', data);
    return response.data;
  },

  acceptJoinRequest: async (data: AcceptJoinRequest): Promise<AcceptJoinResponse> => {
    const response = await apiClient.post<AcceptJoinResponse>('/api/v1/invites/accept-join', data);
    return response.data;
  },

  cancelInvite: async (inviteId: string): Promise<void> => {
    await apiClient.delete(`/api/v1/invites/${inviteId}`);
  },

  resendInvite: async (inviteId: string): Promise<ParentInviteResponse> => {
    const response = await apiClient.post<ParentInviteResponse>(`/api/v1/invites/${inviteId}/resend`);
    return response.data;
  },

  getPendingInvites: async (): Promise<PendingInvite[]> => {
    const response = await apiClient.get<PendingInvite[]>('/api/v1/invites');
    return response.data;
  },
};

export default apiClient;
