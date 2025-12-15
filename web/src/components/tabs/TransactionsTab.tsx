import React, { useEffect, useState, useCallback } from 'react';
import { useAuth } from '../../contexts/AuthContext';
import { transactionsApi } from '../../services/api';
import { TransactionType, TransactionCategory, type Transaction, type CreateTransactionRequest } from '../../types';

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

    // Check if this is a debit that exceeds spending balance
    if (formData.type === TransactionType.Debit && formData.amount > currentBalance) {
      const totalAvailable = currentBalance + savingsBalance;

      if (formData.amount > totalAvailable && !allowDebt) {
        setError(`Insufficient funds. Total available (spending + savings): ${formatCurrency(totalAvailable)}`);
        return;
      }

      // Show confirmation dialog to draw from savings (and possibly go into debt)
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

  return (
    <div className="space-y-6">
      {/* Savings Confirmation Dialog */}
      {showSavingsConfirm && pendingTransaction && (() => {
        const shortfall = pendingTransaction.amount - currentBalance;
        const totalAvailable = currentBalance + savingsBalance;
        const willGoIntoDebt = pendingTransaction.amount > totalAvailable;
        const debtAmount = willGoIntoDebt ? pendingTransaction.amount - totalAvailable : 0;
        const fromSavings = willGoIntoDebt ? savingsBalance : shortfall;

        return (
          <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50 flex items-center justify-center">
            <div className="relative mx-auto p-6 border w-full max-w-md shadow-lg rounded-lg bg-white">
              <h3 className="text-lg font-medium text-gray-900 mb-4">
                {willGoIntoDebt ? 'Go Into Debt?' : 'Draw from Savings?'}
              </h3>
              <p className="text-sm text-gray-600 mb-4">
                The spending balance ({formatCurrency(currentBalance)}) is not enough for this {formatCurrency(pendingTransaction.amount)} transaction.
              </p>
              {willGoIntoDebt ? (
                <p className="text-sm text-amber-700 bg-amber-50 rounded-md p-3 mb-4">
                  This will use all savings ({formatCurrency(savingsBalance)}) and put the account {formatCurrency(debtAmount)} into debt.
                </p>
              ) : (
                <p className="text-sm text-gray-600 mb-4">
                  Would you like to draw {formatCurrency(fromSavings)} from savings to complete this transaction?
                </p>
              )}
              <div className="bg-gray-50 rounded-md p-3 mb-4">
                <div className="flex justify-between text-sm">
                  <span className="text-gray-600">Spending balance:</span>
                  <span className="font-medium">{formatCurrency(currentBalance)}</span>
                </div>
                {fromSavings > 0 && (
                  <div className="flex justify-between text-sm">
                    <span className="text-gray-600">From savings:</span>
                    <span className="font-medium text-amber-600">{formatCurrency(fromSavings)}</span>
                  </div>
                )}
                {willGoIntoDebt && (
                  <div className="flex justify-between text-sm">
                    <span className="text-gray-600">Into debt:</span>
                    <span className="font-medium text-red-600">-{formatCurrency(debtAmount)}</span>
                  </div>
                )}
                <div className="flex justify-between text-sm border-t border-gray-200 mt-2 pt-2">
                  <span className="text-gray-600">Total:</span>
                  <span className="font-medium">{formatCurrency(pendingTransaction.amount)}</span>
                </div>
              </div>
              <div className="flex justify-end space-x-3">
                <button
                  type="button"
                  onClick={handleCancelDrawFromSavings}
                  disabled={isSubmitting}
                  className="px-4 py-2 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 disabled:opacity-50"
                >
                  Cancel
                </button>
                <button
                  type="button"
                  onClick={handleConfirmDrawFromSavings}
                  disabled={isSubmitting}
                  className={`px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-50 ${
                    willGoIntoDebt
                      ? 'bg-amber-600 hover:bg-amber-700 focus:ring-amber-500'
                      : 'bg-primary-600 hover:bg-primary-700 focus:ring-primary-500'
                  }`}
                >
                  {isSubmitting ? 'Processing...' : willGoIntoDebt ? 'Yes, Go Into Debt' : 'Yes, Draw from Savings'}
                </button>
              </div>
            </div>
          </div>
        );
      })()}

      {/* Header with Add Transaction Button */}
      {isParent && (
        <div className="flex justify-between items-center">
          <h3 className="text-lg font-medium text-gray-900">Transaction History</h3>
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
                Add Transaction
              </>
            )}
          </button>
        </div>
      )}

      {/* Add Transaction Form */}
      {showAddForm && isParent && (
        <div className="bg-white border border-gray-200 rounded-lg p-6">
          <h4 className="text-md font-medium text-gray-900 mb-4">New Transaction</h4>
          <form onSubmit={handleSubmit} className="space-y-4">
            {error && (
              <div className="rounded-md bg-red-50 p-4">
                <div className="text-sm text-red-800">{error}</div>
              </div>
            )}

            <div className="grid grid-cols-2 gap-4">
              <div>
                <label htmlFor="type" className="block text-sm font-medium text-gray-700">
                  Type
                </label>
                <select
                  id="type"
                  value={formData.type}
                  onChange={(e) => setFormData({ ...formData, type: e.target.value as typeof TransactionType[keyof typeof TransactionType] })}
                  className="mt-1 block w-full pl-3 pr-10 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                >
                  <option value={TransactionType.Credit}>Credit (Add Money)</option>
                  <option value={TransactionType.Debit}>Debit (Spend Money)</option>
                </select>
              </div>

              <div>
                <label htmlFor="amount" className="block text-sm font-medium text-gray-700">
                  Amount
                </label>
                <div className="mt-1 relative rounded-md shadow-sm">
                  <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                    <span className="text-gray-500 sm:text-sm">$</span>
                  </div>
                  <input
                    type="number"
                    id="amount"
                    step="0.01"
                    min="0.01"
                    required
                    value={formData.amount || ''}
                    onChange={(e) => setFormData({ ...formData, amount: parseFloat(e.target.value) || 0 })}
                    className="block w-full pl-7 pr-12 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                  />
                </div>
              </div>
            </div>

            <div>
              <label htmlFor="category" className="block text-sm font-medium text-gray-700">
                Category
              </label>
              <select
                id="category"
                value={formData.category}
                onChange={(e) => setFormData({ ...formData, category: e.target.value as typeof TransactionCategory[keyof typeof TransactionCategory] })}
                className="mt-1 block w-full pl-3 pr-10 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
              >
                {formData.type === TransactionType.Credit ? (
                  <>
                    <option value={TransactionCategory.Allowance}>Allowance</option>
                    <option value={TransactionCategory.Chores}>Chores</option>
                    <option value={TransactionCategory.Gift}>Gift</option>
                    <option value={TransactionCategory.Other}>Other</option>
                  </>
                ) : (
                  <>
                    <option value={TransactionCategory.Toys}>Toys</option>
                    <option value={TransactionCategory.Games}>Games</option>
                    <option value={TransactionCategory.Candy}>Candy</option>
                    <option value={TransactionCategory.Books}>Books</option>
                    <option value={TransactionCategory.Clothes}>Clothes</option>
                    <option value={TransactionCategory.Electronics}>Electronics</option>
                    <option value={TransactionCategory.Food}>Food</option>
                    <option value={TransactionCategory.Entertainment}>Entertainment</option>
                    <option value={TransactionCategory.Savings}>Savings</option>
                    <option value={TransactionCategory.Charity}>Charity</option>
                  </>
                )}
              </select>
            </div>

            <div>
              <label htmlFor="description" className="block text-sm font-medium text-gray-700">
                Description
              </label>
              <input
                type="text"
                id="description"
                required
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                placeholder="e.g., Weekly allowance, Toy purchase, etc."
              />
            </div>

            <div>
              <label htmlFor="notes" className="block text-sm font-medium text-gray-700">
                Notes (optional)
              </label>
              <textarea
                id="notes"
                rows={3}
                value={formData.notes}
                onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                placeholder="Add any additional details..."
              />
            </div>

            <div className="flex justify-end space-x-3">
              <button
                type="button"
                onClick={() => {
                  setShowAddForm(false);
                  setError('');
                }}
                className="px-4 py-2 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isSubmitting}
                className="px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50"
              >
                {isSubmitting ? 'Creating...' : 'Create Transaction'}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Transactions List */}
      {transactions.length === 0 ? (
        <div className="text-center py-12 bg-white rounded-lg border border-gray-200">
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
              d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"
            />
          </svg>
          <h3 className="mt-2 text-sm font-medium text-gray-900">No transactions yet</h3>
          <p className="mt-1 text-sm text-gray-500">
            {isParent ? 'Add a transaction to get started.' : 'No transactions to display.'}
          </p>
        </div>
      ) : (
        <div className="bg-white shadow overflow-hidden sm:rounded-md">
          <ul className="divide-y divide-gray-200">
            {transactions.map((transaction) => (
              <li key={transaction.id}>
                <div className="px-4 py-4 sm:px-6">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center min-w-0 flex-1">
                      <div className={`flex-shrink-0 h-10 w-10 rounded-full flex items-center justify-center ${
                        transaction.type === TransactionType.Credit
                          ? 'bg-green-100'
                          : 'bg-red-100'
                      }`}>
                        {transaction.type === TransactionType.Credit ? (
                          <svg className="h-6 w-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
                          </svg>
                        ) : (
                          <svg className="h-6 w-6 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 12H4" />
                          </svg>
                        )}
                      </div>
                      <div className="ml-4 min-w-0 flex-1">
                        <p className="text-sm font-medium text-gray-900 truncate">
                          {transaction.description}
                        </p>
                        <p className="text-sm text-gray-500">
                          {formatDate(transaction.createdAt)} • By {transaction.createdByName}
                        </p>
                        {transaction.notes && (
                          <p className="text-sm text-gray-600 mt-1 italic">
                            {transaction.notes}
                          </p>
                        )}
                      </div>
                    </div>
                    <div className="ml-4 flex-shrink-0 text-right">
                      <p className={`text-lg font-semibold ${
                        transaction.type === TransactionType.Credit
                          ? 'text-green-600'
                          : 'text-red-600'
                      }`}>
                        {transaction.type === TransactionType.Credit ? '+' : '-'}
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
        </div>
      )}
    </div>
  );
};
