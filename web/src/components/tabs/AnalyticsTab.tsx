import React, { useEffect, useState, useCallback } from 'react';
import { analyticsApi } from '../../services/api';
import { getCategoryInfo } from '../../utils/categoryEmoji';
import {
  LineChart,
  Line,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';
import type {
  BalancePoint,
  IncomeSpendingSummary,
  MonthlyComparison,
  CategoryBreakdown,
} from '../../types';
import { TrendingUp, TrendingDown, PiggyBank } from 'lucide-react';

interface AnalyticsTabProps {
  childId: string;
}

export const AnalyticsTab: React.FC<AnalyticsTabProps> = ({ childId }) => {
  const [balanceHistory, setBalanceHistory] = useState<BalancePoint[]>([]);
  const [incomeSpending, setIncomeSpending] = useState<IncomeSpendingSummary | null>(null);
  const [monthlyComparison, setMonthlyComparison] = useState<MonthlyComparison[]>([]);
  const [categoryBreakdown, setCategoryBreakdown] = useState<CategoryBreakdown[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [days, setDays] = useState(30);

  const loadAnalytics = useCallback(async () => {
    try {
      setIsLoading(true);
      setError('');
      const [balance, income, monthly, category] = await Promise.all([
        analyticsApi.getBalanceHistory(childId, days),
        analyticsApi.getIncomeVsSpending(childId),
        analyticsApi.getMonthlyComparison(childId, 6),
        analyticsApi.getSpendingBreakdown(childId),
      ]);
      setBalanceHistory(balance);
      setIncomeSpending(income);
      setMonthlyComparison(monthly);
      setCategoryBreakdown(category);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to load analytics');
    } finally {
      setIsLoading(false);
    }
  }, [childId, days]);

  useEffect(() => {
    loadAnalytics();
  }, [loadAnalytics]);

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);
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

  if (error) {
    return (
      <div className="rounded-xl bg-red-50 p-4">
        <div className="text-sm text-red-800">{error}</div>
      </div>
    );
  }

  return (
    <div className="space-y-5">
      {/* Period Selector */}
      <div className="flex gap-2">
        {[
          { label: '30 Days', value: 30 },
          { label: '60 Days', value: 60 },
          { label: '90 Days', value: 90 },
        ].map((opt) => (
          <button
            key={opt.value}
            onClick={() => setDays(opt.value)}
            className={`px-3.5 py-1.5 rounded-xl text-sm font-medium transition-colors ${
              days === opt.value
                ? 'bg-primary-600 text-white'
                : 'bg-white text-gray-500 hover:bg-gray-50'
            }`}
          >
            {opt.label}
          </button>
        ))}
      </div>

      {/* Summary Cards */}
      {incomeSpending && (
        <div className="grid grid-cols-3 gap-3">
          <div className="bg-white rounded-2xl shadow-sm p-4">
            <div className="flex items-center gap-2 mb-1">
              <TrendingUp className="w-4 h-4 text-primary-500" />
              <span className="text-xs font-medium text-gray-400">Earned</span>
            </div>
            <p className="text-xl font-bold text-gray-900 font-headline">
              {formatCurrency(incomeSpending.totalIncome)}
            </p>
          </div>
          <div className="bg-white rounded-2xl shadow-sm p-4">
            <div className="flex items-center gap-2 mb-1">
              <TrendingDown className="w-4 h-4 text-secondary-400" />
              <span className="text-xs font-medium text-gray-400">Spent</span>
            </div>
            <p className="text-xl font-bold text-gray-900 font-headline">
              {formatCurrency(incomeSpending.totalSpending)}
            </p>
          </div>
          <div className="bg-white rounded-2xl shadow-sm p-4">
            <div className="flex items-center gap-2 mb-1">
              <PiggyBank className="w-4 h-4 text-tertiary-500" />
              <span className="text-xs font-medium text-gray-400">Saved</span>
            </div>
            <p className="text-xl font-bold text-gray-900 font-headline">
              {formatCurrency(incomeSpending.netSavings)}
            </p>
            <p className="text-xs text-tertiary-600 font-medium mt-0.5">
              {(incomeSpending.savingsRate * 100).toFixed(0)}% rate
            </p>
          </div>
        </div>
      )}

      {/* Balance History Chart */}
      {balanceHistory.length > 0 && (
        <div className="bg-white rounded-2xl shadow-sm p-5">
          <h4 className="text-sm font-semibold text-gray-900 mb-4">Balance Over Time</h4>
          <div className="h-[200px] sm:h-[260px]">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={balanceHistory.map(p => ({
                date: formatDate(p.date),
                balance: p.balance,
              }))}>
                <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
                <XAxis dataKey="date" tick={{ fontSize: 11 }} interval="preserveStartEnd" />
                <YAxis tickFormatter={(value) => `$${value}`} tick={{ fontSize: 11 }} width={50} />
                <Tooltip formatter={(value) => formatCurrency(value as number)} />
                <Line type="monotone" dataKey="balance" stroke="#16a34a" strokeWidth={2.5} dot={false} name="Balance" />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </div>
      )}

      {/* Category Breakdown */}
      {categoryBreakdown.length > 0 && (
        <div className="bg-white rounded-2xl shadow-sm p-5">
          <h4 className="text-sm font-semibold text-gray-900 mb-4">Where Money Goes</h4>
          <div className="space-y-3">
            {categoryBreakdown.map((cat) => {
              const info = getCategoryInfo(cat.category);
              return (
                <div key={cat.category} className="flex items-center gap-3">
                  <div className={`w-8 h-8 rounded-lg ${info.color} flex items-center justify-center text-sm flex-shrink-0`}>
                    {info.emoji}
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between mb-1">
                      <span className="text-sm text-gray-700 truncate">{cat.category.replace(/([A-Z])/g, ' $1').trim()}</span>
                      <span className="text-sm font-medium text-gray-900 ml-2">{cat.percentage.toFixed(0)}%</span>
                    </div>
                    <div className="h-1.5 rounded-full bg-gray-100 overflow-hidden">
                      <div
                        className="h-full rounded-full bg-primary-500 transition-all"
                        style={{ width: `${cat.percentage}%` }}
                      />
                    </div>
                  </div>
                  <span className="text-xs text-gray-400 flex-shrink-0 w-16 text-right">{formatCurrency(cat.amount)}</span>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* Monthly Comparison */}
      {monthlyComparison.length > 0 && (
        <div className="bg-white rounded-2xl shadow-sm p-5">
          <h4 className="text-sm font-semibold text-gray-900 mb-4">Monthly Comparison</h4>
          <div className="h-[200px] sm:h-[260px]">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={monthlyComparison}>
                <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
                <XAxis dataKey="monthName" tick={{ fontSize: 10 }} />
                <YAxis tickFormatter={(value) => `$${value}`} tick={{ fontSize: 11 }} width={50} />
                <Tooltip formatter={(value) => formatCurrency(value as number)} />
                <Bar dataKey="income" fill="#16a34a" name="Earned" radius={[4, 4, 0, 0]} />
                <Bar dataKey="spending" fill="#f59e0b" name="Spent" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>
      )}
    </div>
  );
};
