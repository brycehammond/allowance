import React, { useEffect, useState, useCallback } from 'react';
import { useAuth } from '../../contexts/AuthContext';
import { savingsGoalsApi } from '../../services/api';
import type {
  SavingsGoal,
  CreateSavingsGoalRequest,
  ContributeToGoalRequest,
  GoalCategory,
  AutoTransferType,
  GoalChallenge,
  MatchingRule,
  CreateMatchingRuleRequest,
  CreateGoalChallengeRequest,
} from '../../types';
import { GoalCategory as GoalCategoryEnum, MatchingType } from '../../types';

interface SavingsGoalsTabProps {
  childId: string;
  currentBalance: number;
  onBalanceChange?: () => void;
}

export const SavingsGoalsTab: React.FC<SavingsGoalsTabProps> = ({
  childId,
  currentBalance,
  onBalanceChange,
}) => {
  const { user } = useAuth();
  const [goals, setGoals] = useState<SavingsGoal[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [showAddForm, setShowAddForm] = useState(false);
  const [showContributeModal, setShowContributeModal] = useState<string | null>(null);
  const [showMatchingModal, setShowMatchingModal] = useState<string | null>(null);
  const [showChallengeModal, setShowChallengeModal] = useState<string | null>(null);
  const [selectedGoalDetails, setSelectedGoalDetails] = useState<{
    goal: SavingsGoal;
    matching: MatchingRule | null;
    challenge: GoalChallenge | null;
  } | null>(null);
  const [includeCompleted, setIncludeCompleted] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const isParent = user?.role === 'Parent';

  const [formData, setFormData] = useState<CreateSavingsGoalRequest>({
    childId,
    name: '',
    targetAmount: 0,
    category: 'Toy' as GoalCategory,
  });

  const [contributeData, setContributeData] = useState<ContributeToGoalRequest>({
    amount: 0,
    description: '',
  });

  const [matchingData, setMatchingData] = useState<CreateMatchingRuleRequest>({
    matchType: 'RatioMatch',
    matchRatio: 0.5,
  });

  const [challengeData, setChallengeData] = useState<CreateGoalChallengeRequest>({
    targetAmount: 0,
    endDate: '',
    bonusAmount: 0,
  });

  const loadGoals = useCallback(async () => {
    try {
      setIsLoading(true);
      const data = await savingsGoalsApi.getByChild(childId, undefined, includeCompleted);
      setGoals(data);
    } catch (err: unknown) {
      const errorMessage =
        err instanceof Error && 'response' in err
          ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
          : undefined;
      setError(errorMessage || 'Failed to load savings goals');
    } finally {
      setIsLoading(false);
    }
  }, [childId, includeCompleted]);

  useEffect(() => {
    loadGoals();
  }, [loadGoals]);

  const handleCreateGoal = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (formData.targetAmount <= 0) {
      setError('Target amount must be greater than 0');
      return;
    }

    setIsSubmitting(true);
    try {
      await savingsGoalsApi.create(formData);
      setShowAddForm(false);
      setFormData({
        childId,
        name: '',
        targetAmount: 0,
        category: 'Toy' as GoalCategory,
      });
      await loadGoals();
    } catch (err: unknown) {
      const errorMessage =
        err instanceof Error && 'response' in err
          ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
          : undefined;
      setError(errorMessage || 'Failed to create savings goal');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleContribute = async (goalId: string) => {
    if (contributeData.amount <= 0) {
      setError('Amount must be greater than 0');
      return;
    }

    if (contributeData.amount > currentBalance) {
      setError('Insufficient balance');
      return;
    }

    setIsSubmitting(true);
    try {
      const result = await savingsGoalsApi.contribute(goalId, contributeData);
      setShowContributeModal(null);
      setContributeData({ amount: 0, description: '' });
      await loadGoals();
      onBalanceChange?.();

      // Show celebration for milestones
      if (result.newMilestonesAchieved.length > 0) {
        const milestone = result.newMilestonesAchieved[result.newMilestonesAchieved.length - 1];
        alert(`Milestone achieved! ${milestone.percentComplete}% complete!`);
      }
    } catch (err: unknown) {
      const errorMessage =
        err instanceof Error && 'response' in err
          ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
          : undefined;
      setError(errorMessage || 'Failed to contribute to goal');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCreateMatching = async (goalId: string) => {
    setIsSubmitting(true);
    try {
      await savingsGoalsApi.createMatchingRule(goalId, matchingData);
      setShowMatchingModal(null);
      setMatchingData({ matchType: 'RatioMatch', matchRatio: 0.5 });
      await loadGoals();
    } catch (err: unknown) {
      const errorMessage =
        err instanceof Error && 'response' in err
          ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
          : undefined;
      setError(errorMessage || 'Failed to create matching rule');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCreateChallenge = async (goalId: string) => {
    if (challengeData.targetAmount <= 0 || challengeData.bonusAmount <= 0) {
      setError('Target and bonus amounts must be greater than 0');
      return;
    }

    setIsSubmitting(true);
    try {
      await savingsGoalsApi.createChallenge(goalId, challengeData);
      setShowChallengeModal(null);
      setChallengeData({ targetAmount: 0, endDate: '', bonusAmount: 0 });
      await loadGoals();
    } catch (err: unknown) {
      const errorMessage =
        err instanceof Error && 'response' in err
          ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
          : undefined;
      setError(errorMessage || 'Failed to create challenge');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handlePauseResume = async (goal: SavingsGoal) => {
    try {
      if (goal.status === 'Active') {
        await savingsGoalsApi.pause(goal.id);
      } else if (goal.status === 'Paused') {
        await savingsGoalsApi.resume(goal.id);
      }
      await loadGoals();
    } catch (err: unknown) {
      const errorMessage =
        err instanceof Error && 'response' in err
          ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
          : undefined;
      setError(errorMessage || 'Failed to update goal status');
    }
  };

  const handleMarkPurchased = async (goalId: string) => {
    if (!confirm('Mark this goal as purchased? This will move the saved amount to completed.')) {
      return;
    }

    try {
      await savingsGoalsApi.markAsPurchased(goalId);
      await loadGoals();
    } catch (err: unknown) {
      const errorMessage =
        err instanceof Error && 'response' in err
          ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
          : undefined;
      setError(errorMessage || 'Failed to mark as purchased');
    }
  };

  const handleDelete = async (goalId: string) => {
    if (!confirm('Are you sure you want to cancel this goal? Saved funds will be returned to the balance.')) {
      return;
    }

    try {
      await savingsGoalsApi.delete(goalId);
      await loadGoals();
      onBalanceChange?.();
    } catch (err: unknown) {
      const errorMessage =
        err instanceof Error && 'response' in err
          ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
          : undefined;
      setError(errorMessage || 'Failed to delete goal');
    }
  };

  const loadGoalDetails = async (goal: SavingsGoal) => {
    try {
      const [matching, challenge] = await Promise.all([
        savingsGoalsApi.getMatchingRule(goal.id),
        savingsGoalsApi.getChallenge(goal.id),
      ]);
      setSelectedGoalDetails({ goal, matching, challenge });
    } catch {
      setSelectedGoalDetails({ goal, matching: null, challenge: null });
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  const getCategoryIcon = (category: GoalCategory) => {
    const icons: Record<GoalCategory, string> = {
      Toy: '🧸',
      Game: '🎮',
      Electronics: '📱',
      Clothing: '👕',
      Experience: '🎢',
      Savings: '🏦',
      Charity: '💝',
      Other: '🎯',
    };
    return icons[category] || '🎯';
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Active':
        return 'bg-blue-100 text-blue-800';
      case 'Paused':
        return 'bg-yellow-100 text-yellow-800';
      case 'Completed':
        return 'bg-green-100 text-green-800';
      case 'Purchased':
        return 'bg-purple-100 text-purple-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  if (isLoading) {
    return (
      <div className="flex justify-center items-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-500"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-4">
        <div>
          <h3 className="text-lg font-medium text-gray-900">Savings Goals</h3>
          <p className="text-sm text-gray-500">
            Available to save: {formatCurrency(currentBalance)}
          </p>
        </div>
        <div className="flex items-center gap-4">
          <label className="flex items-center text-sm text-gray-600">
            <input
              type="checkbox"
              checked={includeCompleted}
              onChange={(e) => setIncludeCompleted(e.target.checked)}
              className="mr-2 rounded border-gray-300 text-primary-600 focus:ring-primary-500"
            />
            Show completed
          </label>
          <button
            onClick={() => setShowAddForm(!showAddForm)}
            className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500"
          >
            {showAddForm ? (
              <>
                <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
                Cancel
              </>
            ) : (
              <>
                <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
                </svg>
                New Goal
              </>
            )}
          </button>
        </div>
      </div>

      {error && (
        <div className="rounded-md bg-red-50 p-4">
          <div className="text-sm text-red-800">{error}</div>
          <button onClick={() => setError('')} className="text-sm text-red-600 underline mt-1">
            Dismiss
          </button>
        </div>
      )}

      {/* Add Goal Form */}
      {showAddForm && (
        <div className="bg-white border border-gray-200 rounded-lg p-6">
          <h4 className="text-md font-medium text-gray-900 mb-4">New Savings Goal</h4>
          <form onSubmit={handleCreateGoal} className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label htmlFor="name" className="block text-sm font-medium text-gray-700">
                  Goal Name
                </label>
                <input
                  type="text"
                  id="name"
                  required
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                  placeholder="e.g., New bicycle"
                />
              </div>

              <div>
                <label htmlFor="category" className="block text-sm font-medium text-gray-700">
                  Category
                </label>
                <select
                  id="category"
                  value={formData.category}
                  onChange={(e) => setFormData({ ...formData, category: e.target.value as GoalCategory })}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                >
                  {Object.entries(GoalCategoryEnum).map(([key, value]) => (
                    <option key={key} value={value}>
                      {getCategoryIcon(value)} {key}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label htmlFor="targetAmount" className="block text-sm font-medium text-gray-700">
                  Target Amount
                </label>
                <div className="mt-1 relative rounded-md shadow-sm">
                  <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                    <span className="text-gray-500 sm:text-sm">$</span>
                  </div>
                  <input
                    type="number"
                    id="targetAmount"
                    step="0.01"
                    min="0.01"
                    required
                    value={formData.targetAmount || ''}
                    onChange={(e) => setFormData({ ...formData, targetAmount: parseFloat(e.target.value) || 0 })}
                    className="block w-full pl-7 pr-12 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                  />
                </div>
              </div>

              <div>
                <label htmlFor="autoTransfer" className="block text-sm font-medium text-gray-700">
                  Auto-transfer from Allowance
                </label>
                <select
                  id="autoTransfer"
                  value={formData.autoTransferType || 'None'}
                  onChange={(e) => setFormData({ ...formData, autoTransferType: e.target.value as AutoTransferType })}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                >
                  <option value="None">None</option>
                  <option value="FixedAmount">Fixed Amount</option>
                  <option value="Percentage">Percentage</option>
                </select>
              </div>

              {formData.autoTransferType === 'FixedAmount' && (
                <div>
                  <label htmlFor="autoTransferAmount" className="block text-sm font-medium text-gray-700">
                    Auto-transfer Amount
                  </label>
                  <div className="mt-1 relative rounded-md shadow-sm">
                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                      <span className="text-gray-500 sm:text-sm">$</span>
                    </div>
                    <input
                      type="number"
                      id="autoTransferAmount"
                      step="0.01"
                      min="0.01"
                      value={formData.autoTransferAmount || ''}
                      onChange={(e) =>
                        setFormData({ ...formData, autoTransferAmount: parseFloat(e.target.value) || 0 })
                      }
                      className="block w-full pl-7 pr-12 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                    />
                  </div>
                </div>
              )}

              {formData.autoTransferType === 'Percentage' && (
                <div>
                  <label htmlFor="autoTransferPercentage" className="block text-sm font-medium text-gray-700">
                    Auto-transfer Percentage
                  </label>
                  <div className="mt-1 relative rounded-md shadow-sm">
                    <input
                      type="number"
                      id="autoTransferPercentage"
                      step="1"
                      min="1"
                      max="100"
                      value={formData.autoTransferPercentage || ''}
                      onChange={(e) =>
                        setFormData({ ...formData, autoTransferPercentage: parseFloat(e.target.value) || 0 })
                      }
                      className="block w-full pr-8 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                    />
                    <div className="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none">
                      <span className="text-gray-500 sm:text-sm">%</span>
                    </div>
                  </div>
                </div>
              )}
            </div>

            <div>
              <label htmlFor="description" className="block text-sm font-medium text-gray-700">
                Description (optional)
              </label>
              <textarea
                id="description"
                rows={2}
                value={formData.description || ''}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                placeholder="What are you saving for?"
              />
            </div>

            <div className="flex justify-end space-x-3">
              <button
                type="button"
                onClick={() => setShowAddForm(false)}
                className="px-4 py-2 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isSubmitting}
                className="px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50"
              >
                {isSubmitting ? 'Creating...' : 'Create Goal'}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Goals List */}
      {goals.length === 0 ? (
        <div className="text-center py-12 bg-white rounded-lg border border-gray-200">
          <svg className="mx-auto h-12 w-12 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
          <h3 className="mt-2 text-sm font-medium text-gray-900">No savings goals yet</h3>
          <p className="mt-1 text-sm text-gray-500">Start saving toward something special!</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
          {goals.map((goal) => (
            <div
              key={goal.id}
              className={`bg-white rounded-lg shadow-sm border-2 overflow-hidden cursor-pointer hover:shadow-md transition-shadow ${
                goal.isCompleted ? 'border-green-200' : goal.status === 'Paused' ? 'border-yellow-200' : 'border-gray-200'
              }`}
              onClick={() => loadGoalDetails(goal)}
            >
              <div className="p-6">
                <div className="flex items-start justify-between mb-4">
                  <div className="flex items-center">
                    <span className="text-2xl mr-3">{getCategoryIcon(goal.category)}</span>
                    <div>
                      <h4 className="text-lg font-medium text-gray-900">{goal.name}</h4>
                      <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${getStatusColor(goal.status)}`}>
                        {goal.statusName}
                      </span>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    {goal.hasMatchingRule && (
                      <span className="text-xs bg-purple-100 text-purple-800 px-2 py-1 rounded" title="Parent matching active">
                        💰 Match
                      </span>
                    )}
                    {goal.hasActiveChallenge && (
                      <span className="text-xs bg-orange-100 text-orange-800 px-2 py-1 rounded" title="Challenge active">
                        🏆 Challenge
                      </span>
                    )}
                  </div>
                </div>

                {/* Progress Bar */}
                <div className="mb-4">
                  <div className="flex justify-between text-sm mb-1">
                    <span className="text-gray-600">Progress</span>
                    <span className="font-medium text-gray-900">{goal.progressPercentage}%</span>
                  </div>
                  <div className="w-full bg-gray-200 rounded-full h-3 overflow-hidden">
                    <div
                      className={`h-3 rounded-full transition-all duration-500 ${
                        goal.isCompleted ? 'bg-green-500' : 'bg-primary-500'
                      }`}
                      style={{ width: `${Math.min(goal.progressPercentage, 100)}%` }}
                    />
                  </div>
                  {/* Milestones */}
                  <div className="flex justify-between mt-2">
                    {goal.milestones.map((milestone) => (
                      <div
                        key={milestone.id}
                        className={`text-xs ${milestone.isAchieved ? 'text-green-600' : 'text-gray-400'}`}
                        title={milestone.isAchieved ? `Achieved!` : `${milestone.percentComplete}%`}
                      >
                        {milestone.isAchieved ? '✓' : '○'} {milestone.percentComplete}%
                      </div>
                    ))}
                  </div>
                </div>

                {/* Amount Info */}
                <div className="flex justify-between items-center mb-4 p-3 bg-gray-50 rounded-lg">
                  <div>
                    <p className="text-sm text-gray-600">Saved</p>
                    <p className="text-xl font-bold text-primary-600">{formatCurrency(goal.currentAmount)}</p>
                  </div>
                  <div className="text-right">
                    <p className="text-sm text-gray-600">Goal</p>
                    <p className="text-xl font-bold text-gray-900">{formatCurrency(goal.targetAmount)}</p>
                  </div>
                </div>

                {goal.amountRemaining > 0 && (
                  <p className="text-sm text-gray-500 mb-4 text-center">
                    {formatCurrency(goal.amountRemaining)} to go!
                  </p>
                )}

                {/* Actions */}
                <div className="flex flex-wrap gap-2" onClick={(e) => e.stopPropagation()}>
                  {goal.status === 'Active' && (
                    <button
                      onClick={() => {
                        setShowContributeModal(goal.id);
                        setContributeData({ amount: 0, description: '' });
                      }}
                      className="flex-1 px-3 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700"
                    >
                      Add Money
                    </button>
                  )}

                  {goal.isCompleted && isParent && goal.status !== 'Purchased' && (
                    <button
                      onClick={() => handleMarkPurchased(goal.id)}
                      className="flex-1 px-3 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-green-600 hover:bg-green-700"
                    >
                      Mark Purchased
                    </button>
                  )}

                  {isParent && goal.status !== 'Purchased' && goal.status !== 'Completed' && (
                    <>
                      <button
                        onClick={() => handlePauseResume(goal)}
                        className="px-3 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
                      >
                        {goal.status === 'Paused' ? 'Resume' : 'Pause'}
                      </button>

                      {!goal.hasMatchingRule && (
                        <button
                          onClick={() => {
                            setShowMatchingModal(goal.id);
                            setMatchingData({ matchType: 'RatioMatch', matchRatio: 0.5 });
                          }}
                          className="px-3 py-2 border border-purple-300 text-sm font-medium rounded-md text-purple-700 bg-white hover:bg-purple-50"
                          title="Add parent matching"
                        >
                          💰 Match
                        </button>
                      )}

                      {!goal.hasActiveChallenge && (
                        <button
                          onClick={() => {
                            setShowChallengeModal(goal.id);
                            const nextWeek = new Date();
                            nextWeek.setDate(nextWeek.getDate() + 7);
                            setChallengeData({
                              targetAmount: Math.min(goal.amountRemaining, goal.targetAmount * 0.25),
                              endDate: nextWeek.toISOString().split('T')[0],
                              bonusAmount: 5,
                            });
                          }}
                          className="px-3 py-2 border border-orange-300 text-sm font-medium rounded-md text-orange-700 bg-white hover:bg-orange-50"
                          title="Create challenge"
                        >
                          🏆 Challenge
                        </button>
                      )}

                      <button
                        onClick={() => handleDelete(goal.id)}
                        className="px-3 py-2 border border-red-300 text-sm font-medium rounded-md text-red-700 bg-white hover:bg-red-50"
                        title="Cancel goal"
                      >
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={2}
                            d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                          />
                        </svg>
                      </button>
                    </>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Contribute Modal */}
      {showContributeModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-lg p-6 max-w-md w-full">
            <h3 className="text-lg font-medium text-gray-900 mb-4">Add Money to Goal</h3>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700">Amount</label>
                <div className="mt-1 relative rounded-md shadow-sm">
                  <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                    <span className="text-gray-500 sm:text-sm">$</span>
                  </div>
                  <input
                    type="number"
                    step="0.01"
                    min="0.01"
                    max={currentBalance}
                    value={contributeData.amount || ''}
                    onChange={(e) => setContributeData({ ...contributeData, amount: parseFloat(e.target.value) || 0 })}
                    className="block w-full pl-7 pr-12 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                  />
                </div>
                <p className="mt-1 text-xs text-gray-500">Available: {formatCurrency(currentBalance)}</p>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700">Note (optional)</label>
                <input
                  type="text"
                  value={contributeData.description || ''}
                  onChange={(e) => setContributeData({ ...contributeData, description: e.target.value })}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                  placeholder="e.g., Birthday money"
                />
              </div>

              <div className="flex justify-end space-x-3">
                <button
                  onClick={() => setShowContributeModal(null)}
                  className="px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
                >
                  Cancel
                </button>
                <button
                  onClick={() => handleContribute(showContributeModal)}
                  disabled={isSubmitting || contributeData.amount <= 0}
                  className="px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 disabled:opacity-50"
                >
                  {isSubmitting ? 'Adding...' : 'Add Money'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Matching Rule Modal */}
      {showMatchingModal && isParent && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-lg p-6 max-w-md w-full">
            <h3 className="text-lg font-medium text-gray-900 mb-4">Set Up Parent Matching</h3>
            <p className="text-sm text-gray-600 mb-4">
              Match your child's contributions to encourage saving!
            </p>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700">Match Type</label>
                <select
                  value={matchingData.matchType}
                  onChange={(e) => setMatchingData({ ...matchingData, matchType: e.target.value as typeof MatchingType[keyof typeof MatchingType] })}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                >
                  <option value="RatioMatch">Ratio Match (e.g., $1 for every $2)</option>
                  <option value="PercentageMatch">Percentage Match (e.g., 50%)</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700">
                  {matchingData.matchType === 'RatioMatch' ? 'Match Ratio' : 'Match Percentage'}
                </label>
                <div className="mt-1 relative rounded-md shadow-sm">
                  <input
                    type="number"
                    step="0.01"
                    min="0.01"
                    max={matchingData.matchType === 'PercentageMatch' ? 100 : 10}
                    value={matchingData.matchRatio || ''}
                    onChange={(e) => setMatchingData({ ...matchingData, matchRatio: parseFloat(e.target.value) || 0 })}
                    className="block w-full pr-16 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                  />
                  <div className="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none">
                    <span className="text-gray-500 sm:text-sm">
                      {matchingData.matchType === 'PercentageMatch' ? '%' : ':1'}
                    </span>
                  </div>
                </div>
                <p className="mt-1 text-xs text-gray-500">
                  {matchingData.matchType === 'RatioMatch'
                    ? `You'll add $${matchingData.matchRatio.toFixed(2)} for every $1 saved`
                    : `You'll match ${(matchingData.matchRatio * 100).toFixed(0)}% of each contribution`}
                </p>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700">Max Match Amount (optional)</label>
                <div className="mt-1 relative rounded-md shadow-sm">
                  <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                    <span className="text-gray-500 sm:text-sm">$</span>
                  </div>
                  <input
                    type="number"
                    step="1"
                    min="0"
                    value={matchingData.maxMatchAmount || ''}
                    onChange={(e) =>
                      setMatchingData({ ...matchingData, maxMatchAmount: parseFloat(e.target.value) || undefined })
                    }
                    className="block w-full pl-7 pr-12 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                    placeholder="No limit"
                  />
                </div>
              </div>

              <div className="flex justify-end space-x-3">
                <button
                  onClick={() => setShowMatchingModal(null)}
                  className="px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
                >
                  Cancel
                </button>
                <button
                  onClick={() => handleCreateMatching(showMatchingModal)}
                  disabled={isSubmitting || matchingData.matchRatio <= 0}
                  className="px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-purple-600 hover:bg-purple-700 disabled:opacity-50"
                >
                  {isSubmitting ? 'Creating...' : 'Create Matching Rule'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Challenge Modal */}
      {showChallengeModal && isParent && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-lg p-6 max-w-md w-full">
            <h3 className="text-lg font-medium text-gray-900 mb-4">Create Savings Challenge</h3>
            <p className="text-sm text-gray-600 mb-4">
              Set a target with a deadline and bonus reward!
            </p>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700">Target Amount to Save</label>
                <div className="mt-1 relative rounded-md shadow-sm">
                  <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                    <span className="text-gray-500 sm:text-sm">$</span>
                  </div>
                  <input
                    type="number"
                    step="0.01"
                    min="0.01"
                    value={challengeData.targetAmount || ''}
                    onChange={(e) =>
                      setChallengeData({ ...challengeData, targetAmount: parseFloat(e.target.value) || 0 })
                    }
                    className="block w-full pl-7 pr-12 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700">Deadline</label>
                <input
                  type="date"
                  value={challengeData.endDate}
                  min={new Date().toISOString().split('T')[0]}
                  onChange={(e) => setChallengeData({ ...challengeData, endDate: e.target.value })}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700">Bonus Reward</label>
                <div className="mt-1 relative rounded-md shadow-sm">
                  <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                    <span className="text-gray-500 sm:text-sm">$</span>
                  </div>
                  <input
                    type="number"
                    step="0.01"
                    min="0.01"
                    value={challengeData.bonusAmount || ''}
                    onChange={(e) =>
                      setChallengeData({ ...challengeData, bonusAmount: parseFloat(e.target.value) || 0 })
                    }
                    className="block w-full pl-7 pr-12 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                  />
                </div>
                <p className="mt-1 text-xs text-gray-500">
                  This bonus will be added to the goal when the challenge is completed on time!
                </p>
              </div>

              <div className="flex justify-end space-x-3">
                <button
                  onClick={() => setShowChallengeModal(null)}
                  className="px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
                >
                  Cancel
                </button>
                <button
                  onClick={() => handleCreateChallenge(showChallengeModal)}
                  disabled={isSubmitting || challengeData.targetAmount <= 0 || !challengeData.endDate}
                  className="px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-orange-600 hover:bg-orange-700 disabled:opacity-50"
                >
                  {isSubmitting ? 'Creating...' : 'Create Challenge'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Goal Details Modal */}
      {selectedGoalDetails && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-lg p-6 max-w-lg w-full max-h-[90vh] overflow-y-auto">
            <div className="flex justify-between items-start mb-4">
              <div>
                <h3 className="text-lg font-medium text-gray-900">
                  {getCategoryIcon(selectedGoalDetails.goal.category)} {selectedGoalDetails.goal.name}
                </h3>
                <span
                  className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${getStatusColor(
                    selectedGoalDetails.goal.status
                  )}`}
                >
                  {selectedGoalDetails.goal.statusName}
                </span>
              </div>
              <button
                onClick={() => setSelectedGoalDetails(null)}
                className="text-gray-400 hover:text-gray-600"
              >
                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            {selectedGoalDetails.goal.description && (
              <p className="text-sm text-gray-600 mb-4">{selectedGoalDetails.goal.description}</p>
            )}

            <div className="space-y-4">
              {/* Progress */}
              <div className="p-4 bg-gray-50 rounded-lg">
                <div className="flex justify-between mb-2">
                  <span className="text-2xl font-bold text-primary-600">
                    {formatCurrency(selectedGoalDetails.goal.currentAmount)}
                  </span>
                  <span className="text-2xl font-bold text-gray-400">
                    / {formatCurrency(selectedGoalDetails.goal.targetAmount)}
                  </span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-4">
                  <div
                    className={`h-4 rounded-full ${
                      selectedGoalDetails.goal.isCompleted ? 'bg-green-500' : 'bg-primary-500'
                    }`}
                    style={{ width: `${Math.min(selectedGoalDetails.goal.progressPercentage, 100)}%` }}
                  />
                </div>
                <p className="text-center text-sm text-gray-600 mt-2">
                  {selectedGoalDetails.goal.progressPercentage}% complete
                </p>
              </div>

              {/* Matching Rule Info */}
              {selectedGoalDetails.matching && (
                <div className="p-4 bg-purple-50 rounded-lg">
                  <h4 className="font-medium text-purple-800 mb-2">💰 Parent Matching Active</h4>
                  <p className="text-sm text-purple-700">
                    {selectedGoalDetails.matching.matchType === 'RatioMatch'
                      ? `Matching $${selectedGoalDetails.matching.matchRatio.toFixed(2)} for every $1 saved`
                      : `Matching ${(selectedGoalDetails.matching.matchRatio * 100).toFixed(0)}% of contributions`}
                  </p>
                  <p className="text-xs text-purple-600 mt-1">
                    Total matched so far: {formatCurrency(selectedGoalDetails.matching.totalMatchedAmount)}
                    {selectedGoalDetails.matching.maxMatchAmount &&
                      ` (max: ${formatCurrency(selectedGoalDetails.matching.maxMatchAmount)})`}
                  </p>
                </div>
              )}

              {/* Challenge Info */}
              {selectedGoalDetails.challenge && (
                <div className="p-4 bg-orange-50 rounded-lg">
                  <h4 className="font-medium text-orange-800 mb-2">🏆 Active Challenge</h4>
                  <p className="text-sm text-orange-700">
                    Save {formatCurrency(selectedGoalDetails.challenge.targetAmount)} by{' '}
                    {new Date(selectedGoalDetails.challenge.endDate).toLocaleDateString()}
                  </p>
                  <div className="mt-2">
                    <div className="flex justify-between text-xs text-orange-600 mb-1">
                      <span>Progress: {formatCurrency(selectedGoalDetails.challenge.currentAmount)}</span>
                      <span>{selectedGoalDetails.challenge.progressPercentage}%</span>
                    </div>
                    <div className="w-full bg-orange-200 rounded-full h-2">
                      <div
                        className="h-2 rounded-full bg-orange-500"
                        style={{ width: `${Math.min(selectedGoalDetails.challenge.progressPercentage, 100)}%` }}
                      />
                    </div>
                  </div>
                  <p className="text-xs text-orange-600 mt-2">
                    {selectedGoalDetails.challenge.daysRemaining} days remaining • Bonus:{' '}
                    {formatCurrency(selectedGoalDetails.challenge.bonusAmount)}
                  </p>
                </div>
              )}

              {/* Auto-transfer Info */}
              {selectedGoalDetails.goal.autoTransferType !== 'None' && (
                <div className="p-4 bg-blue-50 rounded-lg">
                  <h4 className="font-medium text-blue-800 mb-1">🔄 Auto-transfer Enabled</h4>
                  <p className="text-sm text-blue-700">
                    {selectedGoalDetails.goal.autoTransferType === 'FixedAmount'
                      ? `${formatCurrency(selectedGoalDetails.goal.autoTransferAmount || 0)} per allowance`
                      : `${selectedGoalDetails.goal.autoTransferPercentage}% of each allowance`}
                  </p>
                </div>
              )}

              {/* Created Date */}
              <p className="text-xs text-gray-500 text-center">
                Created {new Date(selectedGoalDetails.goal.createdAt).toLocaleDateString()}
              </p>
            </div>

            <div className="mt-6 flex justify-end">
              <button
                onClick={() => setSelectedGoalDetails(null)}
                className="px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
