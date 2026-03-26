import React, { useEffect, useState, useCallback } from 'react';
import { savingsApi } from '../../services/api';
import { PiggyBank, ArrowDownToLine, ArrowUpFromLine, Settings } from 'lucide-react';
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
  const [depositData, setDepositData] = useState<DepositToSavingsRequest>({ childId, amount: 0, description: '' });
  const [withdrawData, setWithdrawData] = useState<WithdrawFromSavingsRequest>({ childId, amount: 0, description: '' });
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
    if (depositData.amount <= 0) { setError('Amount must be greater than 0'); return; }
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
    if (withdrawData.amount <= 0) { setError('Amount must be greater than 0'); return; }
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
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
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
      <div className="text-center py-12 bg-white rounded-2xl shadow-sm">
        <div className="text-3xl mb-3">🏦</div>
        <h3 className="text-sm font-semibold text-gray-900">Savings Account Not Enabled</h3>
        <p className="mt-1 text-sm text-gray-400">Enable savings in the Settings tab.</p>
      </div>
    );
  }

  if (summary?.balanceHidden) return null;

  return (
    <div className="space-y-5">
      {error && (
        <div className="rounded-xl bg-red-50 p-3">
          <div className="text-sm text-red-800">{error}</div>
        </div>
      )}

      {/* Savings Hero */}
      <div className="bg-white rounded-2xl shadow-sm p-6 text-center">
        <div className="mx-auto w-14 h-14 rounded-2xl bg-tertiary-50 flex items-center justify-center mb-3">
          <PiggyBank className="w-7 h-7 text-tertiary-600" />
        </div>
        <p className="text-xs font-medium text-gray-400">Savings Balance</p>
        <p className="text-3xl font-bold text-primary-600 font-headline mt-1">
          {formatCurrency(summary.currentBalance ?? 0)}
        </p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-3 gap-3">
        <div className="bg-white rounded-2xl shadow-sm p-4">
          <div className="flex items-center gap-1.5 mb-1">
            <ArrowDownToLine className="w-3.5 h-3.5 text-primary-500" />
            <span className="text-xs text-gray-400">Deposited</span>
          </div>
          <p className="text-base font-bold text-gray-900">{formatCurrency(summary.totalDeposited ?? 0)}</p>
        </div>
        <div className="bg-white rounded-2xl shadow-sm p-4">
          <div className="flex items-center gap-1.5 mb-1">
            <ArrowUpFromLine className="w-3.5 h-3.5 text-gray-400" />
            <span className="text-xs text-gray-400">Withdrawn</span>
          </div>
          <p className="text-base font-bold text-gray-900">{formatCurrency(summary.totalWithdrawn ?? 0)}</p>
        </div>
        <div className="bg-white rounded-2xl shadow-sm p-4">
          <div className="flex items-center gap-1.5 mb-1">
            <Settings className="w-3.5 h-3.5 text-tertiary-500" />
            <span className="text-xs text-gray-400">Auto-Save</span>
          </div>
          <p className="text-xs font-medium text-gray-700 mt-0.5">{summary.configDescription}</p>
        </div>
      </div>

      {/* Action Buttons */}
      <div className="flex gap-3">
        <button
          onClick={() => { setShowDepositForm(true); setShowWithdrawForm(false); }}
          className="flex-1 inline-flex justify-center items-center gap-2 py-2.5 text-sm font-medium rounded-xl text-white bg-primary-600 hover:bg-primary-700 transition-colors"
        >
          <ArrowDownToLine className="w-4 h-4" /> Deposit
        </button>
        <button
          onClick={() => { setShowWithdrawForm(true); setShowDepositForm(false); }}
          className="flex-1 inline-flex justify-center items-center gap-2 py-2.5 text-sm font-medium rounded-xl text-gray-700 bg-gray-100 hover:bg-gray-200 transition-colors"
        >
          <ArrowUpFromLine className="w-4 h-4" /> Withdraw
        </button>
      </div>

      {/* Deposit Form */}
      {showDepositForm && (
        <div className="bg-white rounded-2xl shadow-sm p-5">
          <h4 className="text-sm font-semibold text-gray-900 mb-3">Deposit to Savings</h4>
          <form onSubmit={handleDeposit} className="space-y-3">
            <div>
              <label className="block text-xs font-medium text-gray-500 mb-1">Amount</label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <span className="text-gray-400 text-sm">$</span>
                </div>
                <input type="number" step="0.01" min="0.01" required value={depositData.amount || ''}
                  onChange={(e) => setDepositData({ ...depositData, amount: parseFloat(e.target.value) || 0 })}
                  className="block w-full pl-7 pr-4 py-2.5 bg-gray-50 border-0 rounded-xl ring-1 ring-inset ring-gray-200 focus:ring-2 focus:ring-primary-500 sm:text-sm"
                />
              </div>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-500 mb-1">Description</label>
              <input type="text" required value={depositData.description}
                onChange={(e) => setDepositData({ ...depositData, description: e.target.value })}
                className="block w-full px-3 py-2.5 bg-gray-50 border-0 rounded-xl ring-1 ring-inset ring-gray-200 focus:ring-2 focus:ring-primary-500 sm:text-sm"
                placeholder="e.g., Manual savings deposit"
              />
            </div>
            <div className="flex gap-3 pt-1">
              <button type="button" onClick={() => { setShowDepositForm(false); setError(''); }}
                className="flex-1 px-4 py-2.5 text-sm font-medium rounded-xl text-gray-700 bg-gray-100 hover:bg-gray-200 transition-colors">Cancel</button>
              <button type="submit" disabled={isSubmitting}
                className="flex-1 px-4 py-2.5 text-sm font-medium rounded-xl text-white bg-primary-600 hover:bg-primary-700 disabled:opacity-50 transition-colors">
                {isSubmitting ? 'Depositing...' : 'Deposit'}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Withdraw Form */}
      {showWithdrawForm && (
        <div className="bg-white rounded-2xl shadow-sm p-5">
          <h4 className="text-sm font-semibold text-gray-900 mb-3">Withdraw from Savings</h4>
          <form onSubmit={handleWithdraw} className="space-y-3">
            <div>
              <label className="block text-xs font-medium text-gray-500 mb-1">Amount</label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <span className="text-gray-400 text-sm">$</span>
                </div>
                <input type="number" step="0.01" min="0.01" max={summary.currentBalance ?? 0} required value={withdrawData.amount || ''}
                  onChange={(e) => setWithdrawData({ ...withdrawData, amount: parseFloat(e.target.value) || 0 })}
                  className="block w-full pl-7 pr-4 py-2.5 bg-gray-50 border-0 rounded-xl ring-1 ring-inset ring-gray-200 focus:ring-2 focus:ring-primary-500 sm:text-sm"
                />
              </div>
              <p className="mt-1 text-xs text-gray-400">Available: {formatCurrency(summary.currentBalance ?? 0)}</p>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-500 mb-1">Description</label>
              <input type="text" required value={withdrawData.description}
                onChange={(e) => setWithdrawData({ ...withdrawData, description: e.target.value })}
                className="block w-full px-3 py-2.5 bg-gray-50 border-0 rounded-xl ring-1 ring-inset ring-gray-200 focus:ring-2 focus:ring-primary-500 sm:text-sm"
                placeholder="e.g., Withdraw for purchase"
              />
            </div>
            <div className="flex gap-3 pt-1">
              <button type="button" onClick={() => { setShowWithdrawForm(false); setError(''); }}
                className="flex-1 px-4 py-2.5 text-sm font-medium rounded-xl text-gray-700 bg-gray-100 hover:bg-gray-200 transition-colors">Cancel</button>
              <button type="submit" disabled={isSubmitting}
                className="flex-1 px-4 py-2.5 text-sm font-medium rounded-xl text-white bg-red-500 hover:bg-red-600 disabled:opacity-50 transition-colors">
                {isSubmitting ? 'Withdrawing...' : 'Withdraw'}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Savings Activity */}
      <div>
        <h4 className="text-sm font-semibold text-gray-900 mb-3">Savings Activity</h4>
        {transactions.length === 0 ? (
          <div className="text-center py-8 bg-white rounded-2xl shadow-sm">
            <p className="text-sm text-gray-400">No savings activity yet</p>
          </div>
        ) : (
          <div className="space-y-2">
            {transactions.map((tx) => {
              const isDeposit = tx.type === 'Deposit' || tx.type === 'AutoTransfer';
              return (
                <div key={tx.id} className="bg-white rounded-xl p-3.5 shadow-sm flex items-center gap-3">
                  <div className={`w-10 h-10 rounded-xl flex items-center justify-center flex-shrink-0 ${isDeposit ? 'bg-primary-50' : 'bg-gray-100'}`}>
                    {isDeposit
                      ? <ArrowDownToLine className="w-4.5 h-4.5 text-primary-600" />
                      : <ArrowUpFromLine className="w-4.5 h-4.5 text-gray-500" />
                    }
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-gray-900 truncate">{tx.description}</p>
                    <p className="text-xs text-gray-400">{formatDate(tx.createdAt)}</p>
                  </div>
                  <p className={`text-sm font-semibold flex-shrink-0 ${isDeposit ? 'text-primary-600' : 'text-gray-600'}`}>
                    {isDeposit ? '+' : '-'}{formatCurrency(tx.amount)}
                  </p>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
};
