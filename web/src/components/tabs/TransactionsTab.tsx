import React, { useEffect, useState, useCallback } from 'react';
import { useAuth } from '../../contexts/AuthContext';
import { transactionsApi } from '../../services/api';
import { TransactionType, TransactionCategory, type Transaction, type CreateTransactionRequest } from '../../types';
import { getCategoryInfo } from '../../utils/categoryEmoji';
import { Plus, X } from 'lucide-react';

interface TransactionsTabProps {
  childId: string;
  currentBalance: number;
  savingsBalance: number;
  allowDebt: boolean;
  onBalanceChange?: () => void;
}

export const TransactionsTab: React.FC<TransactionsTabProps> = ({ childId, currentBalance, savingsBalance, allowDebt, onBalanceChange }) => {
  const { user } = useAuth();
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [showAddForm, setShowAddForm] = useState(false);
  const [formData, setFormData] = useState<CreateTransactionRequest>({
    childId,
    amount: 0,
    type: TransactionType.Credit,
    category: TransactionCategory.Allowance,
    description: '',
    notes: '',
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [showSavingsConfirm, setShowSavingsConfirm] = useState(false);
  const [pendingTransaction, setPendingTransaction] = useState<CreateTransactionRequest | null>(null);

  const isParent = user?.role === 'Parent';

  const loadTransactions = useCallback(async () => {
    try {
      setIsLoading(true);
      const data = await transactionsApi.getByChild(childId);
      setTransactions(data);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to load transactions');
    } finally {
      setIsLoading(false);
    }
  }, [childId]);

  useEffect(() => {
    loadTransactions();
  }, [loadTransactions]);

  const submitTransaction = async (transaction: CreateTransactionRequest) => {
    setIsSubmitting(true);
    try {
      await transactionsApi.create(transaction);
      setShowAddForm(false);
      setShowSavingsConfirm(false);
      setPendingTransaction(null);
      setFormData({
        childId,
        amount: 0,
        type: TransactionType.Credit,
        category: TransactionCategory.Allowance,
        description: '',
        notes: '',
      });
      await loadTransactions();
      onBalanceChange?.();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to create transaction');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (formData.amount <= 0) {
      setError('Amount must be greater than 0');
      return;
    }

    if (formData.type === TransactionType.Debit && formData.amount > currentBalance) {
      const totalAvailable = currentBalance + savingsBalance;

      if (formData.amount > totalAvailable && !allowDebt) {
        setError(`Insufficient funds. Total available (spending + savings): ${formatCurrency(totalAvailable)}`);
        return;
      }

      setPendingTransaction(formData);
      setShowSavingsConfirm(true);
      return;
    }

    await submitTransaction(formData);
  };

  const handleConfirmDrawFromSavings = async () => {
    if (!pendingTransaction) return;
    await submitTransaction({ ...pendingTransaction, drawFromSavings: true });
  };

  const handleCancelDrawFromSavings = () => {
    setShowSavingsConfirm(false);
    setPendingTransaction(null);
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  };

  const getDateLabel = (dateString: string) => {
    const date = new Date(dateString);
    const today = new Date();
    const yesterday = new Date();
    yesterday.setDate(yesterday.getDate() - 1);

    if (date.toDateString() === today.toDateString()) return 'Today';
    if (date.toDateString() === yesterday.toDateString()) return 'Yesterday';
    return formatDate(dateString);
  };

  // Group transactions by date
  const groupedTransactions = transactions.reduce<Record<string, Transaction[]>>((groups, tx) => {
    const key = new Date(tx.createdAt).toDateString();
    if (!groups[key]) groups[key] = [];
    groups[key].push(tx);
    return groups;
  }, {});

  if (isLoading) {
    return (
      <div className="flex justify-center items-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-500"></div>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Savings Confirmation Dialog */}
      {showSavingsConfirm && pendingTransaction && (() => {
        const shortfall = pendingTransaction.amount - currentBalance;
        const totalAvailable = currentBalance + savingsBalance;
        const willGoIntoDebt = pendingTransaction.amount > totalAvailable;
        const debtAmount = willGoIntoDebt ? pendingTransaction.amount - totalAvailable : 0;
        const fromSavings = willGoIntoDebt ? savingsBalance : shortfall;

        return (
          <div className="fixed inset-0 bg-gray-900/50 overflow-y-auto h-full w-full z-50 flex items-center justify-center p-4">
            <div className="relative mx-auto p-6 w-full max-w-md shadow-xl rounded-2xl bg-white">
              <h3 className="text-lg font-semibold text-gray-900 font-headline mb-3">
                {willGoIntoDebt ? 'Go Into Debt?' : 'Draw from Savings?'}
              </h3>
              <p className="text-sm text-gray-600 mb-3">
                The spending balance ({formatCurrency(currentBalance)}) is not enough for this {formatCurrency(pendingTransaction.amount)} transaction.
              </p>
              {willGoIntoDebt ? (
                <p className="text-sm text-amber-700 bg-amber-50 rounded-xl p-3 mb-4">
                  This will use all savings ({formatCurrency(savingsBalance)}) and put the account {formatCurrency(debtAmount)} into debt.
                </p>
              ) : (
                <p className="text-sm text-gray-600 mb-4">
                  Draw {formatCurrency(fromSavings)} from savings to complete this transaction?
                </p>
              )}
              <div className="bg-gray-50 rounded-xl p-3 mb-4 space-y-1.5">
                <div className="flex justify-between text-sm">
                  <span className="text-gray-500">Spending balance</span>
                  <span className="font-medium">{formatCurrency(currentBalance)}</span>
                </div>
                {fromSavings > 0 && (
                  <div className="flex justify-between text-sm">
                    <span className="text-gray-500">From savings</span>
                    <span className="font-medium text-amber-600">{formatCurrency(fromSavings)}</span>
                  </div>
                )}
                {willGoIntoDebt && (
                  <div className="flex justify-between text-sm">
                    <span className="text-gray-500">Into debt</span>
                    <span className="font-medium text-red-600">-{formatCurrency(debtAmount)}</span>
                  </div>
                )}
              </div>
              <div className="flex gap-3">
                <button
                  type="button"
                  onClick={handleCancelDrawFromSavings}
                  disabled={isSubmitting}
                  className="flex-1 px-4 py-2.5 text-sm font-medium rounded-xl text-gray-700 bg-gray-100 hover:bg-gray-200 disabled:opacity-50 transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="button"
                  onClick={handleConfirmDrawFromSavings}
                  disabled={isSubmitting}
                  className={`flex-1 px-4 py-2.5 text-sm font-medium rounded-xl text-white disabled:opacity-50 transition-colors ${
                    willGoIntoDebt
                      ? 'bg-amber-600 hover:bg-amber-700'
                      : 'bg-primary-600 hover:bg-primary-700'
                  }`}
                >
                  {isSubmitting ? 'Processing...' : willGoIntoDebt ? 'Go Into Debt' : 'Draw from Savings'}
                </button>
              </div>
            </div>
          </div>
        );
      })()}

      {/* Header */}
      <div className="flex justify-between items-center">
        <h3 className="text-base font-semibold text-gray-900">Recent Activity</h3>
        {isParent && (
          <button
            onClick={() => setShowAddForm(!showAddForm)}
            className={`inline-flex items-center gap-1.5 px-3.5 py-2 text-sm font-medium rounded-xl transition-colors ${
              showAddForm
                ? 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                : 'bg-primary-600 text-white hover:bg-primary-700'
            }`}
          >
            {showAddForm ? <X className="w-4 h-4" /> : <Plus className="w-4 h-4" />}
            {showAddForm ? 'Cancel' : 'Add Transaction'}
          </button>
        )}
      </div>

      {/* Add Transaction Form */}
      {showAddForm && isParent && (
        <div className="bg-white rounded-2xl shadow-sm p-5">
          <h4 className="text-sm font-semibold text-gray-900 mb-4">New Transaction</h4>
          <form onSubmit={handleSubmit} className="space-y-4">
            {error && (
              <div className="rounded-xl bg-red-50 p-3">
                <div className="text-sm text-red-800">{error}</div>
              </div>
            )}

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label htmlFor="type" className="block text-xs font-medium text-gray-500 mb-1">Type</label>
                <select
                  id="type"
                  value={formData.type}
                  onChange={(e) => setFormData({ ...formData, type: e.target.value as typeof TransactionType[keyof typeof TransactionType] })}
                  className="block w-full px-3 py-2.5 bg-gray-50 border-0 rounded-xl ring-1 ring-inset ring-gray-200 focus:ring-2 focus:ring-primary-500 sm:text-sm"
                >
                  <option value={TransactionType.Credit}>Credit (Add Money)</option>
                  <option value={TransactionType.Debit}>Debit (Spend Money)</option>
                </select>
              </div>
              <div>
                <label htmlFor="amount" className="block text-xs font-medium text-gray-500 mb-1">Amount</label>
                <div className="relative">
                  <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                    <span className="text-gray-400 text-sm">$</span>
                  </div>
                  <input
                    type="number"
                    id="amount"
                    step="0.01"
                    min="0.01"
                    required
                    value={formData.amount || ''}
                    onChange={(e) => setFormData({ ...formData, amount: parseFloat(e.target.value) || 0 })}
                    className="block w-full pl-7 pr-4 py-2.5 bg-gray-50 border-0 rounded-xl ring-1 ring-inset ring-gray-200 focus:ring-2 focus:ring-primary-500 sm:text-sm"
                  />
                </div>
              </div>
            </div>

            <div>
              <label htmlFor="category" className="block text-xs font-medium text-gray-500 mb-1">Category</label>
              <select
                id="category"
                value={formData.category}
                onChange={(e) => setFormData({ ...formData, category: e.target.value as typeof TransactionCategory[keyof typeof TransactionCategory] })}
                className="block w-full px-3 py-2.5 bg-gray-50 border-0 rounded-xl ring-1 ring-inset ring-gray-200 focus:ring-2 focus:ring-primary-500 sm:text-sm"
              >
                {formData.type === TransactionType.Credit ? (
                  <>
                    <option value={TransactionCategory.Allowance}>Allowance</option>
                    <option value={TransactionCategory.Chores}>Chores</option>
                    <option value={TransactionCategory.Gift}>Gift</option>
                    <option value={TransactionCategory.BonusReward}>Bonus/Reward</option>
                    <option value={TransactionCategory.Task}>Task</option>
                    <option value={TransactionCategory.OtherIncome}>Other Income</option>
                  </>
                ) : (
                  <>
                    <option value={TransactionCategory.Toys}>Toys</option>
                    <option value={TransactionCategory.Games}>Games</option>
                    <option value={TransactionCategory.Books}>Books</option>
                    <option value={TransactionCategory.Clothes}>Clothes</option>
                    <option value={TransactionCategory.Snacks}>Snacks</option>
                    <option value={TransactionCategory.Candy}>Candy</option>
                    <option value={TransactionCategory.Electronics}>Electronics</option>
                    <option value={TransactionCategory.Entertainment}>Entertainment</option>
                    <option value={TransactionCategory.Sports}>Sports</option>
                    <option value={TransactionCategory.Crafts}>Crafts</option>
                    <option value={TransactionCategory.Savings}>Savings</option>
                    <option value={TransactionCategory.Charity}>Charity</option>
                    <option value={TransactionCategory.Investment}>Investment</option>
                    <option value={TransactionCategory.OtherSpending}>Other</option>
                  </>
                )}
              </select>
            </div>

            <div>
              <label htmlFor="description" className="block text-xs font-medium text-gray-500 mb-1">Description</label>
              <input
                type="text"
                id="description"
                required
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                className="block w-full px-3 py-2.5 bg-gray-50 border-0 rounded-xl ring-1 ring-inset ring-gray-200 focus:ring-2 focus:ring-primary-500 sm:text-sm"
                placeholder="e.g., Weekly allowance, Toy purchase"
              />
            </div>

            <div>
              <label htmlFor="notes" className="block text-xs font-medium text-gray-500 mb-1">Notes (optional)</label>
              <textarea
                id="notes"
                rows={2}
                value={formData.notes}
                onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
                className="block w-full px-3 py-2.5 bg-gray-50 border-0 rounded-xl ring-1 ring-inset ring-gray-200 focus:ring-2 focus:ring-primary-500 sm:text-sm"
                placeholder="Additional details..."
              />
            </div>

            <div className="flex gap-3 pt-1">
              <button
                type="button"
                onClick={() => { setShowAddForm(false); setError(''); }}
                className="flex-1 px-4 py-2.5 text-sm font-medium rounded-xl text-gray-700 bg-gray-100 hover:bg-gray-200 transition-colors"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isSubmitting}
                className="flex-1 px-4 py-2.5 text-sm font-medium rounded-xl text-white bg-primary-600 hover:bg-primary-700 disabled:opacity-50 transition-colors"
              >
                {isSubmitting ? 'Creating...' : 'Create'}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Transactions List - Grouped by Date */}
      {transactions.length === 0 ? (
        <div className="text-center py-12 bg-white rounded-2xl shadow-sm">
          <div className="text-3xl mb-3">📋</div>
          <h3 className="text-sm font-semibold text-gray-900">No transactions yet</h3>
          <p className="mt-1 text-sm text-gray-400">
            {isParent ? 'Add a transaction to get started.' : 'No transactions to display.'}
          </p>
        </div>
      ) : (
        <div className="space-y-5">
          {Object.entries(groupedTransactions).map(([dateKey, txs]) => (
            <div key={dateKey}>
              <p className="text-xs font-medium text-gray-400 uppercase tracking-wider mb-2 px-1">
                {getDateLabel(txs[0].createdAt)}
              </p>
              <div className="space-y-2">
                {txs.map((transaction) => {
                  const cat = getCategoryInfo(transaction.category);
                  const isCredit = transaction.type === TransactionType.Credit;
                  return (
                    <div key={transaction.id} className="bg-white rounded-xl p-3.5 shadow-sm flex items-center gap-3">
                      <div className={`w-10 h-10 rounded-xl ${cat.color} flex items-center justify-center flex-shrink-0 text-lg`}>
                        {cat.emoji}
                      </div>
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium text-gray-900 truncate">{transaction.description}</p>
                        <p className="text-xs text-gray-400">{transaction.category.replace(/([A-Z])/g, ' $1').trim()}</p>
                      </div>
                      <div className="text-right flex-shrink-0">
                        <p className={`text-sm font-semibold ${isCredit ? 'text-primary-600' : 'text-gray-700'}`}>
                          {isCredit ? '+' : '-'}{formatCurrency(transaction.amount)}
                        </p>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
