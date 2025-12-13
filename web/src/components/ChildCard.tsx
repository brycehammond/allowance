import React from 'react';
import { useNavigate } from 'react-router-dom';
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

  return (
    <div className="bg-white overflow-hidden shadow rounded-lg hover:shadow-lg transition-shadow duration-200">
      <div className="p-6">
        {/* Child Name */}
        <div className="flex items-center justify-between mb-4">
          <div>
            <h3 className="text-lg font-medium text-gray-900">{child.fullName}</h3>
            <p className="text-sm text-gray-500">Weekly: {formatCurrency(child.weeklyAllowance)}</p>
          </div>
          <div className="flex-shrink-0">
            <div className="h-12 w-12 rounded-full bg-primary-100 flex items-center justify-center">
              <span className="text-xl font-semibold text-primary-600">
                {child.firstName.charAt(0)}
              </span>
            </div>
          </div>
        </div>

        {/* Balances */}
        <div className="mb-4 space-y-3">
          {/* Total Balance - Prominent */}
          <div>
            <p className="text-3xl font-semibold text-gray-900">
              {formatCurrency(child.currentBalance + child.savingsBalance)}
            </p>
            <p className="text-sm text-gray-600">Total Balance</p>
          </div>

          {/* Spending & Savings - Side by Side */}
          <div className="grid grid-cols-2 gap-3 pt-2 border-t border-gray-100">
            <div>
              <p className="text-lg font-medium text-gray-900">
                {formatCurrency(child.currentBalance)}
              </p>
              <p className="text-xs text-gray-500">Spending</p>
            </div>
            <div>
              <p className="text-lg font-medium text-primary-600">
                {formatCurrency(child.savingsBalance)}
              </p>
              <p className="text-xs text-gray-500">Savings</p>
            </div>
          </div>
        </div>

        {/* Actions */}
        <div className="flex space-x-3">
          <button
            onClick={() => navigate(`/children/${child.id}`)}
            className="flex-1 inline-flex justify-center items-center px-3 py-2 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500"
          >
            View Details
          </button>
          <button
            onClick={() => navigate(`/children/${child.id}`)}
            className="flex-1 inline-flex justify-center items-center px-3 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500"
          >
            Transactions
          </button>
        </div>
      </div>
    </div>
  );
};
