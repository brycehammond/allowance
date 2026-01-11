// User and Authentication
export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: 'Parent' | 'Child';
  familyId: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
  familyName: string;
}

export interface AuthResponse {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: 'Parent' | 'Child';
  familyId: string | null;
  familyName: string | null;
  token: string;
  expiresAt: string;
}

// Child
export type DayOfWeek = 'Sunday' | 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday' | 'Saturday';

export interface Child {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  weeklyAllowance: number;
  currentBalance: number;
  savingsBalance: number;
  lastAllowanceDate: string;
  allowanceDay?: DayOfWeek | null;
  savingsAccountEnabled: boolean;
  savingsTransferType: 'Percentage' | 'FixedAmount';
  savingsTransferPercentage?: number;
  savingsTransferAmount?: number;
  savingsBalanceVisibleToChild: boolean;
  allowDebt: boolean;
}

export interface CreateChildRequest {
  email: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
  weeklyAllowance: number;
  savingsAccountEnabled?: boolean;
  savingsTransferType?: 'Percentage' | 'FixedAmount';
  savingsTransferPercentage?: number;
  savingsTransferAmount?: number;
  initialBalance?: number;
  initialSavingsBalance?: number;
}

export interface UpdateChildSettingsRequest {
  weeklyAllowance: number;
  allowanceDay?: DayOfWeek | null;
  savingsAccountEnabled?: boolean;
  savingsTransferType?: 'Percentage' | 'FixedAmount';
  savingsTransferPercentage?: number;
  savingsTransferAmount?: number;
  savingsBalanceVisibleToChild?: boolean;
  allowDebt?: boolean;
}

// Transaction
export const TransactionType = {
  Credit: 'Credit',
  Debit: 'Debit',
} as const;

export type TransactionType = typeof TransactionType[keyof typeof TransactionType];

// Categories (defined early since Transaction uses it)
export const TransactionCategory = {
  // Income categories (1-9)
  Allowance: 'Allowance',
  Chores: 'Chores',
  Gift: 'Gift',
  BonusReward: 'BonusReward',
  Task: 'Task',
  OtherIncome: 'OtherIncome',

  // Spending categories (10-29)
  Toys: 'Toys',
  Games: 'Games',
  Books: 'Books',
  Clothes: 'Clothes',
  Snacks: 'Snacks',
  Candy: 'Candy',
  Electronics: 'Electronics',
  Entertainment: 'Entertainment',
  Sports: 'Sports',
  Crafts: 'Crafts',
  OtherSpending: 'OtherSpending',

  // Savings & Giving categories (30-39)
  Savings: 'Savings',
  Charity: 'Charity',
  Investment: 'Investment',
} as const;

export type TransactionCategory = typeof TransactionCategory[keyof typeof TransactionCategory];

export interface Transaction {
  id: string;
  childId: string;
  amount: number;
  type: TransactionType;
  category: TransactionCategory;
  description: string;
  balanceAfter: number;
  createdAt: string;
  createdBy: string;
  createdByName: string;
  notes?: string;
}

export interface CreateTransactionRequest {
  childId: string;
  amount: number;
  type: TransactionType;
  category: TransactionCategory;
  description: string;
  notes?: string;
  drawFromSavings?: boolean;
}

// Wish List
export interface WishListItem {
  id: string;
  childId: string;
  name: string;
  price: number;
  url?: string;
  notes?: string;
  isPurchased: boolean;
  purchasedAt?: string;
  createdAt: string;
  canAfford: boolean;
}

export interface CreateWishListItemRequest {
  childId: string;
  itemName: string;
  targetAmount: number;
}

// API Error
export interface ApiError {
  message: string;
  errors?: Record<string, string[]>;
}

// Analytics
export interface BalancePoint {
  date: string;
  balance: number;
  transactionDescription?: string;
}

export interface IncomeSpendingSummary {
  totalIncome: number;
  totalSpending: number;
  netSavings: number;
  incomeTransactionCount: number;
  spendingTransactionCount: number;
  savingsRate: number;
}

export interface DataPoint {
  date: string;
  value: number;
}

export const TrendDirection = {
  Up: 'Up',
  Down: 'Down',
  Stable: 'Stable',
} as const;

export type TrendDirection = typeof TrendDirection[keyof typeof TrendDirection];

export interface TrendData {
  points: DataPoint[];
  direction: TrendDirection;
  changePercent: number;
  description: string;
}

export interface MonthlyComparison {
  year: number;
  month: number;
  monthName: string;
  income: number;
  spending: number;
  netSavings: number;
  endingBalance: number;
}

export interface CategoryBreakdown {
  category: string;
  amount: number;
  transactionCount: number;
  percentage: number;
}

export const TimePeriod = {
  Week: 'Week',
  Month: 'Month',
  Year: 'Year',
} as const;

export type TimePeriod = typeof TimePeriod[keyof typeof TimePeriod];

// Savings Account
export interface SavingsTransaction {
  id: string;
  childId: string;
  type: 'Deposit' | 'Withdrawal' | 'AutoTransfer';
  amount: number;
  description: string;
  balanceAfter: number;
  createdAt: string;
  createdById: string;
  createdByName: string;
}

export interface SavingsAccountSummary {
  childId: string;
  isEnabled: boolean;
  currentBalance: number | null;
  transferType: 'Percentage' | 'FixedAmount';
  transferAmount: number;
  transferPercentage: number;
  totalTransactions: number | null;
  totalDeposited: number | null;
  totalWithdrawn: number | null;
  lastTransactionDate?: string | null;
  configDescription: string;
  balanceHidden?: boolean;
}

export interface DepositToSavingsRequest {
  childId: string;
  amount: number;
  description: string;
}

export interface WithdrawFromSavingsRequest {
  childId: string;
  amount: number;
  description: string;
}

export interface UpdateSavingsConfigRequest {
  childId: string;
  transferType: 'Percentage' | 'FixedAmount';
  amount: number;
}

// CategoryInfo (TransactionCategory is defined earlier in the file)
export interface CategoryInfo {
  category: TransactionCategory;
  displayName: string;
  transactionType: 'Credit' | 'Debit';
}

// Parent Invites
export interface SendParentInviteRequest {
  email: string;
  firstName: string;
  lastName: string;
}

export interface ParentInviteResponse {
  inviteId: string;
  email: string;
  firstName: string;
  lastName: string;
  isExistingUser: boolean;
  expiresAt: string;
  message: string;
}

export interface ValidateInviteResponse {
  isValid: boolean;
  isExistingUser: boolean;
  firstName: string | null;
  lastName: string | null;
  familyName: string | null;
  inviterName: string | null;
  errorMessage: string | null;
}

export interface AcceptInviteRequest {
  token: string;
  email: string;
  password: string;
}

export interface AcceptJoinRequest {
  token: string;
}

export interface AcceptJoinResponse {
  familyId: string;
  familyName: string;
  message: string;
}

export interface PendingInvite {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isExistingUser: boolean;
  status: 'Pending' | 'Accepted' | 'Expired' | 'Cancelled';
  expiresAt: string;
  createdAt: string;
}

// Achievement Badges
export const BadgeCategory = {
  Saving: 'Saving',
  Spending: 'Spending',
  Goals: 'Goals',
  Chores: 'Chores',
  Streaks: 'Streaks',
  Milestones: 'Milestones',
  Special: 'Special',
} as const;

export type BadgeCategory = typeof BadgeCategory[keyof typeof BadgeCategory];

export const BadgeRarity = {
  Common: 'Common',
  Uncommon: 'Uncommon',
  Rare: 'Rare',
  Epic: 'Epic',
  Legendary: 'Legendary',
} as const;

export type BadgeRarity = typeof BadgeRarity[keyof typeof BadgeRarity];

export interface BadgeDto {
  id: string;
  code: string;
  name: string;
  description: string;
  iconUrl: string;
  category: BadgeCategory;
  categoryName: string;
  rarity: BadgeRarity;
  rarityName: string;
  pointsValue: number;
  isSecret: boolean;
  isEarned: boolean;
  earnedAt: string | null;
  isDisplayed: boolean;
  currentProgress: number | null;
  targetProgress: number | null;
  progressPercentage: number | null;
}

export interface ChildBadgeDto {
  id: string;
  badgeId: string;
  badgeName: string;
  badgeDescription: string;
  iconUrl: string;
  category: BadgeCategory;
  categoryName: string;
  rarity: BadgeRarity;
  rarityName: string;
  pointsValue: number;
  earnedAt: string;
  isDisplayed: boolean;
  isNew: boolean;
  earnedContext: string | null;
}

export interface BadgeProgressDto {
  badgeId: string;
  badgeName: string;
  description: string;
  iconUrl: string;
  category: BadgeCategory;
  categoryName: string;
  rarity: BadgeRarity;
  rarityName: string;
  pointsValue: number;
  currentProgress: number;
  targetProgress: number;
  progressPercentage: number;
  progressText: string;
}

export interface ChildPointsDto {
  totalPoints: number;
  availablePoints: number;
  spentPoints: number;
  badgesEarned: number;
  rewardsUnlocked: number;
}

export interface AchievementSummaryDto {
  totalBadges: number;
  earnedBadges: number;
  totalPoints: number;
  availablePoints: number;
  recentBadges: ChildBadgeDto[];
  inProgressBadges: BadgeProgressDto[];
  badgesByCategory: Record<string, number>;
}

export interface UpdateBadgeDisplayRequest {
  isDisplayed: boolean;
}

export interface MarkBadgesSeenRequest {
  badgeIds: string[];
}
