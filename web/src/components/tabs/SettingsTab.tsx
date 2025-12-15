import React, { useState, useEffect } from 'react';
import { childrenApi } from '../../services/api';
import type { Child, UpdateChildSettingsRequest, DayOfWeek } from '../../types';
import { Save, DollarSign, PiggyBank, Percent, Calendar, Eye, EyeOff, CreditCard } from 'lucide-react';

const DAYS_OF_WEEK: { value: DayOfWeek; label: string }[] = [
  { value: 'Sunday', label: 'Sunday' },
  { value: 'Monday', label: 'Monday' },
  { value: 'Tuesday', label: 'Tuesday' },
  { value: 'Wednesday', label: 'Wednesday' },
  { value: 'Thursday', label: 'Thursday' },
  { value: 'Friday', label: 'Friday' },
  { value: 'Saturday', label: 'Saturday' },
];

interface SettingsTabProps {
  childId: string;
  child: Child;
  onUpdate: () => void;
}

export const SettingsTab: React.FC<SettingsTabProps> = ({ childId, child, onUpdate }) => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  // Form state
  const [weeklyAllowance, setWeeklyAllowance] = useState(child.weeklyAllowance.toString());
  const [allowanceDay, setAllowanceDay] = useState<DayOfWeek | ''>(child.allowanceDay || '');
  const [savingsEnabled, setSavingsEnabled] = useState(child.savingsAccountEnabled);
  const [transferType, setTransferType] = useState<'Percentage' | 'FixedAmount'>(
    child.savingsTransferType || 'Percentage'
  );
  const [transferPercentage, setTransferPercentage] = useState(
    (child.savingsTransferPercentage ?? 20).toString()
  );
  const [transferAmount, setTransferAmount] = useState(
    (child.savingsTransferAmount ?? 0).toString()
  );
  const [savingsBalanceVisible, setSavingsBalanceVisible] = useState(
    child.savingsBalanceVisibleToChild
  );
  const [allowDebt, setAllowDebt] = useState(child.allowDebt);

  // Reset form when child changes
  useEffect(() => {
    setWeeklyAllowance(child.weeklyAllowance.toString());
    setAllowanceDay(child.allowanceDay || '');
    setSavingsEnabled(child.savingsAccountEnabled);
    setTransferType(child.savingsTransferType || 'Percentage');
    setTransferPercentage((child.savingsTransferPercentage ?? 20).toString());
    setTransferAmount((child.savingsTransferAmount ?? 0).toString());
    setSavingsBalanceVisible(child.savingsBalanceVisibleToChild);
    setAllowDebt(child.allowDebt);
  }, [child]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setSuccess('');
    setIsLoading(true);

    try {
      const allowanceValue = parseFloat(weeklyAllowance);
      if (isNaN(allowanceValue) || allowanceValue < 0) {
        setError('Please enter a valid allowance amount');
        setIsLoading(false);
        return;
      }

      const data: UpdateChildSettingsRequest = {
        weeklyAllowance: allowanceValue,
        allowanceDay: allowanceDay || null,
        savingsAccountEnabled: savingsEnabled,
        savingsBalanceVisibleToChild: savingsBalanceVisible,
        allowDebt: allowDebt,
      };

      if (savingsEnabled) {
        data.savingsTransferType = transferType;
        if (transferType === 'Percentage') {
          const pct = parseFloat(transferPercentage);
          if (isNaN(pct) || pct < 0 || pct > 100) {
            setError('Percentage must be between 0 and 100');
            setIsLoading(false);
            return;
          }
          data.savingsTransferPercentage = pct;
        } else {
          const amt = parseFloat(transferAmount);
          if (isNaN(amt) || amt < 0) {
            setError('Please enter a valid savings amount');
            setIsLoading(false);
            return;
          }
          data.savingsTransferAmount = amt;
        }
      }

      await childrenApi.updateSettings(childId, data);
      setSuccess('Settings saved successfully!');
      onUpdate();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { error?: { message?: string }; message?: string } } }).response?.data?.error?.message ||
          (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to save settings. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  // Calculate example savings
  const calculateExampleSavings = () => {
    const allowance = parseFloat(weeklyAllowance) || 0;
    if (!savingsEnabled || allowance === 0) return 0;

    if (transferType === 'Percentage') {
      const pct = parseFloat(transferPercentage) || 0;
      return (allowance * pct) / 100;
    } else {
      const amt = parseFloat(transferAmount) || 0;
      return Math.min(amt, allowance);
    }
  };

  const exampleSavings = calculateExampleSavings();
  const exampleSpending = (parseFloat(weeklyAllowance) || 0) - exampleSavings;

  return (
    <div className="bg-white shadow-sm rounded-lg p-6">
      <h3 className="text-lg font-semibold text-gray-900 mb-6">Allowance Settings</h3>

      <form onSubmit={handleSubmit} className="space-y-6">
        {error && (
          <div className="rounded-md bg-red-50 p-4">
            <div className="text-sm text-red-800">{error}</div>
          </div>
        )}

        {success && (
          <div className="rounded-md bg-green-50 p-4">
            <div className="text-sm text-green-800">{success}</div>
          </div>
        )}

        {/* Weekly Allowance */}
        <div>
          <label htmlFor="weeklyAllowance" className="block text-sm font-medium text-gray-700 mb-1">
            Weekly Allowance
          </label>
          <div className="relative rounded-md shadow-sm">
            <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
              <DollarSign className="h-5 w-5 text-gray-400" />
            </div>
            <input
              type="number"
              id="weeklyAllowance"
              value={weeklyAllowance}
              onChange={(e) => setWeeklyAllowance(e.target.value)}
              min="0"
              step="0.01"
              className="block w-full rounded-md border-gray-300 pl-10 pr-3 py-2 focus:border-primary-500 focus:ring-primary-500 sm:text-sm"
              placeholder="0.00"
            />
          </div>
        </div>

        {/* Allowance Day */}
        <div>
          <label htmlFor="allowanceDay" className="block text-sm font-medium text-gray-700 mb-1">
            Allowance Day
          </label>
          <div className="relative rounded-md shadow-sm">
            <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
              <Calendar className="h-5 w-5 text-gray-400" />
            </div>
            <select
              id="allowanceDay"
              value={allowanceDay}
              onChange={(e) => setAllowanceDay(e.target.value as DayOfWeek | '')}
              className="block w-full rounded-md border-gray-300 pl-10 pr-3 py-2 focus:border-primary-500 focus:ring-primary-500 sm:text-sm"
            >
              <option value="">Any day (rolling 7-day window)</option>
              {DAYS_OF_WEEK.map((day) => (
                <option key={day.value} value={day.value}>
                  {day.label}
                </option>
              ))}
            </select>
          </div>
          <p className="mt-1 text-sm text-gray-500">
            {allowanceDay
              ? `Allowance is paid every ${allowanceDay}`
              : 'Allowance is paid 7 days after the last payment'}
          </p>
        </div>

        {/* Savings Account Toggle */}
        <div className="border-t border-gray-200 pt-6">
          <div className="flex items-center justify-between">
            <div className="flex items-center">
              <PiggyBank className="h-5 w-5 text-primary-500 mr-2" />
              <div>
                <label htmlFor="savingsEnabled" className="text-sm font-medium text-gray-700">
                  Automatic Savings
                </label>
                <p className="text-sm text-gray-500">
                  Automatically transfer part of each allowance to savings
                </p>
              </div>
            </div>
            <button
              type="button"
              onClick={() => setSavingsEnabled(!savingsEnabled)}
              className={`relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 ${
                savingsEnabled ? 'bg-primary-600' : 'bg-gray-200'
              }`}
            >
              <span
                className={`pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out ${
                  savingsEnabled ? 'translate-x-5' : 'translate-x-0'
                }`}
              />
            </button>
          </div>
        </div>

        {/* Savings Configuration */}
        {savingsEnabled && (
          <div className="bg-gray-50 rounded-lg p-4 space-y-4">
            {/* Transfer Type */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Transfer Type
              </label>
              <div className="grid grid-cols-2 gap-3">
                <button
                  type="button"
                  onClick={() => setTransferType('Percentage')}
                  className={`flex items-center justify-center px-4 py-3 border rounded-lg text-sm font-medium transition-colors ${
                    transferType === 'Percentage'
                      ? 'border-primary-500 bg-primary-50 text-primary-700'
                      : 'border-gray-300 bg-white text-gray-700 hover:bg-gray-50'
                  }`}
                >
                  <Percent className="h-4 w-4 mr-2" />
                  Percentage
                </button>
                <button
                  type="button"
                  onClick={() => setTransferType('FixedAmount')}
                  className={`flex items-center justify-center px-4 py-3 border rounded-lg text-sm font-medium transition-colors ${
                    transferType === 'FixedAmount'
                      ? 'border-primary-500 bg-primary-50 text-primary-700'
                      : 'border-gray-300 bg-white text-gray-700 hover:bg-gray-50'
                  }`}
                >
                  <DollarSign className="h-4 w-4 mr-2" />
                  Fixed Amount
                </button>
              </div>
            </div>

            {/* Percentage Input */}
            {transferType === 'Percentage' && (
              <div>
                <label htmlFor="transferPercentage" className="block text-sm font-medium text-gray-700 mb-1">
                  Savings Percentage
                </label>
                <div className="relative rounded-md shadow-sm">
                  <input
                    type="number"
                    id="transferPercentage"
                    value={transferPercentage}
                    onChange={(e) => setTransferPercentage(e.target.value)}
                    min="0"
                    max="100"
                    step="1"
                    className="block w-full rounded-md border-gray-300 pr-10 py-2 focus:border-primary-500 focus:ring-primary-500 sm:text-sm"
                    placeholder="20"
                  />
                  <div className="pointer-events-none absolute inset-y-0 right-0 flex items-center pr-3">
                    <span className="text-gray-500 sm:text-sm">%</span>
                  </div>
                </div>
              </div>
            )}

            {/* Fixed Amount Input */}
            {transferType === 'FixedAmount' && (
              <div>
                <label htmlFor="transferAmount" className="block text-sm font-medium text-gray-700 mb-1">
                  Savings Amount
                </label>
                <div className="relative rounded-md shadow-sm">
                  <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
                    <DollarSign className="h-5 w-5 text-gray-400" />
                  </div>
                  <input
                    type="number"
                    id="transferAmount"
                    value={transferAmount}
                    onChange={(e) => setTransferAmount(e.target.value)}
                    min="0"
                    step="0.01"
                    className="block w-full rounded-md border-gray-300 pl-10 pr-3 py-2 focus:border-primary-500 focus:ring-primary-500 sm:text-sm"
                    placeholder="0.00"
                  />
                </div>
              </div>
            )}

            {/* Preview */}
            <div className="bg-white rounded-lg p-4 border border-gray-200">
              <h4 className="text-sm font-medium text-gray-700 mb-3">Weekly Breakdown Preview</h4>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <p className="text-xs text-gray-500 mb-1">To Spending</p>
                  <p className="text-lg font-semibold text-gray-900">{formatCurrency(exampleSpending)}</p>
                </div>
                <div>
                  <p className="text-xs text-gray-500 mb-1">To Savings</p>
                  <p className="text-lg font-semibold text-primary-600">{formatCurrency(exampleSavings)}</p>
                </div>
              </div>
            </div>

            {/* Savings Balance Visibility Toggle */}
            <div className="flex items-center justify-between pt-2">
              <div className="flex items-center">
                {savingsBalanceVisible ? (
                  <Eye className="h-5 w-5 text-primary-500 mr-2" />
                ) : (
                  <EyeOff className="h-5 w-5 text-gray-400 mr-2" />
                )}
                <div>
                  <label htmlFor="savingsBalanceVisible" className="text-sm font-medium text-gray-700">
                    Show Savings Balance to Child
                  </label>
                  <p className="text-sm text-gray-500">
                    {savingsBalanceVisible
                      ? 'Child can see their savings balance'
                      : 'Savings balance is hidden from child'}
                  </p>
                </div>
              </div>
              <button
                type="button"
                onClick={() => setSavingsBalanceVisible(!savingsBalanceVisible)}
                className={`relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 ${
                  savingsBalanceVisible ? 'bg-primary-600' : 'bg-gray-200'
                }`}
              >
                <span
                  className={`pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out ${
                    savingsBalanceVisible ? 'translate-x-5' : 'translate-x-0'
                  }`}
                />
              </button>
            </div>
          </div>
        )}

        {/* Allow Debt Toggle */}
        <div className="border-t border-gray-200 pt-6">
          <div className="flex items-center justify-between">
            <div className="flex items-center">
              <CreditCard className="h-5 w-5 text-amber-500 mr-2" />
              <div>
                <label htmlFor="allowDebt" className="text-sm font-medium text-gray-700">
                  Allow Debt
                </label>
                <p className="text-sm text-gray-500">
                  {allowDebt
                    ? 'Child can spend more than their balance (goes negative)'
                    : 'Transactions are blocked if balance is insufficient'}
                </p>
              </div>
            </div>
            <button
              type="button"
              onClick={() => setAllowDebt(!allowDebt)}
              className={`relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 ${
                allowDebt ? 'bg-amber-500' : 'bg-gray-200'
              }`}
            >
              <span
                className={`pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out ${
                  allowDebt ? 'translate-x-5' : 'translate-x-0'
                }`}
              />
            </button>
          </div>
          {allowDebt && (
            <p className="mt-2 text-sm text-amber-600 bg-amber-50 rounded-md p-3">
              When spending exceeds available funds, savings will be used first before going into debt.
            </p>
          )}
        </div>

        {/* Submit Button */}
        <div className="pt-4">
          <button
            type="submit"
            disabled={isLoading}
            className="w-full flex justify-center items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isLoading ? (
              <>
                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                Saving...
              </>
            ) : (
              <>
                <Save className="h-4 w-4 mr-2" />
                Save Settings
              </>
            )}
          </button>
        </div>
      </form>
    </div>
  );
};
