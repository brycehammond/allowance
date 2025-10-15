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
  token: string;
  expiresAt: string;
  user: User;
}

// Child
export interface Child {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  weeklyAllowance: number;
  currentBalance: number;
  lastAllowanceDate: string;
  savingsAccountEnabled: boolean;
  savingsTransferType: 'Percentage' | 'FixedAmount';
  savingsTransferPercentage?: number;
  savingsTransferAmount?: number;
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
}

export interface UpdateChildSettingsRequest {
  weeklyAllowance: number;
  savingsAccountEnabled?: boolean;
  savingsTransferType?: 'Percentage' | 'FixedAmount';
  savingsTransferPercentage?: number;
  savingsTransferAmount?: number;
}

// Transaction
export const TransactionType = {
  Credit: 'Credit',
  Debit: 'Debit',
} as const;

export type TransactionType = typeof TransactionType[keyof typeof TransactionType];

// Categories (defined early since Transaction uses it)
export const TransactionCategory = {
  // Income categories
  Allowance: 'Allowance',
  Chores: 'Chores',
  Gift: 'Gift',
  Other: 'Other',

  // Expense categories
  Toys: 'Toys',
  Games: 'Games',
  Candy: 'Candy',
  Books: 'Books',
  Clothes: 'Clothes',
  Electronics: 'Electronics',
  Food: 'Food',
  Entertainment: 'Entertainment',
  Savings: 'Savings',
  Charity: 'Charity',
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
}

export interface CreateTransactionRequest {
  childId: string;
  amount: number;
  type: TransactionType;
  category: TransactionCategory;
  description: string;
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
  type: 'Deposit' | 'Withdrawal' | 'AutomaticTransfer';
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
  currentBalance: number;
  transferType: 'Percentage' | 'FixedAmount';
  transferAmount: number;
  transferPercentage: number;
  totalTransactions: number;
  totalDeposited: number;
  totalWithdrawn: number;
  lastTransactionDate?: string;
  configDescription: string;
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
