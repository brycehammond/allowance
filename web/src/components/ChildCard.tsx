import React from 'react';
import { useNavigate } from 'react-router-dom';
import { ChevronRight } from 'lucide-react';
import type { Child } from '../types';

interface ChildCardProps {
  child: Child;
}

export const ChildCard: React.FC<ChildCardProps> = ({ child }) => {
  const navigate = useNavigate();
  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  const totalBalance = child.currentBalance + child.savingsBalance;
  const savingsRate = totalBalance > 0
    ? Math.round((child.savingsBalance / totalBalance) * 100)
    : 0;

  return (
    <button
      onClick={() => navigate(`/children/${child.id}`)}
      className="w-full text-left bg-white rounded-2xl shadow-sm hover:shadow-md transition-shadow duration-200 p-5 group"
    >
      <div className="flex items-center justify-between">
        {/* Left: Avatar + Info */}
        <div className="flex items-center gap-3.5 min-w-0">
          <div className="h-11 w-11 rounded-full bg-primary-100 flex items-center justify-center flex-shrink-0">
            <span className="text-lg font-semibold text-primary-600">
              {child.firstName.charAt(0)}
            </span>
          </div>
          <div className="min-w-0">
            <h3 className="text-base font-semibold text-gray-900 truncate">{child.fullName}</h3>
            <p className="text-xs text-gray-400">{formatCurrency(child.weeklyAllowance)}/week</p>
          </div>
        </div>

        {/* Right: Savings badge + Chevron */}
        <div className="flex items-center gap-2 flex-shrink-0">
          {savingsRate > 0 && (
            <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-tertiary-50 text-tertiary-600">
              Saving {savingsRate}%
            </span>
          )}
          <ChevronRight className="h-4.5 w-4.5 text-gray-300 group-hover:text-gray-400 transition-colors" />
        </div>
      </div>

      {/* Balance */}
      <div className="mt-4">
        <p className="text-2xl font-bold text-gray-900 font-headline">
          {formatCurrency(totalBalance)}
        </p>
      </div>

      {/* Spending / Savings breakdown */}
      <div className="mt-3 flex items-center gap-4 text-sm">
        <div className="flex items-center gap-1.5">
          <span className="w-2 h-2 rounded-full bg-primary-500"></span>
          <span className="text-gray-500">Spending</span>
          <span className="font-medium text-gray-700">{formatCurrency(child.currentBalance)}</span>
        </div>
        <div className="flex items-center gap-1.5">
          <span className="w-2 h-2 rounded-full bg-tertiary-500"></span>
          <span className="text-gray-500">Savings</span>
          <span className="font-medium text-gray-700">{formatCurrency(child.savingsBalance)}</span>
        </div>
      </div>

      {/* Ratio bar */}
      {totalBalance > 0 && (
        <div className="mt-3 h-1.5 rounded-full bg-gray-100 overflow-hidden flex">
          <div
            className="bg-primary-500 rounded-l-full"
            style={{ width: `${Math.max(((child.currentBalance / totalBalance) * 100), 2)}%` }}
          />
          <div
            className="bg-tertiary-500 rounded-r-full"
            style={{ width: `${Math.max(((child.savingsBalance / totalBalance) * 100), 2)}%` }}
          />
        </div>
      )}
    </button>
  );
};
