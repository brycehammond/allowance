import React, { useEffect, useState, useCallback } from 'react';
import { savingsApi } from '../../services/api';
import type {
  SavingsAccountSummary,
  SavingsTransaction,
  DepositToSavingsRequest,
  WithdrawFromSavingsRequest,
} from '../../types';

interface SavingsTabProps {
  childId: string;
  onBalanceChange?: () => void;
}

export const SavingsTab: React.FC<SavingsTabProps> = ({ childId, onBalanceChange }) => {
  const [summary, setSummary] = useState<SavingsAccountSummary | null>(null);
  const [transactions, setTransactions] = useState<SavingsTransaction[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [showDepositForm, setShowDepositForm] = useState(false);
  const [showWithdrawForm, setShowWithdrawForm] = useState(false);
  const [depositData, setDepositData] = useState<DepositToSavingsRequest>({
    childId,
    amount: 0,
    description: '',
  });
  const [withdrawData, setWithdrawData] = useState<WithdrawFromSavingsRequest>({
    childId,
    amount: 0,
    description: '',
  });
  const [isSubmitting, setIsSubmitting] = useState(false);

  const loadSavingsData = useCallback(async () => {
    try {
      setIsLoading(true);
      const [summaryData, historyData] = await Promise.all([
        savingsApi.getSummary(childId),
        savingsApi.getHistory(childId, 50),
      ]);
      setSummary(summaryData);
      setTransactions(historyData);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to load savings data');
    } finally {
      setIsLoading(false);
    }
  }, [childId]);

  useEffect(() => {
    loadSavingsData();
  }, [loadSavingsData]);

  const handleDeposit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (depositData.amount <= 0) {
      setError('Amount must be greater than 0');
      return;
    }

    setIsSubmitting(true);

    try {
      await savingsApi.deposit(depositData);
      setShowDepositForm(false);
      setDepositData({ childId, amount: 0, description: '' });
      await loadSavingsData();
      onBalanceChange?.();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to deposit funds');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleWithdraw = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (withdrawData.amount <= 0) {
      setError('Amount must be greater than 0');
      return;
    }

    setIsSubmitting(true);

    try {
      await savingsApi.withdraw(withdrawData);
      setShowWithdrawForm(false);
      setWithdrawData({ childId, amount: 0, description: '' });
      await loadSavingsData();
      onBalanceChange?.();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to withdraw funds');
    } finally {
      setIsSubmitting(false);
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  if (isLoading) {
    return (
      <div className="flex justify-center items-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-500"></div>
      </div>
    );
  }

  if (!summary?.isEnabled) {
    return (
      <div className="bg-white rounded-lg border-2 border-dashed border-gray-300 p-12">
        <div className="text-center">
          <svg
            className="mx-auto h-12 w-12 text-gray-400"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z"
            />
          </svg>
          <h3 className="mt-2 text-sm font-medium text-gray-900">Savings Account Not Enabled</h3>
          <p className="mt-1 text-sm text-gray-500">
            This child does not have a savings account enabled yet.
          </p>
        </div>
      </div>
    );
  }

  // Hide savings tab entirely when balance is hidden from child
  if (summary?.balanceHidden) {
    return null;
  }

  return (
    <div className="space-y-6">
      {error && (
        <div className="rounded-md bg-red-50 p-4">
          <div className="text-sm text-red-800">{error}</div>
        </div>
      )}

      {/* Summary Cards */}
      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
        <div className="bg-white overflow-hidden shadow rounded-lg">
          <div className="p-5">
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <svg
                  className="h-6 w-6 text-primary-500"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                  />
                </svg>
              </div>
              <div className="ml-5 w-0 flex-1">
                <dl>
                  <dt className="text-sm font-medium text-gray-500 truncate">Savings Balance</dt>
                  <dd>
                    <div className="text-lg font-medium text-gray-900">
                      {formatCurrency(summary.currentBalance ?? 0)}
                    </div>
                  </dd>
                </dl>
              </div>
            </div>
          </div>
        </div>

        <div className="bg-white overflow-hidden shadow rounded-lg">
          <div className="p-5">
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <svg
                  className="h-6 w-6 text-green-500"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M7 11l5-5m0 0l5 5m-5-5v12"
                  />
                </svg>
              </div>
              <div className="ml-5 w-0 flex-1">
                <dl>
                  <dt className="text-sm font-medium text-gray-500 truncate">Total Deposited</dt>
                  <dd>
                    <div className="text-lg font-medium text-gray-900">
                      {formatCurrency(summary.totalDeposited ?? 0)}
                    </div>
                  </dd>
                </dl>
              </div>
            </div>
          </div>
        </div>

        <div className="bg-white overflow-hidden shadow rounded-lg">
          <div className="p-5">
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <svg
                  className="h-6 w-6 text-red-500"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M17 13l-5 5m0 0l-5-5m5 5V6"
                  />
                </svg>
              </div>
              <div className="ml-5 w-0 flex-1">
                <dl>
                  <dt className="text-sm font-medium text-gray-500 truncate">Total Withdrawn</dt>
                  <dd>
                    <div className="text-lg font-medium text-gray-900">
                      {formatCurrency(summary.totalWithdrawn ?? 0)}
                    </div>
                  </dd>
                </dl>
              </div>
            </div>
          </div>
        </div>

        <div className="bg-white overflow-hidden shadow rounded-lg">
          <div className="p-5">
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <svg
                  className="h-6 w-6 text-secondary-500"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
                  />
                </svg>
              </div>
              <div className="ml-5 w-0 flex-1">
                <dl>
                  <dt className="text-sm font-medium text-gray-500 truncate">Auto Transfer</dt>
                  <dd>
                    <div className="text-sm font-medium text-gray-900">
                      {summary.configDescription}
                    </div>
                  </dd>
                </dl>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Action Buttons */}
      <div className="flex space-x-3">
        <button
          onClick={() => {
            setShowDepositForm(true);
            setShowWithdrawForm(false);
          }}
          className="flex-1 inline-flex justify-center items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500"
        >
          <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
          </svg>
          Deposit
        </button>
        <button
          onClick={() => {
            setShowWithdrawForm(true);
            setShowDepositForm(false);
          }}
          className="flex-1 inline-flex justify-center items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-red-600 hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500"
        >
          <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 12H4" />
          </svg>
          Withdraw
        </button>
      </div>

      {/* Deposit Form */}
      {showDepositForm && (
        <div className="bg-white border border-gray-200 rounded-lg p-6">
          <h4 className="text-md font-medium text-gray-900 mb-4">Deposit to Savings</h4>
          <form onSubmit={handleDeposit} className="space-y-4">
            <div>
              <label htmlFor="depositAmount" className="block text-sm font-medium text-gray-700">
                Amount
              </label>
              <div className="mt-1 relative rounded-md shadow-sm">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <span className="text-gray-500 sm:text-sm">$</span>
                </div>
                <input
                  type="number"
                  id="depositAmount"
                  step="0.01"
                  min="0.01"
                  required
                  value={depositData.amount || ''}
                  onChange={(e) =>
                    setDepositData({ ...depositData, amount: parseFloat(e.target.value) || 0 })
                  }
                  className="block w-full pl-7 pr-12 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                />
              </div>
            </div>

            <div>
              <label htmlFor="depositDescription" className="block text-sm font-medium text-gray-700">
                Description
              </label>
              <input
                type="text"
                id="depositDescription"
                required
                value={depositData.description}
                onChange={(e) => setDepositData({ ...depositData, description: e.target.value })}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                placeholder="e.g., Manual savings deposit"
              />
            </div>

            <div className="flex justify-end space-x-3">
              <button
                type="button"
                onClick={() => {
                  setShowDepositForm(false);
                  setError('');
                }}
                className="px-4 py-2 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isSubmitting}
                className="px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 disabled:opacity-50"
              >
                {isSubmitting ? 'Depositing...' : 'Deposit'}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Withdraw Form */}
      {showWithdrawForm && (
        <div className="bg-white border border-gray-200 rounded-lg p-6">
          <h4 className="text-md font-medium text-gray-900 mb-4">Withdraw from Savings</h4>
          <form onSubmit={handleWithdraw} className="space-y-4">
            <div>
              <label htmlFor="withdrawAmount" className="block text-sm font-medium text-gray-700">
                Amount
              </label>
              <div className="mt-1 relative rounded-md shadow-sm">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <span className="text-gray-500 sm:text-sm">$</span>
                </div>
                <input
                  type="number"
                  id="withdrawAmount"
                  step="0.01"
                  min="0.01"
                  max={summary.currentBalance ?? 0}
                  required
                  value={withdrawData.amount || ''}
                  onChange={(e) =>
                    setWithdrawData({ ...withdrawData, amount: parseFloat(e.target.value) || 0 })
                  }
                  className="block w-full pl-7 pr-12 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                />
              </div>
              <p className="mt-1 text-sm text-gray-500">
                Available: {formatCurrency(summary.currentBalance ?? 0)}
              </p>
            </div>

            <div>
              <label htmlFor="withdrawDescription" className="block text-sm font-medium text-gray-700">
                Description
              </label>
              <input
                type="text"
                id="withdrawDescription"
                required
                value={withdrawData.description}
                onChange={(e) => setWithdrawData({ ...withdrawData, description: e.target.value })}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                placeholder="e.g., Withdraw for purchase"
              />
            </div>

            <div className="flex justify-end space-x-3">
              <button
                type="button"
                onClick={() => {
                  setShowWithdrawForm(false);
                  setError('');
                }}
                className="px-4 py-2 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isSubmitting}
                className="px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-red-600 hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 disabled:opacity-50"
              >
                {isSubmitting ? 'Withdrawing...' : 'Withdraw'}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Transaction History */}
      <div className="bg-white shadow rounded-lg">
        <div className="px-4 py-5 sm:px-6 border-b border-gray-200">
          <h3 className="text-lg font-medium text-gray-900">Transaction History</h3>
        </div>
        {transactions.length === 0 ? (
          <div className="text-center py-12">
            <p className="text-sm text-gray-500">No transactions yet</p>
          </div>
        ) : (
          <ul className="divide-y divide-gray-200">
            {transactions.map((transaction) => (
              <li key={transaction.id}>
                <div className="px-4 py-4 sm:px-6">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center min-w-0 flex-1">
                      <div
                        className={`flex-shrink-0 h-10 w-10 rounded-full flex items-center justify-center ${
                          transaction.type === 'Deposit' || transaction.type === 'AutoTransfer'
                            ? 'bg-green-100'
                            : 'bg-red-100'
                        }`}
                      >
                        {transaction.type === 'Deposit' || transaction.type === 'AutoTransfer' ? (
                          <svg className="h-6 w-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 11l5-5m0 0l5 5m-5-5v12" />
                          </svg>
                        ) : (
                          <svg className="h-6 w-6 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 13l-5 5m0 0l-5-5m5 5V6" />
                          </svg>
                        )}
                      </div>
                      <div className="ml-4 min-w-0 flex-1">
                        <p className="text-sm font-medium text-gray-900 truncate">
                          {transaction.description}
                        </p>
                        <p className="text-sm text-gray-500">
                          {formatDate(transaction.createdAt)} •{' '}
                          {transaction.createdByName}
                        </p>
                      </div>
                    </div>
                    <div className="ml-4 flex-shrink-0 text-right">
                      <p
                        className={`text-lg font-semibold ${
                          transaction.type === 'Deposit' || transaction.type === 'AutoTransfer'
                            ? 'text-green-600'
                            : 'text-red-600'
                        }`}
                      >
                        {transaction.type === 'Deposit' || transaction.type === 'AutoTransfer' ? '+' : '-'}
                        {formatCurrency(transaction.amount)}
                      </p>
                      <p className="text-sm text-gray-500">
                        Balance: {formatCurrency(transaction.balanceAfter)}
                      </p>
                    </div>
                  </div>
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
};
