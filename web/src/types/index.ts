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

// Rewards
export const RewardType = {
  Avatar: 'Avatar',
  Theme: 'Theme',
  Title: 'Title',
  Special: 'Special',
} as const;

export type RewardType = typeof RewardType[keyof typeof RewardType];

export interface RewardDto {
  id: string;
  name: string;
  description: string;
  type: RewardType;
  typeName: string;
  value: string;
  previewUrl: string | null;
  pointsCost: number;
  isUnlocked: boolean;
  isEquipped: boolean;
  canAfford: boolean;
}

// Tasks/Chores
export const ChoreTaskStatus = {
  Active: 'Active',
  Archived: 'Archived',
} as const;

export type ChoreTaskStatus = typeof ChoreTaskStatus[keyof typeof ChoreTaskStatus];

export const RecurrenceType = {
  Daily: 'Daily',
  Weekly: 'Weekly',
  Monthly: 'Monthly',
} as const;

export type RecurrenceType = typeof RecurrenceType[keyof typeof RecurrenceType];

export const CompletionStatus = {
  PendingApproval: 'PendingApproval',
  Approved: 'Approved',
  Rejected: 'Rejected',
} as const;

export type CompletionStatus = typeof CompletionStatus[keyof typeof CompletionStatus];

export interface ChoreTask {
  id: string;
  childId: string;
  childName: string;
  title: string;
  description: string | null;
  rewardAmount: number;
  status: ChoreTaskStatus;
  isRecurring: boolean;
  recurrenceType: RecurrenceType | null;
  recurrenceDisplay: string;
  createdAt: string;
  createdById: string;
  createdByName: string;
  totalCompletions: number;
  pendingApprovals: number;
  lastCompletedAt: string | null;
}

export interface CreateTaskRequest {
  childId: string;
  title: string;
  description?: string;
  rewardAmount: number;
  isRecurring: boolean;
  recurrenceType?: RecurrenceType;
  recurrenceDay?: DayOfWeek;
  recurrenceDayOfMonth?: number;
}

export interface UpdateTaskRequest {
  title: string;
  description?: string;
  rewardAmount: number;
  isRecurring: boolean;
  recurrenceType?: RecurrenceType;
  recurrenceDay?: DayOfWeek;
  recurrenceDayOfMonth?: number;
}

export interface TaskCompletion {
  id: string;
  taskId: string;
  taskTitle: string;
  rewardAmount: number;
  childId: string;
  childName: string;
  completedAt: string;
  notes: string | null;
  photoUrl: string | null;
  status: CompletionStatus;
  approvedById: string | null;
  approvedByName: string | null;
  approvedAt: string | null;
  rejectionReason: string | null;
  transactionId: string | null;
}

export interface TaskStatistics {
  totalTasks: number;
  activeTasks: number;
  archivedTasks: number;
  totalCompletions: number;
  pendingApprovals: number;
  totalEarned: number;
  pendingEarnings: number;
  completionRate: number;
}

export interface ReviewCompletionRequest {
  isApproved: boolean;
  rejectionReason?: string;
}

// Savings Goals
export const GoalStatus = {
  Active: 'Active',
  Completed: 'Completed',
  Purchased: 'Purchased',
  Cancelled: 'Cancelled',
  Paused: 'Paused',
} as const;

export type GoalStatus = typeof GoalStatus[keyof typeof GoalStatus];

export const GoalCategory = {
  Toy: 'Toy',
  Game: 'Game',
  Electronics: 'Electronics',
  Clothing: 'Clothing',
  Experience: 'Experience',
  Savings: 'Savings',
  Charity: 'Charity',
  Other: 'Other',
} as const;

export type GoalCategory = typeof GoalCategory[keyof typeof GoalCategory];

export const ContributionType = {
  ChildDeposit: 'ChildDeposit',
  AutoTransfer: 'AutoTransfer',
  ParentMatch: 'ParentMatch',
  ParentGift: 'ParentGift',
  ChallengeBonus: 'ChallengeBonus',
  Withdrawal: 'Withdrawal',
  ExternalGift: 'ExternalGift',
} as const;

export type ContributionType = typeof ContributionType[keyof typeof ContributionType];

export const MatchingType = {
  RatioMatch: 'RatioMatch',
  PercentageMatch: 'PercentageMatch',
  MilestoneBonus: 'MilestoneBonus',
} as const;

export type MatchingType = typeof MatchingType[keyof typeof MatchingType];

export const ChallengeStatus = {
  Active: 'Active',
  Completed: 'Completed',
  Failed: 'Failed',
  Cancelled: 'Cancelled',
} as const;

export type ChallengeStatus = typeof ChallengeStatus[keyof typeof ChallengeStatus];

export const AutoTransferType = {
  None: 'None',
  FixedAmount: 'FixedAmount',
  Percentage: 'Percentage',
} as const;

export type AutoTransferType = typeof AutoTransferType[keyof typeof AutoTransferType];

export interface SavingsGoal {
  id: string;
  childId: string;
  name: string;
  description: string | null;
  targetAmount: number;
  currentAmount: number;
  category: GoalCategory;
  categoryName: string;
  status: GoalStatus;
  statusName: string;
  imageUrl: string | null;
  autoTransferType: AutoTransferType;
  autoTransferAmount: number | null;
  autoTransferPercentage: number | null;
  priority: number;
  progressPercentage: number;
  amountRemaining: number;
  isCompleted: boolean;
  createdAt: string;
  completedAt: string | null;
  purchasedAt: string | null;
  milestones: GoalMilestone[];
  hasActiveChallenge: boolean;
  hasMatchingRule: boolean;
}

export interface GoalMilestone {
  id: string;
  percentComplete: number;
  isAchieved: boolean;
  achievedAt: string | null;
  bonusAmount: number | null;
}

export interface GoalContribution {
  id: string;
  goalId: string;
  childId: string;
  amount: number;
  type: ContributionType;
  typeName: string;
  description: string | null;
  goalBalanceAfter: number;
  parentMatchId: string | null;
  createdAt: string;
  createdById: string;
  createdByName: string;
}

export interface MatchingRule {
  id: string;
  goalId: string;
  matchType: MatchingType;
  matchTypeName: string;
  matchRatio: number;
  maxMatchAmount: number | null;
  totalMatchedAmount: number;
  isActive: boolean;
  createdAt: string;
}

export interface GoalChallenge {
  id: string;
  goalId: string;
  targetAmount: number;
  currentAmount: number;
  bonusAmount: number;
  startDate: string;
  endDate: string;
  status: ChallengeStatus;
  statusName: string;
  progressPercentage: number;
  daysRemaining: number;
  isExpired: boolean;
  completedAt: string | null;
}

export interface GoalProgressEvent {
  contribution: GoalContribution;
  goal: SavingsGoal;
  newMilestonesAchieved: GoalMilestone[];
  matchContribution: GoalContribution | null;
  challengeCompleted: boolean;
  challengeBonus: GoalContribution | null;
}

// Savings Goals Requests
export interface CreateSavingsGoalRequest {
  childId: string;
  name: string;
  description?: string;
  targetAmount: number;
  category: GoalCategory;
  imageUrl?: string;
  autoTransferType?: AutoTransferType;
  autoTransferAmount?: number;
  autoTransferPercentage?: number;
  priority?: number;
}

export interface UpdateSavingsGoalRequest {
  name?: string;
  description?: string;
  targetAmount?: number;
  category?: GoalCategory;
  imageUrl?: string;
  autoTransferType?: AutoTransferType;
  autoTransferAmount?: number;
  autoTransferPercentage?: number;
  priority?: number;
}

export interface ContributeToGoalRequest {
  amount: number;
  description?: string;
}

export interface WithdrawFromGoalRequest {
  amount: number;
  reason?: string;
}

export interface CreateMatchingRuleRequest {
  matchType: MatchingType;
  matchRatio: number;
  maxMatchAmount?: number;
}

export interface UpdateMatchingRuleRequest {
  matchRatio?: number;
  maxMatchAmount?: number;
  isActive?: boolean;
}

export interface CreateGoalChallengeRequest {
  targetAmount: number;
  endDate: string;
  bonusAmount: number;
}

export interface MarkGoalPurchasedRequest {
  purchaseNotes?: string;
}

// Gifting - Gift Links
export const GiftLinkVisibility = {
  Minimal: 'Minimal',
  WithGoals: 'WithGoals',
  /** @deprecated Wish list feature removed. Use WithGoals or Minimal instead. */
  WithWishList: 'WithWishList',
  Full: 'Full',
} as const;

export type GiftLinkVisibility = typeof GiftLinkVisibility[keyof typeof GiftLinkVisibility];

export const GiftOccasion = {
  Birthday: 'Birthday',
  Christmas: 'Christmas',
  Hanukkah: 'Hanukkah',
  Easter: 'Easter',
  Graduation: 'Graduation',
  GoodGrades: 'GoodGrades',
  Holiday: 'Holiday',
  JustBecause: 'JustBecause',
  Reward: 'Reward',
  Other: 'Other',
} as const;

export type GiftOccasion = typeof GiftOccasion[keyof typeof GiftOccasion];

export const GiftStatus = {
  Pending: 'Pending',
  Approved: 'Approved',
  Rejected: 'Rejected',
  Expired: 'Expired',
} as const;

export type GiftStatus = typeof GiftStatus[keyof typeof GiftStatus];

export interface GiftLink {
  id: string;
  childId: string;
  childFirstName: string;
  token: string;
  name: string;
  description: string | null;
  visibility: GiftLinkVisibility;
  isActive: boolean;
  expiresAt: string | null;
  maxUses: number | null;
  currentUses: number;
  minAmount: number | null;
  maxAmount: number | null;
  defaultOccasion: GiftOccasion | null;
  createdAt: string;
  updatedAt: string;
  portalUrl: string;
}

export interface CreateGiftLinkRequest {
  childId: string;
  name: string;
  description?: string;
  visibility?: GiftLinkVisibility;
  expiresAt?: string;
  maxUses?: number;
  minAmount?: number;
  maxAmount?: number;
  defaultOccasion?: GiftOccasion;
}

export interface UpdateGiftLinkRequest {
  name?: string;
  description?: string;
  visibility?: GiftLinkVisibility;
  expiresAt?: string;
  maxUses?: number;
  minAmount?: number;
  maxAmount?: number;
  defaultOccasion?: GiftOccasion;
}

export interface GiftLinkStats {
  linkId: string;
  totalGifts: number;
  pendingGifts: number;
  approvedGifts: number;
  rejectedGifts: number;
  totalAmountReceived: number;
  lastGiftAt: string | null;
}

// Gifting - Gifts
export interface Gift {
  id: string;
  childId: string;
  childFirstName: string;
  giverName: string;
  giverEmail: string | null;
  giverRelationship: string | null;
  amount: number;
  occasion: GiftOccasion;
  customOccasion: string | null;
  message: string | null;
  status: GiftStatus;
  rejectionReason: string | null;
  processedById: string | null;
  processedAt: string | null;
  allocatedToGoalId: string | null;
  allocatedToGoalName: string | null;
  savingsPercentage: number | null;
  createdAt: string;
  hasThankYouNote: boolean;
}

export interface SubmitGiftRequest {
  giverName: string;
  giverEmail?: string;
  giverRelationship?: string;
  amount: number;
  occasion: GiftOccasion;
  customOccasion?: string;
  message?: string;
}

export interface GiftSubmissionResult {
  giftId: string;
  childFirstName: string;
  amount: number;
  confirmationMessage: string;
}

export interface ApproveGiftRequest {
  allocateToGoalId?: string;
  savingsPercentage?: number;
}

export interface RejectGiftRequest {
  reason?: string;
}

// Gifting - Gift Portal (Public)
export interface PortalSavingsGoal {
  id: string;
  name: string;
  description: string | null;
  targetAmount: number;
  currentAmount: number;
  progressPercentage: number;
}

export interface GiftPortalData {
  childFirstName: string;
  childPhotoUrl: string | null;
  minAmount: number | null;
  maxAmount: number | null;
  defaultOccasion: GiftOccasion | null;
  visibility: GiftLinkVisibility;
  savingsGoals: PortalSavingsGoal[];
}

// Gifting - Thank You Notes
export interface ThankYouNote {
  id: string;
  giftId: string;
  childId: string;
  childFirstName: string;
  giverName: string;
  message: string;
  imageUrl: string | null;
  isSent: boolean;
  sentAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface PendingThankYou {
  giftId: string;
  giverName: string;
  giverRelationship: string | null;
  amount: number;
  occasion: GiftOccasion;
  customOccasion: string | null;
  receivedAt: string;
  daysSinceReceived: number;
  hasNote: boolean;
}

export interface CreateThankYouNoteRequest {
  message: string;
  imageUrl?: string;
}

export interface UpdateThankYouNoteRequest {
  message?: string;
  imageUrl?: string;
}
