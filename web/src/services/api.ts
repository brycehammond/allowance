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
  type BadgeCategory,
  type BadgeDto,
  type ChildBadgeDto,
  type BadgeProgressDto,
  type ChildPointsDto,
  type AchievementSummaryDto,
  type UpdateBadgeDisplayRequest,
  type MarkBadgesSeenRequest,
  type RewardType,
  type RewardDto,
  type ChoreTask,
  type ChoreTaskStatus,
  type CreateTaskRequest,
  type UpdateTaskRequest,
  type TaskCompletion,
  type CompletionStatus,
  type TaskStatistics,
  type ReviewCompletionRequest,
  type SavingsGoal,
  type GoalStatus,
  type GoalContribution,
  type ContributionType,
  type MatchingRule,
  type GoalChallenge,
  type GoalProgressEvent,
  type CreateSavingsGoalRequest,
  type UpdateSavingsGoalRequest,
  type ContributeToGoalRequest,
  type WithdrawFromGoalRequest,
  type CreateMatchingRuleRequest,
  type UpdateMatchingRuleRequest,
  type CreateGoalChallengeRequest,
  type MarkGoalPurchasedRequest,
  type Notification,
  type NotificationListResponse,
  type NotificationPreferences,
  type NotificationType,
  type MarkNotificationsReadRequest,
  type UpdateNotificationPreferencesRequest,
  type UpdateQuietHoursRequest,
  type DeviceTokenResponse,
  type RegisterDeviceRequest,
  type GiftLink,
  type CreateGiftLinkRequest,
  type UpdateGiftLinkRequest,
  type GiftLinkStats,
  type Gift,
  type SubmitGiftRequest,
  type GiftSubmissionResult,
  type ApproveGiftRequest,
  type RejectGiftRequest,
  type GiftPortalData,
  type ThankYouNote,
  type PendingThankYou,
  type CreateThankYouNoteRequest,
  type UpdateThankYouNoteRequest,
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

  deleteAccount: async (): Promise<{ message: string }> => {
    const response = await apiClient.delete<{ message: string }>('/api/v1/auth/account');
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

// Badges API
export const badgesApi = {
  getAll: async (category?: BadgeCategory, includeSecret: boolean = false): Promise<BadgeDto[]> => {
    const response = await apiClient.get<BadgeDto[]>('/api/v1/badges', {
      params: { category, includeSecret },
    });
    return response.data;
  },

  getChildBadges: async (
    childId: string,
    category?: BadgeCategory,
    newOnly: boolean = false
  ): Promise<ChildBadgeDto[]> => {
    const response = await apiClient.get<ChildBadgeDto[]>(`/api/v1/children/${childId}/badges`, {
      params: { category, newOnly },
    });
    return response.data;
  },

  getBadgeProgress: async (childId: string): Promise<BadgeProgressDto[]> => {
    const response = await apiClient.get<BadgeProgressDto[]>(`/api/v1/children/${childId}/badges/progress`);
    return response.data;
  },

  getAchievementSummary: async (childId: string): Promise<AchievementSummaryDto> => {
    const response = await apiClient.get<AchievementSummaryDto>(`/api/v1/children/${childId}/badges/summary`);
    return response.data;
  },

  toggleBadgeDisplay: async (
    childId: string,
    badgeId: string,
    data: UpdateBadgeDisplayRequest
  ): Promise<ChildBadgeDto> => {
    const response = await apiClient.patch<ChildBadgeDto>(
      `/api/v1/children/${childId}/badges/${badgeId}/display`,
      data
    );
    return response.data;
  },

  markBadgesSeen: async (childId: string, data: MarkBadgesSeenRequest): Promise<void> => {
    await apiClient.post(`/api/v1/children/${childId}/badges/seen`, data);
  },

  getChildPoints: async (childId: string): Promise<ChildPointsDto> => {
    const response = await apiClient.get<ChildPointsDto>(`/api/v1/children/${childId}/points`);
    return response.data;
  },
};

// Rewards API
export const rewardsApi = {
  getAvailable: async (type?: RewardType, childId?: string): Promise<RewardDto[]> => {
    const response = await apiClient.get<RewardDto[]>('/api/v1/rewards', {
      params: { type, childId },
    });
    return response.data;
  },

  getChildRewards: async (childId: string): Promise<RewardDto[]> => {
    const response = await apiClient.get<RewardDto[]>(`/api/v1/children/${childId}/rewards`);
    return response.data;
  },

  unlock: async (childId: string, rewardId: string): Promise<RewardDto> => {
    const response = await apiClient.post<RewardDto>(
      `/api/v1/children/${childId}/rewards/${rewardId}/unlock`
    );
    return response.data;
  },

  equip: async (childId: string, rewardId: string): Promise<RewardDto> => {
    const response = await apiClient.post<RewardDto>(
      `/api/v1/children/${childId}/rewards/${rewardId}/equip`
    );
    return response.data;
  },

  unequip: async (childId: string, rewardId: string): Promise<void> => {
    await apiClient.post(`/api/v1/children/${childId}/rewards/${rewardId}/unequip`);
  },
};

// Tasks/Chores API
export const tasksApi = {
  getAll: async (childId?: string, status?: ChoreTaskStatus, isRecurring?: boolean): Promise<ChoreTask[]> => {
    const response = await apiClient.get<ChoreTask[]>('/api/v1/tasks', {
      params: { childId, status, isRecurring },
    });
    return response.data;
  },

  getById: async (taskId: string): Promise<ChoreTask> => {
    const response = await apiClient.get<ChoreTask>(`/api/v1/tasks/${taskId}`);
    return response.data;
  },

  create: async (data: CreateTaskRequest): Promise<ChoreTask> => {
    const response = await apiClient.post<ChoreTask>('/api/v1/tasks', data);
    return response.data;
  },

  update: async (taskId: string, data: UpdateTaskRequest): Promise<ChoreTask> => {
    const response = await apiClient.put<ChoreTask>(`/api/v1/tasks/${taskId}`, data);
    return response.data;
  },

  archive: async (taskId: string): Promise<void> => {
    await apiClient.delete(`/api/v1/tasks/${taskId}`);
  },

  complete: async (taskId: string, notes?: string, photo?: File): Promise<TaskCompletion> => {
    const formData = new FormData();
    if (notes) {
      formData.append('notes', notes);
    }
    if (photo) {
      formData.append('photo', photo);
    }
    const response = await apiClient.post<TaskCompletion>(
      `/api/v1/tasks/${taskId}/complete`,
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      }
    );
    return response.data;
  },

  getCompletions: async (
    taskId: string,
    status?: CompletionStatus,
    startDate?: string,
    endDate?: string
  ): Promise<TaskCompletion[]> => {
    const response = await apiClient.get<TaskCompletion[]>(`/api/v1/tasks/${taskId}/completions`, {
      params: { status, startDate, endDate },
    });
    return response.data;
  },

  getPendingApprovals: async (): Promise<TaskCompletion[]> => {
    const response = await apiClient.get<TaskCompletion[]>('/api/v1/tasks/completions/pending');
    return response.data;
  },

  reviewCompletion: async (completionId: string, data: ReviewCompletionRequest): Promise<TaskCompletion> => {
    const response = await apiClient.put<TaskCompletion>(
      `/api/v1/tasks/completions/${completionId}/review`,
      data
    );
    return response.data;
  },

  getStatistics: async (childId: string): Promise<TaskStatistics> => {
    const response = await apiClient.get<TaskStatistics>(`/api/v1/children/${childId}/tasks/statistics`);
    return response.data;
  },
};

// Savings Goals API
export const savingsGoalsApi = {
  // Goal CRUD
  getByChild: async (
    childId: string,
    status?: GoalStatus,
    includeCompleted: boolean = false
  ): Promise<SavingsGoal[]> => {
    const response = await apiClient.get<SavingsGoal[]>(`/api/v1/children/${childId}/savings-goals`, {
      params: { status, includeCompleted },
    });
    return response.data;
  },

  getById: async (goalId: string): Promise<SavingsGoal> => {
    const response = await apiClient.get<SavingsGoal>(`/api/v1/savings-goals/${goalId}`);
    return response.data;
  },

  create: async (data: CreateSavingsGoalRequest): Promise<SavingsGoal> => {
    const response = await apiClient.post<SavingsGoal>('/api/v1/savings-goals', data);
    return response.data;
  },

  update: async (goalId: string, data: UpdateSavingsGoalRequest): Promise<SavingsGoal> => {
    const response = await apiClient.put<SavingsGoal>(`/api/v1/savings-goals/${goalId}`, data);
    return response.data;
  },

  delete: async (goalId: string): Promise<void> => {
    await apiClient.delete(`/api/v1/savings-goals/${goalId}`);
  },

  pause: async (goalId: string): Promise<SavingsGoal> => {
    const response = await apiClient.post<SavingsGoal>(`/api/v1/savings-goals/${goalId}/pause`);
    return response.data;
  },

  resume: async (goalId: string): Promise<SavingsGoal> => {
    const response = await apiClient.post<SavingsGoal>(`/api/v1/savings-goals/${goalId}/resume`);
    return response.data;
  },

  // Contributions
  contribute: async (goalId: string, data: ContributeToGoalRequest): Promise<GoalProgressEvent> => {
    const response = await apiClient.post<GoalProgressEvent>(`/api/v1/savings-goals/${goalId}/contribute`, data);
    return response.data;
  },

  withdraw: async (goalId: string, data: WithdrawFromGoalRequest): Promise<GoalContribution> => {
    const response = await apiClient.post<GoalContribution>(`/api/v1/savings-goals/${goalId}/withdraw`, data);
    return response.data;
  },

  getContributions: async (
    goalId: string,
    type?: ContributionType,
    startDate?: string,
    endDate?: string
  ): Promise<GoalContribution[]> => {
    const response = await apiClient.get<GoalContribution[]>(`/api/v1/savings-goals/${goalId}/contributions`, {
      params: { type, startDate, endDate },
    });
    return response.data;
  },

  markAsPurchased: async (goalId: string, data?: MarkGoalPurchasedRequest): Promise<SavingsGoal> => {
    const response = await apiClient.post<SavingsGoal>(`/api/v1/savings-goals/${goalId}/purchase`, data || {});
    return response.data;
  },

  // Matching Rules
  createMatchingRule: async (goalId: string, data: CreateMatchingRuleRequest): Promise<MatchingRule> => {
    const response = await apiClient.post<MatchingRule>(`/api/v1/savings-goals/${goalId}/matching`, data);
    return response.data;
  },

  getMatchingRule: async (goalId: string): Promise<MatchingRule | null> => {
    try {
      const response = await apiClient.get<MatchingRule>(`/api/v1/savings-goals/${goalId}/matching`);
      return response.data;
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 404) {
        return null;
      }
      throw err;
    }
  },

  updateMatchingRule: async (goalId: string, data: UpdateMatchingRuleRequest): Promise<MatchingRule> => {
    const response = await apiClient.put<MatchingRule>(`/api/v1/savings-goals/${goalId}/matching`, data);
    return response.data;
  },

  deleteMatchingRule: async (goalId: string): Promise<void> => {
    await apiClient.delete(`/api/v1/savings-goals/${goalId}/matching`);
  },

  // Challenges
  createChallenge: async (goalId: string, data: CreateGoalChallengeRequest): Promise<GoalChallenge> => {
    const response = await apiClient.post<GoalChallenge>(`/api/v1/savings-goals/${goalId}/challenge`, data);
    return response.data;
  },

  getChallenge: async (goalId: string): Promise<GoalChallenge | null> => {
    try {
      const response = await apiClient.get<GoalChallenge>(`/api/v1/savings-goals/${goalId}/challenge`);
      return response.data;
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 404) {
        return null;
      }
      throw err;
    }
  },

  cancelChallenge: async (goalId: string): Promise<void> => {
    await apiClient.delete(`/api/v1/savings-goals/${goalId}/challenge`);
  },

  getChildChallenges: async (childId: string): Promise<GoalChallenge[]> => {
    const response = await apiClient.get<GoalChallenge[]>(`/api/v1/children/${childId}/challenges`);
    return response.data;
  },
};

// Notifications API
export const notificationsApi = {
  getNotifications: async (
    page: number = 1,
    pageSize: number = 20,
    unreadOnly: boolean = false,
    type?: NotificationType
  ): Promise<NotificationListResponse> => {
    const response = await apiClient.get<NotificationListResponse>('/api/v1/notifications', {
      params: { page, pageSize, unreadOnly, type },
    });
    return response.data;
  },

  getUnreadCount: async (): Promise<{ count: number }> => {
    const response = await apiClient.get<{ count: number }>('/api/v1/notifications/unread-count');
    return response.data;
  },

  getById: async (id: string): Promise<Notification> => {
    const response = await apiClient.get<Notification>(`/api/v1/notifications/${id}`);
    return response.data;
  },

  markAsRead: async (id: string): Promise<Notification> => {
    const response = await apiClient.patch<Notification>(`/api/v1/notifications/${id}/read`);
    return response.data;
  },

  markMultipleAsRead: async (data: MarkNotificationsReadRequest): Promise<{ markedCount: number }> => {
    const response = await apiClient.post<{ markedCount: number }>('/api/v1/notifications/read', data);
    return response.data;
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/v1/notifications/${id}`);
  },

  deleteAllRead: async (): Promise<{ deletedCount: number }> => {
    const response = await apiClient.delete<{ deletedCount: number }>('/api/v1/notifications');
    return response.data;
  },

  getPreferences: async (): Promise<NotificationPreferences> => {
    const response = await apiClient.get<NotificationPreferences>('/api/v1/notifications/preferences');
    return response.data;
  },

  updatePreferences: async (data: UpdateNotificationPreferencesRequest): Promise<NotificationPreferences> => {
    const response = await apiClient.put<NotificationPreferences>('/api/v1/notifications/preferences', data);
    return response.data;
  },

  updateQuietHours: async (data: UpdateQuietHoursRequest): Promise<NotificationPreferences> => {
    const response = await apiClient.put<NotificationPreferences>('/api/v1/notifications/preferences/quiet-hours', data);
    return response.data;
  },

  registerDevice: async (data: RegisterDeviceRequest): Promise<DeviceTokenResponse> => {
    const response = await apiClient.post<DeviceTokenResponse>('/api/v1/devices', data);
    return response.data;
  },

  getDevices: async (): Promise<DeviceTokenResponse[]> => {
    const response = await apiClient.get<DeviceTokenResponse[]>('/api/v1/devices');
    return response.data;
  },

  unregisterDevice: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/v1/devices/${id}`);
  },
};

// Gift Links API (Parent only)
export const giftLinksApi = {
  getAll: async (): Promise<GiftLink[]> => {
    const response = await apiClient.get<GiftLink[]>('/api/v1/gift-links');
    return response.data;
  },

  getById: async (id: string): Promise<GiftLink> => {
    const response = await apiClient.get<GiftLink>(`/api/v1/gift-links/${id}`);
    return response.data;
  },

  create: async (data: CreateGiftLinkRequest): Promise<GiftLink> => {
    const response = await apiClient.post<GiftLink>('/api/v1/gift-links', data);
    return response.data;
  },

  update: async (id: string, data: UpdateGiftLinkRequest): Promise<GiftLink> => {
    const response = await apiClient.put<GiftLink>(`/api/v1/gift-links/${id}`, data);
    return response.data;
  },

  deactivate: async (id: string): Promise<void> => {
    await apiClient.post(`/api/v1/gift-links/${id}/deactivate`);
  },

  regenerateToken: async (id: string): Promise<GiftLink> => {
    const response = await apiClient.post<GiftLink>(`/api/v1/gift-links/${id}/regenerate-token`);
    return response.data;
  },

  getStats: async (id: string): Promise<GiftLinkStats> => {
    const response = await apiClient.get<GiftLinkStats>(`/api/v1/gift-links/${id}/stats`);
    return response.data;
  },
};

// Gifts API
export const giftsApi = {
  // Public portal endpoints (no auth required)
  getPortalData: async (token: string): Promise<GiftPortalData> => {
    const response = await apiClient.get<GiftPortalData>(`/api/v1/gifts/portal/${token}`);
    return response.data;
  },

  submitGift: async (token: string, data: SubmitGiftRequest): Promise<GiftSubmissionResult> => {
    const response = await apiClient.post<GiftSubmissionResult>(`/api/v1/gifts/portal/${token}/submit`, data);
    return response.data;
  },

  // Authenticated endpoints
  getPendingGifts: async (): Promise<Gift[]> => {
    const response = await apiClient.get<Gift[]>('/api/v1/gifts/pending');
    return response.data;
  },

  getById: async (id: string): Promise<Gift> => {
    const response = await apiClient.get<Gift>(`/api/v1/gifts/${id}`);
    return response.data;
  },

  getByChild: async (childId: string): Promise<Gift[]> => {
    const response = await apiClient.get<Gift[]>(`/api/v1/gifts/child/${childId}`);
    return response.data;
  },

  approve: async (id: string, data: ApproveGiftRequest): Promise<Gift> => {
    const response = await apiClient.post<Gift>(`/api/v1/gifts/${id}/approve`, data);
    return response.data;
  },

  reject: async (id: string, data: RejectGiftRequest): Promise<Gift> => {
    const response = await apiClient.post<Gift>(`/api/v1/gifts/${id}/reject`, data);
    return response.data;
  },
};

// Thank You Notes API
export const thankYouNotesApi = {
  getPending: async (): Promise<PendingThankYou[]> => {
    const response = await apiClient.get<PendingThankYou[]>('/api/v1/gifts/thank-you/pending');
    return response.data;
  },

  getByGiftId: async (giftId: string): Promise<ThankYouNote> => {
    const response = await apiClient.get<ThankYouNote>(`/api/v1/gifts/${giftId}/thank-you`);
    return response.data;
  },

  create: async (giftId: string, data: CreateThankYouNoteRequest): Promise<ThankYouNote> => {
    const response = await apiClient.post<ThankYouNote>(`/api/v1/gifts/${giftId}/thank-you`, data);
    return response.data;
  },

  update: async (giftId: string, data: UpdateThankYouNoteRequest): Promise<ThankYouNote> => {
    const response = await apiClient.put<ThankYouNote>(`/api/v1/gifts/${giftId}/thank-you`, data);
    return response.data;
  },

  send: async (giftId: string): Promise<ThankYouNote> => {
    const response = await apiClient.post<ThankYouNote>(`/api/v1/gifts/${giftId}/thank-you/send`);
    return response.data;
  },
};

export default apiClient;
